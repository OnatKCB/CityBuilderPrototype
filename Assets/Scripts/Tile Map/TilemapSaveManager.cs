// filepath: d:\Projects\Unity Projects\CozyCityBuilderPrototype\Assets\Scripts\Tile Map\TilemapSaveManager.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using TileMapSystem;

/// <summary>
/// Manages saving, loading, and deleting of tilemap saves.
/// Following Single Responsibility Principle - this class only handles save management operations.
/// </summary>
public class TilemapSaveManager : MonoBehaviour
{    [Header("Save Settings")]
    [Tooltip("Base directory where all saves will be stored")]
    public string baseSaveDirectory = "TilemapSaves";
    [Tooltip("Default name for new saves")]
    public string defaultSaveName = "New Tilemap";
    
    [Header("Current Save")]
    [SerializeField] private string currentSaveName;
    [SerializeField] private string currentSaveFilePath;
    [SerializeField] private bool isWorkingWithSavedMap = false;
    
    // Reference to the TilemapGenerator
    [Tooltip("Reference to the TilemapGenerator component (required)")]
    [SerializeField] private TilemapGenerator tilemapGenerator;
    
    // Track all available saves
    [SerializeField] private List<SaveInfo> availableSaves = new List<SaveInfo>();

    // Events
    public delegate void SaveListChangedHandler(List<SaveInfo> saves);
    public event SaveListChangedHandler OnSaveListChanged;

    public delegate void CurrentSaveChangedHandler(string saveName, string savePath);
    public event CurrentSaveChangedHandler OnCurrentSaveChanged;    private void Awake()
    {
        // Get reference to the TilemapGenerator if not already assigned
        if (tilemapGenerator == null)
        {
            // Try to find on the same GameObject first
            tilemapGenerator = GetComponent<TilemapGenerator>();
            
            // If still null, try to find in scene
            if (tilemapGenerator == null)
            {
                tilemapGenerator = FindObjectOfType<TilemapGenerator>();
                
                if (tilemapGenerator != null)
                {
                    Debug.Log("TilemapGenerator found in scene and automatically assigned.");
                }
            }
            
            // Final check
            if (tilemapGenerator == null)
            {
                Debug.LogError("TilemapSaveManager requires a TilemapGenerator reference! Please assign it in the inspector.");
                enabled = false;
                return;
            }
        }

        // Create the base save directory if it doesn't exist
        string fullSavePath = Path.Combine(Application.persistentDataPath, baseSaveDirectory);
        if (!Directory.Exists(fullSavePath))
        {
            Directory.CreateDirectory(fullSavePath);
            Debug.Log($"Created save directory: {fullSavePath}");
        }
        
        // Initialize with default save name
        currentSaveName = defaultSaveName;
        UpdateCurrentSaveFilePath();
        
        // Find all existing saves
        RefreshAvailableSaves();
    }

    private void Start()
    {
        // If we're working with a previously saved map, ensure the path is correctly set
        if (isWorkingWithSavedMap && !string.IsNullOrEmpty(currentSaveFilePath))
        {
            tilemapGenerator.SetSaveFilePath(currentSaveFilePath);
        }
        
        // Auto-load the most recent map if any exists
        if (Application.isPlaying && availableSaves.Count > 0)
        {
            // Check if the tilemap was previously generated or has existing tiles
            if (tilemapGenerator.CanSaveOrLoad)
            {
                // If a current save is specified and exists, load it
                if (isWorkingWithSavedMap && !string.IsNullOrEmpty(currentSaveFilePath) && File.Exists(currentSaveFilePath))
                {
                    Debug.Log($"Loading current map: {currentSaveName}");
                    tilemapGenerator.LoadTilemap();
                }
                // Otherwise load the most recent save
                else if (availableSaves.Count > 0)
                {
                    Debug.Log($"Loading most recent map: {availableSaves[0].saveName}");
                    LoadTilemap(availableSaves[0]);
                }
            }
        }
    }

    /// <summary>
    /// Get the list of all available saves
    /// </summary>
    public List<SaveInfo> GetAvailableSaves()
    {
        return availableSaves;
    }

    /// <summary>
    /// Checks if there are any saved maps available
    /// </summary>
    /// <returns>True if there are saved maps available</returns>
    public bool HasSavedMaps()
    {
        return availableSaves.Count > 0;
    }

    /// <summary>
    /// Gets the most recent save if any exists
    /// </summary>
    /// <returns>The most recent SaveInfo or null if none exist</returns>
    public SaveInfo GetMostRecentSave()
    {
        return availableSaves.Count > 0 ? availableSaves[0] : null;
    }

