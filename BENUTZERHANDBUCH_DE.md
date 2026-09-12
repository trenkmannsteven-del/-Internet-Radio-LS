# LOS SANTOS INTERNET RADIO LS v1.0.2 BETA TEST
## Benutzerhandbuch – Kurzfassung

Dieses Handbuch ist wie ein kleines Fahrzeug-Handbuch aufgebaut: zuerst Schnellstart und Bedienelemente, danach die einzelnen Systeme und zum Schluss Hilfe bei Problemen.

> **Wichtig:** Die Mod ist ausschließlich für **GTA V Singleplayer** vorgesehen. Erkennt sie eine Online-/Netzwerksitzung, werden die Mod-Funktionen deaktiviert.

## Inhaltsverzeichnis

1. [Schnellstart](#1-schnellstart)
2. [Bedienelemente](#2-bedienelemente)
3. [HOME und Quellen](#3-home-und-quellen)
4. [Internet Radio](#4-internet-radio)
5. [Spotify und YouTube Music](#5-spotify-und-youtube-music)
6. [Favoriten](#6-favoriten)
7. [Fahrzeug-Menü](#7-fahrzeug-menü)
8. [Licht und Kennzeichenleuchte](#8-licht-und-kennzeichenleuchte)
9. [Turbo Blow-Off](#9-turbo-blow-off)
10. [HUD, Tacho und Fahrzeuganzeigen](#10-hud-tacho-und-fahrzeuganzeigen)
11. [Einstellungen und Speicherung](#11-einstellungen-und-speicherung)
12. [Installation und Updates](#12-installation-und-updates)
13. [Problemlösung](#13-problemlösung)
14. [Beta-Test: Was melden?](#14-beta-test-was-melden)

---

## 1. Schnellstart

1. **ScriptHookV** und **ScriptHookVDotNet** installieren.
2. Den enthaltenen Ordner `scripts` in das GTA-V-Hauptverzeichnis kopieren.
3. Sicherstellen, dass der Windows-Benutzer, mit dem GTA V läuft, **Lesen/Schreiben/Ändern** in `GTA V/scripts/InternetRadio/` darf.
4. GTA V im **Singleplayer** starten.
5. In ein unterstütztes Fahrzeug einsteigen.
6. `NUM0` drücken, um das Multimedia-Menü zu öffnen.
7. Mit `NUM8 / NUM2` navigieren und mit `NUM5` auswählen.

Beim ersten Start wird `scripts/InternetRadio/UserSettings.ini` automatisch angelegt. Persönliche Einstellungen werden dort getrennt von der Hauptkonfiguration gespeichert.

### Referenz-Setup für diesen Beta-Test

Das getestete Entwickler-Setup verwendet:

- **ScriptHookV .NET Enhanced 3.9.0.6 (1.1.0.6)**
- **API 3.9.0**
- GTA V Enhanced / Singleplayer

Andere kompatible SHVDN-Versionen können funktionieren. Bei Compile-Problemen zuerst prüfen, ob `ScriptHookVDotNet.asi`, `ScriptHookVDotNet2.dll` und `ScriptHookVDotNet3.dll` aus **demselben Release-Paket** stammen.

## 2. Bedienelemente

| Taste | Funktion |
|---|---|
| `NUM0` | Multimedia-Menü öffnen / schließen |
| `NUM8 / NUM2` | Auswahl hoch / runter; in Spotify/YouTube vorheriger / nächster Titel |
| `NUM4 / NUM6` | Im Menü Reiter wechseln; außerhalb Quelle/Sender zurück / vor |
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

> Die Mod verwendet für die Fahrzeug-/Radio-Steuerung bewusst den Nummernblock. `Num Lock` sollte eingeschaltet sein.

## 3. HOME und Quellen

Der HOME-Bildschirm dient als Zentrale. Von dort wechselst du zu:

- Internet Radio
- GTA Radio
- YouTube Music
- Spotify
- CAR / Fahrzeug
- Skins / Designs
- Einstellungen
- Info

Die aktive Quelle wird beim Fahrzeugwechsel und nach einem Neustart soweit möglich wiederhergestellt. Dabei wird zwischen Internet Radio, GTA Radio, Spotify und YouTube Music unterschieden.

## 4. Internet Radio

Im Radio-Bereich kannst du Sender nach Kategorie auswählen. Metadaten wie Titel und Künstler werden angezeigt, wenn der jeweilige Stream sie bereitstellt.

Wenn ein Stream nicht erreichbar ist, kann ein anderer Sender getestet werden. Die Streams werden von Drittanbietern betrieben und können unabhängig von der Mod offline sein, ihre URL ändern oder regional blockiert sein.

## 5. Spotify und YouTube Music

Spotify und YouTube Music werden über die vorhandene Windows-/App-Mediensitzung angebunden.

1. Spotify oder YouTube Music öffnen.
2. Einen Titel starten.
3. Den entsprechenden Reiter im LS-Multimedia-Menü öffnen.
4. Mit `NUM5` die Quelle aktivieren.
5. `NUM1` = Play/Pause, `NUM8 / NUM2` = vorheriger/nächster Titel.

Die rechte Statusspalte zeigt Verbindung und App-Lautstärke. **NICHT VERBUNDEN** bedeutet, dass aktuell keine passende Mediensitzung erkannt wurde.

Der Playing/Paused-Zustand wird benutzerbezogen gespeichert, soweit er von der jeweiligen Windows-Mediensitzung zuverlässig gelesen werden kann.

## 6. Favoriten

Auf der Senderseite kannst du mit `LEERTASTE` einen Sender als Favorit speichern oder wieder entfernen. Es sind bis zu **6 Favoriten** vorgesehen.

Mit `ENTF` kann ein markierter Favorit direkt gelöscht werden. Favoriten werden in den User-Einstellungen gespeichert und bleiben nach einem Neustart erhalten.

## 7. Fahrzeug-Menü

Im Reiter **CAR / FAHRZEUG** befinden sich zusätzliche Komfort-, Licht- und Anzeigeoptionen. Dazu gehören je nach Fahrzeug unter anderem:

- Automatik-Blinker
- Warnblinker über `NUM7`
- Fernlicht über `NUM9`
- Beat-Neon
- Innenraum-/Kabinenlicht
- Kennzeichenleuchte
- Turbo Blow-Off
- Tacho / Mini-HUD
- Herstellerlogo
- Drehzahl-/Fahrzeuganzeigen

Nicht jede Funktion ist für jede Fahrzeugklasse sinnvoll. Fahrräder, Züge und andere Sonderfahrzeuge können einzelne Systeme absichtlich nicht verwenden.

## 8. Licht und Kennzeichenleuchte

Die Kennzeichenleuchte der **v1.0.2 Beta Test** basiert auf der getesteten v7.41-Logik.

- Tagsüber bleibt sie standardmäßig **AUS**.
- Tageslicht, Schatten, DRL oder automatische GTA-Lichtmeldungen schalten sie nicht allein ein.
- Ein echter Fahrer-Lichtbefehl kann sie tagsüber aktivieren.
- Nachts folgt sie dem echten Abblend-/Fernlicht.
- Kurze Statussprünge werden entprellt, damit die Leuchte nicht flackert.
- `PLATE LIGHT` im CAR-Menü muss natürlich aktiviert sein.

Bei statischem Neon kann die Kennzeichenleuchte dessen Farbe übernehmen. Ohne statisches Neon orientiert sie sich an der normalen bzw. Xenon-Lichtfarbe. Beat-Neon lässt die Kennzeichenleuchte nicht pulsieren.

## 9. Turbo Blow-Off

**Turbo Blow-Off** ist bewusst nur `AN / AUS`.

Bei `AN` wird nach echtem Turbo-Lastaufbau einmalig ein stärkerer SPORT-artiger Blow-Off-Akzent ausgelöst:

- beim Hochschalten oder
- bei deutlichem Gaswegnehmen nach Boost.

Die Anti-Spam-Logik verhindert die früheren mehrfachen Wiederholungen. Bremsen allein soll keinen Effekt-Stack auslösen. Die Erkennung funktioniert auch bei hohem Tempo und in der Luft.

Unterstützt werden erkannte Tuning-Turbos sowie eine konservative Auswahl serienmäßiger Turbo-Fahrzeuge. Zusätzliche Add-on-Modelle können in `InternetRadio.ini` über `FactoryTurboModels=` ergänzt werden.

## 10. HUD, Tacho und Fahrzeuganzeigen

Je nach Fahrzeugklasse werden unterschiedliche Anzeigen verwendet:

- Straßenfahrzeuge: Tacho / RPM / Fahrzeugdaten
- Boote: Marine-HUD
- Flugzeuge und Helikopter: Flight-HUD

Die Geschwindigkeitsanzeige verwendet die tatsächliche GTA-Fahrzeuggeschwindigkeit. Herstellerlogos werden aus GTA-eigenen Fahrzeug-HUD-Texturen geladen; wenn kein passendes Logo verfügbar ist, wird ein neutrales Symbol verwendet.

## 11. Einstellungen und Speicherung

Persönliche Einstellungen werden automatisch in:

`GTA V/scripts/InternetRadio/UserSettings.ini`

gespeichert. Dazu gehören unter anderem viele UI-, Audio-, Fahrzeug-, Favoriten-, zuletzt verwendete Quellen-/Sender- und Medienzustände.

`InternetRadio.ini` enthält dagegen die Hauptkonfiguration, Senderliste, technische Standardwerte und optionale Add-on-Konfigurationen. Eigene manuelle Änderungen an dieser Datei sollten vor einem Update gesichert werden.

Reine Momentzustände wie aktuell eingeschaltete Warnblinker, Fernlicht oder die aktuelle Menüposition müssen nicht dauerhaft gespeichert werden.

## 12. Installation und Updates

### Neuinstallation

Den kompletten Ordner `scripts` aus dem Beta-Paket in das GTA-V-Hauptverzeichnis kopieren.

### Update von einer älteren Version

1. GTA V beenden.
2. Optional `scripts/InternetRadio/UserSettings.ini` sichern.
3. Neue `scripts`-Dateien über die alten kopieren.
4. `UserSettings.ini` behalten, wenn deine persönlichen Einstellungen erhalten bleiben sollen.
5. GTA V starten und Funktion testen.

### Deinstallation

Die Mod-Dateien unter `scripts/03_InternetRadioSimple.3.cs` und `scripts/InternetRadio/` entfernen. Andere Mods im `scripts`-Ordner nicht löschen.

## 13. Problemlösung

**Menü öffnet nicht**  
Prüfe ScriptHookV, ScriptHookVDotNet, Singleplayer-Modus und ob du in einem unterstützten Fahrzeug sitzt.

**C#-Compile-Fehler beim Start**  
Öffne `ScriptHookVDotNet.log`. Prüfe die installierte SHVDN-Version und ob alle SHVDN-Dateien aus demselben Paket stammen. Als getestete Referenz dient **ScriptHookV .NET Enhanced 3.9.0.6 (1.1.0.6), API 3.9.0**.

**Radio spielt nicht**  
Teste einen anderen Sender. Drittanbieter-Streams können offline oder regional blockiert sein.

**Spotify / YouTube zeigt NICHT VERBUNDEN**  
Starte zuerst einen Titel in der jeweiligen App bzw. im Browser und prüfe, ob Windows eine Mediensitzung erkennt.

**Einstellungen werden nicht gespeichert**  
Prüfe Lese-/Schreib-/Änderungsrechte für `GTA V/scripts/InternetRadio/`.

**Kennzeichenleuchte bleibt aus**  
Prüfe `PLATE LIGHT` im CAR-Menü und schalte das echte Fahrzeuglicht ein.

**Kennzeichenleuchte ist tagsüber an / flackert**  
Bitte Fahrzeugname, Tageszeit, Lichtstellung und möglichst ein kurzes Video melden. v1.0.2 Beta Test enthält dafür die strengere v7.41-Logik.

**Nach Änderung der INI passiert nichts**  
`F8` drücken oder GTA V neu starten.

## 14. Beta-Test: Was melden?

Für Fehlerberichte helfen möglichst genaue Angaben:

- GTA V **Enhanced oder Legacy**
- verwendete ScriptHookVDotNet-Version
- Fahrzeugname / Add-on-Spawnname
- Quelle: Radio, GTA Radio, Spotify oder YouTube Music
- aktive Einstellung
- was genau passiert ist und wie oft
- Screenshot oder kurzes Video bei UI-/Lichtproblemen
- relevante Zeilen aus `ScriptHookVDotNet.log`

Für **v1.0.2 Beta Test** sind besonders wichtig:

- rechte UI-Positionierung
- Kennzeichenleuchte bei Tag/Nacht und wechselnden Lichtverhältnissen
- Turbo Blow-Off
- Speichern/Wiederherstellen der User-Einstellungen
- Spotify-/YouTube-Verbindungsstatus

---

**LOS SANTOS INTERNET RADIO LS v1.0.2 BETA TEST**  
Inoffizielle Singleplayer-Modifikation für GTA V.
