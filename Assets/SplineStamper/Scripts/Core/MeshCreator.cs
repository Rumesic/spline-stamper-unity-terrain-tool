
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[RequireComponent(typeof(Stamper))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshCreator : MonoBehaviour
{

    [Range(0, 15f)]
    [SerializeField] float extraWidth = 0;
    [Range(0, 45f)]
    [SerializeField] float heightOffset = 0.1f;


    float spacing = 1;
    float roadWidth = 1;

    public bool autoUpdate = true;
    float tiling = 1;
    Vector3[] evenPoints;
    Mesh currentMesh;

    public void UpdateRoad()
    {
        Stamper stamper = GetComponent<Stamper>();
        Spline spline = stamper.spline;
        roadWidth = stamper.pathWidth + extraWidth;
        spacing = stamper.spacing;
        evenPoints = spline.CalculateEvenlySpacedPoints(spacing, heightOffset, true);
        GetComponent<MeshFilter>().mesh = CreateRoadMesh(evenPoints, spline.isClosed);

        int textureRepeat = Mathf.RoundToInt(tiling * evenPoints.Length * spacing * .05f);
    }

    Mesh CreateRoadMesh(Vector3[] points, bool isClosed)
    {
        Vector3[] verts = new Vector3[points.Length * 2];
        Vector2[] uvs = new Vector2[verts.Length];
        int numTris = 2 * (points.Length - 1) + ((isClosed) ? 2 : 0);
        int[] tris = new int[numTris * 3];
        int vertIndex = 0;
        int triIndex = 0;

        for (int i = 0; i < points.Length; i++)
        {
            Vector3 forward = Vector3.zero;
            if (i < points.Length - 1 || isClosed)
            {
                forward += points[(i + 1) % points.Length] - points[i];
            }
            if (i > 0 || isClosed)
            {
                forward += points[i] - points[(i - 1 + points.Length) % points.Length];
            }

            forward.Normalize();

            Vector3 left = new Vector3(-forward.z, 0, forward.x);
            verts[vertIndex] = points[i] - transform.position + left * roadWidth * .5f;
            verts[vertIndex + 1] = points[i] - transform.position - left * roadWidth * .5f;

            float completionPercent = i / (float)(points.Length - 1);
            float v = 1 - Mathf.Abs(2 * completionPercent - 1);
            uvs[vertIndex] = new Vector3(0, v);
            uvs[vertIndex + 1] = new Vector3(1, v);

            if (i < points.Length - 1 || isClosed)
            {
                tris[triIndex] = vertIndex;
                tris[triIndex + 1] = (vertIndex + 2) % verts.Length;
                tris[triIndex + 2] = vertIndex + 1;

                tris[triIndex + 3] = vertIndex + 1;
                tris[triIndex + 4] = (vertIndex + 2) % verts.Length;
                tris[triIndex + 5] = (vertIndex + 3) % verts.Length;
            }

            vertIndex += 2;
            triIndex += 6;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;

        currentMesh = mesh;
        return mesh;
    }

    public void SaveMesh()
    {
        AssetDatabase.CreateAsset(currentMesh, "Assets/GeneratedMesh.asset");
        AssetDatabase.SaveAssets();
    }

}