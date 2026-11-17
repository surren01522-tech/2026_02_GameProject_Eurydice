Shader "Shaped Clouds/Mesh/Solid" {
    Properties {
		_MainTex ("Main Texture", 2D) = "white" {}
		[HDR]
		_Color ("Color", Color) = (1, 1, 1)
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
			
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata {
                float4 vertex: POSITION;
                float2 uv: TEXCOORD0;
            };

            struct v2f {
                float4 vertex: SV_POSITION;
                float2 uv: TEXCOORD0;
            };

            sampler2D _MainTex;
			
            CBUFFER_START(UnityPerMaterial)
			float4 _Color;
			float _Density;
			CBUFFER_END

            v2f vert(appdata v) {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i): SV_Target {
                return tex2D(_MainTex, i.uv) * _Color * _Density;

            }
            ENDHLSL
        }
    }
}
