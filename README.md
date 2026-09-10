# Internet Radio LS / Los Santos Multimedia

**Public Beta v1.0.1 — based on the tested TEST74.2 hotfix**

Internet Radio LS adds an in-game multimedia/head-unit style interface to **GTA V single-player** with real internet radio, favorites, live metadata where available, YouTube Music and Spotify media-session integration, multiple themes, a visualizer, audio ducking and optional audio-reactive vehicle lighting.

> **Single-player only.** The public build includes a GTA Online/network-session guard that disables the mod when an online/network session is detected.

## Download

Use the latest package from the **GitHub Releases** page:

https://github.com/trenkmannsteven-del/-Internet-Radio-LS/releases

The v1.0.1 public-beta package is built from the tested TEST74.2 code path.

## Highlights

- 61 internet radio stations in 10 categories
- 13 visual themes
- 7 UI languages: English, German, Spanish, French, Italian, Portuguese (Brazil), Turkish
- up to 6 favorite stations
- live station metadata where supported
- YouTube Music integration
- Spotify integration
- separate service volume handling
- compact driving HUD and volume HUD
- dialogue / phone / menu / Los Santos Customs ducking
- live audio visualizer
- optional bass-reactive vehicle neon and cabin light
- safe low-frequency analyzer fallback
- automatic user settings
- runtime log-size protection

## Requirements

- GTA V for Windows
- ScriptHookV
- ScriptHookVDotNet
- Windows PowerShell
- Internet connection for radio streams and optional online media integrations

LemonUI is **not required**.

## Installation

1. Download the latest ZIP from GitHub Releases.
2. Extract it into the GTA V installation directory.
3. Keep the included `scripts/InternetRadio/` folder structure intact.
4. Make sure `GTA V/scripts/InternetRadio/` is writable by your Windows user account.
5. Start GTA V in single-player, enter a supported vehicle and press `NUM0`.

## Controls

| Key | Action |
|---|---|
| `NUM0` | Open / close multimedia menu |
| `NUM8 / NUM2` | Navigate up/down; app selection on HOME |
| `NUM4 / NUM6` | Change tab / previous-next where supported |
| `NUM5` | Select / open app / activate service |
| `NUM1` | Radio power or play/pause depending on page |
| `NUM3` | Stop radio / pause media service depending on page |
| `NUM- / NUM+` | Volume down / up |
| `SPACE` | Add / remove favorite on Stations page |
| `F8` | Reload configuration |

## Media Artwork & Privacy

The public build does **not** bundle third-party album/track artwork. **Media Artwork defaults to OFF** on fresh installations. If the user explicitly enables it, artwork may be read from locally available media-session information and cached temporarily for the in-game interface. Runtime caches are cleaned by the mod and again on the next start where applicable.

The mod does not include developer telemetry or analytics. Internet access is used for the radio streams and optional media-service integration required by the feature set.

See:

- `DISCLAIMER_AND_LEGAL.txt`
- `THIRD_PARTY_NOTICES.txt`
- `PRIVACY_AND_NETWORK.txt`

## Public-release branding

The public build uses neutral/mod-created media icons. No Spotify or YouTube logo files and no third-party album artwork are bundled in the package.

## Source layout

After a v1.0.1 release is published, the release-sync workflow updates the repository source tree from the tested release ZIP:

- `scripts/03_InternetRadioSimple.3.cs` — GTA/ScriptHookVDotNet controller and UI
- `scripts/InternetRadio/*.ps1` — radio/media/analyzer helper processes
- `scripts/InternetRadio/InternetRadio.ini` — station and default configuration
- `scripts/InternetRadio/*.png` — original/mod-created UI assets used by the public build

## Known limitations

- Third-party radio streams can go offline, change URL, or be geo-blocked.
- YouTube Music / Spotify media-session behavior depends on Windows, the browser/app and the third-party service.
- A hard process crash can leave temporary runtime files behind; the mod performs startup cleanup where applicable.

## License & legal

Project code and original/mod-created assets are currently published under an **All Rights Reserved** project notice. Publishing the source on GitHub does not grant a general open-source license.

This is an unofficial independent fan-made modification. It is not affiliated with, endorsed by, sponsored by, or officially connected with Rockstar Games, Take-Two Interactive, Spotify, Google, YouTube, or any radio/stream provider.

Third-party names and service references are used for identification of supported integrations only. All third-party rights remain with their respective owners.

See `LICENSE`, `DISCLAIMER_AND_LEGAL.txt` and `THIRD_PARTY_NOTICES.txt` for details.
