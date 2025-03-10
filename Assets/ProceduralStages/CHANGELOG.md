## 2.0.2  
1. **Updated for Latest Game Version:** Ensured compatibility with the most recent update.  
2. **PhotoMode Crash Fix:** Resolved a crash that occurred when pausing the game during the Mithrix fight while using the PhotoMode mod.

## 2.0.1  
1. **Multiplayer Stage Desync Fix:** Resolved an issue causing stage desynchronization in multiplayer.  

## 2.0
1. **Custom Themes for Vanilla Stages and Main Menu:** Introduced custom themes for vanilla stages and the main menu, currently available only for non-DLC stages.  
2. **Default Spawn Rate Adjustment:** Reduced the default spawn rate for procedural stages to allow vanilla stages with custom themes to spawn.  
3. **Stage Loading Fix:** Resolved an issue where stages would sometimes fail to load.  

## 1.20
1. **Prop Rendering Fix:** Resolved an issue where props would occasionally fail to render.
2. **Lunar Fields Gravity Adjustment:** Updated the gravity in Lunar Fields to match the vanilla moon's gravity.
3. **Procedural Stage Command:** Introduced the `ps_set_stage` command to allow switching between procedural stages.
    - Syntax: `ps_set_stage <terrain_type> <theme> <stage_count>`.
    - Available terrain types: `Random`, `OpenCaves`, `Islands`, `TunnelCaves`, `Mines`, `Basalt`, `Towers`, `Temple`, `Moon`, and `PotRolling`.
    - Available themes: `Random`, `LegacyRandom`, `Desert`, `Snow`, `Void`, `Plains`, and `Mushroom`.
4. **Lighting and Fog Adjustment:** Improved lighting and fog settings to brighten the stage.

## 1.19
1. Added compatibility to Seekers of the Storm

