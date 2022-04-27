/****************************************************************************
 * Copyright ©2021 Khoa Nguyen and Quan Dang. Adapted from CSE 457 Modeler by
 * Brian Curless. All rights reserved. Permission is hereby granted to
 * students registered for University of Washington CSE 457.
 * No other use, copying, distribution, or modification is permitted without
 * prior written consent. Copyrights for third-party components of this work
 * must be honored.  Instructors interested in reusing these course materials
 * should contact the authors below.
 * Khoa Nguyen: https://github.com/akkaneror
 * Quan Dang: https://github.com/QuanGary
 ****************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Mathf;

/// <summary>
/// SurfaceOfRevolution is responsible for generating a mesh given curve points.
/// </summary>

#if (UNITY_EDITOR)
public class SurfaceOfRevolution : MonoBehaviour
{
    private Mesh mesh;

    private List<Vector2> curvePoints;
    private int _mode;
    private int _numCtrlPts;
    private readonly string _curvePointsFile = "curvePoints.txt";
    private Vector3[] normals;
    private int[] triangles;
    private Vector2[] UVs;
    private Vector3[] vertices;

    private int subdivisions;
    public TextMeshProUGUI subdivisionText;

    private void Start()
    {
        subdivisions = 16;
        subdivisionText.text = "Subdivision: " + subdivisions.ToString();
    }

    private void Update()
    {
    }

    public void Initialize()
    {
        // Create an empty mesh
        mesh = new Mesh();
        mesh.indexFormat =
            UnityEngine.Rendering.IndexFormat.UInt32; // Set Unity's max number of vertices for a mesh to be ~4 billion
        GetComponent<MeshFilter>().mesh = mesh;

        // Load curve points
        ReadCurveFile(_curvePointsFile);

        // Invalid number of control points
        if (_mode == 0 && _numCtrlPts < 4 || _mode == 1 && _numCtrlPts < 2) return;

        // Calculate and draw mesh
        ComputeMeshData();
        UpdateMeshData();
    }


    /// <summary>
    /// Computes the surface revolution mesh given the curve points and the number of radial subdivisions.
    /// 
    /// Inputs:
    /// curvePoints : the list of sampled points on the curve.
    /// subdivisions: the number of radial subdivisions
    /// 
    /// Outputs:
    /// vertices : a list of `Vector3` containing the vertex positions
    /// normals  : a list of `Vector3` containing the vertex normals. The normal should be pointing out of
    ///            the mesh.
    /// UVs      : a list of `Vector2` containing the texture coordinates of each vertex
    /// triangles: an integer array containing vertex indices (of the `vertices` list). The first three
    ///            elements describe the first triangle, the fourth to sixth elements describe the second
    ///            triangle, and so on. The vertex must be oriented counterclockwise when viewed from the 
    ///            outside.
    /// </summary>
    private void ComputeMeshData()
    {
        // TODO: Compute and set vertex positions, normals, UVs, and triangle faces
        // You will want to use curvePoints and subdivisions variables, and you will
        // want to change the size of these arrays

        int numVerts = curvePoints.Count * (subdivisions + 1);
        vertices = new Vector3[numVerts];
        normals = new Vector3[numVerts];
        UVs = new Vector2[numVerts];

        float yMax = NegativeInfinity;
        float yMin = Infinity;
        float cumulative = 0;

        // precomputes the vertices
        for (int s = 0; s <= subdivisions; s++)
        {
            float theta = ((float)s / subdivisions) * 2 * PI;
            // rotates each point to the correct position
            for (int i = 0; i < curvePoints.Count; i++)
            {

                Vector3 v = curvePoints[i];
                Vector3 rotatedVert = new Vector3(v.x * Cos(theta) + v.z * Sin(theta),
                                                    v.y,
                                                    v.z * Cos(theta) - v.x * Sin(theta));
                vertices[i + curvePoints.Count * s] = rotatedVert;

                if (s == 0)
                {
                    if (v.y < yMin)
                    {
                        yMin = v.y;
                    }

                    if (v.y > yMax)
                    {
                        yMax = v.y;
                    }

                    if (i > 0)
                    {
                        cumulative += Vector3.Distance(vertices[i], vertices[i - 1]);
                    }
                }
            }
        }

        // computes normals and uvs
        for (int s = 0; s <= subdivisions; s++)
        {
            float theta = ((float)s / subdivisions) * 2 * PI;
            float sum = 0;
            // rotates each point to the correct position
            for (int i = 0; i < curvePoints.Count; i++)
            {

                Vector3 n;
                if (i == 0)
                {
                    n = Vector3.Cross((vertices[i] - vertices[i + 1]) / Vector3.Magnitude(vertices[i] - vertices[i + 1]), Vector3.forward);
                }
                else if (i == curvePoints.Count - 1)
                {
                    n = Vector3.Cross((vertices[i - 1] - vertices[i]) / Vector3.Magnitude(vertices[i - 1] - vertices[i]), Vector3.forward);
                }
                else
                {
                    n = Vector3.Cross((vertices[i - 1] - vertices[i + 1]) / Vector3.Magnitude(vertices[i - 1] - vertices[i + 1]), Vector3.forward);
                }

                Vector3 rotatedNorm = new Vector3(n.x * Cos(theta) + n.z * Sin(theta),
                                                    n.y,
                                                    n.z * Cos(theta) - n.x * Sin(theta));
                normals[i + curvePoints.Count * s] = rotatedNorm;


                if (i > 0)
                {
                    sum += Vector3.Distance(vertices[i], vertices[i - 1]);
                }

                float u = 1 - theta / (2 * PI);
                float v = sum / cumulative;
                UVs[i + curvePoints.Count * s] = new Vector2(u, v);
            }
        }

        int numTris = (curvePoints.Count - 1) * 2 * subdivisions;
        triangles = new int[numTris * 3];

        // computes the triangles and normals
        for (int s = 0; s < subdivisions; s++)
        {
            for (int i = 0; i < curvePoints.Count - 1; i++)
            {
                // we compute 2 triangles at the same time, since 2 triangles make 1 rectangle

                // the corners of the rectangle, clockwise from top left
                int c1, c2, c3, c4;
                c1 = i + curvePoints.Count * s;
                c2 = c1 + curvePoints.Count;
                c3 = c2 + 1;
                c4 = c1 + 1;

                // the vertices of the triangles are just permutations of the 4 corners
                // (we have to mod them by numVerts so that the last row connects back up to the first
                int v1, v2, v3, v4, v5, v6;
                v1 = c1;
                v2 = c4;
                v3 = c3;
                v4 = c1;
                v5 = c3;
                v6 = c2;

                // curvePoints.Count - 1 is the number of bands. 
                // Each band is made of 2 triangles.
                int numTrisInStrip = (curvePoints.Count - 1) * 2;
                int triIndex1 = numTrisInStrip * s + i * 2;
                int triIndex2 = triIndex1 + 1;


                triangles[triIndex1 * 3 + 0] = v1;
                triangles[triIndex1 * 3 + 1] = v2;
                triangles[triIndex1 * 3 + 2] = v3;

                //Debug.Log("tri " + triIndex1 + " = [" + v1 + ", " + v2 + ", " + v3 + "]");

                triangles[triIndex2 * 3 + 0] = v4 % numVerts;
                triangles[triIndex2 * 3 + 1] = v5 % numVerts;
                triangles[triIndex2 * 3 + 2] = v6 % numVerts;

                //Debug.Log("tri " + triIndex2 + " = [" + v4 + ", " + v5 + ", " + v6 + "]");
            }
        }
    }

    private void UpdateMeshData()
    {
        // Assign data to mesh
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.triangles = triangles;
        mesh.uv = UVs;
    }

    // Export mesh as an asset
    public void ExportMesh()
    {
        string path = EditorUtility.SaveFilePanel("Save Mesh Asset", "Assets/ExportedMesh/", mesh.name, "asset");
        if (string.IsNullOrEmpty(path)) return;
        path = FileUtil.GetProjectRelativePath(path);
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
    }

    public void SubdivisionValueChanged(Slider slider)
    {
        subdivisions = (int)slider.value;
        subdivisionText.text = "Subdivision: " + subdivisions.ToString();
    }

    private void ReadCurveFile(string file)
    {
        curvePoints = new List<Vector2>();
        string line;

        var f =
            new StreamReader(file);
        if ((line = f.ReadLine()) != null)
        {
            var curveData = line.Split(' ');
            _mode = Convert.ToInt32(curveData[0]);
            _numCtrlPts = Convert.ToInt32(curveData[1]);
        }

        while ((line = f.ReadLine()) != null)
        {
            var curvePoint = line.Split(' ');
            var x = float.Parse(curvePoint[0]);
            var y = float.Parse(curvePoint[1]);
            curvePoints.Add(new Vector2(x, y));
        }

        f.Close();
    }
}
#endif
