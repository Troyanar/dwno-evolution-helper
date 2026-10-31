# DWNO Evo-Helfer

Ein Helfer-Fenster für **Digimon World: Next Order** (Steam, Windows): Es liest die Werte deiner Partner live
aus dem Spiel und zeigt, zu welchen Digimon sie digitieren können, was noch fehlt und wann es so weit ist.

**[⬇ Neueste Version herunterladen](../../releases/latest)** – ZIP entpacken, `DWNO-Evo-Helfer.exe` starten.

## Was er kann

- **Partner live:** Werte, Alter, Lebenszeit, für jedes mögliche Ziel ✓/✗ pro Bedingung und was noch fehlt
- **Laut Spiel:** die Entscheidung des Spiels selbst (`PartnerCtrl.CheckEvolution()`), dazu eine Vorhersage
- **Wann:** Tage bis zur Digitation, Spieluhrzeit des nächsten Tageswechsels und geschätzte echte Zeit
- **Trainingsplan:** misst deine echten Trainings und rechnet aus, wie oft welche Arena noch nötig ist
- **Warnungen:** knappe Bedingungen, Lebenszeit reicht nicht, Partner-Alter laufen auseinander
- **Digidex:** alle Digimon mit Bedingungen, Vorstufen, Zielen und dem Weg von deinem Partner
- **Ziel-Digimon je Partner**, beide Partner nebeneinander, Protokoll jeder Digitation
- **Tastenkürzel im Spiel:** Strg+Umschalt+E ein-/ausblenden · Strg+Umschalt+D Durchklicken
- Sprache folgt dem Spiel (de, en, fr, it, es, pt-BR, jp, kr, tc)

## Einrichten

1. Spiel beenden, `DWNO-Evo-Helfer.exe` starten (Windows-Warnung: „Weitere Informationen“ → „Trotzdem ausführen“).
2. **Installieren** klicken – lädt den Mod-Loader [BepInEx](https://github.com/BepInEx/BepInEx) (offizieller Build) in den
   Spielordner und legt das kleine Plugin dazu.
3. Spiel starten, Spielstand laden. Der Digidex funktioniert auch ohne Spiel.

Entfernen: ⚙ → „Plugin entfernen“; BepInEx selbst: im Spielordner `BepInEx\` und `winhttp.dll` löschen.

## Aufbau

- `app/` – die EXE (WPF, .NET Framework 4.8, läuft auf jedem Windows 10/11). Bettet das Plugin, die Digimon-Bilder
  (`Resources/icons.zip`) und eine Digimon-Tabelle (`Resources/gamedata.json`) ein.
- `plugin/` – BepInEx-Plugin. Liest die Spieldaten und schreibt nach `%LOCALAPPDATA%\DWNO-EvoHelper\`
  (`gamedata.json`, `live.json`, `evolog.jsonl`). Es verändert nichts am Spiel oder Spielstand.

Die Digitationsbedingungen stammen direkt aus den Spieldaten (`ParameterDigimonData.m_evo_*`):
`999999` = keine Bedingung, positiver Wert = Mindestwert, negativer Wert = Höchstwert, `m_evo_matches` = nötige Anzahl.

## Bauen

Benötigt das .NET-8-SDK. Das Plugin wird gegen die Interop-DLLs gebaut, die BepInEx beim ersten Spielstart erzeugt.

```
dotnet build plugin -c Release -o plugin\bin\out -p:GameDir="<Spielordner>"
dotnet build app -c Release
```

## Lizenz und Hinweis

Der Code steht unter der [MIT-Lizenz](LICENSE).

Fan-Projekt, nicht mit Bandai Namco verbunden. Digimon-Namen, -Bilder und Spieldaten © Akiyoshi Hongo,
Toei Animation, Bandai Namco Entertainment – sie fallen nicht unter die MIT-Lizenz.
Schrift: Public Sans (SIL Open Font License, `app/Fonts/OFL.txt`).

---

### English

A companion window for **Digimon World: Next Order** (Steam, Windows). It reads your partners' stats live from the game
and shows which Digimon they can digivolve into, what is still missing and when it happens – plus a Digidex, a
training plan, warnings and in-game hotkeys. Download the latest release, run `DWNO-Evo-Helfer.exe`, click
**Install** (sets up BepInEx and the plugin), start the game. The UI follows the game language.
