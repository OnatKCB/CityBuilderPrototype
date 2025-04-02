using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraInputHandler
{
    private Vector2 lastMousePosition;
    private bool isDragging;

    public bool IsDragging => isDragging;
    public Vector2 InputDelta { get; private set; }

    public void ProcessInput()
    {
        Vector2 currentMousePosition = Input.mousePosition;

        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            lastMousePosition = currentMousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            InputDelta = currentMousePosition - lastMousePosition;
            lastMousePosition = currentMousePosition;
        }
        else
        {
            InputDelta = Vector2.zero;
        }
    }
}
