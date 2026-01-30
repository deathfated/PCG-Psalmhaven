Shader "YmneShader/CelCustom"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        
        [Header(Simple Subsurface Scattering)]
        _SSSColor ("SSS Color", Color) = (1.0, 0.5, 0.35, 1)
        _SSSIntensity ("SSS Intensity", Range(0, 2)) = 1.0
        
        [Header(Environment Lighting)]
        [Toggle] _ReceiveShadows ("Receive Shadows", Float) = 1
        [Toggle] _FlatLightProbe ("Flat Light Probe (Ignore Normal)", Float) = 1

        [Header(SH Gradient (Ignored when Flat Light Probe is enabled))]
        [Toggle] _SHGradient ("SH Gradient (Vertical)", Float) = 0
        _SHGradientIntensity ("SH Gradient Intensity", Range(0, 2)) = 1.0
        _SHGradientScale ("SH Gradient Scale", Range(0.1, 10)) = 1.0
        _SHGradientOffset ("SH Gradient Offset", Range(-5, 5)) = 0.0
        
        [Header(Self Shadow Gradient)]
        [Toggle] _SelfShadowGradient ("Enable Self Shadow Gradient", Float) = 0
        [KeywordEnum(Sphere, Normal, Flat)] _ShadowNormalMode ("Normal Mode", Float) = 2
        _SelfShadowColor ("Self Shadow Color", Color) = (0.2, 0.25, 0.3, 1)
        _SelfShadowIntensity ("Self Shadow Intensity", Range(0, 1)) = 0.5
        _SelfShadowSoftness ("Self Shadow Softness", Range(0.01, 2)) = 0.5
        _SelfShadowOffset ("Self Shadow Offset", Range(-1, 1)) = 0.0
        _SphereCenterHeight ("Sphere Center Height", Range(-5, 10)) = 1.0
        
        [Header(Emission)]
        [Toggle] _UseAlbedoAsEmission ("Use Same Emission Texture As Albedo", Float) = 0
        _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        _EmissionMap ("Emission Map", 2D) = "white" {}
        _EmissionIntensity ("Emission Intensity", Range(0, 10)) = 1.0

        [Header(See Through When Occluded)]
        [Toggle] _EnableSeeThrough ("Enable See Through", Float) = 0
        _SeeThroughColor ("See Through Color (Flat Unlit)", Color) = (0.4, 0.4, 0.5, 1)
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        // See-Through Pass - Shows flat color when behind walls
        Pass
        {
            Name "SeeThrough"
            Tags { "LightMode" = "Always" }
            
            ZTest Greater    // Only render where occluded (behind other geometry)
            ZWrite Off
            Cull Back
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            fixed4 _SeeThroughColor;
            half _EnableSeeThrough;
            
            struct appdata
            {
                float4 vertex : POSITION;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
            };
            
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                if (_EnableSeeThrough < 0.5) discard;
                return fixed4(_SeeThroughColor.rgb, 1.0);
            }
            ENDCG
        }

        CGPROGRAM
        #pragma surface surf SoftDoll fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _EmissionMap;
        
        fixed4 _Color;
        fixed4 _EmissionColor;
        fixed4 _SSSColor;
        
        half _SSSIntensity;
        half _ReceiveShadows;
        half _FlatLightProbe;
        half _SHGradient;
        half _SHGradientIntensity;
        half _SHGradientScale;
        half _SHGradientOffset;
        
        half _SelfShadowGradient;
        half _ShadowNormalMode;
        fixed4 _SelfShadowColor;
        half _SelfShadowIntensity;
        half _SelfShadowSoftness;
        half _SelfShadowOffset;
        half _SphereCenterHeight;
        half _UseAlbedoAsEmission;
        half _EmissionIntensity;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
            float3 worldNormal;
            float3 worldPos;
            INTERNAL_DATA
        };

        struct SurfaceOutputSoftDoll
        {
            fixed3 Albedo;
            fixed3 Normal;
            fixed3 Emission;
            half Occlusion;
            fixed Alpha;
            fixed3 SSSColor;
            half GradientT; // 0 = bottom, 1 = top for SH gradient
            half3 SpherizedNormal; // Spherized normal for self-shadow (object center to vertex)
        };

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        inline half4 LightingSoftDoll(SurfaceOutputSoftDoll s, half3 viewDir, UnityGI gi)
        {
            // Use flat lighting - ignore actual normals, pretend everything faces up
            float3 N = float3(0, 1, 0);
            float3 L = gi.light.dir;
            half3 lightColor = gi.light.color;
            
            // === OPTIONAL SHADOW RECEIVING ===
            // When shadows are disabled, use unattenuated light color
            if (_ReceiveShadows < 0.5)
            {
                lightColor = _LightColor0.rgb;
            }
            
            // === SIMPLE DIFFUSE ===
            half NdotL = dot(N, L);
            half diffuseTerm = saturate(NdotL);
            
            // === SUBSURFACE SCATTERING ===
            half3 sss = _SSSColor.rgb * diffuseTerm * _SSSIntensity * lightColor;
            
            // === COMBINE ===
            half3 diffuse = s.Albedo * lightColor * diffuseTerm;
            
            // Spherical Harmonics Gradient: blend between SH down and SH up based on vertical position
            half3 ambientLight = gi.indirect.diffuse;
            
            // Flat Light Probe: average SH from 6 directions for uniform GI (ignores normals)
            if (_FlatLightProbe > 0.5)
            {
                half3 shUp    = ShadeSH9(half4(0, 1, 0, 1));
                half3 shDown  = ShadeSH9(half4(0, -1, 0, 1));
                half3 shRight = ShadeSH9(half4(1, 0, 0, 1));
                half3 shLeft  = ShadeSH9(half4(-1, 0, 0, 1));
                half3 shFront = ShadeSH9(half4(0, 0, 1, 1));
                half3 shBack  = ShadeSH9(half4(0, 0, -1, 1));
                ambientLight = (shUp + shDown + shRight + shLeft + shFront + shBack) / 6.0;
            }
            
            if (_SHGradient > 0.5)
            {
                // Sample SH from up and down directions for vertical gradient
                half3 shUp   = ShadeSH9(half4(0, 1, 0, 1));
                half3 shDown = ShadeSH9(half4(0, -1, 0, 1));
                
                // Blend based on gradient factor passed from surf, with intensity control
                half3 shGradient = lerp(shDown, shUp, s.GradientT);
                ambientLight = lerp(ambientLight, shGradient, _SHGradientIntensity);
            }
            half3 ambient = s.Albedo * ambientLight;
            
            half4 c;
            c.rgb = diffuse + ambient + sss;
            
            // === SELF SHADOW GRADIENT ===
            // Use spherized normal (object center to vertex) with light direction
            if (_SelfShadowGradient > 0.5)
            {
                // Calculate NdotL using spherized normal
                half sphereNdotL = dot(s.SpherizedNormal, L);
                // Apply offset and softness for smooth transition
                half shadowTerm = smoothstep(-_SelfShadowSoftness + _SelfShadowOffset, 
                                              _SelfShadowSoftness + _SelfShadowOffset, 
                                              sphereNdotL);
                
                // Blend toward shadow color
                half3 shadowedColor = c.rgb * _SelfShadowColor.rgb;
                c.rgb = lerp(lerp(c.rgb, shadowedColor, _SelfShadowIntensity), c.rgb, shadowTerm);
            }
            
            c.a = s.Alpha;
            
            return c;
        }

        inline void LightingSoftDoll_GI(SurfaceOutputSoftDoll s, UnityGIInput data, inout UnityGI gi)
        {
            // Use flat up-facing normal for light probe sampling (keeps the flatness)
            gi = UnityGlobalIllumination(data, 1.0, float3(0, 1, 0));
        }

        void surf(Input IN, inout SurfaceOutputSoftDoll o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            
            o.Albedo = c.rgb;
            o.Normal = float3(0, 0, 1);
            o.Occlusion = 1.0;
            o.Alpha = c.a;
            o.SSSColor = c.rgb * _SSSColor.rgb;
            
            // Emission
            fixed3 emission = _EmissionColor.rgb;
            if (_UseAlbedoAsEmission > 0.5)
            {
                emission *= tex2D(_MainTex, IN.uv_MainTex).rgb;
            }
            else
            {
                emission *= tex2D(_EmissionMap, IN.uv_MainTex).rgb;
            }
            o.Emission = emission * _EmissionIntensity;
            
            // Calculate SH gradient factor based on world Y position
            half gradientY = (IN.worldPos.y + _SHGradientOffset) * _SHGradientScale;
            o.GradientT = saturate(gradientY * 0.5 + 0.5); // Remap to 0-1 range
            
            // Calculate normal for self-shadow based on mode
            float3 dir;
            if (_ShadowNormalMode < 0.5)
            {
                // Sphere mode: direction from object center to vertex
                float3 objectPivot = float3(unity_ObjectToWorld[0].w, unity_ObjectToWorld[1].w, unity_ObjectToWorld[2].w);
                float3 shadowCenter = objectPivot + float3(0, _SphereCenterHeight, 0);
                dir = normalize(IN.worldPos - shadowCenter);
            }
            else if (_ShadowNormalMode < 1.5)
            {
                // Normal mode: use actual mesh world normal (consistent across meshes)
                // Use WorldNormalVector for proper surface shader compatibility
                dir = normalize(WorldNormalVector(IN, float3(0, 0, 1)));
            }
            else
            {
                // Flat mode: always use up vector
                dir = float3(0, 1, 0);
            }
            
            o.SpherizedNormal = dir;
        }
        ENDCG
        
        // Shadow Caster Pass - Uses flat normal to match lighting
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            ZWrite On
            ZTest LEqual
            Cull Back
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                V2F_SHADOW_CASTER;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                // Use flat up-facing normal to match lighting calculations
                // This prevents self-shadowing artifacts on curved surfaces
                float3 flatNormal = float3(0, 1, 0);
                
                // Apply shadow caster with flat normal bias
                o.pos = UnityClipSpaceShadowCasterPos(v.vertex.xyz, flatNormal);
                o.pos = UnityApplyLinearShadowBias(o.pos);
                
                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
