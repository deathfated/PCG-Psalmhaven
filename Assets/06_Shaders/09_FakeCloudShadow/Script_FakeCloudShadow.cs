using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace YmneShader.Volumetric
{
    [ExecuteAlways]
    public class Script_FakeCloudShadow : MonoBehaviour
    {
        [Header("Shadow Settings")]
        public Color shadowColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        [Min(0f)] public float intensity = 1.0f;
        [Range(0f, 10f)] public float contrast = 1.0f;
        [Range(0.001f, 0.5f)] public float edgeSoftness = 0.1f;

        [Header("Transform Settings")]
        public Vector3 size = Vector3.one;

        [Header("Noise Settings")]
        public Vector3 noiseScale = new Vector3(3f, 3f, 3f);
        public Vector3 speed = new Vector3(0.2f, 0.1f, 0.2f);

        // Internal rendering objects
        [HideInInspector] public Mesh boxMesh;
        [HideInInspector] public Shader shader; 
        private Material _instanceMaterial;

        // Shader Property IDs
        private static readonly int ShadowColorID = Shader.PropertyToID("_ShadowColor");
        private static readonly int IntensityID = Shader.PropertyToID("_Intensity");
        private static readonly int ContrastID = Shader.PropertyToID("_Contrast");
        private static readonly int NoiseScaleID = Shader.PropertyToID("_NoiseScale");
        private static readonly int SpeedID = Shader.PropertyToID("_Speed");
        private static readonly int EdgeSoftnessID = Shader.PropertyToID("_EdgeSoftness");

        private void OnEnable()
        {
            if (boxMesh == null)
                boxMesh = GenerateCubeMesh();

            // Initialize Material
            if (_instanceMaterial == null)
            {
                if (shader == null)
                    shader = Shader.Find("YmneShader/VolumetricCloudShadow");

                if (shader != null)
                {
                    _instanceMaterial = new Material(shader);
                    _instanceMaterial.hideFlags = HideFlags.HideAndDontSave;
                }
            }

            transform.localScale = Vector3.one; 
            UpdateMaterial();
        }

        private void OnDisable()
        {
            if (_instanceMaterial != null)
            {
                if (Application.isPlaying)
                    Destroy(_instanceMaterial);
                else
                    DestroyImmediate(_instanceMaterial);
                
                _instanceMaterial = null;
            }
        }

        private void OnValidate()
        {
            UpdateMaterial();
            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UpdateMaterial();
                if (transform.localScale != Vector3.one)
                    transform.localScale = Vector3.one;
            }
            #endif
        }

        public void UpdateMaterial()
        {
            if (_instanceMaterial == null) return;

            _instanceMaterial.SetColor(ShadowColorID, shadowColor);
            _instanceMaterial.SetFloat(IntensityID, intensity);
            _instanceMaterial.SetFloat(ContrastID, contrast);
            _instanceMaterial.SetVector(NoiseScaleID, noiseScale);
            _instanceMaterial.SetVector(SpeedID, speed);
            _instanceMaterial.SetFloat(EdgeSoftnessID, edgeSoftness);
        }

        private void OnRenderObject()
        {
            if (boxMesh == null || _instanceMaterial == null) return;

            // Create matrix with custom size
            Matrix4x4 matrix = Matrix4x4.TRS(
                transform.position,
                transform.rotation,
                size
            );

            _instanceMaterial.SetPass(0);
            Graphics.DrawMeshNow(boxMesh, matrix);
        }

        private static Mesh GenerateCubeMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "FakeCloudShadow_Box";

            Vector3[] vertices = new Vector3[8]
            {
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3( 0.5f, -0.5f, -0.5f),
                new Vector3( 0.5f,  0.5f, -0.5f),
                new Vector3(-0.5f,  0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f,  0.5f),
                new Vector3( 0.5f, -0.5f,  0.5f),
                new Vector3( 0.5f,  0.5f,  0.5f),
                new Vector3(-0.5f,  0.5f,  0.5f)
            };
            
            // Standard triangles (CW or CCW)
            int[] triangles = new int[]
            {
                0, 2, 1, 0, 3, 2, // Front
                2, 3, 7, 2, 7, 6, // Top
                1, 2, 6, 1, 6, 5, // Right
                4, 0, 1, 4, 1, 5, // Bottom
                3, 0, 4, 3, 4, 7, // Left
                5, 6, 7, 5, 7, 4  // Back
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Use Particle System icon
            Gizmos.DrawIcon(transform.position, "Particle System", true, shadowColor); 
            
            Gizmos.color = new Color(shadowColor.r, shadowColor.g, shadowColor.b, 0.2f);
            
            // Use custom matrix for gizmo to match render size
            Matrix4x4 matrix = Matrix4x4.TRS(transform.position, transform.rotation, size);
            Gizmos.matrix = matrix;
            
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = shadowColor;
            
            // Use custom matrix for gizmo to match render size
            Matrix4x4 matrix = Matrix4x4.TRS(transform.position, transform.rotation, size);
            Gizmos.matrix = matrix;
            
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }

        [MenuItem("GameObject/Light/Fake Cloud Shadow", false, 11)]
        private static void CreateFakeCloudShadow(MenuCommand menuCommand)
        {
            GameObject obj = new GameObject("Fake Cloud Shadow");
            var script = obj.AddComponent<Script_FakeCloudShadow>();
            
            // Find shader explicitly if needed, but OnEnable handles it
            var shader = Shader.Find("YmneShader/VolumetricCloudShadow");
            if(shader != null) script.shader = shader;

            GameObjectUtility.SetParentAndAlign(obj, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(obj, "Create Fake Cloud Shadow");
            Selection.activeObject = obj;
        }
#endif
    }
}
