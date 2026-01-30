#ifndef HLSL_OCEAN_WATER_INCLUDED
#define HLSL_OCEAN_WATER_INCLUDED

#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "AutoLight.cginc"

// ============================================
// VARIABLES
// ============================================
float _WaterTiling;
float _WaterHeightScale;

float4 _ShallowColor;
float4 _DeepColor;
float4 _FoamColor;
float _DepthFade;

float _WaveHeight;
float _WaveFrequency;
float _WaveSpeed;
float4 _WaveDirection;

int _ToonSteps;
float _ShadowSoftness;
float4 _ShadowColor;

float _SpecularSize;
float _SpecularIntensity;
float _SpecularSmoothness;

float _FresnelPower;
float _FresnelIntensity;
float4 _FresnelColor;

float _FoamThreshold;
float _FoamIntensity;
float _CrestThreshold;

float _Opacity;
float _OpacityIntensity;

// Sparkle Variables
float _SparkleIntensity;
float _SparkleScale;
float _SparkleSpeed;
float _SparkleDensity;
float _SparkleThreshold;
float _SparkleBloom;
float4 _SparkleColor;

// Depth texture
UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

// SSR Variables (Cheap Fake Planar Reflection)
float _SSRIntensity;
float _SSRDistortion;
float _SSRStretch;
float _SSRFlip;
float _SSRBlur;
float _SSRFadeDistance;

// Caustics Variables
float _CausticsIntensity1, _CausticsScale1, _CausticsSpeed1, _CausticsContrast1, _CausticsWarpStrength1, _CausticsWarpScale1;
float _CausticsIntensity2, _CausticsScale2, _CausticsSpeed2, _CausticsContrast2, _CausticsWarpStrength2, _CausticsWarpScale2;
float _CausticsChromAb;

// Screen grab texture for SSR
sampler2D _GrabTexture;
float4 _GrabTexture_TexelSize;

// Custom Captured Screen Space Shadow Map
sampler2D _GlobalScreenSpaceShadowMap;

// Fallback for SHADOW_COORDS if not defined (e.g. in ShadowCaster pass)
#ifndef SHADOW_COORDS
    #define SHADOW_COORDS(x)
#endif

#ifndef TRANSFER_SHADOW
    #define TRANSFER_SHADOW(x)
#endif

#ifndef SHADOW_ATTENUATION
    #define SHADOW_ATTENUATION(x) 1.0
#endif

#ifndef UNITY_LIGHT_ATTENUATION
    #define UNITY_LIGHT_ATTENUATION(dest, input, worldPos) dest = 1.0
#endif

// ============================================
// STYLIZED OCEAN WATER - Shared Functions
// ============================================

// --- Hash Functions ---
float Hash21(float2 p)
{
    p = frac(p * float2(234.34, 435.345));
    p += dot(p, p + 34.23);
    return frac(p.x * p.y);
}

float2 Hash22(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * float3(.1031, .1030, .0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.xx + p3.yz) * p3.zy);
}

