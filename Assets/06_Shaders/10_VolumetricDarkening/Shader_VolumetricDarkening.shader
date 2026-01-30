Shader "YmneShader/VolumetricDarkening"
{
    Properties
    {
        _ShadowColor ("Darkening Color", Color) = (0, 0, 0, 1)
        _Intensity ("Intensity", Range(0, 5)) = 1.0
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.5)) = 0.1
    }
    SubShader
    {
        Tags { "Queue"="Transparent+1" "RenderType"="Transparent" "DisableBatching"="True" }
        LOD 100

        Cull Front
        ZWrite Off
        ZTest Always
        // Multiplicative blending: Dest * Source + Zero
        // If Output is White (1,1,1), Dest stays same. 
        // If Output is Dark, Dest gets darker.
        Blend DstColor Zero
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float3 rayDir : TEXCOORD1;
            };

            sampler2D _CameraDepthTexture;
            
            fixed4 _ShadowColor;
            float _Intensity;
            float _EdgeSoftness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                
                // Calculate Ray Direction (Camera to Vertex)
                // We use the vertex world position to determine the ray direction
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.rayDir = worldPos - _WorldSpaceCameraPos;
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. Calculate Screen UV
                float2 uv = i.screenPos.xy / i.screenPos.w;
                
                // 2. Sample Depth
                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
                float linearDepth = LinearEyeDepth(rawDepth);
                
                // 3. Reconstruct World Position
                float3 rayDir = normalize(i.rayDir);
                
                // Convert LinearEyeDepth (Z distance) to Radial Distance
                // ViewZ is the distance along the camera's forward axis
                float3 cameraForward = -UNITY_MATRIX_V[2].xyz;
                float viewZ = dot(rayDir, cameraForward);
                float dist = linearDepth / viewZ;
                
                float3 worldPos = _WorldSpaceCameraPos + rayDir * dist;
                
                // 4. Object Space Projection to check bounds
                float3 objPos = mul(unity_WorldToObject, float4(worldPos, 1.0)).xyz;
                
                // 5. Box Bounds Check (-0.5 to 0.5)
                float3 distToEdge = 0.5 - abs(objPos);
                float minEdgeDist = min(distToEdge.x, min(distToEdge.y, distToEdge.z));
                
                // Calculate edge fade
                // We add a small margin (0.001) to ensure the effect fades to 0 BEFORE hitting the mesh edge.
                // This prevents precision artifacts/bleeding at the exact geometry boundary.
                float edgeMask = smoothstep(0.001, _EdgeSoftness, minEdgeDist);

                // Calculate final shadow factor
                // 0 = no shadow (white), 1 = full shadow color
                float shadowFactor = _Intensity * edgeMask;
                
                // Lerp between White (no change) and ShadowColor based on intensity
                // Multiplicative blend: White * BG = BG. ShadowColor * BG = Darker BG.
                fixed3 finalColor = lerp(fixed3(1,1,1), _ShadowColor.rgb, saturate(shadowFactor));

                return fixed4(finalColor, 1.0);
            }
            ENDCG
        }
    }
}
