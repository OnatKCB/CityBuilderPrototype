using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragMovementStrategy : ICameraMovementStrategy
{
    private readonly float dragSpeed;

    public DragMovementStrategy(float dragSpeed)
    {
        this.dragSpeed = dragSpeed;
    }

    public void ProcessMovement(Vector2 inputDelta, Transform cameraTransform)
    {
        Vector2 movement = -inputDelta * dragSpeed * Time.deltaTime;
        cameraTransform.Translate(movement);
    }
}
