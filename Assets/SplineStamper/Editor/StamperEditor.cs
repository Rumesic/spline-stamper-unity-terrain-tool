using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Stamper)), CanEditMultipleObjects]
public class StamperEditor : Editor
{
    Stamper stamper;
    //List<Path> paths;
    Spline spline;

    //int activePath;
    const float segmentSelectDistanceThreshold = 10f;
    int selectedSegmentIndex = -1;

    Vector3 pivotPos;
    bool moveToolActive;
    int activeIndex;
    Quaternion activeRot;
    Vector3 activePosition;
    Vector3 offsets;

    int currentControl;
    //private Dictionary<int, int> ControlToNode = new Dictionary<int, int>();
   // private Dictionary<int, int> NodeToControl = new Dictionary<int, int>();
    //List<int> controlIDs = new List<int>();

    int lastControlID;
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUI.BeginChangeCheck();

        /*
        if (GUILayout.Button("Create New"))
        {
            Undo.RecordObject(creator, "Create New");
            creator.CreatePath();
            //path = creator.path;
        }
        */
        /*
        foreach(Path path in creator.paths)
        {
            if (path.isClosed != path.IsClosed)
                path.IsClosed = path.isClosed;
            path.ToggleClosed();
        }
        */


        if (GUILayout.Button("Toggle Closed"))
        {
            Undo.RecordObject(stamper, "Toggle Closed");
            stamper.spline.ToggleClosed();
        }
        /*
        if (GUILayout.Button("Manual Stamp"))
        {
            Undo.RecordObject(stamper, "Manual Stamp");
            //Undo.RecordObject(creator.terData, "Manual Stamp");
            stamper.spline.StampTerrain();
        }

        */
        if (GUILayout.Button("Manual Undo"))
        {
            //Undo.RecordObject(stamper, "Manual Undo");
            //Undo.RecordObject(creator.terData, "Manual Undo");
            stamper.spline.UndoStamp();
        }

        if (GUILayout.Button("Create Spline Follower"))
        {
            //Undo.RecordObject(stamper, "Create Spline Follower");
            //Undo.RecordObject(creator.terData, "Manual Undo");
            stamper.CreateSplineFollower();
        }


