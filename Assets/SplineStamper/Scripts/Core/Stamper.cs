using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
public class Stamper : MonoBehaviour
{
    [Header("Path Settings")]
    [Header("Ctrl + Left click - Delete Point")]
    [Header("Shift + Left click on Curve - Insert Point")]
    [Header("Shift + Left Click - Add Point")]

    [Header("Controls")]
    [Range(1f, 40f)] [Tooltip("Width of the generated path")]
    public float pathWidth = 15;
    [Range(0, 10)]
    [Tooltip("Which terrain layer the stamper will use")]
    public int textureLayer = 0;
    [Range(0, 1)]
    [HideInInspector]
    public float textureOpacity = 0.8f;

    [Header("Falloff Settings")]
    [Range(0f, 10f)] [SerializeField] [Tooltip("Falloff Size of the generated path")]
    public float falloffDistance = 7f;
    [Range(0, 10)]
    [Tooltip("Which terrain layer the falloff will use")]
    public int falloffTextureLayer = 0;
    [Range(0, 1)]
    [HideInInspector]
    public float falloffTextureOpacity = 0.8f;


    [Header("Advanced")] 
    [Range(.2f, 15f)] [Tooltip("Spacing between each even point. Used for Mesh Creator. This will increase precision and mesh polygon count")]
    public float spacing = 12f;

    [Header("Debug")]
    public bool showSpacingGizmo = false;

    [HideInInspector] public Terrain terrain;
    [HideInInspector] public TerrainData terData;
    [HideInInspector] public Spline spline;

    List<Vector3> debugPoints = new List<Vector3>();
    [HideInInspector]
    public SplineStamper splineStamper;

    Vector3 stamperPosition;

    [HideInInspector]
    public Vector3[] evenPoints;
    int tRes;
    int xBase;
    int yBase;
    int mapX;
    int mapY;
    int layerCount;

    [HideInInspector]
    public bool stampActive;
    [HideInInspector]
    public int indexInData;
    [HideInInspector]
    public bool canStamp;

    public void CreatePath()
    {
        spline = new Spline(this);
        //SetPivot();
        spline.AutoSetControlPoints = true;
        UpdatePoints();
    }

    public void UpdatePoints()
    {
        evenPoints = spline.CalculateEvenlySpacedPoints(spacing, 1);
    }


    void SetPivot()
    {
        Vector3 pos = Vector3.zero;
        int pointsCount = 0;

        for (int i = 0; i < spline.NumPoints; i++)
        {
            if (i % 3 == 0)
            {
                pos += spline.points[i];
                pointsCount += 1;
            }
        }
        transform.position = pos / pointsCount;
    }

    private void Reset()
    {
        CreatePath();
    }

    public void CalculateTerrain()
    {
        if(terrain == null)
            terrain = FindObjectOfType<Terrain>();
        if(terData == null)
            terData = terrain.terrainData;

        xBase = 0;
        yBase = 0;
        tRes = terData.heightmapResolution;
        mapX = terData.alphamapWidth;
        mapY = terData.alphamapHeight;
        layerCount = terData.alphamapLayers;
        stamperPosition = terrain.transform.position;
        textureLayer = Mathf.Clamp(textureLayer, 0, layerCount -1);
    }

    public float GetHeight(Vector3 pos)
    {
        float height = terrain.SampleHeight(pos + terrain.transform.position);
        return height;
    }


