using System;
using UnityEngine;

namespace TileMapSystem
{
    /// <summary>
    /// Represents data for a single tile in the tilemap.
    /// Following Single Responsibility Principle - this class only handles tile data storage.
    /// </summary>
    [Serializable]
    public class TileData
    {
        // Position information
        public int x;
        public int y;
        [SerializeField]
        public Vector3 worldPosition; // Store the actual world position for more precise placement
        
        // Object placement information
        public bool hasObject = false; // Flag indicating if this tile has an object placed on it
        public Vector3 objectWorldPosition; // Position of the placed object (may differ from tile position)
        
        // Prefab identification
        public string prefabName;      // Name of the prefab (basic identifier)
        public string displayName;     // Human-readable name for UI display
        public string resourcePath;    // Full resource path for loading from Resources folder
        public string prefabOriginalName; // The original name of the prefab before instantiation
        
        // Additional data
        public string uniqueId;        // Unique identifier for the tile object
        public string tileType;        // The type of tile (e.g. ground, water, road)
        
        public string tileName;        // The name of the tile itself (e.g. Tile_0_0)
        
        public override string ToString()
        {
            if (hasObject)
                return $"{tileName}, x: {x}, y: {y}, prefabName: {displayName}";
            else
                return $"{tileName}, x: {x}, y: {y}, (empty base tile)";
        }
        
        // Custom method to create a JSON-like string for debugging
        public string ToDetailedString()
        {
            return $"{{\"tileName\":\"{tileName}\", \"x\":{x}, \"y\":{y}, " +
                   $"\"hasObject\":{hasObject.ToString().ToLower()}, " +
                   $"\"tileType\":\"{tileType}\", " +
                   (hasObject ? $"\"prefabName\":\"{prefabName}\", \"displayName\":\"{displayName}\"" : "\"empty\":true") +
                   "}}";
        }
    }
}