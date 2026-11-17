Shader "Shaped Clouds/BaseNoiseShader" {
	Properties {
		_NoiseTexture ("Noise Texture", 2D) = "gray" {}

	}

	SubShader {
		Tags {
			"RenderPipeline" = "UniversalPipeline"
			"RenderType" = "Transparent"
		}
		Blend One One
		Cull Off
		ZTest Always
		ZWrite Off

		Pass {
			HLSLPROGRAM
			#pragma vertex vert alpha
			#pragma fragment frag alpha

			#pragma multi_compile_local _ SC_ORTHOGRAPHIC

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct appdata {
				float4 vertex: POSITION;
				uint vertexID : SV_VertexID;
			};

			struct v2f {
				float4 vertex: SV_POSITION;
				float3 worldPos: POSITION1;
			};

			sampler2D _NoiseTexture;

			float _Density;

			float3 _CameraOffset;
			float _CloudHeight;
			float _Curvature;

			float _Cloudiness;

			float4 _Layer[8];
			float _LayerRotation[8];
			
			int _LayerCount = 0;

			float4x4 _OrthographicMatrix;

			float2 scaledPos(float2 uv) {
				return (uv + _CameraOffset.xz / _CloudHeight);

			}

			float cloudDensity(float2 uv, float cloudiness) {
				float r = 0;

				float c, s;
				float2 uv2;
				float4 sample;

				int count = min(_LayerCount, 8);
				[unroll(8)]
				for (int i = 0; i < count; i ++) {
					c = cos(_LayerRotation[i]);
					s = sin(_LayerRotation[i]);

					uv2 = float2(uv.x * c - uv.y * s, uv.x * s + uv.y * c);
					uv2 = uv2*_Layer[i].z + _Layer[i].xy;

					sample = tex2D(_NoiseTexture, uv2);
					r += sample.r *_Layer[i].w;

				}

				return r + cloudiness;

			}

			v2f vert(appdata v) {
				v2f o;
				o.vertex = GetFullScreenTriangleVertexPosition(v.vertexID);

				float2 uv = GetFullScreenTriangleTexCoord(v.vertexID);
				#if SC_ORTHOGRAPHIC
					#if UNITY_REVERSED_Z
						o.worldPos = ComputeWorldSpacePosition(uv, 1, _OrthographicMatrix);
					#else
						o.worldPos = ComputeWorldSpacePosition(uv, -1, _OrthographicMatrix);
					#endif
				#else
					o.worldPos = ComputeWorldSpacePosition(uv, .5, UNITY_MATRIX_I_VP);
				#endif

				return o;
			}

			float frag(v2f i): SV_Target {
				// return i.worldPos;
				float3 viewDir = normalize(i.worldPos - GetCameraPositionWS());
				
				float domeFactor = 1 / (viewDir.y + _Curvature);
				if (domeFactor < 0)
					return 0;
					
				float2 uv = float2(viewDir.x * domeFactor, viewDir.z * domeFactor);

				float density = cloudDensity(scaledPos(uv), _Cloudiness);
				if (density < 0)
					density = 0;
				density = density * _Density;

				return density;
				
			}
			ENDHLSL
		}
	}
}