// --- Noise Functions ---
float Noise2D(float2 uv)
{
    float2 i = floor(uv);
    float2 f = frac(uv);
    
    float a = Hash21(i);
    float b = Hash21(i + float2(1.0, 0.0));
    float c = Hash21(i + float2(0.0, 1.0));
    float d = Hash21(i + float2(1.0, 1.0));
    
    // Smooth interpolation
    float2 u = f * f * (3.0 - 2.0 * f);
    
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

// --- Helper Functions ---
float2 Rotate2D(float2 uv, float angle)
{
    float s = sin(angle);
    float c = cos(angle);
    return float2(uv.x * c - uv.y * s, uv.x * s + uv.y * c);
}

// --- Caustics Functions (Worley Noise) ---
float WorleyNoise(float2 uv, float time, float contrast) 
{ 
    float2 gv = frac(uv) - 0.5;
    float2 id = floor(uv); 
    float minDist1 = 1.0, minDist2 = 1.0;
    
    [unroll]
    for (int y = -1; y <= 1; y++) { 
        [unroll]
        for (int x = -1; x <= 1; x++) { 
            float2 offs = float2(x, y);
            float2 h = Hash22(id + offs); 
            float2 r = offs - gv + (h * 0.5 + 0.25 * sin(time + h * 5.0));
            float dist = dot(r, r); 
            if (dist < minDist1) {minDist2 = minDist1; minDist1 = dist;} 
            else if (dist < minDist2) {minDist2 = dist;} 
        } 
    } 
    return pow(saturate(1.0 - (sqrt(minDist2) - sqrt(minDist1))), contrast);
}

float GetCausticsLayer(float2 worldPosXZ, float time, float scale, float speed, float contrast, float warpStrength, float warpScale) 
{ 
    // Reuse Noise2D for value noise warping
    float warpModulation = Noise2D(worldPosXZ * warpScale * 0.5 + time * 0.05) * 0.5 + 0.5;
    float2 warp_uv = worldPosXZ * warpScale + time * 0.1; 
    float2 warp_offset = float2(Noise2D(warp_uv), Noise2D(warp_uv + float2(5.2, 1.3)));
    float2 uv = worldPosXZ + (warp_offset * 2.0 - 1.0) * warpStrength * warpModulation;
    return WorleyNoise(uv * scale, time * speed, contrast); 
}



// --- Gerstner Wave ---
// Single Gerstner wave calculation
float3 GerstnerWave(
    float2 position,        // World XZ position
    float2 direction,       // Normalized wave direction
    float steepness,        // Wave steepness (0-1)
    float wavelength,       // Distance between crests
    float speed,            // Wave animation speed
    float time,             // Current time
    inout float3 tangent,   // Output tangent
    inout float3 binormal   // Output binormal
)
{
    float k = 2.0 * 3.14159265 / wavelength;
    float c = sqrt(9.8 / k); // Wave speed from dispersion relation
    float2 d = normalize(direction);
    float f = k * (dot(d, position) - c * speed * time);
    float a = steepness / k;
    
    // Gerstner displacement
    float3 displacement;
    displacement.x = d.x * a * cos(f);
    displacement.y = a * sin(f);
    displacement.z = d.y * a * cos(f);
    
    // Accumulate tangent/binormal for normal calculation
    tangent += float3(
        -d.x * d.x * steepness * sin(f),
        d.x * steepness * cos(f),
        -d.x * d.y * steepness * sin(f)
    );
    
    binormal += float3(
        -d.x * d.y * steepness * sin(f),
        d.y * steepness * cos(f),
        -d.y * d.y * steepness * sin(f)
    );
    
    return displacement;
}

// Multi-wave Gerstner displacement with randomized variations
void CalculateGerstnerWaves(
    float2 worldXZ,
    float waveHeight,
    float waveFrequency,
    float waveSpeed,
    float2 waveDirection,
    float time,
    out float3 displacement,
    out float3 normal
)
{
    float3 tangent = float3(1, 0, 0);
    float3 binormal = float3(0, 0, 1);
    displacement = float3(0, 0, 0);
    
    // Apply water tiling to world position
    float2 tiledXZ = worldXZ * _WaterTiling;
    
    // Wave 1 - Primary direction (Main Swell)
    float2 dir1 = normalize(waveDirection);
    displacement += GerstnerWave(tiledXZ, dir1, 0.25 * waveHeight, 20.0 / waveFrequency, waveSpeed, time, tangent, binormal);
    
    // Wave 2 - Offset direction (Secondary Swell)
    float2 dir2 = normalize(waveDirection + float2(0.4, -0.3));
    displacement += GerstnerWave(tiledXZ, dir2, 0.2 * waveHeight, 13.0 / waveFrequency, waveSpeed * 1.1, time, tangent, binormal);
    
    // Wave 3 - Counter direction (Choppiness)
    float2 dir3 = normalize(float2(-waveDirection.y, waveDirection.x) * 0.7 + waveDirection);
    displacement += GerstnerWave(tiledXZ, dir3, 0.15 * waveHeight, 7.0 / waveFrequency, waveSpeed * 0.9, time, tangent, binormal);
    
    // Wave 4 - Detail ripples
    float2 dir4 = normalize(waveDirection + float2(-0.5, 0.6));
    displacement += GerstnerWave(tiledXZ, dir4, 0.1 * waveHeight, 3.0 / waveFrequency, waveSpeed * 1.4, time, tangent, binormal);
    
    // Wave 5 - Extra variation layer
    float2 dir5 = normalize(dir1 + float2(0.2, 0.2));
    displacement += GerstnerWave(tiledXZ, dir5, 0.08 * waveHeight, 1.7 / waveFrequency, waveSpeed * 1.7, time, tangent, binormal);

    // Calculate normal from tangent/binormal
    normal = normalize(cross(binormal, tangent));
}

// --- Toon Shading ---
float ToonLighting(float NdotL, int steps, float shadowSoftness)
{
    // Remap NdotL from [-1,1] to [0,1]
    float lighting = saturate(NdotL);
    
    // Apply softness before quantization
    lighting = smoothstep(0.0, shadowSoftness, lighting);
    
    // Quantize into steps
    float stepsF = (float)steps;
    return floor(lighting * stepsF) / (stepsF - 1.0);
}

// --- Stylized Specular ---
float ToonSpecular(float3 viewDir, float3 lightDir, float3 normal, float size, float smoothness)
{
    float3 halfDir = normalize(viewDir + lightDir);
    float NdotH = saturate(dot(normal, halfDir));
    
    // Hard edge specular
    float spec = pow(NdotH, size * 100.0);
    return smoothstep(0.5 - smoothness, 0.5 + smoothness, spec);
}

// --- Fresnel Effect ---
float FresnelEffect(float3 viewDir, float3 normal, float power)
{
    float NdotV = saturate(dot(normal, viewDir));
    return pow(1.0 - NdotV, power);
}

// --- Anime Sparkles ---
// Adds glittering stars/sparkles based on view and light direction
float3 CalculateAnimeSparkles(float3 viewDir, float3 lightDir, float3 normal, float3 worldPos, float time)
{
    float3 halfDir = normalize(viewDir + lightDir);
    float NdotH = saturate(dot(normal, halfDir));
    
    // 1. Specular Mask
    // Use specular size to control spread
    float specPower = 5.0 / max(_SpecularSize, 0.05);
    float specMask = pow(NdotH, specPower); 
    
    // Use _SparkleThreshold to control the cutoff
    specMask = smoothstep(_SparkleThreshold, _SparkleThreshold + 0.2, specMask);
    
    // Bloom Mask (Center boost)
    float bloomProx = pow(NdotH, 100.0); 
    float bloomFactor = 1.0 + (bloomProx * _SparkleBloom);
    
    // 2. Animated Pattern with 3x3 Search (No Grid Artifacts)
    // Apply water tiling to sparkle coordinates
    float2 sparkleUV = worldPos.xz * _WaterTiling * _SparkleScale;
    float2 id = floor(sparkleUV);
    float2 uv0 = frac(sparkleUV); // 0..1 in center cell

    float3 totalSparkles = 0;
    
    // Billboarding Vectors
    float viewAngle = saturate(dot(viewDir, float3(0, 1, 0)));
    viewAngle = max(viewAngle, 0.2);
    float2 viewPlaneDir = normalize(viewDir.xz + 0.0001);
    float2 viewRight = float2(viewPlaneDir.y, -viewPlaneDir.x);
    
    // Check 3x3 neighbors to allow sparkles to cross cell boundaries
    // This removes the "Grid" look and allows random clustering
    [unroll]
    for (int y = -1; y <= 1; y++) 
    {
        [unroll]
        for (int x = -1; x <= 1; x++) 
        {
            float2 neighbor = float2(x, y);
            float2 cellID = id + neighbor;
            
            // Random properties for this neighbor's star
            float rnd = Hash21(cellID);
            
            // Random culling (sparseness)
            // Use _SparkleDensity to control how many stars appear
            if (rnd > _SparkleDensity) continue;
            
            // Random position in cell
            // Jitter range increased to fill space better
            float2 jitter = (float2(Hash21(cellID * 2.54), Hash21(cellID * 1.32)) - 0.5); 
            
            // Vector from current pixel to this neighbor's star
            float2 f = neighbor + jitter + 0.5 - uv0; 
            
            // --- Apply Billboarding to this vector ---
            float2 f_billboard;
            f_billboard.x = dot(f, viewRight);
            f_billboard.y = dot(f, viewPlaneDir);
            f_billboard.y *= viewAngle; // Perspective correction
            
            // --- Star Shape ---
            float dist = length(f_billboard);
            
            // Animation phase
            float anim = sin(time * _SparkleSpeed * (0.8 + rnd * 0.4) + rnd * 12.0) * 0.5 + 0.5;
            
            // Random Size Variation
            // Generate a secondary random value for size
            float sizeRnd = frac(rnd * 45.67);
            float randomScale = 0.5 + sizeRnd * 1.0;
            
            // Apply scale to thinness (cross) and sizeVal (glow)
            float thinness = 0.02 * (0.5 + anim) * randomScale;
            float crossShape = thinness / (abs(f_billboard.x * f_billboard.y) + 0.0001);
            
            float sizeVal = (0.4 * anim + 0.1) * randomScale;
            float glow = smoothstep(sizeVal, 0.0, dist);
            float star = crossShape * glow;
            
            float sparkleVal = smoothstep(0.5, 1.0, star);
            
            // Add to total
            totalSparkles += _SparkleColor.rgb * sparkleVal * sparkleVal; // Square for sharper HDR look
        }
    }
    
    return totalSparkles * specMask * _SparkleIntensity * bloomFactor;
}


// ============================================
// SCREEN SPACE RAYMARCHING SSR
// ============================================

// Additional SSR params
float _SSRStepSize;
float _SSRThickness;
float _SSRSamples; // New

float3 CalculateCheapSSR(float4 screenPos, float3 viewDir, float3 normal, float3 worldPos)
{
    // 1. Setup
    float2 screenUV = screenPos.xy / screenPos.w;
    float3 reflectionDir = reflect(-viewDir, normal);
    
    // We need to define the ray in Screen Space.
    // Calculate a target point in world space and project it to screen space.
    
    // Target Setup
    // Scale search distance by Step Size to give control over "Length" vs "Precision"
    // Larger Step Size = Further search, but coarser steps.
    float searchDist = 20.0 * _SSRStepSize; 
    float3 targetWorldPos = worldPos + reflectionDir * searchDist;
    float4 targetClipPos = mul(UNITY_MATRIX_VP, float4(targetWorldPos, 1.0));
    float3 targetScreenPos = targetClipPos.xyz / targetClipPos.w;
    
    // Convert clip space XY (-1..1) to UV space (0..1)
    float2 targetUV = targetScreenPos.xy * 0.5 + 0.5;
    
    // Handle platform UVs
    #if UNITY_UV_STARTS_AT_TOP
    if (_ProjectionParams.x < 0)
        targetUV.y = 1.0 - targetUV.y;
    #endif
    
    // Limit step size to avoid overstepping too fast or too slow
    // We normalize the step so 1 step moves roughly _SSRStepSize pixels (ish)?
    // Let's just user fixed step count and distribute them.
    
    // Calculate vector from current pixel to target
    float2 uvDir = targetUV - screenUV;

    int steps = (int)_SSRSamples;
    float2 step = uvDir / (float)steps;
    
    // Jitter (Randomize start position slightly to hide banding)
    float jitter = Hash21(screenUV * _Time.y + viewDir.xy);
    float2 currentUV = screenUV + step * (jitter * 0.5);
    
    // Track current ray depth (Linear 0..1)
    // We need start depth (surface) and end depth (target)
    
    float startDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV));
    
    // View Space Z = mul(UNITY_MATRIX_V, worldPos).z
    // LinearEyeDepth is basically -ViewZ.
    float startViewDepth = -mul(UNITY_MATRIX_V, float4(worldPos, 1.0)).z;
    float endViewDepth = -mul(UNITY_MATRIX_V, float4(targetWorldPos, 1.0)).z;
    
    float depthStep = (endViewDepth - startViewDepth) / (float)steps;
    float currentRayDepth = startViewDepth + depthStep * (jitter * 0.5);
    
    // Marching Loop
    [loop] // Hint to compiler to use dynamic branching instead of unrolling
    for (int i = 0; i < 64; i++)
    {
        if (i >= steps) break;
        currentUV += step;
        currentRayDepth += depthStep;
        
        // Bounds Check
        if (currentUV.x < 0 || currentUV.x > 1 || currentUV.y < 0 || currentUV.y > 1) break;
        
        // Sample Scene Depth
        float sceneRawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, currentUV);
        float sceneDepth = LinearEyeDepth(sceneRawDepth);
        
        // Intersection Check
        if (currentRayDepth > sceneDepth)
        {
            // Potential hit using depth buffer
            float diff = currentRayDepth - sceneDepth;
            
            // Thickness approximation
            float thickness = 1.5; // Roughly 1.5m thickness
            
            if (diff < thickness)
            {
                 
                 // Distance Fade (screen edges)
                 float2 edgeDist = abs(currentUV * 2 - 1);
                 float edgeFade = 1.0 - saturate(max(edgeDist.x, edgeDist.y));
                 edgeFade = smoothstep(0.0, 0.2, edgeFade);
                 
                 // Fade from Camera Distance (Old param support)
                 float distanceFromCamera = length(worldPos - _WorldSpaceCameraPos);
                 float camFade = 1.0 - saturate(distanceFromCamera / _SSRFadeDistance);
                 
                 // Sample color with cheap blur support
                 float3 reflColor = 0;
                 if (_SSRBlur > 0.0001)
                 {
                     // 4-tap blur
                     reflColor += tex2D(_GrabTexture, currentUV + float2(_SSRBlur, 0)).rgb;
                     reflColor += tex2D(_GrabTexture, currentUV + float2(-_SSRBlur, 0)).rgb;
                     reflColor += tex2D(_GrabTexture, currentUV + float2(0, _SSRBlur)).rgb;
                     reflColor += tex2D(_GrabTexture, currentUV + float2(0, -_SSRBlur)).rgb;
                     reflColor *= 0.25;
                 }
                 else
                 {
                     reflColor = tex2D(_GrabTexture, currentUV).rgb;
                 }
                 
                 return reflColor * _SSRIntensity * edgeFade * camFade;
            }
        }
    }
    
    return 0;
}



