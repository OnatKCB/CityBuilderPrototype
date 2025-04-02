using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TileMapSystem
{
    /// <summary>
    /// Container for all savable tilemap data.
    /// Following Single Responsibility Principle - this class only handles serializable save data.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        [SerializeField]
        public List<TileData> tiles;
        public string lastSaveDate;
        public int mapWidth;
        public int mapHeight;
        public string version = "1.1"; // Track the version of the save format
        
        // Metadata for the save file
        public string createdBy = "CozyCityBuilder";
        public string saveDescription;
        
        // Cache of the formatted tile data for display
        [NonSerialized]
        private string formattedTileData;
        
        public SaveData()
        {
            lastSaveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            tiles = new List<TileData>();
        }
        
        public void AddTile(TileData tile)
        {
            if (tile != null)
            {
                tiles.Add(tile);
                // Clear the formatted cache when adding a tile
                formattedTileData = null;
            }
        }
        
        // Override ToString to provide the exact format requested by the user
        public override string ToString()
        {
            if (formattedTileData == null)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("{\n");
                
                // Tiles section in the exact format requested by the user
                sb.Append("    \"tiles\": [");
                
                if (tiles != null && tiles.Count > 0)
                {
                    for (int i = 0; i < tiles.Count; i++)
                    {
                        if (i == 0) sb.Append("\n        ");
                        else sb.Append(",\n        ");
                        
                        TileData tile = tiles[i];
                        if (tile.hasObject)
                        {
                            // Format for tiles with objects: "Tile_0_0, x: 0, y: 0, prefabName: Cobblestone Sidewalk"
                            sb.Append($"{tile.tileName}, x: {tile.x}, y: {tile.y}, prefabName: {tile.displayName}");
                        }
                        else
                        {
                            // Format for empty tiles: "Tile_0_0, x: 0, y: 0, (empty base tile)"
                            sb.Append($"{tile.tileName}, x: {tile.x}, y: {tile.y}, (empty base tile)");
                        }
                    }
                    sb.Append("\n    ");
                }
                
                sb.Append("],\n");
                sb.Append($"    \"lastSaveDate\": \"{lastSaveDate}\",\n");
                sb.Append($"    \"mapWidth\": {mapWidth},\n");
                sb.Append($"    \"mapHeight\": {mapHeight},\n");
                sb.Append($"    \"version\": \"{version}\",\n");
                sb.Append($"    \"createdBy\": \"{createdBy}\",\n");
                sb.Append($"    \"saveDescription\": \"{saveDescription}\"\n");
                sb.Append("}");
                
                formattedTileData = sb.ToString();
            }
            
            return formattedTileData;
        }
    }
}