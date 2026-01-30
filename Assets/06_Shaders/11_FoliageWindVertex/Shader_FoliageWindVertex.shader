Shader "YmneShader/FoliageWindVertex"
{
    Properties
    {
        [Header(Main Settings)]
        _Color ("Tint Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        
        [Header(Subsurface Scattering)]
        [Tooltip(Color of the transmitted light)]
        _SSSColor ("SSS Color", Color) = (0.5, 0.8, 0.2, 1)
        [Tooltip(Intensity of the SSS effect)]
        _SSSStrength ("SSS Strength", Range(0, 5)) = 1.0
        [Tooltip(Distortion of transmission normal)]
        _SSSDistortion ("SSS Distortion", Range(0, 1)) = 0.5
        [Tooltip(Focus of the transmission spot)]
        _SSSPower ("SSS Power", Range(0.1, 10)) = 3.0
        
        [Header(Wind Settings)]
        [Tooltip(Overall speed multiplier)]
        _WindSpeed ("Wind Speed", Range(0, 10)) = 1.0
        [Tooltip(Strength of the main branch swaying)]
        _WindStrength ("Main Sway Strength", Range(0, 2)) = 0.1
        [Tooltip(Strength of the leaf flutter detail)]
        _WindDetailStrength ("Detail Flutter Strength", Range(0,0.5)) = 0.05
        [Tooltip(Frequency of the wind noise)]
        _WindFrequency ("Wind Frequency", Range(0, 5)) = 1.0
        [Tooltip(Scale of the wind noise pattern)]
        _WindScale ("Wind Noise Scale", Float) = 0.5

        [Header(Voxel Settings)]
        [Tooltip(Size of voxels for snapping wind noise. Set to 0 to disable.)]
        _VoxelSize ("Voxel Size", Float) = 0.1
    }
    
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 200
        Cull Off
        
        CGPROGRAM
        // Physically based Standard lighting model, enable shadows on all light types
        // Using 'surf' function, 'StandardTranslucent' custom lighting, 'vert' vertex modifier
        #pragma surface surf StandardTranslucent vertex:vert alphatest:_Cutoff addshadow fullforwardshadows
        #pragma target 3.0

        #include "UnityPBSLighting.cginc"

        // -----------------------------------------------------------------------------
        // Properties
        // -----------------------------------------------------------------------------
        
        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        
        // SSS
        fixed4 _SSSColor;
        half _SSSStrength;
        half _SSSDistortion;
        half _SSSPower;

        // Wind
        half _WindSpeed;
        half _WindStrength;
        half _WindDetailStrength;
        half _WindFrequency;
        half _WindScale;
        
        // Voxel
        float _VoxelSize;

        // -----------------------------------------------------------------------------
        // Lighting Models
        // -----------------------------------------------------------------------------

        // Custom Lighting Function for Translucency / SSS
        fixed4 LightingStandardTranslucent(SurfaceOutputStandard s, fixed3 viewDir, UnityGI gi)
        {
            // 1. Standard PBR Lighting
            fixed4 pbr = LightingStandard(s, viewDir, gi);
            
            // 2. Subsurface Scattering Calculation
            // Simulate light passing through the object using a distorted normal approach
            float3 L = gi.light.dir;
            float3 V = viewDir;
            float3 N = s.Normal;
            
            // Distort the normal towards the light direction for the transmission term
            float3 H = normalize(L + N * _SSSDistortion);
            float VdotH = pow(saturate(dot(V, -H)), _SSSPower);
            
            // Calculate final SSS contribution
            float3 sss = _SSSColor.rgb * VdotH * _SSSStrength * gi.light.color * s.Albedo.rgb;
            
            pbr.rgb += sss;
            return pbr;
        }

        // Global Illumination Function (Required for custom lighting models)
        void LightingStandardTranslucent_GI(SurfaceOutputStandard s, UnityGIInput data, inout UnityGI gi)
        {
            LightingStandard_GI(s, data, gi);
        }

        // -----------------------------------------------------------------------------
        // Vertex Modification (Wind)
        // -----------------------------------------------------------------------------

        void vert(inout appdata_full v)
        {
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            float t = _Time.y * _WindSpeed;
            
            // Qualtize position for voxel rendering if enabled
            float3 windPos = worldPos;
            if (_VoxelSize > 0.0)
            {
                // Snap to center of voxel (offset by 0.5 to avoid boundary issues at integer coordinates)
                float3 snapPos = worldPos / _VoxelSize;
                windPos = floor(snapPos + 0.5) * _VoxelSize;
            }
            
            // 1. Main Sway (Low Frequency)
            // Simulates entire branches moving in the wind
            float offsetMain = sin(t + windPos.x * _WindScale + windPos.z * _WindScale);
            float3 sway = float3(offsetMain, 0, offsetMain) * _WindStrength;
            
            // 2. Detail Flutter (High Frequency)
            // Simulates individual leaves fluttering
            float offsetDetail = sin(t * 3.0 + windPos.x * 5.0 + windPos.z * 5.0);
            float3 flutter = float3(offsetDetail, offsetDetail * 0.5, offsetDetail) * _WindDetailStrength;
            
            // 3. Masking
            // Use vertex.y (local space) instead of UVs to avoid tearing on texture atlased models
            // Assuming pivot is at bottom. 
            float mask = max(0, v.vertex.y); // Clamp to 0 to avoid root moving if pivot is centered
            mask = mask * mask; // Non-linear stiffness curve
            
            // Apply Offset
            v.vertex.xyz += (sway + flutter) * mask;
        }

        // -----------------------------------------------------------------------------
        // Surface Shader
        // -----------------------------------------------------------------------------

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    
    FallBack "Legacy Shaders/Transparent/Cutout/VertexLit"
}
