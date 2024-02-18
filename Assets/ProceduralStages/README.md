# Procedural Stages

Procedural Stages replaces conventional static terrains with procedurally generated environments, offering a fresh and varied experience with each stage while striving to maintain the familiar feel of vanilla stages.

## How does the generation works ?
At the beginning of each stage, a newly procedurally generated environment is created. The spawn pools for monsters and interactables, as well as the music selection, are randomly chosen from various stages.

## Screenshots

![Image1](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.5/Image3.png)
![Image2](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.5/Image2.png)
![Image3](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.5/Image6.png)
![Image4](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.5/Image7.png)
![Image5](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.5/Image1.png)
![Image6](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.5/Image8.png)

## Configuration

You can edit the configuration in `Settings > Mod Options > ProceduralStages`.

|Catgeory|Name|Default value|Description|
|--|--|--|--|
|Configuration|Replace all stages|Enabled|If enabled, all stages will be procedurally generated. If disabled, normal stages and procedurally generated stages will be used.

More configurations coming soon!

## Report an issue

If you encounter any issues, feel free to reach out to me on Discord (@Lawlzee) or create a [GitHub issue](https://github.com/Lawlzee/UnityMapGenerator/issues/new).

### Credits stuff

|Stage number|Interactable credits|Monster credit|
|--|--|--|
|1|225|150|
|2|300|180|
|3|375|210|
|4|425|240|
|5|500|270|

## Todo list
- [X] Support multiplayer
- [X] Be compatible with the Risk of Resources Gauntlet mod
- [X] Make the color palette less blue
- [X] Add decorations to the map (grass, trees, rocks, pillars, etc.)
- [X] Optimize the NodeGraphs
- [X] Optimize the map mesh
- [X] Fix the bug where the screen flashes white when entering the first stage
- [X] Fix the bug where the screen flashes white when entering a void seed
- [X] Add a dead barrier if the player clips through the map
- [X] Improve music selection
- [X] Add more post-processing effects.
- [X] Implement Simulacrum support.
- [X] Optimize stage performance.
- [X] Prevent interactables from spawning on slopes.
- [X] Fix the bug where the player sometime gets stuck in the drop pod.
- [X] Legendary chest on stage 4 always cost 400
- [X] Fix drones spawning out of the map
- [X] Add compatibility with the `Judgement` mod
- [X] Improve the orientation of Newt altars.
- [X] Add stage unique interactables. For example, add a red chest on stage 4
- [X] Add collision to props
- [ ] Add more configurations
- [ ] Create a random DccsPool instead of reusing the DccsPools from the game
- [ ] Replace the moon with a procedurally generated stage
- [ ] Randomize stage names
- [ ] Address the issue of getting stuck in holes in the map.
- [ ] Enhance stage creation performance.
- [ ] Enhance stage runtime performance.
- [ ] Enhance the uniqueness of stages.
- [ ] Remove the ability to dream for normal stages


## Algorithms used

This section includes the algorithms utilized for implementing procedural terrain generation. If you are not a programmer, feel free to skip this section.

- [2D / 3D Cellular Automata](https://www.youtube.com/watch?v=v7yyZZjF1z4&list=PLFt_AvWsXl0eZgMK_DT5_biRkWXftAOf9)
- [2D / 3D Perlin noise](https://en.wikipedia.org/wiki/Perlin_noise)
- [Marching Cubes](https://www.youtube.com/watch?v=M3iI2l0ltbE)
- [Mesh simplification](https://www.youtube.com/watch?v=biLY19kuGOs)
    - [UnityMeshSimplifier](https://github.com/Whinarn/UnityMeshSimplifier)