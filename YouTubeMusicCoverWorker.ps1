param(
    [Parameter(Mandatory=$true)][string]$BaseDir,
    [Parameter(Mandatory=$true)][string]$Key,
    [string]$ExpectedTitleBase64 = "",
    [string]$ExpectedArtistBase64 = ""
)

$ErrorActionPreference = "Stop"
$coverDir = Join-Path $BaseDir "ytmusic_covers"
$logPath = Join-Path $BaseDir "ytmusic_cover_worker.log"
New-Item -ItemType Directory -Path $coverDir -Force | Out-Null

function Log([string]$s) {
    try { Add-Content -LiteralPath $logPath -Value ((Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff") + "  " + $s) -Encoding UTF8 } catch {}
}
function From-B64([string]$s) {
    try { if([string]::IsNullOrWhiteSpace($s)){return ""}; return [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($s)) } catch { return "" }
}
$expectedTitle = From-B64 $ExpectedTitleBase64
$expectedArtist = From-B64 $ExpectedArtistBase64

try {
    Add-Type -AssemblyName System.Runtime.WindowsRuntime -ErrorAction Stop
    [void][Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager, Windows.Media.Control, ContentType=WindowsRuntime]
    [void][Windows.Media.Control.GlobalSystemMediaTransportControlsSessionMediaProperties, Windows.Media.Control, ContentType=WindowsRuntime]
    [void][Windows.Storage.Streams.IRandomAccessStreamWithContentType, Windows.Storage.Streams, ContentType=WindowsRuntime]
} catch { Log ("WINRT LOAD FEHLER | " + $_.Exception.Message); exit 11 }

try {
    Add-Type -AssemblyName System.Drawing -ErrorAction Stop
} catch { Log ("SYSTEM.DRAWING LOAD FEHLER | " + $_.Exception.Message); exit 14 }

$script:asTaskGeneric = ([System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {
    $_.Name -eq "AsTask" -and $_.IsGenericMethod -and $_.GetParameters().Count -eq 1
} | Select-Object -First 1)
if($null -eq $script:asTaskGeneric) { Log "AsTask<T> fehlt"; exit 12 }
function Await-WinRT([object]$Operation, [Type]$ResultType) {
    $m = $script:asTaskGeneric.MakeGenericMethod($ResultType)
    $task = $m.Invoke($null, @($Operation))
    $task.Wait()
    return $task.Result
}

# IRandomAccessStream -> COM IStream. Dieser Prozess ist absichtlich isoliert und darf
# vom Haupt-Bridge-Prozess hart beendet werden, falls ein Browser-Thumbnail haengt.
$readerSource = @'
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
public static class SafeYtCoverReader
{
    [DllImport("shcore.dll", PreserveSig=true)]
    private static extern int CreateStreamOverRandomAccessStream(
        IntPtr randomAccessStream,
        ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IStream stream);

    public static byte[] ReadAll(object randomAccessStream, int maxBytes)
    {
        if (randomAccessStream == null) return null;
        IntPtr unk = IntPtr.Zero;
        IStream s = null;
        try
        {
            unk = Marshal.GetIUnknownForObject(randomAccessStream);
            Guid iid = new Guid("0000000C-0000-0000-C000-000000000046");
            int hr = CreateStreamOverRandomAccessStream(unk, ref iid, out s);
            if (hr != 0 || s == null) Marshal.ThrowExceptionForHR(hr);
            using (MemoryStream ms = new MemoryStream())
            {
                byte[] buffer = new byte[32768];
                IntPtr pcb = Marshal.AllocHGlobal(sizeof(int));
                try
                {
                    while (ms.Length < maxBytes)
                    {
                        Marshal.WriteInt32(pcb, 0);
                        s.Read(buffer, buffer.Length, pcb);
                        int got = Marshal.ReadInt32(pcb);
                        if (got <= 0) break;
                        ms.Write(buffer, 0, got);
                    }
                }
                finally { Marshal.FreeHGlobal(pcb); }
                if (ms.Length >= maxBytes) throw new InvalidOperationException("Cover groesser als Limit");
                return ms.ToArray();
            }
        }
        finally
        {
            if (s != null) try { Marshal.ReleaseComObject(s); } catch {}
            if (unk != IntPtr.Zero) Marshal.Release(unk);
        }
    }
}
'@
try { Add-Type -TypeDefinition $readerSource -Language CSharp -ErrorAction Stop } catch { Log ("ISTREAM HELPER FEHLER | " + $_.Exception.Message); exit 13 }

$managerType = [Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager, Windows.Media.Control, ContentType=WindowsRuntime]
$mediaPropsType = [Windows.Media.Control.GlobalSystemMediaTransportControlsSessionMediaProperties, Windows.Media.Control, ContentType=WindowsRuntime]
$randomAccessType = [Windows.Storage.Streams.IRandomAccessStreamWithContentType, Windows.Storage.Streams, ContentType=WindowsRuntime]

function Score-Source([string]$src, [string]$state, [string]$title, [string]$artist) {
    $score = 0
    $s = if($null -eq $src){""}else{$src.ToLowerInvariant()}
    if($s.Contains("chrome")){ $score += 100 }
    elseif($s.Contains("msedge") -or $s.Contains("edge")){ $score += 90 }
    elseif($s.Contains("firefox")){ $score += 80 }
    if($state -eq "Playing"){ $score += 30 }
    if(-not [string]::IsNullOrWhiteSpace($expectedTitle) -and $title -eq $expectedTitle){ $score += 200 }
    if(-not [string]::IsNullOrWhiteSpace($expectedArtist) -and $artist -eq $expectedArtist){ $score += 80 }
    return $score
}

$manager = $null
try {
    $manager = Await-WinRT ($managerType::RequestAsync()) $managerType
    if($null -eq $manager){ Log "MANAGER null"; exit 20 }
    $best = $null
    foreach($session in @($manager.GetSessions())) {
        try {
            $props = Await-WinRT ($session.TryGetMediaPropertiesAsync()) $mediaPropsType
            if($null -eq $props){ continue }
            $state = "Unknown"
            try { $state = [string]$session.GetPlaybackInfo().PlaybackStatus } catch {}
            $src = ""
            try { $src = [string]$session.SourceAppUserModelId } catch {}
            $title = [string]$props.Title
            $artist = [string]$props.Artist
            $score = Score-Source $src $state $title $artist
            if($null -eq $best -or $score -gt $best.Score) {
                $best = [pscustomobject]@{ Session=$session; Props=$props; Score=$score; Source=$src; Title=$title; Artist=$artist }
            }
        } catch {}
    }
    if($null -eq $best){ Log "KEINE SESSION"; exit 21 }
    if(-not [string]::IsNullOrWhiteSpace($expectedTitle) -and $best.Title -ne $expectedTitle) {
        Log ("TRACK GEWECHSELT | erwartet=" + $expectedTitle + " | gefunden=" + $best.Title)
        exit 22
    }
    $thumb = $null
    try { $thumb = $best.Props.Thumbnail } catch {}
    if($null -eq $thumb){ Log "KEIN THUMBNAIL"; exit 23 }

    Log ("READ START | " + $best.Source + " | " + $best.Title)
    $ras = Await-WinRT ($thumb.OpenReadAsync()) $randomAccessType
    if($null -eq $ras){ Log "OpenReadAsync null"; exit 24 }
    try {
        [byte[]]$bytes = [SafeYtCoverReader]::ReadAll($ras, 8388608)
    } finally { try { $ras.Dispose() } catch {} }
    if($null -eq $bytes -or $bytes.Length -lt 1024){ Log ("LEER/ZU KLEIN | Bytes=" + $(if($null -eq $bytes){0}else{$bytes.Length})); exit 25 }

    # V12.8: NIEMALS die Browser-Rohdaten direkt an GTA weiterreichen.
    # Erst vollstaendig dekodieren und als standardisiertes 32-bit PNG neu encodieren.
    $rawSig = "unknown"
    if($bytes.Length -ge 4 -and $bytes[0] -eq 0xFF -and $bytes[1] -eq 0xD8){ $rawSig = "jpeg" }
    elseif($bytes.Length -ge 8 -and $bytes[0] -eq 0x89 -and $bytes[1] -eq 0x50 -and $bytes[2] -eq 0x4E -and $bytes[3] -eq 0x47){ $rawSig = "png" }
    elseif($bytes.Length -ge 12 -and [Text.Encoding]::ASCII.GetString($bytes,0,4) -eq "RIFF" -and [Text.Encoding]::ASCII.GetString($bytes,8,4) -eq "WEBP"){ $rawSig = "webp" }
    Log ("RAW COVER | Type=" + $rawSig + " | Bytes=" + $bytes.Length)

    $final = Join-Path $coverDir ("ytm_ready_" + $Key + ".png")
    $tmp = Join-Path $coverDir ("ytm_ready_" + $Key + ".png.tmp")
    try { Remove-Item -LiteralPath $tmp -Force -ErrorAction SilentlyContinue } catch {}

    $ms = $null
    $img = $null
    $bmp = $null
    $gfx = $null
    try {
        $ms = New-Object IO.MemoryStream(,$bytes)
        # validateImageData=true zwingt GDI+ zu einer echten Dekodierung statt nur Header-Erkennung.
        $img = [Drawing.Image]::FromStream($ms, $true, $true)
        if($null -eq $img -or $img.Width -lt 8 -or $img.Height -lt 8 -or $img.Width -gt 8192 -or $img.Height -gt 8192) {
            throw New-Object InvalidOperationException("Ungueltige Cover-Abmessungen")
        }

        $side = 512
        $bmp = New-Object Drawing.Bitmap($side, $side, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $gfx = [Drawing.Graphics]::FromImage($bmp)
        $gfx.Clear([Drawing.Color]::FromArgb(255, 10, 12, 16))
        $gfx.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $gfx.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::HighQuality
        $gfx.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality

        $scale = [Math]::Min($side / [double]$img.Width, $side / [double]$img.Height)
        $dw = [Math]::Max(1, [int][Math]::Round($img.Width * $scale))
        $dh = [Math]::Max(1, [int][Math]::Round($img.Height * $scale))
        $dx = [int](($side - $dw) / 2)
        $dy = [int](($side - $dh) / 2)
        $gfx.DrawImage($img, $dx, $dy, $dw, $dh)
        $gfx.Dispose(); $gfx = $null
        $bmp.Save($tmp, [Drawing.Imaging.ImageFormat]::Png)
    } catch {
        Log ("COVER DECODE/REENCODE FEHLER | Type=" + $rawSig + " | " + $_.Exception.Message)
        exit 27
    } finally {
        if($null -ne $gfx){ try { $gfx.Dispose() } catch {} }
        if($null -ne $bmp){ try { $bmp.Dispose() } catch {} }
        if($null -ne $img){ try { $img.Dispose() } catch {} }
        if($null -ne $ms){ try { $ms.Dispose() } catch {} }
    }

    # Zweite Validierung: nur ein von GDI+ erneut lesbares 512x512-PNG darf freigegeben werden.
    $verify = $null
    try {
        $fi = Get-Item -LiteralPath $tmp -ErrorAction Stop
        if($fi.Length -lt 2048 -or $fi.Length -gt 4194304){ throw New-Object InvalidOperationException("PNG-Dateigroesse ausserhalb Limit: " + $fi.Length) }
        $verify = [Drawing.Image]::FromFile($tmp)
        if($verify.Width -ne 512 -or $verify.Height -ne 512){ throw New-Object InvalidOperationException("PNG-Verifikation falsche Abmessung") }
    } catch {
        Log ("COVER VERIFY FEHLER | " + $_.Exception.Message)
        try { Remove-Item -LiteralPath $tmp -Force -ErrorAction SilentlyContinue } catch {}
        exit 28
    } finally { if($null -ne $verify){ try { $verify.Dispose() } catch {} } }

    Move-Item -LiteralPath $tmp -Destination $final -Force
    $readyInfo = Get-Item -LiteralPath $final
    Log ("COVER READY NORMALIZED | " + $final + " | " + $readyInfo.Length + " Bytes | 512x512")

    # V12.13: Cover-Cache begrenzen. Immer nur die 3 neuesten validierten Cover behalten.
    # Die Bereinigung laeuft hier im externen Worker und niemals im GTA-Frame-Renderer.
    try {
        $readyCovers = @(Get-ChildItem -LiteralPath $coverDir -File -Filter "ytm_ready_*.png" -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTimeUtc -Descending)
        if($readyCovers.Count -gt 3) {
            $oldCovers = @($readyCovers | Select-Object -Skip 3)
            foreach($oldCover in $oldCovers) {
                try {
                    Remove-Item -LiteralPath $oldCover.FullName -Force -ErrorAction Stop
                    Log ("COVER CACHE DELETE | " + $oldCover.Name)
                } catch {
                    Log ("COVER CACHE DELETE FEHLER | " + $oldCover.Name + " | " + $_.Exception.Message)
                }
            }
        }
        Log ("COVER CACHE | Behalten=" + ([Math]::Min(3, $readyCovers.Count)) + " | Gefunden=" + $readyCovers.Count)
    } catch {
        Log ("COVER CACHE FEHLER | " + $_.Exception.Message)
    }

    exit 0
} catch {
    Log ("WORKER FEHLER | " + $_.Exception.ToString())
    exit 99
}