// ============================================
// STRUCTS
// ============================================

struct appdata
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 worldPos : TEXCOORD1;
    float3 worldNormal : TEXCOORD2;
    float3 viewDir : TEXCOORD3;
    float4 screenPos : TEXCOORD4;
    SHADOW_COORDS(5)
    float waveHeight : TEXCOORD6; // Pass wave height for crest foam
    UNITY_VERTEX_OUTPUT_STEREO
};

struct appdata_add
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f_add
{
    float4 pos : SV_POSITION;
    float3 worldPos : TEXCOORD0;
    float3 worldNormal : TEXCOORD1;
    float3 viewDir : TEXCOORD2;
    LIGHTING_COORDS(3, 4)
    UNITY_VERTEX_OUTPUT_STEREO
};

struct appdata_shadow
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f_shadow
{
    V2F_SHADOW_CASTER;
    UNITY_VERTEX_OUTPUT_STEREO
};

// ============================================
// VERTEX SHADERS
// ============================================

v2f vert(appdata v)
{
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    
    // Get world position before displacement
    float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    
    // Calculate Gerstner wave displacement
    float3 displacement;
    float3 waveNormal;
    CalculateGerstnerWaves(
        worldPos.xz,
        _WaveHeight,
        _WaveFrequency,
        _WaveSpeed,
        _WaveDirection,
        _Time.y,
        displacement,
        waveNormal
    );
    
    // Pass wave height (displacement Y) to fragment
    o.waveHeight = displacement.y;
    
    // Apply displacement with height scale (flattens or exaggerates Y)
    displacement.y *= _WaterHeightScale;
    worldPos += displacement;
    
    // Transform back to object space for clip position
    float4 localPos = mul(unity_WorldToObject, float4(worldPos, 1.0));
    o.pos = UnityObjectToClipPos(localPos);
    
    o.worldPos = worldPos;
    o.worldNormal = waveNormal;
    o.viewDir = normalize(_WorldSpaceCameraPos - worldPos);
    // Standard UVs from mesh for tiling if needed
    o.uv = v.uv; 
    o.screenPos = ComputeScreenPos(o.pos);
    
    TRANSFER_SHADOW(o);
    
    return o;
}

