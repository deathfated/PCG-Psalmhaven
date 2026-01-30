using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace YmneShader.Volumetric
{
    [ExecuteAlways]
    public class Script_VolumetricDarkening : MonoBehaviour
    {
        [Header("Darkening Settings")]
        public Color darkeningColor = new Color(0.0f, 0.0f, 0.0f, 1f);
        [Min(0f)] public float intensity = 1.0f;
        [Range(0.001f, 0.5f)] public float edgeSoftness = 0.1f;

        [Header("Bounds Settings")]
        public Vector3 size = Vector3.one;

        // Internal rendering objects
        [HideInInspector] public Mesh boxMesh;
        [HideInInspector] public Shader shader; 
        private Material _instanceMaterial;

        // Shader Property IDs
        private static readonly int ShadowColorID = Shader.PropertyToID("_ShadowColor");
        private static readonly int IntensityID = Shader.PropertyToID("_Intensity");
        private static readonly int EdgeSoftnessID = Shader.PropertyToID("_EdgeSoftness");

        private void OnEnable()
        {
            if (boxMesh == null)
                boxMesh = GenerateCubeMesh();

            // Initialize Material
            if (_instanceMaterial == null)
            {
                if (shader == null)
                    shader = Shader.Find("YmneShader/VolumetricDarkening");

                // Try to find it again if it was just created or reference is lost
                if (shader == null)
                     shader = Shader.Find("YmneShader/VolumetricDarkening");

                if (shader != null)
                {
                    _instanceMaterial = new Material(shader);
                    _instanceMaterial.hideFlags = HideFlags.HideAndDontSave;
                }
            }

            // Force local scale to one to rely on 'size' property
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

            _instanceMaterial.SetColor(ShadowColorID, darkeningColor);
            _instanceMaterial.SetFloat(IntensityID, intensity);
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
            mesh.name = "VolumetricDarkening_Box";

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
            Gizmos.DrawIcon(transform.position, "Light Gizmo", true, darkeningColor); 
            
            Gizmos.color = new Color(darkeningColor.r, darkeningColor.g, darkeningColor.b, 0.2f);
            
            Matrix4x4 matrix = Matrix4x4.TRS(transform.position, transform.rotation, size);
            Gizmos.matrix = matrix;
            
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = darkeningColor;
            
            Matrix4x4 matrix = Matrix4x4.TRS(transform.position, transform.rotation, size);
            Gizmos.matrix = matrix;
            
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }

        [MenuItem("GameObject/Light/Volumetric Darkening", false, 12)]
        private static void CreateVolumetricDarkening(MenuCommand menuCommand)
        {
            GameObject obj = new GameObject("Volumetric Darkening");
            var script = obj.AddComponent<Script_VolumetricDarkening>();
            
            var shader = Shader.Find("YmneShader/VolumetricDarkening");
            if(shader != null) script.shader = shader;

            GameObjectUtility.SetParentAndAlign(obj, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(obj, "Create Volumetric Darkening");
            Selection.activeObject = obj;
        }
#endif
    }
}
