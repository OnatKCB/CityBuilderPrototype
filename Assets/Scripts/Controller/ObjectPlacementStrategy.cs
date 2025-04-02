using System.Collections.Generic;
using UnityEngine;
using BuildingType = System.Int32;

public class ObjectPlacementStrategy : IInteractionStrategy
{
    private GameObject[] buildingPrefabs;
    private IBuildingMenu buildingMenu;
    private int selectedBuildingIndex = -1;
    
    // Store resource paths for each prefab
    private Dictionary<GameObject, string> prefabResourcePaths = new Dictionary<GameObject, string>();
    
    // Reference to LayerOrderManager for handling object sorting
    private LayerOrderManager layerOrderManager;

    public ObjectPlacementStrategy(GameObject[] prefabs, IBuildingMenu menu)
    {
        buildingPrefabs = prefabs;
        buildingMenu = menu;
        buildingMenu.Initialize(OnBuildingSelected);
        
        // Cache the resource paths for all building prefabs
        InitializePrefabResourcePaths();
        
        // Find the LayerOrderManager in the scene
        layerOrderManager = Object.FindObjectOfType<LayerOrderManager>();
        if (layerOrderManager == null)
        {
            Debug.LogWarning("No LayerOrderManager found in scene. Objects won't be auto-sorted.");
        }
    }
    
    private void InitializePrefabResourcePaths()
    {
        prefabResourcePaths = new Dictionary<GameObject, string>();
        
        // For each prefab, try to determine its Resources path
        for (int i = 0; i < buildingPrefabs.Length; i++)
        {
            GameObject prefab = buildingPrefabs[i];
            if (prefab != null)
            {
                // Try to get the resource path
                string resourcePath = GetResourcePath(prefab);
                if (!string.IsNullOrEmpty(resourcePath))
                {
                    prefabResourcePaths[prefab] = resourcePath;
                    Debug.Log($"Registered resource path for prefab {prefab.name}: {resourcePath}");
                }
                else
                {
                    Debug.LogWarning($"Could not determine resource path for prefab: {prefab.name}. " +
                                     "Make sure it's located in a Resources folder.");
                }
            }
        }
    }
    
    private string GetResourcePath(GameObject prefab)
    {
        // Helper method to extract resource path from a prefab
        // This is a simple implementation that assumes prefabs are directly in the Resources folder
        // A more robust solution might use AssetDatabase in editor
        #if UNITY_EDITOR
        string path = UnityEditor.AssetDatabase.GetAssetPath(prefab);
        if (string.IsNullOrEmpty(path))
            return null;
            
        // Check if it's in a Resources folder
        int resourcesIndex = path.IndexOf("Resources/");
        if (resourcesIndex < 0)
        {
            Debug.LogWarning($"Prefab {prefab.name} is not in a Resources folder. It cannot be loaded at runtime.");
            return null;
        }
        
        // Extract the path after "Resources/"
        string resourcePath = path.Substring(resourcesIndex + 10); // "Resources/".Length = 10
        
        // Remove the extension
        if (resourcePath.EndsWith(".prefab"))
        {
            resourcePath = resourcePath.Substring(0, resourcePath.Length - 7); // ".prefab".Length = 7
        }
        
        return resourcePath;
        #else
        // Runtime fallback - less reliable
        return prefab.name;
        #endif
    }

    private void OnBuildingSelected(BuildingType buildingType)
    {
        if (buildingType >= 0 && buildingType < buildingPrefabs.Length)
        {
            selectedBuildingIndex = buildingType; // Store the selected building index
            Debug.Log($"Selected building type: {buildingType} ({buildingPrefabs[buildingType].name})");
        }
        else
        {
            Debug.LogWarning($"Invalid building type selected: {buildingType}. Max index: {buildingPrefabs.Length - 1}");
            // Default to the first building if available
            selectedBuildingIndex = (buildingPrefabs.Length > 0) ? 0 : -1;
        }
        
        // Hide menu only after successful selection
        buildingMenu.Hide(); 
    }

