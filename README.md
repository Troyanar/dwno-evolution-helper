# Digimon World Next Order Evolution Helper

A companion window for **Digimon World: Next Order** (Steam, Windows). It reads your partners' stats live from the
game and shows which Digimon they can digivolve into, what is still missing and when it will happen.

**[⬇ Download the latest version](../../releases/latest)** – unzip, run `DWNO-Evolution-Helper.exe`.

## Features

- **Live partners:** stats, age, lifetime, ✓/✗ for every condition of every possible digivolution and what is missing
- **"According to the game":** the game's own decision (`PartnerCtrl.CheckEvolution()`), plus a prediction
- **When:** days until the digivolution window, game time of the next day change and an estimate in real time
- **Training plan:** measures your actual training sessions and calculates how often to use which gym
- **Warnings:** conditions close to a limit, lifetime running out, partners' ages drifting apart
- **Digidex:** every Digimon with its requirements, pre-evolutions, evolutions and the path from your partner
- **Target Digimon per partner**, both partners side by side, a log of every digivolution
- **In-game hotkeys:** Ctrl+Shift+E show/hide · Ctrl+Shift+D click-through
- English by default; also Deutsch, Français, Italiano, Español, Português (BR), 日本語, 한국어, 繁體中文 or
  "Auto" (same language as the game)

## Setup

1. Close the game and run `DWNO-Evolution-Helper.exe` (Windows SmartScreen: "More info" → "Run anyway").
2. Click **Install** – this downloads the mod loader [BepInEx](https://github.com/BepInEx/BepInEx) (official build)
   into the game folder and adds the small plugin.
3. Start the game and load your save. The Digidex also works without the game.

Uninstall: ⚙ → "Remove plugin"; to remove BepInEx itself, delete `BepInEx\` and `winhttp.dll` in the game folder.

## How it works

- `app/` – the EXE (WPF, .NET Framework 4.8, runs on any Windows 10/11). It embeds the plugin, the Digimon pictures
  (`Resources/icons.zip`) and a Digimon table (`Resources/gamedata.json`).
- `plugin/` – BepInEx plugin. Reads the game data and writes to `%LOCALAPPDATA%\DWNO-EvoHelper\`
  (`gamedata.json`, `live.json`, `evolog.jsonl`). It does not change the game or your save.

The digivolution requirements come straight from the game data (`ParameterDigimonData.m_evo_*`):
`999999` = no requirement, positive = minimum, negative = maximum, `m_evo_matches` = how many must be met.

## Building

Requires the .NET 8 SDK. The plugin is built against the interop DLLs that BepInEx generates on the first game start.

```
dotnet build plugin -c Release -o plugin\bin\out -p:GameDir="<game folder>"
dotnet build app -c Release
```

## License and notice

The code is released under the [MIT License](LICENSE); exceptions are listed in [NOTICE](NOTICE).

Fan project, not affiliated with Bandai Namco. Digimon names, pictures and game data © Akiyoshi Hongo,
Toei Animation, Bandai Namco Entertainment – they are not covered by the MIT License.
Font: Public Sans (SIL Open Font License, `app/Fonts/OFL.txt`).
