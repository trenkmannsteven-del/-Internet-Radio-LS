# Internet Radio LS / Los Santos Multimedia

**Current public test build: v1.0.2 BETA TEST - tested base v7.41**

Internet Radio LS adds an in-game multimedia/head-unit style interface to **GTA V Singleplayer** with Internet Radio, GTA Radio integration, favorites, live metadata where available, Spotify and YouTube Music media-session integration, multiple themes, vehicle HUD features, audio ducking and optional vehicle lighting features.

> **Singleplayer only.** The mod disables its functions when an online/network session is detected.

## Download

Use the GitHub Release **`v1.0.2-beta-test`** for the packaged beta build.

The installable package contains:

- `scripts/03_InternetRadioSimple.3.cs`
- `scripts/InternetRadio/...`

## User manuals

PDF manuals are available in all seven supported UI languages:

| Language | PDF |
|---|---|
| Deutsch | [Benutzerhandbuch PDF](docs/manuals/pdf/Internet_Radio_LS_v1.0.2_BETA_Manual_DE.pdf) |
| English | [User Manual PDF](docs/manuals/pdf/Internet_Radio_LS_v1.0.2_BETA_Manual_EN.pdf) |
| Español | [Manual PDF](docs/manuals/pdf/Internet_Radio_LS_v1.0.2_BETA_Manual_ES.pdf) |
| Français | [Manuel PDF](docs/manuals/pdf/Internet_Radio_LS_v1.0.2_BETA_Manual_FR.pdf) |
| Italiano | [Manuale PDF](docs/manuals/pdf/Internet_Radio_LS_v1.0.2_BETA_Manual_IT.pdf) |
| Português (Brasil) | [Manual PDF](docs/manuals/pdf/Internet_Radio_LS_v1.0.2_BETA_Manual_PT-BR.pdf) |
| Türkçe | [Kullanıcı Kılavuzu PDF](docs/manuals/pdf/Internet_Radio_LS_v1.0.2_BETA_Manual_TR.pdf) |

See the complete [documentation index](docs/README.md) for setup, legal information, release notes and editable manual sources.

## v1.0.2 Beta Test highlights

- cleaned and repositioned right-side information/status panels
- clearer Spotify / YouTube Music connection and app-volume display
- persistent user settings for important UI/audio/source/vehicle preferences
- refined vehicle speedometer/HUD behavior
- independent manual hazards and refined automatic indicators
- Turbo Blow-Off reduced to a simple ON/OFF switch with one stronger SPORT-style BOV event and anti-spam logic
- factory-turbo recognition plus optional add-on factory-turbo model names
- plate-light support with static-neon/xenon color behavior
- **v7.41 strict plate-light logic:** normal daylight OFF, explicit driver/headlight intent, night low/high-beam follow and flicker debounce
- PDF user manuals in all seven UI languages

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

### Tested reference setup

- **ScriptHookV .NET Enhanced 3.9.0.6 (1.1.0.6)**
- **API 3.9.0**
- GTA V Enhanced / Singleplayer

Other compatible SHVDN builds may work. If a `.3.cs` compile error occurs, verify that `ScriptHookVDotNet.asi`, `ScriptHookVDotNet2.dll` and `ScriptHookVDotNet3.dll` all come from the same SHVDN release package.

## Personal settings

Personal settings are stored in `scripts/InternetRadio/UserSettings.ini`. The main station/technical configuration remains in `scripts/InternetRadio/InternetRadio.ini`.

The Windows account running GTA V needs read/write/modify permission for `scripts/InternetRadio/` so user settings, caches and helper status files can be created safely.

## Repository layout

```text
.github/                  GitHub workflows and project metadata
scripts/                  Installable mod source and runtime files
docs/
  manuals/
    pdf/                   User manuals in 7 languages
    source/                Editable Markdown manual sources
  setup/                   Installation and update documentation
  legal/                   Privacy, disclaimer, credits and third-party notices
  releases/                Version-specific release notes
  archive/                 Older documentation kept for reference
  maintainer/              Publishing/maintenance notes
README.md                  Project overview
CHANGELOG.md               Current change history
LICENSE                    Project license notice
```

## Privacy and legal

The public build does **not** bundle third-party album/track artwork. The mod does not include developer telemetry or analytics. Internet access is used for radio streams and optional media-service integration.

See [docs/legal](docs/legal/) for the disclaimer, privacy/network information, credits and third-party notices.

Project code and original/mod-created assets are currently published under an **All Rights Reserved** project notice. Publishing the source on GitHub does not grant a general open-source license.

This is an unofficial independent fan-made modification. It is not affiliated with, endorsed by, sponsored by, or officially connected with Rockstar Games, Take-Two Interactive, Spotify, Google, YouTube, or any radio/stream provider.
