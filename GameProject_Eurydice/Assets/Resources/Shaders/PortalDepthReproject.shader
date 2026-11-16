Shader "Hidden/PortalDepthReproject"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        ZTest Always
        ZWrite Off
        Cull Off
        Blend Off
        ColorMask R

        HLSLINCLUDE
        #define USE_FULL_PRECISION_BLIT_TEXTURE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

        struct DepthVaryings
        {
            float4 positionCS : SV_POSITION;
            float2 screenUV   : TEXCOORD0;
            float2 blitUV     : TEXCOORD1;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        DepthVaryings VertDepth(Attributes input)
        {
            DepthVaryings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            float2 uv = GetFullScreenTriangleTexCoord(input.vertexID);
            output.screenUV = uv;
            output.blitUV = DYNAMIC_SCALING_APPLY_SCALEBIAS(uv);
            return output;
        }
        ENDHLSL

        Pass
        {
            Name "CopyRawDepth"
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex VertDepth
            #pragma fragment FragCopyRaw

            float FragCopyRaw(DepthVaryings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return LOAD_TEXTURE2D_X(_CameraDepthTexture, uint2(input.positionCS.xy)).r;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ReprojectObliqueDepth"
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex VertDepth
            #pragma fragment FragReproject

            float FragReproject(DepthVaryings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float rawDepth = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, input.blitUV, 0).r;
                #if UNITY_REVERSED_Z
                if (rawDepth <= 0.0)
                    return 0.0;
                #else
                if (rawDepth >= 1.0)
                    return 1.0;
                rawDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, rawDepth);
                #endif

                float3 posWS = ComputeWorldSpacePosition(input.screenUV, rawDepth, UNITY_MATRIX_I_VP);
                float eyeDepth = max(LinearEyeDepth(posWS, UNITY_MATRIX_V), 1e-4);
                return saturate((rcp(eyeDepth) - _ZBufferParams.w) / _ZBufferParams.z);
            }
            ENDHLSL
        }
    }
}
