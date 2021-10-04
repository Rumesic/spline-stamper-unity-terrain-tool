using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using UnityEditor;

[System.Serializable]
public class Spline 
{
    [SerializeField, HideInInspector]
    public List<Vector3> points;


    public bool isClosed;

    [SerializeField, HideInInspector]
    bool autoSetControlPoints;
    public Stamper stamper;

    public bool toSnap;

    //Constructor
    public Spline(Stamper s)
    {
        stamper = s;
        stamper.CalculateTerrain();
        Vector3 terSize = stamper.terData.size;
        points = new List<Vector3>
        {
            Vector3.left * 20,
            (Vector3.left+Vector3.forward) * 5,
            (Vector3.right + Vector3.back) * 5,
            (Vector3.right * 20),
        };
    }

    public Vector3 this[int i]
    {
        get
        {
            return points[i];
        }
    }

    public bool AutoSetControlPoints
    {
        get
        {
            return autoSetControlPoints;
        }
        set
        {
            if(autoSetControlPoints != value)
            {
                autoSetControlPoints = value;
                if(autoSetControlPoints)
                {
                    AutoSetAllControlPoints();
                }
            }
        }
    }

    public int NumPoints
    {
        get
        {
            return points.Count;
        }
    }

    public int NumSegments
    {
        get
        {
            return (points.Count) / 3;
        }
    }

    public void AddSegment(Vector3 anchorPos)
    {
        Vector3 lastPoint = points[points.Count - 1];
        Vector3 lastControl = points[points.Count - 2];
        Vector3 firstControl = points[points.Count - 3];
        Vector3 firstPoint = points[points.Count - 4];

        Vector3 newControlPoint = lastPoint * 2 - lastControl;

        points.Add(newControlPoint);
        points.Add((newControlPoint + anchorPos) / 2);
        points.Add(anchorPos);
        if(autoSetControlPoints)
        {
            AutoSetAllAffectedControlPoints(points.Count - 1);
        }
    }

    public void SplitSegment(Vector3 anchorPos, int segmentIndex)
    {
        points.InsertRange(segmentIndex * 3 + 2, new Vector3[] { Vector3.zero, anchorPos, Vector3.zero });
        if (autoSetControlPoints)
        {
            AutoSetAllAffectedControlPoints(segmentIndex * 3 + 3);
        }
        else
        {
            AutoSetAnchorControlPoints(segmentIndex * 3 + 3);
        }

    }

    public void DeleteSegment(int anchorIndex)
    {
        if (NumSegments > 2 || !isClosed && NumSegments > 1)
        {
            if (anchorIndex == 0)
            {
                if (isClosed)
                {
                    points[points.Count - 1] = points[2];
                }
                points.RemoveRange(0, 3);
            }
            else if (anchorIndex == points.Count - 1 && !isClosed)
            {
                points.RemoveRange(anchorIndex - 2, 3);
            }
            else
            {
                points.RemoveRange(anchorIndex - 1, 3);
            }
        }
    }

    public Vector3[] GetPointsInSegment(int i)
    {
        return new Vector3[]
        {
            points[i*3],
            points[i*3+1],
            points[i*3+2],
            points[LoopIndex(i*3+3)]
        };
    }

