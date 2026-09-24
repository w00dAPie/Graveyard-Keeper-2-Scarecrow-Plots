# Graveyard Keeper 2 - Scarecrow Plots

A small BepInEx mod for Graveyard Keeper 2 that restores the two garden plots normally occupied by the scarecrow.

The scarecrow itself remains visually unchanged. The mod only adds the two missing usable garden plots.

## Features

- Adds the two missing garden plots around the scarecrow
- Automatically detects the upgraded garden layout
- Does not depend on fixed world coordinates
- Supports differently positioned and rotated fields
- Uses the game's normal garden plot and save system
- Existing crops and garden behavior work normally
- Prevents duplicate plots from being created
- Can safely remove plots created by the mod when disabled

## Requirements

- Graveyard Keeper 2
- BepInEx 5.x

## Installation

1. Install BepInEx 5 for Graveyard Keeper 2.
2. Create the folder:

   `BepInEx/plugins/GK2ScarecrowPlots/`

3. Copy `GK2ScarecrowPlots.dll` into that folder.
4. Start the game.

## Configuration

After the first launch, the configuration file is created in:

`BepInEx/config/de.w00dst0ckOo.gk2.scarecrowplots.cfg`

The mod is enabled by default:

```ini
[General]

Enabled = true