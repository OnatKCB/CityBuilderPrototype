using UnityEngine;

public class ObjectDeletionStrategy : IInteractionStrategy
{
    public void ProcessInteraction(Vector2 mousePosition, TilemapGenerator tilemap)
    {
        // Check if the left mouse button is clicked (for deletion)
        if (Input.GetMouseButtonDown(0)) // Right click to delete
        {
            // Convert mouse position to grid position
            Vector2Int gridPosition = GetGridPositionFromMouse(mousePosition, tilemap);
            Debug.Log($"Attempting to delete object at grid position: {gridPosition}");
            
            if (tilemap.IsValidGridPosition(gridPosition))
            {
                if (tilemap.TryDeleteObjectAt(gridPosition))
                {
                    Debug.Log($"Object deleted at grid position: {gridPosition}");
                    
                    // Mark the tilemap for auto-saving after deleting an object
                    tilemap.MarkForSave();
                }
                else
                {
                    Debug.Log($"No object to delete at grid position: {gridPosition}");
                }
            }
            else
            {
                Debug.LogWarning($"Invalid grid position for deletion: {gridPosition}");
            }
        }
    }

    private Vector2Int GetGridPositionFromMouse(Vector2 mousePosition, TilemapGenerator tilemap)
    {
        // Null checks to avoid NullReferenceException
        if (tilemap == null || Camera.main == null)
        {
            Debug.LogError("Tilemap or Camera.main is null in GetGridPositionFromMouse");
            return new Vector2Int(-1, -1); // Return invalid position
        }

        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, 0));
        
        // Different conversion logic based on the grid type
        if (tilemap.useUnityGrid && tilemap.unityGrid != null)
        {
            Vector3Int cellPosition = tilemap.unityGrid.WorldToCell(worldPosition);
            return new Vector2Int(cellPosition.x, cellPosition.y);
        }
        else
        {
            // Use the tilemap's IsometricToGrid method
            return tilemap.IsometricToGrid(new Vector2(worldPosition.x, worldPosition.y));
        }
    }

    public void OnModeEnter()
    {
        Debug.Log("Entered Object Deletion Mode - Click to delete objects");
    }

    public void OnModeExit() { }
}