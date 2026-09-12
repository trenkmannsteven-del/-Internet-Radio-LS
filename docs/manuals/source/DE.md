# LOS SANTOS INTERNET RADIO LS
## Benutzerhandbuch - v1.0.2 BETA TEST

**Sprache:** Deutsch  
**Getestete Basis:** v7.41  
**Einsatz:** GTA V Singleplayer

> **Wichtig:** Diese Mod ist ausschließlich für GTA V Singleplayer vorgesehen. Wird eine Online-/Netzwerksitzung erkannt, werden die Mod-Funktionen deaktiviert.

## Inhaltsverzeichnis

1. Schnellstart
2. Bedienelemente
3. HOME und Audioquellen
4. Internet Radio
5. Spotify und YouTube Music
6. Favoriten
7. CAR / Fahrzeug-Menü
8. Licht und Kennzeichenleuchte
9. Turbo Blow-Off
10. HUD, Tacho und Fahrzeuganzeigen
11. Einstellungen und Speicherung
12. Installation, Update und Deinstallation
13. Problemlösung
14. Beta-Test: Was melden?

---

## 1. Schnellstart

1. **ScriptHookV** und **ScriptHookVDotNet** installieren.
2. Den kompletten Ordner `scripts` aus dem Paket in das GTA-V-Hauptverzeichnis kopieren.
3. Sicherstellen, dass der Windows-Benutzer, mit dem GTA V gestartet wird, **Lesen/Schreiben/Ändern** in `GTA V/scripts/InternetRadio/` darf.
4. GTA V im **Singleplayer** starten.
5. In ein unterstütztes Fahrzeug einsteigen.
6. `NUM0` drücken, um das Multimedia-Menü zu öffnen.
7. Mit `NUM8 / NUM2` navigieren und mit `NUM5` auswählen.

Beim ersten Start wird `scripts/InternetRadio/UserSettings.ini` automatisch angelegt. Persönliche Einstellungen werden dort getrennt von der Hauptkonfiguration gespeichert.

### Referenz-Setup für diesen Beta-Test

- **ScriptHookV .NET Enhanced 3.9.0.6 (1.1.0.6)**
- **API 3.9.0**
- GTA V Enhanced / Singleplayer

Andere kompatible SHVDN-Versionen können funktionieren. Bei Compile-Problemen zuerst prüfen, ob `ScriptHookVDotNet.asi`, `ScriptHookVDotNet2.dll` und `ScriptHookVDotNet3.dll` aus **demselben Release-Paket** stammen.

## 2. Bedienelemente

| Taste | Funktion |
|---|---|
| `NUM0` | Multimedia-Menü öffnen / schließen |
| `NUM8 / NUM2` | Auswahl hoch / runter; in Spotify/YouTube vorheriger / nächster Titel |
| `NUM4 / NUM6` | Reiter wechseln; außerhalb des Menüs Sender/Quelle zurück / vor |
| `NUM5` | Auswählen / anwenden / Quelle aktivieren |
| `NUM1` | Aktive Quelle an/aus bzw. Play/Pause |
| `NUM3` | Stop/Pause der aktiven Quelle |
| `NUM- / NUM+` | Lautstärke leiser / lauter |
| `NUM7` | Warnblinker an / aus |
| `NUM9` | Manuelles Fernlicht an / aus |
| `LEERTASTE` | Sender als Favorit speichern / entfernen |
| `ENTF` | Markierten Favoriten löschen |
| `F8` | Konfiguration neu laden |
| `ESC / BACK` | Menü schließen |

`Num Lock` sollte eingeschaltet sein.

## 3. HOME und Audioquellen

HOME ist die Zentrale des Systems. Von dort erreichst du Internet Radio, GTA Radio, YouTube Music, Spotify, CAR / Fahrzeug, Skins / Designs, Einstellungen und Info.

Die zuletzt verwendete Quelle wird soweit möglich nach Fahrzeugwechseln und Neustarts wiederhergestellt. Internet Radio, GTA Radio, Spotify und YouTube Music werden getrennt behandelt.

## 4. Internet Radio

