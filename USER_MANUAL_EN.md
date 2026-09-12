# LOS SANTOS INTERNET RADIO LS v1.0.2 BETA TEST
## Quick User Manual

This manual is arranged like a small vehicle handbook: quick start and controls first, then the individual systems, followed by troubleshooting.

> **Important:** This mod is intended for **GTA V Singleplayer only**. Mod functions are disabled when an online/network session is detected.

## Table of Contents

1. [Quick Start](#1-quick-start)
2. [Controls](#2-controls)
3. [HOME and Sources](#3-home-and-sources)
4. [Internet Radio](#4-internet-radio)
5. [Spotify and YouTube Music](#5-spotify-and-youtube-music)
6. [Favorites](#6-favorites)
7. [Vehicle Menu](#7-vehicle-menu)
8. [Lights and Plate Light](#8-lights-and-plate-light)
9. [Turbo Blow-Off](#9-turbo-blow-off)
10. [HUD and Vehicle Displays](#10-hud-and-vehicle-displays)
11. [Settings and Saving](#11-settings-and-saving)
12. [Installation and Updates](#12-installation-and-updates)
13. [Troubleshooting](#13-troubleshooting)
14. [Beta Testing: What to Report](#14-beta-testing-what-to-report)

---

## 1. Quick Start

1. Install **ScriptHookV** and **ScriptHookVDotNet**.
2. Copy the included `scripts` folder into the GTA V main directory.
3. Make sure the Windows account running GTA V has **read/write/modify** access to `GTA V/scripts/InternetRadio/`.
4. Start GTA V in **Singleplayer**.
5. Enter a supported vehicle.
6. Press `NUM0` to open the multimedia menu.
7. Navigate with `NUM8 / NUM2` and select with `NUM5`.

`UserSettings.ini` is created automatically on first use.

### Reference setup used for this beta

- **ScriptHookV .NET Enhanced 3.9.0.6 (1.1.0.6)**
- **API 3.9.0**
- GTA V Enhanced / Singleplayer

Other compatible SHVDN versions may work. If you get compile errors, first make sure `ScriptHookVDotNet.asi`, `ScriptHookVDotNet2.dll` and `ScriptHookVDotNet3.dll` all come from the **same release package**.

## 2. Controls

| Key | Action |
|---|---|
| `NUM0` | Open / close multimedia menu |
| `NUM8 / NUM2` | Move selection; previous/next track in Spotify/YouTube |
| `NUM4 / NUM6` | Change menu tab; previous/next station or source action outside menu |
| `NUM5` | Select / apply / activate source |
| `NUM1` | Active source on/off or play/pause |
| `NUM3` | Stop/pause active source |
| `NUM- / NUM+` | Volume down / up |
| `NUM7` | Hazard lights on / off |
| `NUM9` | Manual high beams on / off |
| `SPACE` | Add/remove station favorite |
| `DELETE` | Remove selected favorite |
| `F8` | Reload configuration |
| `ESC / BACK` | Close menu |

`Num Lock` should be enabled.

## 3. HOME and Sources

HOME is the main hub for Internet Radio, GTA Radio, YouTube Music, Spotify, CAR, skins, settings and information. The mod remembers the last active source and restores it where possible after vehicle changes or restarts.

## 4. Internet Radio

Choose stations by category. Track/artist metadata is displayed when the stream provides it. Third-party streams can go offline, change address or be region blocked independently of the mod.

## 5. Spotify and YouTube Music

The mod uses the available Windows/app media session. Start playback in Spotify or YouTube Music first, open the matching LS Multimedia tab and activate it with `NUM5`.

`NUM1` controls play/pause and `NUM8 / NUM2` can control previous/next track in those tabs. The right-side panel shows connection state and app volume.

## 6. Favorites

Press `SPACE` on a station to add/remove it as a favorite. Up to **6 favorites** are supported. Press `DELETE` to remove a selected favorite. Favorites are stored in user settings.

## 7. Vehicle Menu

The CAR tab includes optional vehicle functions such as automatic indicators, hazards, high beams, Beat Neon, cabin light, plate light, Turbo Blow-Off, speedometer/mini HUD and manufacturer logo display.

Some special vehicle classes intentionally do not use every feature.

## 8. Lights and Plate Light

v1.0.2 Beta Test uses the tested v7.41 plate-light logic:

- plate light stays **OFF during normal daylight**
- daylight/DRL/automatic GTA light reports do not switch it on by themselves
- explicit driver light intent can enable it during daytime
- at night it follows real low/high beams
- brief light-state changes are debounced to prevent flicker

Static neon can provide the plate-light color. Without static neon, the plate lamp follows the normal/xenon light family. Beat Neon does not pulse the plate lamp.

## 9. Turbo Blow-Off

Turbo Blow-Off is intentionally **ON/OFF only**. When enabled, one stronger SPORT-style BOV accent is added after real turbo load on an upshift or a clear throttle lift. Anti-spam logic prevents the old repeated-effect behavior.

## 10. HUD and Vehicle Displays

Road vehicles use road-speed/RPM information, boats use a marine HUD and aircraft use a flight HUD. Speed is based on GTA's real entity speed. Manufacturer logos use GTA vehicle-HUD textures with a neutral fallback.

## 11. Settings and Saving

Personal settings are stored in:

`GTA V/scripts/InternetRadio/UserSettings.ini`

`InternetRadio.ini` contains the main configuration, stations and technical defaults. Back up manual changes to that file before updating.

## 12. Installation and Updates

For a fresh install, copy the package's complete `scripts` folder into the GTA V main directory.

For an update, close GTA V, optionally back up `UserSettings.ini`, copy the new files over the old ones and keep `UserSettings.ini` if you want to preserve your personal settings.

## 13. Troubleshooting

**C# compile error:** Check `ScriptHookVDotNet.log`, your SHVDN version and that all SHVDN files come from the same package. Tested reference: **ScriptHookV .NET Enhanced 3.9.0.6 (1.1.0.6), API 3.9.0**.

**Settings not saved:** Check read/write/modify permissions for `GTA V/scripts/InternetRadio/`.

**Spotify/YouTube not connected:** Start playback in the app/browser first and check whether Windows exposes a media session.

**Plate light stays off:** Enable `PLATE LIGHT` in CAR settings and switch on the actual vehicle lights.

## 14. Beta Testing: What to Report

Please include GTA V Enhanced/Legacy, SHVDN version, vehicle/model, active source/settings, exact reproduction steps and screenshots/video for UI or lighting issues.

Important v1.0.2 beta-test areas: right-side UI positioning, plate light, Turbo Blow-Off, persistent user settings and Spotify/YouTube connection state.

---

**LOS SANTOS INTERNET RADIO LS v1.0.2 BETA TEST**  
Unofficial GTA V Singleplayer modification.
