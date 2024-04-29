# Procedural Stages

Procedural Stages replaces conventional static terrains with procedurally generated environments, offering a fresh and varied experience with each stage while striving to maintain the familiar feel of vanilla stages.

[Changelog](https://thunderstore.io/package/Lawlzee/ProceduralStages/changelog/)

## Features

- **Diverse Terrain Types**: Explore islands, open caves, twisted canyons and tunnel caves, each dynamically generated for a fresh adventure.
- **Balanced Gameplay**: Despite the procedural generation, the stages are balanced to feel like vanilla stages.
- **Multiplayer Support:** Enjoy the procedural adventure with friends.
- **Integration with Simulacrum:** Seamlessly compatible with Simulacrum.
- **Adaptive Map Size:** The map dynamically adjusts in size based on the stage number.
- **Dynamic Map Themes:** Experience different visual themes with every stage.
- **Stage-Specific Interactables:** Encounter stage-specific elements such as pressure plates, timed security chests, legendary chests, and the stage 4 alloy vulture nests.
- **Random Decoration Placement:** Discover unique environments with randomly placed decorations.
- **Randomized Enemy Pool:** Experience a dynamically curated assortment of enemies.
- **Varied Music Selection:** Immerse yourself in randomly selected music tracks.
- **Support for 'Judgement' Mod:** Compatible with the `Judgement` mod.
- **Teleport To Playable Area button**: Added a "Teleport To Playable Area" button in the pause menu. This button allows players to escape from being stuck in inaccessible areas, such as holes or glitches, by instantly teleporting them back to a playable area.

## Terrain Types

## Island Stages

![Image3](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.7/Image5.png)
![Image4](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.7.1/Image5.png)
![Image6](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.7/Image3.png)

## Open Caves Stages

![Image5](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.8/Image4.png)
![Image2](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.5/Image2.png)
![Image1](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.5/Image3.png)

## Tunnel Caves Stages

![Image5](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.9/Image5.png)
![Image2](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.9/Image7.png)
![Image1](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.9/Image9.png)

## Twisted Canyons Stages

![Image5](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.11/Image1.png)
![Image2](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.11/Image7.png)
![Image1](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.11/Image3.png)

[More screenshots are available here.](https://github.com/Lawlzee/UnityMapGenerator/tree/master/Mod/Images)

## Videos

Below are several videos that demonstrate the mod in action:
- [Video by LimeLight](https://www.youtube.com/watch?v=CDH7QYtNGvc&lc=UgziI767yJ6zojgI77R4AaABAg)
- [Video by TrentoMento](https://www.youtube.com/watch?v=5wyeGS0PbeU)
- [Video by PixelClub](https://www.youtube.com/watch?v=dBWXATNUGjY)


## Configuration

You can edit the configuration in `Settings > Mod Options > ProceduralStages`. All configurations can be adjusted at any time, even in the middle of a run. In multiplayer, the host's configuration is used.

| Category      | Name                 | Default value    | Description                                                                                                                                                                      |
|---------------|----------------------|------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|                                        
| Configuration | Infinite map scaling | Disabled         | If enabled, the stage size scaling will not be reset every loop. Exercise caution when utilizing this feature, as it may lead to increased map generation time and a decrease in framerate. |
| Stage `[1,5]` | `<Terrain type>` map spawn rate | See table below | Specifies the percentage of maps that will be generated with the `<Terrain type>` for stage 1. If the total percentage for stage `X` is less than 100%, normal stages may also spawn. If the total percentage for stage `X` is 0%, only normal stages will spawn.|
| Debug         | Stage seed           |                  | Specifies the stage seed. If left blank, a random seed will be used.                                                      |

Here are the default spawn rates for all terrain types:

| Stage   | Open Caves | Tunnel Caves | Islands | Twisted Canyons |
|---------|------------|--------------|---------|-----------------|
| Stage 1 | 40%        | 20%          | 20%     | 20%             |
| Stage 2 | 40%        | 20%          | 40%     | 20%             |                 
| Stage 3 | 10%        | 40%          | 10%     | 40%             |
| Stage 4 | 20%        | 40%          | 20%     | 20%             |
| Stage 5 | 20%        | 20%          | 20%     | 40%             |

## Report an issue

If you encounter any issues, feel free to reach out to me on Discord (@Lawlzee) or create a [GitHub issue](https://github.com/Lawlzee/UnityMapGenerator/issues/new). Please include your log file; it is really useful for troubleshooting.

### Credits stuff

|Stage number|Interactable credits|Monster credit|
|--|--|--|
|1|225|150|
|2|300|180|
|3|375|210|
|4|425|240|
|5|500|270|

## Todo list

- Fix the bug where the player sometime gets stuck in the drop pod.
- Add biomes/map types (snow, grass, desert, etc)
- Add props clustering
- Add floating island map?
- Replace special stages with procedural maps (Gilded Coast, Void Fields, Void Locus)
- Improve compatibility with Ro2API.DirectorAPI
- Randomize more terrain settings
- Create a random DccsPool instead of reusing the DccsPools from the game
- Replace the moon with a procedurally generated stage
- Randomize stage names
- Address the issue of getting stuck in holes in the map.
- Enhance stage creation performance.
- Enhance the uniqueness of stages.


## Algorithms used

This section includes the algorithms utilized for implementing procedural terrain generation. If you are not a programmer, feel free to skip this section.

- [Spaghetti caves, Fractional Brownian Motion, domain mapping and more](https://www.youtube.com/watch?v=ob3VwY4JyzE)
- [2D / 3D Cellular Automata](https://www.youtube.com/watch?v=v7yyZZjF1z4&list=PLFt_AvWsXl0eZgMK_DT5_biRkWXftAOf9)
- [2D / 3D Perlin noise](https://en.wikipedia.org/wiki/Perlin_noise)
- [Marching Cubes](https://www.youtube.com/watch?v=M3iI2l0ltbE)
- [Mesh simplification](https://www.youtube.com/watch?v=biLY19kuGOs)
    - [UnityMeshSimplifier](https://github.com/Whinarn/UnityMeshSimplifier)