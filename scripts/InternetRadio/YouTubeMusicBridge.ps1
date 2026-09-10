param(
    [Parameter(Mandatory=$true)][string]$BaseDir,
    [int]$ParentPid = 0,
    [int]$AutoLaunch = 1,
    [int]$AutoPlay = 1,
    [int]$ArtworkEnabled = 0
)

$ErrorActionPreference = "Stop"
$commandDir = Join-Path $BaseDir "ytmusic_commands"
$statusPath = Join-Path $BaseDir "ytmusic_status.txt"
$logPath = Join-Path $BaseDir "ytmusic_bridge.log"
$openResultPath = Join-Path $BaseDir "ytmusic_open_result.txt"
$coverDir = Join-Path $BaseDir "ytmusic_covers"
$coverWorkerPath = Join-Path $BaseDir "YouTubeMusicCoverWorker.ps1"
$script:coverWorker = $null
$script:coverWorkerKey = ""
$script:coverWorkerStarted = Get-Date "2000-01-01"
$script:coverLastAttempt = @{}
$script:launchedByBridge = $false
$script:autoLaunchAttempted = $false
$script:lastYtUiScan = Get-Date "2000-01-01"
$script:lastYtUiPresent = $false
$script:artworkEnabled = ([int]$ArtworkEnabled -ne 0)

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

function Write-Log([string]$Text) {
    try { Write-BoundedLog -Path $logPath -Line ((Get-Date).ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + $Text) } catch {}
}
function To-B64([string]$Text) {
    if($null -eq $Text) { $Text = "" }
    return [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($Text))
}

# V12.10: YouTube Music beim Ende von GTA wirklich schliessen, ohne pauschal
# den kompletten Browser abzuschießen. Zuerst wird per UI Automation ein
# YouTube-Music-Tab gesucht; bei einer PWA/einem eigenen Fenster wird nur das
# passende Hauptfenster geschlossen.
$script:closeUiReady = $false
function Ensure-CloseUi {
    if($script:closeUiReady) { return $true }
    try {
        Add-Type -AssemblyName UIAutomationClient -ErrorAction Stop
        Add-Type -AssemblyName UIAutomationTypes -ErrorAction Stop
        Add-Type -AssemblyName System.Windows.Forms -ErrorAction Stop
        try {
            Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class YtmWindowNative
{
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
}
'@ -ErrorAction SilentlyContinue
        } catch {}
        $script:closeUiReady = $true
        return $true
    } catch {
        Write-Log ("YT CLOSE UI INIT FEHLER | " + $_.Exception.Message)
        return $false
    }
}

function Is-YtMusicText([string]$Text) {
    if([string]::IsNullOrWhiteSpace($Text)) { return $false }
    $t = $Text.ToLowerInvariant()
    return ($t.Contains("youtube music") -or $t.Contains("music.youtube.com"))
}

function Test-YoutubeMusicUiRaw {
    $browserNames = @("chrome", "msedge", "firefox")
    foreach($name in $browserNames) {
        try {
            $procs = @(Get-Process -Name $name -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 })
            foreach($proc in $procs) {
                $title = ""
                try { $title = [string]$proc.MainWindowTitle } catch {}
                if(Is-YtMusicText $title) { return $true }
            }
        } catch {}
    }
    if(Ensure-CloseUi) {
        foreach($name in $browserNames) {
            try {
                $procs = @(Get-Process -Name $name -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 })
                foreach($proc in $procs) {
                    try {
                        $root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)
                        if($null -eq $root) { continue }
                        $cond = New-Object System.Windows.Automation.PropertyCondition(
                            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
                            [System.Windows.Automation.ControlType]::TabItem)
                        $tabs = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
                        for($i = 0; $i -lt $tabs.Count; $i++) {
                            $tabName = ""
                            try { $tabName = [string]$tabs.Item($i).Current.Name } catch {}
                            if(Is-YtMusicText $tabName) { return $true }
                        }
                    } catch {}
                }
            } catch {}
        }
    }
    return $false
}

function Test-YoutubeMusicUi {
    # TEST59: UI Automation can be surprisingly expensive with many browser tabs.
    # Cache the result briefly; command/detection logic stays responsive while the
    # background bridge avoids repeatedly walking the whole browser accessibility tree.
    $now = Get-Date
    if(($now - $script:lastYtUiScan).TotalMilliseconds -lt 700) { return $script:lastYtUiPresent }
    $script:lastYtUiScan = $now
    $present = $false
    try { $present = [bool](Test-YoutubeMusicUiRaw) } catch { $present = $false }
    $script:lastYtUiPresent = $present
    return $present
}

