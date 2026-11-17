Shader "Shaped Clouds/CloudBlit" {
    SubShader {
        Tags {
			"RenderPipeline" = "UniversalPipeline"
			"RenderType" = "Transparent"
		}
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off
		ZTest Equal
		ZWrite Off

        Pass {
            HLSLPROGRAM
            #pragma vertex vert alpha
            #pragma fragment frag alpha

			#pragma multi_compile_local _ SC_RENDER_ABOVE_OPAQUES

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct appdata {
				uint vertexID : SV_VertexID;
			};

			struct v2f {
				float4 vertex: SV_POSITION;
				float2 texcoord: TEXCOORD0;
				float3 worldPos: POSITION1;
				float4 viewDir: POSITION2;
			};

			sampler2D _BlitTexture;
            sampler2D _CameraDepthTexture;

			float _CloudDepth = 300;

			float _CloudHeight;
			float _Curvature;

			v2f vert(appdata v) {
				v2f o;
				o.vertex = GetFullScreenTriangleVertexPosition(v.vertexID);
				#if !SC_RENDER_ABOVE_OPAQUES
					#if UNITY_REVERSED_Z
						o.vertex.z = 0;
					#else
						o.vertex.z = o.vertex.w;
					#endif
				#endif
				float2 uv = GetFullScreenTriangleTexCoord(v.vertexID);
				o.texcoord = uv;
				o.viewDir = mul(unity_CameraInvProjection, float4(uv * 2.0 - 1.0, 1.0, 1.0));
				o.worldPos = ComputeWorldSpacePosition(o.texcoord, .5, UNITY_MATRIX_I_VP);

				return o;
			}


            float4 frag(v2f i): SV_Target {
				// Horrible way to do it.
				
				#if !SC_RENDER_ABOVE_OPAQUES
					return tex2D(_BlitTexture, i.texcoord.xy);
				#else
					// Different from i.viewDir, accurate world space view direction.
					float3 viewDir = normalize(i.worldPos - GetCameraPositionWS());
					float domeFactor = 1 / (viewDir.y + _Curvature);
					if (domeFactor < 0)
						return 0;

					float z = tex2D(_CameraDepthTexture, i.texcoord.xy).r;
					float depth = Linear01Depth(z, _ZBufferParams);

					#if UNITY_REVERSED_Z
						z = 1.0f - z;
					#endif
					if (z >= 1)
						return tex2D(_BlitTexture, i.texcoord.xy);


					float3 worldPos = float3(viewDir.x*domeFactor, viewDir.y, viewDir.z*domeFactor) * _CloudHeight;

					float3 viewPos = (i.viewDir.xyz / i.viewDir.w) * depth;

					// Distance to clouds - distance to scene.
					float delta = length(worldPos) - length(viewPos);
					if (delta > 0)
						return 0;
					delta /= -_CloudDepth;

					float4 r = tex2D(_BlitTexture, i.texcoord.xy);

					r.a *= pow(saturate(delta-1+r.a), .75);
					return r;
				
				#endif

            }
            ENDHLSL
        }
    }
}
