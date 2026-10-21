Shader "SeamlessWormhole/SphericalLens"
{
    Properties
    {
        _TargetCube ("Linked Orange/Green World", Cube) = "" {}
        _GlassTint ("Glass Tint", Color) = (0.39, 0.58, 0.75, 1)
        _HaloColor ("Soft Edge Tint", Color) = (0.74, 0.91, 1.0, 1)
        _Distortion ("Ray Bending", Range(0, 1)) = 0.79
        _Refraction ("Glass Refraction", Range(1.01, 2)) = 1.17
        _EdgePower ("Edge Softness", Range(1, 8)) = 3.0
        _HaloIntensity ("Edge Brightness", Range(0, 8)) = 0.35
        _TargetBrightness ("Linked World Brightness", Range(0, 3)) = 1.05
    }

    SubShader
    {
        // 렌즈는 불투명한 깊이 값을 기록합니다. 투명 유리처럼 뒤쪽 바닥이 비치지 않게 합니다.
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry+30" "RenderType" = "Opaque" }

        Pass
        {
            Name "Spherical Seamless Wormhole"
            Tags { "LightMode" = "UniversalForward" }
            Blend Off
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURECUBE(_TargetCube);
            SAMPLER(sampler_TargetCube);

            CBUFFER_START(UnityPerMaterial)
                float4 _GlassTint;
                float4 _HaloColor;
                float _Distortion;
                float _Refraction;
                float _EdgePower;
                float _HaloIntensity;
                float _TargetBrightness;
                float4x4 _PortalRayMap;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionOS = input.positionOS.xyz;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // 원형 렌즈 범위 밖은 그리지 않습니다.
                float radial = length(input.positionOS.xy) * 2.0;
                clip(1.0 - radial);
                radial = saturate(radial);

                float3 viewToCamera = normalize(_WorldSpaceCameraPos - input.positionWS);
                // 실제 Mesh는 통과 판정이 쉬운 얇은 Quad이지만, 굴절용 법선은
                // 원형 화면 좌표에서 만든 가상의 반구를 사용합니다.
                // 따라서 평면을 통과하면서도 렌즈 안 세계는 구체처럼 휘어 보입니다.
                float2 lensPlane = input.positionOS.xy * 2.0;
                float sphereZ = sqrt(saturate(1.0 - dot(lensPlane, lensPlane)));
                float3 virtualSphereNormalOS = normalize(float3(lensPlane, sphereZ));
                float3 normal = normalize(TransformObjectToWorldNormal(virtualSphereNormalOS));

                // 양쪽에서 통과할 수 있으므로 앞·뒷면 모두 같은 굴절 계산을 사용합니다.
                if (dot(normal, viewToCamera) < 0.0)
                    normal *= -1.0;

                // 2D 텍스처가 아니라 굽은 3D 광선으로 Cubemap을 읽어 렌즈 안 공간감을 만듭니다.
                float3 directRay = normalize(-viewToCamera);
                float3 refractedRay = refract(directRay, normal, rcp(_Refraction));
                if (dot(refractedRay, refractedRay) < 0.001)
                    refractedRay = reflect(directRay, normal);

                float3 bentRay = normalize(lerp(directRay, refractedRay, _Distortion));
                float3 linkedRay = normalize(mul((float3x3)_PortalRayMap, bentRay));
                float3 linkedWorld = SAMPLE_TEXTURECUBE(_TargetCube, sampler_TargetCube, linkedRay).rgb
                    * _TargetBrightness;

                float lensEdge = smoothstep(0.78, 1.0, radial);

                float fresnel = pow(1.0 - saturate(dot(normal, viewToCamera)), _EdgePower);

                // 연결 세계에서 가장자리 색으로 한 번만 부드럽게 전환합니다.
                // 얇고 밝은 경계선은 의도적으로 만들지 않습니다.
                float3 colour = linkedWorld * lerp(1.05, 0.88, lensEdge);
                colour += _GlassTint.rgb * lensEdge * 0.08;
                colour += _HaloColor.rgb * fresnel * _HaloIntensity;

                return half4(colour, 1.0);
            }
            ENDHLSL
        }
    }
}
