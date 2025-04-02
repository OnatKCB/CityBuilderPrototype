# CozyCityBuilderPrototype

## 1. Project Description

CozyCityBuilderPrototype is a Unity-based isometric city building game that allows players to create their own cozy little cities. Built with a focus on relaxing gameplay mechanics, this prototype implements core features of city building games including:

- Isometric tile-based map system
- Building placement and deletion
- Camera controls for navigating the map
- Save and load functionality for persistent city development
- Intuitive interaction systems for building placement

This project serves as a foundation for a complete city builder game, with a focus on creating a peaceful and relaxing building experience.

## 2. Code Structure

The project is organized into several key directories:

### Scripts
- **Building**: Contains interfaces and implementations for the building system
- **Controller**: Handles player interaction, camera movement, and object placement strategies
- **Managers**: Controls system-wide functionality like rendering order
- **Pawns**: Player representation and movement in the game world
- **Tile Map**: Core tilemap generation, data management and persistence systems

### Unity Integration
- The project implements Unity's Grid and Tilemap systems for efficient tile management
- Custom editor tools to help with development and debugging
- Supports both custom grid handling and Unity's built-in Grid component

## 3. Classes And Their Functions

### Tile System
- **TilemapGenerator**: Core class handling the generation, management and persistence of the isometric tilemap
- **TileData**: Data structure for storing information about individual tiles
- **SaveData**: Container for persistent game data, including map dimensions and tile information
- **ITilemapInterfaces**: Defines interfaces for tilemap generation, persistence, and coordinate systems

### Interaction System
- **IsometricInteractionController**: Main controller managing player interaction modes
- **InteractionMode**: Enumerates the different interaction modes (camera control, object placement, object deletion)
- **IInteractionStrategy**: Interface for different interaction strategies
- **ObjectPlacementStrategy**: Handles the placement of buildings on the tilemap
- **ObjectDeletionStrategy**: Manages the deletion of placed objects
- **BuildingMenu**: UI component for selecting buildings to place

### Camera System
- **IsometricCameraController**: Manages camera movement and zoom in the isometric view
- **ICameraMovementStrategy**: Interface for different camera movement implementations
- **DragMovementStrategy**: Implementation of camera movement via mouse drag
- **CameraControlStrategy**: General camera control behavior

### Player System
- **PlayerPawn**: Represents the player in the game world
- **Pawn**: Base class for game entities with movement capabilities

## 4. How to Use

### Getting Started
1. Open the project in Unity (2022.3 LTS or later recommended)
2. Load the main scene from Assets/Scenes
3. Press Play to enter the game

### Basic Controls
- **C key**: Switch to camera control mode (allows panning around the map)
- **P key**: Switch to placement mode (for placing buildings)
- **D key**: Switch to deletion mode (for removing buildings)
- **Mouse drag**: Move the camera (in camera mode) or place/delete objects (in respective modes)

### Building Placement
1. Press P to enter placement mode
2. Select a building from the building menu
3. Click on a tile to place the selected building
4. Buildings can only be placed on valid, empty tiles

### Saving and Loading
- The game automatically saves your city when exiting play mode
- Your progress is automatically loaded when starting the game
- The save system uses JSON for storing city data

### Extending the Project
- Add new building prefabs to the Resources folder
- Register new prefabs with the TilemapGenerator
- Extend the ObjectPlacementStrategy to handle new building types
- Create custom tile types by modifying the TileData class

This prototype provides a solid foundation for developing a full-featured city builder game with isometric graphics, focusing on creating a relaxing and enjoyable building experience.

