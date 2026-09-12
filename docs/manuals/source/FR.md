# LOS SANTOS INTERNET RADIO LS
## Manuel utilisateur - v1.0.2 BETA TEST

**Langue :** Français  
**Base testée :** v7.41  
**Utilisation :** GTA V Singleplayer

> **Important :** ce mod est destiné uniquement à GTA V Singleplayer. Si une session en ligne/réseau est détectée, les fonctions du mod sont désactivées.

## Sommaire

1. Démarrage rapide
2. Commandes
3. HOME et sources audio
4. Internet Radio
5. Spotify et YouTube Music
6. Favoris
7. Menu CAR / Véhicule
8. Éclairage et feu de plaque
9. Turbo Blow-Off
10. HUD, compteur et affichages véhicule
11. Réglages et sauvegarde
12. Installation, mise à jour et désinstallation
13. Dépannage
14. Beta Test : informations à signaler

---

## 1. Démarrage rapide

1. Installez **ScriptHookV** et **ScriptHookVDotNet**.
2. Copiez le dossier complet `scripts` du paquet dans le dossier principal de GTA V.
3. Vérifiez que le compte Windows utilisé pour lancer GTA V possède les droits **lecture/écriture/modification** sur `GTA V/scripts/InternetRadio/`.
4. Lancez GTA V en **Singleplayer**.
5. Entrez dans un véhicule compatible.
6. Appuyez sur `NUM0` pour ouvrir le menu multimédia.
7. Naviguez avec `NUM8 / NUM2` et validez avec `NUM5`.

Au premier lancement, `scripts/InternetRadio/UserSettings.ini` est créé automatiquement. Les préférences personnelles y sont enregistrées séparément de la configuration principale.

### Configuration de référence de cette bêta

- **ScriptHookV .NET Enhanced 3.9.0.6 (1.1.0.6)**
- **API 3.9.0**
- GTA V Enhanced / Singleplayer

D'autres versions SHVDN compatibles peuvent fonctionner. En cas d'erreur de compilation, vérifiez d'abord que `ScriptHookVDotNet.asi`, `ScriptHookVDotNet2.dll` et `ScriptHookVDotNet3.dll` proviennent du **même paquet de version**.

## 2. Commandes

| Touche | Fonction |
|---|---|
| `NUM0` | Ouvrir / fermer le menu multimédia |
| `NUM8 / NUM2` | Déplacer la sélection ; piste précédente/suivante dans Spotify/YouTube |
| `NUM4 / NUM6` | Changer d'onglet ; station/source précédente/suivante hors menu |
| `NUM5` | Sélectionner / appliquer / activer une source |
| `NUM1` | Source active on/off ou lecture/pause |
| `NUM3` | Stop/pause de la source active |
| `NUM- / NUM+` | Volume - / + |
| `NUM7` | Feux de détresse on/off |
| `NUM9` | Pleins phares manuels on/off |
| `ESPACE` | Ajouter/retirer une station des favoris |
| `SUPPR` | Supprimer le favori sélectionné |
| `F8` | Recharger la configuration |
| `ESC / BACK` | Fermer le menu |

`Num Lock` doit être activé.

## 3. HOME et sources audio

HOME est le centre du système. Il donne accès à Internet Radio, GTA Radio, YouTube Music, Spotify, CAR/Véhicule, skins, réglages et informations.

La dernière source active est restaurée si possible après un changement de véhicule ou un redémarrage. Internet Radio, GTA Radio, Spotify et YouTube Music sont suivis séparément.

## 4. Internet Radio

Choisissez les stations par catégorie. Le titre et l'artiste sont affichés lorsque le flux fournit des métadonnées.

Les flux sont gérés par des services tiers. Une station peut devenir indisponible, changer d'URL ou être bloquée selon la région indépendamment du mod. Si une station ne fonctionne pas, testez-en d'abord une autre.

## 5. Spotify et YouTube Music

Spotify et YouTube Music utilisent la session multimédia Windows/de l'application disponible.

1. Ouvrez Spotify ou YouTube Music.
2. Lancez un titre.
3. Ouvrez l'onglet correspondant dans le menu LS.
4. Appuyez sur `NUM5` pour activer la source.
5. `NUM1` = Lecture/Pause, `NUM8 / NUM2` = piste précédente/suivante.

Le panneau de droite indique l'état de connexion et le volume de l'application. **NON CONNECTÉ** signifie qu'aucune session multimédia correspondante n'est actuellement détectée.

## 6. Favoris

Appuyez sur `ESPACE` sur une station pour l'ajouter ou la retirer des favoris. Jusqu'à **6 favoris** sont pris en charge. `SUPPR` retire le favori sélectionné.

Les favoris sont enregistrés dans les paramètres utilisateur et restent disponibles après redémarrage.

## 7. Menu CAR / Véhicule

L'onglet **CAR / VÉHICULE** regroupe des fonctions optionnelles de confort, d'éclairage et d'affichage : clignotants automatiques, feux de détresse via `NUM7`, pleins phares via `NUM9`, Beat Neon, éclairage cabine, feu de plaque, Turbo Blow-Off, compteur / Mini HUD, logo constructeur et affichages RPM/véhicule.

