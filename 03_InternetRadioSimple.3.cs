using System;
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

    private readonly List<Station> stations = new List<Station>();
    private string dir;
    private string logPath;
    private int index;
    private int volume = 25;
    private int playerMaxVolume = 70;
    private float volumeCurveExponent = 1.10f;
    private int youtubeMaxVolume = 55;
    private float youtubeVolumeCurveExponent = 1.55f;
    private int youtubeVolumeProfileVersion = 0;
    private bool autoLaunchYouTubeMusic = false;
    private bool autoActivateYouTubeMusic = false;
    private bool autoPlayYouTubeMusic = false;
    private bool youtubeAutoActivationConsumed = false;
    private bool youtubeAutoPlayPending = false;
    private int volumeStep = 2;
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
    // V12.44: Dynamic station/category tint is exclusive to the SPECTRUM skin.
    // The other skins keep their coherent fixed theme; category colors are used as semantic accents.
    private float dynamicRadioBackgroundStrength = 0.72f;
    private float dynamicMenuR = 61f;
    private float dynamicMenuG = 238f;
    private float dynamicMenuB = 105f;
    private bool dynamicMenuColorInitialized = false;
    private bool menuSlowMotionEnabled = true;
    private float menuTimeScale = 0.45f;
    private bool menuSlowMotionApplied = false;
    private int menuIndex;
    private int menuTab = 0; // 0 Radio, 1 Stations, 2 Categories, 3 Info, 4 Skins, 5 YouTube Music, 6 Settings
    private int packIndex = 0;
    private int skinIndex = 0;
    private int skinMenuIndex = 0;
    private int settingsMenuIndex = 0;
    private string uiLanguage = "EN";
    private bool volumeHoldActive = false;
    private Keys volumeHoldKey = Keys.None;
    private DateTime volumeHoldNext = DateTime.MinValue;
    private readonly string[] skinNames = new string[] { "MODERN GREEN", "OEM BLUE", "RED SPORT", "AMBER CLASSIC", "MINIMAL WHITE", "SPECTRUM", "GTA VICE CITY", "GTA SAN ANDREAS", "CYBERPUNK NEON", "NFS UNDERGROUND 2", "MINECRAFT", "GANGSTER LUXE", "SAKURA ZEN" };
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
    private GTA.UI.CustomSprite minecraftGrassBlockSprite;
    private GTA.UI.CustomSprite sakuraZenMotifSprite;
    private string youtubeCoverTrackKey = "";
    private DateTime youtubeCoverNextLoadCheck = DateTime.MinValue;
    private DateTime youtubeMiniHideAt = DateTime.MinValue;
    private int appliedYoutubeVolume = -1;
    private float appliedYoutubeMixerLevel = -1.0f;
    private string bannerTop = "";
    private string bannerBottom = "";
    private DateTime bannerUntil = DateTime.MinValue;
    private string uiTitle = "INTERNET RADIO LS";
    private bool metadataEnabled = true;
    private string cachedAudioState = "Stopped";
    private string metadataTitle = "";
    private string metadataArtist = "";
    private string metadataMediaName = "";
    private string lastMetadataAnnouncement = "";
    private readonly float[] visualizerBars = new float[15];
    private float visualizerLevel = 0.0f;
    private DateTime lastVisualizerUpdate = DateTime.MinValue;
    private bool visualizerMeterAvailable = true;
    private bool userSettingsDirty = false;
    private DateTime userSettingsSaveAt = DateTime.MinValue;

    public InternetRadioSimpleV120YouTube()
    {
        dir = GetDir();
        Directory.CreateDirectory(dir);
        logPath = Path.Combine(dir, "03_RADIO.log");
        Log("KONSTRUKTOR START v1.0.0 NEXUS RELEASE");
        Instance = this;
        for (int i = 0; i < visualizerBars.Length; i++) visualizerBars[i] = 0.08f;
        try
        {
            LoadIni();
            EnsureUserSettingsFile();
            LoadMinecraftGrassBlockSprite();
            LoadSakuraZenMotifSprite();
            audio = new AudioWorker(logPath);
            youtube = new YouTubeMusicBridge(logPath, autoLaunchYouTubeMusic, autoPlayYouTubeMusic);
            Interval = 100;
            Tick += OnTick;
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
            Aborted += OnAbort;
            menuIndex = index;
            ShowBanner("InternetRadio LS v1.0.0", T("LOADED"), 4200);
            GTA.UI.Screen.ShowSubtitle("~b~InternetRadio LS v1.0.0~s~ - " + T("LOADED"), 3500);
            Log("Initialisierung komplett | Sender=" + stations.Count);
        }
        catch (Exception ex)
        {
            Log("INIT FEHLER: " + ex.ToString());
        }
    }

    private void OnTick(object sender, EventArgs e)
    {
        try
        {
            // GTA-Pause-/Hauptmenue: Stream sofort stoppen und keinerlei Radio-UI zeichnen.
            bool pauseMenuActive = false;
            try { pauseMenuActive = Function.Call<bool>(Hash.IS_PAUSE_MENU_ACTIVE); } catch { }

            if (pauseMenuActive)
            {
                // Never leave GTA slowed when the real pause/main menu takes over.
                RestoreGameTimeScale();
                if (!wasPauseMenuActive)
                {
                    resumeAfterPause = !youtubeModeActive && enabled && shouldPlay;
                    youtubeResumeAfterPause = youtubeModeActive && IsYouTubePlaying();
                    if (shouldPlay) StopRadio();
                    if (youtubeResumeAfterPause && youtube != null) youtube.Pause();
                    menuOpen = false;
                    Log("PAUSE MENU OPEN | RadioResume=" + resumeAfterPause + " | YouTubeResume=" + youtubeResumeAfterPause);
                }
                wasPauseMenuActive = true;
                return;
            }

            UpdateMenuSlowMotion();
            UpdateVolumeHold();
            FlushUserSettingsIfDue();

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
                bool inVehicle = IsRadioCapableVehicle();

                // V12.24: YouTube Music is fully on-demand. No automatic source switch on vehicle entry.

                if (justReturnedFromPause)
                {
                    if (youtubeModeActive)
                    {
                        if (youtubeResumeAfterPause && inVehicle && youtube != null) youtube.Play();
                        youtubeResumeAfterPause = false;
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
                    if (!inVehicle && wasInVehicle)
                    {
                        youtubeResumeAfterVehicle = IsYouTubePlaying();
                        if (youtubeResumeAfterVehicle && youtube != null) youtube.Pause();
                    }
                    else if (inVehicle && !wasInVehicle && youtubeResumeAfterVehicle)
                    {
                        if (youtube != null) youtube.Play();
                        youtubeResumeAfterVehicle = false;
                    }
                }
                else
                {
                    if (enabled && autoStart && inVehicle && !wasInVehicle) StartRadio();
                    if (!inVehicle && wasInVehicle) StopRadio();
                }

                wasInVehicle = inVehicle;

                if (youtubeModeActive && inVehicle)
                {
                    // YT Music uebernimmt exklusiv: weder GTA-Radio noch unser Webradio darf parallel laufen.
                    if (shouldPlay) StopRadio();
                    Game.RadioStation = RadioStation.RadioOff;
                    UpdateSpeechDucking();
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
                        ShowBanner(CurrentStationLine(), Shorten(metaNow, 58), 3500);
                        Log("METADATA | " + metaNow);
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
                UpdateYouTubeCoverSprite();
                if (youtubeSelectionPending && youtubeAvailable && IsYouTubePlaying())
                {
                    youtubeSelectionPending = false;
                    ActivateYouTubeMode();
                    ShowBanner("YouTube Music", T("YT_DETECTED"), 2200);
                    Log("YT USER SELECTION | Playing browser media session detected, source activated | " + youtubeSource + " | " + youtubeTitle);
                }
                if (youtubeModeActive && youtubeAutoPlayPending && youtubeAvailable)
                {
                    youtube.Play();
                    youtubeAutoPlayPending = false;
                    Log("YT AUTO PLAY | Media-Session bereit");
                }
                if (youtubeModeActive && youtubeAvailable) ApplyEffectiveVolume();
            }

            UpdateAudioVisualizer();
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
                float scale = Math.Max(0.20f, Math.Min(1.00f, menuTimeScale));
                Function.Call(Hash.SET_TIME_SCALE, scale);
                if (!menuSlowMotionApplied)
                {
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
        try
        {
            // Explicit reset is important on menu close, pause menu and script abort.
            Function.Call(Hash.SET_TIME_SCALE, 1.0f);
        }
        catch { }

        if (menuSlowMotionApplied)
        {
            menuSlowMotionApplied = false;
            Log("MENU SLOW MOTION OFF | TimeScale=1.00");
        }
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        try
        {
            try { if (Function.Call<bool>(Hash.IS_PAUSE_MENU_ACTIVE)) return; } catch { }
            if (e.KeyCode != volumeDownKey && e.KeyCode != volumeUpKey) return;
            if (volumeHoldActive && volumeHoldKey == e.KeyCode) return;

            volumeHoldActive = true;
            volumeHoldKey = e.KeyCode;
            volumeHoldNext = DateTime.Now.AddMilliseconds(320);
            HandleVolumeStep(e.KeyCode == volumeDownKey ? -1 : 1);
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
        HandleVolumeStep(volumeHoldKey == volumeDownKey ? -1 : 1);
        volumeHoldNext = DateTime.Now.AddMilliseconds(125);
    }

    private void HandleVolumeStep(int direction)
    {
        if (menuOpen && menuTab == 6) AdjustSelectedSetting(direction);
        else AdjustVolume(direction * volumeStep);
    }

    private void OnKeyUp(object sender, KeyEventArgs e)
    {
        try
        {
            try { if (Function.Call<bool>(Hash.IS_PAUSE_MENU_ACTIVE)) return; } catch { }
            if (e.KeyCode == volumeDownKey || e.KeyCode == volumeUpKey)
            {
                volumeHoldActive = false;
                volumeHoldKey = Keys.None;
                if (menuOpen && menuTab == 6) SaveAudioAndLanguageSettings();
                else SaveVolumeToIni();
                SaveUserSettings();
                return;
            }
            if (e.KeyCode == menuKey)
            {
                menuOpen = !menuOpen;
                menuIndex = index;
                UpdateMenuSlowMotion();
                if (!menuOpen) SaveUserSettings();
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

            if (e.KeyCode == toggleKey)
            {
                if (youtubeModeActive) ToggleYouTubeMusic();
                else
                {
                    enabled = !enabled;
                    MarkUserSettingsDirty();
                    if (!enabled) StopRadio(); else StartRadio();
                    ShowBanner(enabled ? T("RADIO_ON") : T("RADIO_OFF"), CurrentStationLine(), 2200);
                }
            }
            else if (e.KeyCode == nextKey)
            {
                if (youtubeModeActive) YouTubeNext(); else Change(1);
            }
            else if (e.KeyCode == prevKey)
            {
                if (youtubeModeActive) YouTubePrevious(); else Change(-1);
            }
            else if (e.KeyCode == stopKey)
            {
                if (youtubeModeActive) PauseYouTubeMusic();
                else
                {
                    StopRadio();
                    ShowBanner(T("STATE_STOPPED"), CurrentStationLine(), 1800);
                }
            }
            else if (e.KeyCode == reloadKey)
            {
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
            if (key == menuKey || key == Keys.Escape || key == Keys.Back)
            {
                menuOpen = false;
                RestoreGameTimeScale();
            }
            return;
        }

        // Im Menue wechseln Num4/Num6 die linken Reiter. Ausserhalb des Menues
        // bleiben sie wie gewohnt Sender zurueck/weiter.
        if (key == prevKey)
        {
            ChangeMenuTab(-1);
            return;
        }
        if (key == nextKey)
        {
            ChangeMenuTab(1);
            return;
        }

        if (key == menuUpKey)
        {
            if (menuTab == 2) MovePack(-1);
            else if (menuTab == 4) MoveSkin(-1);
            else if (menuTab == 5) YouTubePrevious();
            else if (menuTab == 6) MoveSettings(-1);
            else if (menuTab == 0) MoveMenu(-1);
            else if (menuTab == 1) MoveFilteredMenu(-1);
        }
        else if (key == menuDownKey)
        {
            if (menuTab == 2) MovePack(1);
            else if (menuTab == 4) MoveSkin(1);
            else if (menuTab == 5) YouTubeNext();
            else if (menuTab == 6) MoveSettings(1);
            else if (menuTab == 0) MoveMenu(1);
            else if (menuTab == 1) MoveFilteredMenu(1);
        }
        else if (key == selectKey || key == Keys.Enter)
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
                HandleYouTubeSelect();
            }
            else if (menuTab == 6)
            {
                ApplySelectedSetting();
            }
            else
            {
                ActivateInternetRadioFromMenu();
            }
        }
        else if (key == Keys.Space && (menuTab == 0 || menuTab == 1))
        {
            ToggleFavorite(menuIndex);
        }
        else if (key == Keys.Delete && (menuTab == 0 || menuTab == 1))
        {
            RemoveFavorite(menuIndex);
        }
        else if (key == stopKey)
        {
            if (menuTab == 5 || youtubeModeActive) PauseYouTubeMusic();
            else
            {
                StopRadio();
                ShowBanner(T("STATE_STOPPED"), CurrentStationLine(), 1800);
            }
        }
        else if (key == toggleKey)
        {
            if (menuTab == 5 || youtubeModeActive) ToggleYouTubeMusic();
            else
            {
                enabled = !enabled;
                MarkUserSettingsDirty();
                if (!enabled) StopRadio(); else StartRadio();
            }
        }
        else if (key == menuKey || key == Keys.Escape || key == Keys.Back)
        {
            menuOpen = false;
            RestoreGameTimeScale();
            SaveUserSettings();
        }
    }

    private void ChangeMenuTab(int delta)
    {
        menuTab += delta;
        while (menuTab < 0) menuTab += 7;
        while (menuTab >= 7) menuTab -= 7;

        if (menuTab == 0)
        {
            menuIndex = index;
        }
        else if (menuTab == 1)
        {
            EnsureFilteredMenuIndex();
        }
        else if (menuTab == 2)
        {
            SyncPackIndex();
        }
        else if (menuTab == 4)
        {
            skinMenuIndex = skinIndex;
        }
        else if (menuTab == 6)
        {
            settingsMenuIndex = Math.Max(0, Math.Min(6, settingsMenuIndex));
        }

        ShowBanner(T("TAB"), GetMenuTabName(), 1200);
    }

    private string GetMenuTabName()
    {
        if (menuTab == 1) return T("STATIONS");
        if (menuTab == 2) return T("PACKS");
        if (menuTab == 3) return T("INFO");
        if (menuTab == 4) return T("SKINS");
        if (menuTab == 5) return "YOUTUBE MUSIC";
        if (menuTab == 6) return T("SETTINGS");
        return "RADIO";
    }

    private void MoveSettings(int delta)
    {
        settingsMenuIndex += delta;
        while (settingsMenuIndex < 0) settingsMenuIndex += 7;
        while (settingsMenuIndex >= 7) settingsMenuIndex -= 7;
    }

    private void ApplySelectedSetting()
    {
        if (settingsMenuIndex == 0)
        {
            if (uiLanguage == "EN") uiLanguage = "DE";
            else if (uiLanguage == "DE") uiLanguage = "ES";
            else uiLanguage = "EN";
            SaveAudioAndLanguageSettings();
            SaveUserSettings();
            ShowBanner(T("LANGUAGE"), GetLanguageDisplayName(), 1800);
        }
        else
        {
            ShowBanner(T("SETTINGS"), T("USE_PLUS_MINUS"), 1800);
        }
    }

    private void AdjustSelectedSetting(int direction)
    {
        if (settingsMenuIndex == 0) return;
        if (settingsMenuIndex == 1)
            duckVolumePercent = Math.Max(20, Math.Min(100, duckVolumePercent + direction * 5));
        else if (settingsMenuIndex == 2)
            playerMaxVolume = Math.Max(20, Math.Min(100, playerMaxVolume + direction * 5));
        else if (settingsMenuIndex == 3)
            youtubeMaxVolume = Math.Max(5, Math.Min(100, youtubeMaxVolume + direction * 5));
        else if (settingsMenuIndex == 4)
            volumeStep = Math.Max(1, Math.Min(5, volumeStep + direction));
        else if (settingsMenuIndex == 5)
            duckReleaseMs = Math.Max(500, Math.Min(4000, duckReleaseMs + direction * 250));
        else if (settingsMenuIndex == 6)
            duckFadeOutMs = Math.Max(200, Math.Min(2000, duckFadeOutMs + direction * 100));

        appliedStreamVolume = -1;
        appliedYoutubeVolume = -1;
        appliedYoutubeMixerLevel = -1.0f;
        ApplyEffectiveVolume();
        MarkUserSettingsDirty();
    }

    private string GetLanguageDisplayName()
    {
        if (uiLanguage == "EN") return "English";
        if (uiLanguage == "ES") return "Español";
        return "Deutsch";
    }

    private static string NormalizeLanguage(string value)
    {
        string v = (value ?? "EN").Trim().ToUpperInvariant();
        if (v.StartsWith("EN")) return "EN";
        if (v.StartsWith("DE")) return "DE";
        if (v.StartsWith("ES")) return "ES";
        return "EN";
    }

    private bool IsYouTubePlaying()
    {
        return string.Equals(youtubeState, "Playing", StringComparison.OrdinalIgnoreCase);
    }

    private void ActivateInternetRadioFromMenu()
    {
        if (youtubeModeActive && youtube != null)
        {
            youtube.Pause();
            youtube.SetManagedActive(false);
            youtube.RestoreVolume();
            try { InternetRadioBrowserVolume.Restore(); } catch { }
        }
        youtubeModeActive = false;
        youtubeSelectionPending = false;
        youtubeMiniHideAt = DateTime.MinValue;
        appliedYoutubeVolume = -1;
        appliedYoutubeMixerLevel = -1.0f;
        duckActive = false;
        duckReason = "";
        duckMix = 0.0f;
        lastDuckRampAt = DateTime.Now;
        index = menuIndex;
        StartRadio();
        ShowBanner("Internet Radio", T("NOW_ACTIVE") + ": " + CurrentStationLine(), 1800);
    }

    private void ActivateYouTubeMode()
    {
        if (!youtubeModeActive)
        {
            // Exklusive Quellenumschaltung: Webradio wirklich stoppen, bevor YT Music uebernimmt.
            StopRadio();
            Game.RadioStation = RadioStation.RadioOff;
            youtubeModeActive = true;
            youtubeAutoActivationConsumed = true;
            // Wenn das Cover schon vor der Aktivierung bereit war, zeige die Mini-Anzeige noch 2 Sekunden.
            // Wenn noch kein Cover da ist, bleibt sie sichtbar, bis der Cover-Load erfolgreich war.
            youtubeMiniHideAt = youtubeCoverSprite != null ? DateTime.Now.AddSeconds(2) : DateTime.MinValue;
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

    private void HandleYouTubeSelect()
    {
        if (youtube == null)
        {
            ShowBanner("YouTube Music", T("BRIDGE_UNAVAILABLE"), 2200);
            return;
        }

        // V12.28: Num5 is an explicit foreground action.
        // Close our menu first so GTA releases slow-motion/focus, then let Windows open
        // YouTube Music visibly. The hidden PowerShell bridge only detects/controls media.
        menuOpen = false;
        RestoreGameTimeScale();
        SaveUserSettings();

        bool opened = OpenYouTubeMusicForeground();
        youtubeSelectionPending = true;
        youtube.BeginDetection();

        if (youtubeAvailable)
        {
            ActivateYouTubeMode();
            if (!IsYouTubePlaying()) youtube.Play();
        }

        ShowBanner("YouTube Music", opened ? T("YT_OPENED_SELECT") : T("YT_OPEN_FAILED"), 4200);
        Log("YT MENU FOREGROUND START | ShellOpen=" + opened + " | SessionAvailable=" + youtubeAvailable);
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
        if (youtube == null)
        {
            ShowBanner("YouTube Music", T("BRIDGE_UNAVAILABLE"), 2200);
            return;
        }
        if (!youtubeAvailable)
        {
            HandleYouTubeSelect();
            return;
        }

        ActivateYouTubeMode();
        youtubeSelectionPending = false;
        if (IsYouTubePlaying())
        {
            youtube.Pause();
            ShowBanner("YouTube Music", T("STATE_PAUSED"), 1600);
        }
        else
        {
            youtube.Play();
            ShowBanner("YouTube Music", T("STATE_PLAYING"), 1600);
        }
    }

    private void PauseYouTubeMusic()
    {
        if (youtube == null || !youtubeAvailable)
        {
            ShowBanner("YouTube Music", T("YT_START_HINT"), 1800);
            return;
        }
        ActivateYouTubeMode();
        youtube.Pause();
        ShowBanner("YouTube Music", T("STATE_PAUSED"), 1600);
    }

    private void YouTubeNext()
    {
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
        if (youtube == null || !youtubeAvailable)
        {
            ShowBanner("YouTube Music", T("YT_START_HINT"), 1800);
            return;
        }
        ActivateYouTubeMode();
        youtube.Previous();
        ShowBanner("YouTube Music", T("PREVIOUS_TRACK"), 1300);
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
        EnsureFilteredMenuIndex();
        menuTab = 1;
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

    private void MoveMenu(int delta)
    {
        if (stations.Count == 0) return;
        int count = stations.Count;
        do
        {
            menuIndex += delta;
            while (menuIndex < 0) menuIndex += count;
            while (menuIndex >= count) menuIndex -= count;
        }
        while (count > 1 && !stations[menuIndex].Enabled);
    }

    private bool IsCycleVehicle()
    {
        try
        {
            Ped player = Game.LocalPlayerPed;
            if (player == null || !player.IsInVehicle()) return false;
            int vehicleHandle = Function.Call<int>(Hash.GET_VEHICLE_PED_IS_IN, player.Handle, false);
            if (vehicleHandle == 0) return false;
            // GTA vehicle class 13 = Cycles (BMX, bicycle, cruiser, mountain/race bike).
            int vehicleClass = Function.Call<int>(Hash.GET_VEHICLE_CLASS, vehicleHandle);
            return vehicleClass == 13;
        }
        catch { return false; }
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

    private void StartRadio()
    {
        if (youtubeModeActive)
        {
            // Sicherheitsgurt gegen Doppel-Audio: YT Music hat Prioritaet.
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
        int startUserVolume = duckActive ? (int)Math.Round(volume * (duckVolumePercent / 100.0)) : volume;
        startUserVolume = Math.Max(0, Math.Min(100, startUserVolume));
        int startPlayerVolume = MapUserVolumeToPlayer(startUserVolume);
        appliedStreamVolume = startPlayerVolume;
        if (audio != null) audio.Play(s.Url, startPlayerVolume);
        Log("VOLUME MAP | UI=" + startUserVolume + "% -> Player=" + startPlayerVolume + "%");
        ShowBanner(s.Name, StationDetailsLine(s), 2600);
        Log("PLAY " + s.Name + " | " + s.Url);
    }

    private void CheckRadioStreamHealth()
    {
        if (!autoSkipUnavailable || !shouldPlay || youtubeModeActive || audio == null) return;
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
        // V12.24: YouTube Music is intentionally user-controlled.
        // Legacy AutoLaunch/AutoPlay behavior is ignored for a predictable UX.
        autoLaunchYouTubeMusic = false;
        autoActivateYouTubeMusic = false;
        autoPlayYouTubeMusic = false;
        volumeStep = Math.Max(1, Math.Min(5, GetInt(ini, "General", "VolumeStep", 2)));
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
            duckVolumePercent = 70;
            SaveGeneralValue("AudioProfileVersion", "4");
            SaveGeneralValue("PlayerMaxVolume", playerMaxVolume.ToString());
            SaveGeneralValue("VolumeCurveExponent", volumeCurveExponent.ToString(System.Globalization.CultureInfo.InvariantCulture));
            SaveGeneralValue("YouTubeMaxVolume", youtubeMaxVolume.ToString());
            SaveGeneralValue("YouTubeVolumeCurveExponent", youtubeVolumeCurveExponent.ToString(System.Globalization.CultureInfo.InvariantCulture));
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
        index = Math.Max(0, Math.Min(stations.Count - 1, start - 1));
        while (stations.Count > 0 && !stations[index].Enabled) index = (index + 1) % stations.Count;
        menuIndex = index;
        RebuildMenuPacks();
        Log("INI loaded | Stations=" + stations.Count + " | Start=" + index + " | Categories=" + Math.Max(0, menuPacks.Count - 2) + " | Favorites=" + favoriteStationNames.Count);
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
                if (dynamicMenuColorInitialized)
                {
                    r = ClampColor((int)Math.Round(dynamicMenuR));
                    g = ClampColor((int)Math.Round(dynamicMenuG));
                    b = ClampColor((int)Math.Round(dynamicMenuB));
                }
                else { r = 232; g = 92; b = 220; }
                break;                                      // Spectrum
            case 6: r = 255; g = 124; b = 196; break;     // GTA Vice City
            case 7: r = 126; g = 201; b = 71; break;      // GTA San Andreas
            case 8: r = 0; g = 229; b = 255; break;       // Cyberpunk: cyan HUD selection
            case 9: r = 157; g = 231; b = 67; break;      // NFSU2: acid green selection
            case 10: r = 109; g = 182; b = 72; break;     // Minecraft grass accent
            case 11: r = 212; g = 178; b = 98; break;     // Gangster Luxe champagne gold
            case 12: r = 98; g = 190; b = 184; break;      // Sakura Zen jade
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
            case 12: r = 10; g = 16; b = 17; break;
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
            case 12: r = 20; g = 30; b = 31; break;
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
            case 12: r = 12; g = 27; b = 29; break;
            default: r = 2; g = 24; b = 12; break;
        }
    }

    // V12.44: secondary accent makes the premium themes visually distinct instead
    // of being one shared layout with only a different primary color.
    private void GetSkinSecondaryAccent(out int r, out int g, out int b)
    {
        switch (skinIndex)
        {
            case 6: r = 57; g = 221; b = 255; break;      // Vice City: cyan opposite the sunset pink
            case 7: r = 214; g = 174; b = 76; break;     // San Andreas: aged gold / warm street tone
            case 8: r = 228; g = 48; b = 47; break;      // Cyberpunk: red interface rails against cyan
            case 9: r = 198; g = 204; b = 207; break;    // NFSU2: brushed chrome against acid green
            case 10: r = 126; g = 88; b = 56; break;      // Minecraft dirt accent
            case 11: r = 104; g = 18; b = 30; break;      // Gangster Luxe: deep bordeaux accent against gold
            case 12: r = 235; g = 126; b = 157; break;    // Sakura Zen blossom pink
            default: GetSkinAccent(out r, out g, out b); break;
        }
    }

    private void GetThemeFrameAccent(out int r, out int g, out int b)
    {
        switch (skinIndex)
        {
            case 9: r = 198; g = 204; b = 207; break;     // NFSU2 uses a chrome outer frame
            case 10: r = 95; g = 140; b = 68; break;      // Minecraft keeps a softer grass-border tone
            case 12: r = 222; g = 217; b = 198; break;    // Sakura Zen benefits from a calm ivory frame
            default: GetSkinAccent(out r, out g, out b); break;
        }
    }

    private void GetThemeFrameSecondary(out int r, out int g, out int b)
    {
        switch (skinIndex)
        {
            case 6: r = 57; g = 221; b = 255; break;
            case 7: r = 214; g = 174; b = 76; break;
            case 8: r = 228; g = 48; b = 47; break;
            case 9: r = 157; g = 231; b = 67; break;
            case 10: r = 126; g = 88; b = 56; break;
            case 11: r = 104; g = 18; b = 30; break;
            case 12: r = 235; g = 126; b = 157; break;
            default: GetSkinAccent(out r, out g, out b); break;
        }
    }

    private bool IsPremiumThemeSkin()
    {
        return skinIndex >= 6 && skinIndex <= 12;
    }

    private void LoadMinecraftGrassBlockSprite()
    {
        try
        {
            string path = Path.Combine(dir, "MinecraftGrassBlock.png");
            if (!File.Exists(path))
            {
                minecraftGrassBlockSprite = null;
                Log("MINECRAFT BLOCK ASSET FEHLT | " + path);
                return;
            }
            minecraftGrassBlockSprite = new GTA.UI.CustomSprite(
                path,
                new System.Drawing.SizeF(128f, 128f),
                new System.Drawing.PointF(0f, 0f),
                System.Drawing.Color.White);
            Log("MINECRAFT BLOCK ASSET READY | " + path);
        }
        catch (Exception ex)
        {
            minecraftGrassBlockSprite = null;
            Log("MINECRAFT BLOCK ASSET FEHLER | " + ex.Message);
        }
    }

    private bool DrawMinecraftGrassBlockSprite(float left, float top, float width, float height)
    {
        if (minecraftGrassBlockSprite == null) return false;
        try
        {
            minecraftGrassBlockSprite.Size = new System.Drawing.SizeF(width * GTA.UI.Screen.Width, height * GTA.UI.Screen.Height);
            minecraftGrassBlockSprite.Position = new System.Drawing.PointF(left * GTA.UI.Screen.Width, top * GTA.UI.Screen.Height);
            minecraftGrassBlockSprite.Draw();
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

    private void DrawMinecraftGrassBlockIcon(float left, float top, float size, int alpha)
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

        if (skinIndex == 6)
        {
            // V12.49 Vice City: cleaner Miami look with restrained sunset rails.
            DrawRect(left + width * 0.26f, top + 0.010f, width * 0.26f, 0.0018f, ar, ag, ab, 210);
            DrawRect(left + width * 0.77f, top + 0.010f, width * 0.18f, 0.0018f, sr, sg, sb, 210);
            DrawRect(left + 0.012f, top + height * 0.28f, 0.0016f, height * 0.11f, ar, ag, ab, 135);
            DrawRect(left + width - 0.012f, top + height * 0.72f, 0.0016f, height * 0.12f, sr, sg, sb, 135);
        }
        else if (skinIndex == 7)
        {
            // V12.49 San Andreas: clean street look, green/gold rails without muddy overlays.
            DrawRect(left + width * 0.50f, top + 0.010f, width * 0.86f, 0.0018f, ar, ag, ab, 175);
            DrawRect(left + width * 0.78f, top + height - 0.010f, width * 0.18f, 0.0018f, sr, sg, sb, 185);
            DrawRect(left + 0.012f, top + height * 0.54f, 0.0018f, height * 0.24f, ar, ag, ab, 138);
        }
        else if (skinIndex == 8)
        {
            // V12.49 Cyberpunk: cleaner black tech surface with red scan rails and cyan UI.
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
            // V12.49 NFS Underground 2: chrome/acid-green frame with restrained tuner streaks.
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
            // V12.49 Minecraft: restrained backdrop only; the visible grass block is drawn in the header layer.
            DrawRect(left + width * 0.56f, top + 0.010f, width * 0.66f, 0.0014f, ar, ag, ab, 165);
            DrawRect(left + width * 0.88f, top + height - 0.010f, width * 0.08f, 0.0014f, sr, sg, sb, 150);
        }
        else if (skinIndex == 11)
        {
            // V12.54 Gangster Luxe: premium noir, clean gold rails with subtle bordeaux tailoring.
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
        if (skinIndex == 6)
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
        if (!IsPremiumThemeSkin()) return;
        int ar, ag, ab; GetSkinAccent(out ar, out ag, out ab);
        int sr, sg, sb; GetSkinSecondaryAccent(out sr, out sg, out sb);
        if (skinIndex == 6)
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

    public void RenderUi()

    {
        try
        {
            bool pause = false;
            try { pause = Function.Call<bool>(Hash.IS_PAUSE_MENU_ACTIVE); } catch { }
            if (pause) return;
            DrawMiniUi();
            if (menuOpen) DrawMenu();
            DrawBanner();
        }
        catch { }
    }

    private void DrawMiniUi()
    {
        if (!showMiniUi) return;
        if (!enabled && !youtubeModeActive && !menuOpen) return;
        if (menuOpen && hideMiniUiWhileMenuOpen) return;
        bool visible;
        if (youtubeModeActive)
        {
            // V12.12: YT-Mini bleibt sichtbar, bis ein Cover erfolgreich geladen wurde.
            // Ab dem erfolgreichen Cover-Load laeuft exakt ein 2-Sekunden-Timer; danach schliesst die Mini-Anzeige.
            visible = youtubeCoverSprite == null || youtubeMiniHideAt == DateTime.MinValue || DateTime.Now < youtubeMiniHideAt;
        }
        else
        {
            visible = (persistentMiniUi && shouldPlay) || DateTime.Now < bannerUntil.AddSeconds(2);
        }
        if (!visible) return;

        Station s = Current();
        string station = youtubeModeActive ? "YOUTUBE MUSIC" : (s != null ? s.Name : T("NO_STATION"));
        string detail = youtubeModeActive ? (string.IsNullOrEmpty(youtubeArtist) ? T("MEDIA_SESSION") : youtubeArtist) : (s != null ? StationDetailsLine(s) : T("NO_ACTIVE_STATION"));
        string state = youtubeModeActive ? LocalizeYouTubeState() : (enabled ? MapState(cachedAudioState) : T("STATE_OFF"));
        string nowPlaying = youtubeModeActive ? GetYouTubeNowPlayingLine() : GetNowPlayingLine();

        float left = miniUiLeft;
        float top = miniUiTop;
        float width = Math.Max(miniUiWidth, 0.265f);
        float height = string.IsNullOrEmpty(nowPlaying) ? 0.118f : 0.136f;
        int ar, ag, ab;
        GetSkinAccent(out ar, out ag, out ab);
        int br, bg, bb; GetSkinBase(out br, out bg, out bb);
        int or, og, ob; GetSkinOled(out or, out og, out ob);

        // V12.41: Spectrum affects the mini UI too. Use the current station's exact
        // genre/category color so the mini display and the large menu stay consistent.
        if (IsSpectrumSkin())
        {
            int tr, tg, tb;
            if (youtubeModeActive) { tr = 224; tg = 52; tb = 68; }
            else if (s != null) GetCategoryColor(s.Pack ?? "", out tr, out tg, out tb);
            else GetSkinAccent(out tr, out tg, out tb);

            if (!dynamicMenuColorInitialized)
            {
                dynamicMenuR = tr; dynamicMenuG = tg; dynamicMenuB = tb;
                dynamicMenuColorInitialized = true;
            }
            else
            {
                const float miniFade = 0.12f;
                dynamicMenuR += (tr - dynamicMenuR) * miniFade;
                dynamicMenuG += (tg - dynamicMenuG) * miniFade;
                dynamicMenuB += (tb - dynamicMenuB) * miniFade;
            }

            ar = ClampColor((int)Math.Round(dynamicMenuR));
            ag = ClampColor((int)Math.Round(dynamicMenuG));
            ab = ClampColor((int)Math.Round(dynamicMenuB));
            float strength = dynamicRadioBackgroundStrength;
            br = MixColor(br, ar, 0.48f * strength);
            bg = MixColor(bg, ag, 0.48f * strength);
            bb = MixColor(bb, ab, 0.48f * strength);
            or = MixColor(or, ar, 0.31f * strength);
            og = MixColor(og, ag, 0.31f * strength);
            ob = MixColor(ob, ab, 0.31f * strength);
        }

        // Modernes Infotainment im gewaehlten Skin.
        DrawRect(left + width / 2f, top + height / 2f, width, height, br, bg, bb, 242);
        if (skinIndex == 10) DrawFrame(left, top, width, height, 72, 80, 72, 135, 0.0010f);
        else DrawFrame(left, top, width, height, IsPremiumThemeSkin() ? ar : 88, IsPremiumThemeSkin() ? ag : 96, IsPremiumThemeSkin() ? ab : 106, IsPremiumThemeSkin() ? 130 : 220, 0.0012f);
        DrawPremiumMiniDecor(left, top, width, height);
        float miniHeaderTint = IsSpectrumSkin() ? 0.18f : (IsPremiumThemeSkin() ? 0.14f : 0.0f);
        DrawRect(left + width / 2f, top + 0.016f, width - 0.010f, 0.028f, MixColor(13, ar, miniHeaderTint), MixColor(17, ag, miniHeaderTint), MixColor(22, ab, miniHeaderTint), 246);
        DrawText("LOS SANTOS INTERNET RADIO", left + 0.010f, top + 0.006f, 0.205f, 0, 244, 246, 249, 238);
        DrawText("made by st3v3nblub", left + 0.165f, top + 0.008f, 0.135f, 0, ar, ag, ab, 225);

        float oledLeft = left + 0.010f;
        float oledTop = top + 0.036f;
        float oledWidth = width - 0.020f;
        float oledHeight = string.IsNullOrEmpty(nowPlaying) ? 0.060f : 0.078f;
        DrawRect(oledLeft + oledWidth / 2f, oledTop + oledHeight / 2f, oledWidth, oledHeight, or, og, ob, 248);
        DrawFrame(oledLeft, oledTop, oledWidth, oledHeight, ar, ag, ab, 145, 0.0010f);

        // V12.9: Das bereits validierte YT-Cover auch in der kleinen Anzeige zeigen.
        // Hier wird nur gezeichnet; es gibt keinerlei Datei-/Streamzugriff im Frame-Renderer.
        float miniTextX = oledLeft + 0.009f;
        bool miniCoverDrawn = false;
        if (youtubeModeActive && youtubeCoverSprite != null)
        {
            float miniCoverH = Math.Max(0.040f, oledHeight - 0.012f);
            float screenAspectFix = (float)GTA.UI.Screen.Height / Math.Max(1f, (float)GTA.UI.Screen.Width);
            float miniCoverW = miniCoverH * screenAspectFix;
            float miniCoverLeft = oledLeft + 0.006f;
            float miniCoverTop = oledTop + 0.006f;
            DrawRect(miniCoverLeft + miniCoverW / 2f, miniCoverTop + miniCoverH / 2f, miniCoverW + 0.003f, miniCoverH + 0.003f, 9, 12, 16, 245);
            miniCoverDrawn = DrawYouTubeCover(miniCoverLeft, miniCoverTop, miniCoverW, miniCoverH);
            if (miniCoverDrawn)
            {
                DrawFrame(miniCoverLeft, miniCoverTop, miniCoverW, miniCoverH, ar, ag, ab, 150, 0.0010f);
                miniTextX = miniCoverLeft + miniCoverW + 0.009f;
            }
        }

        DrawText(Shorten(station, miniCoverDrawn ? 22 : 31), miniTextX, oledTop + 0.010f, miniCoverDrawn ? 0.285f : 0.330f, 0, 235, 255, 238, 248);
        DrawText(youtubeModeActive ? "YT MUSIC" : "ON AIR", oledLeft + oledWidth - 0.066f, oledTop + 0.012f, 0.155f, 0, ar, ag, ab, (youtubeModeActive ? youtubeAvailable : shouldPlay) ? 240 : 110);
        if (!string.IsNullOrEmpty(nowPlaying))
            DrawText(Shorten(nowPlaying, miniCoverDrawn ? 34 : 46), miniTextX, oledTop + 0.038f, 0.210f, 0, 178, 236, 189, 232);
        else
            DrawText(Shorten(detail, miniCoverDrawn ? 34 : 46), miniTextX, oledTop + 0.038f, 0.200f, 0, 164, 213, 174, 222);

        float footerY = top + height - 0.015f;
        DrawText("NUM0 " + T("MENU"), left + 0.012f, footerY - 0.008f, 0.155f, 0, 184, 188, 196, 218);
        DrawText("4/6 " + T("SEEK"), left + 0.082f, footerY - 0.008f, 0.155f, 0, 184, 188, 196, 218);
        DrawText("-/+ " + T("VOLUME_SHORT"), left + 0.144f, footerY - 0.008f, 0.155f, 0, 184, 188, 196, 218);
        DrawText(youtubeModeActive ? "MEDIA" : (volume + "%"), left + width - 0.050f, footerY - 0.008f, 0.165f, 0, 245, 247, 249, 232);
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
            // V12.42: Spectrum follows YouTube Music red whenever YT is the active
            // audio source, not only while the YT MUSIC tab itself is open.
            r = 224; g = 52; b = 68; // YouTube Music / media red
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

        int tr, tg, tb;
        GetRadioThemeTarget(out tr, out tg, out tb);
        if (!dynamicMenuColorInitialized)
        {
            dynamicMenuR = tr; dynamicMenuG = tg; dynamicMenuB = tb;
            dynamicMenuColorInitialized = true;
        }
        else
        {
            // Smooth, frame-based transition. 0.11 feels close to GTA's quick station-color crossfade.
            const float fade = 0.11f;
            dynamicMenuR += (tr - dynamicMenuR) * fade;
            dynamicMenuG += (tg - dynamicMenuG) * fade;
            dynamicMenuB += (tb - dynamicMenuB) * fade;
        }

        int dr = ClampColor((int)Math.Round(dynamicMenuR));
        int dg = ClampColor((int)Math.Round(dynamicMenuG));
        int db = ClampColor((int)Math.Round(dynamicMenuB));
        float strength = dynamicRadioBackgroundStrength;

        // Keep dark surfaces readable while making the selected station unmistakable.
        br = MixColor(br, dr, 0.46f * strength);
        bg = MixColor(bg, dg, 0.46f * strength);
        bb = MixColor(bb, db, 0.46f * strength);
        pr = MixColor(pr, dr, 0.34f * strength);
        pg = MixColor(pg, dg, 0.34f * strength);
        pb = MixColor(pb, db, 0.34f * strength);
        ar = MixColor(ar, dr, 0.88f * strength);
        ag = MixColor(ag, dg, 0.88f * strength);
        ab = MixColor(ab, db, 0.88f * strength);
    }

    private void DrawDynamicRadioBackdrop(float left, float top, float width, float height)
    {
        if (!IsSpectrumSkin()) return;
        int r = ClampColor((int)Math.Round(dynamicMenuR));
        int g = ClampColor((int)Math.Round(dynamicMenuG));
        int b = ClampColor((int)Math.Round(dynamicMenuB));

        // Layered translucent bands create the bold station-card feeling of GTA's radio UI.
        DrawRect(left + width * 0.50f, top + height * 0.21f, width * 0.985f, height * 0.28f, r, g, b, 34);
        DrawRect(left + width * 0.50f, top + height * 0.52f, width * 0.985f, height * 0.34f, ClampColor(r - 24), ClampColor(g - 24), ClampColor(b - 24), 29);
        DrawRect(left + width * 0.50f, top + height * 0.82f, width * 0.985f, height * 0.20f, r, g, b, 20);
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
        DrawFrame(left, top, width, height, outerR, outerG, outerB, skinIndex == 10 ? 118 : (IsPremiumThemeSkin() ? 155 : 242), 0.0019f);
        DrawFrame(left + 0.004f, top + 0.004f, width - 0.008f, height - 0.008f, 27, 32, 39, 245, 0.0011f);

        float headerH = 0.066f;
        DrawRect(left + width / 2f, top + headerH / 2f, width - 0.010f, headerH - 0.006f, pr, pg, pb, 249);
        DrawRect(left + width / 2f, top + headerH - 0.003f, width - 0.025f, 0.0015f, 63, 72, 82, 195);
        DrawPremiumHeaderDecor(left + 0.005f, top + 0.003f, width - 0.010f, headerH - 0.006f);
        float headerTextX = left + (skinIndex == 10 ? 0.061f : 0.030f);
        if (skinIndex == 10)
        {
            if (!DrawMinecraftGrassBlockSprite(left + 0.008f, top + 0.005f, 0.035f, 0.058f))
                DrawMinecraftGrassBlockIcon(left + 0.009f, top + 0.008f, 0.030f, 235);
        }
        else if (skinIndex == 12)
        {
            DrawSakuraZenMotifSprite(left + width * 0.52f, top + 0.0035f, width * 0.36f, 0.058f);
        }
        DrawText("LOS SANTOS INTERNET RADIO", headerTextX, top + 0.012f, 0.395f, 0, 247, 249, 251, 250);
        DrawText("made by st3v3nblub", left + 0.293f, top + 0.021f, 0.160f, 0, ar, ag, ab, 235);
        DrawText(T("WORLDWIDE_RADIO"), headerTextX + 0.001f, top + 0.043f, 0.170f, 0, 151, 159, 170, 226);
        DrawText("LS", left + width - 0.082f, top + 0.013f, 0.390f, 0, 228, 231, 235, 238);

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
        DrawFrame(navLeft, bodyTop, navWidth, bodyH, panelFr, panelFg, panelFb, 225, 0.0012f);
        DrawNavItem(navLeft + 0.007f, bodyTop + 0.020f, navWidth - 0.014f, "RADIO", menuTab == 0, ar, ag, ab);
        DrawNavItem(navLeft + 0.007f, bodyTop + 0.075f, navWidth - 0.014f, T("STATIONS"), menuTab == 1, ar, ag, ab);
        DrawNavItem(navLeft + 0.007f, bodyTop + 0.130f, navWidth - 0.014f, T("PACKS"), menuTab == 2, ar, ag, ab);
        DrawNavItem(navLeft + 0.007f, bodyTop + 0.185f, navWidth - 0.014f, T("INFO"), menuTab == 3, ar, ag, ab);
        DrawNavItem(navLeft + 0.007f, bodyTop + 0.240f, navWidth - 0.014f, T("SKINS"), menuTab == 4, ar, ag, ab);
        DrawNavItem(navLeft + 0.007f, bodyTop + 0.295f, navWidth - 0.014f, "YT MUSIC", menuTab == 5, 235, 72, 88);
        DrawNavItem(navLeft + 0.007f, bodyTop + 0.350f, navWidth - 0.014f, T("SETTINGS_NAV"), menuTab == 6, ar, ag, ab);
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
        DrawFrame(navHelpLeft, navHelpTop, navHelpWidth, navHelpHeight, navHelpFr, navHelpFg, navHelpFb, skinIndex == 10 ? 82 : (IsPremiumThemeSkin() ? 105 : 95), 0.0008f);
        DrawText("NUM4 / NUM6", navHelpLeft + 0.012f, navHelpTop + 0.015f, 0.180f, 0, ar, ag, ab, 235);
        DrawText(T("CHANGE_TAB"), navHelpLeft + 0.012f, navHelpTop + 0.041f, 0.158f, 0, 191, 196, 204, 222);
        DrawText("NUM0  " + T("EXIT"), navHelpLeft + 0.012f, navHelpTop + 0.067f, 0.158f, 0, 191, 196, 204, 222);

        float rightWidth = 0.145f;
        float rightLeft = left + width - rightWidth - 0.012f;
        DrawRect(rightLeft + rightWidth / 2f, bodyTop + bodyH / 2f, rightWidth, bodyH, pr, pg, pb, 248);
        DrawFrame(rightLeft, bodyTop, rightWidth, bodyH, panelFr, panelFg, panelFb, 225, 0.0012f);
        if (menuTab == 5)
        {
            DrawYouTubeSidePanel(rightLeft + 0.014f, bodyTop + 0.018f, rightWidth - 0.028f, ar, ag, ab);
        }
        else if (menuTab == 6)
        {
            DrawSettingsSidePanel(rightLeft + 0.014f, bodyTop + 0.018f, rightWidth - 0.028f, ar, ag, ab);
        }
        else
        {
            DrawVolumeWidget(rightLeft + 0.014f, bodyTop + 0.018f, rightWidth - 0.028f, 0.145f, volume / 100.0f, ar, ag, ab);
            DrawControlButton(rightLeft + 0.014f, bodyTop + 0.190f, rightWidth - 0.028f, "NUM-   " + T("QUIETER"), false, ar, ag, ab);
            DrawControlButton(rightLeft + 0.014f, bodyTop + 0.247f, rightWidth - 0.028f, "NUM+   " + T("LOUDER"), false, ar, ag, ab);
            DrawControlButton(rightLeft + 0.014f, bodyTop + 0.304f, rightWidth - 0.028f, "NUM1   " + T("POWER"), enabled, ar, ag, ab);
            DrawControlButton(rightLeft + 0.014f, bodyTop + 0.361f, rightWidth - 0.028f, "NUM3   " + T("STOP"), false, ar, ag, ab);
        }
        if (duckActive)
        {
            int dfr = 226, dfg = 180, dfb = 72;
            if (IsPremiumThemeSkin()) GetThemeFrameSecondary(out dfr, out dfg, out dfb);
            DrawRect(rightLeft + rightWidth / 2f, bodyBottom - 0.055f, rightWidth - 0.028f, 0.052f, MixColor(14, dfr, 0.18f), MixColor(12, dfg, 0.15f), MixColor(10, dfb, 0.12f), 238);
            DrawFrame(rightLeft + 0.014f, bodyBottom - 0.081f, rightWidth - 0.028f, 0.052f, dfr, dfg, dfb, 176, 0.001f);
            DrawText(T("DUCKING") + ": " + LocalizeDuckReason(), rightLeft + 0.020f, bodyBottom - 0.071f, 0.165f, 0, 248, 232, 210, 238);
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
            DrawSettingsTab(centerLeft, bodyTop, centerWidth, bodyBottom, ar, ag, ab);
        else
            DrawRadioTab(centerLeft, bodyTop, centerWidth, bodyBottom, ar, ag, ab);

        if (IsPremiumThemeSkin())
        {
            int cfr, cfg, cfb; GetThemeFrameAccent(out cfr, out cfg, out cfb);
            int csr, csg, csb; GetThemeFrameSecondary(out csr, out csg, out csb);
            DrawFrame(centerLeft, bodyTop, centerWidth, bodyBottom - bodyTop, cfr, cfg, cfb, skinIndex == 10 ? 86 : 92, 0.0008f);
            if (skinIndex == 8) DrawRect(centerLeft + centerWidth * 0.72f, bodyTop + 0.003f, centerWidth * 0.26f, 0.0011f, csr, csg, csb, 205);
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
        if (IsSpectrumSkin() && youtubeModeActive)
        {
            // V12.42: Keep the lower action area synchronized with active YouTube Music.
            far = 224; fag = 52; fab = 68;
        }
        else if (menuTab == 2 && packIndex >= 0 && packIndex < menuPacks.Count)
            GetCategoryColor(menuPacks[packIndex], out far, out fag, out fab);
        else if ((menuTab == 0 || menuTab == 1) && menuIndex >= 0 && menuIndex < stations.Count)
            GetCategoryColor(stations[menuIndex].Pack ?? "", out far, out fag, out fab);
        bool footerUseTheme = menuTab <= 2 || (IsSpectrumSkin() && (menuTab == 5 || youtubeModeActive));
        DrawRect(left + width / 2f, footerTop + footerH / 2f, width - 0.010f, footerH - 0.006f, 12, 17, 23, 248);
        int footerFrameR = far, footerFrameG = fag, footerFrameB = fab;
        if (IsPremiumThemeSkin()) GetThemeFrameAccent(out footerFrameR, out footerFrameG, out footerFrameB);
        DrawFrame(left + 0.005f, footerTop + 0.003f, width - 0.010f, footerH - 0.006f, footerFrameR, footerFrameG, footerFrameB, (footerUseTheme || IsPremiumThemeSkin() ? 125 : 75), 0.0009f);
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
        DrawText(menuTab == 5 ? "NUM8/2  " + T("TRACK_NAV") : (menuTab == 6 ? "NUM8/2  " + T("SETTING_SHORT") : "NUM8/2  " + T("NAV")), left + 0.180f, footerTop + 0.0115f, 0.166f, 0, footerUseTheme ? far : 207, footerUseTheme ? fag : 212, footerUseTheme ? fab : 220, 230);
        DrawText(menuTab == 5 ? "NUM5  " + T("OPEN_YT") : (menuTab == 6 ? "NUM5  " + T("APPLY") : "NUM5  " + T("SELECT")), left + 0.305f, footerTop + 0.0115f, 0.166f, 0, footerUseTheme ? far : 207, footerUseTheme ? fag : 212, footerUseTheme ? fab : 220, 230);
        DrawText(menuTab == 6 ? "NUM-/+  " + T("VALUE") : "NUM-/+  " + T("VOLUME_SHORT"), left + 0.485f, footerTop + 0.0115f, 0.166f, 0, footerUseTheme ? far : 207, footerUseTheme ? fag : 212, footerUseTheme ? fab : 220, 230);
        DrawText("NUM1  " + T("POWER"), left + 0.610f, footerTop + 0.0115f, 0.166f, 0, footerUseTheme ? far : 207, footerUseTheme ? fag : 212, footerUseTheme ? fab : 220, 230);
        DrawText("NUM0  " + T("EXIT"), left + width - 0.108f, footerTop + 0.0115f, 0.166f, 0, far, fag, fab, 235);
    }

    private void DrawRadioTab(float left, float top, float width, float bottom, int ar, int ag, int ab)
    {
        int or, og, ob; GetSkinOled(out or, out og, out ob);
        float oledHeight = 0.144f;
        DrawRect(left + width / 2f, top + oledHeight / 2f, width, oledHeight, or, og, ob, 252);
        DrawFrame(left, top, width, oledHeight, ar, ag, ab, 170, 0.0014f);
        Station current = Current();
        string np = GetNowPlayingLine();
        float textLeft = left + 0.120f;
        if (current != null) DrawStationLogoBadge(left + 0.014f, top + 0.018f, 0.090f, 0.100f, current, ar, ag, ab);
        DrawText(Shorten(current != null ? current.Name : T("NO_STATION"), 34), textLeft, top + 0.018f, 0.390f, 0, 237, 255, 239, 250);
        if (shouldPlay) DrawText("ON AIR", textLeft, top + 0.048f, 0.155f, 0, ar, ag, ab, 245);
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
        DrawText("SPACE = " + T("FAVORITE_SHORT"), left + width - 0.205f, top + 0.045f, 0.158f, 0, 174, 181, 191, 215);

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
        if (string.Equals(pack, "HIP-HOP / URBAN", StringComparison.OrdinalIgnoreCase)) return "HIP-HOP / URBAN";
        if (string.Equals(pack, "OLD SCHOOL HIP-HOP / RNB", StringComparison.OrdinalIgnoreCase)) return es ? "OLD SCHOOL HIP-HOP / R&B" : "OLD SCHOOL HIP-HOP / R&B";
        if (string.Equals(pack, "POP / CHARTS", StringComparison.OrdinalIgnoreCase)) return es ? "POP / ÉXITOS" : (en ? "POP / CHARTS" : "POP / CHARTS");
        if (string.Equals(pack, "ROCK / INDIE / ALTERNATIVE", StringComparison.OrdinalIgnoreCase)) return es ? "ROCK / INDIE / ALTERNATIVO" : "ROCK / INDIE / ALTERNATIVE";
        if (string.Equals(pack, "ELECTRONIC / CLUB", StringComparison.OrdinalIgnoreCase)) return es ? "ELECTRÓNICA / CLUB" : (en ? "ELECTRONIC / CLUB" : "ELEKTRONIK / CLUB");
        if (string.Equals(pack, "SYNTHWAVE / NIGHT DRIVE", StringComparison.OrdinalIgnoreCase)) return es ? "SYNTHWAVE / NOCHE" : (en ? "SYNTHWAVE / NIGHT DRIVE" : "SYNTHWAVE / NACHTFAHRT");
        if (string.Equals(pack, "COUNTRY / AMERICANA", StringComparison.OrdinalIgnoreCase)) return "COUNTRY / AMERICANA";
        if (string.Equals(pack, "SOUL / FUNK / REGGAE / WORLD", StringComparison.OrdinalIgnoreCase)) return "SOUL / FUNK / REGGAE / WORLD";
        if (string.Equals(pack, "JAZZ / ECLECTIC", StringComparison.OrdinalIgnoreCase)) return es ? "JAZZ / ECLÉCTICO" : (en ? "JAZZ / ECLECTIC" : "JAZZ / EKLEKTISCH");
        if (string.Equals(pack, "NEWS / TALK", StringComparison.OrdinalIgnoreCase)) return es ? "NOTICIAS / TALK" : (en ? "NEWS / TALK" : "NACHRICHTEN / TALK");
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
            else if (i == 5) { sr = 232; sg = 92; sb = 220; }
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
                float bandW = chipW / 3f;
                DrawRect(chipLeft + bandW * 0.5f, rowCenterY, bandW, chipH, 245, 170, 44, 238);
                DrawRect(chipLeft + bandW * 1.5f, rowCenterY, bandW, chipH, 49, 190, 226, 238);
                DrawRect(chipLeft + bandW * 2.5f, rowCenterY, bandW, chipH, 232, 92, 220, 238);
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

    private void DrawInfoTab(float left, float top, float width, float bottom, int ar, int ag, int ab)
    {
        Station current = Current();
        DrawRect(left + width / 2f, top + (bottom - top) / 2f, width, bottom - top, 10, 15, 20, 248);
        DrawFrame(left, top, width, bottom - top, 45, 54, 64, 220, 0.0011f);
        DrawText(T("INFO"), left + 0.020f, top + 0.018f, 0.360f, 0, 244, 247, 250, 245);
        if (current != null)
        {
            DrawStationLogoBadge(left + 0.020f, top + 0.072f, 0.120f, 0.100f, current, ar, ag, ab);
            DrawText(Shorten(current.Name, 40), left + 0.160f, top + 0.074f, 0.360f, 0, 244, 247, 250, 245);
            string np = GetNowPlayingLine();
            DrawText(Shorten(string.IsNullOrEmpty(np) ? T("NO_METADATA") : np, 62), left + 0.160f, top + 0.112f, 0.230f, 0, 180, 239, 190, 230);
            DrawInfoRow(left + 0.022f, top + 0.205f, width - 0.044f, T("STATUS"), MapState(cachedAudioState), ar, ag, ab);
            DrawInfoRow(left + 0.022f, top + 0.255f, width - 0.044f, T("PACK"), DisplayPackName(current.Pack), ar, ag, ab);
            DrawInfoRow(left + 0.022f, top + 0.305f, width - 0.044f, T("GENRE"), current.Genre ?? "-", ar, ag, ab);
            DrawInfoRow(left + 0.022f, top + 0.355f, width - 0.044f, T("VIBE"), current.Vibe ?? "-", ar, ag, ab);
            DrawInfoRow(left + 0.022f, top + 0.405f, width - 0.044f, T("VOLUME"), volume + "%", ar, ag, ab);
            DrawInfoRow(left + 0.022f, top + 0.455f, width - 0.044f, T("DUCKING"), duckActive ? LocalizeDuckReason() : T("READY"), ar, ag, ab);
        }
        DrawText(T("INFO_HELP"), left + 0.022f, bottom - 0.042f, 0.185f, 0, 174, 181, 191, 215);
    }

    private string GetYouTubeNowPlayingLine()
    {
        if (!string.IsNullOrEmpty(youtubeArtist) && !string.IsNullOrEmpty(youtubeTitle)) return youtubeArtist + " - " + youtubeTitle;
        if (!string.IsNullOrEmpty(youtubeTitle)) return youtubeTitle;
        return "";
    }

    private void DrawYouTubeMusicTab(float left, float top, float width, float bottom, int ar, int ag, int ab)
    {
        int yr = 235, yg = 72, yb = 88;
        float h = bottom - top;
        DrawRect(left + width / 2f, top + h / 2f, width, h, 10, 13, 18, 250);
        DrawFrame(left, top, width, h, 54, 61, 70, 220, 0.0012f);

        float coverLeft = left + 0.020f;
        float coverTop = top + 0.018f;
        float coverW = 0.075f;
        float coverH = 0.120f;
        bool coverDrawn = DrawYouTubeCover(coverLeft, coverTop, coverW, coverH);
        if (!coverDrawn)
        {
            DrawRect(coverLeft + coverW / 2f, coverTop + coverH / 2f, coverW, coverH, 42, 12, 18, 246);
            DrawText("YT", coverLeft + 0.014f, coverTop + 0.026f, 0.360f, 0, 252, 245, 247, 248);
            DrawText("MUSIC", coverLeft + 0.010f, coverTop + 0.073f, 0.160f, 0, 244, 181, 191, 235);
        }
        DrawFrame(coverLeft, coverTop, coverW, coverH, yr, yg, yb, youtubeModeActive ? 220 : 120, 0.0014f);

        DrawText("YOUTUBE MUSIC", left + 0.112f, top + 0.020f, 0.380f, 0, 247, 249, 251, 248);
        DrawText(T("MEDIA_SESSION"), left + 0.112f, top + 0.060f, 0.180f, 0, 173, 180, 190, 220);
        DrawText(youtubeAvailable ? T("CONNECTED") : T("NOT_CONNECTED"), left + width - 0.125f, top + 0.024f, 0.175f, 0, youtubeAvailable ? 91 : 230, youtubeAvailable ? 230 : 94, youtubeAvailable ? 124 : 94, 235);
        DrawText(T("VOLUME_SHORT") + " " + volume + "%  |  YT OUT " + MapUserVolumeToYouTube(volume) + "%", left + 0.112f, top + 0.096f, 0.185f, 0, duckActive ? 248 : 195, duckActive ? 205 : 201, duckActive ? 105 : 210, 225);

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
        // V12.8: Nur extern normalisierte ytm_ready_*.png-Dateien duerfen als DirectX-Textur geladen werden.
        // Der externe Cover-Worker schreibt Bilder atomar fertig, bevor die Bridge den Pfad meldet.
        string trackKey = (youtubeTitle ?? "") + "|" + (youtubeArtist ?? "") + "|" + (youtubeAlbum ?? "");
        if (!string.Equals(trackKey, youtubeCoverTrackKey, StringComparison.Ordinal))
        {
            youtubeCoverTrackKey = trackKey;
            youtubeCoverSprite = null;
            youtubeCoverLoadedPath = "";
            youtubeMiniHideAt = DateTime.MinValue;
        }

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

            youtubeCoverSprite = new GTA.UI.CustomSprite(
                youtubeCoverPath,
                new System.Drawing.SizeF(128f, 128f),
                new System.Drawing.PointF(0f, 0f),
                System.Drawing.Color.White);
            youtubeCoverLoadedPath = youtubeCoverPath;
            if (youtubeModeActive)
            {
                youtubeMiniHideAt = DateTime.Now.AddSeconds(2);
                Log("YT MINI AUTO HIDE | Cover geladen, schliesst in 2 Sekunden");
            }
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
        // Frame-sicher: hier gibt es keinerlei File.Exists/FileInfo/Stream/Thumbnail-Zugriffe.
        if (youtubeCoverSprite == null) return false;
        try
        {
            youtubeCoverSprite.Size = new System.Drawing.SizeF(width * GTA.UI.Screen.Width, height * GTA.UI.Screen.Height);
            youtubeCoverSprite.Position = new System.Drawing.PointF(left * GTA.UI.Screen.Width, top * GTA.UI.Screen.Height);
            youtubeCoverSprite.Draw();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void DrawYouTubeSidePanel(float left, float top, float width, int ar, int ag, int ab)
    {
        int yr = 235, yg = 72, yb = 88;
        DrawVolumeWidget(left, top, width, 0.145f, volume / 100.0f, yr, yg, yb);
        DrawText("YT OUT " + MapUserVolumeToYouTube(volume) + "%", left + 0.010f, top + 0.116f, 0.140f, 0, yr, yg, yb, 220);
        DrawText(youtubeAvailable ? T("YT_SESSION_ONLINE") : T("YT_SESSION_OFFLINE"), left + 0.010f, top + 0.158f, 0.150f, 0, youtubeAvailable ? 100 : 220, youtubeAvailable ? 232 : 92, youtubeAvailable ? 132 : 92, 228);
        DrawControlButton(left, top + 0.195f, width, "NUM8   " + T("PREV_SHORT"), false, yr, yg, yb);
        DrawControlButton(left, top + 0.252f, width, "NUM5   " + T("OPEN_YT"), false, yr, yg, yb);
        DrawControlButton(left, top + 0.309f, width, "NUM2   " + T("NEXT_SHORT"), false, yr, yg, yb);
        DrawControlButton(left, top + 0.366f, width, "NUM1   " + T("PLAY_PAUSE"), youtubeModeActive, yr, yg, yb);
    }

    private void DrawSettingsTab(float left, float top, float width, float bottom, int ar, int ag, int ab)
    {
        DrawRect(left + width / 2f, top + (bottom - top) / 2f, width, bottom - top, 10, 15, 20, 248);
        DrawFrame(left, top, width, bottom - top, 45, 54, 64, 220, 0.0011f);
        DrawText(T("SETTINGS"), left + 0.020f, top + 0.018f, 0.360f, 0, 244, 247, 250, 245);
        DrawText(T("SETTINGS_HINT"), left + 0.020f, top + 0.058f, 0.180f, 0, 174, 181, 191, 220);

        float y = top + 0.103f;
        float sy = 0.054f;
        DrawSettingRow(left + 0.022f, y, width - 0.044f, T("LANGUAGE"), GetLanguageDisplayName(), settingsMenuIndex == 0, ar, ag, ab);
        DrawSettingRow(left + 0.022f, y + sy, width - 0.044f, T("DUCK_LEVEL"), duckVolumePercent + "%", settingsMenuIndex == 1, ar, ag, ab);
        DrawSettingRow(left + 0.022f, y + sy * 2f, width - 0.044f, T("RADIO_OUTPUT"), playerMaxVolume + "%", settingsMenuIndex == 2, ar, ag, ab);
        DrawSettingRow(left + 0.022f, y + sy * 3f, width - 0.044f, T("YT_OUTPUT"), youtubeMaxVolume + "%", settingsMenuIndex == 3, ar, ag, ab);
        DrawSettingRow(left + 0.022f, y + sy * 4f, width - 0.044f, T("VOLUME_STEP"), volumeStep + "%", settingsMenuIndex == 4, ar, ag, ab);
        DrawSettingRow(left + 0.022f, y + sy * 5f, width - 0.044f, T("DUCK_HOLD"), duckReleaseMs + " ms", settingsMenuIndex == 5, ar, ag, ab);
        DrawSettingRow(left + 0.022f, y + sy * 6f, width - 0.044f, T("DUCK_FADE"), duckFadeOutMs + " ms", settingsMenuIndex == 6, ar, ag, ab);

        DrawText(T("SETTINGS_NOTE"), left + 0.022f, bottom - 0.075f, 0.175f, 0, 174, 181, 191, 215);
        DrawText(T("HOLD_VOLUME_NOTE"), left + 0.022f, bottom - 0.045f, 0.175f, 0, ar, ag, ab, 225);
    }

    private void DrawSettingRow(float left, float top, float width, string label, string value, bool selected, int ar, int ag, int ab)
    {
        DrawRect(left + width / 2f, top + 0.025f, width, 0.050f, selected ? 18 : 14, selected ? 31 : 20, selected ? 24 : 26, 240);
        if (selected) DrawFrame(left, top, width, 0.050f, ar, ag, ab, 210, 0.0011f);
        DrawText((selected ? ">  " : "   ") + label, left + 0.014f, top + 0.013f, 0.225f, 0, 238, 242, 246, 235);
        DrawText(value, left + width - 0.125f, top + 0.013f, 0.225f, 0, ar, ag, ab, 238);
    }

    private void DrawSettingsSidePanel(float left, float top, float width, int ar, int ag, int ab)
    {
        DrawText(T("AUDIO"), left + 0.008f, top + 0.010f, 0.220f, 0, 239, 242, 246, 238);
        DrawRect(left + width / 2f, top + 0.065f, width, 0.070f, 14, 20, 26, 235);
        DrawText(T("DUCKING"), left + 0.008f, top + 0.040f, 0.155f, 0, 166, 174, 184, 220);
        DrawText(duckVolumePercent + "%", left + width - 0.048f, top + 0.040f, 0.205f, 0, ar, ag, ab, 238);
        DrawRect(left + width / 2f, top + 0.145f, width, 0.070f, 14, 20, 26, 235);
        DrawText(T("RADIO_OUTPUT_SHORT"), left + 0.008f, top + 0.120f, 0.150f, 0, 166, 174, 184, 220);
        DrawText(playerMaxVolume + "%", left + width - 0.048f, top + 0.120f, 0.205f, 0, ar, ag, ab, 238);
        DrawRect(left + width / 2f, top + 0.225f, width, 0.070f, 14, 20, 26, 235);
        DrawText(T("YT_OUTPUT_SHORT"), left + 0.008f, top + 0.200f, 0.150f, 0, 166, 174, 184, 220);
        DrawText(youtubeMaxVolume + "%", left + width - 0.048f, top + 0.200f, 0.205f, 0, ar, ag, ab, 238);
        DrawText(T("HOLD_PLUS_MINUS"), left + 0.008f, top + 0.280f, 0.155f, 0, 174, 181, 191, 218);
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
            DrawText(Shorten((selected ? ">  " : "   ") + favoriteMark + st.Name, 31), left + 0.016f, rowTextY, 0.245f, 0, 244, 247, 250, st.Enabled ? 242 : 112);
            if (active) DrawText("ON AIR", left + 0.232f, rowTextY + 0.001f, 0.140f, 0, cr, cg, cb, 238);
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
        DrawStationLogoBadge(left + 0.014f, top + 0.013f, 0.095f, Math.Min(0.066f, height - 0.020f), sel, cr, cg, cb);
        float textLeft = left + 0.118f;
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

    private void DrawNavItem(float left, float top, float width, string label, bool active, int ar, int ag, int ab)
    {
        int rr = active ? 28 : 17, rg = active ? 31 : 17, rb = active ? 34 : 19;
        if (active && IsPremiumThemeSkin())
        {
            rr = MixColor(rr, ar, 0.14f);
            rg = MixColor(rg, ag, 0.14f);
            rb = MixColor(rb, ab, 0.14f);
        }
        DrawRect(left + width / 2f, top + 0.024f, width, 0.048f, rr, rg, rb, 225);
        if (active)
        {
            DrawRect(left + 0.0025f, top + 0.024f, 0.005f, 0.048f, ar, ag, ab, 235);
            if (IsPremiumThemeSkin())
            {
                int sr, sg, sb; GetSkinSecondaryAccent(out sr, out sg, out sb);
                if (skinIndex == 8) DrawRect(left + width - 0.0020f, top + 0.024f, 0.0030f, 0.048f, sr, sg, sb, 215);
                else if (skinIndex == 11)
                {
                    DrawRect(left + width * 0.73f, top + 0.046f, width * 0.14f, 0.0012f, ar, ag, ab, 200);
                    DrawRect(left + width * 0.90f, top + 0.046f, width * 0.05f, 0.0012f, sr, sg, sb, 190);
                }
                else DrawRect(left + width * 0.78f, top + 0.046f, width * 0.18f, 0.0014f, sr, sg, sb, 185);
            }
        }
        DrawText(label, left + 0.016f, top + 0.014f, 0.245f, 0, active ? 240 : 190, active ? 240 : 190, active ? 243 : 195, 230);
    }

    private void DrawControlButton(float left, float top, float width, string label, bool active, int ar, int ag, int ab)
    {
        DrawRect(left + width / 2f, top + 0.020f, width, 0.040f, active ? 23 : 18, active ? 31 : 18, active ? 25 : 21, 225);
        DrawFrame(left, top, width, 0.040f, active ? ar : 70, active ? ag : 72, active ? ab : 78, active ? 180 : 190, 0.001f);
        if (active && IsPremiumThemeSkin())
        {
            int sr, sg, sb; GetSkinSecondaryAccent(out sr, out sg, out sb);
            DrawRect(left + width - 0.004f, top + 0.020f, 0.0030f, 0.030f, sr, sg, sb, 215);
            if (skinIndex == 11) DrawRect(left + width * 0.70f, top + 0.036f, width * 0.18f, 0.0011f, ar, ag, ab, 185);
        }
        DrawText(label, left + 0.008f, top + 0.0115f, 0.205f, 0, 220, 220, 225, 225);
    }

    private void DrawVolumeWidget(float left, float top, float width, float height, float value, int ar, int ag, int ab)
    {
        value = Math.Max(0f, Math.Min(1f, value));
        int vr = 14, vg = 18, vb = 24;
        if (IsPremiumThemeSkin())
        {
            vr = MixColor(vr, ar, 0.08f);
            vg = MixColor(vg, ag, 0.08f);
            vb = MixColor(vb, ab, 0.08f);
        }
        DrawRect(left + width / 2f, top + height / 2f, width, height, vr, vg, vb, 240);
        if (IsPremiumThemeSkin())
        {
            if (skinIndex == 10) DrawFrame(left, top, width, height, 72, 79, 72, 120, 0.0009f);
            else if (skinIndex == 11)
            {
                DrawFrame(left, top, width, height, ar, ag, ab, 128, 0.0009f);
            }
            else
            {
                int sr, sg, sb; GetSkinSecondaryAccent(out sr, out sg, out sb);
                DrawFrame(left, top, width, height, sr, sg, sb, 135, 0.0010f);
            }
        }
        else DrawFrame(left, top, width, height, 54, 62, 72, 210, 0.0010f);

        // Narrow side panel: use the short label so German/Spanish can never collide
        // with the percentage. The old +/- mini-buttons were also removed because the
        // real control buttons directly below already provide those actions.
        DrawText(T("VOLUME_SHORT"), left + 0.010f, top + 0.013f, 0.185f, 0, 188, 195, 205, 230);
        string percent = ((int)Math.Round(value * 100f)).ToString() + "%";
        DrawText(percent, left + width - 0.052f, top + 0.009f, 0.255f, 0, 249, 250, 252, 244);

        float trackLeft = left + 0.012f;
        float trackWidth = width - 0.024f;
        float trackY = top + 0.062f;
        DrawRect(trackLeft + trackWidth / 2f, trackY, trackWidth, 0.007f, 54, 60, 69, 220);
        float fillWidth = Math.Max(0.0015f, trackWidth * value);
        DrawRect(trackLeft + fillWidth / 2f, trackY, fillWidth, 0.007f, ar, ag, ab, 238);
        if (IsPremiumThemeSkin() && fillWidth > 0.012f)
        {
            int sr, sg, sb; GetSkinSecondaryAccent(out sr, out sg, out sb);
            float accentW = Math.Min(fillWidth * 0.28f, 0.026f);
            if (skinIndex == 11) accentW = Math.Min(fillWidth * 0.22f, 0.018f);
            DrawRect(trackLeft + fillWidth - accentW / 2f, trackY, accentW, 0.007f, sr, sg, sb, 238);
        }
        DrawRect(trackLeft + trackWidth * value, trackY, 0.006f, 0.017f, 236, 239, 243, 232);

        DrawText("NUM-  /  NUM+", left + 0.012f, top + 0.093f, 0.150f, 0, 168, 176, 186, 220);
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

        int cr, cg, cb;
        GetCategoryColor(st != null ? (st.Pack ?? "") : "", out cr, out cg, out cb);
        int bgR = MixColor(14, cr, 0.26f);
        int bgG = MixColor(18, cg, 0.22f);
        int bgB = MixColor(24, cb, 0.20f);
        int stripeR = MixColor(cr, 255, 0.08f);
        int stripeG = MixColor(cg, 255, 0.08f);
        int stripeB = MixColor(cb, 255, 0.08f);

        DrawRect(left + width / 2f, top + height / 2f, width, height, 11, 16, 21, 246);
        DrawRect(left + width / 2f, top + height / 2f, width - 0.004f, height - 0.004f, bgR, bgG, bgB, 232);
        DrawRect(left + 0.007f, top + height / 2f, 0.004f, height - 0.010f, stripeR, stripeG, stripeB, 228);
        DrawFrame(left, top, width, height, cr, cg, cb, 185, 0.0010f);
        DrawText(Shorten(line1, 8), left + 0.015f, top + 0.012f, line1.Length <= 2 ? 0.340f : 0.240f, 0, 245, 248, 250, 240);
        DrawText(Shorten(line2, 8), left + 0.015f, top + 0.039f, 0.170f, 0, cr, cg, cb, 222);
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
        DrawText("VOL", centerX - 0.015f, centerY - 0.011f, 0.165f, 0, 230, 231, 234, 230);
    }

    private void DrawDigitalKnob(float centerX, float centerY, float diameter, float value, int ar, int ag, int ab)
    {
        value = Math.Max(0f, Math.Min(1f, value));
        DrawRect(centerX, centerY, diameter, diameter, 28, 29, 33, 230);
        DrawRect(centerX, centerY, diameter * 0.74f, diameter * 0.74f, 82, 84, 90, 230);
        DrawRect(centerX, centerY, diameter * 0.58f, diameter * 0.58f, 20, 21, 24, 235);
        float barW = diameter * 0.74f;
        DrawSegmentMeter(centerX - barW / 2f, centerY + diameter * 0.46f, barW, 0.006f, value, ar, ag, ab);
        DrawText("VOL", centerX - 0.016f, centerY - 0.010f, 0.185f, 0, 225, 225, 228, 230);
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
            DrawRect(left + bw / 2f + i * (bw + gap), top + height - h / 2f, bw, h, ar, ag, ab, alpha);
        }
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
                if (youtubeModeActive && youtubeAvailable)
                {
                    sourceActive = IsYouTubePlaying() || string.Equals(youtubeState, "Playing", StringComparison.OrdinalIgnoreCase);
                    if (sourceActive) peak = InternetRadioAudioMeter.GetPeakForBrowser(youtubeSource);
                }
                else if (enabled && shouldPlay && audio != null)
                {
                    sourceActive = string.Equals(cachedAudioState ?? "", "Playing", StringComparison.OrdinalIgnoreCase) || string.Equals(audio.StateText ?? "", "Playing", StringComparison.OrdinalIgnoreCase);
                    if (sourceActive)
                    {
                        int helperPid = audio.HelperPid;
                        if (helperPid > 0) peak = InternetRadioAudioMeter.GetPeakForProcessId(helperPid);
                    }
                }
            }
            else
            {
                sourceActive = youtubeModeActive ? IsYouTubePlaying() : (enabled && shouldPlay && string.Equals(cachedAudioState ?? "", "Playing", StringComparison.OrdinalIgnoreCase));
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
                float boosted = peak * 2.20f;
                if (boosted > 1.0f) boosted = 1.0f;
                targetLevel = (float)Math.Pow(boosted, 0.72f);
                if (targetLevel < 0.03f && peak > 0.001f) targetLevel = 0.03f;
            }
            else
            {
                // Safe fallback: the mod still loads and the display keeps moving even without Core Audio metering.
                targetLevel = 0.22f;
            }
        }

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

    private void DrawBanner()
    {
        if (DateTime.Now > bannerUntil || string.IsNullOrEmpty(bannerTop)) return;
        float width = 0.29f;
        float height = 0.07f;
        float left = 0.355f;
        float top = 0.80f;
        DrawRect(left + width / 2f, top + height / 2f, width, height, 0, 0, 0, 160);
        DrawRect(left + width / 2f, top + 0.009f, width, 0.018f, 54, 137, 214, 215);
        DrawText(bannerTop, left + 0.010f, top + 0.012f, 0.36f, 0, 255, 255, 255, 240);
        DrawText(bannerBottom, left + 0.010f, top + 0.036f, 0.28f, 0, 215, 215, 215, 225);
    }

    private void ShowBanner(string top, string bottom, int durationMs)
    {
        bannerTop = top ?? "";
        bannerBottom = bottom ?? "";
        bannerUntil = DateTime.Now.AddMilliseconds(durationMs <= 0 ? 2000 : durationMs);
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

    private string LocalizeDuckReason()
    {
        if (string.Equals(duckReason, "MENUE", StringComparison.OrdinalIgnoreCase)) return T("MENU");
        if (string.Equals(duckReason, "DIALOG", StringComparison.OrdinalIgnoreCase)) return T("DIALOG");
        if (string.Equals(duckReason, "TELEFON", StringComparison.OrdinalIgnoreCase) || string.Equals(duckReason, "ANRUF", StringComparison.OrdinalIgnoreCase)) return T("PHONE");
        return duckReason;
    }

    private void AdjustVolume(int delta)
    {
        volume = Math.Max(0, Math.Min(100, volume + delta));
        ApplyEffectiveVolume();
        string volumeSource = youtubeModeActive ? ("YouTube Music | OUT " + MapUserVolumeToYouTube(volume) + "%") : CurrentStationLine();
        ShowBanner(T("VOLUME") + " " + volume + "%", duckActive ? (T("DUCKING") + ": " + LocalizeDuckReason()) : volumeSource, 900);
        Log((youtubeModeActive ? "YT " : "RADIO ") + "VOLUME " + volume);
        MarkUserSettingsDirty();
    }

    private void ApplyEffectiveVolume()
    {
        float safeDuckMix = Math.Max(0.0f, Math.Min(1.0f, duckMix));
        float duckFloor = Math.Max(0.05f, Math.Min(1.0f, duckVolumePercent / 100.0f));
        float effectiveFactor = 1.0f - safeDuckMix * (1.0f - duckFloor);
        int userTarget = (int)Math.Round(volume * effectiveFactor);
        userTarget = Math.Max(0, Math.Min(100, userTarget));
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

    private void UpdateSpeechDucking()
    {
        // V12.33: Ducking is allowed while YT mode owns the browser audio session even when
        // GSMTC briefly reports Paused/Unavailable during track changes.
        bool sourcePlaying = youtubeModeActive ? true : shouldPlay;
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
        bool wantDuck = speechRecent || phoneCall || ownMenu;

        string requestedReason = "";
        if (phoneCall) requestedReason = "ANRUF";
        else if (speechRecent) requestedReason = "DIALOG";
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
            // Force a fresh volume command for every small fade step. At 100 ms logic ticks
            // this produces a smooth ramp without doing any work in the frame renderer.
            appliedStreamVolume = -1;
            appliedYoutubeVolume = -1;
            appliedYoutubeMixerLevel = -1.0f;
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
            if (values.TryGetValue("Language", out v)) uiLanguage = NormalizeLanguage(v);
            if (values.TryGetValue("Volume", out v) && int.TryParse(v, out iv)) volume = Math.Max(0, Math.Min(100, iv));
            if (values.TryGetValue("VolumeStep", out v) && int.TryParse(v, out iv)) volumeStep = Math.Max(1, Math.Min(5, iv));
            if (values.TryGetValue("DuckVolumePercent", out v) && int.TryParse(v, out iv)) duckVolumePercent = Math.Max(20, Math.Min(100, iv));
            if (values.TryGetValue("DuckReleaseMs", out v) && int.TryParse(v, out iv)) duckReleaseMs = Math.Max(500, Math.Min(4000, iv));
            if (values.TryGetValue("DuckFadeOutMs", out v) && int.TryParse(v, out iv)) duckFadeOutMs = Math.Max(200, Math.Min(2000, iv));
            if (values.TryGetValue("PlayerMaxVolume", out v) && int.TryParse(v, out iv)) playerMaxVolume = Math.Max(20, Math.Min(100, iv));
            if (values.TryGetValue("YouTubeMaxVolume", out v) && int.TryParse(v, out iv)) youtubeMaxVolume = Math.Max(5, Math.Min(100, iv));
            if (values.TryGetValue("VolumeCurveExponent", out v) && float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out fv)) volumeCurveExponent = Math.Max(1.0f, Math.Min(3.0f, fv));
            if (values.TryGetValue("YouTubeVolumeCurveExponent", out v) && float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out fv)) youtubeVolumeCurveExponent = Math.Max(0.8f, Math.Min(2.8f, fv));
            if (values.TryGetValue("YouTubeVolumeProfileVersion", out v) && int.TryParse(v, out iv)) youtubeVolumeProfileVersion = Math.Max(0, iv);
            if (values.TryGetValue("Skin", out v)) skinIndex = GetSkinIndex(v);
            skinMenuIndex = skinIndex;
            if (values.TryGetValue("MenuSlowMotion", out v) && bool.TryParse(v, out bv)) menuSlowMotionEnabled = bv;
            if (values.TryGetValue("MenuTimeScale", out v) && float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out fv)) menuTimeScale = Math.Max(0.20f, Math.Min(1.00f, fv));
            if (values.TryGetValue("ShowMiniUi", out v) && bool.TryParse(v, out bv)) showMiniUi = bv;
            if (values.TryGetValue("PersistentMiniUi", out v) && bool.TryParse(v, out bv)) persistentMiniUi = bv;
            if (values.TryGetValue("HideMiniUiWhileMenuOpen", out v) && bool.TryParse(v, out bv)) hideMiniUiWhileMenuOpen = bv;
            if (values.TryGetValue("MenuOpenSound", out v) && bool.TryParse(v, out bv)) menuOpenSoundEnabled = bv;
            if (values.TryGetValue("SpectrumSkinStrength", out v) && float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out fv))
                dynamicRadioBackgroundStrength = Math.Max(0.25f, Math.Min(1.00f, fv));
            else if (values.TryGetValue("RainbowSkinStrength", out v) && float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out fv))
                dynamicRadioBackgroundStrength = Math.Max(0.25f, Math.Min(1.00f, fv));
            else if (values.TryGetValue("DynamicRadioBackgroundStrength", out v) && float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out fv))
                dynamicRadioBackgroundStrength = Math.Max(0.25f, Math.Min(1.00f, fv));
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
            Log("USER SETTINGS LOADED | Language=" + uiLanguage + " | Volume=" + volume + " | Duck=" + duckVolumePercent + " | Hold=" + duckReleaseMs + "ms | Fade=" + duckFadeOutMs + "ms | RadioMax=" + playerMaxVolume + " | YTMax=" + youtubeMaxVolume + " | Skin=" + skinNames[skinIndex] + " | Favorites=" + favoriteStationNames.Count);
        }
        catch (Exception ex)
        {
            Log("USER SETTINGS LOAD FEHLER | " + ex.Message);
        }
    }

    private void SaveUserSettings()
    {
        try
        {
            string path = UserSettingsPath();
            string temp = path + ".tmp";
            string favoritesData = "";
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
                "Language=" + uiLanguage + Environment.NewLine +
                "Volume=" + volume + Environment.NewLine +
                "DuckVolumePercent=" + duckVolumePercent + Environment.NewLine +
                "DuckReleaseMs=" + duckReleaseMs + Environment.NewLine +
                "DuckFadeOutMs=" + duckFadeOutMs + Environment.NewLine +
                "PlayerMaxVolume=" + playerMaxVolume + Environment.NewLine +
                "YouTubeMaxVolume=" + youtubeMaxVolume + Environment.NewLine +
                "VolumeCurveExponent=" + volumeCurveExponent.ToString(System.Globalization.CultureInfo.InvariantCulture) + Environment.NewLine +
                "YouTubeVolumeCurveExponent=" + youtubeVolumeCurveExponent.ToString(System.Globalization.CultureInfo.InvariantCulture) + Environment.NewLine +
                "YouTubeVolumeProfileVersion=" + youtubeVolumeProfileVersion + Environment.NewLine +
                "Skin=" + skinNames[Math.Max(0, Math.Min(skinNames.Length - 1, skinIndex))] + Environment.NewLine +
                "MenuSlowMotion=" + menuSlowMotionEnabled.ToString().ToLowerInvariant() + Environment.NewLine +
                "MenuTimeScale=" + menuTimeScale.ToString(System.Globalization.CultureInfo.InvariantCulture) + Environment.NewLine +
                "ShowMiniUi=" + showMiniUi.ToString().ToLowerInvariant() + Environment.NewLine +
                "PersistentMiniUi=" + persistentMiniUi.ToString().ToLowerInvariant() + Environment.NewLine +
                "HideMiniUiWhileMenuOpen=" + hideMiniUiWhileMenuOpen.ToString().ToLowerInvariant() + Environment.NewLine +
                "MenuOpenSound=" + menuOpenSoundEnabled.ToString().ToLowerInvariant() + Environment.NewLine +
                "SpectrumSkinStrength=" + dynamicRadioBackgroundStrength.ToString(System.Globalization.CultureInfo.InvariantCulture) + Environment.NewLine +
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
            Log("USER SETTINGS SAVED | Language=" + uiLanguage + " | Volume=" + volume + " | Duck=" + duckVolumePercent + " | Hold=" + duckReleaseMs + "ms | Fade=" + duckFadeOutMs + "ms | RadioMax=" + playerMaxVolume + " | YTMax=" + youtubeMaxVolume + " | Favorites=" + favoriteStationNames.Count);
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
        SaveGeneralValue("Language", uiLanguage);
        SaveGeneralValue("AudioProfileVersion", "4");
        SaveGeneralValue("DuckVolumePercent", duckVolumePercent.ToString());
        SaveGeneralValue("VolumeStep", volumeStep.ToString());
        SaveGeneralValue("PlayerMaxVolume", playerMaxVolume.ToString());
        SaveGeneralValue("VolumeCurveExponent", volumeCurveExponent.ToString(System.Globalization.CultureInfo.InvariantCulture));
        SaveGeneralValue("YouTubeMaxVolume", youtubeMaxVolume.ToString());
        SaveGeneralValue("YouTubeVolumeCurveExponent", youtubeVolumeCurveExponent.ToString(System.Globalization.CultureInfo.InvariantCulture));
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
        bool en = uiLanguage == "EN";
        bool es = uiLanguage == "ES";
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
        if (key == "PACKS") return es ? "CATEGORÍAS" : (en ? "CATEGORIES" : "KATEGORIEN");
        if (key == "INFO") return "INFO";
        if (key == "SKINS") return "SKINS";
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
        if (key == "DUCKING") return "DUCKING";
        if (key == "READY") return es ? "Listo" : (en ? "Ready" : "Bereit");
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
        if (key == "SKINS_THEMES") return es ? "SKINS / TEMAS" : (en ? "SKINS / THEMES" : "SKINS / THEMES");
        if (key == "SKIN_HINT") return es ? "Num8/2 elegir skin | Num5 aplicar" : (en ? "Num8/2 choose skin | Num5 apply" : "Num8/2 Skin wählen | Num5 anwenden");
        if (key == "ACTIVE") return es ? "ACTIVO" : (en ? "ACTIVE" : "AKTIV");
        if (key == "PREVIEW") return es ? "VISTA PREVIA" : (en ? "PREVIEW" : "VORSCHAU");
        if (key == "SKIN_HELP") return es ? "El skin cambia la interfaz; el audio no cambia." : (en ? "The skin changes the interface; audio is unchanged." : "Der Skin ändert die Oberfläche; das Audio bleibt unverändert.");
        if (key == "INFO_HELP") return es ? "Num4/6 cambia pestañas | Num0 cierra el menú" : (en ? "Num4/6 changes tabs | Num0 closes the menu" : "Num4/6 wechselt Reiter | Num0 schließt das Menü");
        if (key == "CONNECTED") return es ? "CONECTADO" : (en ? "CONNECTED" : "VERBUNDEN");
        if (key == "NOT_CONNECTED") return es ? "NO CONECTADO" : (en ? "NOT CONNECTED" : "NICHT VERBUNDEN");
        if (key == "MEDIA_SESSION") return es ? "SESIÓN MULTIMEDIA" : (en ? "WINDOWS MEDIA SESSION" : "WINDOWS-MEDIENSESSION");
        if (key == "NOW_PLAYING") return es ? "REPRODUCIENDO" : (en ? "NOW PLAYING" : "JETZT LÄUFT");
        if (key == "YT_START_HINT") return es ? "Pulsa Num5 para abrir YouTube Music en primer plano" : (en ? "Press Num5 to open YouTube Music in the foreground" : "Num5 drücken, um YouTube Music im Vordergrund zu öffnen");
        if (key == "YT_START_SONG") return es ? "Elige música allí. Al reproducir, GTA la detectará automáticamente." : (en ? "Choose music there. When playback starts, GTA detects it automatically." : "Wähle dort Musik aus. Sobald sie läuft, erkennt GTA sie automatisch.");
        if (key == "YT_CHOOSE_MUSIC") return es ? "Elige música allí - GTA detecta la reproducción" : (en ? "Choose music there - GTA detects playback" : "Dort Musik wählen - GTA erkennt die Wiedergabe");
        if (key == "YT_DETECTED") return es ? "Reproducción detectada - YT Music activo" : (en ? "Playback detected - YT Music active" : "Wiedergabe erkannt - YT Music aktiv");
        if (key == "YT_STARTING") return es ? "Iniciando reproducción" : (en ? "Starting playback" : "Wiedergabe wird gestartet");
        if (key == "YT_OPENED_SELECT") return es ? "YouTube Music abierto - elige música y pulsa reproducir" : (en ? "YouTube Music opened - choose music and press play" : "YouTube Music geöffnet - Musik wählen und Wiedergabe starten");
        if (key == "BRIDGE_UNAVAILABLE") return es ? "Bridge no disponible" : (en ? "Bridge unavailable" : "Bridge nicht verfügbar");
        if (key == "YT_SESSION_ONLINE") return es ? "SESIÓN YT ONLINE" : (en ? "YT SESSION ONLINE" : "YT-SESSION ONLINE");
        if (key == "YT_SESSION_OFFLINE") return es ? "SESIÓN YT NO DISPONIBLE" : (en ? "YT SESSION OFFLINE" : "YT-SESSION OFFLINE");
        if (key == "PREV_SHORT") return es ? "ANTERIOR" : (en ? "PREV" : "ZURÜCK");
        if (key == "NEXT_SHORT") return es ? "SIGUIENTE" : (en ? "NEXT" : "WEITER");
        if (key == "PAUSE") return es ? "PAUSA" : "PAUSE";
        if (key == "PREVIOUS_TRACK") return es ? "Título anterior" : (en ? "Previous track" : "Vorheriger Titel");
        if (key == "NEXT_TRACK") return es ? "Siguiente título" : (en ? "Next track" : "Nächster Titel");
        if (key == "TRACK_NAV") return es ? "ANT./SIG." : (en ? "PREV/NEXT" : "ZURÜCK/WEITER");
        if (key == "PLAY_PAUSE") return "PLAY / PAUSE";
        if (key == "YT_OPEN_FAILED") return es ? "No se pudo abrir YouTube Music - revisa el navegador predeterminado" : (en ? "Could not open YouTube Music - check your default browser" : "YouTube Music konnte nicht geöffnet werden - Standardbrowser prüfen");
        if (key == "OPEN_YT") return es ? "ABRIR / INICIAR" : (en ? "OPEN / START" : "ÖFFNEN / STARTEN");
        if (key == "OPEN_PLAY") return es ? "ABRIR / PLAY" : (en ? "OPEN / PLAY" : "ÖFFNEN / PLAY");
        if (key == "SOURCE") return es ? "Fuente" : (en ? "Source" : "Quelle");
        if (key == "YT_EXCLUSIVE") return es ? "YT Music sustituye la radio cuando está activo." : (en ? "YT Music replaces internet radio while active." : "YT Music ersetzt das Internetradio, solange es aktiv ist.");
        if (key == "SETTINGS_HINT") return es ? "Num8/2 seleccionar | Num5 idioma | Mantén Num-/+ para ajustar" : (en ? "Num8/2 select | Num5 language | Hold Num-/+ to adjust" : "Num8/2 wählen | Num5 Sprache | Num-/+ halten zum Einstellen");
        if (key == "LANGUAGE") return es ? "IDIOMA" : (en ? "LANGUAGE" : "SPRACHE");
        if (key == "DUCK_LEVEL") return es ? "VOLUMEN DURANTE DUCKING" : (en ? "DUCKING VOLUME" : "DUCKING-LAUTSTÄRKE");
        if (key == "RADIO_OUTPUT") return es ? "SALIDA MÁX. DE RADIO" : (en ? "RADIO MAX OUTPUT" : "RADIO MAX. AUSGABE");
        if (key == "YT_OUTPUT") return es ? "SALIDA MÁX. DE YT" : (en ? "YT MAX OUTPUT" : "YT MAX. AUSGABE");
        if (key == "VOLUME_STEP") return es ? "PASO DE VOLUMEN" : (en ? "VOLUME STEP" : "LAUTSTÄRKE-SCHRITT");
        if (key == "RADIO_OUTPUT_SHORT") return es ? "RADIO MÁX." : (en ? "RADIO MAX" : "RADIO MAX");
        if (key == "YT_OUTPUT_SHORT") return es ? "YT MÁX." : (en ? "YT MAX" : "YT MAX");
        if (key == "AUDIO") return "AUDIO";
        if (key == "DUCK_HOLD") return es ? "RETENCIÓN DIÁLOGO" : (en ? "DIALOGUE HOLD" : "DIALOG-HALTEZEIT");
        if (key == "DUCK_FADE") return es ? "FUNDIDO DUCKING" : (en ? "DUCKING FADE" : "DUCKING-ÜBERBLENDUNG");
        if (key == "SETTINGS_NOTE") return es ? "Ducking: mantiene el volumen bajo entre frases y vuelve suavemente." : (en ? "Ducking stays low between dialogue lines and fades back smoothly." : "Ducking bleibt zwischen Dialogzeilen leise und blendet weich zurück.");
        if (key == "HOLD_VOLUME_NOTE") return es ? "Fuera de Ajustes: mantén Num-/Num+ para cambiar el volumen de forma continua." : (en ? "Outside Settings: hold Num-/Num+ for continuous volume changes." : "Außerhalb der Einstellungen: Num-/Num+ für kontinuierliche Lautstärke halten.");
        if (key == "HOLD_PLUS_MINUS") return es ? "MANTENER +/-" : (en ? "HOLD +/-" : "+/- HALTEN");
        if (key == "USE_PLUS_MINUS") return es ? "Usa Num-/Num+ para cambiar el valor" : (en ? "Use Num-/Num+ to change the value" : "Mit Num-/Num+ den Wert ändern");
        if (key == "MENU_HINT") return es ? "Num4/6 pestaña | Num8/2 selección | Num5 OK | Espacio favorito" : (en ? "Num4/6 tab | Num8/2 selection | Num5 OK | Space favorite" : "Num4/6 Reiter | Num8/2 Auswahl | Num5 OK | Leertaste Favorit");
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
        if (key == "STATION_UNAVAILABLE") return es ? "Emisora no disponible - saltando" : (en ? "Station unavailable - skipping" : "Sender nicht verfügbar - überspringe");
        return key;
    }

    private void DrawRect(float x, float y, float width, float height, int r, int g, int b, int a)
    {
        Function.Call(Hash.DRAW_RECT, x, y, width, height, r, g, b, a);
    }

    private void DrawText(string text, float x, float y, float scale, int font, int r, int g, int b, int a)
    {
        Function.Call(Hash.SET_TEXT_FONT, font);
        Function.Call(Hash.SET_TEXT_SCALE, 0.0f, scale);
        Function.Call(Hash.SET_TEXT_COLOUR, r, g, b, a);
        Function.Call(Hash.SET_TEXT_WRAP, 0.0f, 1.0f);
        Function.Call(Hash.SET_TEXT_OUTLINE);
        Function.Call(Hash.BEGIN_TEXT_COMMAND_DISPLAY_TEXT, "STRING");
        Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, text ?? "");
        Function.Call(Hash.END_TEXT_COMMAND_DISPLAY_TEXT, x, y);
    }

    private void OnAbort(object sender, EventArgs e)
    {
        // Safety first: GTA must never remain in slow motion after the script unloads.
        RestoreGameTimeScale();
        Instance = null;
        try { SaveVolumeToIni(); } catch { }
        try { SaveAudioAndLanguageSettings(); } catch { }
        try { SaveUserSettings(); } catch { }
        try { if (audio != null) audio.Close(); } catch { }
        try
        {
            if (youtube != null)
            {
                // V12.24: Beim Beenden nur einen eindeutig gefundenen YT-Music-Tab
                // per Ctrl+W schliessen. Niemals ein komplettes Chrome/Edge-Fenster
                // oder einen Browserprozess beenden.
                if (youtubeModeActive) youtube.CloseManagedApp();
                else youtube.SetManagedActive(false);
                youtube.Close();
            }
        }
        catch { }
        try { InternetRadioBrowserVolume.Restore(); } catch { }
        Log("ABORT | YT_AUTOCLOSE=" + (youtubeModeActive ? "JA" : "NEIN"));
    }

    private void Log(string text)
    {
        try { File.AppendAllText(logPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + text + Environment.NewLine); } catch { }
    }

    private static string GetDir()
    {
        string b = AppDomain.CurrentDomain.BaseDirectory;
        string s = Path.Combine(b, "scripts");
        if (Directory.Exists(s)) return Path.Combine(s, "InternetRadio");
        return Path.Combine(b, "InternetRadio");
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

        public bool Available { get { RefreshStatus(); return available; } }
        public string StateText { get { RefreshStatus(); return state; } }
        public string Title { get { RefreshStatus(); return title; } }
        public string Artist { get { RefreshStatus(); return artist; } }
        public string Album { get { RefreshStatus(); return album; } }
        public string Source { get { RefreshStatus(); return source; } }
        public string CoverPath { get { RefreshStatus(); return coverPath; } }

        public YouTubeMusicBridge(string logPath, bool autoLaunchYouTubeMusic, bool autoPlayYouTubeMusic)
        {
            log = logPath;
            dir = Path.GetDirectoryName(logPath);
            helperPath = Path.Combine(dir, "YouTubeMusicBridge.ps1");
            commandDir = Path.Combine(dir, "ytmusic_commands");
            statusPath = Path.Combine(dir, "ytmusic_status.txt");
            coverWorkerPath = Path.Combine(dir, "YouTubeMusicCoverWorker.ps1");
            autoLaunch = autoLaunchYouTubeMusic;
            autoPlay = autoPlayYouTubeMusic;
            StartHelper();
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
        public void Close()
        {
            try { SendCommand("RESTORE_VOLUME"); SendCommand("QUIT"); } catch { }
            try { if (helperProcess != null) helperProcess.Close(); } catch { }
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
                try { File.AppendAllText(startupLog, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  C# BRIDGE LAUNCH | " + helperPath + Environment.NewLine, Encoding.UTF8); } catch { }
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "powershell.exe";
                psi.Arguments = "-NoProfile -Sta -ExecutionPolicy Bypass -WindowStyle Hidden -File \"" + helperPath + "\" -BaseDir \"" + dir + "\" -ParentPid " + Process.GetCurrentProcess().Id + " -AutoLaunch " + (autoLaunch ? "1" : "0") + " -AutoPlay " + (autoPlay ? "1" : "0");
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
                    try { File.AppendAllText(startupLog, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  STDERR | " + e.Data + Environment.NewLine, Encoding.UTF8); } catch { }
                };
                p.Exited += delegate(object sender, EventArgs e)
                {
                    try { File.AppendAllText(startupLog, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  PROCESS EXIT | Code=" + p.ExitCode + Environment.NewLine, Encoding.UTF8); } catch { }
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
            try { File.AppendAllText(log, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + s + Environment.NewLine); } catch { }
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
            StartHelper();
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
            try { SendCommand("QUIT", "", 0); } catch { }
            try
            {
                if (helperProcess != null) helperProcess.Close();
            }
            catch { }
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
            try { File.AppendAllText(log, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + s + Environment.NewLine); } catch { }
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
        return p=="chrome"||p=="msedge"||p=="firefox";
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
        return p == "chrome" || p == "msedge" || p == "firefox";
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
