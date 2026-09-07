# Internet Radio LS

Internet Radio LS adds real internet radio stations to GTA V with categories, favorites, live metadata, YouTube Music integration, multiple themes and a live audio visualizer.

## Features

- 61 real internet radio stations
- 10 radio categories
- 13 selectable themes
- 3 selectable interface languages
- Favorites system with up to 6 slots
- Live metadata where supported
- YouTube Music integration
- Live audio-reactive visualizer
- Dialogue, phone and menu ducking
- Adjustable volume step
- Mini radio UI while driving
- Category-based station colors
- Automatic user settings file
- F8 configuration reload

## Requirements

- Grand Theft Auto V
- ScriptHookV
- ScriptHookVDotNet
- Internet connection

LemonUI is not required.

Google Chrome is recommended for YouTube Music integration.

## Installation

1. Copy the included files into your GTA V `scripts` folder.
2. Keep the `InternetRadio` folder structure intact.
3. Start GTA V.
4. Internet Radio LS will automatically create `UserSettings.ini` on first launch.

The folder:

`GTA V/scripts/InternetRadio`

must have write permission because the mod creates and updates settings, status and runtime files.

## Controls

- `NUM0` - Open / close radio menu
- `NUM8 / NUM2` - Navigate up / down
- `NUM4 / NUM6` - Previous / next page or option
- `NUM5` - Select
- `NUM1` - Radio power / play-pause depending on page
- `NUM3` - Stop radio
- `NUM- / NUM+` - Volume down / up
- `SPACE` - Add / remove favorite
- `F8` - Reload configuration

## Themes

1. Modern Green
2. OEM Blue
3. Red Sport
4. Amber Classic
5. Minimal White
6. Spectrum
7. GTA Vice City
8. GTA San Andreas
9. Cyberpunk Neon
10. NFS Underground 2
11. Minecraft
12. Gangster Luxe
13. Sakura Zen

## Custom Stations

Stations can be added manually in:

`InternetRadio.ini`

Continue the station numbering and add the direct stream URL.

Example:

```ini
[Station62]
Enabled=true
Name=PowerTürk
Pack=TURKISH RADIO
Genre=Turkish Pop
Region=Turkey
Vibe=Turkish Pop / Hits
Url=https://listen.powerapp.com.tr/powerturk/128/icecast.audio

After editing the file, press F8 in-game or restart GTA V.

Geo-Blocking

Some radio stations may be unavailable depending on your country or region.

If a station does not work, you can disable it by changing:

Enabled=true

to:

Enabled=false

Stream URLs can also change over time because they are controlled by the radio providers.

User Settings

UserSettings.ini is created automatically for every user and should not be included in public release packages.

It stores personal settings such as volume, theme, favorites and other preferences.

Compatibility

Tested with:

GTA V
Google Chrome for YouTube Music
NaturalVision Evolved (NVE)
Support

If you enjoy the mod and want to support development:

https://ko-fi.com/st3v3nblub

Author

Made by St3v3nblub

Disclaimer

Internet Radio LS is an independent fan-made GTA V mod.

It is not affiliated with Rockstar Games, Google, YouTube, NaturalVision Evolved, or any of the included radio stations or stream providers.
