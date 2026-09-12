# LOS SANTOS INTERNET RADIO LS
## Manuale utente - v1.0.2 BETA TEST

**Lingua:** Italiano  
**Base testata:** v7.41  
**Uso:** GTA V Singleplayer

> **Importante:** questa mod è destinata esclusivamente a GTA V Singleplayer. Se viene rilevata una sessione online/di rete, le funzioni della mod vengono disattivate.

## Indice

1. Avvio rapido
2. Comandi
3. HOME e sorgenti audio
4. Internet Radio
5. Spotify e YouTube Music
6. Preferiti
7. Menu CAR / Veicolo
8. Luci e luce targa
9. Turbo Blow-Off
10. HUD, tachimetro e indicatori
11. Impostazioni e salvataggio
12. Installazione, aggiornamento e disinstallazione
13. Risoluzione dei problemi
14. Beta Test: cosa segnalare

---

## 1. Avvio rapido

1. Installa **ScriptHookV** e **ScriptHookVDotNet**.
2. Copia l'intera cartella `scripts` del pacchetto nella cartella principale di GTA V.
3. Assicurati che l'account Windows usato per avviare GTA V abbia permessi di **lettura/scrittura/modifica** su `GTA V/scripts/InternetRadio/`.
4. Avvia GTA V in **Singleplayer**.
5. Entra in un veicolo supportato.
6. Premi `NUM0` per aprire il menu multimediale.
7. Naviga con `NUM8 / NUM2` e seleziona con `NUM5`.

Al primo avvio viene creato automaticamente `scripts/InternetRadio/UserSettings.ini`. Le preferenze personali vengono salvate separatamente dalla configurazione principale.

### Configurazione di riferimento per questa beta

- **ScriptHookV .NET Enhanced 3.9.0.6 (1.1.0.6)**
- **API 3.9.0**
- GTA V Enhanced / Singleplayer

Altre versioni SHVDN compatibili possono funzionare. In caso di errori di compilazione, verifica che `ScriptHookVDotNet.asi`, `ScriptHookVDotNet2.dll` e `ScriptHookVDotNet3.dll` provengano dallo **stesso pacchetto di rilascio**.

## 2. Comandi

| Tasto | Funzione |
|---|---|
| `NUM0` | Apri / chiudi menu multimediale |
| `NUM8 / NUM2` | Sposta selezione; brano precedente/successivo in Spotify/YouTube |
| `NUM4 / NUM6` | Cambia scheda; stazione/sorgente precedente/successiva fuori dal menu |
| `NUM5` | Seleziona / applica / attiva sorgente |
| `NUM1` | Sorgente attiva on/off o Play/Pause |
| `NUM3` | Stop/Pause della sorgente attiva |
| `NUM- / NUM+` | Volume giù / su |
| `NUM7` | Quattro frecce on/off |
| `NUM9` | Abbaglianti manuali on/off |
| `SPAZIO` | Aggiungi/rimuovi stazione dai preferiti |
| `CANC` | Elimina preferito selezionato |
| `F8` | Ricarica configurazione |
| `ESC / BACK` | Chiudi menu |

`Num Lock` deve essere attivo.

## 3. HOME e sorgenti audio

HOME è il centro del sistema. Da qui puoi aprire Internet Radio, GTA Radio, YouTube Music, Spotify, CAR/Veicolo, skin, impostazioni e informazioni.

L'ultima sorgente attiva viene ripristinata quando possibile dopo il cambio veicolo o un riavvio. Internet Radio, GTA Radio, Spotify e YouTube Music vengono gestiti separatamente.

## 4. Internet Radio

Scegli le stazioni per categoria. Titolo e artista vengono mostrati quando lo stream fornisce i metadati.

Gli stream sono gestiti da terzi. Una stazione può andare offline, cambiare URL o essere bloccata per regione indipendentemente dalla mod. Se una stazione non funziona, provane prima un'altra.

## 5. Spotify e YouTube Music

Spotify e YouTube Music utilizzano la sessione multimediale disponibile di Windows/dell'app.

1. Apri Spotify o YouTube Music.
2. Avvia un brano.
3. Apri la relativa scheda nel menu LS.
4. Premi `NUM5` per attivare la sorgente.
5. `NUM1` = Play/Pause, `NUM8 / NUM2` = brano precedente/successivo.

Il pannello destro mostra stato di connessione e volume dell'app. **NON CONNESSO** significa che non è stata rilevata una sessione multimediale compatibile.

## 6. Preferiti

Premi `SPAZIO` su una stazione per aggiungerla o rimuoverla dai preferiti. Sono supportati fino a **6 preferiti**. `CANC` elimina il preferito selezionato.

I preferiti vengono salvati nelle impostazioni utente e rimangono disponibili dopo il riavvio.

## 7. Menu CAR / Veicolo

La scheda **CAR / VEICOLO** contiene funzioni opzionali di comfort, illuminazione e visualizzazione, a seconda del veicolo:

