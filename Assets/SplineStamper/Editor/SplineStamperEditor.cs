using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SplineStamper))]
public class SplineStamperEditor : Editor
{
    SplineStamper maker;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUI.BeginChangeCheck();

        /*
        if (GUILayout.Button("Create New Stamper"))
        {
            //Undo.RecordObject(maker, "Create New");
            //maker.CreatePathCreator();
            //path = creator.path;
        }
        */
        /*
        if (GUILayout.Button("Destroy All Stampers"))
        {
            Undo.RecordObject(maker, "Destroy All");
            maker.DestroyAll();
            //path = creator.path;
        }
        */
        if (EditorGUI.EndChangeCheck())
        {
            SceneView.RepaintAll();
        }
    }

    private void OnSceneGUI()
    {
        Event guiEvent = Event.current;
        if (guiEvent.type == EventType.Repaint)
            maker.UpdateEditor();

        Input();

        if (maker.creators.Count > 0)
        {
            Draw();
        }
    }

    void Input()
    {
        Event guiEvent = Event.current;

        Vector2 mousePos = Event.current.mousePosition;
        RaycastHit _saveRaycastHit;
        Ray worldRay = HandleUtility.GUIPointToWorldRay(mousePos);

        Vector3 mouseWorldPos = Vector3.zero;
        if (Physics.Raycast(worldRay, out _saveRaycastHit, Mathf.Infinity))
        {
            if (_saveRaycastHit.collider.gameObject != null)
            {
                mouseWorldPos = _saveRaycastHit.point;
            }
        }
        if (guiEvent.control)
            HandleUtility.AddDefaultControl(0);

        //On Mouse Click Split or Add segment
        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.control)
        {
            maker.CreatePathCreator(mouseWorldPos);
        }
    }


    void Draw()
    {
        if (maker.creators[0] == null)
            return;
        //Texture2D play = EditorGUIUtility.Load("Assets/Icons/play.png") as Texture2D;
        foreach (Stamper s in maker.creators)
        {
            Vector3 pos = Vector3.zero;
            int count = 0;
            for (int i = 0; i < s.spline.NumPoints; i++)
            {
                if (i % 3 == 0)
                {
                    pos += (s.spline[i] + s.transform.position);
                    count += 1;
                }
            }
            pos = pos / count;
            Texture2D tex = EditorGUIUtility.Load("SplineStamper/Icons/EditIcon.png") as Texture2D;
            GUIStyle style = new GUIStyle();
            style.normal.background = tex;
            Handles.BeginGUI();
            //Vector3 pos = s.transform.position;
            Vector2 pos2D = HandleUtility.WorldToGUIPoint(pos);
            float distance = HandleUtility.GetHandleSize(pos);
            float size = 500 / distance;
            if (GUI.Button(new Rect(pos2D.x - (size / 2), pos2D.y - (size / 2), size, size), "", style))
            {
                GameObject[] newSelection = new GameObject[1];
                newSelection[0] = s.gameObject;
                Selection.objects = newSelection;
            }
            Handles.EndGUI();

            //Handles.Button(s.transform.position, Quaternion.identity, 5, 10, Handles.SphereHandleCap);
            //if(Handles.Button(s.transform.position, Quaternion.identity, 5, 5, Handles.SphereHandleCap))
            //{

            //}
            //Handles.Label(s.transform.position + labelOffset, "", style);
        }
    }

    void GUISetup()
    {

    }






    void OnEnable()
    {
        maker = (SplineStamper)target;
    }
}


