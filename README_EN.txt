TEST74.2 - SAFE CLOSE REQUEST HOTFIX
- Built on stable TEST73; menu-close-on-press now uses a harmless request flag consumed on Tick.
- Removed direct KeyDown state changes and GetAsyncKeyState polling.

TEST73 - ICON BASELINE + NEON ACCENT BASE
- Optische Icon-Zentrierung pro Symbol und dezenter Neon-Sockel unter den Symbolen; keine Funktionslogik geaendert.

TEST72 - ICON ALIGNMENT & SPACING POLISH
- Rein visueller Feinschliff fuer Icon-Zentrierung, optische Groesse und konsistente Abstaende in HOME, Navigation und Widgets.

LOS SANTOS MULTIMEDIA / INTERNET RADIO LS v1.0.1
PUBLIC RELEASE - NEXUS MODS / GITHUB
Created by St3v3nblub

IMPORTANT BEFORE USE
- Intended for GTA V single-player only. If the mod detects a GTA Online/network session, its features are disabled.
- This is an unofficial independent mod; no affiliation, endorsement, sponsorship, or partnership with Rockstar Games, Take-Two, Spotify, Google/YouTube, or other third parties.
- No third-party album/track cover files are bundled with the download.
- Media Artwork defaults to OFF on a fresh installation and can be enabled explicitly by the user.
- See THIRD_PARTY_NOTICES.txt, PRIVACY_AND_NETWORK.txt and DISCLAIMER_AND_LEGAL.txt for third-party, network and temporary-file details.


DESCRIPTION
Internet Radio LS adds a complete internet radio system to GTA V with 61 real internet radio stations, 10 categories, 13 visual themes, favorites, live metadata where available, YouTube Music and Spotify integration, automatic audio ducking, a live visualizer and optional audio-reactive vehicle lighting.

The interface is designed to fit naturally into GTA V and includes 7 selectable interface languages.

QUICK START
1. Install ScriptHookV.
2. Install ScriptHookVDotNet.
3. Copy the included "scripts" folder into your GTA V installation directory.
4. Make sure GTA V/scripts/InternetRadio is writable by your Windows user account.
5. Start GTA V, enter a supported vehicle and press NUM0.

MAIN FEATURES
- 61 real internet radio stations
- 10 station categories
- 13 visual themes
- 7 selectable interface languages
- Up to 6 favorite stations
- Live station metadata where supported
- Live audio-reactive visualizer
- YouTube Music integration
- Spotify integration
- Track, artist and album display; optional media artwork is available but defaults to OFF on fresh installs
- Separate YouTube Music and Spotify volume handling
- Smooth hold navigation and progressive menu scrolling
- New infotainment home screen with Now Playing, active source and app tiles for Radio, YouTube Music, Spotify, Car, Skins and Settings
- Soft Spectrum theme with a slow restrained colour flow, deliberate accents and smoothly blended visualizer
- Anti-aliased transparent symbols for navigation, app tiles, volume, source, vehicle and settings
- Optional media artwork is displayed proportionally; no third-party cover files are bundled in the public build
- Theme-aware compact volume HUD
- Compact mini radio UI while driving
- Dialogue, phone-call, menu and Los Santos Customs ducking
- Source restore after player death / next supported vehicle entry
- Adjustable 1-5% volume step
- Category-based station colors
- Optional audio-reactive vehicle neon
- Optional audio-reactive cabin/interior light
- Adjustable Beat Sensitivity, Voice Filter, Pulse Strength and Pulse Speed
- External low-frequency bass analyzer with automatic safe fallback
- Personal settings saved automatically
- Log files automatically limited to about 256 KiB each
- F8 configuration reload
- Radio/media controls blocked on bicycles/BMX and other unsupported vehicle types where applicable

BASS-REACTIVE NEON
Beat-neon controls now live in their own CAR SETTINGS tab. Beat Neon, sensitivity, voice filter, pulse strength and pulse speed are adjusted there.

The beat-neon system can use an external Windows loopback analyzer to focus on low frequencies:
- Approx. 32-78 Hz: sub / low bass
- Approx. 86-176 Hz: kick / bass punch
- Approx. 260-1850 Hz: voice / mids reference used for rejection

If the external analyzer cannot run, Internet Radio LS automatically falls back to the stable peak/transient detector instead of preventing the radio from loading.

The analyzer helper is:
GTA V/scripts/InternetRadio/BassAnalyzerBridge.ps1

THEMES
Modern Green, OEM Blue, Red Sport, Amber Classic, Minimal White, Spectrum (soft dynamic accent palette),
Neon Sunset, West Coast, Future Neon, Street Tuner,
Block World, Noir Luxe and Sakura Zen.

REQUIREMENTS
- GTA V for Windows
- ScriptHookV
- ScriptHookVDotNet
- Internet connection for radio streaming and online music services
- Windows PowerShell for helper scripts
- Write permission for GTA V/scripts/InternetRadio/

LemonUI is NOT required.

MUSIC SERVICES / COMPATIBILITY
- Google Chrome tested with YouTube Music integration
- Spotify Desktop is recommended
- Spotify Web Player media-session fallback is included for Chrome / Edge / Firefox
- NaturalVision Evolved (NVE) tested and compatible

WRITE PERMISSION REQUIRED
Internet Radio LS needs write permission for:
GTA V/scripts/InternetRadio/

The mod creates and updates runtime files such as UserSettings.ini, media status files and logs. If settings or favorites are not being saved, check the folder permissions and make sure the folder is not read-only.

USER SETTINGS
On first use, the mod automatically creates:
GTA V/scripts/InternetRadio/UserSettings.ini

This file stores personal settings such as language, volume, theme, favorites and beat-neon tuning.
When updating the mod, keep your existing UserSettings.ini if you want to preserve your settings.

LOG SIZE PROTECTION
Mod-generated .log files are automatically capped at about 256 KiB each. When a log reaches the limit, old log content is cleared and logging continues. No large backup log is kept.

GEO-BLOCKING / STATION AVAILABILITY
Internet Radio LS uses real internet radio streams provided by third parties. Some stations may be geo-blocked or region-restricted depending on your country. Stations can also temporarily go offline, change stream URLs or provide no metadata.

A station not playing does not automatically mean the mod is broken. Geo-blocking and regional restrictions are controlled by the radio provider.

DISABLE A BLOCKED OR UNWANTED STATION
Open:
GTA V/scripts/InternetRadio/InternetRadio.ini

Find the station entry and change:
Enabled=true

to:
Enabled=false

Then press F8 in-game to reload the configuration or restart GTA V.
Using Enabled=false is recommended instead of deleting the station entry so it can easily be restored later.

BASIC CONTROLS
NUM0       Open / close menu
NUM8/NUM2  Navigate up / down; select app on HOME; previous / next where supported
NUM4/NUM6  Change tab / navigate; previous / next outside menu where supported
NUM5       Open app on HOME; otherwise select / apply / activate music service
NUM1       Radio power / play-pause depending on page
NUM3       Stop radio / pause music service depending on page
NUM-/NUM+  Volume down / up
SPACE      Add / remove favorite on the Stations page
F8         Reload configuration

IMPORTANT
Internet Radio LS is an independent fan-made GTA V mod and is not affiliated with Rockstar Games, Google, YouTube, Spotify, NaturalVision Evolved, or any included radio station or streaming provider.

Station availability, metadata, service behavior and stream URLs are controlled by third parties and may change.

Languages: English, German, Spanish, French, Italian, Portuguese (Brazil), Turkish.