- Indicatori automatici
- Quattro frecce con `NUM7`
- Abbaglianti con `NUM9`
- Beat Neon
- Luce abitacolo
- Luce targa
- Turbo Blow-Off
- Tachimetro / Mini HUD
- Logo del costruttore
- RPM e indicatori veicolo

Alcune classi speciali possono volutamente non usare tutte le funzioni.

## 8. Luci e luce targa

v1.0.2 Beta Test usa la logica v7.41 testata:

- La luce targa resta **SPENTA durante la normale luce diurna**.
- Luce ambiente, ombre, DRL o stati automatici di GTA non la accendono da soli.
- Un vero comando luci del conducente può attivarla di giorno.
- Di notte segue i veri anabbaglianti/abbaglianti.
- I brevi cambi di stato vengono filtrati per evitare sfarfallii.
- `PLATE LIGHT` deve essere attivo nel menu CAR.

Il neon statico può fornire il colore della luce targa. Senza neon statico, la lampada segue la famiglia di colore dei fari normali/xeno. Beat Neon non fa pulsare la luce targa.

## 9. Turbo Blow-Off

**Turbo Blow-Off** è volutamente solo `ON / OFF`.

Quando attivo, viene generato un singolo accento SPORT dopo un reale carico del turbo:

- durante una cambiata verso l'alto, oppure
- con un netto rilascio dell'acceleratore dopo il boost.

La logica anti-spam evita ripetizioni. Il rilevamento continua a funzionare ad alta velocità e in aria. Modelli add-on possono essere aggiunti in `InternetRadio.ini` con `FactoryTurboModels=`.

## 10. HUD, tachimetro e indicatori

In base alla classe del veicolo:

- Veicoli stradali: velocità / RPM / dati veicolo
- Barche: HUD nautico
- Aerei ed elicotteri: HUD di volo

La velocità usa la velocità reale del veicolo in GTA. I loghi del costruttore vengono caricati dalle texture HUD di GTA, con simbolo neutro se non è disponibile un logo compatibile.

## 11. Impostazioni e salvataggio

Le preferenze personali vengono salvate automaticamente in:

`GTA V/scripts/InternetRadio/UserSettings.ini`

Comprendono numerose opzioni UI, audio, veicolo, preferiti, ultima sorgente e ultima stazione.

`InternetRadio.ini` contiene configurazione principale, lista stazioni, valori tecnici e opzioni add-on. Esegui un backup delle modifiche manuali prima di aggiornare.

## 12. Installazione, aggiornamento e disinstallazione

### Nuova installazione

Copia l'intera cartella `scripts` del pacchetto beta nella cartella principale di GTA V.

### Aggiornamento

1. Chiudi GTA V.
2. Facoltativamente esegui il backup di `scripts/InternetRadio/UserSettings.ini`.
3. Copia i nuovi file `scripts` sopra quelli vecchi.
4. Mantieni `UserSettings.ini` per conservare le preferenze personali.
5. Avvia GTA V e testa la mod.

### Disinstallazione

Rimuovi `scripts/03_InternetRadioSimple.3.cs` e `scripts/InternetRadio/`. Non eliminare altre mod dalla cartella `scripts`.

## 13. Risoluzione dei problemi

**Il menu non si apre:** controlla ScriptHookV, ScriptHookVDotNet, modalità Singleplayer e un veicolo supportato.

**Errore di compilazione C#:** apri `ScriptHookVDotNet.log`, controlla la versione SHVDN e che tutti i file SHVDN provengano dallo stesso pacchetto. Riferimento testato: **ScriptHookV .NET Enhanced 3.9.0.6 (1.1.0.6), API 3.9.0**.

**La radio non riproduce:** prova un'altra stazione; gli stream di terzi possono essere offline o bloccati per regione.

**Spotify/YouTube mostra NON CONNESSO:** avvia prima la riproduzione nell'app/browser e verifica che Windows esponga una sessione multimediale.

**Le impostazioni non vengono salvate:** controlla i permessi lettura/scrittura/modifica su `GTA V/scripts/InternetRadio/`.

**La luce targa resta spenta:** attiva `PLATE LIGHT` e accendi le luci reali del veicolo.

**La luce targa è accesa di giorno o sfarfalla:** segnala veicolo, orario, posizione luci e, se possibile, un breve video.

**Le modifiche INI non hanno effetto:** premi `F8` o riavvia GTA V.

## 14. Beta Test: cosa segnalare

Indica se possibile:

- GTA V **Enhanced o Legacy**
- versione ScriptHookVDotNet
- nome veicolo / spawn name add-on
- sorgente attiva
- impostazione attiva
- passaggi esatti per riprodurre il problema
- screenshot/video per problemi UI o luci
- righe rilevanti di `ScriptHookVDotNet.log`

Aree prioritarie v1.0.2: pannello destro UI, luce targa, Turbo Blow-Off, persistenza delle impostazioni e stato connessione Spotify/YouTube.

---

**LOS SANTOS INTERNET RADIO LS v1.0.2 BETA TEST**  
Modifica non ufficiale per GTA V Singleplayer.
