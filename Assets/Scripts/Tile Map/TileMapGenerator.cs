using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;
using TileMapSystem;
using UnityEngine.Tilemaps; // Add Unity Tilemap namespace

#if UNITY_EDITOR
using UnityEditor;
#endif

// Main class that implements all interfaces - adhering to Single Responsibility Principle
// through composition and clear separation of behaviors
[ExecuteInEditMode]
public class TilemapGenerator : MonoBehaviour, ITilemapGenerator, ITilemapPersistence, IGridCoordinateSystem
{
    [Header("Tilemap Settings")]
    public int mapWidth = 10;
    public int mapHeight = 10;
    public float tileWidth = 1f;
    public float tileHeight = 0.5f;

    [Header("Unity Grid Integration")]
    [Tooltip("Use Unity's built-in Grid component instead of custom grid")]
    public bool useUnityGrid = true;
    [Tooltip("Reference to a Unity Grid component - will be auto-created if null")]
    public Grid unityGrid;
    [Tooltip("Where to place tiles - either on Unity Grid or on custom grid container")]
    public Transform gridContainer;
    
    [Header("References")]
    public GameObject tilePrefab;

    // Reference to the Unity Tilemap component if using Unity Grid
    [HideInInspector] public Tilemap unityTilemap;
    
    [HideInInspector]
    public GameObject[,] tiles;

    [SerializeField]
    protected string saveFilePath;
    
    [Header("Generation Status")]
    [SerializeField]
    private bool hasBeenGenerated = false;
    
    // Property to check if the map can be saved/loaded
    public bool CanSaveOrLoad => hasBeenGenerated || CheckIfTilesExist();

    // Dictionary to track prefab paths for objects placed on tiles
    [HideInInspector]
    private Dictionary<string, string> objectToPrefabPathMap = new Dictionary<string, string>();
    
    // Auto-save settings
    [Header("Auto Save Settings")]
    public bool enableAutoSave = true;
    [Tooltip("Time in seconds between auto-saves during gameplay (0 = save immediately on changes)")]
    public float autoSaveInterval = 5f;
    private float lastAutoSaveTime;
    private bool pendingChanges = false;

    // Track if we're currently loading to prevent recursive save calls
    private bool isCurrentlyLoading = false;

    protected virtual void Start()
    {
        // Only set the save path, don't generate automatically
        if (string.IsNullOrEmpty(saveFilePath))
        {
            saveFilePath = Path.Combine(Application.persistentDataPath, "saveData.json");
            Debug.Log("Save file path: " + saveFilePath);
        }
        
        // Check if map was previously generated or there are existing tiles
        if (Application.isPlaying)
        {
            // Fix: Check if tiles already exist in the grid before trying to load
            bool tilesExist = CheckIfTilesExist();
            
            if (hasBeenGenerated || tilesExist)
            {
                if (!hasBeenGenerated && tilesExist)
                {
                    // Update state to reflect existing tiles
                    hasBeenGenerated = true;
                    Debug.Log("Found existing tiles, updating generation state to true");
                }
                LoadTilemap();
            }
        }
    }

    // Ensure the proper grid setup exists based on selected options
    private void EnsureGridSetupExists()
    {
        if (useUnityGrid)
        {
            EnsureUnityGridExists();
        }
        else
        {
            EnsureCustomGridContainerExists();
        }
    }

    // Ensure Unity Grid component exists
    private void EnsureUnityGridExists()
    {
        // Look for an existing Grid component
        if (unityGrid == null)
        {
            // Try to find a Grid in the scene
            unityGrid = FindObjectOfType<Grid>();
            
            // If not found, create one
            if (unityGrid == null)
            {
                GameObject gridObj = new GameObject("Unity Grid");
                unityGrid = gridObj.AddComponent<Grid>();
                unityGrid.cellLayout = GridLayout.CellLayout.Isometric;
                unityGrid.cellSize = new Vector3(tileWidth, tileHeight, 1f);
                Debug.Log("Created new Unity Grid object");
            }
        }
        
        // Look for or create a Tilemap within the grid
        if (unityTilemap == null)
        {
            // Check if Grid already has a Tilemap child
            unityTilemap = unityGrid.GetComponentInChildren<Tilemap>();
            
            if (unityTilemap == null)
            {
                // Create a new Tilemap GameObject under the Grid
                GameObject tilemapObj = new GameObject("Tilemap");
                tilemapObj.transform.SetParent(unityGrid.transform, false);
                unityTilemap = tilemapObj.AddComponent<Tilemap>();
                tilemapObj.AddComponent<TilemapRenderer>();
                Debug.Log("Created new Tilemap object under Unity Grid");
            }
        }
        
        // Set the grid container to be the Tilemap's transform
        gridContainer = unityTilemap.transform;
    }

