# ProceduralWorlds

ProceduralWorlds is a game developed as part of a game AI coursework. It features:

- Procedural terrain generation using open simplex noise.
- Two types of procedurally generated enemies.
- Six collectible items.
- Player mechanics including shooting bullets to eliminate enemies.
- Enemies using pathfinding algorithms (A\*, BFS, DFS) to navigate the terrain.

## Features

- **Procedural Terrain Generation**: The terrain extends with the player's location according to a gradient noise generation algorithm called open simplex noise.
- **Enemy Types**: Two types of enemies are procedurally generated in the game world.
- **Collectibles**: Six different collectible items are scattered across the terrain.
- **Player Mechanics**: Players can shoot bullets to eliminate enemies.
- **Pathfinding Algorithms**: Enemies use A\*, BFS, and DFS algorithms to find routes on the grid.

## Installation

1. Clone the repository:

   ```bash
   git clone https://github.com/acekavi/procedural-worlds.git
   cd ProceduralWorlds
   ```

2. Open the project using Unity Hub

3. Run the scene: Assets/Scenes/Terrain

## Usage

- **Movement**: Use `WASD` keys to move around.
- **Shooting**: Use the mouse button to shoot bullets at enemies.
- **Collecting Items**: Move over collectible items to collect them.

## Development

### Pathfinding Algorithms

- **A\***: An efficient and popular pathfinding algorithm used by enemies to navigate the grid.
- **BFS**: Breadth-First Search, used for exploring the shortest path in unweighted grids.
- **DFS**: Depth-First Search, used for exploring all possible paths in a depth-ward manner.

### Procedural Generation

The game uses open simplex noise to generate an infinite, procedurally generated terrain that extends with the player's location. This method provides a more natural and smoother terrain generation compared to traditional Perlin noise.

## Contributing

Feel free to fork the repository and submit pull requests. Contributions are welcome!

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Acknowledgments

- The implementation of procedural terrain generation is inspired by the techniques used in Minecraft.
- Pathfinding algorithms (A\*, BFS, DFS) are fundamental algorithms in game AI and have been implemented as part of the coursework.