function Find-BrowserExe([string]$Browser) {
    $candidates = @()
    if($Browser -eq "chrome") {
        try { $candidates += (Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe' -ErrorAction SilentlyContinue).'(default)' } catch {}
        try { $candidates += (Get-ItemProperty 'HKLM:\Software\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe' -ErrorAction SilentlyContinue).'(default)' } catch {}
        $candidates += (Join-Path $env:LOCALAPPDATA 'Google\Chrome\Application\chrome.exe')
        $candidates += (Join-Path $env:ProgramFiles 'Google\Chrome\Application\chrome.exe')
        if(${env:ProgramFiles(x86)}) { $candidates += (Join-Path ${env:ProgramFiles(x86)} 'Google\Chrome\Application\chrome.exe') }
    } elseif($Browser -eq "msedge") {
        try { $candidates += (Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe' -ErrorAction SilentlyContinue).'(default)' } catch {}
        try { $candidates += (Get-ItemProperty 'HKLM:\Software\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe' -ErrorAction SilentlyContinue).'(default)' } catch {}
        $candidates += (Join-Path $env:ProgramFiles 'Microsoft\Edge\Application\msedge.exe')
        if(${env:ProgramFiles(x86)}) { $candidates += (Join-Path ${env:ProgramFiles(x86)} 'Microsoft\Edge\Application\msedge.exe') }
    }
    foreach($c in $candidates) {
        if(-not [string]::IsNullOrWhiteSpace([string]$c) -and (Test-Path -LiteralPath ([string]$c))) { return [string]$c }
    }
    return ""
}

function Ensure-WindowTools {
    if("YtmWindowTools" -as [type]) { return }
    Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class YtmWindowTools {
    [DllImport("user32.dll")]
    public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
}
"@
}

function Minimize-YoutubeMusicUi {
    try {
        Ensure-WindowTools
        foreach($name in @("chrome","msedge")) {
            foreach($proc in @(Get-Process -Name $name -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 })) {
                $title = ""
                try { $title = [string]$proc.MainWindowTitle } catch {}
                if(Is-YtMusicText $title) {
                    [void][YtmWindowTools]::ShowWindowAsync($proc.MainWindowHandle, 6)
                }
            }
        }
    } catch { Write-Log ("YT MINIMIZE FEHLER | " + $_.Exception.Message) }
}

function Get-PlaylistIdFromUrl([string]$Url) {
    try {
        if([string]::IsNullOrWhiteSpace($Url)) { return "" }
        $m = [regex]::Match($Url, '(?i)[?&]list=([^&#]+)')
        if($m.Success) { return [Uri]::UnescapeDataString($m.Groups[1].Value) }
    } catch {}
    return ""
}

function Resolve-MusicPlaylistUrl([string]$Url) {
    # V12.22: Wenn bereits ein konkreter music.youtube.com/watch?v=...&list=... Link
    # hinterlegt ist, exakt diesen verwenden. Nicht auf einen anderen Playlist-Titel umschreiben.
    if(-not [string]::IsNullOrWhiteSpace($Url) -and [regex]::IsMatch($Url, '(?i)[?&]v=[A-Za-z0-9_-]{11}')) {
        Write-Log ("YT PLAYLIST DIREKTLINK | " + $Url)
        return $Url
    }
    $id = Get-PlaylistIdFromUrl $Url
    if([string]::IsNullOrWhiteSpace($id)) { return $Url }

    # Fuer eine echte Media-Session braucht YouTube Music ein konkretes Video in der Queue.
    # Erst versuchen wir den offiziellen Playlist-RSS-Feed; bei privaten/ungewoehnlichen
    # Playlists faellt die Funktion auf die normale Playlist-Seite zurueck.
    $videoId = ""
    try {
        $feedUrl = 'https://www.youtube.com/feeds/videos.xml?playlist_id=' + [Uri]::EscapeDataString($id)
        $resp = Invoke-WebRequest -Uri $feedUrl -UseBasicParsing -TimeoutSec 6 -Headers @{ 'User-Agent'='Mozilla/5.0' } -ErrorAction Stop
        $m = [regex]::Match([string]$resp.Content, '<yt:videoId>([A-Za-z0-9_-]{11})</yt:videoId>')
        if($m.Success) {
            $videoId = $m.Groups[1].Value
            Write-Log ("YT PLAYLIST RESOLVE RSS OK | LIST=" + $id + " | VIDEO=" + $videoId)
        }
    } catch {
        Write-Log ("YT PLAYLIST RESOLVE RSS FEHLER | " + $_.Exception.Message)
    }

    if([string]::IsNullOrWhiteSpace($videoId)) {
        try {
            $musicPageUrl = 'https://music.youtube.com/playlist?list=' + [Uri]::EscapeDataString($id)
            $musicResp = Invoke-WebRequest -Uri $musicPageUrl -UseBasicParsing -TimeoutSec 7 -Headers @{ 'User-Agent'='Mozilla/5.0' } -ErrorAction Stop
            $mm = [regex]::Match([string]$musicResp.Content, '"videoId":"([A-Za-z0-9_-]{11})"')
            if($mm.Success) {
                $videoId = $mm.Groups[1].Value
                Write-Log ("YT PLAYLIST RESOLVE MUSIC HTML OK | LIST=" + $id + " | VIDEO=" + $videoId)
            }
        } catch {
            Write-Log ("YT PLAYLIST RESOLVE MUSIC HTML FEHLER | " + $_.Exception.Message)
        }
    }

    if([string]::IsNullOrWhiteSpace($videoId)) {
        try {
            $pageUrl = 'https://www.youtube.com/playlist?list=' + [Uri]::EscapeDataString($id)
            $resp2 = Invoke-WebRequest -Uri $pageUrl -UseBasicParsing -TimeoutSec 7 -Headers @{ 'User-Agent'='Mozilla/5.0' } -ErrorAction Stop
            $m2 = [regex]::Match([string]$resp2.Content, '"videoId":"([A-Za-z0-9_-]{11})"')
            if($m2.Success) {
                $videoId = $m2.Groups[1].Value
                Write-Log ("YT PLAYLIST RESOLVE HTML OK | LIST=" + $id + " | VIDEO=" + $videoId)
            }
        } catch {
            Write-Log ("YT PLAYLIST RESOLVE HTML FEHLER | " + $_.Exception.Message)
        }
    }

    if(-not [string]::IsNullOrWhiteSpace($videoId)) {
        return ('https://music.youtube.com/watch?v=' + [Uri]::EscapeDataString($videoId) + '&list=' + [Uri]::EscapeDataString($id) + '&index=1&playnext=1&autoplay=1')
    }

    Write-Log ("YT PLAYLIST RESOLVE OHNE VIDEO | LIST=" + $id + " | Playlist-Seite als Fallback")
    return ('https://music.youtube.com/playlist?list=' + [Uri]::EscapeDataString($id))
}

function Get-YoutubeMusicTabTarget {
    if(-not (Ensure-CloseUi)) { return $null }
    foreach($name in @("chrome","msedge")) {
        try {
            $procs = @(Get-Process -Name $name -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 })
            foreach($proc in $procs) {
                $hwnd = $proc.MainWindowHandle
                try {
                    $root = [System.Windows.Automation.AutomationElement]::FromHandle($hwnd)
                    if($null -eq $root) { continue }
                    $cond = New-Object System.Windows.Automation.PropertyCondition(
                        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
                        [System.Windows.Automation.ControlType]::TabItem)
                    $tabs = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
                    for($i = 0; $i -lt $tabs.Count; $i++) {
                        $tab = $tabs.Item($i)
                        $tabName = ""
                        try { $tabName = [string]$tab.Current.Name } catch {}
                        if(Is-YtMusicText $tabName) {
                            return @{ Proc=$proc; Hwnd=$hwnd; Tab=$tab; TabName=$tabName; Browser=$name }
                        }
                    }
                } catch {}
            }
        } catch {}
    }
    return $null
}

function Select-YoutubeMusicTab([object]$Target) {
    if($null -eq $Target) { return $false }
    try {
        $tab = $Target.Tab
        $patternObj = $null
        if($tab.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$patternObj)) {
            ([System.Windows.Automation.SelectionItemPattern]$patternObj).Select()
        } else {
            $tab.SetFocus()
        }
        return $true
    } catch {
        try { $Target.Tab.SetFocus(); return $true } catch { return $false }
    }
}

function Navigate-ExistingYoutubeMusicTab([string]$Url) {
    $target = Get-YoutubeMusicTabTarget
    if($null -eq $target) { return $false }
    $oldClipboard = $null
    $hadClipboardText = $false
    try {
        Ensure-WindowTools
        [void](Select-YoutubeMusicTab $target)
        [void][YtmWindowTools]::ShowWindowAsync($target.Hwnd, 9)
        Start-Sleep -Milliseconds 90
        try { [void][YtmWindowNative]::SetForegroundWindow($target.Hwnd) } catch {}
        Start-Sleep -Milliseconds 90

        try {
            $hadClipboardText = [System.Windows.Forms.Clipboard]::ContainsText()
            if($hadClipboardText) { $oldClipboard = [System.Windows.Forms.Clipboard]::GetText() }
            [System.Windows.Forms.Clipboard]::SetText($Url)
        } catch {}

        [System.Windows.Forms.SendKeys]::SendWait("^l")
        Start-Sleep -Milliseconds 60
        [System.Windows.Forms.SendKeys]::SendWait("^v")
        Start-Sleep -Milliseconds 45
        [System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
        Start-Sleep -Milliseconds 500
        # V12.22: Fenster sichtbar lassen; Nutzer minimiert YouTube Music selbst.

        try {
            if($hadClipboardText -and $null -ne $oldClipboard) { [System.Windows.Forms.Clipboard]::SetText([string]$oldClipboard) }
            elseif(-not $hadClipboardText) { [System.Windows.Forms.Clipboard]::Clear() }
        } catch {}

        Write-Log ("YT SINGLE TAB NAV OK | " + $target.Browser + " | " + $Url)
        return $true
    } catch {
        try {
            if($hadClipboardText -and $null -ne $oldClipboard) { [System.Windows.Forms.Clipboard]::SetText([string]$oldClipboard) }
        } catch {}
        Write-Log ("YT SINGLE TAB NAV FEHLER | " + $_.Exception.Message)
        return $false
    }
}

function Write-YtOpenResult([string]$State, [string]$Detail) {
    try {
        $tmp = $openResultPath + ".tmp"
        $data = "Time=" + (Get-Date).ToString("o") + "`r`nState=" + $State + "`r`nDetail=" + $Detail + "`r`n"
        [IO.File]::WriteAllText($tmp, $data, [Text.Encoding]::UTF8)
        Move-Item -LiteralPath $tmp -Destination $openResultPath -Force
    } catch {}
}

function Focus-Or-OpenYoutubeMusicHome {
    $ytHomeUrl = "https://music.youtube.com/"
    try {
        try { if(Test-Path -LiteralPath $openResultPath) { Remove-Item -LiteralPath $openResultPath -Force -ErrorAction SilentlyContinue } } catch {}

        # 1) Never open a duplicate when an actual YT Music tab can be identified.
        $target = Get-YoutubeMusicTabTarget
        if($null -ne $target) {
            Ensure-WindowTools
            [void](Select-YoutubeMusicTab $target)
            [void][YtmWindowTools]::ShowWindowAsync($target.Hwnd, 9)
            Start-Sleep -Milliseconds 100
            try { [void][YtmWindowNative]::SetForegroundWindow($target.Hwnd) } catch {}
            Write-Log ("YT HOME FOCUS EXISTING OK | " + $target.Browser + " | " + $target.TabName)
            Write-YtOpenResult "FOCUSED" ($target.Browser + " | " + $target.TabName)
            return $true
        }

        # 2) Dedicated/PWA window fallback. Focus only; still no new tab.
        foreach($name in @("chrome","msedge","firefox")) {
            foreach($proc in @(Get-Process -Name $name -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 })) {
                $title = ""
                try { $title = [string]$proc.MainWindowTitle } catch {}
                if(Is-YtMusicText $title) {
                    Ensure-WindowTools
                    [void][YtmWindowTools]::ShowWindowAsync($proc.MainWindowHandle, 9)
                    Start-Sleep -Milliseconds 80
                    try { [void][YtmWindowNative]::SetForegroundWindow($proc.MainWindowHandle) } catch {}
                    Write-Log ("YT HOME WINDOW FOCUS EXISTING OK | " + $name + " | " + $title)
                    Write-YtOpenResult "FOCUSED" ($name + " | " + $title)
                    return $true
                }
            }
        }

        # 3) No actual YT Music UI exists. Open ONCE through the registered Windows URL handler.
        # V12.34: Do NOT use explorer.exe here. explorer.exe can return successfully while the
        # URL is not actually dispatched to the browser, which made V12.33 report success with
        # no visible YouTube Music tab.
        $launched = $false
        try {
            $psi = New-Object System.Diagnostics.ProcessStartInfo
            $psi.FileName = $ytHomeUrl
            $psi.UseShellExecute = $true
            $psi.Verb = "open"
            [void][System.Diagnostics.Process]::Start($psi)
            $launched = $true
            Write-Log "YT HOME URL HANDLER REQUEST | UseShellExecute=true"
        } catch {
            Write-Log ("YT HOME URL HANDLER FEHLER | " + $_.Exception.Message)
        }

        if(-not $launched) {
            Write-YtOpenResult "FAILED" "Windows shell launch failed"
            return $false
        }

        $script:launchedByBridge = $true
        Write-YtOpenResult "OPEN_REQUESTED" "https://music.youtube.com/"

        # Wait for the tab/window and foreground it. Do not launch a second URL if
        # UI Automation cannot see it immediately; that is how duplicate tabs happen.
        for($try = 0; $try -lt 20; $try++) {
            Start-Sleep -Milliseconds 250
            $newTarget = Get-YoutubeMusicTabTarget
            if($null -ne $newTarget) {
                try {
                    Ensure-WindowTools
                    [void](Select-YoutubeMusicTab $newTarget)
                    [void][YtmWindowTools]::ShowWindowAsync($newTarget.Hwnd, 9)
                    Start-Sleep -Milliseconds 80
                    try { [void][YtmWindowNative]::SetForegroundWindow($newTarget.Hwnd) } catch {}
                    Write-Log ("YT HOME OPEN + FOREGROUND OK | " + $newTarget.Browser + " | " + $newTarget.TabName)
                    Write-YtOpenResult "OPENED" ($newTarget.Browser + " | " + $newTarget.TabName)
                } catch {
                    Write-Log ("YT HOME FOREGROUND NACH OPEN FEHLER | " + $_.Exception.Message)
                }
                return $true
            }
        }

        # Shell call succeeded; leave it at one request even if UIA cannot identify the tab.
        Write-Log "YT HOME SHELL OPENED | tab not identifiable yet; NO SECOND OPEN"
        Write-YtOpenResult "OPENED_UNCONFIRMED" "Shell request succeeded; UIA did not identify tab"
        return $true
    } catch {
        Write-Log ("YT HOME OPEN/FOCUS FEHLER | " + $_.Exception.Message)
        Write-YtOpenResult "FAILED" $_.Exception.Message
        return $false
    }
}

function Start-YoutubeMusicUrl([string]$Url, [bool]$ReplaceExisting) {
    if([string]::IsNullOrWhiteSpace($Url)) { $Url = "https://music.youtube.com/" }
    if(-not $Url.StartsWith("https://music.youtube.com/", [System.StringComparison]::OrdinalIgnoreCase)) {
        Write-Log ("YT URL BLOCKIERT | " + $Url)
        return $false
    }

    try {
        # Wichtig: Wenn bereits ein normaler YT-Music-Tab existiert, wird GENAU DIESER
        # Tab navigiert. Kein --app und kein --new-window mehr.
        if($ReplaceExisting) {
            if(Navigate-ExistingYoutubeMusicTab $Url) { return $true }

            # Alte PWA/App-Fenster aus V12.18 koennen keine Adressleiste haben. Einmalig
            # schliessen und danach nur noch den neuen Single-Tab-Weg benutzen.
            if(Test-YoutubeMusicUi) {
                Write-Log "YT SINGLE TAB MIGRATION | altes App/PWA-Fenster wird geschlossen"
                try { [void](Close-YoutubeMusicUi) } catch {}
                Start-Sleep -Milliseconds 300
            }
        }

        # V12.31: Do not trust the broad Test-YoutubeMusicUi check here. It can report
        # a stale/hidden browser UI even when no selectable YT Music tab was found.
        # At this point Get-YoutubeMusicTabTarget already failed, so opening exactly
        # one new tab is the correct and deterministic fallback.
        $browser = Find-BrowserExe "chrome"
        $source = "Chrome-Tab"
        if([string]::IsNullOrWhiteSpace($browser)) { $browser = Find-BrowserExe "msedge"; $source = "Edge-Tab" }
        if(-not [string]::IsNullOrWhiteSpace($browser)) {
            # --new-tab reuses an existing browser window when possible. Since we only
            # reach this branch when no actual YT Music tab was found, this cannot create
            # a duplicate YT Music tab through the bridge itself.
            [void](Start-Process -FilePath $browser -ArgumentList @('--autoplay-policy=no-user-gesture-required','--new-tab',$Url) -PassThru -ErrorAction Stop)
            $script:launchedByBridge = $true
            Write-Log ("YT TAB START REQUEST | " + $source + " | " + $Url)

            # Give Chrome/Edge time to create the tab, then focus the exact YT Music tab.
            $focused = $false
            for($try = 0; $try -lt 12; $try++) {
                Start-Sleep -Milliseconds 250
                $newTarget = Get-YoutubeMusicTabTarget
                if($null -ne $newTarget) {
                    try {
                        Ensure-WindowTools
                        [void](Select-YoutubeMusicTab $newTarget)
                        [void][YtmWindowTools]::ShowWindowAsync($newTarget.Hwnd, 9)
                        Start-Sleep -Milliseconds 80
                        try { [void][YtmWindowNative]::SetForegroundWindow($newTarget.Hwnd) } catch {}
                        $focused = $true
                        Write-Log ("YT TAB FOREGROUND OK | " + $newTarget.Browser + " | " + $newTarget.TabName)
                    } catch { Write-Log ("YT TAB FOREGROUND FEHLER | " + $_.Exception.Message) }
                    break
                }
            }
            if(-not $focused) { Write-Log "YT TAB STARTED | foreground target not yet visible; browser launch succeeded" }
            return $true
        }

        [void](Start-Process $Url -ErrorAction Stop)
        $script:launchedByBridge = $true
        Write-Log ("YT TAB START REQUEST | Standardbrowser | " + $Url)
        Start-Sleep -Milliseconds 650
        return $true
    } catch {
        Write-Log ("YT SINGLE TAB START FEHLER | " + $_.Exception.Message)
        return $false
    }
}

function Invoke-YoutubeMusicPlayButton {
    # V12.21: nur echte PLAY-/PLAY-ALL-Schaltflaechen ausloesen.
    # Kein Space/Toggle und kein erneutes Oeffnen einer URL.
    $current = Get-PreferredMediaSession
    if($null -ne $current -and $current.State -eq "Playing") {
        Write-Log "YT SAFE PLAY BUTTON | bereits Playing - nichts tun"
        return $true
    }

    $target = Get-YoutubeMusicTabTarget
    if($null -eq $target -or -not (Ensure-CloseUi)) {
        Write-Log "YT SAFE PLAY BUTTON | kein YT-Music-Tab gefunden"
        return $false
    }
    try {
        Ensure-WindowTools
        [void](Select-YoutubeMusicTab $target)
        $root = [System.Windows.Automation.AutomationElement]::FromHandle($target.Hwnd)
        if($null -eq $root) { return $false }
        $cond = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::Button)
        $buttons = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
        $best = $null
        for($i = 0; $i -lt $buttons.Count; $i++) {
            $button = $buttons.Item($i)
            $name = ""
            try { $name = [string]$button.Current.Name } catch {}
            if([string]::IsNullOrWhiteSpace($name)) { continue }
            $n = $name.Trim().ToLowerInvariant()
            if($n.Contains("pause") -or $n.Contains("next") -or $n.Contains("previous") -or $n.Contains("weiter") -or $n.Contains("zurueck") -or $n.Contains("zurück")) { continue }
            $isPlayAll = $n.Contains("play all") -or $n.Contains("alle wiedergeben") -or $n.Contains("wiedergabe starten")
            $isPlay = ($n -eq "play") -or ($n -eq "abspielen") -or ($n -eq "wiedergabe")
            if($isPlayAll -or $isPlay) {
                $best = $button
                Write-Log ("YT SAFE PLAY BUTTON GEFUNDEN | " + $name)
                if($isPlayAll) { break }
            }
        }
        if($null -eq $best) {
            Write-Log "YT SAFE PLAY BUTTON | keiner gefunden"
            return $false
        }
        $invokeObj = $null
        if($best.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$invokeObj)) {
            ([System.Windows.Automation.InvokePattern]$invokeObj).Invoke()
            Start-Sleep -Milliseconds 350
            Write-Log "YT SAFE PLAY BUTTON | INVOKE OK | Fenster bleibt sichtbar"
            return $true
        }
        Write-Log "YT SAFE PLAY BUTTON | kein InvokePattern"
        return $false
    } catch {
        Write-Log ("YT SAFE PLAY BUTTON FEHLER | " + $_.Exception.Message)
        return $false
    }
}

