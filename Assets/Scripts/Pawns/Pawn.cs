using UnityEngine;

public abstract class Pawn : MonoBehaviour
{
    protected Vector2Int currentGridPosition;
    protected TilemapGenerator currentTilemap;

    public Vector2Int CurrentGridPosition => currentGridPosition;

    protected virtual void Start()
    {
        currentTilemap = GetComponentInParent<TilemapGenerator>();
        if (currentTilemap == null)
        {
            Debug.LogError("Pawn must be a child of a TilemapGenerator!");
        }
        currentGridPosition = currentTilemap.IsometricToGrid(transform.position);
    }

    public virtual bool TryMove(Vector2Int direction)
    {
        if (currentTilemap.TryMove(currentGridPosition, direction, out Vector2Int newGridPos))
        {
            Vector2 newWorldPos = currentTilemap.GridToIsometric(newGridPos.x, newGridPos.y);
            transform.position = new Vector3(newWorldPos.x, newWorldPos.y, transform.position.z);
            currentGridPosition = newGridPos;
            return true;
        }
        return false;
    }

    protected abstract void HandleMovement();
}