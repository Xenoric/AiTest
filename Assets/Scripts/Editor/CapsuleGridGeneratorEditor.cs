using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(CapsuleGridGenerator))]
public class CapsuleGridGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CapsuleGridGenerator generator = (CapsuleGridGenerator)target;

        generator.showNodeNeighborsInEditor = EditorGUILayout.Toggle(
            "Show Node Neighbors", 
            generator.showNodeNeighborsInEditor
        );

        EditorGUILayout.Space();
        
        if (GUILayout.Button("Generate Border Nodes"))
        {
            generator.ClearAllNodes();
            generator.GenerateBorderNodes();
        }

        if (GUILayout.Button("Clear Nodes"))
        {
            generator.ClearAllNodes();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Save Nodes"))
        {
            generator.SaveNodesToFile();
        }
        
        EditorGUILayout.Space();

        if (GUILayout.Button("Load Nodes"))
        {
            generator.LoadNodesFromFile();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Save Nodes with Neighbors"))
        {
            generator.SaveNodesWithNeighbors();
        }
    }
}