param(
    [Parameter(Mandatory=$true)][string]$BaseDir,
    [int]$ParentPid = 0
)

$ErrorActionPreference = "Continue"
$commandDir  = Join-Path $BaseDir "player_commands"
$statusPath  = Join-Path $BaseDir "player_status.txt"
$logPath     = Join-Path $BaseDir "external_player.log"
$currentVolume = 55
$player = $null
$lastState = -999
$activeUrl = ""
$lastPlayAttempt = Get-Date "2000-01-01"
$lastStatusWrite = Get-Date "2000-01-01"
$lastMetadataPoll = Get-Date "2000-01-01"
$metaTitle = ""
$metaArtist = ""
$metaMediaName = ""
$icyStreamTitle = ""
$lastIcyPoll = Get-Date "2000-01-01"
$icyPollSeconds = 8

function Write-Log([string]$Text) {
    try { Add-Content -LiteralPath $logPath -Value ((Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff") + "  " + $Text) -Encoding UTF8 } catch {}
}
function State-Name([int]$State) {
    switch ($State) { 0{"Undefined"} 1{"Stopped"} 2{"Paused"} 3{"Playing"} 4{"ScanForward"} 5{"ScanReverse"} 6{"Buffering"} 7{"Waiting"} 8{"MediaEnded"} 9{"Transitioning"} 10{"Ready"} 11{"Reconnecting"} default{"Unknown"} }
}
function Pump-Messages([int]$Milliseconds = 0) {
    $until = (Get-Date).AddMilliseconds($Milliseconds)
    do {
        try { [System.Windows.Forms.Application]::DoEvents() } catch {}
        if ($Milliseconds -gt 0) { Start-Sleep -Milliseconds 10 }
    } while ($Milliseconds -gt 0 -and (Get-Date) -lt $until)
}
function To-B64([string]$Text) {
    try {
        if ($null -eq $Text) { $Text = "" }
        return [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($Text))
    } catch { return "" }
}
function Safe-Info($Media,[string]$Key) {
    try {
        if ($null -eq $Media) { return "" }
        $v = [string]$Media.getItemInfo($Key)
        if ($null -eq $v) { return "" }
        return $v.Trim()
    } catch { return "" }
}
function First-Info($Media,[string[]]$Keys) {
    foreach($k in $Keys) {
        $v = Safe-Info $Media $k
        if(-not [string]::IsNullOrWhiteSpace($v)) { return $v }
    }
    return ""
}
function Split-ArtistTitle([string]$Value) {
    $result = @{ Artist=""; Title="" }
    if([string]::IsNullOrWhiteSpace($Value)) { return $result }
    $v = $Value.Trim()
    if($v -match '^\s*(.+?)\s+[\-–—]\s+(.+?)\s*$') {
        $result.Artist = $matches[1].Trim()
        $result.Title = $matches[2].Trim()
    } else {
        $result.Title = $v
    }
    return $result
}
function Read-IcyStreamTitle([string]$Url) {
    if([string]::IsNullOrWhiteSpace($Url)) { return "" }
    $resp=$null; $stream=$null
    try {
        try { [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 -bor [Net.SecurityProtocolType]::Tls11 -bor [Net.SecurityProtocolType]::Tls } catch {}
        $req = [System.Net.HttpWebRequest]::Create($Url)
        $req.Method = "GET"
        $req.AllowAutoRedirect = $true
        $req.Timeout = 1300
        $req.ReadWriteTimeout = 1300
        $req.UserAgent = "GTA5-InternetRadio/11.2.5"
        $req.Headers.Add("Icy-MetaData","1")
        try { $req.AutomaticDecompression = [Net.DecompressionMethods]::GZip -bor [Net.DecompressionMethods]::Deflate } catch {}
        $resp = $req.GetResponse()
        $metaText = [string]$resp.Headers["icy-metaint"]
        $metaInt = 0
        if(-not [int]::TryParse($metaText,[ref]$metaInt) -or $metaInt -le 0) { return "" }
        $stream = $resp.GetResponseStream()
        $buffer = New-Object byte[] 4096
        $remain = $metaInt
        while($remain -gt 0) {
            $want = [Math]::Min($buffer.Length,$remain)
            $read = $stream.Read($buffer,0,$want)
            if($read -le 0) { return "" }
            $remain -= $read
        }
        $lenByte = $stream.ReadByte()
        if($lenByte -le 0) { return "" }
        $metaLen = $lenByte * 16
        $meta = New-Object byte[] $metaLen
        $off=0
        while($off -lt $metaLen) {
            $read=$stream.Read($meta,$off,$metaLen-$off)
            if($read -le 0) { break }
            $off += $read
        }
        if($off -le 0) { return "" }
        $raw = [Text.Encoding]::UTF8.GetString($meta,0,$off).Trim([char]0).Trim()
        if($raw -match "StreamTitle='([^']*)'") { return $matches[1].Trim() }
        return ""
    } catch {
        return ""
    } finally {
        try { if($null -ne $stream) { $stream.Close() } } catch {}
        try { if($null -ne $resp) { $resp.Close() } } catch {}
    }
}
function Update-Metadata {
    try {
        $media = $player.currentMedia
        $name = ""
        $title = ""
        $artist = ""
        if($null -ne $media) {
            try { $name = ([string]$media.name).Trim() } catch {}
            $title = First-Info $media @("Title","WM/Title","OriginalTitle","Subtitle")
            $artist = First-Info $media @("Artist","Author","WM/Artist","WM/AlbumArtist","WM/Composer")
            $streamField = First-Info $media @("StreamTitle","WM/StreamTitle","NowPlaying","WM/NowPlaying")
            if(-not [string]::IsNullOrWhiteSpace($streamField)) {
                $split = Split-ArtistTitle $streamField
                if(-not [string]::IsNullOrWhiteSpace($split.Artist)) { $artist = $split.Artist }
                if(-not [string]::IsNullOrWhiteSpace($split.Title)) { $title = $split.Title }
                $name = $streamField
            }
        }

        # Direkter ICY/Shoutcast-Fallback. Nur alle paar Sekunden und mit kurzem Timeout,
        # damit die Player-Schleife nicht durch langsame Server blockiert wird.
        $now = Get-Date
        if(-not [string]::IsNullOrWhiteSpace($activeUrl) -and (($now-$lastIcyPoll).TotalSeconds -ge $icyPollSeconds)) {
            $script:lastIcyPoll = $now
            $icy = Read-IcyStreamTitle $activeUrl
            if(-not [string]::IsNullOrWhiteSpace($icy)) {
                if($icy -ne $script:icyStreamTitle) { Write-Log ("ICY META | " + $icy) }
                $script:icyStreamTitle = $icy
            }
        }

        if(-not [string]::IsNullOrWhiteSpace($script:icyStreamTitle)) {
            $split = Split-ArtistTitle $script:icyStreamTitle
            if(-not [string]::IsNullOrWhiteSpace($split.Artist)) { $artist = $split.Artist }
            if(-not [string]::IsNullOrWhiteSpace($split.Title)) { $title = $split.Title }
            $name = $script:icyStreamTitle
        } elseif(([string]::IsNullOrWhiteSpace($title) -or [string]::IsNullOrWhiteSpace($artist)) -and $name -match '^\s*(.+?)\s+[\-–—]\s+(.+?)\s*$') {
            if([string]::IsNullOrWhiteSpace($artist)) { $artist = $matches[1].Trim() }
            if([string]::IsNullOrWhiteSpace($title)) { $title = $matches[2].Trim() }
        }

        $changed = ($title -ne $script:metaTitle) -or ($artist -ne $script:metaArtist) -or ($name -ne $script:metaMediaName)
        $script:metaTitle = $title
        $script:metaArtist = $artist
        $script:metaMediaName = $name
        if($changed -and (-not [string]::IsNullOrWhiteSpace($title) -or -not [string]::IsNullOrWhiteSpace($name))) {
            Write-Log ("META | Artist=" + $artist + " | Title=" + $title + " | Name=" + $name)
        }
    } catch { Write-Log ("META FEHLER | " + $_.Exception.Message) }
}
function Write-Status {
    try {
        $state = [int]$player.playState
        $name = State-Name $state
        $data = "State=$state`r`n" +
                "StateText=$name`r`n" +
                "Volume=$currentVolume`r`n" +
                "TitleBase64=$(To-B64 $metaTitle)`r`n" +
                "ArtistBase64=$(To-B64 $metaArtist)`r`n" +
                "MediaNameBase64=$(To-B64 $metaMediaName)`r`n"
        $tmp = $statusPath + ".tmp"
        Set-Content -LiteralPath $tmp -Value $data -Encoding UTF8
        Move-Item -LiteralPath $tmp -Destination $statusPath -Force
    } catch {}
}
function Log-WmpError {
    try {
        $err = $player.error; if ($null -eq $err) { return }
        $count = [int]$err.errorCount; if ($count -le 0) { return }
        Write-Log "WMP ERROR COUNT=$count"
        $limit = [Math]::Min($count,3)
        for($i=0;$i -lt $limit;$i++) { try { $item=$err.item($i); Write-Log ("WMP ERROR[$i] " + $item.errorDescription + " | Context=" + $item.errorContext) } catch {} }
    } catch {}
}
function Read-CommandFile([string]$Path) {
    try {
        $map=@{}
        foreach($line in (Get-Content -LiteralPath $Path -Encoding UTF8)) {
            $p=$line.IndexOf('=')
            if($p -gt 0) { $map[$line.Substring(0,$p).Trim()]=$line.Substring($p+1).Trim() }
        }
        return $map
    } catch { Write-Log ("COMMAND READ FEHLER | " + $_.Exception.Message); return $null }
}
function Process-Command([hashtable]$cmd,[string]$fileName) {
    if($null -eq $cmd) { return $false }
    $action=([string]$cmd["Action"]).ToUpperInvariant()
    $v=$currentVolume; $parsed=0
    if([int]::TryParse([string]$cmd["Volume"],[ref]$parsed)) { $v=[Math]::Max(0,[Math]::Min(100,$parsed)) }
    Write-Log ("CMD " + $action + " | " + $fileName + " | Vol=" + $v)

    if($action -eq "PLAY") {
        $url=""
        try { $url=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String([string]$cmd["UrlBase64"])) } catch { Write-Log ("URL DECODE FEHLER | " + $_.Exception.Message) }
        if(-not [string]::IsNullOrWhiteSpace($url)) {
            try {
                $script:activeUrl=$url; $script:currentVolume=$v
                $script:metaTitle=""; $script:metaArtist=""; $script:metaMediaName=""; $script:icyStreamTitle=""; $script:lastIcyPoll=Get-Date "2000-01-01"
                try { $script:player.controls.stop() } catch {}
                $script:player.settings.mute=$false
                $script:player.settings.volume=$script:currentVolume
                $script:player.settings.autoStart=$true
                $script:player.URL=$url
                Write-Log "URL GESETZT | Vol=$currentVolume | $url"
                Pump-Messages 350
                $script:player.controls.play()
                $script:lastPlayAttempt=Get-Date
                $script:lastMetadataPoll=Get-Date "2000-01-01"
                Write-Log "PLAY AUFGERUFEN"
            } catch { Write-Log ("PLAY FEHLER | " + $_.Exception.ToString()) }
        }
    } elseif($action -eq "VOLUME") {
        try { $script:currentVolume=$v; $script:player.settings.mute=$false; $script:player.settings.volume=$script:currentVolume; Write-Log "VOLUME | $currentVolume" } catch { Write-Log ("VOLUME FEHLER | " + $_.Exception.Message) }
    } elseif($action -eq "STOP") {
        try { $script:player.controls.stop() } catch {}; $script:activeUrl=""; $script:metaTitle=""; $script:metaArtist=""; $script:metaMediaName=""; $script:icyStreamTitle=""; $script:lastIcyPoll=Get-Date "2000-01-01"; Write-Log "STOP"
    } elseif($action -eq "QUIT") {
        try { $script:player.controls.stop() } catch {}; Write-Log "QUIT"; return $true
    }
    return $false
}

try {
    if(-not (Test-Path -LiteralPath $BaseDir)) { New-Item -ItemType Directory -Path $BaseDir -Force | Out-Null }
    if(-not (Test-Path -LiteralPath $commandDir)) { New-Item -ItemType Directory -Path $commandDir -Force | Out-Null }
    try { Add-Type -AssemblyName System.Windows.Forms -ErrorAction Stop } catch { Write-Log ("WINDOWS FORMS FEHLER | " + $_.Exception.Message); throw }
    Write-Log "EXTERNAL PLAYER V11.2.5 WMP + ICY META START | PID=$PID | Parent=$ParentPid | Apartment=$([Threading.Thread]::CurrentThread.GetApartmentState())"
    try { $player=New-Object -ComObject WMPlayer.OCX -ErrorAction Stop } catch { Write-Log ("WMP COM START FEHLER | " + $_.Exception.ToString()); throw }
    $player.settings.autoStart=$true; $player.settings.mute=$false; $player.settings.balance=0; $player.settings.volume=$currentVolume
    try { $player.settings.enableErrorDialogs=$false } catch {}
    Write-Log "WMP BEREIT"

    $quit=$false
    while(-not $quit) {
        if($ParentPid -gt 0) { $parent=Get-Process -Id $ParentPid -ErrorAction SilentlyContinue; if($null -eq $parent) { Write-Log "PARENT NICHT MEHR AKTIV - ENDE"; break } }

        $files = @(Get-ChildItem -LiteralPath $commandDir -Filter "*.cmd" -File -ErrorAction SilentlyContinue | Sort-Object Name)
        foreach($f in $files) {
            $cmd=Read-CommandFile $f.FullName
            try { $quit=Process-Command $cmd $f.Name } catch { Write-Log ("COMMAND PROCESS FEHLER | " + $_.Exception.ToString()) }
            try { Remove-Item -LiteralPath $f.FullName -Force -ErrorAction SilentlyContinue } catch {}
            if($quit) { break }
        }

        Pump-Messages 0
        try {
            $state=[int]$player.playState
            if($state -ne $lastState) { $lastState=$state; Write-Log ("WMP STATE $state = " + (State-Name $state)); if($state -eq 0 -or $state -eq 1 -or $state -eq 8 -or $state -eq 10) { Log-WmpError } }
            if(-not [string]::IsNullOrWhiteSpace($activeUrl) -and ($state -eq 1 -or $state -eq 10) -and (((Get-Date)-$lastPlayAttempt).TotalSeconds -ge 2)) {
                try { $player.controls.play(); $lastPlayAttempt=Get-Date; Write-Log "PLAY RETRY | State=$state" } catch { Write-Log ("PLAY RETRY FEHLER | " + $_.Exception.Message) }
            }
        } catch { Write-Log ("STATE FEHLER | " + $_.Exception.Message) }

        $now=Get-Date
        if((($now-$lastMetadataPoll).TotalMilliseconds -ge 1000)) {
            $script:lastMetadataPoll=$now
            Update-Metadata
        }
        if((($now-$lastStatusWrite).TotalMilliseconds -ge 250)) {
            $script:lastStatusWrite=$now
            Write-Status
        }
        Start-Sleep -Milliseconds 25
    }
} catch { Write-Log ("PLAYER FEHLER | " + $_.Exception.ToString()) }
finally {
    try { if($null -ne $player) { $player.controls.stop() } } catch {}
    try { if($null -ne $player) { [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($player) } } catch {}
    try { Remove-Item -LiteralPath $statusPath -Force -ErrorAction SilentlyContinue } catch {}
    Write-Log "EXTERNAL PLAYER ENDE"
}
