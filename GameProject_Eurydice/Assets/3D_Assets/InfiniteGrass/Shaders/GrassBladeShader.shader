Shader "InfiniteGrass/GrassBladeShader"
{

    Properties
    {
        [MainTexture] _BaseColorTexture("BaseColor Texture", 2D) = "white" {}
        _ColorA("ColorA", Color) = (0,0,0,1)
        _ColorB("ColorB", Color) = (1,1,1,1)
        _AOColor("AO Color", Color) = (0.5,0.5,0.5)

        [Header(Grass Shape)][Space]
        _GrassWidth("Grass Width", Float) = 1
        _GrassHeight("Grass Height", Float) = 1
        _GrassWidthRandomness("Grass Width Randomness", Range(0, 1)) = 0.25
        _GrassHeightRandomness("Grass Height Randomness", Range(0, 1)) = 0.5

        _GrassCurving("Grass Curving", Float) = 0.1
        [Space]
        _ExpandDistantGrassWidth("Expand Distant Grass Width", Float) = 1
        _ExpandDistantGrassRange("Expand Distant Grass Range", Vector) = (50, 200, 0, 0)

        [Header(Wind)][Space]
        _WindTexture("Wind Texture", 2D) = "white" {}
        _WindScroll("Wind Scroll", Vector) = (1, 1, 0, 0)
        _WindStrength("Wind Strength", Float) = 1

        [Header(Lighting)][Space]
        _RandomNormal("Random Normal", Range(0, 1)) = 0.1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue"="Geometry"}

        Pass
        {
            Cull Off
            ZTest Less
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                half3 normalWS     : TEXCOORD1;
                half3 albedo       : TEXCOORD2;
                half3 sh           : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
                float fogFactor    : TEXCOORD5;
                half colorMask     : TEXCOORD6;
                half posY          : TEXCOORD7;
            };

            CBUFFER_START(UnityPerMaterial)
                half3 _ColorA;
                half3 _ColorB;
                float4 _BaseColorTexture_ST;
                half3 _AOColor;

                float _GrassWidth;
                float _GrassHeight;
                float _GrassCurving;
                float _GrassWidthRandomness;
                float _GrassHeightRandomness;

                float _ExpandDistantGrassWidth;
                float2 _ExpandDistantGrassRange;

                float4 _WindTexture_ST;
                float _WindStrength;
                float2 _WindScroll;

                half _RandomNormal;
            CBUFFER_END

            float2 _CenterPos;
            float _GrassDataRadius;
            float _GrassDrawDistance;
            float2 _LocalSlopeCenterPos;
            float _LocalSlopeRadius;
            float _HasLocalSlopeRT;
            float _HasGlobalSlopeRT;
            float _HasColorRT;
            float2 _SlopeBoundsYMinMax;
            float _SlopeVerticalTolerance;

            StructuredBuffer<float3> _GrassPositions;

            TEXTURE2D(_BaseColorTexture);
            SAMPLER(sampler_BaseColorTexture);
            TEXTURE2D(_WindTexture);
            SAMPLER(sampler_WindTexture);

            TEXTURE2D(_GrassColorRT);
            SAMPLER(sampler_GrassColorRT);
            TEXTURE2D(_GrassSlopeRT);
            SAMPLER(sampler_GrassSlopeRT);
            TEXTURE2D(_GrassLocalSlopeRT);
            SAMPLER(sampler_GrassLocalSlopeRT);

            half3 ApplySingleDirectLight(Light light, half3 N, half3 V, half3 albedo, half mask, half positionY)
            {
                half3 H = normalize(light.direction + V);

                half directDiffuse = dot(N, light.direction) * 0.5 + 0.5;

                float directSpecular = saturate(dot(N, H));
                directSpecular *= directSpecular;
                directSpecular *= directSpecular;
                directSpecular *= directSpecular;
                directSpecular *= directSpecular;
                directSpecular *= positionY * 0.12;

                half3 lighting = light.color * (light.shadowAttenuation * light.distanceAttenuation);
                return (albedo * directDiffuse + directSpecular * (1.0 - mask)) * lighting;
            }

            uint murmurHash3(int input)
            {
                uint h = asuint(input);
                h ^= h >> 16;
                h *= 0x85ebca6b;
                h ^= h >> 13;
                h *= 0xc2b2ae3d;
                h ^= h >> 16;
                return h;
            }

            float Remap(float In, float2 InMinMax, float2 OutMinMax)
            {
                return OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / max(InMinMax.y - InMinMax.x, 1e-5);
            }

            Varyings vert(Attributes IN, uint instanceID : SV_InstanceID)
            {
                Varyings OUT;

                float3 pivot = _GrassPositions[instanceID];

                int ix = (int)round(pivot.x * 100.0);
                int iz = (int)round(pivot.z * 100.0);
                int baseKey = ix * 73856093 ^ iz * 19349663;
                uint h0 = murmurHash3(baseKey);
                uint h1 = murmurHash3(baseKey ^ 83492791);

                float randWidth = (h0 & 0xFFFFu) / 65535.0;
                float randHeight = (h0 >> 16) / 65535.0;
                float2 randCurve = float2((h1 & 0xFFu) / 127.5 - 1.0, ((h1 >> 8) & 0xFFu) / 127.5 - 1.0);
                float2 randNorm = float2(((h1 >> 16) & 0xFFu) / 127.5 - 1.0, (h1 >> 24) / 127.5 - 1.0);

                float grassWidth = _GrassWidth * (1.0 - randWidth * _GrassWidthRandomness);
                float distanceFromCamera = length(_WorldSpaceCameraPos - pivot);
                grassWidth += saturate(Remap(distanceFromCamera, _ExpandDistantGrassRange, float2(0.0, 1.0))) * _ExpandDistantGrassWidth;
                grassWidth *= (1.0 - IN.positionOS.y);

                float grassHeight = _GrassHeight * (1.0 - randHeight * _GrassHeightRandomness);
                if (_GrassDrawDistance > 0.0)
                {
                    float edgeFade = 1.0 - smoothstep(_GrassDrawDistance * 0.94, _GrassDrawDistance, distanceFromCamera);
                    grassHeight *= edgeFade;
                    grassWidth *= edgeFade;
                }

                float2 uv = (pivot.xz - _CenterPos) / max(_GrassDataRadius, 1e-5) * 0.5 + 0.5;
                bool inGlobalUV = all(uv >= 0.0) && all(uv <= 1.0);

                float2 localUV = (pivot.xz - _LocalSlopeCenterPos) / max(_LocalSlopeRadius, 1e-5) * 0.5 + 0.5;
                bool inLocalUV = (_HasLocalSlopeRT > 0.5) && all(localUV >= 0.0) && all(localUV <= 1.0);

                float4 slope = float4(0.5, 0.5, 0.0, 0.0);
                float slopeHeightEncoded = 0.0;

                if (inLocalUV)
                {
                    slope = SAMPLE_TEXTURE2D_LOD(_GrassLocalSlopeRT, sampler_GrassLocalSlopeRT, localUV, 0);
                    uint w, h;
                    _GrassLocalSlopeRT.GetDimensions(w, h);
                    int2 px = clamp(int2(localUV * float2(w, h)), int2(0, 0), int2((int)w - 1, (int)h - 1));
                    slopeHeightEncoded = _GrassLocalSlopeRT.Load(int3(px, 0)).b;
                }
                else if (_HasGlobalSlopeRT > 0.5 && inGlobalUV)
                {
                    slope = SAMPLE_TEXTURE2D_LOD(_GrassSlopeRT, sampler_GrassSlopeRT, uv, 0);
                    uint w, h;
                    _GrassSlopeRT.GetDimensions(w, h);
                    int2 px = clamp(int2(uv * float2(w, h)), int2(0, 0), int2((int)w - 1, (int)h - 1));
                    slopeHeightEncoded = _GrassSlopeRT.Load(int3(px, 0)).b;
                }

                if (slopeHeightEncoded > 0.005)
                {
                    float slopeWorldY = Remap(slopeHeightEncoded, float2(0.01, 1.0), _SlopeBoundsYMinMax);
                    if (abs(pivot.y - slopeWorldY) > _SlopeVerticalTolerance)
                        slope.a = 0.0;
                }

                float xSlope = slope.r * 2.0 - 1.0;
                float zSlope = slope.g * 2.0 - 1.0;

                float3 slopeDirection = normalize(float3(xSlope, 1.0 - (max(abs(xSlope), abs(zSlope)) * 0.5), zSlope));
                float3 bladeDirection = normalize(lerp(float3(0.0, 1.0, 0.0), slopeDirection, saturate(slope.a)));

                half3 windTex = SAMPLE_TEXTURE2D_LOD(_WindTexture, sampler_WindTexture, TRANSFORM_TEX(pivot.xz, _WindTexture) + _WindScroll * _Time.y, 0).rgb;
                float2 wind = (windTex.rg * 2.0 - 1.0) * _WindStrength * (1.0 - saturate(slope.a));

                bladeDirection.xz += wind * IN.positionOS.y;
                bladeDirection = normalize(bladeDirection);

                float3 cameraTransformForwardWS = -UNITY_MATRIX_V[2].xyz;
                float3 rawCross = cross(bladeDirection, cameraTransformForwardWS);
                float crossLenSq = dot(rawCross, rawCross);
                float3 rightTangent = crossLenSq > 1e-5 ? rawCross * rsqrt(crossLenSq) : UNITY_MATRIX_V[0].xyz;

                float3 positionOS = bladeDirection * IN.positionOS.y * grassHeight
                                  + rightTangent * IN.positionOS.x * grassWidth;

                positionOS.xz += (IN.positionOS.y * IN.positionOS.y) * randCurve * _GrassCurving;

                float3 positionWS = positionOS + pivot;

                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.positionWS = positionWS;

                half baseSample = SAMPLE_TEXTURE2D_LOD(_BaseColorTexture, sampler_BaseColorTexture, TRANSFORM_TEX(pivot.xz, _BaseColorTexture), 0).r;
                half3 baseColor = lerp(_ColorA, _ColorB, baseSample);
                half3 albedo = lerp(_AOColor, baseColor, IN.positionOS.y);

                float4 color = (_HasColorRT > 0.5 && inGlobalUV)
                    ? SAMPLE_TEXTURE2D_LOD(_GrassColorRT, sampler_GrassColorRT, uv, 0)
                    : float4(0.0, 0.0, 0.0, 0.0);
                albedo = lerp(albedo, color.rgb, color.a);

                half3 normalWS = normalize(bladeDirection + cameraTransformForwardWS * -0.5 + _RandomNormal * half3(randNorm.x, 0.0, randNorm.y));
                OUT.albedo = albedo;
                OUT.colorMask = color.a;
                OUT.posY = IN.positionOS.y;
                OUT.normalWS = normalWS;
                OUT.sh = SampleSH(normalWS);

                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    OUT.shadowCoord = ComputeScreenPos(OUT.positionCS);
                #elif defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                    OUT.shadowCoord = TransformWorldToShadowCoord(positionWS);
                #else
                    OUT.shadowCoord = float4(0.0, 0.0, 0.0, 0.0);
                #endif

                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half3 N = normalize(IN.normalWS);
                half3 V = normalize(_WorldSpaceCameraPos - IN.positionWS);
                half3 albedo = IN.albedo;
                half mask = IN.colorMask;
                half posY = IN.posY;

                half3 result = IN.sh * albedo;

                Light mainLight = GetMainLight(IN.shadowCoord);
                result += ApplySingleDirectLight(mainLight, N, V, albedo, mask, posY);

                #if defined(_ADDITIONAL_LIGHTS)
                int additionalLightsCount = GetAdditionalLightsCount();
                for (int i = 0; i < additionalLightsCount; ++i)
                {
                    Light light = GetAdditionalLight(i, IN.positionWS);
                    result += ApplySingleDirectLight(light, N, V, albedo, mask, posY);
                }
                #endif

                result = MixFog(result, IN.fogFactor);
                return half4(result, 1);
            }
            ENDHLSL
        }
    }
}
