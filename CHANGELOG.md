# Changelog

## v1.0.1 Public Beta — TEST74.2

- Rebuilt the safe menu-close fix on the stable TEST73 base.
- Menu close-on-press now uses a harmless request flag consumed on the normal game tick.
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

## TEST70

- Integrated legal/disclaimer notices into the package and INFO UI.

## TEST69

- Optional media-artwork control and runtime cover-cache cleanup.
- No third-party album/track artwork bundled in the public package.
