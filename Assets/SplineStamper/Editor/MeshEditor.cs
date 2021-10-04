using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MeshCreator))]
public class MeshEditor : Editor
{

    MeshCreator creator;
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUI.BeginChangeCheck();

        if (GUILayout.Button("Save Mesh"))
        {
            Undo.RecordObject(creator, "Save Mesh");
            creator.SaveMesh();
        }

        if (EditorGUI.EndChangeCheck())
        {
            SceneView.RepaintAll();
        }

    }

    void OnSceneGUI()
    {
        Event guiEvent = Event.current;

        if (creator.autoUpdate && guiEvent.type == EventType.Repaint)
        {
            creator.UpdateRoad();
        }

        if (guiEvent.type == EventType.MouseMove)
        {
            SetCreator();
        }
    }

    void OnEnable()
    {
        SetCreator();
    }

    void SetCreator()
    {
        creator = (MeshCreator)target;
    }
}