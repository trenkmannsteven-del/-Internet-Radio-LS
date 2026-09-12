# LOS SANTOS INTERNET RADIO LS
## User Manual - v1.0.2 BETA TEST

**Language:** English  
**Tested base:** v7.41  
**Use:** GTA V Singleplayer

> **Important:** This mod is intended for GTA V Singleplayer only. Mod functions are disabled when an online/network session is detected.

## Table of Contents

1. Quick Start
2. Controls
3. HOME and Audio Sources
4. Internet Radio
5. Spotify and YouTube Music
6. Favorites
7. CAR / Vehicle Menu
8. Lights and Plate Light
9. Turbo Blow-Off
10. HUD, Speedometer and Vehicle Displays
11. Settings and Saving
12. Installation, Update and Uninstall
13. Troubleshooting
14. Beta Testing: What to Report

---

## 1. Quick Start

1. Install **ScriptHookV** and **ScriptHookVDotNet**.
2. Copy the complete `scripts` folder from the package into the GTA V main directory.
3. Make sure the Windows account used to run GTA V has **read/write/modify** permission for `GTA V/scripts/InternetRadio/`.
4. Start GTA V in **Singleplayer**.
5. Enter a supported vehicle.
6. Press `NUM0` to open the multimedia menu.
7. Navigate with `NUM8 / NUM2` and select with `NUM5`.

On first use, `scripts/InternetRadio/UserSettings.ini` is created automatically. Personal settings are stored there separately from the main configuration.

### Reference setup for this beta

- **ScriptHookV .NET Enhanced 3.9.0.6 (1.1.0.6)**
- **API 3.9.0**
- GTA V Enhanced / Singleplayer

Other compatible SHVDN versions may work. If you get compile errors, first make sure `ScriptHookVDotNet.asi`, `ScriptHookVDotNet2.dll` and `ScriptHookVDotNet3.dll` all come from the **same release package**.

## 2. Controls

| Key | Action |
|---|---|
| `NUM0` | Open / close multimedia menu |
| `NUM8 / NUM2` | Move selection; previous/next track in Spotify/YouTube |
| `NUM4 / NUM6` | Change tab; previous/next station/source action outside menu |
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

## 3. HOME and Audio Sources

HOME is the main hub for Internet Radio, GTA Radio, YouTube Music, Spotify, CAR / Vehicle, Skins / Themes, Settings and Info.

The last active source is restored where possible after vehicle changes and restarts. Internet Radio, GTA Radio, Spotify and YouTube Music are tracked separately.

## 4. Internet Radio

Choose stations by category. Track and artist metadata is shown when the stream provides it.

Streams are operated by third parties. A station can go offline, change its URL or be region-blocked independently of the mod. If one station does not work, test another station first.

## 5. Spotify and YouTube Music

Spotify and YouTube Music use the available Windows/app media session.

1. Open Spotify or YouTube Music.
2. Start a track.
3. Open the matching tab in the LS multimedia menu.
4. Press `NUM5` to activate the source.
5. `NUM1` = Play/Pause, `NUM8 / NUM2` = previous/next track.

The right-side status panel shows connection state and app volume. **NOT CONNECTED** means that no matching media session is currently detected.

## 6. Favorites

Press `SPACE` on a station to add or remove it as a favorite. Up to **6 favorites** are supported. Press `DELETE` to remove the selected favorite.

Favorites are stored in the user settings and remain available after a restart.

## 7. CAR / Vehicle Menu

The **CAR / VEHICLE** tab contains optional comfort, lighting and display features. Depending on the vehicle, these include automatic indicators, hazards via `NUM7`, high beams via `NUM9`, Beat Neon, cabin light, plate light, Turbo Blow-Off, speedometer / Mini HUD, manufacturer logo, RPM and vehicle displays.

Not every feature is useful for every vehicle class. Bicycles, trains and other special vehicles may intentionally skip some systems.

## 8. Lights and Plate Light

v1.0.2 Beta Test uses the tested v7.41 plate-light logic:

