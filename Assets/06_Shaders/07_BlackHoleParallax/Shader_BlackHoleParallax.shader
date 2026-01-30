Shader "YmneShader/BlackHoleParallax"
{
    Properties
    {
        [Header(Main Settings)]
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Accretion Color", Color) = (1, 0.55, 0.1, 1)
        _InnerColor ("Inner Focus Color", Color) = (1, 0.9, 0.5, 1)
        
        [Header(Black Hole Dimensions)]
        _Radius ("Event Horizon Radius", Range(0, 0.5)) = 0.15
        _DiskWidth ("Accretion Disk Width", Range(0, 0.8)) = 0.35
        _Softness ("Disk Softness", Range(0.001, 1.0)) = 0.1
        
        [Header(Parallax)]
        _ParallaxStrength ("Star Depth", Range(-2, 2)) = 0.5
        _HoleParallax ("Hole Depth", Range(-1, 1)) = 0.1
        
        [Header(Lensing)]
        _LensingStrength ("Lensing Distortion", Range(0, 5)) = 0.5
        
        [Header(Animation)]
        _Speed ("Rotation Speed", Float) = 0.5
        _Twist ("Twist", Float) = 5.0
        
        [Header(Emission)]
        _EmissionPower ("Emission Intensity", Float) = 3.0
        
        [Header(Star Field)]
        _StarDensity ("Star Density", Float) = 50.0
        _StarSize ("Star Size", Range(0.001, 0.05)) = 0.01
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 viewDir : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            float4 _Color;
            float4 _InnerColor;
            float _Radius;
            float _DiskWidth;
            float _Softness;
            float _ParallaxStrength;
            float _HoleParallax;
            float _LensingStrength;
            float _Speed;
            float _Twist;
            float _EmissionPower;
            float _StarDensity;
            float _StarSize;

            // Simple pseudo-random
            float Hash21(float2 p) {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Simple noise
            float Noise(float2 p) {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float FractalNoise(float2 uv) {
                float n = 0.0;
                float div = 0.5;
                float scale = 1.0;
                for (int i = 0; i < 4; i++) {
                    n += Noise(uv * scale) * div;
                    scale *= 2.0;
                    div *= 0.5;
                }
                return n;
            }

            // Procedural Stars
            float StarLayer(float2 uv, float scale, float density) {
                float2 gv = frac(uv * scale) - 0.5;
                float2 id = floor(uv * scale);
                float n = Hash21(id);
                
                float star = 0;
                // Threshold based on density
                if (n < density / (scale * scale) * 10.0) {
                     float d = length(gv);
                     // Smooth soft star
                     float m = smoothstep(_StarSize, _StarSize * 0.2, d);
                     // Twinkle
                     float flash = sin(_Time.y * 2.0 + n * 100.0) * 0.5 + 0.5;
                     star = m * flash; // * (0.5 + 0.5 * n); // variation
                }
                return star;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                // Object Space View Direction: CameraPosObj - VertexObj
                float3 camPosObj = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz;
                o.viewDir = camPosObj - v.vertex.xyz; // normalized in frag for better interpolation
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Normalize view direction
                float3 viewDir = normalize(i.viewDir);
                
                // 1. Calculate Hole Position with Parallax
                // We want to simulate the hole being "behind" the quad surface surface.
                // Parallax Offset = ViewDir.xy * Depth
                float2 holeOffset = viewDir.xy * _HoleParallax;
                float2 uvCentered = i.uv - 0.5; // -0.5 to 0.5
                float2 uvHole = uvCentered + holeOffset;
                
                // Polar Coordinates relative to hole center
                float dist = length(uvHole);
                float angle = atan2(uvHole.y, uvHole.x);
                
                // 2. Gravitational Lensing / Distortion
                // Distortion gets stronger closer to radius
                // "Einstein Ring" effect
                // Offset the background UV lookup based on distance to hole
                // Simple radial distortion: pull UVs inwards or push outwards
                float lensParams = _LensingStrength * 0.05;
                // Avoid division by zero
                float distortion = lensParams / (dist + 0.05);
                float2 lensOffset = normalize(uvHole) * -distortion * smoothstep(0, _Radius * 2.0, dist);
                
                // 3. Background Stars with Parallax
                // Stars are "far away", so they move differently (usually less or more depending on reference)
                // If hole is at depth A, stars at depth B.
                float2 starParallax = viewDir.xy * _ParallaxStrength;
                float2 uvStars = uvCentered + starParallax + lensOffset;
                
                float stars = StarLayer(uvStars, 15.0, _StarDensity);
                stars += StarLayer(uvStars + float2(0.3, 0.7), 25.0, _StarDensity) * 0.6;
                stars += StarLayer(uvStars - float2(0.2, 0.1), 8.0, _StarDensity * 0.5) * 0.3;

                // 4. Accretion Disk
                // We want a swirling noise ring
                float rot = _Time.y * _Speed;
                // Twist the UVs for the disk logic
                // Spiral coordinate
                float spiral = angle + _Twist / (dist + 0.1); 
                float2 diskNoiseUV = float2(spiral, dist - _Time.y * _Speed * 0.2);
                
                float noiseVal = FractalNoise(float2(spiral * 3.0, dist * 10.0));
                noiseVal += FractalNoise(float2(spiral * 6.0 - _Time.y, dist * 20.0));
                noiseVal *= 0.5;
                
                // Radial Gradient for Disk
                // Start from Radius, fade out
                float diskMask = smoothstep(_Radius, _Radius + _Softness, dist);
                float diskFade = 1.0 - smoothstep(_Radius, _Radius + _DiskWidth, dist);
                
                float diskFinal = diskMask * diskFade * noiseVal;
                
                // Colorize
                float3 accretion = lerp(_Color.rgb, _InnerColor.rgb, noiseVal) * diskFinal * _EmissionPower;
                
                // 5. Event Horizon (Black Center)
                // Everything inside _Radius is black (absorbs mechanism)
                float horizon = smoothstep(_Radius, _Radius - 0.01, dist);
                
                // 6. Photon Ring (Bright Edge)
                float photonRing = smoothstep(_Radius + 0.01, _Radius, dist) - smoothstep(_Radius, _Radius - 0.002, dist);
                float3 ringColor = _InnerColor.rgb * photonRing * _EmissionPower * 2.0;
                
                // Composition
                // Background (Stars)
                float3 finalColor = stars;
                
                // Mask stars by Event Horizon
                finalColor *= (1.0 - horizon);
                
                // Add Accretion Disk (Additive)
                finalColor += accretion;
                
                // Add Photon Ring
                finalColor += ringColor;
                
                return fixed4(finalColor, 1.0);
            }
            ENDCG
        }
    }
}
