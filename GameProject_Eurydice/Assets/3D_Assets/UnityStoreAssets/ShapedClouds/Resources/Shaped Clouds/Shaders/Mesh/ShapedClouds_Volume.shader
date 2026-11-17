Shader "Shaped Clouds/Mesh/Volume" {
    Properties {
		_Density ("Density", Float) = 1
    }
    SubShader {
        Tags {
			"RenderPipeline" = "UniversalPipeline"
			"RenderType" = "Transparent"
		}
		Blend One One
		ZWrite Off
		Cull Off
		ZTest Always

        Pass {
            HLSLPROGRAM
            #pragma vertex vert alpha
            #pragma fragment frag alpha
			#pragma target 3.0
			
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata {
                float4 vertex: POSITION;
                float2 uv: TEXCOORD0;
            };

            struct v2f {
                float4 vertex: SV_POSITION;
                float2 uv: TEXCOORD0;
				float3 worldPos: POSITION1;
            };

			
            CBUFFER_START(UnityPerMaterial)
			float _Density;
			CBUFFER_END

            v2f vert(appdata v) {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
				o.worldPos = TransformObjectToWorld(v.vertex.xyz);
                return o;
            }

            float frag(v2f i, float facing : VFACE): SV_Target {
				float dst = distance(i.worldPos, GetCameraPositionWS()) * _Density;
				return facing > 0 ? -dst : dst;

            }

            ENDHLSL
        }
    }
}