- The plate light stays **OFF during normal daylight**.
- Daylight, shadows, DRL or automatic GTA light reports do not switch it on by themselves.
- Explicit driver light intent can enable it during daytime.
- At night it follows the real low/high beams.
- Brief light-state changes are debounced to prevent flicker.
- `PLATE LIGHT` must be enabled in the CAR menu.

Static neon can provide the plate-light color. Without static neon, the plate lamp follows the normal/xenon light family. Beat Neon does not pulse the plate lamp.

## 9. Turbo Blow-Off

**Turbo Blow-Off** is intentionally `ON / OFF` only. When enabled, one stronger SPORT-style blow-off accent is triggered after real turbo load on an upshift or a clear throttle lift after boost.

Anti-spam logic prevents repeated effect stacks. Detection continues to work at high speed and while airborne. Additional add-on models can be listed in `InternetRadio.ini` using `FactoryTurboModels=`.

## 10. HUD, Speedometer and Vehicle Displays

Road vehicles use speedometer/RPM/vehicle data, boats use a marine HUD, and aircraft/helicopters use a flight HUD.

Speed uses GTA's actual vehicle speed. Manufacturer logos are loaded from GTA vehicle-HUD textures; a neutral fallback is used if no matching logo is available.

## 11. Settings and Saving

Personal settings are stored automatically in `GTA V/scripts/InternetRadio/UserSettings.ini`. This includes many UI, audio, vehicle, favorite, last-source and last-station settings.

`InternetRadio.ini` contains the main configuration, station list, technical defaults and optional add-on configuration. Back up manual changes to this file before updating.

Temporary runtime states such as currently active hazards, high beams or menu cursor position do not need to be saved permanently.

## 12. Installation, Update and Uninstall

### Fresh install

Copy the complete `scripts` folder from the beta package into the GTA V main directory.

### Update

1. Close GTA V.
2. Optionally back up `scripts/InternetRadio/UserSettings.ini`.
3. Copy the new `scripts` files over the old ones.
4. Keep `UserSettings.ini` if you want to preserve personal settings.
5. Start GTA V and test the mod.

### Uninstall

Remove `scripts/03_InternetRadioSimple.3.cs` and `scripts/InternetRadio/`. Do not delete other mods from the `scripts` folder.

## 13. Troubleshooting

**Menu does not open:** Check ScriptHookV, ScriptHookVDotNet, Singleplayer mode and that you are in a supported vehicle.

**C# compile error at startup:** Open `ScriptHookVDotNet.log`. Check the installed SHVDN version and make sure all SHVDN files come from the same package. Tested reference: **ScriptHookV .NET Enhanced 3.9.0.6 (1.1.0.6), API 3.9.0**.

**Radio does not play:** Test another station. Third-party streams can be offline or region-blocked.

**Spotify / YouTube shows NOT CONNECTED:** Start playback in the app/browser first and check whether Windows exposes a media session.

**Settings are not saved:** Check read/write/modify permissions for `GTA V/scripts/InternetRadio/`.

**Plate light stays off:** Enable `PLATE LIGHT` in CAR settings and switch on the actual vehicle lights.

**Plate light is on during the day or flickers:** Report the vehicle name, time of day, light position and, if possible, a short video. v1.0.2 includes the stricter v7.41 logic for this issue.

**INI changes have no effect:** Press `F8` or restart GTA V.

## 14. Beta Testing: What to Report

Useful information for bug reports: GTA V **Enhanced or Legacy**, ScriptHookVDotNet version, vehicle/add-on spawn name, active source, active setting, exact reproduction steps, screenshot or short video for UI/lighting issues, and relevant lines from `ScriptHookVDotNet.log`.

Especially important for v1.0.2: right-side UI positioning, plate light behavior, Turbo Blow-Off, user-setting persistence and Spotify/YouTube connection status.

---

**LOS SANTOS INTERNET RADIO LS v1.0.2 BETA TEST**  
Unofficial GTA V Singleplayer modification.
