#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Custom editor for TilemapSaveManager that provides UI for save/load operations
/// </summary>
[CustomEditor(typeof(TilemapSaveManager))]
public class TilemapSaveManagerEditor : Editor
{
    private string newSaveName = "";
    private Vector2 scrollPosition;
    private bool showSaveList = true;
    private bool showNewSaveOptions = false;
    private bool showDeleteConfirmation = false;
    private TilemapSaveManager.SaveInfo saveToDelete;

    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();
        
        // Get a reference to the target
        TilemapSaveManager saveManager = (TilemapSaveManager)target;
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Tilemap Save Management", EditorStyles.boldLabel);
        
        // Current save info
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Current Map:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Name: {saveManager.GetCurrentSaveName()}");
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(5);
        
        // New save section
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        showNewSaveOptions = EditorGUILayout.Foldout(showNewSaveOptions, "Save Current Map", true);
        
        if (showNewSaveOptions)
        {
            // Save name field
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Save Name:", GUILayout.Width(80));
            newSaveName = EditorGUILayout.TextField(newSaveName);
            EditorGUILayout.EndHorizontal();

            // Save buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save As New"))
            {
                if (!string.IsNullOrEmpty(newSaveName))
                {
                    saveManager.SaveTilemap(newSaveName);
                    newSaveName = "";
                }
                else
                {
                    EditorUtility.DisplayDialog("Invalid Name", "Please enter a valid save name.", "OK");
                }
            }

            if (GUILayout.Button("Save (Overwrite Current)"))
            {
                saveManager.SaveTilemap();
            }
            EditorGUILayout.EndHorizontal();
            
            // Generate new map button
            if (GUILayout.Button("Generate New Map"))
            {
                if (EditorUtility.DisplayDialog("Generate New Map", 
                    "This will clear the current map. Unsaved changes will be lost. Continue?", 
                    "Yes", "Cancel"))
                {
                    saveManager.CreateNewTilemap();
                }
            }
        }
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(5);
        
        // Saved maps section
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        showSaveList = EditorGUILayout.Foldout(showSaveList, "Saved Maps", true);
        
        if (showSaveList)
        {
            // Refresh button
            if (GUILayout.Button("Refresh Save List"))
            {
                saveManager.RefreshAvailableSaves();
            }
            
            List<TilemapSaveManager.SaveInfo> saves = saveManager.GetAvailableSaves();
            
            if (saves.Count == 0)
            {
                EditorGUILayout.HelpBox("No saved maps found.", MessageType.Info);
            }
            else
            {
                // Scrollable list of saved maps
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
                
                foreach (var save in saves)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    EditorGUILayout.LabelField(save.saveName, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Last Saved: {save.lastSaveDate}");
                    EditorGUILayout.LabelField($"Map Size: {save.mapWidth}x{save.mapHeight} ({save.tileCount} tiles)");
                    
                    if (!string.IsNullOrEmpty(save.description))
                    {
                        EditorGUILayout.LabelField($"Description: {save.description}");
                    }
                    
                    EditorGUILayout.BeginHorizontal();
                    
                    // Load button
                    if (GUILayout.Button("Load"))
                    {
                        if (EditorUtility.DisplayDialog("Load Map", 
                            $"Load map '{save.saveName}'? Unsaved changes to the current map will be lost.", 
                            "Load", "Cancel"))
                        {
                            saveManager.LoadTilemap(save);
                        }
                    }
                    
                    // Delete button
                    if (GUILayout.Button("Delete", GUILayout.Width(60)))
                    {
                        saveToDelete = save;
                        showDeleteConfirmation = true;
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                }
                
                EditorGUILayout.EndScrollView();
            }
        }
        EditorGUILayout.EndVertical();
        
        // Delete confirmation dialog
        if (showDeleteConfirmation && saveToDelete != null)
        {
            if (EditorUtility.DisplayDialog("Confirm Delete", 
                $"Are you sure you want to delete '{saveToDelete.saveName}'? This action cannot be undone.", 
                "Delete", "Cancel"))
            {
                saveManager.DeleteSave(saveToDelete);
                saveToDelete = null;
            }
            
            showDeleteConfirmation = false;
        }
        
        // Force the inspector to update if something changes
        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }
}
#endif
