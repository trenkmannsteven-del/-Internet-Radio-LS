# LOS SANTOS INTERNET RADIO LS
## Manual de usuario - v1.0.2 BETA TEST

**Idioma:** Español  
**Base probada:** v7.41  
**Uso:** GTA V Singleplayer

> **Importante:** Este mod está pensado exclusivamente para GTA V Singleplayer. Si se detecta una sesión online/de red, las funciones del mod se desactivan.

## Índice

1. Inicio rápido
2. Controles
3. HOME y fuentes de audio
4. Internet Radio
5. Spotify y YouTube Music
6. Favoritos
7. Menú CAR / Vehículo
8. Luces y luz de matrícula
9. Turbo Blow-Off
10. HUD, velocímetro e indicadores
11. Ajustes y guardado
12. Instalación, actualización y desinstalación
13. Solución de problemas
14. Beta Test: qué informar

---

## 1. Inicio rápido

1. Instala **ScriptHookV** y **ScriptHookVDotNet**.
2. Copia la carpeta completa `scripts` del paquete en la carpeta principal de GTA V.
3. Asegúrate de que la cuenta de Windows que ejecuta GTA V tenga permisos de **lectura/escritura/modificación** en `GTA V/scripts/InternetRadio/`.
4. Inicia GTA V en **Singleplayer**.
5. Entra en un vehículo compatible.
6. Pulsa `NUM0` para abrir el menú multimedia.
7. Navega con `NUM8 / NUM2` y selecciona con `NUM5`.

En el primer inicio se crea automáticamente `scripts/InternetRadio/UserSettings.ini`. Los ajustes personales se guardan allí por separado de la configuración principal.

### Configuración de referencia para esta beta

- **ScriptHookV .NET Enhanced 3.9.0.6 (1.1.0.6)**
- **API 3.9.0**
- GTA V Enhanced / Singleplayer

Otras versiones compatibles de SHVDN pueden funcionar. Si aparecen errores de compilación, comprueba primero que `ScriptHookVDotNet.asi`, `ScriptHookVDotNet2.dll` y `ScriptHookVDotNet3.dll` pertenezcan al **mismo paquete de versión**.

## 2. Controles

| Tecla | Función |
|---|---|
| `NUM0` | Abrir / cerrar menú multimedia |
| `NUM8 / NUM2` | Subir/bajar selección; pista anterior/siguiente en Spotify/YouTube |
| `NUM4 / NUM6` | Cambiar pestaña; estación/fuente anterior/siguiente fuera del menú |
| `NUM5` | Seleccionar / aplicar / activar fuente |
| `NUM1` | Fuente activa on/off o Play/Pause |
| `NUM3` | Stop/Pause de la fuente activa |
| `NUM- / NUM+` | Bajar / subir volumen |
| `NUM7` | Luces de emergencia on/off |
| `NUM9` | Luces largas manuales on/off |
| `ESPACIO` | Añadir/quitar estación de favoritos |
| `SUPR` | Eliminar favorito seleccionado |
| `F8` | Recargar configuración |
| `ESC / BACK` | Cerrar menú |

`Num Lock` debe estar activado.

## 3. HOME y fuentes de audio

HOME es el centro del sistema. Desde allí puedes acceder a Internet Radio, GTA Radio, YouTube Music, Spotify, CAR/Vehículo, skins, ajustes e información.

La última fuente activa se restaura cuando es posible tras cambiar de vehículo o reiniciar. Internet Radio, GTA Radio, Spotify y YouTube Music se gestionan por separado.

## 4. Internet Radio

Puedes elegir estaciones por categoría. El título y el artista se muestran cuando el stream ofrece metadatos.

Los streams pertenecen a terceros. Una estación puede quedar offline, cambiar de URL o estar bloqueada por región sin relación con el mod. Si una estación falla, prueba otra primero.

## 5. Spotify y YouTube Music

Spotify y YouTube Music usan la sesión multimedia disponible de Windows/la aplicación.

1. Abre Spotify o YouTube Music.
2. Inicia una canción.
3. Abre la pestaña correspondiente en el menú multimedia LS.
4. Pulsa `NUM5` para activar la fuente.
5. `NUM1` = Play/Pause, `NUM8 / NUM2` = pista anterior/siguiente.

El panel derecho muestra el estado de conexión y el volumen de la aplicación. **NO CONECTADO** significa que no se detecta una sesión multimedia compatible.

## 6. Favoritos

Pulsa `ESPACIO` sobre una estación para añadirla o quitarla de favoritos. Se admiten hasta **6 favoritos**. `SUPR` elimina el favorito seleccionado.

Los favoritos se guardan en los ajustes del usuario y permanecen después de reiniciar.

## 7. Menú CAR / Vehículo

