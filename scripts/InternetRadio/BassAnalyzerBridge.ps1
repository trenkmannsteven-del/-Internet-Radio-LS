param(
    [Parameter(Mandatory=$true)][string]$BaseDir,
    [Parameter(Mandatory=$true)][int]$ParentPid
)

$ErrorActionPreference = "Stop"
$statusPath = Join-Path $BaseDir "bass_status.txt"
$startupLog = Join-Path $BaseDir "bass_analyzer_startup.log"

function Write-BoundedLog {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Line
    )

    try {
        $maxBytes = 262144

        if (Test-Path -LiteralPath $Path) {
            try {
                $length = (Get-Item -LiteralPath $Path -ErrorAction Stop).Length
                if ($length -ge $maxBytes) {
                    $marker = (
                        (Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff") +
                        "  [LOG RESET: 256 KiB limit reached]" +
                        [Environment]::NewLine
                    )
                    [IO.File]::WriteAllText(
                        $Path,
                        $marker,
                        [Text.UTF8Encoding]::new($false)
                    )
                }
            } catch {}
        }

        [IO.File]::AppendAllText(
            $Path,
            $Line + [Environment]::NewLine,
            [Text.UTF8Encoding]::new($false)
        )
    } catch {}
}

function Write-Stage([string]$Text) {
    try {
        Write-BoundedLog -Path $startupLog -Line (
            "$([DateTime]::Now.ToString('yyyy-MM-dd HH:mm:ss.fff'))  " + $Text)
    } catch {}
}


function Write-FailedStatus([string]$Message) {
    try {
        $tmp = $statusPath + ".tmp"
        $err64 = ""
        if ($Message) {
            $err64 = [Convert]::ToBase64String(
                [Text.Encoding]::UTF8.GetBytes($Message))
        }
        $data = @(
            "Available=false"
            "LowBass=0"
            "KickBass=0"
            "Mids=0"
            "UtcTicks=$([DateTime]::UtcNow.Ticks)"
            "ErrorBase64=$err64"
        ) -join [Environment]::NewLine

        [IO.File]::WriteAllText(
            $tmp, $data, [Text.UTF8Encoding]::new($false))

        if (Test-Path -LiteralPath $statusPath) {
            Remove-Item -LiteralPath $statusPath -Force -ErrorAction SilentlyContinue
        }
        Move-Item -LiteralPath $tmp -Destination $statusPath -Force
    } catch {}
}

try {
    Write-BoundedLog -Path $startupLog -Line (
        "$([DateTime]::Now.ToString('yyyy-MM-dd HH:mm:ss.fff'))  " +
        "EXTERNAL BASS HELPER START | ParentPid=$ParentPid")
} catch {}