function Send-YoutubeMusicPlayHotkey {
    $target = Get-YoutubeMusicTabTarget
    if($null -eq $target) { return $false }
    try {
        Ensure-WindowTools
        [void](Select-YoutubeMusicTab $target)
        [void][YtmWindowTools]::ShowWindowAsync($target.Hwnd, 9)
        Start-Sleep -Milliseconds 90
        try { [void][YtmWindowNative]::SetForegroundWindow($target.Hwnd) } catch {}
        Start-Sleep -Milliseconds 100
        [System.Windows.Forms.SendKeys]::SendWait(" ")
        Start-Sleep -Milliseconds 180
        [void][YtmWindowTools]::ShowWindowAsync($target.Hwnd, 6)
        Write-Log "YT PLAY HOTKEY | SPACE gesendet"
        return $true
    } catch {
        try { [void][YtmWindowTools]::ShowWindowAsync($target.Hwnd, 6) } catch {}
        Write-Log ("YT PLAY HOTKEY FEHLER | " + $_.Exception.Message)
        return $false
    }
}

function Start-YoutubeMusicAutomatically {
    if($script:autoLaunchAttempted) { return }
    $script:autoLaunchAttempted = $true
    if($AutoLaunch -ne 1) {
        Write-Log "YT AUTO START AUS (AutoLaunch=0)"
        return
    }

    try {
        # Eine vorhandene Chrome/Edge-Media-Session ist verlaesslicher als der Fenstertitel.
        # So oeffnet ein Bridge-Neustart nicht erneut YouTube Music, nur weil der Songtitel
        # im Fenstertitel steht und dort nicht "YouTube Music" vorkommt.
        $existing = Get-PreferredMediaSession
        if($null -ne $existing -and $null -ne $existing.Session) {
            Write-Log ("YT AUTO START | Media-Session bereits vorhanden | " + $existing.Source + " | " + $existing.Title)
            return
        }
        if(Test-YoutubeMusicUi) {
            Write-Log "YT AUTO START | bereits geoeffnet"
        } else {
            $browser = Find-BrowserExe "chrome"
            $source = "Chrome-App"
            if([string]::IsNullOrWhiteSpace($browser)) {
                $browser = Find-BrowserExe "msedge"
                $source = "Edge-App"
            }
            [void](Start-YoutubeMusicUrl "https://music.youtube.com/" $false)
            Write-Log "YT AUTO START OK | sichtbar; manuell minimieren"
        }
    } catch {
        Write-Log ("YT AUTO START FEHLER | " + $_.Exception.Message)
        return
    }

    if($AutoPlay -eq 1) {
        # Wiedergabe startet bewusst erst beim ersten Einsteigen ins Fahrzeug.
        # Dadurch laeuft YT Music nicht schon im GTA-Lademenue los.
        Write-Log "YT AUTO PLAY | wartet auf Fahrzeug-Aktivierung durch GTA-Script"
    }
}