    // Ensure there's a custom grid container available when not using Unity Grid
    private void EnsureCustomGridContainerExists()
    {
        if (gridContainer == null)
        {
            // Check if a grid container already exists as a child
            Transform existingGrid = transform.Find("GridContainer");
            if (existingGrid != null)
            {
                gridContainer = existingGrid;
                return;
            }
            
            // Create a new grid container
            GameObject newGridObj = new GameObject("GridContainer");
            gridContainer = newGridObj.transform;
            gridContainer.SetParent(transform, false);
            gridContainer.localPosition = Vector3.zero;
            
            Debug.Log("Created new custom grid container object");
        }
    }

    // Public method exposed for the inspector button
    public void GenerateMap()
    {
        EnsureGridSetupExists();
        ClearExistingTiles();
        GenerateTilemap();
        hasBeenGenerated = true;
        
        #if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        #endif
    }

    // ITilemapGenerator Implementation
    public void GenerateTilemap()
    {
        tiles = new GameObject[mapWidth, mapHeight];

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                CreateTile(x, y);
            }
        }
    }

    public void ClearExistingTiles()
    {
        // Clear any existing tiles
        if (tiles != null)
        {
            for (int x = 0; x < tiles.GetLength(0); x++)
            {
                for (int y = 0; y < tiles.GetLength(1); y++)
                {
                    if (tiles[x, y] != null)
                    {
                        if (Application.isPlaying)
                            Destroy(tiles[x, y]);
                        else
                            DestroyImmediate(tiles[x, y]);
                    }
                }
            }
        }
        
        // Clean up any leftover children directly on this GameObject
        foreach (Transform child in transform)
        {
            // Skip the grid container itself
            if (child == gridContainer) continue;
            
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
        
        // Also clean up any leftover children in the gridContainer if it exists
        if (gridContainer != null)
        {
            foreach (Transform child in gridContainer)
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
    }

    public void CreateTile(int x, int y)
    {
        // Ensure grid setup exists
        EnsureGridSetupExists();
        
        // Convert grid coordinates to isometric position
        Vector2 position = GridToIsometric(x, y);
        
        // Different tile creation logic based on whether we're using Unity Grid
        if (useUnityGrid && unityGrid != null)
        {
            // Create tile in Unity Tilemap system
            Vector3Int cellPosition = new Vector3Int(x, y, 0);
            
            // Instantiate the tile object
            GameObject tile = Instantiate(tilePrefab, unityGrid.GetCellCenterWorld(cellPosition), Quaternion.identity);
            tile.name = $"Tile_{x}_{y}";
            tile.transform.SetParent(gridContainer, false);
            
            tiles[x, y] = tile;
        }
        else
        {
            // Use traditional custom grid approach
            GameObject tile = Instantiate(tilePrefab, new Vector3(position.x, position.y, 0), Quaternion.identity);
            
            // Set parent to gridContainer if available, otherwise fall back to this transform
            Transform parent = gridContainer != null ? gridContainer : transform;
            tile.transform.SetParent(parent, false);
            tile.name = $"Tile_{x}_{y}";

            tiles[x, y] = tile;
        }
    }

    // If using Unity Grid, convert our grid coordinates to Unity Grid cell positions
    public Vector3Int GetCellPosition(int x, int y)
    {
        return new Vector3Int(x, y, 0);
    }

    // Get world position for the given grid coordinates
    public Vector3 GetWorldPosition(int x, int y)
    {
        if (useUnityGrid && unityGrid != null)
        {
            return unityGrid.GetCellCenterWorld(new Vector3Int(x, y, 0));
        }
        else
        {
            Vector2 position = GridToIsometric(x, y);
            return new Vector3(position.x, position.y, 0);
        }
    }

    // ITilemapPersistence Implementation
    public void SaveTilemap()
    {
        if (!CanSaveOrLoad)
        {
            Debug.LogWarning("Cannot save tilemap that hasn't been generated yet.");
            return;
        }

        // Make sure grid container exists
        EnsureGridSetupExists();

        SaveData saveData = new SaveData();
        saveData.mapWidth = mapWidth;
        saveData.mapHeight = mapHeight;
        saveData.saveDescription = "Tilemap save data with detailed tile information";
        int savedCount = 0;
        int emptyTileCount = 0;

        // First check if the tiles array is initialized
        if (tiles == null)
        {
            Debug.LogWarning("Tiles array is null! Initializing it before saving.");
            tiles = new GameObject[mapWidth, mapHeight];
            
            // Try to find existing tiles in the grid container and populate the array
            if (gridContainer != null)
            {
                foreach (Transform child in gridContainer)
                {
                    if (child.name.StartsWith("Tile_"))
                    {
                        // Extract coordinates from name (e.g., "Tile_3_4" -> x=3, y=4)
                        string[] parts = child.name.Split('_');
                        if (parts.Length >= 3 && int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int y))
                        {
                            if (x >= 0 && x < mapWidth && y >= 0 && y < mapHeight)
                            {
                                tiles[x, y] = child.gameObject;
                            }
                        }
                    }
                }
            }
        }

        // Now process the tiles
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                // Skip if the tile at this position is null
                if (tiles == null || tiles.GetLength(0) <= x || tiles.GetLength(1) <= y || tiles[x, y] == null)
                {
                    continue;
                }

                GameObject tile = tiles[x, y];
                
                // Store the base tile info
                string tileName = tile.name;
                
                // Create base tile data that will be used regardless of whether objects are placed
                TileMapSystem.TileData tileData = new TileMapSystem.TileData
                {
                    x = x,
                    y = y,
                    worldPosition = tile.transform.position,
                    tileName = tileName, // Store the tile's name (e.g., Tile_0_0)
                    tileType = "BaseTile" // Mark this as a base tile
                };
                
                // Check if there are objects placed on this tile
                if (tile.transform.childCount > 0)
                {
                    GameObject childObject = tile.transform.GetChild(0).gameObject;
                    string objectName = childObject.name;
                    
                    // Get the original prefab name without (Clone) if it exists
                    string originalName = objectName;
                    if (originalName.EndsWith("(Clone)"))
                    {
                        originalName = originalName.Substring(0, originalName.Length - 7);
                    }
                    
                    // Get the display name (formatted for readability)
                    string displayName = FormatDisplayName(originalName);
                    
                    // Get the resource path from our dictionary if it exists
                    string resourcePath = "";
                    if (objectToPrefabPathMap.TryGetValue(objectName, out resourcePath))
                    {
                        Debug.Log($"Found prefab path for {objectName}: {resourcePath}");
                    }
                    else if (objectToPrefabPathMap.TryGetValue(originalName, out resourcePath))
                    {
                        Debug.Log($"Found prefab path for original name {originalName}: {resourcePath}");
                    }
                    else
                    {
                        // If we don't have it in the dictionary, use the object name as a fallback
                        resourcePath = originalName;
                        Debug.Log($"No prefab path found, using original name as fallback: {originalName}");
                    }
                    
                    // Create a unique ID if one doesn't exist
                    string uniqueId = childObject.GetInstanceID().ToString();
                    
                    // Get actual world position for more precise placement
                    Vector3 worldPosition = childObject.transform.position;
                    
                    // Update the tile data with object information
                    tileData.hasObject = true; // Set hasObject to true
                    tileData.objectWorldPosition = worldPosition;
                    tileData.prefabName = objectName;
                    tileData.displayName = displayName;
                    tileData.resourcePath = resourcePath;
                    tileData.uniqueId = uniqueId;
                    tileData.prefabOriginalName = originalName;
                    tileData.tileType = DetermineTileType(childObject);
                    savedCount++;
                    Debug.Log($"Saved tile '{tileName}' at ({x},{y}) - Position: {worldPosition}, " + 
                             $"Prefab: '{objectName}', Original name: '{originalName}'");
                }
                else
                {
                    tileData.hasObject = false; // Explicitly set hasObject to false for empty tiles
                    emptyTileCount++;
                }
                
                // Add the tile data to our save data
                saveData.AddTile(tileData);
            }
        }

        // First serialize using Unity's JsonUtility for file saving
        string jsonForFile = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(saveFilePath, jsonForFile);
        
        // Now use our custom string representation for display/debug purposes
        string jsonForDisplay = saveData.ToString();
        Debug.Log($"Tilemap saved to {saveFilePath} with {savedCount} objects and {emptyTileCount} empty tiles recorded ({savedCount + emptyTileCount} total tiles)");
        Debug.Log($"JSON content: {jsonForDisplay}");
    }
    
    // Updated method without using tags
    private string DetermineTileType(GameObject obj)
    {
        // We avoid using tags as requested, and instead use name-based detection
        string lowercaseName = obj.name.ToLowerInvariant();
        
        // Type detection based on prefab name patterns
        if (lowercaseName.Contains("building") || lowercaseName.Contains("house") || 
            lowercaseName.Contains("tower") || lowercaseName.Contains("shop"))
            return "Building";
        else if (lowercaseName.Contains("tree") || lowercaseName.Contains("plant") || 
                 lowercaseName.Contains("flower") || lowercaseName.Contains("decoration"))
            return "Decoration";
        else if (lowercaseName.Contains("road") || lowercaseName.Contains("path") || 
                 lowercaseName.Contains("pavement"))
            return "Road";
        
        // Check for specific components instead of tags
        if (obj.GetComponent<UnityEngine.AI.NavMeshObstacle>() != null)
            return "Obstacle";
            
        return "Structure";
    }

    public void LoadTilemap()
    {
        if (!CanSaveOrLoad)
        {
            Debug.LogWarning("Cannot load tilemap that hasn't been generated yet.");
            return;
        }

        // Set loading flag to prevent auto-save during load
        isCurrentlyLoading = true;

        // Make sure grid container exists
        EnsureGridSetupExists();

        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            SaveData saveData = JsonUtility.FromJson<SaveData>(json);
            
            // Verify map dimensions - warn if different
            if (saveData.mapWidth != mapWidth || saveData.mapHeight != mapHeight)
            {
                Debug.LogWarning($"Loaded map dimensions ({saveData.mapWidth}x{saveData.mapHeight}) " +
                                $"differ from current dimensions ({mapWidth}x{mapHeight})");
            }
            
            // Check if the tiles array is null or incorrectly sized - initialize it if needed
            if (tiles == null)
            {
                Debug.LogWarning("Tiles array is null! Initializing it before loading.");
                tiles = new GameObject[mapWidth, mapHeight];
                
                // Try to find existing tiles in the grid container and populate the array
                if (gridContainer != null)
                {
                    foreach (Transform child in gridContainer)
                    {
                        if (child.name.StartsWith("Tile_"))
                        {
                            // Extract coordinates from name (e.g., "Tile_3_4" -> x=3, y=4)
                            string[] parts = child.name.Split('_');
                            if (parts.Length >= 3 && int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int y))
                            {
                                if (x >= 0 && x < mapWidth && y >= 0 && y < mapHeight)
                                {
                                    tiles[x, y] = child.gameObject;
                                }
                            }
                        }
                    }
                }
                
                // If we couldn't populate the array with existing tiles, we need to generate them
                if (!CheckIfTilesExist())
                {
                    Debug.Log("No existing tiles found. Generating base tilemap before loading objects.");
                    GenerateTilemap();
                }
            }

            // Clear existing tile children before loading - with proper null checking
            if (tiles != null)
            {
                for (int x = 0; x < tiles.GetLength(0); x++)
                {
                    for (int y = 0; y < tiles.GetLength(1); y++)
                    {
                        GameObject tile = tiles[x, y];
                        if (tile != null)
                        {
                            foreach (Transform child in tile.transform)
                            {
                                if (Application.isPlaying)
                                    Destroy(child.gameObject);
                                else
                                    DestroyImmediate(child.gameObject);
                            }
                        }
                    }
                }
            }
            
            // Clear the dictionary to avoid stale entries
            objectToPrefabPathMap.Clear();
            
            // First try cleaning up resources to ensure we're not using cached objects
            Resources.UnloadUnusedAssets();
            
            int loadedCount = 0;
            foreach (TileMapSystem.TileData tileData in saveData.tiles)
            {
                // Skip if out of bounds (in case the map size changed)
                if (tileData.x >= mapWidth || tileData.y >= mapHeight || tileData.x < 0 || tileData.y < 0)
                {
                    Debug.LogWarning($"Skipping out-of-bounds tile at ({tileData.x}, {tileData.y})");
                    continue;
                }

                // Skip if no object on this tile or if we're only handling base tiles (not objects)
                if (!tileData.hasObject)
                {
                    continue;
                }
                
                // Skip if the tile at this position doesn't exist in our array
                if (tiles == null || tiles.GetLength(0) <= tileData.x || tiles.GetLength(1) <= tileData.y || tiles[tileData.x, tileData.y] == null)
                {
                    Debug.LogWarning($"Cannot place object at ({tileData.x}, {tileData.y}) - tile doesn't exist in current grid");
                    continue;
                }
                
                // Try multiple path options, prioritizing most likely paths for finding the prefab
                GameObject prefab = null;
                string pathUsed = "";
                string[] pathsToTry = new string[] 
                {
                    tileData.resourcePath,
                    tileData.prefabOriginalName,  // Try the original prefab name first
                    tileData.prefabName,
                    StripCloneSuffix(tileData.prefabName) // Try name without (Clone) suffix
                };
                
                foreach (string path in pathsToTry)
                {
                    if (string.IsNullOrEmpty(path)) continue;
                    
                    // Try direct path
                    prefab = Resources.Load<GameObject>(path);
                    if (prefab != null) {
                        pathUsed = path;
                        Debug.Log($"Found prefab using path: {path}");
                        break;
                    }
                    
                    // Try without Resources/ prefix
                    if (path.Contains("Resources/"))
                    {
                        string adjustedPath = path.Substring(path.IndexOf("Resources/") + 10);
                        prefab = Resources.Load<GameObject>(adjustedPath);
                        if (prefab != null) {
                            pathUsed = adjustedPath;
                            Debug.Log($"Found prefab using adjusted path: {adjustedPath}");
                            break;
                        }
                    }
                    
                    // Try using just the name portion of the path
                    if (path.Contains("/"))
                    {
                        string nameOnly = path.Substring(path.LastIndexOf('/') + 1);
                        prefab = Resources.Load<GameObject>(nameOnly);
                        if (prefab != null) {
                            pathUsed = nameOnly;
                            Debug.Log($"Found prefab using name-only: {nameOnly}");
                            break;
                        }
                    }
                }
                
                if (prefab != null && tiles[tileData.x, tileData.y] != null)
                {
                    GameObject parentTile = tiles[tileData.x, tileData.y];
                    
                    // First instantiate at the world origin to avoid position shifting
                    GameObject newObject = Instantiate(prefab);
                    
                    // Set the object's name based on saved prefab name data if available
                    string objectName;
                    if (!string.IsNullOrEmpty(tileData.prefabOriginalName))
                    {
                        objectName = tileData.prefabOriginalName;
                    }
                    else if (!string.IsNullOrEmpty(tileData.prefabName))
                    {
                        objectName = StripCloneSuffix(tileData.prefabName);
                    }
                    else
                    {
                        objectName = prefab.name;
                    }
                    
                    // Ensure the name includes prefab information
                    newObject.name = objectName;
                    
                    // Store prefab path information in the dictionary
                    objectToPrefabPathMap[objectName] = pathUsed;
                    if (!string.IsNullOrEmpty(tileData.prefabName) && tileData.prefabName != objectName)
                    {
                        objectToPrefabPathMap[tileData.prefabName] = pathUsed;
                    }

                    // Important fix: Set the position BEFORE parenting to maintain correct placement
                    Vector3 targetPosition;
                    if (tileData.hasObject && tileData.objectWorldPosition != Vector3.zero)
                    {
                        // Use the exact saved world position from our data
                        targetPosition = tileData.objectWorldPosition;
                        newObject.transform.position = targetPosition;
                    }
                    else
                    {
                        // Use the tile position as fallback
                        targetPosition = parentTile.transform.position;
                        newObject.transform.position = targetPosition;
                    }
                    
                    // Now parent the object to the tile
                    newObject.transform.SetParent(parentTile.transform, true);
                    
                    // Ensure position is maintained after parenting (sometimes Unity shifts position during parenting)
                    if (newObject.transform.position != targetPosition)
                    {
                        newObject.transform.position = targetPosition;
                    }
                    
                    // Log details about the loaded object
                    Debug.Log($"Placed {objectName} on {tileData.tileName} at ({tileData.x},{tileData.y}) - " +
                             $"Position: {newObject.transform.position}, Parent: {parentTile.name}");
                    
                    loadedCount++;
                }
                else
                {
                    string pathsAttempted = string.Join(", ", pathsToTry.Where(p => !string.IsNullOrEmpty(p)));
                    Debug.LogError($"Failed to load prefab for tile at ({tileData.x}, {tileData.y}). " + 
                                 $"Attempted paths: {pathsAttempted}");
                }
            }
            
            Debug.Log($"Tilemap loaded from {saveFilePath}. {loadedCount}/{saveData.tiles.Count} objects placed successfully.");
            
            // Display the JSON content that was loaded
            Debug.Log($"Loaded JSON content: {saveData}");
            
            // Additional log for any debug info
            if (saveData.tiles.Count(t => t.hasObject) > 0 && loadedCount == 0)
            {
                Debug.LogWarning("No objects were loaded! Check that your prefabs are in Resources folders and their paths are correct.");
                Debug.Log("Make sure your prefabs are properly saved in a Resources folder in your project.");
            }
        }
        else
        {
            Debug.LogWarning("Save file not found!");
        }
        
        // Clear the loading flag
        isCurrentlyLoading = false;
    }
    
    // Helper method to strip (Clone) suffix from object names
    private string StripCloneSuffix(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;
            
        if (name.EndsWith("(Clone)"))
        {
            return name.Substring(0, name.Length - 7);
        }
        return name;
    }
    
    // Helper method to format prefab names for better display
    private string FormatDisplayName(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName))
            return prefabName;

        // Replace underscores with spaces
        string displayName = prefabName.Replace('_', ' ');

        // Insert spaces before capital letters (for camelCase and PascalCase)
        for (int i = displayName.Length - 1; i > 0; i--)
        {
            if (char.IsUpper(displayName[i]) && !char.IsWhiteSpace(displayName[i-1]))
            {
                displayName = displayName.Insert(i, " ");
            }
        }

        // Ensure the first letter is capitalized
        if (displayName.Length > 0)
        {
            displayName = char.ToUpper(displayName[0]) + displayName.Substring(1);
        }

        return displayName;
    }

    // Enhanced method to register a prefab path when an object is placed
    public void RegisterPrefabPath(GameObject gameObject, string prefabPath)
    {
        if (gameObject != null && !string.IsNullOrEmpty(prefabPath))
        {
            // Store with both full path and object name for redundancy
            objectToPrefabPathMap[gameObject.name] = prefabPath;
            
            // Also store with (Clone) suffix removed for runtime instantiated objects
            string cleanName = gameObject.name;
            if (cleanName.EndsWith("(Clone)"))
            {
                cleanName = cleanName.Substring(0, cleanName.Length - 7);
                objectToPrefabPathMap[cleanName] = prefabPath;
            }
            
            // If path contains a Resources folder, extract the part after it for proper loading
            if (prefabPath.Contains("Resources/"))
            {
                int resourceIndex = prefabPath.LastIndexOf("Resources/") + 10;
                string resourceRelativePath = prefabPath.Substring(resourceIndex);
                
                // If the path has extension, remove it (Resources.Load doesn't use extensions)
                if (resourceRelativePath.EndsWith(".prefab"))
                {
                    resourceRelativePath = resourceRelativePath.Substring(0, resourceRelativePath.Length - 7);
                }
                
                // Store the adjusted path for easier loading
                objectToPrefabPathMap[gameObject.name + "_resource"] = resourceRelativePath;
                Debug.Log($"Registered resource-relative path: {resourceRelativePath}");
            }
            
            // Log registration to help with debugging
            Debug.Log($"Registered prefab path for {gameObject.name}: {prefabPath}");
            
            // If in editor, mark dirty so information persists
            #if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            #endif
        }
    }

    // Method to expose the save file path to the editor
    public string GetSaveFilePath()
    {
        return saveFilePath;
    }

    // Utility methods
    public Vector2 GridToIsometric(int x, int y)
    {
        float isoX = (x - y) * tileWidth / 2;
        float isoY = (x + y) * tileHeight / 2;
        return new Vector2(isoX, isoY);
    }

    public Vector2Int IsometricToGrid(Vector2 isoPosition)
    {
        float x = (isoPosition.x / tileWidth + isoPosition.y / tileHeight);
        float y = (-isoPosition.x / tileWidth + isoPosition.y / tileHeight);
        return new Vector2Int(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
    }

    public bool IsValidGridPosition(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < mapWidth && gridPos.y >= 0 && gridPos.y < mapHeight;
    }

    public bool TryMove(Vector2Int currentGridPos, Vector2Int direction, out Vector2Int newGridPos)
    {
        newGridPos = currentGridPos + direction;
        return IsValidGridPosition(newGridPos);
    }

    public bool TryDeleteObjectAt(Vector2Int gridPosition)
    {
        // Use our improved GetTileAt method to find the tile more reliably
        GameObject tile = GetTileAt(gridPosition);
        
        if (tile != null && tile.transform.childCount > 0)
        {
            // Log what we're going to delete for easier debugging
            Debug.Log($"Deleting objects on tile at {gridPosition}. Found {tile.transform.childCount} children.");
            
            // Create a temporary list to avoid modification during enumeration issues
            List<GameObject> childrenToDelete = new List<GameObject>();
            foreach (Transform child in tile.transform)
            {
                childrenToDelete.Add(child.gameObject);
            }
            
            // Now delete all children
            foreach (GameObject childObject in childrenToDelete)
            {
                if (Application.isPlaying)
                    Destroy(childObject);
                else
                    DestroyImmediate(childObject);
            }
            
            // Mark this change for auto-saving
            pendingChanges = true;
            return true;
        }
        
        Debug.Log($"No objects to delete at position {gridPosition}. Tile exists: {tile != null}");
        return false;
    }

    // Save when the component is disabled (which happens when exiting play mode)
    private void OnDisable()
    {
        // Only save if we're in play mode and not in the process of application quitting
        // The check for Time.frameCount prevents issues during scene reload
        if (Application.isPlaying && !isCurrentlyLoading && CanSaveOrLoad && Time.frameCount > 0)
        {
            Debug.Log("TilemapGenerator being disabled (exiting play mode) - saving tilemap state");
            SaveTilemap();
        }
    }

#if UNITY_EDITOR
    // This method is called when exiting play mode in the Unity Editor
    private void OnApplicationQuitInEditor()
    {
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode &&
            !UnityEditor.EditorApplication.isPlaying)
        {
            Debug.Log("Detected exit from play mode in Editor - saving tilemap");
            SaveTilemap();
        }
    }
    
    private void OnEnable()
    {
        // Subscribe to the playmodeStateChanged event
        UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }
    
    private void OnDestroy()
    {
        // Clean up subscription to prevent memory leaks
        UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }
    
    private void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
    {
        // We specifically want to save when exiting play mode
        if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
        {
            Debug.Log("Play mode is exiting - saving tilemap");
            SaveTilemap();
        }
        // We may also want to load when entering play mode
        else if (state == UnityEditor.PlayModeStateChange.EnteredPlayMode)
        {
            // Add a small delay to ensure scene is properly set up before loading
            UnityEditor.EditorApplication.delayCall += () => {
                if (CanSaveOrLoad && enableAutoSave)
                {
                    Debug.Log("Play mode entered - loading previously saved tilemap");
                    LoadTilemap();
                }
            };
        }
    }
