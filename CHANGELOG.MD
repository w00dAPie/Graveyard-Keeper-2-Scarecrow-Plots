# Changelog

## 0.1.7

- Fixed scarecrow detection after upgrading the garden.
- Added support for changed and nested scarecrow hierarchies.
- Improved compatibility with different garden upgrade levels.
- Prevented incorrect grid fallback detection when the upgraded scarecrow is available.

## 0.1.6

### Changed

- Reduced unnecessary scarecrow and garden discovery work.
- Reused already discovered scarecrow references instead of performing redundant searches.
- Reduced duplicate garden processing where possible while preserving delayed initialization handling.
- Reduced allocations in fallback garden detection.
- Improved garden data collection efficiency.
- Reduced unnecessary configuration persistence work.
- Reduced debug formatting overhead when debug logging is disabled.

### Performance

- Improved behavior around area and garden loading by reducing avoidable scene and object processing.

## 0.1.5

### Fixed

- Fixed incorrect garden plot detection on saves where the scarecrow object is loaded after the garden zone.
- Added automatic garden reprocessing once the scarecrow instance becomes available.
- Improved scarecrow anchor detection reliability for larger or modified garden layouts.
- Prevented the grid fallback from being the final detection result when the actual scarecrow object becomes available later.

## 0.1.4

### Added

- Optional `HideScarecrow` setting
- Hides the scarecrow mesh and shadow
- Disables the scarecrow collision objects while hidden
- Disables the scarecrow `NoBuildArea` while hidden
- Automatically detects newly loaded scarecrow instances
- Reapplies the hidden state after save/load and garden area reloads
- Scarecrow transform detection using the actual `garden_t1(Clone)` hierarchy
- Grid-based scarecrow field detection remains available as fallback
- Automatic migration of v0.1.3 configuration values
- Migration support for existing `CreatedPlotIds`
- Dedicated configuration handling
- Dedicated logging helper
- Dedicated garden startup service
- Dedicated scarecrow watcher service

### Changed

- Configuration sections reorganized into:
  - `01 - General`
  - `02 - Visual`
  - `90 - Debug`
  - `99 - Internal`
- Legacy configuration values are migrated into the new section layout
- Plot processing and scarecrow visual handling are now separated
- Garden startup processing no longer depends on the scarecrow GameObject already being loaded
- Scarecrow hiding now works independently from garden plot detection
- Scarecrow renderer and collider handling now targets only the confirmed scarecrow hierarchy
- Existing scarecrow instances are only processed once per instance ID
- Refactored runtime logic into smaller dedicated classes and services
- Reduced responsibilities inside `Plugin.cs`
- Improved debug logging around garden detection, scarecrow detection and instance handling

### Fixed

- Fixed scarecrow hiding after save/load
- Fixed scarecrow collision remaining active after hiding the visual
- Fixed `NoBuildArea` remaining active after hiding the scarecrow
- Fixed scarecrow shadow remaining visible
- Fixed timing issues caused by the scarecrow GameObject not existing during early garden processing
- Fixed garden processing depending on scarecrow scene object availability
- Fixed duplicate processing of the same scarecrow instance
- Fixed configuration migration losing previously stored garden plot IDs
- Fixed migrated values being used as defaults instead of actual configuration values
- Fixed old configuration sections remaining after migration
- Improved reliability across save/load cycles
- Existing garden plots are detected before new plots are created
- Prevents duplicate scarecrow plots from being spawned

### Internal

- Added exact scarecrow root detection for:
  - `garden_t1(Clone)/Base/scarecrow_on_stick`
- Scarecrow hiding now disables:
  - `scarecrow_on_stick`
  - `scarecrow_on_stick_sh`
  - `Obstacle`
  - `Obstacle_1`
  - `NoBuildArea`
- Garden plot detection keeps the previous grid detector as a fallback
- Added instance-based scarecrow tracking for dynamically loaded garden scenes


## 0.1.3

### Added

- Added direct game-start fallback processing through `MainGame.OnGameStarted`
- Added detailed debug logging for garden processing and scarecrow detection
- Added shared garden zone processing logic

### Changed

- Garden processing can now run even when the normal world-zone hook is not sufficient
- Improved startup reliability when loading into different game states
- Consolidated plot processing into a common processor

### Fixed

- Improved reliability of garden plot creation after game load
- Fixed cases where the garden was not processed early enough
- Improved handling when garden data exists before all related scene objects are instantiated


## 0.1.2

### Changed

- Improved occupancy detection for the two scarecrow plot positions
- Improved direct-slot detection around the upgraded scarecrow field
- Tightened garden position matching

### Fixed

- Fixed duplicate plot creation in already occupied target positions
- Fixed incorrect handling when one or both scarecrow plots already existed
- Improved detection of existing garden beds at the target locations


## 0.1.1

### Changed

- Improved garden field detection
- Improved handling of the upgraded garden layout
- Refined detection tolerances for garden bed positions

### Fixed

- Improved reliability when detecting the scarecrow field geometry
- Reduced false detections outside the intended garden layout


## 0.1.0

### Added

- Initial release
- Restores the two missing usable garden plots around the upgraded scarecrow
- Automatically detects the scarecrow garden layout
- Uses the game's normal `garden_empty` objects
- Uses the normal game save system
- Prevents duplicate plots from being created
- Stores the unique IDs of plots created by the mod
- Allows mod-created plots to be removed when the mod is disabled
- Supports garden layouts without relying on a single fixed world position
- No dependency on the GK2 Mod Framework
- BepInEx-based standalone mod

### Known Behavior

- The scarecrow remains visible in the original release
- The two restored garden plots can overlap visually with the scarecrow
- Plot removal depends on the stored `CreatedPlotIds`
