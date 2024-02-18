## 1.6
- Added Judgement compatibility
- Interactables can not longer spawn inside of rocks
- Added ring event
- Added AWU event
- Added missing collisions to decorations
- Increased map size

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