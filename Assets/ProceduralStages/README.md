# Procedural Stages

Adds procedurally generated stages to Risk of Rain 2. Not multiplayer compatible yet.

## How does the generation works ?
At the beginning of each stage, a newly procedurally generated environment is created. The spawn pools for monsters and interactables, as well as the music selection, are randomly chosen from various stages.

### Credits stuff

|Stage number|Interactable credits|Monster credit|
|--|--|--|
|1|225|150|
|2|300|180|
|3|375|210|
|4|425|240|
|5|500|270|

## Configuration

You can edit the configuration in `Settings > Mod Options > ProceduralStages`.

|Catgeory|Name|Default value|Description|
|--|--|--|--|
|Configuration|Replace all stages|Enabled|If enabled, all stages will be procedurally generated. If disabled, normal stages and procedurally generated stages will be used.

todo: add more configurations

## Screenshots

![Image1](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.2/Image1.png)
![Image2](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.2/Image2.png)
![Image4](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.2/Image4.png)
![Image5](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.2/Image5.png)
![Image6](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.2/Image6.png)
![Image3](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.2/Image8.png)

## Todo list
- [X] Support multiplayer
- [X] Be compatible with the Risk of Resources Gauntlet mod
- [X] Make the color palette less blue
- [ ] Add decorations to the map (grass, trees, rocks, pillars, etc.)
- [ ] Add more configurations
- [ ] Add stage unique interactables. For example, add a red chest on stage 4
- [x] Optimize the NodeGraphs
- [x] Optimize the map mesh
- [X] Fix the bug where the screen flashes white when entering the first stage
- [x] Fix the bug where the screen flashes white when entering a void seed
- [ ] Add a dead barrier if the player clips through the map
- [ ] Create a random DccsPool instead of reusing the DccsPools from the game
- [x] Improve music selection
- [ ] Replace the moon with a procedurally generated stage
- [ ] Randomize stage names
- [ ] Add more post-processing effects.
- [ ] Address the issue of getting stuck in holes in the map.
- [ ] Implement Simulacrum support.
- [ ] Enhance stage creation performance.
- [X] Optimize stage performance.
- [ ] Enhance the uniqueness of stages.
- [X] Prevent interactables from spawning on slopes.
- [ ] Fix the bug where the player sometime gets stuck in the drop pod.
- [ ] Remove the ability to dream for normal stages
- [ ] Improve the orientation of Newt altars.
- [X] Legendary chest on stage 4 always cost 400
- [ ] Fix drones spawning out of the map


## Report an issue

If you encounter any issues, feel free to reach out to me on Discord (@Lawlzee) or create a [GitHub issue](https://github.com/Lawlzee/UnityMapGenerator/issues/new).

## Algorithms used

This section includes the algorithms utilized for implementing procedural terrain generation. If you are not a programmer, feel free to skip this section.

- [2D / 3D Cellular Automata](https://www.youtube.com/watch?v=v7yyZZjF1z4&list=PLFt_AvWsXl0eZgMK_DT5_biRkWXftAOf9)
- [2D / 3D Perlin noise](https://en.wikipedia.org/wiki/Perlin_noise)
- [Marching Cubes](https://www.youtube.com/watch?v=M3iI2l0ltbE)
- [Mesh simplification](https://www.youtube.com/watch?v=biLY19kuGOs)
    - [UnityMeshSimplifier](https://github.com/Whinarn/UnityMeshSimplifier)