    public void MovePoints(int i, Vector3 pos)
    {
        Vector3 deltaMove = pos - points[i];
        if(i % 3 == 0 || !autoSetControlPoints)
        {
            points[i] = pos;


            if (toSnap)
            {
                float heightOffset = 0.05f;
                points[i] = new Vector3(pos.x, stamper.GetHeight(pos) + heightOffset, pos.z);
            }

            else points[i] = pos;

            if (autoSetControlPoints)
            {
                AutoSetAllAffectedControlPoints(i);
            }

            else
            {
                //MoveAnchorPointBehaviour
                if (i % 3 == 0)
                {
                    if (i + 1 < points.Count || isClosed)
                    {
                        points[LoopIndex(i + 1)] += deltaMove;
                    }
                    if (i - 1 >= 0 || isClosed)
                    {
                        points[LoopIndex(i - 1)] += deltaMove;
                    }
                }

                //if its not an Anchor points
                else
                {
                    bool nextPointIsAnchor = (i + 1) % 3 == 0;

                    //Ternary Operator
                    int correspondingControlIndex = (nextPointIsAnchor) ? i + 2 : i - 2;
                    int anchorIndex = (nextPointIsAnchor) ? i + 1 : i - 1;

                    //Check if CorrespondingControlIndex exists
                    if (correspondingControlIndex >= 0 && correspondingControlIndex < points.Count || isClosed)
                    {
                        float dst = (points[LoopIndex(anchorIndex)] - points[LoopIndex(correspondingControlIndex)]).magnitude;
                        Vector3 dir = (points[LoopIndex(anchorIndex)] - pos).normalized;
                        points[LoopIndex(correspondingControlIndex)] = points[LoopIndex(anchorIndex)] + dir * dst;
                    }
                }
            }
        }
    }

    public void ToggleClosed()
    {
        isClosed = !isClosed;
        if (isClosed)
        {
            points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);
            points.Add(points[0] * 2 - points[1]);
            if (autoSetControlPoints)
            {
                AutoSetAnchorControlPoints(0);
                AutoSetAnchorControlPoints(points.Count - 3);
            }
        }

