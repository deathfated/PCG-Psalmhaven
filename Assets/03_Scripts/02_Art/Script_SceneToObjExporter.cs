using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class SceneToObjExporter : EditorWindow
{
    [MenuItem("Export/Export Whole Scene to OBJ")]
    static void Init()
    {
        SceneToObjExporter window = (SceneToObjExporter)EditorWindow.GetWindow(typeof(SceneToObjExporter));
        window.Show();
    }

    // Terrain resolution: 1 = Full detail (Slow/Heavy), 2 = Half, 4 = Quarter, etc.
    // Recommended: 2 or 4 to keep file size manageable.
    public int terrainSampling = 4; 
    
    void OnGUI()
    {
        GUILayout.Label("Export Scene to OBJ", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label("Terrain Quality Settings", EditorStyles.boldLabel);
        GUILayout.Label("1 = Full Detail (Heavy!), 4 = Optimized (Recommended)", EditorStyles.helpBox);
        terrainSampling = EditorGUILayout.IntSlider("Resolution Downsample", terrainSampling, 1, 8);

        GUILayout.Space(20);

        if (GUILayout.Button("Export Scene (.obj)"))
        {
            ExportScene();
        }
    }

    void ExportScene()
    {
        string path = EditorUtility.SaveFilePanel("Save Scene to OBJ", "", "UnitySceneExport.obj", "obj");
        if (string.IsNullOrEmpty(path)) return;

        StringBuilder sb = new StringBuilder();
        int vertexOffset = 0;

        // 1. Export All MeshFilters (Standard Objects)
        MeshFilter[] meshFilters = FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.sharedMesh == null) continue;

            sb.Append(MeshToString(mf, ref vertexOffset));
        }

        // 2. Export Terrain
        Terrain[] terrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
        foreach (Terrain terrain in terrains)
        {
            sb.Append(TerrainToString(terrain, ref vertexOffset));
        }

        File.WriteAllText(path, sb.ToString());
        EditorUtility.DisplayDialog("Success", "Scene exported to " + path, "OK");
    }

    string MeshToString(MeshFilter mf, ref int vertexOffset)
    {
        Mesh m = mf.sharedMesh;
        StringBuilder sb = new StringBuilder();

        sb.Append("g " + mf.name + "\n");

        // Vertices
        foreach (Vector3 v in m.vertices)
        {
            Vector3 worldPos = mf.transform.TransformPoint(v);
            sb.Append(string.Format("v {0} {1} {2}\n", -worldPos.x, worldPos.y, worldPos.z)); // Invert X for OBJ standard
        }

        // Normals
        foreach (Vector3 n in m.normals)
        {
            Vector3 worldNormal = mf.transform.TransformDirection(n);
            sb.Append(string.Format("vn {0} {1} {2}\n", -worldNormal.x, worldNormal.y, worldNormal.z));
        }

        // UVs
        foreach (Vector2 uv in m.uv)
        {
            sb.Append(string.Format("vt {0} {1}\n", uv.x, uv.y));
        }

        // Faces
        for (int i = 0; i < m.triangles.Length; i += 3)
        {
            int i1 = m.triangles[i] + 1 + vertexOffset;
            int i2 = m.triangles[i + 1] + 1 + vertexOffset;
            int i3 = m.triangles[i + 2] + 1 + vertexOffset;
            
            // Format: f v/vt/vn
            sb.Append(string.Format("f {0}/{0}/{0} {2}/{2}/{2} {1}/{1}/{1}\n", i1, i2, i3));
        }

        vertexOffset += m.vertices.Length;
        return sb.ToString();
    }

    string TerrainToString(Terrain terrain, ref int vertexOffset)
    {
        StringBuilder sb = new StringBuilder();
        TerrainData data = terrain.terrainData;
        
        // Safety check for export resolution
        int w = data.heightmapResolution;
        int h = data.heightmapResolution;
        
        Vector3 meshScale = data.size;
        meshScale = new Vector3(meshScale.x / (w - 1), meshScale.y, meshScale.z / (h - 1));
        
        int tRes = terrainSampling; // Downsampling factor
        
        sb.Append("g " + terrain.name + "\n");

        // 1. Generate Vertices
        for (int y = 0; y < h; y += tRes)
        {
            for (int x = 0; x < w; x += tRes)
            {
                // Get Height
                float height = data.GetHeight(x, y);
                
                // Convert to World Position
                Vector3 worldPos = terrain.transform.position + new Vector3(x * meshScale.x, height, y * meshScale.z);
                
                // Write to OBJ (Invert X for standard OBJ orientation)
                sb.Append(string.Format("v {0} {1} {2}\n", -worldPos.x, worldPos.y, worldPos.z));
                
                // Simple UVs (0 to 1 based on position)
                sb.Append(string.Format("vt {0} {1}\n", (float)x / w, (float)y / h));
                
                // Simple Up Normals (Optimized)
                sb.Append("vn 0 1 0\n"); 
            }
        }

        // 2. Generate Faces
        int wVertices = (w - 1) / tRes + 1;
        int hVertices = (h - 1) / tRes + 1;

        for (int y = 0; y < hVertices - 1; y++)
        {
            for (int x = 0; x < wVertices - 1; x++)
            {
                int i = (y * wVertices) + x;
                
                int v1 = i + 1 + vertexOffset;
                int v2 = i + 1 + wVertices + vertexOffset;
                int v3 = i + 1 + wVertices + 1 + vertexOffset;
                int v4 = i + 1 + 1 + vertexOffset;

                // Two triangles per grid square
                sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n", v4, v3, v1));
                sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n", v1, v3, v2));
            }
        }

        vertexOffset += wVertices * hVertices;
        return sb.ToString();
    }
}