    public void ProcessInteraction(Vector2 mousePosition, TilemapGenerator tilemap)
    {
        // Allow placing objects directly when in build mode
        if (selectedBuildingIndex == -1 || selectedBuildingIndex >= buildingPrefabs.Length)
        {
            Debug.LogWarning("Invalid building index selected.");
            return; // Ensure a valid building index is selected
        }

        // Check if the left mouse button is clicked
        if (Input.GetMouseButtonDown(0))
        {
            Vector2Int gridPosition = GetGridPositionFromMouse(mousePosition, tilemap);
            Debug.Log($"Attempting to place object at grid position: {gridPosition}");

            if (tilemap.IsValidGridPosition(gridPosition))
            {
                // Get tile at the grid position
                GameObject tile = tilemap.GetTileAt(gridPosition);
                
                if (tile == null)
                {
                    Debug.LogWarning($"No tile found at grid position: {gridPosition}");
                    return;
                }
                
                // Check if the tile already has a child object
                if (tile.transform.childCount > 0)
                {
                    Debug.LogWarning($"Tile at {gridPosition} already has an object. Cannot place another.");
                    return; // Prevent placing an object if the tile is occupied
                }

                GameObject selectedPrefab = buildingPrefabs[selectedBuildingIndex];
                
                // Get the appropriate world position based on grid type
                Vector3 worldPosition;
                if (tilemap.useUnityGrid && tilemap.unityGrid != null)
                {
                    worldPosition = tilemap.unityGrid.GetCellCenterWorld(new Vector3Int(gridPosition.x, gridPosition.y, 0));
                }
                else
                {
                    Vector2 isoPosition = tilemap.GridToIsometric(gridPosition.x, gridPosition.y);
                    worldPosition = new Vector3(isoPosition.x, isoPosition.y, 0);
                }
                
                Debug.Log($"Placing object at world position: {worldPosition}");

                GameObject newObject = Object.Instantiate(selectedPrefab, worldPosition, Quaternion.identity);
                if (newObject == null)
                {
                    Debug.LogError("Failed to instantiate the prefab. Check if the prefab is assigned correctly.");
                    return;
                }

                // Remove the "(Clone)" suffix from the name
                newObject.name = selectedPrefab.name; // Set the name to the prefab's name

                // Set the tile as the parent
                newObject.transform.parent = tile.transform;
                
                // Register the prefab path with the tilemap generator
                if (prefabResourcePaths.TryGetValue(selectedPrefab, out string resourcePath))
                {
                    tilemap.RegisterPrefabPath(newObject, resourcePath);
                    Debug.Log($"Registered resource path '{resourcePath}' for object {newObject.name}");
                }
                else
                {
                    Debug.LogWarning($"No resource path found for prefab {selectedPrefab.name}. Save/load functionality may be limited.");
                }
                
                // Update layer ordering after placing an object
                if (layerOrderManager != null)
                {
                    layerOrderManager.MarkSortingDirty();
                    Debug.Log("Layer ordering marked for update");
                }
                
                // Mark the tilemap for auto-saving after placing an object
                tilemap.MarkForSave();
                
                Debug.Log($"Object placed at grid position: {gridPosition}, world position: {worldPosition}");
            }
            else
            {
                Debug.LogWarning($"Invalid grid position for placement: {gridPosition}");
            }
        }
    }

    private Vector2Int GetGridPositionFromMouse(Vector2 mousePosition, TilemapGenerator tilemap)
    {
        // Add null checks to avoid NullReferenceException
        if (tilemap == null || Camera.main == null)
        {
            Debug.LogError("Tilemap or Camera.main is null in GetGridPositionFromMouse");
            return new Vector2Int(-1, -1); // Return invalid position
        }

        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, 0));
        Vector2 worldPos2D = new Vector2(worldPosition.x, worldPosition.y);
        
        // Different conversion logic based on the grid type
        if (tilemap.useUnityGrid && tilemap.unityGrid != null)
        {
            Vector3Int cellPosition = tilemap.unityGrid.WorldToCell(worldPosition);
            return new Vector2Int(cellPosition.x, cellPosition.y);
        }
        else
        {
            // Use the tilemap's IsometricToGrid method to convert world position to grid position
            return tilemap.IsometricToGrid(worldPos2D);
        }
    }

    public void OnModeEnter()
    {
        // Implement any necessary logic when entering this mode
    }

    public void OnModeExit()
    {
        buildingMenu.Hide();
        selectedBuildingIndex = -1;
    }
}