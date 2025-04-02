using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class IsometricInteractionController : MonoBehaviour
{
    [Header("Key Bindings")]
    [SerializeField] private KeyCode cameraModeKey = KeyCode.C;
    [SerializeField] private KeyCode placementModeKey = KeyCode.P;
    [SerializeField] private KeyCode deletionModeKey = KeyCode.D;

    [Header("References")]
    [SerializeField] private TilemapGenerator tilemap;
    [SerializeField] private GameObject objectPrefab;
    [SerializeField] private float dragSpeed = 0.5f;
    [SerializeField] private GameObject[] buildingPrefabs;
    [SerializeField] private BuildingMenu buildingMenu;

    private Dictionary<InteractionMode, IInteractionStrategy> interactionStrategies;
    private InteractionMode currentMode = InteractionMode.CameraControl;
    private bool isInteracting = false;

    private void Start()
    {
        InitializeStrategies();
        SetMode(InteractionMode.CameraControl); // Set initial mode
    }

    private void InitializeStrategies()
    {
        interactionStrategies = new Dictionary<InteractionMode, IInteractionStrategy>
        {
            { InteractionMode.CameraControl, new CameraControlStrategy(dragSpeed) },
            { InteractionMode.ObjectPlacement, new ObjectPlacementStrategy(buildingPrefabs, buildingMenu) },
            { InteractionMode.ObjectDeletion, new ObjectDeletionStrategy() }
        };
    }

    private void SetMode(InteractionMode mode)
    {
        if (currentMode != mode)
        {
            // Exit the current mode
            if (interactionStrategies.ContainsKey(currentMode))
            {
                interactionStrategies[currentMode].OnModeExit();
            }

            // Set the new mode
            currentMode = mode;

            // Enter the new mode
            if (interactionStrategies.ContainsKey(currentMode))
            {
                interactionStrategies[currentMode].OnModeEnter();
            }

            // Show the building menu if entering ObjectPlacement mode
            if (currentMode == InteractionMode.ObjectPlacement)
            {
                buildingMenu.Show(); // Show the building menu
            }

            Debug.Log($"Switched to mode: {currentMode}");
        }
    }

    public void ToggleBuildMode() 
    {
        StartCoroutine(ActivateBuildModeAfterMenuClose());
    }

    private IEnumerator ActivateBuildModeAfterMenuClose()
    {
        // Wait until the building menu is closed
        while (buildingMenu.IsActive())
        {
            yield return null; // Wait for the next frame
        }

        // Now set the mode to ObjectPlacement
        SetMode(InteractionMode.ObjectPlacement);
    }

    private void Update()
    {
        HandleModeSelection(); // Check for mode changes
        HandleInteraction(); // Handle interactions based on the current mode
    }

    private void HandleInteraction()
    {
        // Check if the pointer is over a UI element
        if (IsPointerOverUIElement())
        {
            return; // Do not process tilemap interaction if over UI
        }

        if (interactionStrategies.ContainsKey(currentMode))
        {
            interactionStrategies[currentMode].ProcessInteraction(Input.mousePosition, tilemap);
        }
    }

    // Method to check if the pointer is over a UI element
    private bool IsPointerOverUIElement()
    {
        // Check if the pointer is over any UI element
        return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }

    public void PlaceObject()
    {
        Debug.Log("PlaceObject method called");

        // Example positions, replace with your actual logic
        Vector3 worldPosition = new Vector3(0, 0, 0); // Replace with actual world position calculation
        Vector2Int gridPosition = new Vector2Int(0, 0); // Replace with actual grid position calculation

        if (objectPrefab == null || tilemap == null)
        {
            Debug.LogError("objectPrefab or tilemap is not assigned.");
            return;
        }

        GameObject newObject = Object.Instantiate(objectPrefab, new Vector3(worldPosition.x, worldPosition.y, 0), Quaternion.identity);
        newObject.transform.parent = tilemap.tiles[gridPosition.x, gridPosition.y].transform; // Set the tile as the parent
        Debug.Log($"Object placed at grid position: {gridPosition}, world position: {worldPosition}");
    }

    private void HandleModeSelection()
    {
        InteractionMode newMode = currentMode;

        if (Input.GetKeyDown(cameraModeKey))
            newMode = InteractionMode.CameraControl;
        else if (Input.GetKeyDown(placementModeKey))
            newMode = InteractionMode.ObjectPlacement;
        else if (Input.GetKeyDown(deletionModeKey))
            newMode = InteractionMode.ObjectDeletion;

        if (newMode != currentMode)
        {
            interactionStrategies[currentMode].OnModeExit();
            currentMode = newMode;
            interactionStrategies[currentMode].OnModeEnter();
        }
    }

    public enum Mode {
        None,
        CameraControl,
        Build,
        Delete
    }

    // Method to activate a mode and deactivate others
    private void SetMode(Mode mode) {
        // Deactivate all strategies first
        foreach (var strategy in interactionStrategies.Values)
        {
            strategy.OnModeExit();
        }

        // Update currentMode to the new mode
        currentMode = (InteractionMode)mode;

        // Activate the selected strategy
        if (interactionStrategies.ContainsKey(currentMode))
        {
            interactionStrategies[currentMode].OnModeEnter();
        }
    }
    // Public methods to be called by UI buttons
    public void ToggleCameraControl() {
        SetMode(InteractionMode.CameraControl);
    }

    public void ToggleDeleteMode() {
        SetMode(InteractionMode.ObjectDeletion);
    }
}