# Changelog

## v1.0.2 BETA TEST — 74.3 tested base

- Reworked and repositioned the right-side status/information panels for more consistent spacing and readable service status.
- Cleaned Spotify / YouTube Music status labels and connection/app-volume presentation.
- Improved persistent user settings for important source, station, skin, playback and vehicle/UI preferences.
- Improved vehicle speedometer/HUD behavior and manufacturer-logo fallback handling.
- Manual hazards remain independent via NUM7; automatic-indicator behavior refined.
- Added/refined plate-light feature with static-neon/xenon color behavior.
- v7.41 plate-light fix: normal daylight is gated OFF, explicit driver/headlight intent can enable the lamp, night follows real low/high beams, and short state changes are debounced to prevent flicker.
- Turbo Blow-Off is now a simple ON/OFF feature using the tested SPORT-style timing.
- Turbo BOV event is limited to one event after real boost load on upshift or clear throttle lift, with anti-spam cooldown.
- Factory/stock turbo recognition retained; add-on factory-turbo model names can be configured via `FactoryTurboModels=`.
- Added German and English quick user manuals with tables of contents and beta troubleshooting guidance.
- Reference test environment documented as ScriptHookV .NET Enhanced 3.9.0.6 (1.1.0.6), API 3.9.0.

## v1.0.1 Public Beta — TEST74.2

- Rebuilt the safe menu-close fix on the stable TEST73 base.
- Menu close-on-press uses a harmless request flag consumed on the normal game tick.
- Removed the crash-prone direct `KeyDown` state-toggle approach from TEST74.
- Removed the `GetAsyncKeyState` menu polling approach from TEST74.1.
- Retained final icon baseline/card-balance tuning and the subtle neon pedestal under UI symbols.
- Public-release safeguards retained: single-player/network guard, neutral provider icons, Artwork OFF by default, runtime cache cleanup and legal/privacy notices.

## TEST73

- Per-icon optical alignment corrections.
- Added restrained neon-style accent pedestal below media/UI symbols.

## TEST72

- Icon alignment, size normalization and spacing polish across HOME, navigation and widgets.

## TEST71

- Public-release build for GitHub/Nexus preparation.
- Neutral media/provider icons.
- Media Artwork defaults to OFF on clean installs.
- Added single-player/network-session guard.
- Added third-party, privacy/network and publishing documentation.