        else
        {
            points.RemoveRange(points.Count - 2, 2);
            if (autoSetControlPoints)
            {
                AutoSetStartAndEndControls();
            }
        }
    }

    public Vector3[] CalculateEvenlySpacedPoints(float spacing, float heightOffset, bool changeHeight = false, float resolution = 1)
    {
        List<Vector3> evenlySpacedPoints = new List<Vector3>();
        Vector3 firstPoint = points[0];
        firstPoint = ClampPoint(firstPoint + stamper.transform.position, changeHeight, heightOffset);
        evenlySpacedPoints.Add(firstPoint);
        Vector3 previousPoint = firstPoint;
        float dstSinceLastEvenPoint = 0;

        for (int segmentIndex = 0; segmentIndex < NumSegments; segmentIndex++)
        {
            Vector3[] p = GetPointsInSegment(segmentIndex);
            float controlNetLength = Vector3.Distance(p[0], p[1]) + Vector3.Distance(p[1], p[2]) + Vector3.Distance(p[2], p[3]);
            float estimatedCurveLength = Vector3.Distance(p[0], p[3]) + controlNetLength / 2f;
            int divisions = Mathf.CeilToInt(estimatedCurveLength * resolution * 10);
            float t = 0;
            while (t <= 1)
            {
                t += 1f / divisions;
                Vector3 pointOnCurve = Bezier.EvaluateCubic(p[0] + stamper.transform.position, p[1] + stamper.transform.position, p[2] + stamper.transform.position, p[3] + stamper.transform.position, t);
                dstSinceLastEvenPoint += Vector3.Distance(previousPoint, pointOnCurve);

                while (dstSinceLastEvenPoint >= spacing)
                {
                    float overshootDst = dstSinceLastEvenPoint - spacing;
                    Vector3 newEvenlySpacedPoint = pointOnCurve + (previousPoint - pointOnCurve).normalized * overshootDst;
                    newEvenlySpacedPoint = ClampPoint(newEvenlySpacedPoint, changeHeight, heightOffset);

                    evenlySpacedPoints.Add(newEvenlySpacedPoint);
                    dstSinceLastEvenPoint = overshootDst;
                    previousPoint = newEvenlySpacedPoint;
                }

                previousPoint = pointOnCurve;
            }
        }
        return evenlySpacedPoints.ToArray();
    }

    Vector3 ClampPoint(Vector3 toClamp, bool changeHeight = false, float heightOffset = 0)
    {
        float offset = 0;
        float pathSize = stamper.pathWidth + stamper.falloffDistance + offset;
        float clampedX = Mathf.Clamp(toClamp.x, pathSize, stamper.terData.size.x - pathSize + stamper.terrain.transform.position.x);
        float clampedY = Mathf.Clamp(toClamp.y, stamper.terrain.transform.position.y, stamper.terrain.transform.position.y + stamper.terData.size.y);
        float clampedZ = Mathf.Clamp(toClamp.z, pathSize, stamper.terData.size.z - pathSize + stamper.terrain.transform.position.z);
        Vector3 clampedPoint;
        if (changeHeight)
            clampedPoint = new Vector3(clampedX, stamper.GetHeight(toClamp) + heightOffset, clampedZ);
        else clampedPoint = new Vector3(clampedX, clampedY, clampedZ);
        return clampedPoint;
    }

    public void SnapToTerrain()
    {
        toSnap = !toSnap;
        float heightOffset = 0.15f;


        for (int i = 0; i < points.Count - 1; i++)
        {
            if (i % 3 == 0)
            {
                Vector3 pos = points[i];
                points[i] = new Vector3(pos.x, stamper.GetHeight(pos) + heightOffset, pos.z);

                if (autoSetControlPoints)
                {
                    AutoSetAllAffectedControlPoints(i);
                }
            }
        }

    }

    public void StampTerrain()
    {
        float spacingPerPixel = 0.000002f;

        float spacingPerSize = 0.00015f;
        float stampSpacing = spacingPerPixel * stamper.terData.heightmapResolution * stamper.pathWidth;
        //float stampSpacing = spacingPerPixel * (stamper.terData.heightmapResolution * stamper.terData.heightmapResolution);
        stamper.evenPoints = CalculateEvenlySpacedPoints(spacingPerSize * ((stamper.terData.size.x + stamper.terData.size.z) / 2), 0);
        stamper.SetHeight(stamper.evenPoints, stamper.pathWidth);
    }

    public void UndoStamp()
    {
        stamper.ManualUndo();
    }

    void AutoSetAllAffectedControlPoints(int updatedAnchorIndex)
    {
        if (NumSegments < 2)
            return;
        for (int i = updatedAnchorIndex - 3; i <= updatedAnchorIndex + 3; i += 3)
        {
            if (i >= 0 && i < points.Count || isClosed)
            {
                AutoSetAnchorControlPoints(LoopIndex(i));
            }
        }
        AutoSetStartAndEndControls();
    }

    void AutoSetAllControlPoints()
    {
        if (NumSegments < 2)
            return;
        for (int i = 0; i < points.Count; i += 3)
        {
            AutoSetAnchorControlPoints(i);
        }
        AutoSetStartAndEndControls();
    }

    void AutoSetAnchorControlPoints(int anchorIndex)
    {
        if (NumSegments < 2)
            return;
        Vector3 anchorPos = points[anchorIndex];
        Vector3 dir = Vector3.zero;
        float[] neighbourDistances = new float[2];

        if (anchorIndex - 3 >= 0 || isClosed)
        {
            Vector3 offset = points[LoopIndex(anchorIndex - 3)] - anchorPos;
            dir += offset.normalized;
            neighbourDistances[0] = offset.magnitude;
        }
		if (anchorIndex + 3 >= 0 || isClosed)
		{
			Vector3 offset = points[LoopIndex(anchorIndex + 3)] - anchorPos;
			dir -= offset.normalized;
			neighbourDistances[1] = -offset.magnitude;
		}

        dir.Normalize();

        for (int i = 0; i < 2; i++)
        {
            int controlIndex = anchorIndex + i * 2 - 1;
            if (controlIndex >= 0 && controlIndex < points.Count || isClosed)
            {
                points[LoopIndex(controlIndex)] = anchorPos + dir * neighbourDistances[i] * .5f;
            }
        }
    }

    void AutoSetStartAndEndControls()
    {
        if (NumSegments < 2)
            return;
        if (!isClosed)
        {
            points[1] = (points[0] + points[2]) * .5f;
            points[points.Count - 2] = (points[points.Count - 1] + points[points.Count - 3]) * .5f;
        }
    }

    int LoopIndex(int i )
    {
        return (i + points.Count) % points.Count;
    }

}