La pestaña **CAR / VEHÍCULO** contiene funciones opcionales de comodidad, iluminación e indicadores, según el vehículo: intermitentes automáticos, emergencia con `NUM7`, luces largas con `NUM9`, Beat Neon, luz de cabina, luz de matrícula, Turbo Blow-Off, velocímetro / Mini HUD, logotipo del fabricante, RPM e indicadores del vehículo.

Algunos vehículos especiales pueden omitir intencionadamente ciertas funciones.

## 8. Luces y luz de matrícula

v1.0.2 Beta Test usa la lógica probada v7.41:

- La luz de matrícula permanece **APAGADA con luz diurna normal**.
- La luz ambiental, sombras, DRL o estados automáticos de GTA no la encienden por sí solos.
- Una orden real del conductor sobre las luces puede activarla durante el día.
- De noche sigue las luces de cruce/carretera reales.
- Los cambios breves de estado se filtran para evitar parpadeos.
- `PLATE LIGHT` debe estar activado en el menú CAR.

El neón estático puede aportar el color de la luz de matrícula. Sin neón estático, usa la familia de color de faros normales/xenón. Beat Neon no hace parpadear la matrícula.

## 9. Turbo Blow-Off

**Turbo Blow-Off** tiene solo `ON / OFF`. Cuando está activo, se genera una sola descarga tipo SPORT después de carga real del turbo al subir de marcha o al soltar claramente el acelerador después del boost.

La lógica anti-spam evita repeticiones. Funciona también a alta velocidad y en el aire. Se pueden añadir modelos extra en `InternetRadio.ini` mediante `FactoryTurboModels=`.

## 10. HUD, velocímetro e indicadores

Vehículos de carretera usan velocímetro/RPM/datos, los barcos usan HUD marítimo y los aviones/helicópteros usan HUD de vuelo.

La velocidad usa la velocidad real del vehículo de GTA. Los logotipos se cargan desde texturas HUD de GTA y se usa un símbolo neutro si no existe uno compatible.

## 11. Ajustes y guardado

Los ajustes personales se guardan automáticamente en `GTA V/scripts/InternetRadio/UserSettings.ini`. Incluyen numerosos ajustes de interfaz, audio, vehículo, favoritos, última fuente y última estación.

`InternetRadio.ini` contiene la configuración principal, lista de estaciones, valores técnicos y opciones para add-ons. Haz copia de seguridad de cambios manuales antes de actualizar.

## 12. Instalación, actualización y desinstalación

### Instalación nueva

Copia la carpeta completa `scripts` del paquete beta en la carpeta principal de GTA V.

### Actualización

1. Cierra GTA V.
2. Opcionalmente guarda una copia de `scripts/InternetRadio/UserSettings.ini`.
3. Copia los nuevos archivos `scripts` sobre los antiguos.
4. Conserva `UserSettings.ini` para mantener tus ajustes personales.
5. Inicia GTA V y prueba el mod.

### Desinstalación

Elimina `scripts/03_InternetRadioSimple.3.cs` y `scripts/InternetRadio/`. No borres otros mods del directorio `scripts`.

## 13. Solución de problemas

**El menú no se abre:** comprueba ScriptHookV, ScriptHookVDotNet, Singleplayer y un vehículo compatible.

**Error de compilación C#:** revisa `ScriptHookVDotNet.log`, la versión SHVDN y que todos los archivos SHVDN provengan del mismo paquete. Referencia probada: **ScriptHookV .NET Enhanced 3.9.0.6 (1.1.0.6), API 3.9.0**.

**La radio no reproduce:** prueba otra estación; los streams de terceros pueden estar offline o bloqueados por región.

**Spotify/YouTube aparece NO CONECTADO:** inicia primero la reproducción en la app/navegador y comprueba que Windows detecte una sesión multimedia.

**Los ajustes no se guardan:** comprueba permisos de lectura/escritura/modificación en `GTA V/scripts/InternetRadio/`.

**La luz de matrícula no se enciende:** activa `PLATE LIGHT` y enciende las luces reales del vehículo.

**La luz de matrícula se enciende de día o parpadea:** informa del vehículo, hora, posición de luces y, si es posible, adjunta un vídeo corto.

**Los cambios del INI no se aplican:** pulsa `F8` o reinicia GTA V.

## 14. Beta Test: qué informar

Incluye, si es posible: GTA V **Enhanced o Legacy**, versión de ScriptHookVDotNet, nombre del vehículo / spawn name del add-on, fuente activa, ajuste activo, pasos exactos para reproducir, captura o vídeo para problemas visuales/de iluminación y líneas relevantes de `ScriptHookVDotNet.log`.

Áreas prioritarias de v1.0.2: panel derecho de la UI, luz de matrícula, Turbo Blow-Off, persistencia de ajustes y estado de conexión Spotify/YouTube.

---

**LOS SANTOS INTERNET RADIO LS v1.0.2 BETA TEST**  
Modificación no oficial para GTA V Singleplayer.