    [ContextMenu("Stamp")]
    public void ContextStamp()
    {
        spline.StampTerrain();
    }
    public void SetHeight(Vector3[] points, float roadWidth)
    {
        debugPoints.Clear();
        CalculateTerrain();

        AdjustSplineFollowers();
        if (stampActive == true)
        {
            ManualUndo();
        }

        float[,] heights = terData.GetHeights(xBase, yBase, tRes, tRes);
        float[,,] splats = terData.GetAlphamaps(xBase, yBase, mapX, mapY);

        float[,] startHeights = terData.GetHeights(xBase, yBase, tRes, tRes);
        float [,,] startSplats = terData.GetAlphamaps(xBase, yBase, mapX, mapY);

        float stepsPerPixel = 0.006f;
        float mainSteps = stepsPerPixel * terData.heightmapResolution * roadWidth;

        float maxSteps = stepsPerPixel * terData.heightmapResolution * (roadWidth + falloffDistance);
        float falloffSteps = maxSteps - mainSteps;


        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 forward = (points[i + 1] - terrain.transform.position) - (points[i] - terrain.transform.position);
            forward.Normalize();

            Vector3 left = new Vector3(-forward.z, 0, forward.x);
            float stepIndex = 0;
            Vector3 leftFalloff = left * falloffDistance;
            Vector3 rightFalloff = -left * falloffDistance;
            
            Vector3 farLeft = ((points[i] - terrain.transform.position) + left * roadWidth * 0.5f);
            Vector3 farRight = ((points[i] - terrain.transform.position) - left * roadWidth * 0.5f);

            Vector3 farLeftFalloff = farLeft + leftFalloff;
            Vector3 farRightFalloff = farRight + rightFalloff;

            float heightFalloffLeft = GetHeight(farLeftFalloff);

            float heightFalloffRight = GetHeight(farRightFalloff);

            float heightLerp = 0;
            float sideIndex = -1;

            for (int s = 0; s <= maxSteps; s++)
            {
                float step = stepIndex / maxSteps;
                Vector3 current = Vector3.Lerp(farLeftFalloff, farRightFalloff, step);

                //X Terrain coordinates are equal to the usual Z coordinates
                float targetX = current.z / terData.size.z * tRes;
                //Y Terrain coordinates are equal to the usual X coordinates
                float targetY = current.x / terData.size.x * tRes;
                float height = current.y;

                if (sideIndex == -1)
                    height = Mathf.Lerp(heightFalloffLeft, current.y, heightLerp);
                else if(sideIndex == 1)
                    height = Mathf.Lerp(heightFalloffRight, current.y, heightLerp);

                float targetHeight = height / terData.size.y;
                heights[(int)targetX, (int)targetY] = targetHeight;
                stepIndex += 1;

                int targetTextureX = (int)((current.x / terData.size.x) * terData.alphamapWidth);
                int targetTextureY = (int)((current.z / terData.size.z) * terData.alphamapHeight);

                float initialSplatFalloff = 0;
                float falloffPercent = 0;

                float initialSplatPath = 0;
                float pathPercent = 0;

                if (heightLerp > -0.92f && heightLerp < 0.92f)
                {
                    initialSplatFalloff = splats[(int)targetTextureY, (int)targetTextureX, falloffTextureLayer];
                    initialSplatFalloff = Mathf.Clamp(initialSplatFalloff, 0, 1);
                    if (initialSplatFalloff > 0)
                    {
                        falloffPercent = 100 * ((falloffTextureOpacity - initialSplatFalloff) / initialSplatFalloff);

                    }
                    else falloffPercent = falloffTextureOpacity;
                }
                else
                {
                    initialSplatPath = splats[(int)targetTextureY, (int)targetTextureX, textureLayer];
                    initialSplatPath = Mathf.Clamp(initialSplatFalloff, 0, 1);
                    if (initialSplatPath > 0)
                    {
                        pathPercent = 100 * ((textureOpacity - initialSplatPath) / initialSplatPath);
                    }
                    else pathPercent = textureOpacity;
                }

                for (int a = 0; a < terData.alphamapLayers; a++)
                {
                    if (heightLerp > -0.85f && heightLerp < 0.85f)
                    {
                        if(falloffTextureLayer != textureLayer)
                        {
                            if (a == falloffTextureLayer)
                                splats[(int)targetTextureY, (int)targetTextureX, a] = 1;
                            else
                            {
                                float currentValue = splats[(int)targetTextureY, (int)targetTextureX, a];
                                float difference = (currentValue / 100) * falloffPercent;
                                float target = (falloffTextureOpacity > initialSplatFalloff) ? currentValue - difference : currentValue + difference;
                                target = Mathf.Clamp(target, 0, 1);
                                splats[(int)targetTextureY, (int)targetTextureX, a] = 0;
                            }
                        }
                        
                    }
                    else
                    {
                        if (a == textureLayer)
                            splats[(int)targetTextureY, (int)targetTextureX, a] = 1;
                        else
                        {
                            float currentValue = splats[(int)targetTextureY, (int)targetTextureX, a];
                            float difference = (currentValue / 100) * pathPercent;
                            float target = (textureOpacity > initialSplatPath) ? currentValue - difference : currentValue + difference;
                            target = Mathf.Clamp(target, 0, 1);
                            splats[(int)targetTextureY, (int)targetTextureX, a] = 0;
                        } 
                    }
                }

                //Heightlerp Time
                if (s < falloffSteps)
                {
                    sideIndex = -1;
                    heightLerp += 1 / falloffSteps;
                }

                else if (s > (maxSteps - falloffSteps))
                {
                    sideIndex = 1;
                    heightLerp -= 1 / falloffSteps;
                }
            }
        }