v2f_add vertAdd(appdata_add v)
{
    v2f_add o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    
    float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    
    // Gerstner waves
    float3 displacement;
    float3 waveNormal;
    CalculateGerstnerWaves(
        worldPos.xz,
        _WaveHeight,
        _WaveFrequency,
        _WaveSpeed,
        _WaveDirection,
        _Time.y,
        displacement,
        waveNormal
    );
    // Apply displacement with height scale
    displacement.y *= _WaterHeightScale;
    worldPos += displacement;
    
    float4 localPos = mul(unity_WorldToObject, float4(worldPos, 1.0));
    o.pos = UnityObjectToClipPos(localPos);
    
    o.worldPos = worldPos;
    o.worldNormal = waveNormal;
    o.viewDir = normalize(_WorldSpaceCameraPos - worldPos);
    
    TRANSFER_VERTEX_TO_FRAGMENT(o);
    
    return o;
}

v2f_shadow vertShadow(appdata_shadow v)
{
    v2f_shadow o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    
    // Apply wave displacement for accurate shadows
    float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    
    float3 displacement;
    float3 waveNormal;
    CalculateGerstnerWaves(
        worldPos.xz,
        _WaveHeight,
        _WaveFrequency,
        _WaveSpeed,
        _WaveDirection,
        _Time.y,
        displacement,
        waveNormal
    );
    // Apply displacement with height scale
    displacement.y *= _WaterHeightScale;
    worldPos += displacement;
    v.vertex = mul(unity_WorldToObject, float4(worldPos, 1.0));
    v.normal = mul((float3x3)unity_WorldToObject, waveNormal);
    
    TRANSFER_SHADOW_CASTER_NORMALOFFSET(o);
    return o;
}