        /*
        if (GUILayout.Button("Snap to Terrain"))
        {
            Undo.RecordObject(creator, "Snap to Terrain");
            path.SnapToTerrain();
        }

        if (GUILayout.Button("Stamp Terrain"))
        {
            Debug.Log("Calculating Heightmap. This may take a while.");
            Undo.RecordObject(FindObjectOfType<Terrain>().terrainData, "Stamp Terrain");
            path.StampTerrain();
        }

        if (GUILayout.Button("Undo Last Stamp"))
        {
            Debug.Log("Restoring Heightmap. This may take a while.");
            Undo.RecordObject(FindObjectOfType<Terrain>().terrainData, "Stamp Terrain");
            path.UndoStamp();
        }

        bool autoSetControlPoints = GUILayout.Toggle(path.AutoSetControlPoints, "Auto Set Control Points");
        if(autoSetControlPoints != path.AutoSetControlPoints)
        {
            Undo.RecordObject(creator, "Toggle Auto Set Control Points");
            path.AutoSetControlPoints = autoSetControlPoints;
        }
        */
        if (EditorGUI.EndChangeCheck())
        {
            SceneView.RepaintAll();
        }

    }
    private void OnSceneGUI()
    {
        Input();
        Draw();
        MoveTool();
    }

    void Input()
    {
        Event guiEvent = Event.current;

        Vector2 mousePos = Event.current.mousePosition;
        //Vector3 creatorPos = stamper.terrain.transform.position;
        Vector3 creatorPos = stamper.transform.position;
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
        else
        {
            //Vector3 lastPointPos = path.points[path.points.Count - 1];
            //mouseWorldPos = worldRay.origin + (worldRay.direction * (Vector3.Distance(lastPointPos, worldRay.origin)));
        }

        //Handles.DrawSphere(0, mouseWorldPos, Quaternion.identity, .5f);

        //Get closest Path on Mouse Move
        if (guiEvent.type == EventType.MouseMove)
        {
            //SetCreator();
            //GetClosestPath(mouseWorldPos, creatorPos);
        }

        //On Mouse Click Split or Add segment
        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.shift)
        {
            if(selectedSegmentIndex != -1)
            {
                Undo.RecordObject(stamper, "Split segment");
                stamper.spline.SplitSegment(mouseWorldPos - creatorPos, selectedSegmentIndex);
                stamper.spline.StampTerrain();
            }
            else if(!stamper.spline.isClosed)
            {
                Undo.RecordObject(stamper, "Add segment");
                stamper.spline.AddSegment(mouseWorldPos - creatorPos);
                stamper.spline.StampTerrain();
            }
        }

        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.control)
        {
            float minDstToAnchor = 100;
            int closestAnchorIndex = -1;

            for (int i = 0; i < stamper.spline.NumPoints; i += 3)
            {
                float dst = Vector3.Distance(mouseWorldPos, stamper.spline[i] + creatorPos);
                if (dst < minDstToAnchor)
                {
                    minDstToAnchor = dst;
                    closestAnchorIndex = i;
                }
            }
            //Debug.Log(minDstToAnchor);
            if (closestAnchorIndex != -1)
            {
                Undo.RecordObject(stamper, "Delete segment");
                stamper.spline.DeleteSegment(closestAnchorIndex);
                stamper.spline.StampTerrain();
            }

        }

        float minDstToSegment = segmentSelectDistanceThreshold;
        int newselectedSegementindex = -1;
        
        if(guiEvent.type == EventType.MouseMove)
        {
            for (int i = 0; i < stamper.spline.NumSegments; i++)
            {
                Vector3[] points = stamper.spline.GetPointsInSegment(i);
                float dst = HandleUtility.DistancePointBezier(mouseWorldPos, points[0] + creatorPos, points[3] + creatorPos, points[1] + creatorPos, points[2] + creatorPos);
                if (dst < minDstToSegment)
                {
                    minDstToSegment = dst;
                    newselectedSegementindex = i;
                }
            }
            if(newselectedSegementindex != selectedSegmentIndex)
            {
                selectedSegmentIndex = newselectedSegementindex;
                HandleUtility.Repaint();
            }
        }
        if (currentControl == 0 && stamper.canStamp == true)
        {
            //Debug.Log(stamper.stamperData);
            //stamper.stamperData.SetData();

            spline.StampTerrain();
            activeRot = CalculateRot(activePosition);
            stamper.canStamp = false;

        }

        HandleUtility.AddDefaultControl(0);
        currentControl = GUIUtility.hotControl;
        //Debug.Log(GUIUtility.hotControl);

    }
    /*
    void GetClosestPath(Vector3 mouseWorldPos, Vector3 creatorPos)
    {
        int pathIndex = 0;
        float minDist = Mathf.Infinity;
        foreach (Path path in creator.maker.paths)
        {
            for(int i = 0; i < path.points.Count -1; i++)
            {
                float dst = Vector3.Distance(mouseWorldPos, path.points[i]);
                if (dst < minDist)
                {
                    activePath = pathIndex;
                    minDist = dst;
                }
            }

            pathIndex++;
        }
    }
    */
    void Draw()
    {
        Vector3 terrainPos = stamper.terrain.transform.position;
        offsets = stamper.transform.position;
        for (int i = 0; i < spline.NumSegments; i++)
        {
            Vector3[] points = spline.GetPointsInSegment(i);
            Handles.color = Color.black;
            //Handles.DrawLine(creatorPos + points[1], creatorPos + points[0]);
            //Handles.DrawLine(creatorPos + points[2], creatorPos + points[3]);
            Color segmentColor;

            segmentColor = (i == selectedSegmentIndex && Event.current.shift) ? Color.green : Color.white;
            Handles.DrawBezier(offsets + points[0], offsets + points[3], offsets + points[1], offsets + points[2], segmentColor, null, 3);
        }

        Handles.color = Color.green;
        //int _currentHandle = 
        //_currentHandle = (EditorGUIUtility.hotControl != 0) ? EditorGUIUtility.hotControl : _currentHandle;
        //Debug.Log(GUIUtility.hotControl);
        //Debug.Log(selectedHandleControlId);

        for (int i = 0; i < spline.NumPoints; i++)
        {
            if (i % 3 == 0)
            {
                //Vector3 newPdos = Handles.FreeMoveHandle(creatorPos + spline[i], Quaternion.identity, 1.5f, Vector3.zero, Handles.SphereHandleCap);
                Vector3 newPos;
                Vector3 newPosOverride;
                bool overrideHandle = false;
                int currentControlID;

                //Vector3 pos = terrainPos + spline[i] + (stamper.transform.position - pivotPos);
                Vector3 pos = spline[i] + offsets;
                //Debug.Log(pivotPos);
                //Quaternion rot = Quaternion.identity;
                //Vector3 scale = new Vector3(1, 1, 1);

                //Quaternion rot = (i != spline.NumPoints - 1) ? Quaternion.LookRotation(spline[i + 3] - spline[i]) : Quaternion.LookRotation(spline[i - 3] - spline[i]);
                //newPos = Handles.PositionHandle(pos, Quaternion.identity);

                if(activePosition != pos)
                {
                    /*
                    if (Handles.Button(pos, Quaternion.identity, 2, 2, Handles.SphereHandleCap))
                    {
                        //Quaternion rot = (i != spline.NumPoints - 1) ? Quaternion.LookRotation(spline[i + 3] - spline[i]) : Quaternion.LookRotation(spline[i - 3] - spline[i]);
                        SetActiveOptions(i, pos, CalculateRot(pos));
                        moveToolActive = true;
                        //newPos = Handles.FreeMoveHandle(pos, Quaternion.identity, 1.5f, Vector3.zero, Handles.SphereHandleCap);
                    }
                    */
                    Texture2D tex = EditorGUIUtility.Load("SplineStamper/Icons/MoveIcon.png") as Texture2D;
                    GUIStyle style = new GUIStyle();
                    style.normal.background = tex;
                    Handles.BeginGUI();
                    //Vector3 pos = s.transform.position;
                    Vector2 pos2D = HandleUtility.WorldToGUIPoint(pos);
                    float distance = HandleUtility.GetHandleSize(pos);
                    float size = 500 / distance;
                    if (GUI.Button(new Rect(pos2D.x - (size / 2), pos2D.y - (size / 2), size, size), "", style))
                    {
                        SetActiveOptions(i, pos, CalculateRot(pos));
                        moveToolActive = true;
                    }
                    Handles.EndGUI();



                }

                //MovePoints(i, newPos, offsets);
                //bool testButton = Handles.Button(creatorPos + spline[i], Quaternion.identity, 1.5f, 1.5f, Handles.SphereHandleCap);
                /*
                newPos = Handles.FreeMoveHandle(creatorPos + spline[i], Quaternion.identity, 1.5f, Vector3.zero,
                    (controlID, position, rotation, size, eventType) =>
                    {
                        if (controlID == lastControlID)
                        {
                            currentControlID = controlID;
                            overrideHandle = true;
                            Handles.color = Color.green;
                            //Handles.SphereHandleCap(controlID, position, rotation, size, eventType);
                            //newPosOverride = Handles.PositionHandle(creatorPos + spline[i], Quaternion.identity);
                            
                        }

                        else
                        {
                            currentControlID = controlID;
                            overrideHandle = false;
                            Handles.color = Color.white;
                            Handles.SphereHandleCap(controlID, position, rotation, size, eventType);
                            //newPosOverride = Vector3.zero;
                        }
                    });

                //Remembers the selected Handle and avoids it being deselected unless new one is clicked
                if (GUIUtility.hotControl != 0)
                    lastControlID = GUIUtility.hotControl;

                if(overrideHandle == true)
                    newPos = Handles.PositionHandle(creatorPos + spline[i], Quaternion.identity);

                Debug.Log(overrideHandle);
                */
                //Debug.Log(newPos + " + " + (newPos + creatorPos));
                //Debug.Log(newPosOverride);
                //Debug.Log(lastControlID);

                /*
                if (overrideHandle == true)
                    newPos = Handles.PositionHandle(creatorPos + spline[i], Quaternion.identity);
                */
                //Debug.Log(lastControlID);
                /*
                Vector3 newPos;
                if (i == 0)
                    newPos = Handles.PositionHandle(creatorPos + spline[i], Quaternion.identity);
                else newPos = Handles.FreeMoveHandle(creatorPos + spline[i], Quaternion.identity, 1.5f, Vector3.zero, Handles.SphereHandleCap);
                */
                //Vector3 newPos = Handles.PositionHandle(creatorPos + path[i], Quaternion.identity);
                //Check if position is changed

                //MovePoints(i, newPos, offsets);
            }
        }

    }

    Quaternion CalculateRot(Vector3 pos)
    {
        stamper.UpdatePoints();
        int closestPoint = GetClosestEvenPoint(stamper.evenPoints, pos);
        Vector3 forward;
        if (closestPoint != stamper.evenPoints.Length -1)
            forward = (stamper.evenPoints[closestPoint + 1] - stamper.transform.position) - (stamper.evenPoints[closestPoint] - stamper.transform.position);
        else forward = (stamper.evenPoints[closestPoint] - stamper.transform.position) - (stamper.evenPoints[closestPoint -1] - stamper.transform.position);

        Quaternion rot = Quaternion.LookRotation(forward);
        return rot;
    }
    void SetActiveOptions(int i, Vector3 pos, Quaternion rot)
    {
        activeIndex = i;
        activePosition = pos;
        activeRot = rot;
    }

    void MoveTool()
    {
        if(moveToolActive == true)
        {
            Vector3 pos = spline[activeIndex] + offsets;
            Vector3 newPos = Handles.PositionHandle(pos, activeRot);
            activePosition = newPos;
            MovePoints(activeIndex, newPos, offsets);
        }
            
    }

    void MovePoints(int i, Vector3 newPos, Vector3 offsets)
    {
        if (spline[i] != newPos - offsets)
        {
            Undo.RecordObject(stamper, "Move Point");
            spline.MovePoints(i, newPos - offsets);
            stamper.canStamp = true;
        }
    }

    int GetClosestEvenPoint(Vector3[] points, Vector3 handlePos)
    {
        int closestIndex = 1000000;
        float minDist = Mathf.Infinity;
        Vector3 currentPos = handlePos;

        for (int i = 0; i < points.Length; i ++)
        {
            float dist = Vector3.Distance(points[i], currentPos);
            if (dist < minDist)
            {
                closestIndex = i;
                minDist = dist;
            }
        }
        return closestIndex;
    }

    void HandleFunc(int controlID, Vector3 position, Quaternion rotation, float size)
    {
        if (controlID == GUIUtility.hotControl)
            GUI.color = Color.red;
        else
            GUI.color = Color.green;
        Handles.Label(position, "BlaBLa");
        Handles.FreeMoveHandle(position, rotation, size, Vector3.zero, Handles.SphereHandleCap);
        //Handles.SphereHandleCap(controlID, position, rotation, size, EventType.Used);
        GUI.color = Color.white;
    }

    void OnEnable()
    {
        SetCreator();
        moveToolActive = false;
        //SetPivot();
    }

    void SetCreator()
    {
        stamper = (Stamper)target;
        //stamper.stamperData = stamper.scriptable.stampDataList[0];
        spline = stamper.spline;
        //spline.StampTerrain();
    }

}