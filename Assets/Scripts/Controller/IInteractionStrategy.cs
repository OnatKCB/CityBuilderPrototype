using UnityEngine;

public interface IInteractionStrategy
{
    void ProcessInteraction(Vector2 mousePosition, TilemapGenerator tilemap);
    void OnModeEnter();
    void OnModeExit();
}