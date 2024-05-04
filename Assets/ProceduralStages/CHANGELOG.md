## 1.12
1. Fixed compatibility issue with Starstorm 2
1. Random prop scale
1. Balanced stage size islands + canyons
1. Improve texture scale
1. Desert theme
1. Renamed stages (todo change readme)
1. Added new stage name, description and dream message

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