$code = @'
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace InternetRadioLSBass
{
    public static class LoopbackBassMeter
    {
        private static void Stage(string path, string text)
        {
            try
            {
                try
                {
                    if (File.Exists(path))
                    {
                        FileInfo fi = new FileInfo(path);
                        if (fi.Length >= 262144L)
                        {
                            File.WriteAllText(
                                path,
                                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") +
                                "  [LOG RESET: 256 KiB limit reached]" +
                                Environment.NewLine,
                                new System.Text.UTF8Encoding(false));
                        }
                    }
                }
                catch { }

                File.AppendAllText(
                    path,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") +
                    "  CSHARP " + text + Environment.NewLine,
                    new System.Text.UTF8Encoding(false));
            }
            catch { }
        }

        private enum EDataFlow { eRender = 0, eCapture = 1, eAll = 2 }
        private enum ERole { eConsole = 0, eMultimedia = 1, eCommunications = 2 }

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        private struct WAVEFORMATEX
        {
            public ushort wFormatTag;
            public ushort nChannels;
            public uint nSamplesPerSec;
            public uint nAvgBytesPerSec;
            public ushort nBlockAlign;
            public ushort wBitsPerSample;
            public ushort cbSize;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        private struct WAVEFORMATEXTENSIBLE
        {
            public WAVEFORMATEX Format;
            public ushort wValidBitsPerSample;
            public uint dwChannelMask;
            public Guid SubFormat;
        }

        [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        private class MMDeviceEnumeratorComObject { }

        [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            int EnumAudioEndpoints(EDataFlow dataFlow, int stateMask, out IntPtr devices);
            int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice endpoint);
            int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice device);
            int RegisterEndpointNotificationCallback(IntPtr callback);
            int UnregisterEndpointNotificationCallback(IntPtr callback);
        }

        [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            int Activate(ref Guid iid, int clsctx, IntPtr activationParams,
                [MarshalAs(UnmanagedType.IUnknown)] out object result);
            int OpenPropertyStore(int access, out IntPtr properties);
            int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
            int GetState(out int state);
        }

        [ComImport, Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioClient
        {
            int Initialize(int shareMode, uint streamFlags,
                long bufferDuration, long periodicity,
                IntPtr format, IntPtr sessionGuid);
            int GetBufferSize(out uint frames);
            int GetStreamLatency(out long latency);
            int GetCurrentPadding(out uint frames);
            int IsFormatSupported(int shareMode, IntPtr format,
                out IntPtr closestMatch);
            int GetMixFormat(out IntPtr format);
            int GetDevicePeriod(out long defaultPeriod, out long minimumPeriod);
            int Start();
            int Stop();
            int Reset();
            int SetEventHandle(IntPtr handle);
            int GetService(ref Guid iid,
                [MarshalAs(UnmanagedType.IUnknown)] out object service);
        }

        [ComImport, Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioCaptureClient
        {
            int GetBuffer(out IntPtr data, out uint frames, out uint flags,
                out ulong devicePosition, out ulong qpcPosition);
            int ReleaseBuffer(uint frames);
            int GetNextPacketSize(out uint frames);
        }

        [DllImport("ole32.dll")]
        private static extern int CoInitializeEx(IntPtr reserved, uint coInit);

        [DllImport("ole32.dll")]
        private static extern void CoUninitialize();

        private const int CLSCTX_ALL = 23;
        private const int AUDCLNT_SHAREMODE_SHARED = 0;
        private const uint AUDCLNT_STREAMFLAGS_LOOPBACK = 0x00020000;
        private const uint AUDCLNT_BUFFERFLAGS_SILENT = 0x00000002;

        private const ushort WAVE_FORMAT_PCM = 0x0001;
        private const ushort WAVE_FORMAT_IEEE_FLOAT = 0x0003;
        private const ushort WAVE_FORMAT_EXTENSIBLE = 0xFFFE;

        private static readonly Guid PcmGuid =
            new Guid("00000001-0000-0010-8000-00AA00389B71");
        private static readonly Guid FloatGuid =
            new Guid("00000003-0000-0010-8000-00AA00389B71");

        private const int N = 4096;
        private const int Hop = 1280;

        private static readonly double[] ring = new double[N];
        private static readonly double[] window = BuildWindow();

        private static int writeIndex;
        private static int ringCount;
        private static int sinceAnalysis;
        private static string diagLog = null;
        private static bool loggedFirstAnalysis = false;
        private static DateTime lastStatusPublish = DateTime.MinValue;
        private static double latestLow = 0.0;
        private static double latestKick = 0.0;
        private static double latestMids = 0.0;

        public static void Run(string statusPath, string startupLog, int parentPid)
        {
            bool comInitialised = false;
            IMMDeviceEnumerator enumerator = null;
            IMMDevice device = null;
            object clientObject = null;
            object captureObject = null;
            IAudioClient client = null;
            IAudioCaptureClient capture = null;
            IntPtr mixPtr = IntPtr.Zero;

            try
            {
                Stage(startupLog, "RUN ENTER");
                int hr = CoInitializeEx(IntPtr.Zero, 0);
                // RPC_E_CHANGED_MODE means COM was already initialized by PowerShell.
                // COM calls can still be used; only call CoUninitialize if this call succeeded.
                comInitialised = hr >= 0;
                Stage(startupLog, "COM INIT HRESULT=" + hr);

                enumerator =
                    (IMMDeviceEnumerator)(new MMDeviceEnumeratorComObject());
                Stage(startupLog, "MMDEVICE ENUMERATOR CREATED");

                // Prefer the normal console default endpoint used by games/music playback.
                hr = enumerator.GetDefaultAudioEndpoint(
                    EDataFlow.eRender, ERole.eConsole, out device);

                if (hr != 0 || device == null)
                {
                    Stage(startupLog, "eConsole endpoint failed HRESULT=" + hr + " | trying eMultimedia");
                    hr = enumerator.GetDefaultAudioEndpoint(
                        EDataFlow.eRender, ERole.eMultimedia, out device);
                }

                if (hr != 0 || device == null)
                    throw new Exception(
                        "Default render endpoint unavailable. HRESULT=" + hr);

                Stage(startupLog, "DEFAULT RENDER ENDPOINT OK");

                Guid clientGuid = typeof(IAudioClient).GUID;
                hr = device.Activate(
                    ref clientGuid, CLSCTX_ALL, IntPtr.Zero, out clientObject);

                if (hr != 0 || clientObject == null)
                    throw new Exception(
                        "IAudioClient Activate failed. HRESULT=" + hr);

                Stage(startupLog, "IAUDIOCLIENT ACTIVATE OK");

                client = clientObject as IAudioClient;
                if (client == null)
                    throw new Exception("IAudioClient cast failed.");

                hr = client.GetMixFormat(out mixPtr);
                if (hr != 0 || mixPtr == IntPtr.Zero)
                    throw new Exception(
                        "GetMixFormat failed. HRESULT=" + hr);

                WAVEFORMATEX fmt =
                    (WAVEFORMATEX)Marshal.PtrToStructure(
                        mixPtr, typeof(WAVEFORMATEX));

                if (fmt.nChannels < 1 ||
                    fmt.nChannels > 16 ||
                    fmt.nSamplesPerSec < 8000)
                    throw new Exception("Unsupported Windows mix format.");

                Stage(
                    startupLog,
                    "MIX FORMAT rate=" + fmt.nSamplesPerSec +
                    " channels=" + fmt.nChannels +
                    " bits=" + fmt.wBitsPerSample +
                    " tag=" + fmt.wFormatTag);

                bool isFloat =
                    fmt.wFormatTag == WAVE_FORMAT_IEEE_FLOAT;
                bool isPcm =
                    fmt.wFormatTag == WAVE_FORMAT_PCM;

                if (fmt.wFormatTag == WAVE_FORMAT_EXTENSIBLE &&
                    fmt.cbSize >= 22)
                {
                    WAVEFORMATEXTENSIBLE ext =
                        (WAVEFORMATEXTENSIBLE)Marshal.PtrToStructure(
                            mixPtr, typeof(WAVEFORMATEXTENSIBLE));

                    isFloat = ext.SubFormat == FloatGuid;
                    isPcm = ext.SubFormat == PcmGuid;
                }

                if (!isFloat && !isPcm)
                    throw new Exception("Unsupported audio subtype.");

                hr = client.Initialize(
                    AUDCLNT_SHAREMODE_SHARED,
                    AUDCLNT_STREAMFLAGS_LOOPBACK,
                    1000000L,
                    0L,
                    mixPtr,
                    IntPtr.Zero);

                if (hr != 0)
                    throw new Exception(
                        "Loopback Initialize failed. HRESULT=" + hr);

                Stage(startupLog, "LOOPBACK INITIALIZE OK");

                Guid captureGuid = typeof(IAudioCaptureClient).GUID;
                hr = client.GetService(ref captureGuid, out captureObject);

                if (hr != 0 || captureObject == null)
                    throw new Exception(
                        "IAudioCaptureClient failed. HRESULT=" + hr);

                capture = captureObject as IAudioCaptureClient;
                if (capture == null)
                    throw new Exception(
                        "IAudioCaptureClient cast failed.");

                Stage(startupLog, "CAPTURE CLIENT SERVICE OK");

                writeIndex = 0;
                ringCount = 0;
                sinceAnalysis = 0;
                diagLog = startupLog;
                loggedFirstAnalysis = false;
                lastStatusPublish = DateTime.MinValue;
                latestLow = 0.0;
                latestKick = 0.0;
                latestMids = 0.0;

                hr = client.Start();
                if (hr != 0)
                    throw new Exception(
                        "Audio client start failed. HRESULT=" + hr);

                Stage(startupLog, "AUDIO CLIENT START OK");
                // Mark capture path active immediately. Values remain zero until
                // enough samples arrive for the first frequency analysis.
                WriteStatus(statusPath, true, 0.0, 0.0, 0.0);
                Stage(startupLog, "STATUS AVAILABLE=TRUE WRITTEN");

                float[] floatBuffer = null;
                short[] shortBuffer = null;
                int[] intBuffer = null;
                byte[] byteBuffer = null;

                DateTime parentCheck = DateTime.UtcNow;
                bool loggedFirstPacket = false;

                while (true)
                {
                    if ((DateTime.UtcNow - parentCheck).TotalMilliseconds >= 1000.0)
                    {
                        parentCheck = DateTime.UtcNow;
                        try
                        {
                            Process p = Process.GetProcessById(parentPid);
                            if (p == null || p.HasExited) break;
                        }
                        catch { break; }
                    }

                    uint packetFrames;
                    hr = capture.GetNextPacketSize(out packetFrames);

                    if (hr != 0)
                        throw new Exception(
                            "GetNextPacketSize failed. HRESULT=" + hr);

                    if (packetFrames == 0)
                    {
                        Thread.Sleep(4);
                        continue;
                    }

                    while (packetFrames > 0)
                    {
                        if (!loggedFirstPacket)
                        {
                            loggedFirstPacket = true;
                            Stage(startupLog, "FIRST AUDIO PACKET frames=" + packetFrames);
                        }

                        IntPtr data;
                        uint frames;
                        uint flags;
                        ulong devicePos;
                        ulong qpcPos;

                        hr = capture.GetBuffer(
                            out data, out frames, out flags,
                            out devicePos, out qpcPos);

                        if (hr != 0)
                            throw new Exception(
                                "GetBuffer failed. HRESULT=" + hr);

                        try
                        {
                            if ((flags & AUDCLNT_BUFFERFLAGS_SILENT) != 0 ||
                                data == IntPtr.Zero)
                            {
                                for (uint f = 0; f < frames; f++)
                                    Push(
                                        0.0,
                                        (int)fmt.nSamplesPerSec,
                                        statusPath);
                            }
                            else
                            {
                                int channels = fmt.nChannels;
                                int sampleCount =
                                    checked((int)frames * channels);

                                if (isFloat && fmt.wBitsPerSample == 32)
                                {
                                    if (floatBuffer == null ||
                                        floatBuffer.Length < sampleCount)
                                        floatBuffer =
                                            new float[sampleCount];

                                    Marshal.Copy(
                                        data, floatBuffer, 0, sampleCount);

                                    for (int f = 0; f < (int)frames; f++)
                                    {
                                        double sum = 0.0;
                                        int o = f * channels;

                                        for (int ch = 0;
                                             ch < channels;
                                             ch++)
                                            sum += floatBuffer[o + ch];

                                        Push(
                                            sum / channels,
                                            (int)fmt.nSamplesPerSec,
                                            statusPath);
                                    }
                                }
                                else if (isPcm &&
                                         fmt.wBitsPerSample == 16)
                                {
                                    if (shortBuffer == null ||
                                        shortBuffer.Length < sampleCount)
                                        shortBuffer =
                                            new short[sampleCount];

                                    Marshal.Copy(
                                        data, shortBuffer, 0, sampleCount);

                                    for (int f = 0; f < (int)frames; f++)
                                    {
                                        double sum = 0.0;
                                        int o = f * channels;

                                        for (int ch = 0;
                                             ch < channels;
                                             ch++)
                                            sum +=
                                                shortBuffer[o + ch] /
                                                32768.0;

                                        Push(
                                            sum / channels,
                                            (int)fmt.nSamplesPerSec,
                                            statusPath);
                                    }
                                }
                                else if (isPcm &&
                                         fmt.wBitsPerSample == 32)
                                {
                                    if (intBuffer == null ||
                                        intBuffer.Length < sampleCount)
                                        intBuffer =
                                            new int[sampleCount];

                                    Marshal.Copy(
                                        data, intBuffer, 0, sampleCount);

                                    for (int f = 0; f < (int)frames; f++)
                                    {
                                        double sum = 0.0;
                                        int o = f * channels;

                                        for (int ch = 0;
                                             ch < channels;
                                             ch++)
                                            sum +=
                                                intBuffer[o + ch] /
                                                2147483648.0;

                                        Push(
                                            sum / channels,
                                            (int)fmt.nSamplesPerSec,
                                            statusPath);
                                    }
                                }
                                else if (isPcm &&
                                         fmt.wBitsPerSample == 24)
                                {
                                    int byteCount =
                                        checked(
                                            (int)frames *
                                            fmt.nBlockAlign);

                                    if (byteBuffer == null ||
                                        byteBuffer.Length < byteCount)
                                        byteBuffer =
                                            new byte[byteCount];

                                    Marshal.Copy(
                                        data, byteBuffer, 0, byteCount);

                                    for (int f = 0; f < (int)frames; f++)
                                    {
                                        double sum = 0.0;
                                        int frameOffset =
                                            f * fmt.nBlockAlign;

                                        for (int ch = 0;
                                             ch < channels;
                                             ch++)
                                        {
                                            int o =
                                                frameOffset + ch * 3;

                                            int v =
                                                byteBuffer[o] |
                                                (byteBuffer[o + 1] << 8) |
                                                (byteBuffer[o + 2] << 16);

                                            if ((v & 0x800000) != 0)
                                                v |=
                                                    unchecked(
                                                        (int)0xFF000000);

                                            sum +=
                                                v / 8388608.0;
                                        }

                                        Push(
                                            sum / channels,
                                            (int)fmt.nSamplesPerSec,
                                            statusPath);
                                    }
                                }
                            }
                        }
                        finally
                        {
                            try { capture.ReleaseBuffer(frames); }
                            catch { }
                        }

                        hr = capture.GetNextPacketSize(
                            out packetFrames);

                        if (hr != 0)
                            throw new Exception(
                                "GetNextPacketSize failed. HRESULT=" + hr);
                    }
                }

                try { client.Stop(); } catch { }
            }
            finally
            {
                if (mixPtr != IntPtr.Zero)
                    try { Marshal.FreeCoTaskMem(mixPtr); } catch { }

                if (captureObject != null &&
                    Marshal.IsComObject(captureObject))
                    try { Marshal.ReleaseComObject(captureObject); }
                    catch { }

                if (clientObject != null &&
                    Marshal.IsComObject(clientObject))
                    try { Marshal.ReleaseComObject(clientObject); }
                    catch { }

                if (device != null &&
                    Marshal.IsComObject(device))
                    try { Marshal.ReleaseComObject(device); }
                    catch { }

                if (enumerator != null &&
                    Marshal.IsComObject(enumerator))
                    try { Marshal.ReleaseComObject(enumerator); }
                    catch { }

                if (comInitialised)
                    try { CoUninitialize(); } catch { }
            }
        }

        private static void Push(
            double sample, int sampleRate, string statusPath)
        {
            if (Double.IsNaN(sample) || Double.IsInfinity(sample))
                sample = 0.0;

            if (sample > 1.5) sample = 1.5;
            if (sample < -1.5) sample = -1.5;

            ring[writeIndex] = sample;

            writeIndex++;
            if (writeIndex >= N) writeIndex = 0;

            if (ringCount < N) ringCount++;

            sinceAnalysis++;

            // TEST56: keep the helper responsive enough for short kick transients
            // without returning to the old high-rate workload. 30 analyses/sec is
            // still only one fifth of the ~150/sec case seen at 192 kHz.
            int effectiveHop = Math.Max(Hop, sampleRate / 30);

            if (ringCount >= N &&
                sinceAnalysis >= effectiveHop)
            {
                sinceAnalysis = 0;
                Analyze(sampleRate, statusPath);
            }
        }

        private static void Analyze(
            int sampleRate, string statusPath)
        {
            double low =
                Band(
                    sampleRate,
                    new double[] {
                        32, 42, 52, 64, 78
                    });

            double kick =
                Band(
                    sampleRate,
                    new double[] {
                        86, 100, 116, 134, 154, 176
                    });

            double mids =
                Band(
                    sampleRate,
                    new double[] {
                        260, 360, 520, 760,
                        1050, 1450, 1850
                    });

            latestLow = low;
            latestKick = kick;
            latestMids = mids;

            // TEST56: publish in step with the 30 Hz analyzer so GTA sees each
            // fresh transient quickly, while still keeping file I/O tightly bounded.
            DateTime now = DateTime.UtcNow;
            if (lastStatusPublish == DateTime.MinValue ||
                (now - lastStatusPublish).TotalMilliseconds >= 33.0)
            {
                lastStatusPublish = now;
                WriteStatus(
                    statusPath, true, latestLow, latestKick, latestMids);
            }

            if (!loggedFirstAnalysis)
            {
                loggedFirstAnalysis = true;
                Stage(
                    diagLog,
                    "FIRST ANALYSIS low=" +
                    low.ToString("0.000000", CultureInfo.InvariantCulture) +
                    " kick=" +
                    kick.ToString("0.000000", CultureInfo.InvariantCulture) +
                    " mids=" +
                    mids.ToString("0.000000", CultureInfo.InvariantCulture));
            }
        }

        private static double Band(
            int sampleRate, double[] frequencies)
        {
            double sum = 0.0;

            for (int i = 0;
                 i < frequencies.Length;
                 i++)
            {
                double m =
                    Goertzel(
                        sampleRate,
                        frequencies[i]);

                sum += m * m;
            }

            return Math.Sqrt(
                sum / frequencies.Length);
        }

        private static double Goertzel(
            int sampleRate, double frequency)
        {
            if (frequency <= 0.0 ||
                frequency >= sampleRate * 0.5)
                return 0.0;

            double omega =
                2.0 * Math.PI *
                frequency / sampleRate;

            double coeff =
                2.0 * Math.Cos(omega);

            double s0 = 0.0;
            double s1 = 0.0;
            double s2 = 0.0;

            int idx = writeIndex;

            for (int i = 0; i < N; i++)
            {
                double x =
                    ring[idx] * window[i];

                s0 =
                    x + coeff * s1 - s2;

                s2 = s1;
                s1 = s0;

                idx++;
                if (idx >= N) idx = 0;
            }

            double power =
                s1 * s1 +
                s2 * s2 -
                coeff * s1 * s2;

            if (power <= 0.0)
                return 0.0;

            return
                Math.Sqrt(power) *
                (4.0 / N);
        }

        private static double[] BuildWindow()
        {
            double[] w =
                new double[N];

            for (int i = 0; i < N; i++)
                w[i] =
                    0.5 -
                    0.5 *
                    Math.Cos(
                        (2.0 * Math.PI * i) /
                        (N - 1));

            return w;
        }

        private static void WriteStatus(
            string path,
            bool available,
            double low,
            double kick,
            double mids)
        {
            try
            {
                CultureInfo inv =
                    CultureInfo.InvariantCulture;

                string temp =
                    path + ".tmp";

                string data =
                    "Available=" +
                    available.ToString().ToLowerInvariant() +
                    Environment.NewLine +
                    "LowBass=" +
                    low.ToString("0.00000000", inv) +
                    Environment.NewLine +
                    "KickBass=" +
                    kick.ToString("0.00000000", inv) +
                    Environment.NewLine +
                    "Mids=" +
                    mids.ToString("0.00000000", inv) +
                    Environment.NewLine +
                    "UtcTicks=" +
                    DateTime.UtcNow.Ticks.ToString(inv) +
                    Environment.NewLine;

                File.WriteAllText(
                    temp,
                    data,
                    new System.Text.UTF8Encoding(false));

                if (File.Exists(path))
                {
                    try
                    {
                        File.Replace(
                            temp,
                            path,
                            null,
                            true);
                    }
                    catch
                    {
                        try { File.Delete(path); }
                        catch { }

                        File.Move(temp, path);
                    }
                }
                else
                {
                    File.Move(temp, path);
                }
            }
            catch { }
        }
    }
}
'@

try {
    Write-Stage "PS BEFORE ADD-TYPE"
    Add-Type -TypeDefinition $code -Language CSharp -ErrorAction Stop
    Write-Stage "PS AFTER ADD-TYPE"
    Write-FailedStatus ""
    Write-Stage "PS BEFORE LOOPBACK RUN"
    [InternetRadioLSBass.LoopbackBassMeter]::Run(
        $statusPath, $startupLog, $ParentPid)
    Write-Stage "PS LOOPBACK RUN RETURNED"
    Write-FailedStatus ""
}
catch {
    $msg = $_.Exception.ToString()

    try {
        Write-BoundedLog -Path $startupLog -Line (
            "$([DateTime]::Now.ToString('yyyy-MM-dd HH:mm:ss.fff'))  " +
            "ERROR | $msg")
    } catch {}

    Write-FailedStatus $msg
    Start-Sleep -Milliseconds 250
}
