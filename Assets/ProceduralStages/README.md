# Procedural Stages

Procedural Stages replaces conventional static terrains with procedurally generated environments, offering a fresh and varied experience with each stage while striving to maintain the familiar feel of vanilla stages.

[Changelog](https://thunderstore.io/package/Lawlzee/ProceduralStages/changelog/)

## Features

- **Diverse Terrain Types**: Explore islands, open caves, twisted canyons, basalt isle, tunnel caves, lunar fields, temple and block maze, each dynamically generated for a fresh adventure.
- **Dynamic Map Themes**: Experience different visual themes with every stage, including Plains, Desert, Snow, Void, Mushroom, and the old "random" theme.
- **Balanced Gameplay**: Despite the procedural generation, the stages are balanced to feel like vanilla stages.
- **Multiplayer Support:** Enjoy the procedural adventure with friends.
- **Integration with Simulacrum:** Seamlessly compatible with Simulacrum.
- **Adaptive Map Size:** The map dynamically adjusts in size based on the stage number.
- **Dynamic Map Themes:** Experience different visual themes with every stage.
- **Stage-Specific Interactables:** Encounter stage-specific elements such as pressure plates, timed security chests, legendary chests, and the stage 4 alloy vulture nests.
- **Random Decoration Placement:** Discover unique environments with randomly placed decorations.
- **Randomized Enemy Pool:** Experience a dynamically curated assortment of enemies.
- **Varied Music Selection:** Immerse yourself in randomly selected music tracks.
- **Teleport To Playable Area button**: Added a "Teleport To Playable Area" button in the pause menu. This button allows players to escape from being stuck in inaccessible areas, such as holes or glitches, by instantly teleporting them back to a playable area.
- **Support for 'Judgement' Mod:** Compatible with the `Judgement` mod.

## Stages

Each stage is randomly generated, featuring unique terrain types and themes:

- **Terrain Types:** Encounter islands, open caves, twisted canyons, basalt isle, tunnel caves, lunar fields, temple and block maze.
- **Themes:** Experience various visual themes including Plains, Desert, Snow, Void, Mushroom, and the legacy "Random" theme.

### Screenshots


![screenshot](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.15/Image20.png)
*Temple map with the plains theme*

![screenshot](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.12/Image12.png)
*Twisted canyons map with the desert theme*

![screenshot](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.15/Image2.png)
*Open cave map with the void theme*

![screenshot](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.13/Image8.png)
*Basalt isle map with the snow theme*

![screenshot](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.15/Image26.png)
*Block maze map with the mushroom theme*

![screenshot](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.15/Image16.png)
*Twisted canyons map with the snow theme*

![screenshot](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.12/Image11.png)
*Tunnel cave map with the mushroom theme*

![screenshot](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.15/Image21.png)
*Temple map with the snow theme*

![screenshot](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.15/Image13.png)
*Islands map with the mushroom theme*

![screenshot](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.13/Image13.png)
*Basalt isle map with the desert theme*

![screenshot](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.12/Image13.png)
*Twisted canyons map with the plains theme*

![screenshot](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.12/Image23.png)
*Tunnel cave map with the void theme*

![screenshot](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.12/Image7.png)
*Tunnel cave map with the snow theme*

![screenshot](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.15/Image12.png)
*Islands map with the void theme*

![screenshot](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.15/Image6.png)
*Open cave map with the snow theme*

![screenshot](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.15/Image25.png)
*Block maze map with the void theme*

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
| Performance   | Occlusion culling frame delay | 6 | The number of frames between each occlusion culling check impacts performance. A shorter delay decreases FPS, while a longer delay causes decorations to flicker more when moving quickly. The game operates at 60 frames per second. Any changes to this configuration will take effect at the start of the next stage.|
| Themes        | `<Theme>` spawn rate | See table below  | Specifies the percentage of stages that will be generated with the `<theme>` theme. |
| All Stages    | `<Terrain type>` map spawn rate | Varied | Sets the overall percentage of stages that will feature the `<Terrain type>` terrain type. Adjusting this value will automatically update the spawn rates for this terrain type in each individual stage. |
| Stage `[1,5]` | `<Terrain type>` map spawn rate | See table below | Specifies the percentage of maps that will be generated with the `<Terrain type>` for stage 1. If the total percentage for stage `X` is less than 15% , normal stages may also spawn. If the total percentage for stage `X` is 0%, only normal stages will spawn.|
| Moon          | Lunar Fields map spawn rate | 100%      | Indicates the percentage of final stages featuring the custom \"Lunar Fields\" terrain type instead of the vanilla moon stage. If this percentage is less than 100%, the normal moon stage will also appear. If the total percentage is 0%, only the normal moon stage will be generated. |
| Moon          | Required pillars     | 4                | Number of pillars necessary to access the Mithrix arena |
| Debug         | Stage seed           |                  | Specifies the stage seed. If left blank, a random seed will be used.                                                      |


### Themes

| Theme | Default spawn rate|
|-------|-------------------|
| Desert| 18 %              |
| Snow  | 18 %              |
| Void  | 18 %              |
| Plains| 18 %              |
| Mushroom| 18 %            |
| Legacy Random| 10 %       |

### Terrain types

Here are the default spawn rates for all terrain types:

| Stage   | Open Caves | Tunnel Caves | Lonely Island | Twisted Canyons | Basalt Isle | Temple | Block maze |
|---------|------------|--------------|---------------|-----------------|-------------|--------|------------|
| Stage 1 | 5%         | 5%           | 25%           | 10%             | 25%         | 25%    | 5%         |
| Stage 2 | 25%        | 25%          | 5%            | 10%             | 5%          | 5%     | 25%        |
| Stage 3 | 10%        | 10%          | 15%           | 25%             | 10%         | 15%    | 15%        |
| Stage 4 | 15%        | 5%           | 20%           | 10%             | 10%         | 20%    | 20%        |
| Stage 5 | 15%        | 25%          | 5%            | 20%             | 25%         | 5%     | 5%         |

## Troubleshooting

If you encounter any issues with the stage not loading or not loading properly, try creating a new, fresh profile with the same mods. This solution has resolved the issue for multiple users.

Alternatively, you can create a new profile with only procedural stages installed and then add your other mods one by one. This method can help identify any compatibility issues.

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

## Algorithms used

This section includes the algorithms utilized for implementing procedural terrain generation. If you are not a programmer, feel free to skip this section.

- [Spaghetti caves, Fractional Brownian Motion, domain warping and more](https://www.youtube.com/watch?v=ob3VwY4JyzE)
- [2D / 3D Cellular Automata](https://www.youtube.com/watch?v=v7yyZZjF1z4&list=PLFt_AvWsXl0eZgMK_DT5_biRkWXftAOf9)
- [2D / 3D Perlin noise](https://en.wikipedia.org/wiki/Perlin_noise)
- [Marching Cubes](https://www.youtube.com/watch?v=M3iI2l0ltbE)
- [Mesh simplification](https://www.youtube.com/watch?v=biLY19kuGOs)
    - [UnityMeshSimplifier](https://github.com/Whinarn/UnityMeshSimplifier)