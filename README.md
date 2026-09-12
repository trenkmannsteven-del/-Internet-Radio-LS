# Internet Radio LS / Los Santos Multimedia

**v1.0.2 BETA TEST — based on the user-tested v7.41 build**

Internet Radio LS adds an in-game multimedia/head-unit style interface to **GTA V single-player** with real internet radio, GTA Radio integration, favorites, live metadata where available, YouTube Music and Spotify media-session integration, multiple themes, vehicle HUD features, audio ducking and optional vehicle lighting features.

> **Single-player only.** The mod disables its functions when an online/network session is detected.

## v1.0.2 Beta Test

This beta focuses on stability and vehicle/UI polish. The current tested base includes:

- cleaned and repositioned right-side information/status panels
- clearer Spotify / YouTube Music connection and app-volume display
- persistent user settings for important UI/audio/source/vehicle preferences
- refined vehicle speedometer/HUD behavior
- independent manual hazards and refined automatic indicators
- Turbo Blow-Off reduced to a simple ON/OFF switch with one stronger SPORT-style BOV event and anti-spam logic
- factory-turbo recognition plus optional add-on factory-turbo model names
- plate-light support with static-neon/xenon color behavior
- **v7.41 strict plate-light logic:** normal daylight OFF, explicit driver/headlight intent, night low/high-beam follow and flicker debounce
- German and English quick user manuals

### Beta download / installation

For the beta-test source, use the current `main` branch and copy its `scripts` folder into your GTA V main directory.

The packaged beta uses this layout:

- `scripts/03_InternetRadioSimple.3.cs`
- `scripts/InternetRadio/...`

Before updating, optionally back up `scripts/InternetRadio/UserSettings.ini`.

## User manuals

- `BENUTZERHANDBUCH_DE.md` — German quick handbook with table of contents
- `USER_MANUAL_EN.md` — English quick handbook with table of contents
- `RELEASE_NOTES_v1.0.2_BETA_TEST.txt` — beta changes and test focus

## Reference setup

The developer/test reference currently used for this beta is:

- **ScriptHookV .NET Enhanced 3.9.0.6 (1.1.0.6)**
- **API 3.9.0**
- GTA V Enhanced / Singleplayer

Other compatible SHVDN builds may work. If a `.3.cs` compile error occurs, verify that `ScriptHookVDotNet.asi`, `ScriptHookVDotNet2.dll` and `ScriptHookVDotNet3.dll` all come from the same SHVDN release package.

## Core features

- internet radio stations grouped by categories
- favorites and live metadata where supported
- GTA Radio integration
- YouTube Music integration
- Spotify integration
- separate media-service volume handling
- 7 UI languages: English, German, Spanish, French, Italian, Portuguese (Brazil), Turkish
- multiple visual themes
- compact driving HUD / speedometer
- manufacturer-logo display with fallback
- dialogue / phone / menu / Los Santos Customs ducking
- optional Beat Neon and cabin lighting
- plate light linked to real vehicle-light intent
- automatic user-settings persistence
- runtime log-size protection

## Basic controls

| Key | Action |
|---|---|
| `NUM0` | Open / close multimedia menu |
| `NUM8 / NUM2` | Navigate; previous/next track in media tabs |
| `NUM4 / NUM6` | Change menu tab; previous/next outside menu |
| `NUM5` | Select / apply / activate source |
| `NUM1` | Active source on/off or play/pause |
| `NUM3` | Stop / pause |
| `NUM- / NUM+` | Volume down / up |
| `NUM7` | Hazard lights |
| `NUM9` | Manual high beams |
| `SPACE` | Add / remove station favorite |
| `DELETE` | Remove selected favorite |
| `F8` | Reload configuration |

## Requirements

- GTA V for Windows
- ScriptHookV
- ScriptHookVDotNet / ScriptHookV .NET Enhanced compatible with the game build
- Windows PowerShell
- Internet connection for radio streams and optional media integrations

LemonUI is **not required**.

## Personal settings

Personal settings are stored in `scripts/InternetRadio/UserSettings.ini`. The main station/technical configuration remains in `scripts/InternetRadio/InternetRadio.ini`.

The Windows user running GTA V needs read/write/modify permission for `scripts/InternetRadio/` so user settings, caches and helper status files can be created safely.

## Media artwork & privacy

The public build does **not** bundle third-party album/track artwork. Media artwork can be enabled by the user and may use locally available media-session information and temporary cache files.

The mod does not include developer telemetry or analytics. Internet access is used for radio streams and optional media-service integration.

See:

- `DISCLAIMER_AND_LEGAL.txt`
- `THIRD_PARTY_NOTICES.txt`
- `PRIVACY_AND_NETWORK.txt`

## Known limitations

- Third-party radio streams can go offline, change URL, or be geo-blocked.
- YouTube Music / Spotify media-session behavior depends on Windows, the browser/app and the third-party service.
- Vehicle-specific GTA behavior can differ between stock and add-on models; beta feedback should include the vehicle/model name.
- v1.0.2 is a **beta test**, so reproducible logs/screenshots are useful.

## License & legal

Project code and original/mod-created assets are currently published under an **All Rights Reserved** project notice. Publishing the source on GitHub does not grant a general open-source license.

This is an unofficial independent fan-made modification. It is not affiliated with, endorsed by, sponsored by, or officially connected with Rockstar Games, Take-Two Interactive, Spotify, Google, YouTube, or any radio/stream provider.
