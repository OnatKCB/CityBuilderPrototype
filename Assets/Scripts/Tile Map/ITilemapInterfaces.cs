using UnityEngine;
using System.Collections.Generic;
using TileMapSystem;

// Interface for map generation - following Interface Segregation Principle
public interface ITilemapGenerator
{
    void GenerateTilemap();
    void ClearExistingTiles();
    void CreateTile(int x, int y);
    bool TryDeleteObjectAt(Vector2Int gridPosition);
}

// Interface for save/load operations - following Interface Segregation Principle
public interface ITilemapPersistence
{
    void SaveTilemap();
    void LoadTilemap();
    void RegisterPrefabPath(GameObject gameObject, string prefabPath);
    string GetSaveFilePath();
}

// Interface for coordinate conversion and grid operations - Single Responsibility
public interface IGridCoordinateSystem
{
    Vector2 GridToIsometric(int x, int y);
    Vector2Int IsometricToGrid(Vector2 isoPosition);
    bool IsValidGridPosition(Vector2Int gridPos);
    bool TryMove(Vector2Int currentGridPos, Vector2Int direction, out Vector2Int newGridPos);
}