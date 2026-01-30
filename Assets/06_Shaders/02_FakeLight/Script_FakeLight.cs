using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class Script_FakeLight : MonoBehaviour
{
    [Header("Light Settings")]
    public Color lightColor = new Color(0.95f, 0.95f, 0.95f, 1f);
    [Min(0f)] public float intensity = 128f; // Reduced from 256 to work with One One blending
    [Range(1f, 5f)] public float falloffExponent = 2.8f;
    [Min(0.01f)] public float lightSize = 1f;
    
    [Header("Halo Settings")]
    public bool enableHalo = false;
    [Range(0f, 5f)] public float haloSize = 0.5f;
    [Range(0f, 5f)] public float haloIntensity = 1f;
    [Range(1f, 10f)] public float haloFalloffExponent = 4f;
    
    [Header("Shadow Settings")]
    public bool enableShadows = false;
    public bool useTAAJitter = false;
    [Range(0f, 1f)] public float shadowStrength = 1f;
    [Range(4, 128)] public int shadowSteps = 4;
    [Range(0.001f, 0.2f)] public float shadowBias = 0.05f;
    [Range(0f, 128f)] public float shadowMaxDistance = 4f;
    [Range(0f, 1f)] public float shadowBlur = 0f;
    [Range(0f, 1f)] public float shadowSourceRadius = 1f;
    
    // Custom rendering (no MeshRenderer needed)
    [HideInInspector] public Mesh sphereMesh;
    [HideInInspector] public Material material; // Deprecated, keeping for migration
    [HideInInspector] public Shader shader; // The shader to use for creation
    private Material _instanceMaterial; // The runtime instance

    
    // Shader property IDs (cached for performance)
    private static readonly int ColorID = Shader.PropertyToID("_Color");
    private static readonly int IntensityID = Shader.PropertyToID("_Intensity");
    private static readonly int FalloffExpID = Shader.PropertyToID("_FalloffExp");
    private static readonly int EnableHaloID = Shader.PropertyToID("_EnableHalo");
    private static readonly int HaloSizeID = Shader.PropertyToID("_HaloSize");
    private static readonly int HaloIntensityID = Shader.PropertyToID("_HaloIntensity");
    private static readonly int HaloFalloffExpID = Shader.PropertyToID("_HaloFalloffExp");
    private static readonly int EnableShadowsID = Shader.PropertyToID("_EnableShadows");
    private static readonly int UseTAAJitterID = Shader.PropertyToID("_UseTAAJitter");
    private static readonly int ShadowStrengthID = Shader.PropertyToID("_ShadowStrength");
    private static readonly int ShadowStepsID = Shader.PropertyToID("_ShadowSteps");
    private static readonly int ShadowBiasID = Shader.PropertyToID("_ShadowBias");
    private static readonly int ShadowMaxDistID = Shader.PropertyToID("_ShadowMaxDist");
    private static readonly int ShadowBlurID = Shader.PropertyToID("_ShadowBlur");
    private static readonly int ShadowSourceRadiusID = Shader.PropertyToID("_ShadowSourceRadius");
    
    private void OnEnable()
    {
        // Generate mesh if needed
        if (sphereMesh == null)
        {
            sphereMesh = GenerateLowPolySphere(1, 0.5f);
        }
        
        // Migration/Initialization logic
        if (_instanceMaterial == null)
        {
            // If we have a shader, use it
            if (shader != null)
            {
                _instanceMaterial = new Material(shader);
                _instanceMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
            // Fallback: If no shader but legacy material exists, grab shader from it
            else if (material != null)
            {
                shader = material.shader;
                _instanceMaterial = new Material(shader);
                _instanceMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        // Keep transform scale locked at 1,1,1
        transform.localScale = Vector3.one;
        
        UpdateMaterial();
    }

    private void OnDisable()
    {
        if (_instanceMaterial != null)
        {
            DestroyImmediate(_instanceMaterial);
            _instanceMaterial = null;
        }
    }
    
    private void OnValidate()
    {
        UpdateMaterial();
        
        // Keep transform scale locked at 1,1,1
        transform.localScale = Vector3.one;
    }
    
    private void Start()
    {
        UpdateMaterial();
    }
    
    private void Update()
    {
        #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UpdateMaterial();
            
            // Keep transform scale locked at 1,1,1
            if (transform.localScale != Vector3.one)
                transform.localScale = Vector3.one;
        }
        #endif
    }
    
    // Custom rendering - this makes the mesh completely unclickable
    private void OnRenderObject()
    {
        if (sphereMesh == null || _instanceMaterial == null)
            return;
            
        // Create transform matrix with lightSize scaling
        Matrix4x4 matrix = Matrix4x4.TRS(
            transform.position,
            transform.rotation,
            Vector3.one * lightSize
        );
            
        _instanceMaterial.SetPass(0);
        Graphics.DrawMeshNow(sphereMesh, matrix);
    }
    

    
    public void UpdateMaterial()
    {
        if (_instanceMaterial == null)
            return;
        
        // Light settings
        _instanceMaterial.SetColor(ColorID, lightColor);
        _instanceMaterial.SetFloat(IntensityID, intensity);
        _instanceMaterial.SetFloat(FalloffExpID, falloffExponent);
        
        // Halo settings
        _instanceMaterial.SetFloat(EnableHaloID, enableHalo ? 1f : 0f);
        _instanceMaterial.SetFloat(HaloSizeID, haloSize);
        _instanceMaterial.SetFloat(HaloIntensityID, haloIntensity);
        _instanceMaterial.SetFloat(HaloFalloffExpID, haloFalloffExponent);
        
        // Shadow settings
        _instanceMaterial.SetFloat(EnableShadowsID, enableShadows ? 1f : 0f);
        _instanceMaterial.SetFloat(UseTAAJitterID, useTAAJitter ? 1f : 0f);
        _instanceMaterial.SetFloat(ShadowStrengthID, shadowStrength);
        _instanceMaterial.SetFloat(ShadowStepsID, shadowSteps);
        _instanceMaterial.SetFloat(ShadowBiasID, shadowBias);
        _instanceMaterial.SetFloat(ShadowMaxDistID, shadowMaxDistance);
        _instanceMaterial.SetFloat(ShadowBlurID, shadowBlur);
        _instanceMaterial.SetFloat(ShadowSourceRadiusID, shadowSourceRadius);
        
        // Update material keywords for shader variants
        if (enableHalo)
            _instanceMaterial.EnableKeyword("_ENABLE_HALO");
        else
            _instanceMaterial.DisableKeyword("_ENABLE_HALO");
            
        if (enableShadows)
            _instanceMaterial.EnableKeyword("_ENABLE_SHADOWS");
        else
            _instanceMaterial.DisableKeyword("_ENABLE_SHADOWS");
            
        if (useTAAJitter)
            _instanceMaterial.EnableKeyword("_USE_TAA_JITTER");
        else
            _instanceMaterial.DisableKeyword("_USE_TAA_JITTER");
    }
    
#if UNITY_EDITOR
    // Draw gizmo to visualize the light in scene view
    private void OnDrawGizmos()
    {
        // Draw point light icon for easy clicking
        Gizmos.DrawIcon(transform.position, "PointLight Gizmo", true, lightColor);
        
        Gizmos.color = new Color(lightColor.r, lightColor.g, lightColor.b, 0.3f);
        Gizmos.DrawWireSphere(transform.position, lightSize * 0.5f);
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = lightColor;
        Gizmos.DrawWireSphere(transform.position, lightSize * 0.5f);
        
        if (enableHalo)
        {
            Gizmos.color = new Color(lightColor.r, lightColor.g, lightColor.b, 0.2f);
            Gizmos.DrawWireSphere(transform.position, lightSize * 0.5f * haloSize);
        }
    }
    
    /// <summary>
    /// Generates a low-poly sphere mesh (icosphere) procedurally.
    /// </summary>
    /// <param name="subdivisions">Number of subdivisions (0 = icosahedron with 20 faces, 1 = 80 faces, 2 = 320 faces)</param>
    /// <param name="radius">Radius of the sphere</param>
    private static Mesh GenerateLowPolySphere(int subdivisions = 1, float radius = 0.5f)
    {
        Mesh mesh = new Mesh();
        mesh.name = "FakeLight_Sphere";
        
        // Golden ratio for icosahedron
        float t = (1f + Mathf.Sqrt(5f)) / 2f;
        
        // Create 12 vertices of an icosahedron
        var vertices = new System.Collections.Generic.List<Vector3>
        {
            new Vector3(-1,  t,  0).normalized * radius,
            new Vector3( 1,  t,  0).normalized * radius,
            new Vector3(-1, -t,  0).normalized * radius,
            new Vector3( 1, -t,  0).normalized * radius,
            new Vector3( 0, -1,  t).normalized * radius,
            new Vector3( 0,  1,  t).normalized * radius,
            new Vector3( 0, -1, -t).normalized * radius,
            new Vector3( 0,  1, -t).normalized * radius,
            new Vector3( t,  0, -1).normalized * radius,
            new Vector3( t,  0,  1).normalized * radius,
            new Vector3(-t,  0, -1).normalized * radius,
            new Vector3(-t,  0,  1).normalized * radius
        };
        
        // Create 20 triangles of the icosahedron (reversed winding for outward-facing normals)
        var triangles = new System.Collections.Generic.List<int>
        {
            // 5 faces around point 0
            0, 5, 11,   0, 1, 5,    0, 7, 1,    0, 10, 7,   0, 11, 10,
            // 5 adjacent faces
            1, 9, 5,    5, 4, 11,   11, 2, 10,  10, 6, 7,   7, 8, 1,
            // 5 faces around point 3
            3, 4, 9,    3, 2, 4,    3, 6, 2,    3, 8, 6,    3, 9, 8,
            // 5 adjacent faces
            4, 5, 9,    2, 11, 4,   6, 10, 2,   8, 7, 6,    9, 1, 8
        };
        
        // Cache for midpoint indices to avoid duplicates
        var midpointCache = new System.Collections.Generic.Dictionary<long, int>();
        
        // Subdivide
        for (int i = 0; i < subdivisions; i++)
        {
            var newTriangles = new System.Collections.Generic.List<int>();
            
            for (int j = 0; j < triangles.Count; j += 3)
            {
                int v1 = triangles[j];
                int v2 = triangles[j + 1];
                int v3 = triangles[j + 2];
                
                // Get midpoints
                int a = GetMidpoint(v1, v2, vertices, midpointCache, radius);
                int b = GetMidpoint(v2, v3, vertices, midpointCache, radius);
                int c = GetMidpoint(v3, v1, vertices, midpointCache, radius);
                
                // Create 4 new triangles
                newTriangles.Add(v1); newTriangles.Add(c); newTriangles.Add(a);
                newTriangles.Add(v2); newTriangles.Add(a); newTriangles.Add(b);
                newTriangles.Add(v3); newTriangles.Add(b); newTriangles.Add(c);
                newTriangles.Add(a);  newTriangles.Add(c); newTriangles.Add(b);
            }
            
            triangles = newTriangles;
            midpointCache.Clear();
        }
        
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }
    
    private static int GetMidpoint(int i1, int i2, 
        System.Collections.Generic.List<Vector3> vertices, 
        System.Collections.Generic.Dictionary<long, int> cache, 
        float radius)
    {
        // Create a unique key for the edge
        long smallerIndex = Mathf.Min(i1, i2);
        long greaterIndex = Mathf.Max(i1, i2);
        long key = (smallerIndex << 32) + greaterIndex;
        
        if (cache.TryGetValue(key, out int ret))
            return ret;
        
        // Calculate midpoint and push to sphere surface
        Vector3 p1 = vertices[i1];
        Vector3 p2 = vertices[i2];
        Vector3 middle = ((p1 + p2) / 2f).normalized * radius;
        
        int newIndex = vertices.Count;
        vertices.Add(middle);
        cache[key] = newIndex;
        
        return newIndex;
    }
    
    // Menu item to create Fake Light
    [MenuItem("GameObject/Light/Fake Light", false, 10)]
    private static void CreateFakeLight(MenuCommand menuCommand)
    {
        // Get the path to the script to find associated assets
        string[] guids = AssetDatabase.FindAssets("t:Script Script_FakeLight");
        if (guids.Length == 0)
        {
            Debug.LogError("Could not find Script_FakeLight script!");
            return;
        }
        
        string scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        string folderPath = System.IO.Path.GetDirectoryName(scriptPath);
        
        // Generate low-poly sphere mesh procedurally (1 subdivision = 80 faces)
        Mesh fakeLightMesh = GenerateLowPolySphere(1, 0.5f);
        
        // Load shader and create material
        string shaderPath = folderPath + "/Shader_FakeLight.shader";
        Shader fakeLightShader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
        
        if (fakeLightShader == null)
        {
            Debug.LogError($"Could not find Shader_FakeLight.shader at {shaderPath}");
            return;
        }
        
        // Create the game object
        GameObject lightObj = new GameObject("Fake Light");
        
        // Add the fake light script first
        Script_FakeLight fakeLight = lightObj.AddComponent<Script_FakeLight>();
        
        // Assign shader
        fakeLight.shader = fakeLightShader;
        
        // Generate the mesh
        fakeLight.sphereMesh = GenerateLowPolySphere(1, 0.5f);
        
        // Force initialization (will create material instance)
        fakeLight.SendMessage("OnEnable"); 
        
        // Initialize material properties
        fakeLight.UpdateMaterial();
        
        // Parent to context object if any
        GameObjectUtility.SetParentAndAlign(lightObj, menuCommand.context as GameObject);
        
        // Register undo
        Undo.RegisterCreatedObjectUndo(lightObj, "Create Fake Light");
        
        // Select the new object
        Selection.activeObject = lightObj;
        
        Debug.Log("Created Fake Light!");
    }
    
    // Custom editor
    [CustomEditor(typeof(Script_FakeLight))]
    [CanEditMultipleObjects]
    public class Script_FakeLightEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck())
            {
                // Update material on all selected fake lights
                foreach (Object obj in targets)
                {
                    Script_FakeLight fakeLight = (Script_FakeLight)obj;
                    fakeLight.UpdateMaterial();
                }
            }
            
            // Show info about scale and rendering
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Transform scale is locked at (1,1,1). Visual size is controlled by 'Light Size' parameter.", MessageType.Info);
        }
    }
#endif
}
