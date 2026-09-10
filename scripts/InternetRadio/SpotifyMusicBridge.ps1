param(
    [Parameter(Mandatory=$true)][string]$BaseDir,
    [int]$ParentPid = 0,
    [int]$ArtworkEnabled = 0
)

$ErrorActionPreference = "Stop"
$commandDir = Join-Path $BaseDir "spotify_commands"
$statusPath = Join-Path $BaseDir "spotify_status.txt"
$logPath = Join-Path $BaseDir "spotify_bridge.log"
$coverDir = Join-Path $BaseDir "spotify_covers"
$coverWorkerPath = Join-Path $BaseDir "SpotifyCoverWorker.ps1"
$script:coverWorker = $null
$script:coverWorkerKey = ""
$script:coverWorkerStarted = Get-Date "2000-01-01"
$script:coverLastAttempt = @{}
$script:manager = $null
$script:lastGoodInfo = $null
$script:lastGoodAt = Get-Date "2000-01-01"
$script:aggressiveDetectUntil = Get-Date "2000-01-01"
$script:lastManagerRefresh = Get-Date "2000-01-01"
$script:managedActive = $false
$script:closeUiReady = $false
$script:closeUiAttempted = $false
$script:lastSpotifyWebUiScan = Get-Date "2000-01-01"
$script:lastSpotifyWebUiPresent = $false
$script:artworkEnabled = ([int]$ArtworkEnabled -ne 0)

function Ensure-SpotifyCloseUi {
    if($script:closeUiReady) { return $true }
    if($script:closeUiAttempted) { return $false }
    $script:closeUiAttempted = $true
    try {
        Add-Type -AssemblyName UIAutomationClient -ErrorAction Stop
        Add-Type -AssemblyName UIAutomationTypes -ErrorAction Stop
        Add-Type -AssemblyName System.Windows.Forms -ErrorAction Stop
        $script:closeUiReady = $true
        return $true
    } catch {
        Write-Log ("SPOTIFY CLOSE UI LOAD ERROR | " + $_.Exception.Message)
        return $false
    }
}

New-Item -ItemType Directory -Path $commandDir -Force | Out-Null
New-Item -ItemType Directory -Path $coverDir -Force | Out-Null

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

try {
    Add-Type -AssemblyName System.Runtime.WindowsRuntime -ErrorAction Stop
    [void][Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager, Windows.Media.Control, ContentType=WindowsRuntime]
    [void][Windows.Media.Control.GlobalSystemMediaTransportControlsSessionMediaProperties, Windows.Media.Control, ContentType=WindowsRuntime]
} catch {
    Write-Log ("WINRT LOAD ERROR | " + $_.Exception.Message)
    exit 11
}

$script:asTaskGeneric = ([System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {
    $_.Name -eq "AsTask" -and $_.IsGenericMethod -and $_.GetParameters().Count -eq 1
} | Select-Object -First 1)
if($null -eq $script:asTaskGeneric) { Write-Log "AsTask<T> missing"; exit 12 }

function Await-WinRT([object]$Operation, [Type]$ResultType) {
    $m = $script:asTaskGeneric.MakeGenericMethod($ResultType)
    $task = $m.Invoke($null, @($Operation))
    $task.Wait()
    return $task.Result
}

$managerType = [Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager, Windows.Media.Control, ContentType=WindowsRuntime]
$mediaPropsType = [Windows.Media.Control.GlobalSystemMediaTransportControlsSessionMediaProperties, Windows.Media.Control, ContentType=WindowsRuntime]

function Ensure-Manager {
    if($null -ne $script:manager) { return $true }
    try {
        $script:manager = Await-WinRT ($managerType::RequestAsync()) $managerType
        Write-Log "GSMTC MANAGER READY"
        return ($null -ne $script:manager)
    } catch {
        Write-Log ("GSMTC MANAGER ERROR | " + $_.Exception.Message)
        $script:manager = $null
        return $false
    }
}

function Reset-MediaManager([string]$Reason) {
    try { $script:manager = $null } catch {}
    $script:lastManagerRefresh = Get-Date
    Write-Log ("GSMTC MANAGER RESET | " + $Reason)
    [void](Ensure-Manager)
}

function Is-SpotifySource([string]$Source) {
    if([string]::IsNullOrWhiteSpace($Source)) { return $false }
    return $Source.ToLowerInvariant().Contains("spotify")
}
function Is-BrowserSource([string]$Source) {
    if([string]::IsNullOrWhiteSpace($Source)) { return $false }
    $s = $Source.ToLowerInvariant()
    return ($s.Contains("chrome") -or $s.Contains("msedge") -or $s.Contains("edge") -or $s.Contains("firefox"))
}

function Test-SpotifyWebUiRaw {
    # Browser fallback is accepted only when a visible browser window itself
    # identifies Spotify. A separately open desktop Spotify app must not make
    # an unrelated Chrome/Edge/Firefox media session look like Spotify.
    foreach($name in @("chrome","msedge","firefox")) {
        try {
            foreach($p in @(Get-Process -Name $name -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 })) {
                $title = ""
                try { $title = [string]$p.MainWindowTitle } catch {}
                if(-not [string]::IsNullOrWhiteSpace($title) -and $title.ToLowerInvariant().Contains("spotify")) { return $true }
            }
        } catch {}
    }
    return $false
}