Certains véhicules spéciaux peuvent volontairement ne pas utiliser toutes les fonctions.

## 8. Éclairage et feu de plaque

La v1.0.2 Beta Test utilise la logique v7.41 testée :

- Le feu de plaque reste **ÉTEINT en plein jour normal**.
- La lumière ambiante, les ombres, les DRL ou les états automatiques de GTA ne l'allument pas seuls.
- Une commande réelle du conducteur sur les phares peut l'activer de jour.
- La nuit, il suit les feux de croisement/route réels.
- Les changements d'état très courts sont filtrés pour éviter le scintillement.
- `PLATE LIGHT` doit être activé dans le menu CAR.

Un néon statique peut fournir la couleur du feu de plaque. Sans néon statique, la lampe suit la famille de couleur des phares normaux/xénon. Beat Neon ne fait pas pulser le feu de plaque.

## 9. Turbo Blow-Off

**Turbo Blow-Off** est volontairement limité à `ON / OFF`. Lorsqu'il est activé, un seul accent de décharge type SPORT est déclenché après une vraie charge turbo lors d'un passage au rapport supérieur ou d'un relâchement net de l'accélérateur après le boost.

La logique anti-spam empêche les répétitions. La détection fonctionne également à haute vitesse et en l'air. Des modèles add-on peuvent être ajoutés avec `FactoryTurboModels=` dans `InternetRadio.ini`.

## 10. HUD, compteur et affichages véhicule

Les véhicules routiers utilisent vitesse/RPM/données véhicule, les bateaux un HUD marin et les avions/hélicoptères un HUD de vol.

La vitesse est basée sur la vitesse réelle du véhicule dans GTA. Les logos constructeur utilisent les textures HUD de GTA, avec un symbole neutre si aucun logo compatible n'est disponible.

## 11. Réglages et sauvegarde

Les paramètres personnels sont automatiquement enregistrés dans `GTA V/scripts/InternetRadio/UserSettings.ini`. Cela comprend de nombreux réglages d'interface, audio, véhicule, favoris, dernière source et dernière station.

`InternetRadio.ini` contient la configuration principale, la liste des stations, les valeurs techniques et les options pour add-ons. Sauvegardez vos modifications manuelles avant une mise à jour.

## 12. Installation, mise à jour et désinstallation

### Nouvelle installation

Copiez le dossier complet `scripts` du paquet bêta dans le dossier principal de GTA V.

### Mise à jour

1. Fermez GTA V.
2. Sauvegardez éventuellement `scripts/InternetRadio/UserSettings.ini`.
3. Copiez les nouveaux fichiers `scripts` par-dessus les anciens.
4. Conservez `UserSettings.ini` pour garder vos préférences.
5. Relancez GTA V et testez le mod.

### Désinstallation

Supprimez `scripts/03_InternetRadioSimple.3.cs` et `scripts/InternetRadio/`. Ne supprimez pas les autres mods du dossier `scripts`.

## 13. Dépannage

**Le menu ne s'ouvre pas :** vérifiez ScriptHookV, ScriptHookVDotNet, le mode Singleplayer et un véhicule compatible.

**Erreur de compilation C# :** consultez `ScriptHookVDotNet.log`, la version SHVDN et assurez-vous que tous les fichiers SHVDN viennent du même paquet. Référence testée : **ScriptHookV .NET Enhanced 3.9.0.6 (1.1.0.6), API 3.9.0**.

**La radio ne joue pas :** testez une autre station ; les flux tiers peuvent être hors ligne ou bloqués régionalement.

**Spotify/YouTube affiche NON CONNECTÉ :** lancez d'abord la lecture dans l'application/le navigateur et vérifiez que Windows expose une session multimédia.

**Les réglages ne sont pas sauvegardés :** vérifiez les droits lecture/écriture/modification sur `GTA V/scripts/InternetRadio/`.

**Le feu de plaque reste éteint :** activez `PLATE LIGHT` et allumez les vrais phares du véhicule.

**Le feu de plaque s'allume de jour ou scintille :** signalez le véhicule, l'heure, la position des phares et, si possible, une courte vidéo.

**Les modifications INI n'ont aucun effet :** appuyez sur `F8` ou redémarrez GTA V.

## 14. Beta Test : informations à signaler

Merci d'indiquer si possible : GTA V **Enhanced ou Legacy**, version de ScriptHookVDotNet, nom du véhicule / spawn name de l'add-on, source active, réglage actif, étapes exactes pour reproduire, capture/vidéo pour les problèmes d'interface ou d'éclairage et lignes utiles de `ScriptHookVDotNet.log`.

Points prioritaires v1.0.2 : panneau droit de l'UI, feu de plaque, Turbo Blow-Off, persistance des réglages et état de connexion Spotify/YouTube.

---

**LOS SANTOS INTERNET RADIO LS v1.0.2 BETA TEST**  
Modification non officielle pour GTA V Singleplayer.