    /// <summary>
    /// Updates the current save name and file path
    /// </summary>
    /// <param name="saveName">The new save name</param>
    public void SetCurrentSaveName(string saveName)
    {
        if (string.IsNullOrEmpty(saveName))
        {
            saveName = defaultSaveName;
        }

        currentSaveName = saveName;
        UpdateCurrentSaveFilePath();
        
        // Trigger event
        OnCurrentSaveChanged?.Invoke(currentSaveName, currentSaveFilePath);
    }

    /// <summary>
    /// Get the current save name
    /// </summary>
    public string GetCurrentSaveName()
    {
        return currentSaveName;
    }

    /// <summary>
    /// Updates the file path based on the current save name
    /// </summary>
    private void UpdateCurrentSaveFilePath()
    {
        string sanitizedName = SanitizeFileName(currentSaveName);
        currentSaveFilePath = Path.Combine(
            Application.persistentDataPath,
            baseSaveDirectory,
            $"{sanitizedName}.json"
        );
    }

    /// <summary>
    /// Sanitizes a filename by removing invalid characters
    /// </summary>
    private string SanitizeFileName(string fileName)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        return new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
    }

    /// <summary>
    /// Saves the current tilemap with the specified name
    /// </summary>
    /// <param name="saveName">Optional name for the save. If null, uses the current save name.</param>
    public void SaveTilemap(string saveName = null)
    {
        if (!string.IsNullOrEmpty(saveName))
        {
            SetCurrentSaveName(saveName);
        }

        // Ensure the save directory exists
        string saveDirectory = Path.GetDirectoryName(currentSaveFilePath);
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }

        // Set the save file path on the TilemapGenerator
        tilemapGenerator.SetSaveFilePath(currentSaveFilePath);
        
        // Save the tilemap
        tilemapGenerator.SaveTilemap();
        
        // Update state
        isWorkingWithSavedMap = true;
        
        // Refresh the list of available saves
        RefreshAvailableSaves();
        
        Debug.Log($"Tilemap saved as '{currentSaveName}' to {currentSaveFilePath}");
    }

    /// <summary>
    /// Loads a tilemap from the specified save
    /// </summary>
    /// <param name="saveInfo">The save info to load</param>
    public void LoadTilemap(SaveInfo saveInfo)
    {
        if (saveInfo == null)
        {
            Debug.LogError("Cannot load null save!");
            return;
        }

        // Set the current save name and file path
        currentSaveName = saveInfo.saveName;
        currentSaveFilePath = saveInfo.filePath;
        isWorkingWithSavedMap = true;

        // Read SaveData from file
        TileMapSystem.SaveData saveData = null;
        if (File.Exists(currentSaveFilePath))
        {
            string json = File.ReadAllText(currentSaveFilePath);
            saveData = JsonUtility.FromJson<TileMapSystem.SaveData>(json);
        }
        else
        {
            Debug.LogError($"Save file not found at {currentSaveFilePath}");
            return;
        }

        // Apply map settings from SaveData to TilemapGenerator
        if (saveData != null)
        {
            // Set the save file path on the TilemapGenerator FIRST
            // This ensures when we generate the map, it knows where to save/load from
            tilemapGenerator.SetSaveFilePath(currentSaveFilePath);
            
            // Apply the saved map dimensions
            tilemapGenerator.mapWidth = saveData.mapWidth;
            tilemapGenerator.mapHeight = saveData.mapHeight;
            
            // Apply the saved map shape settings
            if (!string.IsNullOrEmpty(saveData.mapShape))
            {
                if (Enum.TryParse(saveData.mapShape, out MapShape loadedShape))
                {
                    tilemapGenerator.mapShape = loadedShape;
                    Debug.Log($"Set map shape to: {tilemapGenerator.mapShape}");
                }
                else
                {
                    Debug.LogWarning($"Could not parse map shape: {saveData.mapShape}");
                }
            }
            else
            {
                Debug.LogWarning("No map shape found in save data, using default Rectangle shape");
            }
            
            if (saveData.shapeThickness > 0)
            {
                tilemapGenerator.shapeThickness = saveData.shapeThickness;
                Debug.Log($"Set shape thickness to: {tilemapGenerator.shapeThickness}");
            }
            else
            {
                Debug.LogWarning("No shape thickness found in save data, using default thickness of 1");
            }
            
            // Clear existing tiles first to ensure clean regeneration
            tilemapGenerator.ClearExistingTiles();
            
            // Generate the map with the correct shape
            tilemapGenerator.GenerateMap();
            
            // Instead of calling LoadTilemap, we'll manually place objects on the map
            // from the save data to ensure they go in the right places
            PlaceObjectsFromSaveData(saveData);
        }
        else
        {
            Debug.LogError($"Failed to deserialize SaveData from {currentSaveFilePath}");
            return;
        }

        // Trigger event
        OnCurrentSaveChanged?.Invoke(currentSaveName, currentSaveFilePath);

        Debug.Log($"Loaded tilemap from '{saveInfo.saveName}' ({currentSaveFilePath}) and applied map settings from save file.");
    }
    
    /// <summary>
    /// Places objects on the map according to the save data
    /// </summary>
    /// <param name="saveData">The save data containing tile information</param>
    private void PlaceObjectsFromSaveData(TileMapSystem.SaveData saveData)
    {
        if (saveData == null || saveData.tiles == null)
            return;
            
        int placedCount = 0;
        int skippedCount = 0;
        
        // Apply map shape settings first to ensure the map is generated properly
        bool mapShapeSet = false;
        if (!string.IsNullOrEmpty(saveData.mapShape))
        {
            if (Enum.TryParse(saveData.mapShape, out MapShape loadedShape))
            {
                tilemapGenerator.mapShape = loadedShape;
                mapShapeSet = true;
                Debug.Log($"Set map shape to: {tilemapGenerator.mapShape} (from PlaceObjectsFromSaveData)");
            }
        }
        
        if (saveData.shapeThickness > 0)
        {
            tilemapGenerator.shapeThickness = saveData.shapeThickness;
            Debug.Log($"Set shape thickness to: {saveData.shapeThickness} (from PlaceObjectsFromSaveData)");
        }
            
        // Go through all tiles in the save data that have objects
        foreach (TileMapSystem.TileData tileData in saveData.tiles)
        {
            // Skip tiles that don't have objects
            if (!tileData.hasObject)
                continue;
                
            // Skip invalid positions
            if (tileData.x < 0 || tileData.x >= tilemapGenerator.mapWidth || 
                tileData.y < 0 || tileData.y >= tilemapGenerator.mapHeight)
            {
                Debug.LogWarning($"Skipping object at invalid position ({tileData.x}, {tileData.y})");
                skippedCount++;
                continue;
            }
            
            // Try to find the tile at this position
            Vector2Int gridPos = new Vector2Int(tileData.x, tileData.y);
            GameObject tile = tilemapGenerator.GetTileAt(gridPos);
            
            if (tile == null)
            {
                // If the map has a special shape, this position may be outside the shape
                if (mapShapeSet && tilemapGenerator.mapShape != MapShape.Rectangle)
                {
                    Debug.LogWarning($"No tile found at position ({tileData.x}, {tileData.y}) - this may be due to the {tilemapGenerator.mapShape} map shape");
                }
                else
                {
                    Debug.LogWarning($"No tile found at position ({tileData.x}, {tileData.y}) - trying to create tile");
                    
                    // Try to create a tile at this position if it's not found (may happen with certain map shapes)
                    tilemapGenerator.CreateTile(tileData.x, tileData.y);
                    
                    // Try again to get the tile
                    tile = tilemapGenerator.GetTileAt(gridPos);
                    
                    if (tile == null)
                    {
                        Debug.LogError($"Failed to create tile at position ({tileData.x}, {tileData.y})");
                        skippedCount++;
                        continue;
                    }
                }
            }
            
            // Try to load the prefab
            string prefabPath = tileData.resourcePath;
            if (string.IsNullOrEmpty(prefabPath))
                prefabPath = tileData.prefabOriginalName;
                
            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab == null && prefabPath.Contains("/"))
            {
                // Try loading just the name part
                string prefabName = prefabPath.Substring(prefabPath.LastIndexOf('/') + 1);
                prefab = Resources.Load<GameObject>(prefabName);
                
                // If still null, try to find among existing prefabs by name
                if (prefab == null)
                {
                    Debug.LogWarning($"Could not load prefab at path: {prefabPath}, trying to find by name");
                }
            }
            
            if (prefab != null)
            {
                // Remove any existing objects on this tile
                foreach (Transform child in tile.transform)
                {
                    if (Application.isPlaying)
                        Destroy(child.gameObject);
                    else
                        DestroyImmediate(child.gameObject);
                }
                
                // Instantiate the new object on this tile
                GameObject newObject = Instantiate(prefab);
                
                // Set position
                Vector3 targetPosition = tileData.objectWorldPosition;
                if (targetPosition == Vector3.zero)
                    targetPosition = tile.transform.position;
                    
                newObject.transform.position = targetPosition;
                
                // Parent to tile
                newObject.transform.SetParent(tile.transform, true);
                
                // Keep the position correct after parenting
                if (newObject.transform.position != targetPosition)
                    newObject.transform.position = targetPosition;
                    
                // Set name
                if (!string.IsNullOrEmpty(tileData.prefabOriginalName))
                    newObject.name = tileData.prefabOriginalName;
                    
                // Register the prefab path
                tilemapGenerator.RegisterPrefabPath(newObject, prefabPath);
                
                placedCount++;
            }
            else
            {
                Debug.LogWarning($"Could not load prefab at path: {prefabPath}");
                skippedCount++;
            }
        }
        
        Debug.Log($"Placed {placedCount} objects from save data with {saveData.tiles.Count(t => t.hasObject)} object tiles. Skipped {skippedCount} objects.");
    }

    /// <summary>
    /// Deletes a save file
    /// </summary>
    /// <param name="saveInfo">The save info to delete</param>
    /// <returns>True if the save was deleted successfully</returns>
    public bool DeleteSave(SaveInfo saveInfo)
    {
        if (saveInfo == null || !File.Exists(saveInfo.filePath))
        {
            Debug.LogWarning($"Cannot delete save: File not found at {saveInfo?.filePath}");
            return false;
        }

        try
        {
            File.Delete(saveInfo.filePath);
            
            // If we're deleting the current save, reset to default
            if (currentSaveFilePath == saveInfo.filePath)
            {
                currentSaveName = defaultSaveName;
                UpdateCurrentSaveFilePath();
                isWorkingWithSavedMap = false;
                
                // Reset the TilemapGenerator's save path
                tilemapGenerator.SetSaveFilePath(currentSaveFilePath);
                
                // Trigger event
                OnCurrentSaveChanged?.Invoke(currentSaveName, currentSaveFilePath);
            }
            
            Debug.Log($"Deleted save '{saveInfo.saveName}' at {saveInfo.filePath}");
            
            // Refresh the list of available saves
            RefreshAvailableSaves();
            
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error deleting save '{saveInfo.saveName}': {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Creates a new tilemap (clears existing and sets a new save name)
    /// </summary>
    /// <param name="saveName">Optional name for the new tilemap</param>
    public void CreateNewTilemap(string saveName = null)
    {
        // Set a new save name
        SetCurrentSaveName(saveName ?? defaultSaveName);
        isWorkingWithSavedMap = false;
        
        // Clear existing tilemap
        tilemapGenerator.ClearExistingTiles();
        
        // Generate new tilemap
        tilemapGenerator.GenerateMap();
        
        // Set the new save file path on the TilemapGenerator
        tilemapGenerator.SetSaveFilePath(currentSaveFilePath);
        
        Debug.Log($"Created new tilemap with name '{currentSaveName}'");
    }

    /// <summary>
    /// Refreshes the list of available saves
    /// </summary>
    public void RefreshAvailableSaves()
    {
        availableSaves.Clear();
        
        string saveDirectory = Path.Combine(Application.persistentDataPath, baseSaveDirectory);
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }

        // Find all JSON files in the save directory
        string[] saveFiles = Directory.GetFiles(saveDirectory, "*.json");
        
        foreach (string filePath in saveFiles)
        {
            try
            {
                // Load the save data to extract metadata
                string json = File.ReadAllText(filePath);
                SaveData saveData = JsonUtility.FromJson<SaveData>(json);
                
                if (saveData != null)
                {
                    // Extract the filename without extension as the save name
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    
                    SaveInfo saveInfo = new SaveInfo
                    {
                        saveName = fileName,
                        filePath = filePath,
                        lastSaveDate = saveData.lastSaveDate,
                        description = saveData.saveDescription,
                        mapWidth = saveData.mapWidth,
                        mapHeight = saveData.mapHeight,
                        tileCount = saveData.tiles.Count
                    };
                    
                    availableSaves.Add(saveInfo);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error loading save file at {filePath}: {ex.Message}");
            }
        }
        
        // Sort saves by last save date (newest first)
        availableSaves.Sort((a, b) => DateTime.Parse(b.lastSaveDate).CompareTo(DateTime.Parse(a.lastSaveDate)));
        
        // Trigger event
        OnSaveListChanged?.Invoke(availableSaves);
        
        Debug.Log($"Found {availableSaves.Count} saved tilemaps");
    }

    /// <summary>
    /// Represents metadata about a saved tilemap
    /// </summary>
    [Serializable]
    public class SaveInfo
    {
        public string saveName;
        public string filePath;
        public string lastSaveDate;
        public string description;
        public int mapWidth;
        public int mapHeight;
        public int tileCount;
        
        public override string ToString()
        {
            return $"{saveName} ({mapWidth}x{mapHeight}, {tileCount} tiles) - {lastSaveDate}";
        }
    }
}
