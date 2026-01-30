Shader "Custom/Effects/FakeLightShaft"
{
    Properties
    {
        [Header(General Settings)]
        [HDR] _Color ("Light Color", Color) = (1, 1, 0.8, 1)
        _Intensity ("Intensity", Range(0, 10)) = 1.0



        [Header(Fading)]
        _FadeTop ("Fade Top", Range(0, 1)) = 0.1
        _FadeBottom ("Fade Bottom", Range(0, 1)) = 0.2
        _SideFade ("Side Fade", Range(0, 1)) = 0.1
        _NearFadeDistance ("Camera Near Fade Dist", Float) = 1.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
        }
        
        // Additive blending for light effects
        Blend SrcAlpha One 
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR; // Particle system support
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float3 worldPos : TEXCOORD1;
            };

            float4 _Color;
            float _Intensity;
            float _FadeTop;
            float _FadeBottom;
            float _SideFade;
            float _NearFadeDistance;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv; 
                o.color = v.color;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // ---------------------------------------------------------
                // Masking / Fading
                // ---------------------------------------------------------
                float fadeV = smoothstep(0.0, _FadeBottom, i.uv.y) * smoothstep(1.0, 1.0 - _FadeTop, i.uv.y);

                float distFromCenter = abs(i.uv.x - 0.5) * 2.0; 
                float fadeH = smoothstep(1.0, 1.0 - _SideFade, distFromCenter);

                // Camera Near Fade
                float distToCam = distance(_WorldSpaceCameraPos, i.worldPos);
                float fadeCam = smoothstep(0.0, _NearFadeDistance, distToCam);

                float finalAlpha = fadeV * fadeH * fadeCam;

                // ---------------------------------------------------------
                // Final Composition
                // ---------------------------------------------------------
                fixed4 finalColor = _Color * _Intensity * i.color;
                finalColor.a *= finalAlpha;

                UNITY_APPLY_FOG(i.fogCoord, finalColor);

                return finalColor;
            }
            ENDCG
        }
    }
}
