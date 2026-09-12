TEST74.2 - SAFE CLOSE REQUEST HOTFIX
- Built on stable TEST73; menu-close-on-press now uses a harmless request flag consumed on Tick.
- Removed direct KeyDown state changes and GetAsyncKeyState polling.

TEST73 - ICON BASELINE + NEON ACCENT BASE
- Optische Icon-Zentrierung pro Symbol und dezenter Neon-Sockel unter den Symbolen; keine Funktionslogik geaendert.

TEST72 - ICON ALIGNMENT & SPACING POLISH
- Rein visueller Feinschliff fuer Icon-Zentrierung, optische Groesse und konsistente Abstaende in HOME, Navigation und Widgets.

LOS SANTOS MULTIMEDIA / INTERNET RADIO LS v1.0.1
PUBLIC RELEASE - NEXUS MODS / GITHUB
Erstellt von St3v3nblub

WICHTIG VOR DER NUTZUNG
- Nur fuer GTA-V-Einzelspieler vorgesehen. Erkennt die Mod eine GTA-Online-/Netzwerksitzung, werden ihre Funktionen deaktiviert.
- Diese Mod ist inoffiziell und unabhaengig; keine Verbindung, Unterstuetzung oder Partnerschaft mit Rockstar Games, Take-Two, Spotify, Google/YouTube oder anderen Drittanbietern.
- Es werden keine Album-/Track-Cover Dritter im Download mitgeliefert.
- Medien-Cover sind bei einer frischen Installation standardmaessig AUS und koennen vom Nutzer optional aktiviert werden.
- Details zu Drittanbietern, Netzwerkzugriff und temporären Dateien: THIRD_PARTY_NOTICES.txt, PRIVACY_AND_NETWORK.txt und DISCLAIMER_AND_LEGAL.txt.


BESCHREIBUNG
Internet Radio LS erweitert GTA V um ein vollständiges Internet-Radio-System mit 61 echten Internet-Radiosendern, 10 Kategorien, 13 Designs, Favoriten, Live-Metadaten soweit verfügbar, YouTube-Music- und Spotify-Integration, automatischem Audio-Ducking, Live-Visualizer und optionaler musikreaktiver Fahrzeugbeleuchtung.

Die Oberfläche ist so gestaltet, dass sie sich natürlich in GTA V einfügt. Der Mod besitzt außerdem 7 auswählbare Interface-Sprachen.

SCHNELLSTART
1. ScriptHookV installieren.
2. ScriptHookVDotNet installieren.
3. Den enthaltenen Ordner "scripts" in das GTA-V-Hauptverzeichnis kopieren.
4. Sicherstellen, dass dein Windows-Benutzer Schreibrechte für GTA V/scripts/InternetRadio besitzt.
5. GTA V starten, in ein unterstütztes Fahrzeug einsteigen und NUM0 drücken.

HAUPTFUNKTIONEN
- 61 echte Internet-Radiosender
- 10 Senderkategorien
- 13 visuelle Designs
- 7 auswählbare Interface-Sprachen
- Bis zu 6 Lieblingssender
- Live-Sender-Metadaten, sofern verfügbar
- Live-Audio-Visualizer
- YouTube-Music-Integration
- Spotify-Integration
- Titel-, Künstler- und Albumanzeige; optionale Coveranzeige ist bei Neuinstallation standardmäßig AUS
- Separate Lautstärkeregelung für YouTube Music und Spotify
- Smoothes Halten der Navigationstasten und beschleunigtes Menü-Scrolling
- Neuer Infotainment-Homescreen mit Now Playing, aktiver Quelle und App-Kacheln für Radio, YouTube Music, Spotify, Fahrzeug, Designs und Einstellungen
- Soft-Spectrum-Design mit langsamem, dezentem Farbverlauf, gezielten Akzenten und weich gemischtem Visualizer
- Geglättete transparente Symbole für Navigation, App-Kacheln, Lautstärke, Quelle, Fahrzeug und Einstellungen
- Optionale Medien-Cover werden proportional dargestellt; im Public Build werden keine Drittanbieter-Coverdateien mitgeliefert
- Theme-abhängiges kompaktes Lautstärke-HUD
- Kompakte Mini-Radio-Anzeige während der Fahrt
- Ducking bei Dialogen, Telefonaten, Menüs und Los Santos Customs
- Wiederherstellung der aktiven Musikquelle nach Tod / nächstem unterstützten Fahrzeug
- Einstellbare Lautstärkeschritte von 1-5%
- Kategorieabhängige Senderfarben
- Optionales musikreaktives Fahrzeug-Neon
- Optionales musikreaktives Innenraum-/Kabinenlicht
- Einstellbare Beat Sensitivity, Voice Filter, Pulse Strength und Pulse Speed
- Externer Bass-Frequenz-Analyzer mit automatischem sicherem Fallback
- Persönliche Einstellungen werden automatisch gespeichert
- Logdateien werden automatisch auf ca. 256 KiB pro Datei begrenzt
- Konfiguration mit F8 neu laden
- Radio-/Mediensteuerung auf Fahrrädern/BMX und weiteren nicht unterstützten Fahrzeugtypen gesperrt, soweit zutreffend

BASSREAKTIVES NEON
Die Beat-Neon-Einstellungen befinden sich im eigenen Reiter FAHRZEUG / CAR SETTINGS. Dort lassen sich Beat-Neon, Empfindlichkeit, Stimmfilter, Puls-Stärke und Puls-Geschwindigkeit einstellen.

