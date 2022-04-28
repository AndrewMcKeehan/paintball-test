using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplatMesher : MonoBehaviour
{
	public static SplatMesher instance;

    private Mesh mesh;
    private MeshFilter filter;
    private GameObject helper;

    private const int splatVerts = 4;
    private float splatRadius = 0.8f;
    private const int trianglesPerSplat = splatVerts - 2;
    private int numSplats = 0;
    private int maxSplats = 64;
    private int maxMaxSplats = 4096;

    private Vector3[] normals;
    private int[] triangles;
    private Vector2[] UVs;
    private Vector3[] vertices;
    // Start is called before the first frame update
    void Start()
    {
		if (instance == null)
		{
			instance = this;
		}
		else
		{
			Destroy(this);
		}
		

        mesh = new Mesh();
        Debug.Log(mesh.isReadable);
        filter = GetComponentInChildren<MeshFilter>();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        helper = new GameObject();
        helper.transform.parent = transform;
        helper.name = "helper";

        InitializeArrays();


        ApplyMesh();
    }

	private void OnDestroy()
	{
        // rest in peace helper, you will be missed o7
        Destroy(helper);
	}

	private void Update()
	{
		/*
        if (Input.GetMouseButtonDown(0)) 
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit, 100))
			{
                AddSplat(hit);
			}
        }
		*/
	}

    void InitializeArrays()
	{
        // splatVerts is the number of vertices in a splat, and maxSplats is the maximum number of splats available at this time
        int numVerts = maxSplats * splatVerts;
        vertices = new Vector3[numVerts];

        // one normal and uv per vertex
        normals = new Vector3[numVerts];
        UVs = new Vector2[numVerts];
        
        // Each splat takes 1 triangle + 1 additional triangle for each vertex over 3.
        // We multiply by 3 because 3 indices make up 1 triangle.
        triangles = new int[(splatVerts - 2) * maxSplats * 3];
    }

    void ExpandArrays()
    {
        maxSplats *= 2;
        int numVerts = maxSplats * splatVerts;
        Vector3[] newVerts = new Vector3[numVerts];
        vertices.CopyTo(newVerts, 0);
        vertices = newVerts;

        Vector3[] newNormals = new Vector3[numVerts];
        normals.CopyTo(newNormals, 0);
        normals = newNormals;

        Vector2[] newUVs = new Vector2[numVerts];
        UVs.CopyTo(newUVs, 0);
        UVs = newUVs;

        int[] newTriangles = new int[(splatVerts - 2) * maxSplats * 3];
        triangles.CopyTo(newTriangles, 0);
        triangles = newTriangles;
        Debug.Log("expanded to " + maxSplats);
    }

    void FinalizeMesh()
	{
        // creates a new gameobject to hold the current mesh
        GameObject g = Instantiate(filter.gameObject, transform);
        g.name = filter.name;

        // clears this mesh
        InitializeArrays();
        filter = g.GetComponent<MeshFilter>();
        mesh = new Mesh();
        numSplats = 0;
	}

    public void AddSplat(RaycastHit hit)
	{
        Transform t = helper.transform;
        t.position = hit.point;
        t.LookAt(t.position + hit.normal);
        t.TransformDirection(Vector3.up);

        float r = Random.Range(0f, 1f);
        t.Translate(Vector3.forward * r * 0.05f, Space.Self);
        t.Rotate(Vector3.forward * r * 360, Space.Self);

        int c = Random.Range(0, 6);


        Vector3 normal = t.TransformDirection(Vector3.forward);
        int vertexStartIndex = numSplats * splatVerts;
        for (int i = 0; i < splatVerts; i++)
		{
            normals[vertexStartIndex + i] = normal;
            float u = 0;
            float v = c / 6.0f;
            if (i == 0 || i == 1)
			{
                u = 1;
			}
            if (i == 1 || i == 2)
			{
                v = (c + 1) / 6.0f;
			}
            UVs[vertexStartIndex + i] = new Vector2(u, v);
            t.Rotate(Vector3.forward * (360.0f / splatVerts), Space.Self);
            vertices[vertexStartIndex + i] = t.position + t.TransformDirection(Vector3.up) * splatRadius;
		}

        for (int i = 0; i < trianglesPerSplat; i++)
		{
            int triangleStartIndex = (trianglesPerSplat * numSplats + i) * 3;
            triangles[triangleStartIndex + 0] = vertexStartIndex;
            triangles[triangleStartIndex + 1] = vertexStartIndex + 1 + i;
            triangles[triangleStartIndex + 2] = vertexStartIndex + 2 + i;
        }

        numSplats++;
        ApplyMesh();


        if (numSplats == maxMaxSplats)
		{
            FinalizeMesh();
		}
        else if (numSplats == maxSplats)
		{
            ExpandArrays();
		}
	}

	void ApplyMesh()
	{
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.triangles = triangles;
        mesh.uv = UVs;

        filter.mesh = mesh;
    }

    Vector3 RotatePointAroundAxis(Vector3 point, Vector3 axis, Vector3 angles)
    {
        return Quaternion.Euler(angles) * (point - axis) + axis;
    }
}