Im Radio-Bereich kannst du Sender nach Kategorien auswählen. Titel- und Künstlerdaten werden angezeigt, wenn der jeweilige Stream Metadaten bereitstellt.

Die Streams werden von Drittanbietern betrieben. Ein Sender kann unabhängig von der Mod offline sein, seine URL ändern oder regional blockiert sein. Wenn ein Sender nicht funktioniert, zuerst einen anderen Sender testen.

## 5. Spotify und YouTube Music

Spotify und YouTube Music werden über die vorhandene Windows-/App-Mediensitzung eingebunden.

1. Spotify oder YouTube Music öffnen.
2. Einen Titel starten.
3. Den passenden Reiter im LS-Multimedia-Menü öffnen.
4. Mit `NUM5` die Quelle aktivieren.
5. `NUM1` = Play/Pause, `NUM8 / NUM2` = vorheriger/nächster Titel.

Die rechte Statusspalte zeigt Verbindung und App-Lautstärke. **NICHT VERBUNDEN** bedeutet, dass aktuell keine passende Mediensitzung erkannt wird.

## 6. Favoriten

Mit `LEERTASTE` kann ein Sender als Favorit gespeichert oder entfernt werden. Es sind bis zu **6 Favoriten** vorgesehen. Mit `ENTF` lässt sich ein markierter Favorit direkt löschen.

Favoriten werden in den User-Einstellungen gespeichert und bleiben nach einem Neustart erhalten.

## 7. CAR / Fahrzeug-Menü

Im Reiter **CAR / FAHRZEUG** befinden sich zusätzliche Komfort-, Licht- und Anzeigeoptionen. Dazu gehören je nach Fahrzeug Automatik-Blinker, Warnblinker über `NUM7`, Fernlicht über `NUM9`, Beat-Neon, Innenraum-/Kabinenlicht, Kennzeichenleuchte, Turbo Blow-Off, Tacho / Mini-HUD, Herstellerlogo sowie Drehzahl- und Fahrzeuganzeigen.

Nicht jede Funktion ist für jede Fahrzeugklasse sinnvoll. Fahrräder, Züge und andere Sonderfahrzeuge können einzelne Systeme absichtlich nicht verwenden.

## 8. Licht und Kennzeichenleuchte

Die **v1.0.2 Beta Test** verwendet die getestete v7.41-Logik für die Kennzeichenleuchte:

- Tagsüber bleibt sie standardmäßig **AUS**.
- Tageslicht, Schatten, DRL oder automatische GTA-Lichtmeldungen schalten sie nicht allein ein.
- Ein echter Fahrer-Lichtbefehl kann sie tagsüber aktivieren.
- Nachts folgt sie dem echten Abblend-/Fernlicht.
- Kurze Statussprünge werden entprellt, um Flackern zu vermeiden.
- `PLATE LIGHT` im CAR-Menü muss aktiviert sein.

Bei statischem Neon kann die Kennzeichenleuchte dessen Farbe übernehmen. Ohne statisches Neon orientiert sie sich an der normalen bzw. Xenon-Lichtfarbe. Beat-Neon lässt die Kennzeichenleuchte nicht pulsieren.

## 9. Turbo Blow-Off

**Turbo Blow-Off** ist bewusst nur `AN / AUS`. Bei `AN` wird nach echtem Turbo-Lastaufbau einmalig ein stärkerer SPORT-artiger Blow-Off-Akzent ausgelöst: beim Hochschalten oder bei deutlichem Gaswegnehmen nach Boost.

Die Anti-Spam-Logik verhindert wiederholte Effekt-Stacks. Die Erkennung funktioniert auch bei hohem Tempo und in der Luft. Zusätzliche Add-on-Modelle können in `InternetRadio.ini` über `FactoryTurboModels=` ergänzt werden.

## 10. HUD, Tacho und Fahrzeuganzeigen

Je nach Fahrzeugklasse werden unterschiedliche Anzeigen verwendet: Straßenfahrzeuge nutzen Tacho/RPM/Fahrzeugdaten, Boote das Marine-HUD und Flugzeuge/Helikopter das Flight-HUD.

