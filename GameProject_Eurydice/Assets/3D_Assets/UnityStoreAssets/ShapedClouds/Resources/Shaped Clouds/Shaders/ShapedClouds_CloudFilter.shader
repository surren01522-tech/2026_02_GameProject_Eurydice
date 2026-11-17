Shader "Shaped Clouds/CloudFilter" {
	Properties {
		_NoiseTexture ("Noise Texture", 2D) = "white" {}
	}

	SubShader {
		Tags {
			"RenderPipeline" = "UniversalPipeline"
			"RenderType" = "Opaque"
		}
		Cull Off
		ZTest Always
		ZWrite Off

		Pass {
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile_local _ SC_ORTHOGRAPHIC
			#pragma multi_compile_local _ SC_CUSTOM_AMBIENT_COLOR
			#pragma multi_compile_local _ SC_CUSTOM_SUN
			#pragma multi_compile_local _ SC_CUSTOM_MOON

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"

			struct appdata {
				uint vertexID: SV_VertexID;
			};

			struct v2f {
				float2 texcoord: TEXCOORD0;
				float4 vertex: SV_POSITION;
				float3 worldPos: POSITION1;
			};

			sampler2D _MainTex;

			sampler2D _NoiseTexture;

			float _FadeDistance;
			float _FadeLength;

			float _DetailLevel;

			float4 _Color;
			float3 _SunsetColor;

			float3 _SunColor;
			float3 _SunDirection;

			float3 _MoonColor;
			float3 _MoonDirection;

			float _AmbientDay = 0.3;
			float _AmbientNight = 0.002;

			float3 _CustomAmbientColor;

			float _Density;
			float _ColorAbsorption;

			float3 _CameraOffset;
			float _CloudHeight;
			float _Curvature;

			float4 _Layer[8];
			float _LayerRotation[8];
			
			int _LayerCount = 0;

			float4x4 _OrthographicMatrix;

			float2 scaledPos(float2 uv) {
				return (uv + _CameraOffset.xz / _CloudHeight);

			}

			float cloudNoise(float2 uv) {
				// Generates cloud noise.

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
					r += sample.r * _Layer[i].w;

				}

				return r;

			}

			float HenyeyGreenstein(float g, float d) {
				return (1-g*g) / (2 * pow(1+g*g - 2*g * d, 3/2));

			}


			v2f vert(appdata v) {
				v2f o;
				o.vertex = GetFullScreenTriangleVertexPosition(v.vertexID);

				// Get world position of the vertex.
				o.texcoord = GetFullScreenTriangleTexCoord(v.vertexID);

				// Get a direction vector going from the camera to the vertex.
				#if SC_ORTHOGRAPHIC
					#if UNITY_REVERSED_Z
						o.worldPos = ComputeWorldSpacePosition(o.texcoord, 1, _OrthographicMatrix);
					#else
						o.worldPos = ComputeWorldSpacePosition(o.texcoord, -1, _OrthographicMatrix);
					#endif
				#else
					o.worldPos = ComputeWorldSpacePosition(o.texcoord, .5, UNITY_MATRIX_I_VP);
				#endif

				return o;
			}

			float4 frag(v2f i): SV_Target {
				float4 col = 1;

				float3 viewDir = normalize(i.worldPos - GetCameraPositionWS());
				// Dome thing.
				float domeFactor = 1 / (viewDir.y + _Curvature);
				if (domeFactor < 0)
					return 0;
					
				float2 uv = scaledPos(float2(viewDir.x * domeFactor, viewDir.z * domeFactor));

				// Number between 0 and 1 determining how much the cloud fades away.
				float fade = saturate(viewDir.y + 1 + _FadeDistance);
				if (fade == 0)
					return 0;
				else if (fade < 1)
					fade = (fade-1) / _FadeLength + 1;

				// If the base texture is empty in this fragment, returns nothing.
				float4 base = tex2D(_MainTex, i.texcoord);
				if (base.r <= 0)
					return 0;
				
				// Admittedly, a little bit confusing, but thickness represents the density of the cloud generated with noise in the base texture.
				float thickness = base.r;

				float detail = -saturate(base.r) + _DetailLevel;
				if (detail >= 0)
					thickness -= cloudNoise(uv) * detail;

				thickness = max(thickness, 0);

				#if SC_CUSTOM_SUN
					float3 sunDirection = _SunDirection;
					float3 sunColor = _SunColor;
				#else
					float3 sunDirection = _MainLightPosition.xyz;
					float3 sunColor = _MainLightColor.rgb;
				#endif

				// Dot product of view dir and main light dir.
				// Equivalent to cosine of the angle between the two vectors, which we use later.
				float d = dot(viewDir, sunDirection.xyz);

				#if SC_CUSTOM_MOON
					float d2 = dot(viewDir, _MoonDirection);
				#else
					float d2 = -d;
				#endif
					

				// Transparency is a simple 1-e^(-t*d*2), where t is the thickness of the cloud and d is the density.
				// This is then multiplied by the alpha of the cloud color, so that it directly controls how visible the cloud is.
				col.a = saturate((1-exp(-thickness * _Density)) * _Color.a * fade);

				// Arbitrary numbers, basically means clouds with higher heights continue receiving light for longer.
				float h = max(min(_CloudHeight, 10000), 500) / 60000; 
				float light = saturate(sunDirection.y + h + .25);

				#if SC_CUSTOM_MOON
					float moonLight = saturate(_MoonDirection.y + h + .15);
				#else
					float moonLight = saturate(-sunDirection.y - h);
				#endif

				float3 sColor = (exp(-thickness * _ColorAbsorption) * HenyeyGreenstein(0.4, d)) * light * sunColor * lerp(float3(1,1,1), _SunsetColor, 1-light);
				float3 mColor = (exp(-thickness * _ColorAbsorption) * HenyeyGreenstein(0.4, d2)) * moonLight * _MoonColor;

				#if SC_CUSTOM_AMBIENT_COLOR
					float3 ambientColor = _CustomAmbientColor;
				#else
					float3 ambientColor = light * sunColor * _AmbientDay + moonLight * _MoonColor * _AmbientNight;
				#endif

				col.rgb = (sColor + mColor) * _Color.rgb + ambientColor;
				
				// Makes sure there are no negative values.
				col.r = max(col.r, 0);
				col.g = max(col.g, 0);
				col.b = max(col.b, 0);

				return col;
				
			}
			ENDHLSL
		}
	}
}
