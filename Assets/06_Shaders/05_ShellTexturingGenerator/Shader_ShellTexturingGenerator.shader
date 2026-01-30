Shader "YmneShader/ShellTexturingGenerator"
{
    Properties
    {
        [Header(General Settings)]
        [Enum(Dirt, 0, Sand, 1, Rock, 2)] _MaterialType ("Material Type", Int) = 1
        
        [Header(Colors)]
        _BaseColor ("Base Color", Color) = (0.76, 0.65, 0.38, 1)
        _VariationColor ("Variation Color", Color) = (0.6, 0.5, 0.3, 1)
        _ColorVariationScale ("Color Noise Scale", Float) = 0.5
        
        [Header(PBR)]
        _Smoothness ("Smoothness", Range(0, 1)) = 0.2
        _SmoothnessVariation ("Smoothness Randomness", Range(0, 1)) = 0.1
        _SpecularColor ("Specular Color", Color) = (0.2, 0.2, 0.2, 1)
        
        [Header(Geometry)]
        _Density ("Density", Range(0.01, 1.0)) = 0.5
        _ShellThickness ("Shell Thickness", Range(0, 1)) = 0.5
        
        [Header(Triplanar Settings)]
        _TriplanarScale ("Triplanar Scale", Float) = 0.5
        _TriplanarBlendSharpness ("Triplanar Blend Sharpness", Range(1, 64)) = 10.0
        
        [Header(Parallax Settings)]
        _ParallaxStrength ("Parallax Strength", Range(0.0, 0.5)) = 0.1
        [IntRange] _ParallaxSteps ("Parallax Steps", Range(2, 128)) = 16
        [IntRange] _ParallaxRefinement ("Refinement Steps", Range(1, 8)) = 4
        
        [Header(Stylization)]
        [IntRange] _PixelTextureResolution ("Pixel Texture Resolution", Range(32, 512)) = 128
        
        [Header(Lighting)]
        _AmbientOcclusion ("Ambient Occlusion", Range(0, 1)) = 0.8
        _Brightness ("Brightness", Range(0.1, 2.0)) = 1.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }
        
        CGINCLUDE
        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "AutoLight.cginc"
        
        // Properties
        int _MaterialType;
        float4 _BaseColor;
        float4 _VariationColor;
        float _ColorVariationScale;
        
        float _Smoothness;
        float _SmoothnessVariation;
        float4 _SpecularColor;
        
        float _Density;
        float _ShellThickness;
        
        float _TriplanarScale;
        float _TriplanarBlendSharpness;
        
        float _ParallaxStrength;
        int _ParallaxSteps;
        int _ParallaxRefinement;
        
        int _PixelTextureResolution;
        
        float _AmbientOcclusion;
        float _Brightness;
        
        // --- NOISE FUNCTIONS ---
        
        float Hash21(float2 p)
        {
            p = frac(p * float2(234.34, 435.345));
            p += dot(p, p + 34.23);
            return frac(p.x * p.y);
        }

        float3 Hash23(float2 p)
        {
            float3 p3 = frac(float3(p.xyx) * float3(.1031, .1030, .0973));
            p3 += dot(p3, p3.yzx + 33.33);
            return frac((p3.xxy + p3.yzz) * p3.zyx);
        }
        
        float Noise2D(float2 uv)
        {
            float2 i = floor(uv);
            float2 f = frac(uv);
            
            float a = Hash21(i);
            float b = Hash21(i + float2(1.0, 0.0));
            float c = Hash21(i + float2(0.0, 1.0));
            float d = Hash21(i + float2(1.0, 1.0));
            
            float2 u = f * f * (3.0 - 2.0 * f);
            
            return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
        }
        
        float FBM(float2 uv, int octaves)
        {
            float value = 0.0;
            float amplitude = 0.5;
            float frequency = 1.0;
            float maxValue = 0.0;
            
            for (int i = 0; i < octaves; i++)
            {
                value += amplitude * Noise2D(uv * frequency);
                maxValue += amplitude;
                amplitude *= 0.5;
                frequency *= 2.0;
            }
            
            return value / maxValue;
        }

        // Voronoi / Cellular Noise for Rock
        float Voronoi(float2 uv, float angleOffset, float cellDensity)
        {
            float2 g = floor(uv * cellDensity);
            float2 f = frac(uv * cellDensity);
            float t = 8.0;
            float3 res = float3(8.0, 0.0, 0.0);

            for(int y = -1; y <= 1; y++)
            {
                for(int x = -1; x <= 1; x++)
                {
                    float2 lattice = float2(x,y);
                    float2 offset = Hash23(g + lattice).xy;
                    float d = distance(lattice + offset, f);
                    if(d < res.x)
                    {
                        res = float3(d, offset.x, offset.y);
                    }
                }
            }
            return res.x;
        }
        
        float2 PixelateUV(float2 uv, int pixelSize)
        {
            float size = (float)pixelSize;
            return floor(uv * size) / size;
        }
        
        float3 GetTriplanarWeights(float3 normalWS, float sharpness)
        {
            float3 weights = abs(normalWS);
            weights = pow(weights, sharpness);
            return weights / (weights.x + weights.y + weights.z);
        }
        
        // --- MATERIAL GENERATION ---
        
        float GetMaterialDensity(float2 uv, float height, int type)
        {
            float density = 0.0;
            
            if (type == 0) // Dirt
            {
                // Clumpy, noisy
                float noiseVal = FBM(uv * 4.0, 3);
                density = smoothstep(height, height + 0.2, noiseVal);
            }
            else if (type == 1) // Sand
            {
                // Simple high frequency noise, very dense
                float noiseVal = Noise2D(uv * 50.0);
                // Mix with lower freq for dunes/variation
                float dune = Noise2D(uv * 2.0);
                noiseVal = lerp(noiseVal, dune, 0.3);
                density = step(height, noiseVal * 0.8 + 0.2); // Usually fairly solid
            }
            else if (type == 2) // Rock
            {
                 // Voronoi for cracks/chips
                float v = Voronoi(uv, 0.0, 4.0);
                density = step(height, 1.0 - v); 
            }
            
            return density;
        }
        
        float3 GetMaterialColor(float3 worldPos, float2 uv, int type, float height)
        {
            // Random variation based on position
            float3 noisePos = worldPos * _ColorVariationScale;
            float variation = Noise2D(noisePos.xz + noisePos.y); // FBM(noisePos.xz, 2);
            
            float3 col = lerp(_BaseColor.rgb, _VariationColor.rgb, variation);
            
            // Add some per-pixel noise for texture
            float grain = Hash21(uv * 100.0);
            col += (grain - 0.5) * 0.05;
            
            // darken lower layers for pseudo-AO
            col *= lerp(0.5, 1.0, height);
            
            return col;
        }

        // --- PARALLAX ---
        
        // Output: mask (0 or 1), materialColor, finalHeight, randomSpecular
        float ParallaxShellSample(float3 worldPos, float3 normalWS, out float3 materialColor, out float finalHeight, out float randomSpec)
        {
            float3 weights = GetTriplanarWeights(normalWS, _TriplanarBlendSharpness);
            
            float3 viewDirWS = normalize(_WorldSpaceCameraPos - worldPos);
            float NdotV = dot(viewDirWS, normalWS);
            float rayLength = _ParallaxStrength / max(NdotV, 0.1);
            
            float3 parallaxOffsetStep = -viewDirWS * rayLength / (float)_ParallaxSteps;
            float3 currentPos = worldPos;
            
            float accumulatedMask = 0.0;
            materialColor = _BaseColor.rgb;
            finalHeight = 0.0;
            randomSpec = 0.0;
            
            // Specular randomness based on position
            float3 specNoisePos = worldPos * _ColorVariationScale * 2.0;
            randomSpec = Noise2D(specNoisePos.xz); 
            
            // Phase 1: March
            for (int i = 0; i < _ParallaxSteps; i++)
            {
                float shellIndex = (float)i / (float)_ParallaxSteps;
                float shellHeight = 1.0 - shellIndex;
                
                // Triplanar Sample
                float2 uvX = PixelateUV(currentPos.zy * _TriplanarScale, _PixelTextureResolution);
                float maskX = GetMaterialDensity(uvX, shellHeight, _MaterialType);
                
                float2 uvY = PixelateUV(currentPos.xz * _TriplanarScale, _PixelTextureResolution);
                float maskY = GetMaterialDensity(uvY, shellHeight, _MaterialType);
                
                float2 uvZ = PixelateUV(currentPos.xy * _TriplanarScale, _PixelTextureResolution);
                float maskZ = GetMaterialDensity(uvZ, shellHeight, _MaterialType);
                
                float blendedMask = maskX * weights.x + maskY * weights.y + maskZ * weights.z;
                
                if (blendedMask > 0.5)
                {
                    accumulatedMask = 1.0;
                    finalHeight = shellHeight;
                    
                    // Sample Color at this height/pos
                    // Use dominant plane UV for color sampling or blended? Blended is better.
                    float3 colX = GetMaterialColor(currentPos, uvX, _MaterialType, shellHeight);
                    float3 colY = GetMaterialColor(currentPos, uvY, _MaterialType, shellHeight);
                    float3 colZ = GetMaterialColor(currentPos, uvZ, _MaterialType, shellHeight);
                    
                    materialColor = colX * weights.x + colY * weights.y + colZ * weights.z;
                    break;
                }
                
                currentPos += parallaxOffsetStep;
            }
            
            if (accumulatedMask < 0.5)
            {
               // Hit nothing (below depth), show background/bottom
               materialColor = _VariationColor.rgb * 0.2; // very dark
               finalHeight = 0.0;
            }
            
            return accumulatedMask;
        }
        
        ENDCG
        
        // =============================================
        // Forward Base Pass
        // =============================================
        Pass
        {
            Name "ForwardBase"
            Tags { "LightMode" = "ForwardBase" }
            
            Cull Back
            ZWrite On
            ZTest LEqual
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fwdbase
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                SHADOW_COORDS(2)
                #ifdef DYNAMICLIGHTMAP_ON
                float2 dynamicLightmapUV : TEXCOORD3;
                #endif
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                o.pos = UnityObjectToClipPos(v.vertex);
                o.positionWS = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normalWS = UnityObjectToWorldNormal(v.normal);
                
                #ifdef DYNAMICLIGHTMAP_ON
                o.dynamicLightmapUV = v.uv2 * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
                #endif
                
                TRANSFER_SHADOW(o);
                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                float3 viewDirWS = normalize(_WorldSpaceCameraPos - i.positionWS);
                
                float3 matColor;
                float height;
                float rndSpec;
                float mask = ParallaxShellSample(i.positionWS, normalize(i.normalWS), matColor, height, rndSpec);
                
                float3 normalWS = normalize(i.normalWS);
                
                // --- PBR LIGHTING CALCULATION ---
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float3 H = normalize(L + viewDirWS);
                
                float NdotL = saturate(dot(normalWS, L));
                float NdotH = saturate(dot(normalWS, H));
                
                // Randomized Specular/Smoothness
                float smoothVal = clamp(_Smoothness + (rndSpec - 0.5) * _SmoothnessVariation * 2.0, 0.0, 1.0);
                float specPower = exp2(10.0 * smoothVal + 1.0);
                
                // Specular term (Blinn-Phong)
                float3 spec = _SpecularColor.rgb * pow(NdotH, specPower) * smoothVal;
                
                // Shadows
                float shadow = SHADOW_ATTENUATION(i);
                
                // Ambient (GI)
                float3 ambient = ShadeSH9(float4(normalWS, 1));
                #ifdef DYNAMICLIGHTMAP_ON
                float4 realtimeGI = UNITY_SAMPLE_TEX2D(unity_DynamicLightmap, i.dynamicLightmapUV);
                ambient += DecodeRealtimeLightmap(realtimeGI);
                #endif
                
                float3 finalColor = matColor * (ambient + NdotL * _LightColor0.rgb * shadow) + spec * NdotL * shadow * _LightColor0.rgb;
                
                // AO at bottom
                float ao = lerp(_AmbientOcclusion, 1.0, height);
                finalColor *= ao;
                
                finalColor *= _Brightness;
                
                return float4(finalColor, 1.0);
            }
            ENDCG
        }
        
        // =============================================
        // Forward Add Pass
        // =============================================
        Pass
        {
            Name "ForwardAdd"
            Tags { "LightMode" = "ForwardAdd" }
            
            Blend One One
            Cull Back
            ZWrite Off
            ZTest LEqual
            
            CGPROGRAM
            #pragma vertex vertAdd
            #pragma fragment fragAdd
            #pragma multi_compile_instancing
            #pragma multi_compile_fwdadd_fullshadows
            
            struct appdata_add
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f_add
            {
                float4 pos : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                LIGHTING_COORDS(2, 3)
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            v2f_add vertAdd(appdata_add v)
            {
                v2f_add o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.positionWS = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normalWS = UnityObjectToWorldNormal(v.normal);
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }
            
            float4 fragAdd(v2f_add i) : SV_Target
            {
                float3 viewDirWS = normalize(_WorldSpaceCameraPos - i.positionWS);
                 
                // Re-sample for consistancy (expensive, but necessary for multipass)
                float3 matColor;
                float height;
                float rndSpec;
                float mask = ParallaxShellSample(i.positionWS, normalize(i.normalWS), matColor, height, rndSpec);
                
                float3 normalWS = normalize(i.normalWS);
                
                float3 lightDir;
                #if defined(POINT) || defined(SPOT) || defined(POINT_COOKIE)
                    float3 lightToVertex = _WorldSpaceLightPos0.xyz - i.positionWS;
                    lightDir = normalize(lightToVertex);
                #else
                    lightDir = normalize(_WorldSpaceLightPos0.xyz);
                #endif
                
                UNITY_LIGHT_ATTENUATION(atten, i, i.positionWS);
                
                float NdotL = saturate(dot(normalWS, lightDir));
                
                // Specular
                float3 H = normalize(lightDir + viewDirWS);
                float NdotH = saturate(dot(normalWS, H));
                float smoothVal = clamp(_Smoothness + (rndSpec - 0.5) * _SmoothnessVariation * 2.0, 0.0, 1.0);
                float specPower = exp2(10.0 * smoothVal + 1.0);
                float3 spec = _SpecularColor.rgb * pow(NdotH, specPower) * smoothVal;
                
                float ao = lerp(_AmbientOcclusion, 1.0, height);
                
                float3 diff = matColor * NdotL;
                float3 finalColor = (diff + spec) * atten * _LightColor0.rgb * ao * _Brightness;
                
                return float4(finalColor, 1.0);
            }
            ENDCG
        }
        
        // =============================================
        // Shadow Caster Pass
        // =============================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            Cull Back
            ZWrite On
            ZTest LEqual
            
            CGPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_instancing
            #pragma multi_compile_shadowcaster
            
            struct appdata_shadow { float4 vertex : POSITION; float3 normal : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f_shadow { V2F_SHADOW_CASTER; UNITY_VERTEX_OUTPUT_STEREO };
            
            v2f_shadow vertShadow(appdata_shadow v)
            {
                v2f_shadow o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o);
                return o;
            }
            float4 fragShadow(v2f_shadow i) : SV_Target { SHADOW_CASTER_FRAGMENT(i); }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