function Test-SpotifyWebUi {
    # TEST59: avoid rescanning browser windows more than necessary while metadata
    # polling is active. Desktop Spotify sessions bypass this fallback completely.
    $now = Get-Date
    if(($now - $script:lastSpotifyWebUiScan).TotalMilliseconds -lt 900) { return $script:lastSpotifyWebUiPresent }
    $script:lastSpotifyWebUiScan = $now
    $present = $false
    try { $present = [bool](Test-SpotifyWebUiRaw) } catch { $present = $false }
    $script:lastSpotifyWebUiPresent = $present
    return $present
}

function Get-SessionInfo([object]$Session) {
    $info = @{ Session=$Session; Source=""; State="Unknown"; Title=""; Artist=""; Album=""; Score=0 }
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
        }
    } catch {}

    if(Is-SpotifySource ([string]$info.Source)) { $info.Score += 500 }
    elseif(Is-BrowserSource ([string]$info.Source)) { $info.Score += 60 }
    if($info.State -eq "Playing") { $info.Score += 40 }
    elseif($info.State -eq "Paused") { $info.Score += 15 }
    if(-not [string]::IsNullOrWhiteSpace([string]$info.Title)) { $info.Score += 15 }
    if(-not [string]::IsNullOrWhiteSpace([string]$info.Artist)) { $info.Score += 15 }
    if(-not [string]::IsNullOrWhiteSpace([string]$info.Album)) { $info.Score += 5 }
    return $info
}