## 1.18
1. **Minimum Stage Completion Configuration:** Added an option to set a minimum number of completed stages before procedural stages can spawn.
2. **Terrain Repetition Control:** Introduced a configuration option to prevent the repetition of terrain types in each loop.
3. **Public Gauntlet Mod Compatibility:** Added support for the [Public Gauntlet](https://thunderstore.io/package/riskofresources/Public_Gauntlet/) mod.

## 1.17.3
1. **Lunar Fields Multiplayer Fixes:**
    1. Resolved terrain desync issues between players.
    2. Fixed the Mithrix fight issues.
    3. Antigravity bubbles now extend for all players when pillars are charged.
    4. Fixed the issue of lunar golems not spawning.
2. **Prop Synchronization:** Props are no longer desynced between players in multiplayer.
3. **Lunar Fields Music Update:** Lunar Fields now uses the same music as the vanilla moon stage.


## 1.17.2
1. Fixed lag spikes during the Mithrix fight by simplifying the air node graph. This should also improve the performance of pathfinding for flying enemies.

## 1.17.1
1. Fixed "Lunar Fields" not loading

## 1.17
1. **Introducing Lunar Fields Terrain:** This new terrain type, "Lunar Fields" replaces the previous moon terrain. In this stage, players traverse antigravity force fields between gravity spheres to charge the pillars. The number of pillars and the spawn rate of the stage are configurable.
2. **Varied Island Rocks:** Enhanced the appearance of rocks on island stages to reduce uniformity.

## 1.16
1. **Enhanced Outer Map Areas:** On Lonely Island, Basalt Isle, and Temple terrain types, islands will now be generated on the map's periphery, making these areas more interesting.
2. **Theme Spawn Rate Configuration:** Added a configuration option to adjust theme spawn rates.
3. **Unified Terrain Spawn Rate Configs:** Introduced configurations to adjust terrain spawn rates for all stages simultaneously.
4. **Improved Ground Graph Generation:** Rewrote the ground graph generation algorithm to enhance ground enemy pathfinding and reduce the likelihood of interactables spawning within decorations like trees.
5. **Multiplayer Terrain Sync Fix:** Fixed an issue in multiplayer where the host's terrain type configuration was not correctly synced with other players, causing discrepancies in terrain.

## 1.15
1. **New Terrain Type: Temple:** Introducing a circular ruin as a new terrain type.
2. **New Terrain Type: Block Maze:** Added a blocky maze as a new terrain variant.
3. **Enhanced Open Caves:** Open caves now feature varied topography instead of being flat, making the caves more interesting.
4. **Volcanic Particles:** Added volcanic particles to the basalt isle's volcano for enhanced visual effects.
5. **Improved Footstep Sounds:** Walking now produces sounds based on the material you are walking on, resolving the `X is missing surface def` log issue.

## 1.14
1. **Basalt Isle Cave:** The basalt isle now features a guaranteed cave leading to the center of the central mountain.
2. **Rocky Island Walls:** Enhanced the island walls to appear more rocky and realistic.
3. **Stalactites in Caves:** Added Stalactites to open caves ceilling.
4. **Stage Shadows:** Added shadows to the stages for improved visual depth.
5. **Decoration Pop Fix:** Resolved an issue where decorations would pop when close to the camera.
6. **Occlusion Culling Configuration:** Introduced a configurable delay for occlusion culling.
7. **White Triangle Flicker Fix:** Fixed the issue causing a flickering white triangle.
8. **Enemy Pool Fix:** Corrected an issue where enemies were being selected for the pool before reaching the minimum stage required for them to spawn. This resolves the problem of having too few monster types in the enemy pool.

## 1.13
1. **Basalt Isle:** Introducing a new stage type named Basalt Isle.
2. **Stage Generation Performance:** Improved map generation by ~2x
3. **Enhanced Visuals:** Detailed textures and refined blending techniques for improved visual aesthetics.
4. **Theme Enhancements:** Tweaked the textures and color palettes for better visuals.
5. **Culling Issue:** Fixed an issue where decorations would be culled when underwater.
6. **Enemy Pool Fix:** Addressed an issue where an enemy could be selected multiple times in the enemy pool, causing enemies to be less varied.

## 1.12
1. **New Themes**: Introduced five new themes: Plains, Desert, Snow, Void, and Mushroom. The old "random" theme will still be able to spawn.
2. **Stage Size Adjustments**: Balanced the sizes of different stages. Increased the size of Islands and Open caves, while reducing the size of tunnel caves and Twisted canyons.
3. **Theme-based Updates**: Altered stage names, descriptions, and dream messages to match the terrain type.
4. **Texture Enhancement**: Improved the quality of textures across the board.
5. **Compatibility Fixes**: Addressed compatibility issues with Starstorm 2.
6. **Removed dependency**: Removed dependency on `R2API.Language`.

## 1.11
1. **Twisted Canyons Stages**: Introducing a new stage type featuring twisted canyons.
2. **Teleport To Playable Area button**: Added a "Teleport To Playable Area" button in the pause menu. This button allows players to escape from being stuck in inaccessible areas, such as holes or glitches, by instantly teleporting them back to a playable area.
3. **Increased Water Level**: In the tunnel caves stages, the water level has been raised.
4. **Post Processing in Water**: Enhancements have been made to the visual effects within the water.
5. **Improved Cave Shapes**: The shapes of caves in the tunnel caves stages have been improved.

## 1.10
1. **Configurable Terrain Spawn Rates**: Spawn rates for different terrain types can now be customized for each stage (1 to 5) of the game. The lunar seers will also utilize these spawn rates to determine the destinations.
2. **Lunar Seers Terrain Choice**: The lunar seers in the bazaar will now display the terrain type of procedural stages.
3. **Multiplayer Terrain Synchronization Fix**: An issue where the terrain would differ slightly between players in multiplayer mode has been resolved.
4. **Host Mod Configuration in Multiplayer**: In multiplayer mode, the host's configuration will now apply to all players.

## 1.9

1. **Tunnel Caves Addition**: Introduced tunnel caves, reminiscent of [Minecraft's spaghetti caves](https://minecraft.fandom.com/wiki/Cave#Spaghetti_cave).
   - Tunnel caves now have a 25% chance of spawning.
   - Open caves also now have a 25% chance of spawning.
   - These caves are currently experimental and may exhibit peculiarities. Expect interactables to spawn in unusual locations. Players might encounter getting stuck in holes, necessitating the use of 'no clip' mode to escape. This issue will be addressed in a future update.
   
2. **Music Fix**: Rectified occasional issues with music not playing during teleporter events.

## 1.8
1. Overhauled overworld maps to resemble islands.
2. Introduced water puddles to cave stages.
3. Addressed a bug where teleporters and interactable objects failed to spawn in island maps.
4. Added an option to prevent the stage size scaling from resetting after each loop.
5. Added an option to specify the stage seed.

## 1.7.2
1. Fixed decoration not rendering/flickering (for real this time).

## 1.7.1
1. Fixed decoration not rendering/flickering.
2. Decreased overworld map size.
3. Raised the height threshold for teleportation out of the map zone.
4. Fixed missing collisions.

## 1.7

1. **Overworld Map Type**: A new map type, the overworld map, has been added. Now, 50% of the generated maps will be overworld maps.
2. **Performance Enhancement: Occlusion Culling**: Performance has been improved by implementing occlusion culling. This should lead to a significant increase in frames per second (fps), potentially doubling the previous performance.
3. **Improved Air Graph**: Enemies are now capable of flying over obstacles and pursuing the players even on top of walls.

## 1.6
1. **Stage 2 Pressure Plate Mechanic:** Stage 2 of the game will now have a 33% chance to include a pressure plate mechanic similar to the one found in Abandoned Aqueduct. Activating two pressure plates will spawn two Elite Elder Lemurians, each dropping a Kjaro's Band and a Runald's Band.
2. **Stage 3 Changes:**: Stage 3 now has a 33% chance to include a Timed Security Chest similar to Rallypoint Delta.
3. **Stage 4 Alloy Vulture Nests:** Stage 4 now has a 33% chance to include Alloy Vulture Nests, resembling the mechanic in Siren's Call. Destroying six eggs will spawn an Alloy Worship Unit, rewarding players with a legendary item for each player in the game.
4. **Stage 4 legendary chest chance reduced to 66%:** The likelihood of encountering a legendary chest in Stage 4 has been reduced to 66%.
5. **Increased Map Size:** The overall size of the game map has been increased, potentially providing a larger and more expansive environment for players to explore.
6. **Missing Collisions for Decorations:** Addressed missing collisions for decorations within the game.
7. **Interactables No Longer Spawn Inside Rocks:** A bug or issue where interactable objects were spawning inside rocks has been fixed, preventing inconvenient situations for players.
8. **Compatibility with [Judgement Mod](https://thunderstore.io/package/Nuxlar/Judgement/):** The mod has been updated to ensure compatibility with the [Judgement mod](https://thunderstore.io/package/Nuxlar/Judgement/).

## 1.5
- Added Decorations
   - Trees, grass, rocks, vines, etc., have been introduced as decorative elements in the environment.
- Dynamic Stage Size
   - The stage size is now scaled based on the current stage count. As the stage count increases, the size of the map also grows accordingly.

## 1.4
- Fixed compatibility issue with the BetterLoadingScreen mod
  - Resolved an issue that was causing compatibility problems with the BetterLoadingScreen mod. This fix ensures that both mods can work together smoothly.
- Improved simulacrum support
   - Addressed an issue where too many monsters were spawning in the simulacrum.
   - Modified the monster and interactable pool selection to come from a random simulacrum stage.
   - Removed Newt Altar
- Improved monster selection algorithm for normal stages
   - Enhanced the algorithm for selecting monsters.
   - Monsters from similar stage counts are now more likely to be selected, providing a more balanced experience.
- Removed dependencies
   - Eliminated the dependencies on `RiskofThunder-R2API_Stages` and `RiskofThunder-R2API_ContentManagement`.

## 1.3
- Added textures
- Added simulacrum support

## 1.2.1
- Fixed an issue where monsters would continue to spawn after the teleporter finished charging.
- Fixed an issue where music would sometimes not play.
- Players and enemies are now teleported when they clip out of the map.

## 1.2.0
- Implemented multiplayer support
- Enhanced map generation
  - Improved map smoothness
  - Optimized placement of interactables
  - Upgraded node graph generation
  - Simplified mesh for better performance
- Addressed various bugs
  - Fixed the problem where legendary chests on stage 4 consistently cost 400 gold
  - Resolved screen flashing during the loading of stage 1
  - Fixed the pings not working

## 1.1.0

- Visual improvements
  - The stage is now smoother and less blocky 
  - Added texture to the floor and walls
  - Added fog
  - Improved the lighting
- Improved interactable placements
  - Newt altars are more hidden 
  - Teleporters now spawn in flatter and more open areas
  - Capped the height where interactables can spawn
- Added a guaranteed legendary chest on stage 4.
- Improved music selection
- Performance improvements
  - Optimized the stage mesh
  - Optimized node graph
- Fixed the bug where the screen flashes white when entering a void seed

## 1.0.1
- Added missing dependency

## 1.0.0

- First release