using System;
// LOS SANTOS INTERNET RADIO LS - v1.0.2 BETA TEST (v7.41 tested base)
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;
using GTA;
using GTA.UI;
using GTA.Native;
using Keys = System.Windows.Forms.Keys;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;

public class InternetRadioSimpleV120YouTube : Script
{
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

    public static InternetRadioSimpleV120YouTube Instance;
    private class Station
    {
        public string Name;
        public string Url;
        public bool Enabled;
        public string Genre;
        public string Region;
        public string Vibe;
        public string Pack;
    }

    // v7.9: GTA's original in-game radio stations are a first-class source in the
    // multimedia UI. No audio is copied or redistributed; playback stays inside GTA.
    private class GtaRadioStationEntry
    {
        public RadioStation Station;
        public string Name;
    }

    private readonly List<Station> stations = new List<Station>();
    private readonly List<GtaRadioStationEntry> gtaRadioStations = new List<GtaRadioStationEntry>();
    private bool gtaRadioModeActive = false;
    private bool gtaRadioPlayingRequested = true;
    private int gtaRadioIndex = 0;
    private int gtaRadioMenuIndex = 0;
    private int rememberedGtaRadioMenuIndex = -1;
    private DateTime gtaRadioMiniHideAt = DateTime.MinValue;
    private DateTime gtaRadioLastNativeSyncAt = DateTime.MinValue;
    // v7.10: GTA exposes the current radio track through its own text-id table.
    // Keep it cached so frame rendering never calls audio/text natives directly.
    private int gtaRadioTrackId = -1;
    private string gtaRadioArtist = "";
    private string gtaRadioTitle = "";
    private DateTime gtaRadioLastMetadataQueryAt = DateTime.MinValue;
    private int gtaRadioLastMetadataLogId = int.MinValue;
    private string dir;
    private string logPath;
    private int index;
    private int volume = 25;
    private int playerMaxVolume = 70;
    private float volumeCurveExponent = 1.10f;
    private int youtubeMaxVolume = 55;
    private float youtubeVolumeCurveExponent = 1.55f;
    private int youtubeVolumeProfileVersion = 0;
    private int spotifyMaxVolume = 55;
    private float spotifyVolumeCurveExponent = 1.55f;
    private bool autoLaunchYouTubeMusic = false;
    private bool autoActivateYouTubeMusic = false;
    private bool autoPlayYouTubeMusic = false;
    private bool youtubeAutoActivationConsumed = false;
    private bool youtubeAutoPlayPending = false;
    private int volumeStep = 1;
    private bool enabled = true;
    private bool onlyInVehicle = true;
    private bool autoStart = true;
    private bool showMiniUi = true;
    private bool persistentMiniUi = false;
    private bool hideMiniUiWhileMenuOpen = true;
    private float miniUiLeft = 0.73f;
    private float miniUiTop = 0.64f;
    private float miniUiWidth = 0.235f;
    private float miniUiHeight = 0.122f;

    // Custom in-car speedometer. Kept independent from media source colours so it
    // remains part of the selected vehicle/UI skin even when YouTube/Spotify is active.
    private bool showCustomSpeedometer = true;
    private float speedometerLeft = 0.760f;
    private float speedometerTop = 0.826f;
    private float speedometerWidth = 0.205f;
    private float speedometerHeight = 0.126f;
    private int speedometerOpacity = 92;
    private bool speedometerShowCarIcon = true; // GTA in-game manufacturer logo
    private bool speedometerShowRpm = true;
    private bool speedometerUseMph = false;
    private bool showBoatHud = true;
    private bool showFlightHud = true;
    // Standard marine/aviation units: knots, feet and feet/minute.
    // Metric mode uses km/h, metres and metres/second.
    private bool navigationUseMetric = false;

    // v7.28: Tesla-style automatic indicators. Steering intent activates the appropriate
    // side; cancellation waits for the actual manoeuvre to finish. Lane changes keep the
    // same signal through the counter-steer phase, while real turns use heading change +
    // steering recentring as the completion cue. Hazards remain manual on one key.
    private bool automaticIndicatorsEnabled = true;
    private bool automaticEmergencyHazardsEnabled = false;
    private bool manualHazardsActive = false;
    // Manual high beam: one configurable toggle key. The light override is released
    // again when switched off, when leaving/changing the vehicle, or when the script stops.
    private bool manualHighBeamActive = false;
    private int manualHighBeamVehicleHandle = 0;
    private int automaticIndicatorVehicleHandle = 0;
    private int automaticIndicatorState = 0; // -1 left, 0 off, +1 right, 2 hazards
    private int automaticIndicatorCandidate = 0;
    private DateTime automaticIndicatorCandidateSince = DateTime.MinValue;
    private DateTime automaticIndicatorActivatedAt = DateTime.MinValue;
    private DateTime automaticIndicatorCenteredSince = DateTime.MinValue;
    private DateTime automaticIndicatorLowSpeedSince = DateTime.MinValue;
    private DateTime automaticIndicatorCompletionSince = DateTime.MinValue;
    private DateTime automaticIndicatorLastSpeedAt = DateTime.MinValue;
    private DateTime automaticHazardUntil = DateTime.MinValue;
    private float automaticIndicatorLastSpeedMps = 0.0f;
    private float automaticIndicatorStartHeading = 0.0f;
    private float automaticIndicatorMaxHeadingDelta = 0.0f;
    private float automaticIndicatorPeakSteer = 0.0f;
    private bool automaticIndicatorCounterSteerSeen = false;
    private bool automaticIndicatorTurnDetected = false;

    private int speedometerLogoCachedModelHash = 0;
    private string speedometerLogoCachedTxd = "";
    private string speedometerLogoCachedTexture = "";
    private float speedometerLogoCachedAspect = 1.85f;
    private bool speedometerLogoAspectResolved = false;

    private bool speechDucking = true;
    private bool phoneCallDucking = true;
    private bool menuDucking = true;
    private int duckVolumePercent = 70;
    // DuckReleaseMs ist die Dialog-Haltezeit: kurze Pausen zwischen Saetzen bleiben geduckt.
    private int duckReleaseMs = 1800;
    private int duckAttackMs = 350;
    private int duckFadeOutMs = 900;
    private float duckMix = 0.0f;
    private DateTime lastDuckRampAt = DateTime.MinValue;
    private bool duckActive;
    private string duckReason = "";
    private DateTime lastSpeechAt = DateTime.MinValue;
    private DateTime lastSpeechScan = DateTime.MinValue;
    private DateTime lastLogicTick = DateTime.MinValue;

    // TEST54 PERFORMANCE PASS:
    // Expensive GTA native state is refreshed by the normal ~100 ms logic tick
    // and reused by frame rendering / lighting instead of being queried repeatedly.
    private bool cachedPauseMenuActive = false;
    private bool cachedPlayerDead = false;
    private bool cachedRadioCapableVehicle = false;
    private bool cachedAudioReactiveLightingVehicle = false;
    private bool cachedRoadSpeedometerVehicle = false;
    private bool cachedBoatHudVehicle = false;
    private bool cachedAircraftHudVehicle = false;
    private int cachedVehicleClassId = -1;

    // v7.28: Vehicle instruments use GTA's own physics velocity directly.
    // GET_ENTITY_SPEED/Vehicle.Speed is already metres per second; deriving speed from
    // world displacement divided by wall-clock time caused lag and wrong readings during
    // slow motion, Franklin's driving focus and frame stalls.
    private int instrumentMotionVehicleHandle = 0;
    private bool instrumentMotionSampleValid = false;
    private float instrumentMotionSpeed3dMps = 0.0f;
    private float instrumentMotionGroundSpeedMps = 0.0f;
    private float instrumentMotionVerticalMps = 0.0f;
    private bool cachedInLosSantosCustoms = false;
    private int cachedVehicleHandle = 0;
    // v7.28: playback continuity tracks the actual vehicle handle, not only the
    // coarse in-vehicle boolean. This catches direct car-to-car switches where
    // GTA never exposes an on-foot tick between the two vehicles.
    private int playbackVehicleHandle = 0;
    private bool radioResumeAfterVehicle = false;
    private int appliedStreamVolume = -1;
    private bool wasInVehicle;
    private bool shouldPlay;
    private bool autoSkipUnavailable = true;
    private int streamFailTimeoutMs = 9000;
    private DateTime radioLastHealthyAt = DateTime.MinValue;
    private bool radioFailureHandled = false;
    private bool wasPauseMenuActive;
    private bool resumeAfterPause;
    private bool menuOpen;
    private bool menuOpenSoundEnabled = true;
    // TEST69: provider artwork is optional and session-local only.
    private bool mediaArtworkEnabled = false;
    // TEST72 public build: hard-disable mod features whenever a GTA Online
    // network session is detected. This is an additional safeguard on top of
    // ScriptHookV's own single-player restrictions.
    private bool publicReleaseOnlineGuardActive = false;
    private bool publicReleaseWasOnline = false;
    private DateTime publicReleaseLastOnlineCheck = DateTime.MinValue;
    // V12.44: Dynamic station/category tint is exclusive to the SPECTRUM skin.
    // The other skins keep their coherent fixed theme; category colors are used as semantic accents.
    private float dynamicRadioBackgroundStrength = 0.72f;
    private float dynamicMenuR = 61f;
    private float dynamicMenuG = 238f;
    private float dynamicMenuB = 105f;
    private bool dynamicMenuColorInitialized = false;

    // TEST61 DYNAMIC SPECTRUM:
    // A small cached four-colour palette drives the Spectrum skin across the UI.
    // It updates at 20 Hz (50 ms), so the look can move continuously without adding
    // meaningful work to the GTA frame loop.
    private int spectrumPaletteLastTick = -1000;
    private int spectrumPrimaryR = 35, spectrumPrimaryG = 214, spectrumPrimaryB = 255;
    private int spectrumSecondaryR = 235, spectrumSecondaryG = 68, spectrumSecondaryB = 218;
    private int spectrumTertiaryR = 139, spectrumTertiaryG = 92, spectrumTertiaryB = 246;
    private int spectrumWarmR = 255, spectrumWarmG = 181, spectrumWarmB = 56;
    private bool menuSlowMotionEnabled = true;
    private float menuTimeScale = 0.45f;
    private bool menuSlowMotionApplied = false;
    private int menuIndex;
    // TEST60: Home dashboard selection (Radio, YT Music, Spotify, Car, Skins, Settings).
    private int homeMenuIndex = 0;
    private int menuTab = 0; // Internal IDs stay stable; visual/navigation order is defined below.
    private static readonly int[] MenuTabOrder = new int[] { 0, 1, 9, 5, 6, 2, 4, 7, 8, 3 };
    private int packIndex = 0;
    private int skinIndex = 0;
    private int skinMenuIndex = 0;
    private int settingsMenuIndex = 0;
    private int carSettingsMenuIndex = 0;
    private string uiLanguage = "EN";
    private bool volumeHoldActive = false;
    private Keys volumeHoldKey = Keys.None;
    private DateTime volumeHoldNext = DateTime.MinValue;
    private DateTime volumeHoldStartedAt = DateTime.MinValue;
    // v1.0.1: Smooth menu navigation. Num8/Num2 and Num4/Num6 can be held
    // to scroll through lists/tabs instead of requiring repeated taps.
    private bool menuHoldActive = false;
    private Keys menuHoldKey = Keys.None;
    private DateTime menuHoldNext = DateTime.MinValue;
    private DateTime menuHoldStartedAt = DateTime.MinValue;
    // Menu close: visual state is cleared immediately on KeyDown; cleanup/save/time-scale
    // restoration stays on the normal main Tick for thread-safe native/file work.
    private bool menuCloseRequested = false;
    private bool menuCloseHandledOnPress = false;

    // v7.6 loading-transition safety: never process custom input/rendering while GTA is
    // faded/loading or before a valid local player ped exists. Native calls against stale
    // entity handles during a transition can crash the game below managed try/catch.
    private bool gameplayInteractionReady = false;
    private DateTime gameplayReadyCandidateSince = DateTime.MinValue;
    private bool restoreTimeScaleWhenGameplayReady = false;
    internal static bool SafeRenderState = false;

    // v1.0.1 TEST10: remember each menu tab's cursor while the menu is open.
    private int rememberedRadioMenuIndex = -1;
    private int rememberedStationMenuIndex = -1;
    private int rememberedPackMenuIndex = -1;
    private int rememberedSkinMenuIndex = -1;
    private int rememberedSettingsMenuIndex = -1;
    private int rememberedCarSettingsMenuIndex = -1;
    private DateTime resetDefaultsConfirmUntil = DateTime.MinValue;

    // Soft mini-UI fade. The visibility timer is independent from the notification banner.
    // This keeps the mini UI available even though general popup banners are disabled in v1.0.1.
    private float miniUiFade = 0.0f;
    private DateTime miniUiFadeUpdatedAt = DateTime.MinValue;
    private DateTime miniUiVisibleUntil = DateTime.MinValue;

    // v1.0.1 TEST20: compact themed volume HUD. It has its own timer/fade so
    // changing the volume never re-enables the old notification banner.
    private float volumeHudFade = 0.0f;
    private DateTime volumeHudFadeUpdatedAt = DateTime.MinValue;
    private DateTime volumeHudVisibleUntil = DateTime.MinValue;
    private int volumeHudDirection = 0;

    private float drawAlphaMultiplier = 1.0f;

    // v7.24: ScriptHookVDotNet CustomSprite uses persistent texture-instance slots for
    // roughly 100 ms. If one frame draws fewer copies of the same PNG than the previous
    // frame, an old instance can remain visible for a moment and look like a flicker.
    // Track the per-frame draw count and overwrite only the now-unused slots transparently.
    // This keeps icons/covers cached and stable while switching tabs without mass-flushing
    // every sprite on a navigation change.
    private readonly int[] uiIconDrawCountThisFrame = new int[13];
    private readonly int[] uiIconDrawCountPreviousFrame = new int[13];
    private int youtubeCoverDrawCountThisFrame = 0;
    private int youtubeCoverDrawCountPreviousFrame = 0;
    private int spotifyCoverDrawCountThisFrame = 0;
    private int spotifyCoverDrawCountPreviousFrame = 0;

    private readonly string[] skinNames = new string[] { "MODERN GREEN", "OEM BLUE", "RED SPORT", "AMBER CLASSIC", "MINIMAL WHITE", "SPECTRUM", "NEON SUNSET", "WEST COAST", "FUTURE NEON", "STREET TUNER", "BLOCK WORLD", "NOIR LUXE", "SAKURA ZEN" };
    private string selectedPack = "";
    private const int MaxFavorites = 6;
    private const string FavoritesPackKey = "FAVORITEN";
    private readonly List<string> favoriteStationNames = new List<string>();
    private readonly List<string> menuPacks = new List<string>();
    private readonly List<int> filteredStationIndices = new List<int>();
    private Keys toggleKey = Keys.NumPad1;
    private Keys prevKey = Keys.NumPad4;
    private Keys nextKey = Keys.NumPad6;
    private Keys reloadKey = Keys.F8;
    private Keys menuKey = Keys.NumPad0;
    private Keys menuUpKey = Keys.NumPad8;
    private Keys menuDownKey = Keys.NumPad2;
    private Keys selectKey = Keys.NumPad5;
    private Keys volumeDownKey = Keys.Subtract;
    private Keys volumeUpKey = Keys.Add;
    private Keys stopKey = Keys.NumPad3;
    private Keys hazardKey = Keys.NumPad7;
    private Keys highBeamKey = Keys.NumPad9;
    private static readonly int[] CarSettingOrder = new int[] {
        0, 1, 2, 3, 4, 5, 6, 19, 18,    // lighting incl. plate light + automatic indicators
        20,                               // vehicle audio: turbo impact
        7, 10, 11, 12, 14, 15, 16, 17, // speedometer
        8, 9, 13                         // boat/flight HUD
    };
    private AudioWorker audio;
    private YouTubeMusicBridge youtube;
    private bool youtubeModeActive = false;
    private bool youtubeSelectionPending = false;
    private bool youtubeResumeAfterPause = false;
    private bool youtubeResumeAfterVehicle = false;
    private bool youtubeAvailable = false;
    private string youtubeState = "Not connected";
    private string youtubeTitle = "";
    private string youtubeArtist = "";
    private string youtubeAlbum = "";
    private string youtubeSource = "";
    private string youtubeCoverPath = "";
    private string youtubeCoverLoadedPath = "";
    private GTA.UI.CustomSprite youtubeCoverSprite;
    private GTA.UI.CustomSprite blockWorldMotifSprite;
    private GTA.UI.CustomSprite sakuraZenMotifSprite;
    private GTA.UI.CustomSprite spectrumMotifSprite;
    private GTA.UI.CustomSprite neonSunsetMotifSprite;
    private GTA.UI.CustomSprite westCoastMotifSprite;
    private GTA.UI.CustomSprite futureNeonMotifSprite;
    private GTA.UI.CustomSprite streetTunerMotifSprite;
    private GTA.UI.CustomSprite noirLuxeMotifSprite;
    // TEST62: anti-aliased transparent UI icons are loaded once and reused.
    // They replace the blocky rectangle-built TEST61 symbols without adding frame-time I/O.
    private readonly GTA.UI.CustomSprite[] uiIconSprites = new GTA.UI.CustomSprite[13];
    private readonly float[] uiIconAutoOffsetX = new float[13];
    private readonly float[] uiIconAutoOffsetY = new float[13];
    private readonly float[] uiIconAutoScale = new float[13];
    private readonly string[] uiIconAssetNames = new string[]
    {
        "UiIcon_Home.png", "UiIcon_Radio.png", "UiIcon_Grid.png", "UiIcon_Info.png",
        "UiIcon_Skins.png", "UiIcon_Music.png", "UiIcon_Streaming.png", "UiIcon_Car.png",
        "UiIcon_Settings.png", "UiIcon_Volume.png", "UiIcon_Source.png", "UiIcon_Neon.png",
        "UiIcon_GtaRadio.png"
    };
    // v7.21: use GTA's own HUD texture dictionary for radio station logos.
    // No station-logo PNG copies are required; we draw the original cells directly
    // from hud.ytd (including any hud.ytd replacement the player already uses).
    private bool gtaHudTextureDictionaryRequested = false;
    private DateTime gtaHudTextureRetryAt = DateTime.MinValue;
    private string youtubeCoverTrackKey = "";
    private DateTime youtubeCoverNextLoadCheck = DateTime.MinValue;
    private DateTime youtubeMiniHideAt = DateTime.MinValue;
    private int appliedYoutubeVolume = -1;
    private float appliedYoutubeMixerLevel = -1.0f;
    private SpotifyMusicBridge spotify;
    private BassAnalyzerBridge bassAnalyzer;
    private bool spotifyModeActive = false;
    private bool spotifySelectionPending = false;
    private bool spotifyResumeAfterPause = false;
    private bool spotifyResumeAfterVehicle = false;
    private bool spotifyAutoPlayPending = false;
    private bool spotifyAvailable = false;
    private string spotifyState = "Not connected";
    private string spotifyTitle = "";
    private string spotifyArtist = "";
    private string spotifyAlbum = "";
    private string spotifySource = "";
    private string spotifyCoverPath = "";
    private string spotifyCoverLoadedPath = "";
    private GTA.UI.CustomSprite spotifyCoverSprite;
    private string spotifyCoverTrackKey = "";
    private DateTime spotifyCoverNextLoadCheck = DateTime.MinValue;
    private DateTime spotifyMiniHideAt = DateTime.MinValue;
    private int appliedSpotifyVolume = -1;
    private float appliedSpotifyMixerLevel = -1.0f;

    // v7.28: persist the last active media source and Internet-Radio station.
    // This keeps RADIO / GTA / YOUTUBE / SPOTIFY and the selected stream stable across
    // vehicle changes, F8 reloads, ScriptHookVDotNet reloads and GTA restarts.
    private string preferredVehicleSource = "RADIO"; // RADIO / GTA / YOUTUBE / SPOTIFY
    private int lastInternetStationIndex = -1;
    private string lastInternetStationName = "";
    // v7.31: persist the real Playing/Paused state for external media sources too.
    // Temporary pauses caused by leaving the car, death or the GTA pause menu are ignored.
    private bool lastYouTubePlaybackPlaying = false;
    private bool lastSpotifyPlaybackPlaying = false;
    private DateTime youtubePlaybackCommandGraceUntil = DateTime.MinValue;
    private DateTime spotifyPlaybackCommandGraceUntil = DateTime.MinValue;
    private bool wasPlayerDead = false;

    private string bannerTop = "";
    private string bannerBottom = "";
    private DateTime bannerUntil = DateTime.MinValue;
    private int bannerR = 190;
    private int bannerG = 24;
    private int bannerB = 38;
    private string actionFeedbackText = "";
    private DateTime actionFeedbackUntil = DateTime.MinValue;
    // v1.0.1: notification bar is intentionally only used for explicit
    // YouTube Music Num5 action. All other ShowBanner calls stay silent.
    private bool allowBannerOnce = false;
    private string uiTitle = "INTERNET RADIO LS";
    private bool metadataEnabled = true;
    private string cachedAudioState = "Stopped";
    private string metadataTitle = "";
    private string metadataArtist = "";
    private string metadataMediaName = "";
    private string lastMetadataAnnouncement = "";
    private bool suppressNextMetadataBanner = false;
    private readonly float[] visualizerBars = new float[15];
    private float visualizerLevel = 0.0f;
    private float visualizerRawPeak = 0.0f;
    private DateTime lastVisualizerUpdate = DateTime.MinValue;

    // TEST53: browser/media Core Audio enumeration is comparatively expensive.
    // Keep the visualizer at 45 ms, but sample external browser/Spotify peaks
    // much less often and reuse the latest value between samples.
    private float cachedExternalMediaPeak = 0.0f;
    private DateTime lastExternalMediaPeakQuery = DateTime.MinValue;
    private string cachedExternalMediaPeakSource = "";
    private int externalMediaPeakQueryInFlight = 0;
    private const int ExternalMediaPeakQueryMs = 180;

    // Internet Radio used the same expensive Core Audio session enumeration.
    // Cache that peak too; visualizer animation remains independent.
    private float cachedRadioMediaPeak = 0.0f;
    private DateTime lastRadioMediaPeakQuery = DateTime.MinValue;
    private int cachedRadioMediaPeakPid = 0;
    private const int RadioMediaPeakQueryMs = 180;
    private bool visualizerMeterAvailable = true;

    // v1.0.1 TEST24: audio-reactive vehicle neon. Only vehicles that already
    // have at least one neon side enabled are affected; the original colour
    // and enabled sides are restored when playback stops, the player exits,
    // enters Los Santos Customs, switches vehicle or the script shuts down.
    private bool audioReactiveNeonEnabled = true;
    // TEST38: user-adjustable beat-neon tuning.
    // Sensitivity: 1..10, VoiceFilter/PulseStrength/PulseSpeed: 0..100.
    // VoiceFilter is transient gating, not a real frequency/FFT vocal filter.
    private int neonBeatSensitivity = 6;
    private int neonVoiceFilter = 70;
    private int neonPulseStrength = 85;
    private int neonPulseSpeed = 60;
    private int neonTrackedVehicle = 0;
    private readonly bool[] neonOriginalEnabled = new bool[4];
    private int neonBaseR = 255;
    private int neonBaseG = 255;
    private int neonBaseB = 255;
    private bool neonCaptured = false;
    private bool neonCaptureChecked = false;
    private bool neonEffectApplied = false;
    // TEST33: stronger safe perceptual neon pulse. Installed neon stays enabled; only its
    // RGB colour is driven through a strong gamma curve. No underbody light renderer.
    private bool neonFlashOn = false;
    private float neonLastReactive = 0.0f;
    private float neonBeatBaseline = 0.0f;

    // TEST42: real low-frequency analysis is performed OUTSIDE ScriptHookVDotNet
    // in BassAnalyzerBridge.ps1. Only simple text values are read here.
    private float neonBassBaseline = 0.0f;
    private float neonBassPeak = 0.0001f;
    private float neonBassLast = 0.0f;
    private float neonBassDisplay = 0.0f;
    private float neonMidDisplay = 0.0f;

    private float neonDimLevel = 1.0f;
    private DateTime neonFlashUntil = DateTime.MinValue;
    private DateTime neonNextBeatAllowed = DateTime.MinValue;
    private DateTime lastNeonUpdate = DateTime.MinValue;

    // TEST27: audio-reactive cabin light. A small dynamic light is drawn inside
    // the current vehicle every frame by a lightweight companion Script class.
    // This does not modify the vehicle's permanent light settings.
    private bool audioReactiveInteriorLightEnabled = true;
    private int interiorLightStrength = 70;
    private float interiorLightLevel = 0.0f;
    private DateTime lastInteriorLightUpdate = DateTime.MinValue;

    internal static bool CabinLightRenderActive = false;
    internal static int CabinLightVehicleHandle = 0;
    internal static int CabinLightR = 255;
    internal static int CabinLightG = 255;
    internal static int CabinLightB = 255;
    internal static float CabinLightRange = 1.35f;
    internal static float CabinLightIntensity = 0.0f;

    // TEST54: positions are calculated on the controller tick, not every frame.
    internal static float CabinLightFrontX = 0.0f;
    internal static float CabinLightFrontY = 0.0f;
    internal static float CabinLightFrontZ = 0.0f;
    internal static float CabinLightRearX = 0.0f;
    internal static float CabinLightRearY = 0.0f;
    internal static float CabinLightRearZ = 0.0f;

    // v7.31: rear number-plate illumination. A tiny per-frame light is anchored to
    // the vehicle's platelight/numberplate bone. With active neon it uses the STATIC
    // base neon RGB and deliberately ignores Beat Neon. Without neon it follows the
    // vehicle's selected stock/xenon headlight colour family.
    private bool plateLightEnabled = true;
    internal static bool PlateLightRenderActive = false;
    internal static int PlateLightVehicleHandle = 0;
    internal static int PlateLightBoneIndex = -1;
    internal static int PlateLightR = 248;
    internal static int PlateLightG = 248;
    internal static int PlateLightB = 242;
    internal static float PlateLightRange = 1.05f;
    internal static float PlateLightIntensity = 1.10f;
    internal static float PlateLightFallbackY = -1.70f;
    internal static float PlateLightFallbackZ = 0.30f;

    // v7.41: stricter plate-light gate. GTA's GET_VEHICLE_LIGHTS_STATE can expose
    // daytime/automatic light activity that is not a reliable low-beam switch signal.
    // During daylight we therefore keep the plate lamp OFF by default and only latch it
    // after an actual VehicleHeadlight control press (or real high beam). At night the
    // native low/high-beam state is followed with debounce. This removes daylight flicker.
    private int plateLightVisibilityVehicleHandle = 0;
    private bool plateLightMainLightsLatched = false;
    private bool plateLightHeadlightControlWasDown = false;
    private bool plateLightDayManualIntent = false;
    private bool plateLightLastAmbientDark = false;
    private DateTime plateLightHeadlightInputSettleAt = DateTime.MinValue;
    private DateTime plateLightRawOnSince = DateTime.MinValue;
    private DateTime plateLightRawOffSince = DateTime.MinValue;

    // v7.37: Turbo audio is a simple ON/OFF switch and is focused on the useful part:
    // a single fuller SPORT-style blow-off on a real upshift or a genuine throttle lift
    // after sustained turbo load. There are no sustained-load/high-speed/airborne boost
    // pulses anymore, so holding speed or braking cannot spam four effects in a row.
    // High speed and being airborne do not disable the blow-off detector.
    private bool turboImpactEnabled = true;

    // v7.35: slot 18 only reports an AFTERMARKET toggle mod. A number of GTA vehicles
    // already use a turbocharged powertrain/audio setup in stock form, so relying on
    // IS_TOGGLE_MOD_ON(vehicle, 18) alone incorrectly excluded them. Keep a conservative
    // built-in stock-turbo model set and allow extra/add-on spawn names through the INI.
    // This does not install a turbo or change power; it only permits Turbo Impact audio.
    private readonly HashSet<int> factoryTurboModelHashes = new HashSet<int>();
    private string factoryTurboModelsConfigured = "";
    private int turboImpactLastDetectionVehicleHandle = 0;
    private string turboImpactLastDetectionKind = "";

    private int turboImpactVehicleHandle = 0;
    private int turboImpactLastGear = -1;
    private float turboImpactLastRpm = 0.0f;
    private float turboImpactLastThrottle = 0.0f;
    private bool turboImpactArmed = false;
    private DateTime turboImpactBoostSince = DateTime.MinValue;
    private DateTime turboImpactLastTriggerAt = DateTime.MinValue;
    private DateTime turboImpactLastUpdateAt = DateTime.MinValue;
    private bool turboImpactBoostPulseActive = false;
    private DateTime turboImpactBoostPulseUntil = DateTime.MinValue;
    private bool turboImpactLastInAir = false;
    private bool turboImpactLoadPulseUsed = false;

    private bool userSettingsDirty = false;
    private DateTime userSettingsSaveAt = DateTime.MinValue;

    public InternetRadioSimpleV120YouTube()
    {
        dir = GetDir();
        Directory.CreateDirectory(dir);
        logPath = Path.Combine(dir, "03_RADIO.log");
        Log("KONSTRUKTOR START v1.0.2 BETA TEST");
        Instance = this;
        for (int i = 0; i < visualizerBars.Length; i++) visualizerBars[i] = 0.08f;
        try
        {
            LoadIni();
            LoadGtaRadioStations();
            EnsureUserSettingsFile();
            // TEST69: remove stale runtime artwork from an unclean previous session.
            CleanupMediaArtworkCache("STARTUP");
            LoadUiIconSprites();
            PrimeGtaRadioHudTextureDictionary();
            audio = new AudioWorker(logPath);
            youtube = new YouTubeMusicBridge(logPath, autoLaunchYouTubeMusic, autoPlayYouTubeMusic, mediaArtworkEnabled);
            spotify = new SpotifyMusicBridge(logPath, mediaArtworkEnabled);

            // Lazy: helper only starts later when Beat Neon is actually active with music.
            bassAnalyzer = new BassAnalyzerBridge(logPath);
            // TEST33: stable 60 ms controller tick keeps the pulse responsive without extra renderer load.
            // samples without returning to the unsafe per-frame lighting experiment.
            // Expensive vehicle/ducking logic remains independently throttled to ~100 ms.
            Interval = 60;
            Tick += OnTick;
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
            Aborted += OnAbort;
            menuIndex = index;
            // v1.0.1: No startup notification/banner.
            Log("Initialisierung komplett | InternetSender=" + stations.Count + " | GTA-Sender=" + gtaRadioStations.Count);
        }
        catch (Exception ex)
        {
            Log("INIT FEHLER: " + ex.ToString());
        }
    }

    private bool IsPublicReleaseOnlineSessionActive()
    {
        DateTime now = DateTime.Now;
        if ((now - publicReleaseLastOnlineCheck).TotalMilliseconds < 1000.0)
            return publicReleaseOnlineGuardActive;

        publicReleaseLastOnlineCheck = now;
        bool active = false;
        try { active = Function.Call<bool>((Hash)0xD83C2B94E7508980UL); } catch { active = false; }
        publicReleaseOnlineGuardActive = active;
        return active;
    }

    private bool UpdateGameplayInteractionReadiness()
    {
        bool baseReady = true;

        try
        {
            if (Function.Call<bool>(Hash.IS_SCREEN_FADED_OUT) ||
                Function.Call<bool>(Hash.IS_SCREEN_FADING_OUT) ||
                Function.Call<bool>(Hash.IS_SCREEN_FADING_IN))
                baseReady = false;
        }
        catch { baseReady = false; }

        try
        {
            Ped player = Game.LocalPlayerPed;
            if (player == null || player.Handle == 0) baseReady = false;
        }
        catch { baseReady = false; }

        if (!baseReady)
        {
            if (menuSlowMotionApplied) restoreTimeScaleWhenGameplayReady = true;
            menuSlowMotionApplied = false;
            gameplayInteractionReady = false;
            gameplayReadyCandidateSince = DateTime.MinValue;
            SafeRenderState = false;

            // Pure managed-state cleanup only. Do not touch entity natives while GTA is
            // transitioning between loading/player states.
            menuOpen = false;
            menuCloseRequested = false;
            menuCloseHandledOnPress = false;
            menuHoldActive = false;
            menuHoldKey = Keys.None;
            volumeHoldActive = false;
            volumeHoldKey = Keys.None;
            drawAlphaMultiplier = 1.0f;

            cachedRadioCapableVehicle = false;
            cachedAudioReactiveLightingVehicle = false;
            cachedRoadSpeedometerVehicle = false;
            cachedBoatHudVehicle = false;
            cachedAircraftHudVehicle = false;
            cachedVehicleHandle = 0;
            cachedVehicleClassId = -1;
            playbackVehicleHandle = 0;
            radioResumeAfterVehicle = false;

            CabinLightRenderActive = false;
            CabinLightVehicleHandle = 0;
            ClearVehiclePlateLight();
            instrumentMotionSampleValid = false;
            instrumentMotionVehicleHandle = 0;
            return false;
        }

        DateTime now = DateTime.UtcNow;
        if (!gameplayInteractionReady)
        {
            if (gameplayReadyCandidateSince == DateTime.MinValue)
            {
                gameplayReadyCandidateSince = now;
                SafeRenderState = false;
                return false;
            }

            // Let GTA settle for a short moment after the ped/screen become valid.
            // This prevents button mashing on the last loading frames from entering
            // menu/vehicle native code while the world is still being swapped.
            if ((now - gameplayReadyCandidateSince).TotalMilliseconds < 1250.0)
            {
                SafeRenderState = false;
                return false;
            }

            gameplayInteractionReady = true;
            SafeRenderState = true;

            if (restoreTimeScaleWhenGameplayReady)
            {
                try { Function.Call(Hash.SET_TIME_SCALE, 1.0f); } catch { }
                restoreTimeScaleWhenGameplayReady = false;
            }

            Log("GAMEPLAY READY | loading/input safety released");
        }
        else
        {
            SafeRenderState = true;
        }

        return true;
    }

    private bool EnforcePublicReleaseSinglePlayerGuard()
    {
        bool online = IsPublicReleaseOnlineSessionActive();
        if (!online)
        {
            if (publicReleaseWasOnline)
            {
                publicReleaseWasOnline = false;
                Log("TEST71 SINGLEPLAYER GUARD | Online session ended; mod remains user-controlled and does not auto-resume media");
            }
            return false;
        }

        if (!publicReleaseWasOnline)
        {
            publicReleaseWasOnline = true;
            try { RestoreGameTimeScale(); } catch { }
            try { RestoreAudioReactiveNeon(true); } catch { }
            try { ClearAudioReactiveInteriorLight(); } catch { }
            try { ClearVehiclePlateLight(); } catch { }
            try { ResetTurboImpactState(true); } catch { }
            try { ResetAutomaticVehicleIndicators(true); } catch { }
            try { ResetManualVehicleHighBeam(true); } catch { }
            try { if (shouldPlay) StopRadio(); } catch { }
            try { if (youtubeModeActive && youtube != null) youtube.Pause(); } catch { }
            try { if (spotifyModeActive && spotify != null) spotify.Pause(); } catch { }
            try { if (gtaRadioModeActive) Game.RadioStation = RadioStation.RadioOff; } catch { }
            ResetGtaRadioTrackMetadata();
            gtaRadioModeActive = false;
            gtaRadioPlayingRequested = false;
            menuOpen = false;
            Log("TEST71 SINGLEPLAYER GUARD | GTA Online/network session detected; mod UI/audio/vehicle effects disabled");
        }
        return true;
    }

    private void OnTick(object sender, EventArgs e)
    {
        try
        {
            // GTA-Pause-/Hauptmenue: Stream sofort stoppen und keinerlei Radio-UI zeichnen.
            bool pauseMenuActive = false;
            try { pauseMenuActive = Function.Call<bool>(Hash.IS_PAUSE_MENU_ACTIVE); } catch { }
            cachedPauseMenuActive = pauseMenuActive;

            if (pauseMenuActive)
            {
                // Never leave GTA slowed when the real pause/main menu takes over.
                RestoreGameTimeScale();
                // Restore the vehicle's normal neon colour while playback is paused.
                RestoreAudioReactiveNeon(false);
                ClearAudioReactiveInteriorLight();
                ClearVehiclePlateLight();
                ResetTurboImpactState(true);
                if (!wasPauseMenuActive)
                {
                    resumeAfterPause = !IsExternalMusicModeActive() && enabled && shouldPlay;
                    youtubeResumeAfterPause = youtubeModeActive && IsYouTubePlaying();
                    spotifyResumeAfterPause = spotifyModeActive && IsSpotifyPlaying();
                    if (shouldPlay) StopRadio();
                    if (youtubeResumeAfterPause && youtube != null) youtube.Pause();
                    if (spotifyResumeAfterPause && spotify != null) spotify.Pause();
                    menuOpen = false;
                    Log("PAUSE MENU OPEN | RadioResume=" + resumeAfterPause + " | YouTubeResume=" + youtubeResumeAfterPause + " | SpotifyResume=" + spotifyResumeAfterPause);
                }
                wasPauseMenuActive = true;
                return;
            }

            if (!UpdateGameplayInteractionReadiness()) return;

            if (EnforcePublicReleaseSinglePlayerGuard()) return;

            // Finish menu-close cleanup on the main Tick. The KeyDown handler already
            // clears menuOpen immediately so the visual layer disappears without delay.
            if (menuCloseRequested)
            {
                menuCloseRequested = false;
                menuOpen = false;
                menuHoldActive = false;
                menuHoldKey = Keys.None;
                drawAlphaMultiplier = 1.0f;
                RestoreGameTimeScale();
                MarkUserSettingsDirty();
            }

            UpdateMenuSlowMotion();
            UpdateVolumeHold();
            UpdateMenuHold();
            FlushUserSettingsIfDue();
            if (menuOpen) SuppressConflictingGtaUiControls();

            bool justReturnedFromPause = wasPauseMenuActive;
            if (justReturnedFromPause)
            {
                wasPauseMenuActive = false;
                Log("PAUSE MENU CLOSED");
            }

            // SAFE: Teure Fahrzeug-/Ducking-Logik bleibt auf ca. 100 ms gedrosselt.
            DateTime now = DateTime.Now;
            if ((now - lastLogicTick).TotalMilliseconds >= 100 || justReturnedFromPause)
            {
                lastLogicTick = now;
                bool playerDead = IsPlayerDeadOrDying();
                bool inVehicle = !playerDead && IsRadioCapableVehicle();

                cachedPlayerDead = playerDead;
                cachedRadioCapableVehicle = inVehicle;
                cachedVehicleHandle = 0;
                cachedAudioReactiveLightingVehicle = false;
                cachedRoadSpeedometerVehicle = false;
                cachedBoatHudVehicle = false;
                cachedAircraftHudVehicle = false;
                cachedVehicleClassId = -1;
                cachedInLosSantosCustoms = false;

                if (inVehicle)
                {
                    try
                    {
                        Ped cachedPlayer = Game.LocalPlayerPed;
                        if (cachedPlayer != null)
                        {
                            cachedVehicleHandle = Function.Call<int>(
                                Hash.GET_VEHICLE_PED_IS_IN,
                                cachedPlayer.Handle,
                                false);

                            if (cachedVehicleHandle != 0)
                            {
                                cachedVehicleClassId = Function.Call<int>(
                                    Hash.GET_VEHICLE_CLASS,
                                    cachedVehicleHandle);

                                // Vehicle instruments are deliberately class-specific:
                                // road HUD = cars/trucks/vans/service/emergency/military/open-wheel + motorcycles,
                                // marine HUD = boats, flight HUD = helicopters/planes. Cycles and trains stay native-only.
                                cachedRoadSpeedometerVehicle =
                                    cachedVehicleClassId != 13 &&
                                    cachedVehicleClassId != 14 &&
                                    cachedVehicleClassId != 15 &&
                                    cachedVehicleClassId != 16 &&
                                    cachedVehicleClassId != 21;
                                cachedBoatHudVehicle = cachedVehicleClassId == 14;
                                cachedAircraftHudVehicle = cachedVehicleClassId == 15 || cachedVehicleClassId == 16;
                                cachedAudioReactiveLightingVehicle = cachedRoadSpeedometerVehicle;
                            }
                        }
                    }
                    catch
                    {
                        cachedVehicleHandle = 0;
                        cachedAudioReactiveLightingVehicle = false;
                        cachedRoadSpeedometerVehicle = false;
                        cachedBoatHudVehicle = false;
                        cachedAircraftHudVehicle = false;
                        cachedVehicleClassId = -1;
                    }

                    try { cachedInLosSantosCustoms = IsInLosSantosCustoms(); }
                    catch { cachedInLosSantosCustoms = false; }
                }

                // v7.28: distinguish a real leave/enter from a direct vehicle replacement.
                // A handle change while both ticks are "in vehicle" still counts as a fresh
                // vehicle entry for source restoration (especially native GTA Radio).
                int previousPlaybackVehicleHandle = playbackVehicleHandle;
                bool vehicleChangedDirectly =
                    inVehicle &&
                    wasInVehicle &&
                    previousPlaybackVehicleHandle != 0 &&
                    cachedVehicleHandle != 0 &&
                    previousPlaybackVehicleHandle != cachedVehicleHandle;
                bool leftRadioVehicle = !inVehicle && wasInVehicle;
                bool enteredRadioVehicle = inVehicle && (!wasInVehicle || vehicleChangedDirectly);

                if (vehicleChangedDirectly)
                {
                    if (youtubeModeActive) preferredVehicleSource = "YOUTUBE";
                    else if (spotifyModeActive) preferredVehicleSource = "SPOTIFY";
                    else if (gtaRadioModeActive) preferredVehicleSource = "GTA";
                    else preferredVehicleSource = "RADIO";

                    Log("VEHICLE SWITCH | Old=" + previousPlaybackVehicleHandle +
                        " | New=" + cachedVehicleHandle +
                        " | PreferredSource=" + preferredVehicleSource);
                }

                // TEST22: death/respawn must not silently switch an external source back
                // to Internet Radio. Remember the active source, pause it while the player
                // is dead, and force the next supported vehicle entry to restore it.
                if (playerDead)
                {
                    if (!wasPlayerDead)
                    {
                        if (youtubeModeActive)
                        {
                            preferredVehicleSource = "YOUTUBE";
                            youtubeResumeAfterVehicle = IsYouTubePlaying();
                            youtubeAutoPlayPending = false;
                            if (youtube != null) youtube.Pause();
                        }
                        else if (spotifyModeActive)
                        {
                            preferredVehicleSource = "SPOTIFY";
                            spotifyResumeAfterVehicle = IsSpotifyPlaying();
                            spotifyAutoPlayPending = false;
                            if (spotify != null) spotify.Pause();
                        }
                        else if (gtaRadioModeActive)
                        {
                            preferredVehicleSource = "GTA";
                            try { Game.RadioStation = RadioStation.RadioOff; } catch { }
                            ResetGtaRadioTrackMetadata();
                        }
                        else if (shouldPlay)
                        {
                            preferredVehicleSource = "RADIO";
                            radioResumeAfterVehicle = true;
                        }

                        if (shouldPlay) StopRadio();
                        menuOpen = false;
                        RestoreGameTimeScale();
                        miniUiVisibleUntil = DateTime.MinValue;
                        youtubeMiniHideAt = DateTime.MinValue;
                        spotifyMiniHideAt = DateTime.MinValue;
                        volumeHudVisibleUntil = DateTime.MinValue;
                        Log("PLAYER DEATH | PreferredSource=" + preferredVehicleSource);
                    }

                    wasPlayerDead = true;
                    ClearAudioReactiveInteriorLight();
                    ClearVehiclePlateLight();
                    ResetTurboImpactState(true);
                    // The respawned Ped is a new vehicle-state transition. Reset this now
                    // so entering the next car is always detected as a fresh entry.
                    wasInVehicle = false;
                    ResetAutomaticVehicleIndicators(true);
                    ResetManualVehicleHighBeam(true);
                    return;
                }

                if (wasPlayerDead)
                {
                    wasPlayerDead = false;
                    wasInVehicle = false;
                    Log("PLAYER RESPAWN | PreferredSource=" + preferredVehicleSource);
                }

                // If another game-state transition cleared the active mode while the player
                // was dead, rebuild it from the remembered source before AutoStart can start
                // Internet Radio in the next vehicle. This never opens a new app/tab.
                if (enteredRadioVehicle && !youtubeModeActive && !spotifyModeActive && !gtaRadioModeActive)
                {
                    if (preferredVehicleSource == "GTA" && gtaRadioStations.Count > 0)
                    {
                        StopRadio();
                        gtaRadioModeActive = true;
                        gtaRadioMenuIndex = Math.Max(0, Math.Min(gtaRadioStations.Count - 1, gtaRadioIndex));
                        if (gtaRadioPlayingRequested)
                        {
                            ApplySelectedGtaRadioStation(false);
                            UpdateGtaRadioTrackMetadata(true);
                        }
                        gtaRadioMiniHideAt = DateTime.Now.AddMilliseconds(4200);
                        miniUiFadeUpdatedAt = DateTime.MinValue;
                        Log("GTA RADIO SOURCE RESTORED ON VEHICLE ENTRY | Station=" + CurrentGtaRadioStationName());
                    }
                    else if (preferredVehicleSource == "YOUTUBE" && youtube != null)
                    {
                        StopRadio();
                        Game.RadioStation = RadioStation.RadioOff;
                        youtubeModeActive = true;
                        spotifyModeActive = false;
                        youtube.SetManagedActive(true);
                        youtube.BeginDetection();
                        youtubeMiniHideAt = DateTime.Now.AddMilliseconds(4200);
                        miniUiFadeUpdatedAt = DateTime.MinValue;

                        bool resumeYoutube = youtubeResumeAfterVehicle;
                        if (resumeYoutube && youtubeAvailable)
                        {
                            youtube.Play();
                            youtubePlaybackCommandGraceUntil = DateTime.Now.AddMilliseconds(1400);
                            youtubeAutoPlayPending = false;
                        }
                        else
                        {
                            youtubeAutoPlayPending = resumeYoutube;
                        }
                        youtubeResumeAfterVehicle = false;

                        Log("YT SOURCE RESTORED ON VEHICLE ENTRY | Available=" + youtubeAvailable + " | Resume=" + resumeYoutube);
                    }
                    else if (preferredVehicleSource == "SPOTIFY" && spotify != null)
                    {
                        StopRadio();
                        Game.RadioStation = RadioStation.RadioOff;
                        spotifyModeActive = true;
                        youtubeModeActive = false;
                        spotify.SetManagedActive(true);
                        spotify.BeginDetection();
                        spotifyMiniHideAt = DateTime.Now.AddMilliseconds(4200);
                        miniUiFadeUpdatedAt = DateTime.MinValue;

                        bool resumeSpotify = spotifyResumeAfterVehicle;
                        if (resumeSpotify && spotifyAvailable)
                        {
                            spotify.Play();
                            spotifyPlaybackCommandGraceUntil = DateTime.Now.AddMilliseconds(1400);
                            spotifyAutoPlayPending = false;
                        }
                        else
                        {
                            spotifyAutoPlayPending = resumeSpotify;
                        }
                        spotifyResumeAfterVehicle = false;

                        Log("SPOTIFY SOURCE RESTORED ON VEHICLE ENTRY | Available=" + spotifyAvailable + " | Resume=" + resumeSpotify);
                    }
                    else if (preferredVehicleSource == "RADIO" && radioResumeAfterVehicle && enabled)
                    {
                        StartRadio(false);
                        radioResumeAfterVehicle = false;
                        Log("INTERNET RADIO SOURCE RESTORED ON VEHICLE ENTRY | Station=" + CurrentStationLine());
                    }
                }

                // TEST20: a Num5 request is only valid while the player remains in a
                // radio-capable vehicle. If the player gets out before Windows exposes
                // the media session, cancel the pending activation instead of letting it
                // become active later while on foot/on a bicycle.
                if (!inVehicle)
                {
                    if (youtubeSelectionPending)
                    {
                        youtubeSelectionPending = false;
                        youtubeAutoPlayPending = false;
                        Log("YT PENDING CANCELLED | left radio-capable vehicle before detection");
                    }
                    if (spotifySelectionPending)
                    {
                        spotifySelectionPending = false;
                        Log("SPOTIFY PENDING CANCELLED | left radio-capable vehicle before detection");
                    }
                }

                // V12.24: YouTube Music is fully on-demand. No automatic source switch on vehicle entry.

                if (justReturnedFromPause)
                {
                    if (youtubeModeActive)
                    {
                        if (youtubeResumeAfterPause && inVehicle && youtube != null) youtube.Play();
                        youtubeResumeAfterPause = false;
                    }
                    else if (spotifyModeActive)
                    {
                        if (spotifyResumeAfterPause && inVehicle && spotify != null) spotify.Play();
                        spotifyResumeAfterPause = false;
                    }
                    else if (resumeAfterPause && enabled && inVehicle)
                    {
                        StartRadio();
                        Log("PAUSE RESUME | Radio wieder gestartet");
                    }
                    resumeAfterPause = false;
                }
                else if (youtubeModeActive)
                {
                    if (leftRadioVehicle)
                    {
                        youtubeResumeAfterVehicle = IsYouTubePlaying();
                        if (youtubeResumeAfterVehicle && youtube != null) youtube.Pause();
                    }
                    else if (enteredRadioVehicle)
                    {
                        // TEST19: whenever the player gets back into a radio-capable vehicle,
                        // show the YT mini UI again. Resetting the cover timer makes it visible
                        // immediately; once the cover is drawn the normal 2.5s countdown starts.
                        youtubeMiniHideAt = DateTime.Now.AddMilliseconds(4200);
                        miniUiFadeUpdatedAt = DateTime.MinValue;
                        if (youtubeResumeAfterVehicle && youtube != null)
                        {
                            if (youtubeAvailable) { youtube.Play(); youtubePlaybackCommandGraceUntil = DateTime.Now.AddMilliseconds(1400); }
                            else
                            {
                                youtube.BeginDetection();
                                youtubeAutoPlayPending = true;
                            }
                        }
                        youtubeResumeAfterVehicle = false;
                        Log("YT VEHICLE ENTER | Mini UI rearmed | PreferredSource=" + preferredVehicleSource);
                    }
                }
                else if (spotifyModeActive)
                {
                    if (leftRadioVehicle)
                    {
                        spotifyResumeAfterVehicle = IsSpotifyPlaying();
                        if (spotifyResumeAfterVehicle && spotify != null) spotify.Pause();
                    }
                    else if (enteredRadioVehicle)
                    {
                        // TEST19: same re-entry behavior for Spotify.
                        spotifyMiniHideAt = DateTime.Now.AddMilliseconds(4200);
                        miniUiFadeUpdatedAt = DateTime.MinValue;
                        if (spotifyResumeAfterVehicle && spotify != null)
                        {
                            if (spotifyAvailable) { spotify.Play(); spotifyPlaybackCommandGraceUntil = DateTime.Now.AddMilliseconds(1400); }
                            else
                            {
                                spotify.BeginDetection();
                                spotifyAutoPlayPending = true;
                            }
                        }
                        spotifyResumeAfterVehicle = false;
                        Log("SPOTIFY VEHICLE ENTER | Mini UI rearmed | PreferredSource=" + preferredVehicleSource);
                    }
                }
                else if (gtaRadioModeActive)
                {
                    if (leftRadioVehicle)
                    {
                        try { Game.RadioStation = RadioStation.RadioOff; } catch { }
                        ResetGtaRadioTrackMetadata();
                    }
                    else if (enteredRadioVehicle)
                    {
                        if (gtaRadioPlayingRequested)
                        {
                            ApplySelectedGtaRadioStation(false);
                            UpdateGtaRadioTrackMetadata(true);
                        }
                        gtaRadioMiniHideAt = DateTime.Now.AddMilliseconds(4200);
                        miniUiFadeUpdatedAt = DateTime.MinValue;
                        Log("GTA RADIO VEHICLE ENTER | Station=" + CurrentGtaRadioStationName());
                    }
                }
                else
                {
                    if (leftRadioVehicle)
                    {
                        preferredVehicleSource = "RADIO";
                        radioResumeAfterVehicle = shouldPlay;
                        if (shouldPlay) StopRadio();
                        Log("INTERNET RADIO VEHICLE LEAVE | Resume=" + radioResumeAfterVehicle +
                            " | Station=" + CurrentStationLine());
                    }
                    else if (enteredRadioVehicle)
                    {
                        // Direct car-to-car switches keep the current stream alive. A normal
                        // leave/re-entry resumes the exact station even when AutoStart is off.
                        if (!vehicleChangedDirectly)
                        {
                            if (radioResumeAfterVehicle && enabled)
                            {
                                StartRadio(false);
                                Log("INTERNET RADIO VEHICLE RESUME | Station=" + CurrentStationLine());
                            }
                            else if (!shouldPlay && enabled && autoStart)
                            {
                                StartRadio(false);
                                Log("INTERNET RADIO VEHICLE AUTOSTART | Station=" + CurrentStationLine());
                            }
                        }
                        else if (shouldPlay && showMiniUi)
                        {
                            miniUiVisibleUntil = DateTime.Now.AddMilliseconds(4200);
                        }
                        radioResumeAfterVehicle = false;
                    }
                }

                wasInVehicle = inVehicle;
                playbackVehicleHandle = inVehicle ? cachedVehicleHandle : 0;

                if (gtaRadioModeActive && inVehicle)
                {
                    SyncGtaRadioStationFromGame();
                    UpdateGtaRadioTrackMetadata(false);
                }

                if (IsExternalMusicModeActive() && inVehicle)
                {
                    // External music owns the audio slot exclusively: neither GTA radio nor web radio runs in parallel.
                    if (shouldPlay) StopRadio();
                    Game.RadioStation = RadioStation.RadioOff;
                    UpdateSpeechDucking();
                }
                else if (gtaRadioModeActive && inVehicle)
                {
                    // Native GTA Radio owns its complete audio mix. The mod deliberately does
                    // not change GTA Radio volume or ducking; use GTA Settings > Audio instead.
                    if (shouldPlay) StopRadio();
                    if (!gtaRadioPlayingRequested)
                    {
                        try { Game.RadioStation = RadioStation.RadioOff; } catch { }
                    }
                    if (duckActive)
                    {
                        duckActive = false;
                        duckReason = "";
                        duckMix = 0.0f;
                        lastDuckRampAt = DateTime.Now;
                        appliedStreamVolume = -1;
                        appliedYoutubeVolume = -1;
                        appliedYoutubeMixerLevel = -1.0f;
                        appliedSpotifyVolume = -1;
                        appliedSpotifyMixerLevel = -1.0f;
                    }
                }
                else if (enabled && shouldPlay && inVehicle)
                {
                    Game.RadioStation = RadioStation.RadioOff;
                    UpdateSpeechDucking();
                }
                else if (duckActive)
                {
                    duckActive = false;
                    duckReason = "";
                    duckMix = 0.0f;
                    lastDuckRampAt = DateTime.Now;
                    appliedStreamVolume = -1;
                    appliedYoutubeVolume = -1;
                    appliedYoutubeMixerLevel = -1.0f;
                    appliedSpotifyVolume = -1;
                    appliedSpotifyMixerLevel = -1.0f;
                    ApplyEffectiveVolume();
                }
            }

            // Status/Metadaten werden nur hier aktualisiert, nicht im Frame-Renderer.
            if (audio != null)
            {
                cachedAudioState = audio.StateText;
                if (metadataEnabled)
                {
                    metadataTitle = audio.Title;
                    metadataArtist = audio.Artist;
                    metadataMediaName = audio.MediaName;
                    string metaNow = GetNowPlayingLine();
                    if (shouldPlay && !string.IsNullOrEmpty(metaNow) && !string.Equals(metaNow, lastMetadataAnnouncement, StringComparison.Ordinal))
                    {
                        lastMetadataAnnouncement = metaNow;
                        if (suppressNextMetadataBanner)
                        {
                            suppressNextMetadataBanner = false;
                            Log("METADATA BANNER SUPPRESSED | AUTO START | " + metaNow);
                        }
                        else
                        {
                            ShowBanner(CurrentStationLine(), Shorten(metaNow, 58), 3500);
                            Log("METADATA | " + metaNow);
                        }
                    }
                }
            }

            CheckRadioStreamHealth();

            if (youtube != null)
            {
                youtubeAvailable = youtube.Available;
                youtubeState = youtube.StateText;
                youtubeTitle = youtube.Title;
                youtubeArtist = youtube.Artist;
                youtubeAlbum = youtube.Album;
                youtubeSource = youtube.Source;
                youtubeCoverPath = youtube.CoverPath;

                // TEST15: defensive isolation. A Spotify GSMTC source must never be rendered
                // as YouTube Music, even if Windows briefly serves stale media-session data.
                if (!string.IsNullOrEmpty(youtubeSource) && youtubeSource.IndexOf("spotify", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    youtubeAvailable = false;
                    youtubeState = "Not connected";
                    youtubeTitle = "";
                    youtubeArtist = "";
                    youtubeAlbum = "";
                    youtubeCoverPath = "";
                    Log("YT SESSION REJECTED | Spotify source leaked into YT bridge | " + youtubeSource);
                }
                UpdateYouTubeCoverSprite();
                if (youtubeSelectionPending && IsRadioCapableVehicle() && youtubeAvailable && IsYouTubePlaying())
                {
                    youtubeSelectionPending = false;
                    ActivateYouTubeMode();
                    ShowBanner("YouTube Music", T("YT_DETECTED"), 2200);
                    Log("YT USER SELECTION | Playing browser media session detected, source activated | " + youtubeSource + " | " + youtubeTitle);
                }
                if (youtubeModeActive && youtubeAutoPlayPending && IsRadioCapableVehicle() && youtubeAvailable)
                {
                    youtube.Play();
                    youtubePlaybackCommandGraceUntil = DateTime.Now.AddMilliseconds(1400);
                    youtubeAutoPlayPending = false;
                    Log("YT AUTO PLAY | Media-Session bereit");
                }
                if (youtubeModeActive && youtubeAvailable) ApplyEffectiveVolume();
            }

            if (spotify != null)
            {
                spotifyAvailable = spotify.Available;
                spotifyState = spotify.StateText;
                spotifyTitle = spotify.Title;
                spotifyArtist = spotify.Artist;
                spotifyAlbum = spotify.Album;
                spotifySource = spotify.Source;
                spotifyCoverPath = spotify.CoverPath;
                UpdateSpotifyCoverSprite();
                if (spotifySelectionPending && IsRadioCapableVehicle() && spotifyAvailable && IsSpotifyPlaying())
                {
                    spotifySelectionPending = false;
                    ActivateSpotifyMode();
                    ShowBanner("Spotify", T("SPOTIFY_DETECTED"), 2200);
                    Log("SPOTIFY USER SELECTION | Playing media session detected, source activated | " + spotifySource + " | " + spotifyTitle);
                }
                if (spotifyModeActive && spotifyAutoPlayPending && IsRadioCapableVehicle() && spotifyAvailable)
                {
                    spotify.Play();
                    spotifyPlaybackCommandGraceUntil = DateTime.Now.AddMilliseconds(1400);
                    spotifyAutoPlayPending = false;
                    Log("SPOTIFY AUTO PLAY | media session ready after vehicle restore");
                }
                if (spotifyModeActive && spotifyAvailable) ApplyEffectiveVolume();
            }

            // v7.31: also learn play/pause changes made directly in Spotify/YouTube.
            CapturePersistentExternalPlaybackState();

            UpdateAutomaticVehicleIndicators();
            UpdateManualVehicleHighBeam();
            UpdateAudioVisualizer();
            UpdateAudioReactiveNeon();
            UpdateVehiclePlateLight();
            UpdateTurboImpact();
            UpdateAudioReactiveInteriorLight();
        }
        catch (Exception ex)
        {
            Log("TICK FEHLER: " + ex.Message);
        }
    }

    private void PlayMenuOpenSound()
    {
        if (!menuOpenSoundEnabled) return;
        try
        {
            // Native GTA frontend sound: no extra WAV/MP3 file is required.
            Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET", true);
            Log("MENU OPEN SOUND | SELECT | HUD_FRONTEND_DEFAULT_SOUNDSET");
        }
        catch (Exception ex)
        {
            Log("MENU OPEN SOUND FEHLER | " + ex.Message);
        }
    }

    private void UpdateMenuSlowMotion()
    {
        try
        {
            if (menuOpen && menuSlowMotionEnabled)
            {
                if (!menuSlowMotionApplied)
                {
                    float scale = Math.Max(0.20f, Math.Min(1.00f, menuTimeScale));
                    Function.Call(Hash.SET_TIME_SCALE, scale);
                    menuSlowMotionApplied = true;
                    Log("MENU SLOW MOTION ON | TimeScale=" + scale.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
                }
            }
            else
            {
                RestoreGameTimeScale();
            }
        }
        catch (Exception ex)
        {
            if (menuSlowMotionApplied) Log("MENU SLOW MOTION FEHLER: " + ex.Message);
            menuSlowMotionApplied = false;
        }
    }

    private void RestoreGameTimeScale()
    {
        // TEST54: do not call SET_TIME_SCALE every controller tick.
        if (!menuSlowMotionApplied) return;

        try { Function.Call(Hash.SET_TIME_SCALE, 1.0f); }
        catch { }

        menuSlowMotionApplied = false;
        Log("MENU SLOW MOTION OFF | TimeScale=1.00");
    }

    private bool IsMenuToggleKey(Keys key)
    {
        return key == menuKey;
    }

    private bool IsMenuUpKey(Keys key)
    {
        return key == menuUpKey;
    }

    private bool IsMenuDownKey(Keys key)
    {
        return key == menuDownKey;
    }

    private bool IsPreviousKey(Keys key)
    {
        return key == prevKey;
    }

    private bool IsNextKey(Keys key)
    {
        return key == nextKey;
    }

    private bool IsSelectKey(Keys key)
    {
        return key == selectKey;
    }

    private bool IsToggleKey(Keys key)
    {
        return key == toggleKey;
    }

    private bool IsStopKey(Keys key)
    {
        return key == stopKey;
    }

    private bool IsVolumeDownKey(Keys key)
    {
        return key == volumeDownKey;
    }

    private bool IsVolumeUpKey(Keys key)
    {
        return key == volumeUpKey;
    }

    private bool IsHazardKey(Keys key)
    {
        return key == hazardKey;
    }

    private bool IsHighBeamKey(Keys key)
    {
        return key == highBeamKey;
    }

    private int GetCarSettingId(int menuPosition)
    {
        if (menuPosition < 0 || menuPosition >= CarSettingOrder.Length) return 0;
        return CarSettingOrder[menuPosition];
    }

    private int GetCarSettingGroup(int menuPosition)
    {
        if (menuPosition < 9) return 0;   // lighting
        if (menuPosition < 10) return 1;  // audio
        if (menuPosition < 18) return 2;  // speedometer
        return 3;                         // boat/flight HUD
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        try
        {
            if (!gameplayInteractionReady || !SafeRenderState) return;
            if (EnforcePublicReleaseSinglePlayerGuard()) return;
            try { if (Function.Call<bool>(Hash.IS_PAUSE_MENU_ACTIVE)) return; } catch { }

            if (IsMenuToggleKey(e.KeyCode) && menuOpen)
            {
                if (!menuCloseHandledOnPress)
                {
                    // Hide the menu immediately so START/home icons cannot linger until
                    // the 60 ms logic Tick. Native/time-scale/file cleanup is deferred.
                    menuOpen = false;
                    menuHoldActive = false;
                    menuHoldKey = Keys.None;
                    menuCloseHandledOnPress = true;
                    menuCloseRequested = true;
                }
                return;
            }

            // Smooth menu repeat: react immediately on press, then repeat while held.
            // YouTube track previous/next on Num8/Num2 is intentionally excluded
            // from hold-repeat to avoid skipping several tracks by accident.
            if (menuOpen && IsMenuRepeatKey(e.KeyCode))
            {
                if (menuHoldActive && menuHoldKey == e.KeyCode) return;
                menuHoldActive = true;
                menuHoldKey = e.KeyCode;
                menuHoldStartedAt = DateTime.Now;
                menuHoldNext = DateTime.Now.AddMilliseconds(280);
                HandleMenuKeys(e.KeyCode);
                return;
            }

            if (!IsVolumeDownKey(e.KeyCode) && !IsVolumeUpKey(e.KeyCode)) return;
            // Native GTA Radio volume is controlled only in GTA Settings > Audio.
            // Keep Num-/Num+ available for CAR/SETTINGS value editing, but never
            // start the mod volume hold/repeat while GTA Radio itself is active.
            if (gtaRadioModeActive && !(menuOpen && (menuTab == 7 || menuTab == 8))) return;
            if (volumeHoldActive && volumeHoldKey == e.KeyCode) return;

            volumeHoldActive = true;
            volumeHoldKey = e.KeyCode;
            volumeHoldStartedAt = DateTime.Now;
            volumeHoldNext = DateTime.Now.AddMilliseconds(320);
            HandleVolumeStep(IsVolumeDownKey(e.KeyCode) ? -1 : 1);
        }
        catch (Exception ex)
        {
            Log("KEYDOWN FEHLER: " + ex.Message);
        }
    }

    private void UpdateVolumeHold()
    {
        if (!volumeHoldActive || volumeHoldKey == Keys.None) return;
        bool isDown = false;
        try { isDown = (GetAsyncKeyState((int)volumeHoldKey) & 0x8000) != 0; } catch { }
        if (!isDown)
        {
            volumeHoldActive = false;
            volumeHoldKey = Keys.None;
            return;
        }
        if (DateTime.Now < volumeHoldNext) return;
        HandleVolumeStep(IsVolumeDownKey(volumeHoldKey) ? -1 : 1);
        double heldMs = (DateTime.Now - volumeHoldStartedAt).TotalMilliseconds;
        int repeatMs = heldMs >= 2600 ? 45 : (heldMs >= 1600 ? 60 : (heldMs >= 800 ? 85 : 120));
        volumeHoldNext = DateTime.Now.AddMilliseconds(repeatMs);
    }

    private void HandleVolumeStep(int direction)
    {
        if (menuOpen && menuTab == 7) AdjustSelectedCarSetting(direction);
        else if (menuOpen && menuTab == 8) AdjustSelectedSetting(direction);
        else AdjustVolume(direction);
    }

    private bool IsMenuRepeatKey(Keys key)
    {
        if (IsPreviousKey(key) || IsNextKey(key)) return true;
        if (IsMenuUpKey(key) || IsMenuDownKey(key))
        {
            // In YouTube Music and Spotify tabs up/down control previous/next track.
            // Keep those as deliberate single presses.
            return menuTab != 5 && menuTab != 6;
        }
        return false;
    }

    private void UpdateMenuHold()
    {
        if (!menuHoldActive || menuHoldKey == Keys.None) return;
        if (!menuOpen)
        {
            menuHoldActive = false;
            menuHoldKey = Keys.None;
            return;
        }

        bool isDown = false;
        try { isDown = (GetAsyncKeyState((int)menuHoldKey) & 0x8000) != 0; } catch { }
        if (!isDown)
        {
            menuHoldActive = false;
            menuHoldKey = Keys.None;
            return;
        }

        if (DateTime.Now < menuHoldNext) return;

        HandleMenuKeys(menuHoldKey);

        // Progressive acceleration. Vertical list scrolling gets fast after a longer hold;
        // tab swiping (Num4/Num6) stays a little slower so tabs remain controllable.
        double heldMs = (DateTime.Now - menuHoldStartedAt).TotalMilliseconds;
        bool tabSwipe = IsPreviousKey(menuHoldKey) || IsNextKey(menuHoldKey);
        int repeatMs;
        if (tabSwipe)
            repeatMs = heldMs >= 1800 ? 115 : (heldMs >= 850 ? 150 : 190);
        else
            repeatMs = heldMs >= 2600 ? 50 : (heldMs >= 1500 ? 65 : (heldMs >= 700 ? 90 : 125));
        menuHoldNext = DateTime.Now.AddMilliseconds(repeatMs);
    }

    private void OnKeyUp(object sender, KeyEventArgs e)
    {
        try
        {
            if (!gameplayInteractionReady || !SafeRenderState) return;
            if (EnforcePublicReleaseSinglePlayerGuard()) return;
            try { if (Function.Call<bool>(Hash.IS_PAUSE_MENU_ACTIVE)) return; } catch { }
            // Repeat-enabled menu keys are handled on KeyDown/UpdateMenuHold.
            // Never process them a second time on KeyUp.
            if (menuOpen && IsMenuRepeatKey(e.KeyCode))
            {
                if (menuHoldKey == e.KeyCode)
                {
                    menuHoldActive = false;
                    menuHoldKey = Keys.None;
                }
                return;
            }
            if (IsVolumeDownKey(e.KeyCode) || IsVolumeUpKey(e.KeyCode))
            {
                volumeHoldActive = false;
                volumeHoldKey = Keys.None;
                if (menuOpen && menuTab == 8) SaveAudioAndLanguageSettings();
                else if (!(menuOpen && menuTab == 7)) SaveVolumeToIni();
                SaveUserSettings();
                return;
            }
            if (IsMenuToggleKey(e.KeyCode))
            {
                if (menuCloseHandledOnPress)
                {
                    menuCloseHandledOnPress = false;
                    return;
                }

                // Opening remains on the proven TEST73 KeyUp path.
                menuOpen = !menuOpen;
                menuIndex = index;
                UpdateMenuSlowMotion();
                if (!menuOpen)
                {
                    menuHoldActive = false;
                    menuHoldKey = Keys.None;
                    SaveUserSettings();
                }
                if (menuOpen)
                {
                    menuTab = 0;
                    dynamicMenuColorInitialized = false;
                    PlayMenuOpenSound();
                    ShowBanner("Internet Radio LS", T("MENU_HINT"), 2400);
                }
                return;
            }

            if (menuOpen)
            {
                HandleMenuKeys(e.KeyCode);
                return;
            }

            if (IsHazardKey(e.KeyCode))
            {
                ToggleManualVehicleHazards();
                return;
            }

            if (IsHighBeamKey(e.KeyCode))
            {
                ToggleManualVehicleHighBeam();
                return;
            }

            if (IsToggleKey(e.KeyCode))
            {
                if (youtubeModeActive) { ToggleYouTubeMusic(); ShowActionFeedback("YOUTUBE", T(youtubeModeActive ? "ON" : "OFF")); }
                else if (spotifyModeActive) { ToggleSpotifyMusic(); ShowActionFeedback("SPOTIFY", T(spotifyModeActive ? "ON" : "OFF")); }
                else if (gtaRadioModeActive) { ToggleGtaRadio(); ShowActionFeedback(T("GTA_RADIO"), T(gtaRadioPlayingRequested ? "ON" : "OFF")); }
                else
                {
                    enabled = !enabled;
                    MarkUserSettingsDirty();
                    if (!enabled) StopRadio(); else StartRadio();
                    ShowActionFeedback("RADIO", T(enabled ? "ON" : "OFF"));
                }
            }
            else if (IsNextKey(e.KeyCode))
            {
                if (youtubeModeActive) YouTubeNext();
                else if (spotifyModeActive) SpotifyNext();
                else if (gtaRadioModeActive) ChangeGtaRadio(1);
                else Change(1);
            }
            else if (IsPreviousKey(e.KeyCode))
            {
                if (youtubeModeActive) YouTubePrevious();
                else if (spotifyModeActive) SpotifyPrevious();
                else if (gtaRadioModeActive) ChangeGtaRadio(-1);
                else Change(-1);
            }
            else if (IsStopKey(e.KeyCode))
            {
                if (youtubeModeActive) PauseYouTubeMusic();
                else if (spotifyModeActive) PauseSpotifyMusic();
                else if (gtaRadioModeActive) PauseGtaRadio();
                else
                {
                    StopRadio();
                    ShowBanner(T("STATE_STOPPED"), CurrentStationLine(), 1800);
                }
            }
            else if (e.KeyCode == reloadKey)
            {
                // v7.31: commit source/station plus the real external Playing/Paused state
                // before re-reading the config.
                CapturePersistentExternalPlaybackState();
                SaveUserSettings();
                StopRadio();
                LoadIni();
                menuIndex = index;
                ShowBanner(T("SETTINGS"), T("READY"), 2200);
            }
        }
        catch (Exception ex)
        {
            Log("KEY FEHLER: " + ex.ToString());
        }
    }

    private void HandleMenuKeys(Keys key)
    {
        if (stations.Count == 0)
        {
            if (IsMenuToggleKey(key) || key == Keys.Escape || key == Keys.Back)
            {
                menuOpen = false;
                RestoreGameTimeScale();
            }
            return;
        }

        // Im Menue wechseln Num4/Num6 die linken Reiter. Ausserhalb des Menues
        // bleiben sie wie gewohnt Sender zurueck/weiter.
        if (IsPreviousKey(key))
        {
            ChangeMenuTab(-1);
            return;
        }
        if (IsNextKey(key))
        {
            ChangeMenuTab(1);
            return;
        }

        if (IsMenuUpKey(key))
        {
            if (menuTab == 2) MovePack(-1);
            else if (menuTab == 4) MoveSkin(-1);
            else if (menuTab == 5) YouTubePrevious();
            else if (menuTab == 6) SpotifyPrevious();
            else if (menuTab == 7) MoveCarSettings(-1);
            else if (menuTab == 8) MoveSettings(-1);
            else if (menuTab == 9) MoveGtaRadioSelection(-1);
            else if (menuTab == 0) MoveHomeSelection(-1);
            else if (menuTab == 1) MoveFilteredMenu(-1);
        }
        else if (IsMenuDownKey(key))
        {
            if (menuTab == 2) MovePack(1);
            else if (menuTab == 4) MoveSkin(1);
            else if (menuTab == 5) YouTubeNext();
            else if (menuTab == 6) SpotifyNext();
            else if (menuTab == 7) MoveCarSettings(1);
            else if (menuTab == 8) MoveSettings(1);
            else if (menuTab == 9) MoveGtaRadioSelection(1);
            else if (menuTab == 0) MoveHomeSelection(1);
            else if (menuTab == 1) MoveFilteredMenu(1);
        }
        else if (IsSelectKey(key))
        {
            if (menuTab == 2)
            {
                ApplySelectedPack();
            }
            else if (menuTab == 3)
            {
                ShowBanner(T("INFO"), CurrentStationLine(), 1600);
            }
            else if (menuTab == 4)
            {
                ApplySelectedSkin();
            }
            else if (menuTab == 5)
            {
                HandleYouTubeSelect(true);
            }
            else if (menuTab == 6)
            {
                HandleSpotifySelect(true);
            }
            else if (menuTab == 7)
            {
                ApplySelectedCarSetting();
            }
            else if (menuTab == 8)
            {
                ApplySelectedSetting();
            }
            else if (menuTab == 9)
            {
                ActivateGtaRadioFromMenu();
            }
            else if (menuTab == 0)
            {
                OpenHomeSelection();
            }
            else
            {
                ActivateInternetRadioFromMenu();
            }
        }
        else if (key == Keys.Space && menuTab == 1)
        {
            ToggleFavorite(menuIndex);
        }
        else if (key == Keys.Delete && menuTab == 1)
        {
            RemoveFavorite(menuIndex);
        }
        else if (IsStopKey(key))
        {
            // While the menu is open, the selected service tab wins over the
            // currently active source. This lets the user switch cleanly from
            // YouTube Music to Spotify (and back) without the old source
            // intercepting NUM1/NUM3.
            if (menuTab == 5) PauseYouTubeMusic();
            else if (menuTab == 6) PauseSpotifyMusic();
            else if (menuTab == 9) PauseGtaRadio();
            else if (youtubeModeActive) PauseYouTubeMusic();
            else if (spotifyModeActive) PauseSpotifyMusic();
            else if (gtaRadioModeActive) PauseGtaRadio();
            else
            {
                StopRadio();
                ShowBanner(T("STATE_STOPPED"), CurrentStationLine(), 1800);
            }
        }
        else if (IsToggleKey(key))
        {
            if (menuTab == 5) { ToggleYouTubeMusic(); ShowActionFeedback("YOUTUBE", T(youtubeModeActive ? "ON" : "OFF")); }
            else if (menuTab == 6) { ToggleSpotifyMusic(); ShowActionFeedback("SPOTIFY", T(spotifyModeActive ? "ON" : "OFF")); }
            else if (menuTab == 9) { ToggleGtaRadio(); ShowActionFeedback(T("GTA_RADIO"), T(gtaRadioPlayingRequested ? "ON" : "OFF")); }
            else if (youtubeModeActive) { ToggleYouTubeMusic(); ShowActionFeedback("YOUTUBE", T(youtubeModeActive ? "ON" : "OFF")); }
            else if (spotifyModeActive) { ToggleSpotifyMusic(); ShowActionFeedback("SPOTIFY", T(spotifyModeActive ? "ON" : "OFF")); }
            else if (gtaRadioModeActive) { ToggleGtaRadio(); ShowActionFeedback(T("GTA_RADIO"), T(gtaRadioPlayingRequested ? "ON" : "OFF")); }
            else
            {
                enabled = !enabled;
                MarkUserSettingsDirty();
                if (!enabled) StopRadio(); else StartRadio();
                ShowActionFeedback("RADIO", T(enabled ? "ON" : "OFF"));
            }
        }
        else if (IsMenuToggleKey(key) || key == Keys.Escape || key == Keys.Back)
        {
            menuOpen = false;
            RestoreGameTimeScale();
            SaveUserSettings();
        }
    }

    private void ChangeMenuTab(int delta)
    {
        RememberCurrentMenuPosition();

        int orderIndex = 0;
        for (int i = 0; i < MenuTabOrder.Length; i++)
        {
            if (MenuTabOrder[i] == menuTab) { orderIndex = i; break; }
        }
        orderIndex += delta;
        while (orderIndex < 0) orderIndex += MenuTabOrder.Length;
        while (orderIndex >= MenuTabOrder.Length) orderIndex -= MenuTabOrder.Length;
        menuTab = MenuTabOrder[orderIndex];

        RestoreMenuPositionForCurrentTab();
        ShowBanner(T("TAB"), GetMenuTabName(), 1200);
    }

    private void RememberCurrentMenuPosition()
    {
        if (menuTab == 0) rememberedRadioMenuIndex = menuIndex;
        else if (menuTab == 1) rememberedStationMenuIndex = menuIndex;
        else if (menuTab == 2) rememberedPackMenuIndex = packIndex;
        else if (menuTab == 4) rememberedSkinMenuIndex = skinMenuIndex;
        else if (menuTab == 7) rememberedCarSettingsMenuIndex = carSettingsMenuIndex;
        else if (menuTab == 8) rememberedSettingsMenuIndex = settingsMenuIndex;
        else if (menuTab == 9) rememberedGtaRadioMenuIndex = gtaRadioMenuIndex;
    }

    private void RestoreMenuPositionForCurrentTab()
    {
        if (menuTab == 0)
        {
            int candidate = rememberedRadioMenuIndex;
            if (candidate >= 0 && candidate < stations.Count && stations[candidate].Enabled) menuIndex = candidate;
            else menuIndex = index;
        }
        else if (menuTab == 1)
        {
            int candidate = rememberedStationMenuIndex;
            bool found = false;
            for (int i = 0; i < filteredStationIndices.Count; i++)
            {
                if (filteredStationIndices[i] == candidate) { found = true; break; }
            }
            if (found) menuIndex = candidate;
            else EnsureFilteredMenuIndex();
        }
        else if (menuTab == 2)
        {
            if (rememberedPackMenuIndex >= 0 && rememberedPackMenuIndex < menuPacks.Count)
                packIndex = rememberedPackMenuIndex;
            else
                SyncPackIndex();
        }
        else if (menuTab == 4)
        {
            if (rememberedSkinMenuIndex >= 0 && rememberedSkinMenuIndex < skinNames.Length)
                skinMenuIndex = rememberedSkinMenuIndex;
            else
                skinMenuIndex = skinIndex;
        }
        else if (menuTab == 7)
        {
            if (rememberedCarSettingsMenuIndex >= 0 && rememberedCarSettingsMenuIndex < CarSettingOrder.Length)
                carSettingsMenuIndex = rememberedCarSettingsMenuIndex;
            else
                carSettingsMenuIndex = Math.Max(0, Math.Min(CarSettingOrder.Length - 1, carSettingsMenuIndex));
        }
        else if (menuTab == 8)
        {
            if (rememberedSettingsMenuIndex >= 0 && rememberedSettingsMenuIndex < 10)
                settingsMenuIndex = rememberedSettingsMenuIndex;
            else
                settingsMenuIndex = Math.Max(0, Math.Min(9, settingsMenuIndex));
        }
        else if (menuTab == 9)
        {
            if (gtaRadioStations.Count == 0) gtaRadioMenuIndex = 0;
            else if (rememberedGtaRadioMenuIndex >= 0 && rememberedGtaRadioMenuIndex < gtaRadioStations.Count)
                gtaRadioMenuIndex = rememberedGtaRadioMenuIndex;
            else
                gtaRadioMenuIndex = Math.Max(0, Math.Min(gtaRadioStations.Count - 1, gtaRadioIndex));
        }
    }

    private string GetMenuTabName()
    {
        if (menuTab == 1) return T("STATIONS");
        if (menuTab == 2) return T("PACKS");
        if (menuTab == 3) return T("INFO");
        if (menuTab == 4) return T("SKINS");
        if (menuTab == 5) return "YOUTUBE MUSIC";
        if (menuTab == 6) return "SPOTIFY";
        if (menuTab == 7) return T("CAR_SETTINGS");
        if (menuTab == 8) return T("SETTINGS");
        if (menuTab == 9) return T("GTA_RADIO");
        return T("HOME");
    }

    private void MoveSettings(int delta)
    {
        settingsMenuIndex += delta;
        while (settingsMenuIndex < 0) settingsMenuIndex += 10;
        while (settingsMenuIndex >= 10) settingsMenuIndex -= 10;
    }

    private void MoveCarSettings(int delta)
    {
        int totalCarSettings = CarSettingOrder.Length;
        carSettingsMenuIndex += delta;
        while (carSettingsMenuIndex < 0) carSettingsMenuIndex += totalCarSettings;
        while (carSettingsMenuIndex >= totalCarSettings) carSettingsMenuIndex -= totalCarSettings;
    }

    private void ApplySelectedSetting()
    {
        if (settingsMenuIndex == 0)
        {
            if (uiLanguage == "EN") uiLanguage = "DE";
            else if (uiLanguage == "DE") uiLanguage = "ES";
            else if (uiLanguage == "ES") uiLanguage = "FR";
            else if (uiLanguage == "FR") uiLanguage = "IT";
            else if (uiLanguage == "IT") uiLanguage = "PT";
            else if (uiLanguage == "PT") uiLanguage = "TR";
            else uiLanguage = "EN";
            SaveAudioAndLanguageSettings();
            SaveUserSettings();
            ShowBanner(T("LANGUAGE"), GetLanguageDisplayName(), 1800);
        }
        else if (settingsMenuIndex == 8)
        {
            SetMediaArtworkEnabled(!mediaArtworkEnabled);
            MarkUserSettingsDirty();
            ShowActionFeedback(T("ARTWORK"), T(mediaArtworkEnabled ? "ON" : "OFF"));
        }
        else if (settingsMenuIndex == 9)
        {
            if (DateTime.Now <= resetDefaultsConfirmUntil)
            {
                ResetUserFriendlyDefaults();
                resetDefaultsConfirmUntil = DateTime.MinValue;
                ShowActionFeedback(T("RESET_DEFAULTS"), T("DONE"));
            }
            else
            {
                resetDefaultsConfirmUntil = DateTime.Now.AddSeconds(4);
                ShowActionFeedback(T("RESET_DEFAULTS"), T("PRESS_AGAIN_CONFIRM"));
            }
        }
        else
        {
            ShowActionFeedback(T("SETTINGS"), T("USE_PLUS_MINUS"));
        }
    }

    private void ApplySelectedCarSetting()
    {
        int item = GetCarSettingId(carSettingsMenuIndex);
        bool handled = true;
        string feedbackLabel = T("CAR_SETTINGS");
        bool feedbackState = false;

        if (item == 0)
        {
            SetAudioReactiveNeonEnabled(!audioReactiveNeonEnabled);
            feedbackLabel = T("BEAT_NEON"); feedbackState = audioReactiveNeonEnabled;
        }
        else if (item == 5)
        {
            audioReactiveInteriorLightEnabled = !audioReactiveInteriorLightEnabled;
            feedbackLabel = T("INTERIOR_LIGHT"); feedbackState = audioReactiveInteriorLightEnabled;
        }
        else if (item == 19)
        {
            plateLightEnabled = !plateLightEnabled;
            if (!plateLightEnabled) ClearVehiclePlateLight();
            feedbackLabel = T("PLATE_LIGHT"); feedbackState = plateLightEnabled;
        }
        else if (item == 18)
        {
            automaticIndicatorsEnabled = !automaticIndicatorsEnabled;
            feedbackLabel = T("AUTO_INDICATORS"); feedbackState = automaticIndicatorsEnabled;
            // Turning automation off must never disable manually selected hazards.
            if (!automaticIndicatorsEnabled && !manualHazardsActive && automaticIndicatorVehicleHandle != 0)
                SetAutomaticVehicleIndicatorState(automaticIndicatorVehicleHandle, 0);
        }
        else if (item == 20)
        {
            turboImpactEnabled = !turboImpactEnabled;
            ResetTurboImpactState(true);
            MarkUserSettingsDirty();
            ShowActionFeedback(T("TURBO_IMPACT"), GetTurboImpactModeText());
            return;
        }
        else if (item == 7)
        {
            showCustomSpeedometer = !showCustomSpeedometer;
            feedbackLabel = T("ROAD_HUD"); feedbackState = showCustomSpeedometer;
        }
        else if (item == 8)
        {
            showBoatHud = !showBoatHud;
            feedbackLabel = T("BOAT_HUD"); feedbackState = showBoatHud;
        }
        else if (item == 9)
        {
            showFlightHud = !showFlightHud;
            feedbackLabel = T("FLIGHT_HUD"); feedbackState = showFlightHud;
        }
        else if (item == 10)
        {
            speedometerShowCarIcon = !speedometerShowCarIcon;
            feedbackLabel = T("SPEEDO_CAR_ICON"); feedbackState = speedometerShowCarIcon;
        }
        else if (item == 11)
        {
            speedometerShowRpm = !speedometerShowRpm;
            feedbackLabel = T("SPEEDO_RPM_BAR"); feedbackState = speedometerShowRpm;
        }
        else if (item == 12)
        {
            speedometerUseMph = !speedometerUseMph;
            MarkUserSettingsDirty();
            ShowActionFeedback(T("SPEEDO_UNIT_MODE"), speedometerUseMph ? "MPH" : "KM/H");
            return;
        }
        else if (item == 13)
        {
            navigationUseMetric = !navigationUseMetric;
            MarkUserSettingsDirty();
            ShowActionFeedback(T("NAV_UNIT_MODE"), T(navigationUseMetric ? "METRIC" : "STANDARD"));
            return;
        }
        else
        {
            handled = false;
        }

        if (!handled)
        {
            ShowActionFeedback(T("CAR_SETTINGS"), T("USE_PLUS_MINUS"));
            return;
        }

        MarkUserSettingsDirty();
        ShowActionFeedback(feedbackLabel, T(feedbackState ? "ON" : "OFF"));
    }

    private void AdjustSelectedSetting(int direction)
    {
        if (settingsMenuIndex == 0) return;
        if (settingsMenuIndex == 1)
            duckVolumePercent = Math.Max(20, Math.Min(100, duckVolumePercent + direction));
        else if (settingsMenuIndex == 2)
            playerMaxVolume = Math.Max(20, Math.Min(100, playerMaxVolume + direction));
        else if (settingsMenuIndex == 3)
            youtubeMaxVolume = Math.Max(5, Math.Min(100, youtubeMaxVolume + direction));
        else if (settingsMenuIndex == 4)
            spotifyMaxVolume = Math.Max(5, Math.Min(100, spotifyMaxVolume + direction));
        else if (settingsMenuIndex == 5)
            volumeStep = 1;
        else if (settingsMenuIndex == 6)
            duckReleaseMs = Math.Max(500, Math.Min(4000, duckReleaseMs + direction * 250));
        else if (settingsMenuIndex == 7)
            duckFadeOutMs = Math.Max(200, Math.Min(2000, duckFadeOutMs + direction * 100));
        else if (settingsMenuIndex == 8)
            SetMediaArtworkEnabled(direction >= 0);

        appliedStreamVolume = -1;
        appliedYoutubeVolume = -1;
        appliedYoutubeMixerLevel = -1.0f;
        appliedSpotifyVolume = -1;
        appliedSpotifyMixerLevel = -1.0f;
        ApplyEffectiveVolume();
        MarkUserSettingsDirty();
    }

    private void AdjustSelectedCarSetting(int direction)
    {
        int item = GetCarSettingId(carSettingsMenuIndex);
        if (item == 0)
            SetAudioReactiveNeonEnabled(direction >= 0);
        else if (item == 1)
            neonBeatSensitivity = Math.Max(1, Math.Min(10, neonBeatSensitivity + direction));
        else if (item == 2)
            neonVoiceFilter = Math.Max(0, Math.Min(100, neonVoiceFilter + direction * 5));
        else if (item == 3)
            neonPulseStrength = Math.Max(10, Math.Min(100, neonPulseStrength + direction * 5));
        else if (item == 4)
            neonPulseSpeed = Math.Max(10, Math.Min(100, neonPulseSpeed + direction * 5));
        else if (item == 5)
            audioReactiveInteriorLightEnabled = direction >= 0;
        else if (item == 6)
            interiorLightStrength = Math.Max(10, Math.Min(100, interiorLightStrength + direction * 5));
        else if (item == 19)
        {
            plateLightEnabled = direction >= 0;
            if (!plateLightEnabled) ClearVehiclePlateLight();
        }
        else if (item == 18)
        {
            automaticIndicatorsEnabled = direction >= 0;
            if (!automaticIndicatorsEnabled && !manualHazardsActive && automaticIndicatorVehicleHandle != 0)
                SetAutomaticVehicleIndicatorState(automaticIndicatorVehicleHandle, 0);
        }
        else if (item == 20)
        {
            turboImpactEnabled = direction >= 0;
            ResetTurboImpactState(true);
        }
        else if (item == 7)
            showCustomSpeedometer = direction >= 0;
        else if (item == 8)
            showBoatHud = direction >= 0;
        else if (item == 9)
            showFlightHud = direction >= 0;
        else if (item == 10)
            speedometerShowCarIcon = direction >= 0;
        else if (item == 11)
            speedometerShowRpm = direction >= 0;
        else if (item == 12)
            speedometerUseMph = direction >= 0;
        else if (item == 13)
            navigationUseMetric = direction >= 0;
        else if (item == 14)
        {
            float newWidth = Math.Max(0.150f, Math.Min(0.320f, speedometerWidth + direction * 0.010f));
            speedometerWidth = newWidth;
            speedometerHeight = Math.Max(0.095f, Math.Min(0.180f, newWidth * (0.126f / 0.205f)));
        }
        else if (item == 15)
            speedometerLeft = Math.Max(0.005f, Math.Min(0.845f, speedometerLeft + direction * 0.010f));
        else if (item == 16)
            speedometerTop = Math.Max(0.600f, Math.Min(0.930f, speedometerTop + direction * 0.010f));
        else if (item == 17)
            speedometerOpacity = Math.Max(20, Math.Min(100, speedometerOpacity + direction * 5));

        MarkUserSettingsDirty();
    }

    private void SetMediaArtworkEnabled(bool enabledValue)
    {
        if (mediaArtworkEnabled == enabledValue) return;
        mediaArtworkEnabled = enabledValue;

        if (!mediaArtworkEnabled)
        {
            ReleaseMediaArtworkSprites();
        }

        try { if (youtube != null) youtube.SetArtworkEnabled(mediaArtworkEnabled); } catch { }
        try { if (spotify != null) spotify.SetArtworkEnabled(mediaArtworkEnabled); } catch { }
        Log("MEDIA ARTWORK " + (mediaArtworkEnabled ? "ON" : "OFF"));
    }

    private void ReleaseMediaArtworkSprites()
    {
        // Clear the old persistent ScriptHookV texture slots before dropping our last
        // reference. Otherwise the previous cover can remain visible for ~100 ms.
        FlushCustomSpriteInstances(youtubeCoverSprite, Math.Max(4, youtubeCoverDrawCountPreviousFrame));
        FlushCustomSpriteInstances(spotifyCoverSprite, Math.Max(4, spotifyCoverDrawCountPreviousFrame));
        youtubeCoverSprite = null;
        spotifyCoverSprite = null;
        youtubeCoverDrawCountThisFrame = youtubeCoverDrawCountPreviousFrame = 0;
        spotifyCoverDrawCountThisFrame = spotifyCoverDrawCountPreviousFrame = 0;
        youtubeCoverPath = "";
        spotifyCoverPath = "";
        youtubeCoverLoadedPath = "";
        spotifyCoverLoadedPath = "";
        // Mini UI timers are independent from artwork. Covers are optional/opt-in.
    }

    private void FlushCustomSpriteInstances(GTA.UI.CustomSprite sprite, int instanceCount)
    {
        if (sprite == null || instanceCount <= 0) return;
        try
        {
            System.Drawing.Color oldColor = sprite.Color;
            System.Drawing.SizeF oldSize = sprite.Size;
            System.Drawing.PointF oldPosition = sprite.Position;
            bool oldEnabled = sprite.Enabled;

            sprite.Enabled = true;
            sprite.Color = System.Drawing.Color.FromArgb(0, 255, 255, 255);
            sprite.Size = new System.Drawing.SizeF(1f, 1f);
            sprite.Position = new System.Drawing.PointF(-4096f, -4096f);

            // SHVDN CustomSprite.Draw() keeps every instance alive for 100 ms.
            // Re-drawing the same instance slots transparent cancels that tail on
            // the next render frame instead of letting PNGs linger after the UI.
            for (int i = 0; i < instanceCount; i++) sprite.Draw();

            sprite.Color = oldColor;
            sprite.Size = oldSize;
            sprite.Position = oldPosition;
            sprite.Enabled = oldEnabled;
        }
        catch { }
    }

    private void FlushMenuCustomSpriteResidue()
    {
        for (int i = 0; i < uiIconSprites.Length; i++)
            FlushCustomSpriteInstances(uiIconSprites[i], 8);

        FlushCustomSpriteInstances(youtubeCoverSprite, 4);
        FlushCustomSpriteInstances(spotifyCoverSprite, 4);

        // These are normally absent in the clean-header build, but flushing them
        // too keeps the renderer safe if an older asset folder is reused.
        FlushCustomSpriteInstances(blockWorldMotifSprite, 2);
        FlushCustomSpriteInstances(sakuraZenMotifSprite, 2);
        FlushCustomSpriteInstances(spectrumMotifSprite, 2);
        FlushCustomSpriteInstances(neonSunsetMotifSprite, 2);
        FlushCustomSpriteInstances(westCoastMotifSprite, 2);
        FlushCustomSpriteInstances(futureNeonMotifSprite, 2);
        FlushCustomSpriteInstances(streetTunerMotifSprite, 2);
        FlushCustomSpriteInstances(noirLuxeMotifSprite, 2);
    }

    private void ResetTrackedSpriteDrawCounts()
    {
        for (int i = 0; i < uiIconDrawCountThisFrame.Length; i++) uiIconDrawCountThisFrame[i] = 0;
        youtubeCoverDrawCountThisFrame = 0;
        spotifyCoverDrawCountThisFrame = 0;
    }

    private void PadUnusedCustomSpriteSlots(GTA.UI.CustomSprite sprite, int unusedSlots)
    {
        if (sprite == null || unusedSlots <= 0) return;
        try
        {
            System.Drawing.Color oldColor = sprite.Color;
            System.Drawing.SizeF oldSize = sprite.Size;
            System.Drawing.PointF oldPosition = sprite.Position;
            bool oldEnabled = sprite.Enabled;

            sprite.Enabled = true;
            sprite.Color = System.Drawing.Color.FromArgb(0, 255, 255, 255);
            sprite.Size = new System.Drawing.SizeF(1f, 1f);
            sprite.Position = new System.Drawing.PointF(-4096f, -4096f);
            for (int i = 0; i < unusedSlots; i++) sprite.Draw();

            sprite.Color = oldColor;
            sprite.Size = oldSize;
            sprite.Position = oldPosition;
            sprite.Enabled = oldEnabled;
        }
        catch { }
    }

    private void StabilizeTrackedCustomSpriteSlots()
    {
        // Visible instances are always drawn first. If this frame uses fewer copies of
        // a cached PNG than the previous frame, immediately occupy the remaining old
        // instance slots with transparent off-screen draws. No full sprite flush occurs
        // while switching pages, so there is no one-frame flash from clear/redraw races.
        for (int i = 0; i < uiIconSprites.Length && i < uiIconDrawCountThisFrame.Length; i++)
        {
            int current = Math.Max(0, uiIconDrawCountThisFrame[i]);
            int previous = Math.Max(0, uiIconDrawCountPreviousFrame[i]);
            if (current < previous) PadUnusedCustomSpriteSlots(uiIconSprites[i], previous - current);
            uiIconDrawCountPreviousFrame[i] = current;
        }

        if (youtubeCoverDrawCountThisFrame < youtubeCoverDrawCountPreviousFrame)
            PadUnusedCustomSpriteSlots(youtubeCoverSprite, youtubeCoverDrawCountPreviousFrame - youtubeCoverDrawCountThisFrame);
        if (spotifyCoverDrawCountThisFrame < spotifyCoverDrawCountPreviousFrame)
            PadUnusedCustomSpriteSlots(spotifyCoverSprite, spotifyCoverDrawCountPreviousFrame - spotifyCoverDrawCountThisFrame);

        youtubeCoverDrawCountPreviousFrame = Math.Max(0, youtubeCoverDrawCountThisFrame);
        spotifyCoverDrawCountPreviousFrame = Math.Max(0, spotifyCoverDrawCountThisFrame);
    }

    private void CleanupMediaArtworkCache(string reason)
    {
        try
        {
            string[] folders = new string[] { "spotify_covers", "ytmusic_covers" };
            for (int i = 0; i < folders.Length; i++)
            {
                string cacheDir = Path.Combine(dir, folders[i]);
                if (!Directory.Exists(cacheDir)) continue;
                foreach (string file in Directory.GetFiles(cacheDir))
                {
                    try { File.Delete(file); } catch { }
                }
                try
                {
                    if (Directory.GetFiles(cacheDir).Length == 0 && Directory.GetDirectories(cacheDir).Length == 0)
                        Directory.Delete(cacheDir, false);
                }
                catch { }
            }
            Log("MEDIA ARTWORK CACHE CLEAN | " + reason);
        }
        catch (Exception ex)
        {
            Log("MEDIA ARTWORK CACHE CLEAN ERROR | " + reason + " | " + ex.Message);
        }
    }

    private string GetLanguageDisplayName()
    {
        if (uiLanguage == "EN") return "English";
        if (uiLanguage == "DE") return "Deutsch";
        if (uiLanguage == "ES") return "Español";
        if (uiLanguage == "FR") return "Français";
        if (uiLanguage == "IT") return "Italiano";
        if (uiLanguage == "PT") return "Português (BR)";
        if (uiLanguage == "TR") return "Türkçe";
        return "English";
    }

    private static string NormalizeLanguage(string value)
    {
        string v = (value ?? "EN").Trim().ToUpperInvariant();
        if (v.StartsWith("EN")) return "EN";
        if (v.StartsWith("DE")) return "DE";
        if (v.StartsWith("ES")) return "ES";
        if (v.StartsWith("FR")) return "FR";
        if (v.StartsWith("IT")) return "IT";
        if (v.StartsWith("PT") || v.StartsWith("BR")) return "PT";
        if (v.StartsWith("TR") || v.StartsWith("TU")) return "TR";
        return "EN";
    }

    private string TranslateFrench(string key)
    {
        switch (key)
        {
            case "LOADED": return "Chargé - langue, audio et radio prêts";
            case "STATIONS": return "STATIONS";
            case "FAVORITES": return "FAVORIS";
            case "FAVORITE_SHORT": return "FAV +/-";
            case "FAVORITE_ADDED": return "Ajouté";
            case "FAVORITE_REMOVED": return "Supprimé";
            case "FAVORITES_FULL": return "Liste pleine : maximum 6 stations";
            case "NOT_FAVORITE": return "Cette station n'est pas dans les favoris";
            case "NO_FAVORITES": return "Aucun favori - choisissez une station et appuyez sur Espace";
            case "WORLDWIDE_RADIO": return "RADIO EN LIGNE MONDIALE";
            case "HOME": return "ACCUEIL";
            case "HOME_APPS": return "APPLIS";
            case "HOME_HINT": return "Num8/2 choisir appli | Num5 ouvrir";
            case "OPEN": return "OUVRIR";
            case "INFOTAINMENT": return "SYSTÈME MULTIMÉDIA & VÉHICULE";
            case "PACKS": return "CATÉGORIES";
            case "INFO": return "INFO";
            case "SKINS": return "THÈMES";
            case "SETTINGS": return "PARAMÈTRES";
            case "SETTINGS_NAV": return "PARAMÈTRES";
            case "CAR_SETTINGS": return "PARAMÈTRES VÉHICULE";
            case "CAR_SETTINGS_NAV": return "VÉHICULE";
            case "CAR_SETTINGS_HINT": return "Num8/2 sélectionner | Num5 activer | -/+ régler";
            case "CAR_SETTINGS_NOTE": return "Réglez ici Beat Neon, l’éclairage intérieur, la plaque et les clignotants automatiques.";
            case "INTERIOR_LIGHT": return "ÉCLAIRAGE INTÉRIEUR";
            case "INTERIOR_STRENGTH": return "INTENSITÉ ÉCLAIRAGE";
            case "PLATE_LIGHT": return "ÉCLAIRAGE DE PLAQUE";
            case "AUTO_INDICATORS": return "CLIGNOTANTS AUTOMATIQUES";
            case "SPEEDOMETER": return "COMPTEUR";
            case "ROAD_HUD": return "COMPTEUR ROUTE";
            case "BOAT_HUD": return "HUD MARINE";
            case "FLIGHT_HUD": return "HUD AÉRIEN";
            case "SPEEDO_CAR_ICON": return "LOGO CONSTRUCTEUR";
            case "SPEEDO_RPM_BAR": return "BARRE TR/MIN";
            case "SPEEDO_UNIT_MODE": return "UNITÉ DE VITESSE";
            case "NAV_UNIT_MODE": return "UNITÉS MER / AIR";
            case "STANDARD": return "STANDARD";
            case "METRIC": return "MÉTRIQUE";
            case "SPEEDO_SIZE": return "TAILLE DU COMPTEUR";
            case "SPEEDO_POS_X": return "POSITION X";
            case "SPEEDO_POS_Y": return "POSITION Y";
            case "SPEEDO_OPACITY": return "OPACITÉ";
            case "SPEEDOMETER_NOTE": return "HUD route, marine et aérien, unités, logo, barre moteur, taille, position et opacité sont réglables ici.";
            case "CAR_SETTINGS_SCROLL_NOTE": return T("ALT_CONTROL_HINT");
            case "SPEEDO_DRIVE": return "CONDUITE";
            case "SPEEDO_GEAR": return "RAPPORT";
            case "SPEEDO_RPM": return "TR/MIN";
            case "SPEEDO_MARINE": return "MARINE";
            case "SPEEDO_FLIGHT": return "VOL";
            case "SPEEDO_HEADING": return "CAP";
            case "SPEEDO_WATER_SPEED": return "VITESSE SUR L'EAU";
            case "SPEEDO_GROUND_SPEED": return "VITESSE SOL";
            case "SPEEDO_ALTITUDE": return "ALTITUDE";
            case "SPEEDO_VERTICAL": return "VITESSE VERT.";
            case "SPEEDO_UNIT": return "KM/H";
            case "MENU": return "MENU";
            case "EXIT": return "FERMER";
            case "POWER": return "MARCHE/ARRÊT";
            case "STOP": return "ARRÊTER";
            case "NAV": return "NAVIGUER";
            case "DIALOG": return "DIALOGUE";
            case "PHONE": return "APPEL";
            case "TAB": return "Onglet";
            case "TAB_SHORT": return "ONGLET";
            case "SETTING_SHORT": return "RÉGLAGE";
            case "CHANGE_TAB": return "CHANGER D'ONGLET";
            case "SELECT": return "SÉLECTIONNER";
            case "START": return "DÉMARRER";
            case "APPLY": return "APPLIQUER";
            case "VALUE": return "VALEUR";
            case "QUIETER": return "BAISSER";
            case "LOUDER": return "MONTER";
            case "SEEK": return "CHANGER";
            case "VOLUME": return "VOLUME";
            case "VOLUME_SHORT": return "VOL";
            case "DUCKING": return "RÉDUCTION PENDANT DIALOGUE";
            case "STATUS": return "STATUT";
            case "PACK": return "CATÉGORIE";
            case "VIBE": return "AMBIANCE";
            case "GENRE": return "GENRE";
            case "STATION_COUNT": return "stations";
            case "MANUAL": return "MANUEL";
            case "STATION": return "STATION";
            case "PACK_GENRE": return "CATÉGORIE / GENRE";
            case "NO_STATION": return "Aucune station";
            case "NO_ACTIVE_STATION": return "Aucune station active";
            case "NO_METADATA": return "Aucune métadonnée de morceau";
            case "ALL_STATIONS": return "Toutes les stations";
            case "START_STATION": return "Démarrer la station";
            case "PACKS_GROUPS": return "CATÉGORIES RADIO";
            case "PACK_HINT": return "Num8/2 choisir catégorie | Num5 afficher stations";
            case "SKINS_THEMES": return "THÈMES";
            case "SKIN_HINT": return "Num8/2 choisir thème | Num5 appliquer";
            case "ACTIVE": return "ACTIF";
            case "PREVIEW": return "APERÇU";
            case "SKIN_HELP": return "Le thème change l'interface ; l'audio reste inchangé.";
            case "INFO_HELP": return "Num4/6 change d'onglet | Num0 ferme le menu";
            case "CONNECTED": return "CONNECTÉ";
            case "NOT_CONNECTED": return "NON CONNECTÉ";
            case "MEDIA_SESSION": return "SESSION MÉDIA WINDOWS";
            case "NOW_PLAYING": return "LECTURE EN COURS";
            case "YT_START_HINT": return "Num5 : activer dans le véhicule / ouvrir YT Music";
            case "YT_START_SONG": return "Choisissez la musique là-bas. Au démarrage, GTA la détecte automatiquement.";
            case "YT_CHOOSE_MUSIC": return "Choisissez la musique - GTA détecte la lecture";
            case "YT_DETECTED": return "Lecture détectée - YT Music actif";
            case "YT_STARTING": return "Démarrage de la lecture";
            case "YT_OPENED_SELECT": return "YT Music ouvert - appuyez sur Lire dans YT Music";
            case "BRIDGE_UNAVAILABLE": return "Passerelle indisponible";
            case "YT_SESSION_ONLINE": return "SESSION YT EN LIGNE";
            case "YT_SESSION_OFFLINE": return "SESSION YT HORS LIGNE";
            case "PREV_SHORT": return "PRÉC.";
            case "NEXT_SHORT": return "SUIV.";
            case "PAUSE": return "PAUSE";
            case "PREVIOUS_TRACK": return "Morceau précédent";
            case "NEXT_TRACK": return "Morceau suivant";
            case "TRACK_NAV": return "PRÉC./SUIV.";
            case "PLAY_PAUSE": return "LIRE / PAUSE";
            case "YT_OPEN_FAILED": return "Impossible d'ouvrir YouTube Music - vérifiez le navigateur par défaut";
            case "OPEN_YT": return "OUVRIR / DÉMARRER";
            case "OPEN_PLAY": return "OUVRIR / LIRE";
            case "SOURCE": return "Source";
            case "YT_EXCLUSIVE": return "YT Music remplace la radio internet lorsqu’il est actif.";
            case "SETTINGS_HINT": return "Num8/2 sélectionner | Num5 appliquer | -/+ régler";
            case "LANGUAGE": return "LANGUE";
            case "ARTWORK": return "POCHETTES MÉDIA";
            case "ARTWORK_NOTE": return "Désactivé par défaut ; si activé, l’artwork est mis en cache localement pendant la session puis supprimé à la fermeture.";
            case "LEGAL": return "MENTIONS LÉGALES";
            case "LEGAL_NOTICE_1": return "Mod non officiel et indépendant ; aucune affiliation, sponsorisation ou approbation par des tiers.";
            case "LEGAL_NOTICE_2": return "Marques et médias restent à leurs titulaires. Aucune pochette incluse ; artwork optionnel, local et temporaire.";
            case "LEGAL_NOTICE_3": return "Mode solo uniquement ; les fonctions sont désactivées si une session GTA Online est détectée.";
            case "DUCK_LEVEL": return "VOLUME PENDANT DIALOGUE";
            case "RADIO_OUTPUT": return "LIMITE VOLUME RADIO";
            case "YT_OUTPUT": return "LIMITE VOLUME YT";
            case "VOLUME_STEP": return "PAS DE VOLUME";
            case "BEAT_NEON": return "NÉON RÉACTIF";
            case "BASS_ANALYZER": return "ANALYSEUR BASSES";
            case "BEAT_SENSITIVITY": return "SENSIBILITÉ BEAT";
            case "VOICE_FILTER": return "FILTRE VOIX";
            case "PULSE_STRENGTH": return "FORCE PULSATION";
            case "PULSE_SPEED": return "VITESSE PULSATION";
            case "NEON_TUNING_NOTE": return "Réglage transitoire : ce n’est pas un filtre FFT de fréquences.";
            case "ON": return "ACTIVÉ";
            case "OFF": return "DÉSACTIVÉ";
            case "RADIO_OUTPUT_SHORT": return "RADIO MAX";
            case "YT_OUTPUT_SHORT": return "YT MAX";
            case "AUDIO": return "AUDIO";
            case "DUCK_HOLD": return "MAINTIEN DIALOGUE";
            case "DUCK_FADE": return "RETOUR ATTÉNUATION";
            case "SETTINGS_NOTE": return "L'atténuation garde le volume bas entre les dialogues puis le remonte en douceur.";
            case "HOLD_VOLUME_NOTE": return "Hors Paramètres : maintenez Num-/Num+ pour changer le volume en continu.";
            case "HOLD_PLUS_MINUS": return "MAINTENIR +/-";
            case "USE_PLUS_MINUS": return "Utilisez Num-/Num+ pour changer la valeur";
            case "MENU_HINT": return "Num4/6 onglet | Num8/2 sélection | Num5 OK | Espace favori";
            case "RADIO_ON": return "InternetRadio ACTIVÉE";
            case "RADIO_OFF": return "InternetRadio DÉSACTIVÉE";
            case "NOW_ACTIVE": return "Actif maintenant";
            case "CHECK_INI": return "Vérifiez InternetRadio.ini";
            case "VEHICLE_ONLY": return "Véhicule uniquement";
            case "VEHICLE_ONLY_NOTE": return "InternetRadio est limitée aux véhicules";
            case "NO_CYCLE_RADIO": return "Pas de radio à vélo";
            case "NO_CYCLE_RADIO_NOTE": return "BMX et vélos ne prennent pas en charge InternetRadio";
            case "STATE_PLAYING": return "Lecture";
            case "STATE_BUFFERING": return "Chargement";
            case "STATE_CONNECTING": return "Connexion";
            case "STATE_WAITING": return "En attente";
            case "STATE_PAUSED": return "En pause";
            case "STATE_STOPPED": return "Arrêté";
            case "STATE_ENDED": return "Terminé";
            case "STATE_OFF": return "Éteint";
            case "ON_AIR": return "EN DIRECT";
            case "SPACE_KEY": return "ESPACE";
            case "MEDIA_SHORT": return "MÉDIA";
            case "YT_OUT_SHORT": return "SORTIE YT";
            case "STATION_UNAVAILABLE": return "Station indisponible - passage à la suivante";
            case "SPOTIFY_START_HINT": return "Num5 : activer dans le véhicule / ouvrir Spotify";
            case "SPOTIFY_START_SONG": return "Choisissez la musique dans Spotify. Au démarrage, GTA la détecte automatiquement.";
            case "SPOTIFY_DETECTED": return "Lecture détectée - Spotify actif";
            case "SPOTIFY_OPENED_SELECT": return "Spotify ouvert - appuyez sur Lire dans Spotify";
            case "SPOTIFY_OPEN_FAILED": return "Impossible d'ouvrir Spotify - vérifiez l'installation ou le navigateur";
            case "SPOTIFY_SESSION_ONLINE": return "SESSION SPOTIFY EN LIGNE";
            case "SPOTIFY_SESSION_OFFLINE": return "SESSION SPOTIFY HORS LIGNE";
            case "SPOTIFY_EXCLUSIVE": return "Spotify remplace la radio internet lorsqu'il est actif.";
            case "SPOTIFY_OUTPUT": return "LIMITE VOLUME SPOTIFY";
            case "SPOTIFY_OUTPUT_SHORT": return "SPOTIFY MAX";
            case "SPOTIFY_OUT_SHORT": return "SORTIE SPOTIFY";
            case "OPEN_SPOTIFY": return "OUVRIR / DÉMARRER";
            default: return null;
        }
    }

    private string TranslateItalian(string key)
    {
        switch (key)
        {
            case "LOADED": return "Caricato - lingua, audio e radio pronti";
            case "STATIONS": return "STAZIONI";
            case "FAVORITES": return "PREFERITI";
            case "FAVORITE_SHORT": return "PREF +/-";
            case "FAVORITE_ADDED": return "Aggiunta";
            case "FAVORITE_REMOVED": return "Rimossa";
            case "FAVORITES_FULL": return "Lista piena: massimo 6 stazioni";
            case "NOT_FAVORITE": return "La stazione non è nei preferiti";
            case "NO_FAVORITES": return "Nessun preferito - scegli una stazione e premi Spazio";
            case "WORLDWIDE_RADIO": return "RADIO INTERNET MONDIALE";
            case "HOME": return "HOME";
            case "HOME_APPS": return "APP";
            case "HOME_HINT": return "Num8/2 scegli app | Num5 apri";
            case "OPEN": return "APRI";
            case "INFOTAINMENT": return "SISTEMA MULTIMEDIA & VEICOLO";
            case "PACKS": return "CATEGORIE";
            case "INFO": return "INFO";
            case "SKINS": return "TEMI";
            case "SETTINGS": return "IMPOSTAZIONI";
            case "SETTINGS_NAV": return "IMPOST.";
            case "CAR_SETTINGS": return "IMPOSTAZIONI AUTO";
            case "CAR_SETTINGS_NAV": return "AUTO";
            case "CAR_SETTINGS_HINT": return "Num8/2 seleziona | Num5 attiva | -/+ regola";
            case "CAR_SETTINGS_NOTE": return "Qui regoli Beat Neon, luce interna, luce targa e frecce automatiche.";
            case "INTERIOR_LIGHT": return "LUCE INTERNA";
            case "INTERIOR_STRENGTH": return "INTENSITÀ LUCE";
            case "PLATE_LIGHT": return "LUCE TARGA";
            case "AUTO_INDICATORS": return "FRECCE AUTOMATICHE";
            case "SPEEDOMETER": return "TACHIMETRO";
            case "ROAD_HUD": return "HUD STRADA";
            case "BOAT_HUD": return "HUD NAUTICO";
            case "FLIGHT_HUD": return "HUD VOLO";
            case "SPEEDO_CAR_ICON": return "LOGO PRODUTTORE";
            case "SPEEDO_RPM_BAR": return "BARRA GIRI";
            case "SPEEDO_UNIT_MODE": return "UNITÀ VELOCITÀ";
            case "NAV_UNIT_MODE": return "UNITÀ MARE / VOLO";
            case "STANDARD": return "STANDARD";
            case "METRIC": return "METRICO";
            case "SPEEDO_SIZE": return "DIMENSIONE TACHIMETRO";
            case "SPEEDO_POS_X": return "POSIZIONE X";
            case "SPEEDO_POS_Y": return "POSIZIONE Y";
            case "SPEEDO_OPACITY": return "OPACITÀ";
            case "SPEEDOMETER_NOTE": return "HUD strada, nautico e volo, unità, logo, barra giri, dimensione, posizione e opacità sono regolabili qui.";
            case "CAR_SETTINGS_SCROLL_NOTE": return T("ALT_CONTROL_HINT");
            case "SPEEDO_DRIVE": return "GUIDA";
            case "SPEEDO_GEAR": return "MARCIA";
            case "SPEEDO_RPM": return "GIRI/MIN";
            case "SPEEDO_MARINE": return "NAUTICA";
            case "SPEEDO_FLIGHT": return "VOLO";
            case "SPEEDO_HEADING": return "ROTTA";
            case "SPEEDO_WATER_SPEED": return "VELOCITÀ ACQUA";
            case "SPEEDO_GROUND_SPEED": return "VELOCITÀ SUOLO";
            case "SPEEDO_ALTITUDE": return "ALTITUDINE";
            case "SPEEDO_VERTICAL": return "VELOCITÀ VERT.";
            case "SPEEDO_UNIT": return "KM/H";
            case "MENU": return "MENU";
            case "EXIT": return "CHIUDI";
            case "POWER": return "ACC./SP.";
            case "STOP": return "ARRESTA";
            case "NAV": return "NAVIGA";
            case "DIALOG": return "DIALOGO";
            case "PHONE": return "CHIAMATA";
            case "TAB": return "Scheda";
            case "TAB_SHORT": return "SCHEDA";
            case "SETTING_SHORT": return "IMPOST.";
            case "CHANGE_TAB": return "CAMBIA SCHEDA";
            case "SELECT": return "SELEZIONA";
            case "START": return "AVVIA";
            case "APPLY": return "APPLICA";
            case "VALUE": return "VALORE";
            case "QUIETER": return "PIÙ BASSO";
            case "LOUDER": return "PIÙ ALTO";
            case "SEEK": return "CAMBIA";
            case "VOLUME": return "VOLUME";
            case "VOLUME_SHORT": return "VOL";
            case "DUCKING": return "RIDUZIONE DURANTE DIALOGHI";
            case "STATUS": return "STATO";
            case "PACK": return "CATEGORIA";
            case "VIBE": return "ATMOSFERA";
            case "GENRE": return "GENERE";
            case "STATION_COUNT": return "stazioni";
            case "MANUAL": return "MANUALE";
            case "STATION": return "STAZIONE";
            case "PACK_GENRE": return "CATEGORIA / GENERE";
            case "NO_STATION": return "Nessuna stazione";
            case "NO_ACTIVE_STATION": return "Nessuna stazione attiva";
            case "NO_METADATA": return "Nessun metadato del brano";
            case "ALL_STATIONS": return "Tutte le stazioni";
            case "START_STATION": return "Avvia stazione";
            case "PACKS_GROUPS": return "CATEGORIE RADIO";
            case "PACK_HINT": return "Num8/2 scegli categoria | Num5 mostra stazioni";
            case "SKINS_THEMES": return "TEMI";
            case "SKIN_HINT": return "Num8/2 scegli tema | Num5 applica";
            case "ACTIVE": return "ATTIVO";
            case "PREVIEW": return "ANTEPRIMA";
            case "SKIN_HELP": return "Il tema cambia l'interfaccia; l'audio resta invariato.";
            case "INFO_HELP": return "Num4/6 cambia scheda | Num0 chiude il menu";
            case "CONNECTED": return "CONNESSO";
            case "NOT_CONNECTED": return "NON CONNESSO";
            case "MEDIA_SESSION": return "SESSIONE MEDIA WINDOWS";
            case "NOW_PLAYING": return "IN RIPRODUZIONE";
            case "YT_START_HINT": return "Num5: attiva nel veicolo / apri YT Music";
            case "YT_START_SONG": return "Scegli la musica lì. Quando parte, GTA la rileva automaticamente.";
            case "YT_CHOOSE_MUSIC": return "Scegli la musica - GTA rileva la riproduzione";
            case "YT_DETECTED": return "Riproduzione rilevata - YT Music attivo";
            case "YT_STARTING": return "Avvio riproduzione";
            case "YT_OPENED_SELECT": return "YT Music aperto - premi Riproduci in YT Music";
            case "BRIDGE_UNAVAILABLE": return "Collegamento non disponibile";
            case "YT_SESSION_ONLINE": return "SESSIONE YT ATTIVA";
            case "YT_SESSION_OFFLINE": return "SESSIONE YT INATTIVA";
            case "PREV_SHORT": return "PREC.";
            case "NEXT_SHORT": return "SUCC.";
            case "PAUSE": return "PAUSA";
            case "PREVIOUS_TRACK": return "Brano precedente";
            case "NEXT_TRACK": return "Brano successivo";
            case "TRACK_NAV": return "PREC./SUCC.";
            case "PLAY_PAUSE": return "RIPRODUCI / PAUSA";
            case "YT_OPEN_FAILED": return "Impossibile aprire YouTube Music - controlla il browser predefinito";
            case "OPEN_YT": return "APRI / AVVIA";
            case "OPEN_PLAY": return "APRI / RIPRODUCI";
            case "SOURCE": return "Sorgente";
            case "YT_EXCLUSIVE": return "YT Music sostituisce la radio internet quando è attivo.";
            case "SETTINGS_HINT": return "Num8/2 seleziona | Num5 applica | -/+ regola";
            case "LANGUAGE": return "LINGUA";
            case "ARTWORK": return "COPERTINE MEDIA";
            case "ARTWORK_NOTE": return "Disattivato per impostazione predefinita; se attivato, le copertine vengono memorizzate localmente solo durante la sessione e cancellate alla chiusura.";
            case "LEGAL": return "NOTE LEGALI";
            case "LEGAL_NOTICE_1": return "Mod non ufficiale e indipendente; nessuna affiliazione, sponsorizzazione o approvazione di terze parti.";
            case "LEGAL_NOTICE_2": return "Marchi e media restano ai rispettivi titolari. Nessuna copertina inclusa; artwork opzionale, locale e temporaneo.";
            case "LEGAL_NOTICE_3": return "Solo giocatore singolo; le funzioni vengono disattivate se viene rilevata una sessione GTA Online.";
            case "DUCK_LEVEL": return "VOLUME DURANTE DIALOGHI";
            case "RADIO_OUTPUT": return "LIMITE VOLUME RADIO";
            case "YT_OUTPUT": return "LIMITE VOLUME YT";
            case "VOLUME_STEP": return "PASSO VOLUME";
            case "BEAT_NEON": return "NEON REATTIVO";
            case "BASS_ANALYZER": return "ANALIZZATORE BASSI";
            case "BEAT_SENSITIVITY": return "SENSIBILITÀ BEAT";
            case "VOICE_FILTER": return "FILTRO VOCE";
            case "PULSE_STRENGTH": return "FORZA IMPULSO";
            case "PULSE_SPEED": return "VELOCITÀ IMPULSO";
            case "NEON_TUNING_NOTE": return "Filtro a transienti: non è un vero filtro di frequenza FFT.";
            case "ON": return "ATTIVO";
            case "OFF": return "DISATTIVO";
            case "RADIO_OUTPUT_SHORT": return "RADIO MAX";
            case "YT_OUTPUT_SHORT": return "YT MAX";
            case "AUDIO": return "AUDIO";
            case "DUCK_HOLD": return "TENUTA DIALOGHI";
            case "DUCK_FADE": return "RITORNO ATTENUAZIONE";
            case "SETTINGS_NOTE": return "L'attenuazione mantiene basso il volume tra le battute e lo ripristina gradualmente.";
            case "HOLD_VOLUME_NOTE": return "Fuori da Impostazioni: tieni Num-/Num+ per cambiare il volume in continuo.";
            case "HOLD_PLUS_MINUS": return "TIENI +/-";
            case "USE_PLUS_MINUS": return "Usa Num-/Num+ per cambiare il valore";
            case "MENU_HINT": return "Num4/6 scheda | Num8/2 selezione | Num5 OK | Spazio preferito";
            case "RADIO_ON": return "InternetRadio ATTIVA";
            case "RADIO_OFF": return "InternetRadio DISATTIVA";
            case "NOW_ACTIVE": return "Ora attivo";
            case "CHECK_INI": return "Controlla InternetRadio.ini";
            case "VEHICLE_ONLY": return "Solo nel veicolo";
            case "VEHICLE_ONLY_NOTE": return "InternetRadio è limitata ai veicoli";
            case "NO_CYCLE_RADIO": return "Niente radio in bici";
            case "NO_CYCLE_RADIO_NOTE": return "BMX e biciclette non supportano InternetRadio";
            case "STATE_PLAYING": return "In riproduzione";
            case "STATE_BUFFERING": return "Buffering";
            case "STATE_CONNECTING": return "Connessione";
            case "STATE_WAITING": return "In attesa";
            case "STATE_PAUSED": return "In pausa";
            case "STATE_STOPPED": return "Fermato";
            case "STATE_ENDED": return "Terminato";
            case "STATE_OFF": return "Spento";
            case "ON_AIR": return "IN ONDA";
            case "SPACE_KEY": return "SPAZIO";
            case "MEDIA_SHORT": return "MEDIA";
            case "YT_OUT_SHORT": return "USCITA YT";
            case "STATION_UNAVAILABLE": return "Stazione non disponibile - salto";
            case "SPOTIFY_START_HINT": return "Num5: attiva nel veicolo / apri Spotify";
            case "SPOTIFY_START_SONG": return "Scegli la musica in Spotify. Quando parte, GTA la rileva automaticamente.";
            case "SPOTIFY_DETECTED": return "Riproduzione rilevata - Spotify attivo";
            case "SPOTIFY_OPENED_SELECT": return "Spotify aperto - premi Riproduci in Spotify";
            case "SPOTIFY_OPEN_FAILED": return "Impossibile aprire Spotify - controlla l'installazione o il browser";
            case "SPOTIFY_SESSION_ONLINE": return "SESSIONE SPOTIFY ATTIVA";
            case "SPOTIFY_SESSION_OFFLINE": return "SESSIONE SPOTIFY INATTIVA";
            case "SPOTIFY_EXCLUSIVE": return "Spotify sostituisce la radio internet quando è attivo.";
            case "SPOTIFY_OUTPUT": return "LIMITE VOLUME SPOTIFY";
            case "SPOTIFY_OUTPUT_SHORT": return "SPOTIFY MAX";
            case "SPOTIFY_OUT_SHORT": return "USCITA SPOTIFY";
            case "OPEN_SPOTIFY": return "APRI / AVVIA";
            default: return null;
        }
    }

    private string TranslatePortuguese(string key)
    {
        switch (key)
        {
            case "LOADED": return "Carregado - idioma, áudio e rádio prontos";
            case "STATIONS": return "ESTAÇÕES";
            case "FAVORITES": return "FAVORITOS";
            case "FAVORITE_SHORT": return "FAV +/-";
            case "FAVORITE_ADDED": return "Adicionada";
            case "FAVORITE_REMOVED": return "Removida";
            case "FAVORITES_FULL": return "Lista cheia: máximo de 6 estações";
            case "NOT_FAVORITE": return "A estação não está nos favoritos";
            case "NO_FAVORITES": return "Sem favoritos - escolha uma estação e pressione Espaço";
            case "WORLDWIDE_RADIO": return "RÁDIO PELA INTERNET MUNDIAL";
            case "HOME": return "INÍCIO";
            case "HOME_APPS": return "APPS";
            case "HOME_HINT": return "Num8/2 escolher app | Num5 abrir";
            case "OPEN": return "ABRIR";
            case "INFOTAINMENT": return "SISTEMA MULTIMÍDIA & VEÍCULO";
            case "PACKS": return "CATEGORIAS";
            case "INFO": return "INFO";
            case "SKINS": return "TEMAS";
            case "SETTINGS": return "CONFIGURAÇÕES";
            case "SETTINGS_NAV": return "CONFIG.";
            case "CAR_SETTINGS": return "CONFIGURAÇÕES DO CARRO";
            case "CAR_SETTINGS_NAV": return "CARRO";
            case "CAR_SETTINGS_HINT": return "Num8/2 selecionar | Num5 alternar | -/+ ajustar";
            case "CAR_SETTINGS_NOTE": return "Configure aqui Beat Neon, luz interna, luz da placa e piscas automáticos.";
            case "INTERIOR_LIGHT": return "LUZ INTERNA";
            case "INTERIOR_STRENGTH": return "INTENSIDADE DA LUZ";
            case "PLATE_LIGHT": return "LUZ DA PLACA";
            case "AUTO_INDICATORS": return "PISCAS AUTOMÁTICOS";
            case "SPEEDOMETER": return "VELOCÍMETRO";
            case "ROAD_HUD": return "HUD RODOVIÁRIO";
            case "BOAT_HUD": return "HUD NÁUTICO";
            case "FLIGHT_HUD": return "HUD DE VOO";
            case "SPEEDO_CAR_ICON": return "LOGO DO FABRICANTE";
            case "SPEEDO_RPM_BAR": return "BARRA RPM";
            case "SPEEDO_UNIT_MODE": return "UNIDADE DE VELOCIDADE";
            case "NAV_UNIT_MODE": return "UNIDADES MAR / AR";
            case "STANDARD": return "PADRÃO";
            case "METRIC": return "MÉTRICO";
            case "SPEEDO_SIZE": return "TAMANHO DO VELOCÍMETRO";
            case "SPEEDO_POS_X": return "POSIÇÃO X";
            case "SPEEDO_POS_Y": return "POSIÇÃO Y";
            case "SPEEDO_OPACITY": return "OPACIDADE";
            case "SPEEDOMETER_NOTE": return "HUD rodoviário, náutico e de voo, unidades, logo, barra RPM, tamanho, posição e opacidade podem ser ajustados aqui.";
            case "CAR_SETTINGS_SCROLL_NOTE": return T("ALT_CONTROL_HINT");
            case "SPEEDO_DRIVE": return "CONDUÇÃO";
            case "SPEEDO_GEAR": return "MARCHA";
            case "SPEEDO_RPM": return "RPM";
            case "SPEEDO_MARINE": return "NÁUTICO";
            case "SPEEDO_FLIGHT": return "VOO";
            case "SPEEDO_HEADING": return "RUMO";
            case "SPEEDO_WATER_SPEED": return "VELOCIDADE NA ÁGUA";
            case "SPEEDO_GROUND_SPEED": return "VELOCIDADE NO SOLO";
            case "SPEEDO_ALTITUDE": return "ALTITUDE";
            case "SPEEDO_VERTICAL": return "VELOCIDADE VERT.";
            case "SPEEDO_UNIT": return "KM/H";
            case "MENU": return "MENU";
            case "EXIT": return "FECHAR";
            case "POWER": return "LIG./DESL.";
            case "STOP": return "PARAR";
            case "NAV": return "NAVEGAR";
            case "DIALOG": return "DIÁLOGO";
            case "PHONE": return "CHAMADA";
            case "TAB": return "Aba";
            case "TAB_SHORT": return "ABA";
            case "SETTING_SHORT": return "AJUSTE";
            case "CHANGE_TAB": return "MUDAR ABA";
            case "SELECT": return "SELECIONAR";
            case "START": return "INICIAR";
            case "APPLY": return "APLICAR";
            case "VALUE": return "VALOR";
            case "QUIETER": return "DIMINUIR";
            case "LOUDER": return "AUMENTAR";
            case "SEEK": return "MUDAR";
            case "VOLUME": return "VOLUME";
            case "VOLUME_SHORT": return "VOL";
            case "DUCKING": return "REDUÇÃO DURANTE DIÁLOGOS";
            case "STATUS": return "ESTADO";
            case "PACK": return "CATEGORIA";
            case "VIBE": return "CLIMA";
            case "GENRE": return "GÊNERO";
            case "STATION_COUNT": return "estações";
            case "MANUAL": return "MANUAL";
            case "STATION": return "ESTAÇÃO";
            case "PACK_GENRE": return "CATEGORIA / GÊNERO";
            case "NO_STATION": return "Sem estação";
            case "NO_ACTIVE_STATION": return "Nenhuma estação ativa";
            case "NO_METADATA": return "Sem metadados da música";
            case "ALL_STATIONS": return "Todas as estações";
            case "START_STATION": return "Iniciar estação";
            case "PACKS_GROUPS": return "CATEGORIAS DE RÁDIO";
            case "PACK_HINT": return "Num8/2 escolher categoria | Num5 mostrar estações";
            case "SKINS_THEMES": return "TEMAS";
            case "SKIN_HINT": return "Num8/2 escolher tema | Num5 aplicar";
            case "ACTIVE": return "ATIVO";
            case "PREVIEW": return "PRÉVIA";
            case "SKIN_HELP": return "O tema muda a interface; o áudio não é alterado.";
            case "INFO_HELP": return "Num4/6 muda abas | Num0 fecha o menu";
            case "CONNECTED": return "CONECTADO";
            case "NOT_CONNECTED": return "NÃO CONECTADO";
            case "MEDIA_SESSION": return "SESSÃO DE MÍDIA DO WINDOWS";
            case "NOW_PLAYING": return "TOCANDO AGORA";
            case "YT_START_HINT": return "Num5: ativar no veículo / abrir YT Music";
            case "YT_START_SONG": return "Escolha a música lá. Quando começar, o GTA detecta automaticamente.";
            case "YT_CHOOSE_MUSIC": return "Escolha a música - o GTA detecta a reprodução";
            case "YT_DETECTED": return "Reprodução detectada - YT Music ativo";
            case "YT_STARTING": return "Iniciando reprodução";
            case "YT_OPENED_SELECT": return "YT Music aberto - pressione Reproduzir no YT Music";
            case "BRIDGE_UNAVAILABLE": return "Integração indisponível";
            case "YT_SESSION_ONLINE": return "SESSÃO YT ATIVA";
            case "YT_SESSION_OFFLINE": return "SESSÃO YT INATIVA";
            case "PREV_SHORT": return "ANT.";
            case "NEXT_SHORT": return "PRÓX.";
            case "PAUSE": return "PAUSA";
            case "PREVIOUS_TRACK": return "Faixa anterior";
            case "NEXT_TRACK": return "Próxima faixa";
            case "TRACK_NAV": return "ANT./PRÓX.";
            case "PLAY_PAUSE": return "REPRODUZIR / PAUSAR";
            case "YT_OPEN_FAILED": return "Não foi possível abrir o YouTube Music - verifique o navegador padrão";
            case "OPEN_YT": return "ABRIR / INICIAR";
            case "OPEN_PLAY": return "ABRIR / REPRODUZIR";
            case "SOURCE": return "Fonte";
            case "YT_EXCLUSIVE": return "O YT Music substitui a rádio pela internet enquanto estiver ativo.";
            case "SETTINGS_HINT": return "Num8/2 selecionar | Num5 aplicar | -/+ ajustar";
            case "LANGUAGE": return "IDIOMA";
            case "ARTWORK": return "CAPAS DE MÍDIA";
            case "ARTWORK_NOTE": return "Desativado por padrão; se ativado, as capas ficam em cache apenas localmente durante a sessão e são apagadas ao sair.";
            case "LEGAL": return "AVISO LEGAL";
            case "LEGAL_NOTICE_1": return "Mod não oficial e independente; sem afiliação, patrocínio ou endosso de terceiros.";
            case "LEGAL_NOTICE_2": return "Marcas e mídia pertencem aos seus titulares. Sem capas incluídas; arte opcional, local e temporária.";
            case "LEGAL_NOTICE_3": return "Apenas modo individual; as funções são desativadas se uma sessão do GTA Online for detectada.";
            case "DUCK_LEVEL": return "VOLUME DURANTE DIÁLOGO";
            case "RADIO_OUTPUT": return "LIMITE DE VOLUME RÁDIO";
            case "YT_OUTPUT": return "LIMITE DE VOLUME YT";
            case "VOLUME_STEP": return "PASSO DO VOLUME";
            case "BEAT_NEON": return "NEON REATIVO";
            case "BASS_ANALYZER": return "ANALISADOR DE GRAVES";
            case "BEAT_SENSITIVITY": return "SENSIBILIDADE";
            case "VOICE_FILTER": return "FILTRO DE VOZ";
            case "PULSE_STRENGTH": return "FORÇA DO PULSO";
            case "PULSE_SPEED": return "VELOCIDADE";
            case "NEON_TUNING_NOTE": return "Filtro de transientes: não é um filtro FFT de frequência real.";
            case "ON": return "LIGADO";
            case "OFF": return "DESLIGADO";
            case "RADIO_OUTPUT_SHORT": return "RÁDIO MÁX.";
            case "YT_OUTPUT_SHORT": return "YT MÁX.";
            case "AUDIO": return "ÁUDIO";
            case "DUCK_HOLD": return "TEMPO DO DIÁLOGO";
            case "DUCK_FADE": return "RETORNO DA ATENUAÇÃO";
            case "SETTINGS_NOTE": return "A atenuação mantém o volume baixo entre falas e o restaura suavemente.";
            case "HOLD_VOLUME_NOTE": return "Fora de Configurações: segure Num-/Num+ para alterar o volume continuamente.";
            case "HOLD_PLUS_MINUS": return "SEGURE +/-";
            case "USE_PLUS_MINUS": return "Use Num-/Num+ para alterar o valor";
            case "MENU_HINT": return "Num4/6 aba | Num8/2 seleção | Num5 OK | Espaço favorito";
            case "RADIO_ON": return "InternetRadio LIGADA";
            case "RADIO_OFF": return "InternetRadio DESLIGADA";
            case "NOW_ACTIVE": return "Ativo agora";
            case "CHECK_INI": return "Verifique InternetRadio.ini";
            case "VEHICLE_ONLY": return "Somente em veículo";
            case "VEHICLE_ONLY_NOTE": return "InternetRadio é limitada a veículos";
            case "NO_CYCLE_RADIO": return "Sem rádio em bicicletas";
            case "NO_CYCLE_RADIO_NOTE": return "BMX e bicicletas não suportam InternetRadio";
            case "STATE_PLAYING": return "Tocando";
            case "STATE_BUFFERING": return "Carregando";
            case "STATE_CONNECTING": return "Conectando";
            case "STATE_WAITING": return "Aguardando";
            case "STATE_PAUSED": return "Pausado";
            case "STATE_STOPPED": return "Parado";
            case "STATE_ENDED": return "Finalizado";
            case "STATE_OFF": return "Desligado";
            case "ON_AIR": return "NO AR";
            case "SPACE_KEY": return "ESPAÇO";
            case "MEDIA_SHORT": return "MÍDIA";
            case "YT_OUT_SHORT": return "SAÍDA YT";
            case "STATION_UNAVAILABLE": return "Estação indisponível - pulando";
            case "SPOTIFY_START_HINT": return "Num5: ativar no veículo / abrir Spotify";
            case "SPOTIFY_START_SONG": return "Escolha a música no Spotify. Quando começar, o GTA detecta automaticamente.";
            case "SPOTIFY_DETECTED": return "Reprodução detectada - Spotify ativo";
            case "SPOTIFY_OPENED_SELECT": return "Spotify aberto - pressione Reproduzir no Spotify";
            case "SPOTIFY_OPEN_FAILED": return "Não foi possível abrir o Spotify - verifique a instalação ou o navegador";
            case "SPOTIFY_SESSION_ONLINE": return "SESSÃO SPOTIFY ATIVA";
            case "SPOTIFY_SESSION_OFFLINE": return "SESSÃO SPOTIFY INATIVA";
            case "SPOTIFY_EXCLUSIVE": return "O Spotify substitui a rádio pela internet enquanto estiver ativo.";
            case "SPOTIFY_OUTPUT": return "LIMITE DE VOLUME SPOTIFY";
            case "SPOTIFY_OUTPUT_SHORT": return "SPOTIFY MÁX.";
            case "SPOTIFY_OUT_SHORT": return "SAÍDA SPOTIFY";
            case "OPEN_SPOTIFY": return "ABRIR / INICIAR";
            default: return null;
        }
    }

    private string TranslateTurkish(string key)
    {
        switch (key)
        {
            case "LOADED": return "Yüklendi - dil, ses ve radyo hazır";
            case "STATIONS": return "İSTASYONLAR";
            case "FAVORITES": return "FAVORİLER";
            case "FAVORITE_SHORT": return "FAV +/-";
            case "FAVORITE_ADDED": return "Eklendi";
            case "FAVORITE_REMOVED": return "Kaldırıldı";
            case "FAVORITES_FULL": return "Liste dolu: en fazla 6 istasyon";
            case "NOT_FAVORITE": return "İstasyon favorilerde değil";
            case "NO_FAVORITES": return "Favori yok - bir istasyon seçip Boşluk tuşuna bas";
            case "WORLDWIDE_RADIO": return "DÜNYA ÇAPINDA ÇEVRİM İÇİ RADYO";
            case "HOME": return "ANA SAYFA";
            case "HOME_APPS": return "UYGULAMALAR";
            case "HOME_HINT": return "Num8/2 uygulama seç | Num5 aç";
            case "OPEN": return "AÇ";
            case "INFOTAINMENT": return "MULTİMEDYA & ARAÇ SİSTEMİ";
            case "PACKS": return "KATEGORİLER";
            case "INFO": return "BİLGİ";
            case "SKINS": return "TEMALAR";
            case "SETTINGS": return "AYARLAR";
            case "SETTINGS_NAV": return "AYARLAR";
            case "CAR_SETTINGS": return "ARAÇ AYARLARI";
            case "CAR_SETTINGS_NAV": return "ARAÇ";
            case "CAR_SETTINGS_HINT": return "Num8/2 seç | Num5 aç-kapat | -/+ ayarla";
            case "CAR_SETTINGS_NOTE": return "Beat Neon, iç aydınlatma, plaka ışığı ve otomatik sinyaller burada ayarlanır.";
            case "INTERIOR_LIGHT": return "İÇ AYDINLATMA";
            case "INTERIOR_STRENGTH": return "İÇ IŞIK GÜCÜ";
            case "PLATE_LIGHT": return "PLAKA AYDINLATMASI";
            case "AUTO_INDICATORS": return "OTOMATİK SİNYALLER";
            case "SPEEDOMETER": return "HIZ GÖSTERGESİ";
            case "ROAD_HUD": return "YOL GÖSTERGESİ";
            case "BOAT_HUD": return "DENİZ GÖSTERGESİ";
            case "FLIGHT_HUD": return "UÇUŞ GÖSTERGESİ";
            case "SPEEDO_CAR_ICON": return "ÜRETİCİ LOGOSU";
            case "SPEEDO_RPM_BAR": return "DEVİR ÇUBUĞU";
            case "SPEEDO_UNIT_MODE": return "HIZ BİRİMİ";
            case "NAV_UNIT_MODE": return "DENİZ / HAVA BİRİMİ";
            case "STANDARD": return "STANDART";
            case "METRIC": return "METRİK";
            case "SPEEDO_SIZE": return "GÖSTERGE BOYUTU";
            case "SPEEDO_POS_X": return "X KONUMU";
            case "SPEEDO_POS_Y": return "Y KONUMU";
            case "SPEEDO_OPACITY": return "OPAKLIK";
            case "SPEEDOMETER_NOTE": return "Yol, deniz ve uçuş göstergeleri, birimler, logo, devir çubuğu, boyut, konum ve opaklık buradan ayarlanır.";
            case "CAR_SETTINGS_SCROLL_NOTE": return T("ALT_CONTROL_HINT");
            case "SPEEDO_DRIVE": return "SÜRÜŞ";
            case "SPEEDO_GEAR": return "VİTES";
            case "SPEEDO_RPM": return "DEV/DAK";
            case "SPEEDO_MARINE": return "DENİZ";
            case "SPEEDO_FLIGHT": return "UÇUŞ";
            case "SPEEDO_HEADING": return "ROTA";
            case "SPEEDO_WATER_SPEED": return "SU HIZI";
            case "SPEEDO_GROUND_SPEED": return "YER HIZI";
            case "SPEEDO_ALTITUDE": return "İRTİFA";
            case "SPEEDO_VERTICAL": return "DİKEY HIZ";
            case "SPEEDO_UNIT": return "KM/SA";
            case "MENU": return "MENÜ";
            case "EXIT": return "KAPAT";
            case "POWER": return "AÇ/KAPAT";
            case "STOP": return "DURDUR";
            case "NAV": return "GEZİNME";
            case "DIALOG": return "DİYALOG";
            case "PHONE": return "ARAMA";
            case "TAB": return "Sekme";
            case "TAB_SHORT": return "SEKME";
            case "SETTING_SHORT": return "AYAR";
            case "CHANGE_TAB": return "SEKME DEĞİŞTİR";
            case "SELECT": return "SEÇ";
            case "START": return "BAŞLAT";
            case "APPLY": return "UYGULA";
            case "VALUE": return "DEĞER";
            case "QUIETER": return "AZALT";
            case "LOUDER": return "ARTIR";
            case "SEEK": return "DEĞİŞTİR";
            case "VOLUME": return "SES";
            case "VOLUME_SHORT": return "SES";
            case "DUCKING": return "DİYALOGDA SES AZALTMA";
            case "STATUS": return "DURUM";
            case "PACK": return "KATEGORİ";
            case "VIBE": return "ATMOSFER";
            case "GENRE": return "TÜR";
            case "STATION_COUNT": return "istasyon";
            case "MANUAL": return "MANUEL";
            case "STATION": return "İSTASYON";
            case "PACK_GENRE": return "KATEGORİ / TÜR";
            case "NO_STATION": return "İstasyon yok";
            case "NO_ACTIVE_STATION": return "Aktif istasyon yok";
            case "NO_METADATA": return "Şarkı bilgisi yok";
            case "ALL_STATIONS": return "Tüm istasyonlar";
            case "START_STATION": return "İstasyonu başlat";
            case "PACKS_GROUPS": return "RADYO KATEGORİLERİ";
            case "PACK_HINT": return "Num8/2 kategori seç | Num5 istasyonları göster";
            case "SKINS_THEMES": return "TEMALAR";
            case "SKIN_HINT": return "Num8/2 tema seç | Num5 uygula";
            case "ACTIVE": return "AKTİF";
            case "PREVIEW": return "ÖNİZLEME";
            case "SKIN_HELP": return "Tema arayüzü değiştirir; ses değişmez.";
            case "INFO_HELP": return "Num4/6 sekme değiştirir | Num0 menüyü kapatır";
            case "CONNECTED": return "BAĞLI";
            case "NOT_CONNECTED": return "BAĞLI DEĞİL";
            case "MEDIA_SESSION": return "WINDOWS MEDYA OTURUMU";
            case "NOW_PLAYING": return "ŞİMDİ ÇALIYOR";
            case "YT_START_HINT": return "Num5: araçta etkinleştir / YT Music aç";
            case "YT_START_SONG": return "Müziği oradan seç. Çalma başlayınca GTA otomatik algılar.";
            case "YT_CHOOSE_MUSIC": return "Müziği seç - GTA çalmayı algılar";
            case "YT_DETECTED": return "Çalma algılandı - YT Music aktif";
            case "YT_STARTING": return "Çalma başlatılıyor";
            case "YT_OPENED_SELECT": return "YT Music açıldı - YT Music'te Oynat düğmesine bas";
            case "BRIDGE_UNAVAILABLE": return "Bağlantı köprüsü kullanılamıyor";
            case "YT_SESSION_ONLINE": return "YT OTURUMU ÇEVRİM İÇİ";
            case "YT_SESSION_OFFLINE": return "YT OTURUMU ÇEVRİM DIŞI";
            case "PREV_SHORT": return "ÖNCEKİ";
            case "NEXT_SHORT": return "SONRAKİ";
            case "PAUSE": return "DURAKLAT";
            case "PREVIOUS_TRACK": return "Önceki parça";
            case "NEXT_TRACK": return "Sonraki parça";
            case "TRACK_NAV": return "ÖNCEKİ/SONRAKİ";
            case "PLAY_PAUSE": return "OYNAT / DURAKLAT";
            case "YT_OPEN_FAILED": return "YouTube Music açılamadı - varsayılan tarayıcıyı kontrol et";
            case "OPEN_YT": return "AÇ / BAŞLAT";
            case "OPEN_PLAY": return "AÇ / OYNAT";
            case "SOURCE": return "Kaynak";
            case "YT_EXCLUSIVE": return "YT Music aktifken internet radyosunun yerini alır.";
            case "SETTINGS_HINT": return "Num8/2 seç | Num5 uygula | -/+ ayarla";
            case "LANGUAGE": return "DİL";
            case "ARTWORK": return "MEDYA KAPAKLARI";
            case "ARTWORK_NOTE": return "Varsayılan olarak kapalıdır; etkinleştirilirse kapaklar yalnızca oturum sırasında yerel olarak önbelleğe alınır ve çıkışta silinir.";
            case "LEGAL": return "YASAL UYARI";
            case "LEGAL_NOTICE_1": return "Resmî olmayan bağımsız mod; üçüncü taraflarla bağlantı, sponsorluk veya onay yoktur.";
            case "LEGAL_NOTICE_2": return "Marka ve medya hakları sahiplerine aittir. Kapaklar pakete dahil değildir; görseller isteğe bağlı, yerel ve geçicidir.";
            case "LEGAL_NOTICE_3": return "Yalnızca tek oyunculu mod; GTA Online oturumu algılanırsa özellikler devre dışı bırakılır.";
            case "DUCK_LEVEL": return "DİYALOGDA SES DÜZEYİ";
            case "RADIO_OUTPUT": return "RADYO SES SINIRI";
            case "YT_OUTPUT": return "YT SES SINIRI";
            case "VOLUME_STEP": return "SES ADIMI";
            case "BEAT_NEON": return "SESE DUYARLI NEON";
            case "BASS_ANALYZER": return "BAS ANALİZÖRÜ";
            case "BEAT_SENSITIVITY": return "BEAT HASSASİYETİ";
            case "VOICE_FILTER": return "SES FİLTRESİ";
            case "PULSE_STRENGTH": return "DARBE GÜCÜ";
            case "PULSE_SPEED": return "DARBE HIZI";
            case "NEON_TUNING_NOTE": return "Geçiş filtresidir; gerçek FFT frekans filtresi değildir.";
            case "ON": return "AÇIK";
            case "OFF": return "KAPALI";
            case "RADIO_OUTPUT_SHORT": return "RADYO MAKS.";
            case "YT_OUTPUT_SHORT": return "YT MAKS.";
            case "AUDIO": return "SES";
            case "DUCK_HOLD": return "DİYALOG BEKLEME";
            case "DUCK_FADE": return "SES GERİ DÖNÜŞÜ";
            case "SETTINGS_NOTE": return "Ses kısma, diyalog aralarında sesi düşük tutar ve yumuşakça geri yükseltir.";
            case "HOLD_VOLUME_NOTE": return "Ayarlar dışında: sesi sürekli değiştirmek için Num-/Num+ basılı tut.";
            case "HOLD_PLUS_MINUS": return "+/- BASILI TUT";
            case "USE_PLUS_MINUS": return "Değeri değiştirmek için Num-/Num+ kullan";
            case "MENU_HINT": return "Num4/6 sekme | Num8/2 seçim | Num5 OK | Boşluk favori";
            case "RADIO_ON": return "InternetRadio AÇIK";
            case "RADIO_OFF": return "InternetRadio KAPALI";
            case "NOW_ACTIVE": return "Şimdi aktif";
            case "CHECK_INI": return "InternetRadio.ini dosyasını kontrol et";
            case "VEHICLE_ONLY": return "Sadece araçta";
            case "VEHICLE_ONLY_NOTE": return "InternetRadio yalnızca araçlarda kullanılabilir";
            case "NO_CYCLE_RADIO": return "Bisiklette radyo yok";
            case "NO_CYCLE_RADIO_NOTE": return "BMX ve bisikletler InternetRadio'yu desteklemez";
            case "STATE_PLAYING": return "Çalıyor";
            case "STATE_BUFFERING": return "Yükleniyor";
            case "STATE_CONNECTING": return "Bağlanıyor";
            case "STATE_WAITING": return "Bekliyor";
            case "STATE_PAUSED": return "Duraklatıldı";
            case "STATE_STOPPED": return "Durduruldu";
            case "STATE_ENDED": return "Bitti";
            case "STATE_OFF": return "Kapalı";
            case "ON_AIR": return "YAYINDA";
            case "SPACE_KEY": return "BOŞLUK";
            case "MEDIA_SHORT": return "MEDYA";
            case "YT_OUT_SHORT": return "YT ÇIKIŞ";
            case "STATION_UNAVAILABLE": return "İstasyon kullanılamıyor - atlanıyor";
            case "SPOTIFY_START_HINT": return "Num5: araçta etkinleştir / Spotify aç";
            case "SPOTIFY_START_SONG": return "Müziği Spotify'dan seç. Çalma başlayınca GTA otomatik algılar.";
            case "SPOTIFY_DETECTED": return "Çalma algılandı - Spotify aktif";
            case "SPOTIFY_OPENED_SELECT": return "Spotify açıldı - Spotify'da Oynat düğmesine bas";
            case "SPOTIFY_OPEN_FAILED": return "Spotify açılamadı - kurulumu veya tarayıcıyı kontrol et";
            case "SPOTIFY_SESSION_ONLINE": return "SPOTIFY OTURUMU ÇEVRİM İÇİ";
            case "SPOTIFY_SESSION_OFFLINE": return "SPOTIFY OTURUMU ÇEVRİM DIŞI";
            case "SPOTIFY_EXCLUSIVE": return "Spotify aktifken internet radyosunun yerini alır.";
            case "SPOTIFY_OUTPUT": return "SPOTIFY SES SINIRI";
            case "SPOTIFY_OUTPUT_SHORT": return "SPOTIFY MAKS.";
            case "SPOTIFY_OUT_SHORT": return "SPOTIFY ÇIKIŞ";
            case "OPEN_SPOTIFY": return "AÇ / BAŞLAT";
            default: return null;
        }
    }

    private bool IsExternalMusicModeActive()
    {
        return youtubeModeActive || spotifyModeActive;
    }

    private void RememberYouTubePlaybackState(bool playing)
    {
        if (lastYouTubePlaybackPlaying == playing) return;
        lastYouTubePlaybackPlaying = playing;
        MarkUserSettingsDirty();
        Log("YT PLAYBACK STATE REMEMBERED | " + (playing ? "Playing" : "Paused"));
    }

    private void RememberSpotifyPlaybackState(bool playing)
    {
        if (lastSpotifyPlaybackPlaying == playing) return;
        lastSpotifyPlaybackPlaying = playing;
        MarkUserSettingsDirty();
        Log("SPOTIFY PLAYBACK STATE REMEMBERED | " + (playing ? "Playing" : "Paused"));
    }

    private void CapturePersistentExternalPlaybackState()
    {
        try
        {
            // Only real in-vehicle media state is persistent. Auto-pauses for vehicle exit,
            // death or the GTA pause menu must keep the previous resume intent unchanged.
            if (cachedRadioCapableVehicle && youtubeModeActive && youtubeAvailable && DateTime.Now >= youtubePlaybackCommandGraceUntil)
            {
                if (string.Equals(youtubeState, "Playing", StringComparison.OrdinalIgnoreCase)) RememberYouTubePlaybackState(true);
                else if (string.Equals(youtubeState, "Paused", StringComparison.OrdinalIgnoreCase)) RememberYouTubePlaybackState(false);
            }
            if (cachedRadioCapableVehicle && spotifyModeActive && spotifyAvailable && DateTime.Now >= spotifyPlaybackCommandGraceUntil)
            {
                if (string.Equals(spotifyState, "Playing", StringComparison.OrdinalIgnoreCase)) RememberSpotifyPlaybackState(true);
                else if (string.Equals(spotifyState, "Paused", StringComparison.OrdinalIgnoreCase)) RememberSpotifyPlaybackState(false);
            }
        }
        catch { }
    }

    private bool IsYouTubePlaying()
    {
        return string.Equals(youtubeState, "Playing", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsSpotifyPlaying()
    {
        return string.Equals(spotifyState, "Playing", StringComparison.OrdinalIgnoreCase);
    }

    private void LoadGtaRadioStations()
    {
        gtaRadioStations.Clear();
        try
        {
            Array values = Enum.GetValues(typeof(RadioStation));
            foreach (object value in values)
            {
                RadioStation station = (RadioStation)value;
                if (station == RadioStation.RadioOff) continue;
                string enumName = station.ToString();
                // Self Radio and Media Player are user-content players, not Rockstar broadcast stations.
                if (string.Equals(enumName, "SelfRadio", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(enumName, "MediaPlayer", StringComparison.OrdinalIgnoreCase)) continue;
                gtaRadioStations.Add(new GtaRadioStationEntry { Station = station, Name = GetGtaRadioDisplayName(station, enumName) });
            }
        }
        catch (Exception ex)
        {
            Log("GTA RADIO LIST ERROR | " + ex.Message);
        }
        if (gtaRadioStations.Count > 0)
        {
            gtaRadioIndex = Math.Max(0, Math.Min(gtaRadioStations.Count - 1, gtaRadioIndex));
            gtaRadioMenuIndex = gtaRadioIndex;
        }
    }

    private string GetGtaRadioDisplayName(RadioStation station, string fallback)
    {
        // SHVDN's RadioStation values follow GTA's native radio order. Brand names stay untranslated.
        switch ((int)station)
        {
            case 0: return "Los Santos Rock Radio";
            case 1: return "Non Stop Pop FM";
            case 2: return "Radio Los Santos";
            case 3: return "Channel X";
            case 4: return "West Coast Talk Radio";
            case 5: return "Rebel Radio";
            case 6: return "Soulwax FM";
            case 7: return "East Los FM";
            case 8: return "West Coast Classics";
            case 9: return "Blaine County Radio";
            case 10: return "The Blue Ark";
            case 11: return "Worldwide FM";
            case 12: return "FlyLo FM";
            case 13: return "The Lowdown 91.1";
            case 14: return "Radio Mirror Park";
            case 15: return "Space 103.2";
            case 16: return "Vinewood Boulevard Radio";
            case 17: return "Self Radio";
            case 18: return "The Lab";
            case 19: return "blonded Los Santos 97.8 FM";
            case 20: return "Los Santos Underground Radio";
            case 21: return "iFruit Radio";
            case 22: return "Still Slipping Los Santos";
            case 23: return "Kult FM";
            case 24: return "The Music Locker";
            case 25: return "Media Player";
            case 26: return "MOTOMAMI Los Santos";
        }
        return PrettifyEnumName(fallback);
    }

    private string PrettifyEnumName(string value)
    {
        if (string.IsNullOrEmpty(value)) return T("GTA_RADIO");
        StringBuilder b = new StringBuilder();
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(value[i - 1])) b.Append(' ');
            b.Append(c);
        }
        return b.ToString();
    }

    private string CurrentGtaRadioStationName()
    {
        if (gtaRadioStations.Count == 0) return T("NO_STATION");
        int safe = Math.Max(0, Math.Min(gtaRadioStations.Count - 1, gtaRadioIndex));
        return gtaRadioStations[safe].Name;
    }

    private RadioStation GetCurrentGtaRadioStationEnum()
    {
        if (gtaRadioStations.Count == 0) return RadioStation.RadioOff;
        int safe = Math.Max(0, Math.Min(gtaRadioStations.Count - 1, gtaRadioIndex));
        return gtaRadioStations[safe].Station;
    }

    private void GetGtaRadioLogoSpec(RadioStation station, out string line1, out string line2, out int cr, out int cg, out int cb)
    {
        line1 = "GTA"; line2 = "RADIO";
        cr = 72; cg = 170; cb = 255;

        switch ((int)station)
        {
            case 0: line1 = "LS ROCK"; line2 = "RADIO"; cr = 214; cg = 150; cb = 72; break;
            case 1: line1 = "NON STOP"; line2 = "POP"; cr = 255; cg = 108; cb = 178; break;
            case 2: line1 = "RADIO"; line2 = "LOS SANTOS"; cr = 230; cg = 76; cb = 64; break;
            case 3: line1 = "CHANNEL"; line2 = "X"; cr = 222; cg = 226; cb = 232; break;
            case 4: line1 = "WCTR"; line2 = "TALK"; cr = 76; cg = 162; cb = 255; break;
            case 5: line1 = "REBEL"; line2 = "RADIO"; cr = 228; cg = 176; cb = 96; break;
            case 6: line1 = "SOULWAX"; line2 = "FM"; cr = 108; cg = 235; cb = 214; break;
            case 7: line1 = "EAST LOS"; line2 = "FM"; cr = 84; cg = 204; cb = 108; break;
            case 8: line1 = "WEST COAST"; line2 = "CLASSICS"; cr = 245; cg = 197; cb = 76; break;
            case 9: line1 = "BLAINE"; line2 = "COUNTY"; cr = 210; cg = 162; cb = 88; break;
            case 10: line1 = "BLUE"; line2 = "ARK"; cr = 82; cg = 210; cb = 190; break;
            case 11: line1 = "WORLDWIDE"; line2 = "FM"; cr = 128; cg = 226; cb = 255; break;
            case 12: line1 = "FLYLO"; line2 = "FM"; cr = 255; cg = 130; cb = 214; break;
            case 13: line1 = "LOWDOWN"; line2 = "91.1"; cr = 255; cg = 196; cb = 92; break;
            case 14: line1 = "MIRROR"; line2 = "PARK"; cr = 124; cg = 224; cb = 172; break;
            case 15: line1 = "SPACE"; line2 = "103.2"; cr = 226; cg = 118; cb = 255; break;
            case 16: line1 = "VINEWOOD"; line2 = "BLVD"; cr = 255; cg = 226; cb = 128; break;
            case 18: line1 = "THE"; line2 = "LAB"; cr = 255; cg = 92; cb = 132; break;
            case 19: line1 = "BLONDED"; line2 = "97.8"; cr = 255; cg = 146; cb = 92; break;
            case 20: line1 = "LS"; line2 = "UNDERGROUND"; cr = 196; cg = 108; cb = 255; break;
            case 21: line1 = "IFRUIT"; line2 = "RADIO"; cr = 108; cg = 255; cb = 176; break;
            case 22: line1 = "STILL"; line2 = "SLIPPING"; cr = 255; cg = 144; cb = 208; break;
            case 23: line1 = "KULT"; line2 = "FM"; cr = 180; cg = 160; cb = 255; break;
            case 24: line1 = "MUSIC"; line2 = "LOCKER"; cr = 92; cg = 205; cb = 255; break;
            case 26: line1 = "MOTO"; line2 = "MAMI"; cr = 255; cg = 102; cb = 164; break;
        }
    }

    private string GetGtaRadioGenre(RadioStation station)
    {
        // v7.22: genre is tied directly to the selected GTA station. It is never
        // inferred from logo text or now-playing metadata. Keep these labels short
        // enough for both the station list and the compact side panel.
        switch ((int)station)
        {
            case 0: return "Classic Rock";
            case 1: return "Pop / Dance";
            case 2: return "Hip-Hop / Rap";
            case 3: return "Punk / Hardcore";
            case 4: return "Talk Radio";
            case 5: return "Country / Rock";
            case 6: return "Electronic / Dance";
            case 7: return "Latin / Mexican";
            case 8: return "West Coast Hip-Hop";
            case 9: return "Talk / Community";
            case 10: return "Reggae / Dub";
            case 11: return "World / Electronic";
            case 12: return "Experimental Electronic";
            case 13: return "Soul / Funk";
            case 14: return "Indie / Electronic";
            case 15: return "Funk / R&B";
            case 16: return "Alternative / Indie Rock";
            case 18: return "Electronic / Hip-Hop";
            case 19: return "R&B / Soul / Hip-Hop";
            case 20: return "House / Techno";
            case 21: return "Hip-Hop / Grime";
            case 22: return "House / Dance";
            case 23: return "Alternative / Post-Punk";
            case 24: return "House / Club";
            case 26: return "Latin / Reggaeton";
            default: return "Music";
        }
    }

    private void PrimeGtaRadioHudTextureDictionary()
    {
        try
        {
            Function.Call(Hash.REQUEST_STREAMED_TEXTURE_DICT, "hud", true);
            gtaHudTextureDictionaryRequested = true;
            gtaHudTextureRetryAt = DateTime.UtcNow.AddSeconds(1.0);
        }
        catch
        {
            gtaHudTextureDictionaryRequested = false;
            gtaHudTextureRetryAt = DateTime.UtcNow.AddSeconds(1.0);
        }
    }

    private bool EnsureGtaRadioHudTextureDictionary()
    {
        try
        {
            if (Function.Call<bool>(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED, "hud")) return true;
            if (!gtaHudTextureDictionaryRequested || DateTime.UtcNow >= gtaHudTextureRetryAt)
            {
                Function.Call(Hash.REQUEST_STREAMED_TEXTURE_DICT, "hud", true);
                gtaHudTextureDictionaryRequested = true;
                gtaHudTextureRetryAt = DateTime.UtcNow.AddSeconds(1.0);
            }
        }
        catch { }
        return false;
    }

    private bool GetGtaRadioOriginalLogoUv(RadioStation station, out string textureName,
        out float u1, out float v1, out float u2, out float v2)
    {
        textureName = "";
        u1 = 0.0f; v1 = 0.0f; u2 = 1.0f; v2 = 1.0f;

        int slotX = 1;
        int slotY = 1;
        bool atlas512 = false;

        // Rockstar's original base-station atlas is a 4x4 sheet of 128px cells.
        // Mapping follows the same cells used by hud.gfx for the radio wheel.
        switch ((int)station)
        {
            case 0:  textureName = "gtav_radio_stations_texture_512"; slotX = 4; slotY = 1; atlas512 = true; break; // LS Rock
            case 1:  textureName = "gtav_radio_stations_texture_512"; slotX = 1; slotY = 2; atlas512 = true; break; // Non Stop Pop
            case 2:  textureName = "gtav_radio_stations_texture_512"; slotX = 3; slotY = 3; atlas512 = true; break; // Radio Los Santos
            case 3:  textureName = "gtav_radio_stations_texture_512"; slotX = 3; slotY = 1; atlas512 = true; break; // Channel X
            case 4:  textureName = "gta_radio_stations_texture02_512"; slotX = 2; slotY = 1; atlas512 = true; break; // WCTR
            case 5:  textureName = "gtav_radio_stations_texture_512"; slotX = 1; slotY = 1; atlas512 = true; break; // Rebel
            case 6:  textureName = "gtav_radio_stations_texture_512"; slotX = 2; slotY = 2; atlas512 = true; break; // Soulwax
            case 7:  textureName = "gtav_radio_stations_texture_512"; slotX = 2; slotY = 1; atlas512 = true; break; // East Los
            case 8:  textureName = "gtav_radio_stations_texture_512"; slotX = 1; slotY = 3; atlas512 = true; break; // West Coast Classics
            case 9:  textureName = "gtav_radio_stations_texture_512"; slotX = 3; slotY = 2; atlas512 = true; break; // Blaine County
            case 10: textureName = "gtav_radio_stations_texture_512"; slotX = 4; slotY = 3; atlas512 = true; break; // Blue Ark
            case 11: textureName = "gtav_radio_stations_texture_512"; slotX = 1; slotY = 4; atlas512 = true; break; // Worldwide
            case 12: textureName = "gtav_radio_stations_texture_512"; slotX = 2; slotY = 4; atlas512 = true; break; // FlyLo
            case 13: textureName = "gtav_radio_stations_texture_512"; slotX = 3; slotY = 4; atlas512 = true; break; // Lowdown
            case 14: textureName = "gtav_radio_stations_texture_512"; slotX = 4; slotY = 2; atlas512 = true; break; // Mirror Park
            case 15: textureName = "gta_radio_stations_texture02_512"; slotX = 1; slotY = 1; atlas512 = true; break; // Space 103.2
            case 16: textureName = "gtav_radio_stations_texture_512"; slotX = 2; slotY = 3; atlas512 = true; break; // Vinewood Blvd

            // Rockstar added later stations as individual 128x128 HUD textures.
            case 18: textureName = "gta_radio_stations_texture04_128"; break; // The Lab
            case 19: textureName = "gta_radio_stations_texture05_128"; break; // blonded
            case 20: textureName = "gta_radio_stations_texture06_128"; break; // LS Underground
            case 21: textureName = "gta_radio_stations_texture07_128"; break; // iFruit
            case 22: textureName = "gta_radio_stations_texture08_128"; break; // Still Slipping
            case 23: textureName = "gta_radio_stations_texture09_128"; break; // Kult FM
            case 24: textureName = "gta_radio_stations_texture10_128"; break; // Music Locker
            case 25: textureName = "gta_radio_stations_texture11_128"; break; // Media Player
            case 26: textureName = "gta_radio_stations_texture12_128"; break; // MOTOMAMI
            default: return false;
        }

        if (atlas512)
        {
            const float cell = 0.25f;
            u1 = (slotX - 1) * cell;
            v1 = (slotY - 1) * cell;
            u2 = u1 + cell;
            v2 = v1 + cell;
        }
        return !string.IsNullOrEmpty(textureName);
    }

    private bool TryDrawGtaRadioOriginalGameLogo(float left, float top, float width, float height, RadioStation station, bool active)
    {
        try
        {
            if (!EnsureGtaRadioHudTextureDictionary()) return false;

            string textureName;
            float u1, v1, u2, v2;
            if (!GetGtaRadioOriginalLogoUv(station, out textureName, out u1, out v1, out u2, out v2)) return false;

            // Validate that this build actually exposes the expected texture. This
            // also makes Enhanced/Legacy differences fall back safely instead of
            // producing an empty box.
            GTA.Math.Vector3 resolution = Function.Call<GTA.Math.Vector3>(Hash.GET_TEXTURE_RESOLUTION, "hud", textureName);
            if (resolution.X < 2.0f || resolution.Y < 2.0f) return false;

            float drawH = Math.Max(0.001f, height);
            float drawW = Math.Min(width, PixelSquareWidth(drawH));
            float centerX = left + width * 0.5f;
            float centerY = top + height * 0.5f;
            int alpha = Math.Max(0, Math.Min(255, (int)Math.Round((active ? 255.0f : 238.0f) * drawAlphaMultiplier)));

            Function.Call(
                unchecked((Hash)0x95812F9B26074726UL), // DRAW_SPRITE_ARX_WITH_UV
                "hud", textureName,
                centerX, centerY, drawW, drawH,
                u1, v1, u2, v2,
                0.0f, 255, 255, 255, alpha, 0);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void DrawGtaRadioLogoBadge(float left, float top, float width, float height, RadioStation station, bool active)
    {
        if (TryDrawGtaRadioOriginalGameLogo(left, top, width, height, station, active)) return;

        string line1, line2; int cr, cg, cb;
        GetGtaRadioLogoSpec(station, out line1, out line2, out cr, out cg, out cb);

        int bgR = MixColor(10, cr, active ? 0.18f : 0.11f);
        int bgG = MixColor(14, cg, active ? 0.18f : 0.11f);
        int bgB = MixColor(20, cb, active ? 0.18f : 0.11f);
        int innerR = MixColor(bgR, 255, 0.08f);
        int innerG = MixColor(bgG, 255, 0.08f);
        int innerB = MixColor(bgB, 255, 0.08f);
        int stripeR = MixColor(cr, 245, 0.18f);
        int stripeG = MixColor(cg, 245, 0.18f);
        int stripeB = MixColor(cb, 245, 0.18f);

        DrawRect(left + width / 2f + 0.0011f, top + height / 2f + 0.0016f, width, height, 0, 0, 0, active ? 54 : 42);
        DrawRect(left + width / 2f, top + height / 2f, width, height, 11, 15, 22, active ? 242 : 232);
        DrawRect(left + width / 2f, top + height * 0.30f, width - 0.003f, height * 0.54f, bgR, bgG, bgB, active ? 178 : 154);
        DrawRect(left + 0.0038f, top + height / 2f, 0.0021f, height * 0.66f, stripeR, stripeG, stripeB, active ? 205 : 168);
        DrawRect(left + width * 0.52f, top + height * 0.18f, width * 0.76f, height * 0.10f, innerR, innerG, innerB, active ? 56 : 42);
        DrawFrame(left, top, width, height, cr, cg, cb, active ? 122 : 84, 0.00065f);

        float line1Scale = line1.Length <= 3 ? 0.205f : (line1.Length <= 6 ? 0.175f : 0.148f);
        float line2Scale = line2.Length <= 4 ? 0.135f : (line2.Length <= 8 ? 0.118f : 0.102f);
        if (height < 0.040f)
        {
            line1Scale *= 0.88f;
            line2Scale *= 0.88f;
        }
        DrawTextCentered(Shorten(line1, 12), left + width * 0.53f, top + height * 0.20f, line1Scale, 0, 242, 246, 250, active ? 238 : 222);
        DrawTextCentered(Shorten(line2, 12), left + width * 0.53f, top + height * 0.56f, line2Scale, 0, stripeR, stripeG, stripeB, active ? 224 : 204);
    }

    private void ResetGtaRadioTrackMetadata()
    {
        gtaRadioTrackId = -1;
        gtaRadioArtist = "";
        gtaRadioTitle = "";
        gtaRadioLastMetadataQueryAt = DateTime.MinValue;
        gtaRadioLastMetadataLogId = int.MinValue;
    }

    private string GetGtaRadioTextLabel(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        string value = "";
        try
        {
            // GET_FILENAME_FOR_AUDIO_CONVERSATION / legacy _GET_LABEL_TEXT.
            // This is the same text-table path GTA's own radio wheel uses.
            value = Function.Call<string>((Hash)0x7B5280EBA9840C72UL, key);
        }
        catch { value = ""; }

        if (string.IsNullOrWhiteSpace(value) ||
            string.Equals(value, "NULL", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, key, StringComparison.OrdinalIgnoreCase))
        {
            // SHVDN wrapper fallback. On some Enhanced builds the wrapper can resolve
            // a text label even if the direct string-return native briefly comes back empty.
            try { value = Game.GetLocalizedString(key); } catch { value = ""; }
        }

        if (string.IsNullOrWhiteSpace(value)) return "";
        value = value.Trim();
        if (string.Equals(value, "NULL", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, key, StringComparison.OrdinalIgnoreCase)) return "";
        return value;
    }

    private bool TryResolveGtaRadioTrackLabels(int rawTrackId, out string artist, out string title, out string labelBase)
    {
        artist = "";
        title = "";
        labelBase = "";
        if (rawTrackId == 0 || rawTrackId == -1) return false;

        List<string> candidates = new List<string>();
        string signedText = rawTrackId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        candidates.Add(signedText);

        uint unsignedId = unchecked((uint)rawTrackId);
        string unsignedText = unsignedId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!string.Equals(unsignedText, signedText, StringComparison.Ordinal)) candidates.Add(unsignedText);

        // Older radio text tables usually use plain decimal IDs, but a few tables/builds
        // expose fixed-width numeric labels. Trying these costs virtually nothing because
        // missing GXT labels simply resolve to NULL.
        if (rawTrackId > 0)
        {
            string d4 = rawTrackId.ToString("D4", System.Globalization.CultureInfo.InvariantCulture);
            string d5 = rawTrackId.ToString("D5", System.Globalization.CultureInfo.InvariantCulture);
            string d6 = rawTrackId.ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
            if (!candidates.Contains(d4)) candidates.Add(d4);
            if (!candidates.Contains(d5)) candidates.Add(d5);
            if (!candidates.Contains(d6)) candidates.Add(d6);
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            string candidate = candidates[i];
            string a = GetGtaRadioTextLabel(candidate + "A");
            string t = GetGtaRadioTextLabel(candidate + "S");
            if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(t)) continue;
            artist = a;
            title = t;
            labelBase = candidate;
            return true;
        }
        return false;
    }

    private void UpdateGtaRadioTrackMetadata(bool force)
    {
        if (!gtaRadioModeActive || !gtaRadioPlayingRequested || !cachedRadioCapableVehicle)
        {
            if (!gtaRadioModeActive || !gtaRadioPlayingRequested) ResetGtaRadioTrackMetadata();
            return;
        }

        DateTime now = DateTime.Now;
        if (!force && gtaRadioLastMetadataQueryAt != DateTime.MinValue &&
            (now - gtaRadioLastMetadataQueryAt).TotalMilliseconds < 180.0) return;
        gtaRadioLastMetadataQueryAt = now;

        try
        {
            int trackId = Function.Call<int>((Hash)0x50B196FC9ED6545BUL); // GET_AUDIBLE_MUSIC_TRACK_TEXT_ID
            if (trackId == 0 || trackId == -1)
            {
                if (force) { gtaRadioArtist = ""; gtaRadioTitle = ""; }
                return;
            }

            string artist;
            string title;
            string labelBase;
            bool resolved = TryResolveGtaRadioTrackLabels(trackId, out artist, out title, out labelBase);
            bool changed = trackId != gtaRadioTrackId ||
                !string.Equals(artist, gtaRadioArtist, StringComparison.Ordinal) ||
                !string.Equals(title, gtaRadioTitle, StringComparison.Ordinal);

            gtaRadioTrackId = trackId;
            gtaRadioArtist = resolved ? artist : "";
            gtaRadioTitle = resolved ? title : "";

            // One diagnostic line per raw GTA track id makes Enhanced-specific label
            // issues visible without flooding the log every 180 ms.
            if (gtaRadioLastMetadataLogId != trackId)
            {
                gtaRadioLastMetadataLogId = trackId;
                uint unsignedId = unchecked((uint)trackId);
                Log("GTA RADIO META RAW | Signed=" + trackId + " | Unsigned=" + unsignedId +
                    " | LabelBase=" + (resolved ? labelBase : "MISS") +
                    " | Artist=" + gtaRadioArtist + " | Title=" + gtaRadioTitle);
            }

            if (changed && resolved)
            {
                gtaRadioMiniHideAt = now.AddMilliseconds(4200);
                miniUiFadeUpdatedAt = DateTime.MinValue;
                Log("GTA RADIO TRACK | ID=" + trackId + " | Artist=" + gtaRadioArtist + " | Title=" + gtaRadioTitle);
            }
        }
        catch (Exception ex)
        {
            Log("GTA RADIO METADATA ERROR | " + ex.Message);
        }
    }

    private string GetGtaRadioNowPlayingLine()
    {
        if (!string.IsNullOrEmpty(gtaRadioArtist) && !string.IsNullOrEmpty(gtaRadioTitle))
            return gtaRadioArtist + " · " + gtaRadioTitle;
        if (!string.IsNullOrEmpty(gtaRadioTitle)) return gtaRadioTitle;
        if (!string.IsNullOrEmpty(gtaRadioArtist)) return gtaRadioArtist;
        return "";
    }

    private void MoveGtaRadioSelection(int delta)
    {
        if (gtaRadioStations.Count == 0) return;
        gtaRadioMenuIndex += delta;
        while (gtaRadioMenuIndex < 0) gtaRadioMenuIndex += gtaRadioStations.Count;
        while (gtaRadioMenuIndex >= gtaRadioStations.Count) gtaRadioMenuIndex -= gtaRadioStations.Count;
    }

    private void SyncGtaRadioStationFromGame()
    {
        if (!gtaRadioModeActive || !gtaRadioPlayingRequested || !cachedRadioCapableVehicle || gtaRadioStations.Count == 0) return;
        DateTime now = DateTime.Now;
        if (gtaRadioLastNativeSyncAt != DateTime.MinValue && (now - gtaRadioLastNativeSyncAt).TotalMilliseconds < 180.0) return;
        gtaRadioLastNativeSyncAt = now;
        try
        {
            RadioStation nativeStation = Game.RadioStation;
            if (nativeStation == RadioStation.RadioOff) return;
            for (int i = 0; i < gtaRadioStations.Count; i++)
            {
                if (gtaRadioStations[i].Station != nativeStation) continue;
                if (gtaRadioIndex != i)
                {
                    gtaRadioIndex = i;
                    gtaRadioMenuIndex = i;
                    gtaRadioMiniHideAt = now.AddMilliseconds(4200);
                    miniUiFadeUpdatedAt = DateTime.MinValue;
                    MarkUserSettingsDirty();
                    Log("GTA RADIO NATIVE WHEEL SYNC | " + CurrentGtaRadioStationName());
                }
                break;
            }
        }
        catch { }
    }

    private void ActivateGtaRadioFromMenu()
    {
        if (gtaRadioStations.Count == 0)
        {
            ShowBanner(T("GTA_RADIO"), T("NO_STATION"), 1800);
            return;
        }
        if (!IsRadioCapableVehicle())
        {
            ShowBanner(T("GTA_RADIO"), T("VEHICLE_ONLY_NOTE"), 2200);
            return;
        }
        gtaRadioIndex = Math.Max(0, Math.Min(gtaRadioStations.Count - 1, gtaRadioMenuIndex));
        ActivateGtaRadioMode(true);
        ShowActionFeedback(T("SOURCE"), T("GTA_RADIO"));
    }

    private void ActivateGtaRadioMode(bool showBanner)
    {
        if (gtaRadioStations.Count == 0) return;
        if (!IsRadioCapableVehicle())
        {
            if (showBanner) ShowBanner(T("GTA_RADIO"), T("VEHICLE_ONLY_NOTE"), 2200);
            return;
        }

        StopRadio();
        if (youtubeModeActive && youtube != null)
        {
            youtube.Pause(); youtube.SetManagedActive(false); youtube.RestoreVolume();
        }
        if (spotifyModeActive && spotify != null)
        {
            spotify.Pause(); spotify.SetManagedActive(false); spotify.RestoreVolume();
        }
        try { InternetRadioBrowserVolume.Restore(); } catch { }

        youtubeModeActive = false;
        spotifyModeActive = false;
        youtubeSelectionPending = false;
        spotifySelectionPending = false;
        youtubeAutoPlayPending = false;
        spotifyAutoPlayPending = false;
        gtaRadioModeActive = true;
        gtaRadioPlayingRequested = true;
        preferredVehicleSource = "GTA";
        radioResumeAfterVehicle = false;
        duckActive = false;
        duckReason = "";
        duckMix = 0.0f;
        lastDuckRampAt = DateTime.Now;
        volumeHoldActive = false;
        volumeHoldKey = Keys.None;
        volumeHudVisibleUntil = DateTime.MinValue;
        volumeHudFade = 0.0f;
        ResetGtaRadioTrackMetadata();
        ApplySelectedGtaRadioStation(false);
        UpdateGtaRadioTrackMetadata(true);
        gtaRadioMiniHideAt = DateTime.Now.AddMilliseconds(4200);
        miniUiFadeUpdatedAt = DateTime.MinValue;
        MarkUserSettingsDirty();
        if (showBanner) ShowBanner(T("GTA_RADIO"), CurrentGtaRadioStationName(), 1800);
        Log("GTA RADIO ACTIVE | " + CurrentGtaRadioStationName());
    }

    private void ApplySelectedGtaRadioStation(bool showBanner)
    {
        if (gtaRadioStations.Count == 0) return;
        gtaRadioIndex = Math.Max(0, Math.Min(gtaRadioStations.Count - 1, gtaRadioIndex));
        gtaRadioMenuIndex = gtaRadioIndex;
        if (!gtaRadioModeActive || !gtaRadioPlayingRequested) return;
        if (!IsRadioCapableVehicle()) return;
        try
        {
            Game.RadioStation = gtaRadioStations[gtaRadioIndex].Station;
            // Prevent the just-entered vehicle's previous native station from immediately
            // overwriting our remembered station during the next sync pass.
            gtaRadioLastNativeSyncAt = DateTime.Now;
            ResetGtaRadioTrackMetadata();
            gtaRadioMiniHideAt = DateTime.Now.AddMilliseconds(4200);
            miniUiFadeUpdatedAt = DateTime.MinValue;
            if (showBanner) ShowBanner(T("GTA_RADIO"), CurrentGtaRadioStationName(), 1500);
            Log("GTA RADIO STATION | " + CurrentGtaRadioStationName() + " | Enum=" + gtaRadioStations[gtaRadioIndex].Station);
        }
        catch (Exception ex) { Log("GTA RADIO SET ERROR | " + ex.Message); }
    }

    private void ChangeGtaRadio(int delta)
    {
        if (gtaRadioStations.Count == 0) return;
        if (!gtaRadioModeActive)
        {
            gtaRadioMenuIndex = gtaRadioIndex;
            ActivateGtaRadioMode(false);
        }
        gtaRadioIndex += delta;
        while (gtaRadioIndex < 0) gtaRadioIndex += gtaRadioStations.Count;
        while (gtaRadioIndex >= gtaRadioStations.Count) gtaRadioIndex -= gtaRadioStations.Count;
        gtaRadioMenuIndex = gtaRadioIndex;
        gtaRadioPlayingRequested = true;
        ApplySelectedGtaRadioStation(true);
        MarkUserSettingsDirty();
    }

    private void PauseGtaRadio()
    {
        if (!gtaRadioModeActive) return;
        gtaRadioPlayingRequested = false;
        try { Game.RadioStation = RadioStation.RadioOff; } catch { }
        ResetGtaRadioTrackMetadata();
        gtaRadioMiniHideAt = DateTime.Now.AddMilliseconds(1800);
        miniUiFadeUpdatedAt = DateTime.MinValue;
        ShowBanner(T("GTA_RADIO"), T("STATE_STOPPED"), 1400);
        Log("GTA RADIO STOPPED");
    }

    private void ToggleGtaRadio()
    {
        if (!gtaRadioModeActive)
        {
            ActivateGtaRadioMode(true);
            return;
        }
        gtaRadioPlayingRequested = !gtaRadioPlayingRequested;
        if (gtaRadioPlayingRequested)
        {
            ApplySelectedGtaRadioStation(false);
            UpdateGtaRadioTrackMetadata(true);
            ShowBanner(T("GTA_RADIO"), CurrentGtaRadioStationName(), 1400);
        }
        else
        {
            try { Game.RadioStation = RadioStation.RadioOff; } catch { }
            ResetGtaRadioTrackMetadata();
            ShowBanner(T("GTA_RADIO"), T("STATE_OFF"), 1400);
        }
        gtaRadioMiniHideAt = DateTime.Now.AddMilliseconds(1800);
        miniUiFadeUpdatedAt = DateTime.MinValue;
    }

    private void ActivateInternetRadioFromMenu()
    {
        if (youtubeModeActive && youtube != null)
        {
            youtube.Pause();
            youtube.SetManagedActive(false);
            youtube.RestoreVolume();
        }
        if (spotifyModeActive && spotify != null)
        {
            spotify.Pause();
            spotify.SetManagedActive(false);
            spotify.RestoreVolume();
        }
        try { InternetRadioBrowserVolume.Restore(); } catch { }

        youtubeModeActive = false;
        spotifyModeActive = false;
        ResetGtaRadioTrackMetadata();
        gtaRadioModeActive = false;
        gtaRadioPlayingRequested = false;
        try { Game.RadioStation = RadioStation.RadioOff; } catch { }
        preferredVehicleSource = "RADIO";
        radioResumeAfterVehicle = false;
        ShowActionFeedback(T("SOURCE"), "RADIO");
        youtubeSelectionPending = false;
        spotifySelectionPending = false;
        spotifyAutoPlayPending = false;
        youtubeMiniHideAt = DateTime.MinValue;
        spotifyMiniHideAt = DateTime.MinValue;
        appliedYoutubeVolume = -1;
        appliedYoutubeMixerLevel = -1.0f;
        appliedSpotifyVolume = -1;
        appliedSpotifyMixerLevel = -1.0f;
        duckActive = false;
        duckReason = "";
        duckMix = 0.0f;
        lastDuckRampAt = DateTime.Now;
        index = menuIndex;
        RememberCurrentInternetStation();
        MarkUserSettingsDirty();
        StartRadio();
        ShowBanner("Internet Radio", T("NOW_ACTIVE") + ": " + CurrentStationLine(), 1800);
    }

    private void ActivateYouTubeMode()
    {
        preferredVehicleSource = "YOUTUBE";
        radioResumeAfterVehicle = false;
        MarkUserSettingsDirty();
        ResetGtaRadioTrackMetadata();
        gtaRadioModeActive = false;
        gtaRadioPlayingRequested = false;
        // Re-arm the compact HUD on every explicit YouTube action/source return.
        // Do not depend on artwork or a metadata change: Windows may briefly keep
        // the previous track metadata when switching source or going to previous.
        youtubeMiniHideAt = DateTime.Now.AddMilliseconds(4200);
        miniUiFadeUpdatedAt = DateTime.MinValue;
        if (!youtubeModeActive)
        {
            // Exclusive source switch: stop radio and release Spotify before YT Music takes over.
            StopRadio();
            Game.RadioStation = RadioStation.RadioOff;
            if (spotify != null && (spotifyModeActive || spotifySelectionPending || spotifyAutoPlayPending))
            {
                // Only close Spotify when this mod actually manages or is selecting it.
                // A merely detected background session is left untouched.
                spotify.Pause();
                spotify.SetManagedActive(false);
                spotify.RestoreVolume();
                spotify.CloseManagedApp();
                try { InternetRadioBrowserVolume.Restore(); } catch { }
            }
            spotifyModeActive = false;
            spotifySelectionPending = false;
            spotifyAutoPlayPending = false;
            spotifyMiniHideAt = DateTime.MinValue;
            appliedSpotifyVolume = -1;
            appliedSpotifyMixerLevel = -1.0f;

            youtubeModeActive = true;
            youtubeAutoActivationConsumed = true;
            youtubeMiniHideAt = DateTime.Now.AddMilliseconds(4200);
            enabled = true;
            appliedYoutubeVolume = -1;
            appliedYoutubeMixerLevel = -1.0f;
            if (youtube != null)
            {
                youtube.SetManagedActive(true);
                ApplyEffectiveVolume();
            }
        }
    }

    private void HandleYouTubeSelect(bool showNum5Hint)
    {
        // TEST17: external music may only be opened/started from a radio-capable vehicle.
        // This prevents NUM5 (and fallback start requests) from launching YT Music on foot.
        if (!IsRadioCapableVehicle())
        {
            if (showNum5Hint) ShowYouTubeSelectBanner("YouTube Music", T("VEHICLE_ONLY_NOTE"), 2400);
            Log("YT START BLOCKED | not in radio-capable vehicle");
            return;
        }

        if (youtube == null)
        {
            if (showNum5Hint) ShowYouTubeSelectBanner("YouTube Music", T("BRIDGE_UNAVAILABLE"), 2200);
            return;
        }

        ShowActionFeedback(T("SOURCE"), "YOUTUBE");

        // V12.28: Num5 is an explicit foreground action.
        // Close our menu first so GTA releases slow-motion/focus, then let Windows open
        // YouTube Music visibly. The hidden PowerShell bridge only detects/controls media.
        menuOpen = false;
        RestoreGameTimeScale();
        SaveUserSettings();

        // TEST59: only touch the other bridge when it was actually active/pending/visible.
        // Older builds always launched Spotify here and blocked GTA for 260 ms, even when
        // Spotify had never been used. Strict source filtering makes a main-thread sleep unnecessary.
        bool spotifyNeedsClose = spotifyModeActive || spotifySelectionPending || spotifyAutoPlayPending;
        if (spotify != null && spotifyNeedsClose)
        {
            spotify.Pause();
            spotify.SetManagedActive(false);
            spotify.RestoreVolume();
            spotify.CloseManagedApp();
            try { InternetRadioBrowserVolume.Restore(); } catch { }
            Log("YT HANDOFF | Spotify close queued without blocking GTA");
        }
        spotifyModeActive = false;
        spotifySelectionPending = false;
        spotifyAutoPlayPending = false;
        spotifyMiniHideAt = DateTime.MinValue;
        appliedSpotifyVolume = -1;
        appliedSpotifyMixerLevel = -1.0f;

        bool opened = OpenYouTubeMusicForeground();
        youtubeSelectionPending = true;
        youtube.BeginDetection();

        if (youtubeAvailable)
        {
            ActivateYouTubeMode();
            if (!IsYouTubePlaying()) youtube.Play();
            youtubePlaybackCommandGraceUntil = DateTime.Now.AddMilliseconds(1200);
            RememberYouTubePlaybackState(true);
        }

        if (showNum5Hint)
            ShowYouTubeSelectBanner("YouTube Music", opened ? T("YT_OPENED_SELECT") : T("YT_OPEN_FAILED"), 4200);
        Log("YT MENU FOREGROUND START | ShellOpen=" + opened + " | SessionAvailable=" + youtubeAvailable + " | Num5Hint=" + showNum5Hint);
    }

    private bool OpenYouTubeMusicForeground()
    {
        try
        {
            // V12.29: Do NOT ShellExecute the URL from GTA. That could create a second tab/window
            // while the bridge was also trying to focus/open YouTube Music.
            // The bridge first searches all Chrome/Edge tabs for YouTube Music and focuses the
            // existing tab. Only when none exists is one new tab opened.
            if (youtube == null) return false;
            youtube.OpenHome();
            BringBrowserToForegroundDelayed();
            Log("YT V12.36 OPEN REQUEST | DIRECT URL HANDLER IF NO EXISTING TAB");
            return true;
        }
        catch (Exception ex)
        {
            Log("YT SINGLE INSTANCE REQUEST FEHLER | " + ex.Message);
            return false;
        }
    }

    private void BringBrowserToForegroundDelayed()
    {
        try
        {
            Thread t = new Thread(delegate()
            {
                try
                {
                    // V12.33: focus-only retry loop. This thread NEVER opens a browser/tab,
                    // therefore it cannot create duplicate YouTube Music tabs.
                    string[] names = new string[] { "chrome", "msedge", "firefox" };
                    for (int attempt = 0; attempt < 24; attempt++)
                    {
                        Thread.Sleep(attempt == 0 ? 700 : 220);
                        IntPtr ytWindow = IntPtr.Zero;
                        for (int n = 0; n < names.Length && ytWindow == IntPtr.Zero; n++)
                        {
                            Process[] processes;
                            try { processes = Process.GetProcessesByName(names[n]); }
                            catch { continue; }
                            for (int i = 0; i < processes.Length; i++)
                            {
                                Process proc = processes[i];
                                try
                                {
                                    if (proc.MainWindowHandle == IntPtr.Zero) continue;
                                    string title = "";
                                    try { title = (proc.MainWindowTitle ?? "").ToLowerInvariant(); } catch { }
                                    if (title.Contains("youtube music") || title.Contains("music.youtube.com"))
                                    {
                                        ytWindow = proc.MainWindowHandle;
                                        break;
                                    }
                                }
                                catch { }
                                finally { try { proc.Dispose(); } catch { } }
                            }
                        }
                        if (ytWindow != IntPtr.Zero)
                        {
                            try { ShowWindowAsync(ytWindow, 9); } catch { }
                            Thread.Sleep(100);
                            try { SetForegroundWindow(ytWindow); } catch { }
                            Log("YT FOREGROUND OK | attempt=" + (attempt + 1));
                            return;
                        }
                    }
                    Log("YT FOREGROUND | kein sichtbares YT-Fenster nach Retry");
                }
                catch (Exception ex) { Log("YT FOREGROUND THREAD INTERN | " + ex.Message); }
            });
            t.IsBackground = true;
            t.Name = "InternetRadio-YT-Foreground";
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }
        catch (Exception ex) { Log("YT FOREGROUND THREAD FEHLER | " + ex.Message); }
    }

    private void ToggleYouTubeMusic()
    {
        if (!IsRadioCapableVehicle())
        {
            // TEST20: only an explicit Num5 action is allowed to show the service banner.
            Log("YT CONTROL BLOCKED | not in radio-capable vehicle");
            return;
        }
        if (youtube == null)
        {
            ShowBanner("YouTube Music", T("BRIDGE_UNAVAILABLE"), 2200);
            return;
        }
        if (!youtubeAvailable)
        {
            HandleYouTubeSelect(false);
            return;
        }

        ActivateYouTubeMode();
        youtubeSelectionPending = false;
        if (IsYouTubePlaying())
        {
            youtube.Pause();
            youtubePlaybackCommandGraceUntil = DateTime.Now.AddMilliseconds(1200);
            RememberYouTubePlaybackState(false);
            ShowBanner("YouTube Music", T("STATE_PAUSED"), 1600);
        }
        else
        {
            youtube.Play();
            youtubePlaybackCommandGraceUntil = DateTime.Now.AddMilliseconds(1200);
            RememberYouTubePlaybackState(true);
            ShowBanner("YouTube Music", T("STATE_PLAYING"), 1600);
        }
    }

    private void PauseYouTubeMusic()
    {
        if (!IsRadioCapableVehicle())
        {
            // Silent outside a supported vehicle; Num5 remains the only banner action.
            Log("YT PAUSE BLOCKED | not in radio-capable vehicle");
            return;
        }
        if (youtube == null || !youtubeAvailable)
        {
            ShowBanner("YouTube Music", T("YT_START_HINT"), 1800);
            return;
        }
        ActivateYouTubeMode();
        youtube.Pause();
        youtubePlaybackCommandGraceUntil = DateTime.Now.AddMilliseconds(1200);
        RememberYouTubePlaybackState(false);
        ShowBanner("YouTube Music", T("STATE_PAUSED"), 1600);
    }

    private void YouTubeNext()
    {
        if (!IsRadioCapableVehicle()) return;
        if (youtube == null || !youtubeAvailable)
        {
            ShowBanner("YouTube Music", T("YT_START_HINT"), 1800);
            return;
        }
        ActivateYouTubeMode();
        youtube.Next();
        ShowBanner("YouTube Music", T("NEXT_TRACK"), 1300);
    }

    private void YouTubePrevious()
    {
        if (!IsRadioCapableVehicle()) return;
        if (youtube == null || !youtubeAvailable)
        {
            ShowBanner("YouTube Music", T("YT_START_HINT"), 1800);
            return;
        }
        ActivateYouTubeMode();
        youtube.Previous();
        ShowBanner("YouTube Music", T("PREVIOUS_TRACK"), 1300);
    }

    private void ActivateSpotifyMode()
    {
        preferredVehicleSource = "SPOTIFY";
        radioResumeAfterVehicle = false;
        MarkUserSettingsDirty();
        ResetGtaRadioTrackMetadata();
        gtaRadioModeActive = false;
        gtaRadioPlayingRequested = false;
        // Same behavior as YouTube: every explicit Spotify action/source return
        // re-arms the mini HUD, independent of cover loading or metadata timing.
        spotifyMiniHideAt = DateTime.Now.AddMilliseconds(4200);
        miniUiFadeUpdatedAt = DateTime.MinValue;
        if (!spotifyModeActive)
        {
            StopRadio();
            Game.RadioStation = RadioStation.RadioOff;
            if (youtube != null && (youtubeModeActive || youtubeSelectionPending || youtubeAutoPlayPending))
            {
                // Only close YouTube Music when this mod actually manages or is selecting it.
                // A merely detected background session is left untouched.
                youtube.Pause();
                youtube.SetManagedActive(false);
                youtube.RestoreVolume();
                youtube.CloseManagedApp();
                try { InternetRadioBrowserVolume.Restore(); } catch { }
            }
            youtubeModeActive = false;
            youtubeSelectionPending = false;
            youtubeAutoPlayPending = false;
            youtubeMiniHideAt = DateTime.MinValue;
            appliedYoutubeVolume = -1;
            appliedYoutubeMixerLevel = -1.0f;

            spotifyModeActive = true;
            spotifyMiniHideAt = DateTime.Now.AddMilliseconds(4200);
            enabled = true;
            appliedSpotifyVolume = -1;
            appliedSpotifyMixerLevel = -1.0f;
            if (spotify != null)
            {
                spotify.SetManagedActive(true);
                ApplyEffectiveVolume();
            }
        }
    }

    private void HandleSpotifySelect(bool showNum5Hint)
    {
        // TEST17: same vehicle-only rule as Internet Radio and YouTube Music.
        if (!IsRadioCapableVehicle())
        {
            if (showNum5Hint) ShowSpotifySelectBanner("Spotify", T("VEHICLE_ONLY_NOTE"), 2400);
            Log("SPOTIFY START BLOCKED | not in radio-capable vehicle");
            return;
        }

        if (spotify == null)
        {
            if (showNum5Hint) ShowSpotifySelectBanner("Spotify", T("BRIDGE_UNAVAILABLE"), 2200);
            return;
        }

        ShowActionFeedback(T("SOURCE"), "SPOTIFY");
        menuOpen = false;
        RestoreGameTimeScale();
        SaveUserSettings();

        // TEST59: symmetric lazy hand-off. Do not start/control YouTube unless it was
        // actually in use, and never sleep on GTA's script thread during source selection.
        bool youtubeNeedsClose = youtubeModeActive || youtubeSelectionPending || youtubeAutoPlayPending;
        if (youtube != null && youtubeNeedsClose)
        {
            youtube.Pause();
            youtube.SetManagedActive(false);
            youtube.RestoreVolume();
            youtube.CloseManagedApp();
            try { InternetRadioBrowserVolume.Restore(); } catch { }
            Log("SPOTIFY HANDOFF | YouTube close queued without blocking GTA");
        }
        youtubeModeActive = false;
        youtubeSelectionPending = false;
        youtubeAutoPlayPending = false;
        youtubeMiniHideAt = DateTime.MinValue;
        appliedYoutubeVolume = -1;
        appliedYoutubeMixerLevel = -1.0f;

        bool opened = OpenSpotifyForeground();
        spotifySelectionPending = true;
        spotify.BeginDetection();

        if (spotifyAvailable)
        {
            ActivateSpotifyMode();
            if (!IsSpotifyPlaying()) spotify.Play();
            spotifyPlaybackCommandGraceUntil = DateTime.Now.AddMilliseconds(1200);
            RememberSpotifyPlaybackState(true);
        }

        if (showNum5Hint)
            ShowSpotifySelectBanner("Spotify", opened ? T("SPOTIFY_OPENED_SELECT") : T("SPOTIFY_OPEN_FAILED"), 4200);
        Log("SPOTIFY MENU FOREGROUND START | Open=" + opened + " | SessionAvailable=" + spotifyAvailable + " | Num5Hint=" + showNum5Hint);
    }

    private bool OpenSpotifyForeground()
    {
        try
        {
            if (spotify == null) return false;
            spotify.OpenHome();
            return true;
        }
        catch (Exception ex)
        {
            Log("SPOTIFY OPEN REQUEST FEHLER | " + ex.Message);
            return false;
        }
    }

    private void ToggleSpotifyMusic()
    {
        if (!IsRadioCapableVehicle())
        {
            // TEST20: only an explicit Num5 action is allowed to show the service banner.
            Log("SPOTIFY CONTROL BLOCKED | not in radio-capable vehicle");
            return;
        }
        if (spotify == null)
        {
            ShowBanner("Spotify", T("BRIDGE_UNAVAILABLE"), 2200);
            return;
        }
        if (!spotifyAvailable)
        {
            HandleSpotifySelect(false);
            return;
        }

        ActivateSpotifyMode();
        spotifySelectionPending = false;
        if (IsSpotifyPlaying())
        {
            spotify.Pause();
            spotifyPlaybackCommandGraceUntil = DateTime.Now.AddMilliseconds(1200);
            RememberSpotifyPlaybackState(false);
            ShowBanner("Spotify", T("STATE_PAUSED"), 1600);
        }
        else
        {
            spotify.Play();
            spotifyPlaybackCommandGraceUntil = DateTime.Now.AddMilliseconds(1200);
            RememberSpotifyPlaybackState(true);
            ShowBanner("Spotify", T("STATE_PLAYING"), 1600);
        }
    }

    private void PauseSpotifyMusic()
    {
        if (!IsRadioCapableVehicle())
        {
            // Silent outside a supported vehicle; Num5 remains the only banner action.
            Log("SPOTIFY PAUSE BLOCKED | not in radio-capable vehicle");
            return;
        }
        if (spotify == null || !spotifyAvailable)
        {
            ShowBanner("Spotify", T("SPOTIFY_START_HINT"), 1800);
            return;
        }
        ActivateSpotifyMode();
        spotify.Pause();
        spotifyPlaybackCommandGraceUntil = DateTime.Now.AddMilliseconds(1200);
        RememberSpotifyPlaybackState(false);
        ShowBanner("Spotify", T("STATE_PAUSED"), 1600);
    }

    private void SpotifyNext()
    {
        if (!IsRadioCapableVehicle()) return;
        if (spotify == null || !spotifyAvailable)
        {
            ShowBanner("Spotify", T("SPOTIFY_START_HINT"), 1800);
            return;
        }
        ActivateSpotifyMode();
        spotify.Next();
        ShowBanner("Spotify", T("NEXT_TRACK"), 1300);
    }

    private void SpotifyPrevious()
    {
        if (!IsRadioCapableVehicle()) return;
        if (spotify == null || !spotifyAvailable)
        {
            ShowBanner("Spotify", T("SPOTIFY_START_HINT"), 1800);
            return;
        }
        ActivateSpotifyMode();
        spotify.Previous();
        ShowBanner("Spotify", T("PREVIOUS_TRACK"), 1300);
    }

    private void MoveSkin(int delta)
    {
        if (skinNames.Length == 0) return;
        skinMenuIndex += delta;
        while (skinMenuIndex < 0) skinMenuIndex += skinNames.Length;
        while (skinMenuIndex >= skinNames.Length) skinMenuIndex -= skinNames.Length;
    }

    private void ApplySelectedSkin()
    {
        if (skinNames.Length == 0) return;
        skinIndex = Math.Max(0, Math.Min(skinNames.Length - 1, skinMenuIndex));
        dynamicMenuColorInitialized = false;
        SaveSkinToIni();
        SaveUserSettings();
        ShowBanner(T("SKINS"), T("ACTIVE") + ": " + skinNames[skinIndex], 1800);
        Log("SKIN " + skinNames[skinIndex]);
    }

    private void MovePack(int delta)
    {
        if (menuPacks.Count == 0) return;
        packIndex += delta;
        while (packIndex < 0) packIndex += menuPacks.Count;
        while (packIndex >= menuPacks.Count) packIndex -= menuPacks.Count;
    }

    private void ApplySelectedPack()
    {
        if (menuPacks.Count == 0) return;
        string chosen = menuPacks[Math.Max(0, Math.Min(menuPacks.Count - 1, packIndex))];
        selectedPack = string.Equals(chosen, "ALLE SENDER", StringComparison.OrdinalIgnoreCase) ? "" : chosen;
        RebuildFilteredStations();
        rememberedStationMenuIndex = -1;
        EnsureFilteredMenuIndex();
        menuTab = 1;
        rememberedStationMenuIndex = menuIndex;
        ShowBanner(T("PACKS"), string.IsNullOrEmpty(selectedPack) ? T("ALL_STATIONS") : DisplayPackName(selectedPack), 1800);
    }

    private void SyncPackIndex()
    {
        if (menuPacks.Count == 0) { packIndex = 0; return; }
        string target = string.IsNullOrEmpty(selectedPack) ? "ALLE SENDER" : selectedPack;
        for (int i = 0; i < menuPacks.Count; i++)
        {
            if (string.Equals(menuPacks[i], target, StringComparison.OrdinalIgnoreCase))
            {
                packIndex = i;
                return;
            }
        }
        packIndex = 0;
    }

    private void RebuildMenuPacks()
    {
        menuPacks.Clear();
        menuPacks.Add("ALLE SENDER");
        menuPacks.Add(FavoritesPackKey);
        for (int i = 0; i < stations.Count; i++)
        {
            string pack = stations[i].Pack ?? "";
            if (pack.Length == 0) continue;
            bool exists = false;
            for (int j = 0; j < menuPacks.Count; j++)
            {
                if (string.Equals(menuPacks[j], pack, StringComparison.OrdinalIgnoreCase)) { exists = true; break; }
            }
            if (!exists) menuPacks.Add(pack);
        }
        RebuildFilteredStations();
        SyncPackIndex();
    }

    private void RebuildFilteredStations()
    {
        filteredStationIndices.Clear();
        if (string.Equals(selectedPack, FavoritesPackKey, StringComparison.OrdinalIgnoreCase))
        {
            for (int f = 0; f < favoriteStationNames.Count; f++)
            {
                int favoriteIndex = FindStationIndexByName(favoriteStationNames[f]);
                if (favoriteIndex >= 0 && stations[favoriteIndex].Enabled) filteredStationIndices.Add(favoriteIndex);
            }
            return;
        }

        for (int i = 0; i < stations.Count; i++)
        {
            if (!stations[i].Enabled) continue;
            if (string.IsNullOrEmpty(selectedPack) || string.Equals(stations[i].Pack, selectedPack, StringComparison.OrdinalIgnoreCase))
                filteredStationIndices.Add(i);
        }
    }

    private void EnsureFilteredMenuIndex()
    {
        if (filteredStationIndices.Count == 0) { menuIndex = index; return; }
        for (int i = 0; i < filteredStationIndices.Count; i++)
            if (filteredStationIndices[i] == menuIndex) return;
        menuIndex = filteredStationIndices[0];
    }

    private void MoveFilteredMenu(int delta)
    {
        if (filteredStationIndices.Count == 0) return;
        int pos = 0;
        for (int i = 0; i < filteredStationIndices.Count; i++)
        {
            if (filteredStationIndices[i] == menuIndex) { pos = i; break; }
        }
        pos += delta;
        while (pos < 0) pos += filteredStationIndices.Count;
        while (pos >= filteredStationIndices.Count) pos -= filteredStationIndices.Count;
        menuIndex = filteredStationIndices[pos];
    }

    private int CountStationsInPack(string pack)
    {
        if (string.Equals(pack, FavoritesPackKey, StringComparison.OrdinalIgnoreCase)) return favoriteStationNames.Count;
        int count = 0;
        for (int i = 0; i < stations.Count; i++)
        {
            if (!stations[i].Enabled) continue;
            if (string.Equals(pack, "ALLE SENDER", StringComparison.OrdinalIgnoreCase) || string.Equals(stations[i].Pack, pack, StringComparison.OrdinalIgnoreCase))
                count++;
        }
        return count;
    }

    private int FindStationIndexByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return -1;
        for (int i = 0; i < stations.Count; i++)
            if (string.Equals(stations[i].Name, name, StringComparison.OrdinalIgnoreCase)) return i;
        return -1;
    }

    private bool IsFavorite(int stationIndex)
    {
        if (stationIndex < 0 || stationIndex >= stations.Count) return false;
        string name = stations[stationIndex].Name;
        for (int i = 0; i < favoriteStationNames.Count; i++)
            if (string.Equals(favoriteStationNames[i], name, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private void ToggleFavorite(int stationIndex)
    {
        if (stationIndex < 0 || stationIndex >= stations.Count) return;
        if (IsFavorite(stationIndex))
        {
            RemoveFavorite(stationIndex);
            return;
        }
        if (favoriteStationNames.Count >= MaxFavorites)
        {
            ShowBanner(T("FAVORITES"), T("FAVORITES_FULL"), 2200);
            return;
        }
        favoriteStationNames.Add(stations[stationIndex].Name);
        RebuildFilteredStations();
        SaveUserSettings();
        ShowBanner(T("FAVORITES"), T("FAVORITE_ADDED") + ": " + stations[stationIndex].Name + " (" + favoriteStationNames.Count + "/" + MaxFavorites + ")", 2200);
        Log("FAVORITE ADDED | " + stations[stationIndex].Name + " | Count=" + favoriteStationNames.Count);
    }

    private void RemoveFavorite(int stationIndex)
    {
        if (stationIndex < 0 || stationIndex >= stations.Count) return;
        string name = stations[stationIndex].Name;
        int removeAt = -1;
        for (int i = 0; i < favoriteStationNames.Count; i++)
            if (string.Equals(favoriteStationNames[i], name, StringComparison.OrdinalIgnoreCase)) { removeAt = i; break; }
        if (removeAt < 0)
        {
            ShowBanner(T("FAVORITES"), T("NOT_FAVORITE"), 1600);
            return;
        }
        favoriteStationNames.RemoveAt(removeAt);
        RebuildFilteredStations();
        EnsureFilteredMenuIndex();
        SaveUserSettings();
        ShowBanner(T("FAVORITES"), T("FAVORITE_REMOVED") + ": " + name + " (" + favoriteStationNames.Count + "/" + MaxFavorites + ")", 2200);
        Log("FAVORITE REMOVED | " + name + " | Count=" + favoriteStationNames.Count);
    }

    private void SanitizeFavorites()
    {
        bool changed = false;
        for (int i = favoriteStationNames.Count - 1; i >= 0; i--)
        {
            if (FindStationIndexByName(favoriteStationNames[i]) < 0)
            {
                favoriteStationNames.RemoveAt(i);
                changed = true;
                continue;
            }
            for (int j = 0; j < i; j++)
            {
                if (string.Equals(favoriteStationNames[j], favoriteStationNames[i], StringComparison.OrdinalIgnoreCase))
                {
                    favoriteStationNames.RemoveAt(i);
                    changed = true;
                    break;
                }
            }
        }
        while (favoriteStationNames.Count > MaxFavorites)
        {
            favoriteStationNames.RemoveAt(favoriteStationNames.Count - 1);
            changed = true;
        }
        if (changed) MarkUserSettingsDirty();
    }

    // TEST60: the first tab is a real infotainment home screen. Num8/Num2
    // moves through the seven app tiles; Num5 opens the selected module.
    private void MoveHomeSelection(int delta)
    {
        homeMenuIndex += delta;
        while (homeMenuIndex < 0) homeMenuIndex += 7;
        while (homeMenuIndex >= 7) homeMenuIndex -= 7;
    }

    private void OpenHomeSelection()
    {
        int targetTab = 1;
        if (homeMenuIndex == 1) targetTab = 9;
        else if (homeMenuIndex == 2) targetTab = 5;
        else if (homeMenuIndex == 3) targetTab = 6;
        else if (homeMenuIndex == 4) targetTab = 7;
        else if (homeMenuIndex == 5) targetTab = 4;
        else if (homeMenuIndex == 6) targetTab = 8;

        RememberCurrentMenuPosition();
        menuTab = targetTab;
        RestoreMenuPositionForCurrentTab();
        ShowBanner(T("TAB"), GetMenuTabName(), 1000);
    }

    private void MoveMenu(int delta)
    {
        if (stations.Count == 0) return;
        int count = stations.Count;

        // Wrap around at both ends, but never loop forever if every entry is disabled.
        int attempts = 0;
        do
        {
            menuIndex += delta;
            while (menuIndex < 0) menuIndex += count;
            while (menuIndex >= count) menuIndex -= count;
            attempts++;
        }
        while (attempts < count && !stations[menuIndex].Enabled);
    }

    private bool IsCycleVehicle()
    {
        try
        {
            Ped player = Game.LocalPlayerPed;
            if (player == null) return false;

            // TEST29: BMX/bicycle detection is deliberately redundant. Some game/SHVDN
            // combinations can be inconsistent about IsInVehicle() while the player is
            // mounted on a bicycle, so do not rely on that single check.
            int vehicleHandle = 0;
            try { vehicleHandle = Function.Call<int>(Hash.GET_VEHICLE_PED_IS_IN, player.Handle, false); } catch { }

            if (vehicleHandle != 0)
            {
                try
                {
                    // GTA vehicle class 13 = Cycles.
                    if (Function.Call<int>(Hash.GET_VEHICLE_CLASS, vehicleHandle) == 13) return true;
                }
                catch { }

                try
                {
                    int modelHash = Function.Call<int>(Hash.GET_ENTITY_MODEL, vehicleHandle);
                    // IS_THIS_MODEL_A_BICYCLE - raw native keeps this compatible even if
                    // the friendly Hash enum name differs between SHVDN builds.
                    if (modelHash != 0 && Function.Call<bool>((Hash)0xBF94DD42F63BDED2UL, modelHash)) return true;
                }
                catch { }
            }

            // Final v3 wrapper fallback. This also distinguishes bicycles from motorcycles.
            try
            {
                Vehicle currentVehicle = player.CurrentVehicle;
                if (currentVehicle != null && currentVehicle.IsBicycle) return true;
            }
            catch { }

            return false;
        }
        catch { return false; }
    }

    private bool IsPlayerDeadOrDying()
    {
        try
        {
            Ped player = Game.LocalPlayerPed;
            if (player == null) return true;
            return Function.Call<bool>(Hash.IS_ENTITY_DEAD, player.Handle, false);
        }
        catch
        {
            return false;
        }
    }

    private bool IsRadioCapableVehicle()
    {
        try
        {
            Ped player = Game.LocalPlayerPed;
            return player != null && player.IsInVehicle() && !IsCycleVehicle();
        }
        catch { return false; }
    }

    // Audio-reactive vehicle lighting is intentionally limited to road vehicles.
    // Radio/media playback may still work in other supported vehicles, but we do not
    // pulse neon/cabin lights on bicycles, boats, helicopters, planes or trains.
    private bool IsAudioReactiveLightingVehicle()
    {
        try
        {
            Ped player = Game.LocalPlayerPed;
            if (player == null) return false;

            // Explicitly reject BMX/bicycles before the generic in-vehicle/class checks.
            // This closes the edge case where bicycle state/class reporting is delayed.
            if (IsCycleVehicle()) return false;
            if (!player.IsInVehicle()) return false;

            int vehicleHandle = Function.Call<int>(Hash.GET_VEHICLE_PED_IS_IN, player.Handle, false);
            if (vehicleHandle == 0) return false;

            int vehicleClass = Function.Call<int>(Hash.GET_VEHICLE_CLASS, vehicleHandle);
            switch (vehicleClass)
            {
                case 13: // Cycles
                case 14: // Boats
                case 15: // Helicopters
                case 16: // Planes
                case 21: // Trains
                    return false;
                default:
                    return true;
            }
        }
        catch { return false; }
    }

    private void StartRadio(bool showBanner = true)
    {
        if (IsExternalMusicModeActive() || gtaRadioModeActive)
        {
            // Safety guard against double audio: another managed source owns playback.
            if (audio != null) audio.Stop();
            shouldPlay = false;
            return;
        }
        if (!enabled) return;
        if (IsCycleVehicle())
        {
            StopRadio();
            ShowBanner(T("NO_CYCLE_RADIO"), T("NO_CYCLE_RADIO_NOTE"), 2200);
            return;
        }
        if (onlyInVehicle && !IsRadioCapableVehicle())
        {
            ShowBanner(T("VEHICLE_ONLY"), T("VEHICLE_ONLY_NOTE"), 2000);
            return;
        }
        Station s = Current();
        if (s == null)
        {
            ShowBanner(T("NO_STATION"), T("CHECK_INI"), 2500);
            return;
        }
        Game.RadioStation = RadioStation.RadioOff;
        shouldPlay = true;
        radioLastHealthyAt = DateTime.Now;
        radioFailureHandled = false;
        lastMetadataAnnouncement = "";
        suppressNextMetadataBanner = !showBanner;
        if (!showBanner)
        {
            bannerTop = "";
            bannerBottom = "";
            bannerUntil = DateTime.MinValue;
        }
        int startUserVolume = duckActive ? (int)Math.Round(volume * (duckVolumePercent / 100.0)) : volume;
        startUserVolume = Math.Max(0, Math.Min(100, startUserVolume));
        int startPlayerVolume = MapUserVolumeToPlayer(startUserVolume);
        appliedStreamVolume = startPlayerVolume;
        if (audio != null) audio.Play(s.Url, startPlayerVolume);
        Log("VOLUME MAP | UI=" + startUserVolume + "% -> Player=" + startPlayerVolume + "%");
        // Mini UI is intentionally decoupled from ShowBanner(). General banners are disabled,
        // but the radio mini UI should still appear when playback starts (including silent auto-start).
        if (showMiniUi) miniUiVisibleUntil = DateTime.Now.AddMilliseconds(showBanner ? 4600 : 4200);
        if (showBanner) ShowBanner(s.Name, StationDetailsLine(s), 2600);
        Log("PLAY " + s.Name + " | " + s.Url + (showBanner ? "" : " | SILENT AUTO START"));
    }

    private void CheckRadioStreamHealth()
    {
        if (!autoSkipUnavailable || !shouldPlay || IsExternalMusicModeActive() || audio == null) return;
        if (onlyInVehicle && !IsRadioCapableVehicle()) return;

        string state = (cachedAudioState ?? "").Trim();
        DateTime now = DateTime.Now;
        if (state.Equals("Playing", StringComparison.OrdinalIgnoreCase))
        {
            radioLastHealthyAt = now;
            radioFailureHandled = false;
            return;
        }

        // Buffering/Transitioning/Reconnecting are legitimate temporary states.
        if (state.Equals("Buffering", StringComparison.OrdinalIgnoreCase) ||
            state.Equals("Transitioning", StringComparison.OrdinalIgnoreCase) ||
            state.Equals("Reconnecting", StringComparison.OrdinalIgnoreCase)) return;

        if (radioLastHealthyAt == DateTime.MinValue) radioLastHealthyAt = now;
        if (radioFailureHandled) return;
        if ((now - radioLastHealthyAt).TotalMilliseconds < streamFailTimeoutMs) return;

        bool failedState = state.Equals("Ready", StringComparison.OrdinalIgnoreCase) ||
                           state.Equals("Stopped", StringComparison.OrdinalIgnoreCase) ||
                           state.Equals("MediaEnded", StringComparison.OrdinalIgnoreCase) ||
                           state.Equals("Undefined", StringComparison.OrdinalIgnoreCase) ||
                           state.Equals("AudioFehler", StringComparison.OrdinalIgnoreCase);
        if (!failedState) return;

        radioFailureHandled = true;
        Station failed = Current();
        string failedName = failed != null ? failed.Name : T("STATION");
        Log("STREAM UNAVAILABLE | " + failedName + " | State=" + state + " | Timeout=" + streamFailTimeoutMs + "ms");

        int oldIndex = index;
        Change(1);
        Station next = Current();
        string nextName = next != null ? next.Name : T("NO_STATION");
        ShowBanner(failedName, T("STATION_UNAVAILABLE") + " -> " + nextName, 3500);
        Log("AUTO SKIP | " + failedName + " -> " + nextName + " | FromIndex=" + oldIndex + " ToIndex=" + index);
    }

    private void StopRadio()
    {
        shouldPlay = false;
        radioFailureHandled = false;
        radioLastHealthyAt = DateTime.MinValue;
        lastMetadataAnnouncement = "";
        duckActive = false;
        duckReason = "";
        duckMix = 0.0f;
        lastDuckRampAt = DateTime.Now;
        appliedStreamVolume = -1;
        if (audio != null) audio.Stop();
    }

    private void Change(int d)
    {
        if (stations.Count == 0) return;
        int tries = 0;
        do
        {
            index += d;
            if (index < 0) index = stations.Count - 1;
            if (index >= stations.Count) index = 0;
            tries++;
        }
        while (tries < stations.Count && (!stations[index].Enabled || string.IsNullOrEmpty(stations[index].Url)));

        menuIndex = index;
        preferredVehicleSource = "RADIO";
        RememberCurrentInternetStation();
        MarkUserSettingsDirty();
        if (shouldPlay) StartRadio();
        else
        {
            Station s = Current();
            if (s != null) ShowBanner(s.Name, StationDetailsLine(s), 2200);
        }
    }

    private Station Current()
    {
        if (stations.Count == 0) return null;
        if (index < 0 || index >= stations.Count) index = 0;
        if (stations[index].Enabled && !string.IsNullOrEmpty(stations[index].Url)) return stations[index];
        for (int i = 0; i < stations.Count; i++)
        {
            if (stations[i].Enabled && !string.IsNullOrEmpty(stations[i].Url))
            {
                index = i;
                return stations[i];
            }
        }
        return null;
    }

    private void LoadIni()
    {
        string path = Path.Combine(dir, "InternetRadio.ini");
        if (!File.Exists(path)) throw new FileNotFoundException("InternetRadio.ini missing", path);

        string section = "";
        Dictionary<string, Dictionary<string, string>> ini = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (string raw in File.ReadAllLines(path))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#")) continue;
            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                section = line.Substring(1, line.Length - 2).Trim();
                if (!ini.ContainsKey(section)) ini[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                continue;
            }
            int p = line.IndexOf('=');
            if (p > 0 && section.Length > 0)
                ini[section][line.Substring(0, p).Trim()] = line.Substring(p + 1).Trim();
        }

        volume = Math.Max(0, Math.Min(100, GetInt(ini, "General", "Volume", 45)));
        int audioProfileVersion = GetInt(ini, "General", "AudioProfileVersion", 0);
        uiLanguage = NormalizeLanguage(Get(ini, "General", "Language", "EN"));
        playerMaxVolume = Math.Max(1, Math.Min(100, GetInt(ini, "General", "PlayerMaxVolume", 70)));
        volumeCurveExponent = Math.Max(1.0f, Math.Min(3.0f, GetFloat(ini, "General", "VolumeCurveExponent", 1.10f)));
        youtubeMaxVolume = Math.Max(5, Math.Min(100, GetInt(ini, "General", "YouTubeMaxVolume", 55)));
        youtubeVolumeCurveExponent = Math.Max(0.8f, Math.Min(2.8f, GetFloat(ini, "General", "YouTubeVolumeCurveExponent", 1.55f)));
        spotifyMaxVolume = Math.Max(5, Math.Min(100, GetInt(ini, "General", "SpotifyMaxVolume", 55)));
        spotifyVolumeCurveExponent = Math.Max(0.8f, Math.Min(2.8f, GetFloat(ini, "General", "SpotifyVolumeCurveExponent", 1.55f)));
        // V12.24: YouTube Music is intentionally user-controlled.
        // Legacy AutoLaunch/AutoPlay behavior is ignored for a predictable UX.
        autoLaunchYouTubeMusic = false;
        autoActivateYouTubeMusic = false;
        autoPlayYouTubeMusic = false;
        volumeStep = 1;
        onlyInVehicle = GetBool(ini, "General", "OnlyInVehicle", true);
        autoStart = GetBool(ini, "General", "AutoStart", true);
        autoSkipUnavailable = GetBool(ini, "General", "AutoSkipUnavailable", true);
        streamFailTimeoutMs = Math.Max(5000, Math.Min(20000, GetInt(ini, "General", "StreamFailTimeoutMs", 9000)));
        enabled = GetBool(ini, "General", "Enabled", true);
        showMiniUi = GetBool(ini, "General", "ShowMiniUi", true);
        persistentMiniUi = GetBool(ini, "General", "PersistentMiniUi", false);
        hideMiniUiWhileMenuOpen = GetBool(ini, "General", "HideMiniUiWhileMenuOpen", true);
        menuOpenSoundEnabled = GetBool(ini, "General", "MenuOpenSound", true);
        // V12.40: Spectrum is the dynamic color skin, not a global background toggle.
        // Accept the V12.39 strength key as a backwards-compatible fallback.
        dynamicRadioBackgroundStrength = Math.Max(0.25f, Math.Min(1.00f,
            GetFloat(ini, "General", "SpectrumSkinStrength",
                GetFloat(ini, "General", "RainbowSkinStrength",
                    GetFloat(ini, "General", "DynamicRadioBackgroundStrength", 0.72f)))));
        menuSlowMotionEnabled = GetBool(ini, "General", "MenuSlowMotion", true);
        menuTimeScale = Math.Max(0.20f, Math.Min(1.00f, GetFloat(ini, "General", "MenuTimeScale", 0.45f)));
        miniUiLeft = GetFloat(ini, "General", "MiniUiLeft", 0.73f);
        miniUiTop = GetFloat(ini, "General", "MiniUiTop", 0.64f);
        miniUiWidth = GetFloat(ini, "General", "MiniUiWidth", 0.235f);
        miniUiHeight = GetFloat(ini, "General", "MiniUiHeight", 0.122f);
        showCustomSpeedometer = GetBool(ini, "Cars", "ShowCustomSpeedometer", GetBool(ini, "General", "ShowCustomSpeedometer", true));
        speedometerShowCarIcon = GetBool(ini, "Cars", "SpeedometerShowCarIcon", true);
        speedometerShowRpm = GetBool(ini, "Cars", "SpeedometerShowRpm", true);
        speedometerUseMph = string.Equals(Get(ini, "Cars", "SpeedometerUnit", "KMH").Trim(), "MPH", StringComparison.OrdinalIgnoreCase);
        showBoatHud = GetBool(ini, "Cars", "ShowBoatHud", true);
        showFlightHud = GetBool(ini, "Cars", "ShowFlightHud", true);
        navigationUseMetric = string.Equals(Get(ini, "Cars", "NavigationUnits", "STANDARD").Trim(), "METRIC", StringComparison.OrdinalIgnoreCase);
        automaticIndicatorsEnabled = GetBool(ini, "Cars", "AutomaticIndicators", true);
        automaticEmergencyHazardsEnabled = false; // Hazards are manual-only; kept as a legacy field for settings compatibility.
        speedometerLeft = GetFloat(ini, "Cars", "SpeedometerLeft", GetFloat(ini, "General", "SpeedometerLeft", 0.760f));
        speedometerTop = GetFloat(ini, "Cars", "SpeedometerTop", GetFloat(ini, "General", "SpeedometerTop", 0.826f));
        speedometerWidth = GetFloat(ini, "Cars", "SpeedometerWidth", GetFloat(ini, "General", "SpeedometerWidth", 0.205f));
        speedometerHeight = GetFloat(ini, "Cars", "SpeedometerHeight", GetFloat(ini, "General", "SpeedometerHeight", 0.126f));
        speedometerOpacity = Math.Max(20, Math.Min(100, GetInt(ini, "Cars", "SpeedometerOpacity", GetInt(ini, "General", "SpeedometerOpacity", 92))));
        audioReactiveNeonEnabled = GetBool(ini, "Cars", "AudioReactiveNeon", audioReactiveNeonEnabled);
        neonBeatSensitivity = Math.Max(1, Math.Min(10, GetInt(ini, "Cars", "NeonBeatSensitivity", neonBeatSensitivity)));
        neonVoiceFilter = Math.Max(0, Math.Min(100, GetInt(ini, "Cars", "NeonVoiceFilter", neonVoiceFilter)));
        neonPulseStrength = Math.Max(10, Math.Min(100, GetInt(ini, "Cars", "NeonPulseStrength", neonPulseStrength)));
        neonPulseSpeed = Math.Max(10, Math.Min(100, GetInt(ini, "Cars", "NeonPulseSpeed", neonPulseSpeed)));
        audioReactiveInteriorLightEnabled = GetBool(ini, "Cars", "AudioReactiveInteriorLight", audioReactiveInteriorLightEnabled);
        interiorLightStrength = Math.Max(10, Math.Min(100, GetInt(ini, "Cars", "InteriorLightStrength", interiorLightStrength)));
        plateLightEnabled = GetBool(ini, "Cars", "PlateLightEnabled", true);
        // v7.37: prefer the boolean setting, but migrate old v7.32-v7.35 mode values
        // automatically (any legacy mode > 0 becomes ON).
        turboImpactEnabled = GetBool(ini, "Cars", "TurboImpactEnabled", GetInt(ini, "Cars", "TurboImpactMode", 2) > 0);
        factoryTurboModelsConfigured = Get(ini, "Cars", "FactoryTurboModels", "");
        RebuildFactoryTurboModelHashes();
        speechDucking = GetBool(ini, "General", "SpeechDucking", true);
        phoneCallDucking = GetBool(ini, "General", "PhoneCallDucking", true);
        menuDucking = GetBool(ini, "General", "MenuDucking", true);
        metadataEnabled = GetBool(ini, "General", "MetadataEnabled", true);
        duckVolumePercent = Math.Max(5, Math.Min(100, GetInt(ini, "General", "DuckVolumePercent", 70)));
        if (audioProfileVersion < 4)
        {
            // V12.24: louder, more linear and easier to use while keeping GTA audio separate.
            playerMaxVolume = 70;
            volumeCurveExponent = 1.10f;
            youtubeMaxVolume = 55;
            youtubeVolumeCurveExponent = 1.55f;
            spotifyMaxVolume = 55;
            spotifyVolumeCurveExponent = 1.55f;
            duckVolumePercent = 70;
            SaveGeneralValue("AudioProfileVersion", "4");
            SaveGeneralValue("PlayerMaxVolume", playerMaxVolume.ToString());
            SaveGeneralValue("VolumeCurveExponent", volumeCurveExponent.ToString(System.Globalization.CultureInfo.InvariantCulture));
            SaveGeneralValue("YouTubeMaxVolume", youtubeMaxVolume.ToString());
            SaveGeneralValue("YouTubeVolumeCurveExponent", youtubeVolumeCurveExponent.ToString(System.Globalization.CultureInfo.InvariantCulture));
            SaveGeneralValue("SpotifyMaxVolume", spotifyMaxVolume.ToString());
            SaveGeneralValue("SpotifyVolumeCurveExponent", spotifyVolumeCurveExponent.ToString(System.Globalization.CultureInfo.InvariantCulture));
            SaveGeneralValue("DuckVolumePercent", duckVolumePercent.ToString());
        SaveGeneralValue("VolumeStep", volumeStep.ToString());
            Log("AUDIO PROFILE MIGRATION V4 | RadioMax=70 | YTMax=55 | Duck=70");
        }
        int duckProfileVersion = Math.Max(0, GetInt(ini, "General", "DuckingProfileVersion", 0));
        duckReleaseMs = Math.Max(500, Math.Min(4000, GetInt(ini, "General", "DuckReleaseMs", 1800)));
        duckAttackMs = Math.Max(150, Math.Min(1200, GetInt(ini, "General", "DuckAttackMs", 350)));
        duckFadeOutMs = Math.Max(200, Math.Min(2000, GetInt(ini, "General", "DuckFadeOutMs", 900)));
        if (duckProfileVersion < 2)
        {
            // V12.30: smooth conversation ducking. Keep music down through natural gaps between lines.
            duckReleaseMs = 1800;
            duckAttackMs = 350;
            duckFadeOutMs = 900;
            SaveGeneralValue("DuckingProfileVersion", "2");
            SaveGeneralValue("DuckReleaseMs", duckReleaseMs.ToString());
            SaveGeneralValue("DuckAttackMs", duckAttackMs.ToString());
            SaveGeneralValue("DuckFadeOutMs", duckFadeOutMs.ToString());
            Log("DUCKING PROFILE MIGRATION V2 | Hold=1800ms | Attack=350ms | ReleaseFade=900ms");
        }
        uiTitle = Get(ini, "General", "UiTitle", "INTERNET RADIO LS");
        skinIndex = GetSkinIndex(Get(ini, "General", "Skin", "MODERN GREEN"));
        skinMenuIndex = skinIndex;
        LoadUserSettingsOverrides();
        if (youtubeVolumeProfileVersion < 1)
        {
            // V12.54: one-time migration from the old very loud 1:1 YouTube mapping.
            // This intentionally runs after UserSettings.ini so legacy YTMax=100 cannot override it.
            youtubeMaxVolume = 55;
            youtubeVolumeCurveExponent = 1.55f;
            youtubeVolumeProfileVersion = 1;
            appliedYoutubeVolume = -1;
            appliedYoutubeMixerLevel = -1.0f;
            SaveGeneralValue("YouTubeMaxVolume", youtubeMaxVolume.ToString());
            SaveGeneralValue("YouTubeVolumeCurveExponent", youtubeVolumeCurveExponent.ToString(System.Globalization.CultureInfo.InvariantCulture));
            SaveUserSettings();
            Log("YT VOLUME PROFILE V1 | Max=55 | Curve=1.55 | smooth mixer enabled");
        }
        int start = GetInt(ini, "General", "StartStation", 1);

        toggleKey = ParseKey(Get(ini, "Controls", "ToggleKey", "NumPad1"), Keys.NumPad1);
        prevKey = ParseKey(Get(ini, "Controls", "PreviousStationKey", "NumPad4"), Keys.NumPad4);
        nextKey = ParseKey(Get(ini, "Controls", "NextStationKey", "NumPad6"), Keys.NumPad6);
        reloadKey = ParseKey(Get(ini, "Controls", "ReloadConfigKey", "F8"), Keys.F8);
        menuKey = ParseKey(Get(ini, "Controls", "MenuKey", "NumPad0"), Keys.NumPad0);
        menuUpKey = ParseKey(Get(ini, "Controls", "MenuUpKey", "NumPad8"), Keys.NumPad8);
        menuDownKey = ParseKey(Get(ini, "Controls", "MenuDownKey", "NumPad2"), Keys.NumPad2);
        selectKey = ParseKey(Get(ini, "Controls", "SelectKey", "NumPad5"), Keys.NumPad5);
        volumeDownKey = ParseKey(Get(ini, "Controls", "VolumeDownKey", "Subtract"), Keys.Subtract);
        volumeUpKey = ParseKey(Get(ini, "Controls", "VolumeUpKey", "Add"), Keys.Add);
        stopKey = ParseKey(Get(ini, "Controls", "StopKey", "NumPad3"), Keys.NumPad3);
        hazardKey = ParseKey(Get(ini, "Controls", "HazardKey", "NumPad7"), Keys.NumPad7);
        highBeamKey = ParseKey(Get(ini, "Controls", "HighBeamKey", "NumPad9"), Keys.NumPad9);

        stations.Clear();
        for (int i = 1; i <= 80; i++)
        {
            string sec = "Station" + i;
            if (!ini.ContainsKey(sec)) continue;
            string url = Get(ini, sec, "Url", "");
            if (url.Length == 0) continue;
            stations.Add(new Station
            {
                Name = Get(ini, sec, "Name", "Sender " + i),
                Url = url,
                Enabled = GetBool(ini, sec, "Enabled", true),
                Genre = Get(ini, sec, "Genre", "Mix"),
                Region = Get(ini, sec, "Region", ""),
                Vibe = Get(ini, sec, "Vibe", ""),
                Pack = Get(ini, sec, "Pack", "Freie Sender")
            });
        }

        SanitizeFavorites();
        int fallbackStationIndex = Math.Max(0, Math.Min(Math.Max(0, stations.Count - 1), start - 1));
        index = ResolveRememberedInternetStationIndex(fallbackStationIndex);
        while (stations.Count > 0 && (!stations[index].Enabled || string.IsNullOrEmpty(stations[index].Url))) index = (index + 1) % stations.Count;
        menuIndex = index;
        RememberCurrentInternetStation();
        RebuildMenuPacks();
        Log("INI loaded | Stations=" + stations.Count + " | RestoredStation=" + index + " | Source=" + preferredVehicleSource + " | Categories=" + Math.Max(0, menuPacks.Count - 2) + " | Favorites=" + favoriteStationNames.Count);
    }

    private static Keys ParseKey(string text, Keys fallback)
    {
        try
        {
            if (string.IsNullOrEmpty(text)) return fallback;
            return (Keys)Enum.Parse(typeof(Keys), text, true);
        }
        catch { return fallback; }
    }

    private static string Get(Dictionary<string, Dictionary<string, string>> ini, string sec, string key, string fallback)
    {
        Dictionary<string, string> s; string v;
        return ini.TryGetValue(sec, out s) && s.TryGetValue(key, out v) ? v : fallback;
    }

    private static int GetInt(Dictionary<string, Dictionary<string, string>> ini, string sec, string key, int fallback)
    {
        int v; return int.TryParse(Get(ini, sec, key, fallback.ToString()), out v) ? v : fallback;
    }

    private static float GetFloat(Dictionary<string, Dictionary<string, string>> ini, string sec, string key, float fallback)
    {
        float v; return float.TryParse(Get(ini, sec, key, fallback.ToString(System.Globalization.CultureInfo.InvariantCulture)), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v) ? v : fallback;
    }

    private static bool GetBool(Dictionary<string, Dictionary<string, string>> ini, string sec, string key, bool fallback)
    {
        bool v; return bool.TryParse(Get(ini, sec, key, fallback ? "true" : "false"), out v) ? v : fallback;
    }

    private int GetSkinIndex(string name)
    {
        if (string.IsNullOrEmpty(name)) return 0;
        string normalized = name.Trim();
        // V12.44 compatibility: existing V12.40-V12.43 user settings used RAINBOW.
        // Keep those installs seamless while presenting the improved name SPECTRUM.
        if (string.Equals(normalized, "RAINBOW", StringComparison.OrdinalIgnoreCase)) normalized = "SPECTRUM";
        // TEST71 public-release migration from older theme names that referenced third-party titles.
        if (string.Equals(normalized, "GTA VICE CITY", StringComparison.OrdinalIgnoreCase)) normalized = "NEON SUNSET";
        else if (string.Equals(normalized, "GTA SAN ANDREAS", StringComparison.OrdinalIgnoreCase)) normalized = "WEST COAST";
        else if (string.Equals(normalized, "CYBERPUNK NEON", StringComparison.OrdinalIgnoreCase)) normalized = "FUTURE NEON";
        else if (string.Equals(normalized, "NFS UNDERGROUND 2", StringComparison.OrdinalIgnoreCase)) normalized = "STREET TUNER";
        else if (string.Equals(normalized, "MINECRAFT", StringComparison.OrdinalIgnoreCase)) normalized = "BLOCK WORLD";
        else if (string.Equals(normalized, "GANGSTER LUXE", StringComparison.OrdinalIgnoreCase)) normalized = "NOIR LUXE";
        for (int i = 0; i < skinNames.Length; i++)
            if (string.Equals(skinNames[i], normalized, StringComparison.OrdinalIgnoreCase)) return i;
        return 0;
    }

    private void GetSkinAccent(out int r, out int g, out int b)
    {
        switch (skinIndex)
        {
            case 1: r = 67; g = 158; b = 255; break;      // OEM Blue
            case 2: r = 245; g = 72; b = 72; break;       // Red Sport
            case 3: r = 244; g = 174; b = 64; break;      // Amber Classic
            case 4: r = 225; g = 230; b = 236; break;     // Minimal White
            case 5:
                UpdateSpectrumPalette();
                r = spectrumPrimaryR; g = spectrumPrimaryG; b = spectrumPrimaryB;
                break;                                      // Spectrum
            case 6: r = 255; g = 124; b = 196; break;     // Neon Sunset
            case 7: r = 126; g = 201; b = 71; break;      // West Coast
            case 8: r = 0; g = 229; b = 255; break;       // Future Neon: cyan HUD selection
            case 9: r = 157; g = 231; b = 67; break;      // NFSU2: acid green selection
            case 10: r = 109; g = 182; b = 72; break;     // Block World green accent
            case 11: r = 212; g = 178; b = 98; break;     // Noir Luxe champagne gold
            case 12: r = 255; g = 166; b = 205; break;    // Sakura Zen soft blossom pink
            default: r = 61; g = 238; b = 105; break;     // Modern Green
        }
    }

    private void GetSkinBase(out int r, out int g, out int b)
    {
        switch (skinIndex)
        {
            case 1: r = 6; g = 12; b = 23; break;
            case 2: r = 20; g = 8; b = 10; break;
            case 3: r = 19; g = 13; b = 7; break;
            case 4: r = 13; g = 15; b = 18; break;
            case 5: r = 8; g = 9; b = 14; break;
            case 6: r = 10; g = 20; b = 38; break;
            case 7: r = 18; g = 13; b = 9; break;
            case 8: r = 5; g = 7; b = 12; break;
            case 9: r = 12; g = 13; b = 14; break;
            case 10: r = 14; g = 15; b = 14; break;
            case 11: r = 6; g = 6; b = 8; break;
            case 12: r = 18; g = 10; b = 18; break;
            default: r = 6; g = 9; b = 13; break;
        }
    }

    private void GetSkinPanel(out int r, out int g, out int b)
    {
        switch (skinIndex)
        {
            case 1: r = 10; g = 20; b = 34; break;
            case 2: r = 28; g = 12; b = 15; break;
            case 3: r = 28; g = 20; b = 10; break;
            case 4: r = 22; g = 24; b = 28; break;
            case 5: r = 14; g = 15; b = 23; break;
            case 6: r = 17; g = 32; b = 57; break;
            case 7: r = 28; g = 23; b = 16; break;
            case 8: r = 9; g = 13; b = 20; break;
            case 9: r = 27; g = 28; b = 30; break;
            case 10: r = 20; g = 22; b = 20; break;
            case 11: r = 16; g = 15; b = 18; break;
            case 12: r = 32; g = 19; b = 31; break;
            default: r = 10; g = 14; b = 19; break;
        }
    }

    private void GetSkinOled(out int r, out int g, out int b)
    {
        switch (skinIndex)
        {
            case 1: r = 3; g = 18; b = 38; break;
            case 2: r = 31; g = 5; b = 8; break;
            case 3: r = 32; g = 20; b = 4; break;
            case 4: r = 18; g = 20; b = 23; break;
            case 5: r = 11; g = 12; b = 20; break;
            case 6: r = 8; g = 20; b = 44; break;
            case 7: r = 12; g = 27; b = 15; break;
            case 8: r = 3; g = 8; b = 14; break;
            case 9: r = 15; g = 16; b = 17; break;
            case 10: r = 16; g = 22; b = 16; break;
            case 11: r = 11; g = 9; b = 11; break;
            case 12: r = 25; g = 12; b = 24; break;
            default: r = 2; g = 24; b = 12; break;
        }
    }

    // V12.44: secondary accent makes the premium themes visually distinct instead
    // of being one shared layout with only a different primary color.
    private void GetSkinSecondaryAccent(out int r, out int g, out int b)
    {
        switch (skinIndex)
        {
            case 5:
                UpdateSpectrumPalette();
                r = spectrumSecondaryR; g = spectrumSecondaryG; b = spectrumSecondaryB;
                break;                                      // Spectrum rotating secondary
            case 6: r = 57; g = 221; b = 255; break;      // Vice City: cyan opposite the sunset pink
            case 7: r = 214; g = 174; b = 76; break;     // West Coast: aged gold / warm street tone
            case 8: r = 228; g = 48; b = 47; break;      // Future Neon: red interface rails against cyan
            case 9: r = 198; g = 204; b = 207; break;    // NFSU2: brushed chrome against acid green
            case 10: r = 126; g = 88; b = 56; break;      // Block World earth accent
            case 11: r = 104; g = 18; b = 30; break;      // Noir Luxe: deep bordeaux accent against gold
            case 12: r = 255; g = 114; b = 176; break;    // Sakura Zen vivid blossom secondary
            default: GetSkinAccent(out r, out g, out b); break;
        }
    }

    private void GetThemeFrameAccent(out int r, out int g, out int b)
    {
        switch (skinIndex)
        {
            case 5:
                UpdateSpectrumPalette();
                r = spectrumPrimaryR; g = spectrumPrimaryG; b = spectrumPrimaryB;
                break;
            case 9: r = 198; g = 204; b = 207; break;     // NFSU2 uses a chrome outer frame
            case 10: r = 95; g = 140; b = 68; break;      // Block World keeps a softer green-border tone
            case 12: r = 248; g = 223; b = 233; break;    // Sakura Zen pearl-pink frame
            default: GetSkinAccent(out r, out g, out b); break;
        }
    }

    private void GetThemeFrameSecondary(out int r, out int g, out int b)
    {
        switch (skinIndex)
        {
            case 5:
                UpdateSpectrumPalette();
                r = spectrumSecondaryR; g = spectrumSecondaryG; b = spectrumSecondaryB;
                break;
            case 6: r = 57; g = 221; b = 255; break;
            case 7: r = 214; g = 174; b = 76; break;
            case 8: r = 228; g = 48; b = 47; break;
            case 9: r = 157; g = 231; b = 67; break;
            case 10: r = 126; g = 88; b = 56; break;
            case 11: r = 104; g = 18; b = 30; break;
            case 12: r = 255; g = 114; b = 176; break;
            default: GetSkinAccent(out r, out g, out b); break;
        }
    }

    private bool IsPremiumThemeSkin()
    {
        // TEST61: Spectrum now participates in the premium frame/decor path too.
        return skinIndex == 5 || (skinIndex >= 6 && skinIndex <= 12);
    }

    private void HsvToRgb(float hue, float saturation, float value, out int r, out int g, out int b)
    {
        hue = hue - (float)Math.Floor(hue);
        saturation = Math.Max(0f, Math.Min(1f, saturation));
        value = Math.Max(0f, Math.Min(1f, value));

        float h = hue * 6f;
        int sector = (int)Math.Floor(h);
        float f = h - sector;
        float p = value * (1f - saturation);
        float q = value * (1f - saturation * f);
        float t = value * (1f - saturation * (1f - f));

        float rf = value, gf = t, bf = p;
        switch (sector % 6)
        {
            case 0: rf = value; gf = t;     bf = p;     break;
            case 1: rf = q;     gf = value; bf = p;     break;
            case 2: rf = p;     gf = value; bf = t;     break;
            case 3: rf = p;     gf = q;     bf = value; break;
            case 4: rf = t;     gf = p;     bf = value; break;
            case 5: rf = value; gf = p;     bf = q;     break;
        }

        r = ClampColor((int)Math.Round(rf * 255f));
        g = ClampColor((int)Math.Round(gf * 255f));
        b = ClampColor((int)Math.Round(bf * 255f));
    }

    private void UpdateSpectrumPalette()
    {
        if (!IsSpectrumSkin()) return;

        int now = Environment.TickCount & int.MaxValue;
        if (spectrumPaletteLastTick >= 0 && now - spectrumPaletteLastTick < 100) return;
        spectrumPaletteLastTick = now;

        // Calm but visibly moving Spectrum: one coherent colour family with a
        // 45-second full hue cycle. This is quick enough to notice in-game while
        // avoiding the old station/category jumps and frantic multi-colour feel.
        float hue = (now % 45000) / 45000.0f;
        HsvToRgb(hue,          0.40f, 0.91f, out spectrumPrimaryR,   out spectrumPrimaryG,   out spectrumPrimaryB);
        HsvToRgb(hue + 0.025f, 0.37f, 0.89f, out spectrumSecondaryR, out spectrumSecondaryG, out spectrumSecondaryB);
        HsvToRgb(hue + 0.050f, 0.35f, 0.87f, out spectrumTertiaryR,  out spectrumTertiaryG,  out spectrumTertiaryB);
        HsvToRgb(hue + 0.075f, 0.36f, 0.88f, out spectrumWarmR,      out spectrumWarmG,      out spectrumWarmB);
    }

    private void GetSpectrumPhaseColorCached(int phase, out int r, out int g, out int b)
    {
        switch (((phase % 4) + 4) % 4)
        {
            case 1: r = spectrumSecondaryR; g = spectrumSecondaryG; b = spectrumSecondaryB; break;
            case 2: r = spectrumTertiaryR;  g = spectrumTertiaryG;  b = spectrumTertiaryB;  break;
            case 3: r = spectrumWarmR;      g = spectrumWarmG;      b = spectrumWarmB;      break;
            default:r = spectrumPrimaryR;   g = spectrumPrimaryG;   b = spectrumPrimaryB;   break;
        }
    }

    private void GetSpectrumPhaseColor(int phase, out int r, out int g, out int b)
    {
        UpdateSpectrumPalette();
        GetSpectrumPhaseColorCached(phase, out r, out g, out b);
    }

    private void DrawSpectrumRail(float left, float centerY, float width, float height, int alpha, int phase)
    {
        if (!IsSpectrumSkin()) return;
        UpdateSpectrumPalette();

        // Blend only between very close neighbouring colours. Ten small segments
        // make the rail read as one soft accent rather than a moving rainbow strip.
        const int segments = 10;
        int r1, g1, b1, r2, g2, b2;
        GetSpectrumPhaseColorCached(phase, out r1, out g1, out b1);
        GetSpectrumPhaseColorCached(phase + 1, out r2, out g2, out b2);
        float segmentW = width / segments;
        for (int i = 0; i < segments; i++)
        {
            float t = segments <= 1 ? 0.0f : (float)i / (segments - 1);
            int r = MixColor(r1, r2, t);
            int g = MixColor(g1, g2, t);
            int b = MixColor(b1, b2, t);
            DrawRect(left + segmentW * (i + 0.5f), centerY, segmentW + 0.0004f, height, r, g, b, Math.Min(alpha, 150));
        }
    }

    private void DrawSpectrumFrame(float left, float top, float width, float height, int alpha, float thickness, int phase)
    {
        if (!IsSpectrumSkin()) return;
        UpdateSpectrumPalette();

        // TEST62: panel frames are intentionally single-colour. Rainbow rails now
        // live only at deliberate highlight locations, which removes the noisy
        // 'every edge is a rainbow' appearance from TEST61.
        int r, g, b;
        GetSpectrumPhaseColorCached(phase, out r, out g, out b);
        DrawFrame(left, top, width, height, r, g, b, Math.Min(alpha, 135), thickness);
        if (alpha >= 175 && thickness >= 0.0010f)
        {
            int sr = MixColor(r, 235, 0.20f);
            int sg = MixColor(g, 240, 0.20f);
            int sb = MixColor(b, 248, 0.20f);
            DrawRect(left + width / 2f, top + thickness * 1.6f, width - thickness * 5.0f, Math.Max(0.00045f, thickness * 0.36f), sr, sg, sb, 72);
        }
    }

    private void LoadUiIconSprites()
    {
        for (int i = 0; i < uiIconSprites.Length && i < uiIconAssetNames.Length; i++)
        {
            uiIconAutoOffsetX[i] = 0.0f;
            uiIconAutoOffsetY[i] = 0.0f;
            uiIconAutoScale[i] = 1.0f;
            try
            {
                string path = Path.Combine(dir, uiIconAssetNames[i]);
                if (!File.Exists(path))
                {
                    uiIconSprites[i] = null;
                    continue;
                }
                int spriteAlpha = (i == 5 || i == 6) ? 255 : 220;
                uiIconSprites[i] = new GTA.UI.CustomSprite(
                    path,
                    new System.Drawing.SizeF(128f, 128f),
                    new System.Drawing.PointF(0f, 0f),
                    System.Drawing.Color.FromArgb(spriteAlpha, 255, 255, 255));
                LoadUiIconOpticalMetrics(i, path);
            }
            catch
            {
                uiIconSprites[i] = null;
                uiIconAutoOffsetX[i] = 0.0f;
                uiIconAutoOffsetY[i] = 0.0f;
                uiIconAutoScale[i] = 1.0f;
            }
        }
        Log("TEST71 UI ICONS READY | neutral anti-aliased PNG assets loaded");
    }

    private void LoadUiIconOpticalMetrics(int icon, string path)
    {
        try
        {
            using (var bmp = new System.Drawing.Bitmap(path))
            {
                int w = bmp.Width;
                int h = bmp.Height;
                int minX = w, minY = h, maxX = -1, maxY = -1;
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (bmp.GetPixel(x, y).A <= 16) continue;
                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                    }
                }
                if (maxX < minX || maxY < minY) return;
                float cx = ((minX + maxX + 1) * 0.5f) / Math.Max(1f, (float)w);
                float cy = ((minY + maxY + 1) * 0.5f) / Math.Max(1f, (float)h);
                float visibleW = (maxX - minX + 1) / Math.Max(1f, (float)w);
                float visibleH = (maxY - minY + 1) / Math.Max(1f, (float)h);
                float visibleMax = Math.Max(0.01f, Math.Max(visibleW, visibleH));
                // PNG assets are pre-centered on their canvas; keep renderer translation at zero
                // so sidebar/compact tiles are geometrically centered in-game.
                uiIconAutoOffsetX[icon] = 0.0f;
                uiIconAutoOffsetY[icon] = 0.0f;
                uiIconAutoScale[icon] = Math.Max(0.96f, Math.Min(1.16f, 0.84f / visibleMax));
            }
        }
        catch
        {
            uiIconAutoOffsetX[icon] = 0.0f;
            uiIconAutoOffsetY[icon] = 0.0f;
            uiIconAutoScale[icon] = 1.0f;
        }
    }

    private bool DrawUiIconSprite(int icon, float left, float top, float size, int requestedAlpha)
    {
        if (icon < 0 || icon >= uiIconSprites.Length || uiIconSprites[icon] == null) return false;
        try
        {
            // 'size' is the intended icon height in normalized coordinates. Convert
            // the width so the square PNG stays square in physical screen pixels.
            float aspectFix = (float)GTA.UI.Screen.Height / Math.Max(1f, (float)GTA.UI.Screen.Width);
            float drawW = size * aspectFix;
            float drawLeft = left + drawW * GetUiIconOpticalOffsetX(icon);
            float drawTop = top + size * GetUiIconOpticalOffsetY(icon);
            uiIconSprites[icon].Size = new System.Drawing.SizeF(drawW * GTA.UI.Screen.Width, size * GTA.UI.Screen.Height);
            uiIconSprites[icon].Position = new System.Drawing.PointF(drawLeft * GTA.UI.Screen.Width, drawTop * GTA.UI.Screen.Height);
            int spriteBaseAlpha = (icon == 5 || icon == 6) ? 255 : 220;
            int localAlpha = Math.Max(0, Math.Min(255, requestedAlpha));
            int spriteAlpha = Math.Max(0, Math.Min(255, (int)Math.Round(Math.Min(spriteBaseAlpha, localAlpha) * drawAlphaMultiplier)));
            uiIconSprites[icon].Color = System.Drawing.Color.FromArgb(spriteAlpha, 255, 255, 255);
            uiIconSprites[icon].Draw();
            if (icon >= 0 && icon < uiIconDrawCountThisFrame.Length) uiIconDrawCountThisFrame[icon]++;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private float PixelSquareWidth(float normalizedHeight)
    {
        return normalizedHeight * ((float)GTA.UI.Screen.Height / Math.Max(1f, (float)GTA.UI.Screen.Width));
    }

    private void DrawMediaCoverShell(float left, float top, float width, float height, int r, int g, int b, bool active)
    {
        // TEST63: calmer premium cover frame for the new media-card artwork.
        DrawRect(left + width / 2f + 0.0020f, top + height / 2f + 0.0028f, width + 0.004f, height + 0.005f, 0, 0, 0, 72);
        DrawRect(left + width / 2f, top + height / 2f, width + 0.0026f, height + 0.0026f, 9, 13, 18, 234);
        DrawFrame(left - 0.0012f, top - 0.0012f, width + 0.0024f, height + 0.0024f, r, g, b, active ? 118 : 58, 0.0008f);
        DrawRect(left + width / 2f, top + 0.0010f, width * 0.82f, 0.0009f, MixColor(r, 246, 0.24f), MixColor(g, 246, 0.24f), MixColor(b, 246, 0.24f), active ? 48 : 24);
    }

    private void LoadBlockWorldMotifSprite()
    {
        try
        {
            string path = Path.Combine(dir, "BlockWorldMotif.png");
            if (!File.Exists(path))
            {
                blockWorldMotifSprite = null;
                Log("BLOCK WORLD ASSET FEHLT | " + path);
                return;
            }
            blockWorldMotifSprite = new GTA.UI.CustomSprite(
                path,
                new System.Drawing.SizeF(128f, 128f),
                new System.Drawing.PointF(0f, 0f),
                System.Drawing.Color.White);
            Log("BLOCK WORLD ASSET READY | " + path);
        }
        catch (Exception ex)
        {
            blockWorldMotifSprite = null;
            Log("BLOCK WORLD ASSET FEHLER | " + ex.Message);
        }
    }

    private bool DrawBlockWorldMotifSprite(float left, float top, float width, float height)
    {
        if (blockWorldMotifSprite == null) return false;
        try
        {
            blockWorldMotifSprite.Size = new System.Drawing.SizeF(width * GTA.UI.Screen.Width, height * GTA.UI.Screen.Height);
            blockWorldMotifSprite.Position = new System.Drawing.PointF(left * GTA.UI.Screen.Width, top * GTA.UI.Screen.Height);
            blockWorldMotifSprite.Draw();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void LoadSakuraZenMotifSprite()
    {
        try
        {
            string path = Path.Combine(dir, "SakuraZenMotif.png");
            if (!File.Exists(path))
            {
                sakuraZenMotifSprite = null;
                Log("SAKURA ZEN MOTIF ASSET FEHLT | " + path);
                return;
            }
            sakuraZenMotifSprite = new GTA.UI.CustomSprite(
                path,
                new System.Drawing.SizeF(512f, 128f),
                new System.Drawing.PointF(0f, 0f),
                System.Drawing.Color.White);
            Log("SAKURA ZEN MOTIF READY | " + path);
        }
        catch (Exception ex)
        {
            sakuraZenMotifSprite = null;
            Log("SAKURA ZEN MOTIF FEHLER | " + ex.Message);
        }
    }

    private bool DrawSakuraZenMotifSprite(float left, float top, float width, float height)
    {
        if (sakuraZenMotifSprite == null) return false;
        try
        {
            sakuraZenMotifSprite.Size = new System.Drawing.SizeF(width * GTA.UI.Screen.Width, height * GTA.UI.Screen.Height);
            sakuraZenMotifSprite.Position = new System.Drawing.PointF(left * GTA.UI.Screen.Width, top * GTA.UI.Screen.Height);
            sakuraZenMotifSprite.Draw();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private GTA.UI.CustomSprite LoadPremiumHeaderMotif(string fileName, string logName)
    {
        try
        {
            string path = Path.Combine(dir, fileName);
            if (!File.Exists(path)) return null;
            GTA.UI.CustomSprite sprite = new GTA.UI.CustomSprite(
                path,
                new System.Drawing.SizeF(1024f, 320f),
                new System.Drawing.PointF(0f, 0f),
                System.Drawing.Color.White);
            Log(logName + " READY | " + path);
            return sprite;
        }
        catch (Exception ex)
        {
            Log(logName + " ERROR | " + ex.Message);
            return null;
        }
    }

    private bool DrawPremiumHeaderMotif(GTA.UI.CustomSprite sprite, float left, float top, float width, float height)
    {
        if (sprite == null) return false;
        try
        {
            sprite.Size = new System.Drawing.SizeF(width * GTA.UI.Screen.Width, height * GTA.UI.Screen.Height);
            sprite.Position = new System.Drawing.PointF(left * GTA.UI.Screen.Width, top * GTA.UI.Screen.Height);
            sprite.Draw();
            return true;
        }
        catch { return false; }
    }

    private void LoadSpectrumMotifSprite()
    {
        spectrumMotifSprite = LoadPremiumHeaderMotif("SpectrumMotif.png", "SPECTRUM HEADER");
    }

    private bool DrawSpectrumMotifSprite(float left, float top, float width, float height)
    {
        return DrawPremiumHeaderMotif(spectrumMotifSprite, left, top, width, height);
    }

    private void LoadWestCoastMotifSprite()
    {
        westCoastMotifSprite = LoadPremiumHeaderMotif("WestCoastMotif.png", "WEST COAST HEADER");
    }

    private bool DrawWestCoastMotifSprite(float left, float top, float width, float height)
    {
        return DrawPremiumHeaderMotif(westCoastMotifSprite, left, top, width, height);
    }

    private void LoadStreetTunerMotifSprite()
    {
        streetTunerMotifSprite = LoadPremiumHeaderMotif("StreetTunerMotif.png", "STREET TUNER HEADER");
    }

    private bool DrawStreetTunerMotifSprite(float left, float top, float width, float height)
    {
        return DrawPremiumHeaderMotif(streetTunerMotifSprite, left, top, width, height);
    }

    private void LoadNoirLuxeMotifSprite()
    {
        noirLuxeMotifSprite = LoadPremiumHeaderMotif("NoirLuxeMotif.png", "NOIR LUXE HEADER");
    }

    private bool DrawNoirLuxeMotifSprite(float left, float top, float width, float height)
    {
        return DrawPremiumHeaderMotif(noirLuxeMotifSprite, left, top, width, height);
    }

    private void LoadNeonSunsetMotifSprite()
    {
        try
        {
            string path = Path.Combine(dir, "NeonSunsetMotif.png");
            if (!File.Exists(path)) { neonSunsetMotifSprite = null; return; }
            neonSunsetMotifSprite = new GTA.UI.CustomSprite(path, new System.Drawing.SizeF(1024f, 320f), new System.Drawing.PointF(0f, 0f), System.Drawing.Color.White);
            Log("NEON SUNSET MOTIF READY | " + path);
        }
        catch (Exception ex) { neonSunsetMotifSprite = null; Log("NEON SUNSET MOTIF ERROR | " + ex.Message); }
    }

    private bool DrawNeonSunsetMotifSprite(float left, float top, float width, float height)
    {
        if (neonSunsetMotifSprite == null) return false;
        try
        {
            neonSunsetMotifSprite.Size = new System.Drawing.SizeF(width * GTA.UI.Screen.Width, height * GTA.UI.Screen.Height);
            neonSunsetMotifSprite.Position = new System.Drawing.PointF(left * GTA.UI.Screen.Width, top * GTA.UI.Screen.Height);
            neonSunsetMotifSprite.Draw();
            return true;
        }
        catch { return false; }
    }

    private void LoadFutureNeonMotifSprite()
    {
        try
        {
            string path = Path.Combine(dir, "FutureNeonMotif.png");
            if (!File.Exists(path)) { futureNeonMotifSprite = null; return; }
            futureNeonMotifSprite = new GTA.UI.CustomSprite(path, new System.Drawing.SizeF(1024f, 320f), new System.Drawing.PointF(0f, 0f), System.Drawing.Color.White);
            Log("FUTURE NEON MOTIF READY | " + path);
        }
        catch (Exception ex) { futureNeonMotifSprite = null; Log("FUTURE NEON MOTIF ERROR | " + ex.Message); }
    }

    private bool DrawFutureNeonMotifSprite(float left, float top, float width, float height)
    {
        if (futureNeonMotifSprite == null) return false;
        try
        {
            futureNeonMotifSprite.Size = new System.Drawing.SizeF(width * GTA.UI.Screen.Width, height * GTA.UI.Screen.Height);
            futureNeonMotifSprite.Position = new System.Drawing.PointF(left * GTA.UI.Screen.Width, top * GTA.UI.Screen.Height);
            futureNeonMotifSprite.Draw();
            return true;
        }
        catch { return false; }
    }

    private void DrawBlockWorldMotifIcon(float left, float top, float size, int alpha)
    {
        float bodyH = size * 0.76f;
        float topH = size * 0.24f;
        float cx = left + size / 2f;
        float bodyCy = top + topH + bodyH / 2f;
        float topCy = top + topH / 2f;

        DrawRect(cx, bodyCy, size, bodyH, 108, 75, 45, alpha);
        DrawRect(cx, topCy, size, topH, 103, 175, 76, alpha);
        DrawFrame(left, top, size, size, 74, 118, 54, Math.Max(125, alpha - 45), 0.0007f);

        float cell = size / 4f;
        DrawRect(left + cell * 0.65f, top + topH + cell * 0.75f, cell * 0.55f, cell * 0.36f, 125, 89, 57, Math.Max(0, alpha - 18));
        DrawRect(left + cell * 1.75f, top + topH + cell * 0.48f, cell * 0.50f, cell * 0.34f, 92, 63, 40, Math.Max(0, alpha - 8));
        DrawRect(left + cell * 2.75f, top + topH + cell * 1.18f, cell * 0.54f, cell * 0.36f, 137, 95, 61, Math.Max(0, alpha - 22));
        DrawRect(left + cell * 1.05f, top + topH * 0.54f, cell * 0.52f, topH * 0.36f, 136, 202, 92, Math.Max(0, alpha - 8));
        DrawRect(left + cell * 2.22f, top + topH * 0.42f, cell * 0.44f, topH * 0.30f, 78, 148, 57, Math.Max(0, alpha - 8));
    }

    private void DrawPremiumThemeBackdrop(float left, float top, float width, float height)
    {
        if (!IsPremiumThemeSkin()) return;
        int ar, ag, ab; GetSkinAccent(out ar, out ag, out ab);
        int sr, sg, sb; GetSkinSecondaryAccent(out sr, out sg, out sb);

        if (skinIndex == 5)
        {
            // TEST61 Spectrum: moving multicolour rails instead of a single station tint.
            DrawSpectrumRail(left + 0.012f, top + 0.010f, width - 0.024f, 0.0018f, 205, 0);
            DrawSpectrumRail(left + width * 0.54f, top + height - 0.010f, width * 0.40f, 0.0014f, 165, 2);
            DrawRect(left + 0.011f, top + height * 0.33f, 0.0015f, height * 0.18f, ar, ag, ab, 130);
            DrawRect(left + width - 0.011f, top + height * 0.69f, 0.0015f, height * 0.14f, sr, sg, sb, 125);
        }
        else if (skinIndex == 6)
        {
            // V12.49 Vice City: cleaner Miami look with restrained sunset rails.
            DrawRect(left + width * 0.26f, top + 0.010f, width * 0.26f, 0.0018f, ar, ag, ab, 210);
            DrawRect(left + width * 0.77f, top + 0.010f, width * 0.18f, 0.0018f, sr, sg, sb, 210);
            DrawRect(left + 0.012f, top + height * 0.28f, 0.0016f, height * 0.11f, ar, ag, ab, 135);
            DrawRect(left + width - 0.012f, top + height * 0.72f, 0.0016f, height * 0.12f, sr, sg, sb, 135);
        }
        else if (skinIndex == 7)
        {
            // West Coast: clean street look, green/gold rails without muddy overlays.
            DrawRect(left + width * 0.50f, top + 0.010f, width * 0.86f, 0.0018f, ar, ag, ab, 175);
            DrawRect(left + width * 0.78f, top + height - 0.010f, width * 0.18f, 0.0018f, sr, sg, sb, 185);
            DrawRect(left + 0.012f, top + height * 0.54f, 0.0018f, height * 0.24f, ar, ag, ab, 138);
        }
        else if (skinIndex == 8)
        {
            // V12.49 Future Neon: cleaner black tech surface with red scan rails and cyan UI.
            DrawRect(left + width * 0.22f, top + 0.010f, width * 0.20f, 0.0012f, sr, sg, sb, 215);
            DrawRect(left + width * 0.72f, top + 0.010f, width * 0.14f, 0.0012f, 243, 214, 55, 205);
            DrawRect(left + 0.010f, top + height * 0.32f, 0.0015f, height * 0.16f, ar, ag, ab, 190);
            DrawRect(left + width - 0.010f, top + height * 0.66f, 0.0015f, height * 0.12f, sr, sg, sb, 175);
            for (int i = 0; i < 5; i++)
            {
                float x = left + width * (0.41f + i * 0.065f);
                DrawRect(x, top + height * (0.17f + (i % 2) * 0.020f), width * 0.030f, 0.0009f, sr, sg, sb, 110 + i * 15);
            }
        }
        else if (skinIndex == 9)
        {
            // V12.49 Street Tuner: chrome/acid-green frame with restrained tuner streaks.
            DrawRect(left + width * 0.44f, top + 0.010f, width * 0.46f, 0.0018f, sr, sg, sb, 170);
            DrawRect(left + width * 0.82f, top + 0.013f, width * 0.14f, 0.0010f, 196, 36, 65, 185);
            DrawRect(left + 0.012f, top + height * 0.58f, 0.0014f, height * 0.18f, 196, 36, 65, 145);
            for (int i = 0; i < 4; i++)
            {
                float w = width * (0.065f - i * 0.010f);
                DrawRect(left + width - 0.034f - i * 0.016f, top + height - 0.012f - i * 0.0030f, w, 0.0013f, ar, ag, ab, 180 - i * 12);
            }
        }
        else if (skinIndex == 10)
        {
            // TEST71 Block World: restrained pixel-world backdrop; the original project motif is drawn in the header layer.
            DrawRect(left + width * 0.56f, top + 0.010f, width * 0.66f, 0.0014f, ar, ag, ab, 165);
            DrawRect(left + width * 0.88f, top + height - 0.010f, width * 0.08f, 0.0014f, sr, sg, sb, 150);
        }
        else if (skinIndex == 11)
        {
            // V12.54 Noir Luxe: premium noir, clean gold rails with subtle bordeaux tailoring.
            DrawRect(left + width * 0.52f, top + 0.010f, width * 0.76f, 0.0017f, ar, ag, ab, 220);
            DrawRect(left + width * 0.22f, top + 0.013f, width * 0.09f, 0.0010f, sr, sg, sb, 185);
            DrawRect(left + width * 0.84f, top + height - 0.010f, width * 0.09f, 0.0012f, sr, sg, sb, 165);
            DrawRect(left + width * 0.68f, top + height - 0.013f, width * 0.14f, 0.0010f, ar, ag, ab, 155);
        }
        else if (skinIndex == 12)
        {
            DrawRect(left + width * 0.52f, top + 0.010f, width * 0.58f, 0.0015f, ar, ag, ab, 185);
            DrawRect(left + width * 0.83f, top + 0.013f, width * 0.13f, 0.0010f, sr, sg, sb, 175);
            DrawRect(left + width * 0.68f, top + height - 0.010f, width * 0.24f, 0.0011f, 232, 226, 207, 135);
            DrawRect(left + 0.012f, top + height * 0.38f, 0.0013f, height * 0.14f, sr, sg, sb, 125);
        }
    }

    private void DrawPremiumHeaderDecor(float left, float top, float width, float headerH)
    {
        if (!IsPremiumThemeSkin()) return;
        int ar, ag, ab; GetSkinAccent(out ar, out ag, out ab);
        int sr, sg, sb; GetSkinSecondaryAccent(out sr, out sg, out sb);
        if (skinIndex == 5)
        {
            DrawSpectrumRail(left + width * 0.18f, top + headerH - 0.0040f, width * 0.68f, 0.0018f, 225, 0);
            DrawSpectrumRail(left + width * 0.78f, top + 0.006f, width * 0.16f, 0.0010f, 190, 2);
        }
        else if (skinIndex == 6)
        {
            DrawRect(left + width * 0.28f, top + headerH - 0.0040f, width * 0.36f, 0.0018f, ar, ag, ab, 225);
            DrawRect(left + width * 0.70f, top + headerH - 0.0040f, width * 0.22f, 0.0018f, sr, sg, sb, 225);
        }
        else if (skinIndex == 7)
        {
            DrawRect(left + width * 0.50f, top + headerH - 0.0040f, width * 0.56f, 0.0018f, ar, ag, ab, 200);
            DrawRect(left + width * 0.86f, top + headerH - 0.0040f, width * 0.10f, 0.0018f, sr, sg, sb, 185);
        }
        else if (skinIndex == 8)
        {
            DrawRect(left + width * 0.26f, top + 0.006f, width * 0.22f, 0.0011f, sr, sg, sb, 225);
            DrawRect(left + width * 0.66f, top + headerH - 0.005f, width * 0.26f, 0.0011f, ar, ag, ab, 225);
            DrawRect(left + width * 0.91f, top + 0.006f, width * 0.06f, 0.0013f, 243, 214, 55, 220);
        }
        else if (skinIndex == 9)
        {
            DrawRect(left + width * 0.44f, top + headerH - 0.004f, width * 0.62f, 0.0018f, sr, sg, sb, 175);
            DrawRect(left + width * 0.84f, top + headerH - 0.004f, width * 0.10f, 0.0018f, ar, ag, ab, 225);
        }
        else if (skinIndex == 10)
        {
            DrawRect(left + width * 0.60f, top + headerH - 0.0040f, width * 0.66f, 0.0013f, ar, ag, ab, 170);
            DrawRect(left + width * 0.88f, top + headerH - 0.0040f, width * 0.07f, 0.0011f, sr, sg, sb, 145);
        }
        else if (skinIndex == 11)
        {
            DrawRect(left + width * 0.54f, top + headerH - 0.0040f, width * 0.78f, 0.0016f, ar, ag, ab, 228);
            DrawRect(left + width * 0.19f, top + 0.005f, width * 0.10f, 0.0010f, sr, sg, sb, 175);
            DrawRect(left + width * 0.90f, top + 0.005f, width * 0.05f, 0.0009f, 224, 224, 226, 95);
        }
        else if (skinIndex == 12)
        {
            DrawRect(left + width * 0.48f, top + headerH - 0.0040f, width * 0.46f, 0.0015f, ar, ag, ab, 205);
            DrawRect(left + width * 0.76f, top + headerH - 0.0040f, width * 0.13f, 0.0015f, sr, sg, sb, 190);
            DrawRect(left + width * 0.91f, top + headerH - 0.0040f, width * 0.06f, 0.0012f, 232, 226, 207, 145);
        }
    }

    private void DrawPremiumMiniDecor(float left, float top, float width, float height)
    {
        // Source-aware compact HUD decoration: external media should read as the
        // active service immediately instead of keeping the selected skin colour.
        if (youtubeModeActive || spotifyModeActive)
        {
            int mediaAr, mediaAg, mediaAb;
            if (youtubeModeActive) { mediaAr = 255; mediaAg = 0; mediaAb = 0; }
            else { mediaAr = 30; mediaAg = 215; mediaAb = 96; }
            int mediaSr = MixColor(mediaAr, 255, 0.32f);
            int mediaSg = MixColor(mediaAg, 255, 0.32f);
            int mediaSb = MixColor(mediaAb, 255, 0.32f);
            DrawRect(left + width * 0.52f, top + 0.003f, width * 0.50f, 0.0013f, mediaAr, mediaAg, mediaAb, 205);
            DrawRect(left + width * 0.22f, top + height - 0.003f, width * 0.14f, 0.0010f, mediaSr, mediaSg, mediaSb, 145);
            return;
        }

        if (!IsPremiumThemeSkin()) return;
        int ar, ag, ab; GetSkinAccent(out ar, out ag, out ab);
        int sr, sg, sb; GetSkinSecondaryAccent(out sr, out sg, out sb);
        if (skinIndex == 5)
        {
            // TEST65.1 SAFE: one calm accent rail instead of full rainbow mini-HUD decoration.
            DrawRect(left + width * 0.52f, top + 0.003f, width * 0.50f, 0.0012f, ar, ag, ab, 145);
            DrawRect(left + width * 0.24f, top + height - 0.003f, width * 0.12f, 0.0009f, sr, sg, sb, 100);
        }
        else if (skinIndex == 6)
        {
            DrawRect(left + width * 0.26f, top + 0.003f, width * 0.34f, 0.0015f, ar, ag, ab, 220);
            DrawRect(left + width * 0.72f, top + 0.003f, width * 0.18f, 0.0015f, sr, sg, sb, 220);
        }
        else if (skinIndex == 7)
        {
            DrawRect(left + 0.004f, top + height * 0.50f, 0.0018f, height * 0.70f, ar, ag, ab, 190);
            DrawRect(left + width - 0.004f, top + height * 0.50f, 0.0012f, height * 0.54f, sr, sg, sb, 140);
        }
        else if (skinIndex == 8)
        {
            DrawFrame(left + 0.002f, top + 0.002f, width - 0.004f, height - 0.004f, sr, sg, sb, 75, 0.0007f);
            DrawRect(left + width * 0.76f, top + 0.004f, width * 0.14f, 0.0011f, ar, ag, ab, 235);
            DrawRect(left + width * 0.18f, top + height - 0.004f, width * 0.12f, 0.0009f, 243, 214, 55, 200);
        }
        else if (skinIndex == 9)
        {
            DrawFrame(left + 0.002f, top + 0.002f, width - 0.004f, height - 0.004f, sr, sg, sb, 75, 0.0007f);
            DrawRect(left + width * 0.72f, top + 0.004f, width * 0.16f, 0.0013f, ar, ag, ab, 230);
            DrawRect(left + 0.004f, top + height * 0.64f, 0.0015f, height * 0.24f, 196, 36, 65, 170);
        }
        else if (skinIndex == 10)
        {
            DrawRect(left + width * 0.64f, top + 0.004f, width * 0.26f, 0.0012f, ar, ag, ab, 210);
            DrawRect(left + width * 0.20f, top + height - 0.004f, width * 0.10f, 0.0009f, sr, sg, sb, 170);
        }
        else if (skinIndex == 11)
        {
            DrawFrame(left + 0.002f, top + 0.002f, width - 0.004f, height - 0.004f, ar, ag, ab, 98, 0.0007f);
            DrawRect(left + width * 0.74f, top + 0.004f, width * 0.16f, 0.0011f, sr, sg, sb, 170);
            DrawRect(left + width * 0.24f, top + height - 0.004f, width * 0.14f, 0.0010f, ar, ag, ab, 165);
        }
        else if (skinIndex == 12)
        {
            DrawFrame(left + 0.002f, top + 0.002f, width - 0.004f, height - 0.004f, ar, ag, ab, 88, 0.0007f);
            DrawRect(left + width * 0.72f, top + 0.004f, width * 0.15f, 0.0011f, sr, sg, sb, 175);
            DrawRect(left + width * 0.26f, top + height - 0.004f, width * 0.14f, 0.0010f, 232, 226, 207, 125);
        }
    }

    private void SuppressConflictingGtaUiControls()
    {
        try
        {
            // TEST68: keep GTA overlays (character wheel / weapon wheel etc.)
            // from drawing over the multimedia menu while it is open.
            int[] controls = new int[] { 19, 37, 44, 48, 157, 158, 159, 160, 161, 162, 163, 165, 166, 167, 168, 169 };
            for (int i = 0; i < controls.Length; i++)
                Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, controls[i], true);
        }
        catch { }
    }

    public void RenderUi()

    {
        try
        {
            if (!SafeRenderState || cachedPauseMenuActive || publicReleaseOnlineGuardActive) return;

            // v7.24: never mass-flush cached PNGs on a tab change. CustomSprite instance
            // counts are stabilized at the end of every render frame instead. This keeps
            // the same cached texture alive continuously and removes the switch flicker.
            ResetTrackedSpriteDrawCounts();

            UpdateInstrumentMotionSample();
            DrawMiniUi();
            // Mini UI may use a temporary fade alpha; never let it affect other rendering.
            drawAlphaMultiplier = 1.0f;
            if (menuOpen) DrawMenu();
            DrawVolumeHud();
            DrawActionFeedback();
            drawAlphaMultiplier = 1.0f;
            DrawCustomVehicleInstruments();
            drawAlphaMultiplier = 1.0f;
            DrawBanner();

            // Must run after every visible PNG draw so stale higher instance slots from
            // the preceding frame are replaced transparently without clearing live ones.
            StabilizeTrackedCustomSpriteSlots();
        }
        catch { }
    }

    private void DrawMiniUi()
    {
        // TEST20: the mini UI is vehicle-only and disappears immediately when the
        // player gets out or uses a bicycle/BMX. This also prevents a stale external
        // media cover from lingering on screen between vehicle transitions.
        if (!cachedRadioCapableVehicle)
        {
            miniUiFade = 0.0f;
            miniUiFadeUpdatedAt = DateTime.Now;
            return;
        }

        bool targetVisible = showMiniUi;

        if (!enabled && !IsExternalMusicModeActive() && !gtaRadioModeActive && !menuOpen) targetVisible = false;
        if (menuOpen && hideMiniUiWhileMenuOpen) targetVisible = false;

        if (targetVisible)
        {
            if (youtubeModeActive)
            {
                // Artwork is optional: persistence/timer controls visibility, not cover availability.
                targetVisible = (persistentMiniUi && IsYouTubePlaying()) ||
                                (youtubeMiniHideAt != DateTime.MinValue && DateTime.Now < youtubeMiniHideAt);
            }
            else if (spotifyModeActive)
            {
                targetVisible = (persistentMiniUi && IsSpotifyPlaying()) ||
                                (spotifyMiniHideAt != DateTime.MinValue && DateTime.Now < spotifyMiniHideAt);
            }
            else if (gtaRadioModeActive)
            {
                targetVisible = (persistentMiniUi && gtaRadioPlayingRequested) ||
                                (gtaRadioMiniHideAt != DateTime.MinValue && DateTime.Now < gtaRadioMiniHideAt);
            }
            else
            {
                // Persistent mode stays visible while radio is playing. In normal mode the mini UI
                // uses its own short timer instead of the popup-banner timer.
                targetVisible = (persistentMiniUi && shouldPlay) || DateTime.Now < miniUiVisibleUntil;
            }
        }

        UpdateMiniUiFade(targetVisible);
        if (miniUiFade <= 0.01f) return;

        float previousAlphaMultiplier = drawAlphaMultiplier;
        drawAlphaMultiplier = Math.Max(0.0f, Math.Min(1.0f, miniUiFade));

        Station s = Current();
        string station = youtubeModeActive ? "YOUTUBE MUSIC" : (spotifyModeActive ? "SPOTIFY" : (gtaRadioModeActive ? CurrentGtaRadioStationName() : (s != null ? s.Name : T("NO_STATION"))));
        string detail = youtubeModeActive ? (string.IsNullOrEmpty(youtubeArtist) ? T("MEDIA_SESSION") : youtubeArtist) :
                        (spotifyModeActive ? (string.IsNullOrEmpty(spotifyArtist) ? T("MEDIA_SESSION") : spotifyArtist) :
                        (gtaRadioModeActive ? (!string.IsNullOrEmpty(gtaRadioArtist) ? gtaRadioArtist : (!string.IsNullOrEmpty(gtaRadioTitle) ? gtaRadioTitle : T("GTA_RADIO"))) : (s != null ? StationDetailsLine(s) : T("NO_ACTIVE_STATION"))));
        string state = youtubeModeActive ? LocalizeYouTubeState() : (spotifyModeActive ? LocalizeSpotifyState() : (gtaRadioModeActive ? T(gtaRadioPlayingRequested ? "STATE_PLAYING" : "STATE_OFF") : (enabled ? MapState(cachedAudioState) : T("STATE_OFF"))));
        string nowPlaying = youtubeModeActive ? GetYouTubeNowPlayingLine() : (spotifyModeActive ? GetSpotifyNowPlayingLine() : (gtaRadioModeActive ? GetGtaRadioNowPlayingLine() : GetNowPlayingLine()));

        float left = miniUiLeft;
        float top = miniUiTop;
        float width = Math.Max(0.220f, Math.Min(0.450f, miniUiWidth));
        float height = Math.Max(0.110f, Math.Min(0.220f, miniUiHeight));

        // TEST21: when the compact volume HUD is active, keep the media mini UI
        // above it instead of letting both overlays collide with the vehicle HUD.
        // Respect a custom user position if it is already higher than the safe stack.
        bool volumeLayoutActive = !menuOpen && (DateTime.Now < volumeHudVisibleUntil || volumeHudFade > 0.01f);
        if (volumeLayoutActive)
        {
            float volumeHudTop = GetVolumeHudTop();
            const float stackGap = 0.012f;
            float safeMiniTop = volumeHudTop - stackGap - height;
            if (top > safeMiniTop) top = safeMiniTop;
        }
        int ar, ag, ab;
        GetSkinAccent(out ar, out ag, out ab);
        int br, bg, bb; GetSkinBase(out br, out bg, out bb);
        int or, og, ob; GetSkinOled(out or, out og, out ob);

        // External media always owns the compact HUD accent, regardless of skin.
        // YouTube = red, Spotify = green. Radio keeps the selected skin/category logic.
        if (youtubeModeActive || spotifyModeActive)
        {
            if (youtubeModeActive) { ar = 255; ag = 0; ab = 0; }
            else { ar = 30; ag = 215; ab = 96; }
            br = MixColor(br, ar, 0.18f);
            bg = MixColor(bg, ag, 0.18f);
            bb = MixColor(bb, ab, 0.18f);
            or = MixColor(or, ar, 0.11f);
            og = MixColor(og, ag, 0.11f);
            ob = MixColor(ob, ab, 0.11f);
        }
        // Calm Spectrum: the compact radio HUD follows the same slowly drifting
        // palette as the main skin instead of jumping between station/category colours.
        else if (IsSpectrumSkin())
        {
            int tr, tg, tb;
            GetSpectrumPhaseColor(0, out tr, out tg, out tb);

            if (!dynamicMenuColorInitialized)
            {
                dynamicMenuR = tr; dynamicMenuG = tg; dynamicMenuB = tb;
                dynamicMenuColorInitialized = true;
            }
            else
            {
                const float miniFade = 0.04f;
                dynamicMenuR += (tr - dynamicMenuR) * miniFade;
                dynamicMenuG += (tg - dynamicMenuG) * miniFade;
                dynamicMenuB += (tb - dynamicMenuB) * miniFade;
            }

            ar = ClampColor((int)Math.Round(dynamicMenuR));
            ag = ClampColor((int)Math.Round(dynamicMenuG));
            ab = ClampColor((int)Math.Round(dynamicMenuB));
            float strength = dynamicRadioBackgroundStrength;
            br = MixColor(br, ar, 0.11f * strength);
            bg = MixColor(bg, ag, 0.11f * strength);
            bb = MixColor(bb, ab, 0.11f * strength);
            or = MixColor(or, ar, 0.07f * strength);
            og = MixColor(og, ag, 0.07f * strength);
            ob = MixColor(ob, ab, 0.07f * strength);
        }

        // Modernes Infotainment im gewaehlten Skin. External media overrides the
        // frame/header accent so the source colour is visible on every skin.
        bool miniSourceAccentActive = youtubeModeActive || spotifyModeActive || gtaRadioModeActive;
        DrawRect(left + width / 2f, top + height / 2f, width, height, br, bg, bb, 242);
        if (miniSourceAccentActive) DrawFrame(left, top, width, height, ar, ag, ab, 205, 0.0012f);
        else if (skinIndex == 10) DrawFrame(left, top, width, height, 72, 80, 72, 135, 0.0010f);
        else DrawFrame(left, top, width, height, IsPremiumThemeSkin() ? ar : 88, IsPremiumThemeSkin() ? ag : 96, IsPremiumThemeSkin() ? ab : 106, IsPremiumThemeSkin() ? 130 : 220, 0.0012f);
        DrawPremiumMiniDecor(left, top, width, height);
        float miniHeaderTint = miniSourceAccentActive ? 0.14f : (IsSpectrumSkin() ? 0.08f : (IsPremiumThemeSkin() ? 0.14f : 0.0f));
        DrawRect(left + width / 2f, top + 0.016f, width - 0.010f, 0.028f, MixColor(13, ar, miniHeaderTint), MixColor(17, ag, miniHeaderTint), MixColor(22, ab, miniHeaderTint), 246);
        DrawText(gtaRadioModeActive ? "LOS SANTOS GTA RADIO" : (IsExternalMusicModeActive() ? "LOS SANTOS MULTIMEDIA" : "LOS SANTOS INTERNET RADIO"), left + 0.010f, top + 0.006f, 0.205f, 0, 244, 246, 249, 238);
        DrawText("made by st3v3nblub", left + 0.165f, top + 0.008f, 0.135f, 0, ar, ag, ab, 225);

        float oledLeft = left + 0.010f;
        float oledTop = top + 0.036f;
        float oledWidth = width - 0.020f;
        float oledHeight = Math.Max(0.052f, height - 0.058f);
        DrawRect(oledLeft + oledWidth / 2f, oledTop + oledHeight / 2f, oledWidth, oledHeight, or, og, ob, 248);
        DrawFrame(oledLeft, oledTop, oledWidth, oledHeight, ar, ag, ab, 145, 0.0010f);

        // Validated media covers are drawn from memory only; no file access happens in the frame renderer.
        float miniTextX = oledLeft + 0.009f;
        bool miniCoverDrawn = false;
        if ((youtubeModeActive && youtubeCoverSprite != null) || (spotifyModeActive && spotifyCoverSprite != null))
        {
            float miniCoverH = Math.Max(0.040f, oledHeight - 0.012f);
            float screenAspectFix = (float)GTA.UI.Screen.Height / Math.Max(1f, (float)GTA.UI.Screen.Width);
            float miniCoverW = miniCoverH * screenAspectFix;
            float miniCoverLeft = oledLeft + 0.006f;
            float miniCoverTop = oledTop + 0.006f;
            DrawRect(miniCoverLeft + miniCoverW / 2f, miniCoverTop + miniCoverH / 2f, miniCoverW + 0.003f, miniCoverH + 0.003f, 9, 12, 16, 245);
            miniCoverDrawn = youtubeModeActive ?
                DrawYouTubeCover(miniCoverLeft, miniCoverTop, miniCoverW, miniCoverH) :
                DrawSpotifyCover(miniCoverLeft, miniCoverTop, miniCoverW, miniCoverH);
            if (miniCoverDrawn)
            {
                DrawFrame(miniCoverLeft, miniCoverTop, miniCoverW, miniCoverH, ar, ag, ab, 150, 0.0010f);
                miniTextX = miniCoverLeft + miniCoverW + 0.009f;

            }
        }

        // v7.19: GTA Radio shows the current station badge instead of the generic V.
        // This keeps the mini HUD consistent with the dedicated GTA Radio page.
        bool miniGtaLogoDrawn = false;
        if (gtaRadioModeActive)
        {
            float gtaLogoH = Math.Max(0.038f, Math.Min(0.052f, oledHeight - 0.014f));
            float gtaLogoW = PixelSquareWidth(gtaLogoH) * 1.08f;
            float gtaLogoLeft = oledLeft + 0.006f;
            float gtaLogoTop = oledTop + (oledHeight - gtaLogoH) / 2f;
            DrawGtaRadioLogoBadge(gtaLogoLeft, gtaLogoTop, gtaLogoW, gtaLogoH, GetCurrentGtaRadioStationEnum(), gtaRadioPlayingRequested);
            miniTextX = gtaLogoLeft + gtaLogoW + 0.010f;
            miniGtaLogoDrawn = true;
        }

        int miniTitleR = (youtubeModeActive || spotifyModeActive || gtaRadioModeActive) ? MixColor(245, ar, 0.28f) : 235;
        int miniTitleG = (youtubeModeActive || spotifyModeActive || gtaRadioModeActive) ? MixColor(245, ag, 0.28f) : 255;
        int miniTitleB = (youtubeModeActive || spotifyModeActive || gtaRadioModeActive) ? MixColor(245, ab, 0.28f) : 238;
        bool miniArtworkDrawn = miniCoverDrawn || miniGtaLogoDrawn;
        DrawText(Shorten(station, miniArtworkDrawn ? 22 : 31), miniTextX, oledTop + 0.010f, miniArtworkDrawn ? 0.285f : 0.330f, 0, miniTitleR, miniTitleG, miniTitleB, 248);
        string miniSourceLabel = youtubeModeActive ? "YOUTUBE" : (spotifyModeActive ? "SPOTIFY" : (gtaRadioModeActive ? (gtaRadioPlayingRequested ? "LIVE" : "GTA") : T("ON_AIR")));
        bool miniSourceAvailable = youtubeModeActive ? youtubeAvailable : (spotifyModeActive ? spotifyAvailable : (gtaRadioModeActive ? gtaRadioPlayingRequested : shouldPlay));
        DrawText(miniSourceLabel, oledLeft + oledWidth - 0.066f, oledTop + 0.012f, 0.155f, 0, ar, ag, ab, miniSourceAvailable ? 240 : 110);
        int miniMetaR = (youtubeModeActive || spotifyModeActive || gtaRadioModeActive) ? MixColor(232, ar, 0.48f) : 178;
        int miniMetaG = (youtubeModeActive || spotifyModeActive || gtaRadioModeActive) ? MixColor(232, ag, 0.48f) : 236;
        int miniMetaB = (youtubeModeActive || spotifyModeActive || gtaRadioModeActive) ? MixColor(232, ab, 0.48f) : 189;
        if (!string.IsNullOrEmpty(nowPlaying))
            DrawText(Shorten(nowPlaying, miniArtworkDrawn ? 34 : 46), miniTextX, oledTop + 0.038f, 0.210f, 0, miniMetaR, miniMetaG, miniMetaB, 232);
        else
            DrawText(Shorten(detail, miniArtworkDrawn ? 34 : 46), miniTextX, oledTop + 0.038f, 0.200f, 0, miniMetaR, miniMetaG, miniMetaB, 222);

        float footerY = top + height - 0.015f;
        DrawText("NUM0 " + T("MENU"), left + 0.012f, footerY - 0.008f, 0.155f, 0, 184, 188, 196, 218);
        DrawText("4/6 " + T("SEEK"), left + 0.082f, footerY - 0.008f, 0.155f, 0, 184, 188, 196, 218);
        DrawText("-/+ " + T("VOLUME_SHORT"), left + 0.144f, footerY - 0.008f, 0.155f, 0, 184, 188, 196, 218);
        if (IsExternalMusicModeActive())
            DrawText(T("MEDIA_SHORT"), left + width - 0.050f, footerY - 0.008f, 0.165f, 0, ar, ag, ab, 232);
        else if (gtaRadioModeActive)
            DrawText(volume + "%", left + width - 0.050f, footerY - 0.008f, 0.165f, 0, ar, ag, ab, 232);
        else
            DrawText(volume + "%", left + width - 0.050f, footerY - 0.008f, 0.165f, 0, 245, 247, 249, 232);

        drawAlphaMultiplier = previousAlphaMultiplier;
    }

    private float GetVolumeHudTop()
    {
        const float volumeHeight = 0.058f;
        const float gap = 0.012f;
        if (HasVisibleCustomVehicleInstrument())
        {
            float speedoTop = Math.Max(0.600f, Math.Min(0.930f, speedometerTop));
            return Math.Max(0.560f, Math.Min(0.880f, speedoTop - gap - volumeHeight));
        }
        return 0.756f;
    }

    private float GetCompactHudRightEdge()
    {
        if (HasVisibleCustomVehicleInstrument())
        {
            float speedoWidth = Math.Max(0.150f, Math.Min(0.320f, speedometerWidth));
            return Math.Max(0.250f, Math.Min(0.992f, speedometerLeft + speedoWidth));
        }

        float miniWidthForAlignment = Math.Max(0.220f, Math.Min(0.450f, miniUiWidth));
        return Math.Max(0.250f, Math.Min(0.992f, miniUiLeft + miniWidthForAlignment));
    }

    private void ResetInstrumentMotionSample()
    {
        instrumentMotionVehicleHandle = 0;
        instrumentMotionSampleValid = false;
        instrumentMotionSpeed3dMps = 0.0f;
        instrumentMotionGroundSpeedMps = 0.0f;
        instrumentMotionVerticalMps = 0.0f;
    }

    private void UpdateInstrumentMotionSample()
    {
        if (cachedVehicleHandle == 0 ||
            (!cachedRoadSpeedometerVehicle && !cachedBoatHudVehicle && !cachedAircraftHudVehicle))
        {
            if (instrumentMotionVehicleHandle != 0) ResetInstrumentMotionSample();
            return;
        }

        try
        {
            Vehicle vehicle = Game.Player.Character.CurrentVehicle;
            if (vehicle == null || vehicle.Handle != cachedVehicleHandle)
            {
                ResetInstrumentMotionSample();
                return;
            }

            // GTA already exposes physical velocity in metres/second. Reading that value
            // directly avoids the systematic error that occurs when in-game motion is
            // divided by real elapsed time (slow motion / Franklin ability / frame hitch).
            GTA.Math.Vector3 velocity = vehicle.Velocity;
            float vx = velocity.X;
            float vy = velocity.Y;
            float vz = velocity.Z;

            float groundSpeed = (float)Math.Sqrt(vx * vx + vy * vy);
            float speed3d = 0.0f;
            try { speed3d = Math.Max(0.0f, vehicle.Speed); }
            catch { speed3d = (float)Math.Sqrt(vx * vx + vy * vy + vz * vz); }

            // Defensive sanity guard only. It is deliberately far above normal GTA road
            // speeds so modded high-speed vehicles are not artificially capped.
            if (float.IsNaN(speed3d) || float.IsInfinity(speed3d) || speed3d < 0.0f) speed3d = 0.0f;
            if (float.IsNaN(groundSpeed) || float.IsInfinity(groundSpeed) || groundSpeed < 0.0f) groundSpeed = speed3d;
            if (float.IsNaN(vz) || float.IsInfinity(vz)) vz = 0.0f;

            instrumentMotionVehicleHandle = vehicle.Handle;
            instrumentMotionSampleValid = true;
            instrumentMotionSpeed3dMps = Math.Min(650.0f, speed3d);
            instrumentMotionGroundSpeedMps = Math.Min(650.0f, groundSpeed);
            instrumentMotionVerticalMps = Math.Max(-250.0f, Math.Min(250.0f, vz));
        }
        catch
        {
            ResetInstrumentMotionSample();
        }
    }

    private float GetInstrumentRoadSpeedMps(Vehicle vehicle)
    {
        if (vehicle != null && instrumentMotionSampleValid && instrumentMotionVehicleHandle == vehicle.Handle)
            return Math.Max(0.0f, instrumentMotionSpeed3dMps);
        return vehicle == null ? 0.0f : Math.Max(0.0f, vehicle.Speed);
    }

    private float GetInstrumentGroundSpeedMps(Vehicle vehicle)
    {
        if (vehicle != null && instrumentMotionSampleValid && instrumentMotionVehicleHandle == vehicle.Handle)
            return Math.Max(0.0f, instrumentMotionGroundSpeedMps);
        return vehicle == null ? 0.0f : Math.Max(0.0f, vehicle.Speed);
    }

    private float GetInstrumentVerticalSpeedMps(Vehicle vehicle)
    {
        if (vehicle != null && instrumentMotionSampleValid && instrumentMotionVehicleHandle == vehicle.Handle)
            return instrumentMotionVerticalMps;
        return vehicle == null ? 0.0f : vehicle.Velocity.Z;
    }

    private float NormalizeHeadingDelta(float delta)
    {
        while (delta > 180.0f) delta -= 360.0f;
        while (delta < -180.0f) delta += 360.0f;
        return delta;
    }

    private void ResetAutomaticIndicatorManoeuvreTracking()
    {
        automaticIndicatorCenteredSince = DateTime.MinValue;
        automaticIndicatorLowSpeedSince = DateTime.MinValue;
        automaticIndicatorCompletionSince = DateTime.MinValue;
        automaticIndicatorStartHeading = 0.0f;
        automaticIndicatorMaxHeadingDelta = 0.0f;
        automaticIndicatorPeakSteer = 0.0f;
        automaticIndicatorCounterSteerSeen = false;
        automaticIndicatorTurnDetected = false;
    }

    private void SetAutomaticVehicleIndicatorState(int vehicleHandle, int state)
    {
        if (vehicleHandle == 0) return;
        try
        {
            // GTA native: signal 1 = left, signal 0 = right. GTA itself supplies the
            // actual lamp blink cadence; the script only owns the on/off state.
            bool left = state == -1 || state == 2;
            bool right = state == 1 || state == 2;
            Function.Call((Hash)0xB5D45264751B7DF0UL, vehicleHandle, 1, left);
            Function.Call((Hash)0xB5D45264751B7DF0UL, vehicleHandle, 0, right);

            int previousState = automaticIndicatorState;
            automaticIndicatorState = state;

            if ((state == -1 || state == 1) && previousState != state)
            {
                automaticIndicatorActivatedAt = DateTime.UtcNow;
                ResetAutomaticIndicatorManoeuvreTracking();
                try { automaticIndicatorStartHeading = Function.Call<float>(Hash.GET_ENTITY_HEADING, vehicleHandle); } catch { }
            }
            else if (state == 0)
            {
                automaticIndicatorActivatedAt = DateTime.MinValue;
                ResetAutomaticIndicatorManoeuvreTracking();
            }
            else if (state == 2 && previousState != 2)
            {
                automaticIndicatorActivatedAt = DateTime.UtcNow;
                ResetAutomaticIndicatorManoeuvreTracking();
            }
        }
        catch { }
    }

    private void ResetAutomaticVehicleIndicators(bool turnLightsOff)
    {
        int oldHandle = automaticIndicatorVehicleHandle;
        if (turnLightsOff && oldHandle != 0)
        {
            try
            {
                if (Function.Call<bool>(Hash.DOES_ENTITY_EXIST, oldHandle))
                {
                    Function.Call((Hash)0xB5D45264751B7DF0UL, oldHandle, 1, false);
                    Function.Call((Hash)0xB5D45264751B7DF0UL, oldHandle, 0, false);
                }
            }
            catch { }
        }

        automaticIndicatorVehicleHandle = 0;
        automaticIndicatorState = 0;
        manualHazardsActive = false;
        automaticIndicatorCandidate = 0;
        automaticIndicatorCandidateSince = DateTime.MinValue;
        automaticIndicatorActivatedAt = DateTime.MinValue;
        ResetAutomaticIndicatorManoeuvreTracking();
        automaticIndicatorLastSpeedAt = DateTime.MinValue;
        automaticHazardUntil = DateTime.MinValue;
        automaticIndicatorLastSpeedMps = 0.0f;
    }

    private void ToggleManualVehicleHazards()
    {
        // v7.28: NumPad7 is a manual vehicle function and must work even when the
        // steering-based AutomaticIndicators option is disabled.
        if (cachedPauseMenuActive || publicReleaseOnlineGuardActive ||
            !cachedRadioCapableVehicle || !cachedRoadSpeedometerVehicle || cachedVehicleHandle == 0)
            return;

        try
        {
            Vehicle vehicle = Game.Player.Character.CurrentVehicle;
            if (vehicle == null || vehicle.Handle == 0 || vehicle.Handle != cachedVehicleHandle) return;

            int playerHandle = Game.Player.Character.Handle;
            int driverHandle = Function.Call<int>(Hash.GET_PED_IN_VEHICLE_SEAT, vehicle.Handle, -1);
            if (driverHandle != playerHandle) return;

            if (automaticIndicatorVehicleHandle != vehicle.Handle)
            {
                ResetAutomaticVehicleIndicators(true);
                automaticIndicatorVehicleHandle = vehicle.Handle;
            }

            manualHazardsActive = !manualHazardsActive;
            automaticHazardUntil = DateTime.MinValue;
            automaticIndicatorCandidate = 0;
            automaticIndicatorCandidateSince = DateTime.MinValue;
            automaticIndicatorCenteredSince = DateTime.MinValue;
            automaticIndicatorLowSpeedSince = DateTime.MinValue;
            SetAutomaticVehicleIndicatorState(vehicle.Handle, manualHazardsActive ? 2 : 0);
        }
        catch { }
    }

    private void ApplyManualVehicleHighBeam(int vehicleHandle, bool enabled)
    {
        if (vehicleHandle == 0) return;
        try
        {
            // SET_VEHICLE_LIGHTS: 2 forces the headlights on; 0 releases the override.
            // SET_VEHICLE_FULLBEAM then selects high/low beam state. Raw hashes keep this
            // compatible with older ScriptHookVDotNet Hash enum versions.
            if (enabled)
            {
                Function.Call((Hash)0x34E710FF01247C5AUL, vehicleHandle, 2);
                Function.Call((Hash)0x8B7FD87F0DDB421EUL, vehicleHandle, true);
            }
            else
            {
                Function.Call((Hash)0x8B7FD87F0DDB421EUL, vehicleHandle, false);
                Function.Call((Hash)0x34E710FF01247C5AUL, vehicleHandle, 0);
            }
        }
        catch { }
    }

    private void ResetManualVehicleHighBeam(bool releaseLights)
    {
        int oldHandle = manualHighBeamVehicleHandle;
        if (releaseLights && oldHandle != 0)
        {
            try
            {
                if (Function.Call<bool>(Hash.DOES_ENTITY_EXIST, oldHandle))
                    ApplyManualVehicleHighBeam(oldHandle, false);
            }
            catch { }
        }

        manualHighBeamActive = false;
        manualHighBeamVehicleHandle = 0;
    }

    private void ToggleManualVehicleHighBeam()
    {
        if (cachedPauseMenuActive || publicReleaseOnlineGuardActive ||
            !cachedRadioCapableVehicle || !cachedRoadSpeedometerVehicle || cachedVehicleHandle == 0)
            return;

        try
        {
            Vehicle vehicle = Game.Player.Character.CurrentVehicle;
            if (vehicle == null || vehicle.Handle == 0 || vehicle.Handle != cachedVehicleHandle) return;

            int playerHandle = Game.Player.Character.Handle;
            int driverHandle = Function.Call<int>(Hash.GET_PED_IN_VEHICLE_SEAT, vehicle.Handle, -1);
            if (driverHandle != playerHandle) return;

            if (manualHighBeamVehicleHandle != 0 && manualHighBeamVehicleHandle != vehicle.Handle)
                ResetManualVehicleHighBeam(true);

            manualHighBeamVehicleHandle = vehicle.Handle;
            manualHighBeamActive = !manualHighBeamActive;
            ApplyManualVehicleHighBeam(vehicle.Handle, manualHighBeamActive);
            ShowActionFeedback("FERNLICHT", T(manualHighBeamActive ? "ON" : "OFF"));
        }
        catch { }
    }

    private void UpdateManualVehicleHighBeam()
    {
        if (!manualHighBeamActive) return;

        if (cachedPauseMenuActive || publicReleaseOnlineGuardActive ||
            !cachedRadioCapableVehicle || !cachedRoadSpeedometerVehicle || cachedVehicleHandle == 0)
        {
            ResetManualVehicleHighBeam(true);
            return;
        }

        try
        {
            Vehicle vehicle = Game.Player.Character.CurrentVehicle;
            if (vehicle == null || vehicle.Handle == 0 || vehicle.Handle != manualHighBeamVehicleHandle)
            {
                ResetManualVehicleHighBeam(true);
                return;
            }

            int driverHandle = Function.Call<int>(Hash.GET_PED_IN_VEHICLE_SEAT, vehicle.Handle, -1);
            if (driverHandle != Game.Player.Character.Handle)
            {
                ResetManualVehicleHighBeam(true);
                return;
            }

            // Reassert while active so GTA's native automatic light logic cannot immediately
            // downgrade the beam after the user's toggle press.
            ApplyManualVehicleHighBeam(vehicle.Handle, true);
        }
        catch
        {
            ResetManualVehicleHighBeam(true);
        }
    }

    private void UpdateAutomaticVehicleIndicators()
    {
        // Vehicle/pause safety applies to both automation and manual hazards.
        if (cachedPauseMenuActive || publicReleaseOnlineGuardActive ||
            !cachedRadioCapableVehicle || !cachedRoadSpeedometerVehicle || cachedVehicleHandle == 0)
        {
            if (automaticIndicatorVehicleHandle != 0) ResetAutomaticVehicleIndicators(true);
            return;
        }

        try
        {
            Vehicle vehicle = Game.Player.Character.CurrentVehicle;
            if (vehicle == null || vehicle.Handle == 0 || vehicle.Handle != cachedVehicleHandle)
            {
                ResetAutomaticVehicleIndicators(true);
                return;
            }

            // Only the player driver owns the automatic indicator state. Never modify a
            // vehicle's lamps while the player is merely a passenger.
            int playerHandle = Game.Player.Character.Handle;
            int driverHandle = Function.Call<int>(Hash.GET_PED_IN_VEHICLE_SEAT, vehicle.Handle, -1);
            if (driverHandle != playerHandle)
            {
                ResetAutomaticVehicleIndicators(true);
                return;
            }

            if (automaticIndicatorVehicleHandle != vehicle.Handle)
            {
                ResetAutomaticVehicleIndicators(true);
                automaticIndicatorVehicleHandle = vehicle.Handle;
            }

            // Manual hazards always win over steering-based indicators and remain available
            // even when AutomaticIndicators=false. One press toggles both sides on, the next off.
            if (manualHazardsActive)
            {
                automaticHazardUntil = DateTime.MinValue;
                if (automaticIndicatorState != 2)
                    SetAutomaticVehicleIndicatorState(vehicle.Handle, 2);
                return;
            }

            // Automation OFF means only the steering-driven part is disabled. Clear any
            // directional/automatic state but leave NumPad7 hazard handling untouched.
            if (!automaticIndicatorsEnabled)
            {
                automaticHazardUntil = DateTime.MinValue;
                automaticIndicatorCandidate = 0;
                automaticIndicatorCandidateSince = DateTime.MinValue;
                if (automaticIndicatorState != 0)
                    SetAutomaticVehicleIndicatorState(vehicle.Handle, 0);
                return;
            }

            DateTime now = DateTime.UtcNow;
            float speedMps = 0.0f;
            try { speedMps = Math.Abs(vehicle.Speed); } catch { speedMps = 0.0f; }

            // Emergency hazards are intentionally disabled in the public configuration,
            // but the old compatibility path remains harmless if enabled by an older file.
            if (automaticEmergencyHazardsEnabled && automaticIndicatorLastSpeedAt != DateTime.MinValue)
            {
                double dt = (now - automaticIndicatorLastSpeedAt).TotalSeconds;
                if (dt > 0.025 && dt < 0.40 && automaticIndicatorLastSpeedMps > 11.0f)
                {
                    float deceleration = (automaticIndicatorLastSpeedMps - speedMps) / (float)dt;
                    float brake = 0.0f;
                    bool collided = false;
                    try { brake = Math.Abs(Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, 72)); } catch { }
                    try { collided = Function.Call<bool>(Hash.HAS_ENTITY_COLLIDED_WITH_ANYTHING, vehicle.Handle); } catch { }
                    if (deceleration >= 12.5f && (brake >= 0.82f || collided))
                    {
                        automaticHazardUntil = now.AddMilliseconds(4500);
                        if (automaticIndicatorState != 2)
                            SetAutomaticVehicleIndicatorState(vehicle.Handle, 2);
                    }
                }
            }
            automaticIndicatorLastSpeedMps = speedMps;
            automaticIndicatorLastSpeedAt = now;

            if (automaticHazardUntil != DateTime.MinValue)
            {
                if (now < automaticHazardUntil)
                {
                    if (automaticIndicatorState != 2)
                        SetAutomaticVehicleIndicatorState(vehicle.Handle, 2);
                    return;
                }

                automaticHazardUntil = DateTime.MinValue;
                if (automaticIndicatorState == 2)
                    SetAutomaticVehicleIndicatorState(vehicle.Handle, 0);
            }

            float steer = 0.0f;
            try { steer = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, 59); } catch { }
            if (steer > 1.0f) steer = 1.0f;
            if (steer < -1.0f) steer = -1.0f;

            // v7.29: input alone is not enough. On keyboard a tiny tap can momentarily
            // report full steering input, and on a controller ordinary road corrections
            // can cross the old threshold. Require a real physical steering angle as a
            // second gate. The required wheel angle becomes slightly smaller at road/highway
            // speed, where genuine lane changes naturally use less steering lock.
            float physicalSteeringAngleDeg = 0.0f;
            try { physicalSteeringAngleDeg = Math.Abs(vehicle.SteeringAngle); } catch { }

            const float triggerThreshold = 0.72f;
            const float centeredThreshold = 0.15f;
            const float counterSteerThreshold = 0.20f;
            const float minimumActivationSpeed = 2.80f; // ~10 km/h; parking corrections never auto-signal.
            const float turnHeadingThreshold = 24.0f;   // large course change = real corner/turn.
            const int triggerHoldMs = 260;              // filters short steering taps/corrections.
            const int minimumOnMs = 1450;               // keep at least a useful multi-flash sequence.
            const int completionHoldMs = 280;
            const int maximumOnMs = 9500;

            float requiredPhysicalAngleDeg = speedMps >= 16.7f ? 6.0f :
                                             (speedMps >= 8.3f ? 8.0f : 10.0f);
            bool deliberateSteering = Math.Abs(steer) >= triggerThreshold &&
                                      physicalSteeringAngleDeg >= requiredPhysicalAngleDeg;

            int desired = 0;
            if (speedMps >= minimumActivationSpeed && deliberateSteering)
            {
                if (steer < 0.0f) desired = -1;
                else if (steer > 0.0f) desired = 1;
            }

            // Start only after sustained, clearly deliberate steering. Once active, the
            // signal is no longer tied directly to instantaneous steering: Tesla-style
            // cancellation waits until the manoeuvre itself is complete.
            if (automaticIndicatorState == 0)
            {
                if (desired != 0)
                {
                    if (automaticIndicatorCandidate != desired)
                    {
                        automaticIndicatorCandidate = desired;
                        automaticIndicatorCandidateSince = now;
                    }
                    else if (automaticIndicatorCandidateSince != DateTime.MinValue &&
                             (now - automaticIndicatorCandidateSince).TotalMilliseconds >= triggerHoldMs)
                    {
                        SetAutomaticVehicleIndicatorState(vehicle.Handle, desired);
                        automaticIndicatorCandidate = 0;
                        automaticIndicatorCandidateSince = DateTime.MinValue;
                    }
                }
                else
                {
                    automaticIndicatorCandidate = 0;
                    automaticIndicatorCandidateSince = DateTime.MinValue;
                }
                return;
            }

            if (automaticIndicatorState != -1 && automaticIndicatorState != 1) return;

            double activeMs = automaticIndicatorActivatedAt == DateTime.MinValue
                ? 0.0 : (now - automaticIndicatorActivatedAt).TotalMilliseconds;

            float heading = automaticIndicatorStartHeading;
            try { heading = Function.Call<float>(Hash.GET_ENTITY_HEADING, vehicle.Handle); } catch { }
            float headingDelta = Math.Abs(NormalizeHeadingDelta(heading - automaticIndicatorStartHeading));
            if (headingDelta > automaticIndicatorMaxHeadingDelta)
                automaticIndicatorMaxHeadingDelta = headingDelta;

            float absSteer = Math.Abs(steer);
            if (absSteer > automaticIndicatorPeakSteer)
                automaticIndicatorPeakSteer = absSteer;

            bool steeringOppositeSignal = (automaticIndicatorState < 0 && steer >= counterSteerThreshold) ||
                                          (automaticIndicatorState > 0 && steer <= -counterSteerThreshold);
            if (activeMs >= 350.0 && automaticIndicatorPeakSteer >= triggerThreshold && steeringOppositeSignal)
                automaticIndicatorCounterSteerSeen = true;

            if (automaticIndicatorMaxHeadingDelta >= turnHeadingThreshold)
                automaticIndicatorTurnDetected = true;

            bool centered = absSteer <= centeredThreshold;
            if (centered)
            {
                if (automaticIndicatorCenteredSince == DateTime.MinValue)
                    automaticIndicatorCenteredSince = now;
            }
            else
            {
                automaticIndicatorCenteredSince = DateTime.MinValue;
                automaticIndicatorCompletionSince = DateTime.MinValue;
            }

            // TURN: once the car has made a meaningful course change, wait for the driver
            // to unwind the wheel. The new heading is expected to remain different from the
            // heading at activation, so we deliberately do not wait for it to return.
            bool turnComplete = automaticIndicatorTurnDetected && centered;

            // LANE CHANGE: the same indicator must survive the opposite steering correction.
            // Completion requires that correction plus a near-straight final heading. This
            // prevents the old behaviour where counter-steering could flip the signal side.
            float laneReturnLimit = Math.Max(7.0f, automaticIndicatorMaxHeadingDelta * 0.68f);
            bool laneHeadingSettled = headingDelta <= laneReturnLimit;
            bool laneComplete = !automaticIndicatorTurnDetected &&
                                automaticIndicatorCounterSteerSeen &&
                                centered && laneHeadingSettled;

            bool manoeuvreComplete = turnComplete || laneComplete;
            if (activeMs >= minimumOnMs && manoeuvreComplete)
            {
                if (automaticIndicatorCompletionSince == DateTime.MinValue)
                    automaticIndicatorCompletionSince = now;
                else if ((now - automaticIndicatorCompletionSince).TotalMilliseconds >= completionHoldMs)
                {
                    SetAutomaticVehicleIndicatorState(vehicle.Handle, 0);
                    automaticIndicatorCandidate = 0;
                    automaticIndicatorCandidateSince = DateTime.MinValue;
                    return;
                }
            }
            else if (!manoeuvreComplete)
            {
                automaticIndicatorCompletionSince = DateTime.MinValue;
            }

            // If the vehicle is stopped after the manoeuvre, do not leave the indicator on
            // forever. This is deliberately much slower than the normal completion logic.
            if (speedMps < 0.65f && centered)
            {
                if (automaticIndicatorLowSpeedSince == DateTime.MinValue)
                    automaticIndicatorLowSpeedSince = now;
                else if (activeMs >= minimumOnMs &&
                         (now - automaticIndicatorLowSpeedSince).TotalMilliseconds >= 3500)
                {
                    SetAutomaticVehicleIndicatorState(vehicle.Handle, 0);
                    return;
                }
            }
            else
            {
                automaticIndicatorLowSpeedSince = DateTime.MinValue;
            }

            // Safety fallback for unusual GTA physics/road geometry. Only cancel when the
            // wheel is essentially straight, never while the player is still actively turning.
            if (activeMs >= maximumOnMs && absSteer <= 0.28f)
                SetAutomaticVehicleIndicatorState(vehicle.Handle, 0);
        }
        catch
        {
            ResetAutomaticVehicleIndicators(true);
        }
    }

    private void DrawAutomaticIndicatorHud(float left, float top, float width, float headerH, float uiScale, int ar, int ag, int ab)
    {
        if ((!automaticIndicatorsEnabled && !manualHazardsActive) || automaticIndicatorVehicleHandle == 0 ||
            automaticIndicatorVehicleHandle != cachedVehicleHandle || automaticIndicatorState == 0) return;

        // Match the visual pulse to a normal automotive indicator cadence. The actual
        // exterior lamps are blinked by GTA; this only mirrors the state in the road HUD.
        long blinkStep = (DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond) / 430L;
        if ((blinkStep & 1L) != 0L) return;

        float y = top + Math.Max(0.003f, (headerH - 0.018f) * 0.5f);
        float scale = 0.165f * uiScale;
        int rr = MixColor(ar, 125, 0.10f);
        int rg = MixColor(ag, 255, 0.34f);
        int rb = MixColor(ab, 145, 0.18f);

        // Keep the arrows in a dedicated central header pocket so manufacturer logos,
        // vehicle names and the DRIVE label retain their existing alignment.
        if (automaticIndicatorState == -1 || automaticIndicatorState == 2)
            DrawText("<", left + width * 0.58f, y, scale, 0, rr, rg, rb, 250);
        if (automaticIndicatorState == 1 || automaticIndicatorState == 2)
            DrawText(">", left + width * 0.66f, y, scale, 0, rr, rg, rb, 250);
    }

    private bool HasVisibleCustomVehicleInstrument()
    {
        if (cachedVehicleHandle == 0) return false;
        if (showCustomSpeedometer && cachedRoadSpeedometerVehicle) return true;
        if (showBoatHud && cachedBoatHudVehicle) return true;
        if (showFlightHud && cachedAircraftHudVehicle) return true;
        return false;
    }

    private void DrawCustomVehicleInstruments()
    {
        if (menuOpen || !cachedRadioCapableVehicle || cachedVehicleHandle == 0) return;

        // v7.8 conservative stale-handle guard: keep the proven v7.6 loading flow,
        // but never use a cached vehicle handle after GTA has destroyed the entity.
        try
        {
            if (!Function.Call<bool>(Hash.DOES_ENTITY_EXIST, cachedVehicleHandle)) return;
        }
        catch { return; }

        if (showCustomSpeedometer && cachedRoadSpeedometerVehicle)
            DrawCustomSpeedometer();
        else if (showBoatHud && cachedBoatHudVehicle)
            DrawBoatInstrumentHud();
        else if (showFlightHud && cachedAircraftHudVehicle)
            DrawFlightInstrumentHud();
    }

    private void DrawCustomSpeedometer()
    {
        // Road cluster only: passenger cars, trucks, vans and motorcycles.
        // Marine/flight classes use dedicated instruments below; cycles and trains stay native-only.
        if (!showCustomSpeedometer || menuOpen || !cachedRadioCapableVehicle ||
            !cachedRoadSpeedometerVehicle || cachedVehicleHandle == 0) return;

        try
        {
            Vehicle vehicle = Game.Player.Character.CurrentVehicle;
            if (vehicle == null || vehicle.Handle != cachedVehicleHandle) return;

            // v7.28: use GTA's native physical speed in m/s. Unit conversion is the only
            // calculation here, so slow motion and render timing cannot distort the value.
            float speedMps = GetInstrumentRoadSpeedMps(vehicle);
            int displaySpeed = Math.Max(0, (int)Math.Round(speedMps * (speedometerUseMph ? 2.23693629f : 3.6f)));
            string speedUnitText = speedometerUseMph ? "MPH" : "KM/H";
            float rpm = Math.Max(0.0f, Math.Min(1.0f, vehicle.CurrentRPM));
            int gear = vehicle.CurrentGear;

            string gearText;
            bool speedoBike = false;
            try { speedoBike = Function.Call<bool>(Hash.IS_THIS_MODEL_A_BIKE, vehicle.Model.Hash); } catch { speedoBike = false; }
            if (gear > 0)
            {
                // GTA's automatic cars report gear 1 even while standing still. For a
                // road-car HUD, D is clearer at rest; motorcycles keep their real gear.
                gearText = (!speedoBike && speedMps < 0.45f) ? "D" : gear.ToString();
            }
            else
            {
                // GTA reports gear 0 for neutral/reverse. Use velocity relative to the
                // vehicle heading to distinguish a real reverse movement from neutral.
                GTA.Math.Vector3 velocity = vehicle.Velocity;
                GTA.Math.Vector3 forward = vehicle.ForwardVector;
                float signedForwardSpeed = velocity.X * forward.X + velocity.Y * forward.Y + velocity.Z * forward.Z;
                gearText = signedForwardSpeed < -0.35f ? "R" : "N";
            }

            string vehicleName = "VEHICLE";
            try
            {
                string displayName = vehicle.DisplayName;
                string localized = string.IsNullOrEmpty(displayName) ? "" : Game.GetLocalizedString(displayName);
                if (!string.IsNullOrEmpty(localized) && !string.Equals(localized, "NULL", StringComparison.OrdinalIgnoreCase))
                    vehicleName = localized.ToUpperInvariant();
                else if (!string.IsNullOrEmpty(displayName))
                    vehicleName = displayName.ToUpperInvariant();
            }
            catch { }

            float left = Math.Max(0.005f, Math.Min(0.845f, speedometerLeft));
            float top = Math.Max(0.600f, Math.Min(0.930f, speedometerTop));
            float width = Math.Max(0.150f, Math.Min(0.320f, speedometerWidth));
            float height = Math.Max(0.095f, Math.Min(0.180f, speedometerHeight));
            if (left + width > 0.992f) left = 0.992f - width;
            if (top + height > 0.985f) top = 0.985f - height;

            float sx = width / 0.205f;
            float sy = height / 0.126f;
            float uiScale = Math.Max(0.72f, Math.Min(1.45f, Math.Min(sx, sy)));

            int ar, ag, ab; GetSkinAccent(out ar, out ag, out ab);
            int br, bg, bb; GetSkinBase(out br, out bg, out bb);
            int or, og, ob; GetSkinOled(out or, out og, out ob);

            float previousAlphaMultiplier = drawAlphaMultiplier;
            drawAlphaMultiplier = previousAlphaMultiplier * Math.Max(0.20f, Math.Min(1.0f, speedometerOpacity / 100.0f));

            DrawRect(left + width / 2f, top + height / 2f, width, height, br, bg, bb, 246);
            DrawFrame(left, top, width, height, ar, ag, ab, IsPremiumThemeSkin() ? 170 : 220, 0.0012f);
            DrawPremiumMiniDecor(left, top, width, height);

            float headerH = Math.Max(0.024f, height * 0.23f);
            DrawRect(left + width / 2f, top + headerH / 2f, width - 0.010f, headerH - 0.004f,
                     MixColor(12, ar, 0.11f), MixColor(16, ag, 0.11f), MixColor(21, ab, 0.11f), 246);
            float headerTextLeft = left + 0.010f;
            int vehicleNameLimit = 22;
            if (speedometerShowCarIcon)
            {
                float logoH = Math.Max(0.014f, Math.Min(0.021f, headerH - 0.007f));
                float logoW = Math.Max(PixelSquareWidth(logoH) * 1.85f, 0.026f);
                float logoLeft = left + 0.008f;
                float logoTop = top + Math.Max(0.002f, (headerH - logoH) * 0.5f);
                DrawRect(logoLeft + logoW * 0.5f, logoTop + logoH * 0.5f, logoW + 0.004f, logoH + 0.004f,
                         MixColor(or, ar, 0.12f), MixColor(og, ag, 0.12f), MixColor(ob, ab, 0.12f), 220);

                // Use Rockstar's own GTA V manufacturer textures (MPCarHUD*) for the
                // current model. If a mod/add-on vehicle has no GTA make logo, keep a
                // neutral vehicle symbol as a graceful fallback.
                bool gtaLogoDrawn = DrawGtaVehicleManufacturerLogo(vehicle, logoLeft, logoTop, logoW, logoH, 245);
                if (!gtaLogoDrawn)
                {
                    float fallbackSize = Math.Min(logoH, 0.019f);
                    DrawUiIcon(7, logoLeft + (logoW - PixelSquareWidth(fallbackSize)) * 0.5f,
                               logoTop + (logoH - fallbackSize) * 0.5f, fallbackSize, ar, ag, ab, 235);
                }

                headerTextLeft = logoLeft + logoW + 0.007f;
                vehicleNameLimit = 17;
            }
            // Reserve a small centre pocket while an indicator is active so long
            // manufacturer/model names never collide with the blinking HUD arrows.
            if (automaticIndicatorVehicleHandle == cachedVehicleHandle && automaticIndicatorState != 0)
                vehicleNameLimit = Math.Min(vehicleNameLimit, 13);
            DrawText(Shorten(vehicleName, vehicleNameLimit), headerTextLeft, top + 0.005f, 0.170f * uiScale, 0, 236, 239, 244, 232);
            DrawText(T("SPEEDO_DRIVE"), left + width - 0.052f, top + 0.006f, 0.125f * uiScale, 0, ar, ag, ab, 210);
            DrawAutomaticIndicatorHud(left, top, width, headerH, uiScale, ar, ag, ab);

            float bodyTop = top + headerH + 0.003f;
            float bodyH = height - headerH - 0.010f;
            DrawRect(left + width / 2f, bodyTop + bodyH / 2f, width - 0.014f, bodyH, or, og, ob, 247);

            string speedText = displaySpeed.ToString();
            DrawText(speedText, left + 0.012f, bodyTop + 0.002f, 0.500f * uiScale, 0, 247, 249, 251, 252);
            DrawText(speedUnitText, left + width * 0.43f, bodyTop + 0.027f * uiScale, 0.155f * uiScale, 0, 164, 173, 184, 220);

            float gearW = Math.Max(0.045f, width * 0.25f);
            float gearH = Math.Max(0.045f, bodyH * 0.56f);
            float gearLeft = left + width - gearW - 0.012f;
            float gearTop = bodyTop + 0.008f;
            DrawRect(gearLeft + gearW / 2f, gearTop + gearH / 2f, gearW, gearH, MixColor(or, ar, 0.10f), MixColor(og, ag, 0.10f), MixColor(ob, ab, 0.10f), 248);
            DrawFrame(gearLeft, gearTop, gearW, gearH, ar, ag, ab, 170, 0.0010f);
            DrawText(T("SPEEDO_GEAR"), gearLeft + 0.008f, gearTop + 0.004f, 0.118f * uiScale, 0, 157, 165, 176, 215);
            DrawText(gearText, gearLeft + 0.011f, gearTop + 0.016f, 0.330f * uiScale, 0, ar, ag, ab, 250);

            // Segmented RPM strip. On RED SPORT the limiter automatically switches
            // to a cool cyan contrast so the high-RPM area never disappears into red.
            if (speedometerShowRpm)
            {
                int segments = 12;
                float rpmLeft = left + 0.012f;
                float rpmTop = top + height - 0.018f;
                float rpmWidth = width - 0.024f;
                float gap = 0.0020f;
                float segmentW = (rpmWidth - gap * (segments - 1)) / segments;
                int activeSegments = (int)Math.Ceiling(rpm * segments);
                for (int i = 0; i < segments; i++)
                {
                    bool active = i < activeSegments;
                    int rr = active ? ar : 48;
                    int rg = active ? ag : 53;
                    int rb = active ? ab : 61;
                    int alpha = active ? 235 : 160;
                    if (active && i >= segments - 2 && rpm >= 0.82f)
                    {
                        if (skinIndex == 2) { rr = 74; rg = 210; rb = 255; }
                        else { rr = 245; rg = 72; rb = 72; }
                        alpha = 248;
                    }
                    DrawRect(rpmLeft + segmentW / 2f + i * (segmentW + gap), rpmTop, segmentW, 0.006f, rr, rg, rb, alpha);
                }
                DrawText(T("SPEEDO_RPM"), rpmLeft, rpmTop - 0.013f, 0.120f * uiScale, 0, 132, 141, 153, 205);
            }

            drawAlphaMultiplier = previousAlphaMultiplier;
        }
        catch
        {
            // Never let a transient vehicle handle/model change break the UI renderer.
            drawAlphaMultiplier = 1.0f;
        }
    }

    private void DrawBoatInstrumentHud()
    {
        if (!showBoatHud || menuOpen || !cachedBoatHudVehicle || cachedVehicleHandle == 0) return;
        try
        {
            Vehicle vehicle = Game.Player.Character.CurrentVehicle;
            if (vehicle == null || vehicle.Handle != cachedVehicleHandle) return;

            float speedMps = GetInstrumentGroundSpeedMps(vehicle);
            float displaySpeedValue = navigationUseMetric ? speedMps * 3.6f : speedMps * 1.94384449f;
            int displaySpeed = Math.Max(0, (int)Math.Round(displaySpeedValue));
            string speedUnitText = navigationUseMetric ? "KM/H" : "KTS";
            float heading = vehicle.Heading;
            while (heading < 0.0f) heading += 360.0f;
            while (heading >= 360.0f) heading -= 360.0f;
            int headingDegrees = (int)Math.Round(heading) % 360;

            string vehicleName = GetInstrumentVehicleName(vehicle);
            float left, top, width, height, uiScale;
            GetInstrumentLayout(out left, out top, out width, out height, out uiScale);
            int ar, ag, ab; GetSkinAccent(out ar, out ag, out ab);
            int br, bg, bb; GetSkinBase(out br, out bg, out bb);
            int or, og, ob; GetSkinOled(out or, out og, out ob);

            float previousAlphaMultiplier = drawAlphaMultiplier;
            drawAlphaMultiplier = previousAlphaMultiplier * Math.Max(0.20f, Math.Min(1.0f, speedometerOpacity / 100.0f));

            DrawRect(left + width / 2f, top + height / 2f, width, height, br, bg, bb, 246);
            DrawFrame(left, top, width, height, ar, ag, ab, IsPremiumThemeSkin() ? 170 : 220, 0.0012f);
            DrawPremiumMiniDecor(left, top, width, height);

            float headerH = Math.Max(0.024f, height * 0.23f);
            DrawRect(left + width / 2f, top + headerH / 2f, width - 0.010f, headerH - 0.004f,
                     MixColor(12, ar, 0.11f), MixColor(16, ag, 0.11f), MixColor(21, ab, 0.11f), 246);
            float headerTextLeft = left + 0.010f;
            int vehicleNameLimit = 22;
            if (speedometerShowCarIcon)
            {
                float logoH = Math.Max(0.014f, Math.Min(0.021f, headerH - 0.007f));
                float logoW = Math.Max(PixelSquareWidth(logoH) * 1.85f, 0.026f);
                float logoLeft = left + 0.008f;
                float logoTop = top + Math.Max(0.002f, (headerH - logoH) * 0.5f);
                DrawRect(logoLeft + logoW * 0.5f, logoTop + logoH * 0.5f, logoW + 0.004f, logoH + 0.004f,
                         MixColor(or, ar, 0.12f), MixColor(og, ag, 0.12f), MixColor(ob, ab, 0.12f), 220);
                bool gtaLogoDrawn = DrawGtaVehicleManufacturerLogo(vehicle, logoLeft, logoTop, logoW, logoH, 245);
                if (!gtaLogoDrawn)
                {
                    float fallbackSize = Math.Min(logoH, 0.019f);
                    DrawUiIcon(7, logoLeft + (logoW - PixelSquareWidth(fallbackSize)) * 0.5f,
                               logoTop + (logoH - fallbackSize) * 0.5f, fallbackSize, ar, ag, ab, 235);
                }
                headerTextLeft = logoLeft + logoW + 0.007f;
                vehicleNameLimit = 17;
            }
            DrawText(Shorten(vehicleName, vehicleNameLimit), headerTextLeft, top + 0.005f, 0.170f * uiScale, 0, 236, 239, 244, 232);
            DrawText(T("SPEEDO_MARINE"), left + width - 0.060f, top + 0.006f, 0.125f * uiScale, 0, ar, ag, ab, 210);

            float bodyTop = top + headerH + 0.003f;
            float bodyH = height - headerH - 0.010f;
            DrawRect(left + width / 2f, bodyTop + bodyH / 2f, width - 0.014f, bodyH, or, og, ob, 247);
            DrawText(displaySpeed.ToString(), left + 0.012f, bodyTop + 0.002f, 0.500f * uiScale, 0, 247, 249, 251, 252);
            DrawText(speedUnitText, left + width * 0.43f, bodyTop + 0.027f * uiScale, 0.155f * uiScale, 0, 164, 173, 184, 220);

            float cardW = Math.Max(0.058f, width * 0.29f);
            float cardH = Math.Max(0.045f, bodyH * 0.56f);
            float cardLeft = left + width - cardW - 0.012f;
            float cardTop = bodyTop + 0.008f;
            DrawRect(cardLeft + cardW / 2f, cardTop + cardH / 2f, cardW, cardH,
                     MixColor(or, ar, 0.10f), MixColor(og, ag, 0.10f), MixColor(ob, ab, 0.10f), 248);
            DrawFrame(cardLeft, cardTop, cardW, cardH, ar, ag, ab, 170, 0.0010f);
            DrawText(T("SPEEDO_HEADING"), cardLeft + 0.007f, cardTop + 0.004f, 0.112f * uiScale, 0, 157, 165, 176, 215);
            DrawText(headingDegrees.ToString("000") + "°", cardLeft + 0.007f, cardTop + 0.018f, 0.245f * uiScale, 0, ar, ag, ab, 250);

            DrawText(T("SPEEDO_WATER_SPEED"), left + 0.012f, top + height - 0.019f, 0.120f * uiScale, 0, 132, 141, 153, 205);
            drawAlphaMultiplier = previousAlphaMultiplier;
        }
        catch { drawAlphaMultiplier = 1.0f; }
    }

    private void DrawFlightInstrumentHud()
    {
        if (!showFlightHud || menuOpen || !cachedAircraftHudVehicle || cachedVehicleHandle == 0) return;
        try
        {
            Vehicle vehicle = Game.Player.Character.CurrentVehicle;
            if (vehicle == null || vehicle.Handle != cachedVehicleHandle) return;

            float speedMps = GetInstrumentGroundSpeedMps(vehicle);
            float speedValue = navigationUseMetric ? speedMps * 3.6f : speedMps * 1.94384449f;
            int displaySpeed = Math.Max(0, (int)Math.Round(speedValue));
            string speedUnitText = navigationUseMetric ? "KM/H" : "KTS";

            float altitudeMeters = Math.Max(0.0f, vehicle.Position.Z);
            int altitudeDisplay = navigationUseMetric
                ? (int)Math.Round(altitudeMeters)
                : (int)Math.Round(altitudeMeters * 3.2808399f);
            string altitudeUnit = navigationUseMetric ? "M" : "FT";

            float verticalMps = GetInstrumentVerticalSpeedMps(vehicle);
            string verticalText;
            if (navigationUseMetric)
                verticalText = verticalMps.ToString("+0.0;-0.0;0.0") + " M/S";
            else
                verticalText = Math.Round(verticalMps * 196.850394f).ToString("+0;-0;0") + " FT/MIN";

            float heading = vehicle.Heading;
            while (heading < 0.0f) heading += 360.0f;
            while (heading >= 360.0f) heading -= 360.0f;
            int headingDegrees = (int)Math.Round(heading) % 360;

            string vehicleName = GetInstrumentVehicleName(vehicle);
            float left, top, width, height, uiScale;
            GetInstrumentLayout(out left, out top, out width, out height, out uiScale);
            int ar, ag, ab; GetSkinAccent(out ar, out ag, out ab);
            int br, bg, bb; GetSkinBase(out br, out bg, out bb);
            int or, og, ob; GetSkinOled(out or, out og, out ob);

            float previousAlphaMultiplier = drawAlphaMultiplier;
            drawAlphaMultiplier = previousAlphaMultiplier * Math.Max(0.20f, Math.Min(1.0f, speedometerOpacity / 100.0f));

            DrawRect(left + width / 2f, top + height / 2f, width, height, br, bg, bb, 246);
            DrawFrame(left, top, width, height, ar, ag, ab, IsPremiumThemeSkin() ? 170 : 220, 0.0012f);
            DrawPremiumMiniDecor(left, top, width, height);

            float headerH = Math.Max(0.024f, height * 0.23f);
            DrawRect(left + width / 2f, top + headerH / 2f, width - 0.010f, headerH - 0.004f,
                     MixColor(12, ar, 0.11f), MixColor(16, ag, 0.11f), MixColor(21, ab, 0.11f), 246);
            float headerTextLeft = left + 0.010f;
            int vehicleNameLimit = 22;
            if (speedometerShowCarIcon)
            {
                float logoH = Math.Max(0.014f, Math.Min(0.021f, headerH - 0.007f));
                float logoW = Math.Max(PixelSquareWidth(logoH) * 1.85f, 0.026f);
                float logoLeft = left + 0.008f;
                float logoTop = top + Math.Max(0.002f, (headerH - logoH) * 0.5f);
                DrawRect(logoLeft + logoW * 0.5f, logoTop + logoH * 0.5f, logoW + 0.004f, logoH + 0.004f,
                         MixColor(or, ar, 0.12f), MixColor(og, ag, 0.12f), MixColor(ob, ab, 0.12f), 220);
                bool gtaLogoDrawn = DrawGtaVehicleManufacturerLogo(vehicle, logoLeft, logoTop, logoW, logoH, 245);
                if (!gtaLogoDrawn)
                {
                    float fallbackSize = Math.Min(logoH, 0.019f);
                    DrawUiIcon(7, logoLeft + (logoW - PixelSquareWidth(fallbackSize)) * 0.5f,
                               logoTop + (logoH - fallbackSize) * 0.5f, fallbackSize, ar, ag, ab, 235);
                }
                headerTextLeft = logoLeft + logoW + 0.007f;
                vehicleNameLimit = 17;
            }
            DrawText(Shorten(vehicleName, vehicleNameLimit), headerTextLeft, top + 0.005f, 0.170f * uiScale, 0, 236, 239, 244, 232);
            DrawText(T("SPEEDO_FLIGHT"), left + width - 0.052f, top + 0.006f, 0.125f * uiScale, 0, ar, ag, ab, 210);

            float bodyTop = top + headerH + 0.003f;
            float bodyH = height - headerH - 0.010f;
            DrawRect(left + width / 2f, bodyTop + bodyH / 2f, width - 0.014f, bodyH, or, og, ob, 247);

            DrawText(displaySpeed.ToString(), left + 0.012f, bodyTop + 0.001f, 0.435f * uiScale, 0, 247, 249, 251, 252);
            DrawText(speedUnitText, left + width * 0.34f, bodyTop + 0.021f * uiScale, 0.145f * uiScale, 0, 164, 173, 184, 220);
            DrawText(T("SPEEDO_GROUND_SPEED"), left + 0.012f, bodyTop + 0.052f, 0.110f * uiScale, 0, 132, 141, 153, 205);

            float rightLeft = left + width * 0.54f;
            float rightWidth = width * 0.40f;
            float rowH = Math.Max(0.021f, bodyH * 0.27f);
            float rowGap = 0.004f;
            string[] labels = new string[] { T("SPEEDO_HEADING"), T("SPEEDO_ALTITUDE"), T("SPEEDO_VERTICAL") };
            string[] values = new string[] { headingDegrees.ToString("000") + "°", altitudeDisplay + " " + altitudeUnit, verticalText };
            for (int i = 0; i < 3; i++)
            {
                float rowTop = bodyTop + 0.004f + i * (rowH + rowGap);
                DrawRect(rightLeft + rightWidth / 2f, rowTop + rowH / 2f, rightWidth, rowH,
                         MixColor(or, ar, 0.08f), MixColor(og, ag, 0.08f), MixColor(ob, ab, 0.08f), 242);
                DrawText(labels[i], rightLeft + 0.005f, rowTop + 0.003f, 0.095f * uiScale, 0, 146, 156, 168, 205);
                DrawText(values[i], rightLeft + rightWidth * 0.44f, rowTop + 0.003f, 0.120f * uiScale, 0, ar, ag, ab, 242);
            }

            drawAlphaMultiplier = previousAlphaMultiplier;
        }
        catch { drawAlphaMultiplier = 1.0f; }
    }

    private string GetInstrumentVehicleName(Vehicle vehicle)
    {
        string vehicleName = "VEHICLE";
        try
        {
            string displayName = vehicle.DisplayName;
            string localized = string.IsNullOrEmpty(displayName) ? "" : Game.GetLocalizedString(displayName);
            if (!string.IsNullOrEmpty(localized) && !string.Equals(localized, "NULL", StringComparison.OrdinalIgnoreCase))
                vehicleName = localized.ToUpperInvariant();
            else if (!string.IsNullOrEmpty(displayName))
                vehicleName = displayName.ToUpperInvariant();
        }
        catch { }
        return vehicleName;
    }

    private void GetInstrumentLayout(out float left, out float top, out float width, out float height, out float uiScale)
    {
        left = Math.Max(0.005f, Math.Min(0.845f, speedometerLeft));
        top = Math.Max(0.600f, Math.Min(0.930f, speedometerTop));
        width = Math.Max(0.150f, Math.Min(0.320f, speedometerWidth));
        height = Math.Max(0.095f, Math.Min(0.180f, speedometerHeight));
        if (left + width > 0.992f) left = 0.992f - width;
        if (top + height > 0.985f) top = 0.985f - height;
        float sx = width / 0.205f;
        float sy = height / 0.126f;
        uiScale = Math.Max(0.72f, Math.Min(1.45f, Math.Min(sx, sy)));
    }

    private bool DrawGtaVehicleManufacturerLogo(Vehicle vehicle, float left, float top, float width, float height, int alpha)
    {
        try
        {
            if (vehicle == null) return false;
            int modelHash = vehicle.Model.Hash;
            if (modelHash == 0) return false;

            if (speedometerLogoCachedModelHash != modelHash)
            {
                speedometerLogoCachedModelHash = modelHash;
                speedometerLogoCachedTxd = "";
                speedometerLogoCachedTexture = "";
                speedometerLogoCachedAspect = 1.85f;
                speedometerLogoAspectResolved = false;

                string makeLabel = "";
                try
                {
                    makeLabel = Function.Call<string>((Hash)0xF7AF4F159FF99F97UL, modelHash); // GET_MAKE_NAME_FROM_VEHICLE_MODEL
                }
                catch { makeLabel = ""; }

                makeLabel = (makeLabel ?? "").Trim();
                if (!string.IsNullOrEmpty(makeLabel) &&
                    !string.Equals(makeLabel, "CARNOTFOUND", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(makeLabel, "NULL", StringComparison.OrdinalIgnoreCase))
                {
                    string upper = makeLabel.ToUpperInvariant();
                    // Rockstar's TXD names are lower-case in the game archives.
                    // Keeping the exact native names avoids Enhanced/Legacy case quirks.
                    string txd = "mpcarhud";
                    if (upper == "LCC" || upper == "GROTTI_2" || upper == "PROGEN" || upper == "RUNE") txd = "mpcarhud2";
                    else if (upper == "VYSSER") txd = "mpcarhud3";
                    else if (upper == "MAXWELL") txd = "mpcarhud4";

                    speedometerLogoCachedTxd = txd;
                    speedometerLogoCachedTexture = makeLabel.ToLowerInvariant();
                }
            }

            if (string.IsNullOrEmpty(speedometerLogoCachedTxd) || string.IsNullOrEmpty(speedometerLogoCachedTexture))
                return false;

            // REQUEST_STREAMED_TEXTURE_DICT / HAS_STREAMED_TEXTURE_DICT_LOADED
            Function.Call((Hash)0xDFA2EF8E04127DD5UL, speedometerLogoCachedTxd, false);
            if (!Function.Call<bool>((Hash)0x0145F696AAAAD2E4UL, speedometerLogoCachedTxd)) return false;

            // Validate the actual sprite before drawing it. DRAW_SPRITE may render a
            // plain white placeholder when a TXD is loaded but the requested texture
            // name is missing. Previously that still returned true, so the normal car
            // fallback never appeared. GTA manufacturer logos are real 64x64-ish
            // textures; tiny/invalid resolutions are treated as a failed logo lookup.
            // At the same time preserve the real GTA logo aspect ratio.
            if (!speedometerLogoAspectResolved)
            {
                speedometerLogoAspectResolved = true;
                try
                {
                    GTA.Math.Vector3 textureResolution = Function.Call<GTA.Math.Vector3>(
                        Hash.GET_TEXTURE_RESOLUTION, speedometerLogoCachedTxd, speedometerLogoCachedTexture);
                    if (textureResolution.X < 16.0f || textureResolution.Y < 16.0f)
                    {
                        Log("VEHICLE LOGO FALLBACK | ModelHash=" + modelHash +
                            " | TXD=" + speedometerLogoCachedTxd +
                            " | Texture=" + speedometerLogoCachedTexture +
                            " | Resolution=" + textureResolution.X + "x" + textureResolution.Y);
                        speedometerLogoCachedTxd = "";
                        speedometerLogoCachedTexture = "";
                        return false;
                    }

                    float aspect = textureResolution.X / textureResolution.Y;
                    if (aspect > 0.20f && aspect < 8.0f) speedometerLogoCachedAspect = aspect;
                }
                catch
                {
                    speedometerLogoCachedTxd = "";
                    speedometerLogoCachedTexture = "";
                    return false;
                }
            }

            float maxDrawW = Math.Max(0.001f, width * 0.90f);
            float maxDrawH = Math.Max(0.001f, height * 0.86f);
            float drawH = maxDrawH;
            float drawW = PixelSquareWidth(drawH) * Math.Max(0.20f, speedometerLogoCachedAspect);
            if (drawW > maxDrawW)
            {
                drawW = maxDrawW;
                float screenHeightOverWidth = (float)GTA.UI.Screen.Height / Math.Max(1f, (float)GTA.UI.Screen.Width);
                drawH = drawW / Math.Max(0.0001f, screenHeightOverWidth * Math.Max(0.20f, speedometerLogoCachedAspect));
            }
            drawH = Math.Min(drawH, maxDrawH);

            int effectiveAlpha = (int)Math.Round(Math.Max(0, Math.Min(255, alpha)) * Math.Max(0.0f, Math.Min(1.0f, drawAlphaMultiplier)));
            Function.Call((Hash)0xE7FFAE5EBF23D890UL, // DRAW_SPRITE
                speedometerLogoCachedTxd, speedometerLogoCachedTexture,
                left + width * 0.5f, top + height * 0.5f, drawW, drawH, 0.0f,
                255, 255, 255, effectiveAlpha, false, 0);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void DrawVolumeHud()
    {
        // The HUD belongs to the in-car audio controls. Never leave it floating on foot
        // or on Cycles/BMX. The large radio menu already contains its own volume widget,
        // so the compact HUD is intentionally suppressed while the menu is open.
        if (!IsRadioCapableVehicle() || menuOpen || gtaRadioModeActive)
        {
            volumeHudFade = 0.0f;
            volumeHudFadeUpdatedAt = DateTime.Now;
            if (menuOpen) volumeHudVisibleUntil = DateTime.MinValue;
            return;
        }

        bool targetVisible = DateTime.Now < volumeHudVisibleUntil;
        UpdateVolumeHudFade(targetVisible);
        if (volumeHudFade <= 0.01f) return;

        int ar, ag, ab; GetSkinAccent(out ar, out ag, out ab);
        int br, bg, bb; GetSkinBase(out br, out bg, out bb);
        int or, og, ob; GetSkinOled(out or, out og, out ob);

        // Keep the compact volume HUD in the same source colour as the media mini HUD.
        if (youtubeModeActive || spotifyModeActive)
        {
            if (youtubeModeActive) { ar = 255; ag = 0; ab = 0; }
            else { ar = 30; ag = 215; ab = 96; }
            br = MixColor(br, ar, 0.18f);
            bg = MixColor(bg, ag, 0.18f);
            bb = MixColor(bb, ab, 0.18f);
            or = MixColor(or, ar, 0.10f);
            og = MixColor(og, ag, 0.10f);
            ob = MixColor(ob, ab, 0.10f);
        }
        // Spectrum radio mode follows the current station/category.
        else if (IsSpectrumSkin())
        {
            int tr, tg, tb;
            Station current = Current();
            if (current != null) GetCategoryColor(current.Pack ?? "", out tr, out tg, out tb);
            else GetSkinAccent(out tr, out tg, out tb);
            ar = tr; ag = tg; ab = tb;
            br = MixColor(br, ar, 0.18f);
            bg = MixColor(bg, ag, 0.18f);
            bb = MixColor(bb, ab, 0.18f);
            or = MixColor(or, ar, 0.10f);
            og = MixColor(og, ag, 0.10f);
            ob = MixColor(ob, ab, 0.10f);
        }

        float previousAlphaMultiplier = drawAlphaMultiplier;
        drawAlphaMultiplier = Math.Max(0.0f, Math.Min(1.0f, volumeHudFade));

        // TEST21 layout: right-aligned with the mini radio UI and parked safely
        // above the common bottom-right vehicle HUD zone. When the mini UI is visible,
        // DrawMiniUi() shifts itself just high enough to create a clean vertical stack:
        // Mini UI -> Volume HUD -> Vehicle HUD.
        float width = 0.180f;
        float height = 0.058f;
        float rightEdge = GetCompactHudRightEdge();
        float left = Math.Max(0.010f, rightEdge - width);
        float top = GetVolumeHudTop();

        DrawRect(left + width / 2f, top + height / 2f, width, height, br, bg, bb, 244);
        DrawFrame(left, top, width, height, ar, ag, ab, (youtubeModeActive || spotifyModeActive) ? 205 : (IsPremiumThemeSkin() ? 160 : 220), 0.0012f);
        DrawPremiumMiniDecor(left, top, width, height);

        float innerLeft = left + 0.008f;
        float innerTop = top + 0.008f;
        float innerWidth = width - 0.016f;
        float innerHeight = height - 0.014f;
        DrawRect(innerLeft + innerWidth / 2f, innerTop + innerHeight / 2f, innerWidth, innerHeight, or, og, ob, 246);

        string sourceLabel = youtubeModeActive ? "YOUTUBE" :
                            (spotifyModeActive ? "SPOTIFY" : Shorten(CurrentStationLine(), 18));
        DrawText(T("VOLUME"), innerLeft + 0.008f, innerTop + 0.005f, 0.205f, 0, 238, 241, 245, 240);
        if (youtubeModeActive || spotifyModeActive)
            DrawText(sourceLabel, innerLeft + 0.008f, innerTop + 0.023f, 0.140f, 0, ar, ag, ab, 225);
        else
            DrawText(sourceLabel, innerLeft + 0.008f, innerTop + 0.023f, 0.140f, 0, 164, 173, 184, 215);

        string volumeText = volume + "%";
        DrawText(volumeText, innerLeft + innerWidth - 0.050f, innerTop + 0.004f, 0.285f, 0, ar, ag, ab, 250);

        float barLeft = innerLeft + 0.064f;
        float barTop = innerTop + 0.030f;
        float barWidth = innerWidth - 0.072f;
        float barHeight = 0.007f;
        DrawRect(barLeft + barWidth / 2f, barTop + barHeight / 2f, barWidth, barHeight, 45, 50, 58, 210);
        float fillWidth = barWidth * Math.Max(0.0f, Math.Min(1.0f, volume / 100.0f));
        if (fillWidth > 0.0005f)
            DrawRect(barLeft + fillWidth / 2f, barTop + barHeight / 2f, fillWidth, barHeight, ar, ag, ab, 240);

        // Tiny direction marker gives immediate feedback while holding Num-/Num+.
        string directionText = volumeHudDirection > 0 ? "+" : (volumeHudDirection < 0 ? "-" : "");
        if (!string.IsNullOrEmpty(directionText))
            DrawText(directionText, innerLeft + 0.050f, innerTop + 0.023f, 0.170f, 0, ar, ag, ab, 230);

        drawAlphaMultiplier = previousAlphaMultiplier;
    }

    private void UpdateVolumeHudFade(bool targetVisible)
    {
        DateTime now = DateTime.Now;
        double dt = volumeHudFadeUpdatedAt == DateTime.MinValue ? (1.0 / 60.0) : (now - volumeHudFadeUpdatedAt).TotalSeconds;
        volumeHudFadeUpdatedAt = now;
        if (dt < 0.0) dt = 0.0;
        if (dt > 0.10) dt = 0.10;

        // Fast, polished pop-in; softer fade after the last volume step. Holding +/-
        // continuously refreshes the timer, so the panel stays visible while adjusting.
        float speed = targetVisible ? 9.0f : 4.0f;
        float step = (float)dt * speed;
        volumeHudFade += targetVisible ? step : -step;
        if (volumeHudFade < 0.0f) volumeHudFade = 0.0f;
        if (volumeHudFade > 1.0f) volumeHudFade = 1.0f;
    }

    private void UpdateMiniUiFade(bool targetVisible)
    {
        DateTime now = DateTime.Now;
        double dt = miniUiFadeUpdatedAt == DateTime.MinValue ? (1.0 / 60.0) : (now - miniUiFadeUpdatedAt).TotalSeconds;
        miniUiFadeUpdatedAt = now;
        if (dt < 0.0) dt = 0.0;
        if (dt > 0.10) dt = 0.10;

        // TEST13: YouTube-Mini-UI bleibt nach sichtbarem Cover 2.5 s stehen und blendet dann etwas zuegiger aus.
        float speed = targetVisible ? 7.0f : (IsExternalMusicModeActive() ? 2.5f : 5.0f);
        float step = (float)dt * speed;
        miniUiFade += targetVisible ? step : -step;
        if (miniUiFade < 0.0f) miniUiFade = 0.0f;
        if (miniUiFade > 1.0f) miniUiFade = 1.0f;
    }

    // V12.40: Semantic category palette. These colors stay stable and are used
    // in the Categories screen for every skin. Spectrum uses the same palette as
    // its transition targets, with only a small station-specific variation.
    private void GetCategoryColor(string pack, out int r, out int g, out int b)
    {
        if (string.Equals(pack, FavoritesPackKey, StringComparison.OrdinalIgnoreCase))
        { r = 245; g = 190; b = 64; return; }                    // Favorites = gold
        if (string.Equals(pack, "ALLE SENDER", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(pack))
        { GetSkinAccent(out r, out g, out b); return; }          // All = current skin

        if (string.Equals(pack, "HIP-HOP / URBAN", StringComparison.OrdinalIgnoreCase))
        { r = 245; g = 170; b = 44; return; }                    // warm street / Los Santos gold
        if (string.Equals(pack, "OLD SCHOOL HIP-HOP / RNB", StringComparison.OrdinalIgnoreCase))
        { r = 157; g = 92; b = 204; return; }                    // classic purple
        if (string.Equals(pack, "POP / CHARTS", StringComparison.OrdinalIgnoreCase))
        { r = 235; g = 82; b = 151; return; }                    // pop pink
        if (string.Equals(pack, "ROCK / INDIE / ALTERNATIVE", StringComparison.OrdinalIgnoreCase))
        { r = 221; g = 72; b = 61; return; }                     // rock red
        if (string.Equals(pack, "ELECTRONIC / CLUB", StringComparison.OrdinalIgnoreCase))
        { r = 49; g = 190; b = 226; return; }                    // electric cyan
        if (string.Equals(pack, "SYNTHWAVE / NIGHT DRIVE", StringComparison.OrdinalIgnoreCase))
        { r = 186; g = 68; b = 224; return; }                    // neon violet
        if (string.Equals(pack, "COUNTRY / AMERICANA", StringComparison.OrdinalIgnoreCase))
        { r = 205; g = 139; b = 64; return; }                    // leather / amber
        if (string.Equals(pack, "SOUL / FUNK / REGGAE / WORLD", StringComparison.OrdinalIgnoreCase))
        { r = 74; g = 176; b = 101; return; }                    // organic green
        if (string.Equals(pack, "JAZZ / ECLECTIC", StringComparison.OrdinalIgnoreCase))
        { r = 74; g = 104; b = 190; return; }                    // late-night blue
        if (string.Equals(pack, "NEWS / TALK", StringComparison.OrdinalIgnoreCase))
        { r = 116; g = 133; b = 153; return; }                   // neutral broadcast slate

        GetSkinAccent(out r, out g, out b);
    }

    private bool IsSpectrumSkin()
    {
        return skinIndex >= 0 && skinIndex < skinNames.Length && string.Equals(skinNames[skinIndex], "SPECTRUM", StringComparison.OrdinalIgnoreCase);
    }

    private void GetRadioThemeTarget(out int r, out int g, out int b)
    {
        if (menuTab == 5 || youtubeModeActive)
        {
            r = 92; g = 151; b = 214; // public-build neutral media blue
            return;
        }
        if (menuTab == 6 || spotifyModeActive)
        {
            r = 145; g = 116; b = 202; // public-build neutral media violet
            return;
        }

        string pack = "";
        string stationName = "";
        Station themeStation = null;

        if ((menuTab == 0 || menuTab == 1) && menuIndex >= 0 && menuIndex < stations.Count)
            themeStation = stations[menuIndex];
        else
            themeStation = Current();

        if (menuTab == 2 && packIndex >= 0 && packIndex < menuPacks.Count)
            pack = menuPacks[packIndex] ?? "";
        else if (themeStation != null)
            pack = themeStation.Pack ?? "";

        if (themeStation != null) stationName = themeStation.Name ?? "";

        GetCategoryColor(pack, out r, out g, out b);

        // Spectrum only: a restrained deterministic variation separates stations
        // without destroying the meaning of the category color.
        if (IsSpectrumSkin() && !string.IsNullOrEmpty(stationName))
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < stationName.Length; i++) hash = hash * 31 + char.ToUpperInvariant(stationName[i]);
                int v1 = ((hash & 0x0F) - 7) * 2;
                int v2 = (((hash >> 4) & 0x0F) - 7) * 2;
                int v3 = (((hash >> 8) & 0x0F) - 7) * 2;
                r = ClampColor(r + v1);
                g = ClampColor(g + v2);
                b = ClampColor(b + v3);
            }
        }
    }

    private int ClampColor(int v)
    {
        return Math.Max(0, Math.Min(255, v));
    }

    private int MixColor(int baseValue, int tintValue, float amount)
    {
        amount = Math.Max(0.0f, Math.Min(1.0f, amount));
        return ClampColor((int)Math.Round(baseValue + (tintValue - baseValue) * amount));
    }

    private void GetDynamicMenuTheme(ref int ar, ref int ag, ref int ab, ref int br, ref int bg, ref int bb, ref int pr, ref int pg, ref int pb)
    {
        if (!IsSpectrumSkin()) return;

        UpdateSpectrumPalette();
        dynamicMenuR = spectrumPrimaryR;
        dynamicMenuG = spectrumPrimaryG;
        dynamicMenuB = spectrumPrimaryB;
        dynamicMenuColorInitialized = true;

        float strength = dynamicRadioBackgroundStrength;

        // TEST62: keep the surfaces dark and let Spectrum live in the accents.
        // This prevents large green/purple panel washes while retaining motion.
        br = MixColor(br, spectrumPrimaryR, 0.050f * strength);
        bg = MixColor(bg, spectrumPrimaryG, 0.050f * strength);
        bb = MixColor(bb, spectrumPrimaryB, 0.050f * strength);

        pr = MixColor(pr, spectrumSecondaryR, 0.035f * strength);
        pg = MixColor(pg, spectrumSecondaryG, 0.035f * strength);
        pb = MixColor(pb, spectrumSecondaryB, 0.035f * strength);

        ar = spectrumPrimaryR;
        ag = spectrumPrimaryG;
        ab = spectrumPrimaryB;
    }

    private void DrawDynamicRadioBackdrop(float left, float top, float width, float height)
    {
        if (!IsSpectrumSkin()) return;
        int r = ClampColor((int)Math.Round(dynamicMenuR));
        int g = ClampColor((int)Math.Round(dynamicMenuG));
        int b = ClampColor((int)Math.Round(dynamicMenuB));

        // TEST62: only two very soft ambient washes. Accent rails provide the
        // colour; the panel backgrounds remain near-black for a cleaner head-unit look.
        int r2, g2, b2; GetSpectrumPhaseColor(1, out r2, out g2, out b2);
        DrawRect(left + width * 0.50f, top + height * 0.28f, width * 0.985f, height * 0.34f, r,  g,  b,  10);
        DrawRect(left + width * 0.50f, top + height * 0.73f, width * 0.985f, height * 0.30f, r2, g2, b2, 7);
    }

    private void DrawMenu()
    {
        float left = 0.075f;
        float top = 0.095f;
        float width = 0.850f;
        float height = 0.720f;
        int ar, ag, ab;
        GetSkinAccent(out ar, out ag, out ab);
        int br, bg, bb; GetSkinBase(out br, out bg, out bb);
        int pr, pg, pb; GetSkinPanel(out pr, out pg, out pb);
        GetDynamicMenuTheme(ref ar, ref ag, ref ab, ref br, ref bg, ref bb, ref pr, ref pg, ref pb);

        DrawRect(left + width / 2f, top + height / 2f, width, height, br, bg, bb, 250);
        DrawDynamicRadioBackdrop(left, top, width, height);
        DrawPremiumThemeBackdrop(left, top, width, height);
        int outerR = IsPremiumThemeSkin() ? ar : 116;
        int outerG = IsPremiumThemeSkin() ? ag : 124;
        int outerB = IsPremiumThemeSkin() ? ab : 135;
        if (skinIndex == 10) { outerR = 84; outerG = 91; outerB = 84; }
        if (IsSpectrumSkin()) DrawSpectrumFrame(left, top, width, height, 205, 0.0019f, 0);
        else DrawFrame(left, top, width, height, outerR, outerG, outerB, skinIndex == 10 ? 118 : (IsPremiumThemeSkin() ? 155 : 242), 0.0019f);
        DrawFrame(left + 0.004f, top + 0.004f, width - 0.008f, height - 0.008f, 27, 32, 39, 245, 0.0011f);

        float headerH = 0.066f;
        DrawRect(left + width / 2f, top + headerH / 2f, width - 0.010f, headerH - 0.006f, pr, pg, pb, 249);
        DrawRect(left + width / 2f, top + headerH - 0.003f, width - 0.025f, 0.0015f, 63, 72, 82, 195);
        DrawPremiumHeaderDecor(left + 0.005f, top + 0.003f, width - 0.010f, headerH - 0.006f);
        float headerTextX = left + 0.030f;
        // Clean standard header for every skin: no motif/header artwork.
        bool multimediaHeader = menuTab == 0 || menuTab == 5 || menuTab == 6 || menuTab == 9;
        DrawText(multimediaHeader ? "LOS SANTOS MULTIMEDIA" : "LOS SANTOS INTERNET RADIO", headerTextX, top + 0.012f, 0.395f, 0, 247, 249, 251, 250);
        DrawText("made by st3v3nblub", left + 0.293f, top + 0.021f, 0.160f, 0, ar, ag, ab, 235);
        DrawText(multimediaHeader ? T("INFOTAINMENT") : T("WORLDWIDE_RADIO"), headerTextX + 0.001f, top + 0.043f, 0.170f, 0, 151, 159, 170, 226);
        int lsR = 228, lsG = 231, lsB = 235;
        if (IsSpectrumSkin()) GetSpectrumPhaseColor(1, out lsR, out lsG, out lsB);
        DrawText("LS", left + width - 0.082f, top + 0.013f, 0.390f, 0, lsR, lsG, lsB, 238);

        float bodyTop = top + 0.076f;
        float footerH = 0.044f;
        float bodyBottom = top + height - footerH - 0.010f;
        float bodyH = bodyBottom - bodyTop;

        float navLeft = left + 0.012f;
        float navWidth = 0.118f;
        DrawRect(navLeft + navWidth / 2f, bodyTop + bodyH / 2f, navWidth, bodyH, pr, pg, pb, 248);
        int panelFr = 44, panelFg = 51, panelFb = 60;
        if (IsPremiumThemeSkin())
        {
            int pfr, pfg, pfb; GetThemeFrameAccent(out pfr, out pfg, out pfb);
            panelFr = MixColor(24, pfr, 0.52f);
            panelFg = MixColor(28, pfg, 0.52f);
            panelFb = MixColor(33, pfb, 0.52f);
        }
        DrawFrame(navLeft, bodyTop, navWidth, bodyH, panelFr, panelFg, panelFb, 138, 0.0008f);
        // v7.9: ten tabs including GTA's native radio. Slightly tighter spacing keeps
        // the navigation help card clear without shrinking the actual tab hit area.
        const float navTabStep = 0.041f;
        float navTabTop = bodyTop + 0.018f;
        // Source-first navigation: radios/media together, then library/theme/vehicle/settings/info.
        DrawNavItem(navLeft + 0.007f, navTabTop + navTabStep * 0, navWidth - 0.014f, T("HOME"), 0, menuTab == 0, ar, ag, ab);
        DrawNavItem(navLeft + 0.007f, navTabTop + navTabStep * 1, navWidth - 0.014f, T("STATIONS"), 1, menuTab == 1, ar, ag, ab);
        DrawNavItem(navLeft + 0.007f, navTabTop + navTabStep * 2, navWidth - 0.014f, T("GTA_RADIO"), 12, menuTab == 9, 72, 170, 255);
        DrawNavItem(navLeft + 0.007f, navTabTop + navTabStep * 3, navWidth - 0.014f, "YOUTUBE MUSIC", 5, menuTab == 5, 255, 0, 0);
        DrawNavItem(navLeft + 0.007f, navTabTop + navTabStep * 4, navWidth - 0.014f, "SPOTIFY", 6, menuTab == 6, 30, 215, 96);
        DrawNavItem(navLeft + 0.007f, navTabTop + navTabStep * 5, navWidth - 0.014f, T("PACKS"), 2, menuTab == 2, ar, ag, ab);
        DrawNavItem(navLeft + 0.007f, navTabTop + navTabStep * 6, navWidth - 0.014f, T("SKINS"), 4, menuTab == 4, ar, ag, ab);
        DrawNavItem(navLeft + 0.007f, navTabTop + navTabStep * 7, navWidth - 0.014f, T("CAR_SETTINGS_NAV"), 7, menuTab == 7, ar, ag, ab);
        DrawNavItem(navLeft + 0.007f, navTabTop + navTabStep * 8, navWidth - 0.014f, T("SETTINGS_NAV"), 8, menuTab == 8, ar, ag, ab);
        DrawNavItem(navLeft + 0.007f, navTabTop + navTabStep * 9, navWidth - 0.014f, T("INFO"), 3, menuTab == 3, ar, ag, ab);
        // V12.45: keep the navigation help card visually centered in the free
        // lower part of the left rail. The old card sat too close to the footer,
        // which looked especially uneven with the premium theme border rails.
        float navHelpLeft = navLeft + 0.010f;
        float navHelpWidth = navWidth - 0.020f;
        float navHelpHeight = 0.092f;
        float navHelpBottom = bodyBottom - 0.050f;
        float navHelpTop = navHelpBottom - navHelpHeight;
        DrawRect(navHelpLeft + navHelpWidth / 2f, navHelpTop + navHelpHeight / 2f, navHelpWidth, navHelpHeight, 13, 17, 22, 244);
        int navHelpFr = 50, navHelpFg = 58, navHelpFb = 68;
        if (IsPremiumThemeSkin())
        {
            int hfr, hfg, hfb; GetThemeFrameSecondary(out hfr, out hfg, out hfb);
            navHelpFr = MixColor(28, hfr, 0.42f);
            navHelpFg = MixColor(32, hfg, 0.42f);
            navHelpFb = MixColor(36, hfb, 0.42f);
        }
        DrawFrame(navHelpLeft, navHelpTop, navHelpWidth, navHelpHeight, navHelpFr, navHelpFg, navHelpFb, skinIndex == 10 ? 72 : (IsPremiumThemeSkin() ? 86 : 78), 0.0007f);
        DrawText("NUM4 / NUM6", navHelpLeft + 0.012f, navHelpTop + 0.015f, 0.180f, 0, ar, ag, ab, 235);
        DrawText(T("CHANGE_TAB"), navHelpLeft + 0.012f, navHelpTop + 0.041f, 0.158f, 0, 191, 196, 204, 222);
        DrawText("NUM0  " + T("EXIT"), navHelpLeft + 0.012f, navHelpTop + 0.067f, 0.158f, 0, 191, 196, 204, 222);

        // v7.38: wider, consistently padded right rail. This gives status labels
        // enough room and reserves a fixed bottom slot for audio reduction.
        float rightWidth = 0.158f;
        float rightLeft = left + width - rightWidth - 0.012f;
        float rightPad = 0.012f;
        float rightInnerLeft = rightLeft + rightPad;
        float rightInnerWidth = rightWidth - rightPad * 2f;
        DrawRect(rightLeft + rightWidth / 2f, bodyTop + bodyH / 2f, rightWidth, bodyH, pr, pg, pb, 248);
        DrawFrame(rightLeft, bodyTop, rightWidth, bodyH, panelFr, panelFg, panelFb, 138, 0.0008f);
        if (menuTab == 0)
        {
            DrawHomeSidePanel(rightInnerLeft, bodyTop + 0.016f, rightInnerWidth, ar, ag, ab);
        }
        else if (menuTab == 5)
        {
            DrawYouTubeSidePanel(rightInnerLeft, bodyTop + 0.016f, rightInnerWidth, ar, ag, ab);
        }
        else if (menuTab == 6)
        {
            DrawSpotifySidePanel(rightInnerLeft, bodyTop + 0.016f, rightInnerWidth, ar, ag, ab);
        }
        else if (menuTab == 7)
        {
            DrawCarSettingsSidePanel(rightInnerLeft, bodyTop + 0.016f, rightInnerWidth, ar, ag, ab);
        }
        else if (menuTab == 8)
        {
            DrawSettingsSidePanel(rightInnerLeft, bodyTop + 0.016f, rightInnerWidth, ar, ag, ab);
        }
        else if (menuTab == 9)
        {
            DrawGtaRadioSidePanel(rightInnerLeft, bodyTop + 0.016f, rightInnerWidth, ar, ag, ab);
        }
        else
        {
            DrawVolumeWidget(rightInnerLeft, bodyTop + 0.016f, rightInnerWidth, 0.132f, volume / 100.0f, ar, ag, ab);
            DrawControlButton(rightInnerLeft, bodyTop + 0.166f, rightInnerWidth, "NUM-   " + T("QUIETER"), false, ar, ag, ab);
            DrawControlButton(rightInnerLeft, bodyTop + 0.216f, rightInnerWidth, "NUM+   " + T("LOUDER"), false, ar, ag, ab);
            DrawControlButton(rightInnerLeft, bodyTop + 0.266f, rightInnerWidth, "NUM1   " + T("POWER"), enabled, ar, ag, ab);
            DrawControlButton(rightInnerLeft, bodyTop + 0.316f, rightInnerWidth, "NUM3   " + T("STOP"), false, ar, ag, ab);
        }
        if (duckActive)
        {
            int dfr = 226, dfg = 180, dfb = 72;
            if (IsPremiumThemeSkin()) GetThemeFrameSecondary(out dfr, out dfg, out dfb);
            DrawWidgetCardWithRightValue(
                rightInnerLeft, bodyBottom - 0.073f, rightInnerWidth, 0.056f,
                T("AUDIO_REDUCTION"), LocalizeDuckReason(), duckVolumePercent + "%",
                8, dfr, dfg, dfb, true);
        }

        float centerLeft = navLeft + navWidth + 0.014f;
        float centerRight = rightLeft - 0.014f;
        float centerWidth = centerRight - centerLeft;

        if (menuTab == 1)
            DrawSenderTab(centerLeft, bodyTop, centerWidth, bodyBottom, ar, ag, ab);
        else if (menuTab == 2)
            DrawPackTab(centerLeft, bodyTop, centerWidth, bodyBottom, ar, ag, ab);
        else if (menuTab == 3)
            DrawInfoTab(centerLeft, bodyTop, centerWidth, bodyBottom, ar, ag, ab);
        else if (menuTab == 4)
            DrawSkinTab(centerLeft, bodyTop, centerWidth, bodyBottom, ar, ag, ab);
        else if (menuTab == 5)
            DrawYouTubeMusicTab(centerLeft, bodyTop, centerWidth, bodyBottom, ar, ag, ab);
        else if (menuTab == 6)
            DrawSpotifyMusicTab(centerLeft, bodyTop, centerWidth, bodyBottom, ar, ag, ab);
        else if (menuTab == 7)
            DrawCarSettingsTab(centerLeft, bodyTop, centerWidth, bodyBottom, ar, ag, ab);
        else if (menuTab == 8)
            DrawSettingsTab(centerLeft, bodyTop, centerWidth, bodyBottom, ar, ag, ab);
        else if (menuTab == 9)
            DrawGtaRadioTab(centerLeft, bodyTop, centerWidth, bodyBottom, ar, ag, ab);
        else
            DrawHomeTab(centerLeft, bodyTop, centerWidth, bodyBottom, ar, ag, ab);

        if (IsPremiumThemeSkin())
        {
            int cfr, cfg, cfb; GetThemeFrameAccent(out cfr, out cfg, out cfb);
            int csr, csg, csb; GetThemeFrameSecondary(out csr, out csg, out csb);
            DrawFrame(centerLeft, bodyTop, centerWidth, bodyBottom - bodyTop, cfr, cfg, cfb, skinIndex == 10 ? 76 : 84, 0.0007f);
            if (skinIndex == 5)
            {
                DrawSpectrumRail(centerLeft + centerWidth * 0.55f, bodyTop + 0.003f, centerWidth * 0.38f, 0.0010f, 185, 1);
                DrawSpectrumRail(centerLeft + centerWidth * 0.70f, bodyBottom - 0.003f, centerWidth * 0.22f, 0.0010f, 165, 3);
            }
            else if (skinIndex == 8) DrawRect(centerLeft + centerWidth * 0.72f, bodyTop + 0.003f, centerWidth * 0.26f, 0.0011f, csr, csg, csb, 205);
            else if (skinIndex == 9) DrawRect(centerLeft + centerWidth * 0.88f, bodyBottom - 0.003f, centerWidth * 0.20f, 0.0011f, csr, csg, csb, 205);
            else if (skinIndex == 11)
            {
                DrawRect(centerLeft + centerWidth * 0.86f, bodyTop + 0.003f, centerWidth * 0.12f, 0.0010f, csr, csg, csb, 195);
                DrawRect(centerLeft + centerWidth * 0.74f, bodyBottom - 0.003f, centerWidth * 0.16f, 0.0010f, ar, ag, ab, 185);
            }
            else if (skinIndex == 12)
            {
                DrawRect(centerLeft + centerWidth * 0.82f, bodyTop + 0.003f, centerWidth * 0.14f, 0.0010f, csr, csg, csb, 180);
                DrawRect(centerLeft + centerWidth * 0.70f, bodyBottom - 0.003f, centerWidth * 0.18f, 0.0010f, ar, ag, ab, 170);
            }
        }

        float footerTop = top + height - footerH;
        int far = ar, fag = ag, fab = ab;
        if (menuTab == 9 || gtaRadioModeActive)
        {
            far = 72; fag = 170; fab = 255;
        }
        else if (IsSpectrumSkin() && youtubeModeActive)
        {
            far = 224; fag = 52; fab = 68;
        }
        else if (IsSpectrumSkin() && spotifyModeActive)
        {
            far = 29; fag = 185; fab = 84;
        }
        else if (menuTab == 2 && packIndex >= 0 && packIndex < menuPacks.Count)
            GetCategoryColor(menuPacks[packIndex], out far, out fag, out fab);
        else if ((menuTab == 0 || menuTab == 1) && menuIndex >= 0 && menuIndex < stations.Count)
            GetCategoryColor(stations[menuIndex].Pack ?? "", out far, out fag, out fab);
        bool footerUseTheme = menuTab <= 2 || menuTab == 9 || gtaRadioModeActive || (IsSpectrumSkin() && (menuTab == 5 || youtubeModeActive || menuTab == 6 || spotifyModeActive));
        DrawRect(left + width / 2f, footerTop + footerH / 2f, width - 0.010f, footerH - 0.006f, 12, 17, 23, 248);
        int footerFrameR = far, footerFrameG = fag, footerFrameB = fab;
        if (IsPremiumThemeSkin()) GetThemeFrameAccent(out footerFrameR, out footerFrameG, out footerFrameB);
        DrawFrame(left + 0.005f, footerTop + 0.003f, width - 0.010f, footerH - 0.006f, footerFrameR, footerFrameG, footerFrameB, (footerUseTheme || IsPremiumThemeSkin() ? 88 : 58), 0.0007f);
        if (IsPremiumThemeSkin())
        {
            int fsr, fsg, fsb; GetSkinSecondaryAccent(out fsr, out fsg, out fsb);
            if (skinIndex == 11)
            {
                DrawRect(left + width * 0.76f, footerTop + 0.005f, width * 0.14f, 0.0012f, ar, ag, ab, 210);
                DrawRect(left + width * 0.90f, footerTop + 0.005f, width * 0.07f, 0.0012f, fsr, fsg, fsb, 205);
            }
            else if (skinIndex == 12)
            {
                DrawRect(left + width * 0.75f, footerTop + 0.005f, width * 0.12f, 0.0012f, ar, ag, ab, 195);
                DrawRect(left + width * 0.88f, footerTop + 0.005f, width * 0.07f, 0.0012f, fsr, fsg, fsb, 190);
                DrawRect(left + width * 0.96f, footerTop + 0.005f, width * 0.025f, 0.0012f, 232, 226, 207, 135);
            }
            else DrawRect(left + width * 0.80f, footerTop + 0.005f, width * 0.18f, 0.0014f, fsr, fsg, fsb, 220);
        }
        DrawText("NUM4/6  " + T("TAB_SHORT"), left + 0.025f, footerTop + 0.0115f, 0.166f, 0, far, fag, fab, 235);
        bool settingsStyleTab = menuTab == 7 || menuTab == 8;
        DrawText((menuTab == 5 || menuTab == 6) ? "NUM8/2  " + T("TRACK_NAV") : (menuTab == 0 ? "NUM8/2  " + T("HOME_APPS") : (settingsStyleTab ? "NUM8/2  " + T("SETTING_SHORT") : "NUM8/2  " + T("NAV"))), left + 0.180f, footerTop + 0.0115f, 0.166f, 0, footerUseTheme ? far : 207, footerUseTheme ? fag : 212, footerUseTheme ? fab : 220, 230);
        DrawText(menuTab == 5 ? "NUM5  " + T("OPEN_YT") : (menuTab == 6 ? "NUM5  " + T("OPEN_SPOTIFY") : (menuTab == 0 ? "NUM5  " + T("OPEN") : (settingsStyleTab ? "NUM5  " + T("APPLY") : "NUM5  " + T("SELECT")))), left + 0.305f, footerTop + 0.0115f, 0.166f, 0, footerUseTheme ? far : 207, footerUseTheme ? fag : 212, footerUseTheme ? fab : 220, 230);
        DrawText(settingsStyleTab ? "NUM-/+  " + T("VALUE") : (menuTab == 9 ? T("GTA_AUDIO_SHORT") : "NUM-/+  " + T("VOLUME_SHORT")), left + 0.485f, footerTop + 0.0115f, 0.166f, 0, footerUseTheme ? far : 207, footerUseTheme ? fag : 212, footerUseTheme ? fab : 220, 230);
        DrawText("NUM1  " + T("POWER"), left + 0.610f, footerTop + 0.0115f, 0.166f, 0, footerUseTheme ? far : 207, footerUseTheme ? fag : 212, footerUseTheme ? fab : 220, 230);
        DrawText("NUM0  " + T("EXIT"), left + width - 0.108f, footerTop + 0.0115f, 0.166f, 0, far, fag, fab, 235);
    }

    // TEST60: app-style home dashboard. Rendering is frame-only and reuses already
    // cached metadata/covers, so the new look does not introduce disk/network polling.
    private void DrawHomeTab(float left, float top, float width, float bottom, int ar, int ag, int ab)
    {
        float h = bottom - top;
        DrawRect(left + width / 2f, top + h / 2f, width, h, 9, 13, 18, 248);
        DrawFrame(left, top, width, h, 39, 47, 57, 185, 0.0009f);

        Station current = Current();
        bool yt = youtubeModeActive;
        bool sp = spotifyModeActive;
        bool gt = gtaRadioModeActive;
        string source = yt ? "YOUTUBE MUSIC" : (sp ? "SPOTIFY" : (gt ? T("GTA_RADIO") : "INTERNET RADIO"));
        string title = yt ? youtubeTitle : (sp ? spotifyTitle : (gt ? CurrentGtaRadioStationName() : (current != null ? current.Name : T("NO_STATION"))));
        string subtitle = yt ? youtubeArtist : (sp ? spotifyArtist : "");
        string radioGenre = gt ? GetGtaRadioGenre(GetCurrentGtaRadioStationEnum()) : ((!yt && !sp && current != null) ? (current.Genre ?? "") : "");
        string radioMetadata = gt ? GetGtaRadioNowPlayingLine() : ((!yt && !sp) ? GetNowPlayingLine() : "");
        string state = yt ? LocalizeYouTubeState() : (sp ? LocalizeSpotifyState() : (gt ? T(gtaRadioPlayingRequested ? "STATE_PLAYING" : "STATE_OFF") : (enabled ? MapState(cachedAudioState) : T("STATE_OFF"))));
        if (string.IsNullOrEmpty(title)) title = yt ? T("YT_START_HINT") : (sp ? T("SPOTIFY_START_HINT") : T("NO_STATION"));
        if ((yt || sp) && string.IsNullOrEmpty(subtitle)) subtitle = T("MEDIA_SESSION");
        if (!yt && !sp && string.IsNullOrEmpty(radioGenre)) radioGenre = T("GENRE") + ": Mix";
        if (!yt && !sp && string.IsNullOrEmpty(radioMetadata)) radioMetadata = T("NO_METADATA");

        int sr = ar, sg = ag, sb = ab;
        if (yt) { sr = 255; sg = 0; sb = 0; }
        else if (sp) { sr = 145; sg = 116; sb = 202; }
        else if (gt) { sr = 72; sg = 170; sb = 255; }
        else if (current != null) GetCategoryColor(current.Pack ?? "", out sr, out sg, out sb);
        else if (IsSpectrumSkin()) GetSpectrumPhaseColor(0, out sr, out sg, out sb);

        float heroH = 0.194f;
        float heroLeft = left + 0.014f;
        float heroWidth = width - 0.028f;
        DrawRect(heroLeft + heroWidth / 2f, top + heroH / 2f, heroWidth, heroH, 10, 15, 20, 242);
        DrawFrame(heroLeft, top, heroWidth, heroH, sr, sg, sb, 78, 0.0007f);
        DrawRect(heroLeft + 0.004f, top + heroH / 2f, 0.0028f, heroH - 0.024f, sr, sg, sb, 175);

        float artLeft = heroLeft + 0.018f;
        float artTop = top + 0.024f;
        float artH = 0.142f;
        float artW = PixelSquareWidth(artH);
        DrawMediaCoverShell(artLeft, artTop, artW, artH, sr, sg, sb, yt || sp || gt || enabled);
        bool artDrawn = yt ? DrawYouTubeCover(artLeft, artTop, artW, artH) : (sp ? DrawSpotifyCover(artLeft, artTop, artW, artH) : false);
        if (!artDrawn && gt)
        {
            DrawRect(artLeft + artW / 2f, artTop + artH / 2f, artW, artH, 13, 20, 28, 248);
            float logoPadX = artW * 0.10f;
            float logoPadY = artH * 0.10f;
            bool gtaOriginalDrawn = TryDrawGtaRadioOriginalGameLogo(
                artLeft + logoPadX, artTop + logoPadY, artW - logoPadX * 2f, artH - logoPadY * 2f,
                GetCurrentGtaRadioStationEnum(), gtaRadioPlayingRequested);
            if (!gtaOriginalDrawn)
                DrawUiIcon(12, artLeft + artW * 0.20f, artTop + artH * 0.24f, 0.050f, sr, sg, sb, 240);
            DrawText("GTA RADIO", artLeft + 0.007f, artTop + artH - 0.030f, 0.120f, 0, 214, 228, 242, 228);
            artDrawn = true;
        }
        if (!artDrawn && !yt && !sp && !gt && current != null)
        {
            DrawStationLogoBadge(artLeft, artTop, artW, artH, current, sr, sg, sb);
            artDrawn = true;
        }
        if (!artDrawn)
        {
            DrawRect(artLeft + artW / 2f, artTop + artH / 2f, artW, artH, 17, 23, 30, 248);
            int fallbackIcon = yt ? 5 : (sp ? 6 : (gt ? 12 : 10));
            float iconH = Math.Min(0.048f, artH * 0.38f);
            DrawUiIcon(fallbackIcon, artLeft + artW * 0.20f, artTop + artH * 0.27f, iconH, sr, sg, sb, 235);
            DrawText(yt ? "YOUTUBE MUSIC" : (sp ? "SPOTIFY" : (gt ? "GTA RADIO" : "LS RADIO")), artLeft + 0.008f, artTop + artH - 0.030f, 0.125f, 0, 207, 214, 223, 222);
        }

        float heroText = artLeft + artW + 0.022f;
        DrawText(T("NOW_PLAYING"), heroText, top + 0.017f, 0.165f, 0, sr, sg, sb, 235);
        DrawText(Shorten(title, 30), heroText, top + 0.045f, 0.335f, 0, 246, 248, 250, 248);

        if (yt || sp)
        {
            DrawText(Shorten(subtitle, 44), heroText, top + 0.086f, 0.205f, 0, 188, 196, 207, 230);
        }
        else
        {
            // Radio cards get a dedicated genre row, aligned to the artwork frame,
            // instead of mixing genre/metadata into the station title.
            DrawText(Shorten(radioGenre, 42), heroText, top + 0.082f, 0.177f, 0, sr, sg, sb, 228);
            DrawText(Shorten(radioMetadata, 48), heroText, top + 0.106f, 0.163f, 0, 178, 187, 199, 218);
        }

        float sourceLineX = heroText;
        if (yt || sp || gt)
        {
            int providerIcon = yt ? 5 : (sp ? 6 : 12);
            float providerIconH = 0.015f;
            DrawUiIcon(providerIcon, heroText, top + 0.135f, providerIconH, yt ? 255 : (sp ? 30 : 72), yt ? 0 : (sp ? 215 : 170), yt ? 0 : (sp ? 96 : 255), 245);
            sourceLineX += PixelSquareWidth(providerIconH) + 0.008f;
        }
        DrawText(source + "  |  " + T("STATUS") + ": " + state, sourceLineX, top + 0.137f, 0.152f, 0, sr, sg, sb, 220);
        DrawEqualizer(heroLeft + heroWidth - 0.102f, top + 0.076f, 0.074f, 0.032f, sr, sg, sb);

        float sectionTop = top + heroH + 0.014f;
        int appsR = ar, appsG = ag, appsB = ab;
        if (IsSpectrumSkin()) GetSpectrumPhaseColor(0, out appsR, out appsG, out appsB);
        DrawUiIcon(2, left + 0.019f, sectionTop + 0.001f, 0.020f, appsR, appsG, appsB, 235);
        DrawText(T("HOME_APPS"), left + 0.046f, sectionTop, 0.205f, 0, 238, 241, 245, 240);
        DrawText(T("HOME_HINT"), left + 0.135f, sectionTop + 0.004f, 0.150f, 0, 154, 163, 175, 215);

        // TEST67: head-unit style app launcher -- three apps per row, two rows.
        float gridTop = sectionTop + 0.033f;
        float gapX = 0.010f;
        float gapY = 0.016f;
        float gridLeft = left + 0.014f;
        float gridWidth = width - 0.028f;
        float tileWTop = (gridWidth - gapX * 3f) / 4f;
        float tileWBottom = (gridWidth - gapX * 2f) / 3f;
        float tileH = Math.Max(0.136f, Math.Min(0.162f, (bottom - gridTop - gapY - 0.012f) / 2f));
        float tx1 = gridLeft;
        float tx2 = tx1 + tileWTop + gapX;
        float tx3 = tx2 + tileWTop + gapX;
        float tx4 = tx3 + tileWTop + gapX;
        float bx1 = gridLeft;
        float bx2 = bx1 + tileWBottom + gapX;
        float bx3 = bx2 + tileWBottom + gapX;

        string radioSub = current != null ? Shorten(current.Name, 19) : T("NO_STATION");
        string gtaSub = gtaRadioStations.Count > 0 ? Shorten(gt && !string.IsNullOrEmpty(GetGtaRadioNowPlayingLine()) ? GetGtaRadioNowPlayingLine() : CurrentGtaRadioStationName(), 19) : T("NO_STATION");
        string ytSub = youtubeAvailable ? (string.IsNullOrEmpty(youtubeTitle) ? T("CONNECTED") : Shorten(youtubeTitle, 19)) : T("NOT_CONNECTED");
        string spSub = spotifyAvailable ? (string.IsNullOrEmpty(spotifyTitle) ? T("CONNECTED") : Shorten(spotifyTitle, 19)) : T("NOT_CONNECTED");
        string carSub = T("BEAT_NEON") + ": " + T(audioReactiveNeonEnabled ? "ON" : "OFF");
        string skinSub = (skinIndex >= 0 && skinIndex < skinNames.Length) ? skinNames[skinIndex] : T("ACTIVE");
        string settingsSub = GetLanguageDisplayName() + "  |  " + volume + "%";

        int carR = ar, carG = ag, carB = ab;
        int skinsR = ar, skinsG = ag, skinsB = ab;
        int settingsR = ar, settingsG = ag, settingsB = ab;
        if (IsSpectrumSkin())
        {
            GetSpectrumPhaseColor(0, out carR, out carG, out carB);
            GetSpectrumPhaseColor(0, out skinsR, out skinsG, out skinsB);
            GetSpectrumPhaseColor(0, out settingsR, out settingsG, out settingsB);
        }

        DrawHomeTile(tx1, gridTop, tileWTop, tileH, "RADIO", radioSub, 1, homeMenuIndex == 0, enabled && !yt && !sp && !gt, sr, sg, sb);
        DrawHomeTile(tx2, gridTop, tileWTop, tileH, "GTA RADIO", gtaSub, 12, homeMenuIndex == 1, gt, 72, 170, 255);
        DrawHomeTile(tx3, gridTop, tileWTop, tileH, "YOUTUBE", ytSub, 5, homeMenuIndex == 2, yt, 255, 0, 0);
        DrawHomeTile(tx4, gridTop, tileWTop, tileH, "SPOTIFY", spSub, 6, homeMenuIndex == 3, sp, 30, 215, 96);
        DrawHomeTile(bx1, gridTop + tileH + gapY, tileWBottom, tileH, T("CAR_SETTINGS_NAV"), carSub, 7, homeMenuIndex == 4, audioReactiveNeonEnabled, carR, carG, carB);
        DrawHomeTile(bx2, gridTop + tileH + gapY, tileWBottom, tileH, T("SKINS"), skinSub, 4, homeMenuIndex == 5, true, skinsR, skinsG, skinsB);
        DrawHomeTile(bx3, gridTop + tileH + gapY, tileWBottom, tileH, T("SETTINGS_NAV"), settingsSub, 8, homeMenuIndex == 6, false, settingsR, settingsG, settingsB);
    }

    private void DrawHomeTile(float left, float top, float width, float height, string label, string detail, int icon, bool selected, bool active, int cr, int cg, int cb)
    {
        int rr = selected ? MixColor(10, cr, 0.08f) : 10;
        int rg = selected ? MixColor(14, cg, 0.08f) : 14;
        int rb = selected ? MixColor(18, cb, 0.08f) : 18;
        DrawRect(left + width / 2f, top + height / 2f, width, height, rr, rg, rb, selected ? 246 : 236);
        DrawFrame(left, top, width, height, cr, cg, cb, selected ? 76 : (active ? 44 : 22), 0.00060f);

        float iconH = Math.Min(0.074f, height * 0.54f);
        float iconW = PixelSquareWidth(iconH);
        float labelY = top + height - 0.052f;
        float iconAreaTop = top + 0.013f;
        float iconAreaBottom = labelY - 0.004f;
        float iconTop = iconAreaTop + Math.Max(0f, (iconAreaBottom - iconAreaTop - iconH) / 2f);
        float iconLeft = left + (width - iconW) / 2f;
        if (icon == 12) DrawSymmetricMediaIconTile(iconLeft, iconTop, iconW, iconH, cr, cg, cb, selected || active);
        else DrawMediaIconTile(icon, iconLeft, iconTop, iconW, iconH, cr, cg, cb, selected || active);
        bool originalGtaTileLogo = false;
        if (icon == 12 && gtaRadioStations.Count > 0)
        {
            float padX = iconW * 0.035f;
            float padY = iconH * 0.035f;
            originalGtaTileLogo = TryDrawGtaRadioOriginalGameLogo(
                iconLeft + padX, iconTop + padY, iconW - padX * 2f, iconH - padY * 2f,
                GetCurrentGtaRadioStationEnum(), active);
        }
        if (!originalGtaTileLogo)
            DrawCenteredUiIconInTile(icon, iconLeft, iconTop, iconW, iconH, cr, cg, cb, selected ? 244 : 228);

        DrawText(label, left + 0.014f, labelY, 0.186f, 0, 242, 245, 248, 238);
        DrawText(Shorten(detail ?? "", 20), left + 0.014f, labelY + 0.024f, 0.122f, 0, 147, 157, 170, 208);

        float railW = width - 0.024f;
        DrawRect(left + 0.012f + railW / 2f, top + height - 0.004f, railW, selected ? 0.0021f : 0.0013f, cr, cg, cb, selected ? 195 : 112);
        if (selected)
            DrawText(">", left + width - 0.021f, labelY - 0.001f, 0.205f, 0, cr, cg, cb, 226);

        if (active)
        {
            float chipW = 0.044f;
            DrawRect(left + width - chipW / 2f - 0.010f, top + 0.013f, chipW, 0.016f, MixColor(12, cr, 0.14f), MixColor(15, cg, 0.14f), MixColor(18, cb, 0.14f), 214);
            DrawText(T("ACTIVE"), left + width - chipW - 0.004f, top + 0.006f, 0.098f, 0, cr, cg, cb, 214);
        }
    }

    private void DrawRadioTab(float left, float top, float width, float bottom, int ar, int ag, int ab)
    {
        int or, og, ob; GetSkinOled(out or, out og, out ob);
        float oledHeight = 0.144f;
        DrawRect(left + width / 2f, top + oledHeight / 2f, width, oledHeight, or, og, ob, 252);
        DrawFrame(left, top, width, oledHeight, ar, ag, ab, 170, 0.0014f);
        Station current = Current();
        string np = GetNowPlayingLine();
        float radioLogoH = 0.100f;
        float radioLogoW = PixelSquareWidth(radioLogoH) * 1.05f;
        float radioLogoLeft = left + 0.014f;
        float textLeft = radioLogoLeft + radioLogoW + 0.018f;
        if (current != null) DrawStationLogoBadge(radioLogoLeft, top + 0.018f, radioLogoW, radioLogoH, current, ar, ag, ab);
        DrawText(Shorten(current != null ? current.Name : T("NO_STATION"), 34), textLeft, top + 0.018f, 0.390f, 0, 237, 255, 239, 250);
        if (shouldPlay) DrawText(T("ON_AIR"), textLeft, top + 0.048f, 0.155f, 0, ar, ag, ab, 245);
        DrawText(Shorten(string.IsNullOrEmpty(np) ? T("NO_METADATA") : np, 55), textLeft, top + 0.073f, 0.235f, 0, 183, 241, 193, 236);
        int rcr = ar, rcg = ag, rcb = ab;
        if (current != null) GetCategoryColor(current.Pack ?? "", out rcr, out rcg, out rcb);
        DrawText(Shorten(current != null ? (DisplayPackName(current.Pack) + " | " + (current.Genre ?? "")) : "", 60), textLeft, top + 0.101f, 0.175f, 0, rcr, rcg, rcb, 230);
        DrawEqualizer(left + width - 0.180f, top + 0.062f, 0.140f, 0.044f, ar, ag, ab);

        float listTop = top + oledHeight + 0.014f;
        float infoHeight = 0.092f;
        float infoTop = bottom - infoHeight;
        DrawStationList(left, listTop, width, infoTop - listTop - 0.012f, menuIndex, false, ar, ag, ab);
        DrawStationInfoCard(left, infoTop, width, infoHeight, menuIndex, ar, ag, ab);
    }

    private void DrawSenderTab(float left, float top, float width, float bottom, int ar, int ag, int ab)
    {
        float titleH = 0.075f;
        DrawRect(left + width / 2f, top + titleH / 2f, width, titleH, 12, 18, 24, 248);
        DrawFrame(left, top, width, titleH, 45, 54, 64, 220, 0.0011f);
        DrawText(T("STATIONS"), left + 0.018f, top + 0.014f, 0.345f, 0, 244, 247, 250, 245);
        DrawText(string.IsNullOrEmpty(selectedPack) ? T("ALL_STATIONS") : DisplayPackName(selectedPack), left + 0.018f, top + 0.045f, 0.190f, 0, ar, ag, ab, 230);
        DrawText(T("SPACE_KEY") + " = " + T("FAVORITE_SHORT"), left + width - 0.205f, top + 0.045f, 0.158f, 0, 174, 181, 191, 215);

        float listTop = top + titleH + 0.012f;
        float infoHeight = 0.100f;
        float infoTop = bottom - infoHeight;
        DrawStationList(left, listTop, width, infoTop - listTop - 0.012f, menuIndex, true, ar, ag, ab);
        if (filteredStationIndices.Count == 0 && string.Equals(selectedPack, FavoritesPackKey, StringComparison.OrdinalIgnoreCase))
            DrawText(T("NO_FAVORITES"), left + 0.035f, listTop + 0.115f, 0.265f, 0, 185, 191, 200, 225);
        DrawStationInfoCard(left, infoTop, width, infoHeight, filteredStationIndices.Count > 0 ? menuIndex : -1, ar, ag, ab);
    }

    private string DisplayPackName(string pack)
    {
        if (string.IsNullOrEmpty(pack)) return "";
        if (string.Equals(pack, "ALLE SENDER", StringComparison.OrdinalIgnoreCase)) return T("ALL_STATIONS");
        if (string.Equals(pack, FavoritesPackKey, StringComparison.OrdinalIgnoreCase)) return T("FAVORITES") + " " + favoriteStationNames.Count + "/" + MaxFavorites;
        bool en = uiLanguage == "EN";
        bool es = uiLanguage == "ES";
        bool fr = uiLanguage == "FR";
        bool it = uiLanguage == "IT";
        bool pt = uiLanguage == "PT";
        bool tr = uiLanguage == "TR";
        if (string.Equals(pack, "HIP-HOP / URBAN", StringComparison.OrdinalIgnoreCase)) return "HIP-HOP / URBAN";
        if (string.Equals(pack, "OLD SCHOOL HIP-HOP / RNB", StringComparison.OrdinalIgnoreCase)) return "OLD SCHOOL HIP-HOP / R&B";
        if (string.Equals(pack, "POP / CHARTS", StringComparison.OrdinalIgnoreCase)) return es ? "POP / ÉXITOS" : (fr ? "POP / CLASSEMENTS" : (it ? "POP / CLASSIFICHE" : (pt ? "POP / PARADAS" : (tr ? "POP / LİSTELER" : "POP / CHARTS"))));
        if (string.Equals(pack, "ROCK / INDIE / ALTERNATIVE", StringComparison.OrdinalIgnoreCase)) return es ? "ROCK / INDIE / ALTERNATIVO" : (fr ? "ROCK / INDIE / ALTERNATIF" : (it ? "ROCK / INDIE / ALTERNATIVO" : (pt ? "ROCK / INDIE / ALTERNATIVO" : (tr ? "ROCK / INDIE / ALTERNATİF" : "ROCK / INDIE / ALTERNATIVE"))));
        if (string.Equals(pack, "ELECTRONIC / CLUB", StringComparison.OrdinalIgnoreCase)) return es ? "ELECTRÓNICA / CLUB" : (fr ? "ÉLECTRONIQUE / CLUB" : (it ? "ELETTRONICA / CLUB" : (pt ? "ELETRÔNICA / CLUB" : (tr ? "ELEKTRONİK / KULÜP" : (en ? "ELECTRONIC / CLUB" : "ELEKTRONIK / CLUB")))));
        if (string.Equals(pack, "SYNTHWAVE / NIGHT DRIVE", StringComparison.OrdinalIgnoreCase)) return es ? "SYNTHWAVE / NOCHE" : (fr ? "SYNTHWAVE / NUIT" : (it ? "SYNTHWAVE / GUIDA NOTTURNA" : (pt ? "SYNTHWAVE / NOITE" : (tr ? "SYNTHWAVE / GECE SÜRÜŞÜ" : (en ? "SYNTHWAVE / NIGHT DRIVE" : "SYNTHWAVE / NACHTFAHRT")))));
        if (string.Equals(pack, "COUNTRY / AMERICANA", StringComparison.OrdinalIgnoreCase)) return "COUNTRY / AMERICANA";
        if (string.Equals(pack, "SOUL / FUNK / REGGAE / WORLD", StringComparison.OrdinalIgnoreCase)) return es ? "SOUL / FUNK / REGGAE / MUNDO" : (fr ? "SOUL / FUNK / REGGAE / MONDE" : (it ? "SOUL / FUNK / REGGAE / MONDO" : (pt ? "SOUL / FUNK / REGGAE / MUNDO" : (tr ? "SOUL / FUNK / REGGAE / DÜNYA" : (en ? "SOUL / FUNK / REGGAE / WORLD" : "SOUL / FUNK / REGGAE / WELT")))));
        if (string.Equals(pack, "JAZZ / ECLECTIC", StringComparison.OrdinalIgnoreCase)) return es ? "JAZZ / ECLÉCTICO" : (fr ? "JAZZ / ÉCLECTIQUE" : (it ? "JAZZ / ECLETTICO" : (pt ? "JAZZ / ECLÉTICO" : (tr ? "JAZZ / EKLEKTİK" : (en ? "JAZZ / ECLECTIC" : "JAZZ / EKLEKTISCH")))));
        if (string.Equals(pack, "NEWS / TALK", StringComparison.OrdinalIgnoreCase)) return es ? "NOTICIAS / CHARLA" : (fr ? "ACTUALITÉS / DÉBATS" : (it ? "NOTIZIE / PARLATO" : (pt ? "NOTÍCIAS / CONVERSA" : (tr ? "HABER / SOHBET" : (en ? "NEWS / TALK" : "NACHRICHTEN / TALK")))));
        return pack;
    }

    private void DrawPackTab(float left, float top, float width, float bottom, int ar, int ag, int ab)
    {
        DrawRect(left + width / 2f, top + 0.048f, width, 0.096f, 12, 18, 24, 248);
        DrawFrame(left, top, width, 0.096f, 45, 54, 64, 220, 0.0011f);
        DrawText(T("PACKS_GROUPS"), left + 0.018f, top + 0.016f, 0.350f, 0, 244, 247, 250, 245);
        DrawText(T("PACK_HINT"), left + 0.018f, top + 0.055f, 0.185f, 0, 174, 181, 191, 220);

        float rowTop = top + 0.112f;
        float rowH = 0.058f;
        int visible = 7;
        int start = packIndex - visible / 2;
        if (start < 0) start = 0;
        if (start > Math.Max(0, menuPacks.Count - visible)) start = Math.Max(0, menuPacks.Count - visible);
        for (int j = 0; j < visible && start + j < menuPacks.Count; j++)
        {
            int i = start + j;
            float y = rowTop + j * rowH;
            bool selected = i == packIndex;
            bool active = string.Equals(menuPacks[i], string.IsNullOrEmpty(selectedPack) ? "ALLE SENDER" : selectedPack, StringComparison.OrdinalIgnoreCase);
            int cr, cg, cb;
            GetCategoryColor(menuPacks[i], out cr, out cg, out cb);
            int rowR = selected ? Math.Max(12, cr / 7) : 13;
            int rowG = selected ? Math.Max(15, cg / 7) : 18;
            int rowB = selected ? Math.Max(16, cb / 7) : 24;
            DrawRect(left + width / 2f, y + rowH * 0.43f, width - 0.018f, rowH * 0.76f, rowR, rowG, rowB, selected ? 225 : 235);
            if (selected) DrawFrame(left + 0.009f, y + 0.004f, width - 0.018f, rowH * 0.76f, cr, cg, cb, 225, 0.0011f);
            DrawRect(left + 0.026f, y + rowH * 0.43f, 0.010f, rowH * 0.48f, cr, cg, cb, 235);
            DrawText((selected ? ">  " : "   ") + DisplayPackName(menuPacks[i]), left + 0.038f, y + 0.009f, 0.245f, 0, 244, 247, 250, 240);
            DrawText(CountStationsInPack(menuPacks[i]) + " " + T("STATION_COUNT"), left + width - 0.115f, y + 0.011f, 0.185f, 0, active ? cr : 180, active ? cg : 186, active ? cb : 195, 225);
            if (active) DrawText(T("ACTIVE"), left + width - 0.195f, y + 0.011f, 0.150f, 0, cr, cg, cb, 235);
        }
    }

    private void DrawSkinTab(float left, float top, float width, float bottom, int ar, int ag, int ab)
    {
        DrawRect(left + width / 2f, top + 0.050f, width, 0.100f, 12, 18, 24, 248);
        DrawFrame(left, top, width, 0.100f, 45, 54, 64, 220, 0.0011f);
        DrawText(T("SKINS_THEMES"), left + 0.018f, top + 0.016f, 0.350f, 0, 244, 247, 250, 245);
        DrawText(T("SKIN_HINT"), left + 0.018f, top + 0.057f, 0.185f, 0, 174, 181, 191, 220);

        float rowTop = top + 0.116f;
        float availableRowsH = Math.Max(0.300f, (bottom - 0.055f) - rowTop);
        float rowH = Math.Min(0.068f, availableRowsH / Math.Max(1, skinNames.Length));
        float rowInnerH = rowH * 0.76f;
        float chipW = 0.048f;
        float chipH = Math.Min(0.026f, rowH * 0.60f);
        for (int i = 0; i < skinNames.Length; i++)
        {
            float y = rowTop + i * rowH;
            if (y + rowH > bottom - 0.012f) break;
            bool selected = i == skinMenuIndex;
            bool active = i == skinIndex;
            int sr = 61, sg = 238, sb = 105;
            if (i == 1) { sr = 67; sg = 158; sb = 255; }
            else if (i == 2) { sr = 245; sg = 72; sb = 72; }
            else if (i == 3) { sr = 244; sg = 174; sb = 64; }
            else if (i == 4) { sr = 225; sg = 230; sb = 236; }
            else if (i == 5)
            {
                if (IsSpectrumSkin()) GetSpectrumPhaseColor(0, out sr, out sg, out sb);
                else { sr = 232; sg = 92; sb = 220; }
            }
            else if (i == 6) { sr = 255; sg = 124; sb = 196; }
            else if (i == 7) { sr = 126; sg = 201; sb = 71; }
            else if (i == 8) { sr = 0; sg = 229; sb = 255; }
            else if (i == 9) { sr = 157; sg = 231; sb = 67; }
            else if (i == 10) { sr = 109; sg = 182; sb = 72; }
            else if (i == 11) { sr = 212; sg = 178; sb = 98; }
            else if (i == 12) { sr = 98; sg = 190; sb = 184; }

            float rowTopInner = y + 0.004f;
            float rowCenterY = y + rowH * 0.43f;
            float chipLeft = left + 0.021f;
            float chipTop = rowCenterY - chipH / 2f;
            float chipCx = chipLeft + chipW / 2f;

            DrawRect(left + width / 2f, rowCenterY, width - 0.018f, rowInnerH, selected ? 18 : 13, selected ? 26 : 18, selected ? 31 : 24, selected ? 245 : 235);
            if (selected) DrawFrame(left + 0.009f, rowTopInner, width - 0.018f, rowInnerH, sr, sg, sb, 225, 0.0011f);

            DrawRect(chipCx, rowCenterY, chipW, chipH, 20, 24, 29, 240);
            DrawFrame(chipLeft, chipTop, chipW, chipH, 42, 48, 56, 175, 0.0008f);

            if (i == 0)
            {
                DrawRect(chipCx, rowCenterY, chipW, chipH, 61, 238, 105, 236);
            }
            else if (i == 1)
            {
                DrawRect(chipCx, rowCenterY, chipW, chipH, 67, 158, 255, 236);
            }
            else if (i == 2)
            {
                DrawRect(chipCx, rowCenterY, chipW, chipH, 245, 72, 72, 236);
            }
            else if (i == 3)
            {
                DrawRect(chipCx, rowCenterY, chipW, chipH, 244, 174, 64, 236);
            }
            else if (i == 4)
            {
                DrawRect(chipCx, rowCenterY, chipW, chipH, 228, 232, 238, 236);
            }
            else if (i == 5)
            {
                if (IsSpectrumSkin())
                {
                    DrawSpectrumRail(chipLeft, rowCenterY, chipW, chipH, 238, 0);
                }
                else
                {
                    float bandW = chipW / 3f;
                    DrawRect(chipLeft + bandW * 0.5f, rowCenterY, bandW, chipH, 245, 170, 44, 238);
                    DrawRect(chipLeft + bandW * 1.5f, rowCenterY, bandW, chipH, 49, 190, 226, 238);
                    DrawRect(chipLeft + bandW * 2.5f, rowCenterY, bandW, chipH, 232, 92, 220, 238);
                }
            }
            else if (i == 6)
            {
                DrawRect(chipCx, rowCenterY, chipW, chipH, 236, 118, 191, 238);
                DrawRect(chipLeft + chipW * 0.82f, rowCenterY, chipW * 0.18f, chipH, 57, 221, 255, 238);
            }
            else if (i == 7)
            {
                DrawRect(chipCx, rowCenterY, chipW, chipH, 132, 191, 73, 238);
                DrawRect(chipLeft + chipW * 0.83f, rowCenterY, chipW * 0.16f, chipH, 214, 174, 76, 238);
            }
            else if (i == 8)
            {
                DrawRect(chipCx, rowCenterY, chipW, chipH, 18, 24, 31, 238);
                DrawRect(chipLeft + chipW * 0.32f, chipTop + chipH * 0.22f, chipW * 0.42f, 0.0016f, 243, 214, 55, 220);
                DrawRect(chipLeft + chipW * 0.32f, chipTop + chipH * 0.75f, chipW * 0.42f, 0.0016f, 0, 229, 255, 220);
                DrawRect(chipLeft + chipW * 0.90f, rowCenterY, chipW * 0.10f, chipH, 228, 48, 47, 228);
            }
            else if (i == 9)
            {
                DrawRect(chipCx, rowCenterY, chipW, chipH, 29, 31, 33, 238);
                DrawRect(chipLeft + chipW * 0.38f, chipTop + chipH * 0.22f, chipW * 0.50f, 0.0017f, 157, 231, 67, 225);
                DrawRect(chipLeft + chipW * 0.90f, rowCenterY, chipW * 0.10f, chipH, 198, 204, 207, 228);
            }
            else if (i == 10)
            {
                // V12.54: preview chip stays clean; the real grass block remains only in the live header.
                DrawRect(chipCx, rowCenterY, chipW, chipH, 108, 75, 45, 238);
                DrawRect(chipCx, chipTop + chipH * 0.18f, chipW, chipH * 0.34f, 103, 175, 76, 238);
                DrawRect(chipLeft + chipW * 0.82f, rowCenterY, chipW * 0.16f, chipH, 126, 88, 56, 222);
            }
            else if (i == 11)
            {
                DrawRect(chipCx, rowCenterY, chipW, chipH, 19, 19, 23, 238);
                DrawRect(chipLeft + chipW * 0.38f, chipTop + chipH * 0.22f, chipW * 0.56f, 0.0016f, 212, 178, 98, 220);
                DrawRect(chipLeft + chipW * 0.38f, chipTop + chipH * 0.78f, chipW * 0.42f, 0.0013f, 212, 178, 98, 200);
                DrawRect(chipLeft + chipW * 0.90f, rowCenterY, chipW * 0.10f, chipH, 104, 18, 30, 220);
            }
            else if (i == 12)
            {
                DrawRect(chipCx, rowCenterY, chipW, chipH, 20, 31, 32, 238);
                DrawRect(chipLeft + chipW * 0.32f, chipTop + chipH * 0.24f, chipW * 0.48f, 0.0016f, 98, 190, 184, 220);
                DrawRect(chipLeft + chipW * 0.72f, rowCenterY, chipW * 0.18f, chipH, 235, 126, 157, 224);
                DrawRect(chipLeft + chipW * 0.92f, rowCenterY, chipW * 0.08f, chipH, 224, 88, 83, 215);
            }

            DrawText((selected ? ">  " : "   ") + skinNames[i], left + 0.085f, rowCenterY - 0.013f, 0.285f, 0, 244, 247, 250, 240);
            DrawText(active ? T("ACTIVE") : T("PREVIEW"), left + width - 0.112f, rowCenterY - 0.0115f, 0.185f, 0, active ? sr : 177, active ? sg : 184, active ? sb : 193, 225);
        }

        DrawText(T("SKIN_HELP"), left + 0.020f, bottom - 0.045f, 0.175f, 0, 174, 181, 191, 215);
    }

    private void DrawLegalNoticeCard(float left, float top, float width, int ar, int ag, int ab)
    {
        float height = 0.068f;
        DrawRect(left + width / 2f, top + height / 2f, width, height, 11, 15, 20, 238);
        DrawFrame(left, top, width, height, ar, ag, ab, 44, 0.00055f);
        DrawUiIcon(3, left + 0.009f, top + 0.008f, 0.017f, ar, ag, ab, 218);
        DrawText(T("LEGAL"), left + 0.031f, top + 0.006f, 0.145f, 0, ar, ag, ab, 225);
        DrawText(Shorten(T("LEGAL_NOTICE_1"), 92), left + 0.009f, top + 0.023f, 0.130f, 0, 190, 197, 207, 216);
        DrawText(Shorten(T("LEGAL_NOTICE_2"), 92), left + 0.009f, top + 0.038f, 0.124f, 0, 158, 167, 178, 205);
        DrawText(Shorten(T("LEGAL_NOTICE_3"), 92), left + 0.009f, top + 0.053f, 0.121f, 0, ar, ag, ab, 202);
    }

    private void DrawInfoTab(float left, float top, float width, float bottom, int ar, int ag, int ab)
    {
        Station current = Current();
        DrawRect(left + width / 2f, top + (bottom - top) / 2f, width, bottom - top, 10, 15, 20, 248);
        DrawFrame(left, top, width, bottom - top, 45, 54, 64, 220, 0.0011f);
        DrawText(T("INFO"), left + 0.020f, top + 0.018f, 0.360f, 0, 244, 247, 250, 245);
        if (current != null)
        {
            float infoLogoH = 0.100f;
            float infoLogoW = PixelSquareWidth(infoLogoH) * 1.05f;
            float infoLogoLeft = left + 0.020f;
            float infoTextLeft = infoLogoLeft + infoLogoW + 0.020f;
            DrawStationLogoBadge(infoLogoLeft, top + 0.072f, infoLogoW, infoLogoH, current, ar, ag, ab);
            DrawText(Shorten(current.Name, 40), infoTextLeft, top + 0.074f, 0.360f, 0, 244, 247, 250, 245);
            string np = GetNowPlayingLine();
            DrawText(Shorten(string.IsNullOrEmpty(np) ? T("NO_METADATA") : np, 62), infoTextLeft, top + 0.112f, 0.230f, 0, 180, 239, 190, 230);
            DrawInfoRow(left + 0.022f, top + 0.205f, width - 0.044f, T("STATUS"), MapState(cachedAudioState), ar, ag, ab);
            DrawInfoRow(left + 0.022f, top + 0.255f, width - 0.044f, T("PACK"), DisplayPackName(current.Pack), ar, ag, ab);
            DrawInfoRow(left + 0.022f, top + 0.305f, width - 0.044f, T("GENRE"), current.Genre ?? "-", ar, ag, ab);
            DrawInfoRow(left + 0.022f, top + 0.355f, width - 0.044f, T("VIBE"), current.Vibe ?? "-", ar, ag, ab);
            DrawInfoRow(left + 0.022f, top + 0.405f, width - 0.044f, T("VOLUME"), volume + "%", ar, ag, ab);
            DrawInfoRow(left + 0.022f, top + 0.455f, width - 0.044f, T("DUCKING"), duckActive ? LocalizeDuckReason() : T("READY"), ar, ag, ab);
        }
        DrawLegalNoticeCard(left + 0.022f, top + 0.505f, width - 0.044f, ar, ag, ab);
        DrawText(T("INFO_HELP"), left + 0.022f, bottom - 0.020f, 0.165f, 0, 174, 181, 191, 205);
    }

    private string GetYouTubeNowPlayingLine()
    {
        if (!string.IsNullOrEmpty(youtubeArtist) && !string.IsNullOrEmpty(youtubeTitle)) return youtubeArtist + " - " + youtubeTitle;
        if (!string.IsNullOrEmpty(youtubeTitle)) return youtubeTitle;
        return "";
    }

    private string GetSpotifyNowPlayingLine()
    {
        if (!string.IsNullOrEmpty(spotifyArtist) && !string.IsNullOrEmpty(spotifyTitle)) return spotifyArtist + " - " + spotifyTitle;
        if (!string.IsNullOrEmpty(spotifyTitle)) return spotifyTitle;
        return "";
    }

    private void DrawYouTubeMusicTab(float left, float top, float width, float bottom, int ar, int ag, int ab)
    {
        int yr = 255, yg = 0, yb = 0;
        float h = bottom - top;
        DrawRect(left + width / 2f, top + h / 2f, width, h, 10, 13, 18, 250);
        DrawFrame(left, top, width, h, 54, 61, 70, 220, 0.0012f);

        float coverLeft = left + 0.020f;
        float coverTop = top + 0.018f;
        float coverH = 0.118f;
        float coverW = PixelSquareWidth(coverH);
        DrawMediaCoverShell(coverLeft, coverTop, coverW, coverH, yr, yg, yb, youtubeModeActive);
        bool coverDrawn = DrawYouTubeCover(coverLeft, coverTop, coverW, coverH);
        if (!coverDrawn)
        {
            DrawRect(coverLeft + coverW / 2f, coverTop + coverH / 2f, coverW, coverH, 31, 15, 21, 246);
            DrawUiIcon(5, coverLeft + coverW * 0.18f, coverTop + coverH * 0.23f, 0.045f, yr, yg, yb, 235);
            DrawText("YOUTUBE MUSIC", coverLeft + 0.006f, coverTop + coverH - 0.028f, 0.104f, 0, 229, 205, 211, 230);
        }

        float serviceTextLeft = coverLeft + coverW + 0.022f;
        DrawText("YOUTUBE MUSIC", serviceTextLeft, top + 0.020f, 0.380f, 0, 247, 249, 251, 248);
        DrawText(T("MEDIA_SESSION"), serviceTextLeft, top + 0.060f, 0.180f, 0, 173, 180, 190, 220);
        DrawText(youtubeAvailable ? T("CONNECTED") : T("NOT_CONNECTED"), left + width - 0.125f, top + 0.024f, 0.175f, 0, youtubeAvailable ? 91 : 230, youtubeAvailable ? 230 : 94, youtubeAvailable ? 124 : 94, 235);
        DrawText(T("VOLUME_SHORT") + " " + volume + "%  |  " + T("YT_OUT_SHORT") + " " + MapUserVolumeToYouTube(volume) + "%", serviceTextLeft, top + 0.096f, 0.185f, 0, duckActive ? 248 : 195, duckActive ? 205 : 201, duckActive ? 105 : 210, 225);

        float cardTop = top + 0.155f;
        float cardH = 0.170f;
        DrawRect(left + width / 2f, cardTop + cardH / 2f, width - 0.035f, cardH, 16, 20, 27, 246);
        DrawFrame(left + 0.0175f, cardTop, width - 0.035f, cardH, yr, yg, yb, youtubeModeActive ? 180 : 80, 0.0011f);
        DrawText(T("NOW_PLAYING"), left + 0.035f, cardTop + 0.016f, 0.175f, 0, yr, yg, yb, 235);
        DrawText(Shorten(string.IsNullOrEmpty(youtubeTitle) ? T("YT_START_HINT") : youtubeTitle, 48), left + 0.035f, cardTop + 0.050f, 0.330f, 0, 245, 247, 250, 246);
        DrawText(Shorten(string.IsNullOrEmpty(youtubeArtist) ? T("YT_START_SONG") : youtubeArtist, 58), left + 0.035f, cardTop + 0.092f, 0.225f, 0, 205, 211, 220, 232);
        DrawText(Shorten(string.IsNullOrEmpty(youtubeAlbum) ? "" : youtubeAlbum, 58), left + 0.035f, cardTop + 0.124f, 0.190f, 0, 164, 172, 184, 218);
        DrawText(Shorten(T("STATUS") + ": " + LocalizeYouTubeState(), 30), left + width - 0.155f, cardTop + 0.144f, 0.175f, 0, youtubeAvailable ? yr : 170, youtubeAvailable ? yg : 176, youtubeAvailable ? yb : 184, 225);

        float controlsTop = cardTop + cardH + 0.020f;
        DrawInfoRow(left + 0.022f, controlsTop, width - 0.044f, "NUM8", T("PREVIOUS_TRACK"), yr, yg, yb);
        DrawInfoRow(left + 0.022f, controlsTop + 0.046f, width - 0.044f, "NUM5", T("OPEN_YT"), yr, yg, yb);
        DrawInfoRow(left + 0.022f, controlsTop + 0.092f, width - 0.044f, "NUM2", T("NEXT_TRACK"), yr, yg, yb);
        DrawInfoRow(left + 0.022f, controlsTop + 0.138f, width - 0.044f, "NUM-/+", T("VOLUME"), yr, yg, yb);
        DrawText(Shorten(T("SOURCE") + ": " + (string.IsNullOrEmpty(youtubeSource) ? T("MEDIA_SESSION") : youtubeSource), 62), left + 0.025f, bottom - 0.070f, 0.170f, 0, 177, 184, 194, 220);
        DrawText(duckActive ? (T("DUCKING") + ": " + LocalizeDuckReason()) : T("YT_EXCLUSIVE"), left + 0.025f, bottom - 0.044f, 0.175f, 0, duckActive ? 248 : 177, duckActive ? 205 : 184, duckActive ? 105 : 194, 220);
    }

    private void UpdateYouTubeCoverSprite()
    {
        // Mini-HUD track detection must work even when artwork is disabled.
        // Artwork remains opt-in; only the metadata/timer is independent from cover loading.
        string trackKey = (youtubeTitle ?? "") + "|" + (youtubeArtist ?? "") + "|" + (youtubeAlbum ?? "");
        if (!string.Equals(trackKey, youtubeCoverTrackKey, StringComparison.Ordinal))
        {
            youtubeCoverTrackKey = trackKey;
            youtubeCoverSprite = null;
            youtubeCoverLoadedPath = "";
            if (youtubeModeActive && (!string.IsNullOrEmpty(youtubeTitle) || !string.IsNullOrEmpty(youtubeArtist)))
            {
                youtubeMiniHideAt = DateTime.Now.AddMilliseconds(4200);
                miniUiFadeUpdatedAt = DateTime.MinValue;
            }
        }

        if (!mediaArtworkEnabled)
        {
            youtubeCoverSprite = null;
            youtubeCoverLoadedPath = "";
            youtubeCoverPath = "";
            return;
        }

        // V12.8: Nur extern normalisierte ytm_ready_*.png-Dateien duerfen als DirectX-Textur geladen werden.
        // Der externe Cover-Worker schreibt Bilder atomar fertig, bevor die Bridge den Pfad meldet.
        if (DateTime.Now < youtubeCoverNextLoadCheck) return;
        youtubeCoverNextLoadCheck = DateTime.Now.AddMilliseconds(600);
        if (string.IsNullOrEmpty(youtubeCoverPath)) return;
        if (string.Equals(youtubeCoverLoadedPath, youtubeCoverPath, StringComparison.OrdinalIgnoreCase) && youtubeCoverSprite != null) return;

        try
        {
            if (!File.Exists(youtubeCoverPath)) return;
            string coverName = Path.GetFileName(youtubeCoverPath) ?? "";
            if (!coverName.StartsWith("ytm_ready_", StringComparison.OrdinalIgnoreCase) ||
                !coverName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                Log("YT COVER BLOCKIERT | nicht normalisierter Pfad | " + youtubeCoverPath);
                return;
            }
            FileInfo fi = new FileInfo(youtubeCoverPath);
            if (fi.Length < 2048 || fi.Length > 4194304) return;

            // Kleine Signaturpruefung: verhindert, dass GTA versucht, kaputte/halbe Dateien als Textur zu laden.
            bool validImage = false;
            using (FileStream fs = new FileStream(youtubeCoverPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                int b0 = fs.ReadByte();
                int b1 = fs.ReadByte();
                int b2 = fs.ReadByte();
                int b3 = fs.ReadByte();
                validImage = (b0 == 0x89 && b1 == 0x50 && b2 == 0x4E && b3 == 0x47);
            }
            if (!validImage)
            {
                Log("YT COVER IGNORIERT | ungueltige Bildsignatur | " + youtubeCoverPath);
                return;
            }

            if (youtubeCoverSprite != null)
                FlushCustomSpriteInstances(youtubeCoverSprite, Math.Max(4, youtubeCoverDrawCountPreviousFrame));
            youtubeCoverDrawCountThisFrame = youtubeCoverDrawCountPreviousFrame = 0;
            youtubeCoverSprite = new GTA.UI.CustomSprite(
                youtubeCoverPath,
                new System.Drawing.SizeF(128f, 128f),
                new System.Drawing.PointF(0f, 0f),
                System.Drawing.Color.White);
            youtubeCoverLoadedPath = youtubeCoverPath;
            if (youtubeModeActive)
                Log("YT MINI COVER READY | Mini UI timer remains independent from artwork");
            Log("YT COVER READY | " + youtubeCoverPath + " | " + fi.Length + " Bytes");
        }
        catch (Exception ex)
        {
            youtubeCoverSprite = null;
            youtubeCoverLoadedPath = "";
            Log("YT COVER LOAD FEHLER | " + ex.Message);
        }
    }

    private bool DrawYouTubeCover(float left, float top, float width, float height)
    {
        if (!mediaArtworkEnabled) return false;
        if (drawAlphaMultiplier <= 0.01f) return false;
        // Frame-sicher: hier gibt es keinerlei File.Exists/FileInfo/Stream/Thumbnail-Zugriffe.
        if (youtubeCoverSprite == null) return false;
        try
        {
            youtubeCoverSprite.Size = new System.Drawing.SizeF(width * GTA.UI.Screen.Width, height * GTA.UI.Screen.Height);
            youtubeCoverSprite.Position = new System.Drawing.PointF(left * GTA.UI.Screen.Width, top * GTA.UI.Screen.Height);
            int coverAlpha = Math.Max(0, Math.Min(255, (int)Math.Round(255.0f * drawAlphaMultiplier)));
            youtubeCoverSprite.Color = System.Drawing.Color.FromArgb(coverAlpha, 255, 255, 255);
            youtubeCoverSprite.Draw();
            youtubeCoverDrawCountThisFrame++;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void DrawWidgetCard(float left, float top, float width, float height, string title, string value, int icon, int ar, int ag, int ab, bool strong)
    {
        int bgR = strong ? MixColor(10, ar, 0.07f) : 11;
        int bgG = strong ? MixColor(14, ag, 0.07f) : 15;
        int bgB = strong ? MixColor(18, ab, 0.07f) : 20;
        DrawRect(left + width / 2f, top + height / 2f, width, height, bgR, bgG, bgB, strong ? 240 : 232);
        DrawFrame(left, top, width, height, ar, ag, ab, strong ? 54 : 26, 0.00055f);
        DrawRect(left + width / 2f, top + 0.0032f, width * 0.72f, 0.0007f, 236, 240, 245, strong ? 24 : 12);
        float iconH = 0.030f;
        float iconW = PixelSquareWidth(iconH);
        float iconLeft = left + 0.010f;
        float iconTop = top + 0.008f;
        DrawMediaIconTile(icon, iconLeft, iconTop, iconW, iconH, ar, ag, ab, strong);
        DrawCenteredUiIconInCompactTile(icon, iconLeft, iconTop, iconW, iconH, ar, ag, ab, 226);
        DrawText(title, iconLeft + iconW + 0.008f, top + 0.010f, 0.142f, 0, 158, 168, 180, 214);
        DrawText(Shorten(value ?? "-", 24), left + 0.010f, top + 0.036f, 0.164f, 0, 236, 240, 245, 230);
    }

    private void DrawWidgetCardWithRightValue(float left, float top, float width, float height, string title, string valueLeft, string valueRight, int icon, int ar, int ag, int ab, bool strong)
    {
        int bgR = strong ? MixColor(10, ar, 0.07f) : 11;
        int bgG = strong ? MixColor(14, ag, 0.07f) : 15;
        int bgB = strong ? MixColor(18, ab, 0.07f) : 20;
        DrawRect(left + width / 2f, top + height / 2f, width, height, bgR, bgG, bgB, strong ? 240 : 232);
        DrawFrame(left, top, width, height, ar, ag, ab, strong ? 54 : 26, 0.00055f);
        DrawRect(left + width / 2f, top + 0.0032f, width * 0.72f, 0.0007f, 236, 240, 245, strong ? 24 : 12);
        float iconH = 0.030f;
        float iconW = PixelSquareWidth(iconH);
        float iconLeft = left + 0.010f;
        float iconTop = top + 0.008f;
        DrawMediaIconTile(icon, iconLeft, iconTop, iconW, iconH, ar, ag, ab, strong);
        DrawCenteredUiIconInCompactTile(icon, iconLeft, iconTop, iconW, iconH, ar, ag, ab, 226);
        DrawText(title, iconLeft + iconW + 0.008f, top + 0.010f, 0.142f, 0, 158, 168, 180, 214);
        DrawText(Shorten(valueLeft ?? "-", 14), left + 0.010f, top + 0.036f, 0.128f, 0, 236, 240, 245, 230);
        DrawText(Shorten(valueRight ?? "-", 8), left + width - 0.040f, top + 0.036f, 0.146f, 0, ar, ag, ab, 224);
    }

    private void DrawHomeSidePanel(float left, float top, float width, int ar, int ag, int ab)
    {
        Station current = Current();
        int sr = ar, sg = ag, sb = ab;
        string source = "RADIO";
        string sourceState = enabled ? MapState(cachedAudioState) : T("STATE_OFF");

        if (youtubeModeActive)
        {
            source = "YOUTUBE MUSIC";
            sourceState = LocalizeYouTubeState();
            sr = 255; sg = 0; sb = 0;
        }
        else if (spotifyModeActive)
        {
            source = "SPOTIFY";
            sourceState = LocalizeSpotifyState();
            sr = 30; sg = 215; sb = 96;
        }
        else if (menuTab == 9 || gtaRadioModeActive)
        {
            source = T("GTA_RADIO");
            sourceState = gtaRadioPlayingRequested ? T("STATE_PLAYING") : T("STATE_OFF");
            sr = 72; sg = 170; sb = 255;
        }
        else if (current != null)
        {
            GetCategoryColor(current.Pack ?? "", out sr, out sg, out sb);
        }

        if (gtaRadioModeActive)
            DrawWidgetCard(left, top, width, 0.128f, T("VOLUME"), T("GTA_RADIO_GAME_VOLUME"), 8, sr, sg, sb, false);
        else
            DrawVolumeWidget(left, top, width, 0.128f, volume / 100.0f, sr, sg, sb);
        float y = top + 0.152f;
        DrawWidgetCard(left, y, width, 0.064f, T("SOURCE"), source + "  ·  " + sourceState, 10, sr, sg, sb, true);
        y += 0.078f;
        DrawWidgetCardWithRightValue(left, y, width, 0.064f, T("CAR_SETTINGS_NAV"), T("BEAT_NEON"), T(audioReactiveNeonEnabled ? "ON" : "OFF"), 11, ar, ag, ab, audioReactiveNeonEnabled);
        y += 0.078f;
        DrawWidgetCardWithRightValue(left, y, width, 0.064f, T("SETTINGS_NAV"), GetLanguageDisplayName(), gtaRadioModeActive ? T("GTA_AUDIO_SHORT") : (volume + "%"), 8, ar, ag, ab, false);
        y += 0.078f;
        string skin = (skinIndex >= 0 && skinIndex < skinNames.Length) ? skinNames[skinIndex] : "-";
        DrawWidgetCard(left, y, width, 0.064f, T("SKINS"), skin, 4, ar, ag, ab, false);
    }

    private void DrawYouTubeSidePanel(float left, float top, float width, int ar, int ag, int ab)
    {
        int yr = 255, yg = 0, yb = 0;
        // v7.38: compact media rail; controls finish above the fixed bottom status.
        DrawVolumeWidget(left, top, width, 0.116f, volume / 100.0f, yr, yg, yb);
        float y = top + 0.132f;
        DrawWidgetCard(left, y, width, 0.058f, T("APP_VOLUME"), MapUserVolumeToYouTube(volume) + "%", 5, yr, yg, yb, true);
        y += 0.070f;
        DrawWidgetCard(left, y, width, 0.058f, T("CONNECTION"), youtubeAvailable ? T("CONNECTED") : T("NOT_CONNECTED"), 10, yr, yg, yb, youtubeAvailable);
        y += 0.070f;
        DrawControlButton(left, y, width, "NUM8   " + T("PREV_SHORT"), false, yr, yg, yb);
        y += 0.049f;
        DrawControlButton(left, y, width, "NUM5   " + T("OPEN_YT"), false, yr, yg, yb);
        y += 0.049f;
        DrawControlButton(left, y, width, "NUM2   " + T("NEXT_SHORT"), false, yr, yg, yb);
        y += 0.049f;
        DrawControlButton(left, y, width, "NUM1   " + T("PLAY_PAUSE"), youtubeModeActive, yr, yg, yb);
    }

    private void DrawGtaRadioTab(float left, float top, float width, float bottom, int ar, int ag, int ab)
    {
        int rr = 72, rg = 170, rb = 255;
        float h = bottom - top;
        DrawRect(left + width / 2f, top + h / 2f, width, h, 8, 14, 22, 250);
        DrawFrame(left, top, width, h, rr, rg, rb, 150, 0.0010f);

        float badgeLeft = left + 0.020f;
        float badgeTop = top + 0.016f;
        float badgeH = 0.044f;
        float badgeW = PixelSquareWidth(badgeH) * 1.08f;
        DrawGtaRadioLogoBadge(badgeLeft, badgeTop, badgeW, badgeH, GetCurrentGtaRadioStationEnum(), gtaRadioModeActive && gtaRadioPlayingRequested);
        float gtaHeaderTextLeft = badgeLeft + badgeW + 0.014f;
        DrawText(T("GTA_RADIO"), gtaHeaderTextLeft, top + 0.010f, 0.286f, 0, 245, 248, 252, 248);
        DrawText(Shorten(CurrentGtaRadioStationName(), 38), gtaHeaderTextLeft, top + 0.038f, 0.180f, 0, 232, 238, 246, 235);
        DrawText(GetGtaRadioGenre(GetCurrentGtaRadioStationEnum()), gtaHeaderTextLeft, top + 0.059f, 0.142f, 0, 153, 184, 214, 220);
        DrawText(T("GTA_RADIO_HINT"), left + 0.020f, top + 0.082f, 0.145f, 0, 164, 177, 194, 220);
        DrawRect(left + width / 2f, top + 0.111f, width - 0.040f, 0.030f, 18, 23, 30, 238);
        DrawFrame(left + 0.020f, top + 0.096f, width - 0.040f, 0.030f, rr, rg, rb, 88, 0.0007f);
        DrawText(T("GTA_RADIO_VOLUME_NOTICE"), left + 0.032f, top + 0.102f, 0.150f, 0, 232, 238, 246, 235);

        if (gtaRadioStations.Count == 0)
        {
            DrawText(T("NO_STATION"), left + 0.025f, top + 0.140f, 0.245f, 0, 220, 224, 230, 235);
            return;
        }

        int visible = 7;
        int start = gtaRadioMenuIndex - visible / 2;
        if (start < 0) start = 0;
        if (start > gtaRadioStations.Count - visible) start = Math.Max(0, gtaRadioStations.Count - visible);
        float rowTop = top + 0.138f;
        float rowH = 0.054f;
        for (int row = 0; row < visible; row++)
        {
            int i = start + row;
            if (i >= gtaRadioStations.Count) break;
            bool selected = i == gtaRadioMenuIndex;
            bool active = gtaRadioModeActive && gtaRadioPlayingRequested && i == gtaRadioIndex;
            int fr = selected ? rr : (active ? 55 : 38);
            int fg = selected ? rg : (active ? 122 : 45);
            int fb = selected ? rb : (active ? 176 : 54);
            float y = rowTop + row * rowH;
            float boxH = 0.046f;
            DrawRect(left + width / 2f, y + boxH / 2f, width - 0.040f, boxH, selected ? 16 : 11, selected ? 26 : 18, selected ? 38 : 26, selected ? 248 : 235);
            DrawFrame(left + 0.020f, y, width - 0.040f, boxH, fr, fg, fb, selected ? 190 : (active ? 125 : 52), 0.00075f);
            if (active)
                DrawRect(left + 0.025f, y + boxH / 2f, 0.0023f, boxH - 0.010f, rr, rg, rb, 220);

            float rowBadgeLeft = left + 0.031f;
            float rowBadgeTop = y + 0.0055f;
            float rowBadgeH = boxH - 0.011f;
            float rowBadgeW = PixelSquareWidth(rowBadgeH) * 1.08f;
            DrawGtaRadioLogoBadge(rowBadgeLeft, rowBadgeTop, rowBadgeW, rowBadgeH, gtaRadioStations[i].Station, selected || active);

            float rowTextLeft = rowBadgeLeft + rowBadgeW + 0.014f;
            DrawText(Shorten(gtaRadioStations[i].Name, 35), rowTextLeft, y + 0.0060f, 0.218f, 0,
                selected ? 247 : 215, selected ? 249 : 222, selected ? 252 : 231, 244);
            DrawText(Shorten(GetGtaRadioGenre(gtaRadioStations[i].Station), 31), rowTextLeft, y + 0.0260f, 0.135f, 0,
                selected ? 182 : 134, selected ? 198 : 148, selected ? 216 : 164, 214);
            if (active) DrawText(T("ON_AIR"), left + width - 0.102f, y + 0.011f, 0.142f, 0, rr, rg, rb, 235);
        }

        string gtaNow = GetGtaRadioNowPlayingLine();
        DrawText(string.IsNullOrEmpty(gtaNow) ? T("NO_METADATA") : Shorten(gtaNow, 70), left + 0.022f, bottom - 0.050f, 0.165f, 0,
            string.IsNullOrEmpty(gtaNow) ? 151 : rr, string.IsNullOrEmpty(gtaNow) ? 163 : rg, string.IsNullOrEmpty(gtaNow) ? 179 : rb, 228);
    }

    private void DrawGtaRadioSidePanel(float left, float top, float width, int ar, int ag, int ab)
    {
        int rr = 72, rg = 170, rb = 255;
        float y = top;
        DrawRect(left + width / 2f, y + 0.032f, width, 0.064f, 11, 15, 20, 232);
        DrawFrame(left, y, width, 0.064f, rr, rg, rb, 54, 0.0006f);
        float sideBadgeH = 0.046f;
        float sideBadgeW = PixelSquareWidth(sideBadgeH) * 1.08f;
        DrawGtaRadioLogoBadge(left + 0.010f, y + 0.009f, sideBadgeW, sideBadgeH, GetCurrentGtaRadioStationEnum(), gtaRadioModeActive && gtaRadioPlayingRequested);
        float sideTextLeft = left + 0.010f + sideBadgeW + 0.010f;
        DrawText(T("STATION"), sideTextLeft, y + 0.006f, 0.132f, 0, 158, 168, 180, 214);
        DrawText(Shorten(CurrentGtaRadioStationName(), 19), sideTextLeft, y + 0.026f, 0.144f, 0, 236, 240, 245, 230);
        DrawText(Shorten(GetGtaRadioGenre(GetCurrentGtaRadioStationEnum()), 22), sideTextLeft, y + 0.045f, 0.112f, 0, 137, 166, 194, 208);
        y += 0.078f;
        string gtaNow = GetGtaRadioNowPlayingLine();
        DrawRect(left + width / 2f, y + 0.032f, width, 0.064f, 11, 15, 20, 232);
        DrawFrame(left, y, width, 0.064f, rr, rg, rb, 42, 0.0006f);
        DrawText(T("NOW_PLAYING"), left + 0.010f, y + 0.010f, 0.138f, 0, 158, 168, 180, 214);
        DrawText(string.IsNullOrEmpty(gtaNow) ? T("NO_METADATA") : Shorten(gtaNow, 28), left + 0.010f, y + 0.034f, 0.140f, 0, 232, 238, 246, 224);
        y += 0.078f;
        DrawWidgetCard(left, y, width, 0.064f, T("VOLUME"), T("GTA_RADIO_GAME_VOLUME"), 8, rr, rg, rb, false);
        y += 0.086f;
        DrawControlButton(left, y, width, "NUM8   " + T("PREV_SHORT"), false, rr, rg, rb);
        y += 0.054f;
        DrawControlButton(left, y, width, "NUM5   " + T("SELECT"), false, rr, rg, rb);
        y += 0.054f;
        DrawControlButton(left, y, width, "NUM2   " + T("NEXT_SHORT"), false, rr, rg, rb);
        y += 0.054f;
        DrawControlButton(left, y, width, "NUM1   " + T("POWER"), gtaRadioModeActive && gtaRadioPlayingRequested, rr, rg, rb);
    }

    private void DrawSpotifyMusicTab(float left, float top, float width, float bottom, int ar, int ag, int ab)
    {
        int sr = 145, sg = 116, sb = 202;
        float h = bottom - top;
        DrawRect(left + width / 2f, top + h / 2f, width, h, 9, 15, 12, 250);
        DrawFrame(left, top, width, h, 45, 66, 54, 220, 0.0012f);

        float coverLeft = left + 0.020f;
        float coverTop = top + 0.018f;
        float coverH = 0.118f;
        float coverW = PixelSquareWidth(coverH);
        DrawMediaCoverShell(coverLeft, coverTop, coverW, coverH, sr, sg, sb, spotifyModeActive);
        bool coverDrawn = DrawSpotifyCover(coverLeft, coverTop, coverW, coverH);
        if (!coverDrawn)
        {
            DrawRect(coverLeft + coverW / 2f, coverTop + coverH / 2f, coverW, coverH, 25, 20, 20, 246);
            DrawUiIcon(6, coverLeft + coverW * 0.18f, coverTop + coverH * 0.23f, 0.045f, sr, sg, sb, 235);
            DrawText("SPOTIFY", coverLeft + 0.006f, coverTop + coverH - 0.028f, 0.112f, 0, 198, 226, 207, 230);
        }

        float serviceTextLeft = coverLeft + coverW + 0.022f;
        float serviceIconH = 0.030f;
        float serviceIconW = PixelSquareWidth(serviceIconH);
        DrawMediaIconTile(6, serviceTextLeft, top + 0.018f, serviceIconW, serviceIconH, sr, sg, sb, spotifyModeActive);
        DrawCenteredUiIconInTile(6, serviceTextLeft, top + 0.018f, serviceIconW, serviceIconH, sr, sg, sb, 235);
        float spotifyTitleLeft = serviceTextLeft + PixelSquareWidth(0.030f) + 0.010f;
        DrawText("SPOTIFY", spotifyTitleLeft, top + 0.020f, 0.350f, 0, 247, 249, 251, 248);
        DrawText(T("MEDIA_SESSION"), spotifyTitleLeft, top + 0.060f, 0.180f, 0, 173, 180, 190, 220);
        DrawText(spotifyAvailable ? T("CONNECTED") : T("NOT_CONNECTED"), left + width - 0.125f, top + 0.024f, 0.175f, 0, spotifyAvailable ? 91 : 230, spotifyAvailable ? 230 : 94, spotifyAvailable ? 124 : 94, 235);
        DrawText(T("VOLUME_SHORT") + " " + volume + "%  |  " + T("SPOTIFY_OUT_SHORT") + " " + MapUserVolumeToSpotify(volume) + "%", spotifyTitleLeft, top + 0.096f, 0.185f, 0, duckActive ? 248 : 195, duckActive ? 205 : 201, duckActive ? 105 : 210, 225);

        float cardTop = top + 0.155f;
        float cardH = 0.170f;
        DrawRect(left + width / 2f, cardTop + cardH / 2f, width - 0.035f, cardH, 14, 22, 18, 246);
        DrawFrame(left + 0.0175f, cardTop, width - 0.035f, cardH, sr, sg, sb, spotifyModeActive ? 180 : 80, 0.0011f);
        DrawText(T("NOW_PLAYING"), left + 0.035f, cardTop + 0.016f, 0.175f, 0, sr, sg, sb, 235);
        DrawText(Shorten(string.IsNullOrEmpty(spotifyTitle) ? T("SPOTIFY_START_HINT") : spotifyTitle, 48), left + 0.035f, cardTop + 0.050f, 0.330f, 0, 245, 247, 250, 246);
        DrawText(Shorten(string.IsNullOrEmpty(spotifyArtist) ? T("SPOTIFY_START_SONG") : spotifyArtist, 58), left + 0.035f, cardTop + 0.092f, 0.225f, 0, 205, 211, 220, 232);
        DrawText(Shorten(string.IsNullOrEmpty(spotifyAlbum) ? "" : spotifyAlbum, 58), left + 0.035f, cardTop + 0.124f, 0.190f, 0, 164, 172, 184, 218);
        DrawText(Shorten(T("STATUS") + ": " + LocalizeSpotifyState(), 30), left + width - 0.155f, cardTop + 0.144f, 0.175f, 0, spotifyAvailable ? sr : 170, spotifyAvailable ? sg : 176, spotifyAvailable ? sb : 184, 225);

        float controlsTop = cardTop + cardH + 0.020f;
        DrawInfoRow(left + 0.022f, controlsTop, width - 0.044f, "NUM8", T("PREVIOUS_TRACK"), sr, sg, sb);
        DrawInfoRow(left + 0.022f, controlsTop + 0.046f, width - 0.044f, "NUM5", T("OPEN_SPOTIFY"), sr, sg, sb);
        DrawInfoRow(left + 0.022f, controlsTop + 0.092f, width - 0.044f, "NUM2", T("NEXT_TRACK"), sr, sg, sb);
        DrawInfoRow(left + 0.022f, controlsTop + 0.138f, width - 0.044f, "NUM-/+", T("VOLUME"), sr, sg, sb);
        DrawText(Shorten(T("SOURCE") + ": " + (string.IsNullOrEmpty(spotifySource) ? T("MEDIA_SESSION") : spotifySource), 62), left + 0.025f, bottom - 0.070f, 0.170f, 0, 177, 184, 194, 220);
        DrawText(duckActive ? (T("DUCKING") + ": " + LocalizeDuckReason()) : T("SPOTIFY_EXCLUSIVE"), left + 0.025f, bottom - 0.044f, 0.175f, 0, duckActive ? 248 : 177, duckActive ? 205 : 184, duckActive ? 105 : 194, 220);
    }

    private void UpdateSpotifyCoverSprite()
    {
        // Mini-HUD track detection must work even when artwork is disabled.
        // Artwork remains opt-in; only the metadata/timer is independent from cover loading.
        string trackKey = (spotifyTitle ?? "") + "|" + (spotifyArtist ?? "") + "|" + (spotifyAlbum ?? "");
        if (!string.Equals(trackKey, spotifyCoverTrackKey, StringComparison.Ordinal))
        {
            spotifyCoverTrackKey = trackKey;
            spotifyCoverSprite = null;
            spotifyCoverLoadedPath = "";
            if (spotifyModeActive && (!string.IsNullOrEmpty(spotifyTitle) || !string.IsNullOrEmpty(spotifyArtist)))
            {
                spotifyMiniHideAt = DateTime.Now.AddMilliseconds(4200);
                miniUiFadeUpdatedAt = DateTime.MinValue;
            }
        }

        if (!mediaArtworkEnabled)
        {
            spotifyCoverSprite = null;
            spotifyCoverLoadedPath = "";
            spotifyCoverPath = "";
            return;
        }

        if (DateTime.Now < spotifyCoverNextLoadCheck) return;
        spotifyCoverNextLoadCheck = DateTime.Now.AddMilliseconds(600);
        if (string.IsNullOrEmpty(spotifyCoverPath)) return;
        if (string.Equals(spotifyCoverLoadedPath, spotifyCoverPath, StringComparison.OrdinalIgnoreCase) && spotifyCoverSprite != null) return;

        try
        {
            if (!File.Exists(spotifyCoverPath)) return;
            string coverName = Path.GetFileName(spotifyCoverPath) ?? "";
            if (!coverName.StartsWith("spotify_ready_", StringComparison.OrdinalIgnoreCase) ||
                !coverName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                Log("SPOTIFY COVER BLOCKED | non-normalized path | " + spotifyCoverPath);
                return;
            }
            FileInfo fi = new FileInfo(spotifyCoverPath);
            if (fi.Length < 2048 || fi.Length > 4194304) return;

            bool validImage = false;
            using (FileStream fs = new FileStream(spotifyCoverPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                int b0 = fs.ReadByte();
                int b1 = fs.ReadByte();
                int b2 = fs.ReadByte();
                int b3 = fs.ReadByte();
                validImage = (b0 == 0x89 && b1 == 0x50 && b2 == 0x4E && b3 == 0x47);
            }
            if (!validImage)
            {
                Log("SPOTIFY COVER IGNORED | invalid image signature | " + spotifyCoverPath);
                return;
            }

            if (spotifyCoverSprite != null)
                FlushCustomSpriteInstances(spotifyCoverSprite, Math.Max(4, spotifyCoverDrawCountPreviousFrame));
            spotifyCoverDrawCountThisFrame = spotifyCoverDrawCountPreviousFrame = 0;
            spotifyCoverSprite = new GTA.UI.CustomSprite(
                spotifyCoverPath,
                new System.Drawing.SizeF(128f, 128f),
                new System.Drawing.PointF(0f, 0f),
                System.Drawing.Color.White);
            spotifyCoverLoadedPath = spotifyCoverPath;
            if (spotifyModeActive)
                Log("SPOTIFY MINI COVER READY | Mini UI timer remains independent from artwork");
            Log("SPOTIFY COVER READY | " + spotifyCoverPath + " | " + fi.Length + " Bytes");
        }
        catch (Exception ex)
        {
            spotifyCoverSprite = null;
            spotifyCoverLoadedPath = "";
            Log("SPOTIFY COVER LOAD ERROR | " + ex.Message);
        }
    }

    private bool DrawSpotifyCover(float left, float top, float width, float height)
    {
        if (!mediaArtworkEnabled) return false;
        if (drawAlphaMultiplier <= 0.01f) return false;
        if (spotifyCoverSprite == null) return false;
        try
        {
            spotifyCoverSprite.Size = new System.Drawing.SizeF(width * GTA.UI.Screen.Width, height * GTA.UI.Screen.Height);
            spotifyCoverSprite.Position = new System.Drawing.PointF(left * GTA.UI.Screen.Width, top * GTA.UI.Screen.Height);
            int coverAlpha = Math.Max(0, Math.Min(255, (int)Math.Round(255.0f * drawAlphaMultiplier)));
            spotifyCoverSprite.Color = System.Drawing.Color.FromArgb(coverAlpha, 255, 255, 255);
            spotifyCoverSprite.Draw();
            spotifyCoverDrawCountThisFrame++;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void DrawSpotifySidePanel(float left, float top, float width, int ar, int ag, int ab)
    {
        int sr = 145, sg = 116, sb = 202;
        // v7.38: identical geometry to YouTube Music for a stable visual rhythm.
        DrawVolumeWidget(left, top, width, 0.116f, volume / 100.0f, sr, sg, sb);
        float y = top + 0.132f;
        DrawWidgetCard(left, y, width, 0.058f, T("APP_VOLUME"), MapUserVolumeToSpotify(volume) + "%", 6, sr, sg, sb, true);
        y += 0.070f;
        DrawWidgetCard(left, y, width, 0.058f, T("CONNECTION"), spotifyAvailable ? T("CONNECTED") : T("NOT_CONNECTED"), 10, sr, sg, sb, spotifyAvailable);
        y += 0.070f;
        DrawControlButton(left, y, width, "NUM8   " + T("PREV_SHORT"), false, sr, sg, sb);
        y += 0.049f;
        DrawControlButton(left, y, width, "NUM5   " + T("OPEN_SPOTIFY"), false, sr, sg, sb);
        y += 0.049f;
        DrawControlButton(left, y, width, "NUM2   " + T("NEXT_SHORT"), false, sr, sg, sb);
        y += 0.049f;
        DrawControlButton(left, y, width, "NUM1   " + T("PLAY_PAUSE"), spotifyModeActive, sr, sg, sb);
    }

    private string GetSettingsHelpText(int item)
    {
        if (item == 1) return T("HELP_DUCK_LEVEL");
        if (item >= 2 && item <= 4) return T("HELP_OUTPUT_LIMIT");
        if (item == 5) return T("HELP_VOLUME_STEP");
        if (item == 6) return T("HELP_DUCK_HOLD");
        if (item == 7) return T("HELP_DUCK_FADE");
        if (item == 8) return T("ARTWORK_NOTE");
        if (item == 9) return T("HELP_RESET");
        return T("ALT_CONTROL_HINT");
    }

    private void ResetUserFriendlyDefaults()
    {
        string keepLanguage = uiLanguage;
        volume = 25;
        volumeStep = 1;
        duckVolumePercent = 70;
        duckReleaseMs = 1800;
        duckAttackMs = 350;
        duckFadeOutMs = 900;
        playerMaxVolume = 70;
        youtubeMaxVolume = 55;
        spotifyMaxVolume = 55;
        volumeCurveExponent = 1.10f;
        youtubeVolumeCurveExponent = 1.55f;
        spotifyVolumeCurveExponent = 1.55f;
        SetMediaArtworkEnabled(false);
        showMiniUi = true;
        persistentMiniUi = false;
        hideMiniUiWhileMenuOpen = true;
        menuSlowMotionEnabled = true;
        menuTimeScale = 0.45f;
        menuOpenSoundEnabled = true;
        miniUiLeft = 0.73f;
        miniUiTop = 0.64f;
        miniUiWidth = 0.235f;
        miniUiHeight = 0.122f;
        SetAudioReactiveNeonEnabled(true);
        neonBeatSensitivity = 6;
        neonVoiceFilter = 70;
        neonPulseStrength = 85;
        neonPulseSpeed = 60;
        audioReactiveInteriorLightEnabled = true;
        interiorLightStrength = 70;
        plateLightEnabled = true;
        turboImpactEnabled = true;
        ResetTurboImpactState(true);
        showCustomSpeedometer = true;
        showBoatHud = true;
        showFlightHud = true;
        speedometerShowCarIcon = true;
        speedometerShowRpm = true;
        speedometerUseMph = false;
        navigationUseMetric = false;
        automaticIndicatorsEnabled = true;
        automaticEmergencyHazardsEnabled = false;
        speedometerLeft = 0.760f;
        speedometerTop = 0.826f;
        speedometerWidth = 0.205f;
        speedometerHeight = 0.126f;
        speedometerOpacity = 92;
        skinIndex = 0;
        skinMenuIndex = 0;
        dynamicRadioBackgroundStrength = 0.72f;
        speechDucking = true;
        phoneCallDucking = true;
        menuDucking = true;
        gtaRadioIndex = 0;
        gtaRadioMenuIndex = 0;
        uiLanguage = keepLanguage; // language and favourites intentionally stay untouched
        appliedStreamVolume = -1;
        appliedYoutubeVolume = -1;
        appliedYoutubeMixerLevel = -1.0f;
        appliedSpotifyVolume = -1;
        appliedSpotifyMixerLevel = -1.0f;
        ApplyEffectiveVolume();
        SaveAudioAndLanguageSettings();
        SaveSkinToIni();
        SaveUserSettings();
    }

    private void DrawSettingsTab(float left, float top, float width, float bottom, int ar, int ag, int ab)
    {
        DrawRect(left + width / 2f, top + (bottom - top) / 2f, width, bottom - top, 10, 15, 20, 248);
        DrawFrame(left, top, width, bottom - top, 45, 54, 64, 220, 0.0011f);
        DrawText(T("SETTINGS"), left + 0.020f, top + 0.018f, 0.360f, 0, 244, 247, 250, 245);
        DrawText(T("SETTINGS_HINT"), left + 0.020f, top + 0.058f, 0.180f, 0, 174, 181, 191, 220);

        const int totalSettings = 10;
        float y = top + 0.087f;
        float sy = 0.040f;
        for (int item = 0; item < totalSettings; item++)
        {
            string label = "";
            string value = "";
            if (item == 0) { label = T("LANGUAGE"); value = GetLanguageDisplayName(); }
            else if (item == 1) { label = T("DUCK_LEVEL"); value = duckVolumePercent + "%"; }
            else if (item == 2) { label = T("RADIO_OUTPUT"); value = playerMaxVolume + "%"; }
            else if (item == 3) { label = T("YT_OUTPUT"); value = youtubeMaxVolume + "%"; }
            else if (item == 4) { label = T("SPOTIFY_OUTPUT"); value = spotifyMaxVolume + "%"; }
            else if (item == 5) { label = T("VOLUME_STEP"); value = volumeStep + "%"; }
            else if (item == 6) { label = T("DUCK_HOLD"); value = duckReleaseMs + " ms"; }
            else if (item == 7) { label = T("DUCK_FADE"); value = duckFadeOutMs + " ms"; }
            else if (item == 8) { label = T("ARTWORK"); value = T(mediaArtworkEnabled ? "ON" : "OFF"); }
            else if (item == 9) { label = T("RESET_DEFAULTS"); value = T("NUM5_CONFIRM"); }

            DrawSettingRow(left + 0.022f, y + sy * item, width - 0.044f, label, value, settingsMenuIndex == item, ar, ag, ab);
        }

        string settingsNote = GetSettingsHelpText(settingsMenuIndex);
        DrawText(settingsNote, left + 0.022f, bottom - 0.075f, 0.150f, 0, 174, 181, 191, 205);
        DrawText(T("HOLD_VOLUME_NOTE"), left + 0.022f, bottom - 0.045f, 0.155f, 0, ar, ag, ab, 215);
    }

    private void DrawCarSettingsTab(float left, float top, float width, float bottom, int ar, int ag, int ab)
    {
        DrawRect(left + width / 2f, top + (bottom - top) / 2f, width, bottom - top, 10, 15, 20, 248);
        DrawFrame(left, top, width, bottom - top, 45, 54, 64, 220, 0.0011f);
        DrawText(T("CAR_SETTINGS"), left + 0.020f, top + 0.018f, 0.360f, 0, 244, 247, 250, 245);
        DrawText(T("CAR_SETTINGS_HINT"), left + 0.020f, top + 0.058f, 0.170f, 0, 174, 181, 191, 220);

        int selectedGroup = GetCarSettingGroup(carSettingsMenuIndex);
        string[] groupNames = new string[] { T("CAR_GROUP_LIGHTING"), T("CAR_GROUP_AUDIO"), T("CAR_GROUP_SPEEDO"), T("CAR_GROUP_HUD") };
        float chipLeft = left + 0.022f;
        float chipGap = 0.006f;
        float chipW = (width - 0.044f - chipGap * 3f) / 4f;
        for (int g = 0; g < 4; g++)
        {
            bool activeGroup = g == selectedGroup;
            DrawRect(chipLeft + chipW / 2f, top + 0.094f, chipW, 0.026f, activeGroup ? MixColor(14, ar, 0.14f) : 14, activeGroup ? MixColor(18, ag, 0.14f) : 18, activeGroup ? MixColor(22, ab, 0.14f) : 22, 235);
            DrawFrame(chipLeft, top + 0.081f, chipW, 0.026f, ar, ag, ab, activeGroup ? 180 : 48, 0.0007f);
            DrawText(groupNames[g], chipLeft + 0.008f, top + 0.086f, 0.145f, 0, activeGroup ? 244 : 174, activeGroup ? 247 : 181, activeGroup ? 250 : 191, 225);
            chipLeft += chipW + chipGap;
        }

        int totalCarSettings = CarSettingOrder.Length;
        const int visibleRows = 7;
        int firstItem = carSettingsMenuIndex - 3;
        if (firstItem < 0) firstItem = 0;
        if (firstItem > totalCarSettings - visibleRows) firstItem = totalCarSettings - visibleRows;

        float y = top + 0.122f;
        float sy = 0.052f;
        for (int row = 0; row < visibleRows; row++)
        {
            int menuPosition = firstItem + row;
            int item = GetCarSettingId(menuPosition);
            string label = "";
            string value = "";
            if (item == 0) { label = T("BEAT_NEON"); value = audioReactiveNeonEnabled ? T("ON") : T("OFF"); }
            else if (item == 1) { label = T("BEAT_SENSITIVITY"); value = neonBeatSensitivity + "/10"; }
            else if (item == 2) { label = T("VOICE_FILTER"); value = neonVoiceFilter + "%"; }
            else if (item == 3) { label = T("PULSE_STRENGTH"); value = neonPulseStrength + "%"; }
            else if (item == 4) { label = T("PULSE_SPEED"); value = neonPulseSpeed + "%"; }
            else if (item == 5) { label = T("INTERIOR_LIGHT"); value = audioReactiveInteriorLightEnabled ? T("ON") : T("OFF"); }
            else if (item == 6) { label = T("INTERIOR_STRENGTH"); value = interiorLightStrength + "%"; }
            else if (item == 19) { label = T("PLATE_LIGHT"); value = plateLightEnabled ? T("ON") : T("OFF"); }
            else if (item == 18) { label = T("AUTO_INDICATORS"); value = automaticIndicatorsEnabled ? T("ON") : T("OFF"); }
            else if (item == 20) { label = T("TURBO_IMPACT"); value = GetTurboImpactModeText(); }
            else if (item == 7) { label = T("ROAD_HUD"); value = showCustomSpeedometer ? T("ON") : T("OFF"); }
            else if (item == 8) { label = T("BOAT_HUD"); value = showBoatHud ? T("ON") : T("OFF"); }
            else if (item == 9) { label = T("FLIGHT_HUD"); value = showFlightHud ? T("ON") : T("OFF"); }
            else if (item == 10) { label = T("SPEEDO_CAR_ICON"); value = speedometerShowCarIcon ? T("ON") : T("OFF"); }
            else if (item == 11) { label = T("SPEEDO_RPM_BAR"); value = speedometerShowRpm ? T("ON") : T("OFF"); }
            else if (item == 12) { label = T("SPEEDO_UNIT_MODE"); value = speedometerUseMph ? "MPH" : "KM/H"; }
            else if (item == 13) { label = T("NAV_UNIT_MODE"); value = navigationUseMetric ? T("METRIC") : T("STANDARD"); }
            else if (item == 14) { label = T("SPEEDO_SIZE"); value = Math.Max(50, Math.Min(160, (int)Math.Round(speedometerWidth / 0.205f * 100f))) + "%"; }
            else if (item == 15) { label = T("SPEEDO_POS_X"); value = Math.Max(0, Math.Min(100, (int)Math.Round(speedometerLeft * 100f))) + "%"; }
            else if (item == 16) { label = T("SPEEDO_POS_Y"); value = Math.Max(0, Math.Min(100, (int)Math.Round(speedometerTop * 100f))) + "%"; }
            else if (item == 17) { label = T("SPEEDO_OPACITY"); value = speedometerOpacity + "%"; }

            DrawSettingRow(left + 0.022f, y + sy * row, width - 0.044f, label, value, carSettingsMenuIndex == menuPosition, ar, ag, ab);
        }

        DrawText((carSettingsMenuIndex + 1) + "/" + totalCarSettings, left + width - 0.060f, top + 0.064f, 0.155f, 0, ar, ag, ab, 210);
        string note = selectedGroup == 0 ? T("CAR_SETTINGS_NOTE") :
            (selectedGroup == 1 ? T("CAR_AUDIO_NOTE") :
            (selectedGroup == 2 ? T("SPEEDOMETER_NOTE") : T("CAR_HUD_NOTE")));
        DrawText(note, left + 0.022f, bottom - 0.075f, 0.150f, 0, 174, 181, 191, 205);
        DrawText(T("CAR_SETTINGS_SCROLL_NOTE"), left + 0.022f, bottom - 0.045f, 0.145f, 0, ar, ag, ab, 215);
    }

    private void DrawSettingRow(float left, float top, float width, string label, string value, bool selected, int ar, int ag, int ab)
    {
        const float rowH = 0.041f;
        DrawRect(left + width / 2f, top + rowH / 2f, width, rowH, selected ? 18 : 14, selected ? 31 : 20, selected ? 24 : 26, 240);
        if (selected) DrawFrame(left, top, width, rowH, ar, ag, ab, 210, 0.0011f);
        DrawText((selected ? ">  " : "   ") + label, left + 0.014f, top + 0.009f, 0.210f, 0, 238, 242, 246, 235);
        DrawText(value, left + width - 0.125f, top + 0.009f, 0.210f, 0, ar, ag, ab, 238);
    }

    private void DrawSettingsSidePanel(float left, float top, float width, int ar, int ag, int ab)
    {
        DrawWidgetCardWithRightValue(left, top + 0.010f, width, 0.064f, T("DUCKING"), T("DUCKING"), duckVolumePercent + "%", 8, ar, ag, ab, false);
        DrawWidgetCardWithRightValue(left, top + 0.088f, width, 0.064f, T("RADIO_OUTPUT_SHORT"), T("RADIO_OUTPUT_SHORT"), playerMaxVolume + "%", 1, ar, ag, ab, false);
        DrawWidgetCardWithRightValue(left, top + 0.166f, width, 0.064f, T("YT_OUTPUT_SHORT"), T("YT_OUTPUT_SHORT"), youtubeMaxVolume + "%", 5, ar, ag, ab, false);
        DrawWidgetCardWithRightValue(left, top + 0.244f, width, 0.064f, T("SPOTIFY_OUTPUT_SHORT"), T("SPOTIFY_OUTPUT_SHORT"), spotifyMaxVolume + "%", 6, ar, ag, ab, false);
        DrawWidgetCardWithRightValue(left, top + 0.322f, width, 0.064f, T("VOLUME_STEP"), T("VOLUME_STEP"), volumeStep + "%", 9, ar, ag, ab, false);
        DrawWidgetCardWithRightValue(left, top + 0.400f, width, 0.064f, T("ARTWORK"), T("ARTWORK"), T(mediaArtworkEnabled ? "ON" : "OFF"), 2, ar, ag, ab, mediaArtworkEnabled);
        DrawText(T("HOLD_PLUS_MINUS"), left + 0.008f, top + 0.478f, 0.145f, 0, 174, 181, 191, 210);
    }

    private void DrawCarSettingsSidePanel(float left, float top, float width, int ar, int ag, int ab)
    {
        int selectedGroup = GetCarSettingGroup(carSettingsMenuIndex);
        if (selectedGroup == 1)
        {
            DrawWidgetCardWithRightValue(left, top + 0.006f, width, 0.064f, T("TURBO_IMPACT"), T("TURBO_IMPACT"), GetTurboImpactModeText(), 7, ar, ag, ab, turboImpactEnabled);

            // Short, aligned context box instead of squeezing the full long help text.
            float helpTop = top + 0.084f;
            DrawRect(left + width / 2f, helpTop + 0.041f, width, 0.082f, 12, 16, 21, 232);
            DrawFrame(left, helpTop, width, 0.082f, ar, ag, ab, 38, 0.0006f);
            DrawText(T("BLOWOFF_TRIGGER"), left + 0.010f, helpTop + 0.012f, 0.145f, 0, 158, 168, 180, 214);
            DrawText(T("BLOWOFF_SHIFT_LIFT"), left + 0.010f, helpTop + 0.036f, 0.156f, 0, 232, 236, 241, 228);
            DrawText(T("BLOWOFF_NO_SPAM"), left + 0.010f, helpTop + 0.059f, 0.138f, 0, 174, 181, 191, 210);
            DrawText("NUM5  " + T("ON") + "/" + T("OFF"), left + 0.010f, top + 0.184f, 0.145f, 0, ar, ag, ab, 215);
            return;
        }

        DrawWidgetCardWithRightValue(left, top + 0.010f, width, 0.064f, T("BEAT_NEON"), T("BEAT_NEON"), audioReactiveNeonEnabled ? T("ON") : T("OFF"), 11, ar, ag, ab, audioReactiveNeonEnabled);
        DrawWidgetCardWithRightValue(left, top + 0.088f, width, 0.064f, T("BEAT_SENSITIVITY"), T("BEAT_SENSITIVITY"), neonBeatSensitivity + "/10", 11, ar, ag, ab, false);
        DrawWidgetCardWithRightValue(left, top + 0.166f, width, 0.064f, T("VOICE_FILTER"), T("VOICE_FILTER"), neonVoiceFilter + "%", 8, ar, ag, ab, false);
        DrawWidgetCardWithRightValue(left, top + 0.244f, width, 0.064f, T("PULSE_STRENGTH"), T("PULSE_STRENGTH"), neonPulseStrength + "%", 11, ar, ag, ab, false);
        DrawWidgetCardWithRightValue(left, top + 0.322f, width, 0.064f, T("PULSE_SPEED"), T("PULSE_SPEED"), neonPulseSpeed + "%", 11, ar, ag, ab, false);
        bool analyzerActive = false;
        try { analyzerActive = bassAnalyzer != null && bassAnalyzer.Available; } catch { analyzerActive = false; }
        DrawText(T("BASS_ANALYZER") + ": " + (analyzerActive ? T("ACTIVE") : T("FALLBACK")),
            left + 0.008f, top + 0.406f, 0.145f, 0,
            analyzerActive ? ar : 210,
            analyzerActive ? ag : 155,
            analyzerActive ? ab : 72, 218);
    }

    private void DrawInfoRow(float left, float top, float width, string label, string value, int ar, int ag, int ab)
    {
        DrawRect(left + width / 2f, top + 0.020f, width, 0.040f, 14, 20, 26, 235);
        DrawText(label, left + 0.012f, top + 0.010f, 0.185f, 0, 166, 174, 184, 220);
        DrawText(Shorten(value ?? "-", 52), left + 0.160f, top + 0.008f, 0.220f, 0, 238, 242, 246, 232);
    }

    private void DrawStationList(float left, float top, float width, float height, int selectedIndex, bool filtered, int ar, int ag, int ab)
    {
        DrawRect(left + width / 2f, top + height / 2f, width, height, 8, 12, 17, 250);
        DrawFrame(left, top, width, height, 40, 48, 57, 225, 0.0011f);
        DrawRect(left + width / 2f, top + 0.026f, width - 0.014f, 0.038f, 14, 19, 25, 246);
        DrawText(T("STATION"), left + 0.016f, top + 0.010f, 0.185f, 0, 177, 183, 192, 230);
        DrawText(T("PACK_GENRE"), left + width * 0.58f, top + 0.010f, 0.185f, 0, 177, 183, 192, 230);

        int visibleRows = filtered ? 10 : 7;
        float rowTop = top + 0.050f;
        float rowH = (height - 0.058f) / visibleRows;
        int count = filtered ? filteredStationIndices.Count : stations.Count;
        int selectedPos = 0;
        if (filtered)
        {
            for (int i = 0; i < filteredStationIndices.Count; i++) if (filteredStationIndices[i] == selectedIndex) { selectedPos = i; break; }
        }
        else selectedPos = selectedIndex;
        int start = selectedPos - visibleRows / 2;
        if (start < 0) start = 0;
        if (start > Math.Max(0, count - visibleRows)) start = Math.Max(0, count - visibleRows);

        for (int row = 0; row < visibleRows && start + row < count; row++)
        {
            int idx = filtered ? filteredStationIndices[start + row] : start + row;
            Station st = stations[idx];
            float y = rowTop + row * rowH;
            bool selected = idx == selectedIndex;
            bool active = idx == index;
            int cr, cg, cb;
            GetCategoryColor(st.Pack ?? "", out cr, out cg, out cb);
            if ((row & 1) == 1) DrawRect(left + width / 2f, y + rowH * 0.44f, width - 0.014f, rowH * 0.80f, 11, 16, 21, 235);
            if (selected)
            {
                DrawRect(left + width / 2f, y + rowH * 0.44f, width - 0.016f, rowH * 0.82f, Math.Max(5, cr / 5), Math.Max(7, cg / 4), Math.Max(6, cb / 5), active ? 215 : 178);
                DrawFrame(left + 0.008f, y + rowH * 0.03f, width - 0.016f, rowH * 0.82f, cr, cg, cb, 220, 0.001f);
            }
            else if (active) DrawRect(left + width / 2f, y + rowH * 0.44f, width - 0.016f, rowH * 0.82f, Math.Max(4, cr / 7), Math.Max(6, cg / 6), Math.Max(5, cb / 7), 195);

            float rowTextY = y + rowH * 0.145f;
            DrawRect(left + 0.010f, y + rowH * 0.44f, 0.004f, rowH * 0.52f, cr, cg, cb, st.Enabled ? 220 : 90);
            string favoriteMark = IsFavorite(idx) ? "[F] " : "";
            string stationMark = active ? "▶ " : (selected ? ">  " : "   ");
            DrawText(Shorten(stationMark + favoriteMark + st.Name, 31), left + 0.016f, rowTextY, 0.245f, 0, 244, 247, 250, st.Enabled ? 242 : 112);
            if (active) DrawText(T("ON_AIR"), left + 0.232f, rowTextY + 0.001f, 0.140f, 0, cr, cg, cb, 238);
            DrawText(Shorten(DisplayPackName(st.Pack) + " | " + (st.Genre ?? "Mix"), 38), left + width * 0.58f, rowTextY + 0.001f, 0.178f, 0, st.Enabled ? cr : 130, st.Enabled ? cg : 135, st.Enabled ? cb : 142, st.Enabled ? 215 : 105);
        }
    }

    private void DrawStationInfoCard(float left, float top, float width, float height, int stationIndex, int ar, int ag, int ab)
    {
        DrawRect(left + width / 2f, top + height / 2f, width, height, 12, 17, 23, 249);
        DrawFrame(left, top, width, height, 42, 50, 59, 225, 0.0011f);
        if (stationIndex < 0 || stationIndex >= stations.Count) return;
        Station sel = stations[stationIndex];
        int cr, cg, cb; GetCategoryColor(sel.Pack ?? "", out cr, out cg, out cb);
        DrawFrame(left, top, width, height, cr, cg, cb, 145, 0.0011f);
        float badgeH = Math.Min(0.066f, height - 0.020f);
        float badgeW = PixelSquareWidth(badgeH) * 1.05f;
        float badgeLeft = left + 0.014f;
        DrawStationLogoBadge(badgeLeft, top + 0.013f, badgeW, badgeH, sel, cr, cg, cb);
        float textLeft = badgeLeft + badgeW + 0.014f;
        DrawText(Shorten(sel.Name, 36), textLeft, top + 0.010f, 0.292f, 0, 245, 247, 250, 245);
        DrawText(Shorten(T("PACK") + ": " + DisplayPackName(sel.Pack) + "  |  " + T("GENRE") + ": " + sel.Genre, 60), textLeft, top + 0.036f, 0.186f, 0, cr, cg, cb, 235);
        DrawText(Shorten(T("VIBE") + ": " + (string.IsNullOrEmpty(sel.Vibe) ? "Los Santos Drive" : sel.Vibe), 62), textLeft, top + 0.059f, 0.174f, 0, 170, 177, 186, 218);
    }

    private void DrawCarbonPanel(float left, float top, float width, float height, int alpha)
    {
        // Nur noch als dunkles Gehaeuse. Das eigentliche Carbon wird gezielt auf
        // Header, Seitenleisten und Footer gezeichnet.
        DrawRect(left + width / 2f, top + height / 2f, width, height, 8, 9, 11, alpha);
    }

    private void DrawCarbonTexture(float left, float top, float width, float height, int alpha)
    {
        // V11.5: Carbon komplett entfernt. Stattdessen eine ruhige, moderne Premium-Oberflaeche
        // mit leichtem Metallic-/Gloss-Look, damit die UI sauber und gut lesbar bleibt.
        int baseA = Math.Max(150, Math.Min(250, alpha + 185));
        DrawRect(left + width / 2f, top + height / 2f, width, height, 11, 13, 17, baseA);

        // Sanfte obere Aufhellung fuer einen Head-Unit-/Glass-Look.
        DrawRect(left + width / 2f, top + height * 0.18f, width * 0.98f, height * 0.22f, 24, 28, 34, Math.Max(18, alpha / 3));
        // Dunklere Unterkante fuer etwas Tiefe.
        DrawRect(left + width / 2f, top + height * 0.86f, width * 0.98f, height * 0.18f, 6, 7, 9, Math.Max(16, alpha / 4));
        // Feine Highlight-Linien, damit die Flaechen nicht leer wirken.
        DrawRect(left + width / 2f, top + 0.0010f, width * 0.985f, 0.0011f, 132, 138, 148, Math.Max(24, alpha / 2));
        DrawRect(left + width / 2f, top + height - 0.0010f, width * 0.985f, 0.0011f, 0, 0, 0, 85);
    }

    private void DrawFrame(float left, float top, float width, float height, int r, int g, int b, int a, float thickness)
    {
        DrawRect(left + width / 2f, top + thickness / 2f, width, thickness, r, g, b, a);
        DrawRect(left + width / 2f, top + height - thickness / 2f, width, thickness, r, g, b, a);
        DrawRect(left + thickness / 2f, top + height / 2f, thickness, height, r, g, b, a);
        DrawRect(left + width - thickness / 2f, top + height / 2f, thickness, height, r, g, b, a);
    }

    private float GetUiIconOpticalScale(int icon)
    {
        // Blend a small per-icon base size with automatic normalization from the
        // visible PNG bounds, so user-replaced icons feel equally prominent.
        float baseScale;
        switch (icon)
        {
            case 0: baseScale = 0.69f; break; // home
            case 1: baseScale = 0.70f; break; // radio
            case 2: baseScale = 0.70f; break; // apps/grid
            case 3: baseScale = 0.69f; break; // info
            case 4: baseScale = 0.73f; break; // skins
            case 5: baseScale = 0.71f; break; // music
            case 6: baseScale = 0.70f; break; // streaming
            case 7: baseScale = 0.76f; break; // car
            case 8: baseScale = 0.75f; break; // settings
            case 9: baseScale = 0.71f; break; // volume
            case 10: baseScale = 0.70f; break; // source
            case 11: baseScale = 0.72f; break; // neon
            case 12: baseScale = 0.72f; break; // GTA Radio V
            default: baseScale = 0.70f; break;
        }
        float autoScale = (icon >= 0 && icon < uiIconAutoScale.Length) ? uiIconAutoScale[icon] : 1.0f;
        return Math.Max(0.64f, Math.Min(0.86f, baseScale * autoScale));
    }

    private float GetUiIconOpticalOffsetX(int icon)
    {
        // Auto-center the current PNG artwork from its non-transparent bounds so
        // user-replaced icons sit optically in the middle without hand-tuning.
        if (icon >= 0 && icon < uiIconAutoOffsetX.Length) return uiIconAutoOffsetX[icon];
        return 0.0f;
    }

    private float GetUiIconOpticalOffsetY(int icon)
    {
        if (icon >= 0 && icon < uiIconAutoOffsetY.Length) return uiIconAutoOffsetY[icon];
        return 0.0f;
    }

    private float PixelSquareHeightFromWidth(float normalizedWidth)
    {
        return normalizedWidth * ((float)GTA.UI.Screen.Width / Math.Max(1f, (float)GTA.UI.Screen.Height));
    }

    private void DrawIconNeonPedestal(float tileLeft, float tileTop, float tileWidth, float tileHeight, int r, int g, int b, int a)
    {
        // Keep the base subtle and low so the icon itself reads as centered inside
        // the tile instead of looking visually pushed upward.
        float centerX = tileLeft + tileWidth / 2f;
        float baseY = tileTop + tileHeight * 0.875f;
        float baseW = tileWidth * 0.40f;
        int glowA = Math.Min(68, Math.Max(18, a / 5));
        int midA = Math.Min(116, Math.Max(34, a / 3));
        int coreA = Math.Min(188, Math.Max(62, a - 48));
        DrawRect(centerX, baseY + 0.0012f, baseW * 1.14f, 0.0038f, r, g, b, glowA);
        DrawRect(centerX, baseY + 0.0005f, baseW, 0.0019f, r, g, b, midA);
        DrawRect(centerX, baseY, baseW * 0.70f, 0.0008f, MixColor(r, 245, 0.22f), MixColor(g, 245, 0.22f), MixColor(b, 245, 0.22f), coreA);
    }

    private void DrawCenteredUiIconInTile(int icon, float tileLeft, float tileTop, float tileWidth, float tileHeight, int r, int g, int b, int a)
    {
        // Exact geometric centering inside the visible icon tile.
        float maxHByWidth = PixelSquareHeightFromWidth(tileWidth * 0.82f);
        float maxHByHeight = tileHeight * 0.76f;
        float baseSquareH = Math.Min(maxHByHeight, maxHByWidth);
        float glyphH = baseSquareH * Math.Min(0.98f, GetUiIconOpticalScale(icon) * 1.12f);
        float glyphW = PixelSquareWidth(glyphH);
        float glyphLeft = tileLeft + (tileWidth - glyphW) * 0.5f;
        float glyphTop = tileTop + (tileHeight - glyphH) * 0.5f;
        DrawUiIcon(icon, glyphLeft, glyphTop, glyphH, r, g, b, a);
        DrawIconNeonPedestal(tileLeft, tileTop, tileWidth, tileHeight, r, g, b, a);
    }

    private void DrawCenteredUiIconInCompactTile(int icon, float tileLeft, float tileTop, float tileWidth, float tileHeight, int r, int g, int b, int a)
    {
        // Compact tiles (sidebar/right widgets): same true center, slightly tighter max size.
        float maxHByWidth = PixelSquareHeightFromWidth(tileWidth * 0.78f);
        float maxHByHeight = tileHeight * 0.74f;
        float baseSquareH = Math.Min(maxHByHeight, maxHByWidth);
        float glyphH = baseSquareH * Math.Min(0.98f, GetUiIconOpticalScale(icon) * 1.10f);
        float glyphW = PixelSquareWidth(glyphH);
        float glyphLeft = tileLeft + (tileWidth - glyphW) * 0.5f;
        float glyphTop = tileTop + (tileHeight - glyphH) * 0.5f;
        DrawUiIcon(icon, glyphLeft, glyphTop, glyphH, r, g, b, a);
    }

    private void DrawMediaIconTile(int icon, float left, float top, float width, float height, int ar, int ag, int ab, bool active)
    {
        int bgR = active ? MixColor(15, ar, 0.06f) : 14;
        int bgG = active ? MixColor(19, ag, 0.06f) : 18;
        int bgB = active ? MixColor(24, ab, 0.06f) : 23;

        DrawRect(left + width / 2f + 0.0016f, top + height / 2f + 0.0022f, width + 0.0032f, height + 0.0036f, 0, 0, 0, 54);
        DrawRect(left + width / 2f, top + height / 2f, width, height, bgR, bgG, bgB, active ? 246 : 238);
        DrawFrame(left, top, width, height, ar, ag, ab, active ? 68 : 30, 0.00050f);
        DrawRect(left + 0.0028f, top + height / 2f, 0.0016f, height - 0.009f, ar, ag, ab, active ? 150 : 90);
        DrawRect(left + width / 2f, top + 0.0030f, width * 0.76f, 0.0007f, 236, 240, 245, active ? 28 : 12);
    }

    private void DrawSymmetricMediaIconTile(float left, float top, float width, float height, int ar, int ag, int ab, bool active)
    {
        int bgR = active ? MixColor(15, ar, 0.06f) : 14;
        int bgG = active ? MixColor(19, ag, 0.06f) : 18;
        int bgB = active ? MixColor(24, ab, 0.06f) : 23;

        DrawRect(left + width / 2f + 0.0014f, top + height / 2f + 0.0020f, width + 0.0028f, height + 0.0034f, 0, 0, 0, 52);
        DrawRect(left + width / 2f, top + height / 2f, width, height, bgR, bgG, bgB, active ? 246 : 238);
        DrawFrame(left, top, width, height, ar, ag, ab, active ? 72 : 34, 0.00050f);
        DrawRect(left + width / 2f, top + 0.0030f, width * 0.70f, 0.0007f, 236, 240, 245, active ? 28 : 12);
        DrawRect(left + width / 2f, top + height - 0.0030f, width * 0.42f, 0.0007f, ar, ag, ab, active ? 82 : 42);
    }

    // TEST61: lightweight vector-style UI symbols built only from GTA rectangles/text.
    // No texture dictionary, file I/O or external asset loading is needed.
    // icon: 0 Home, 1 Radio, 2 Grid, 3 Info, 4 Skins, 5 Music,
    //       6 Audio/Spotify, 7 Car, 8 Settings, 9 Volume, 10 Source, 11 Neon, 12 GTA Radio V.
    private void DrawUiIcon(int icon, float left, float top, float size, int r, int g, int b, int a)
    {
        // TEST71 public build: service tiles use original neutral media symbols.
        // The surrounding tile may use the current UI accent; no provider logo
        // colourway is embedded or forced here.
        // Smooth transparent icon assets are used first; legacy primitives remain
        // as a fail-safe if an installation is missing an icon file.
        // Keep the original PNG icon set on every menu page. The close-delay fix is
        // handled by clearing menuOpen immediately on the key press; icon artwork must
        // never change just to work around menu timing.
        if (DrawUiIconSprite(icon, left, top, size, a))
        {
            // TEST73: the old side marker was removed. Alignment and the shared
            // neon pedestal are now handled by DrawCenteredUiIconInTile().
            return;
        }

        float cx = left + size / 2f;
        float cy = top + size / 2f;
        float u = size / 10f;
        int dimA = Math.Max(70, a - 65);

        switch (icon)
        {
            case 0: // Home - stepped roof + cabin.
                DrawRect(cx, top + u * 6.7f, u * 6.0f, u * 4.6f, r, g, b, a);
                DrawRect(cx, top + u * 3.7f, u * 7.4f, u * 1.3f, r, g, b, a);
                DrawRect(cx, top + u * 2.7f, u * 5.0f, u * 1.2f, r, g, b, a);
                DrawRect(cx, top + u * 1.7f, u * 2.6f, u * 1.2f, r, g, b, a);
                DrawRect(cx, top + u * 7.6f, u * 1.3f, u * 2.7f, 12, 17, 23, Math.Max(120, a - 25));
                break;

            case 1: // Radio/broadcast.
                DrawRect(cx, cy, u * 1.5f, u * 6.5f, r, g, b, a);
                DrawRect(left + u * 2.2f, cy, u * 1.2f, u * 4.2f, r, g, b, dimA);
                DrawRect(left + u * 7.8f, cy, u * 1.2f, u * 4.2f, r, g, b, dimA);
                DrawRect(left + u * 0.8f, cy, u * 0.9f, u * 7.2f, r, g, b, Math.Max(55, a - 110));
                DrawRect(left + u * 9.2f, cy, u * 0.9f, u * 7.2f, r, g, b, Math.Max(55, a - 110));
                break;

            case 2: // App/category grid.
                for (int yy = 0; yy < 2; yy++)
                    for (int xx = 0; xx < 2; xx++)
                        DrawRect(left + u * (3.0f + xx * 4.1f), top + u * (3.0f + yy * 4.1f),
                                 u * 2.6f, u * 2.6f, r, g, b, a);
                break;

            case 3: // Information.
                DrawFrame(left + u * 1.5f, top + u * 0.8f, u * 7.0f, u * 8.3f, r, g, b, a, Math.Max(0.00055f, u * 0.55f));
                DrawText("i", left + u * 4.0f, top + u * 1.6f, Math.Max(0.16f, size * 11.0f), 0, r, g, b, a);
                break;

            case 4: // Skin/palette swatches.
                DrawRect(left + u * 2.0f, top + u * 6.8f, u * 3.2f, u * 3.2f, r, g, b, a);
                DrawRect(left + u * 5.0f, top + u * 4.5f, u * 3.0f, u * 3.0f, r, g, b, Math.Max(95, a - 25));
                DrawRect(left + u * 7.3f, top + u * 2.3f, u * 2.5f, u * 2.5f, r, g, b, Math.Max(70, a - 60));
                break;

            case 5: // Music note.
                DrawRect(left + u * 6.9f, top + u * 3.9f, u * 1.4f, u * 6.1f, r, g, b, a);
                DrawRect(left + u * 5.4f, top + u * 1.9f, u * 4.4f, u * 1.4f, r, g, b, a);
                DrawRect(left + u * 3.8f, top + u * 7.4f, u * 3.0f, u * 2.7f, r, g, b, a);
                break;

            case 6: // Streaming/audio bars.
                DrawRect(cx, top + u * 2.5f, u * 7.0f, u * 1.0f, r, g, b, a);
                DrawRect(cx + u * 0.4f, top + u * 5.0f, u * 5.9f, u * 1.0f, r, g, b, Math.Max(90, a - 20));
                DrawRect(cx + u * 0.8f, top + u * 7.5f, u * 4.6f, u * 1.0f, r, g, b, Math.Max(75, a - 40));
                break;

            case 7: // Car.
                DrawRect(cx, top + u * 6.1f, u * 8.0f, u * 3.0f, r, g, b, a);
                DrawRect(cx, top + u * 3.8f, u * 4.8f, u * 2.2f, r, g, b, Math.Max(105, a - 20));
                DrawRect(left + u * 2.6f, top + u * 8.5f, u * 1.7f, u * 1.6f, r, g, b, a);
                DrawRect(left + u * 7.4f, top + u * 8.5f, u * 1.7f, u * 1.6f, r, g, b, a);
                break;

            case 8: // Settings sliders.
                DrawRect(cx, top + u * 2.4f, u * 8.0f, u * 0.9f, r, g, b, dimA);
                DrawRect(cx, top + u * 5.0f, u * 8.0f, u * 0.9f, r, g, b, dimA);
                DrawRect(cx, top + u * 7.6f, u * 8.0f, u * 0.9f, r, g, b, dimA);
                DrawRect(left + u * 3.1f, top + u * 2.4f, u * 1.7f, u * 2.1f, r, g, b, a);
                DrawRect(left + u * 6.8f, top + u * 5.0f, u * 1.7f, u * 2.1f, r, g, b, a);
                DrawRect(left + u * 4.7f, top + u * 7.6f, u * 1.7f, u * 2.1f, r, g, b, a);
                break;

            case 9: // Volume.
                DrawRect(left + u * 2.6f, cy, u * 2.3f, u * 3.6f, r, g, b, a);
                DrawRect(left + u * 4.5f, cy, u * 1.8f, u * 5.5f, r, g, b, a);
                DrawRect(left + u * 7.0f, cy, u * 1.0f, u * 4.0f, r, g, b, dimA);
                DrawRect(left + u * 8.5f, cy, u * 0.9f, u * 6.2f, r, g, b, Math.Max(65, a - 80));
                break;

            case 10: // Source/signal.
                DrawRect(cx, top + u * 6.5f, u * 1.2f, u * 6.2f, r, g, b, a);
                DrawRect(left + u * 2.7f, top + u * 6.0f, u * 1.0f, u * 3.2f, r, g, b, dimA);
                DrawRect(left + u * 7.3f, top + u * 6.0f, u * 1.0f, u * 3.2f, r, g, b, dimA);
                DrawRect(left + u * 1.2f, top + u * 5.6f, u * 0.8f, u * 5.5f, r, g, b, Math.Max(55, a - 100));
                DrawRect(left + u * 8.8f, top + u * 5.6f, u * 0.8f, u * 5.5f, r, g, b, Math.Max(55, a - 100));
                break;

            case 11: // Car + underglow.
                DrawUiIcon(7, left, top, size, r, g, b, a);
                DrawRect(cx, top + u * 9.6f, u * 8.5f, u * 0.9f, r, g, b, Math.Max(120, a - 10));
                break;

            case 12: // GTA Radio V fallback if the PNG asset is missing.
                DrawText("V", left + u * 2.1f, top + u * 0.2f, Math.Max(0.20f, size * 12.0f), 0, r, g, b, a);
                break;
        }
    }

    private void DrawNavItem(float left, float top, float width, string label, int icon, bool active, int ar, int ag, int ab)
    {
        int ir = active ? ar : 190, ig = active ? ag : 196, ib = active ? ab : 204;
        if (IsSpectrumSkin() && active) GetSpectrumPhaseColor(icon, out ir, out ig, out ib);

        if (active)
        {
            DrawRect(left + width / 2f, top + 0.024f, width, 0.048f, MixColor(18, ir, 0.12f), MixColor(22, ig, 0.12f), MixColor(28, ib, 0.12f), 234);
            DrawRect(left + 0.004f, top + 0.024f, 0.0030f, 0.038f, ir, ig, ib, 215);
            DrawText(">", left + width - 0.018f, top + 0.013f, 0.225f, 0, ir, ig, ib, 225);
        }
        else
        {
            DrawRect(left + width / 2f, top + 0.024f, width, 0.048f, 12, 16, 21, 180);
        }

        float iconH = 0.026f;
        float iconW = PixelSquareWidth(iconH);
        float iconColumnLeft = left + 0.006f;
        float iconColumnWidth = 0.039f;
        float iconCenterX = iconColumnLeft + iconColumnWidth * 0.5f;
        float iconLeft = iconCenterX - iconW * 0.5f;
        float iconTop = top + (0.048f - iconH) * 0.5f;
        DrawSymmetricMediaIconTile(iconLeft, iconTop, iconW, iconH, ir, ig, ib, active);
        DrawCenteredUiIconInCompactTile(icon, iconLeft, iconTop, iconW, iconH, ir, ig, ib, active ? 234 : 198);
        float navLabelScale = label != null && label.Length > 10 ? 0.185f : 0.220f;
        DrawText(label, left + 0.049f, top + 0.014f, navLabelScale, 0, active ? 242 : 192, active ? 244 : 196, active ? 247 : 202, 228);
    }

    private void DrawControlButton(float left, float top, float width, string label, bool active, int ar, int ag, int ab)
    {
        int bgR = active ? MixColor(12, ar, 0.10f) : 12;
        int bgG = active ? MixColor(16, ag, 0.10f) : 16;
        int bgB = active ? MixColor(20, ab, 0.10f) : 21;
        DrawRect(left + width / 2f, top + 0.020f, width, 0.040f, bgR, bgG, bgB, 232);
        DrawRect(left + 0.004f, top + 0.020f, 0.0026f, 0.028f, active ? ar : 95, active ? ag : 102, active ? ab : 112, active ? 205 : 120);
        DrawFrame(left, top, width, 0.040f, active ? ar : 66, active ? ag : 72, active ? ab : 82, active ? 92 : 44, 0.0007f);
        DrawText(label, left + 0.010f, top + 0.0115f, 0.195f, 0, 224, 228, 234, 222);
    }

    private void DrawVolumeWidget(float left, float top, float width, float height, float value, int ar, int ag, int ab)
    {
        value = Math.Max(0f, Math.Min(1f, value));
        int vr = MixColor(11, ar, 0.04f);
        int vg = MixColor(15, ag, 0.04f);
        int vb = MixColor(19, ab, 0.04f);
        DrawRect(left + width / 2f, top + height / 2f, width, height, vr, vg, vb, 234);
        DrawFrame(left, top, width, height, ar, ag, ab, 44, 0.00055f);
        DrawRect(left + width / 2f, top + 0.0032f, width * 0.72f, 0.0007f, 236, 240, 245, 14);

        float iconH = 0.032f;
        float iconW = PixelSquareWidth(iconH);
        float iconLeft = left + 0.010f;
        float iconTop = top + 0.007f;
        DrawMediaIconTile(9, iconLeft, iconTop, iconW, iconH, ar, ag, ab, true);
        DrawCenteredUiIconInCompactTile(9, iconLeft, iconTop, iconW, iconH, ar, ag, ab, 224);
        DrawText(T("VOLUME_SHORT"), iconLeft + iconW + 0.009f, top + 0.012f, 0.160f, 0, 166, 174, 184, 220);
        DrawText(((int)Math.Round(value * 100f)).ToString() + "%", left + width - 0.042f, top + 0.010f, 0.176f, 0, 243, 246, 249, 234);

        float trackLeft = left + 0.012f;
        float trackWidth = width - 0.024f;
        float trackY = top + 0.058f;
        DrawRect(trackLeft + trackWidth / 2f, trackY, trackWidth, 0.0055f, 46, 51, 59, 188);
        float fillWidth = Math.Max(0.0012f, trackWidth * value);
        DrawRect(trackLeft + fillWidth / 2f, trackY, fillWidth, 0.0055f, ar, ag, ab, 222);
        float knobX = trackLeft + Math.Min(trackWidth, Math.Max(0f, trackWidth * value));
        DrawRect(knobX, trackY, 0.0044f, 0.012f, 236, 239, 243, 220);
        DrawText("NUM-  /  NUM+", left + 0.012f, top + 0.087f, 0.136f, 0, 154, 163, 174, 208);
    }

    private void DrawStationLogoBadge(float left, float top, float width, float height, Station st, int ar, int ag, int ab)
    {
        string name = st != null && !string.IsNullOrEmpty(st.Name) ? st.Name : "RADIO";
        string upper = name.ToUpperInvariant();
        string line1 = "RADIO";
        string line2 = "LS";

        if (upper.Contains("1LIVE HIP HOP")) { line1 = "1LIVE"; line2 = "HIPHOP"; }
        else if (upper.Contains("1LIVE")) { line1 = "1"; line2 = "LIVE"; }
        else if (upper.Contains("YOU FM")) { line1 = "YOU"; line2 = "FM"; }
        else if (upper.Contains("COSMO")) { line1 = "COSMO"; line2 = "WDR"; }
        else if (upper.Contains("KEXP")) { line1 = "KEXP"; line2 = "SEA"; }
        else if (upper.Contains("WWOZ")) { line1 = "WWOZ"; line2 = "NOLA"; }
        else if (upper.Contains("WNCW")) { line1 = "WNCW"; line2 = "88.7"; }
        else if (upper.Contains("WDVX")) { line1 = "WDVX"; line2 = "TN"; }
        else if (upper.Contains("KXCI")) { line1 = "KXCI"; line2 = "TUC"; }
        else if (upper.Contains("WDR 2")) { line1 = "WDR"; line2 = "2"; }
        else if (upper.Contains("SWR3")) { line1 = "SWR"; line2 = "3"; }
        else if (upper.Contains("BAYERN 3")) { line1 = "BAYERN"; line2 = "3"; }
        else if (upper.Contains("ANTENNE BAYERN")) { line1 = "ANTENNE"; line2 = "BAYERN"; }
        else if (upper.Contains("FFH")) { line1 = "FFH"; line2 = "HIT"; }
        else if (upper.Contains("NDR 2")) { line1 = "NDR"; line2 = "2"; }
        else if (upper.Contains("N-JOY")) { line1 = "N-JOY"; line2 = "NDR"; }
        else if (upper.Contains("BIGFM")) { line1 = "BIG"; line2 = "FM"; }
        else if (upper.Contains("ENERGY")) { line1 = "ENERGY"; line2 = "BERLIN"; }
        else if (upper.Contains("RADIOEINS")) { line1 = "RADIO"; line2 = "EINS"; }
        else if (upper.Contains("HOT 97")) { line1 = "HOT"; line2 = "97"; }
        else if (upper.Contains("POWER 106")) { line1 = "POWER"; line2 = "106"; }
        else if (upper.Contains("KDAY")) { line1 = "KDAY"; line2 = "93.5"; }
        else if (upper.Contains("KROQ")) { line1 = "KROQ"; line2 = "106.7"; }
        else if (upper.Contains("KLOS")) { line1 = "KLOS"; line2 = "95.5"; }
        else if (upper.Contains("KCRW")) { line1 = "KCRW"; line2 = "89.9"; }
        else if (upper.Contains("KUTX")) { line1 = "KUTX"; line2 = "98.9"; }
        else if (upper.Contains("WFUV")) { line1 = "WFUV"; line2 = "90.7"; }
        else if (upper.Contains("KQED")) { line1 = "KQED"; line2 = "88.5"; }
        else if (upper.Contains("WNYC")) { line1 = "WNYC"; line2 = "93.9"; }
        else if (upper.Contains("BBC RADIO 1XTRA")) { line1 = "BBC"; line2 = "1XTRA"; }
        else if (upper.Contains("BBC RADIO 1")) { line1 = "BBC"; line2 = "RADIO 1"; }
        else if (upper.Contains("FUNX")) { line1 = "FUNX"; line2 = "NPO"; }
        else if (upper.Contains("NPO 3FM")) { line1 = "3FM"; line2 = "NPO"; }
        else if (upper.Contains("RADIO 538")) { line1 = "538"; line2 = "NL"; }
        else if (upper.Contains("NRJ")) { line1 = "NRJ"; line2 = "FR"; }
        else if (upper.Contains("LOS40")) { line1 = "LOS40"; line2 = "ES"; }
        else if (upper.Contains("TRIPLE J")) { line1 = "TRIPLE"; line2 = "J"; }
        else if (upper.Contains("ABSOLUTE RADIO")) { line1 = "ABS"; line2 = "RADIO"; }
        else if (upper.Contains("ABSOLUT")) { line1 = "ABS"; line2 = "RADIO"; }
        else if (upper.Contains("FIP")) { line1 = "FIP"; line2 = "FRANCE"; }
        else if (upper.Contains("RADIO PARADISE")) { line1 = "RP"; line2 = "MIX"; }
        else if (upper.Contains("80S80S")) { line1 = "80S"; line2 = "HIPHOP"; }
        else if (upper.Contains("REGENBOGEN")) { line1 = "RR"; line2 = "HIPHOP"; }
        else if (upper.Contains("KISS FM")) { line1 = "KISS"; line2 = "FM"; }
        else
        {
            StringBuilder logoBuilder = new StringBuilder();
            for (int i = 0; i < name.Length && logoBuilder.Length < 6; i++)
            {
                char ch = name[i];
                if (char.IsLetterOrDigit(ch)) logoBuilder.Append(ch);
            }
            string letters = logoBuilder.ToString().ToUpperInvariant();
            if (!string.IsNullOrEmpty(letters)) line1 = letters;
            line2 = "RADIO";
        }

        // The lower badge row is always the station's configured genre. This keeps
        // the HOME artwork honest instead of showing a hand-written pseudo-genre.
        if (st != null && !string.IsNullOrWhiteSpace(st.Genre))
            line2 = st.Genre.Trim().ToUpperInvariant();

        int cr, cg, cb;
        GetCategoryColor(st != null ? (st.Pack ?? "") : "", out cr, out cg, out cb);
        int bgR = MixColor(12, cr, 0.14f);
        int bgG = MixColor(17, cg, 0.12f);
        int bgB = MixColor(23, cb, 0.11f);
        int stripeR = MixColor(cr, 242, 0.16f);
        int stripeG = MixColor(cg, 244, 0.16f);
        int stripeB = MixColor(cb, 248, 0.16f);

        // Keep both text rows inside one balanced inner panel so sender and genre
        // visually belong to the same framed artwork at every badge size.
        DrawRect(left + width / 2f + 0.0012f, top + height / 2f + 0.0018f, width, height, 0, 0, 0, 58);
        DrawRect(left + width / 2f, top + height / 2f, width, height, 10, 15, 21, 238);

        float innerLeft = left + 0.0035f;
        float innerTop = top + height * 0.095f;
        float innerWidth = width - 0.0070f;
        float innerHeight = height * 0.810f;
        DrawRect(innerLeft + innerWidth / 2f, innerTop + innerHeight / 2f, innerWidth, innerHeight, bgR, bgG, bgB, 164);
        DrawFrame(innerLeft, innerTop, innerWidth, innerHeight, cr, cg, cb, 42, 0.00045f);
        DrawRect(left + 0.0040f, top + height / 2f, 0.0022f, height * 0.70f, stripeR, stripeG, stripeB, 176);
        DrawRect(left + width / 2f, innerTop + innerHeight * 0.16f, width * 0.74f, height * 0.085f, MixColor(bgR, 255, 0.12f), MixColor(bgG, 255, 0.12f), MixColor(bgB, 255, 0.12f), 46);
        DrawRect(left + width * 0.53f, top + height * 0.535f, width * 0.64f, 0.00055f, stripeR, stripeG, stripeB, 54);
        DrawFrame(left, top, width, height, cr, cg, cb, 92, 0.0007f);

        float line1Scale = line1.Length <= 2 ? 0.245f : (line1.Length <= 5 ? 0.190f : (line1.Length <= 8 ? 0.164f : 0.144f));
        float line2Scale = line2.Length <= 6 ? 0.128f : (line2.Length <= 10 ? 0.112f : (line2.Length <= 14 ? 0.098f : 0.086f));
        if (height < 0.060f) { line1Scale *= 0.90f; line2Scale *= 0.90f; }
        DrawTextCentered(Shorten(line1, 10), left + width * 0.53f, top + height * 0.245f, line1Scale, 0, 240, 244, 248, 236);
        DrawTextCentered(Shorten(line2, 18), left + width * 0.53f, top + height * 0.625f, line2Scale, 0, stripeR, stripeG, stripeB, 214);
    }

    private void DrawSegmentMeter(float left, float top, float width, float height, float value, int ar, int ag, int ab)
    {
        value = Math.Max(0f, Math.Min(1f, value));
        int segments = 10;
        float gap = width * 0.012f;
        float segW = (width - gap * (segments - 1)) / segments;
        int lit = (int)Math.Round(value * segments);
        for (int i = 0; i < segments; i++)
        {
            bool on = i < lit;
            DrawRect(left + segW / 2f + i * (segW + gap), top + height / 2f, segW, height, on ? ar : 55, on ? ag : 57, on ? ab : 62, on ? 230 : 180);
        }
    }

    private void DrawVolumeDial(float centerX, float centerY, float radius, float value, int ar, int ag, int ab)
    {
        value = Math.Max(0f, Math.Min(1f, value));
        int segments = 24;
        int lit = (int)Math.Round(value * segments);
        for (int i = 0; i < segments; i++)
        {
            double ang = (-140.0 + (280.0 * i / (segments - 1))) * Math.PI / 180.0;
            float x = centerX + (float)Math.Cos(ang) * radius;
            float y = centerY + (float)Math.Sin(ang) * radius * 1.25f;
            bool on = i < lit;
            DrawRect(x, y, 0.0052f, 0.0070f, on ? ar : 52, on ? ag : 55, on ? ab : 60, on ? 235 : 205);
        }
        DrawRect(centerX, centerY, radius * 1.20f, radius * 1.45f, 31, 33, 38, 250);
        DrawRect(centerX, centerY, radius * 0.88f, radius * 1.05f, 78, 82, 89, 245);
        DrawRect(centerX, centerY, radius * 0.66f, radius * 0.80f, 19, 21, 25, 250);
        DrawRect(centerX, centerY - radius * 0.28f, 0.004f, radius * 0.30f, 238, 240, 243, 230);
        DrawText(T("VOLUME_SHORT"), centerX - 0.015f, centerY - 0.011f, 0.165f, 0, 230, 231, 234, 230);
    }

    private void DrawDigitalKnob(float centerX, float centerY, float diameter, float value, int ar, int ag, int ab)
    {
        value = Math.Max(0f, Math.Min(1f, value));
        DrawRect(centerX, centerY, diameter, diameter, 28, 29, 33, 230);
        DrawRect(centerX, centerY, diameter * 0.74f, diameter * 0.74f, 82, 84, 90, 230);
        DrawRect(centerX, centerY, diameter * 0.58f, diameter * 0.58f, 20, 21, 24, 235);
        float barW = diameter * 0.74f;
        DrawSegmentMeter(centerX - barW / 2f, centerY + diameter * 0.46f, barW, 0.006f, value, ar, ag, ab);
        DrawText(T("VOLUME_SHORT"), centerX - 0.016f, centerY - 0.010f, 0.185f, 0, 225, 225, 228, 230);
    }

    private void DrawEqualizer(float left, float top, float width, float height, int ar, int ag, int ab)
    {
        int bars = visualizerBars != null && visualizerBars.Length > 0 ? visualizerBars.Length : 15;
        float gap = width * 0.025f;
        float bw = (width - gap * (bars - 1)) / bars;
        for (int i = 0; i < bars; i++)
        {
            float amount = (visualizerBars != null && i < visualizerBars.Length) ? visualizerBars[i] : 0.10f;
            amount = Math.Max(0.08f, Math.Min(1.0f, amount));
            float h = height * amount;
            int alpha = 118 + (int)(amount * 92.0f);
            int br = ar, bg = ag, bb = ab;
            if (IsSpectrumSkin())
            {
                UpdateSpectrumPalette();
                int r1, g1, b1, r2, g2, b2;
                GetSpectrumPhaseColorCached(0, out r1, out g1, out b1);
                GetSpectrumPhaseColorCached(2, out r2, out g2, out b2);
                float t = bars <= 1 ? 0.0f : (float)i / (bars - 1);
                br = MixColor(r1, r2, t); bg = MixColor(g1, g2, t); bb = MixColor(b1, b2, t);
                alpha = Math.Min(alpha, 190);
            }
            DrawRect(left + bw / 2f + i * (bw + gap), top + height - h / 2f, bw, h, br, bg, bb, alpha);
        }
    }

    private float GetCachedExternalMediaPeak(string source)
    {
        string key = (source ?? "").Trim().ToLowerInvariant();
        DateTime now = DateTime.Now;

        // Source change: do not carry an old Chrome/Spotify peak into the new source.
        if (!string.Equals(key, cachedExternalMediaPeakSource, StringComparison.Ordinal))
        {
            cachedExternalMediaPeakSource = key;
            cachedExternalMediaPeak = 0.0f;
            lastExternalMediaPeakQuery = DateTime.MinValue;
        }

        if ((now - lastExternalMediaPeakQuery).TotalMilliseconds < ExternalMediaPeakQueryMs)
            return cachedExternalMediaPeak;

        // TEST59: Core Audio session enumeration used to run directly on GTA's script
        // thread every ~180 ms for Spotify/YouTube. On systems with many audio sessions
        // that creates small frametime spikes. Queue one sample in the ThreadPool and
        // keep rendering with the last completed peak instead.
        if (Interlocked.CompareExchange(ref externalMediaPeakQueryInFlight, 1, 0) == 0)
        {
            lastExternalMediaPeakQuery = now;
            string requestedKey = key;
            string requestedSource = source ?? "";
            try
            {
                ThreadPool.QueueUserWorkItem(delegate(object state)
                {
                    try
                    {
                        float sampled = InternetRadioAudioMeter.GetPeakForBrowser(requestedSource);
                        sampled = Math.Max(0.0f, Math.Min(1.0f, sampled));
                        if (string.Equals(requestedKey, cachedExternalMediaPeakSource, StringComparison.Ordinal))
                            cachedExternalMediaPeak = sampled;
                    }
                    catch { }
                    finally { Interlocked.Exchange(ref externalMediaPeakQueryInFlight, 0); }
                });
            }
            catch
            {
                Interlocked.Exchange(ref externalMediaPeakQueryInFlight, 0);
            }
        }

        return cachedExternalMediaPeak;
    }

    private float GetCachedRadioMediaPeak(int helperPid)
    {
        if (helperPid <= 0)
        {
            cachedRadioMediaPeakPid = 0;
            cachedRadioMediaPeak = 0.0f;
            lastRadioMediaPeakQuery = DateTime.MinValue;
            return 0.0f;
        }

        DateTime now = DateTime.Now;

        if (helperPid != cachedRadioMediaPeakPid)
        {
            cachedRadioMediaPeakPid = helperPid;
            cachedRadioMediaPeak = 0.0f;
            lastRadioMediaPeakQuery = DateTime.MinValue;
        }

        if ((now - lastRadioMediaPeakQuery).TotalMilliseconds < RadioMediaPeakQueryMs)
            return cachedRadioMediaPeak;

        lastRadioMediaPeakQuery = now;

        try
        {
            cachedRadioMediaPeak = InternetRadioAudioMeter.GetPeakForProcessId(helperPid);
            cachedRadioMediaPeak = Math.Max(0.0f, Math.Min(1.0f, cachedRadioMediaPeak));
        }
        catch { }

        return cachedRadioMediaPeak;
    }

    private void UpdateAudioVisualizer()
    {
        if ((DateTime.Now - lastVisualizerUpdate).TotalMilliseconds < 45.0) return;
        lastVisualizerUpdate = DateTime.Now;

        float peak = 0.0f;
        bool sourceActive = false;

        try
        {
            if (visualizerMeterAvailable)
            {
                if (gtaRadioModeActive && gtaRadioPlayingRequested && cachedRadioCapableVehicle)
                {
                    // GTA's audio lives in the game process, not an external helper/session.
                    // Mark it active here; Beat Neon obtains real bands from BassAnalyzerBridge below.
                    sourceActive = true;
                    peak = 0.0f;
                }
                else if (youtubeModeActive && youtubeAvailable)
                {
                    sourceActive = IsYouTubePlaying() || string.Equals(youtubeState, "Playing", StringComparison.OrdinalIgnoreCase);
                    if (sourceActive) peak = GetCachedExternalMediaPeak(youtubeSource);
                }
                else if (spotifyModeActive && spotifyAvailable)
                {
                    sourceActive = IsSpotifyPlaying() || string.Equals(spotifyState, "Playing", StringComparison.OrdinalIgnoreCase);
                    if (sourceActive) peak = GetCachedExternalMediaPeak(spotifySource);
                }
                else if (enabled && shouldPlay && audio != null)
                {
                    if (!string.IsNullOrEmpty(cachedExternalMediaPeakSource))
                    {
                        cachedExternalMediaPeakSource = "";
                        cachedExternalMediaPeak = 0.0f;
                        lastExternalMediaPeakQuery = DateTime.MinValue;
                    }

                    sourceActive = string.Equals(cachedAudioState ?? "", "Playing", StringComparison.OrdinalIgnoreCase);
                    if (sourceActive)
                    {
                        int helperPid = audio.HelperPid;
                        if (helperPid > 0) peak = GetCachedRadioMediaPeak(helperPid);
                    }
                }
            }
            else
            {
                sourceActive = gtaRadioModeActive ? gtaRadioPlayingRequested :
                    (youtubeModeActive ? IsYouTubePlaying() : (spotifyModeActive ? IsSpotifyPlaying() : (enabled && shouldPlay && string.Equals(cachedAudioState ?? "", "Playing", StringComparison.OrdinalIgnoreCase))));
            }
        }
        catch (Exception ex)
        {
            visualizerMeterAvailable = false;
            peak = 0.0f;
            Log("VISUALIZER METER DISABLED | " + ex.Message);
        }

        float targetLevel = 0.0f;
        if (sourceActive)
        {
            if (visualizerMeterAvailable)
            {
                if (gtaRadioModeActive && gtaRadioPlayingRequested && peak <= 0.0001f)
                {
                    targetLevel = 0.20f;
                }
                float boosted = peak * 2.20f;
                if (boosted > 1.0f) boosted = 1.0f;
                if (!(gtaRadioModeActive && gtaRadioPlayingRequested && peak <= 0.0001f))
                    targetLevel = (float)Math.Pow(boosted, 0.72f);
                if (targetLevel < 0.03f && peak > 0.001f) targetLevel = 0.03f;
            }
            else
            {
                // Safe fallback: the mod still loads and the display keeps moving even without Core Audio metering.
                targetLevel = 0.22f;
            }
        }

        visualizerRawPeak = peak;
        if (targetLevel > visualizerLevel) visualizerLevel = visualizerLevel * 0.18f + targetLevel * 0.82f;
        else visualizerLevel = visualizerLevel * 0.72f + targetLevel * 0.28f;
        if (!sourceActive && visualizerLevel < 0.02f) visualizerLevel = 0.0f;

        double t = Game.GameTime * 0.0080;
        for (int i = 0; i < visualizerBars.Length; i++)
        {
            float centerBias = 1.0f - Math.Abs(((i + 0.5f) / visualizerBars.Length) * 2.0f - 1.0f);
            float shape = 0.68f + 0.32f * centerBias;
            float motion = (float)(0.46 + 0.54 * Math.Abs(Math.Sin(t + i * 0.74)));
            float transient = (float)(Math.Abs(Math.Sin(t * 1.85 + i * 0.92)) * Math.Min(0.24f, peak * 0.70f));
            float desired = 0.08f + visualizerLevel * shape * motion + transient;
            if (desired > 1.0f) desired = 1.0f;

            if (desired > visualizerBars[i]) visualizerBars[i] = visualizerBars[i] * 0.18f + desired * 0.82f;
            else visualizerBars[i] = visualizerBars[i] * 0.72f + desired * 0.28f;

            if (!sourceActive) visualizerBars[i] = Math.Max(0.08f, visualizerBars[i] * 0.88f);
        }
    }

    private bool IsMusicSourcePlayingForNeon()
    {
        try
        {
            // Keep the normal media-state checks, but also accept an already active
            // visualizer signal. This avoids losing the neon effect for a few ticks
            // while Windows updates the media-session state.
            if (youtubeModeActive && youtubeAvailable)
                return IsYouTubePlaying() ||
                    string.Equals(youtubeState, "Playing", StringComparison.OrdinalIgnoreCase) ||
                    visualizerRawPeak > 0.0015f || visualizerLevel > 0.035f;
            if (spotifyModeActive && spotifyAvailable)
                return IsSpotifyPlaying() ||
                    string.Equals(spotifyState, "Playing", StringComparison.OrdinalIgnoreCase) ||
                    visualizerRawPeak > 0.0015f || visualizerLevel > 0.035f;
            if (gtaRadioModeActive)
                return gtaRadioPlayingRequested && cachedRadioCapableVehicle;

            // For Internet Radio, shouldPlay is the authoritative playback intent.
            // The helper status file can lag behind the actual audio by a few hundred ms.
            return enabled && shouldPlay && audio != null;
        }
        catch { return false; }
    }

    private void CaptureVehicleNeon(int vehicleHandle)
    {
        neonTrackedVehicle = vehicleHandle;
        neonCaptured = false;
        neonCaptureChecked = true;
        neonEffectApplied = false;
        if (vehicleHandle == 0) return;

        try
        {
            bool anyEnabled = false;
            for (int i = 0; i < 4; i++)
            {
                bool isEnabled = Function.Call<bool>((Hash)0x8C4B92553E4766A5UL, vehicleHandle, i);
                neonOriginalEnabled[i] = isEnabled;
                if (isEnabled) anyEnabled = true;
            }
            if (!anyEnabled) return;

            using (OutputArgument r = new OutputArgument())
            using (OutputArgument g = new OutputArgument())
            using (OutputArgument b = new OutputArgument())
            {
                Function.Call((Hash)0x7619EEE8C886757FUL, vehicleHandle, r, g, b);
                neonBaseR = Math.Max(0, Math.Min(255, r.GetResult<int>()));
                neonBaseG = Math.Max(0, Math.Min(255, g.GetResult<int>()));
                neonBaseB = Math.Max(0, Math.Min(255, b.GetResult<int>()));
            }

            neonCaptured = true;
            Log("AUDIO NEON CAPTURE | Vehicle=" + vehicleHandle + " | RGB=" + neonBaseR + "," + neonBaseG + "," + neonBaseB);
        }
        catch (Exception ex)
        {
            neonCaptured = false;
            Log("AUDIO NEON CAPTURE FEHLER | " + ex.Message);
        }
    }

    private void RestoreAudioReactiveNeon(bool clearTracking)
    {
        try
        {
            if (neonCaptured && neonTrackedVehicle != 0)
            {
                Function.Call((Hash)0x8E0A582209A62695UL, neonTrackedVehicle, neonBaseR, neonBaseG, neonBaseB);
                for (int i = 0; i < 4; i++)
                    Function.Call((Hash)0x2AA720E4287BF269UL, neonTrackedVehicle, i, neonOriginalEnabled[i]);
            }
        }
        catch { }

        neonEffectApplied = false;
        neonFlashOn = false;
        neonDimLevel = 1.0f;
        neonFlashUntil = DateTime.MinValue;
        neonNextBeatAllowed = DateTime.MinValue;
        neonLastReactive = 0.0f;
        neonBeatBaseline = 0.0f;
        neonBassBaseline = 0.0f;
        neonBassPeak = 0.0001f;
        neonBassLast = 0.0f;
        neonBassDisplay = 0.0f;
        neonMidDisplay = 0.0f;
        if (clearTracking)
        {
            neonTrackedVehicle = 0;
            neonCaptured = false;
            neonCaptureChecked = false;
            for (int i = 0; i < 4; i++) neonOriginalEnabled[i] = false;
        }
    }

    // TEST57: Beat-Neon power saving. Stop the external bass helper immediately
    // when Beat Neon is disabled. Re-enabling remains lazy: the helper starts
    // automatically on the next active music/neon sample, avoiding idle CPU/I/O.
    private void SetAudioReactiveNeonEnabled(bool enabled)
    {
        bool changed = audioReactiveNeonEnabled != enabled;
        audioReactiveNeonEnabled = enabled;

        if (!enabled)
        {
            try { RestoreAudioReactiveNeon(true); } catch { }
            try { if (bassAnalyzer != null) bassAnalyzer.Close(); } catch { }
        }
        else if (changed)
        {
            // No eager process start here. TryGetLevels() starts it as soon as
            // music is playing in a vehicle with installed neon.
        }
    }

    private void UpdateAudioReactiveNeon()
    {
        // TEST56: 35 ms keeps the visual pulse closer to the analyzer updates while
        // still limiting neon work to ~29 lightweight updates/sec.
        if ((DateTime.Now - lastNeonUpdate).TotalMilliseconds < 35.0) return;
        lastNeonUpdate = DateTime.Now;

        if (!audioReactiveNeonEnabled || cachedPlayerDead || !cachedAudioReactiveLightingVehicle)
        {
            RestoreAudioReactiveNeon(true);
            return;
        }

        if (cachedInLosSantosCustoms)
        {
            RestoreAudioReactiveNeon(true);
            return;
        }

        int vehicleHandle = cachedVehicleHandle;

        if (vehicleHandle == 0)
        {
            RestoreAudioReactiveNeon(true);
            return;
        }

        if (vehicleHandle != neonTrackedVehicle)
        {
            RestoreAudioReactiveNeon(true);
            CaptureVehicleNeon(vehicleHandle);
        }
        else if (!neonCaptureChecked)
        {
            CaptureVehicleNeon(vehicleHandle);
        }

        // Only touch a vehicle that already had at least one neon side enabled.
        if (!neonCaptured) return;

        if (!IsMusicSourcePlayingForNeon())
        {
            if (neonEffectApplied) RestoreAudioReactiveNeon(false);
            return;
        }

        try
        {
            DateTime now = DateTime.Now;

            // TEST42 SAFE EXTERNAL BASS ANALYZER:
            // Frequency capture runs in BassAnalyzerBridge.ps1, outside the GTA script.
            // If that helper is unavailable, the proven TEST38 detector is used automatically.
            float lowBass = 0.0f;
            float kickBass = 0.0f;
            float vocalMids = 0.0f;
            bool realBassAvailable = false;

            try
            {
                if (bassAnalyzer != null)
                    realBassAvailable = bassAnalyzer.TryGetLevels(out lowBass, out kickBass, out vocalMids);
            }
            catch
            {
                realBassAvailable = false;
            }

            float strength = Math.Max(0.10f, Math.Min(1.00f, neonPulseStrength / 100.0f));
            float speed = Math.Max(0.10f, Math.Min(1.00f, neonPulseSpeed / 100.0f));
            bool bassPunch = false;
            bool heavyBassPunch = false;
            float body = 0.0f;

            if (realBassAvailable)
            {
                // Real low-frequency bands from Windows loopback:
                // TEST56 widens useful bass coverage: low/sub ~= 32-78 Hz,
                // kick/bass ~= 86-176 Hz.
                // Voice/mids ~= 260-1850 Hz are used only as a rejection reference.
                // TEST43 QUIET BASS BOOST:
                // Preserve isolated low/sub notes instead of averaging them away.
                // This makes a quiet 50-70 Hz note or a soft kick more visible.
                float bassMax = Math.Max(lowBass, kickBass);
                float bassMin = Math.Min(lowBass, kickBass);
                float bassRaw = Math.Max(0.0f, bassMax * 0.78f + bassMin * 0.22f);
                float midRaw = Math.Max(0.000001f, vocalMids);

                // Slower floor learning prevents soft bass notes from being absorbed
                // into the baseline too quickly.
                float floorLearn = 0.010f + (neonVoiceFilter / 100.0f) * 0.012f;
                if (neonBassBaseline <= 0.000001f)
                    neonBassBaseline = bassRaw;
                else
                    neonBassBaseline = neonBassBaseline * (1.0f - floorLearn) + bassRaw * floorLearn;

                // Faster peak decay lets quieter bass become "large" again shortly
                // after a louder hit instead of being masked by the old peak.
                if (bassRaw > neonBassPeak)
                    neonBassPeak = bassRaw;
                else
                    neonBassPeak = Math.Max(
                        neonBassBaseline + 0.000001f,
                        neonBassPeak * 0.986f + neonBassBaseline * 0.014f);

                float range = Math.Max(0.000001f, neonBassPeak - neonBassBaseline);
                float bassNorm = Math.Max(0.0f, Math.Min(1.0f,
                    (bassRaw - neonBassBaseline) / range));
                float bassRise = Math.Max(0.0f, bassRaw - neonBassLast);
                float relativeJump = Math.Max(0.0f,
                    (bassRaw - neonBassBaseline) / Math.Max(0.000001f, neonBassBaseline));
                float bassToMid = bassRaw / midRaw;

                neonBassLast = bassRaw;
                neonBassDisplay = neonBassDisplay * 0.66f + bassNorm * 0.34f;

                float uiScale = Math.Max(neonBassPeak, 0.000001f);
                float midUi = Math.Max(0.0f, Math.Min(1.0f, midRaw / uiScale));

                neonMidDisplay =
                    neonMidDisplay * 0.80f + midUi * 0.20f;

                float sensitivity = Math.Max(1.0f, Math.Min(10.0f, neonBeatSensitivity));
                float voice = Math.Max(0.0f, Math.Min(1.0f, neonVoiceFilter / 100.0f));

                // TEST56 DIRECT BASS RESPONSE:
                // The previous gate rejected too many real kicks when vocals/mids were
                // present. Keep the voice reference, but make it a softer rejection
                // factor and require less rise for short bass transients.
                float normThreshold = 0.68f - (sensitivity - 1.0f) * 0.045f;
                float jumpThreshold = 0.18f - (sensitivity - 1.0f) * 0.014f;
                float ratioThreshold = 0.22f + voice * 0.28f;
                float minRise = Math.Max(
                    0.000001f,
                    neonBassPeak * (0.006f + voice * 0.007f));

                normThreshold = Math.Max(0.24f, Math.Min(0.70f, normThreshold));
                jumpThreshold = Math.Max(0.040f, Math.Min(0.20f, jumpThreshold));

                bassPunch =
                    bassNorm >= normThreshold &&
                    relativeJump >= jumpThreshold &&
                    bassToMid >= ratioThreshold &&
                    bassRise >= minRise;

                // Quiet-bass rescue path:
                // A clean low-frequency transient may pass even if vocals/mids are
                // simultaneously present, but it still needs a strong normalized
                // low-end rise so normal speech should not trigger it by itself.
                float quietNormThreshold = Math.Max(0.34f, normThreshold - 0.11f);
                float quietJumpThreshold = Math.Max(0.032f, jumpThreshold * 0.52f);
                float quietRatioThreshold = Math.Max(0.16f, ratioThreshold * 0.55f);
                bool quietBassPunch =
                    bassNorm >= quietNormThreshold &&
                    relativeJump >= quietJumpThreshold &&
                    bassToMid >= quietRatioThreshold &&
                    bassRise >= minRise * 0.32f;

                heavyBassPunch =
                    bassNorm >= 0.82f &&
                    relativeJump >= Math.Max(0.06f, jumpThreshold * 0.66f) &&
                    bassToMid >= Math.Max(0.28f, ratioThreshold * 0.72f) &&
                    bassRise >= minRise * 0.45f;

                bassPunch = bassPunch || quietBassPunch;

                // Soft bass also contributes more to body glow, not only hard kicks.
                body = Math.Max(neonBassDisplay, bassNorm * 0.86f);
            }
            else
            {
                // Exact safe concept from TEST38.
                float sensitivityOffset = (6.0f - neonBeatSensitivity) * 0.018f;
                float voiceOffset = (neonVoiceFilter - 70.0f) / 100.0f;

                float instant = Math.Max(0.0f, Math.Min(1.0f,
                    visualizerRawPeak * (1.82f +
                    (neonBeatSensitivity - 6) * 0.045f)));
                instant = (float)Math.Pow(instant, 1.08f);

                float baselineLearn = 0.035f +
                    (neonVoiceFilter / 100.0f) * 0.020f;

                if (neonBeatBaseline <= 0.001f)
                    neonBeatBaseline = instant;
                else
                    neonBeatBaseline =
                        neonBeatBaseline * (1.0f - baselineLearn) +
                        instant * baselineLearn;

                float transient = Math.Max(0.0f, instant - neonBeatBaseline);
                float delta = instant - neonLastReactive;

                float minHit = 0.46f + sensitivityOffset + voiceOffset * 0.10f;
                float transientThreshold =
                    0.105f + sensitivityOffset * 0.70f +
                    voiceOffset * 0.075f +
                    neonBeatBaseline * (0.115f + voiceOffset * 0.045f);
                float riseThreshold =
                    0.032f + sensitivityOffset * 0.28f +
                    voiceOffset * 0.020f;

                minHit = Math.Max(0.28f, Math.Min(0.72f, minHit));
                transientThreshold = Math.Max(0.045f,
                    Math.Min(0.30f, transientThreshold));
                riseThreshold = Math.Max(0.010f,
                    Math.Min(0.075f, riseThreshold));

                bassPunch =
                    instant >= minHit &&
                    transient >= transientThreshold &&
                    delta >= riseThreshold;

                heavyBassPunch =
                    instant >= Math.Max(0.48f, minHit + 0.18f) &&
                    transient >= Math.Max(0.075f,
                        transientThreshold * 0.82f) &&
                    delta >= Math.Max(0.008f,
                        riseThreshold * 0.50f);

                neonLastReactive = instant;
                body = Math.Max(neonBeatBaseline,
                    visualizerLevel * 0.40f);
                neonBassDisplay =
                    neonBassDisplay * 0.80f + instant * 0.20f;
                neonMidDisplay =
                    neonMidDisplay * 0.82f + neonBeatBaseline * 0.18f;

            }

            int flashMs = 75 +
                (int)Math.Round((1.0f - speed) * 55.0f);
            int lockoutMs = 125 +
                (int)Math.Round((1.0f - speed) * 95.0f);

            if (now >= neonNextBeatAllowed &&
                (bassPunch || heavyBassPunch))
            {
                neonFlashUntil = now.AddMilliseconds(flashMs);
                neonNextBeatAllowed = now.AddMilliseconds(lockoutMs);
            }

            bool flashOn = now <= neonFlashUntil;

            float baseFloor = 0.34f - strength * 0.20f;
            float bodyGain = 0.50f - strength * 0.12f;
            float maxBody = 0.76f - strength * 0.20f;
            float continuous = baseFloor + bodyGain *
                (float)Math.Pow(
                    Math.Max(0.0f, Math.Min(1.0f, body)),
                    1.45f);
            float targetLevel = flashOn ? 1.0f :
                Math.Max(baseFloor, Math.Min(maxBody, continuous));

            float releaseBlend = 0.10f + speed * 0.23f;
            if (targetLevel > neonDimLevel)
                neonDimLevel =
                    neonDimLevel * 0.05f + targetLevel * 0.95f;
            else
                neonDimLevel =
                    neonDimLevel * (1.0f - releaseBlend) +
                    targetLevel * releaseBlend;

            neonDimLevel =
                Math.Max(baseFloor, Math.Min(1.0f, neonDimLevel));

            float gamma = 1.35f + strength * 0.65f;
            float rgbFactor = 0.020f + 0.980f *
                (float)Math.Pow(neonDimLevel, gamma);
            float beatBoost = flashOn ?
                (1.03f + strength * 0.13f) : 1.00f;

            if (flashOn) rgbFactor = 1.0f;

            int outR = Math.Max(1, Math.Min(255,
                (int)Math.Round(neonBaseR * rgbFactor * beatBoost)));
            int outG = Math.Max(1, Math.Min(255,
                (int)Math.Round(neonBaseG * rgbFactor * beatBoost)));
            int outB = Math.Max(1, Math.Min(255,
                (int)Math.Round(neonBaseB * rgbFactor * beatBoost)));

            Function.Call((Hash)0x8E0A582209A62695UL, vehicleHandle, outR, outG, outB);

            // TEST54: enabled neon sides do not change during a pulse, so there is
            // no need to send four SET_NEON_ENABLED natives every update.
            neonFlashOn = flashOn;
            neonEffectApplied = true;
        }
        catch (Exception ex)
        {
            RestoreAudioReactiveNeon(false);
            Log("AUDIO NEON FEHLER | " + ex.Message);
        }
    }

    private void ClearVehiclePlateLight()
    {
        PlateLightRenderActive = false;
        PlateLightVehicleHandle = 0;
        PlateLightBoneIndex = -1;
        plateLightVisibilityVehicleHandle = 0;
        plateLightMainLightsLatched = false;
        plateLightHeadlightControlWasDown = false;
        plateLightDayManualIntent = false;
        plateLightLastAmbientDark = false;
        plateLightHeadlightInputSettleAt = DateTime.MinValue;
        plateLightRawOnSince = DateTime.MinValue;
        plateLightRawOffSince = DateTime.MinValue;
    }

    private void ResolveVehiclePlateLightAnchor(int vehicleHandle)
    {
        PlateLightBoneIndex = -1;
        PlateLightFallbackY = -1.70f;
        PlateLightFallbackZ = 0.30f;

        if (vehicleHandle == 0) return;

        try
        {
            // "platelight" is the model bone at the lamp above the rear number plate.
            // Add-on vehicles occasionally omit it, so fall back to the numberplate bone.
            int bone = Function.Call<int>(Hash.GET_ENTITY_BONE_INDEX_BY_NAME, vehicleHandle, "platelight");
            if (bone < 0)
                bone = Function.Call<int>(Hash.GET_ENTITY_BONE_INDEX_BY_NAME, vehicleHandle, "numberplate");
            PlateLightBoneIndex = bone;
        }
        catch { PlateLightBoneIndex = -1; }

        if (PlateLightBoneIndex >= 0) return;

        // Last-resort fallback for vehicles with no plate bones. Model dimensions give us
        // a stable rear-centre position that also works for most add-on cars.
        try
        {
            int model = Function.Call<int>(Hash.GET_ENTITY_MODEL, vehicleHandle);
            using (OutputArgument minArg = new OutputArgument())
            using (OutputArgument maxArg = new OutputArgument())
            {
                Function.Call(Hash.GET_MODEL_DIMENSIONS, model, minArg, maxArg);
                GTA.Math.Vector3 min = minArg.GetResult<GTA.Math.Vector3>();
                GTA.Math.Vector3 max = maxArg.GetResult<GTA.Math.Vector3>();
                float height = Math.Max(0.45f, max.Z - min.Z);
                PlateLightFallbackY = min.Y - 0.025f;
                PlateLightFallbackZ = min.Z + height * 0.46f;
            }
        }
        catch { }
    }

    private void GetVehicleMainHeadlightState(int vehicleHandle, out bool lightsOn, out bool highBeamsOn)
    {
        lightsOn = false;
        highBeamsOn = false;
        if (vehicleHandle == 0) return;

        try
        {
            using (OutputArgument lightsArg = new OutputArgument())
            using (OutputArgument highArg = new OutputArgument())
            {
                Function.Call((Hash)0xB91B4C20085BD12FUL, vehicleHandle, lightsArg, highArg); // GET_VEHICLE_LIGHTS_STATE
                lightsOn = lightsArg.GetResult<bool>();
                highBeamsOn = highArg.GetResult<bool>();
            }
        }
        catch { }
    }

    private bool IsPlateLightAmbientDark()
    {
        // A strict time gate prevents daytime DRL/automatic-light reports from powering
        // the custom plate lamp. Manual headlight input can still enable it in daylight.
        try
        {
            int hour = Function.Call<int>(Hash.GET_CLOCK_HOURS);
            int minute = Function.Call<int>(Hash.GET_CLOCK_MINUTES);
            int totalMinutes = ((hour % 24 + 24) % 24) * 60 + Math.Max(0, Math.Min(59, minute));
            return totalMinutes >= (19 * 60 + 30) || totalMinutes < (6 * 60 + 30);
        }
        catch
        {
            return false;
        }
    }

    private bool IsVehicleHeadlightControlDown()
    {
        try
        {
            // INPUT_VEH_HEADLIGHT = 74. Raw native avoids dependency on a specific
            // ScriptHookVDotNet Control enum member.
            return Function.Call<bool>((Hash)0xF3A21BCD95725A4AUL, 0, 74); // IS_CONTROL_PRESSED
        }
        catch
        {
            return false;
        }
    }

    private bool IsVehiclePlateLightVisibilityStable(int vehicleHandle)
    {
        if (vehicleHandle == 0) return false;

        DateTime now = DateTime.Now;
        bool ambientDark = IsPlateLightAmbientDark();
        bool rawLightsOn, rawHighBeamsOn;
        GetVehicleMainHeadlightState(vehicleHandle, out rawLightsOn, out rawHighBeamsOn);

        if (plateLightVisibilityVehicleHandle != vehicleHandle)
        {
            plateLightVisibilityVehicleHandle = vehicleHandle;
            plateLightHeadlightControlWasDown = false;
            plateLightHeadlightInputSettleAt = DateTime.MinValue;
            plateLightRawOnSince = DateTime.MinValue;
            plateLightRawOffSince = DateTime.MinValue;
            plateLightDayManualIntent = false;
            plateLightLastAmbientDark = ambientDark;

            // Night: initialize from GTA's real light state. Day: start OFF so DRLs or
            // automatic daylight activity can never illuminate the number plate by mistake.
            plateLightMainLightsLatched = ambientDark && (rawLightsOn || rawHighBeamsOn);
        }

        bool headlightControlDown = IsVehicleHeadlightControlDown();
        if (headlightControlDown && !plateLightHeadlightControlWasDown)
        {
            // Let GTA finish its own light-state cycle before reading the result.
            plateLightHeadlightInputSettleAt = now.AddMilliseconds(180.0);
        }
        plateLightHeadlightControlWasDown = headlightControlDown;

        // NumPad9's manual high beam is explicit user intent and always powers the plate lamp.
        if (manualHighBeamActive || rawHighBeamsOn)
        {
            plateLightMainLightsLatched = true;
            if (!ambientDark) plateLightDayManualIntent = true;
            plateLightRawOnSince = DateTime.MinValue;
            plateLightRawOffSince = DateTime.MinValue;
            plateLightLastAmbientDark = ambientDark;
            return true;
        }

        if (plateLightHeadlightInputSettleAt != DateTime.MinValue && now >= plateLightHeadlightInputSettleAt)
        {
            bool afterLightsOn, afterHighBeamsOn;
            GetVehicleMainHeadlightState(vehicleHandle, out afterLightsOn, out afterHighBeamsOn);

            if (afterHighBeamsOn)
            {
                plateLightMainLightsLatched = true;
            }
            else if (!afterLightsOn)
            {
                plateLightMainLightsLatched = false;
            }
            else if (!ambientDark)
            {
                // In daylight GTA may report the same lightsOn value for DRL and low beam.
                // The physical headlight button press is therefore the reliable distinction.
                plateLightMainLightsLatched = !plateLightMainLightsLatched;
            }
            else
            {
                plateLightMainLightsLatched = true;
            }

            plateLightDayManualIntent = !ambientDark && plateLightMainLightsLatched;
            plateLightHeadlightInputSettleAt = DateTime.MinValue;
            plateLightRawOnSince = DateTime.MinValue;
            plateLightRawOffSince = DateTime.MinValue;
        }

        if (!ambientDark)
        {
            // Crossing from night to day resets the automatic/native latch. Only a fresh
            // daylight headlight-button action (or high beam above) may turn the plate lamp on.
            if (plateLightLastAmbientDark)
            {
                plateLightMainLightsLatched = false;
                plateLightDayManualIntent = false;
                plateLightRawOnSince = DateTime.MinValue;
                plateLightRawOffSince = DateTime.MinValue;
            }
            plateLightLastAmbientDark = false;
            return plateLightDayManualIntent && plateLightMainLightsLatched;
        }

        plateLightLastAmbientDark = true;
        plateLightDayManualIntent = false;

        // At night the native low/high-beam state is meaningful. Debounce it so brief
        // automatic-light or frame-state changes cannot visibly flicker the plate lamp.
        if (rawLightsOn)
        {
            plateLightRawOffSince = DateTime.MinValue;
            if (!plateLightMainLightsLatched)
            {
                if (plateLightRawOnSince == DateTime.MinValue)
                    plateLightRawOnSince = now;
                if ((now - plateLightRawOnSince).TotalMilliseconds >= 180.0)
                {
                    plateLightMainLightsLatched = true;
                    plateLightRawOnSince = DateTime.MinValue;
                }
            }
            else
            {
                plateLightRawOnSince = DateTime.MinValue;
            }
        }
        else
        {
            plateLightRawOnSince = DateTime.MinValue;
            if (plateLightMainLightsLatched)
            {
                if (plateLightRawOffSince == DateTime.MinValue)
                    plateLightRawOffSince = now;
                if ((now - plateLightRawOffSince).TotalMilliseconds >= 450.0)
                {
                    plateLightMainLightsLatched = false;
                    plateLightRawOffSince = DateTime.MinValue;
                }
            }
            else
            {
                plateLightRawOffSince = DateTime.MinValue;
            }
        }

        return plateLightMainLightsLatched;
    }

    private void GetVehicleHeadlightColourRgb(int vehicleHandle, out int r, out int g, out int b)
    {
        // GTA returns a headlight colour index (0..12, 255 = stock/default).
        // Convert that family to RGB for the small plate light.
        r = 255; g = 244; b = 225;
        try
        {
            int colour = Function.Call<int>((Hash)0x3DFF319A831E0CDBUL, vehicleHandle); // GET_VEHICLE_XENON_LIGHT_COLOR_INDEX
            if (colour == 0) { r = 245; g = 250; b = 255; }
            else if (colour == 1) { r = 170; g = 205; b = 255; }
            else if (colour == 2) { r = 90; g = 175; b = 255; }
            else if (colour == 3) { r = 120; g = 255; b = 210; }
            else if (colour == 4) { r = 155; g = 255; b = 70; }
            else if (colour == 5) { r = 255; g = 235; b = 80; }
            else if (colour == 6) { r = 255; g = 190; b = 45; }
            else if (colour == 7) { r = 255; g = 125; b = 35; }
            else if (colour == 8) { r = 255; g = 55; b = 45; }
            else if (colour == 9) { r = 255; g = 120; b = 190; }
            else if (colour == 10) { r = 255; g = 55; b = 175; }
            else if (colour == 11) { r = 185; g = 85; b = 255; }
            else if (colour == 12) { r = 115; g = 45; b = 255; }
        }
        catch { }
    }

    private void UpdateVehiclePlateLight()
    {
        if (!plateLightEnabled || cachedPlayerDead || !cachedRoadSpeedometerVehicle || cachedVehicleHandle == 0 || cachedInLosSantosCustoms)
        {
            ClearVehiclePlateLight();
            return;
        }

        int vehicleHandle = cachedVehicleHandle;
        if (!IsVehiclePlateLightVisibilityStable(vehicleHandle))
        {
            PlateLightRenderActive = false;
            return;
        }

        try
        {
            if (PlateLightVehicleHandle != vehicleHandle)
            {
                PlateLightVehicleHandle = vehicleHandle;
                ResolveVehiclePlateLightAnchor(vehicleHandle);
            }

            bool hasNeon = false;
            for (int i = 0; i < 4; i++)
            {
                if (Function.Call<bool>((Hash)0x8C4B92553E4766A5UL, vehicleHandle, i))
                {
                    hasNeon = true;
                    break;
                }
            }

            int r, g, b;
            if (hasNeon)
            {
                // Never follow Beat Neon: while the effect is changing GTA's neon RGB,
                // use the captured original/base neon colour for the plate lamp.
                if (neonTrackedVehicle == vehicleHandle && neonCaptured && neonEffectApplied)
                {
                    r = neonBaseR; g = neonBaseG; b = neonBaseB;
                }
                else
                {
                    using (OutputArgument rArg = new OutputArgument())
                    using (OutputArgument gArg = new OutputArgument())
                    using (OutputArgument bArg = new OutputArgument())
                    {
                        Function.Call((Hash)0x7619EEE8C886757FUL, vehicleHandle, rArg, gArg, bArg);
                        r = Math.Max(0, Math.Min(255, rArg.GetResult<int>()));
                        g = Math.Max(0, Math.Min(255, gArg.GetResult<int>()));
                        b = Math.Max(0, Math.Min(255, bArg.GetResult<int>()));
                    }
                }
            }
            else
            {
                GetVehicleHeadlightColourRgb(vehicleHandle, out r, out g, out b);
            }

            PlateLightR = r;
            PlateLightG = g;
            PlateLightB = b;
            PlateLightRange = hasNeon ? 1.10f : 0.95f;
            PlateLightIntensity = hasNeon ? 1.22f : 1.05f;
            PlateLightRenderActive = true;
        }
        catch (Exception ex)
        {
            ClearVehiclePlateLight();
            Log("PLATE LIGHT FEHLER | " + ex.Message);
        }
    }

    private void AddFactoryTurboModels(string names)
    {
        if (string.IsNullOrWhiteSpace(names)) return;
        string[] parts = names.Split(new char[] { ',', ';', '|', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string raw in parts)
        {
            string name = raw.Trim();
            if (name.Length == 0) continue;
            try
            {
                int hash = Game.GenerateHash(name.ToLowerInvariant());
                if (hash != 0) factoryTurboModelHashes.Add(hash);
            }
            catch { }
        }
    }

    private void RebuildFactoryTurboModelHashes()
    {
        factoryTurboModelHashes.Clear();

        // Conservative stock-turbo set for common GTA V / GTA Online road vehicles.
        // The configurable list below is intentionally additive so add-on cars can be
        // supported without recompiling the script. Spawn/model names are used here.
        AddFactoryTurboModels(
            "adder entityxf t20 nero nero2 thrax emerus krieger deveste entity2 entity3 " +
            "italigtb italigtb2 fmj reaper pfister811 sc1 italirsx " +
            "sultan sultanrs sultan2 sultan3 elegy elegy2 kuruma kuruma2 " +
            "jester jester2 jester3 jester4 comet2 comet3 comet4 comet5 comet6 comet7 " +
            "flashgt gb200 omnis tropos calico remus zr350 penumbra2 cypher growler " +
            "jugular komoda drafter tailgater2 sugoi issi7 previon " +
            "paragon paragon2 windsor windsor2 schafter3 schafter4 schafter5 schafter6 " +
            "rebla astron astron2 jubilee deity cinquemila rhinehart stingertt");

        AddFactoryTurboModels(factoryTurboModelsConfigured);
    }

    private bool VehicleHasRecognizedTurbo(int vehicleHandle, Vehicle vehicle, out string detectionKind)
    {
        detectionKind = "NONE";
        if (vehicleHandle == 0 || vehicle == null) return false;

        try
        {
            // Tuning slot 18 = user-installed turbo toggle.
            if (Function.Call<bool>((Hash)0x84B233A8C8FC8AE7UL, vehicleHandle, 18))
            {
                detectionKind = "AFTERMARKET";
                return true;
            }
        }
        catch { }

        try
        {
            if (factoryTurboModelHashes.Contains(vehicle.Model.Hash))
            {
                detectionKind = "FACTORY";
                return true;
            }
        }
        catch { }

        return false;
    }

    private string GetTurboImpactModeText()
    {
        if (turboImpactEnabled)
        {
            if (uiLanguage == "DE") return "AN";
            if (uiLanguage == "ES") return "ACTIVADO";
            if (uiLanguage == "FR") return "ACTIVÉ";
            if (uiLanguage == "IT") return "ATTIVO";
            if (uiLanguage == "PT") return "LIGADO";
            if (uiLanguage == "TR") return "AÇIK";
            return "ON";
        }

        if (uiLanguage == "DE") return "AUS";
        if (uiLanguage == "ES") return "DESACTIVADO";
        if (uiLanguage == "FR") return "DÉSACTIVÉ";
        if (uiLanguage == "IT") return "DISATTIVO";
        if (uiLanguage == "PT") return "DESLIGADO";
        if (uiLanguage == "TR") return "KAPALI";
        return "OFF";
    }

    private void SetTurboBoostAudioState(int vehicleHandle, bool active)
    {
        if (vehicleHandle == 0) return;
        try
        {
            // AUDIO::SET_VEHICLE_BOOST_ACTIVE. This controls GTA's own vehicle boost
            // sound effect; unlike v7.32 it does not allocate or layer foreign sound IDs.
            Function.Call((Hash)0x4A04DE7CAB2739A1UL, vehicleHandle, active);
        }
        catch { }
    }

    private void StopTurboImpactBoostPulse()
    {
        if (turboImpactBoostPulseActive && turboImpactVehicleHandle != 0)
            SetTurboBoostAudioState(turboImpactVehicleHandle, false);
        turboImpactBoostPulseActive = false;
        turboImpactBoostPulseUntil = DateTime.MinValue;
    }

    private void UpdateTurboImpactBoostPulse()
    {
        if (!turboImpactBoostPulseActive) return;
        if (turboImpactBoostPulseUntil == DateTime.MinValue || DateTime.Now < turboImpactBoostPulseUntil) return;
        StopTurboImpactBoostPulse();
    }

    private void ResetTurboImpactState(bool stopPulse)
    {
        if (stopPulse)
            StopTurboImpactBoostPulse();

        turboImpactVehicleHandle = 0;
        turboImpactLastGear = -1;
        turboImpactLastRpm = 0.0f;
        turboImpactLastThrottle = 0.0f;
        turboImpactArmed = false;
        turboImpactBoostSince = DateTime.MinValue;
        turboImpactLastTriggerAt = DateTime.MinValue;
        turboImpactLastInAir = false;
        turboImpactLoadPulseUsed = false;
        turboImpactLastDetectionVehicleHandle = 0;
        turboImpactLastDetectionKind = "";
    }

    // v7.37 triggerKind: 1=upshift, 2=hard throttle lift / blow-off.
    // Only one short SPORT-style pulse is allowed per armed boost phase.
    private void TriggerTurboImpact(int vehicleHandle, int triggerKind)
    {
        if (vehicleHandle == 0 || !turboImpactEnabled) return;

        if (turboImpactBoostPulseActive)
            StopTurboImpactBoostPulse();

        // Keep the former SPORT timing that tested well, but only for actual BOV events.
        // No load/air/high-speed pulses are generated anymore.
        int pulseMs = triggerKind == 1 ? 155 : 115;

        SetTurboBoostAudioState(vehicleHandle, false);
        SetTurboBoostAudioState(vehicleHandle, true);
        turboImpactBoostPulseActive = true;
        turboImpactBoostPulseUntil = DateTime.Now.AddMilliseconds(pulseMs);
    }

    private void UpdateTurboImpact()
    {
        UpdateTurboImpactBoostPulse();

        // Poll drivetrain state at ~40 ms. Speed and airborne state never suppress the
        // detector; they only slightly relax the arming threshold while the wheels unload.
        DateTime updateNow = DateTime.Now;
        if (turboImpactLastUpdateAt != DateTime.MinValue &&
            (updateNow - turboImpactLastUpdateAt).TotalMilliseconds < 40.0) return;
        turboImpactLastUpdateAt = updateNow;

        if (!turboImpactEnabled || cachedPlayerDead || !cachedRoadSpeedometerVehicle ||
            cachedVehicleHandle == 0 || cachedInLosSantosCustoms || menuOpen)
        {
            if (turboImpactVehicleHandle != 0 || turboImpactArmed || turboImpactBoostPulseActive)
                ResetTurboImpactState(true);
            return;
        }

        int vehicleHandle = cachedVehicleHandle;
        Ped player = null;
        try { player = Game.LocalPlayerPed; } catch { }
        if (player == null) return;

        try
        {
            int driver = Function.Call<int>(Hash.GET_PED_IN_VEHICLE_SEAT, vehicleHandle, -1);
            if (driver != player.Handle)
            {
                if (turboImpactVehicleHandle != 0 || turboImpactBoostPulseActive)
                    ResetTurboImpactState(true);
                return;
            }

            Vehicle vehicle = player.CurrentVehicle;
            string turboDetectionKind;
            bool turboInstalled = VehicleHasRecognizedTurbo(vehicleHandle, vehicle, out turboDetectionKind);
            if (!turboInstalled || vehicle == null)
            {
                if (turboImpactVehicleHandle != vehicleHandle)
                    ResetTurboImpactState(true);
                turboImpactVehicleHandle = vehicleHandle;
                turboImpactArmed = false;
                turboImpactBoostSince = DateTime.MinValue;
                turboImpactLoadPulseUsed = false;
                StopTurboImpactBoostPulse();
                return;
            }

            if (turboImpactLastDetectionVehicleHandle != vehicleHandle ||
                !string.Equals(turboImpactLastDetectionKind, turboDetectionKind, StringComparison.Ordinal))
            {
                turboImpactLastDetectionVehicleHandle = vehicleHandle;
                turboImpactLastDetectionKind = turboDetectionKind;
                Log("TURBO BLOW-OFF DETECT | " + turboDetectionKind + " | modelHash=" + vehicle.Model.Hash);
            }

            float rpm = Math.Max(0.0f, Math.Min(1.0f, vehicle.CurrentRPM));
            int gear = vehicle.CurrentGear;
            float throttle = 0.0f;
            float brake = 0.0f;
            try { throttle = Math.Max(0.0f, Math.Min(1.0f, Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, 71))); } catch { }
            try { brake = Math.Max(0.0f, Math.Min(1.0f, Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, 72))); } catch { }
            bool inAir = false;
            try { inAir = Function.Call<bool>(Hash.IS_ENTITY_IN_AIR, vehicleHandle); } catch { }

            if (turboImpactVehicleHandle != vehicleHandle)
            {
                ResetTurboImpactState(true);
                turboImpactVehicleHandle = vehicleHandle;
                turboImpactLastGear = gear;
                turboImpactLastRpm = rpm;
                turboImpactLastThrottle = throttle;
                turboImpactLastInAir = inAir;
                return;
            }

            // SPORT-style arming. Airborne cars use a slightly easier threshold so the BOV
            // still works after a jump even when wheel unloading makes RPM fluctuate.
            float armThrottle = inAir ? 0.56f : 0.68f;
            float armRpm = inAir ? 0.44f : 0.54f;
            const int armMs = 230;
            const int cooldownMs = 650;

            bool underBoostLoad = gear > 0 && throttle >= armThrottle && rpm >= armRpm;
            if (underBoostLoad)
            {
                if (turboImpactBoostSince == DateTime.MinValue)
                    turboImpactBoostSince = updateNow;
                else if (!turboImpactArmed &&
                         (updateNow - turboImpactBoostSince).TotalMilliseconds >= armMs)
                    turboImpactArmed = true;
            }
            else if (!turboImpactArmed)
            {
                turboImpactBoostSince = DateTime.MinValue;
            }

            bool cooldownReady = turboImpactLastTriggerAt == DateTime.MinValue ||
                (updateNow - turboImpactLastTriggerAt).TotalMilliseconds >= cooldownMs;

            // An upshift after real boost gets one stronger BOV accent. A hard lift is an
            // edge event: it can only fire once, then the driver must build boost again.
            // Holding the brake/throttle released therefore cannot repeat the effect.
            bool upshift = turboImpactLastGear > 0 && gear > turboImpactLastGear &&
                           turboImpactLastThrottle >= 0.48f &&
                           turboImpactLastRpm >= Math.Max(0.44f, armRpm - 0.08f);

            bool hardLift = turboImpactLastThrottle >= 0.66f && throttle <= 0.20f &&
                            turboImpactLastRpm >= Math.Max(0.44f, armRpm - 0.08f);

            int triggerKind = -1;
            if (turboImpactArmed && cooldownReady && upshift)
                triggerKind = 1;
            else if (turboImpactArmed && cooldownReady && hardLift)
                triggerKind = 2;

            if (triggerKind >= 0)
            {
                TriggerTurboImpact(vehicleHandle, triggerKind);
                turboImpactLastTriggerAt = updateNow;
                turboImpactArmed = false;
                turboImpactBoostSince = DateTime.MinValue;
            }

            // Do not kill a newly created BOV pulse merely because the player is now
            // braking. Only reset the arming state; re-arming requires another boost phase.
            if (throttle <= 0.20f || brake >= 0.15f)
            {
                turboImpactArmed = false;
                turboImpactBoostSince = DateTime.MinValue;
            }

            turboImpactLastGear = gear;
            turboImpactLastRpm = rpm;
            turboImpactLastThrottle = throttle;
            turboImpactLastInAir = inAir;
        }
        catch
        {
            ResetTurboImpactState(true);
        }
    }

    private void ClearAudioReactiveInteriorLight()
    {
        CabinLightRenderActive = false;
        CabinLightVehicleHandle = 0;
        CabinLightIntensity = 0.0f;
        interiorLightLevel = 0.0f;
    }

    private void UpdateAudioReactiveInteriorLight()
    {
        // The renderer draws every GTA frame; this update only calculates the current
        // music-reactive brightness and colour, so 45 ms is plenty for smooth audio motion.
        if ((DateTime.Now - lastInteriorLightUpdate).TotalMilliseconds < 45.0) return;
        lastInteriorLightUpdate = DateTime.Now;

        if (!audioReactiveInteriorLightEnabled || cachedPlayerDead || !cachedAudioReactiveLightingVehicle)
        {
            ClearAudioReactiveInteriorLight();
            return;
        }

        if (cachedInLosSantosCustoms)
        {
            ClearAudioReactiveInteriorLight();
            return;
        }

        if (!IsMusicSourcePlayingForNeon())
        {
            ClearAudioReactiveInteriorLight();
            return;
        }

        int vehicleHandle = cachedVehicleHandle;

        if (vehicleHandle == 0)
        {
            ClearAudioReactiveInteriorLight();
            return;
        }

        try
        {
            // Prefer the vehicle's own underglow colour only when this exact vehicle
            // really has neon enabled. Vehicles without neon use a normal warm-white
            // cabin light instead of inheriting a theme/service "neon" colour.
            int r, g, b;
            if (neonCaptured && neonTrackedVehicle == vehicleHandle)
            {
                r = neonBaseR;
                g = neonBaseG;
                b = neonBaseB;
            }
            else
            {
                r = 255;
                g = 244;
                b = 220;
            }

            float instant = Math.Max(0.0f, Math.Min(1.0f, visualizerRawPeak * 3.00f));
            instant = (float)Math.Pow(instant, 0.64f);
            float reactive = Math.Max(instant, visualizerLevel);

            // Cabin lighting should breathe instead of strobing. Keep a very low ambience
            // between beats, then use a fast attack and a longer, soft release.
            float target = 0.08f + reactive * 0.68f;
            if (instant >= 0.70f) target += 0.20f;
            target = Math.Max(0.06f, Math.Min(1.0f, target));

            if (target > interiorLightLevel)
                interiorLightLevel = interiorLightLevel * 0.16f + target * 0.84f;
            else
                interiorLightLevel = interiorLightLevel * 0.80f + target * 0.20f;

            interiorLightLevel = Math.Max(0.05f, Math.Min(1.0f, interiorLightLevel));

            float strength = Math.Max(0.10f, Math.Min(1.00f, interiorLightStrength / 100.0f));
            // DRAW_LIGHT_WITH_RANGE intensity is deliberately conservative so the light
            // reads as cabin ambience instead of illuminating the whole street.
            float intensity = (0.18f + interiorLightLevel * 1.55f) * strength;

            CabinLightVehicleHandle = vehicleHandle;
            CabinLightR = Math.Max(0, Math.Min(255, r));
            CabinLightG = Math.Max(0, Math.Min(255, g));
            CabinLightB = Math.Max(0, Math.Min(255, b));
            CabinLightRange = 1.35f;
            CabinLightIntensity = Math.Max(0.05f, Math.Min(2.0f, intensity));

            // TEST55: world-space cabin positions are intentionally NOT cached here.
            // At speed, a 60 ms cached position trails behind the moving vehicle and the
            // dynamic light becomes visible as a backward flicker/streak. The lightweight
            // renderer resolves these two local offsets against the current vehicle transform
            // every rendered frame; brightness/colour analysis remains throttled here.
            CabinLightRenderActive = true;
        }
        catch (Exception ex)
        {
            ClearAudioReactiveInteriorLight();
            Log("AUDIO INTERIOR LIGHT FEHLER | " + ex.Message);
        }
    }

    private void ShowActionFeedback(string label, string value)
    {
        actionFeedbackText = (label ?? "") + (string.IsNullOrEmpty(value) ? "" : "  ·  " + value);
        actionFeedbackUntil = DateTime.Now.AddMilliseconds(900);
    }

    private void DrawActionFeedback()
    {
        if (DateTime.Now > actionFeedbackUntil || string.IsNullOrEmpty(actionFeedbackText)) return;
        int ar, ag, ab; GetSkinAccent(out ar, out ag, out ab);
        float width = Math.Min(0.30f, Math.Max(0.13f, 0.085f + actionFeedbackText.Length * 0.0040f));
        float height = 0.032f;
        float left = 0.5f - width / 2f;
        float top = menuOpen ? 0.828f : 0.735f;
        DrawRect(left + width / 2f, top + height / 2f, width, height, 9, 13, 18, 220);
        DrawFrame(left, top, width, height, ar, ag, ab, 120, 0.0007f);
        DrawText(actionFeedbackText, left + 0.010f, top + 0.0065f, 0.170f, 0, 242, 245, 249, 235);
    }

    private void DrawBanner()
    {
        if (DateTime.Now > bannerUntil || string.IsNullOrEmpty(bannerTop)) return;
        float width = 0.29f;
        float height = 0.07f;
        float left = 0.355f;
        float top = 0.80f;
        // v1.0.1: this notification is reserved for explicit Num5 actions in
        // YouTube Music and Spotify. Each service uses its own recognizable accent.
        DrawRect(left + width / 2f, top + height / 2f, width, height, bannerR, bannerG, bannerB, 225);
        DrawText(bannerTop, left + 0.010f, top + 0.012f, 0.36f, 0, 255, 255, 255, 245);
        DrawText(bannerBottom, left + 0.010f, top + 0.036f, 0.28f, 0, 255, 255, 255, 235);
    }

    private void ShowBanner(string top, string bottom, int durationMs)
    {
        // v1.0.1: no general notification/action bar. The user only wants a
        // hint when Num5 is pressed on the YouTube Music or Spotify tab.
        if (!allowBannerOnce) return;
        allowBannerOnce = false;
        bannerTop = top ?? "";
        bannerBottom = bottom ?? "";
        bannerUntil = DateTime.Now.AddMilliseconds(durationMs <= 0 ? 2000 : durationMs);
    }

    private void ShowYouTubeSelectBanner(string top, string bottom, int durationMs)
    {
        bannerR = 190; bannerG = 24; bannerB = 38;
        allowBannerOnce = true;
        ShowBanner(top, bottom, durationMs);
    }

    private void ShowSpotifySelectBanner(string top, string bottom, int durationMs)
    {
        bannerR = 18; bannerG = 150; bannerB = 68;
        allowBannerOnce = true;
        ShowBanner(top, bottom, durationMs);
    }

    private string CurrentStationLine()
    {
        Station s = Current();
        return s == null ? T("NO_STATION") : s.Name;
    }

    private string StationDetailsLine(Station s)
    {
        if (s == null) return "";
        string a = string.IsNullOrEmpty(s.Pack) ? "" : DisplayPackName(s.Pack) + " | ";
        string b = string.IsNullOrEmpty(s.Genre) ? "Mix" : s.Genre;
        string c = string.IsNullOrEmpty(s.Vibe) ? "" : " | " + s.Vibe;
        return a + b + c;
    }

    private string GetNowPlayingLine()
    {
        if (!metadataEnabled) return "";
        string artist = CleanMeta(metadataArtist);
        string title = CleanMeta(metadataTitle);
        string media = CleanMeta(metadataMediaName);
        Station s = Current();
        string stationName = s != null ? CleanMeta(s.Name) : "";

        if (!string.IsNullOrEmpty(artist) && !string.IsNullOrEmpty(title))
            return artist + " - " + title;
        if (!string.IsNullOrEmpty(title) && !SameMeta(title, stationName))
            return title;
        if (!string.IsNullOrEmpty(media) && !SameMeta(media, stationName))
            return media;
        return "";
    }

    private static bool SameMeta(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
        return string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string CleanMeta(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        string s = value.Replace("\r", " ").Replace("\n", " ").Trim();
        while (s.Contains("  ")) s = s.Replace("  ", " ");
        if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return "";
        return s;
    }

    private static string Shorten(string value, int max)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= max) return value ?? "";
        return value.Substring(0, Math.Max(1, max - 3)) + "...";
    }

    private string MapState(string state)
    {
        string s = (state ?? "").Trim();
        if (s.Equals("Playing", StringComparison.OrdinalIgnoreCase) || s.Equals("Spielt", StringComparison.OrdinalIgnoreCase)) return T("STATE_PLAYING");
        if (s.Equals("Buffering", StringComparison.OrdinalIgnoreCase) || s.Equals("Puffert", StringComparison.OrdinalIgnoreCase)) return T("STATE_BUFFERING");
        if (s.Equals("Reconnecting", StringComparison.OrdinalIgnoreCase) || s.Equals("Verbindet", StringComparison.OrdinalIgnoreCase)) return T("STATE_CONNECTING");
        if (s.Equals("Waiting", StringComparison.OrdinalIgnoreCase) || s.Equals("Wartet", StringComparison.OrdinalIgnoreCase)) return T("STATE_WAITING");
        if (s.Equals("Ready", StringComparison.OrdinalIgnoreCase) || s.Equals("Bereit", StringComparison.OrdinalIgnoreCase)) return T("READY");
        if (s.Equals("Paused", StringComparison.OrdinalIgnoreCase) || s.Equals("Pausiert", StringComparison.OrdinalIgnoreCase)) return T("STATE_PAUSED");
        if (s.Equals("Stopped", StringComparison.OrdinalIgnoreCase) || s.Equals("Gestoppt", StringComparison.OrdinalIgnoreCase)) return T("STATE_STOPPED");
        if (s.Equals("MediaEnded", StringComparison.OrdinalIgnoreCase) || s.Equals("Beendet", StringComparison.OrdinalIgnoreCase)) return T("STATE_ENDED");
        if (s.Equals("Undefined", StringComparison.OrdinalIgnoreCase) || s.Equals("Aus", StringComparison.OrdinalIgnoreCase)) return T("STATE_OFF");
        if (s.Length == 0) return shouldPlay ? T("STATE_CONNECTING") : T("STATE_STOPPED");
        return s;
    }

    private string LocalizeYouTubeState()
    {
        if (!youtubeAvailable && string.IsNullOrEmpty(youtubeTitle)) return T("NOT_CONNECTED");
        return MapState(youtubeState);
    }

    private string LocalizeSpotifyState()
    {
        if (!spotifyAvailable && string.IsNullOrEmpty(spotifyTitle)) return T("NOT_CONNECTED");
        return MapState(spotifyState);
    }

    private string LocalizeDuckReason()
    {
        if (string.Equals(duckReason, "MENUE", StringComparison.OrdinalIgnoreCase)) return T("MENU");
        if (string.Equals(duckReason, "DIALOG", StringComparison.OrdinalIgnoreCase)) return T("DIALOG");
        if (string.Equals(duckReason, "TELEFON", StringComparison.OrdinalIgnoreCase) || string.Equals(duckReason, "ANRUF", StringComparison.OrdinalIgnoreCase)) return T("PHONE");
        if (string.Equals(duckReason, "CUSTOMS", StringComparison.OrdinalIgnoreCase)) return "LS CUSTOMS";
        return duckReason;
    }

    private void AdjustVolume(int delta)
    {
        // GTA Radio uses the game's own Audio setting. Do not alter the mod master
        // volume, show a volume popup, or imply that Num-/Num+ controls native radio.
        if (gtaRadioModeActive) return;
        volume = Math.Max(0, Math.Min(100, volume + delta));
        ApplyEffectiveVolume();

        // TEST20: independent themed volume popup. Refreshing this timer on every
        // step keeps it visible during hold-to-adjust and lets it fade after release.
        volumeHudDirection = delta > 0 ? 1 : (delta < 0 ? -1 : 0);
        volumeHudVisibleUntil = DateTime.Now.AddMilliseconds(1450);

        string volumeSource = youtubeModeActive ? ("YouTube Music | OUT " + MapUserVolumeToYouTube(volume) + "%") :
                              (spotifyModeActive ? ("Spotify | OUT " + MapUserVolumeToSpotify(volume) + "%") : CurrentStationLine());
        if (!IsExternalMusicModeActive() && shouldPlay && showMiniUi)
            miniUiVisibleUntil = DateTime.Now.AddMilliseconds(1800);
        ShowBanner(T("VOLUME") + " " + volume + "%", duckActive ? (T("DUCKING") + ": " + LocalizeDuckReason()) : volumeSource, 900);
        Log((youtubeModeActive ? "YT " : (spotifyModeActive ? "SPOTIFY " : "RADIO ")) + "VOLUME " + volume);
        MarkUserSettingsDirty();
    }

    private void ApplyEffectiveVolume()
    {
        float safeDuckMix = Math.Max(0.0f, Math.Min(1.0f, duckMix));
        float duckFloor = Math.Max(0.05f, Math.Min(1.0f, duckVolumePercent / 100.0f));
        float effectiveFactor = 1.0f - safeDuckMix * (1.0f - duckFloor);
        int userTarget = (int)Math.Round(volume * effectiveFactor);
        userTarget = Math.Max(0, Math.Min(100, userTarget));
        if (gtaRadioModeActive) return;
        if (youtubeModeActive)
        {
            // V12.54: use a continuous Windows-mixer level for fine control. The bridge still
            // receives an integer percentage for compatibility, but it no longer limits mixer precision.
            float youtubeMixerLevel = MapUserVolumeToYouTubeScalar(userTarget);
            int youtubeTarget = Math.Max(0, Math.Min(100, (int)Math.Round(youtubeMixerLevel * 100.0f)));
            bool mixerChanged = appliedYoutubeMixerLevel < 0.0f || Math.Abs(youtubeMixerLevel - appliedYoutubeMixerLevel) >= 0.0005f;
            bool bridgeChanged = youtubeTarget != appliedYoutubeVolume;
            if (!mixerChanged && !bridgeChanged) return;

            int changed = 0;
            if (mixerChanged)
            {
                appliedYoutubeMixerLevel = youtubeMixerLevel;
                try { changed = InternetRadioBrowserVolume.SetVolume(youtubeSource, youtubeMixerLevel); }
                catch (Exception ex) { Log("YT DIRECT MIXER FEHLER | " + ex.Message); }
            }
            if (bridgeChanged)
            {
                appliedYoutubeVolume = youtubeTarget;
                try { if (youtube != null) youtube.SetVolume(youtubeTarget); } catch { }
            }
            Log("YT VOLUME MAP | UI=" + userTarget + "% -> Browser=" + (youtubeMixerLevel * 100.0f).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "% | Bridge=" + youtubeTarget + "% | Sessions=" + changed + " | Available=" + youtubeAvailable);
            return;
        }

        if (spotifyModeActive)
        {
            float spotifyMixerLevel = MapUserVolumeToSpotifyScalar(userTarget);
            int spotifyTarget = Math.Max(0, Math.Min(100, (int)Math.Round(spotifyMixerLevel * 100.0f)));
            bool mixerChanged = appliedSpotifyMixerLevel < 0.0f || Math.Abs(spotifyMixerLevel - appliedSpotifyMixerLevel) >= 0.0005f;
            bool bridgeChanged = spotifyTarget != appliedSpotifyVolume;
            if (!mixerChanged && !bridgeChanged) return;

            int changed = 0;
            if (mixerChanged)
            {
                appliedSpotifyMixerLevel = spotifyMixerLevel;
                try { changed = InternetRadioBrowserVolume.SetVolume(spotifySource, spotifyMixerLevel); }
                catch (Exception ex) { Log("SPOTIFY DIRECT MIXER FEHLER | " + ex.Message); }
            }
            if (bridgeChanged)
            {
                appliedSpotifyVolume = spotifyTarget;
                try { if (spotify != null) spotify.SetVolume(spotifyTarget); } catch { }
            }
            Log("SPOTIFY VOLUME MAP | UI=" + userTarget + "% -> App=" + (spotifyMixerLevel * 100.0f).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "% | Bridge=" + spotifyTarget + "% | Sessions=" + changed + " | Available=" + spotifyAvailable);
            return;
        }

        int playerTarget = MapUserVolumeToPlayer(userTarget);
        if (audio == null) return;
        if (playerTarget == appliedStreamVolume) return;
        appliedStreamVolume = playerTarget;
        audio.SetVolume(playerTarget);
        Log("VOLUME MAP | UI=" + userTarget + "% -> Player=" + playerTarget + "%");
    }

    private int MapUserVolumeToPlayer(int userVolume)
    {
        userVolume = Math.Max(0, Math.Min(100, userVolume));
        if (userVolume <= 0) return 0;

        double normalized = userVolume / 100.0;
        double curved = Math.Pow(normalized, volumeCurveExponent);
        int mapped = (int)Math.Round(playerMaxVolume * curved);

        // 5 % soll noch hoerbar, aber wirklich sehr leise sein. Unter 5 % darf es 0 werden.
        if (userVolume >= 5 && mapped < 1) mapped = 1;
        return Math.Max(0, Math.Min(playerMaxVolume, mapped));
    }

    private float MapUserVolumeToYouTubeScalar(int userVolume)
    {
        userVolume = Math.Max(0, Math.Min(100, userVolume));
        if (userVolume <= 0) return 0.0f;
        double normalized = userVolume / 100.0;
        double curved = Math.Pow(normalized, youtubeVolumeCurveExponent);
        double level = (youtubeMaxVolume / 100.0) * curved;
        return (float)Math.Max(0.0, Math.Min(1.0, level));
    }

    private int MapUserVolumeToYouTube(int userVolume)
    {
        return Math.Max(0, Math.Min(100, (int)Math.Round(MapUserVolumeToYouTubeScalar(userVolume) * 100.0f)));
    }

    private float MapUserVolumeToSpotifyScalar(int userVolume)
    {
        userVolume = Math.Max(0, Math.Min(100, userVolume));
        if (userVolume <= 0) return 0.0f;
        double normalized = userVolume / 100.0;
        double curved = Math.Pow(normalized, spotifyVolumeCurveExponent);
        double level = (spotifyMaxVolume / 100.0) * curved;
        return (float)Math.Max(0.0, Math.Min(1.0, level));
    }

    private int MapUserVolumeToSpotify(int userVolume)
    {
        return Math.Max(0, Math.Min(100, (int)Math.Round(MapUserVolumeToSpotifyScalar(userVolume) * 100.0f)));
    }

    private void UpdateSpeechDucking()
    {
        // GTA Radio keeps Rockstar's native mix and is never ducked by this mod.
        if (gtaRadioModeActive) return;
        // V12.33: Ducking is allowed while YT mode owns the browser audio session even when
        // GSMTC briefly reports Paused/Unavailable during track changes.
        bool sourcePlaying = IsExternalMusicModeActive() ? true : shouldPlay;
        DateTime now = DateTime.Now;

        if (!sourcePlaying)
        {
            bool hadDuck = duckActive || duckMix > 0.001f;
            duckActive = false;
            duckReason = "";
            duckMix = 0.0f;
            lastDuckRampAt = now;
            if (hadDuck)
            {
                appliedStreamVolume = -1;
                appliedYoutubeVolume = -1;
                appliedYoutubeMixerLevel = -1.0f;
                appliedSpotifyVolume = -1;
                appliedSpotifyMixerLevel = -1.0f;
                ApplyEffectiveVolume();
            }
            return;
        }

        bool speechRecent = false;
        if (speechDucking)
        {
            if ((now - lastSpeechScan).TotalMilliseconds >= 100)
            {
                lastSpeechScan = now;
                // Scripted GTA conversations cover mission dialogue, phone-style conversations
                // and NPC speech that is not necessarily coming from a passenger in our vehicle.
                bool scriptedConversation = IsScriptedConversationOngoing();
                bool vehicleSpeech = IsSpeechPlayingInCurrentVehicle();
                if (scriptedConversation || vehicleSpeech) lastSpeechAt = now;
            }
            // Hold the duck across natural pauses between consecutive dialogue lines.
            speechRecent = (now - lastSpeechAt).TotalMilliseconds <= duckReleaseMs;
        }

        bool phoneCall = phoneCallDucking && IsMobilePhoneCallOngoing();
        bool ownMenu = menuDucking && menuOpen;
        // TEST23: The GTA vehicle customization interface is an in-game menu too.
        // Duck Internet Radio, YouTube Music and Spotify while the player is inside
        // Los Santos Customs / the standard vehicle-mod shops.
        bool customsMenu = menuDucking && cachedInLosSantosCustoms;
        bool wantDuck = speechRecent || phoneCall || ownMenu || customsMenu;

        string requestedReason = "";
        if (phoneCall) requestedReason = "ANRUF";
        else if (speechRecent) requestedReason = "DIALOG";
        else if (customsMenu) requestedReason = "CUSTOMS";
        else if (ownMenu) requestedReason = "MENUE";

        if (lastDuckRampAt == DateTime.MinValue) lastDuckRampAt = now;
        double elapsedMs = (now - lastDuckRampAt).TotalMilliseconds;
        if (elapsedMs < 1.0) elapsedMs = 1.0;
        if (elapsedMs > 250.0) elapsedMs = 250.0;
        lastDuckRampAt = now;

        float oldMix = duckMix;
        float targetMix = wantDuck ? 1.0f : 0.0f;
        int rampMs = targetMix > duckMix ? duckAttackMs : duckFadeOutMs;
        float step = (float)(elapsedMs / Math.Max(1, rampMs));

        if (targetMix > duckMix) duckMix = Math.Min(targetMix, duckMix + step);
        else if (targetMix < duckMix) duckMix = Math.Max(targetMix, duckMix - step);

        bool wasActive = duckActive;
        string oldReason = duckReason;
        duckActive = wantDuck || duckMix > 0.01f;
        if (wantDuck) duckReason = requestedReason;
        else if (duckMix <= 0.01f) duckReason = "";

        if (Math.Abs(duckMix - oldMix) >= 0.002f)
        {
            // TEST54: ApplyEffectiveVolume already ignores unchanged targets.
            // Keeping its caches prevents duplicate file/mixer volume writes.
            ApplyEffectiveVolume();
        }

        if (!wasActive && duckActive)
            Log("DUCK FADE IN | " + duckReason + " | Hold=" + duckReleaseMs + "ms | Attack=" + duckAttackMs + "ms");
        else if (wasActive && !duckActive)
            Log("DUCK FADE OUT COMPLETE");
        else if (wantDuck && !string.Equals(oldReason, duckReason, StringComparison.Ordinal))
            Log("DUCK REASON | " + duckReason);
    }

    private bool IsScriptedConversationOngoing()
    {
        try
        {
            // AUDIO::IS_SCRIPTED_CONVERSATION_ONGOING
            return Function.Call<bool>((Hash)0x16754C556D2EDE3DUL);
        }
        catch (Exception ex)
        {
            Log("SCRIPTED CONVERSATION CHECK FEHLER: " + ex.Message);
            return false;
        }
    }

    private bool IsInLosSantosCustoms()
    {
        try
        {
            Ped player = Game.LocalPlayerPed;
            if (player == null || !player.IsInVehicle() || IsCycleVehicle()) return false;

            int vehicleHandle = Function.Call<int>(Hash.GET_VEHICLE_PED_IS_IN, player.Handle, false);
            if (vehicleHandle == 0) return false;

            GTA.Math.Vector3 pos = Function.Call<GTA.Math.Vector3>(Hash.GET_ENTITY_COORDS, vehicleHandle, true);

            // Standard GTA V vehicle modification shops. Coordinates are intentionally
            // checked with a tight radius so merely driving past a shop does not duck audio.
            bool nearShop =
                IsNearPoint(pos, -337.0f, -136.8f, 39.0f, 24.0f) ||   // Burton
                IsNearPoint(pos, 731.8f, -1088.8f, 22.2f, 24.0f) ||   // La Mesa
                IsNearPoint(pos, -1155.5f, -2007.2f, 13.2f, 24.0f) || // LSIA
                IsNearPoint(pos, 1174.8f, 2640.0f, 37.8f, 24.0f) ||   // Harmony
                IsNearPoint(pos, 110.3f, 6626.0f, 31.8f, 24.0f);      // Beeker's / Paleto

            if (!nearShop) return false;

            // Primary signal: the vehicle or player is registered inside an interior.
            int vehicleInterior = 0;
            int playerInterior = 0;
            try { vehicleInterior = Function.Call<int>((Hash)0x2107BA504071A6BBUL, vehicleHandle); } catch { } // GET_INTERIOR_FROM_ENTITY
            try { playerInterior = Function.Call<int>((Hash)0x2107BA504071A6BBUL, player.Handle); } catch { }
            if (vehicleInterior != 0 || playerInterior != 0) return true;

            // Some game builds briefly report interior 0 while the modification camera/menu
            // is taking control. A nearly stationary vehicle very close to the shop center is
            // used as a conservative fallback so ducking does not flicker during that handoff.
            float speed = 99.0f;
            try { speed = Function.Call<float>(Hash.GET_ENTITY_SPEED, vehicleHandle); } catch { }
            if (speed <= 1.5f)
            {
                return
                    IsNearPoint(pos, -337.0f, -136.8f, 39.0f, 14.0f) ||
                    IsNearPoint(pos, 731.8f, -1088.8f, 22.2f, 14.0f) ||
                    IsNearPoint(pos, -1155.5f, -2007.2f, 13.2f, 14.0f) ||
                    IsNearPoint(pos, 1174.8f, 2640.0f, 37.8f, 14.0f) ||
                    IsNearPoint(pos, 110.3f, 6626.0f, 31.8f, 14.0f);
            }
        }
        catch (Exception ex)
        {
            Log("CUSTOMS DUCK CHECK FEHLER: " + ex.Message);
        }
        return false;
    }

    private bool IsNearPoint(GTA.Math.Vector3 pos, float x, float y, float z, float radius)
    {
        float dx = pos.X - x;
        float dy = pos.Y - y;
        float dz = pos.Z - z;
        return (dx * dx + dy * dy + dz * dz) <= (radius * radius);
    }

    private bool IsMobilePhoneCallOngoing()
    {
        try
        {
            // AUDIO::IS_MOBILE_PHONE_CALL_ONGOING
            return Function.Call<bool>((Hash)0x7497D2CE2C30D24CUL);
        }
        catch (Exception ex)
        {
            Log("PHONE DUCK CHECK FEHLER: " + ex.Message);
            return false;
        }
    }

    private bool IsSpeechPlayingInCurrentVehicle()
    {
        try
        {
            Ped player = Game.LocalPlayerPed;
            if (player == null || !player.IsInVehicle()) return false;
            int vehicleHandle = Function.Call<int>(Hash.GET_VEHICLE_PED_IS_IN, player.Handle, false);
            if (vehicleHandle == 0) return false;

            // Spieler zuerst pruefen.
            if (Function.Call<bool>(Hash.IS_ANY_SPEECH_PLAYING, player.Handle)) return true;

            // SAFE: keine World.GetNearbyPeds()-Allokationen. Stattdessen nur
            // die tatsaechlichen Sitze des aktuellen Fahrzeugs pruefen.
            int maxPassengers = 6;
            try
            {
                maxPassengers = Function.Call<int>(Hash.GET_VEHICLE_MAX_NUMBER_OF_PASSENGERS, vehicleHandle);
                if (maxPassengers < 0) maxPassengers = 0;
                if (maxPassengers > 12) maxPassengers = 12;
            }
            catch { maxPassengers = 6; }

            for (int seat = -1; seat < maxPassengers; seat++)
            {
                int pedHandle = 0;
                try { pedHandle = Function.Call<int>(Hash.GET_PED_IN_VEHICLE_SEAT, vehicleHandle, seat, false); }
                catch
                {
                    try { pedHandle = Function.Call<int>(Hash.GET_PED_IN_VEHICLE_SEAT, vehicleHandle, seat); } catch { pedHandle = 0; }
                }
                if (pedHandle == 0 || pedHandle == player.Handle) continue;
                if (Function.Call<bool>(Hash.IS_ANY_SPEECH_PLAYING, pedHandle)) return true;
            }
        }
        catch (Exception ex)
        {
            Log("DUCK CHECK FEHLER: " + ex.Message);
        }
        return false;
    }

    private string NormalizePreferredVehicleSource(string value)
    {
        string source = (value ?? "RADIO").Trim().ToUpperInvariant();
        if (source == "GTA" || source == "GTA RADIO" || source == "GTARADIO") return "GTA";
        if (source == "YOUTUBE" || source == "YOUTUBE MUSIC" || source == "YT") return "YOUTUBE";
        if (source == "SPOTIFY") return "SPOTIFY";
        return "RADIO";
    }

    private void RememberCurrentInternetStation()
    {
        if (stations.Count == 0) return;
        Station station = Current();
        if (station == null) return;
        lastInternetStationIndex = Math.Max(0, Math.Min(stations.Count - 1, index));
        lastInternetStationName = station.Name ?? "";
    }

    private int ResolveRememberedInternetStationIndex(int fallbackIndex)
    {
        if (stations.Count == 0) return 0;

        if (!string.IsNullOrWhiteSpace(lastInternetStationName))
        {
            for (int i = 0; i < stations.Count; i++)
            {
                Station station = stations[i];
                if (station != null && station.Enabled && !string.IsNullOrEmpty(station.Url) &&
                    string.Equals(station.Name ?? "", lastInternetStationName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
        }

        if (lastInternetStationIndex >= 0 && lastInternetStationIndex < stations.Count)
        {
            Station station = stations[lastInternetStationIndex];
            if (station != null && station.Enabled && !string.IsNullOrEmpty(station.Url))
                return lastInternetStationIndex;
        }

        return Math.Max(0, Math.Min(stations.Count - 1, fallbackIndex));
    }

    private string UserSettingsPath()
    {
        return Path.Combine(dir, "UserSettings.ini");
    }

    private void EnsureUserSettingsFile()
    {
        try
        {
            if (!File.Exists(UserSettingsPath())) SaveUserSettings();
        }
        catch (Exception ex) { Log("USER SETTINGS INIT FEHLER | " + ex.Message); }
    }

    private void MarkUserSettingsDirty()
    {
        userSettingsDirty = true;
        userSettingsSaveAt = DateTime.Now.AddMilliseconds(450);
    }

    private void FlushUserSettingsIfDue()
    {
        if (!userSettingsDirty || DateTime.Now < userSettingsSaveAt) return;
        SaveUserSettings();
    }

    private void LoadUserSettingsOverrides()
    {
        try
        {
            string path = UserSettingsPath();
            if (!File.Exists(path)) return;
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            bool inUser = false;
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#")) continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    inUser = line.Equals("[User]", StringComparison.OrdinalIgnoreCase);
                    continue;
                }
                if (!inUser) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                values[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
            }

            string v; int iv; float fv; bool bv;
            if (values.TryGetValue("LastSource", out v)) preferredVehicleSource = NormalizePreferredVehicleSource(v);
            if (values.TryGetValue("LastInternetStationIndex", out v) && int.TryParse(v, out iv)) lastInternetStationIndex = Math.Max(-1, iv);
            if (values.TryGetValue("LastInternetStationName", out v)) lastInternetStationName = (v ?? "").Trim();
            if (values.TryGetValue("YouTubePlaybackState", out v)) lastYouTubePlaybackPlaying = string.Equals((v ?? "").Trim(), "Playing", StringComparison.OrdinalIgnoreCase);
            if (values.TryGetValue("SpotifyPlaybackState", out v)) lastSpotifyPlaybackPlaying = string.Equals((v ?? "").Trim(), "Playing", StringComparison.OrdinalIgnoreCase);
            youtubeResumeAfterVehicle = lastYouTubePlaybackPlaying;
            spotifyResumeAfterVehicle = lastSpotifyPlaybackPlaying;
            if (values.TryGetValue("Language", out v)) uiLanguage = NormalizeLanguage(v);
            if (values.TryGetValue("Volume", out v) && int.TryParse(v, out iv)) volume = Math.Max(0, Math.Min(100, iv));
            if (values.TryGetValue("VolumeStep", out v)) volumeStep = 1;
            if (values.TryGetValue("DuckVolumePercent", out v) && int.TryParse(v, out iv)) duckVolumePercent = Math.Max(20, Math.Min(100, iv));
            if (values.TryGetValue("DuckReleaseMs", out v) && int.TryParse(v, out iv)) duckReleaseMs = Math.Max(500, Math.Min(4000, iv));
            if (values.TryGetValue("DuckFadeOutMs", out v) && int.TryParse(v, out iv)) duckFadeOutMs = Math.Max(200, Math.Min(2000, iv));
            if (values.TryGetValue("PlayerMaxVolume", out v) && int.TryParse(v, out iv)) playerMaxVolume = Math.Max(20, Math.Min(100, iv));
            if (values.TryGetValue("YouTubeMaxVolume", out v) && int.TryParse(v, out iv)) youtubeMaxVolume = Math.Max(5, Math.Min(100, iv));
            if (values.TryGetValue("SpotifyMaxVolume", out v) && int.TryParse(v, out iv)) spotifyMaxVolume = Math.Max(5, Math.Min(100, iv));
            if (values.TryGetValue("VolumeCurveExponent", out v) && float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out fv)) volumeCurveExponent = Math.Max(1.0f, Math.Min(3.0f, fv));
            if (values.TryGetValue("YouTubeVolumeCurveExponent", out v) && float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out fv)) youtubeVolumeCurveExponent = Math.Max(0.8f, Math.Min(2.8f, fv));
            if (values.TryGetValue("SpotifyVolumeCurveExponent", out v) && float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out fv)) spotifyVolumeCurveExponent = Math.Max(0.8f, Math.Min(2.8f, fv));
            if (values.TryGetValue("YouTubeVolumeProfileVersion", out v) && int.TryParse(v, out iv)) youtubeVolumeProfileVersion = Math.Max(0, iv);
            if (values.TryGetValue("Skin", out v)) skinIndex = GetSkinIndex(v);
            skinMenuIndex = skinIndex;
            if (values.TryGetValue("MenuSlowMotion", out v) && bool.TryParse(v, out bv)) menuSlowMotionEnabled = bv;
            if (values.TryGetValue("MenuTimeScale", out v) && float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out fv)) menuTimeScale = Math.Max(0.20f, Math.Min(1.00f, fv));
            if (values.TryGetValue("ShowMiniUi", out v) && bool.TryParse(v, out bv)) showMiniUi = bv;
            if (values.TryGetValue("PersistentMiniUi", out v) && bool.TryParse(v, out bv)) persistentMiniUi = bv;
            if (values.TryGetValue("HideMiniUiWhileMenuOpen", out v) && bool.TryParse(v, out bv)) hideMiniUiWhileMenuOpen = bv;
            if (values.TryGetValue("MenuOpenSound", out v) && bool.TryParse(v, out bv)) menuOpenSoundEnabled = bv;
            if (values.TryGetValue("ArtworkEnabled", out v) && bool.TryParse(v, out bv)) mediaArtworkEnabled = bv;
            if (values.TryGetValue("AudioReactiveNeon", out v) && bool.TryParse(v, out bv)) audioReactiveNeonEnabled = bv;
            if (values.TryGetValue("NeonBeatSensitivity", out v) && int.TryParse(v, out iv)) neonBeatSensitivity = Math.Max(1, Math.Min(10, iv));
            if (values.TryGetValue("NeonVoiceFilter", out v) && int.TryParse(v, out iv)) neonVoiceFilter = Math.Max(0, Math.Min(100, iv));
            if (values.TryGetValue("NeonPulseStrength", out v) && int.TryParse(v, out iv)) neonPulseStrength = Math.Max(10, Math.Min(100, iv));
            if (values.TryGetValue("NeonPulseSpeed", out v) && int.TryParse(v, out iv)) neonPulseSpeed = Math.Max(10, Math.Min(100, iv));
            if (values.TryGetValue("AudioReactiveInteriorLight", out v) && bool.TryParse(v, out bv)) audioReactiveInteriorLightEnabled = bv;
            int interiorStrengthValue;
            if (values.TryGetValue("InteriorLightStrength", out v) && int.TryParse(v, out interiorStrengthValue))
                interiorLightStrength = Math.Max(10, Math.Min(100, interiorStrengthValue));
            if (values.TryGetValue("PlateLightEnabled", out v) && bool.TryParse(v, out bv)) plateLightEnabled = bv;
            if (values.TryGetValue("TurboImpactEnabled", out v) && bool.TryParse(v, out bv))
                turboImpactEnabled = bv;
            else if (values.TryGetValue("TurboImpactMode", out v) && int.TryParse(v, out iv))
                turboImpactEnabled = iv > 0; // migrate v7.32-v7.35 settings
            if (values.TryGetValue("ShowCustomSpeedometer", out v) && bool.TryParse(v, out bv)) showCustomSpeedometer = bv;
            if (values.TryGetValue("SpeedometerShowCarIcon", out v) && bool.TryParse(v, out bv)) speedometerShowCarIcon = bv;
            if (values.TryGetValue("SpeedometerShowRpm", out v) && bool.TryParse(v, out bv)) speedometerShowRpm = bv;
            if (values.TryGetValue("SpeedometerUnit", out v)) speedometerUseMph = string.Equals((v ?? "").Trim(), "MPH", StringComparison.OrdinalIgnoreCase);
            if (values.TryGetValue("ShowBoatHud", out v) && bool.TryParse(v, out bv)) showBoatHud = bv;
            if (values.TryGetValue("ShowFlightHud", out v) && bool.TryParse(v, out bv)) showFlightHud = bv;
            if (values.TryGetValue("NavigationUnits", out v)) navigationUseMetric = string.Equals((v ?? "").Trim(), "METRIC", StringComparison.OrdinalIgnoreCase);
            if (values.TryGetValue("AutomaticIndicators", out v) && bool.TryParse(v, out bv)) automaticIndicatorsEnabled = bv;
            if (values.TryGetValue("SpeedometerLeft", out v) && float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out fv)) speedometerLeft = Math.Max(0.005f, Math.Min(0.845f, fv));
            if (values.TryGetValue("SpeedometerTop", out v) && float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out fv)) speedometerTop = Math.Max(0.600f, Math.Min(0.930f, fv));
            if (values.TryGetValue("SpeedometerWidth", out v) && float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out fv)) speedometerWidth = Math.Max(0.150f, Math.Min(0.320f, fv));
            if (values.TryGetValue("SpeedometerHeight", out v) && float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out fv)) speedometerHeight = Math.Max(0.095f, Math.Min(0.180f, fv));
            if (values.TryGetValue("SpeedometerOpacity", out v) && int.TryParse(v, out iv)) speedometerOpacity = Math.Max(20, Math.Min(100, iv));
            if (values.TryGetValue("SpectrumSkinStrength", out v) && float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out fv))
                dynamicRadioBackgroundStrength = Math.Max(0.25f, Math.Min(1.00f, fv));
            else if (values.TryGetValue("RainbowSkinStrength", out v) && float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out fv))
                dynamicRadioBackgroundStrength = Math.Max(0.25f, Math.Min(1.00f, fv));
            else if (values.TryGetValue("DynamicRadioBackgroundStrength", out v) && float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out fv))
                dynamicRadioBackgroundStrength = Math.Max(0.25f, Math.Min(1.00f, fv));
            if (values.TryGetValue("GtaRadioStationIndex", out v) && int.TryParse(v, out iv)) gtaRadioIndex = Math.Max(0, iv);
            if (values.TryGetValue("Enabled", out v) && bool.TryParse(v, out bv)) enabled = bv;
            if (values.TryGetValue("SpeechDucking", out v) && bool.TryParse(v, out bv)) speechDucking = bv;
            if (values.TryGetValue("PhoneCallDucking", out v) && bool.TryParse(v, out bv)) phoneCallDucking = bv;
            if (values.TryGetValue("MenuDucking", out v) && bool.TryParse(v, out bv)) menuDucking = bv;
            favoriteStationNames.Clear();
            for (int favoriteSlot = 1; favoriteSlot <= MaxFavorites; favoriteSlot++)
            {
                if (values.TryGetValue("Favorite" + favoriteSlot, out v) && !string.IsNullOrWhiteSpace(v))
                {
                    bool duplicate = false;
                    for (int i = 0; i < favoriteStationNames.Count; i++)
                        if (string.Equals(favoriteStationNames[i], v.Trim(), StringComparison.OrdinalIgnoreCase)) { duplicate = true; break; }
                    if (!duplicate) favoriteStationNames.Add(v.Trim());
                }
            }
            Log("USER SETTINGS LOADED | Source=" + preferredVehicleSource + " | YT=" + (lastYouTubePlaybackPlaying ? "Playing" : "Paused") + " | Spotify=" + (lastSpotifyPlaybackPlaying ? "Playing" : "Paused") + " | InternetStation=" + lastInternetStationName + " [" + lastInternetStationIndex + "] | Language=" + uiLanguage + " | Volume=" + volume + " | Duck=" + duckVolumePercent + " | Hold=" + duckReleaseMs + "ms | Fade=" + duckFadeOutMs + "ms | RadioMax=" + playerMaxVolume + " | YTMax=" + youtubeMaxVolume + " | SpotifyMax=" + spotifyMaxVolume + " | Skin=" + skinNames[skinIndex] + " | Favorites=" + favoriteStationNames.Count);
        }
        catch (Exception ex)
        {
            Log("USER SETTINGS LOAD FEHLER | " + ex.Message);
        }
    }

    private void SaveUserSettings()
    {
        volumeStep = 1;
        try
        {
            string path = UserSettingsPath();
            string temp = path + ".tmp";
            string favoritesData = "";
            string safeLastInternetStationName = (lastInternetStationName ?? "").Replace("\r", "").Replace("\n", "");
            for (int favoriteSlot = 0; favoriteSlot < MaxFavorites; favoriteSlot++)
            {
                string favoriteName = favoriteSlot < favoriteStationNames.Count ? favoriteStationNames[favoriteSlot] : "";
                favoriteName = favoriteName.Replace("\r", "").Replace("\n", "");
                favoritesData += "Favorite" + (favoriteSlot + 1) + "=" + favoriteName + Environment.NewLine;
            }
            string data =
                "; Los Santos Internet Radio - persistent user preferences" + Environment.NewLine +
                "; This file is created by the mod. Keep it when updating the mod." + Environment.NewLine +
                "[User]" + Environment.NewLine +
                "LastSource=" + NormalizePreferredVehicleSource(preferredVehicleSource) + Environment.NewLine +
                "LastInternetStationIndex=" + lastInternetStationIndex + Environment.NewLine +
                "LastInternetStationName=" + safeLastInternetStationName + Environment.NewLine +
                "YouTubePlaybackState=" + (lastYouTubePlaybackPlaying ? "Playing" : "Paused") + Environment.NewLine +
                "SpotifyPlaybackState=" + (lastSpotifyPlaybackPlaying ? "Playing" : "Paused") + Environment.NewLine +
                "Language=" + uiLanguage + Environment.NewLine +
                "Volume=" + volume + Environment.NewLine +
                "DuckVolumePercent=" + duckVolumePercent + Environment.NewLine +
                "DuckReleaseMs=" + duckReleaseMs + Environment.NewLine +
                "DuckFadeOutMs=" + duckFadeOutMs + Environment.NewLine +
                "PlayerMaxVolume=" + playerMaxVolume + Environment.NewLine +
                "YouTubeMaxVolume=" + youtubeMaxVolume + Environment.NewLine +
                "SpotifyMaxVolume=" + spotifyMaxVolume + Environment.NewLine +
                "VolumeCurveExponent=" + volumeCurveExponent.ToString(System.Globalization.CultureInfo.InvariantCulture) + Environment.NewLine +
                "YouTubeVolumeCurveExponent=" + youtubeVolumeCurveExponent.ToString(System.Globalization.CultureInfo.InvariantCulture) + Environment.NewLine +
                "SpotifyVolumeCurveExponent=" + spotifyVolumeCurveExponent.ToString(System.Globalization.CultureInfo.InvariantCulture) + Environment.NewLine +
                "YouTubeVolumeProfileVersion=" + youtubeVolumeProfileVersion + Environment.NewLine +
                "Skin=" + skinNames[Math.Max(0, Math.Min(skinNames.Length - 1, skinIndex))] + Environment.NewLine +
                "MenuSlowMotion=" + menuSlowMotionEnabled.ToString().ToLowerInvariant() + Environment.NewLine +
                "MenuTimeScale=" + menuTimeScale.ToString(System.Globalization.CultureInfo.InvariantCulture) + Environment.NewLine +
                "ShowMiniUi=" + showMiniUi.ToString().ToLowerInvariant() + Environment.NewLine +
                "PersistentMiniUi=" + persistentMiniUi.ToString().ToLowerInvariant() + Environment.NewLine +
                "HideMiniUiWhileMenuOpen=" + hideMiniUiWhileMenuOpen.ToString().ToLowerInvariant() + Environment.NewLine +
                "MenuOpenSound=" + menuOpenSoundEnabled.ToString().ToLowerInvariant() + Environment.NewLine +
                "ArtworkEnabled=" + mediaArtworkEnabled.ToString().ToLowerInvariant() + Environment.NewLine +
                "AudioReactiveNeon=" + audioReactiveNeonEnabled.ToString().ToLowerInvariant() + Environment.NewLine +
                "NeonBeatSensitivity=" + neonBeatSensitivity + Environment.NewLine +
                "NeonVoiceFilter=" + neonVoiceFilter + Environment.NewLine +
                "NeonPulseStrength=" + neonPulseStrength + Environment.NewLine +
                "NeonPulseSpeed=" + neonPulseSpeed + Environment.NewLine +
                "AudioReactiveInteriorLight=" + audioReactiveInteriorLightEnabled.ToString().ToLowerInvariant() + Environment.NewLine +
                "InteriorLightStrength=" + interiorLightStrength + Environment.NewLine +
                "PlateLightEnabled=" + plateLightEnabled.ToString().ToLowerInvariant() + Environment.NewLine +
                "TurboImpactEnabled=" + turboImpactEnabled.ToString().ToLowerInvariant() + Environment.NewLine +
                "ShowCustomSpeedometer=" + showCustomSpeedometer.ToString().ToLowerInvariant() + Environment.NewLine +
                "SpeedometerShowCarIcon=" + speedometerShowCarIcon.ToString().ToLowerInvariant() + Environment.NewLine +
                "SpeedometerShowRpm=" + speedometerShowRpm.ToString().ToLowerInvariant() + Environment.NewLine +
                "SpeedometerUnit=" + (speedometerUseMph ? "MPH" : "KMH") + Environment.NewLine +
                "ShowBoatHud=" + showBoatHud.ToString().ToLowerInvariant() + Environment.NewLine +
                "ShowFlightHud=" + showFlightHud.ToString().ToLowerInvariant() + Environment.NewLine +
                "NavigationUnits=" + (navigationUseMetric ? "METRIC" : "STANDARD") + Environment.NewLine +
                "AutomaticIndicators=" + automaticIndicatorsEnabled.ToString().ToLowerInvariant() + Environment.NewLine +
                "AutomaticEmergencyHazards=" + automaticEmergencyHazardsEnabled.ToString().ToLowerInvariant() + Environment.NewLine +
                "SpeedometerLeft=" + speedometerLeft.ToString(System.Globalization.CultureInfo.InvariantCulture) + Environment.NewLine +
                "SpeedometerTop=" + speedometerTop.ToString(System.Globalization.CultureInfo.InvariantCulture) + Environment.NewLine +
                "SpeedometerWidth=" + speedometerWidth.ToString(System.Globalization.CultureInfo.InvariantCulture) + Environment.NewLine +
                "SpeedometerHeight=" + speedometerHeight.ToString(System.Globalization.CultureInfo.InvariantCulture) + Environment.NewLine +
                "SpeedometerOpacity=" + speedometerOpacity + Environment.NewLine +
                "SpectrumSkinStrength=" + dynamicRadioBackgroundStrength.ToString(System.Globalization.CultureInfo.InvariantCulture) + Environment.NewLine +
                "GtaRadioStationIndex=" + gtaRadioIndex + Environment.NewLine +
                "Enabled=" + enabled.ToString().ToLowerInvariant() + Environment.NewLine +
                "SpeechDucking=" + speechDucking.ToString().ToLowerInvariant() + Environment.NewLine +
                "PhoneCallDucking=" + phoneCallDucking.ToString().ToLowerInvariant() + Environment.NewLine +
                "MenuDucking=" + menuDucking.ToString().ToLowerInvariant() + Environment.NewLine +
                favoritesData;
            File.WriteAllText(temp, data, new UTF8Encoding(false));
            if (File.Exists(path))
            {
                try { File.Replace(temp, path, null, true); }
                catch { File.Delete(path); File.Move(temp, path); }
            }
            else File.Move(temp, path);
            userSettingsDirty = false;
            userSettingsSaveAt = DateTime.MinValue;
            Log("USER SETTINGS SAVED | Source=" + NormalizePreferredVehicleSource(preferredVehicleSource) + " | YT=" + (lastYouTubePlaybackPlaying ? "Playing" : "Paused") + " | Spotify=" + (lastSpotifyPlaybackPlaying ? "Playing" : "Paused") + " | InternetStation=" + lastInternetStationName + " [" + lastInternetStationIndex + "] | Language=" + uiLanguage + " | Volume=" + volume + " | Duck=" + duckVolumePercent + " | Hold=" + duckReleaseMs + "ms | Fade=" + duckFadeOutMs + "ms | RadioMax=" + playerMaxVolume + " | YTMax=" + youtubeMaxVolume + " | SpotifyMax=" + spotifyMaxVolume + " | Favorites=" + favoriteStationNames.Count);
        }
        catch (Exception ex)
        {
            Log("USER SETTINGS SAVE FEHLER | " + ex.Message);
        }
    }

    private void SaveGeneralValue(string key, string value)
    {
        try
        {
            string path = Path.Combine(dir, "InternetRadio.ini");
            if (!File.Exists(path)) return;
            List<string> lines = new List<string>(File.ReadAllLines(path));
            int generalStart = -1;
            int generalEnd = lines.Count;
            for (int i = 0; i < lines.Count; i++)
            {
                string t = lines[i].Trim();
                if (t.Equals("[General]", StringComparison.OrdinalIgnoreCase))
                {
                    generalStart = i;
                    continue;
                }
                if (generalStart >= 0 && i > generalStart && t.StartsWith("[") && t.EndsWith("]"))
                {
                    generalEnd = i;
                    break;
                }
            }
            if (generalStart < 0)
            {
                lines.Insert(0, "[General]");
                lines.Insert(1, key + "=" + value);
                File.WriteAllLines(path, lines.ToArray());
                return;
            }
            for (int i = generalStart + 1; i < generalEnd; i++)
            {
                string t = lines[i].Trim();
                if (t.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] = key + "=" + value;
                    File.WriteAllLines(path, lines.ToArray());
                    return;
                }
            }
            lines.Insert(generalEnd, key + "=" + value);
            File.WriteAllLines(path, lines.ToArray());
        }
        catch (Exception ex) { Log("GENERAL SAVE FEHLER | " + key + " | " + ex.Message); }
    }

    private void SaveAudioAndLanguageSettings()
    {
        volumeStep = 1;
        SaveGeneralValue("Language", uiLanguage);
        SaveGeneralValue("AudioProfileVersion", "4");
        SaveGeneralValue("DuckVolumePercent", duckVolumePercent.ToString());
        SaveGeneralValue("VolumeStep", volumeStep.ToString());
        SaveGeneralValue("PlayerMaxVolume", playerMaxVolume.ToString());
        SaveGeneralValue("VolumeCurveExponent", volumeCurveExponent.ToString(System.Globalization.CultureInfo.InvariantCulture));
        SaveGeneralValue("YouTubeMaxVolume", youtubeMaxVolume.ToString());
        SaveGeneralValue("YouTubeVolumeCurveExponent", youtubeVolumeCurveExponent.ToString(System.Globalization.CultureInfo.InvariantCulture));
        SaveGeneralValue("SpotifyMaxVolume", spotifyMaxVolume.ToString());
        SaveGeneralValue("SpotifyVolumeCurveExponent", spotifyVolumeCurveExponent.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    private void SaveSkinToIni()
    {
        try
        {
            string path = Path.Combine(dir, "InternetRadio.ini");
            if (!File.Exists(path)) return;
            string[] lines = File.ReadAllLines(path);
            bool inGeneral = false;
            bool found = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string t = lines[i].Trim();
                if (t.StartsWith("[") && t.EndsWith("]"))
                {
                    if (inGeneral && !found)
                    {
                        List<string> list = new List<string>(lines);
                        list.Insert(i, "Skin=" + skinNames[skinIndex]);
                        File.WriteAllLines(path, list.ToArray());
                        return;
                    }
                    inGeneral = t.Equals("[General]", StringComparison.OrdinalIgnoreCase);
                    continue;
                }
                if (inGeneral && t.StartsWith("Skin=", StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] = "Skin=" + skinNames[skinIndex];
                    found = true;
                    File.WriteAllLines(path, lines);
                    return;
                }
            }
            if (!found)
            {
                List<string> list = new List<string>(lines);
                list.Add("Skin=" + skinNames[skinIndex]);
                File.WriteAllLines(path, list.ToArray());
            }
        }
        catch (Exception ex) { Log("SKIN SAVE FEHLER: " + ex.Message); }
    }

    private void SaveVolumeToIni()
    {
        try
        {
            string path = Path.Combine(dir, "InternetRadio.ini");
            if (!File.Exists(path)) return;
            string[] lines = File.ReadAllLines(path);
            bool inGeneral = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string t = lines[i].Trim();
                if (t.StartsWith("[") && t.EndsWith("]"))
                {
                    inGeneral = t.Equals("[General]", StringComparison.OrdinalIgnoreCase);
                    continue;
                }
                if (inGeneral && t.StartsWith("Volume=", StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] = "Volume=" + volume;
                    File.WriteAllLines(path, lines);
                    return;
                }
            }
        }
        catch (Exception ex) { Log("VOLUME SAVE FEHLER: " + ex.Message); }
    }

    private void DrawVolumeBar(float left, float top, float width, float height)
    {
        DrawRect(left + width / 2f, top + height / 2f, width, height, 45, 45, 45, 180);
        float fill = width * (volume / 100.0f);
        if (fill > 0.001f) DrawRect(left + fill / 2f, top + height / 2f, fill, height, duckActive ? 224 : 54, duckActive ? 168 : 137, duckActive ? 64 : 214, 225);
        DrawText((duckActive ? (duckReason + " ") : "") + volume + "%", left + width - 0.048f, top - 0.006f, 0.22f, 0, 240, 240, 240, 220);
    }

    private string T(string key)
    {
        string translated = null;
        if (uiLanguage == "FR") translated = TranslateFrench(key);
        else if (uiLanguage == "IT") translated = TranslateItalian(key);
        else if (uiLanguage == "PT") translated = TranslatePortuguese(key);
        else if (uiLanguage == "TR") translated = TranslateTurkish(key);
        if (!string.IsNullOrEmpty(translated)) return translated;

        // v7.38: compact right-rail labels shared by every supported language.
        if (key == "APP_VOLUME")
            return uiLanguage == "DE" ? "APP-LAUTSTÄRKE" :
                (uiLanguage == "ES" ? "VOLUMEN APP" :
                (uiLanguage == "FR" ? "VOLUME APP" :
                (uiLanguage == "IT" ? "VOLUME APP" :
                (uiLanguage == "PT" ? "VOLUME DO APP" :
                (uiLanguage == "TR" ? "UYGULAMA SESİ" : "APP VOLUME")))));
        if (key == "CONNECTION")
            return uiLanguage == "DE" ? "VERBINDUNG" :
                (uiLanguage == "ES" ? "CONEXIÓN" :
                (uiLanguage == "FR" ? "CONNEXION" :
                (uiLanguage == "IT" ? "CONNESSIONE" :
                (uiLanguage == "PT" ? "CONEXÃO" :
                (uiLanguage == "TR" ? "BAĞLANTI" : "CONNECTION")))));
        if (key == "AUDIO_REDUCTION")
            return uiLanguage == "DE" ? "AUDIOABSENKUNG" :
                (uiLanguage == "ES" ? "REDUCCIÓN DE AUDIO" :
                (uiLanguage == "FR" ? "RÉDUCTION AUDIO" :
                (uiLanguage == "IT" ? "RIDUZIONE AUDIO" :
                (uiLanguage == "PT" ? "REDUÇÃO DE ÁUDIO" :
                (uiLanguage == "TR" ? "SES AZALTMA" : "AUDIO REDUCTION")))));
        if (key == "BLOWOFF_TRIGGER")
            return uiLanguage == "DE" ? "AUSLÖSER" :
                (uiLanguage == "ES" ? "ACTIVACIÓN" :
                (uiLanguage == "FR" ? "DÉCLENCHEMENT" :
                (uiLanguage == "IT" ? "ATTIVAZIONE" :
                (uiLanguage == "PT" ? "ACIONAMENTO" :
                (uiLanguage == "TR" ? "TETİKLEME" : "TRIGGER")))));
        if (key == "BLOWOFF_SHIFT_LIFT")
            return uiLanguage == "DE" ? "HOCHSCHALTEN / GAS LUPFEN" :
                (uiLanguage == "ES" ? "SUBIR MARCHA / SOLTAR GAS" :
                (uiLanguage == "FR" ? "PASSAGE SUP. / LEVER DE PIED" :
                (uiLanguage == "IT" ? "CAMBIO SU / RILASCIO GAS" :
                (uiLanguage == "PT" ? "SUBIR MARCHA / ALIVIAR GÁS" :
                (uiLanguage == "TR" ? "VİTES BÜYÜT / GAZ BIRAK" : "UPSHIFT / THROTTLE LIFT")))));
        if (key == "BLOWOFF_NO_SPAM")
            return uiLanguage == "DE" ? "EIN IMPULS PRO BOOST-PHASE" :
                (uiLanguage == "ES" ? "UN IMPULSO POR FASE DE BOOST" :
                (uiLanguage == "FR" ? "UNE IMPULSION PAR PHASE DE BOOST" :
                (uiLanguage == "IT" ? "UN IMPULSO PER FASE BOOST" :
                (uiLanguage == "PT" ? "UM IMPULSO POR FASE DE BOOST" :
                (uiLanguage == "TR" ? "BOOST BAŞINA TEK DARBE" : "ONE PULSE PER BOOST PHASE")))));

        // EN is also the safe fallback for any future language code/key combination.
        bool en = uiLanguage == "EN" || (uiLanguage != "DE" && uiLanguage != "ES");
        bool es = uiLanguage == "ES";
        if (key == "GTA_RADIO") return "GTA RADIO";
        if (key == "GTA_RADIO_HINT")
        {
            if (uiLanguage == "DE") return "Num8/2 Sender wählen | Num5 aktivieren";
            if (uiLanguage == "ES") return "Num8/2 elegir emisora | Num5 activar";
            if (uiLanguage == "FR") return "Num8/2 choisir station | Num5 activer";
            if (uiLanguage == "IT") return "Num8/2 scegli stazione | Num5 attiva";
            if (uiLanguage == "PT") return "Num8/2 escolher estação | Num5 ativar";
            if (uiLanguage == "TR") return "Num8/2 istasyon seç | Num5 etkinleştir";
            return "Num8/2 choose station | Num5 activate";
        }
        if (key == "GTA_RADIO_NATIVE_NOTE")
        {
            if (uiLanguage == "DE") return "Originaler GTA-Sender · Audio direkt aus dem Spiel";
            if (uiLanguage == "ES") return "Emisora original de GTA · audio directo del juego";
            if (uiLanguage == "FR") return "Station GTA originale · audio directement du jeu";
            if (uiLanguage == "IT") return "Stazione GTA originale · audio diretto dal gioco";
            if (uiLanguage == "PT") return "Estação original do GTA · áudio direto do jogo";
            if (uiLanguage == "TR") return "Orijinal GTA istasyonu · ses doğrudan oyundan";
            return "Original GTA station · audio directly from the game";
        }
        if (key == "GTA_RADIO_GAME_VOLUME")
        {
            if (uiLanguage == "DE") return "GTA Einstellungen > Audio";
            if (uiLanguage == "ES") return "Ajustes de GTA > Audio";
            if (uiLanguage == "FR") return "Paramètres GTA > Audio";
            if (uiLanguage == "IT") return "Impostazioni GTA > Audio";
            if (uiLanguage == "PT") return "Configurações GTA > Áudio";
            if (uiLanguage == "TR") return "GTA Ayarları > Ses";
            return "GTA Settings > Audio";
        }
        if (key == "GTA_AUDIO_SHORT")
        {
            if (uiLanguage == "DE") return "GTA-AUDIO";
            if (uiLanguage == "ES") return "AUDIO GTA";
            if (uiLanguage == "FR") return "AUDIO GTA";
            if (uiLanguage == "IT") return "AUDIO GTA";
            if (uiLanguage == "PT") return "ÁUDIO GTA";
            if (uiLanguage == "TR") return "GTA SES";
            return "GTA AUDIO";
        }
        if (key == "GTA_RADIO_VOLUME_NOTICE")
        {
            if (uiLanguage == "DE") return "LAUTSTÄRKE: ausschließlich über GTA > Einstellungen > Audio";
            if (uiLanguage == "ES") return "VOLUMEN: solo en GTA > Ajustes > Audio";
            if (uiLanguage == "FR") return "VOLUME : uniquement via GTA > Paramètres > Audio";
            if (uiLanguage == "IT") return "VOLUME: solo da GTA > Impostazioni > Audio";
            if (uiLanguage == "PT") return "VOLUME: apenas em GTA > Configurações > Áudio";
            if (uiLanguage == "TR") return "SES: yalnızca GTA > Ayarlar > Ses üzerinden";
            return "VOLUME: controlled only in GTA > Settings > Audio";
        }
        if (key == "CAR_GROUP_LIGHTING") return uiLanguage == "DE" ? "BELEUCHTUNG" : (uiLanguage == "ES" ? "ILUMINACIÓN" : (uiLanguage == "FR" ? "ÉCLAIRAGE" : (uiLanguage == "IT" ? "ILLUMINAZIONE" : (uiLanguage == "PT" ? "ILUMINAÇÃO" : (uiLanguage == "TR" ? "AYDINLATMA" : "LIGHTING")))));
        if (key == "CAR_GROUP_AUDIO") return uiLanguage == "DE" ? "AUDIO" : (uiLanguage == "ES" ? "AUDIO" : (uiLanguage == "FR" ? "AUDIO" : (uiLanguage == "IT" ? "AUDIO" : (uiLanguage == "PT" ? "ÁUDIO" : (uiLanguage == "TR" ? "SES" : "AUDIO")))));
        if (key == "CAR_GROUP_SPEEDO") return uiLanguage == "DE" ? "TACHO" : (uiLanguage == "ES" ? "VELOCÍMETRO" : (uiLanguage == "FR" ? "COMPTEUR" : (uiLanguage == "IT" ? "TACHIMETRO" : (uiLanguage == "PT" ? "VELOCÍMETRO" : (uiLanguage == "TR" ? "HIZ GÖSTERGESİ" : "SPEEDOMETER")))));
        if (key == "CAR_GROUP_HUD") return uiLanguage == "DE" ? "HUD / SEE / FLUG" : (uiLanguage == "ES" ? "HUD / MAR / AIRE" : (uiLanguage == "FR" ? "HUD / MER / AIR" : (uiLanguage == "IT" ? "HUD / MARE / VOLO" : (uiLanguage == "PT" ? "HUD / MAR / AR" : (uiLanguage == "TR" ? "HUD / DENİZ / HAVA" : "HUD / SEA / AIR")))));
        if (key == "CAR_HUD_NOTE") return uiLanguage == "DE" ? "Hier stellst du Boot-/Flug-HUD und die passenden See-/Flug-Einheiten ein." : (uiLanguage == "ES" ? "Aquí ajustas el HUD de barco/vuelo y sus unidades." : (uiLanguage == "FR" ? "Réglez ici les HUD bateau/vol et leurs unités." : (uiLanguage == "IT" ? "Qui regoli HUD barca/volo e relative unità." : (uiLanguage == "PT" ? "Aqui ajusta HUD de barco/voo e respetivas unidades." : (uiLanguage == "TR" ? "Tekne/uçuş HUD'u ve ilgili birimler burada ayarlanır." : "Configure boat/flight HUDs and their units here.")))));
        if (key == "RESET_DEFAULTS") return uiLanguage == "DE" ? "STANDARDWERTE WIEDERHERSTELLEN" : (uiLanguage == "ES" ? "RESTABLECER PREDETERMINADOS" : (uiLanguage == "FR" ? "RÉTABLIR LES VALEURS PAR DÉFAUT" : (uiLanguage == "IT" ? "RIPRISTINA PREDEFINITI" : (uiLanguage == "PT" ? "REPOR PREDEFINIÇÕES" : (uiLanguage == "TR" ? "VARSAYILANLARI GERİ YÜKLE" : "RESTORE DEFAULTS")))));
        if (key == "NUM5_CONFIRM") return uiLanguage == "DE" ? "NUM5 · BESTÄTIGEN" : (uiLanguage == "ES" ? "NUM5 · CONFIRMAR" : (uiLanguage == "FR" ? "NUM5 · CONFIRMER" : (uiLanguage == "IT" ? "NUM5 · CONFERMA" : (uiLanguage == "PT" ? "NUM5 · CONFIRMAR" : (uiLanguage == "TR" ? "NUM5 · ONAYLA" : "NUM5 · CONFIRM")))));
        if (key == "PRESS_AGAIN_CONFIRM") return uiLanguage == "DE" ? "Num5 erneut drücken zum Bestätigen" : (uiLanguage == "ES" ? "Pulsa Num5 otra vez para confirmar" : (uiLanguage == "FR" ? "Appuyez encore sur Num5 pour confirmer" : (uiLanguage == "IT" ? "Premi di nuovo Num5 per confermare" : (uiLanguage == "PT" ? "Prima Num5 novamente para confirmar" : (uiLanguage == "TR" ? "Onaylamak için Num5'e tekrar bas" : "Press Num5 again to confirm")))));
        if (key == "DONE") return uiLanguage == "DE" ? "ERLEDIGT" : (uiLanguage == "ES" ? "HECHO" : (uiLanguage == "FR" ? "TERMINÉ" : (uiLanguage == "IT" ? "FATTO" : (uiLanguage == "PT" ? "CONCLUÍDO" : (uiLanguage == "TR" ? "TAMAM" : "DONE")))));
        if (key == "ALT_CONTROL_HINT") return uiLanguage == "DE" ? "Num8/2 wählen · Num5 bestätigen · Num4/6 wechseln" : (uiLanguage == "ES" ? "Num8/2 elegir · Num5 confirmar · Num4/6 cambiar" : (uiLanguage == "FR" ? "Num8/2 choisir · Num5 valider · Num4/6 changer" : (uiLanguage == "IT" ? "Num8/2 scegli · Num5 conferma · Num4/6 cambia" : (uiLanguage == "PT" ? "Num8/2 escolher · Num5 confirmar · Num4/6 mudar" : (uiLanguage == "TR" ? "Num8/2 seç · Num5 onayla · Num4/6 değiştir" : "Num8/2 choose · Num5 confirm · Num4/6 change")))));
        if (key == "HELP_DUCK_LEVEL") return uiLanguage == "DE" ? "Legt fest, wie leise Musik während GTA-Dialogen automatisch wird." : (uiLanguage == "ES" ? "Define cuánto baja la música durante diálogos de GTA." : (uiLanguage == "FR" ? "Définit à quel niveau la musique baisse pendant les dialogues GTA." : (uiLanguage == "IT" ? "Imposta quanto si abbassa la musica durante i dialoghi GTA." : (uiLanguage == "PT" ? "Define quanto a música baixa durante diálogos do GTA." : (uiLanguage == "TR" ? "GTA diyaloglarında müziğin ne kadar kısılacağını belirler." : "Sets how quiet music becomes during GTA dialogue.")))));
        if (key == "HELP_OUTPUT_LIMIT") return uiLanguage == "DE" ? "Begrenzt die maximale Lautstärke dieser Quelle; dein Hauptregler bleibt 0–100 %." : (uiLanguage == "ES" ? "Limita el volumen máximo de esta fuente; el control principal sigue en 0–100 %." : (uiLanguage == "FR" ? "Limite le volume maximal de cette source ; le réglage principal reste 0–100 %." : (uiLanguage == "IT" ? "Limita il volume massimo della sorgente; il controllo principale resta 0–100 %." : (uiLanguage == "PT" ? "Limita o volume máximo desta fonte; o controlo principal continua 0–100 %." : (uiLanguage == "TR" ? "Bu kaynağın azami sesini sınırlar; ana ses 0–100 % kalır." : "Limits this source's maximum loudness; the main volume still runs 0–100 %.")))));
        if (key == "HELP_VOLUME_STEP") return uiLanguage == "DE" ? "Ein kurzer Tastendruck ändert exakt 1 %. Gedrückthalten beschleunigt automatisch." : (uiLanguage == "ES" ? "Una pulsación cambia exactamente 1 %. Mantener acelera automáticamente." : (uiLanguage == "FR" ? "Un appui change exactement 1 %. Maintenir accélère automatiquement." : (uiLanguage == "IT" ? "Un tocco cambia esattamente 1 %. Tenere premuto accelera automaticamente." : (uiLanguage == "PT" ? "Um toque altera exatamente 1 %. Manter premido acelera automaticamente." : (uiLanguage == "TR" ? "Kısa basış tam %1 değiştirir. Basılı tutmak otomatik hızlanır." : "A tap changes exactly 1%. Holding the key accelerates automatically.")))));
        if (key == "HELP_DUCK_HOLD") return uiLanguage == "DE" ? "Wie lange die Musik nach einer Dialogzeile noch leise bleibt." : (uiLanguage == "ES" ? "Cuánto tiempo sigue baja la música tras una frase de diálogo." : (uiLanguage == "FR" ? "Durée pendant laquelle la musique reste basse après une réplique." : (uiLanguage == "IT" ? "Quanto resta bassa la musica dopo una battuta." : (uiLanguage == "PT" ? "Quanto tempo a música fica baixa após uma fala." : (uiLanguage == "TR" ? "Bir diyalog satırından sonra müziğin ne kadar süre kısık kalacağı." : "How long music stays quiet after a dialogue line.")))));
        if (key == "HELP_DUCK_FADE") return uiLanguage == "DE" ? "Wie weich die Musik nach einem Dialog wieder auf normale Lautstärke zurückkehrt." : (uiLanguage == "ES" ? "Qué tan suavemente vuelve la música al volumen normal tras el diálogo." : (uiLanguage == "FR" ? "Vitesse du retour progressif au volume normal après le dialogue." : (uiLanguage == "IT" ? "Quanto dolcemente torna il volume normale dopo il dialogo." : (uiLanguage == "PT" ? "Quão suavemente o volume volta ao normal após o diálogo." : (uiLanguage == "TR" ? "Diyalog sonrası sesin normale ne kadar yumuşak döneceği." : "How smoothly music returns to normal volume after dialogue.")))));
        if (key == "HELP_RESET") return uiLanguage == "DE" ? "Setzt Audio, HUD, Beleuchtung und Skin zurück. Sprache und Favoriten bleiben erhalten." : (uiLanguage == "ES" ? "Restablece audio, HUD, luces y tema. Idioma y favoritos se conservan." : (uiLanguage == "FR" ? "Réinitialise audio, HUD, éclairage et thème. Langue et favoris sont conservés." : (uiLanguage == "IT" ? "Ripristina audio, HUD, luci e tema. Lingua e preferiti restano invariati." : (uiLanguage == "PT" ? "Repõe áudio, HUD, iluminação e tema. Idioma e favoritos são mantidos." : (uiLanguage == "TR" ? "Ses, HUD, aydınlatma ve tema sıfırlanır. Dil ve favoriler korunur." : "Resets audio, HUD, lighting and skin. Language and favourites are kept.")))));
        if (key == "LOADED") return es ? "Cargado - idioma, audio y radio listos" : (en ? "Loaded - language, audio and radio ready" : "Geladen - Sprache, Audio und Radio bereit");
        if (key == "STATIONS") return es ? "EMISORAS" : (en ? "STATIONS" : "SENDER");
        if (key == "FAVORITES") return es ? "FAVORITOS" : (en ? "FAVORITES" : "FAVORITEN");
        if (key == "FAVORITE_SHORT") return es ? "FAV +/-" : (en ? "FAV +/-" : "FAV +/-");
        if (key == "FAVORITE_ADDED") return es ? "Añadido" : (en ? "Added" : "Hinzugefügt");
        if (key == "FAVORITE_REMOVED") return es ? "Eliminado" : (en ? "Removed" : "Entfernt");
        if (key == "FAVORITES_FULL") return es ? "Lista llena: máximo 6 emisoras" : (en ? "List full: maximum 6 stations" : "Liste voll: maximal 6 Sender");
        if (key == "NOT_FAVORITE") return es ? "La emisora no está en favoritos" : (en ? "Station is not a favorite" : "Sender ist kein Favorit");
        if (key == "NO_FAVORITES") return es ? "Sin favoritos - selecciona una emisora y pulsa Espacio" : (en ? "No favorites yet - select a station and press Space" : "Noch keine Favoriten - Sender wählen und Leertaste drücken");
        if (key == "WORLDWIDE_RADIO") return es ? "RADIO ONLINE INTERNACIONAL" : (en ? "ONLINE RADIO WORLDWIDE" : "INTERNETRADIO WELTWEIT");
        if (key == "HOME") return es ? "INICIO" : (en ? "HOME" : "START");
        if (key == "HOME_APPS") return es ? "APPS" : (en ? "APPS" : "APPS");
        if (key == "HOME_HINT") return es ? "Num8/2 elegir app | Num5 abrir" : (en ? "Num8/2 choose app | Num5 open" : "Num8/2 App wählen | Num5 öffnen");
        if (key == "OPEN") return es ? "ABRIR" : (en ? "OPEN" : "ÖFFNEN");
        if (key == "INFOTAINMENT") return es ? "SISTEMA MULTIMEDIA Y VEHÍCULO" : (en ? "MULTIMEDIA & VEHICLE SYSTEM" : "MULTIMEDIA- & FAHRZEUGSYSTEM");
        if (key == "PACKS") return es ? "CATEGORÍAS" : (en ? "CATEGORIES" : "KATEGORIEN");
        if (key == "INFO") return "INFO";
        if (key == "SKINS") return es ? "TEMAS" : (en ? "SKINS" : "DESIGNS");
        if (key == "SETTINGS") return es ? "AJUSTES" : (en ? "SETTINGS" : "EINSTELLUNGEN");
        if (key == "SETTINGS_NAV") return es ? "AJUSTES" : (en ? "SETTINGS" : "EINSTELL.");
        if (key == "MENU") return es ? "MENÚ" : (en ? "MENU" : "MENÜ");
        if (key == "EXIT") return es ? "SALIR" : (en ? "EXIT" : "SCHLIESSEN");
        if (key == "POWER") return es ? "ENC./APAG." : (en ? "POWER" : "EIN/AUS");
        if (key == "STOP") return es ? "DETENER" : (en ? "STOP" : "STOPP");
        if (key == "NAV") return es ? "NAVEGAR" : (en ? "NAV" : "NAV");
        if (key == "DIALOG") return es ? "DIÁLOGO" : (en ? "DIALOG" : "DIALOG");
        if (key == "PHONE") return es ? "LLAMADA" : (en ? "PHONE" : "ANRUF");
        if (key == "TAB") return es ? "Pestaña" : (en ? "Tab" : "Reiter");
        if (key == "TAB_SHORT") return es ? "PESTAÑA" : (en ? "TAB" : "REITER");
        if (key == "SETTING_SHORT") return es ? "AJUSTE" : (en ? "SETTING" : "EINST.");
        if (key == "CHANGE_TAB") return es ? "CAMBIAR PESTAÑA" : (en ? "CHANGE TAB" : "REITER WECHSELN");
        if (key == "SELECT") return es ? "SELECCIONAR" : (en ? "SELECT" : "AUSWÄHLEN");
        if (key == "START") return es ? "INICIAR" : (en ? "START" : "STARTEN");
        if (key == "APPLY") return es ? "APLICAR" : (en ? "APPLY" : "ANWENDEN");
        if (key == "VALUE") return es ? "VALOR" : (en ? "VALUE" : "WERT");
        if (key == "QUIETER") return es ? "BAJAR" : (en ? "LOWER" : "LEISER");
        if (key == "LOUDER") return es ? "SUBIR" : (en ? "RAISE" : "LAUTER");
        if (key == "SEEK") return es ? "CAMBIAR" : (en ? "SEEK" : "WECHSEL");
        if (key == "VOLUME") return es ? "VOLUMEN" : (en ? "VOLUME" : "LAUTSTÄRKE");
        if (key == "VOLUME_SHORT") return es ? "VOL" : (en ? "VOL" : "VOL");
        if (key == "DUCKING") return es ? "REDUCCIÓN EN DIÁLOGOS" : (en ? "DIALOGUE VOLUME REDUCTION" : "DIALOG-LAUTSTÄRKE");
        if (key == "STATUS") return es ? "ESTADO" : "STATUS";
        if (key == "PACK") return es ? "CATEGORÍA" : (en ? "CATEGORY" : "KATEGORIE");
        if (key == "VIBE") return es ? "AMBIENTE" : (en ? "VIBE" : "STIMMUNG");
        if (key == "GENRE") return es ? "GÉNERO" : (en ? "GENRE" : "GENRE");
        if (key == "STATION_COUNT") return es ? "emisoras" : (en ? "stations" : "Sender");
        if (key == "MANUAL") return es ? "MANUAL" : (en ? "MANUAL" : "MANUELL");
        if (key == "STATION") return es ? "EMISORA" : (en ? "STATION" : "SENDER");
        if (key == "PACK_GENRE") return es ? "CATEGORÍA / GÉNERO" : (en ? "CATEGORY / GENRE" : "KATEGORIE / GENRE");
        if (key == "NO_STATION") return es ? "Sin emisora" : (en ? "No station" : "Kein Sender");
        if (key == "NO_ACTIVE_STATION") return es ? "Ninguna emisora activa" : (en ? "No active station" : "Kein aktiver Sender");
        if (key == "NO_METADATA") return es ? "Sin metadatos de la canción" : (en ? "No song metadata available" : "Keine Song-Metadaten verfügbar");
        if (key == "ALL_STATIONS") return es ? "Todas las emisoras" : (en ? "All stations" : "Alle Sender");
        if (key == "START_STATION") return es ? "Iniciar emisora" : (en ? "Start station" : "Sender starten");
        if (key == "PACKS_GROUPS") return es ? "CATEGORÍAS DE RADIO" : (en ? "RADIO CATEGORIES" : "RADIO-KATEGORIEN");
        if (key == "PACK_HINT") return es ? "Num8/2 elegir categoría | Num5 mostrar emisoras" : (en ? "Num8/2 choose category | Num5 show stations" : "Num8/2 Kategorie wählen | Num5 Sender anzeigen");
        if (key == "SKINS_THEMES") return es ? "TEMAS" : (en ? "SKINS / THEMES" : "DESIGNS / THEMEN");
        if (key == "SKIN_HINT") return es ? "Num8/2 elegir tema | Num5 aplicar" : (en ? "Num8/2 choose skin | Num5 apply" : "Num8/2 Design wählen | Num5 anwenden");
        if (key == "ACTIVE") return es ? "ACTIVO" : (en ? "ACTIVE" : "AKTIV");
        if (key == "PREVIEW") return es ? "VISTA PREVIA" : (en ? "PREVIEW" : "VORSCHAU");
        if (key == "SKIN_HELP") return es ? "El tema cambia la interfaz; el audio no cambia." : (en ? "The skin changes the interface; audio is unchanged." : "Das Design ändert die Oberfläche; das Audio bleibt unverändert.");
        if (key == "INFO_HELP") return es ? "Num4/6 cambia pestañas | Num0 cierra el menú" : (en ? "Num4/6 changes tabs | Num0 closes the menu" : "Num4/6 wechselt Reiter | Num0 schließt das Menü");
        if (key == "CONNECTED") return es ? "CONECTADO" : (en ? "CONNECTED" : "VERBUNDEN");
        if (key == "NOT_CONNECTED") return es ? "NO CONECTADO" : (en ? "NOT CONNECTED" : "NICHT VERBUNDEN");
        if (key == "MEDIA_SESSION") return es ? "SESIÓN MULTIMEDIA" : (en ? "WINDOWS MEDIA SESSION" : "WINDOWS-MEDIENSESSION");
        if (key == "NOW_PLAYING") return es ? "REPRODUCIENDO" : (en ? "NOW PLAYING" : "JETZT LÄUFT");
        if (key == "YT_START_HINT") return es ? "Num5: activar en vehículo / abrir YT Music" : (en ? "Num5: Activate in vehicle / open YT Music" : "Num5: Im Fahrzeug aktivieren / YT Music öffnen");
        if (key == "YT_START_SONG") return es ? "Elige música allí. Al reproducir, GTA la detectará automáticamente." : (en ? "Choose music there. When playback starts, GTA detects it automatically." : "Wähle dort Musik aus. Sobald sie läuft, erkennt GTA sie automatisch.");
        if (key == "YT_CHOOSE_MUSIC") return es ? "Elige música allí - GTA detecta la reproducción" : (en ? "Choose music there - GTA detects playback" : "Dort Musik wählen - GTA erkennt die Wiedergabe");
        if (key == "YT_DETECTED") return es ? "Reproducción detectada - YT Music activo" : (en ? "Playback detected - YT Music active" : "Wiedergabe erkannt - YT Music aktiv");
        if (key == "YT_STARTING") return es ? "Iniciando reproducción" : (en ? "Starting playback" : "Wiedergabe wird gestartet");
        if (key == "YT_OPENED_SELECT") return es ? "YT Music abierto - pulsa Reproducir en YT Music" : (en ? "YT Music open - press Play in YT Music" : "YT Music geöffnet - Wiedergabe in YT Music drücken");
        if (key == "BRIDGE_UNAVAILABLE") return es ? "Integración no disponible" : (en ? "Bridge unavailable" : "Verbindung nicht verfügbar");
        if (key == "YT_SESSION_ONLINE") return es ? "SESIÓN YT ACTIVA" : (en ? "YT SESSION ONLINE" : "YT-SESSION AKTIV");
        if (key == "YT_SESSION_OFFLINE") return es ? "SESIÓN YT INACTIVA" : (en ? "YT SESSION OFFLINE" : "YT-SESSION INAKTIV");
        if (key == "PREV_SHORT") return es ? "ANTERIOR" : (en ? "PREV" : "ZURÜCK");
        if (key == "NEXT_SHORT") return es ? "SIGUIENTE" : (en ? "NEXT" : "WEITER");
        if (key == "PAUSE") return es ? "PAUSA" : "PAUSE";
        if (key == "PREVIOUS_TRACK") return es ? "Título anterior" : (en ? "Previous track" : "Vorheriger Titel");
        if (key == "NEXT_TRACK") return es ? "Siguiente título" : (en ? "Next track" : "Nächster Titel");
        if (key == "TRACK_NAV") return es ? "ANT./SIG." : (en ? "PREV/NEXT" : "ZURÜCK/WEITER");
        if (key == "PLAY_PAUSE") return es ? "REPRODUCIR / PAUSA" : (en ? "PLAY / PAUSE" : "ABSPIELEN / PAUSE");
        if (key == "YT_OPEN_FAILED") return es ? "No se pudo abrir YouTube Music - revisa el navegador predeterminado" : (en ? "Could not open YouTube Music - check your default browser" : "YouTube Music konnte nicht geöffnet werden - Standardbrowser prüfen");
        if (key == "OPEN_YT") return es ? "ABRIR / INICIAR" : (en ? "OPEN / START" : "ÖFFNEN / STARTEN");
        if (key == "OPEN_PLAY") return es ? "ABRIR / REPRODUCIR" : (en ? "OPEN / PLAY" : "ÖFFNEN / ABSPIELEN");
        if (key == "SOURCE") return es ? "Fuente" : (en ? "Source" : "Quelle");
        if (key == "YT_EXCLUSIVE") return es ? "YT Music sustituye la radio cuando está activo." : (en ? "YT Music replaces internet radio while active." : "YT Music ersetzt das Internetradio, solange es aktiv ist.");
        if (key == "SETTINGS_HINT") return es ? "Num8/2 seleccionar | Num5 aplicar | -/+ ajustar" : (en ? "Num8/2 select | Num5 apply | -/+ adjust" : "Num8/2 wählen | Num5 anwenden | -/+ einstellen");
        if (key == "CAR_SETTINGS") return es ? "AJUSTES DEL COCHE" : (en ? "CAR SETTINGS" : "FAHRZEUG-EINSTELLUNGEN");
        if (key == "CAR_SETTINGS_NAV") return es ? "COCHE" : (en ? "CAR" : "FAHRZEUG");
        if (key == "CAR_SETTINGS_HINT") return es ? "Num8/2 seleccionar | Num5 activar | -/+ ajustar" : (en ? "Num8/2 select | Num5 toggle | -/+ adjust" : "Num8/2 wählen | Num5 umschalten | -/+ einstellen");
        if (key == "CAR_SETTINGS_NOTE") return es ? "Beat Neon, luz interior, luz de matrícula e intermitentes automáticos se configuran aquí." : (en ? "Beat Neon, interior light, plate light and automatic indicators are configured here." : "Beat-Neon, Innenlicht, Kennzeichenlicht und Automatik-Blinker werden hier eingestellt.");
        if (key == "INTERIOR_LIGHT") return es ? "LUZ INTERIOR" : (en ? "INTERIOR LIGHT" : "INNENRAUMLICHT");
        if (key == "INTERIOR_STRENGTH") return es ? "INTENSIDAD LUZ INTERIOR" : (en ? "INTERIOR LIGHT STRENGTH" : "INNENLICHT-STÄRKE");
        if (key == "PLATE_LIGHT") return es ? "LUZ DE MATRÍCULA" : (en ? "PLATE LIGHT" : "KENNZEICHENLICHT");
        if (key == "AUTO_INDICATORS") return es ? "INTERMITENTES AUTOMÁTICOS" : (en ? "AUTOMATIC INDICATORS" : "AUTOMATIK-BLINKER");
        if (key == "TURBO_IMPACT") return uiLanguage == "DE" ? "TURBO BLOW-OFF" : (uiLanguage == "ES" ? "BLOW-OFF TURBO" : (uiLanguage == "FR" ? "BLOW-OFF TURBO" : (uiLanguage == "IT" ? "BLOW-OFF TURBO" : (uiLanguage == "PT" ? "BLOW-OFF TURBO" : (uiLanguage == "TR" ? "TURBO BLOW-OFF" : "TURBO BLOW-OFF")))));
        if (key == "CAR_AUDIO_NOTE") return uiLanguage == "DE" ? "Turbo Blow-off AN/AUS. AN verstärkt einmalig beim Hochschalten oder deutlichen Gaswegnehmen nach Boost. Kein Dauer-/Brems-Spam; funktioniert auch bei hohem Tempo und in der Luft." : (uiLanguage == "ES" ? "Blow-off turbo ON/OFF. ON refuerza una sola vez al subir marcha o soltar claramente el gas tras boost. Sin spam al frenar; funciona también a alta velocidad y en el aire." : (uiLanguage == "FR" ? "Blow-off turbo ON/OFF. ON renforce une seule fois au passage supérieur ou au lever net après boost. Pas de spam au freinage; fonctionne aussi à haute vitesse et en l’air." : (uiLanguage == "IT" ? "Blow-off turbo ON/OFF. ON rinforza una sola volta in cambiata o al rilascio netto dopo boost. Nessuno spam in frenata; funziona anche ad alta velocità e in aria." : (uiLanguage == "PT" ? "Blow-off turbo ON/OFF. ON reforça uma vez ao subir marcha ou aliviar claramente após boost. Sem spam ao travar; funciona também em alta velocidade e no ar." : (uiLanguage == "TR" ? "Turbo blow-off AÇIK/KAPALI. AÇIK, boost sonrası vites büyütmede veya gazı belirgin bırakmada tek kez güçlendirir. Fren spamı yok; yüksek hızda ve havada da çalışır." : "Turbo Blow-off ON/OFF. ON adds one stronger BOV accent on an upshift or a clear throttle lift after boost. No braking spam; it also works at high speed and in the air.")))));
        if (key == "SPEEDOMETER") return es ? "VELOCÍMETRO" : (en ? "SPEEDOMETER" : "TACHO");
        if (key == "ROAD_HUD") return es ? "HUD DE CARRETERA" : (en ? "ROAD HUD" : "STRASSEN-TACHO");
        if (key == "BOAT_HUD") return es ? "HUD NÁUTICO" : (en ? "BOAT HUD" : "BOOT-HUD");
        if (key == "FLIGHT_HUD") return es ? "HUD DE VUELO" : (en ? "FLIGHT HUD" : "FLUG-HUD");
        if (key == "SPEEDO_CAR_ICON") return es ? "LOGO DEL FABRICANTE" : (en ? "MANUFACTURER LOGO" : "HERSTELLERLOGO");
        if (key == "SPEEDO_RPM_BAR") return es ? "BARRA RPM" : (en ? "RPM BAR" : "DREHZAHLLEISTE");
        if (key == "SPEEDO_UNIT_MODE") return es ? "UNIDAD DE VELOCIDAD" : (en ? "SPEED UNIT" : "GESCHWINDIGKEITSEINHEIT");
        if (key == "NAV_UNIT_MODE") return es ? "UNIDADES MAR / AIRE" : (en ? "SEA / AIR UNITS" : "SEE-/FLUG-EINHEITEN");
        if (key == "STANDARD") return es ? "ESTÁNDAR" : (en ? "STANDARD" : "STANDARD");
        if (key == "METRIC") return es ? "MÉTRICO" : (en ? "METRIC" : "METRISCH");
        if (key == "SPEEDO_SIZE") return es ? "TAMAÑO DEL VELOCÍMETRO" : (en ? "SPEEDOMETER SIZE" : "TACHO-GRÖSSE");
        if (key == "SPEEDO_POS_X") return es ? "POSICIÓN X" : (en ? "POSITION X" : "POSITION X");
        if (key == "SPEEDO_POS_Y") return es ? "POSICIÓN Y" : (en ? "POSITION Y" : "POSITION Y");
        if (key == "SPEEDO_OPACITY") return es ? "OPACIDAD" : (en ? "OPACITY" : "DECKKRAFT");
        if (key == "SPEEDOMETER_NOTE") return es ? "HUD de carretera, barco y vuelo, unidades, logo, barra RPM, tamaño, posición y opacidad se ajustan aquí." : (en ? "Road, boat and flight HUDs, units, logo, RPM bar, size, position and opacity are configurable here." : "Straßen-, Boot- und Flug-HUD, Einheiten, Logo, Drehzahlleiste, Größe, Position und Deckkraft sind hier einstellbar.");
        if (key == "CAR_SETTINGS_SCROLL_NOTE") return T("ALT_CONTROL_HINT");
        if (key == "SPEEDO_DRIVE") return es ? "CONDUC." : (en ? "DRIVE" : "FAHRT");
        if (key == "SPEEDO_GEAR") return es ? "MARCHA" : (en ? "GEAR" : "GANG");
        if (key == "SPEEDO_RPM") return es ? "RPM" : (en ? "RPM" : "U/MIN");
        if (key == "SPEEDO_MARINE") return es ? "NÁUTICO" : (en ? "MARINE" : "MARINE");
        if (key == "SPEEDO_FLIGHT") return es ? "VUELO" : (en ? "FLIGHT" : "FLUG");
        if (key == "SPEEDO_HEADING") return es ? "RUMBO" : (en ? "HEADING" : "KURS");
        if (key == "SPEEDO_WATER_SPEED") return es ? "VELOCIDAD EN AGUA" : (en ? "WATER SPEED" : "WASSERFAHRT");
        if (key == "SPEEDO_GROUND_SPEED") return es ? "VELOCIDAD SOBRE SUELO" : (en ? "SPEED OVER GROUND" : "GESCHW. ÜBER GRUND");
        if (key == "SPEEDO_ALTITUDE") return es ? "ALTITUD" : (en ? "ALTITUDE" : "HÖHE");
        if (key == "SPEEDO_VERTICAL") return es ? "VEL. VERTICAL" : (en ? "VERTICAL SPEED" : "VERTIKALGESCHW.");
        if (key == "SPEEDO_UNIT") return "KM/H";
        if (key == "LANGUAGE") return es ? "IDIOMA" : (en ? "LANGUAGE" : "SPRACHE");
        if (key == "ARTWORK") return es ? "CARÁTULAS" : (en ? "MEDIA ARTWORK" : "MEDIEN-COVER");
        if (key == "ARTWORK_NOTE") return es ? "Desactivado por defecto; si se activa, las carátulas solo se guardan localmente durante la sesión y se eliminan al salir." : (en ? "Off by default; if enabled, artwork is cached locally for this session only and deleted on exit." : "Standardmäßig AUS; wenn aktiviert, werden Cover nur während dieser Sitzung lokal zwischengespeichert und beim Beenden gelöscht.");
        if (key == "LEGAL") return es ? "LEGAL / AVISO" : (en ? "LEGAL / DISCLAIMER" : "RECHTLICHES / HINWEIS");
        if (key == "LEGAL_NOTICE_1") return es ? "Mod no oficial e independiente; sin afiliación, patrocinio ni respaldo de terceros." : (en ? "Unofficial independent mod; no affiliation, sponsorship or endorsement by third parties." : "Inoffizielle, unabhängige Mod; keine Verbindung, Förderung oder Unterstützung durch Drittanbieter.");
        if (key == "LEGAL_NOTICE_2") return es ? "Marcas y medios pertenecen a sus titulares. Sin carátulas incluidas; arte opcional, local y temporal." : (en ? "Marks and media remain with their owners. No covers bundled; artwork is optional, local and temporary." : "Marken und Medien bleiben bei ihren Inhabern. Keine Cover im Paket; Artwork optional, lokal und temporär.");
        if (key == "LEGAL_NOTICE_3") return es ? "Solo para un jugador; las funciones se desactivan si se detecta una sesión de GTA Online." : (en ? "Single-player only; features are disabled if a GTA Online session is detected." : "Nur Einzelspieler; bei erkannter GTA-Online-Sitzung werden die Funktionen deaktiviert.");
        if (key == "DUCK_LEVEL") return es ? "VOLUMEN DURANTE DIÁLOGO" : (en ? "VOLUME DURING DIALOGUE" : "LAUTSTÄRKE BEI DIALOG");
        if (key == "RADIO_OUTPUT") return es ? "LÍMITE DE VOLUMEN RADIO" : (en ? "RADIO VOLUME LIMIT" : "RADIO-LAUTSTÄRKE-LIMIT");
        if (key == "YT_OUTPUT") return es ? "LÍMITE DE VOLUMEN YT" : (en ? "YT VOLUME LIMIT" : "YT-LAUTSTÄRKE-LIMIT");
        if (key == "VOLUME_STEP") return es ? "PASO DE VOLUMEN" : (en ? "VOLUME STEP" : "LAUTSTÄRKE-SCHRITT");
        if (key == "BEAT_NEON") return es ? "NEÓN REACTIVO" : (en ? "BEAT NEON" : "BEAT-NEON");
        if (key == "BASS_ANALYZER") return es ? "ANALIZADOR DE BAJOS" : (en ? "BASS ANALYZER" : "BASS-ANALYZER");
        if (key == "BEAT_SENSITIVITY") return es ? "SENSIBILIDAD BEAT" : (en ? "BEAT SENSITIVITY" : "BEAT-EMPFINDLICHKEIT");
        if (key == "VOICE_FILTER") return es ? "FILTRO DE VOZ" : (en ? "VOICE FILTER" : "STIMMFILTER");
        if (key == "PULSE_STRENGTH") return es ? "FUERZA DE PULSO" : (en ? "PULSE STRENGTH" : "PULS-STÄRKE");
        if (key == "PULSE_SPEED") return es ? "VELOCIDAD DE PULSO" : (en ? "PULSE SPEED" : "PULS-GESCHWINDIGKEIT");
        if (key == "NEON_TUNING_NOTE") return es ? "Filtro de transitorios; no es un filtro FFT de frecuencia real." : (en ? "Transient filter; not a true frequency FFT filter." : "Transientenfilter; kein echter FFT-Frequenzfilter.");
        if (key == "ON") return es ? "ACTIVADO" : (en ? "ON" : "AN");
        if (key == "OFF") return es ? "DESACTIVADO" : (en ? "OFF" : "AUS");
        if (key == "RADIO_OUTPUT_SHORT") return es ? "RADIO MÁX." : (en ? "RADIO MAX" : "RADIO MAX");
        if (key == "YT_OUTPUT_SHORT") return es ? "YT MÁX." : (en ? "YT MAX" : "YT MAX");
        if (key == "AUDIO") return "AUDIO";
        if (key == "DUCK_HOLD") return es ? "RETENCIÓN DIÁLOGO" : (en ? "DIALOGUE HOLD" : "DIALOG-HALTEZEIT");
        if (key == "DUCK_FADE") return es ? "RETORNO DE ATENUACIÓN" : (en ? "DUCKING FADE" : "RÜCKBLENDUNG");
        if (key == "SETTINGS_NOTE") return es ? "La atenuación mantiene el volumen bajo entre frases y vuelve suavemente." : (en ? "Ducking stays low between dialogue lines and fades back smoothly." : "Die Absenkung hält die Lautstärke zwischen Dialogzeilen niedrig und blendet weich zurück.");
        if (key == "HOLD_VOLUME_NOTE") return es ? "Fuera de Ajustes: mantén Num-/Num+ para cambiar el volumen de forma continua." : (en ? "Outside Settings: hold Num-/Num+ for continuous volume changes." : "Außerhalb der Einstellungen: Num-/Num+ für kontinuierliche Lautstärke halten.");
        if (key == "HOLD_PLUS_MINUS") return es ? "MANTENER +/-" : (en ? "HOLD +/-" : "+/- HALTEN");
        if (key == "USE_PLUS_MINUS") return es ? "Usa Num-/Num+ para cambiar el valor" : (en ? "Use Num-/Num+ to change the value" : "Mit Num-/Num+ den Wert ändern");
        if (key == "MENU_HINT") return es ? "Num4/6 pestaña | Num8/2 mover | Num5 OK | Num0 cerrar" : (en ? "Num4/6 tab | Num8/2 move | Num5 OK | Num0 close" : "Num4/6 Reiter | Num8/2 bewegen | Num5 OK | Num0 schließen");
        if (key == "RADIO_ON") return es ? "InternetRadio ENCENDIDA" : (en ? "InternetRadio ON" : "InternetRadio EIN");
        if (key == "RADIO_OFF") return es ? "InternetRadio APAGADA" : (en ? "InternetRadio OFF" : "InternetRadio AUS");
        if (key == "NOW_ACTIVE") return es ? "Ahora activo" : (en ? "Now active" : "Jetzt aktiv");
        if (key == "CHECK_INI") return es ? "Comprueba InternetRadio.ini" : (en ? "Check InternetRadio.ini" : "Prüfe die InternetRadio.ini");
        if (key == "VEHICLE_ONLY") return es ? "Solo en vehículo" : (en ? "Vehicle only" : "Nur im Fahrzeug");
        if (key == "VEHICLE_ONLY_NOTE") return es ? "InternetRadio está limitado a vehículos" : (en ? "InternetRadio is limited to vehicles" : "InternetRadio ist auf Fahrzeuge begrenzt");
        if (key == "NO_CYCLE_RADIO") return es ? "Sin radio en bicicleta" : (en ? "No radio on bicycles" : "Kein Radio auf Fahrrädern");
        if (key == "NO_CYCLE_RADIO_NOTE") return es ? "BMX y bicicletas no admiten InternetRadio" : (en ? "BMX and bicycles do not support InternetRadio" : "BMX und Fahrräder unterstützen kein InternetRadio");
        if (key == "STATE_PLAYING") return es ? "Reproduciendo" : (en ? "Playing" : "Spielt");
        if (key == "STATE_BUFFERING") return es ? "Cargando" : (en ? "Buffering" : "Puffert");
        if (key == "STATE_CONNECTING") return es ? "Conectando" : (en ? "Connecting" : "Verbindet");
        if (key == "STATE_WAITING") return es ? "Esperando" : (en ? "Waiting" : "Wartet");
        if (key == "STATE_PAUSED") return es ? "Pausado" : (en ? "Paused" : "Pausiert");
        if (key == "STATE_STOPPED") return es ? "Detenido" : (en ? "Stopped" : "Gestoppt");
        if (key == "STATE_ENDED") return es ? "Finalizado" : (en ? "Ended" : "Beendet");
        if (key == "STATE_OFF") return es ? "Apagado" : (en ? "Off" : "Aus");
        if (key == "ON_AIR") return es ? "EN DIRECTO" : (en ? "ON AIR" : "AUF SENDUNG");
        if (key == "SPACE_KEY") return es ? "ESPACIO" : (en ? "SPACE" : "LEERTASTE");
        if (key == "MEDIA_SHORT") return es ? "MULTIMEDIA" : (en ? "MEDIA" : "MEDIEN");
        if (key == "YT_OUT_SHORT") return es ? "SALIDA YT" : (en ? "YT OUT" : "YT AUSG.");
        if (key == "SPOTIFY_START_HINT") return es ? "Num5: activar en vehículo / abrir Spotify" : (en ? "Num5: Activate in vehicle / open Spotify" : "Num5: Im Fahrzeug aktivieren / Spotify öffnen");
        if (key == "SPOTIFY_START_SONG") return es ? "Elige música en Spotify. Al reproducir, GTA la detecta automáticamente." : (en ? "Choose music in Spotify. When playback starts, GTA detects it automatically." : "Wähle Musik in Spotify. Sobald sie läuft, erkennt GTA sie automatisch.");
        if (key == "SPOTIFY_DETECTED") return es ? "Reproducción detectada - Spotify activo" : (en ? "Playback detected - Spotify active" : "Wiedergabe erkannt - Spotify aktiv");
        if (key == "SPOTIFY_OPENED_SELECT") return es ? "Spotify abierto - pulsa Reproducir en Spotify" : (en ? "Spotify open - press Play in Spotify" : "Spotify geöffnet - Wiedergabe in Spotify drücken");
        if (key == "SPOTIFY_OPEN_FAILED") return es ? "No se pudo abrir Spotify - revisa la instalación o el navegador" : (en ? "Could not open Spotify - check the installation or browser" : "Spotify konnte nicht geöffnet werden - Installation oder Browser prüfen");
        if (key == "SPOTIFY_SESSION_ONLINE") return es ? "SESIÓN SPOTIFY ACTIVA" : (en ? "SPOTIFY SESSION ONLINE" : "SPOTIFY-SESSION AKTIV");
        if (key == "SPOTIFY_SESSION_OFFLINE") return es ? "SESIÓN SPOTIFY INACTIVA" : (en ? "SPOTIFY SESSION OFFLINE" : "SPOTIFY-SESSION INAKTIV");
        if (key == "SPOTIFY_EXCLUSIVE") return es ? "Spotify sustituye la radio cuando está activo." : (en ? "Spotify replaces internet radio while active." : "Spotify ersetzt das Internetradio, solange es aktiv ist.");
        if (key == "SPOTIFY_OUTPUT") return es ? "LÍMITE DE VOLUMEN SPOTIFY" : (en ? "SPOTIFY VOLUME LIMIT" : "SPOTIFY-LAUTSTÄRKE-LIMIT");
        if (key == "SPOTIFY_OUTPUT_SHORT") return es ? "SPOTIFY MÁX." : (en ? "SPOTIFY MAX" : "SPOTIFY MAX");
        if (key == "SPOTIFY_OUT_SHORT") return es ? "SALIDA SPOTIFY" : (en ? "SPOTIFY OUT" : "SPOTIFY AUSG.");
        if (key == "OPEN_SPOTIFY") return es ? "ABRIR / INICIAR" : (en ? "OPEN / START" : "ÖFFNEN / STARTEN");
        if (key == "STATION_UNAVAILABLE") return es ? "Emisora no disponible - saltando" : (en ? "Station unavailable - skipping" : "Sender nicht verfügbar - überspringe");
        return key;
    }

    private void DrawRect(float x, float y, float width, float height, int r, int g, int b, int a)
    {
        int effectiveAlpha = Math.Max(0, Math.Min(255, (int)Math.Round(a * drawAlphaMultiplier)));
        Function.Call(Hash.DRAW_RECT, x, y, width, height, r, g, b, effectiveAlpha);
    }

    private void DrawTextCentered(string text, float centerX, float y, float scale, int font, int r, int g, int b, int a)
    {
        int effectiveAlpha = Math.Max(0, Math.Min(255, (int)Math.Round(a * drawAlphaMultiplier)));
        Function.Call(Hash.SET_TEXT_FONT, font);
        Function.Call(Hash.SET_TEXT_SCALE, 0.0f, scale);
        Function.Call(Hash.SET_TEXT_COLOUR, r, g, b, effectiveAlpha);
        Function.Call(Hash.SET_TEXT_CENTRE, true);
        Function.Call(Hash.SET_TEXT_WRAP, centerX - 0.20f, centerX + 0.20f);
        Function.Call(Hash.SET_TEXT_OUTLINE);
        Function.Call(Hash.BEGIN_TEXT_COMMAND_DISPLAY_TEXT, "STRING");
        Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, text ?? "");
        Function.Call(Hash.END_TEXT_COMMAND_DISPLAY_TEXT, centerX, y);
        Function.Call(Hash.SET_TEXT_CENTRE, false);
    }

    private void DrawText(string text, float x, float y, float scale, int font, int r, int g, int b, int a)
    {
        int effectiveAlpha = Math.Max(0, Math.Min(255, (int)Math.Round(a * drawAlphaMultiplier)));
        Function.Call(Hash.SET_TEXT_FONT, font);
        Function.Call(Hash.SET_TEXT_SCALE, 0.0f, scale);
        Function.Call(Hash.SET_TEXT_COLOUR, r, g, b, effectiveAlpha);
        Function.Call(Hash.SET_TEXT_WRAP, 0.0f, 1.0f);
        Function.Call(Hash.SET_TEXT_OUTLINE);
        Function.Call(Hash.BEGIN_TEXT_COMMAND_DISPLAY_TEXT, "STRING");
        Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, text ?? "");
        Function.Call(Hash.END_TEXT_COMMAND_DISPLAY_TEXT, x, y);
    }

    private void ForceCloseSpotifyHostFallback()
    {
        try
        {
            foreach (Process proc in Process.GetProcessesByName("Spotify"))
            {
                try
                {
                    try { if (!proc.HasExited && proc.MainWindowHandle != IntPtr.Zero) proc.CloseMainWindow(); } catch { }
                    try { if (!proc.WaitForExit(700)) proc.Kill(); } catch { }
                }
                finally { try { proc.Dispose(); } catch { } }
            }
            Log("SPOTIFY HOST FALLBACK CLOSE");
        }
        catch (Exception ex)
        {
            Log("SPOTIFY HOST FALLBACK ERROR | " + ex.Message);
        }
    }

    private void ForceStopHelperProcess(ref Process procRef, string tag)
    {
        Process p = procRef;
        if (p == null) return;
        try
        {
            bool running = false;
            try { running = !p.HasExited; } catch { running = false; }
            if (running)
            {
                try { if (!p.WaitForExit(1400)) p.Kill(); } catch { }
            }
        }
        catch (Exception ex)
        {
            Log(tag + " HELPER FORCE STOP ERROR | " + ex.Message);
        }
        finally
        {
            try { p.Close(); } catch { }
            try { p.Dispose(); } catch { }
            procRef = null;
        }
    }

    private void OnAbort(object sender, EventArgs e)
    {
        // TEST17: GTA exit/script shutdown owns the external music sessions.
        // Pause + close the managed YT Music tab/PWA and Spotify app/web tab,
        // then stop the helper bridges.
        RestoreGameTimeScale();
        try { if (gtaRadioModeActive) Game.RadioStation = RadioStation.RadioOff; } catch { }
        try { RestoreAudioReactiveNeon(true); } catch { }
        try { ClearAudioReactiveInteriorLight(); } catch { }
        try { ClearVehiclePlateLight(); } catch { }
        try { ResetTurboImpactState(true); } catch { }
        try { ResetAutomaticVehicleIndicators(true); } catch { }
        try { ResetManualVehicleHighBeam(true); } catch { }
        Instance = null;
        try { SaveVolumeToIni(); } catch { }
        try { SaveAudioAndLanguageSettings(); } catch { }
        try { CapturePersistentExternalPlaybackState(); } catch { }
        try { SaveUserSettings(); } catch { }
        try { if (bassAnalyzer != null) bassAnalyzer.Close(); } catch { }
        try { if (audio != null) audio.Close(); } catch { }

        try
        {
            if (youtube != null)
            {
                try { youtube.Pause(); } catch { }
                try { youtube.RestoreVolume(); } catch { }
                try { youtube.CloseManagedApp(); } catch { }
                // Give the PowerShell helper a moment to process CLOSE_APP before QUIT.
                try { Thread.Sleep(180); } catch { }
                try { youtube.SetManagedActive(false); } catch { }
                try { youtube.Close(); } catch { }
            }
        }
        catch { }

        try
        {
            if (spotify != null)
            {
                try { spotify.Pause(); } catch { }
                try { spotify.RestoreVolume(); } catch { }
                try { spotify.CloseManagedApp(); } catch { }
                try { Thread.Sleep(260); } catch { }
                try { spotify.SetManagedActive(false); } catch { }
                try { spotify.Close(); } catch { }
                try { ForceCloseSpotifyHostFallback(); } catch { }
            }
        }
        catch { }

        try { ReleaseMediaArtworkSprites(); } catch { }
        try { CleanupMediaArtworkCache("ABORT"); } catch { }
        try { InternetRadioBrowserVolume.Restore(); } catch { }
        Log("ABORT | NEON RESTORED | YT CLOSED | SPOTIFY CLOSED | ARTWORK CACHE CLEANED");
    }

    private static readonly object boundedLogLock = new object();
    private const long MaxLogBytes = 262144L;

    private static void AppendBoundedLog(string path, string line)
    {
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            lock (boundedLogLock)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        FileInfo fi = new FileInfo(path);
                        if (fi.Length >= MaxLogBytes)
                        {
                            string marker =
                                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") +
                                "  [LOG RESET: 256 KiB limit reached]" +
                                Environment.NewLine;

                            File.WriteAllText(path, marker, Encoding.UTF8);
                        }
                    }
                }
                catch { }

                File.AppendAllText(path, line, Encoding.UTF8);
            }
        }
        catch { }
    }

    private void Log(string text)
    {
        AppendBoundedLog(
            logPath,
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") +
            "  " + text + Environment.NewLine);
    }

    private static string GetDir()
    {
        string b = AppDomain.CurrentDomain.BaseDirectory;
        string s = Path.Combine(b, "scripts");
        if (Directory.Exists(s)) return Path.Combine(s, "InternetRadio");
        return Path.Combine(b, "InternetRadio");
    }


    // TEST42: Safe bridge to an OUT-OF-PROCESS bass analyzer.
    // No WASAPI capture COM interfaces live inside the SHVDN script.
    private class BassAnalyzerBridge
    {
        private readonly string log;
        private readonly string dir;
        private readonly string helperPath;
        private readonly string statusPath;
        private Process helperProcess;
        private DateTime lastStartAttempt = DateTime.MinValue;
        private DateTime lastStatusRead = DateTime.MinValue;
        private DateTime lastValidStatus = DateTime.MinValue;
        private bool available;
        private float lowBass;
        private float kickBass;
        private float mids;

        public BassAnalyzerBridge(string logPath)
        {
            log = logPath;
            dir = Path.GetDirectoryName(logPath);
            helperPath = Path.Combine(dir, "BassAnalyzerBridge.ps1");
            statusPath = Path.Combine(dir, "bass_status.txt");
            try { if (File.Exists(statusPath)) File.Delete(statusPath); } catch { }
        }

        public bool Available
        {
            get
            {
                RefreshStatus();
                return available &&
                    (DateTime.Now - lastValidStatus).TotalMilliseconds < 5000.0;
            }
        }

        public bool TryGetLevels(
            out float low, out float kick, out float mid)
        {
            low = 0.0f;
            kick = 0.0f;
            mid = 0.0f;

            EnsureRunning();
            RefreshStatus();

            bool ok = available &&
                (DateTime.Now - lastValidStatus).TotalMilliseconds < 5000.0;

            if (!ok) return false;

            low = lowBass;
            kick = kickBass;
            mid = mids;
            return true;
        }

        public void Close()
        {
            try
            {
                if (helperProcess != null && !helperProcess.HasExited)
                    helperProcess.Kill();
            }
            catch { }

            try
            {
                if (helperProcess != null) helperProcess.Close();
            }
            catch { }

            helperProcess = null;
            available = false;
            lowBass = 0.0f;
            kickBass = 0.0f;
            mids = 0.0f;
            lastValidStatus = DateTime.MinValue;
            lastStatusRead = DateTime.MinValue;
            // TEST57: allow an immediate lazy restart after a quick OFF -> ON.
            lastStartAttempt = DateTime.MinValue;

            try
            {
                if (File.Exists(statusPath)) File.Delete(statusPath);
            }
            catch { }
        }

        private void EnsureRunning()
        {
            try
            {
                if (helperProcess != null && !helperProcess.HasExited)
                    return;
            }
            catch { }

            if ((DateTime.Now - lastStartAttempt).TotalSeconds < 8.0)
                return;

            lastStartAttempt = DateTime.Now;
            StartHelper();
        }

        private void StartHelper()
        {
            try
            {
                if (!File.Exists(helperPath))
                {
                    available = false;
                    Write("BASS ANALYZER HELPER MISSING | " + helperPath);
                    return;
                }

                try
                {
                    if (File.Exists(statusPath)) File.Delete(statusPath);
                }
                catch { }

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "powershell.exe";
                psi.Arguments =
                    "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"" +
                    helperPath + "\" -BaseDir \"" + dir +
                    "\" -ParentPid " + Process.GetCurrentProcess().Id;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;

                Process p = Process.Start(psi);
                if (p == null)
                    throw new Exception("powershell.exe start returned null");

                helperProcess = p;
                Write("BASS ANALYZER HELPER START | PID=" + p.Id);
            }
            catch (Exception ex)
            {
                available = false;
                Write("BASS ANALYZER HELPER START ERROR | " + ex.Message);
            }
        }

        private void RefreshStatus()
        {
            // TEST56: the helper publishes about every 33 ms. TEST55 only read the
            // file every 80 ms, so short kick peaks could be skipped entirely.
            if ((DateTime.Now - lastStatusRead).TotalMilliseconds < 32.0)
                return;

            lastStatusRead = DateTime.Now;

            try
            {
                if (!File.Exists(statusPath))
                {
                    // TEST45: helper replaces bass_status.txt atomically.
                    // During that tiny replace window the file can briefly be absent.
                    // Keep the last good state; the freshness timeout decides whether
                    // the analyzer is really gone.
                    return;
                }

                bool statusAvailable = false;
                float newLow = lowBass;
                float newKick = kickBass;
                float newMid = mids;

                foreach (string line in File.ReadAllLines(statusPath))
                {
                    int p = line.IndexOf('=');
                    if (p <= 0) continue;

                    string key = line.Substring(0, p).Trim();
                    string value = line.Substring(p + 1).Trim();

                    if (key.Equals("Available",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        bool b;
                        if (bool.TryParse(value, out b))
                            statusAvailable = b;
                    }
                    else if (key.Equals("LowBass",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        float f;
                        if (float.TryParse(
                            value,
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out f))
                            newLow = Math.Max(0.0f, f);
                    }
                    else if (key.Equals("KickBass",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        float f;
                        if (float.TryParse(
                            value,
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out f))
                            newKick = Math.Max(0.0f, f);
                    }
                    else if (key.Equals("Mids",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        float f;
                        if (float.TryParse(
                            value,
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out f))
                            newMid = Math.Max(0.0f, f);
                    }
                }

                if (statusAvailable)
                {
                    available = true;
                    lowBass = newLow;
                    kickBass = newKick;
                    mids = newMid;
                    lastValidStatus = DateTime.Now;
                }
                else if ((DateTime.Now - lastValidStatus).TotalMilliseconds >= 5000.0)
                {
                    // Only declare fallback after a real multi-second outage.
                    available = false;
                }
            }
            catch
            {
                // Keep last good values if helper updates the file at the same moment.
            }
        }

        private void Write(string s)
        {
            try
            {
                AppendBoundedLog(
                    log,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") +
                    "  " + s + Environment.NewLine);
            }
            catch { }
        }
    }

    private class YouTubeMusicBridge
    {
        private readonly string log;
        private readonly string dir;
        private readonly string helperPath;
        private readonly string commandDir;
        private readonly string statusPath;
        private readonly string coverWorkerPath;
        private readonly bool autoLaunch;
        private readonly bool autoPlay;
        private Process helperProcess;
        private int sequence;
        private DateTime lastStatusRead = DateTime.MinValue;
        private bool available;
        private string state = "Nicht verbunden";
        private string title = "";
        private string artist = "";
        private string album = "";
        private string source = "";
        private string coverPath = "";
        private bool artworkEnabled = true;

        public bool Available { get { RefreshStatus(); return available; } }
        public string StateText { get { RefreshStatus(); return state; } }
        public string Title { get { RefreshStatus(); return title; } }
        public string Artist { get { RefreshStatus(); return artist; } }
        public string Album { get { RefreshStatus(); return album; } }
        public string Source { get { RefreshStatus(); return source; } }
        public string CoverPath { get { RefreshStatus(); return coverPath; } }

        public YouTubeMusicBridge(string logPath, bool autoLaunchYouTubeMusic, bool autoPlayYouTubeMusic, bool artworkEnabledInitial)
        {
            log = logPath;
            dir = Path.GetDirectoryName(logPath);
            helperPath = Path.Combine(dir, "YouTubeMusicBridge.ps1");
            commandDir = Path.Combine(dir, "ytmusic_commands");
            statusPath = Path.Combine(dir, "ytmusic_status.txt");
            coverWorkerPath = Path.Combine(dir, "YouTubeMusicCoverWorker.ps1");
            autoLaunch = autoLaunchYouTubeMusic;
            autoPlay = autoPlayYouTubeMusic;
            artworkEnabled = artworkEnabledInitial;

            // TEST49: no helper process during GTA/mod startup.
            // YouTube bridge starts only after an actual YouTube Music command.
            try
            {
                Directory.CreateDirectory(commandDir);
                foreach (string old in Directory.GetFiles(commandDir, "*.cmd"))
                    try { File.Delete(old); } catch { }
                try { if (File.Exists(statusPath)) File.Delete(statusPath); } catch { }
            }
            catch { }

            available = false;
            state = "Not connected";
        }

        public void Toggle() { SendCommand("TOGGLE"); }
        public void Play() { SendCommand("PLAY"); }
        public void Pause() { SendCommand("PAUSE"); }
        public void Next() { SendCommand("NEXT"); }
        public void Previous() { SendCommand("PREVIOUS"); }
        public void OpenHome() { SendCommand("OPEN_HOME", -1, "https://music.youtube.com/"); }
        public void BeginDetection() { SendCommand("DETECT"); }
        public void SetVolume(int volume) { SendCommand("VOLUME", volume); }
        public void RestoreVolume() { SendCommand("RESTORE_VOLUME"); }
        public void SetManagedActive(bool active) { SendCommand(active ? "MODE_ON" : "MODE_OFF"); }
        public void StopManagedPlayback() { SendCommand("PAUSE"); }
        public void CloseManagedApp() { SendCommand("CLOSE_APP"); }
        public void SetArtworkEnabled(bool enabledValue)
        {
            artworkEnabled = enabledValue;
            bool running = false;
            try { running = helperProcess != null && !helperProcess.HasExited; } catch { running = false; }
            if (running) SendCommand(enabledValue ? "ARTWORK_ON" : "ARTWORK_OFF");
            if (!enabledValue) coverPath = "";
        }
        public void Close()
        {
            // TEST69: wait for the YouTube bridge to process QUIT so its session-only
            // artwork cache is cleaned before the script releases the helper handle.
            bool running = false;
            try { running = helperProcess != null && !helperProcess.HasExited; } catch { running = false; }

            if (running)
            {
                try { SendCommand("RESTORE_VOLUME"); SendCommand("QUIT"); } catch { }
                try
                {
                    if (helperProcess != null && !helperProcess.WaitForExit(1600))
                        helperProcess.Kill();
                }
                catch { }
            }

            try { if (helperProcess != null) helperProcess.Close(); } catch { }
            try { if (helperProcess != null) helperProcess.Dispose(); } catch { }
            helperProcess = null;
            available = false;
            state = "Not connected";
        }

        private void StartHelper()
        {
            try
            {
                if (!File.Exists(helperPath)) throw new FileNotFoundException("YouTubeMusicBridge.ps1 fehlt", helperPath);
                Directory.CreateDirectory(commandDir);
                foreach (string old in Directory.GetFiles(commandDir, "*.cmd")) try { File.Delete(old); } catch { }
                try { if (File.Exists(statusPath)) File.Delete(statusPath); } catch { }
                string startupLog = Path.Combine(dir, "ytmusic_bridge_startup.log");
                try { AppendBoundedLog(startupLog, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  C# BRIDGE LAUNCH | " + helperPath + Environment.NewLine); } catch { }
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "powershell.exe";
                psi.Arguments = "-NoProfile -Sta -ExecutionPolicy Bypass -WindowStyle Hidden -File \"" + helperPath + "\" -BaseDir \"" + dir + "\" -ParentPid " + Process.GetCurrentProcess().Id + " -AutoLaunch " + (autoLaunch ? "1" : "0") + " -AutoPlay " + (autoPlay ? "1" : "0") + " -ArtworkEnabled " + (artworkEnabled ? "1" : "0");
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                psi.RedirectStandardError = true;
                Process p = new Process();
                p.StartInfo = psi;
                p.EnableRaisingEvents = true;
                p.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e)
                {
                    if (string.IsNullOrEmpty(e.Data)) return;
                    try { AppendBoundedLog(startupLog, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  STDERR | " + e.Data + Environment.NewLine); } catch { }
                };
                p.Exited += delegate(object sender, EventArgs e)
                {
                    try { AppendBoundedLog(startupLog, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  PROCESS EXIT | Code=" + p.ExitCode + Environment.NewLine); } catch { }
                };
                if (!p.Start()) throw new Exception("powershell.exe konnte nicht gestartet werden");
                p.BeginErrorReadLine();
                helperProcess = p;
                Write("YOUTUBE BRIDGE START | PID=" + helperProcess.Id);
            }
            catch (Exception ex)
            {
                available = false;
                state = "Bridge-Fehler";
                Write("YOUTUBE BRIDGE START FEHLER | " + ex.Message);
            }
        }

        private void EnsureHelper()
        {
            try { if (helperProcess == null || helperProcess.HasExited) StartHelper(); } catch { StartHelper(); }
        }

        private void SendCommand(string action, int volume = -1, string url = "")
        {
            try
            {
                EnsureHelper();
                sequence++;
                Directory.CreateDirectory(commandDir);
                string stamp = DateTime.UtcNow.Ticks.ToString("D19") + "_" + sequence.ToString("D6");
                string finalPath = Path.Combine(commandDir, stamp + ".cmd");
                string temp = finalPath + ".tmp";
                string data = "Sequence=" + sequence + Environment.NewLine +
                              "Action=" + action + Environment.NewLine +
                              "Volume=" + volume + Environment.NewLine +
                              "UrlBase64=" + (string.IsNullOrEmpty(url) ? "" : Convert.ToBase64String(Encoding.UTF8.GetBytes(url))) + Environment.NewLine;
                File.WriteAllText(temp, data, Encoding.UTF8);
                File.Move(temp, finalPath);
                Write("YOUTUBE CMD " + action + (volume >= 0 ? (" | Volume=" + volume) : ""));
            }
            catch (Exception ex) { state = "Bridge-Fehler"; Write("YOUTUBE CMD FEHLER | " + ex.Message); }
        }

        private void RefreshStatus()
        {
            if ((DateTime.Now - lastStatusRead).TotalMilliseconds < 300) return;
            lastStatusRead = DateTime.Now;
            try
            {
                if (!File.Exists(statusPath)) return;
                foreach (string line in File.ReadAllLines(statusPath))
                {
                    int p = line.IndexOf('=');
                    if (p <= 0) continue;
                    string key = line.Substring(0, p).Trim();
                    string value = line.Substring(p + 1).Trim();
                    if (key.Equals("Available", StringComparison.OrdinalIgnoreCase)) { bool b; if (bool.TryParse(value, out b)) available = b; }
                    else if (key.Equals("State", StringComparison.OrdinalIgnoreCase)) state = value;
                    else if (key.Equals("TitleBase64", StringComparison.OrdinalIgnoreCase)) title = Decode64(value);
                    else if (key.Equals("ArtistBase64", StringComparison.OrdinalIgnoreCase)) artist = Decode64(value);
                    else if (key.Equals("AlbumBase64", StringComparison.OrdinalIgnoreCase)) album = Decode64(value);
                    else if (key.Equals("SourceBase64", StringComparison.OrdinalIgnoreCase)) source = Decode64(value);
                    else if (key.Equals("CoverPathBase64", StringComparison.OrdinalIgnoreCase)) coverPath = Decode64(value);
                }
            }
            catch { }
        }

        private static string Decode64(string value)
        {
            try { return string.IsNullOrEmpty(value) ? "" : Encoding.UTF8.GetString(Convert.FromBase64String(value)); } catch { return ""; }
        }

        private void Write(string s)
        {
            try { AppendBoundedLog(log, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + s + Environment.NewLine); } catch { }
        }
    }

    private class SpotifyMusicBridge
    {
        private readonly string log;
        private readonly string dir;
        private readonly string helperPath;
        private readonly string commandDir;
        private readonly string statusPath;
        private Process helperProcess;
        private int sequence;
        private DateTime lastStatusRead = DateTime.MinValue;
        private bool available;
        private string state = "Not connected";
        private string title = "";
        private string artist = "";
        private string album = "";
        private string source = "";
        private string coverPath = "";
        private bool artworkEnabled = true;

        public bool Available { get { RefreshStatus(); return available; } }
        public string StateText { get { RefreshStatus(); return state; } }
        public string Title { get { RefreshStatus(); return title; } }
        public string Artist { get { RefreshStatus(); return artist; } }
        public string Album { get { RefreshStatus(); return album; } }
        public string Source { get { RefreshStatus(); return source; } }
        public string CoverPath { get { RefreshStatus(); return coverPath; } }

        public SpotifyMusicBridge(string logPath, bool artworkEnabledInitial)
        {
            log = logPath;
            dir = Path.GetDirectoryName(logPath);
            helperPath = Path.Combine(dir, "SpotifyMusicBridge.ps1");
            commandDir = Path.Combine(dir, "spotify_commands");
            statusPath = Path.Combine(dir, "spotify_status.txt");
            artworkEnabled = artworkEnabledInitial;

            // TEST47: fully lazy Spotify startup.
            // Do NOT launch the Spotify bridge/helper when GTA or the mod starts.
            // The helper is started only after an actual Spotify command, e.g. NUM5
            // in the Spotify tab. This also guarantees that Spotify itself cannot be
            // opened by the integration during normal mod startup.
            try
            {
                Directory.CreateDirectory(commandDir);
                foreach (string old in Directory.GetFiles(commandDir, "*.cmd"))
                    try { File.Delete(old); } catch { }
                try { if (File.Exists(statusPath)) File.Delete(statusPath); } catch { }
            }
            catch { }

            available = false;
            state = "Not connected";
        }

        public void Toggle() { SendCommand("TOGGLE"); }
        public void Play() { SendCommand("PLAY"); }
        public void Pause() { SendCommand("PAUSE"); }
        public void Next() { SendCommand("NEXT"); }
        public void Previous() { SendCommand("PREVIOUS"); }
        public void OpenHome() { SendCommand("OPEN_HOME"); }
        public void BeginDetection() { SendCommand("DETECT"); }
        public void SetVolume(int volume) { SendCommand("VOLUME", volume); }
        public void RestoreVolume() { SendCommand("RESTORE_VOLUME"); }
        public void SetManagedActive(bool active) { SendCommand(active ? "MODE_ON" : "MODE_OFF"); }
        public void CloseManagedApp() { SendCommand("CLOSE_APP"); }
        public void SetArtworkEnabled(bool enabledValue)
        {
            artworkEnabled = enabledValue;
            bool running = false;
            try { running = helperProcess != null && !helperProcess.HasExited; } catch { running = false; }
            if (running) SendCommand(enabledValue ? "ARTWORK_ON" : "ARTWORK_OFF");
            if (!enabledValue) coverPath = "";
        }
        public void Close()
        {
            // TEST66: GTA shutdown waits briefly for the Spotify helper to process
            // CLOSE_APP / QUIT, then force-stops only the helper if it hangs.
            bool running = false;
            try { running = helperProcess != null && !helperProcess.HasExited; } catch { running = false; }

            if (running)
            {
                try { SendCommand("RESTORE_VOLUME"); SendCommand("QUIT"); } catch { }
                try
                {
                    if (helperProcess != null && !helperProcess.WaitForExit(1600))
                        helperProcess.Kill();
                }
                catch { }
            }

            try { if (helperProcess != null) helperProcess.Close(); } catch { }
            try { if (helperProcess != null) helperProcess.Dispose(); } catch { }
            helperProcess = null;
            available = false;
            state = "Not connected";
        }

        private void StartHelper()
        {
            try
            {
                if (!File.Exists(helperPath)) throw new FileNotFoundException("SpotifyMusicBridge.ps1 missing", helperPath);
                Directory.CreateDirectory(commandDir);
                foreach (string old in Directory.GetFiles(commandDir, "*.cmd")) try { File.Delete(old); } catch { }
                try { if (File.Exists(statusPath)) File.Delete(statusPath); } catch { }

                string startupLog = Path.Combine(dir, "spotify_bridge_startup.log");
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "powershell.exe";
                psi.Arguments = "-NoProfile -Sta -ExecutionPolicy Bypass -WindowStyle Hidden -File \"" + helperPath + "\" -BaseDir \"" + dir + "\" -ParentPid " + Process.GetCurrentProcess().Id + " -ArtworkEnabled " + (artworkEnabled ? "1" : "0");
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                psi.RedirectStandardError = true;

                Process p = new Process();
                p.StartInfo = psi;
                p.EnableRaisingEvents = true;
                p.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e)
                {
                    if (string.IsNullOrEmpty(e.Data)) return;
                    try { AppendBoundedLog(startupLog, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  STDERR | " + e.Data + Environment.NewLine); } catch { }
                };
                if (!p.Start()) throw new Exception("powershell.exe could not be started");
                p.BeginErrorReadLine();
                helperProcess = p;
                Write("SPOTIFY BRIDGE LAZY START | PID=" + helperProcess.Id);
            }
            catch (Exception ex)
            {
                available = false;
                state = "Bridge error";
                Write("SPOTIFY BRIDGE START ERROR | " + ex.Message);
            }
        }

        private void EnsureHelper()
        {
            try { if (helperProcess == null || helperProcess.HasExited) StartHelper(); } catch { StartHelper(); }
        }

        private void SendCommand(string action, int volume = -1)
        {
            try
            {
                EnsureHelper();
                sequence++;
                Directory.CreateDirectory(commandDir);
                string stamp = DateTime.UtcNow.Ticks.ToString("D19") + "_" + sequence.ToString("D6");
                string finalPath = Path.Combine(commandDir, stamp + ".cmd");
                string temp = finalPath + ".tmp";
                string data = "Sequence=" + sequence + Environment.NewLine +
                              "Action=" + action + Environment.NewLine +
                              "Volume=" + volume + Environment.NewLine;
                File.WriteAllText(temp, data, Encoding.UTF8);
                File.Move(temp, finalPath);
                Write("SPOTIFY CMD " + action + (volume >= 0 ? (" | Volume=" + volume) : ""));
            }
            catch (Exception ex)
            {
                state = "Bridge error";
                Write("SPOTIFY CMD ERROR | " + ex.Message);
            }
        }

        private void RefreshStatus()
        {
            if ((DateTime.Now - lastStatusRead).TotalMilliseconds < 300) return;
            lastStatusRead = DateTime.Now;
            try
            {
                if (!File.Exists(statusPath)) return;
                foreach (string line in File.ReadAllLines(statusPath))
                {
                    int p = line.IndexOf('=');
                    if (p <= 0) continue;
                    string key = line.Substring(0, p).Trim();
                    string value = line.Substring(p + 1).Trim();
                    if (key.Equals("Available", StringComparison.OrdinalIgnoreCase)) { bool b; if (bool.TryParse(value, out b)) available = b; }
                    else if (key.Equals("State", StringComparison.OrdinalIgnoreCase)) state = value;
                    else if (key.Equals("TitleBase64", StringComparison.OrdinalIgnoreCase)) title = Decode64(value);
                    else if (key.Equals("ArtistBase64", StringComparison.OrdinalIgnoreCase)) artist = Decode64(value);
                    else if (key.Equals("AlbumBase64", StringComparison.OrdinalIgnoreCase)) album = Decode64(value);
                    else if (key.Equals("SourceBase64", StringComparison.OrdinalIgnoreCase)) source = Decode64(value);
                    else if (key.Equals("CoverPathBase64", StringComparison.OrdinalIgnoreCase)) coverPath = Decode64(value);
                }
            }
            catch { }
        }

        private static string Decode64(string value)
        {
            try { return string.IsNullOrEmpty(value) ? "" : Encoding.UTF8.GetString(Convert.FromBase64String(value)); } catch { return ""; }
        }

        private void Write(string text)
        {
            try { AppendBoundedLog(log, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + text + Environment.NewLine); } catch { }
        }
    }

    private class AudioWorker
    {
        private readonly string log;
        private readonly string dir;
        private readonly string helperPath;
        private readonly string commandDir;
        private readonly string statusPath;
        private Process helperProcess;
        private int sequence;
        private string stateText = "Stopped";
        private string title = "";
        private string artist = "";
        private string mediaName = "";
        private DateTime lastStatusRead = DateTime.MinValue;

        public string StateText
        {
            get { RefreshStatus(); return stateText; }
        }
        public string Title
        {
            get { RefreshStatus(); return title; }
        }
        public string Artist
        {
            get { RefreshStatus(); return artist; }
        }
        public string MediaName
        {
            get { RefreshStatus(); return mediaName; }
        }
        public int HelperPid
        {
            get
            {
                try { return helperProcess != null && !helperProcess.HasExited ? helperProcess.Id : 0; }
                catch { return 0; }
            }
        }

        public AudioWorker(string logPath)
        {
            log = logPath;
            dir = Path.GetDirectoryName(logPath);
            helperPath = Path.Combine(dir, "InternetRadioPlayer.ps1");
            commandDir = Path.Combine(dir, "player_commands");
            statusPath = Path.Combine(dir, "player_status.txt");

            // TEST49: keep startup completely quiet.
            // InternetRadioPlayer.ps1 starts only when the radio actually needs it.
            try
            {
                Directory.CreateDirectory(commandDir);
                foreach (string old in Directory.GetFiles(commandDir, "*.cmd"))
                    try { File.Delete(old); } catch { }
                try { if (File.Exists(statusPath)) File.Delete(statusPath); } catch { }
            }
            catch { }

            stateText = "Stopped";
        }

        public void Play(string u, int v)
        {
            stateText = "Reconnecting";
            SendCommand("PLAY", u ?? "", v);
        }

        public void SetVolume(int v)
        {
            SendCommand("VOLUME", "", v);
        }

        public void Stop()
        {
            stateText = "Stopped";
            SendCommand("STOP", "", 0);
        }

        public void Close()
        {
            // TEST49: do not launch the player helper merely to shut it down.
            bool running = false;
            try { running = helperProcess != null && !helperProcess.HasExited; } catch { running = false; }

            if (running)
            {
                try { SendCommand("QUIT", "", 0); } catch { }
            }

            try
            {
                if (helperProcess != null) helperProcess.Close();
            }
            catch { }

            helperProcess = null;
            Write("EXTERNAL AUDIO CLOSE ANGEFORDERT");
        }

        private void StartHelper()
        {
            try
            {
                if (!File.Exists(helperPath))
                    throw new FileNotFoundException("InternetRadioPlayer.ps1 fehlt", helperPath);

                try
                {
                    Directory.CreateDirectory(commandDir);
                    foreach (string old in Directory.GetFiles(commandDir, "*.cmd"))
                        try { File.Delete(old); } catch { }
                }
                catch { }
                try { if (File.Exists(statusPath)) File.Delete(statusPath); } catch { }

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "powershell.exe";
                psi.Arguments = "-NoProfile -Sta -ExecutionPolicy Bypass -WindowStyle Hidden -File \"" + helperPath + "\" -BaseDir \"" + dir + "\" -ParentPid " + Process.GetCurrentProcess().Id;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                helperProcess = Process.Start(psi);
                Write("EXTERNAL AUDIO HELPER START | PID=" + (helperProcess != null ? helperProcess.Id.ToString() : "?"));
            }
            catch (Exception ex)
            {
                stateText = "AudioFehler";
                Write("EXTERNAL AUDIO START FEHLER | " + ex.ToString());
            }
        }

        private void EnsureHelper()
        {
            try
            {
                if (helperProcess == null || helperProcess.HasExited)
                {
                    Write("EXTERNAL AUDIO HELPER NICHT AKTIV - NEUSTART");
                    StartHelper();
                }
            }
            catch
            {
                StartHelper();
            }
        }

        private void SendCommand(string action, string url, int volume)
        {
            try
            {
                EnsureHelper();
                sequence++;
                int vol = Math.Max(0, Math.Min(100, volume));
                string url64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(url ?? ""));
                string data =
                    "Sequence=" + sequence + Environment.NewLine +
                    "Action=" + action + Environment.NewLine +
                    "Volume=" + vol + Environment.NewLine +
                    "UrlBase64=" + url64 + Environment.NewLine;

                Directory.CreateDirectory(commandDir);
                string stamp = DateTime.UtcNow.Ticks.ToString("D19") + "_" + sequence.ToString("D6");
                string finalPath = Path.Combine(commandDir, stamp + ".cmd");
                string temp = finalPath + ".tmp";
                File.WriteAllText(temp, data, Encoding.UTF8);
                File.Move(temp, finalPath);
                Write("EXTERNAL CMD QUEUED " + action + " | Sequence=" + sequence + " | Volume=" + vol);
            }
            catch (Exception ex)
            {
                stateText = "AudioFehler";
                Write("EXTERNAL CMD FEHLER | " + ex.Message);
            }
        }

        private void RefreshStatus()
        {
            if ((DateTime.Now - lastStatusRead).TotalMilliseconds < 300) return;
            lastStatusRead = DateTime.Now;
            try
            {
                if (!File.Exists(statusPath)) return;
                string[] lines = File.ReadAllLines(statusPath);
                foreach (string line in lines)
                {
                    int p = line.IndexOf('=');
                    if (p <= 0) continue;
                    string key = line.Substring(0, p).Trim();
                    string value = line.Substring(p + 1).Trim();
                    if (key.Equals("StateText", StringComparison.OrdinalIgnoreCase) && value.Length > 0)
                        stateText = value;
                    else if (key.Equals("TitleBase64", StringComparison.OrdinalIgnoreCase))
                        title = Decode64(value);
                    else if (key.Equals("ArtistBase64", StringComparison.OrdinalIgnoreCase))
                        artist = Decode64(value);
                    else if (key.Equals("MediaNameBase64", StringComparison.OrdinalIgnoreCase))
                        mediaName = Decode64(value);
                }
            }
            catch { }
        }

        private static string Decode64(string value)
        {
            try
            {
                if (string.IsNullOrEmpty(value)) return "";
                return Encoding.UTF8.GetString(Convert.FromBase64String(value));
            }
            catch { return ""; }
        }

        private void Write(string s)
        {
            try { AppendBoundedLog(log, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + s + Environment.NewLine); } catch { }
        }
    }
}


public static class InternetRadioBrowserVolume
{
    private enum EDataFlow { eRender = 0, eCapture = 1, eAll = 2 }
    private enum ERole { eConsole = 0, eMultimedia = 1, eCommunications = 2 }

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] private class MMDeviceEnumeratorComObject { }
    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator { int EnumAudioEndpoints(EDataFlow f,int m,out IntPtr p); int GetDefaultAudioEndpoint(EDataFlow f,ERole r,out IMMDevice d); int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id,out IMMDevice d); int RegisterEndpointNotificationCallback(IntPtr p); int UnregisterEndpointNotificationCallback(IntPtr p); }
    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice { int Activate(ref Guid iid,int cls,IntPtr ap,[MarshalAs(UnmanagedType.IUnknown)] out object o); int OpenPropertyStore(int a,out IntPtr p); int GetId([MarshalAs(UnmanagedType.LPWStr)] out string s); int GetState(out int s); }
    [ComImport, Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionManager2 { int GetAudioSessionControl(ref Guid g,uint f,out IAudioSessionControl c); int GetSimpleAudioVolume(ref Guid g,uint f,out ISimpleAudioVolume v); int GetSessionEnumerator(out IAudioSessionEnumerator e); int RegisterSessionNotification(IntPtr p); int UnregisterSessionNotification(IntPtr p); int RegisterDuckNotification([MarshalAs(UnmanagedType.LPWStr)] string s,IntPtr p); int UnregisterDuckNotification(IntPtr p); }
    [ComImport, Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionEnumerator { int GetCount(out int c); int GetSession(int i,out IAudioSessionControl c); }
    [ComImport, Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionControl { int GetState(out int v); int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string s); int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string s,ref Guid g); int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string s); int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string s,ref Guid g); int GetGroupingParam(out Guid g); int SetGroupingParam(ref Guid g,ref Guid e); int RegisterAudioSessionNotification(IntPtr p); int UnregisterAudioSessionNotification(IntPtr p); }
    [ComImport, Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionControl2 : IAudioSessionControl { new int GetState(out int v); new int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string s); new int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string s,ref Guid g); new int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string s); new int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string s,ref Guid g); new int GetGroupingParam(out Guid g); new int SetGroupingParam(ref Guid g,ref Guid e); new int RegisterAudioSessionNotification(IntPtr p); new int UnregisterAudioSessionNotification(IntPtr p); int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string s); int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string s); int GetProcessId(out uint p); int IsSystemSoundsSession(); int SetDuckingPreference(bool b); }
    [ComImport, Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ISimpleAudioVolume { int SetMasterVolume(float f,ref Guid g); int GetMasterVolume(out float f); int SetMute(bool b,ref Guid g); int GetMute(out bool b); }

    private static readonly Dictionary<string,float> original = new Dictionary<string,float>(StringComparer.OrdinalIgnoreCase);
    private const int CLSCTX_ALL = 23;

    private static bool Wanted(string processName,string source)
    {
        string p=(processName??"").ToLowerInvariant(); string s=(source??"").ToLowerInvariant();
        if(s.Contains("chrome")) return p=="chrome";
        if(s.Contains("msedge")||s.Contains("edge")) return p=="msedge";
        if(s.Contains("firefox")) return p=="firefox";
        if(s.Contains("spotify")) return p=="spotify";
        return p=="chrome"||p=="msedge"||p=="firefox"||p=="spotify";
    }

    private static T QI<T>(object o) where T:class
    {
        if(o==null) return null; IntPtr u=IntPtr.Zero,p=IntPtr.Zero;
        try { u=Marshal.GetIUnknownForObject(o); Guid id=typeof(T).GUID; if(Marshal.QueryInterface(u,ref id,out p)!=0||p==IntPtr.Zero) return null; return Marshal.GetObjectForIUnknown(p) as T; }
        finally { if(p!=IntPtr.Zero) Marshal.Release(p); if(u!=IntPtr.Zero) Marshal.Release(u); }
    }

    private static int ProcessSessions(string source,float level,bool restore)
    {
        IMMDeviceEnumerator de=null; IMMDevice d=null; object mo=null; IAudioSessionEnumerator se=null; int changed=0;
        try
        {
            de=(IMMDeviceEnumerator)(new MMDeviceEnumeratorComObject());
            if(de.GetDefaultAudioEndpoint(EDataFlow.eRender,ERole.eMultimedia,out d)!=0||d==null) return 0;
            Guid iid=typeof(IAudioSessionManager2).GUID; if(d.Activate(ref iid,CLSCTX_ALL,IntPtr.Zero,out mo)!=0||mo==null) return 0;
            IAudioSessionManager2 m=mo as IAudioSessionManager2; if(m==null||m.GetSessionEnumerator(out se)!=0||se==null) return 0;
            int count=0; se.GetCount(out count);
            for(int i=0;i<count;i++)
            {
                IAudioSessionControl c=null; IAudioSessionControl2 c2=null; ISimpleAudioVolume v=null;
                try
                {
                    if(se.GetSession(i,out c)!=0||c==null) continue; c2=QI<IAudioSessionControl2>(c); v=QI<ISimpleAudioVolume>(c); if(c2==null||v==null) continue;
                    uint pid=0; if(c2.GetProcessId(out pid)!=0||pid==0) continue; string pn; try{pn=Process.GetProcessById((int)pid).ProcessName;}catch{continue;}
                    if(!Wanted(pn,source)) continue; string key=pid.ToString(); string inst=null; try{c2.GetSessionInstanceIdentifier(out inst);}catch{} if(!string.IsNullOrEmpty(inst)) key+="|"+inst;
                    Guid ctx=Guid.Empty;
                    if(restore) { float old; if(original.TryGetValue(key,out old) && v.SetMasterVolume(old,ref ctx)==0) changed++; }
                    else { if(!original.ContainsKey(key)){float old=1f;if(v.GetMasterVolume(out old)==0) original[key]=old;} if(v.SetMasterVolume(level,ref ctx)==0) changed++; }
                }
                finally { if(v!=null&&Marshal.IsComObject(v))try{Marshal.ReleaseComObject(v);}catch{} if(c2!=null&&Marshal.IsComObject(c2))try{Marshal.ReleaseComObject(c2);}catch{} if(c!=null&&Marshal.IsComObject(c))try{Marshal.ReleaseComObject(c);}catch{} }
            }
        }
        finally { if(restore) original.Clear(); if(se!=null&&Marshal.IsComObject(se))try{Marshal.ReleaseComObject(se);}catch{} if(mo!=null&&Marshal.IsComObject(mo))try{Marshal.ReleaseComObject(mo);}catch{} if(d!=null&&Marshal.IsComObject(d))try{Marshal.ReleaseComObject(d);}catch{} if(de!=null&&Marshal.IsComObject(de))try{Marshal.ReleaseComObject(de);}catch{} }
        return changed;
    }

    public static int SetVolume(string source,float level) { level=Math.Max(0f,Math.Min(1f,level)); return ProcessSessions(source,level,false); }
    public static int Restore() { if(original.Count==0) return 0; return ProcessSessions("",1f,true); }
}


public static class InternetRadioAudioMeter
{
    private enum EDataFlow { eRender = 0, eCapture = 1, eAll = 2 }
    private enum ERole { eConsole = 0, eMultimedia = 1, eCommunications = 2 }

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] private class MMDeviceEnumeratorComObject { }
    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator { int EnumAudioEndpoints(EDataFlow f,int m,out IntPtr p); int GetDefaultAudioEndpoint(EDataFlow f,ERole r,out IMMDevice d); int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id,out IMMDevice d); int RegisterEndpointNotificationCallback(IntPtr p); int UnregisterEndpointNotificationCallback(IntPtr p); }
    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice { int Activate(ref Guid iid,int cls,IntPtr ap,[MarshalAs(UnmanagedType.IUnknown)] out object o); int OpenPropertyStore(int a,out IntPtr p); int GetId([MarshalAs(UnmanagedType.LPWStr)] out string s); int GetState(out int s); }
    [ComImport, Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionManager2 { int GetAudioSessionControl(ref Guid g,uint f,out IAudioSessionControl c); int GetSimpleAudioVolume(ref Guid g,uint f,out object v); int GetSessionEnumerator(out IAudioSessionEnumerator e); int RegisterSessionNotification(IntPtr p); int UnregisterSessionNotification(IntPtr p); int RegisterDuckNotification([MarshalAs(UnmanagedType.LPWStr)] string s,IntPtr p); int UnregisterDuckNotification(IntPtr p); }
    [ComImport, Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionEnumerator { int GetCount(out int c); int GetSession(int i,out IAudioSessionControl c); }
    [ComImport, Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionControl { int GetState(out int v); int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string s); int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string s,ref Guid g); int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string s); int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string s,ref Guid g); int GetGroupingParam(out Guid g); int SetGroupingParam(ref Guid g,ref Guid e); int RegisterAudioSessionNotification(IntPtr p); int UnregisterAudioSessionNotification(IntPtr p); }
    [ComImport, Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionControl2 : IAudioSessionControl { new int GetState(out int v); new int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string s); new int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string s,ref Guid g); new int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string s); new int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string s,ref Guid g); new int GetGroupingParam(out Guid g); new int SetGroupingParam(ref Guid g,ref Guid e); new int RegisterAudioSessionNotification(IntPtr p); new int UnregisterAudioSessionNotification(IntPtr p); int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string s); int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string s); int GetProcessId(out uint p); int IsSystemSoundsSession(); int SetDuckingPreference(bool b); }
    [ComImport, Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioMeterInformation { int GetPeakValue(out float pfPeak); int GetMeteringChannelCount(out int pnChannelCount); int GetChannelsPeakValues(int u32ChannelCount,[Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] float[] afPeakValues); int QueryHardwareSupport(out int pdwHardwareSupportMask); }

    private const int CLSCTX_ALL = 23;

    private static T QI<T>(object o) where T : class
    {
        if (o == null) return null;
        IntPtr u = IntPtr.Zero, p = IntPtr.Zero;
        try
        {
            u = Marshal.GetIUnknownForObject(o);
            Guid id = typeof(T).GUID;
            if (Marshal.QueryInterface(u, ref id, out p) != 0 || p == IntPtr.Zero) return null;
            return Marshal.GetObjectForIUnknown(p) as T;
        }
        finally
        {
            if (p != IntPtr.Zero) Marshal.Release(p);
            if (u != IntPtr.Zero) Marshal.Release(u);
        }
    }

    private static bool WantedBrowser(string processName, string source)
    {
        string p = (processName ?? "").ToLowerInvariant();
        string s = (source ?? "").ToLowerInvariant();
        if (s.Contains("chrome")) return p == "chrome";
        if (s.Contains("msedge") || s.Contains("edge")) return p == "msedge";
        if (s.Contains("firefox")) return p == "firefox";
        if (s.Contains("spotify")) return p == "spotify";
        return p == "chrome" || p == "msedge" || p == "firefox" || p == "spotify";
    }

    public static float GetPeakForProcessId(int targetPid)
    {
        if (targetPid <= 0) return 0.0f;
        return QueryPeak(delegate(uint pid, string processName) { return pid == (uint)targetPid; });
    }

    public static float GetPeakForBrowser(string source)
    {
        return QueryPeak(delegate(uint pid, string processName) { return WantedBrowser(processName, source); });
    }

    private static float QueryPeak(Func<uint, string, bool> match)
    {
        IMMDeviceEnumerator de = null; IMMDevice d = null; object mo = null; IAudioSessionEnumerator se = null;
        float maxPeak = 0.0f;
        try
        {
            de = (IMMDeviceEnumerator)(new MMDeviceEnumeratorComObject());
            if (de.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out d) != 0 || d == null) return 0.0f;
            Guid iid = typeof(IAudioSessionManager2).GUID;
            if (d.Activate(ref iid, CLSCTX_ALL, IntPtr.Zero, out mo) != 0 || mo == null) return 0.0f;
            IAudioSessionManager2 m = mo as IAudioSessionManager2;
            if (m == null || m.GetSessionEnumerator(out se) != 0 || se == null) return 0.0f;
            int count = 0; se.GetCount(out count);
            for (int i = 0; i < count; i++)
            {
                IAudioSessionControl c = null; IAudioSessionControl2 c2 = null; IAudioMeterInformation meter = null;
                try
                {
                    if (se.GetSession(i, out c) != 0 || c == null) continue;
                    c2 = QI<IAudioSessionControl2>(c);
                    meter = QI<IAudioMeterInformation>(c);
                    if (c2 == null || meter == null) continue;
                    uint pid = 0; if (c2.GetProcessId(out pid) != 0 || pid == 0) continue;
                    string pn; try { pn = Process.GetProcessById((int)pid).ProcessName; } catch { continue; }
                    if (!match(pid, pn)) continue;
                    float peak = 0.0f; if (meter.GetPeakValue(out peak) == 0 && peak > maxPeak) maxPeak = peak;
                }
                finally
                {
                    if (meter != null && Marshal.IsComObject(meter)) try { Marshal.ReleaseComObject(meter); } catch { }
                    if (c2 != null && Marshal.IsComObject(c2)) try { Marshal.ReleaseComObject(c2); } catch { }
                    if (c != null && Marshal.IsComObject(c)) try { Marshal.ReleaseComObject(c); } catch { }
                }
            }
        }
        catch { return 0.0f; }
        finally
        {
            if (se != null && Marshal.IsComObject(se)) try { Marshal.ReleaseComObject(se); } catch { }
            if (mo != null && Marshal.IsComObject(mo)) try { Marshal.ReleaseComObject(mo); } catch { }
            if (d != null && Marshal.IsComObject(d)) try { Marshal.ReleaseComObject(d); } catch { }
            if (de != null && Marshal.IsComObject(de)) try { Marshal.ReleaseComObject(de); } catch { }
        }
        return maxPeak;
    }
}


public class InternetRadioUiRendererV120 : Script
{
    public InternetRadioUiRendererV120()
    {
        Interval = 0;
        Tick += OnFrame;
    }

    private void OnFrame(object sender, EventArgs e)
    {
        InternetRadioSimpleV120YouTube controller = InternetRadioSimpleV120YouTube.Instance;
        if (controller != null) controller.RenderUi();
    }
}

// v7.41: per-frame number-plate light renderer. The controller uses a stricter
// headlight-intent latch so daytime DRL/automatic states cannot cause plate-light flicker.
public class InternetRadioPlateLightRenderer : Script
{
    public InternetRadioPlateLightRenderer()
    {
        Interval = 0;
        Tick += OnPlateLightTick;
        Aborted += OnPlateLightAbort;
    }

    private void OnPlateLightTick(object sender, EventArgs e)
    {
        if (!InternetRadioSimpleV120YouTube.SafeRenderState ||
            !InternetRadioSimpleV120YouTube.PlateLightRenderActive) return;

        try
        {
            int vehicleHandle = InternetRadioSimpleV120YouTube.PlateLightVehicleHandle;
            if (vehicleHandle == 0) return;

            if (!Function.Call<bool>(Hash.DOES_ENTITY_EXIST, vehicleHandle))
            {
                InternetRadioSimpleV120YouTube.PlateLightRenderActive = false;
                InternetRadioSimpleV120YouTube.PlateLightVehicleHandle = 0;
                return;
            }

            GTA.Math.Vector3 pos;
            int bone = InternetRadioSimpleV120YouTube.PlateLightBoneIndex;
            if (bone >= 0)
            {
                pos = Function.Call<GTA.Math.Vector3>(Hash.GET_WORLD_POSITION_OF_ENTITY_BONE, vehicleHandle, bone);
                // Move the light a few centimetres away from the body so the dynamic point
                // light does not get swallowed by the bumper/plate mesh.
                GTA.Math.Vector3 offset = Function.Call<GTA.Math.Vector3>(
                    Hash.GET_OFFSET_FROM_ENTITY_IN_WORLD_COORDS,
                    vehicleHandle, 0.0f, -0.055f, -0.025f);
                GTA.Math.Vector3 origin = Function.Call<GTA.Math.Vector3>(Hash.GET_ENTITY_COORDS, vehicleHandle, false);
                pos.X += offset.X - origin.X;
                pos.Y += offset.Y - origin.Y;
                pos.Z += offset.Z - origin.Z;
            }
            else
            {
                pos = Function.Call<GTA.Math.Vector3>(
                    Hash.GET_OFFSET_FROM_ENTITY_IN_WORLD_COORDS,
                    vehicleHandle,
                    0.0f,
                    InternetRadioSimpleV120YouTube.PlateLightFallbackY,
                    InternetRadioSimpleV120YouTube.PlateLightFallbackZ);
            }

            Function.Call(Hash.DRAW_LIGHT_WITH_RANGE,
                pos.X, pos.Y, pos.Z,
                InternetRadioSimpleV120YouTube.PlateLightR,
                InternetRadioSimpleV120YouTube.PlateLightG,
                InternetRadioSimpleV120YouTube.PlateLightB,
                InternetRadioSimpleV120YouTube.PlateLightRange,
                InternetRadioSimpleV120YouTube.PlateLightIntensity);
        }
        catch { }
    }

    private void OnPlateLightAbort(object sender, EventArgs e)
    {
        InternetRadioSimpleV120YouTube.PlateLightRenderActive = false;
        InternetRadioSimpleV120YouTube.PlateLightVehicleHandle = 0;
    }
}

// TEST27: per-frame renderer for the audio-reactive cabin light.
// Keeping this separate lets the main radio logic stay throttled at 100 ms while
// DRAW_LIGHT_WITH_RANGE is refreshed every rendered frame, as required by GTA.
public class InternetRadioCabinLightRenderer : Script
{
    public InternetRadioCabinLightRenderer()
    {
        Interval = 0;
        Tick += OnCabinLightTick;
        Aborted += OnCabinLightAbort;
    }

    private void OnCabinLightTick(object sender, EventArgs e)
    {
        if (!InternetRadioSimpleV120YouTube.SafeRenderState ||
            !InternetRadioSimpleV120YouTube.CabinLightRenderActive) return;

        try
        {
            int r = InternetRadioSimpleV120YouTube.CabinLightR;
            int g = InternetRadioSimpleV120YouTube.CabinLightG;
            int b = InternetRadioSimpleV120YouTube.CabinLightB;
            float range = InternetRadioSimpleV120YouTube.CabinLightRange;
            float intensity = InternetRadioSimpleV120YouTube.CabinLightIntensity;
            int vehicleHandle = InternetRadioSimpleV120YouTube.CabinLightVehicleHandle;
            if (vehicleHandle == 0) return;

            // v7.8 conservative stale-handle guard. No extra loading/fade natives are
            // called per frame; we only verify that the cached entity still exists.
            if (!Function.Call<bool>(Hash.DOES_ENTITY_EXIST, vehicleHandle))
            {
                InternetRadioSimpleV120YouTube.CabinLightRenderActive = false;
                InternetRadioSimpleV120YouTube.CabinLightVehicleHandle = 0;
                return;
            }

            // TEST55: resolve the light anchors every frame so they stay attached to a
            // moving vehicle instead of rendering at stale world coordinates behind it.
            GTA.Math.Vector3 front = Function.Call<GTA.Math.Vector3>(
                Hash.GET_OFFSET_FROM_ENTITY_IN_WORLD_COORDS,
                vehicleHandle, 0.0f, 0.18f, 0.58f);

            GTA.Math.Vector3 rear = Function.Call<GTA.Math.Vector3>(
                Hash.GET_OFFSET_FROM_ENTITY_IN_WORLD_COORDS,
                vehicleHandle, 0.0f, -0.42f, 0.52f);

            Function.Call(Hash.DRAW_LIGHT_WITH_RANGE,
                front.X, front.Y, front.Z,
                r, g, b, range, intensity);

            Function.Call(Hash.DRAW_LIGHT_WITH_RANGE,
                rear.X, rear.Y, rear.Z,
                r, g, b, range * 0.82f, intensity * 0.55f);
        }
        catch { }
    }

    private void OnCabinLightAbort(object sender, EventArgs e)
    {
        InternetRadioSimpleV120YouTube.CabinLightRenderActive = false;
    }
}