#endif

    protected virtual void OnApplicationQuit()
    {
        if (CanSaveOrLoad)
        {
            SaveTilemap();
        }
    }

    // Debug method to print information about the current tile status
    public void DebugTileCount()
    {
        if (tiles == null)
        {
            Debug.Log("Tiles array is null. Map has not been generated yet.");
            return;
        }
        
        int totalTiles = tiles.GetLength(0) * tiles.GetLength(1);
        int nonNullTiles = 0;
        int tilesWithObjects = 0;
        
        // Gather detailed information about tiles with objects
        List<string> tileDetails = new List<string>();
        
        for (int x = 0; x < tiles.GetLength(0); x++)
        {
            for (int y = 0; y < tiles.GetLength(1); y++)
            {
                if (tiles[x, y] != null)
                {
                    nonNullTiles++;
                    if (tiles[x, y].transform.childCount > 0)
                    {
                        tilesWithObjects++;
                        
                        // Get detailed information about this occupied tile
                        GameObject occupant = tiles[x, y].transform.GetChild(0).gameObject;
                        string displayName = FormatDisplayName(StripCloneSuffix(occupant.name));
                        string resourcePath = "";
                        objectToPrefabPathMap.TryGetValue(occupant.name, out resourcePath);
                        
                        // Include world position data
                        Vector3 worldPos = occupant.transform.position;
                        
                        tileDetails.Add($"Tile ({x},{y}) at [{worldPos.x:F2}, {worldPos.y:F2}, {worldPos.z:F2}]: " +
                                       $"{occupant.name} - Display: '{displayName}', Path: '{resourcePath}'");
                    }
                }
            }
        }
        
        // Also debug the prefab dictionary contents
        string pathDebug = "Registered prefab paths:\n";
        foreach (var kvp in objectToPrefabPathMap)
        {
            pathDebug += $"- {kvp.Key}: {kvp.Value}\n";
        }
        
        Debug.Log($"Tilemap Debug:\n" +
                 $"Dimensions: {tiles.GetLength(0)}x{tiles.GetLength(1)}\n" +
                 $"Total tiles: {totalTiles}\n" +
                 $"Non-null tiles: {nonNullTiles}\n" +
                 $"Tiles with objects: {tilesWithObjects}");
                 
        // Log detailed tile information
        if (tileDetails.Count > 0)
        {
            Debug.Log("Detailed tile information:\n" + string.Join("\n", tileDetails));
        }
        
        Debug.Log(pathDebug);
                 
        // Debug the save file
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            SaveData saveData = JsonUtility.FromJson<SaveData>(json);
            
            Debug.Log($"Save file info: Created on {saveData.lastSaveDate}");
            Debug.Log($"Saved tiles count: {saveData.tiles.Count}");
            
            // Show a sample of saved tile data
            if (saveData.tiles.Count > 0)
            {
                string sampleTiles = "Sample of saved tiles (up to 5):\n";
                for (int i = 0; i < Mathf.Min(5, saveData.tiles.Count); i++)
                {
                    TileMapSystem.TileData tile = saveData.tiles[i];
                    Vector3 pos = tile.worldPosition;
                    sampleTiles += $"{i+1}. Grid: ({tile.x},{tile.y}) - World: [{pos.x:F2}, {pos.y:F2}, {pos.z:F2}]\n" +
                                   $"   Name: '{tile.prefabName}'\n" +
                                   $"   Display: '{tile.displayName}'\n" + 
                                   $"   Original Name: '{tile.prefabOriginalName}'\n" +
                                   $"   Path: '{tile.resourcePath}'\n" +
                                   $"   Type: '{tile.tileType}'\n";
                }
                Debug.Log(sampleTiles);
            }
        }
        else
        {
            Debug.Log("No save file exists yet.");
        }
    }

    // Public method to get a tile at a specific grid position - enhanced for better reliability
    public GameObject GetTileAt(Vector2Int gridPosition)
    {
        // Early out for clearly invalid positions
        if (!IsValidGridPosition(gridPosition))
        {
            return null;
        }
        
        // Case 1: Check our tiles array first
        if (tiles != null && tiles.GetLength(0) > gridPosition.x && tiles.GetLength(1) > gridPosition.y)
        {
            GameObject tile = tiles[gridPosition.x, gridPosition.y];
            if (tile != null)
            {
                return tile;
            }
        }
        
        // Case 2: If using Unity Grid, try to find the tile at that cell position
        if (useUnityGrid && unityGrid != null && gridContainer != null)
        {
            // Try to find a child with the matching name pattern
            string tileName = $"Tile_{gridPosition.x}_{gridPosition.y}";
            Transform tileTransform = gridContainer.Find(tileName);
            if (tileTransform != null)
            {
                return tileTransform.gameObject;
            }
            
            // If not found by name, try finding the object at the world position
            Vector3Int cellPos = new Vector3Int(gridPosition.x, gridPosition.y, 0);
            Vector3 worldPos = unityGrid.GetCellCenterWorld(cellPos);
            
            // Find any game object near this position
            Collider2D[] colliders = Physics2D.OverlapCircleAll(new Vector2(worldPos.x, worldPos.y), 0.1f);
            foreach (Collider2D collider in colliders)
            {
                if (collider.gameObject.name.StartsWith("Tile_"))
                {
                    return collider.gameObject;
                }
            }
        }
        
        // Case 3: For custom grid, try finding by name in the grid container
        if (gridContainer != null)
        {
            foreach (Transform child in gridContainer)
            {
                if (child.name == $"Tile_{gridPosition.x}_{gridPosition.y}")
                {
                    return child.gameObject;
                }
            }
        }
        
        // Still not found - add debug information to help troubleshoot
        Debug.LogWarning($"Could not find tile at position ({gridPosition.x}, {gridPosition.y}). " +
                        $"Map dimensions: {(tiles != null ? $"{tiles.GetLength(0)}x{tiles.GetLength(1)}" : "tiles array is null")}. " +
                        $"Grid container has {(gridContainer != null ? gridContainer.childCount : 0)} children.");
        
        return null;
    }

    // Helper method to get tile at world position
    public GameObject GetTileAtWorldPosition(Vector2 worldPosition)
    {
        Vector2Int gridPos = IsometricToGrid(worldPosition);
        return GetTileAt(gridPos);
    }

    // Helper method to check if a point is over a valid tile
    public bool IsPointOverTile(Vector2 worldPosition)
    {
        Vector2Int gridPos = IsometricToGrid(worldPosition);
        return IsValidGridPosition(gridPos) && tiles != null && tiles[gridPos.x, gridPos.y] != null;
    }

    // Get the transform of the grid container
    public Transform GetGridContainer()
    {
        EnsureGridSetupExists();
        return gridContainer;
    }

    // Helper method to check if tiles already exist in the grid
    private bool CheckIfTilesExist()
    {
        // First, ensure grid container exists
        EnsureGridSetupExists();
        
        // If using Unity Grid, check if there are tilemaps with content
        if (useUnityGrid && unityTilemap != null && unityTilemap.GetUsedTilesCount() > 0)
        {
            return true;
        }
        
        // For custom grid, check if there are child objects in the grid container
        if (gridContainer != null && gridContainer.childCount > 0)
        {
            return true;
        }
        
        // Check if tiles array has been initialized and has contents
        if (tiles != null && tiles.GetLength(0) > 0 && tiles.GetLength(1) > 0)
        {
            // Check if at least one tile exists
            for (int x = 0; x < tiles.GetLength(0); x++)
            {
                for (int y = 0; y < tiles.GetLength(1); y++)
                {
                    if (tiles[x, y] != null)
                    {
                        return true;
                    }
                }
            }
        }
        
        return false;
    }

    // Update check for auto-save functionality during runtime
    private void Update()
    {
        // Only auto-save during gameplay, not in editor
        if (!Application.isPlaying || !enableAutoSave || isCurrentlyLoading)
            return;
            
        // If we have pending changes and it's time to save
        if (pendingChanges && Time.time >= lastAutoSaveTime + autoSaveInterval)
        {
            Debug.Log("Auto-saving tilemap due to changes...");
            SaveTilemap();
            pendingChanges = false;
            lastAutoSaveTime = Time.time;
        }
    }

    // Method to mark that changes have been made that require saving
    public void MarkForSave()
    {
        if (!enableAutoSave || isCurrentlyLoading)
            return;
            
        pendingChanges = true;
        
        // If interval is 0, save immediately
        if (autoSaveInterval <= 0)
        {
            SaveTilemap();
            pendingChanges = false;
            lastAutoSaveTime = Time.time;
        }
    }
}