function Get-PreferredMediaSession {
    if(-not (Ensure-Manager)) { return $null }
    $all = @()
    $spotifyInfos = @()
    $browserInfos = @()
    try {
        foreach($session in @($script:manager.GetSessions())) {
            $info = Get-SessionInfo $session
            $all += ,$info
            if(Is-SpotifySource ([string]$info.Source)) { $spotifyInfos += ,$info }
            elseif(Is-BrowserSource ([string]$info.Source)) { $browserInfos += ,$info }
        }
    } catch { Write-Log ("GET SESSIONS ERROR | " + $_.Exception.Message) }

    $best = $null
    $pool = @()
    if($spotifyInfos.Count -gt 0) { $pool = $spotifyInfos }
    elseif((Test-SpotifyWebUi) -and $browserInfos.Count -gt 0) { $pool = $browserInfos }

    foreach($info in $pool) {
        if($null -eq $best -or [int]$info.Score -gt [int]$best.Score) { $best = $info }
    }

    if($null -eq $best) {
        try {
            $cur = $script:manager.GetCurrentSession()
            if($null -ne $cur) {
                $ci = Get-SessionInfo $cur
                if((Is-SpotifySource ([string]$ci.Source)) -or ((Test-SpotifyWebUi) -and (Is-BrowserSource ([string]$ci.Source)))) { $best = $ci }
            }
        } catch {}
    }

    if($null -ne $best -and $null -ne $best.Session) {
        $useful = ($best.State -eq "Playing" -or $best.State -eq "Paused" -or -not [string]::IsNullOrWhiteSpace([string]$best.Title))
        if($useful) {
            $script:lastGoodInfo = $best
            $script:lastGoodAt = Get-Date
        }
    }

    if($null -eq $best -and $null -ne $script:lastGoodInfo -and ((Get-Date)-$script:lastGoodAt).TotalSeconds -le 3.5) {
        $best = $script:lastGoodInfo
    }

    if($null -eq $best -and (Get-Date) -lt $script:aggressiveDetectUntil -and ((Get-Date)-$script:lastManagerRefresh).TotalSeconds -ge 2.0) {
        Reset-MediaManager "AGGRESSIVE DETECT"
    }
    return $best
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
        if([string]::IsNullOrWhiteSpace($Key)) { return "" }
        $ready = Join-Path $coverDir ("spotify_ready_" + $Key + ".png")
        if(Test-Path -LiteralPath $ready) {
            $f = Get-Item -LiteralPath $ready -ErrorAction SilentlyContinue
            if($null -ne $f -and $f.Length -ge 2048 -and $f.Length -le 4194304) { return $f.FullName }
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
    if($null -eq $script:coverWorker) { return }
    try {
        if($script:coverWorker.HasExited) {
            Write-Log ("COVER WORKER END | Key=" + $script:coverWorkerKey + " | Code=" + $script:coverWorker.ExitCode)
            $script:coverWorker.Dispose()
            $script:coverWorker = $null
            $script:coverWorkerKey = ""
            return
        }
        if(((Get-Date)-$script:coverWorkerStarted).TotalMilliseconds -gt 3500) {
            Write-Log ("COVER WORKER TIMEOUT | Key=" + $script:coverWorkerKey)
            try { $script:coverWorker.Kill() } catch {}
            try { $script:coverWorker.Dispose() } catch {}
            $script:coverWorker = $null
            $script:coverWorkerKey = ""
        }
    } catch {
        $script:coverWorker = $null
        $script:coverWorkerKey = ""
    }
}

function Start-CoverWorker([string]$Key, [string]$Title, [string]$Artist) {
    if([string]::IsNullOrWhiteSpace($Key) -or -not (Test-Path -LiteralPath $coverWorkerPath)) { return }
    Maintain-CoverWorker
    if($null -ne $script:coverWorker) { return }
    $now = Get-Date
    if($script:coverLastAttempt.ContainsKey($Key)) {
        if(($now - [datetime]$script:coverLastAttempt[$Key]).TotalSeconds -lt 12) { return }
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
        Write-Log ("COVER WORKER START ERROR | " + $_.Exception.Message)
        $script:coverWorker = $null
        $script:coverWorkerKey = ""
    }
}

function Get-MediaSnapshot {
    $available = $false
    $state = "Not connected"
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
    } catch { Write-Log ("SNAPSHOT ERROR | " + $_.Exception.Message) }
    return @{ Available=$available; State=$state; Title=$title; Artist=$artist; Album=$album; Source=$source; Cover=$cover }
}

function Test-SpotifyTabName([string]$Text) {
    if([string]::IsNullOrWhiteSpace($Text)) { return $false }
    $t = $Text.ToLowerInvariant()
    return ($t.Contains("spotify") -or $t.Contains("open.spotify.com"))
}