// ============================================
// FRAGMENT SHADERS
// ============================================

float4 frag(v2f i) : SV_Target
{
    float3 N = normalize(i.worldNormal);
    float3 V = normalize(i.viewDir);
    float3 L = normalize(_WorldSpaceLightPos0.xyz);
    
    // === DEPTH-BASED EFFECTS ===
    float2 screenUV = i.screenPos.xy / i.screenPos.w;
    float sceneDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV));
    float surfaceDepth = i.screenPos.w;
    float waterDepth = sceneDepth - surfaceDepth;
    
    // Depth factor for shallow/deep color
    float depthFactor = saturate(waterDepth / _DepthFade);
    
    // Base water color (shallow to deep gradient)
    float3 waterColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthFactor);
    
    // === TOON LIGHTING ===
    float NdotL = dot(N, L);
    float toonLight = ToonLighting(NdotL, _ToonSteps, _ShadowSoftness);
    
    // Shadow & Attenuation
    UNITY_LIGHT_ATTENUATION(shadowAtten, i, i.worldPos);
    
    // === MANUAL SCREEN SPACE SHADOW READING ===
    #if defined(_RECEIVE_SHADOWS_ON)
        float2 shadowUV = i.screenPos.xy / i.screenPos.w;
        
        // Basic bounds check
        if (shadowUV.x >= 0 && shadowUV.x <= 1 && shadowUV.y >= 0 && shadowUV.y <= 1)
        {
             // Sample our custom captured shadow map
             fixed screenShadow = tex2D(_GlobalScreenSpaceShadowMap, shadowUV).r;
             
             // Apply to standard attenuation
             shadowAtten *= screenShadow;
        }
    #endif

    float shadowFactor = shadowAtten;
    toonLight *= shadowFactor;
    
    // Apply toon shading
    float3 litColor = lerp(_ShadowColor.rgb * waterColor, waterColor, toonLight);
    
    // Main light contribution
    float3 finalColor = litColor * _LightColor0.rgb;
    
    // Ambient
    float3 ambient = ShadeSH9(float4(N, 1)) * waterColor * 0.5;
    finalColor += ambient;
    
    // === SPECULAR ===
    float spec = ToonSpecular(V, L, N, _SpecularSize, _SpecularSmoothness);
    finalColor += spec * _SpecularIntensity * _LightColor0.rgb;
    
    // === ANIME SPARKLES ===
    // Only apply sparkles from main directional light
    float3 sparkles = CalculateAnimeSparkles(V, L, N, i.worldPos, _Time.y);
    // Mask sparkles by shadow so they don't appear in shadows
    sparkles *= shadowFactor;
    finalColor += sparkles;
    
    // === FRESNEL ===
    float fresnel = FresnelEffect(V, N, _FresnelPower);
    finalColor = lerp(finalColor, _FresnelColor.rgb, fresnel * _FresnelIntensity);
    
    // === SCREEN SPACE REFLECTIONS ===
    #if defined(_SSR_ON)
    float3 ssrColor = CalculateCheapSSR(i.screenPos, V, N, i.worldPos);
    finalColor += ssrColor;
    #endif
    
    // === FOAM (FLAT & STYLIZED) ===
    #if defined(_FOAM_ON)
    // 1. Edge Foam: Detect shallow water
    float edgeFoam = 1.0 - smoothstep(0.0, _FoamThreshold, waterDepth);
    
    // 2. Crest Foam: Detect wave peaks
    float crestThresholdVal = _WaveHeight * _CrestThreshold; 
    float crestFoam = smoothstep(crestThresholdVal, _WaveHeight, i.waveHeight);
    
    // Combine foam
    float foamMask = saturate(edgeFoam + crestFoam);
    
    // Sharpen foam mask
    float foamCutoff = 0.5;
    foamMask = smoothstep(foamCutoff - 0.05, foamCutoff + 0.05, foamMask);
    
    // Apply foam
    finalColor = lerp(finalColor, _FoamColor.rgb, foamMask * _FoamIntensity);
    #endif
    
    // === OPACITY ===
    float alpha = lerp(_ShallowColor.a, _DeepColor.a, depthFactor);
    // Apply transparency intensity scaler
    alpha = saturate(alpha * _OpacityIntensity);
    
    alpha = lerp(alpha, 1.0, fresnel * 0.3); // More opaque at edges
    alpha *= _Opacity;
    
    #if defined(_FOAM_ON)
    alpha = lerp(alpha, 1.0, foamMask * _FoamIntensity); // Foam is opaque
    #endif
    
    #if defined(_DISABLE_TRANSPARENCY)
    alpha = 1.0;
    #endif
    
    // === CAUSTICS ===
    // Applied last, additively, masked by shadow and depth
    #if defined(_CAUSTICS_ON)
        // Only show if there is water depth (underwater)
        if (depthFactor > 0.01)
        {
             float3 viewDirVec = normalize(i.viewDir);
             float3 refractedRay = refract(-viewDirVec, N, 1.0 / 1.33); 
             if (length(refractedRay) == 0) refractedRay = -viewDirVec;
             
             // Project to bottom
             float3 underwaterWorldPos = i.worldPos + refractedRay * waterDepth;
             
             // Fade near surface so it doesn't pop
             float causticsFade = saturate(1.0 - (waterDepth * 0.2));
             
             // Setup RGB offsets for Chromatic Aberration
             // We offset the world position sampling slightly for R and B channels
             float2 offsetR = float2(_CausticsChromAb, 0) * 0.05 * (1.0 + waterDepth * 0.1);
             float2 offsetB = -offsetR;
             
             // Layer 1 (RGB)
             float c1_r = GetCausticsLayer(underwaterWorldPos.xz + offsetR, _Time.y, _CausticsScale1, _CausticsSpeed1, _CausticsContrast1, _CausticsWarpStrength1, _CausticsWarpScale1);
             float c1_g = GetCausticsLayer(underwaterWorldPos.xz,           _Time.y, _CausticsScale1, _CausticsSpeed1, _CausticsContrast1, _CausticsWarpStrength1, _CausticsWarpScale1);
             float c1_b = GetCausticsLayer(underwaterWorldPos.xz + offsetB, _Time.y, _CausticsScale1, _CausticsSpeed1, _CausticsContrast1, _CausticsWarpStrength1, _CausticsWarpScale1);
             float3 c1 = float3(c1_r, c1_g, c1_b) * _CausticsIntensity1;

             // Layer 2 (RGB) with rotation and offsets
             float2 rotatedUV = Rotate2D(underwaterWorldPos.xz, 0.5); 
             // We need to rotate the offsets too to match direction, or just adding raw offsets is fine for "aberration" feel :D
             
             float c2_r = GetCausticsLayer(rotatedUV + offsetR, _Time.y, _CausticsScale2, _CausticsSpeed2, _CausticsContrast2, _CausticsWarpStrength2, _CausticsWarpScale2);
             float c2_g = GetCausticsLayer(rotatedUV,           _Time.y, _CausticsScale2, _CausticsSpeed2, _CausticsContrast2, _CausticsWarpStrength2, _CausticsWarpScale2);
             float c2_b = GetCausticsLayer(rotatedUV + offsetB, _Time.y, _CausticsScale2, _CausticsSpeed2, _CausticsContrast2, _CausticsWarpStrength2, _CausticsWarpScale2);
             float3 c2 = float3(c2_r, c2_g, c2_b) * _CausticsIntensity2;
             
             // Combine and mask by shadow/attenuation
             float3 caustics = (c1 + c2) * _LightColor0.rgb * causticsFade * shadowFactor; 
             
             finalColor += caustics;
        }
    #endif

    return float4(finalColor, alpha);
}

