# Procedural Stages

Adds procedurally generated stages to Risk of Rain 2. Not multiplayer compatible yet.

## Configuration
todo

## Screenshots

![Image1](Images/Image1.png)
![Image2](Images/Image2.png)
![Image3](Images/Image3.png)
![Image4](Images/Image4.png)
![Image5](Images/Image5.png)
![Image6](Images/Image6.png)

## Todo list
- [ ] Support multiplayer
- [ ] Be compatible with the Risk of Resources Gauntlet mod
- [ ] Make the color palette less blue
- [ ] Add decorations to the map (trees, rocks, pillars, etc.)
- [ ] Add more configurations
- [ ] Add stage unique interactables. For example, add a red chest on stage 4
- [ ] Optimize the NodeGraphs
- [ ] Optimize the map mesh
- [ ] Fix the bug where the screen flashes white when entering the first stage
- [ ] Fix the bug where the screen flashes white when entering a void seed
- [ ] Add a dead barrier if the player clips through the map
- [ ] Create a random DccsPool instead of reusing the DccsPools from the game
- [ ] Improve music selection
- [ ] Replace the moon with a procedurally generated stage

## Algorithms used
- [2D / 3D Cellular Automata](https://www.youtube.com/watch?v=v7yyZZjF1z4&list=PLFt_AvWsXl0eZgMK_DT5_biRkWXftAOf9)
- [2D / 3D Perlin noise](https://en.wikipedia.org/wiki/Perlin_noise)
- [Marching Cubes](https://www.youtube.com/watch?v=M3iI2l0ltbE)