function Close-SpotifyBrowserTabs {
    if(-not (Ensure-SpotifyCloseUi)) {
        Write-Log "SPOTIFY SAFE CLOSE: UI Automation not available; browser stays open"
        return $false
    }

    $closedAny = $false
    foreach($name in @("chrome","msedge","firefox")) {
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
                # Close from the end so tab indices cannot shift under us.
                for($i = $tabs.Count - 1; $i -ge 0; $i--) {
                    $tab = $tabs.Item($i)
                    $tabName = ""
                    try { $tabName = [string]$tab.Current.Name } catch {}
                    if(-not (Test-SpotifyTabName $tabName)) { continue }
                    try {
                        $patternObj = $null
                        if($tab.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$patternObj)) {
                            ([System.Windows.Automation.SelectionItemPattern]$patternObj).Select()
                        } else { $tab.SetFocus() }
                    } catch { try { $tab.SetFocus() } catch {} }
                    Start-Sleep -Milliseconds 120
                    try {
                        Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class SpotifyWindowNative {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
}
'@ -ErrorAction SilentlyContinue
                        [void][SpotifyWindowNative]::SetForegroundWindow($hwnd)
                    } catch {}
                    Start-Sleep -Milliseconds 90
                    [System.Windows.Forms.SendKeys]::SendWait("^w")
                    Start-Sleep -Milliseconds 120
                    Write-Log ("SPOTIFY SAFE CLOSE TAB OK | SOURCE=" + $name + " | TAB=" + $tabName)
                    $closedAny = $true
                }
            } catch {
                Write-Log ("SPOTIFY SAFE CLOSE TAB SCAN ERROR | SOURCE=" + $name + " | " + $_.Exception.Message)
            }
        }
    }
    return $closedAny
}

function Close-SpotifyDesktopApp {
    $procs = @(Get-Process -Name "Spotify" -ErrorAction SilentlyContinue)
    if($procs.Count -eq 0) { return $false }

    $requestedClose = $false
    foreach($p in $procs) {
        try {
            if($p.MainWindowHandle -ne 0) {
                [void]$p.CloseMainWindow()
                $requestedClose = $true
            }
        } catch {}
    }
    if($requestedClose) { Start-Sleep -Milliseconds 550 }

    # Spotify can remain resident in the tray after WM_CLOSE. The user explicitly
    # switched source, so finish closing only Spotify's own remaining processes.
    $left = @(Get-Process -Name "Spotify" -ErrorAction SilentlyContinue)
    foreach($p in $left) {
        try { Stop-Process -Id $p.Id -Force -ErrorAction Stop } catch {}
    }
    Write-Log ("SPOTIFY DESKTOP CLOSE | initial=" + $procs.Count + " | remaining=" + $left.Count)
    return $true
}