function Close-YoutubeMusicUi {
    # V12.23 SAFE TAB CLOSE: Niemals Chrome/Edge/Firefox komplett beenden.
    # Nur wenn der YT-Music-Tab ueber UI Automation eindeutig gefunden wird,
    # wird genau dieser Tab selektiert und mit Ctrl+W geschlossen.
    $browserNames = @("chrome", "msedge", "firefox")
    $uiReady = Ensure-CloseUi
    if(-not $uiReady) {
        Write-Log "YT SAFE CLOSE: UI Automation nicht verfuegbar; Browser bleibt offen"
        return $false
    }

    foreach($name in $browserNames) {
        $procs = @(Get-Process -Name $name -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 })
        foreach($proc in $procs) {
            $hwnd = $proc.MainWindowHandle
            try {
                $root = [System.Windows.Automation.AutomationElement]::FromHandle($hwnd)
                if($null -eq $root) { continue }
                $cond = New-Object System.Windows.Automation.PropertyCondition(
                    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
                    [System.Windows.Automation.ControlType]::TabItem)
                $tabs = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
                for($i = 0; $i -lt $tabs.Count; $i++) {
                    $tab = $tabs.Item($i)
                    $tabName = ""
                    try { $tabName = [string]$tab.Current.Name } catch {}
                    if(-not (Is-YtMusicText $tabName)) { continue }
                    try {
                        $patternObj = $null
                        if($tab.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$patternObj)) {
                            ([System.Windows.Automation.SelectionItemPattern]$patternObj).Select()
                        } else { $tab.SetFocus() }
                    } catch { try { $tab.SetFocus() } catch {} }
                    Start-Sleep -Milliseconds 140
                    try { [void][YtmWindowNative]::SetForegroundWindow($hwnd) } catch {}
                    Start-Sleep -Milliseconds 100
                    [System.Windows.Forms.SendKeys]::SendWait("^w")
                    Write-Log ("YT SAFE CLOSE TAB OK | SOURCE=" + $name + " | TAB=" + $tabName)
                    return $true
                }
            } catch {
                Write-Log ("YT SAFE CLOSE TAB SCAN FEHLER | SOURCE=" + $name + " | " + $_.Exception.Message)
            }
        }
    }

    Write-Log "YT SAFE CLOSE: kein eindeutiger YT-Music-Tab gefunden; Browser bleibt offen"
    return $false
}

function Stop-And-CloseManagedYoutube([string]$Reason) {
    try { Run-SessionCommand "PAUSE" -1; Write-Log ($Reason + " -> YT PAUSE") } catch { Write-Log ($Reason + " PAUSE FEHLER | " + $_.Exception.Message) }
    try { Restore-YtSessionVolume } catch {}
    try {
        $ok = Close-YoutubeMusicUi
        Write-Log ($Reason + " -> YT CLOSE RESULT=" + $ok)
    } catch { Write-Log ($Reason + " CLOSE FEHLER | " + $_.Exception.Message) }
}

try { Add-Type -AssemblyName System.Runtime.WindowsRuntime -ErrorAction Stop } catch { Write-Log ("System.Runtime.WindowsRuntime FEHLER | " + $_.Exception.Message); exit 10 }
try { Add-Type -AssemblyName System.Drawing -ErrorAction Stop } catch { Write-Log ("System.Drawing FEHLER | " + $_.Exception.Message) }

