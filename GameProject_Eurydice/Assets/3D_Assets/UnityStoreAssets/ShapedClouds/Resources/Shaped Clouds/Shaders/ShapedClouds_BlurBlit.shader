Shader "Shaped Clouds/BlurBlit" {
	SubShader {
    	Tags { "RenderPipeline" = "UniversalPipeline" }
		Cull Off
		ZTest Always
		ZWrite Off
		
		Pass {
			Name "Blur X"
			HLSLPROGRAM
            #pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct appdata {
				uint vertexID : SV_VertexID;
			};

			struct v2f {
				float4 vertex: SV_POSITION;
				float2 texcoord: TEXCOORD0;
			};
			
			sampler2D _BlitTexture;
			float4 _BlitTexture_TexelSize;

			int _Samples;
			float _Intensity;

			static const float weights[] = { 0.14446445, 0.13543542, 0.11153505, 0.08055309, 0.05087564, 0.02798160, 0.01332457, 0.00545096 };

			v2f vert(appdata v) {
				v2f o;
				o.vertex = GetFullScreenTriangleVertexPosition(v.vertexID);
				float2 uv = GetFullScreenTriangleTexCoord(v.vertexID);
				o.texcoord = uv;

				return o;
			}

			float4 frag(v2f i): SV_Target {
                float4 r = 0;
                float tw = 0;

                for (int j = -_Samples; j <= _Samples; j++) {
                    float2 uv = i.texcoord + float2(j*_Intensity/_Samples * _BlitTexture_TexelSize.x / _BlitTexture_TexelSize.y, 0);
                    float4 s = tex2D(_BlitTexture, uv);
                    float w = weights[min(abs(j), 7)];
                    r += w*s;
                    tw += w;
                }

                return r / tw;
            }
			ENDHLSL
		}

		Pass {
			Name "Blur Y"
			HLSLPROGRAM
            #pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct appdata {
				uint vertexID : SV_VertexID;
			};

			struct v2f {
				float4 vertex: SV_POSITION;
				float2 texcoord: TEXCOORD0;
			};
			
			sampler2D _BlitTexture;

			int _Samples;
			float _Intensity;

			static const float weights[] = { 0.14446445, 0.13543542, 0.11153505, 0.08055309, 0.05087564, 0.02798160, 0.01332457, 0.00545096 };

			v2f vert(appdata v) {
				v2f o;
				o.vertex = GetFullScreenTriangleVertexPosition(v.vertexID);
				float2 uv = GetFullScreenTriangleTexCoord(v.vertexID);
				o.texcoord = uv;

				return o;
			}

			float4 frag(v2f i): SV_Target {
                float4 r = 0;
                float tw = 0;

                for (int j = -_Samples; j <= _Samples; j++) {
                    float2 uv = i.texcoord + float2(0, j*_Intensity/_Samples);
                    float4 s = tex2D(_BlitTexture, uv);
                    float w = weights[min(abs(j), 7)];
                    r += w*s;
                    tw += w;
                }

                return r / tw;

            }
			ENDHLSL
		}
	}
}
