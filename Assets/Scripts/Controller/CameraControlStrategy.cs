using UnityEngine;

public class CameraControlStrategy : IInteractionStrategy
{
    private Vector3? lastMousePosition;
    private readonly float dragSpeed;
    private Camera mainCamera;
    private Vector3 initialCameraRotation;
    private bool isDragging = false;

    public CameraControlStrategy(float dragSpeed)
    {
        this.dragSpeed = dragSpeed;
        this.mainCamera = Camera.main;
        if (this.mainCamera != null)
        {
            this.initialCameraRotation = mainCamera.transform.rotation.eulerAngles;
        }
        else
        {
            Debug.LogError("Main camera not found!");
        }
    }

    public void ProcessInteraction(Vector2 mousePosition, TilemapGenerator tilemap)
    {
        if (mainCamera == null)
        {
            Debug.LogError("Main camera is null in CameraControlStrategy");
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            lastMousePosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            lastMousePosition = null;
        }

        if (isDragging && lastMousePosition.HasValue)
        {
            Vector3 delta = Input.mousePosition - lastMousePosition.Value;

            // Check for NaN values
            if (float.IsNaN(delta.x) || float.IsNaN(delta.y))
            {
                Debug.LogWarning("Invalid delta detected in CameraControlStrategy");
                lastMousePosition = Input.mousePosition;
                return;
            }

            // Adjust movement based on camera's local axes
            Vector3 movement = new Vector3(
                -delta.x * dragSpeed * Time.deltaTime,
                -delta.y * dragSpeed * Time.deltaTime,
                0
            );

            // Check for NaN values in movement
            if (float.IsNaN(movement.x) || float.IsNaN(movement.y) || float.IsNaN(movement.z))
            {
                Debug.LogWarning("Invalid movement detected in CameraControlStrategy");
                lastMousePosition = Input.mousePosition;
                return;
            }

            // Move the camera in its own local space
            mainCamera.transform.Translate(movement, Space.Self);

            // Ensure the camera maintains its initial rotation
            mainCamera.transform.rotation = Quaternion.Euler(initialCameraRotation);

            lastMousePosition = Input.mousePosition;

            Debug.Log($"Camera moved: {movement}");
        }
    }

    public void OnModeEnter()
    {
        Debug.Log("Entered Camera Control Mode - Click and drag to move camera");
        isDragging = false;
        lastMousePosition = null;
    }

    public void OnModeExit()
    {
        isDragging = false;
        lastMousePosition = null;
    }
}