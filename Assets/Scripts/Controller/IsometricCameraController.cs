using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IsometricCameraController : MonoBehaviour
{
    [SerializeField] private float dragSpeed = 0.5f;
    [SerializeField] private Camera controlledCamera;

    private ICameraMovementStrategy movementStrategy;
    private CameraInputHandler inputHandler;

    private void Awake()
    {
        ValidateAndInitialize();
    }

    private void ValidateAndInitialize()
    {
        if (controlledCamera == null)
        {
            controlledCamera = Camera.main;
            if (controlledCamera == null)
            {
                Debug.LogError("No camera assigned and no Main camera found in the scene!");
                enabled = false;
                return;
            }
        }

        movementStrategy = new DragMovementStrategy(dragSpeed);
        inputHandler = new CameraInputHandler();
    }

    private void Update()
    {
        inputHandler.ProcessInput();
        
        if (inputHandler.IsDragging)
        {
            movementStrategy.ProcessMovement(inputHandler.InputDelta, controlledCamera.transform);
        }
    }

    // Boundary checking could be added here
    public bool IsWithinBounds(Vector2 position)
    {
        // Implement boundary checking logic here if needed
        return true;
    }
}