function Stop-And-CloseManagedSpotify([string]$Reason) {
    try { Invoke-MediaCommand "PAUSE" } catch { Write-Log ($Reason + " PAUSE ERROR | " + $_.Exception.Message) }
    $desktop = $false
    $tabs = $false
    try { $desktop = Close-SpotifyDesktopApp } catch { Write-Log ($Reason + " DESKTOP CLOSE ERROR | " + $_.Exception.Message) }
    try { $tabs = Close-SpotifyBrowserTabs } catch { Write-Log ($Reason + " TAB CLOSE ERROR | " + $_.Exception.Message) }
    try { $script:lastGoodInfo = $null; $script:lastGoodAt = Get-Date "2000-01-01"; Reset-MediaManager "CLOSE_APP" } catch {}
    Write-Log ($Reason + " -> SPOTIFY CLOSE RESULT | Desktop=" + $desktop + " | WebTabs=" + $tabs)
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

function Open-Spotify {
    $script:aggressiveDetectUntil = (Get-Date).AddSeconds(20)
    try {
        Start-Process -FilePath "spotify:" -ErrorAction Stop | Out-Null
        Write-Log "OPEN SPOTIFY URI"
        return $true
    } catch {
        Write-Log ("SPOTIFY URI FAILED | " + $_.Exception.Message)
    }
    try {
        Start-Process -FilePath "https://open.spotify.com/" -ErrorAction Stop | Out-Null
        Write-Log "OPEN SPOTIFY WEB FALLBACK"
        return $true
    } catch {
        Write-Log ("SPOTIFY WEB OPEN FAILED | " + $_.Exception.Message)
        return $false
    }
}

function Invoke-MediaCommand([string]$Action) {
    if($Action -eq "OPEN_HOME") { [void](Open-Spotify); return }
    if($Action -eq "CLOSE_APP") { Stop-And-CloseManagedSpotify "CLOSE_APP"; $script:managedActive = $false; return }
    if($Action -eq "DETECT") {
        $script:aggressiveDetectUntil = (Get-Date).AddSeconds(20)
        Reset-MediaManager "MANUAL DETECT"
        return
    }
    if($Action -eq "MODE_ON") { $script:managedActive = $true; return }
    if($Action -eq "MODE_OFF") { $script:managedActive = $false; return }
    if($Action -eq "ARTWORK_ON") { $script:artworkEnabled = $true; Write-Log "ARTWORK ON"; return }
    if($Action -eq "ARTWORK_OFF") { $script:artworkEnabled = $false; Clear-CoverCache "ARTWORK OFF"; return }
    if($Action -eq "VOLUME" -or $Action -eq "RESTORE_VOLUME") { return }

    $info = Get-PreferredMediaSession
    if($null -eq $info -or $null -eq $info.Session) {
        Write-Log ("COMMAND WITHOUT SESSION | " + $Action)
        return
    }
    $session = $info.Session
    try {
        $ok = $false
        if($Action -eq "PLAY") { $ok = Await-WinRT ($session.TryPlayAsync()) ([bool]) }
        elseif($Action -eq "PAUSE") { $ok = Await-WinRT ($session.TryPauseAsync()) ([bool]) }
        elseif($Action -eq "TOGGLE") { $ok = Await-WinRT ($session.TryTogglePlayPauseAsync()) ([bool]) }
        elseif($Action -eq "NEXT") { $ok = Await-WinRT ($session.TrySkipNextAsync()) ([bool]) }
        elseif($Action -eq "PREVIOUS") { $ok = Await-WinRT ($session.TrySkipPreviousAsync()) ([bool]) }
        Write-Log ("COMMAND " + $Action + " | OK=" + $ok + " | SOURCE=" + $info.Source + " | TITLE=" + $info.Title)
    } catch {
        Write-Log ("COMMAND ERROR " + $Action + " | " + $_.Exception.Message)
    }
}

function Read-Command([string]$Path) {
    $d = @{}
    try {
        foreach($line in Get-Content -LiteralPath $Path -ErrorAction Stop) {
            $p = $line.IndexOf('=')
            if($p -le 0) { continue }
            $d[$line.Substring(0,$p).Trim()] = $line.Substring($p+1).Trim()
        }
    } catch {}
    return $d
}

Write-Log ("SPOTIFY BRIDGE READY | ParentPid=" + $ParentPid + " | Artwork=" + $script:artworkEnabled)
[void](Ensure-Manager)
if(-not $script:artworkEnabled) { Clear-CoverCache "START DISABLED" }
$lastStatus = Get-Date "2000-01-01"
$lastParentCheck = Get-Date "2000-01-01"

while($true) {
    try {
        if($ParentPid -gt 0 -and ((Get-Date)-$lastParentCheck).TotalMilliseconds -ge 1000) {
            $lastParentCheck = Get-Date
            $parent = Get-Process -Id $ParentPid -ErrorAction SilentlyContinue
            if($null -eq $parent) {
                Write-Log "PARENT ENDED | FORCE CLOSE"
                try { Stop-And-CloseManagedSpotify "PARENT ENDED" } catch {}
                break
            }
        }

        $files = @(Get-ChildItem -LiteralPath $commandDir -Filter "*.cmd" -File -ErrorAction SilentlyContinue | Sort-Object Name)
        foreach($f in $files) {
            $cmd = Read-Command $f.FullName
            $action = ""
            if($cmd.ContainsKey("Action")) { $action = [string]$cmd["Action"] }
            try { Remove-Item -LiteralPath $f.FullName -Force -ErrorAction SilentlyContinue } catch {}
            if($action -eq "QUIT") {
                Write-Log "QUIT | FORCE CLOSE"
                try { Stop-And-CloseManagedSpotify "QUIT" } catch {}
                try { Clear-CoverCache "QUIT" } catch {}
                exit 0
            }
            if(-not [string]::IsNullOrWhiteSpace($action)) { Invoke-MediaCommand $action }
        }

        if(((Get-Date)-$lastStatus).TotalMilliseconds -ge 500) {
            $lastStatus = Get-Date
            Write-Status
        }
    } catch {
        Write-Log ("LOOP ERROR | " + $_.Exception.Message)
    }
    Start-Sleep -Milliseconds 90
}

try { Clear-CoverCache "BRIDGE END" } catch {}
try { Write-Status } catch {}
exit 0
