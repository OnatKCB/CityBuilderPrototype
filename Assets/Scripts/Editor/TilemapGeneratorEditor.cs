using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.Tilemaps;

[CustomEditor(typeof(TilemapGenerator))]
public class TilemapGeneratorEditor : Editor
{
    private SerializedProperty mapWidthProperty;
    private SerializedProperty mapHeightProperty;
    private SerializedProperty tileWidthProperty;
    private SerializedProperty tileHeightProperty;
    private SerializedProperty tilePrefabProperty;
    private SerializedProperty saveFilePathProperty;
    private SerializedProperty hasBeenGeneratedProperty;
    
    // New properties for Unity Grid integration
    private SerializedProperty useUnityGridProperty;
    private SerializedProperty unityGridProperty;
    private SerializedProperty gridContainerProperty;
    
    private void OnEnable()
    {
        mapWidthProperty = serializedObject.FindProperty("mapWidth");
        mapHeightProperty = serializedObject.FindProperty("mapHeight");
        tileWidthProperty = serializedObject.FindProperty("tileWidth");
        tileHeightProperty = serializedObject.FindProperty("tileHeight");
        tilePrefabProperty = serializedObject.FindProperty("tilePrefab");
        saveFilePathProperty = serializedObject.FindProperty("saveFilePath");
        hasBeenGeneratedProperty = serializedObject.FindProperty("hasBeenGenerated");
        
        // Get Unity Grid integration properties
        useUnityGridProperty = serializedObject.FindProperty("useUnityGrid");
        unityGridProperty = serializedObject.FindProperty("unityGrid");
        gridContainerProperty = serializedObject.FindProperty("gridContainer");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        TilemapGenerator generator = (TilemapGenerator)target;
        
        EditorGUILayout.LabelField("Tilemap Settings", EditorStyles.boldLabel);
        
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(mapWidthProperty, new GUIContent("Map Width"));
        EditorGUILayout.PropertyField(mapHeightProperty, new GUIContent("Map Height"));
        bool dimensionsChanged = EditorGUI.EndChangeCheck();
        
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(tileWidthProperty, new GUIContent("Tile Width"));
        EditorGUILayout.PropertyField(tileHeightProperty, new GUIContent("Tile Height"));
        
        // Unity Grid integration section
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Grid Integration", EditorStyles.boldLabel);
        
        // Add useUnityGrid toggle with explanation
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(useUnityGridProperty, new GUIContent("Use Unity Grid", 
            "When enabled, tiles will be placed on Unity's built-in Grid system instead of using a custom container"));
        bool gridTypeChanged = EditorGUI.EndChangeCheck();
        
        // Show warning if grid type changed after map generation
        if (gridTypeChanged && hasBeenGeneratedProperty.boolValue)
        {
            EditorGUILayout.HelpBox("Changing grid type after generation requires regenerating the map!", MessageType.Warning);
        }
        
        // Only show grid reference field if Unity Grid is enabled
        if (useUnityGridProperty.boolValue)
        {
            EditorGUILayout.PropertyField(unityGridProperty, new GUIContent("Unity Grid", 
                "Reference to a Grid component (will be auto-created if empty)"));
            
            // Show information about existing Unity Grid components
            Grid[] existingGrids = FindObjectsOfType<Grid>();
            if (existingGrids.Length > 0)
            {
                EditorGUILayout.HelpBox($"Found {existingGrids.Length} Grid component(s) in the scene.", MessageType.Info);
            }
            
            // Show warning if no Grid component exists in scene
            if (existingGrids.Length == 0 && unityGridProperty.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("No Grid component found in scene. One will be created automatically.", MessageType.Info);
            }
        }
        else
        {
            // Show custom grid container field if not using Unity Grid
            EditorGUILayout.PropertyField(gridContainerProperty, new GUIContent("Grid Container", 
                "Transform where tiles will be parented (will be auto-created if empty)"));
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Tile Prefabs", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(tilePrefabProperty, new GUIContent("Tile Prefab"));
        
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(saveFilePathProperty, new GUIContent("Save File Path"));
        
        // Show a warning if dimensions were changed after generation
        if (dimensionsChanged && hasBeenGeneratedProperty.boolValue)
        {
            EditorGUILayout.HelpBox("Changing dimensions after map generation may cause issues with saved data. Consider regenerating the map.", MessageType.Warning);
        }
        
        serializedObject.ApplyModifiedProperties();
        
        EditorGUILayout.Space(10);
        
        // Generation status display
        if (hasBeenGeneratedProperty.boolValue)
        {
            EditorGUILayout.HelpBox("Map has been generated.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Map has not been generated yet.", MessageType.Info);
        }
        
        // Generation button
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Generate Tilemap", GUILayout.Height(30)))
        {
            Undo.RecordObject(generator, "Generate Tilemap");
            generator.GenerateMap();
            EditorUtility.SetDirty(generator);
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndVertical();
        
        // Save & Load buttons
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Save Tilemap"))
        {
            // Check if the map is generated first
            if (!generator.CanSaveOrLoad)
            {
                Debug.Log("Map not generated yet. Generating map before saving...");
                generator.GenerateMap();
            }
            generator.SaveTilemap();
            EditorUtility.SetDirty(generator);
        }
        
        if (GUILayout.Button("Load Tilemap"))
        {
            // Check if the map is generated first
            if (!generator.CanSaveOrLoad)
            {
                Debug.Log("Map not generated yet. Generating map before loading...");
                generator.GenerateMap();
            }
            generator.LoadTilemap();
            EditorUtility.SetDirty(generator);
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Advanced operations section
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Advanced Operations", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        // Clear all tiles button
        GUI.backgroundColor = new Color(1f, 0.7f, 0.7f); // Light red
        if (GUILayout.Button("Clear All Tiles"))
        {
            if (EditorUtility.DisplayDialog("Clear Confirmation", 
                "Are you sure you want to clear all tiles? This cannot be undone.", "Yes, Clear Tiles", "Cancel"))
            {
                Undo.RecordObject(generator, "Clear Tiles");
                generator.ClearExistingTiles();
                EditorUtility.SetDirty(generator);
            }
        }
        
        // Clear Resources Cache button
        if (GUILayout.Button("Clear Resources Cache"))
        {
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
            Debug.Log("Resources cache cleared. This might help if prefabs aren't loading correctly.");
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Force Reload without clearing tiles
        if (GUILayout.Button("Force Reload (Keep Base Tiles)"))
        {
            if (generator.CanSaveOrLoad)
            {
                // Special reload that preserves the base tiles but re-loads objects on them
                Undo.RecordObject(generator, "Force Reload Tilemap");
                generator.LoadTilemap();
                EditorUtility.SetDirty(generator);
                Debug.Log("Force reload completed. Check the console for any errors.");
            }
            else
            {
                Debug.LogWarning("Cannot reload - map not generated yet.");
            }
        }
        
        // Debug buttons section
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Debugging Tools", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        // Add debug button to show tile count
        if (GUILayout.Button("Debug: Print Tile Info"))
        {
            generator.DebugTileCount();
        }
        
        // Add button to view save file
        if (GUILayout.Button("View Save File"))
        {
            string path = saveFilePathProperty.stringValue;
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                Debug.Log("Save file content: " + json);
                EditorUtility.DisplayDialog("Save File Content", json, "Close");
            }
            else
            {
                Debug.LogWarning("Save file doesn't exist at: " + path);
                EditorUtility.DisplayDialog("File Not Found", "Save file doesn't exist at: " + path, "OK");
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Grid information section
        if (generator.useUnityGrid && generator.unityGrid != null)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Unity Grid Information", EditorStyles.boldLabel);
            
            EditorGUILayout.LabelField("Grid Cell Layout:", generator.unityGrid.cellLayout.ToString());
            EditorGUILayout.LabelField("Grid Cell Size:", generator.unityGrid.cellSize.ToString());
            
            // If Tilemap exists, show information about it
            if (generator.unityTilemap != null)
            {
                EditorGUILayout.LabelField("Tilemap Found:", generator.unityTilemap.name);
                EditorGUILayout.LabelField("Cell Count:", generator.unityTilemap.size.ToString());
            }
        }
        
        // Help box with updated instructions
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Instructions:\n" +
            "1. Set map width and height\n" +
            "2. Choose grid type (Unity Grid or Custom)\n" +
            "3. Assign a tile prefab\n" +
            "4. Click 'Generate Tilemap' to create the grid\n" +
            "5. Use 'Save Tilemap' to store the current state\n" +
            "6. Use 'Load Tilemap' to restore a saved state\n\n" +
            "If objects don't reload correctly after deletion:\n" +
            "- Try 'Clear Resources Cache' then 'Force Reload'\n" +
            "- Check Debug outputs for prefab path information",
            MessageType.Info);
    }
}