Die Geschwindigkeitsanzeige verwendet die tatsächliche GTA-Fahrzeuggeschwindigkeit. Herstellerlogos werden aus GTA-eigenen Fahrzeug-HUD-Texturen geladen; falls kein passendes Logo verfügbar ist, wird ein neutrales Symbol verwendet.

## 11. Einstellungen und Speicherung

Persönliche Einstellungen werden automatisch in `GTA V/scripts/InternetRadio/UserSettings.ini` gespeichert. Dazu gehören unter anderem UI-, Audio-, Fahrzeug-, Favoriten- und zuletzt verwendete Quellen-/Sender-Einstellungen.

`InternetRadio.ini` enthält dagegen die Hauptkonfiguration, Senderliste, technische Standardwerte und optionale Add-on-Konfigurationen. Eigene Änderungen an dieser Datei sollten vor einem Update gesichert werden.

Momentzustände wie aktuell eingeschaltete Warnblinker, Fernlicht oder die aktuelle Menüposition müssen nicht dauerhaft gespeichert werden.

## 12. Installation, Update und Deinstallation

### Neuinstallation

Den kompletten Ordner `scripts` aus dem Beta-Paket in das GTA-V-Hauptverzeichnis kopieren.

### Update

1. GTA V beenden.
2. Optional `scripts/InternetRadio/UserSettings.ini` sichern.
3. Neue `scripts`-Dateien über die alten kopieren.
4. `UserSettings.ini` behalten, wenn persönliche Einstellungen erhalten bleiben sollen.
5. GTA V starten und Funktion testen.

### Deinstallation

`scripts/03_InternetRadioSimple.3.cs` und `scripts/InternetRadio/` entfernen. Andere Mods im `scripts`-Ordner nicht löschen.

## 13. Problemlösung

**Menü öffnet nicht:** ScriptHookV, ScriptHookVDotNet, Singleplayer-Modus und ein unterstütztes Fahrzeug prüfen.

**C#-Compile-Fehler beim Start:** `ScriptHookVDotNet.log` öffnen. SHVDN-Version prüfen und sicherstellen, dass alle SHVDN-Dateien aus demselben Paket stammen. Getestete Referenz: **ScriptHookV .NET Enhanced 3.9.0.6 (1.1.0.6), API 3.9.0**.

**Radio spielt nicht:** Anderen Sender testen. Drittanbieter-Streams können offline oder regional blockiert sein.

**Spotify / YouTube zeigt NICHT VERBUNDEN:** Zuerst einen Titel in der jeweiligen App bzw. im Browser starten und prüfen, ob Windows eine Mediensitzung erkennt.

**Einstellungen werden nicht gespeichert:** Lese-/Schreib-/Änderungsrechte für `GTA V/scripts/InternetRadio/` prüfen.

**Kennzeichenleuchte bleibt aus:** `PLATE LIGHT` im CAR-Menü prüfen und das echte Fahrzeuglicht einschalten.

**Kennzeichenleuchte ist tagsüber an oder flackert:** Fahrzeugname, Tageszeit, Lichtstellung und möglichst ein kurzes Video melden. v1.0.2 enthält dafür die strengere v7.41-Logik.

**Nach Änderung der INI passiert nichts:** `F8` drücken oder GTA V neu starten.

## 14. Beta-Test: Was melden?

Für Fehlerberichte helfen möglichst genaue Angaben: GTA V **Enhanced oder Legacy**, verwendete ScriptHookVDotNet-Version, Fahrzeugname/Add-on-Spawnname, Quelle, aktive Einstellung, genaue Schritte zum Reproduzieren, Screenshot oder kurzes Video bei UI-/Lichtproblemen und relevante Zeilen aus `ScriptHookVDotNet.log`.

Besonders wichtig für v1.0.2: rechte UI-Positionierung, Kennzeichenleuchte, Turbo Blow-Off, Speichern/Wiederherstellen der User-Einstellungen und Spotify-/YouTube-Verbindungsstatus.

---

**LOS SANTOS INTERNET RADIO LS v1.0.2 BETA TEST**  
Inoffizielle Singleplayer-Modifikation für GTA V.
