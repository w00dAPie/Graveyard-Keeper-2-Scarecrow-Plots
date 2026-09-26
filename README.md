# Graveyard Keeper 2 - Scarecrow Plots

A small BepInEx mod for Graveyard Keeper 2 that restores the two garden plots normally occupied by the scarecrow.

The scarecrow can optionally be hidden while keeping the restored garden plots usable.

## Features

- Adds the two missing garden plots around the scarecrow
- Automatically detects upgraded garden layouts
- Supports changed and nested scarecrow hierarchies
- Supports differently positioned garden layouts
- Uses the game's normal garden plot and save system
- Existing crops and garden behavior work normally
- Prevents duplicate plots from being created
- Can optionally hide the scarecrow, including its renderers and colliders
- Can safely remove plots created by the mod when disabled
- Avoids continuous scarecrow polling during gameplay

## Requirements

- Graveyard Keeper 2
- BepInEx 5.4.23.5

## Installation

1. Install BepInEx 5.4.23.5 for Graveyard Keeper 2.
2. Extract the archive into your Graveyard Keeper 2 installation directory.

The DLL should end up here:

`BepInEx/plugins/GK2ScarecrowPlots/GK2ScarecrowPlots.dll`

## Configuration

After the first launch, the configuration file is created in:

`BepInEx/config/de.w00dst0ckOo.gk2.scarecrowplots.cfg`

Example:

```ini
[General]

Enabled = true
HideScarecrow = false

## License

MIT