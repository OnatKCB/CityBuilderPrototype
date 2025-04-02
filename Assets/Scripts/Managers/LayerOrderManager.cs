using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the layer ordering of objects in an isometric grid.
/// Follows the Single Responsibility Principle by handling only layer ordering concerns.
/// </summary>
public class LayerOrderManager : MonoBehaviour
{
    [Header("Sorting Settings")]
    [Tooltip("Base layer for sorting objects")]
    public int baseSortingLayer = 0;
    
    [Tooltip("Distance between layers in the sorting order")]
    public int layerDistance = 10;
    
    [Tooltip("Reference to the TilemapGenerator")]
    public TilemapGenerator tilemapGenerator;
    
    // Used to track when we need to update sorting
    private bool sortingDirty = false;

    private void Awake()
    {
        // Auto-find TilemapGenerator if not set
        if (tilemapGenerator == null)
        {
            tilemapGenerator = FindObjectOfType<TilemapGenerator>();
            if (tilemapGenerator == null)
            {
                Debug.LogError("No TilemapGenerator found in the scene. LayerOrderManager requires a TilemapGenerator.");
            }
        }
    }

    private void Start()
    {
        // Initial sort
        UpdateAllObjectLayers();
    }

    private void LateUpdate()
    {
        // Check if we need to update sorting
        if (sortingDirty)
        {
            UpdateAllObjectLayers();
            sortingDirty = false;
        }
    }

    /// <summary>
    /// Updates sorting layers for all objects on the tilemap
    /// </summary>
    public void UpdateAllObjectLayers()
    {
        if (tilemapGenerator == null || tilemapGenerator.tiles == null)
        {
            return;
        }

        // Create a list of all objects to be sorted
        List<SortableObject> sortableObjects = new List<SortableObject>();

        // Collect all objects from the tiles
        for (int x = 0; x < tilemapGenerator.mapWidth; x++)
        {
            for (int y = 0; y < tilemapGenerator.mapHeight; y++)
            {
                GameObject tile = tilemapGenerator.tiles[x, y];
                if (tile != null && tile.transform.childCount > 0)
                {
                    for (int i = 0; i < tile.transform.childCount; i++)
                    {
                        GameObject obj = tile.transform.GetChild(i).gameObject;
                        
                        // Calculate the sort key based on position
                        // In isometric view, objects in the back (higher y) should be drawn first (lower order)
                        float sortKey = x + y; // Basic sorting key for isometric
                        
                        sortableObjects.Add(new SortableObject(obj, sortKey, new Vector2Int(x, y)));
                    }
                }
            }
        }
        
        // Sort objects from back to front (ascending order key)
        sortableObjects.Sort((a, b) => a.sortKey.CompareTo(b.sortKey));
        
        // Apply sorting layer to renderers
        for (int i = 0; i < sortableObjects.Count; i++)
        {
            SortableObject sortObj = sortableObjects[i];
            ApplySortingLayerToObject(sortObj.gameObject, baseSortingLayer + i * layerDistance);
            
            // Debug log to check sorting
            Debug.Log($"Sorted object: {sortObj.gameObject.name} at ({sortObj.gridPos.x}, {sortObj.gridPos.y}) " +
                     $"with sort key {sortObj.sortKey} to order {baseSortingLayer + i * layerDistance}");
        }
    }
    
    /// <summary>
    /// Applies sorting layer to all renderers in a game object
    /// </summary>
    private void ApplySortingLayerToObject(GameObject obj, int sortingOrder)
    {
        // Get all renderers including child renderers
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        
        foreach (Renderer renderer in renderers)
        {
            renderer.sortingOrder = sortingOrder;
        }
    }
    
    /// <summary>
    /// Marks sorting as dirty, will update in the next LateUpdate
    /// </summary>
    public void MarkSortingDirty()
    {
        sortingDirty = true;
    }
    
    /// <summary>
    /// Helper class to sort game objects
    /// </summary>
    private class SortableObject
    {
        public GameObject gameObject;
        public float sortKey;
        public Vector2Int gridPos;
        
        public SortableObject(GameObject obj, float key, Vector2Int pos)
        {
            gameObject = obj;
            sortKey = key;
            gridPos = pos;
        }
    }
}