        //Apply Changes to the Terrain
        terData.SetHeightsDelayLOD(xBase, yBase, heights);
        terData.SetAlphamaps(xBase, yBase, splats);
        terData.SyncHeightmap();

        //Save Data and Undo State
        splineStamper.scriptablePath.stampDataList[indexInData].flatUndoHeights = SaveHeight(heights, startHeights);
        splineStamper.scriptablePath.stampDataList[indexInData].flatUndoSplats = SaveSplats(startSplats, splats);
        stampActive = true;
    }

    void AdjustSplineFollowers()
    {
        foreach(SplineFollower follower in GetComponentsInChildren<SplineFollower>())
        {
            follower.AdjustPosition();
        }
    }

    public void CreateSplineFollower()
    {
        GameObject temp = new GameObject("SplineFollower");
        temp.transform.parent = gameObject.transform;
        temp.transform.localPosition = Vector3.zero;
        temp.AddComponent<SplineFollower>().AdjustPosition();
    }

    public float[] SaveHeight(float[,] endHeights, float[,] startHeights)
    {
        float[] heights = new float[tRes * tRes];
        for (int x = 0; x < tRes; x++)
        {
            for (int y = 0; y < tRes; y++)
            {
                int flatIndex = Coordinates2DToFlatIndex(new Vector2Int(x, y));
                float height = endHeights[x, y] - startHeights[x, y];
                heights[flatIndex] = height;
            }
        }
        return heights;
    }

    //Saves 3D Array as 1D and Calculates the Undo
    public float[] SaveSplats(float[,,] start, float[,,] end)
    {

        float[] splat = new float[mapX * mapY * layerCount];

        for (int x = 0; x < mapX; x++)
        {
            for (int y = 0; y < mapY; y++)
            {
                for (int z = 0; z < layerCount; z++)
                {
                    int flatIndex = Coordinates3DToFlatIndex(new Vector3Int(x, y, z));
                    splat[flatIndex] = end[x, y, z] - start[x, y, z];
                }
            }
        }
        return splat;
    }

    //2D to 1D Converter
    private int Coordinates2DToFlatIndex(Vector2Int cellIndex)
    {
        return tRes * cellIndex.x + cellIndex.y;
    }

    //1D to 2D Converter
    private Vector2Int FlatIndexToCoordinates2D(int flatIndex, int tRes)
    {
        Vector2Int coordinates = new Vector2Int
        {
            x = flatIndex / tRes,
            y = flatIndex % tRes
        };
        return coordinates;
    }

    //3D to 1D Converter
    private int Coordinates3DToFlatIndex(Vector3Int cellIndex)
    {
        return (cellIndex.z * mapX * mapY) + (cellIndex.y * mapX) + cellIndex.x;
    }

    //1D to 3D converter
    private Vector3Int FlatIndexToCoordinates3D(int flatIndex)
    {
        int tempZ = flatIndex / (mapX * mapY);
        flatIndex -= (tempZ * mapX * mapX);
        int tempY = flatIndex / mapX;
        int tempX = flatIndex % mapX;
        Vector3Int coordinates = new Vector3Int
        {
            z = tempZ,
            y = tempY,
            x = tempX
        };
        return coordinates;
    }

    private Vector3Int LoopFlatIndexToCoordinates3D(int flatIndex)
    {
        int tempZ = flatIndex / (mapX * mapY);
        flatIndex -= (tempZ * mapX * mapX);
        int tempY = flatIndex / mapX;
        int tempX = flatIndex % mapX;
        Vector3Int coordinates = new Vector3Int
        {
            z = tempZ,
            y = tempY,
            x = tempX
        };
        return coordinates;
    }

    public void ManualUndo()
    {
        if (stampActive == true)
        {
#if UNITY_EDITOR
            //Debug.Log("Set Dirty");
            UnityEditor.EditorUtility.SetDirty(splineStamper.scriptablePath);
#endif
            UndoSplats();
            UndoHeights();
        }
    }

    void UndoHeights()
    {
        float[,] currentHeights = new float[tRes, tRes];
        currentHeights = terData.GetHeights(xBase, yBase, tRes, tRes);

        for (int i = 0; i < splineStamper.scriptablePath.stampDataList[indexInData].flatUndoHeights.Length; i++)
        {
            float undoHeight = splineStamper.scriptablePath.stampDataList[indexInData].flatUndoHeights[i];
            if (undoHeight != 0)
            {
                Vector2Int cellCoordinates = FlatIndexToCoordinates2D(i, tRes);
                float currentHeight = currentHeights[cellCoordinates.x, cellCoordinates.y];
                float height = currentHeight - undoHeight;
                height = Mathf.Clamp(height, 0, 1000000000);
                currentHeights[cellCoordinates.x, cellCoordinates.y] = height;
            }
        }

        terData.SetHeightsDelayLOD(xBase, yBase, currentHeights);
        terData.SyncHeightmap();
        stampActive = false;
    }


    void UndoSplats()
    {
        //Undo Splatmaps
        float[,,] targetSplats = new float[mapX, mapY, layerCount];
        targetSplats = terData.GetAlphamaps(xBase, yBase, mapX, mapY);
        for (int i = 0; i < splineStamper.scriptablePath.stampDataList[indexInData].flatUndoSplats.Length; i++)
        {
            float undoSplats = splineStamper.scriptablePath.stampDataList[indexInData].flatUndoSplats[i];
            if (undoSplats != 0)
            {
                Vector3Int cellCoordinates = FlatIndexToCoordinates3D(i);
                float splatValue = targetSplats[cellCoordinates.x, cellCoordinates.y, cellCoordinates.z] - splineStamper.scriptablePath.stampDataList[indexInData].flatUndoSplats[i];
                targetSplats[cellCoordinates.x, cellCoordinates.y, cellCoordinates.z] = splatValue;
            }
        }

        terData.SetAlphamaps(xBase, yBase, targetSplats);
    }

    private void OnValidate()
    {
        canStamp = true;
    }


    private void OnDestroy()
    {
        ManualUndo();
    }

    private void OnDrawGizmos()
    {
        if (showSpacingGizmo)
        {
                for (int i = 0; i < evenPoints.Length - 1; i++)
                {
                    Gizmos.DrawSphere(evenPoints[i], 0.4f);
                    Vector3 forward = evenPoints[i + 1] - evenPoints[i];
                    forward.Normalize();
                    Vector3 left = new Vector3(-forward.z, 0, forward.x);
                    left *= pathWidth * 0.5f;
                    Gizmos.DrawRay(evenPoints[i], forward);
                    Gizmos.DrawRay(evenPoints[i], left);
                    Gizmos.DrawRay(evenPoints[i], -left);
                }
        }
    }
}
