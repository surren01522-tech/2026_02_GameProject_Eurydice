Shader "Eurydice/PortalGateway"
{
    Properties
    {
        [Header(Portal Textures)]
        [NoScaleOffset] _PortalTex("Portal 2D Render Texture", 2D) = "black" {}
        _FallbackColor("Fallback Tint", Color) = (0.12, 0.24, 0.38, 1)
        [HideInInspector] _HasPortalTex("Has Portal Texture", Range(0, 1)) = 0
        _TargetBrightness("Linked World Brightness", Range(0.5, 2.0)) = 1.0

        [Header(Shape)]
        [Toggle(_SHAPE_CUSTOM)] _ShapeMode("Use Custom Polygon Shape", Float) = 0
        [HideInInspector] _PolyCount("Polygon Point Count", Float) = 0
        [HideInInspector] _PolySpace("Polygon Quad Space", Vector) = (0, 0, 2.2, 3.0)
        [HideInInspector] _PolyMetrics("Polygon Metrics", Vector) = (0, 0, 1.0, 0)
        [HideInInspector] _PolyPair0("Poly Pair 0", Vector) = (0, 0, 0, 0)
        [HideInInspector] _PolyPair1("Poly Pair 1", Vector) = (0, 0, 0, 0)
        [HideInInspector] _PolyPair2("Poly Pair 2", Vector) = (0, 0, 0, 0)
        [HideInInspector] _PolyPair3("Poly Pair 3", Vector) = (0, 0, 0, 0)
        [HideInInspector] _PolyPair4("Poly Pair 4", Vector) = (0, 0, 0, 0)
        [HideInInspector] _PolyPair5("Poly Pair 5", Vector) = (0, 0, 0, 0)
        [HideInInspector] _PolyPair6("Poly Pair 6", Vector) = (0, 0, 0, 0)
        [HideInInspector] _PolyPair7("Poly Pair 7", Vector) = (0, 0, 0, 0)
        [HideInInspector] _PolyPair8("Poly Pair 8", Vector) = (0, 0, 0, 0)
        [HideInInspector] _PolyPair9("Poly Pair 9", Vector) = (0, 0, 0, 0)
        [HideInInspector] _PolyPair10("Poly Pair 10", Vector) = (0, 0, 0, 0)
        [HideInInspector] _PolyPair11("Poly Pair 11", Vector) = (0, 0, 0, 0)
        [HideInInspector] _PolyPair12("Poly Pair 12", Vector) = (0, 0, 0, 0)
        [HideInInspector] _PolyPair13("Poly Pair 13", Vector) = (0, 0, 0, 0)
        [HideInInspector] _PolyPair14("Poly Pair 14", Vector) = (0, 0, 0, 0)
        [HideInInspector] _PolyPair15("Poly Pair 15", Vector) = (0, 0, 0, 0)

        [Header(Opening and Closing Transition)]
        _OpenProgress("Open Progress", Range(0, 1)) = 1.0
        _NoiseStrength("Edge Noise Strength", Range(0, 0.5)) = 0.14
        _NoiseScale("Edge Noise Scale", Range(0.5, 16)) = 4.0
        _SwirlStrength("Swirl Distortion", Range(0, 4)) = 1.2

        [Header(Spherical Lens and Rim Glow)]
        _LensCurvature("Virtual Hemisphere Curvature", Range(0, 1)) = 0.55
        _EdgePower("Fresnel Edge Power", Range(1, 8)) = 3.0
        [HDR] _InnerGlowColor("Inner Halo Color", Color) = (0.15, 0.55, 1.2, 1)
        [HDR] _RimColor("Rim Glow Color", Color) = (0.35, 1.7, 2.4, 1)
        _RimWidth("Rim Width", Range(0.005, 0.35)) = 0.07
        _RippleStrength("Inner Ripple Strength", Range(0, 0.05)) = 0.004
        _RippleSpeed("Ripple Speed", Range(0, 10)) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Unlit"
            "Queue" = "Geometry+30"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        #define MAX_POLY_POINTS 32

        CBUFFER_START(UnityPerMaterial)
            float4 _PolySpace;
            float4 _PolyMetrics;
            float4 _PolyPair0;
            float4 _PolyPair1;
            float4 _PolyPair2;
            float4 _PolyPair3;
            float4 _PolyPair4;
            float4 _PolyPair5;
            float4 _PolyPair6;
            float4 _PolyPair7;
            float4 _PolyPair8;
            float4 _PolyPair9;
            float4 _PolyPair10;
            float4 _PolyPair11;
            float4 _PolyPair12;
            float4 _PolyPair13;
            float4 _PolyPair14;
            float4 _PolyPair15;
            half4 _FallbackColor;
            half4 _InnerGlowColor;
            half4 _RimColor;
            float _HasPortalTex;
            float _TargetBrightness;
            float _ShapeMode;
            float _PolyCount;
            float _OpenProgress;
            float _NoiseStrength;
            float _NoiseScale;
            float _SwirlStrength;
            float _LensCurvature;
            float _EdgePower;
            float _RimWidth;
            float _RippleStrength;
            float _RippleSpeed;
        CBUFFER_END

        TEXTURE2D(_PortalTex);
        SAMPLER(sampler_PortalTex);

        float PortalHash21(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p + 45.32);
            return frac(p.x * p.y);
        }

        float PortalValueNoise(float2 p)
        {
            float2 i = floor(p);
            float2 f = frac(p);
            f = f * f * (3.0 - 2.0 * f);
            float a = PortalHash21(i);
            float b = PortalHash21(i + float2(1.0, 0.0));
            float c = PortalHash21(i + float2(0.0, 1.0));
            float d = PortalHash21(i + float2(1.0, 1.0));
            return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
        }

        float PortalFbmNoise(float2 p)
        {
            float v = 0.0;
            float amp = 0.5;
            [unroll]
            for (int i = 0; i < 3; i++)
            {
                v += amp * PortalValueNoise(p);
                p = p * 2.02 + float2(13.1, 7.7);
                amp *= 0.5;
            }
            return v;
        }

        float EvaluateSwirlNoise(float2 p, float r, float openT)
        {
            float transitionBoost = sin(openT * 3.14159265);
            float noiseAmp = _NoiseStrength * (0.25 + 0.75 * transitionBoost);
            if (noiseAmp <= 0.0001)
                return 0.0;

            float time = _Time.y * _RippleSpeed;
            float rot = r * _SwirlStrength - time * 0.8;
            float s, c;
            sincos(rot, s, c);
            float2 swirlUV = float2(p.x * c - p.y * s, p.x * s + p.y * c) * _NoiseScale + float2(0.0, -time * 0.4);
            return (PortalFbmNoise(swirlUV) - 0.5) * noiseAmp;
        }

        void ClipPortalCircle(float2 uv, out float2 lensPlane, out float rSq, out float delta)
        {
            float openT = saturate(_OpenProgress);
            if (openT <= 0.001)
                clip(-1.0);

            float2 p = (uv - 0.5) * 2.0;
            rSq = dot(p, p);
            if (rSq >= 0.998)
                clip(-1.0);

            float r = sqrt(rSq);
            float baseShape = 1.0 - r;
            float effectiveNoise = EvaluateSwirlNoise(p, r, openT);
            float shapeField = saturate(baseShape + effectiveNoise);
            float threshold = lerp(1.02, 0.02, openT);
            delta = shapeField - threshold;
            clip(delta);

            lensPlane = p;
        }

        void ClipPortalCustom(float2 uv, out float2 lensPlane, out float rSq, out float delta)
        {
            float openT = saturate(_OpenProgress);
            if (openT <= 0.001)
                clip(-1.0);

            int count = clamp((int)round(_PolyCount), 0, MAX_POLY_POINTS);
            if (count < 3)
                clip(-1.0);

            float2 pts[MAX_POLY_POINTS] = {
                _PolyPair0.xy, _PolyPair0.zw, _PolyPair1.xy, _PolyPair1.zw,
                _PolyPair2.xy, _PolyPair2.zw, _PolyPair3.xy, _PolyPair3.zw,
                _PolyPair4.xy, _PolyPair4.zw, _PolyPair5.xy, _PolyPair5.zw,
                _PolyPair6.xy, _PolyPair6.zw, _PolyPair7.xy, _PolyPair7.zw,
                _PolyPair8.xy, _PolyPair8.zw, _PolyPair9.xy, _PolyPair9.zw,
                _PolyPair10.xy, _PolyPair10.zw, _PolyPair11.xy, _PolyPair11.zw,
                _PolyPair12.xy, _PolyPair12.zw, _PolyPair13.xy, _PolyPair13.zw,
                _PolyPair14.xy, _PolyPair14.zw, _PolyPair15.xy, _PolyPair15.zw
            };

            float2 localXY = _PolySpace.xy + (uv - 0.5) * _PolySpace.zw;
            float2 v0 = pts[0];
            float d = dot(localXY - v0, localXY - v0);
            float s = 1.0;
            float2 vj = pts[count - 1];

            [loop]
            for (int i = 0; i < count; i++)
            {
                float2 vi = pts[i];
                float2 e = vj - vi;
                float2 w = localXY - vi;
                float denom = max(dot(e, e), 1e-8);
                float2 b = w - e * clamp(dot(w, e) / denom, 0.0, 1.0);
                d = min(d, dot(b, b));

                bool3 c = bool3(localXY.y >= vi.y, localXY.y < vj.y, e.x * w.y > e.y * w.x);
                if (all(c) || all(!c))
                    s *= -1.0;

                vj = vi;
            }

            float sd = -s * sqrt(d);
            float maxInradius = max(_PolyMetrics.z, 0.001);
            float baseShape = sd / maxInradius;
            if (baseShape <= 0.001)
                clip(-1.0);

            float2 toCenter = localXY - _PolyMetrics.xy;
            float2 p = toCenter / maxInradius;
            float r = saturate(1.0 - baseShape);
            float effectiveNoise = EvaluateSwirlNoise(p, r, openT);
            float shapeField = saturate(baseShape + effectiveNoise);
            float threshold = lerp(1.02, 0.02, openT);
            delta = shapeField - threshold;
            clip(delta);

            float lenSq = dot(toCenter, toCenter);
            float2 dir = (lenSq > 1e-6) ? toCenter * rsqrt(lenSq) : float2(0.0, 0.0);
            lensPlane = dir * r;
            rSq = r * r;
        }
        ENDHLSL

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex PortalVert
            #pragma fragment PortalFrag
            #pragma shader_feature_local _SHAPE_CUSTOM
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float4 positionNDC : TEXCOORD2;
                half fogFactor     : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings PortalVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.positionNDC = posInputs.positionNDC;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return output;
            }

            half4 PortalFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 lensPlane;
                float rSq, delta;
            #ifdef _SHAPE_CUSTOM
                ClipPortalCustom(input.uv, lensPlane, rSq, delta);
            #else
                ClipPortalCircle(input.uv, lensPlane, rSq, delta);
            #endif

                float3 viewToCamera = SafeNormalize(_WorldSpaceCameraPos - input.positionWS);
                float curvature = max(0.0, _LensCurvature);
                float curvatureWeight = saturate(curvature * 10.0);

                float sphereZ = sqrt(saturate(1.0 - rSq));
                float3 virtualNormalOS = normalize(float3(lensPlane * curvature, max(0.001, sphereZ)));
                float3 normalWS = SafeNormalize(TransformObjectToWorldNormal(virtualNormalOS));
                if (dot(normalWS, viewToCamera) < 0.0)
                    normalWS = -normalWS;

                float fresnel = pow(1.0 - saturate(dot(normalWS, viewToCamera)), max(1.0, _EdgePower));
                float rimMask = 1.0 - smoothstep(0.0, max(0.001, _RimWidth), max(0.0, delta));
                float lensEdge = (1.0 - saturate(max(0.0, delta) / max(_RimWidth * 3.0, 0.001))) * curvatureWeight;

                half3 portalColor;
                if (_HasPortalTex > 0.5)
                {
                    float time = _Time.y * _RippleSpeed;
                    float transitionBoost = sin(saturate(_OpenProgress) * 3.14159265);
                    float2 rippleOffset = float2(
                        sin(input.uv.y * 16.0 + time * 1.6) + cos(input.uv.x * 12.0 - time * 1.2),
                        cos(input.uv.x * 16.0 - time * 1.4) + sin(input.uv.y * 12.0 + time * 1.8)
                    ) * _RippleStrength * (0.3 + 1.5 * transitionBoost);

                    float2 lensBendOffset = lensPlane * (1.0 - sphereZ) * curvature * 0.06;
                    float2 screenUV = input.positionNDC.xy / max(input.positionNDC.w, 1e-5);
                    float2 portalUV = clamp(screenUV + rippleOffset - lensBendOffset, 0.001, 0.999);

                    portalColor = SAMPLE_TEXTURE2D(_PortalTex, sampler_PortalTex, portalUV).rgb * (half)_TargetBrightness;
                }
                else
                {
                    portalColor = lerp(_FallbackColor.rgb, _InnerGlowColor.rgb, (half)(fresnel * 0.6));
                }

                half3 color = portalColor * lerp((half)1.0, (half)0.88, (half)lensEdge);
                color += _InnerGlowColor.rgb * (half)(fresnel * 0.45 + lensEdge * 0.25);
                color = lerp(color, _RimColor.rgb, (half)(rimMask * rimMask));
                color = MixFog(color, input.fogFactor);

                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            Cull Off
            ZWrite On
            ZTest LEqual
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex PortalDepthVert
            #pragma fragment PortalDepthFrag
            #pragma shader_feature_local _SHAPE_CUSTOM
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings PortalDepthVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half PortalDepthFrag(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 lensPlane;
                float rSq, delta;
            #ifdef _SHAPE_CUSTOM
                ClipPortalCustom(input.uv, lensPlane, rSq, delta);
            #else
                ClipPortalCircle(input.uv, lensPlane, rSq, delta);
            #endif
                return input.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormalsOnly"
            Tags { "LightMode" = "DepthNormalsOnly" }

            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex PortalDepthNormalsVert
            #pragma fragment PortalDepthNormalsFrag
            #pragma shader_feature_local _SHAPE_CUSTOM
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings PortalDepthNormalsVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            half4 PortalDepthNormalsFrag(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 lensPlane;
                float rSq, delta;
            #ifdef _SHAPE_CUSTOM
                ClipPortalCustom(input.uv, lensPlane, rSq, delta);
            #else
                ClipPortalCircle(input.uv, lensPlane, rSq, delta);
            #endif

                float3 normalWS = SafeNormalize(input.normalWS);
                float3 viewDir = GetWorldSpaceNormalizeViewDir(input.positionWS);
                if (dot(normalWS, viewDir) < 0.0)
                    normalWS = -normalWS;

                return half4(NormalizeNormalPerPixel(normalWS), 0.0);
            }
            ENDHLSL
        }
    }
}