# Core-Audio helper: regelt nur die Browser-Audio-Session (Chrome/Edge/Firefox),
# nicht GTA5.exe. Urspruengliche Windows-Mixer-Lautstaerke wird gespeichert und
# beim Verlassen des YT-Modus/Beenden der Bridge wiederhergestellt.
$coreAudioSource = @'
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace YtmCoreAudio
{
    enum EDataFlow { eRender = 0, eCapture = 1, eAll = 2 }
    enum ERole { eConsole = 0, eMultimedia = 1, eCommunications = 2 }

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    class MMDeviceEnumeratorComObject { }

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDeviceEnumerator
    {
        int EnumAudioEndpoints(EDataFlow dataFlow, int dwStateMask, out IntPtr ppDevices);
        int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppEndpoint);
        int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId, out IMMDevice ppDevice);
        int RegisterEndpointNotificationCallback(IntPtr pClient);
        int UnregisterEndpointNotificationCallback(IntPtr pClient);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDevice
    {
        int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
        int OpenPropertyStore(int stgmAccess, out IntPtr ppProperties);
        int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);
        int GetState(out int pdwState);
    }

    [ComImport, Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioSessionManager2
    {
        int GetAudioSessionControl(ref Guid AudioSessionGuid, uint StreamFlags, out IAudioSessionControl SessionControl);
        int GetSimpleAudioVolume(ref Guid AudioSessionGuid, uint StreamFlags, out ISimpleAudioVolume AudioVolume);
        int GetSessionEnumerator(out IAudioSessionEnumerator SessionEnum);
        int RegisterSessionNotification(IntPtr SessionNotification);
        int UnregisterSessionNotification(IntPtr SessionNotification);
        int RegisterDuckNotification([MarshalAs(UnmanagedType.LPWStr)] string sessionID, IntPtr duckNotification);
        int UnregisterDuckNotification(IntPtr duckNotification);
    }

    [ComImport, Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioSessionEnumerator
    {
        int GetCount(out int SessionCount);
        int GetSession(int SessionCount, out IAudioSessionControl Session);
    }

    [ComImport, Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioSessionControl
    {
        int GetState(out int pRetVal);
        int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
        int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string Value, ref Guid EventContext);
        int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
        int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string Value, ref Guid EventContext);
        int GetGroupingParam(out Guid pRetVal);
        int SetGroupingParam(ref Guid Override, ref Guid EventContext);
        int RegisterAudioSessionNotification(IntPtr NewNotifications);
        int UnregisterAudioSessionNotification(IntPtr NewNotifications);
    }

    [ComImport, Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioSessionControl2 : IAudioSessionControl
    {
        new int GetState(out int pRetVal);
        new int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
        new int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string Value, ref Guid EventContext);
        new int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
        new int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string Value, ref Guid EventContext);
        new int GetGroupingParam(out Guid pRetVal);
        new int SetGroupingParam(ref Guid Override, ref Guid EventContext);
        new int RegisterAudioSessionNotification(IntPtr NewNotifications);
        new int UnregisterAudioSessionNotification(IntPtr NewNotifications);
        int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
        int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
        int GetProcessId(out uint pRetVal);
        int IsSystemSoundsSession();
        int SetDuckingPreference(bool optOut);
    }

    [ComImport, Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface ISimpleAudioVolume
    {
        int SetMasterVolume(float fLevel, ref Guid EventContext);
        int GetMasterVolume(out float pfLevel);
        int SetMute(bool bMute, ref Guid EventContext);
        int GetMute(out bool pbMute);
    }

    public static class SessionVolume
    {
        private static readonly Dictionary<uint, float> Original = new Dictionary<uint, float>();
        private const int CLSCTX_ALL = 23;

        private static bool NameMatches(string processName, string[] names)
        {
            if (String.IsNullOrEmpty(processName) || names == null) return false;
            string p = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? processName.Substring(0, processName.Length - 4) : processName;
            foreach (string n0 in names)
            {
                if (String.IsNullOrEmpty(n0)) continue;
                string n = n0.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? n0.Substring(0, n0.Length - 4) : n0;
                if (String.Equals(p, n, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static IAudioSessionEnumerator GetEnumerator(out object managerObj, out IMMDevice device, out IMMDeviceEnumerator deviceEnum)
        {
            managerObj = null; device = null; deviceEnum = null;
            deviceEnum = (IMMDeviceEnumerator)(new MMDeviceEnumeratorComObject());
            int hr = deviceEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out device);
            if (hr != 0 || device == null) return null;
            Guid iid = typeof(IAudioSessionManager2).GUID;
            hr = device.Activate(ref iid, CLSCTX_ALL, IntPtr.Zero, out managerObj);
            if (hr != 0 || managerObj == null) return null;
            IAudioSessionManager2 manager = (IAudioSessionManager2)managerObj;
            IAudioSessionEnumerator sessions;
            hr = manager.GetSessionEnumerator(out sessions);
            if (hr != 0) return null;
            return sessions;
        }

        public static int SetVolumeForProcesses(string[] processNames, float volume)
        {
            if (volume < 0f) volume = 0f;
            if (volume > 1f) volume = 1f;
            object managerObj = null; IMMDevice device = null; IMMDeviceEnumerator deviceEnum = null; IAudioSessionEnumerator sessions = null;
            int changed = 0;
            try
            {
                sessions = GetEnumerator(out managerObj, out device, out deviceEnum);
                if (sessions == null) return 0;
                int count; sessions.GetCount(out count);
                for (int i = 0; i < count; i++)
                {
                    IAudioSessionControl ctl = null;
                    try
                    {
                        if (sessions.GetSession(i, out ctl) != 0 || ctl == null) continue;
                        IAudioSessionControl2 ctl2 = ctl as IAudioSessionControl2;
                        ISimpleAudioVolume simple = ctl as ISimpleAudioVolume;
                        if (ctl2 == null || simple == null) continue;
                        uint pid; if (ctl2.GetProcessId(out pid) != 0 || pid == 0) continue;
                        string pn = "";
                        try { pn = Process.GetProcessById((int)pid).ProcessName; } catch { continue; }
                        if (!NameMatches(pn, processNames)) continue;
                        if (!Original.ContainsKey(pid))
                        {
                            float old; if (simple.GetMasterVolume(out old) == 0) Original[pid] = old;
                        }
                        Guid ctx = Guid.Empty;
                        if (simple.SetMasterVolume(volume, ref ctx) == 0) changed++;
                    }
                    finally { if (ctl != null) try { Marshal.ReleaseComObject(ctl); } catch { } }
                }
            }
            finally
            {
                if (sessions != null) try { Marshal.ReleaseComObject(sessions); } catch { }
                if (managerObj != null) try { Marshal.ReleaseComObject(managerObj); } catch { }
                if (device != null) try { Marshal.ReleaseComObject(device); } catch { }
                if (deviceEnum != null) try { Marshal.ReleaseComObject(deviceEnum); } catch { }
            }
            return changed;
        }

        public static int RestoreCaptured()
        {
            if (Original.Count == 0) return 0;
            object managerObj = null; IMMDevice device = null; IMMDeviceEnumerator deviceEnum = null; IAudioSessionEnumerator sessions = null;
            int changed = 0;
            try
            {
                sessions = GetEnumerator(out managerObj, out device, out deviceEnum);
                if (sessions == null) return 0;
                int count; sessions.GetCount(out count);
                for (int i = 0; i < count; i++)
                {
                    IAudioSessionControl ctl = null;
                    try
                    {
                        if (sessions.GetSession(i, out ctl) != 0 || ctl == null) continue;
                        IAudioSessionControl2 ctl2 = ctl as IAudioSessionControl2;
                        ISimpleAudioVolume simple = ctl as ISimpleAudioVolume;
                        if (ctl2 == null || simple == null) continue;
                        uint pid; if (ctl2.GetProcessId(out pid) != 0 || !Original.ContainsKey(pid)) continue;
                        Guid ctx = Guid.Empty;
                        if (simple.SetMasterVolume(Original[pid], ref ctx) == 0) changed++;
                    }
                    finally { if (ctl != null) try { Marshal.ReleaseComObject(ctl); } catch { } }
                }
            }
            finally
            {
                Original.Clear();
                if (sessions != null) try { Marshal.ReleaseComObject(sessions); } catch { }
                if (managerObj != null) try { Marshal.ReleaseComObject(managerObj); } catch { }
                if (device != null) try { Marshal.ReleaseComObject(device); } catch { }
                if (deviceEnum != null) try { Marshal.ReleaseComObject(deviceEnum); } catch { }
            }
            return changed;
        }
    }
}
'@
try {
    Add-Type -TypeDefinition $coreAudioSource -Language CSharp -ErrorAction Stop
    Write-Log "CORE AUDIO HELPER BEREIT"
} catch {
    Write-Log ("CORE AUDIO HELPER FEHLER | " + $_.Exception.Message)
}

$script:asTaskGeneric = $null
try {
    $script:asTaskGeneric = ([System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {
        $_.Name -eq "AsTask" -and $_.IsGenericMethod -and $_.GetParameters().Count -eq 1
    } | Select-Object -First 1)
    if($null -eq $script:asTaskGeneric) { throw "AsTask<T> nicht gefunden" }
} catch { Write-Log ("AS-TASK FEHLER | " + $_.Exception.Message); exit 11 }

function Await-WinRT([object]$Operation, [Type]$ResultType) {
    $m = $script:asTaskGeneric.MakeGenericMethod($ResultType)
    $task = $m.Invoke($null, @($Operation))
    $task.Wait()
    return $task.Result
}

$managerType = [Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager, Windows.Media.Control, ContentType=WindowsRuntime]
$mediaPropsType = [Windows.Media.Control.GlobalSystemMediaTransportControlsSessionMediaProperties, Windows.Media.Control, ContentType=WindowsRuntime]
$randomAccessType = [Windows.Storage.Streams.IRandomAccessStreamWithContentType, Windows.Storage.Streams, ContentType=WindowsRuntime]
$dataReaderType = [Windows.Storage.Streams.DataReader, Windows.Storage.Streams, ContentType=WindowsRuntime]
$script:manager = $null

function Ensure-Manager {
    if($null -ne $script:manager) { return $true }
    try {
        $script:manager = Await-WinRT ($managerType::RequestAsync()) $managerType
        Write-Log "GSMTC MANAGER BEREIT"
        return ($null -ne $script:manager)
    } catch {
        Write-Log ("GSMTC MANAGER FEHLER | " + $_.Exception.Message)
        $script:manager = $null
        return $false
    }
}

$script:lastSessionScanLog = Get-Date "2000-01-01"
$script:lastGoodInfo = $null
$script:lastGoodAt = Get-Date "2000-01-01"
$script:aggressiveDetectUntil = Get-Date "2000-01-01"
$script:lastManagerRefresh = Get-Date "2000-01-01"

function Reset-MediaManager([string]$Reason) {
    try { $script:manager = $null } catch {}
    $script:lastManagerRefresh = Get-Date
    Write-Log ("GSMTC MANAGER RESET | " + $Reason)
    [void](Ensure-Manager)
}

function Is-SpotifySource([string]$Source) {
    if([string]::IsNullOrWhiteSpace($Source)) { return $false }
    return ([string]$Source).ToLowerInvariant().Contains("spotify")
}

function Is-ExplicitYoutubeSource([string]$Source) {
    if([string]::IsNullOrWhiteSpace($Source)) { return $false }
    $s = ([string]$Source).ToLowerInvariant()
    return ($s.Contains("youtube") -or $s.Contains("ytmusic"))
}

function Is-BrowserSource([string]$Source) {
    $s = ""
    if($null -ne $Source) { $s = ([string]$Source).ToLowerInvariant() }
    return ($s.Contains("chrome") -or $s.Contains("msedge") -or $s.Contains("edge") -or $s.Contains("firefox") -or (Is-ExplicitYoutubeSource $Source))
}

function Get-SessionInfo([object]$Session) {
    $info = @{
        Session=$Session; Source=""; State="Unknown"; Title=""; Artist=""; Album=""; Score=0; Thumbnail=$null
    }
    if($null -eq $Session) { return $info }
    try { $info.Source = [string]$Session.SourceAppUserModelId } catch {}
    try {
        $pb = $Session.GetPlaybackInfo()
        if($null -ne $pb) { $info.State = [string]$pb.PlaybackStatus }
    } catch {}
    try {
        $props = Await-WinRT ($Session.TryGetMediaPropertiesAsync()) $mediaPropsType
        if($null -ne $props) {
            $info.Title = [string]$props.Title
            $info.Artist = [string]$props.Artist
            $info.Album = [string]$props.AlbumTitle
            try { $info.Thumbnail = $props.Thumbnail } catch {}
        }
    } catch {}

    $src = ""
    if($null -ne $info.Source) { $src = ([string]$info.Source).ToLowerInvariant() }
    if($src.Contains("youtube") -or $src.Contains("ytmusic")) { $info.Score += 150 }
    if($src.Contains("chrome"))  { $info.Score += 80 }
    if($src.Contains("msedge"))  { $info.Score += 75 }
    if($src.Contains("edge"))    { $info.Score += 70 }
    if($src.Contains("firefox")) { $info.Score += 65 }
    if($info.State -eq "Playing") { $info.Score += 30 }
    elseif($info.State -eq "Paused") { $info.Score += 12 }
    if(-not [string]::IsNullOrWhiteSpace($info.Title)) { $info.Score += 12 }
    if(-not [string]::IsNullOrWhiteSpace($info.Artist)) { $info.Score += 14 }
    if(-not [string]::IsNullOrWhiteSpace($info.Album)) { $info.Score += 4 }
    return $info
}

function Get-PreferredMediaSession {
    if(-not (Ensure-Manager)) { return $null }
    $best = $null
    $all = @()
    $youtubeInfos = @()
    $ytUiPresent = $false
    try { $ytUiPresent = Test-YoutubeMusicUi } catch {}

    try {
        $sessions = $script:manager.GetSessions()
        foreach($session in $sessions) {
            $info = Get-SessionInfo $session
            $all += ,$info

            # TEST15: Never allow Spotify's desktop GSMTC session to become YouTube Music.
            # Generic browser sessions are accepted only while a real YouTube Music tab/PWA
            # is present. This removes the old fallback that could select Spotify/VLC/etc.
            $src = [string]$info.Source
            if(Is-SpotifySource $src) { continue }
            if(Is-ExplicitYoutubeSource $src) {
                $youtubeInfos += ,$info
            } elseif($ytUiPresent -and (Is-BrowserSource $src)) {
                $youtubeInfos += ,$info
            }
        }
    } catch {
        Write-Log ("GET SESSIONS FEHLER | " + $_.Exception.Message)
    }

    # Strict YouTube-only pool. IMPORTANT: no fallback to $all.
    foreach($info in $youtubeInfos) {
        if($null -eq $best -or [int]$info.Score -gt [int]$best.Score) { $best = $info }
    }

    if($null -eq $best) {
        try {
            $cur = $script:manager.GetCurrentSession()
            if($null -ne $cur) {
                $curInfo = Get-SessionInfo $cur
                $src = [string]$curInfo.Source
                if(-not (Is-SpotifySource $src)) {
                    if((Is-ExplicitYoutubeSource $src) -or ($ytUiPresent -and (Is-BrowserSource $src))) {
                        $best = $curInfo
                    }
                }
            }
        } catch {}
    }

    # During manual YT selection, force a fresh WinRT manager if Chrome created its
    # media session after this helper started. Some Windows builds otherwise expose
    # the new session late.
    if($null -eq $best -and (Get-Date) -lt $script:aggressiveDetectUntil) {
        if(((Get-Date)-$script:lastManagerRefresh).TotalSeconds -ge 2.0) {
            Reset-MediaManager "AGGRESSIVE DETECT - NO YT SESSION"
        }
    }

    if($null -ne $best -and $null -ne $best.Session) {
        $useful = ($best.State -eq "Playing" -or $best.State -eq "Paused" -or -not [string]::IsNullOrWhiteSpace([string]$best.Title) -or -not [string]::IsNullOrWhiteSpace([string]$best.Artist))
        if($useful) {
            $script:lastGoodInfo = $best
            $script:lastGoodAt = Get-Date
        }
    }

    # Keep only a previously validated YouTube/browser session for a short song-change grace window.
    if($null -eq $best -and $null -ne $script:lastGoodInfo -and ((Get-Date)-$script:lastGoodAt).TotalSeconds -le 3.5) {
        $lastSrc = [string]$script:lastGoodInfo.Source
        if(-not (Is-SpotifySource $lastSrc) -and ((Is-ExplicitYoutubeSource $lastSrc) -or ($ytUiPresent -and (Is-BrowserSource $lastSrc)))) {
            $best = $script:lastGoodInfo
        }
    }

    if(((Get-Date)-$script:lastSessionScanLog).TotalSeconds -ge 8) {
        $script:lastSessionScanLog = Get-Date
        if($all.Count -eq 0) {
            Write-Log "SESSION SCAN | 0 Sessions gefunden"
        } else {
            Write-Log ("SESSION SCAN | " + $all.Count + " Sessions | YT candidates=" + $youtubeInfos.Count + " | YT UI=" + $ytUiPresent)
            foreach($i in $all) {
                Write-Log ("  SOURCE=" + $i.Source + " | STATE=" + $i.State + " | SCORE=" + $i.Score + " | TITLE=" + $i.Title + " | ARTIST=" + $i.Artist)
            }
            if($null -ne $best) { Write-Log ("  YT AUSGEWAEHLT=" + $best.Source + " | " + $best.Title) }
            else { Write-Log "  YT AUSGEWAEHLT=<none>" }
        }
    }
    return $best
}

function Get-CurrentMediaSession {
    $preferred = Get-PreferredMediaSession
    if($null -eq $preferred) { return $null }
    return $preferred.Session
}

function Get-CoverKey([object]$Info) {
    try {
        $raw = ([string]$Info.Source) + "`n" + ([string]$Info.Title) + "`n" + ([string]$Info.Artist) + "`n" + ([string]$Info.Album)
        $sha = [Security.Cryptography.SHA1]::Create()
        try { $bytes = $sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($raw)) } finally { $sha.Dispose() }
        return ([BitConverter]::ToString($bytes)).Replace("-", "")
    } catch { return "" }
}

function Save-CoverForInfo([object]$Info) {
    if($null -eq $Info -or $null -eq $Info.Thumbnail) { return "" }
    $ras = $null
    $input = $null
    $reader = $null
    try {
        New-Item -ItemType Directory -Path $coverDir -Force | Out-Null
        $key = Get-CoverKey $Info
        if([string]::IsNullOrWhiteSpace($key)) { return "" }
        $ras = Await-WinRT ($Info.Thumbnail.OpenReadAsync()) $randomAccessType
        if($null -eq $ras) { Write-Log "COVER: OpenReadAsync lieferte null"; return "" }
        $size64 = [uint64]$ras.Size
        if($size64 -le 0 -or $size64 -gt 8388608) { Write-Log ("COVER: ungueltige Groesse " + $size64); return "" }
        $contentType = ""
        try { $contentType = [string]$ras.ContentType } catch {}
        $ext = ".jpg"
        if($contentType.ToLowerInvariant().Contains("png")) { $ext = ".png" }
        elseif($contentType.ToLowerInvariant().Contains("jpeg") -or $contentType.ToLowerInvariant().Contains("jpg")) { $ext = ".jpg" }
        $final = Join-Path $coverDir ("ytm_" + $key + $ext)
        if((Test-Path -LiteralPath $final) -and ((Get-Item -LiteralPath $final).Length -gt 64)) { return $final }
        $input = $ras.GetInputStreamAt(0)
        $reader = $dataReaderType::new($input)
        $requested = [uint32]$size64
        $loaded = Await-WinRT ($reader.LoadAsync($requested)) ([uint32])
        if($loaded -le 0) { Write-Log "COVER: DataReader lud 0 Bytes"; return "" }
        $bytes = New-Object byte[] ([int]$loaded)
        $reader.ReadBytes($bytes)
        $tmp = $final + ".tmp"
        [IO.File]::WriteAllBytes($tmp, $bytes)
        Move-Item -LiteralPath $tmp -Destination $final -Force
        Write-Log ("COVER SAVED RAW | " + $final + " | " + $loaded + " Bytes | " + $contentType)
        return $final
    } catch {
        Write-Log ("COVER FEHLER V12.4 | " + $_.Exception.ToString())
        return ""
    } finally {
        try { if($null -ne $reader) { $reader.Dispose() } } catch {}
        try { if($null -ne $input) { $input.Dispose() } } catch {}
        try { if($null -ne $ras) { $ras.Dispose() } } catch {}
    }
}

function Get-CoverKey([string]$Source, [string]$Title, [string]$Artist, [string]$Album) {
    try {
        $raw = $Source + "`n" + $Title + "`n" + $Artist + "`n" + $Album
        $sha = [Security.Cryptography.SHA1]::Create()
        try { $bytes = $sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($raw)) } finally { $sha.Dispose() }
        return ([BitConverter]::ToString($bytes)).Replace("-", "")
    } catch { return "" }
}

function Get-CachedCover([string]$Key) {
    try {
        if([string]::IsNullOrWhiteSpace($Key)){ return "" }
        # V12.8: Nur normalisierte, vom Worker verifizierte PNGs duerfen an GTA gemeldet werden.
        $ready = Join-Path $coverDir ("ytm_ready_" + $Key + ".png")
        if(Test-Path -LiteralPath $ready) {
            $f = Get-Item -LiteralPath $ready -ErrorAction SilentlyContinue
            if($null -ne $f -and $f.Length -ge 2048 -and $f.Length -le 4194304){ return $f.FullName }
        }
    } catch {}
    return ""
}

function Stop-CoverWorker([string]$Reason) {
    try {
        if($null -ne $script:coverWorker -and -not $script:coverWorker.HasExited) {
            Write-Log ("COVER WORKER KILL | " + $Reason + " | PID=" + $script:coverWorker.Id)
            $script:coverWorker.Kill()
        }
    } catch {}
    try { if($null -ne $script:coverWorker){ $script:coverWorker.Dispose() } } catch {}
    $script:coverWorker = $null
    $script:coverWorkerKey = ""
}

function Clear-CoverCache([string]$Reason) {
    try {
        Stop-CoverWorker $Reason
        if(Test-Path -LiteralPath $coverDir) {
            Get-ChildItem -LiteralPath $coverDir -File -ErrorAction SilentlyContinue | ForEach-Object {
                try { Remove-Item -LiteralPath $_.FullName -Force -ErrorAction Stop } catch {}
            }
        }
        $script:coverLastAttempt = @{}
        Write-Log ("ARTWORK CACHE CLEAR | " + $Reason)
    } catch { Write-Log ("ARTWORK CACHE CLEAR ERROR | " + $_.Exception.Message) }
}

function Maintain-CoverWorker {
    if($null -eq $script:coverWorker){ return }
    try {
        if($script:coverWorker.HasExited) {
            Write-Log ("COVER WORKER ENDE | Key=" + $script:coverWorkerKey + " | Code=" + $script:coverWorker.ExitCode)
            $script:coverWorker.Dispose()
            $script:coverWorker = $null
            $script:coverWorkerKey = ""
            return
        }
        if(((Get-Date)-$script:coverWorkerStarted).TotalMilliseconds -gt 3200) {
            Stop-CoverWorker "TIMEOUT 3200ms"
        }
    } catch { Stop-CoverWorker "STATUS FEHLER" }
}

function Start-CoverWorker([string]$Key, [string]$Title, [string]$Artist) {
    if([string]::IsNullOrWhiteSpace($Key) -or -not (Test-Path -LiteralPath $coverWorkerPath)){ return }
    Maintain-CoverWorker
    if($null -ne $script:coverWorker){ return }
    $now = Get-Date
    if($script:coverLastAttempt.ContainsKey($Key)) {
        if(($now - [datetime]$script:coverLastAttempt[$Key]).TotalSeconds -lt 15){ return }
    }
    $script:coverLastAttempt[$Key] = $now
    try {
        $tb64 = To-B64 $Title
        $ab64 = To-B64 $Artist
        $args = '-NoProfile -Sta -ExecutionPolicy Bypass -WindowStyle Hidden -File "' + $coverWorkerPath + '" -BaseDir "' + $BaseDir + '" -Key "' + $Key + '" -ExpectedTitleBase64 "' + $tb64 + '" -ExpectedArtistBase64 "' + $ab64 + '"'
        $psi = New-Object Diagnostics.ProcessStartInfo
        $psi.FileName = "powershell.exe"
        $psi.Arguments = $args
        $psi.UseShellExecute = $false
        $psi.CreateNoWindow = $true
        $psi.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
        $script:coverWorker = [Diagnostics.Process]::Start($psi)
        $script:coverWorkerKey = $Key
        $script:coverWorkerStarted = Get-Date
        Write-Log ("COVER WORKER START | Key=" + $Key + " | PID=" + $script:coverWorker.Id)
    } catch {
        Write-Log ("COVER WORKER START FEHLER | " + $_.Exception.Message)
        $script:coverWorker = $null
        $script:coverWorkerKey = ""
    }
}

function Get-MediaSnapshot {
    $available = $false
    $state = "Nicht verbunden"
    $title = ""
    $artist = ""
    $album = ""
    $source = ""
    $cover = ""
    try {
        if($script:artworkEnabled) { Maintain-CoverWorker }
        $info = Get-PreferredMediaSession
        if($null -ne $info -and $null -ne $info.Session) {
            $available = $true
            $state = [string]$info.State
            $title = [string]$info.Title
            $artist = [string]$info.Artist
            $album = [string]$info.Album
            $source = [string]$info.Source
            if($script:artworkEnabled) {
                $key = Get-CoverKey $source $title $artist $album
                $cover = Get-CachedCover $key
                if([string]::IsNullOrWhiteSpace($cover)) { Start-CoverWorker $key $title $artist }
            }
        }
    } catch { Write-Log ("SNAPSHOT FEHLER | " + $_.Exception.Message) }
    return @{
        Available=$available; State=$state; Title=$title; Artist=$artist; Album=$album; Source=$source; Cover=$cover
    }
}

function Write-Status {
    try {
        $s = Get-MediaSnapshot
        $data = "Available=$($s.Available)`r`n" +
                "State=$($s.State)`r`n" +
                "TitleBase64=$(To-B64 $s.Title)`r`n" +
                "ArtistBase64=$(To-B64 $s.Artist)`r`n" +
                "AlbumBase64=$(To-B64 $s.Album)`r`n" +
                "SourceBase64=$(To-B64 $s.Source)`r`n" +
                "CoverPathBase64=$(To-B64 $s.Cover)`r`n"
        $tmp = $statusPath + ".tmp"
        Set-Content -LiteralPath $tmp -Value $data -Encoding UTF8
        Move-Item -LiteralPath $tmp -Destination $statusPath -Force
    } catch {}
}

function Read-Command([string]$Path) {
    try {
        $map = @{}
        foreach($line in (Get-Content -LiteralPath $Path -Encoding UTF8)) {
            $p = $line.IndexOf('=')
            if($p -gt 0) { $map[$line.Substring(0,$p).Trim()] = $line.Substring($p+1).Trim() }
        }
        return $map
    } catch { return $null }
}

function Get-VolumeProcessNames([string]$Source) {
    $src = ""
    if($null -ne $Source) { $src = $Source.ToLowerInvariant() }
    if($src.Contains("chrome")) { return [string[]]@("chrome") }
    if($src.Contains("msedge") -or $src.Contains("edge")) { return [string[]]@("msedge") }
    if($src.Contains("firefox")) { return [string[]]@("firefox") }
    return [string[]]@("chrome", "msedge", "firefox")
}

function Set-YtSessionVolume([int]$Volume) {
    try {
        if($Volume -lt 0) { $Volume = 0 }
        if($Volume -gt 100) { $Volume = 100 }
        $info = Get-PreferredMediaSession
        $src = ""
        if($null -ne $info) { $src = [string]$info.Source }
        $names = Get-VolumeProcessNames $src
        $changed = [YtmCoreAudio.SessionVolume]::SetVolumeForProcesses($names, [single]($Volume / 100.0))
        Write-Log ("YT VOLUME | " + $Volume + "% | SOURCE=" + $src + " | SESSIONS=" + $changed)
    } catch { Write-Log ("YT VOLUME FEHLER | " + $_.Exception.Message) }
}

function Restore-YtSessionVolume {
    try {
        $changed = [YtmCoreAudio.SessionVolume]::RestoreCaptured()
        Write-Log ("YT VOLUME RESTORE | SESSIONS=" + $changed)
    } catch { Write-Log ("YT VOLUME RESTORE FEHLER | " + $_.Exception.Message) }
}

function Run-SessionCommand([string]$Action, [int]$Volume) {
    try {
        if($Action -eq "VOLUME") { Set-YtSessionVolume $Volume; return }
        if($Action -eq "RESTORE_VOLUME") { Restore-YtSessionVolume; return }
        $session = Get-CurrentMediaSession
        if($null -eq $session) { Write-Log ("CMD " + $Action + " | KEINE MEDIA SESSION"); return $false }
        switch($Action) {
            "TOGGLE"   { [void](Await-WinRT ($session.TryTogglePlayPauseAsync()) ([bool])) }
            "PLAY"     { [void](Await-WinRT ($session.TryPlayAsync()) ([bool])) }
            "PAUSE"    { [void](Await-WinRT ($session.TryPauseAsync()) ([bool])) }
            "NEXT"     { [void](Await-WinRT ($session.TrySkipNextAsync()) ([bool])) }
            "PREVIOUS" { [void](Await-WinRT ($session.TrySkipPreviousAsync()) ([bool])) }
            "STOP"     { [void](Await-WinRT ($session.TryStopAsync()) ([bool])) }
        }
        Write-Log ("CMD " + $Action + " OK")
        return $true
    } catch { Write-Log ("CMD " + $Action + " FEHLER | " + $_.Exception.Message); return $false }
}

function Run-TransportCommandSafe([string]$Action) {
    # Trackwechsel duerfen niemals eine URL oder ein Browserfenster neu oeffnen.
    # Wenn Chrome beim Songwechsel kurz keine Media-Session liefert, warten wir nur kurz.
    for($attempt = 1; $attempt -le 5; $attempt++) {
        $session = Get-CurrentMediaSession
        if($null -ne $session) {
            try {
                switch($Action) {
                    "NEXT"     { [void](Await-WinRT ($session.TrySkipNextAsync()) ([bool])) }
                    "PREVIOUS" { [void](Await-WinRT ($session.TrySkipPreviousAsync()) ([bool])) }
                    default    { return (Run-SessionCommand $Action -1) }
                }
                Write-Log ("CMD " + $Action + " OK | SAFE TRANSPORT | Versuch=" + $attempt)
                return $true
            } catch {
                Write-Log ("CMD " + $Action + " FEHLER | SAFE TRANSPORT | " + $_.Exception.Message)
                return $false
            }
        }
        if($attempt -lt 5) { Start-Sleep -Milliseconds 180 }
    }
    Write-Log ("CMD " + $Action + " | KEINE MEDIA SESSION NACH WARTEZEIT")
    return $false
}

try {
    New-Item -ItemType Directory -Path $commandDir -Force | Out-Null
    New-Item -ItemType Directory -Path $coverDir -Force | Out-Null
    if(-not $script:artworkEnabled) { Clear-CoverCache "START DISABLED" }
    Write-Log ("ARTWORK INITIAL=" + $script:artworkEnabled)
    # Alte V12.7-Rohcover koennen DirectX-Texture-Fehler ausloesen. Sie werden nie wieder verwendet.
    try {
        Get-ChildItem -LiteralPath $coverDir -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -like "ytm_*" -and $_.Name -notlike "ytm_ready_*" } |
            Remove-Item -Force -ErrorAction SilentlyContinue
    } catch {}
    try { Remove-Item -LiteralPath $statusPath -Force -ErrorAction SilentlyContinue } catch {}
    # TEST55: Do NOT clear *.cmd here. The C# side already clears stale commands before
    # launching this helper. Clearing the queue again inside helper startup races with the
    # first OPEN_HOME command (Num5), so the first press can be deleted before it is read.
    # V12.7: Cover-Lesen laeuft in einem separaten Prozess mit hartem Timeout.
    Write-Log "YOUTUBE MUSIC BRIDGE V12.36 TEST55 START - FIRST NUM5 QUEUE RACE FIX"
    [void](Ensure-Manager)
    Start-YoutubeMusicAutomatically
    $quit = $false
    $managedActive = $false
    $lastStatus = Get-Date "2000-01-01"
    $lastMinimize = Get-Date "2000-01-01"
    $playlistPlayAt = $null
    $playlistPlayAttempts = 0
    $playlistStartedAt = $null
    $lastParentCheck = Get-Date "2000-01-01"
    while(-not $quit) {
        Maintain-CoverWorker
        if($ParentPid -gt 0 -and ((Get-Date)-$lastParentCheck).TotalMilliseconds -ge 1000) {
            $lastParentCheck = Get-Date
            $parent = Get-Process -Id $ParentPid -ErrorAction SilentlyContinue
            if($null -eq $parent) {
                Write-Log ("PARENT ENDE | FORCE CLOSE | MANAGED=" + $managedActive)
                Stop-And-CloseManagedYoutube "PARENT ENDE"
                $managedActive = $false
                $script:launchedByBridge = $false
                break
            }
        }
        if($null -ne $playlistPlayAt -and (Get-Date) -ge $playlistPlayAt) {
            $infoNow = Get-PreferredMediaSession
            if($null -ne $infoNow -and $infoNow.State -eq "Playing") {
                Write-Log ("YT PLAYLIST START BESTAETIGT | " + $infoNow.Title + " | " + $infoNow.Artist)
                $playlistPlayAt = $null
                $playlistPlayAttempts = 0
                $playlistStartedAt = $null
            } else {
                $playlistPlayAttempts++
                $okSessionPlay = $false
                $okUiPlay = $false

                # Vorhandene Media-Session nur mit idempotentem PLAY starten.
                if($null -ne $infoNow) {
                    $okSessionPlay = Run-SessionCommand "PLAY" -1
                }

                # Wenn die Browserseite noch keine Media-Session erzeugt hat, gezielt einen
                # echten Play/Play-All-Button ausloesen. Kein Toggle und kein Space.
                if($playlistPlayAttempts -eq 1 -or $playlistPlayAttempts -eq 3 -or $playlistPlayAttempts -eq 6) {
                    Start-Sleep -Milliseconds 180
                    $check = Get-PreferredMediaSession
                    if($null -eq $check -or $check.State -ne "Playing") {
                        $okUiPlay = Invoke-YoutubeMusicPlayButton
                    }
                }

                $stateText = if($null -ne $infoNow){$infoNow.State}else{"NONE"}
                Write-Log ("YT PLAYLIST START WAIT | Versuch=" + $playlistPlayAttempts + " | Session=" + ($null -ne $infoNow) + " | State=" + $stateText + " | SessionPlay=" + $okSessionPlay + " | UiPlay=" + $okUiPlay)
                $playlistPlayAt = (Get-Date).AddMilliseconds(1100)

                if($null -ne $playlistStartedAt -and ((Get-Date)-$playlistStartedAt).TotalSeconds -gt 18) {
                    Write-Log "YT PLAYLIST START TIMEOUT | kein weiterer URL-Start, Player bleibt offen"
                    $playlistPlayAt = $null
                    $playlistPlayAttempts = 0
                    $playlistStartedAt = $null
                }
            }
            Write-Status
        }
        $files = @(Get-ChildItem -LiteralPath $commandDir -Filter "*.cmd" -File -ErrorAction SilentlyContinue | Sort-Object Name)
        foreach($f in $files) {
            $cmd = Read-Command $f.FullName
            $action = ""
            $volume = -1
            $url = ""
            if($null -ne $cmd -and $cmd.ContainsKey("Action")) { $action = ([string]$cmd["Action"]).ToUpperInvariant() }
            if($null -ne $cmd -and $cmd.ContainsKey("Volume")) { [void][int]::TryParse([string]$cmd["Volume"], [ref]$volume) }
            if($null -ne $cmd -and $cmd.ContainsKey("UrlBase64")) {
                try { if(-not [string]::IsNullOrWhiteSpace([string]$cmd["UrlBase64"])) { $url = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String([string]$cmd["UrlBase64"])) } } catch { $url = "" }
            }
            if($action -eq "MODE_ON") { $managedActive = $true; Write-Log "MANAGED MODE ON" }
            elseif($action -eq "MODE_OFF") { $managedActive = $false; Write-Log "MANAGED MODE OFF" }
            elseif($action -eq "ARTWORK_ON") { $script:artworkEnabled = $true; Write-Log "ARTWORK ON"; Write-Status }
            elseif($action -eq "ARTWORK_OFF") { $script:artworkEnabled = $false; Clear-CoverCache "ARTWORK OFF"; Write-Status }
            elseif($action -eq "DETECT") {
                # V12.28: Detection only. Browser opening is handled by the visible GTA process.
                $script:aggressiveDetectUntil = (Get-Date).AddSeconds(20)
                Reset-MediaManager "DETECT"
                Start-Sleep -Milliseconds 120
                Write-Status
                Write-Log "YT DETECTION WINDOW START | 20s"
            }
            elseif($action -eq "OPEN_HOME") {
                # Manual selection only. Give Windows up to 20 seconds to publish the new
                # Chrome/Edge/Firefox media session and actively refresh GSMTC while waiting.
                [void](Focus-Or-OpenYoutubeMusicHome)
                $script:aggressiveDetectUntil = (Get-Date).AddSeconds(20)
                Reset-MediaManager "OPEN_HOME"
                Start-Sleep -Milliseconds 180
                Write-Status
            }
            elseif($action -eq "OPEN_PLAYLIST") {
                # V12.24 PRE-FINAL: playlist automation was intentionally removed.
                # Keep the command harmless for backwards compatibility with stale queued files.
                Write-Log "YT PLAYLIST COMMAND IGNORED | feature removed in V12.24"
            }
            elseif($action -eq "CLOSE_APP") {
                Stop-And-CloseManagedYoutube "CLOSE_APP"
                $managedActive = $false
            }
            elseif($action -eq "QUIT") {
                Stop-And-CloseManagedYoutube "QUIT"
                $managedActive = $false
                $script:launchedByBridge = $false
                $quit = $true
            }
            elseif($action -eq "REFRESH") { Write-Status }
            elseif($action -eq "NEXT" -or $action -eq "PREVIOUS") { Run-TransportCommandSafe $action; Start-Sleep -Milliseconds 80; Write-Status }
            elseif($action.Length -gt 0) { Run-SessionCommand $action $volume; Start-Sleep -Milliseconds 80; Write-Status }
            try { Remove-Item -LiteralPath $f.FullName -Force -ErrorAction SilentlyContinue } catch {}
            if($quit) { break }
        }
        $statusInterval = 650
        if((Get-Date) -lt $script:aggressiveDetectUntil) { $statusInterval = 250 }
        if(((Get-Date)-$lastStatus).TotalMilliseconds -ge $statusInterval) { Write-Status; $lastStatus = Get-Date }
        Start-Sleep -Milliseconds 120
    }
} catch { Write-Log ("FATAL | " + $_.Exception.ToString()) }
finally {
    Clear-CoverCache "BRIDGE ENDE"
    if($managedActive -or $script:launchedByBridge) {
        try { Stop-And-CloseManagedYoutube "FINALLY" } catch {}
        $managedActive = $false
        $script:launchedByBridge = $false
    }
    Restore-YtSessionVolume
}
Write-Log "BRIDGE ENDE"
