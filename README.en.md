[日本語](README.md)

# LWF Alraune Hotkeys

Give orders to Farming Alraune with hotkeys in **Lazy Witch's Factory**

![LWF Alraune Hotkeys](img/icon.png)

**[Download the latest release](https://github.com/KiyonakaNata/lwf-alraune-hotkeys/releases/latest)** / **[Thunderstore](https://thunderstore.io/c/lazy-witchs-factory/p/KiyonakaNata/LwfAlrauneHotkeys/)** (easy install with a mod manager)

---

## Features

### Orders by key

Hover over a placed Farming Alraune and press a key

| Key | Order |
|---|---|
| `G` | Crossbreeding |
| `H` | Cash |
| `J` | Mining |

- Hold the key and move the cursor → every Farming Alraune you pass gets the same order
- Keys do nothing over the inventory or a window

---

## Install (manual)

1. Install **BepInEx 5** — [releases](https://github.com/BepInEx/BepInEx/releases)
   - Download `BepInEx_win_x64_5.4.x.zip`
   - Extract it into the game folder (next to `LazyWitchsFactory.exe`)

     > **Where is the game folder?** (Steam)
     > Right-click the game in your library → Manage → Browse local files

2. Run the game once and quit, so that `BepInEx/plugins` is created
3. Put `LwfAlrauneHotkeys.dll` from this mod's zip into **`BepInEx/plugins/`**
4. Start the game, hover over a Farming Alraune and press `G`. The info panel under the cursor shows Crossbreeding - Plants or similar

## Uninstall

**This mod only**

- `BepInEx/plugins/LwfAlrauneHotkeys.dll`
- Also delete `BepInEx/config/kiyonakanata.lwfalraunehotkeys.cfg` to remove settings

**BepInEx entirely** (stops all other mods too)

- The `BepInEx` folder
- `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version` in the game folder

---

## Settings

`BepInEx/config/kiyonakanata.lwfalraunehotkeys.cfg` (created after running the game once)

With a mod manager, open it from the manager's Config editor

**[1. General]**

| Entry | Default | Values |
|---|---|---|
| Enabled | `true` | |
| Apply while held | `true` | `false`: only the one under the cursor when the key goes down |

**[2. Keys]**

| Entry | Default | Values |
|---|---|---|
| Crossbreeding | `G` | Key name (`G`, `F5`, `Digit1`, `Numpad1`, …). `None` turns it off |
| Cash | `H` | Same |
| Mining | `J` | Same |

Pick keys the game's Key Config does not use

---

## Requirements

| | |
|---|---|
| Lazy Witch's Factory | Tested on **ver 0.30.1** |
| BepInEx | Tested on **5.4.23.5** (any 5.4.x should work) |

If a game update breaks this mod, that is the end of its life — remove it.

## Troubleshooting

Check `BepInEx/LogOutput.log` first

| Log | State |
|---|---|
| No `[boot] LWF Alraune Hotkeys ...` line | **Not loaded** — check where the DLL is |
| `disabled` in a `[boot]` or `[tick]` line | **Stopped** — game version mismatch |

**Bug reports** should include

- A screenshot
- `BepInEx/LogOutput.log`

---

## Disclaimer

- **Unofficial mod** — not supported by the developer
- Any issues, crashes, or save corruption while modded are at your own risk
- Made in accordance with the [official modding policy](https://store.steampowered.com/news/app/3971650/view/699897618302503133)

Source code is MIT licensed