float4 fragAdd(v2f_add i) : SV_Target
{
    float3 N = normalize(i.worldNormal);
    float3 V = normalize(i.viewDir);
    
    // Light direction
    float3 lightDir;
    #if defined(POINT) || defined(SPOT) || defined(POINT_COOKIE)
        lightDir = normalize(_WorldSpaceLightPos0.xyz - i.worldPos);
    #else
        lightDir = normalize(_WorldSpaceLightPos0.xyz);
    #endif
    
    // Attenuation
    UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
    
    // Toon lighting
    float NdotL = dot(N, lightDir);
    float toonLight = ToonLighting(NdotL, _ToonSteps, _ShadowSoftness);
    
    float3 addColor = _ShallowColor.rgb * toonLight * atten * _LightColor0.rgb;
    
    // Specular
    float spec = ToonSpecular(V, lightDir, N, _SpecularSize, _SpecularSmoothness);
    addColor += spec * _SpecularIntensity * atten * _LightColor0.rgb;
    
    // Sparkles
    float3 sparkles = CalculateAnimeSparkles(V, lightDir, N, i.worldPos, _Time.y);
    addColor += sparkles * atten;
    
    return float4(addColor, 1.0);
}

float4 fragShadow(v2f_shadow i) : SV_Target
{
    SHADOW_CASTER_FRAGMENT(i);
}

#endif