Das Beat-Neon kann einen externen Windows-Loopback-Analyzer verwenden, um tiefe Frequenzen gezielter zu erkennen:
- ca. 32-78 Hz: Sub / tiefer Bass
- ca. 86-176 Hz: Kick / Bass-Punch
- ca. 260-1850 Hz: Stimme / Mitten als Gegenreferenz

Falls der externe Analyzer nicht verfügbar ist, fällt Internet Radio LS automatisch auf die stabile Peak-/Transienten-Erkennung zurück. Das Radio wird dadurch nicht am Laden gehindert.

Der Analyzer-Helper liegt hier:
GTA V/scripts/InternetRadio/BassAnalyzerBridge.ps1

DESIGNS
Modern Green, OEM Blue, Red Sport, Amber Classic, Minimal White, Spectrum (weiche dynamische Akzent-Palette),
Neon Sunset, West Coast, Future Neon, Street Tuner,
Block World, Noir Luxe und Sakura Zen.

VORAUSSETZUNGEN
- GTA V für Windows
- ScriptHookV
- ScriptHookVDotNet
- Internetverbindung für Radio-Streams und Online-Musikdienste
- Windows PowerShell für die Helper-Skripte
- Schreibrechte für GTA V/scripts/InternetRadio/

LemonUI wird NICHT benötigt.

MUSIKDIENSTE / KOMPATIBILITÄT
- Google Chrome mit YouTube Music getestet
- Spotify Desktop wird empfohlen
- Spotify-Web-Player-Media-Session-Fallback für Chrome / Edge / Firefox ist enthalten
- NaturalVision Evolved (NVE) getestet und kompatibel

SCHREIBRECHTE ERFORDERLICH
Internet Radio LS benötigt Schreibrechte für:
GTA V/scripts/InternetRadio/

Der Mod erstellt und aktualisiert Laufzeitdateien wie UserSettings.ini, Medien-Statusdateien und Logs. Wenn Einstellungen oder Favoriten nicht gespeichert werden, zuerst die Ordnerrechte prüfen und sicherstellen, dass der Ordner nicht schreibgeschützt ist.

BENUTZEREINSTELLUNGEN
Beim ersten Start wird automatisch erstellt:
GTA V/scripts/InternetRadio/UserSettings.ini

Darin werden persönliche Einstellungen wie Sprache, Lautstärke, Design, Favoriten und Beat-Neon-Feintuning gespeichert.
Bei Updates die vorhandene UserSettings.ini behalten, wenn die eigenen Einstellungen erhalten bleiben sollen.

LOG-GRÖSSENSCHUTZ
Vom Mod erzeugte .log-Dateien werden automatisch auf ungefähr 256 KiB pro Datei begrenzt. Wird die Grenze erreicht, wird der alte Log-Inhalt geleert und das Logging läuft weiter. Es wird keine große Backup-Logdatei angelegt.

GEO-BLOCKING / SENDERVERFÜGBARKEIT
Internet Radio LS verwendet echte Internet-Radio-Streams von Drittanbietern. Einige Sender können je nach Land geo-geblockt oder regional eingeschränkt sein. Sender können außerdem zeitweise offline sein, ihre Stream-Adresse ändern oder keine Metadaten liefern.

Wenn ein Sender nicht startet, bedeutet das nicht automatisch, dass der Mod defekt ist. Geo-Blocking und regionale Beschränkungen werden vom jeweiligen Radioanbieter festgelegt.

GEBLOCKTEN ODER UNERWÜNSCHTEN SENDER DEAKTIVIEREN
Öffne:
GTA V/scripts/InternetRadio/InternetRadio.ini

Suche den entsprechenden Sender und ändere:
Enabled=true

zu:
Enabled=false

Danach im Spiel F8 drücken, um die Konfiguration neu zu laden, oder GTA V neu starten.
Es wird empfohlen, Enabled=false zu verwenden statt den kompletten Sendereintrag zu löschen. So kann der Sender später leicht wieder aktiviert werden.

STEUERUNG
NUM0       Menü öffnen / schließen
NUM8/NUM2  Hoch / runter; auf HOME App wählen; vorheriger / nächster Titel, wo unterstützt
NUM4/NUM6  Tab wechseln / navigieren; außerhalb des Menüs vorheriger / nächster Titel, wo unterstützt
NUM5       Auf HOME App öffnen; sonst auswählen / anwenden / Musikdienst aktivieren
NUM1       Radio an/aus bzw. Play/Pause je nach Seite
NUM3       Radio stoppen / Musikdienst pausieren je nach Seite
NUM-/NUM+  Lautstärke runter / hoch
SPACE      Auf der Sender-Seite Favorit hinzufügen / entfernen
F8         Konfiguration neu laden

HINWEIS
Internet Radio LS ist ein unabhängiger Fan-Mod für GTA V und steht in keiner Verbindung zu Rockstar Games, Google, YouTube, Spotify, NaturalVision Evolved oder den enthaltenen Radiosendern bzw. Streaming-Anbietern.

Senderverfügbarkeit, Metadaten, Verhalten externer Dienste und Stream-Adressen werden von Drittanbietern kontrolliert und können sich ändern.

Sprachen: Englisch, Deutsch, Spanisch, Französisch, Italienisch, Portugiesisch (Brasilien), Türkisch.
