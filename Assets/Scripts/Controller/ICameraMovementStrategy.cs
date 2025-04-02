using UnityEngine;

// Interface for camera movement strategies
public interface ICameraMovementStrategy
{
    void ProcessMovement(Vector2 inputDelta, Transform cameraTransform);
}