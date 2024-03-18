# Procedural Stages

Procedural Stages replaces conventional static terrains with procedurally generated environments, offering a fresh and varied experience with each stage while striving to maintain the familiar feel of vanilla stages.

## Features

- **Diverse Terrain Types:** Explore islands and caves, each dynamically generated for a fresh adventure.
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

## Screenshots

![Image6](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.7/Image3.png)
![Image1](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.5/Image3.png)
![Image2](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.5/Image2.png)
![Image3](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.7/Image5.png)
![Image4](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.7.1/Image5.png)
![Image5](https://raw.githubusercontent.com/Lawlzee/UnityMapGenerator/master/Mod/Images/1.5/Image1.png)

## Video

[Here is a video created by LimeLight demonstrating the mod](https://www.youtube.com/watch?v=CDH7QYtNGvc&lc=UgziI767yJ6zojgI77R4AaABAg)

## Configuration

You can edit the configuration in `Settings > Mod Options > ProceduralStages`.

| Category      | Name                 | Default value | Description                                                                                                                                                                      |
|---------------|----------------------|---------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Configuration | Replace all stages   | Enabled       | If enabled, all stages will be procedurally generated. If disabled, normal stages and procedurally generated stages will be used.                                             |
| Configuration | Infinite map scaling | Disabled      | If enabled, the stage size scaling will not be reset every loop. Exercise caution when utilizing this feature, as it may lead to increased map generation time and a decrease in framerate. In multiplayer, all players must set the same value. |
| Debug         | Stage seed           |               | Specifies the stage seed. If left blank, a random seed will be used. In multiplayer, all players must set the same value.                                                        |

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

- Fix the bug where the player sometime gets stuck in the drop pod.
- Add biomes/map types (snow, grass, desert, etc)
- Add props clustering
- Add floating island map?
- Replace special stages with procedural maps (Gilded Coast, Void Fields, Void Locus)
- Improve compatibility with Ro2API.DirectorAPI
- Randomize more terrain settings
- Add more configurations
- Create a random DccsPool instead of reusing the DccsPools from the game
- Replace the moon with a procedurally generated stage
- Randomize stage names
- Address the issue of getting stuck in holes in the map.
- Enhance stage creation performance.
- Enhance the uniqueness of stages.
- Remove the ability to dream for normal stages


## Algorithms used

This section includes the algorithms utilized for implementing procedural terrain generation. If you are not a programmer, feel free to skip this section.

- [2D / 3D Cellular Automata](https://www.youtube.com/watch?v=v7yyZZjF1z4&list=PLFt_AvWsXl0eZgMK_DT5_biRkWXftAOf9)
- [2D / 3D Perlin noise](https://en.wikipedia.org/wiki/Perlin_noise)
- [Marching Cubes](https://www.youtube.com/watch?v=M3iI2l0ltbE)
- [Mesh simplification](https://www.youtube.com/watch?v=biLY19kuGOs)
    - [UnityMeshSimplifier](https://github.com/Whinarn/UnityMeshSimplifier)