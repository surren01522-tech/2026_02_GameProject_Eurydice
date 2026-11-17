// Made with Amplify Shader Editor v1.9.9.12
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "SkyClouds_2D"
{
	Properties
	{
		_BaseColor( "Base Color", Color ) = ( 1, 1, 1, 1 )
		_EmmissiveColor( "Emmissive Color", Color ) = ( 0.5490196, 0.627451, 0.7294118, 0 )
		_Scale( "Scale", Float ) = 1
		_WindDirection( "WindDirection", Vector ) = ( -1, -0.3, -0.3, 0 )
		_WindSpeed( "WindSpeed", Float ) = 1
		_Mask0( "Mask0", Vector ) = ( 1.985208, 0.440832, 0.2055923, 1.2 )
		_Mask1( "Mask1", Vector ) = ( 1.985208, 0.440832, 0.2055923, 1.2 )
		_DepthDistance( "Depth Distance", Float ) = 0.4
		_NoiseColorStrength( "Noise Color Strength", Range( 0, 1 ) ) = 0.5565214
		_EmmissiveNoiseColor( "Emmissive Noise Color", Range( 0, 1 ) ) = 0.2000003
		_NoiseSpeed( "Noise Speed", Float ) = 0.2
		_VorSpeed( "Vor Speed", Float ) = 0.05
		_AdditionalNoiseDir( "Additional Noise Dir", Vector ) = ( 0, 0, 0, 0 )
		_AdditionalVoronoiDir( "AdditionalVoronoiDir", Vector ) = ( 0.7, -0.3, -0.3, 0 )
		_VoronoiScale( "Voronoi Scale", Float ) = 1.4
		_NoiseScale( "Noise Scale", Float ) = 20
		_VoronoiStrength( "Voronoi Strength", Float ) = 0.9
		_NoiseStrength( "Noise Strength", Float ) = 0.03
		_NoiseDisplacement( "Noise Displacement", Float ) = 0.5
		_NoiseScale3D( "Noise Scale 3D", Vector ) = ( 1, 0.2, 0.6, 0 )
		_EmmissiveDepthFade( "Emmissive Depth Fade", Range( 0, 1 ) ) = 0.2086951
		_NoiseColorMix( "NoiseColorMix", Range( 0, 1 ) ) = 0.96
		_VertexColorStrength( "Vertex Color Strength", Range( 0, 1 ) ) = 0.7030082


		//_TransmissionShadow( "Transmission Shadow", Range( 0, 1 ) ) = 0.5
		_TransStrength( "Strength", Range( 0, 50 ) ) = 1
		_TransNormal( "Normal Distortion", Range( 0, 1 ) ) = 0.5
		_TransScattering( "Scattering", Range( 1, 50 ) ) = 2
		_TransDirect( "Direct", Range( 0, 1 ) ) = 0.9
		_TransAmbient( "Ambient", Range( 0, 1 ) ) = 0.1
		_TransShadow( "Shadow", Range( 0, 1 ) ) = 0.5

		//_TessPhongStrength( "Tess Phong Strength", Range( 0, 1 ) ) = 0.5
		//_TessValue( "Tess Max Tessellation", Range( 1, 32 ) ) = 16
		//_TessMin( "Tess Min Distance", Float ) = 10
		//_TessMax( "Tess Max Distance", Float ) = 25
		//_TessEdgeLength ( "Tess Edge length", Range( 2, 50 ) ) = 16
		//_TessMaxDisp( "Tess Max Displacement", Float ) = 25

		//_InstancedTerrainNormals("Instanced Terrain Normals", Float) = 1.0

		[ToggleOff(_SPECULARHIGHLIGHTS_OFF)] _SpecularHighlights("Specular Highlights", Float) = 1.0
		[ToggleOff] _EnvironmentReflections("Environment Reflections", Float) = 1.0
		[ToggleOff] _ScreenSpaceReflections("Screen Space Reflections", Float) = 1.0
		[ToggleOff] _ScreenSpaceReflectionsContributeTransparent("Screen Space Reflections Contribute Transparent", Float) = 1.0
		//[HideInInspector][ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0

		[HideInInspector] _QueueOffset("_QueueOffset", Float) = 0
        [HideInInspector] _QueueControl("_QueueControl", Float) = -1

        [HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}

		//[HideInInspector][ToggleUI] _AddPrecomputedVelocity("Add Precomputed Velocity", Float) = 1
		//[HideInInspector] _XRMotionVectorsPass("_XRMotionVectorsPass", Float) = 1

		//[HideInInspector] _AlphaClip("__clip", Float) = 1.0
	}

	SubShader
	{
		PackageRequirements
		{
			"com.unity.render-pipelines.universal": "[17.0,18.0]"
		}

		

		

		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" "UniversalMaterialType"="Lit" }

	LOD 0

		Cull Back
		ZWrite Off
		ZTest LEqual
		Offset 0 , 0
		AlphaToMask Off

		

		HLSLINCLUDE
		#pragma target 4.5
		#pragma prefer_hlslcc gles
		// ensure rendering platforms toggle list is visible

		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

		#define ASE_ADJUST_CLIP_POSITION( x ) x

		#ifndef ASE_TESS_FUNCS
		#define ASE_TESS_FUNCS
		float4 FixedTess( float tessValue )
		{
			return tessValue;
		}

		float CalcDistanceTessFactor (float4 vertex, float minDist, float maxDist, float tess, float4x4 o2w, float3 cameraPos )
		{
			float3 wpos = mul(o2w,vertex).xyz;
			float dist = distance (wpos, cameraPos);
			float f = clamp(1.0 - (dist - minDist) / (maxDist - minDist), 0.01, 1.0) * tess;
			return f;
		}

		float4 CalcTriEdgeTessFactors (float3 triVertexFactors)
		{
			float4 tess;
			tess.x = 0.5 * (triVertexFactors.y + triVertexFactors.z);
			tess.y = 0.5 * (triVertexFactors.x + triVertexFactors.z);
			tess.z = 0.5 * (triVertexFactors.x + triVertexFactors.y);
			tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0f;
			return tess;
		}

		float CalcEdgeTessFactor (float3 wpos0, float3 wpos1, float edgeLen, float3 cameraPos, float4 scParams )
		{
			float dist = distance (0.5 * (wpos0+wpos1), cameraPos);
			float len = distance(wpos0, wpos1);
			float f = max(len * scParams.y / (edgeLen * dist), 1.0);
			return f;
		}

		float DistanceFromPlane (float3 pos, float4 plane)
		{
			float d = dot (float4(pos,1.0f), plane);
			return d;
		}

		bool WorldViewFrustumCull (float3 wpos0, float3 wpos1, float3 wpos2, float cullEps, float4 planes[6] )
		{
			float4 planeTest;
			planeTest.x = (( DistanceFromPlane(wpos0, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[0]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.y = (( DistanceFromPlane(wpos0, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[1]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.z = (( DistanceFromPlane(wpos0, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[2]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.w = (( DistanceFromPlane(wpos0, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[3]) > -cullEps) ? 1.0f : 0.0f );
			return !all (planeTest);
		}

		float4 DistanceBasedTess( float4 v0, float4 v1, float4 v2, float tess, float minDist, float maxDist, float4x4 o2w, float3 cameraPos )
		{
			float3 f;
			f.x = CalcDistanceTessFactor (v0,minDist,maxDist,tess,o2w,cameraPos);
			f.y = CalcDistanceTessFactor (v1,minDist,maxDist,tess,o2w,cameraPos);
			f.z = CalcDistanceTessFactor (v2,minDist,maxDist,tess,o2w,cameraPos);

			return CalcTriEdgeTessFactors (f);
		}

		float4 EdgeLengthBasedTess( float4 v0, float4 v1, float4 v2, float edgeLength, float4x4 o2w, float3 cameraPos, float4 scParams )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;
			tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
			tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
			tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
			tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			return tess;
		}

		float4 EdgeLengthBasedTessCull( float4 v0, float4 v1, float4 v2, float edgeLength, float maxDisplacement, float4x4 o2w, float3 cameraPos, float4 scParams, float4 planes[6] )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;

			if (WorldViewFrustumCull(pos0, pos1, pos2, maxDisplacement, planes))
			{
				tess = 0.0f;
			}
			else
			{
				tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
				tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
				tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
				tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			}
			return tess;
		}
		#endif //ASE_TESS_FUNCS
		ENDHLSL

		
		Pass
		{
			
			Name "Forward"
			Tags { "LightMode"="UniversalForwardOnly" }

			Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
			ZWrite On
			ZTest LEqual
			Offset 0 , 0
			ColorMask RGBA

			

			HLSLPROGRAM

			#define ASE_GEOMETRY
			#define _ALPHATEST_ON
			#define _NORMAL_DROPOFF_TS 1
			#pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
			#pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
			#pragma multi_compile_instancing
			#pragma instancing_options renderinglayer
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#define ASE_FOG 1
			#define ASE_WRITE_DEPTH
			#define ASE_TRANSLUCENCY 1
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define _EMISSION
			#define ASE_VERSION 19912
			#define ASE_SRP_VERSION 170300
			#define REQUIRE_DEPTH_TEXTURE 1


			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
			#pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
			#if ( UNITY_VERSION >= 60010000 )
			#pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
			#endif

			#if defined(UNITY_PLATFORM_META_QUEST) && ( UNITY_VERSION >= 60050000 )
            #pragma multi_compile _ META_QUEST_LIGHTUNROLL
            #endif

			#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
			#if ( UNITY_VERSION >= 60030000 )
			#pragma multi_compile_fragment _ _SCREEN_SPACE_IRRADIANCE
			#endif
			#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
			#pragma multi_compile _ _LIGHT_LAYERS
			#pragma multi_compile_fragment _ _LIGHT_COOKIES
			#if ( UNITY_VERSION >= 60010000 )
			#pragma multi_compile _ _CLUSTER_LIGHT_LOOP
			#else
			#pragma multi_compile _ _FORWARD_PLUS
			#endif

            #if defined(UNITY_PLATFORM_META_QUEST) && ( UNITY_VERSION >= 60050000 )
            #pragma multi_compile _ META_QUEST_ORTHO_PROJ
            #pragma multi_compile _ META_QUEST_NO_SPOTLIGHTS_LIGHT_LOOP
            #endif

			#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
			#pragma multi_compile _ SHADOWS_SHADOWMASK
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile _ LIGHTMAP_ON
			#if ( UNITY_VERSION >= 60010000 )
			#pragma multi_compile _ LIGHTMAP_BICUBIC_SAMPLING
			#endif
			#if ( UNITY_VERSION >= 60030000 )
			#pragma multi_compile_fragment _ REFLECTION_PROBE_ROTATION
			#endif
			#pragma multi_compile _ DYNAMICLIGHTMAP_ON
			#pragma multi_compile _ USE_LEGACY_LIGHTMAPS

			#pragma vertex vert
			#pragma fragment frag

			#if defined( _SPECULAR_SETUP ) && defined( ASE_LIGHTING_SIMPLE )
				#if defined( _SPECULARHIGHLIGHTS_OFF )
					#undef _SPECULAR_COLOR
				#else
					#define _SPECULAR_COLOR
				#endif
			#endif

			#define SHADERPASS SHADERPASS_FORWARD

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#if ( UNITY_VERSION >= 60010000 )
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
			#else
			#pragma multi_compile_fog
			#endif
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#if defined( UNITY_INSTANCING_ENABLED ) && defined( ASE_INSTANCED_TERRAIN ) && ( defined(_TERRAIN_INSTANCED_PERPIXEL_NORMAL) || defined(_INSTANCEDTERRAINNORMALS_PIXEL) )
				#define ENABLE_TERRAIN_PERPIXEL_NORMAL
			#endif

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_VERT_NORMAL
			#define ASE_NEEDS_FRAG_SCREEN_POSITION_NORMALIZED


			#if defined(ASE_WRITE_DEPTH_CONSERVATIVE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			#if ( UNITY_VERSION < 60010000 )
				#define USE_CLUSTER_LIGHT_LOOP USE_FORWARD_PLUS
				#define CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 texcoord : TEXCOORD0;
				#if defined(LIGHTMAP_ON) || defined(ASE_NEEDS_TEXTURE_COORDINATES1)
					float4 texcoord1 : TEXCOORD1;
				#endif
				#if defined(DYNAMICLIGHTMAP_ON) || defined(ASE_NEEDS_TEXTURE_COORDINATES2)
					float4 texcoord2 : TEXCOORD2;
				#endif
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				half3 normalWS : TEXCOORD1;
				float4 tangentWS : TEXCOORD2; // holds terrainUV ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL
				float4 lightmapUVOrVertexSH : TEXCOORD3;
				#if defined(ASE_FOG) || defined(_ADDITIONAL_LIGHTS_VERTEX)
					half4 fogFactorAndVertexLight : TEXCOORD4;
				#endif
				#if defined(DYNAMICLIGHTMAP_ON)
					float2 dynamicLightmapUV : TEXCOORD5;
				#endif
				#if defined(USE_APV_PROBE_OCCLUSION)
					float4 probeOcclusion : TEXCOORD6;
				#endif
				float4 ase_texcoord7 : TEXCOORD7;
				float3 ase_normal : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord8 : TEXCOORD8;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _EmmissiveColor;
			float4 _BaseColor;
			float4 _Mask1;
			float4 _Mask0;
			float3 _NoiseScale3D;
			float3 _WindDirection;
			float3 _AdditionalNoiseDir;
			float3 _AdditionalVoronoiDir;
			float _Scale;
			float _EmmissiveNoiseColor;
			float _VertexColorStrength;
			float _NoiseColorStrength;
			float _NoiseColorMix;
			float _VoronoiStrength;
			float _DepthDistance;
			float _VorSpeed;
			float _VoronoiScale;
			float _NoiseStrength;
			float _NoiseScale;
			float _WindSpeed;
			float _NoiseSpeed;
			float _NoiseDisplacement;
			float _EmmissiveDepthFade;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TRANSMISSION
				float _TransmissionShadow;
			#endif
			#ifdef ASE_TRANSLUCENCY
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			

			float3 mod3D289( float3 x ) { return x - floor( x / 289.0 ) * 289.0; }
			float4 mod3D289( float4 x ) { return x - floor( x / 289.0 ) * 289.0; }
			float4 permute( float4 x ) { return mod3D289( ( x * 34.0 + 1.0 ) * x ); }
			float4 taylorInvSqrt( float4 r ) { return 1.79284291400159 - r * 0.85373472095314; }
			float snoise( float3 v )
			{
				const float2 C = float2( 1.0 / 6.0, 1.0 / 3.0 );
				float3 i = floor( v + dot( v, C.yyy ) );
				float3 x0 = v - i + dot( i, C.xxx );
				float3 g = step( x0.yzx, x0.xyz );
				float3 l = 1.0 - g;
				float3 i1 = min( g.xyz, l.zxy );
				float3 i2 = max( g.xyz, l.zxy );
				float3 x1 = x0 - i1 + C.xxx;
				float3 x2 = x0 - i2 + C.yyy;
				float3 x3 = x0 - 0.5;
				i = mod3D289( i);
				float4 p = permute( permute( permute( i.z + float4( 0.0, i1.z, i2.z, 1.0 ) ) + i.y + float4( 0.0, i1.y, i2.y, 1.0 ) ) + i.x + float4( 0.0, i1.x, i2.x, 1.0 ) );
				float4 j = p - 49.0 * floor( p / 49.0 );  // mod(p,7*7)
				float4 x_ = floor( j / 7.0 );
				float4 y_ = floor( j - 7.0 * x_ );  // mod(j,N)
				float4 x = ( x_ * 2.0 + 0.5 ) / 7.0 - 1.0;
				float4 y = ( y_ * 2.0 + 0.5 ) / 7.0 - 1.0;
				float4 h = 1.0 - abs( x ) - abs( y );
				float4 b0 = float4( x.xy, y.xy );
				float4 b1 = float4( x.zw, y.zw );
				float4 s0 = floor( b0 ) * 2.0 + 1.0;
				float4 s1 = floor( b1 ) * 2.0 + 1.0;
				float4 sh = -step( h, 0.0 );
				float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
				float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
				float3 g0 = float3( a0.xy, h.x );
				float3 g1 = float3( a0.zw, h.y );
				float3 g2 = float3( a1.xy, h.z );
				float3 g3 = float3( a1.zw, h.w );
				float4 norm = taylorInvSqrt( float4( dot( g0, g0 ), dot( g1, g1 ), dot( g2, g2 ), dot( g3, g3 ) ) );
				g0 *= norm.x;
				g1 *= norm.y;
				g2 *= norm.z;
				g3 *= norm.w;
				float4 m = max( 0.6 - float4( dot( x0, x0 ), dot( x1, x1 ), dot( x2, x2 ), dot( x3, x3 ) ), 0.0 );
				m = m* m;
				m = m* m;
				float4 px = float4( dot( x0, g0 ), dot( x1, g1 ), dot( x2, g2 ), dot( x3, g3 ) );
				return 42.0 * dot( m, px);
			}
			
					float2 voronoihash97( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi97( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0; int i, j;
						for ( j = -1; j <= 1; j++ )
						{
							for ( i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash97( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			
					float2 voronoihash96( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi96( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0; int i, j;
						for ( j = -1; j <= 1; j++ )
						{
							for ( i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash96( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			
					float2 voronoihash89( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi89( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0; int i, j;
						for ( j = -1; j <= 1; j++ )
						{
							for ( i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash89( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			

			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				#if ( SHADER_TARGET >= 50 )
				float recip367 = rcp( _Scale );
				#else
				float recip367 = 1.0 / _Scale;
				#endif
				float Scale228 = recip367;
				float4 appendResult329 = (float4(_NoiseScale3D.x , _NoiseScale3D.y , _NoiseScale3D.z , 0.0));
				float3 WindDir317 = ( ( float3( 1, 1, 1 ) * -2 + 1 ) * _WindDirection );
				float3 break332 = WindDir317;
				float temp_output_333_0 = ( abs( break332.x ) + abs( break332.y ) + abs( break332.z ) );
				float4 appendResult340 = (float4(( break332.x / temp_output_333_0 ) , ( break332.y / temp_output_333_0 ) , ( break332.z / temp_output_333_0 ) , 0.0));
				float4 WindDirWeights341 = appendResult340;
				float4 break344 = abs( WindDirWeights341 );
				float4 appendResult330 = (float4(_NoiseScale3D.y , _NoiseScale3D.x , _NoiseScale3D.z , 0.0));
				float4 appendResult331 = (float4(_NoiseScale3D.y , _NoiseScale3D.z , _NoiseScale3D.x , 0.0));
				float4 transform70 = mul(GetObjectToWorldMatrix(),input.positionOS);
				float4 temp_output_71_0 = (transform70).xyzw;
				float simplePerlin3D112 = snoise( ( Scale228 * ( ( ( appendResult329 * break344.x ) + ( appendResult330 * break344.y ) + ( appendResult331 * break344.z ) ) * (temp_output_71_0*1.0 + float4( ( ( WindDir317 + _AdditionalNoiseDir ) * ( _NoiseSpeed * _TimeParameters.x * _WindSpeed ) ) , 0.0 )) ) ).xyz*_NoiseScale );
				simplePerlin3D112 = simplePerlin3D112*0.5 + 0.5;
				float temp_output_118_0 = saturate(  (0.0 + ( simplePerlin3D112 - 0.0 ) * ( 1.0 - 0.0 ) / ( 1.0 - 0.0 ) ) );
				float temp_output_198_0 = ( Scale228 * _VoronoiScale );
				float time97 = 0.0;
				float2 voronoiSmoothId97 = 0;
				float4 break84 = (temp_output_71_0*1.0 + float4( ( ( _TimeParameters.x * _VorSpeed * _WindSpeed ) * ( WindDir317 + _AdditionalVoronoiDir ) ) , 0.0 ));
				float2 appendResult85 = (float2(break84.z , break84.y));
				float2 coords97 = appendResult85 * temp_output_198_0;
				float2 id97 = 0;
				float2 uv97 = 0;
				float fade97 = 0.5;
				float voroi97 = 0;
				float rest97 = 0;
				for( int it97 = 0; it97 <3; it97++ ){
				voroi97 += fade97 * voronoi97( coords97, time97, id97, uv97, 0,voronoiSmoothId97 );
				rest97 += fade97;
				coords97 *= 2;
				fade97 *= 0.5;
				}//Voronoi97
				voroi97 /= rest97;
				float3 objToWorldDir91 = mul( GetObjectToWorldMatrix(), float4( input.normalOS, 0.0 ) ).xyz;
				float3 temp_output_93_0 = pow( abs( objToWorldDir91 ) , 5.0 );
				float dotResult94 = dot( temp_output_93_0 , float3( 1,1,1 ) );
				float3 break104 = ( temp_output_93_0 / dotResult94 );
				float time96 = 217.0;
				float2 voronoiSmoothId96 = 0;
				float2 appendResult86 = (float2(break84.z , break84.x));
				float2 coords96 = appendResult86 * temp_output_198_0;
				float2 id96 = 0;
				float2 uv96 = 0;
				float fade96 = 0.5;
				float voroi96 = 0;
				float rest96 = 0;
				for( int it96 = 0; it96 <3; it96++ ){
				voroi96 += fade96 * voronoi96( coords96, time96, id96, uv96, 0,voronoiSmoothId96 );
				rest96 += fade96;
				coords96 *= 2;
				fade96 *= 0.5;
				}//Voronoi96
				voroi96 /= rest96;
				float time89 = 137.0;
				float2 voronoiSmoothId89 = 0;
				float2 appendResult87 = (float2(break84.x , break84.y));
				float2 coords89 = appendResult87 * temp_output_198_0;
				float2 id89 = 0;
				float2 uv89 = 0;
				float fade89 = 0.5;
				float voroi89 = 0;
				float rest89 = 0;
				for( int it89 = 0; it89 <3; it89++ ){
				voroi89 += fade89 * voronoi89( coords89, time89, id89, uv89, 0,voronoiSmoothId89 );
				rest89 += fade89;
				coords89 *= 2;
				fade89 *= 0.5;
				}//Voronoi89
				voroi89 /= rest89;
				float temp_output_190_0 = saturate(  (0.0 + ( ( 1.0 - ( ( ( voroi97 * break104.x ) + ( voroi96 * break104.y ) ) + ( voroi89 * break104.z ) ) ) - 0.65 ) * ( 1.0 - 0.0 ) / ( 1.0 - 0.65 ) ) );
				float temp_output_122_0 = saturate( ( ( temp_output_118_0 * _NoiseStrength ) + ( temp_output_190_0 * _VoronoiStrength ) ) );
				float3 ase_objectScale = float3( length( GetObjectToWorldMatrix()[ 0 ].xyz ), length( GetObjectToWorldMatrix()[ 1 ].xyz ), length( GetObjectToWorldMatrix()[ 2 ].xyz ) );
				float3 temp_output_184_0 = ( ( ( temp_output_122_0 - 0.5 ) * input.normalOS * ( _NoiseDisplacement / Scale228 ) ) / ase_objectScale );
				float4 temp_output_2_0_g146 = _Mask0;
				float3 worldToObj306 = mul( GetWorldToObjectMatrix(), float4( (temp_output_2_0_g146).xyz, 1 ) ).xyz;
				float3 ase_objectPosition = GetAbsolutePositionWS( UNITY_MATRIX_M._m03_m13_m23 );
				float4 appendResult313 = (float4((temp_output_2_0_g146).w , 0.0 , 0.0 , 0.0));
				float3 worldToObj307 = mul( GetWorldToObjectMatrix(), float4( ( float4( ase_objectPosition , 0.0 ) + appendResult313 ).xyz, 1 ) ).xyz;
				float3 temp_output_304_0 = ( temp_output_184_0 + ( min( ( distance( ( temp_output_184_0 + input.positionOS.xyz ) , worldToObj306 ) - length( worldToObj307 ) ), 0.0 ) * input.normalOS ) );
				float4 temp_output_2_0_g153 = _Mask1;
				float3 worldToObj357 = mul( GetWorldToObjectMatrix(), float4( (temp_output_2_0_g153).xyz, 1 ) ).xyz;
				float4 appendResult354 = (float4((temp_output_2_0_g153).w , 0.0 , 0.0 , 0.0));
				float3 worldToObj358 = mul( GetWorldToObjectMatrix(), float4( ( float4( ase_objectPosition , 0.0 ) + appendResult354 ).xyz, 1 ) ).xyz;
				float3 VertexOffset305 = ( temp_output_304_0 + ( min( ( distance( ( temp_output_304_0 + input.positionOS.xyz ) , worldToObj357 ) - length( worldToObj358 ) ), 0.0 ) * input.normalOS ) );
				
				float3 objectToViewPos = TransformWorldToView( TransformObjectToWorld( input.positionOS.xyz ) );
				float eyeDepth = -objectToViewPos.z;
				output.ase_texcoord8.x = eyeDepth;
				
				output.ase_texcoord7 = input.positionOS;
				output.ase_normal = input.normalOS;
				output.ase_color = input.ase_color;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord8.yzw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = VertexOffset305;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif
				input.normalOS = input.normalOS;
				input.tangentOS = input.tangentOS;

				#ifdef ASE_CUSTOM_MOTION_VECTOR
					// Declared so the Motion Vector output port surfaces on the master node; only consumed by the motion vector passes.
					float3 aseCustomMotionVector = float3(0, 0, 0);
				#endif

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );
				VertexNormalInputs normalInput = GetVertexNormalInputs( input.normalOS, input.tangentOS );

				OUTPUT_LIGHTMAP_UV(input.texcoord1, unity_LightmapST, output.lightmapUVOrVertexSH.xy);
				#if defined(DYNAMICLIGHTMAP_ON)
					output.dynamicLightmapUV.xy = input.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
				#endif
				OUTPUT_SH4(vertexInput.positionWS, normalInput.normalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), output.lightmapUVOrVertexSH.xyz, output.probeOcclusion);

				#if defined(ASE_FOG) || defined(_ADDITIONAL_LIGHTS_VERTEX)
					output.fogFactorAndVertexLight = 0;
					#if defined(ASE_FOG) && !defined(_FOG_FRAGMENT)
						output.fogFactorAndVertexLight.x = ComputeFogFactor(vertexInput.positionCS.z);
					#endif
					#ifdef _ADDITIONAL_LIGHTS_VERTEX
						half3 vertexLight = VertexLighting( vertexInput.positionWS, normalInput.normalWS );
						output.fogFactorAndVertexLight.yzw = vertexLight;
					#endif
				#endif

				output.positionCS = ASE_ADJUST_CLIP_POSITION( vertexInput.positionCS );
				output.positionWS = vertexInput.positionWS;
				output.normalWS = normalInput.normalWS;
				output.tangentWS = float4( normalInput.tangentWS, ( input.tangentOS.w > 0.0 ? 1.0 : -1.0 ) * GetOddNegativeScale() );

				#if defined( ENABLE_TERRAIN_PERPIXEL_NORMAL )
					output.tangentWS.zw = input.texcoord.xy;
					output.tangentWS.xy = input.texcoord.xy * unity_LightmapST.xy + unity_LightmapST.zw;
				#endif
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 texcoord : TEXCOORD0;
				#if defined(LIGHTMAP_ON) || defined(ASE_NEEDS_TEXTURE_COORDINATES1)
					float4 texcoord1 : TEXCOORD1;
				#endif
				#if defined(DYNAMICLIGHTMAP_ON) || defined(ASE_NEEDS_TEXTURE_COORDINATES2)
					float4 texcoord2 : TEXCOORD2;
				#endif
				float4 ase_color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.tangentOS = input.tangentOS;
				output.texcoord = input.texcoord;
				#if defined(LIGHTMAP_ON) || defined(ASE_NEEDS_TEXTURE_COORDINATES1)
					output.texcoord1 = input.texcoord1;
				#endif
				#if defined(DYNAMICLIGHTMAP_ON) || defined(ASE_NEEDS_TEXTURE_COORDINATES2)
					output.texcoord2 = input.texcoord2;
				#endif
				output.ase_color = input.ase_color;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.tangentOS = patch[0].tangentOS * bary.x + patch[1].tangentOS * bary.y + patch[2].tangentOS * bary.z;
				output.texcoord = patch[0].texcoord * bary.x + patch[1].texcoord * bary.y + patch[2].texcoord * bary.z;
				#if defined(LIGHTMAP_ON) || defined(ASE_NEEDS_TEXTURE_COORDINATES1)
					output.texcoord1 = patch[0].texcoord1 * bary.x + patch[1].texcoord1 * bary.y + patch[2].texcoord1 * bary.z;
				#endif
				#if defined(DYNAMICLIGHTMAP_ON) || defined(ASE_NEEDS_TEXTURE_COORDINATES2)
					output.texcoord2 = patch[0].texcoord2 * bary.x + patch[1].texcoord2 * bary.y + patch[2].texcoord2 * bary.z;
				#endif
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag ( PackedVaryings input
						#if defined( ASE_WRITE_DEPTH )
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						#ifdef _WRITE_RENDERING_LAYERS
						#if ( UNITY_VERSION >= 60020000 )
						, out uint outRenderingLayers : SV_Target1
						#else
						, out float4 outRenderingLayers : SV_Target1
						#endif
						#endif
						 ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				#if defined( _SURFACE_TYPE_TRANSPARENT )
					const bool isTransparent = true;
				#else
					const bool isTransparent = false;
				#endif

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
					float4 shadowCoord = TransformWorldToShadowCoord( input.positionWS );
				#else
					float4 shadowCoord = float4(0, 0, 0, 0);
				#endif

				// @diogo: mikktspace compliant
				float renormFactor = 1.0 / max( FLT_MIN, length( input.normalWS ) );

				float3 PositionWS = input.positionWS;
				float3 PositionRWS = GetCameraRelativePositionWS( PositionWS );
				float3 ViewDirWS = GetWorldSpaceNormalizeViewDir( PositionWS );
				float4 ShadowCoord = shadowCoord;
				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;
				float4 ScreenPos = ComputeScreenPos( ClipPos );
				float3 TangentWS = input.tangentWS.xyz * renormFactor;
				float3 BitangentWS = cross( input.normalWS, input.tangentWS.xyz ) * input.tangentWS.w * renormFactor;
				float3 NormalWS = input.normalWS * renormFactor;

				#if defined( ENABLE_TERRAIN_PERPIXEL_NORMAL )
					float2 sampleCoords = (input.tangentWS.zw / _TerrainHeightmapRecipSize.zw + 0.5f) * _TerrainHeightmapRecipSize.xy;
					NormalWS = TransformObjectToWorldNormal(normalize(SAMPLE_TEXTURE2D(_TerrainNormalmapTexture, sampler_TerrainNormalmapTexture, sampleCoords).rgb * 2 - 1));
					TangentWS = -cross(GetObjectToWorldMatrix()._13_23_33, NormalWS);
					BitangentWS = cross(NormalWS, -TangentWS);
				#endif

				#if ( SHADER_TARGET >= 50 )
				float recip367 = rcp( _Scale );
				#else
				float recip367 = 1.0 / _Scale;
				#endif
				float Scale228 = recip367;
				float4 appendResult329 = (float4(_NoiseScale3D.x , _NoiseScale3D.y , _NoiseScale3D.z , 0.0));
				float3 WindDir317 = ( ( float3( 1, 1, 1 ) * -2 + 1 ) * _WindDirection );
				float3 break332 = WindDir317;
				float temp_output_333_0 = ( abs( break332.x ) + abs( break332.y ) + abs( break332.z ) );
				float4 appendResult340 = (float4(( break332.x / temp_output_333_0 ) , ( break332.y / temp_output_333_0 ) , ( break332.z / temp_output_333_0 ) , 0.0));
				float4 WindDirWeights341 = appendResult340;
				float4 break344 = abs( WindDirWeights341 );
				float4 appendResult330 = (float4(_NoiseScale3D.y , _NoiseScale3D.x , _NoiseScale3D.z , 0.0));
				float4 appendResult331 = (float4(_NoiseScale3D.y , _NoiseScale3D.z , _NoiseScale3D.x , 0.0));
				float4 transform70 = mul(GetObjectToWorldMatrix(),input.ase_texcoord7);
				float4 temp_output_71_0 = (transform70).xyzw;
				float simplePerlin3D112 = snoise( ( Scale228 * ( ( ( appendResult329 * break344.x ) + ( appendResult330 * break344.y ) + ( appendResult331 * break344.z ) ) * (temp_output_71_0*1.0 + float4( ( ( WindDir317 + _AdditionalNoiseDir ) * ( _NoiseSpeed * _TimeParameters.x * _WindSpeed ) ) , 0.0 )) ) ).xyz*_NoiseScale );
				simplePerlin3D112 = simplePerlin3D112*0.5 + 0.5;
				float temp_output_118_0 = saturate(  (0.0 + ( simplePerlin3D112 - 0.0 ) * ( 1.0 - 0.0 ) / ( 1.0 - 0.0 ) ) );
				float NoiseColor3D151 = temp_output_118_0;
				float temp_output_198_0 = ( Scale228 * _VoronoiScale );
				float time97 = 0.0;
				float2 voronoiSmoothId97 = 0;
				float4 break84 = (temp_output_71_0*1.0 + float4( ( ( _TimeParameters.x * _VorSpeed * _WindSpeed ) * ( WindDir317 + _AdditionalVoronoiDir ) ) , 0.0 ));
				float2 appendResult85 = (float2(break84.z , break84.y));
				float2 coords97 = appendResult85 * temp_output_198_0;
				float2 id97 = 0;
				float2 uv97 = 0;
				float fade97 = 0.5;
				float voroi97 = 0;
				float rest97 = 0;
				for( int it97 = 0; it97 <3; it97++ ){
				voroi97 += fade97 * voronoi97( coords97, time97, id97, uv97, 0,voronoiSmoothId97 );
				rest97 += fade97;
				coords97 *= 2;
				fade97 *= 0.5;
				}//Voronoi97
				voroi97 /= rest97;
				float3 objToWorldDir91 = mul( GetObjectToWorldMatrix(), float4( input.ase_normal, 0.0 ) ).xyz;
				float3 temp_output_93_0 = pow( abs( objToWorldDir91 ) , 5.0 );
				float dotResult94 = dot( temp_output_93_0 , float3( 1,1,1 ) );
				float3 break104 = ( temp_output_93_0 / dotResult94 );
				float time96 = 217.0;
				float2 voronoiSmoothId96 = 0;
				float2 appendResult86 = (float2(break84.z , break84.x));
				float2 coords96 = appendResult86 * temp_output_198_0;
				float2 id96 = 0;
				float2 uv96 = 0;
				float fade96 = 0.5;
				float voroi96 = 0;
				float rest96 = 0;
				for( int it96 = 0; it96 <3; it96++ ){
				voroi96 += fade96 * voronoi96( coords96, time96, id96, uv96, 0,voronoiSmoothId96 );
				rest96 += fade96;
				coords96 *= 2;
				fade96 *= 0.5;
				}//Voronoi96
				voroi96 /= rest96;
				float time89 = 137.0;
				float2 voronoiSmoothId89 = 0;
				float2 appendResult87 = (float2(break84.x , break84.y));
				float2 coords89 = appendResult87 * temp_output_198_0;
				float2 id89 = 0;
				float2 uv89 = 0;
				float fade89 = 0.5;
				float voroi89 = 0;
				float rest89 = 0;
				for( int it89 = 0; it89 <3; it89++ ){
				voroi89 += fade89 * voronoi89( coords89, time89, id89, uv89, 0,voronoiSmoothId89 );
				rest89 += fade89;
				coords89 *= 2;
				fade89 *= 0.5;
				}//Voronoi89
				voroi89 /= rest89;
				float temp_output_190_0 = saturate(  (0.0 + ( ( 1.0 - ( ( ( voroi97 * break104.x ) + ( voroi96 * break104.y ) ) + ( voroi89 * break104.z ) ) ) - 0.65 ) * ( 1.0 - 0.0 ) / ( 1.0 - 0.65 ) ) );
				float NoiseColorVoronoi152 = temp_output_190_0;
				float lerpResult157 = lerp( NoiseColor3D151 , NoiseColorVoronoi152 , _NoiseColorMix);
				float NoiseColorMix158 = lerpResult157;
				float3 temp_cast_3 = (NoiseColorMix158).xxx;
				float temp_output_2_0_g163 = _NoiseColorStrength;
				float temp_output_3_0_g163 = ( 1.0 - temp_output_2_0_g163 );
				float3 appendResult7_g163 = (float3(temp_output_3_0_g163 , temp_output_3_0_g163 , temp_output_3_0_g163));
				float3 temp_output_21_0 = ( ( temp_cast_3 * temp_output_2_0_g163 ) + appendResult7_g163 );
				float4 lerpResult161 = lerp( _BaseColor , input.ase_color , _VertexColorStrength);
				float4 Albedo30 = saturate( ( float4( temp_output_21_0 , 0.0 ) * lerpResult161 ) );
				
				float3 temp_cast_6 = (NoiseColorMix158).xxx;
				float temp_output_2_0_g161 = _EmmissiveNoiseColor;
				float temp_output_3_0_g161 = ( 1.0 - temp_output_2_0_g161 );
				float3 appendResult7_g161 = (float3(temp_output_3_0_g161 , temp_output_3_0_g161 , temp_output_3_0_g161));
				float depth01_368 = SHADERGRAPH_SAMPLE_SCENE_DEPTH( ScreenPosNorm.xy );
				float temp_output_385_0 = abs( ( _ProjectionParams.z - _ProjectionParams.y ) );
				float eyeDepth = input.ase_texcoord8.x;
				float cameraDepthFade389 = (( eyeDepth -_ProjectionParams.y - 0.0 ) / temp_output_385_0);
				float temp_output_1_0_g160 = saturate( ( ( ( ( 1.0 - depth01_368 ) - cameraDepthFade389 ) * temp_output_385_0 ) / _DepthDistance ) );
				float DepthFade43 = saturate( ( ( ( pow( temp_output_1_0_g160 , 5.0 ) * 6.0 ) - ( pow( temp_output_1_0_g160 , 4.0 ) * 15.0 ) ) + ( pow( temp_output_1_0_g160 , 3.0 ) * 10.0 ) ) );
				float3 temp_cast_8 = (DepthFade43).xxx;
				float temp_output_2_0_g162 = _EmmissiveDepthFade;
				float temp_output_3_0_g162 = ( 1.0 - temp_output_2_0_g162 );
				float3 appendResult7_g162 = (float3(temp_output_3_0_g162 , temp_output_3_0_g162 , temp_output_3_0_g162));
				
				float4 temp_output_2_0_g155 = lerpResult161;
				float BaseColorAlpha162 = (temp_output_2_0_g155).a;
				

				float3 BaseColor = Albedo30.rgb;
				float3 Normal = float3(0, 0, 1);
				float3 Specular = 0.5;
				float Metallic = 0.0;
				float Smoothness = 0.0;
				float Occlusion = 1;
				float3 Emission = ( ( _EmmissiveColor * float4( ( ( temp_cast_6 * temp_output_2_0_g161 ) + appendResult7_g161 ) , 0.0 ) ) * float4( ( ( temp_cast_8 * temp_output_2_0_g162 ) + appendResult7_g162 ) , 0.0 ) ).rgb;
				float Alpha = ( DepthFade43 * BaseColorAlpha162 );
				#if defined( _ALPHATEST_ON )
					float AlphaClipThreshold = 0.0;
					float AlphaClipThresholdShadow = 0.5;
				#endif
				float3 BakedGI = 0;
				float3 RefractionColor = 1;
				float RefractionIndex = 1;
				float3 Transmission = 1;
				float3 Translucency = 1;

				#if defined( ASE_WRITE_DEPTH )
					input.positionCS.z = input.positionCS.z;
				#endif

				#ifdef _CLEARCOAT
					float CoatMask = 0;
					float CoatSmoothness = 0;
				#endif

				#if defined( _ALPHATEST_ON )
					AlphaDiscard( Alpha, AlphaClipThreshold );
				#endif

				#if defined(MAIN_LIGHT_CALCULATE_SHADOWS) && defined(ASE_CHANGES_WORLD_POS)
					ShadowCoord = TransformWorldToShadowCoord( PositionWS );
				#endif

				InputData inputData = (InputData)0;
				inputData.positionWS = PositionWS;
				inputData.positionCS = input.positionCS;
				inputData.normalizedScreenSpaceUV = ScreenPosNorm.xy;
				inputData.viewDirectionWS = ViewDirWS;
				inputData.shadowCoord = ShadowCoord;

				#ifdef _NORMALMAP
						#if _NORMAL_DROPOFF_TS
							inputData.normalWS = TransformTangentToWorld(Normal, half3x3(TangentWS, BitangentWS, NormalWS));
						#elif _NORMAL_DROPOFF_OS
							inputData.normalWS = TransformObjectToWorldNormal(Normal);
						#elif _NORMAL_DROPOFF_WS
							inputData.normalWS = Normal;
						#endif
					inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
				#else
					inputData.normalWS = NormalWS;
				#endif

				#ifdef ASE_FOG
					inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), input.fogFactorAndVertexLight.x);
				#endif
				#ifdef _ADDITIONAL_LIGHTS_VERTEX
					inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
				#endif

				#if defined( ENABLE_TERRAIN_PERPIXEL_NORMAL )
					float3 SH = SampleSH(inputData.normalWS.xyz);
				#else
					float3 SH = input.lightmapUVOrVertexSH.xyz;
				#endif

				#if defined(_SCREEN_SPACE_IRRADIANCE) && ( UNITY_VERSION >= 60030000 )
					#if ( UNITY_VERSION >= 60060000 )
						inputData.bakedGI = SAMPLE_GI(_ScreenSpaceIrradiance, input.positionCS.xy, inputData.normalWS));
					#else
						inputData.bakedGI = SAMPLE_GI(_ScreenSpaceIrradiance, input.positionCS.xy);
					#endif
				#elif defined(DYNAMICLIGHTMAP_ON)
					inputData.bakedGI = SAMPLE_GI(input.lightmapUVOrVertexSH.xy, input.dynamicLightmapUV.xy, SH, inputData.normalWS);
					inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUVOrVertexSH.xy);
				#elif !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
					inputData.bakedGI = SAMPLE_GI( SH, GetAbsolutePositionWS(inputData.positionWS),
						inputData.normalWS,
						inputData.viewDirectionWS,
						input.positionCS.xy,
						input.probeOcclusion,
						inputData.shadowMask );
				#else
					inputData.bakedGI = SAMPLE_GI(input.lightmapUVOrVertexSH.xy, SH, inputData.normalWS);
					inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUVOrVertexSH.xy);
				#endif

				#ifdef ASE_BAKEDGI
					inputData.bakedGI = BakedGI;
				#endif

				#if defined(DEBUG_DISPLAY)
					#if defined(DYNAMICLIGHTMAP_ON)
						inputData.dynamicLightmapUV = input.dynamicLightmapUV.xy;
					#endif
					#if defined(LIGHTMAP_ON)
						inputData.staticLightmapUV = input.lightmapUVOrVertexSH.xy;
					#else
						inputData.vertexSH = SH;
					#endif
					#if defined(USE_APV_PROBE_OCCLUSION)
						inputData.probeOcclusion = input.probeOcclusion;
					#endif
				#endif

				SurfaceData surfaceData;
				surfaceData.albedo              = BaseColor;
				surfaceData.metallic            = saturate(Metallic);
				surfaceData.specular            = Specular;
				surfaceData.smoothness          = saturate(Smoothness),
				surfaceData.occlusion           = Occlusion,
				surfaceData.emission            = Emission,
				surfaceData.alpha               = saturate(Alpha);
				surfaceData.normalTS            = Normal;
				surfaceData.clearCoatMask       = 0;
				surfaceData.clearCoatSmoothness = 1;

				#ifdef _CLEARCOAT
					surfaceData.clearCoatMask       = saturate(CoatMask);
					surfaceData.clearCoatSmoothness = saturate(CoatSmoothness);
				#endif

				#if defined(_DBUFFER)
					ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
				#endif

				#ifdef ASE_LIGHTING_SIMPLE
					half4 color = UniversalFragmentBlinnPhong( inputData, surfaceData);
				#else
					half4 color = UniversalFragmentPBR( inputData, surfaceData);
				#endif

				#ifdef ASE_TRANSMISSION
				{
					float shadow = _TransmissionShadow;

					#define SUM_LIGHT_TRANSMISSION(Light)\
						float3 atten = Light.color * Light.distanceAttenuation;\
						atten = lerp( atten, atten * Light.shadowAttenuation, shadow );\
						half3 transmission = max( 0, -dot( inputData.normalWS, Light.direction ) ) * atten * Transmission;\
						color.rgb += BaseColor * transmission;

					SUM_LIGHT_TRANSMISSION( GetMainLight( inputData.shadowCoord ) );

					#if defined(_ADDITIONAL_LIGHTS)
						uint meshRenderingLayers = GetMeshRenderingLayer();
						uint pixelLightCount = GetAdditionalLightsCount();
						#if USE_CLUSTER_LIGHT_LOOP
							[loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
							{
								CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK

								Light light = GetAdditionalLight(lightIndex, inputData.positionWS, inputData.shadowMask);
								#ifdef _LIGHT_LAYERS
								if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
								#endif
								{
									SUM_LIGHT_TRANSMISSION( light );
								}
							}
						#endif
						LIGHT_LOOP_BEGIN( pixelLightCount )
							Light light = GetAdditionalLight(lightIndex, inputData.positionWS, inputData.shadowMask);
							#ifdef _LIGHT_LAYERS
							if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
							#endif
							{
								SUM_LIGHT_TRANSMISSION( light );
							}
						LIGHT_LOOP_END
					#endif
				}
				#endif

				#ifdef ASE_TRANSLUCENCY
				{
					float shadow = _TransShadow;
					float normal = _TransNormal;
					float scattering = _TransScattering;
					float direct = _TransDirect;
					float ambient = _TransAmbient;
					float strength = _TransStrength;

					#define SUM_LIGHT_TRANSLUCENCY(Light)\
						float3 atten = Light.color * Light.distanceAttenuation;\
						atten = lerp( atten, atten * Light.shadowAttenuation, shadow );\
						half3 lightDir = Light.direction + inputData.normalWS * normal;\
						half VdotL = pow( saturate( dot( inputData.viewDirectionWS, -lightDir ) ), scattering );\
						half3 translucency = atten * ( VdotL * direct + inputData.bakedGI * ambient ) * Translucency;\
						color.rgb += BaseColor * translucency * strength;

					SUM_LIGHT_TRANSLUCENCY( GetMainLight( inputData.shadowCoord ) );

					#if defined(_ADDITIONAL_LIGHTS)
						uint meshRenderingLayers = GetMeshRenderingLayer();
						uint pixelLightCount = GetAdditionalLightsCount();
						#if USE_CLUSTER_LIGHT_LOOP
							[loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
							{
								CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK

								Light light = GetAdditionalLight(lightIndex, inputData.positionWS, inputData.shadowMask);
								#ifdef _LIGHT_LAYERS
								if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
								#endif
								{
									SUM_LIGHT_TRANSLUCENCY( light );
								}
							}
						#endif
						LIGHT_LOOP_BEGIN( pixelLightCount )
							Light light = GetAdditionalLight(lightIndex, inputData.positionWS, inputData.shadowMask);
							#ifdef _LIGHT_LAYERS
							if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
							#endif
							{
								SUM_LIGHT_TRANSLUCENCY( light );
							}
						LIGHT_LOOP_END
					#endif
				}
				#endif

				#ifdef ASE_REFRACTION
					float4 projScreenPos = ScreenPos / ScreenPos.w;
					float3 refractionOffset = ( RefractionIndex - 1.0 ) * mul( UNITY_MATRIX_V, float4( NormalWS,0 ) ).xyz * ( 1.0 - dot( NormalWS, ViewDirWS ) );
					projScreenPos.xy += refractionOffset.xy;
					float3 refraction = SHADERGRAPH_SAMPLE_SCENE_COLOR( projScreenPos.xy ) * RefractionColor;
					color.rgb = lerp( refraction, color.rgb, color.a );
					color.a = 1;
				#endif

				#ifdef ASE_FINAL_COLOR_ALPHA_MULTIPLY
					color.rgb *= color.a;
				#endif

				#ifdef ASE_FOG
					#ifdef TERRAIN_SPLAT_ADDPASS
						color.rgb = MixFogColor(color.rgb, half3(0,0,0), inputData.fogCoord);
					#else
						color.rgb = MixFog(color.rgb, inputData.fogCoord);
					#endif
				#endif

				#if defined( ASE_WRITE_DEPTH )
					outputDepth = input.positionCS.z;
				#endif

				#ifdef _WRITE_RENDERING_LAYERS
					#if ( UNITY_VERSION >= 60020000 )
					outRenderingLayers = EncodeMeshRenderingLayer();
					#else
					uint renderingLayers = GetMeshRenderingLayer();
					outRenderingLayers = float4( EncodeMeshRenderingLayer( renderingLayers ), 0, 0, 0 );
					#endif
				#endif

				#if defined( ASE_OPAQUE_KEEP_ALPHA )
					return half4( color.rgb, color.a );
				#else
					return half4( color.rgb, OutputAlpha( color.a, isTransparent ) );
				#endif
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }

			ZWrite On
			ColorMask R
			AlphaToMask Off

			HLSLPROGRAM

			#define ASE_GEOMETRY
			#define _ALPHATEST_ON
			#define _NORMAL_DROPOFF_TS 1
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#define ASE_FOG 1
			#define ASE_WRITE_DEPTH
			#define ASE_TRANSLUCENCY 1
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define _EMISSION
			#define ASE_VERSION 19912
			#define ASE_SRP_VERSION 170300
			#define REQUIRE_DEPTH_TEXTURE 1


			#pragma vertex vert
			#pragma fragment frag

			#if defined( _SPECULAR_SETUP ) && defined( ASE_LIGHTING_SIMPLE )
				#if defined( _SPECULARHIGHLIGHTS_OFF )
					#undef _SPECULAR_COLOR
				#else
					#define _SPECULAR_COLOR
				#endif
			#endif

			#define SHADERPASS SHADERPASS_DEPTHONLY

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_VERT_NORMAL
			#define ASE_NEEDS_FRAG_SCREEN_POSITION_NORMALIZED


			#if defined(ASE_WRITE_DEPTH_CONSERVATIVE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _EmmissiveColor;
			float4 _BaseColor;
			float4 _Mask1;
			float4 _Mask0;
			float3 _NoiseScale3D;
			float3 _WindDirection;
			float3 _AdditionalNoiseDir;
			float3 _AdditionalVoronoiDir;
			float _Scale;
			float _EmmissiveNoiseColor;
			float _VertexColorStrength;
			float _NoiseColorStrength;
			float _NoiseColorMix;
			float _VoronoiStrength;
			float _DepthDistance;
			float _VorSpeed;
			float _VoronoiScale;
			float _NoiseStrength;
			float _NoiseScale;
			float _WindSpeed;
			float _NoiseSpeed;
			float _NoiseDisplacement;
			float _EmmissiveDepthFade;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TRANSMISSION
				float _TransmissionShadow;
			#endif
			#ifdef ASE_TRANSLUCENCY
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			

			float3 mod3D289( float3 x ) { return x - floor( x / 289.0 ) * 289.0; }
			float4 mod3D289( float4 x ) { return x - floor( x / 289.0 ) * 289.0; }
			float4 permute( float4 x ) { return mod3D289( ( x * 34.0 + 1.0 ) * x ); }
			float4 taylorInvSqrt( float4 r ) { return 1.79284291400159 - r * 0.85373472095314; }
			float snoise( float3 v )
			{
				const float2 C = float2( 1.0 / 6.0, 1.0 / 3.0 );
				float3 i = floor( v + dot( v, C.yyy ) );
				float3 x0 = v - i + dot( i, C.xxx );
				float3 g = step( x0.yzx, x0.xyz );
				float3 l = 1.0 - g;
				float3 i1 = min( g.xyz, l.zxy );
				float3 i2 = max( g.xyz, l.zxy );
				float3 x1 = x0 - i1 + C.xxx;
				float3 x2 = x0 - i2 + C.yyy;
				float3 x3 = x0 - 0.5;
				i = mod3D289( i);
				float4 p = permute( permute( permute( i.z + float4( 0.0, i1.z, i2.z, 1.0 ) ) + i.y + float4( 0.0, i1.y, i2.y, 1.0 ) ) + i.x + float4( 0.0, i1.x, i2.x, 1.0 ) );
				float4 j = p - 49.0 * floor( p / 49.0 );  // mod(p,7*7)
				float4 x_ = floor( j / 7.0 );
				float4 y_ = floor( j - 7.0 * x_ );  // mod(j,N)
				float4 x = ( x_ * 2.0 + 0.5 ) / 7.0 - 1.0;
				float4 y = ( y_ * 2.0 + 0.5 ) / 7.0 - 1.0;
				float4 h = 1.0 - abs( x ) - abs( y );
				float4 b0 = float4( x.xy, y.xy );
				float4 b1 = float4( x.zw, y.zw );
				float4 s0 = floor( b0 ) * 2.0 + 1.0;
				float4 s1 = floor( b1 ) * 2.0 + 1.0;
				float4 sh = -step( h, 0.0 );
				float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
				float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
				float3 g0 = float3( a0.xy, h.x );
				float3 g1 = float3( a0.zw, h.y );
				float3 g2 = float3( a1.xy, h.z );
				float3 g3 = float3( a1.zw, h.w );
				float4 norm = taylorInvSqrt( float4( dot( g0, g0 ), dot( g1, g1 ), dot( g2, g2 ), dot( g3, g3 ) ) );
				g0 *= norm.x;
				g1 *= norm.y;
				g2 *= norm.z;
				g3 *= norm.w;
				float4 m = max( 0.6 - float4( dot( x0, x0 ), dot( x1, x1 ), dot( x2, x2 ), dot( x3, x3 ) ), 0.0 );
				m = m* m;
				m = m* m;
				float4 px = float4( dot( x0, g0 ), dot( x1, g1 ), dot( x2, g2 ), dot( x3, g3 ) );
				return 42.0 * dot( m, px);
			}
			
					float2 voronoihash97( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi97( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0; int i, j;
						for ( j = -1; j <= 1; j++ )
						{
							for ( i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash97( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			
					float2 voronoihash96( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi96( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0; int i, j;
						for ( j = -1; j <= 1; j++ )
						{
							for ( i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash96( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			
					float2 voronoihash89( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi89( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0; int i, j;
						for ( j = -1; j <= 1; j++ )
						{
							for ( i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash89( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			

			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				#if ( SHADER_TARGET >= 50 )
				float recip367 = rcp( _Scale );
				#else
				float recip367 = 1.0 / _Scale;
				#endif
				float Scale228 = recip367;
				float4 appendResult329 = (float4(_NoiseScale3D.x , _NoiseScale3D.y , _NoiseScale3D.z , 0.0));
				float3 WindDir317 = ( ( float3( 1, 1, 1 ) * -2 + 1 ) * _WindDirection );
				float3 break332 = WindDir317;
				float temp_output_333_0 = ( abs( break332.x ) + abs( break332.y ) + abs( break332.z ) );
				float4 appendResult340 = (float4(( break332.x / temp_output_333_0 ) , ( break332.y / temp_output_333_0 ) , ( break332.z / temp_output_333_0 ) , 0.0));
				float4 WindDirWeights341 = appendResult340;
				float4 break344 = abs( WindDirWeights341 );
				float4 appendResult330 = (float4(_NoiseScale3D.y , _NoiseScale3D.x , _NoiseScale3D.z , 0.0));
				float4 appendResult331 = (float4(_NoiseScale3D.y , _NoiseScale3D.z , _NoiseScale3D.x , 0.0));
				float4 transform70 = mul(GetObjectToWorldMatrix(),input.positionOS);
				float4 temp_output_71_0 = (transform70).xyzw;
				float simplePerlin3D112 = snoise( ( Scale228 * ( ( ( appendResult329 * break344.x ) + ( appendResult330 * break344.y ) + ( appendResult331 * break344.z ) ) * (temp_output_71_0*1.0 + float4( ( ( WindDir317 + _AdditionalNoiseDir ) * ( _NoiseSpeed * _TimeParameters.x * _WindSpeed ) ) , 0.0 )) ) ).xyz*_NoiseScale );
				simplePerlin3D112 = simplePerlin3D112*0.5 + 0.5;
				float temp_output_118_0 = saturate(  (0.0 + ( simplePerlin3D112 - 0.0 ) * ( 1.0 - 0.0 ) / ( 1.0 - 0.0 ) ) );
				float temp_output_198_0 = ( Scale228 * _VoronoiScale );
				float time97 = 0.0;
				float2 voronoiSmoothId97 = 0;
				float4 break84 = (temp_output_71_0*1.0 + float4( ( ( _TimeParameters.x * _VorSpeed * _WindSpeed ) * ( WindDir317 + _AdditionalVoronoiDir ) ) , 0.0 ));
				float2 appendResult85 = (float2(break84.z , break84.y));
				float2 coords97 = appendResult85 * temp_output_198_0;
				float2 id97 = 0;
				float2 uv97 = 0;
				float fade97 = 0.5;
				float voroi97 = 0;
				float rest97 = 0;
				for( int it97 = 0; it97 <3; it97++ ){
				voroi97 += fade97 * voronoi97( coords97, time97, id97, uv97, 0,voronoiSmoothId97 );
				rest97 += fade97;
				coords97 *= 2;
				fade97 *= 0.5;
				}//Voronoi97
				voroi97 /= rest97;
				float3 objToWorldDir91 = mul( GetObjectToWorldMatrix(), float4( input.normalOS, 0.0 ) ).xyz;
				float3 temp_output_93_0 = pow( abs( objToWorldDir91 ) , 5.0 );
				float dotResult94 = dot( temp_output_93_0 , float3( 1,1,1 ) );
				float3 break104 = ( temp_output_93_0 / dotResult94 );
				float time96 = 217.0;
				float2 voronoiSmoothId96 = 0;
				float2 appendResult86 = (float2(break84.z , break84.x));
				float2 coords96 = appendResult86 * temp_output_198_0;
				float2 id96 = 0;
				float2 uv96 = 0;
				float fade96 = 0.5;
				float voroi96 = 0;
				float rest96 = 0;
				for( int it96 = 0; it96 <3; it96++ ){
				voroi96 += fade96 * voronoi96( coords96, time96, id96, uv96, 0,voronoiSmoothId96 );
				rest96 += fade96;
				coords96 *= 2;
				fade96 *= 0.5;
				}//Voronoi96
				voroi96 /= rest96;
				float time89 = 137.0;
				float2 voronoiSmoothId89 = 0;
				float2 appendResult87 = (float2(break84.x , break84.y));
				float2 coords89 = appendResult87 * temp_output_198_0;
				float2 id89 = 0;
				float2 uv89 = 0;
				float fade89 = 0.5;
				float voroi89 = 0;
				float rest89 = 0;
				for( int it89 = 0; it89 <3; it89++ ){
				voroi89 += fade89 * voronoi89( coords89, time89, id89, uv89, 0,voronoiSmoothId89 );
				rest89 += fade89;
				coords89 *= 2;
				fade89 *= 0.5;
				}//Voronoi89
				voroi89 /= rest89;
				float temp_output_190_0 = saturate(  (0.0 + ( ( 1.0 - ( ( ( voroi97 * break104.x ) + ( voroi96 * break104.y ) ) + ( voroi89 * break104.z ) ) ) - 0.65 ) * ( 1.0 - 0.0 ) / ( 1.0 - 0.65 ) ) );
				float temp_output_122_0 = saturate( ( ( temp_output_118_0 * _NoiseStrength ) + ( temp_output_190_0 * _VoronoiStrength ) ) );
				float3 ase_objectScale = float3( length( GetObjectToWorldMatrix()[ 0 ].xyz ), length( GetObjectToWorldMatrix()[ 1 ].xyz ), length( GetObjectToWorldMatrix()[ 2 ].xyz ) );
				float3 temp_output_184_0 = ( ( ( temp_output_122_0 - 0.5 ) * input.normalOS * ( _NoiseDisplacement / Scale228 ) ) / ase_objectScale );
				float4 temp_output_2_0_g146 = _Mask0;
				float3 worldToObj306 = mul( GetWorldToObjectMatrix(), float4( (temp_output_2_0_g146).xyz, 1 ) ).xyz;
				float3 ase_objectPosition = GetAbsolutePositionWS( UNITY_MATRIX_M._m03_m13_m23 );
				float4 appendResult313 = (float4((temp_output_2_0_g146).w , 0.0 , 0.0 , 0.0));
				float3 worldToObj307 = mul( GetWorldToObjectMatrix(), float4( ( float4( ase_objectPosition , 0.0 ) + appendResult313 ).xyz, 1 ) ).xyz;
				float3 temp_output_304_0 = ( temp_output_184_0 + ( min( ( distance( ( temp_output_184_0 + input.positionOS.xyz ) , worldToObj306 ) - length( worldToObj307 ) ), 0.0 ) * input.normalOS ) );
				float4 temp_output_2_0_g153 = _Mask1;
				float3 worldToObj357 = mul( GetWorldToObjectMatrix(), float4( (temp_output_2_0_g153).xyz, 1 ) ).xyz;
				float4 appendResult354 = (float4((temp_output_2_0_g153).w , 0.0 , 0.0 , 0.0));
				float3 worldToObj358 = mul( GetWorldToObjectMatrix(), float4( ( float4( ase_objectPosition , 0.0 ) + appendResult354 ).xyz, 1 ) ).xyz;
				float3 VertexOffset305 = ( temp_output_304_0 + ( min( ( distance( ( temp_output_304_0 + input.positionOS.xyz ) , worldToObj357 ) - length( worldToObj358 ) ), 0.0 ) * input.normalOS ) );
				
				float3 objectToViewPos = TransformWorldToView( TransformObjectToWorld( input.positionOS.xyz ) );
				float eyeDepth = -objectToViewPos.z;
				output.ase_texcoord1.x = eyeDepth;
				
				output.ase_color = input.ase_color;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord1.yzw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = VertexOffset305;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;
				input.tangentOS = input.tangentOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				output.positionCS = ASE_ADJUST_CLIP_POSITION( vertexInput.positionCS );
				output.positionWS = vertexInput.positionWS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.tangentOS = input.tangentOS;
				output.ase_color = input.ase_color;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.tangentOS = patch[0].tangentOS * bary.x + patch[1].tangentOS * bary.y + patch[2].tangentOS * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(	PackedVaryings input
						#if defined( ASE_WRITE_DEPTH )
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						 ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				#if defined(MAIN_LIGHT_CALCULATE_SHADOWS) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
				#else
					float4 shadowCoord = float4(0, 0, 0, 0);
				#endif

				float3 PositionWS = input.positionWS;
				float3 PositionRWS = GetCameraRelativePositionWS( input.positionWS );
				float4 ShadowCoord = shadowCoord;
				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;
				float4 ScreenPos = ComputeScreenPos( ClipPos );

				float depth01_368 = SHADERGRAPH_SAMPLE_SCENE_DEPTH( ScreenPosNorm.xy );
				float temp_output_385_0 = abs( ( _ProjectionParams.z - _ProjectionParams.y ) );
				float eyeDepth = input.ase_texcoord1.x;
				float cameraDepthFade389 = (( eyeDepth -_ProjectionParams.y - 0.0 ) / temp_output_385_0);
				float temp_output_1_0_g160 = saturate( ( ( ( ( 1.0 - depth01_368 ) - cameraDepthFade389 ) * temp_output_385_0 ) / _DepthDistance ) );
				float DepthFade43 = saturate( ( ( ( pow( temp_output_1_0_g160 , 5.0 ) * 6.0 ) - ( pow( temp_output_1_0_g160 , 4.0 ) * 15.0 ) ) + ( pow( temp_output_1_0_g160 , 3.0 ) * 10.0 ) ) );
				float4 lerpResult161 = lerp( _BaseColor , input.ase_color , _VertexColorStrength);
				float4 temp_output_2_0_g155 = lerpResult161;
				float BaseColorAlpha162 = (temp_output_2_0_g155).a;
				

				float Alpha = ( DepthFade43 * BaseColorAlpha162 );
				#if defined( _ALPHATEST_ON )
					float AlphaClipThreshold = 0.0;
				#endif

				#if defined( ASE_WRITE_DEPTH )
					input.positionCS.z = input.positionCS.z;
				#endif

				#if defined( _ALPHATEST_ON )
					AlphaDiscard( Alpha, AlphaClipThreshold );
				#endif

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined( ASE_WRITE_DEPTH )
					outputDepth = input.positionCS.z;
				#endif

				return 0;
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "Meta"
			Tags { "LightMode"="Meta" }

			Cull Off

			HLSLPROGRAM
			#define ASE_GEOMETRY
			#define _ALPHATEST_ON
			#define _NORMAL_DROPOFF_TS 1
			#define ASE_FOG 1
			#define ASE_WRITE_DEPTH
			#define ASE_TRANSLUCENCY 1
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define _EMISSION
			#define ASE_VERSION 19912
			#define ASE_SRP_VERSION 170300
			#define REQUIRE_DEPTH_TEXTURE 1

			#pragma shader_feature EDITOR_VISUALIZATION

			#pragma vertex vert
			#pragma fragment frag

			#if defined( _SPECULAR_SETUP ) && defined( ASE_LIGHTING_SIMPLE )
				#if defined( _SPECULARHIGHLIGHTS_OFF )
					#undef _SPECULAR_COLOR
				#else
					#define _SPECULAR_COLOR
				#endif
			#endif

			#define SHADERPASS SHADERPASS_META

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_VERT_NORMAL


			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 texcoord : TEXCOORD0;
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				#ifdef EDITOR_VISUALIZATION
					float4 VizUV : TEXCOORD1;
					float4 LightCoord : TEXCOORD2;
				#endif
				float4 ase_texcoord3 : TEXCOORD3;
				float3 ase_normal : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 ase_texcoord5 : TEXCOORD5;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _EmmissiveColor;
			float4 _BaseColor;
			float4 _Mask1;
			float4 _Mask0;
			float3 _NoiseScale3D;
			float3 _WindDirection;
			float3 _AdditionalNoiseDir;
			float3 _AdditionalVoronoiDir;
			float _Scale;
			float _EmmissiveNoiseColor;
			float _VertexColorStrength;
			float _NoiseColorStrength;
			float _NoiseColorMix;
			float _VoronoiStrength;
			float _DepthDistance;
			float _VorSpeed;
			float _VoronoiScale;
			float _NoiseStrength;
			float _NoiseScale;
			float _WindSpeed;
			float _NoiseSpeed;
			float _NoiseDisplacement;
			float _EmmissiveDepthFade;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TRANSMISSION
				float _TransmissionShadow;
			#endif
			#ifdef ASE_TRANSLUCENCY
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			

			float3 mod3D289( float3 x ) { return x - floor( x / 289.0 ) * 289.0; }
			float4 mod3D289( float4 x ) { return x - floor( x / 289.0 ) * 289.0; }
			float4 permute( float4 x ) { return mod3D289( ( x * 34.0 + 1.0 ) * x ); }
			float4 taylorInvSqrt( float4 r ) { return 1.79284291400159 - r * 0.85373472095314; }
			float snoise( float3 v )
			{
				const float2 C = float2( 1.0 / 6.0, 1.0 / 3.0 );
				float3 i = floor( v + dot( v, C.yyy ) );
				float3 x0 = v - i + dot( i, C.xxx );
				float3 g = step( x0.yzx, x0.xyz );
				float3 l = 1.0 - g;
				float3 i1 = min( g.xyz, l.zxy );
				float3 i2 = max( g.xyz, l.zxy );
				float3 x1 = x0 - i1 + C.xxx;
				float3 x2 = x0 - i2 + C.yyy;
				float3 x3 = x0 - 0.5;
				i = mod3D289( i);
				float4 p = permute( permute( permute( i.z + float4( 0.0, i1.z, i2.z, 1.0 ) ) + i.y + float4( 0.0, i1.y, i2.y, 1.0 ) ) + i.x + float4( 0.0, i1.x, i2.x, 1.0 ) );
				float4 j = p - 49.0 * floor( p / 49.0 );  // mod(p,7*7)
				float4 x_ = floor( j / 7.0 );
				float4 y_ = floor( j - 7.0 * x_ );  // mod(j,N)
				float4 x = ( x_ * 2.0 + 0.5 ) / 7.0 - 1.0;
				float4 y = ( y_ * 2.0 + 0.5 ) / 7.0 - 1.0;
				float4 h = 1.0 - abs( x ) - abs( y );
				float4 b0 = float4( x.xy, y.xy );
				float4 b1 = float4( x.zw, y.zw );
				float4 s0 = floor( b0 ) * 2.0 + 1.0;
				float4 s1 = floor( b1 ) * 2.0 + 1.0;
				float4 sh = -step( h, 0.0 );
				float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
				float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
				float3 g0 = float3( a0.xy, h.x );
				float3 g1 = float3( a0.zw, h.y );
				float3 g2 = float3( a1.xy, h.z );
				float3 g3 = float3( a1.zw, h.w );
				float4 norm = taylorInvSqrt( float4( dot( g0, g0 ), dot( g1, g1 ), dot( g2, g2 ), dot( g3, g3 ) ) );
				g0 *= norm.x;
				g1 *= norm.y;
				g2 *= norm.z;
				g3 *= norm.w;
				float4 m = max( 0.6 - float4( dot( x0, x0 ), dot( x1, x1 ), dot( x2, x2 ), dot( x3, x3 ) ), 0.0 );
				m = m* m;
				m = m* m;
				float4 px = float4( dot( x0, g0 ), dot( x1, g1 ), dot( x2, g2 ), dot( x3, g3 ) );
				return 42.0 * dot( m, px);
			}
			
					float2 voronoihash97( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi97( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0; int i, j;
						for ( j = -1; j <= 1; j++ )
						{
							for ( i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash97( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			
					float2 voronoihash96( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi96( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0; int i, j;
						for ( j = -1; j <= 1; j++ )
						{
							for ( i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash96( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			
					float2 voronoihash89( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi89( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0; int i, j;
						for ( j = -1; j <= 1; j++ )
						{
							for ( i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash89( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			

			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				#if ( SHADER_TARGET >= 50 )
				float recip367 = rcp( _Scale );
				#else
				float recip367 = 1.0 / _Scale;
				#endif
				float Scale228 = recip367;
				float4 appendResult329 = (float4(_NoiseScale3D.x , _NoiseScale3D.y , _NoiseScale3D.z , 0.0));
				float3 WindDir317 = ( ( float3( 1, 1, 1 ) * -2 + 1 ) * _WindDirection );
				float3 break332 = WindDir317;
				float temp_output_333_0 = ( abs( break332.x ) + abs( break332.y ) + abs( break332.z ) );
				float4 appendResult340 = (float4(( break332.x / temp_output_333_0 ) , ( break332.y / temp_output_333_0 ) , ( break332.z / temp_output_333_0 ) , 0.0));
				float4 WindDirWeights341 = appendResult340;
				float4 break344 = abs( WindDirWeights341 );
				float4 appendResult330 = (float4(_NoiseScale3D.y , _NoiseScale3D.x , _NoiseScale3D.z , 0.0));
				float4 appendResult331 = (float4(_NoiseScale3D.y , _NoiseScale3D.z , _NoiseScale3D.x , 0.0));
				float4 transform70 = mul(GetObjectToWorldMatrix(),input.positionOS);
				float4 temp_output_71_0 = (transform70).xyzw;
				float simplePerlin3D112 = snoise( ( Scale228 * ( ( ( appendResult329 * break344.x ) + ( appendResult330 * break344.y ) + ( appendResult331 * break344.z ) ) * (temp_output_71_0*1.0 + float4( ( ( WindDir317 + _AdditionalNoiseDir ) * ( _NoiseSpeed * _TimeParameters.x * _WindSpeed ) ) , 0.0 )) ) ).xyz*_NoiseScale );
				simplePerlin3D112 = simplePerlin3D112*0.5 + 0.5;
				float temp_output_118_0 = saturate(  (0.0 + ( simplePerlin3D112 - 0.0 ) * ( 1.0 - 0.0 ) / ( 1.0 - 0.0 ) ) );
				float temp_output_198_0 = ( Scale228 * _VoronoiScale );
				float time97 = 0.0;
				float2 voronoiSmoothId97 = 0;
				float4 break84 = (temp_output_71_0*1.0 + float4( ( ( _TimeParameters.x * _VorSpeed * _WindSpeed ) * ( WindDir317 + _AdditionalVoronoiDir ) ) , 0.0 ));
				float2 appendResult85 = (float2(break84.z , break84.y));
				float2 coords97 = appendResult85 * temp_output_198_0;
				float2 id97 = 0;
				float2 uv97 = 0;
				float fade97 = 0.5;
				float voroi97 = 0;
				float rest97 = 0;
				for( int it97 = 0; it97 <3; it97++ ){
				voroi97 += fade97 * voronoi97( coords97, time97, id97, uv97, 0,voronoiSmoothId97 );
				rest97 += fade97;
				coords97 *= 2;
				fade97 *= 0.5;
				}//Voronoi97
				voroi97 /= rest97;
				float3 objToWorldDir91 = mul( GetObjectToWorldMatrix(), float4( input.normalOS, 0.0 ) ).xyz;
				float3 temp_output_93_0 = pow( abs( objToWorldDir91 ) , 5.0 );
				float dotResult94 = dot( temp_output_93_0 , float3( 1,1,1 ) );
				float3 break104 = ( temp_output_93_0 / dotResult94 );
				float time96 = 217.0;
				float2 voronoiSmoothId96 = 0;
				float2 appendResult86 = (float2(break84.z , break84.x));
				float2 coords96 = appendResult86 * temp_output_198_0;
				float2 id96 = 0;
				float2 uv96 = 0;
				float fade96 = 0.5;
				float voroi96 = 0;
				float rest96 = 0;
				for( int it96 = 0; it96 <3; it96++ ){
				voroi96 += fade96 * voronoi96( coords96, time96, id96, uv96, 0,voronoiSmoothId96 );
				rest96 += fade96;
				coords96 *= 2;
				fade96 *= 0.5;
				}//Voronoi96
				voroi96 /= rest96;
				float time89 = 137.0;
				float2 voronoiSmoothId89 = 0;
				float2 appendResult87 = (float2(break84.x , break84.y));
				float2 coords89 = appendResult87 * temp_output_198_0;
				float2 id89 = 0;
				float2 uv89 = 0;
				float fade89 = 0.5;
				float voroi89 = 0;
				float rest89 = 0;
				for( int it89 = 0; it89 <3; it89++ ){
				voroi89 += fade89 * voronoi89( coords89, time89, id89, uv89, 0,voronoiSmoothId89 );
				rest89 += fade89;
				coords89 *= 2;
				fade89 *= 0.5;
				}//Voronoi89
				voroi89 /= rest89;
				float temp_output_190_0 = saturate(  (0.0 + ( ( 1.0 - ( ( ( voroi97 * break104.x ) + ( voroi96 * break104.y ) ) + ( voroi89 * break104.z ) ) ) - 0.65 ) * ( 1.0 - 0.0 ) / ( 1.0 - 0.65 ) ) );
				float temp_output_122_0 = saturate( ( ( temp_output_118_0 * _NoiseStrength ) + ( temp_output_190_0 * _VoronoiStrength ) ) );
				float3 ase_objectScale = float3( length( GetObjectToWorldMatrix()[ 0 ].xyz ), length( GetObjectToWorldMatrix()[ 1 ].xyz ), length( GetObjectToWorldMatrix()[ 2 ].xyz ) );
				float3 temp_output_184_0 = ( ( ( temp_output_122_0 - 0.5 ) * input.normalOS * ( _NoiseDisplacement / Scale228 ) ) / ase_objectScale );
				float4 temp_output_2_0_g146 = _Mask0;
				float3 worldToObj306 = mul( GetWorldToObjectMatrix(), float4( (temp_output_2_0_g146).xyz, 1 ) ).xyz;
				float3 ase_objectPosition = GetAbsolutePositionWS( UNITY_MATRIX_M._m03_m13_m23 );
				float4 appendResult313 = (float4((temp_output_2_0_g146).w , 0.0 , 0.0 , 0.0));
				float3 worldToObj307 = mul( GetWorldToObjectMatrix(), float4( ( float4( ase_objectPosition , 0.0 ) + appendResult313 ).xyz, 1 ) ).xyz;
				float3 temp_output_304_0 = ( temp_output_184_0 + ( min( ( distance( ( temp_output_184_0 + input.positionOS.xyz ) , worldToObj306 ) - length( worldToObj307 ) ), 0.0 ) * input.normalOS ) );
				float4 temp_output_2_0_g153 = _Mask1;
				float3 worldToObj357 = mul( GetWorldToObjectMatrix(), float4( (temp_output_2_0_g153).xyz, 1 ) ).xyz;
				float4 appendResult354 = (float4((temp_output_2_0_g153).w , 0.0 , 0.0 , 0.0));
				float3 worldToObj358 = mul( GetWorldToObjectMatrix(), float4( ( float4( ase_objectPosition , 0.0 ) + appendResult354 ).xyz, 1 ) ).xyz;
				float3 VertexOffset305 = ( temp_output_304_0 + ( min( ( distance( ( temp_output_304_0 + input.positionOS.xyz ) , worldToObj357 ) - length( worldToObj358 ) ), 0.0 ) * input.normalOS ) );
				
				float4 ase_positionCS = TransformObjectToHClip( ( input.positionOS ).xyz );
				float4 screenPos = ComputeScreenPos( ase_positionCS );
				output.ase_texcoord4 = screenPos;
				float3 objectToViewPos = TransformWorldToView( TransformObjectToWorld( input.positionOS.xyz ) );
				float eyeDepth = -objectToViewPos.z;
				output.ase_texcoord5.x = eyeDepth;
				
				output.ase_texcoord3 = input.positionOS;
				output.ase_normal = input.normalOS;
				output.ase_color = input.ase_color;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord5.yzw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = VertexOffset305;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;
				input.tangentOS = input.tangentOS;

				#ifdef EDITOR_VISUALIZATION
					float2 VizUV = 0;
					float4 LightCoord = 0;
					UnityEditorVizData(input.positionOS.xyz, input.texcoord.xy, input.texcoord1.xy, input.texcoord2.xy, VizUV, LightCoord);
					output.VizUV = float4(VizUV, 0, 0);
					output.LightCoord = LightCoord;
				#endif

				output.positionCS = MetaVertexPosition( input.positionOS, input.texcoord1.xy, input.texcoord1.xy, unity_LightmapST, unity_DynamicLightmapST );
				output.positionWS = TransformObjectToWorld( input.positionOS.xyz );
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 texcoord : TEXCOORD0;
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.tangentOS = input.tangentOS;
				output.texcoord = input.texcoord;
				output.texcoord1 = input.texcoord1;
				output.texcoord2 = input.texcoord2;
				output.ase_color = input.ase_color;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.tangentOS = patch[0].tangentOS * bary.x + patch[1].tangentOS * bary.y + patch[2].tangentOS * bary.z;
				output.texcoord = patch[0].texcoord * bary.x + patch[1].texcoord * bary.y + patch[2].texcoord * bary.z;
				output.texcoord1 = patch[0].texcoord1 * bary.x + patch[1].texcoord1 * bary.y + patch[2].texcoord1 * bary.z;
				output.texcoord2 = patch[0].texcoord2 * bary.x + patch[1].texcoord2 * bary.y + patch[2].texcoord2 * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(PackedVaryings input  ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				#if defined(MAIN_LIGHT_CALCULATE_SHADOWS) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
				#else
					float4 shadowCoord = float4(0, 0, 0, 0);
				#endif

				float3 PositionWS = input.positionWS;
				float3 PositionRWS = GetCameraRelativePositionWS( input.positionWS );
				float4 ShadowCoord = shadowCoord;

				#if ( SHADER_TARGET >= 50 )
				float recip367 = rcp( _Scale );
				#else
				float recip367 = 1.0 / _Scale;
				#endif
				float Scale228 = recip367;
				float4 appendResult329 = (float4(_NoiseScale3D.x , _NoiseScale3D.y , _NoiseScale3D.z , 0.0));
				float3 WindDir317 = ( ( float3( 1, 1, 1 ) * -2 + 1 ) * _WindDirection );
				float3 break332 = WindDir317;
				float temp_output_333_0 = ( abs( break332.x ) + abs( break332.y ) + abs( break332.z ) );
				float4 appendResult340 = (float4(( break332.x / temp_output_333_0 ) , ( break332.y / temp_output_333_0 ) , ( break332.z / temp_output_333_0 ) , 0.0));
				float4 WindDirWeights341 = appendResult340;
				float4 break344 = abs( WindDirWeights341 );
				float4 appendResult330 = (float4(_NoiseScale3D.y , _NoiseScale3D.x , _NoiseScale3D.z , 0.0));
				float4 appendResult331 = (float4(_NoiseScale3D.y , _NoiseScale3D.z , _NoiseScale3D.x , 0.0));
				float4 transform70 = mul(GetObjectToWorldMatrix(),input.ase_texcoord3);
				float4 temp_output_71_0 = (transform70).xyzw;
				float simplePerlin3D112 = snoise( ( Scale228 * ( ( ( appendResult329 * break344.x ) + ( appendResult330 * break344.y ) + ( appendResult331 * break344.z ) ) * (temp_output_71_0*1.0 + float4( ( ( WindDir317 + _AdditionalNoiseDir ) * ( _NoiseSpeed * _TimeParameters.x * _WindSpeed ) ) , 0.0 )) ) ).xyz*_NoiseScale );
				simplePerlin3D112 = simplePerlin3D112*0.5 + 0.5;
				float temp_output_118_0 = saturate(  (0.0 + ( simplePerlin3D112 - 0.0 ) * ( 1.0 - 0.0 ) / ( 1.0 - 0.0 ) ) );
				float NoiseColor3D151 = temp_output_118_0;
				float temp_output_198_0 = ( Scale228 * _VoronoiScale );
				float time97 = 0.0;
				float2 voronoiSmoothId97 = 0;
				float4 break84 = (temp_output_71_0*1.0 + float4( ( ( _TimeParameters.x * _VorSpeed * _WindSpeed ) * ( WindDir317 + _AdditionalVoronoiDir ) ) , 0.0 ));
				float2 appendResult85 = (float2(break84.z , break84.y));
				float2 coords97 = appendResult85 * temp_output_198_0;
				float2 id97 = 0;
				float2 uv97 = 0;
				float fade97 = 0.5;
				float voroi97 = 0;
				float rest97 = 0;
				for( int it97 = 0; it97 <3; it97++ ){
				voroi97 += fade97 * voronoi97( coords97, time97, id97, uv97, 0,voronoiSmoothId97 );
				rest97 += fade97;
				coords97 *= 2;
				fade97 *= 0.5;
				}//Voronoi97
				voroi97 /= rest97;
				float3 objToWorldDir91 = mul( GetObjectToWorldMatrix(), float4( input.ase_normal, 0.0 ) ).xyz;
				float3 temp_output_93_0 = pow( abs( objToWorldDir91 ) , 5.0 );
				float dotResult94 = dot( temp_output_93_0 , float3( 1,1,1 ) );
				float3 break104 = ( temp_output_93_0 / dotResult94 );
				float time96 = 217.0;
				float2 voronoiSmoothId96 = 0;
				float2 appendResult86 = (float2(break84.z , break84.x));
				float2 coords96 = appendResult86 * temp_output_198_0;
				float2 id96 = 0;
				float2 uv96 = 0;
				float fade96 = 0.5;
				float voroi96 = 0;
				float rest96 = 0;
				for( int it96 = 0; it96 <3; it96++ ){
				voroi96 += fade96 * voronoi96( coords96, time96, id96, uv96, 0,voronoiSmoothId96 );
				rest96 += fade96;
				coords96 *= 2;
				fade96 *= 0.5;
				}//Voronoi96
				voroi96 /= rest96;
				float time89 = 137.0;
				float2 voronoiSmoothId89 = 0;
				float2 appendResult87 = (float2(break84.x , break84.y));
				float2 coords89 = appendResult87 * temp_output_198_0;
				float2 id89 = 0;
				float2 uv89 = 0;
				float fade89 = 0.5;
				float voroi89 = 0;
				float rest89 = 0;
				for( int it89 = 0; it89 <3; it89++ ){
				voroi89 += fade89 * voronoi89( coords89, time89, id89, uv89, 0,voronoiSmoothId89 );
				rest89 += fade89;
				coords89 *= 2;
				fade89 *= 0.5;
				}//Voronoi89
				voroi89 /= rest89;
				float temp_output_190_0 = saturate(  (0.0 + ( ( 1.0 - ( ( ( voroi97 * break104.x ) + ( voroi96 * break104.y ) ) + ( voroi89 * break104.z ) ) ) - 0.65 ) * ( 1.0 - 0.0 ) / ( 1.0 - 0.65 ) ) );
				float NoiseColorVoronoi152 = temp_output_190_0;
				float lerpResult157 = lerp( NoiseColor3D151 , NoiseColorVoronoi152 , _NoiseColorMix);
				float NoiseColorMix158 = lerpResult157;
				float3 temp_cast_3 = (NoiseColorMix158).xxx;
				float temp_output_2_0_g163 = _NoiseColorStrength;
				float temp_output_3_0_g163 = ( 1.0 - temp_output_2_0_g163 );
				float3 appendResult7_g163 = (float3(temp_output_3_0_g163 , temp_output_3_0_g163 , temp_output_3_0_g163));
				float3 temp_output_21_0 = ( ( temp_cast_3 * temp_output_2_0_g163 ) + appendResult7_g163 );
				float4 lerpResult161 = lerp( _BaseColor , input.ase_color , _VertexColorStrength);
				float4 Albedo30 = saturate( ( float4( temp_output_21_0 , 0.0 ) * lerpResult161 ) );
				
				float3 temp_cast_6 = (NoiseColorMix158).xxx;
				float temp_output_2_0_g161 = _EmmissiveNoiseColor;
				float temp_output_3_0_g161 = ( 1.0 - temp_output_2_0_g161 );
				float3 appendResult7_g161 = (float3(temp_output_3_0_g161 , temp_output_3_0_g161 , temp_output_3_0_g161));
				float4 screenPos = input.ase_texcoord4;
				float4 ase_positionSSNorm = screenPos / screenPos.w;
				ase_positionSSNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_positionSSNorm.z : ase_positionSSNorm.z * 0.5 + 0.5;
				float depth01_368 = SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_positionSSNorm.xy );
				float temp_output_385_0 = abs( ( _ProjectionParams.z - _ProjectionParams.y ) );
				float eyeDepth = input.ase_texcoord5.x;
				float cameraDepthFade389 = (( eyeDepth -_ProjectionParams.y - 0.0 ) / temp_output_385_0);
				float temp_output_1_0_g160 = saturate( ( ( ( ( 1.0 - depth01_368 ) - cameraDepthFade389 ) * temp_output_385_0 ) / _DepthDistance ) );
				float DepthFade43 = saturate( ( ( ( pow( temp_output_1_0_g160 , 5.0 ) * 6.0 ) - ( pow( temp_output_1_0_g160 , 4.0 ) * 15.0 ) ) + ( pow( temp_output_1_0_g160 , 3.0 ) * 10.0 ) ) );
				float3 temp_cast_8 = (DepthFade43).xxx;
				float temp_output_2_0_g162 = _EmmissiveDepthFade;
				float temp_output_3_0_g162 = ( 1.0 - temp_output_2_0_g162 );
				float3 appendResult7_g162 = (float3(temp_output_3_0_g162 , temp_output_3_0_g162 , temp_output_3_0_g162));
				
				float4 temp_output_2_0_g155 = lerpResult161;
				float BaseColorAlpha162 = (temp_output_2_0_g155).a;
				

				float3 BaseColor = Albedo30.rgb;
				float3 Emission = ( ( _EmmissiveColor * float4( ( ( temp_cast_6 * temp_output_2_0_g161 ) + appendResult7_g161 ) , 0.0 ) ) * float4( ( ( temp_cast_8 * temp_output_2_0_g162 ) + appendResult7_g162 ) , 0.0 ) ).rgb;
				float Alpha = ( DepthFade43 * BaseColorAlpha162 );
				#if defined( _ALPHATEST_ON )
					float AlphaClipThreshold = 0.0;
				#endif

				#if defined( _ALPHATEST_ON )
					AlphaDiscard( Alpha, AlphaClipThreshold );
				#endif

				MetaInput metaInput = (MetaInput)0;
				metaInput.Albedo = BaseColor;
				metaInput.Emission = Emission;
				#ifdef EDITOR_VISUALIZATION
					metaInput.VizUV = input.VizUV.xy;
					metaInput.LightCoord = input.LightCoord;
				#endif

				return UnityMetaFragment(metaInput);
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "Universal2D"
			Tags { "LightMode"="Universal2D" }

			Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
			ZWrite On
			ZTest LEqual
			Offset 0 , 0
			ColorMask RGBA

			HLSLPROGRAM

			#define ASE_GEOMETRY
			#define _ALPHATEST_ON
			#define _NORMAL_DROPOFF_TS 1
			#define ASE_FOG 1
			#define ASE_WRITE_DEPTH
			#define ASE_TRANSLUCENCY 1
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define _EMISSION
			#define ASE_VERSION 19912
			#define ASE_SRP_VERSION 170300
			#define REQUIRE_DEPTH_TEXTURE 1


			#pragma vertex vert
			#pragma fragment frag

			#if defined( _SPECULAR_SETUP ) && defined( ASE_LIGHTING_SIMPLE )
				#if defined( _SPECULARHIGHLIGHTS_OFF )
					#undef _SPECULAR_COLOR
				#else
					#define _SPECULAR_COLOR
				#endif
			#endif

			#define SHADERPASS SHADERPASS_2D

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_VERT_NORMAL


			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float3 ase_normal : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _EmmissiveColor;
			float4 _BaseColor;
			float4 _Mask1;
			float4 _Mask0;
			float3 _NoiseScale3D;
			float3 _WindDirection;
			float3 _AdditionalNoiseDir;
			float3 _AdditionalVoronoiDir;
			float _Scale;
			float _EmmissiveNoiseColor;
			float _VertexColorStrength;
			float _NoiseColorStrength;
			float _NoiseColorMix;
			float _VoronoiStrength;
			float _DepthDistance;
			float _VorSpeed;
			float _VoronoiScale;
			float _NoiseStrength;
			float _NoiseScale;
			float _WindSpeed;
			float _NoiseSpeed;
			float _NoiseDisplacement;
			float _EmmissiveDepthFade;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TRANSMISSION
				float _TransmissionShadow;
			#endif
			#ifdef ASE_TRANSLUCENCY
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			

			float3 mod3D289( float3 x ) { return x - floor( x / 289.0 ) * 289.0; }
			float4 mod3D289( float4 x ) { return x - floor( x / 289.0 ) * 289.0; }
			float4 permute( float4 x ) { return mod3D289( ( x * 34.0 + 1.0 ) * x ); }
			float4 taylorInvSqrt( float4 r ) { return 1.79284291400159 - r * 0.85373472095314; }
			float snoise( float3 v )
			{
				const float2 C = float2( 1.0 / 6.0, 1.0 / 3.0 );
				float3 i = floor( v + dot( v, C.yyy ) );
				float3 x0 = v - i + dot( i, C.xxx );
				float3 g = step( x0.yzx, x0.xyz );
				float3 l = 1.0 - g;
				float3 i1 = min( g.xyz, l.zxy );
				float3 i2 = max( g.xyz, l.zxy );
				float3 x1 = x0 - i1 + C.xxx;
				float3 x2 = x0 - i2 + C.yyy;
				float3 x3 = x0 - 0.5;
				i = mod3D289( i);
				float4 p = permute( permute( permute( i.z + float4( 0.0, i1.z, i2.z, 1.0 ) ) + i.y + float4( 0.0, i1.y, i2.y, 1.0 ) ) + i.x + float4( 0.0, i1.x, i2.x, 1.0 ) );
				float4 j = p - 49.0 * floor( p / 49.0 );  // mod(p,7*7)
				float4 x_ = floor( j / 7.0 );
				float4 y_ = floor( j - 7.0 * x_ );  // mod(j,N)
				float4 x = ( x_ * 2.0 + 0.5 ) / 7.0 - 1.0;
				float4 y = ( y_ * 2.0 + 0.5 ) / 7.0 - 1.0;
				float4 h = 1.0 - abs( x ) - abs( y );
				float4 b0 = float4( x.xy, y.xy );
				float4 b1 = float4( x.zw, y.zw );
				float4 s0 = floor( b0 ) * 2.0 + 1.0;
				float4 s1 = floor( b1 ) * 2.0 + 1.0;
				float4 sh = -step( h, 0.0 );
				float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
				float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
				float3 g0 = float3( a0.xy, h.x );
				float3 g1 = float3( a0.zw, h.y );
				float3 g2 = float3( a1.xy, h.z );
				float3 g3 = float3( a1.zw, h.w );
				float4 norm = taylorInvSqrt( float4( dot( g0, g0 ), dot( g1, g1 ), dot( g2, g2 ), dot( g3, g3 ) ) );
				g0 *= norm.x;
				g1 *= norm.y;
				g2 *= norm.z;
				g3 *= norm.w;
				float4 m = max( 0.6 - float4( dot( x0, x0 ), dot( x1, x1 ), dot( x2, x2 ), dot( x3, x3 ) ), 0.0 );
				m = m* m;
				m = m* m;
				float4 px = float4( dot( x0, g0 ), dot( x1, g1 ), dot( x2, g2 ), dot( x3, g3 ) );
				return 42.0 * dot( m, px);
			}
			
					float2 voronoihash97( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi97( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0; int i, j;
						for ( j = -1; j <= 1; j++ )
						{
							for ( i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash97( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			
					float2 voronoihash96( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi96( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0; int i, j;
						for ( j = -1; j <= 1; j++ )
						{
							for ( i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash96( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			
					float2 voronoihash89( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi89( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0; int i, j;
						for ( j = -1; j <= 1; j++ )
						{
							for ( i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash89( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			

			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID( input );
				UNITY_TRANSFER_INSTANCE_ID( input, output );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( output );

				#if ( SHADER_TARGET >= 50 )
				float recip367 = rcp( _Scale );
				#else
				float recip367 = 1.0 / _Scale;
				#endif
				float Scale228 = recip367;
				float4 appendResult329 = (float4(_NoiseScale3D.x , _NoiseScale3D.y , _NoiseScale3D.z , 0.0));
				float3 WindDir317 = ( ( float3( 1, 1, 1 ) * -2 + 1 ) * _WindDirection );
				float3 break332 = WindDir317;
				float temp_output_333_0 = ( abs( break332.x ) + abs( break332.y ) + abs( break332.z ) );
				float4 appendResult340 = (float4(( break332.x / temp_output_333_0 ) , ( break332.y / temp_output_333_0 ) , ( break332.z / temp_output_333_0 ) , 0.0));
				float4 WindDirWeights341 = appendResult340;
				float4 break344 = abs( WindDirWeights341 );
				float4 appendResult330 = (float4(_NoiseScale3D.y , _NoiseScale3D.x , _NoiseScale3D.z , 0.0));
				float4 appendResult331 = (float4(_NoiseScale3D.y , _NoiseScale3D.z , _NoiseScale3D.x , 0.0));
				float4 transform70 = mul(GetObjectToWorldMatrix(),input.positionOS);
				float4 temp_output_71_0 = (transform70).xyzw;
				float simplePerlin3D112 = snoise( ( Scale228 * ( ( ( appendResult329 * break344.x ) + ( appendResult330 * break344.y ) + ( appendResult331 * break344.z ) ) * (temp_output_71_0*1.0 + float4( ( ( WindDir317 + _AdditionalNoiseDir ) * ( _NoiseSpeed * _TimeParameters.x * _WindSpeed ) ) , 0.0 )) ) ).xyz*_NoiseScale );
				simplePerlin3D112 = simplePerlin3D112*0.5 + 0.5;
				float temp_output_118_0 = saturate(  (0.0 + ( simplePerlin3D112 - 0.0 ) * ( 1.0 - 0.0 ) / ( 1.0 - 0.0 ) ) );
				float temp_output_198_0 = ( Scale228 * _VoronoiScale );
				float time97 = 0.0;
				float2 voronoiSmoothId97 = 0;
				float4 break84 = (temp_output_71_0*1.0 + float4( ( ( _TimeParameters.x * _VorSpeed * _WindSpeed ) * ( WindDir317 + _AdditionalVoronoiDir ) ) , 0.0 ));
				float2 appendResult85 = (float2(break84.z , break84.y));
				float2 coords97 = appendResult85 * temp_output_198_0;
				float2 id97 = 0;
				float2 uv97 = 0;
				float fade97 = 0.5;
				float voroi97 = 0;
				float rest97 = 0;
				for( int it97 = 0; it97 <3; it97++ ){
				voroi97 += fade97 * voronoi97( coords97, time97, id97, uv97, 0,voronoiSmoothId97 );
				rest97 += fade97;
				coords97 *= 2;
				fade97 *= 0.5;
				}//Voronoi97
				voroi97 /= rest97;
				float3 objToWorldDir91 = mul( GetObjectToWorldMatrix(), float4( input.normalOS, 0.0 ) ).xyz;
				float3 temp_output_93_0 = pow( abs( objToWorldDir91 ) , 5.0 );
				float dotResult94 = dot( temp_output_93_0 , float3( 1,1,1 ) );
				float3 break104 = ( temp_output_93_0 / dotResult94 );
				float time96 = 217.0;
				float2 voronoiSmoothId96 = 0;
				float2 appendResult86 = (float2(break84.z , break84.x));
				float2 coords96 = appendResult86 * temp_output_198_0;
				float2 id96 = 0;
				float2 uv96 = 0;
				float fade96 = 0.5;
				float voroi96 = 0;
				float rest96 = 0;
				for( int it96 = 0; it96 <3; it96++ ){
				voroi96 += fade96 * voronoi96( coords96, time96, id96, uv96, 0,voronoiSmoothId96 );
				rest96 += fade96;
				coords96 *= 2;
				fade96 *= 0.5;
				}//Voronoi96
				voroi96 /= rest96;
				float time89 = 137.0;
				float2 voronoiSmoothId89 = 0;
				float2 appendResult87 = (float2(break84.x , break84.y));
				float2 coords89 = appendResult87 * temp_output_198_0;
				float2 id89 = 0;
				float2 uv89 = 0;
				float fade89 = 0.5;
				float voroi89 = 0;
				float rest89 = 0;
				for( int it89 = 0; it89 <3; it89++ ){
				voroi89 += fade89 * voronoi89( coords89, time89, id89, uv89, 0,voronoiSmoothId89 );
				rest89 += fade89;
				coords89 *= 2;
				fade89 *= 0.5;
				}//Voronoi89
				voroi89 /= rest89;
				float temp_output_190_0 = saturate(  (0.0 + ( ( 1.0 - ( ( ( voroi97 * break104.x ) + ( voroi96 * break104.y ) ) + ( voroi89 * break104.z ) ) ) - 0.65 ) * ( 1.0 - 0.0 ) / ( 1.0 - 0.65 ) ) );
				float temp_output_122_0 = saturate( ( ( temp_output_118_0 * _NoiseStrength ) + ( temp_output_190_0 * _VoronoiStrength ) ) );
				float3 ase_objectScale = float3( length( GetObjectToWorldMatrix()[ 0 ].xyz ), length( GetObjectToWorldMatrix()[ 1 ].xyz ), length( GetObjectToWorldMatrix()[ 2 ].xyz ) );
				float3 temp_output_184_0 = ( ( ( temp_output_122_0 - 0.5 ) * input.normalOS * ( _NoiseDisplacement / Scale228 ) ) / ase_objectScale );
				float4 temp_output_2_0_g146 = _Mask0;
				float3 worldToObj306 = mul( GetWorldToObjectMatrix(), float4( (temp_output_2_0_g146).xyz, 1 ) ).xyz;
				float3 ase_objectPosition = GetAbsolutePositionWS( UNITY_MATRIX_M._m03_m13_m23 );
				float4 appendResult313 = (float4((temp_output_2_0_g146).w , 0.0 , 0.0 , 0.0));
				float3 worldToObj307 = mul( GetWorldToObjectMatrix(), float4( ( float4( ase_objectPosition , 0.0 ) + appendResult313 ).xyz, 1 ) ).xyz;
				float3 temp_output_304_0 = ( temp_output_184_0 + ( min( ( distance( ( temp_output_184_0 + input.positionOS.xyz ) , worldToObj306 ) - length( worldToObj307 ) ), 0.0 ) * input.normalOS ) );
				float4 temp_output_2_0_g153 = _Mask1;
				float3 worldToObj357 = mul( GetWorldToObjectMatrix(), float4( (temp_output_2_0_g153).xyz, 1 ) ).xyz;
				float4 appendResult354 = (float4((temp_output_2_0_g153).w , 0.0 , 0.0 , 0.0));
				float3 worldToObj358 = mul( GetWorldToObjectMatrix(), float4( ( float4( ase_objectPosition , 0.0 ) + appendResult354 ).xyz, 1 ) ).xyz;
				float3 VertexOffset305 = ( temp_output_304_0 + ( min( ( distance( ( temp_output_304_0 + input.positionOS.xyz ) , worldToObj357 ) - length( worldToObj358 ) ), 0.0 ) * input.normalOS ) );
				
				float4 ase_positionCS = TransformObjectToHClip( ( input.positionOS ).xyz );
				float4 screenPos = ComputeScreenPos( ase_positionCS );
				output.ase_texcoord2 = screenPos;
				float3 objectToViewPos = TransformWorldToView( TransformObjectToWorld( input.positionOS.xyz ) );
				float eyeDepth = -objectToViewPos.z;
				output.ase_texcoord3.x = eyeDepth;
				
				output.ase_texcoord1 = input.positionOS;
				output.ase_normal = input.normalOS;
				output.ase_color = input.ase_color;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord3.yzw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = VertexOffset305;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;
				input.tangentOS = input.tangentOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				output.positionCS = vertexInput.positionCS;
				output.positionWS = vertexInput.positionWS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.tangentOS = input.tangentOS;
				output.ase_color = input.ase_color;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.tangentOS = patch[0].tangentOS * bary.x + patch[1].tangentOS * bary.y + patch[2].tangentOS * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(PackedVaryings input  ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( input );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				#if defined(MAIN_LIGHT_CALCULATE_SHADOWS) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
				#else
					float4 shadowCoord = float4(0, 0, 0, 0);
				#endif

				float3 PositionWS = input.positionWS;
				float3 PositionRWS = GetCameraRelativePositionWS( input.positionWS );
				float4 ShadowCoord = shadowCoord;

				#if ( SHADER_TARGET >= 50 )
				float recip367 = rcp( _Scale );
				#else
				float recip367 = 1.0 / _Scale;
				#endif
				float Scale228 = recip367;
				float4 appendResult329 = (float4(_NoiseScale3D.x , _NoiseScale3D.y , _NoiseScale3D.z , 0.0));
				float3 WindDir317 = ( ( float3( 1, 1, 1 ) * -2 + 1 ) * _WindDirection );
				float3 break332 = WindDir317;
				float temp_output_333_0 = ( abs( break332.x ) + abs( break332.y ) + abs( break332.z ) );
				float4 appendResult340 = (float4(( break332.x / temp_output_333_0 ) , ( break332.y / temp_output_333_0 ) , ( break332.z / temp_output_333_0 ) , 0.0));
				float4 WindDirWeights341 = appendResult340;
				float4 break344 = abs( WindDirWeights341 );
				float4 appendResult330 = (float4(_NoiseScale3D.y , _NoiseScale3D.x , _NoiseScale3D.z , 0.0));
				float4 appendResult331 = (float4(_NoiseScale3D.y , _NoiseScale3D.z , _NoiseScale3D.x , 0.0));
				float4 transform70 = mul(GetObjectToWorldMatrix(),input.ase_texcoord1);
				float4 temp_output_71_0 = (transform70).xyzw;
				float simplePerlin3D112 = snoise( ( Scale228 * ( ( ( appendResult329 * break344.x ) + ( appendResult330 * break344.y ) + ( appendResult331 * break344.z ) ) * (temp_output_71_0*1.0 + float4( ( ( WindDir317 + _AdditionalNoiseDir ) * ( _NoiseSpeed * _TimeParameters.x * _WindSpeed ) ) , 0.0 )) ) ).xyz*_NoiseScale );
				simplePerlin3D112 = simplePerlin3D112*0.5 + 0.5;
				float temp_output_118_0 = saturate(  (0.0 + ( simplePerlin3D112 - 0.0 ) * ( 1.0 - 0.0 ) / ( 1.0 - 0.0 ) ) );
				float NoiseColor3D151 = temp_output_118_0;
				float temp_output_198_0 = ( Scale228 * _VoronoiScale );
				float time97 = 0.0;
				float2 voronoiSmoothId97 = 0;
				float4 break84 = (temp_output_71_0*1.0 + float4( ( ( _TimeParameters.x * _VorSpeed * _WindSpeed ) * ( WindDir317 + _AdditionalVoronoiDir ) ) , 0.0 ));
				float2 appendResult85 = (float2(break84.z , break84.y));
				float2 coords97 = appendResult85 * temp_output_198_0;
				float2 id97 = 0;
				float2 uv97 = 0;
				float fade97 = 0.5;
				float voroi97 = 0;
				float rest97 = 0;
				for( int it97 = 0; it97 <3; it97++ ){
				voroi97 += fade97 * voronoi97( coords97, time97, id97, uv97, 0,voronoiSmoothId97 );
				rest97 += fade97;
				coords97 *= 2;
				fade97 *= 0.5;
				}//Voronoi97
				voroi97 /= rest97;
				float3 objToWorldDir91 = mul( GetObjectToWorldMatrix(), float4( input.ase_normal, 0.0 ) ).xyz;
				float3 temp_output_93_0 = pow( abs( objToWorldDir91 ) , 5.0 );
				float dotResult94 = dot( temp_output_93_0 , float3( 1,1,1 ) );
				float3 break104 = ( temp_output_93_0 / dotResult94 );
				float time96 = 217.0;
				float2 voronoiSmoothId96 = 0;
				float2 appendResult86 = (float2(break84.z , break84.x));
				float2 coords96 = appendResult86 * temp_output_198_0;
				float2 id96 = 0;
				float2 uv96 = 0;
				float fade96 = 0.5;
				float voroi96 = 0;
				float rest96 = 0;
				for( int it96 = 0; it96 <3; it96++ ){
				voroi96 += fade96 * voronoi96( coords96, time96, id96, uv96, 0,voronoiSmoothId96 );
				rest96 += fade96;
				coords96 *= 2;
				fade96 *= 0.5;
				}//Voronoi96
				voroi96 /= rest96;
				float time89 = 137.0;
				float2 voronoiSmoothId89 = 0;
				float2 appendResult87 = (float2(break84.x , break84.y));
				float2 coords89 = appendResult87 * temp_output_198_0;
				float2 id89 = 0;
				float2 uv89 = 0;
				float fade89 = 0.5;
				float voroi89 = 0;
				float rest89 = 0;
				for( int it89 = 0; it89 <3; it89++ ){
				voroi89 += fade89 * voronoi89( coords89, time89, id89, uv89, 0,voronoiSmoothId89 );
				rest89 += fade89;
				coords89 *= 2;
				fade89 *= 0.5;
				}//Voronoi89
				voroi89 /= rest89;
				float temp_output_190_0 = saturate(  (0.0 + ( ( 1.0 - ( ( ( voroi97 * break104.x ) + ( voroi96 * break104.y ) ) + ( voroi89 * break104.z ) ) ) - 0.65 ) * ( 1.0 - 0.0 ) / ( 1.0 - 0.65 ) ) );
				float NoiseColorVoronoi152 = temp_output_190_0;
				float lerpResult157 = lerp( NoiseColor3D151 , NoiseColorVoronoi152 , _NoiseColorMix);
				float NoiseColorMix158 = lerpResult157;
				float3 temp_cast_3 = (NoiseColorMix158).xxx;
				float temp_output_2_0_g163 = _NoiseColorStrength;
				float temp_output_3_0_g163 = ( 1.0 - temp_output_2_0_g163 );
				float3 appendResult7_g163 = (float3(temp_output_3_0_g163 , temp_output_3_0_g163 , temp_output_3_0_g163));
				float3 temp_output_21_0 = ( ( temp_cast_3 * temp_output_2_0_g163 ) + appendResult7_g163 );
				float4 lerpResult161 = lerp( _BaseColor , input.ase_color , _VertexColorStrength);
				float4 Albedo30 = saturate( ( float4( temp_output_21_0 , 0.0 ) * lerpResult161 ) );
				
				float4 screenPos = input.ase_texcoord2;
				float4 ase_positionSSNorm = screenPos / screenPos.w;
				ase_positionSSNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_positionSSNorm.z : ase_positionSSNorm.z * 0.5 + 0.5;
				float depth01_368 = SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_positionSSNorm.xy );
				float temp_output_385_0 = abs( ( _ProjectionParams.z - _ProjectionParams.y ) );
				float eyeDepth = input.ase_texcoord3.x;
				float cameraDepthFade389 = (( eyeDepth -_ProjectionParams.y - 0.0 ) / temp_output_385_0);
				float temp_output_1_0_g160 = saturate( ( ( ( ( 1.0 - depth01_368 ) - cameraDepthFade389 ) * temp_output_385_0 ) / _DepthDistance ) );
				float DepthFade43 = saturate( ( ( ( pow( temp_output_1_0_g160 , 5.0 ) * 6.0 ) - ( pow( temp_output_1_0_g160 , 4.0 ) * 15.0 ) ) + ( pow( temp_output_1_0_g160 , 3.0 ) * 10.0 ) ) );
				float4 temp_output_2_0_g155 = lerpResult161;
				float BaseColorAlpha162 = (temp_output_2_0_g155).a;
				

				float3 BaseColor = Albedo30.rgb;
				float Alpha = ( DepthFade43 * BaseColorAlpha162 );
				#if defined( _ALPHATEST_ON )
					float AlphaClipThreshold = 0.0;
				#endif

				half4 color = half4(BaseColor, Alpha );

				#if defined( _ALPHATEST_ON )
					AlphaDiscard( Alpha, AlphaClipThreshold );
				#endif

				return color;
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "DepthNormals"
			Tags { "LightMode"="DepthNormalsOnly" }

			ZWrite On
			Blend One Zero
			ZTest LEqual
			ZWrite On

			HLSLPROGRAM

			#define ASE_GEOMETRY
			#define _ALPHATEST_ON
			#define _NORMAL_DROPOFF_TS 1
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#define ASE_FOG 1
			#define ASE_WRITE_DEPTH
			#define ASE_TRANSLUCENCY 1
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define _EMISSION
			#define ASE_VERSION 19912
			#define ASE_SRP_VERSION 170300
			#define REQUIRE_DEPTH_TEXTURE 1


			#pragma vertex vert
			#pragma fragment frag

			#if defined( _SPECULAR_SETUP ) && defined( ASE_LIGHTING_SIMPLE )
				#if defined( _SPECULARHIGHLIGHTS_OFF )
					#undef _SPECULAR_COLOR
				#else
					#define _SPECULAR_COLOR
				#endif
			#endif

			#define SHADERPASS SHADERPASS_DEPTHNORMALSONLY
			//#define SHADERPASS SHADERPASS_DEPTHNORMALS

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#if defined( UNITY_INSTANCING_ENABLED ) && defined( ASE_INSTANCED_TERRAIN ) && ( defined(_TERRAIN_INSTANCED_PERPIXEL_NORMAL) || defined(_INSTANCEDTERRAINNORMALS_PIXEL) )
				#define ENABLE_TERRAIN_PERPIXEL_NORMAL
			#endif

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_VERT_NORMAL
			#define ASE_NEEDS_FRAG_SCREEN_POSITION_NORMALIZED


			#if defined(ASE_WRITE_DEPTH_CONSERVATIVE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				half4 texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				half3 normalWS : TEXCOORD1;
				float4 tangentWS : TEXCOORD2; // holds terrainUV ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _EmmissiveColor;
			float4 _BaseColor;
			float4 _Mask1;
			float4 _Mask0;
			float3 _NoiseScale3D;
			float3 _WindDirection;
			float3 _AdditionalNoiseDir;
			float3 _AdditionalVoronoiDir;
			float _Scale;
			float _EmmissiveNoiseColor;
			float _VertexColorStrength;
			float _NoiseColorStrength;
			float _NoiseColorMix;
			float _VoronoiStrength;
			float _DepthDistance;
			float _VorSpeed;
			float _VoronoiScale;
			float _NoiseStrength;
			float _NoiseScale;
			float _WindSpeed;
			float _NoiseSpeed;
			float _NoiseDisplacement;
			float _EmmissiveDepthFade;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TRANSMISSION
				float _TransmissionShadow;
			#endif
			#ifdef ASE_TRANSLUCENCY
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			

			float3 mod3D289( float3 x ) { return x - floor( x / 289.0 ) * 289.0; }
			float4 mod3D289( float4 x ) { return x - floor( x / 289.0 ) * 289.0; }
			float4 permute( float4 x ) { return mod3D289( ( x * 34.0 + 1.0 ) * x ); }
			float4 taylorInvSqrt( float4 r ) { return 1.79284291400159 - r * 0.85373472095314; }
			float snoise( float3 v )
			{
				const float2 C = float2( 1.0 / 6.0, 1.0 / 3.0 );
				float3 i = floor( v + dot( v, C.yyy ) );
				float3 x0 = v - i + dot( i, C.xxx );
				float3 g = step( x0.yzx, x0.xyz );
				float3 l = 1.0 - g;
				float3 i1 = min( g.xyz, l.zxy );
				float3 i2 = max( g.xyz, l.zxy );
				float3 x1 = x0 - i1 + C.xxx;
				float3 x2 = x0 - i2 + C.yyy;
				float3 x3 = x0 - 0.5;
				i = mod3D289( i);
				float4 p = permute( permute( permute( i.z + float4( 0.0, i1.z, i2.z, 1.0 ) ) + i.y + float4( 0.0, i1.y, i2.y, 1.0 ) ) + i.x + float4( 0.0, i1.x, i2.x, 1.0 ) );
				float4 j = p - 49.0 * floor( p / 49.0 );  // mod(p,7*7)
				float4 x_ = floor( j / 7.0 );
				float4 y_ = floor( j - 7.0 * x_ );  // mod(j,N)
				float4 x = ( x_ * 2.0 + 0.5 ) / 7.0 - 1.0;
				float4 y = ( y_ * 2.0 + 0.5 ) / 7.0 - 1.0;
				float4 h = 1.0 - abs( x ) - abs( y );
				float4 b0 = float4( x.xy, y.xy );
				float4 b1 = float4( x.zw, y.zw );
				float4 s0 = floor( b0 ) * 2.0 + 1.0;
				float4 s1 = floor( b1 ) * 2.0 + 1.0;
				float4 sh = -step( h, 0.0 );
				float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
				float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
				float3 g0 = float3( a0.xy, h.x );
				float3 g1 = float3( a0.zw, h.y );
				float3 g2 = float3( a1.xy, h.z );
				float3 g3 = float3( a1.zw, h.w );
				float4 norm = taylorInvSqrt( float4( dot( g0, g0 ), dot( g1, g1 ), dot( g2, g2 ), dot( g3, g3 ) ) );
				g0 *= norm.x;
				g1 *= norm.y;
				g2 *= norm.z;
				g3 *= norm.w;
				float4 m = max( 0.6 - float4( dot( x0, x0 ), dot( x1, x1 ), dot( x2, x2 ), dot( x3, x3 ) ), 0.0 );
				m = m* m;
				m = m* m;
				float4 px = float4( dot( x0, g0 ), dot( x1, g1 ), dot( x2, g2 ), dot( x3, g3 ) );
				return 42.0 * dot( m, px);
			}
			
					float2 voronoihash97( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi97( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0; int i, j;
						for ( j = -1; j <= 1; j++ )
						{
							for ( i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash97( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			
					float2 voronoihash96( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi96( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0; int i, j;
						for ( j = -1; j <= 1; j++ )
						{
							for ( i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash96( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			
					float2 voronoihash89( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi89( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0; int i, j;
						for ( j = -1; j <= 1; j++ )
						{
							for ( i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash89( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			

			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				#if ( SHADER_TARGET >= 50 )
				float recip367 = rcp( _Scale );
				#else
				float recip367 = 1.0 / _Scale;
				#endif
				float Scale228 = recip367;
				float4 appendResult329 = (float4(_NoiseScale3D.x , _NoiseScale3D.y , _NoiseScale3D.z , 0.0));
				float3 WindDir317 = ( ( float3( 1, 1, 1 ) * -2 + 1 ) * _WindDirection );
				float3 break332 = WindDir317;
				float temp_output_333_0 = ( abs( break332.x ) + abs( break332.y ) + abs( break332.z ) );
				float4 appendResult340 = (float4(( break332.x / temp_output_333_0 ) , ( break332.y / temp_output_333_0 ) , ( break332.z / temp_output_333_0 ) , 0.0));
				float4 WindDirWeights341 = appendResult340;
				float4 break344 = abs( WindDirWeights341 );
				float4 appendResult330 = (float4(_NoiseScale3D.y , _NoiseScale3D.x , _NoiseScale3D.z , 0.0));
				float4 appendResult331 = (float4(_NoiseScale3D.y , _NoiseScale3D.z , _NoiseScale3D.x , 0.0));
				float4 transform70 = mul(GetObjectToWorldMatrix(),input.positionOS);
				float4 temp_output_71_0 = (transform70).xyzw;
				float simplePerlin3D112 = snoise( ( Scale228 * ( ( ( appendResult329 * break344.x ) + ( appendResult330 * break344.y ) + ( appendResult331 * break344.z ) ) * (temp_output_71_0*1.0 + float4( ( ( WindDir317 + _AdditionalNoiseDir ) * ( _NoiseSpeed * _TimeParameters.x * _WindSpeed ) ) , 0.0 )) ) ).xyz*_NoiseScale );
				simplePerlin3D112 = simplePerlin3D112*0.5 + 0.5;
				float temp_output_118_0 = saturate(  (0.0 + ( simplePerlin3D112 - 0.0 ) * ( 1.0 - 0.0 ) / ( 1.0 - 0.0 ) ) );
				float temp_output_198_0 = ( Scale228 * _VoronoiScale );
				float time97 = 0.0;
				float2 voronoiSmoothId97 = 0;
				float4 break84 = (temp_output_71_0*1.0 + float4( ( ( _TimeParameters.x * _VorSpeed * _WindSpeed ) * ( WindDir317 + _AdditionalVoronoiDir ) ) , 0.0 ));
				float2 appendResult85 = (float2(break84.z , break84.y));
				float2 coords97 = appendResult85 * temp_output_198_0;
				float2 id97 = 0;
				float2 uv97 = 0;
				float fade97 = 0.5;
				float voroi97 = 0;
				float rest97 = 0;
				for( int it97 = 0; it97 <3; it97++ ){
				voroi97 += fade97 * voronoi97( coords97, time97, id97, uv97, 0,voronoiSmoothId97 );
				rest97 += fade97;
				coords97 *= 2;
				fade97 *= 0.5;
				}//Voronoi97
				voroi97 /= rest97;
				float3 objToWorldDir91 = mul( GetObjectToWorldMatrix(), float4( input.normalOS, 0.0 ) ).xyz;
				float3 temp_output_93_0 = pow( abs( objToWorldDir91 ) , 5.0 );
				float dotResult94 = dot( temp_output_93_0 , float3( 1,1,1 ) );
				float3 break104 = ( temp_output_93_0 / dotResult94 );
				float time96 = 217.0;
				float2 voronoiSmoothId96 = 0;
				float2 appendResult86 = (float2(break84.z , break84.x));
				float2 coords96 = appendResult86 * temp_output_198_0;
				float2 id96 = 0;
				float2 uv96 = 0;
				float fade96 = 0.5;
				float voroi96 = 0;
				float rest96 = 0;
				for( int it96 = 0; it96 <3; it96++ ){
				voroi96 += fade96 * voronoi96( coords96, time96, id96, uv96, 0,voronoiSmoothId96 );
				rest96 += fade96;
				coords96 *= 2;
				fade96 *= 0.5;
				}//Voronoi96
				voroi96 /= rest96;
				float time89 = 137.0;
				float2 voronoiSmoothId89 = 0;
				float2 appendResult87 = (float2(break84.x , break84.y));
				float2 coords89 = appendResult87 * temp_output_198_0;
				float2 id89 = 0;
				float2 uv89 = 0;
				float fade89 = 0.5;
				float voroi89 = 0;
				float rest89 = 0;
				for( int it89 = 0; it89 <3; it89++ ){
				voroi89 += fade89 * voronoi89( coords89, time89, id89, uv89, 0,voronoiSmoothId89 );
				rest89 += fade89;
				coords89 *= 2;
				fade89 *= 0.5;
				}//Voronoi89
				voroi89 /= rest89;
				float temp_output_190_0 = saturate(  (0.0 + ( ( 1.0 - ( ( ( voroi97 * break104.x ) + ( voroi96 * break104.y ) ) + ( voroi89 * break104.z ) ) ) - 0.65 ) * ( 1.0 - 0.0 ) / ( 1.0 - 0.65 ) ) );
				float temp_output_122_0 = saturate( ( ( temp_output_118_0 * _NoiseStrength ) + ( temp_output_190_0 * _VoronoiStrength ) ) );
				float3 ase_objectScale = float3( length( GetObjectToWorldMatrix()[ 0 ].xyz ), length( GetObjectToWorldMatrix()[ 1 ].xyz ), length( GetObjectToWorldMatrix()[ 2 ].xyz ) );
				float3 temp_output_184_0 = ( ( ( temp_output_122_0 - 0.5 ) * input.normalOS * ( _NoiseDisplacement / Scale228 ) ) / ase_objectScale );
				float4 temp_output_2_0_g146 = _Mask0;
				float3 worldToObj306 = mul( GetWorldToObjectMatrix(), float4( (temp_output_2_0_g146).xyz, 1 ) ).xyz;
				float3 ase_objectPosition = GetAbsolutePositionWS( UNITY_MATRIX_M._m03_m13_m23 );
				float4 appendResult313 = (float4((temp_output_2_0_g146).w , 0.0 , 0.0 , 0.0));
				float3 worldToObj307 = mul( GetWorldToObjectMatrix(), float4( ( float4( ase_objectPosition , 0.0 ) + appendResult313 ).xyz, 1 ) ).xyz;
				float3 temp_output_304_0 = ( temp_output_184_0 + ( min( ( distance( ( temp_output_184_0 + input.positionOS.xyz ) , worldToObj306 ) - length( worldToObj307 ) ), 0.0 ) * input.normalOS ) );
				float4 temp_output_2_0_g153 = _Mask1;
				float3 worldToObj357 = mul( GetWorldToObjectMatrix(), float4( (temp_output_2_0_g153).xyz, 1 ) ).xyz;
				float4 appendResult354 = (float4((temp_output_2_0_g153).w , 0.0 , 0.0 , 0.0));
				float3 worldToObj358 = mul( GetWorldToObjectMatrix(), float4( ( float4( ase_objectPosition , 0.0 ) + appendResult354 ).xyz, 1 ) ).xyz;
				float3 VertexOffset305 = ( temp_output_304_0 + ( min( ( distance( ( temp_output_304_0 + input.positionOS.xyz ) , worldToObj357 ) - length( worldToObj358 ) ), 0.0 ) * input.normalOS ) );
				
				float3 objectToViewPos = TransformWorldToView( TransformObjectToWorld( input.positionOS.xyz ) );
				float eyeDepth = -objectToViewPos.z;
				output.ase_texcoord3.x = eyeDepth;
				
				output.ase_color = input.ase_color;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord3.yzw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = VertexOffset305;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;
				input.tangentOS = input.tangentOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );
				VertexNormalInputs normalInput = GetVertexNormalInputs( input.normalOS, input.tangentOS );

				output.positionCS = ASE_ADJUST_CLIP_POSITION( vertexInput.positionCS );
				output.positionWS = vertexInput.positionWS;
				output.normalWS = normalInput.normalWS;
				output.tangentWS = float4( normalInput.tangentWS, ( input.tangentOS.w > 0.0 ? 1.0 : -1.0 ) * GetOddNegativeScale() );

				#if defined( ENABLE_TERRAIN_PERPIXEL_NORMAL )
					output.tangentWS.zw = input.texcoord.xy;
					output.tangentWS.xy = input.texcoord.xy * unity_LightmapST.xy + unity_LightmapST.zw;
				#endif
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 texcoord : TEXCOORD0;
				float4 ase_color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.tangentOS = input.tangentOS;
				output.texcoord = input.texcoord;
				output.ase_color = input.ase_color;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.tangentOS = patch[0].tangentOS * bary.x + patch[1].tangentOS * bary.y + patch[2].tangentOS * bary.z;
				output.texcoord = patch[0].texcoord * bary.x + patch[1].texcoord * bary.y + patch[2].texcoord * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			void frag(	PackedVaryings input
						, out half4 outNormalWS : SV_Target0
						#if defined( ASE_WRITE_DEPTH )
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						#ifdef _WRITE_RENDERING_LAYERS
						#if ( UNITY_VERSION >= 60020000 )
						, out uint outRenderingLayers : SV_Target1
						#else
						, out float4 outRenderingLayers : SV_Target1
						#endif
						#endif
						 )
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				#if defined(MAIN_LIGHT_CALCULATE_SHADOWS) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
				#else
					float4 shadowCoord = float4(0, 0, 0, 0);
				#endif

				// @diogo: mikktspace compliant
				float renormFactor = 1.0 / max( FLT_MIN, length( input.normalWS ) );

				float3 PositionWS = input.positionWS;
				float3 PositionRWS = GetCameraRelativePositionWS( input.positionWS );
				float4 ShadowCoord = shadowCoord;
				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;
				float4 ScreenPos = ComputeScreenPos( ClipPos );
				float3 TangentWS = input.tangentWS.xyz * renormFactor;
				float3 BitangentWS = cross( input.normalWS, input.tangentWS.xyz ) * input.tangentWS.w * renormFactor;
				float3 NormalWS = input.normalWS * renormFactor;

				#if defined( ENABLE_TERRAIN_PERPIXEL_NORMAL )
					float2 sampleCoords = (input.tangentWS.zw / _TerrainHeightmapRecipSize.zw + 0.5f) * _TerrainHeightmapRecipSize.xy;
					NormalWS = TransformObjectToWorldNormal(normalize(SAMPLE_TEXTURE2D(_TerrainNormalmapTexture, sampler_TerrainNormalmapTexture, sampleCoords).rgb * 2 - 1));
					TangentWS = -cross(GetObjectToWorldMatrix()._13_23_33, NormalWS);
					BitangentWS = cross(NormalWS, -TangentWS);
				#endif

				float depth01_368 = SHADERGRAPH_SAMPLE_SCENE_DEPTH( ScreenPosNorm.xy );
				float temp_output_385_0 = abs( ( _ProjectionParams.z - _ProjectionParams.y ) );
				float eyeDepth = input.ase_texcoord3.x;
				float cameraDepthFade389 = (( eyeDepth -_ProjectionParams.y - 0.0 ) / temp_output_385_0);
				float temp_output_1_0_g160 = saturate( ( ( ( ( 1.0 - depth01_368 ) - cameraDepthFade389 ) * temp_output_385_0 ) / _DepthDistance ) );
				float DepthFade43 = saturate( ( ( ( pow( temp_output_1_0_g160 , 5.0 ) * 6.0 ) - ( pow( temp_output_1_0_g160 , 4.0 ) * 15.0 ) ) + ( pow( temp_output_1_0_g160 , 3.0 ) * 10.0 ) ) );
				float4 lerpResult161 = lerp( _BaseColor , input.ase_color , _VertexColorStrength);
				float4 temp_output_2_0_g155 = lerpResult161;
				float BaseColorAlpha162 = (temp_output_2_0_g155).a;
				

				float3 Normal = float3(0, 0, 1);
				float Alpha = ( DepthFade43 * BaseColorAlpha162 );
				#if defined( _ALPHATEST_ON )
					float AlphaClipThreshold = 0.0;
				#endif

				#if defined( ASE_WRITE_DEPTH )
					input.positionCS.z = input.positionCS.z;
				#endif

				#if defined( _ALPHATEST_ON )
					AlphaDiscard( Alpha, AlphaClipThreshold );
				#endif

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined( ASE_WRITE_DEPTH )
					outputDepth = input.positionCS.z;
				#endif

				#if defined(_GBUFFER_NORMALS_OCT)
					float2 octNormalWS = PackNormalOctQuadEncode(NormalWS);
					float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
					half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);
					outNormalWS = half4(packedNormalWS, 0.0);
				#else
					#if defined(_NORMALMAP)
						#if _NORMAL_DROPOFF_TS
							float3 normalWS = TransformTangentToWorld(Normal, half3x3(TangentWS, BitangentWS, NormalWS));
						#elif _NORMAL_DROPOFF_OS
							float3 normalWS = TransformObjectToWorldNormal(Normal);
						#elif _NORMAL_DROPOFF_WS
							float3 normalWS = Normal;
						#endif
					#else
						float3 normalWS = NormalWS;
					#endif
					outNormalWS = half4(NormalizeNormalPerPixel(normalWS), 0.0);
				#endif

				#ifdef _WRITE_RENDERING_LAYERS
					#if ( UNITY_VERSION >= 60020000 )
					outRenderingLayers = EncodeMeshRenderingLayer();
					#else
					uint renderingLayers = GetMeshRenderingLayer();
					outRenderingLayers = float4( EncodeMeshRenderingLayer( renderingLayers ), 0, 0, 0 );
					#endif
				#endif
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "SceneSelectionPass"
			Tags { "LightMode"="SceneSelectionPass" }

			Cull Off
			AlphaToMask Off

			HLSLPROGRAM

			#define ASE_GEOMETRY
			#define _ALPHATEST_ON
			#define _NORMAL_DROPOFF_TS 1
			#define ASE_FOG 1
			#define ASE_WRITE_DEPTH
			#define ASE_TRANSLUCENCY 1
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define _EMISSION
			#define ASE_VERSION 19912
			#define ASE_SRP_VERSION 170300
			#define REQUIRE_DEPTH_TEXTURE 1


			#pragma vertex vert
			#pragma fragment frag

			#if defined( _SPECULAR_SETUP ) && defined( ASE_LIGHTING_SIMPLE )
				#if defined( _SPECULARHIGHLIGHTS_OFF )
					#undef _SPECULAR_COLOR
				#else
					#define _SPECULAR_COLOR
				#endif
			#endif

			#define SCENESELECTIONPASS 1

			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define SHADERPASS SHADERPASS_DEPTHONLY

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_VERT_NORMAL
			#define ASE_NEEDS_FRAG_SCREEN_POSITION_NORMALIZED


			#if defined(ASE_WRITE_DEPTH_CONSERVATIVE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _EmmissiveColor;
			float4 _BaseColor;
			float4 _Mask1;
			float4 _Mask0;
			float3 _NoiseScale3D;
			float3 _WindDirection;
			float3 _AdditionalNoiseDir;
			float3 _AdditionalVoronoiDir;
			float _Scale;
			float _EmmissiveNoiseColor;
			float _VertexColorStrength;
			float _NoiseColorStrength;
			float _NoiseColorMix;
			float _VoronoiStrength;
			float _DepthDistance;
			float _VorSpeed;
			float _VoronoiScale;
			float _NoiseStrength;
			float _NoiseScale;
			float _WindSpeed;
			float _NoiseSpeed;
			float _NoiseDisplacement;
			float _EmmissiveDepthFade;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TRANSMISSION
				float _TransmissionShadow;
			#endif
			#ifdef ASE_TRANSLUCENCY
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			

			float3 mod3D289( float3 x ) { return x - floor( x / 289.0 ) * 289.0; }
			float4 mod3D289( float4 x ) { return x - floor( x / 289.0 ) * 289.0; }
			float4 permute( float4 x ) { return mod3D289( ( x * 34.0 + 1.0 ) * x ); }
			float4 taylorInvSqrt( float4 r ) { return 1.79284291400159 - r * 0.85373472095314; }
			float snoise( float3 v )
			{
				const float2 C = float2( 1.0 / 6.0, 1.0 / 3.0 );
				float3 i = floor( v + dot( v, C.yyy ) );
				float3 x0 = v - i + dot( i, C.xxx );
				float3 g = step( x0.yzx, x0.xyz );
				float3 l = 1.0 - g;
				float3 i1 = min( g.xyz, l.zxy );
				float3 i2 = max( g.xyz, l.zxy );
				float3 x1 = x0 - i1 + C.xxx;
				float3 x2 = x0 - i2 + C.yyy;
				float3 x3 = x0 - 0.5;
				i = mod3D289( i);
				float4 p = permute( permute( permute( i.z + float4( 0.0, i1.z, i2.z, 1.0 ) ) + i.y + float4( 0.0, i1.y, i2.y, 1.0 ) ) + i.x + float4( 0.0, i1.x, i2.x, 1.0 ) );
				float4 j = p - 49.0 * floor( p / 49.0 );  // mod(p,7*7)
				float4 x_ = floor( j / 7.0 );
				float4 y_ = floor( j - 7.0 * x_ );  // mod(j,N)
				float4 x = ( x_ * 2.0 + 0.5 ) / 7.0 - 1.0;
				float4 y = ( y_ * 2.0 + 0.5 ) / 7.0 - 1.0;
				float4 h = 1.0 - abs( x ) - abs( y );
				float4 b0 = float4( x.xy, y.xy );
				float4 b1 = float4( x.zw, y.zw );
				float4 s0 = floor( b0 ) * 2.0 + 1.0;
				float4 s1 = floor( b1 ) * 2.0 + 1.0;
				float4 sh = -step( h, 0.0 );
				float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
				float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
				float3 g0 = float3( a0.xy, h.x );
				float3 g1 = float3( a0.zw, h.y );
				float3 g2 = float3( a1.xy, h.z );
				float3 g3 = float3( a1.zw, h.w );
				float4 norm = taylorInvSqrt( float4( dot( g0, g0 ), dot( g1, g1 ), dot( g2, g2 ), dot( g3, g3 ) ) );
				g0 *= norm.x;
				g1 *= norm.y;
				g2 *= norm.z;
				g3 *= norm.w;
				float4 m = max( 0.6 - float4( dot( x0, x0 ), dot( x1, x1 ), dot( x2, x2 ), dot( x3, x3 ) ), 0.0 );
				m = m* m;
				m = m* m;
				float4 px = float4( dot( x0, g0 ), dot( x1, g1 ), dot( x2, g2 ), dot( x3, g3 ) );
				return 42.0 * dot( m, px);
			}
			
					float2 voronoihash97( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi97( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0; int i, j;
						for ( j = -1; j <= 1; j++ )
						{
							for ( i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash97( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			
					float2 voronoihash96( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi96( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0; int i, j;
						for ( j = -1; j <= 1; j++ )
						{
							for ( i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash96( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			
					float2 voronoihash89( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi89( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0; int i, j;
						for ( j = -1; j <= 1; j++ )
						{
							for ( i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash89( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			

			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};

			PackedVaryings VertexFunction(Attributes input  )
			{
				PackedVaryings output;
				ZERO_INITIALIZE(PackedVaryings, output);

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				#if ( SHADER_TARGET >= 50 )
				float recip367 = rcp( _Scale );
				#else
				float recip367 = 1.0 / _Scale;
				#endif
				float Scale228 = recip367;
				float4 appendResult329 = (float4(_NoiseScale3D.x , _NoiseScale3D.y , _NoiseScale3D.z , 0.0));
				float3 WindDir317 = ( ( float3( 1, 1, 1 ) * -2 + 1 ) * _WindDirection );
				float3 break332 = WindDir317;
				float temp_output_333_0 = ( abs( break332.x ) + abs( break332.y ) + abs( break332.z ) );
				float4 appendResult340 = (float4(( break332.x / temp_output_333_0 ) , ( break332.y / temp_output_333_0 ) , ( break332.z / temp_output_333_0 ) , 0.0));
				float4 WindDirWeights341 = appendResult340;
				float4 break344 = abs( WindDirWeights341 );
				float4 appendResult330 = (float4(_NoiseScale3D.y , _NoiseScale3D.x , _NoiseScale3D.z , 0.0));
				float4 appendResult331 = (float4(_NoiseScale3D.y , _NoiseScale3D.z , _NoiseScale3D.x , 0.0));
				float4 transform70 = mul(GetObjectToWorldMatrix(),input.positionOS);
				float4 temp_output_71_0 = (transform70).xyzw;
				float simplePerlin3D112 = snoise( ( Scale228 * ( ( ( appendResult329 * break344.x ) + ( appendResult330 * break344.y ) + ( appendResult331 * break344.z ) ) * (temp_output_71_0*1.0 + float4( ( ( WindDir317 + _AdditionalNoiseDir ) * ( _NoiseSpeed * _TimeParameters.x * _WindSpeed ) ) , 0.0 )) ) ).xyz*_NoiseScale );
				simplePerlin3D112 = simplePerlin3D112*0.5 + 0.5;
				float temp_output_118_0 = saturate(  (0.0 + ( simplePerlin3D112 - 0.0 ) * ( 1.0 - 0.0 ) / ( 1.0 - 0.0 ) ) );
				float temp_output_198_0 = ( Scale228 * _VoronoiScale );
				float time97 = 0.0;
				float2 voronoiSmoothId97 = 0;
				float4 break84 = (temp_output_71_0*1.0 + float4( ( ( _TimeParameters.x * _VorSpeed * _WindSpeed ) * ( WindDir317 + _AdditionalVoronoiDir ) ) , 0.0 ));
				float2 appendResult85 = (float2(break84.z , break84.y));
				float2 coords97 = appendResult85 * temp_output_198_0;
				float2 id97 = 0;
				float2 uv97 = 0;
				float fade97 = 0.5;
				float voroi97 = 0;
				float rest97 = 0;
				for( int it97 = 0; it97 <3; it97++ ){
				voroi97 += fade97 * voronoi97( coords97, time97, id97, uv97, 0,voronoiSmoothId97 );
				rest97 += fade97;
				coords97 *= 2;
				fade97 *= 0.5;
				}//Voronoi97
				voroi97 /= rest97;
				float3 objToWorldDir91 = mul( GetObjectToWorldMatrix(), float4( input.normalOS, 0.0 ) ).xyz;
				float3 temp_output_93_0 = pow( abs( objToWorldDir91 ) , 5.0 );
				float dotResult94 = dot( temp_output_93_0 , float3( 1,1,1 ) );
				float3 break104 = ( temp_output_93_0 / dotResult94 );
				float time96 = 217.0;
				float2 voronoiSmoothId96 = 0;
				float2 appendResult86 = (float2(break84.z , break84.x));
				float2 coords96 = appendResult86 * temp_output_198_0;
				float2 id96 = 0;
				float2 uv96 = 0;
				float fade96 = 0.5;
				float voroi96 = 0;
				float rest96 = 0;
				for( int it96 = 0; it96 <3; it96++ ){
				voroi96 += fade96 * voronoi96( coords96, time96, id96, uv96, 0,voronoiSmoothId96 );
				rest96 += fade96;
				coords96 *= 2;
				fade96 *= 0.5;
				}//Voronoi96
				voroi96 /= rest96;
				float time89 = 137.0;
				float2 voronoiSmoothId89 = 0;
				float2 appendResult87 = (float2(break84.x , break84.y));
				float2 coords89 = appendResult87 * temp_output_198_0;
				float2 id89 = 0;
				float2 uv89 = 0;
				float fade89 = 0.5;
				float voroi89 = 0;
				float rest89 = 0;
				for( int it89 = 0; it89 <3; it89++ ){
				voroi89 += fade89 * voronoi89( coords89, time89, id89, uv89, 0,voronoiSmoothId89 );
				rest89 += fade89;
				coords89 *= 2;
				fade89 *= 0.5;
				}//Voronoi89
				voroi89 /= rest89;
				float temp_output_190_0 = saturate(  (0.0 + ( ( 1.0 - ( ( ( voroi97 * break104.x ) + ( voroi96 * break104.y ) ) + ( voroi89 * break104.z ) ) ) - 0.65 ) * ( 1.0 - 0.0 ) / ( 1.0 - 0.65 ) ) );
				float temp_output_122_0 = saturate( ( ( temp_output_118_0 * _NoiseStrength ) + ( temp_output_190_0 * _VoronoiStrength ) ) );
				float3 ase_objectScale = float3( length( GetObjectToWorldMatrix()[ 0 ].xyz ), length( GetObjectToWorldMatrix()[ 1 ].xyz ), length( GetObjectToWorldMatrix()[ 2 ].xyz ) );
				float3 temp_output_184_0 = ( ( ( temp_output_122_0 - 0.5 ) * input.normalOS * ( _NoiseDisplacement / Scale228 ) ) / ase_objectScale );
				float4 temp_output_2_0_g146 = _Mask0;
				float3 worldToObj306 = mul( GetWorldToObjectMatrix(), float4( (temp_output_2_0_g146).xyz, 1 ) ).xyz;
				float3 ase_objectPosition = GetAbsolutePositionWS( UNITY_MATRIX_M._m03_m13_m23 );
				float4 appendResult313 = (float4((temp_output_2_0_g146).w , 0.0 , 0.0 , 0.0));
				float3 worldToObj307 = mul( GetWorldToObjectMatrix(), float4( ( float4( ase_objectPosition , 0.0 ) + appendResult313 ).xyz, 1 ) ).xyz;
				float3 temp_output_304_0 = ( temp_output_184_0 + ( min( ( distance( ( temp_output_184_0 + input.positionOS.xyz ) , worldToObj306 ) - length( worldToObj307 ) ), 0.0 ) * input.normalOS ) );
				float4 temp_output_2_0_g153 = _Mask1;
				float3 worldToObj357 = mul( GetWorldToObjectMatrix(), float4( (temp_output_2_0_g153).xyz, 1 ) ).xyz;
				float4 appendResult354 = (float4((temp_output_2_0_g153).w , 0.0 , 0.0 , 0.0));
				float3 worldToObj358 = mul( GetWorldToObjectMatrix(), float4( ( float4( ase_objectPosition , 0.0 ) + appendResult354 ).xyz, 1 ) ).xyz;
				float3 VertexOffset305 = ( temp_output_304_0 + ( min( ( distance( ( temp_output_304_0 + input.positionOS.xyz ) , worldToObj357 ) - length( worldToObj358 ) ), 0.0 ) * input.normalOS ) );
				
				float3 objectToViewPos = TransformWorldToView( TransformObjectToWorld( input.positionOS.xyz ) );
				float eyeDepth = -objectToViewPos.z;
				output.ase_texcoord1.x = eyeDepth;
				
				output.ase_color = input.ase_color;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord1.yzw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = VertexOffset305;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				output.positionCS = ASE_ADJUST_CLIP_POSITION( vertexInput.positionCS );
				output.positionWS = vertexInput.positionWS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.tangentOS = input.tangentOS;
				output.ase_color = input.ase_color;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.tangentOS = patch[0].tangentOS * bary.x + patch[1].tangentOS * bary.y + patch[2].tangentOS * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag( PackedVaryings input
				#if defined( ASE_WRITE_DEPTH )
				,out float outputDepth : ASE_SV_DEPTH
				#endif
				 ) : SV_Target
			{
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				float3 PositionWS = input.positionWS;
				float3 PositionRWS = GetCameraRelativePositionWS( PositionWS );
				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;

				float depth01_368 = SHADERGRAPH_SAMPLE_SCENE_DEPTH( ScreenPosNorm.xy );
				float temp_output_385_0 = abs( ( _ProjectionParams.z - _ProjectionParams.y ) );
				float eyeDepth = input.ase_texcoord1.x;
				float cameraDepthFade389 = (( eyeDepth -_ProjectionParams.y - 0.0 ) / temp_output_385_0);
				float temp_output_1_0_g160 = saturate( ( ( ( ( 1.0 - depth01_368 ) - cameraDepthFade389 ) * temp_output_385_0 ) / _DepthDistance ) );
				float DepthFade43 = saturate( ( ( ( pow( temp_output_1_0_g160 , 5.0 ) * 6.0 ) - ( pow( temp_output_1_0_g160 , 4.0 ) * 15.0 ) ) + ( pow( temp_output_1_0_g160 , 3.0 ) * 10.0 ) ) );
				float4 lerpResult161 = lerp( _BaseColor , input.ase_color , _VertexColorStrength);
				float4 temp_output_2_0_g155 = lerpResult161;
				float BaseColorAlpha162 = (temp_output_2_0_g155).a;
				

				surfaceDescription.Alpha = ( DepthFade43 * BaseColorAlpha162 );
				#if defined( _ALPHATEST_ON )
					surfaceDescription.AlphaClipThreshold = 0.0;
				#endif

				#if defined( ASE_WRITE_DEPTH )
					input.positionCS.z = input.positionCS.z;
				#endif

				#ifdef _ALPHATEST_ON
					clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
				#endif

				#if defined( ASE_WRITE_DEPTH )
					outputDepth = input.positionCS.z;
				#endif

				return half4( _ObjectId, _PassValue, 1.0, 1.0 );
			}

			ENDHLSL
		}

		
		Pass
		{
			
			Name "ScenePickingPass"
			Tags { "LightMode"="Picking" }

			AlphaToMask Off

			HLSLPROGRAM

			#define ASE_GEOMETRY
			#define _ALPHATEST_ON
			#define _NORMAL_DROPOFF_TS 1
			#define ASE_FOG 1
			#define ASE_WRITE_DEPTH
			#define ASE_TRANSLUCENCY 1
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define _EMISSION
			#define ASE_VERSION 19912
			#define ASE_SRP_VERSION 170300
			#define REQUIRE_DEPTH_TEXTURE 1


			#pragma vertex vert
			#pragma fragment frag

			#if defined( _SPECULAR_SETUP ) && defined( ASE_LIGHTING_SIMPLE )
				#if defined( _SPECULARHIGHLIGHTS_OFF )
					#undef _SPECULAR_COLOR
				#else
					#define _SPECULAR_COLOR
				#endif
			#endif

		    #define SCENEPICKINGPASS 1

			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define SHADERPASS SHADERPASS_DEPTHONLY

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_VERT_NORMAL
			#define ASE_NEEDS_FRAG_SCREEN_POSITION_NORMALIZED


			#if defined(ASE_WRITE_DEPTH_CONSERVATIVE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _EmmissiveColor;
			float4 _BaseColor;
			float4 _Mask1;
			float4 _Mask0;
			float3 _NoiseScale3D;
			float3 _WindDirection;
			float3 _AdditionalNoiseDir;
			float3 _AdditionalVoronoiDir;
			float _Scale;
			float _EmmissiveNoiseColor;
			float _VertexColorStrength;
			float _NoiseColorStrength;
			float _NoiseColorMix;
			float _VoronoiStrength;
			float _DepthDistance;
			float _VorSpeed;
			float _VoronoiScale;
			float _NoiseStrength;
			float _NoiseScale;
			float _WindSpeed;
			float _NoiseSpeed;
			float _NoiseDisplacement;
			float _EmmissiveDepthFade;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TRANSMISSION
				float _TransmissionShadow;
			#endif
			#ifdef ASE_TRANSLUCENCY
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			

			float3 mod3D289( float3 x ) { return x - floor( x / 289.0 ) * 289.0; }
			float4 mod3D289( float4 x ) { return x - floor( x / 289.0 ) * 289.0; }
			float4 permute( float4 x ) { return mod3D289( ( x * 34.0 + 1.0 ) * x ); }
			float4 taylorInvSqrt( float4 r ) { return 1.79284291400159 - r * 0.85373472095314; }
			float snoise( float3 v )
			{
				const float2 C = float2( 1.0 / 6.0, 1.0 / 3.0 );
				float3 i = floor( v + dot( v, C.yyy ) );
				float3 x0 = v - i + dot( i, C.xxx );
				float3 g = step( x0.yzx, x0.xyz );
				float3 l = 1.0 - g;
				float3 i1 = min( g.xyz, l.zxy );
				float3 i2 = max( g.xyz, l.zxy );
				float3 x1 = x0 - i1 + C.xxx;
				float3 x2 = x0 - i2 + C.yyy;
				float3 x3 = x0 - 0.5;
				i = mod3D289( i);
				float4 p = permute( permute( permute( i.z + float4( 0.0, i1.z, i2.z, 1.0 ) ) + i.y + float4( 0.0, i1.y, i2.y, 1.0 ) ) + i.x + float4( 0.0, i1.x, i2.x, 1.0 ) );
				float4 j = p - 49.0 * floor( p / 49.0 );  // mod(p,7*7)
				float4 x_ = floor( j / 7.0 );
				float4 y_ = floor( j - 7.0 * x_ );  // mod(j,N)
				float4 x = ( x_ * 2.0 + 0.5 ) / 7.0 - 1.0;
				float4 y = ( y_ * 2.0 + 0.5 ) / 7.0 - 1.0;
				float4 h = 1.0 - abs( x ) - abs( y );
				float4 b0 = float4( x.xy, y.xy );
				float4 b1 = float4( x.zw, y.zw );
				float4 s0 = floor( b0 ) * 2.0 + 1.0;
				float4 s1 = floor( b1 ) * 2.0 + 1.0;
				float4 sh = -step( h, 0.0 );
				float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
				float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
				float3 g0 = float3( a0.xy, h.x );
				float3 g1 = float3( a0.zw, h.y );
				float3 g2 = float3( a1.xy, h.z );
				float3 g3 = float3( a1.zw, h.w );
				float4 norm = taylorInvSqrt( float4( dot( g0, g0 ), dot( g1, g1 ), dot( g2, g2 ), dot( g3, g3 ) ) );
				g0 *= norm.x;
				g1 *= norm.y;
				g2 *= norm.z;
				g3 *= norm.w;
				float4 m = max( 0.6 - float4( dot( x0, x0 ), dot( x1, x1 ), dot( x2, x2 ), dot( x3, x3 ) ), 0.0 );
				m = m* m;
				m = m* m;
				float4 px = float4( dot( x0, g0 ), dot( x1, g1 ), dot( x2, g2 ), dot( x3, g3 ) );
				return 42.0 * dot( m, px);
			}
			
					float2 voronoihash97( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi97( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0; int i, j;
						for ( j = -1; j <= 1; j++ )
						{
							for ( i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash97( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			
					float2 voronoihash96( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi96( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0; int i, j;
						for ( j = -1; j <= 1; j++ )
						{
							for ( i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash96( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			
					float2 voronoihash89( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi89( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0; int i, j;
						for ( j = -1; j <= 1; j++ )
						{
							for ( i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash89( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			

			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};

			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output;
				ZERO_INITIALIZE(PackedVaryings, output);

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				#if ( SHADER_TARGET >= 50 )
				float recip367 = rcp( _Scale );
				#else
				float recip367 = 1.0 / _Scale;
				#endif
				float Scale228 = recip367;
				float4 appendResult329 = (float4(_NoiseScale3D.x , _NoiseScale3D.y , _NoiseScale3D.z , 0.0));
				float3 WindDir317 = ( ( float3( 1, 1, 1 ) * -2 + 1 ) * _WindDirection );
				float3 break332 = WindDir317;
				float temp_output_333_0 = ( abs( break332.x ) + abs( break332.y ) + abs( break332.z ) );
				float4 appendResult340 = (float4(( break332.x / temp_output_333_0 ) , ( break332.y / temp_output_333_0 ) , ( break332.z / temp_output_333_0 ) , 0.0));
				float4 WindDirWeights341 = appendResult340;
				float4 break344 = abs( WindDirWeights341 );
				float4 appendResult330 = (float4(_NoiseScale3D.y , _NoiseScale3D.x , _NoiseScale3D.z , 0.0));
				float4 appendResult331 = (float4(_NoiseScale3D.y , _NoiseScale3D.z , _NoiseScale3D.x , 0.0));
				float4 transform70 = mul(GetObjectToWorldMatrix(),input.positionOS);
				float4 temp_output_71_0 = (transform70).xyzw;
				float simplePerlin3D112 = snoise( ( Scale228 * ( ( ( appendResult329 * break344.x ) + ( appendResult330 * break344.y ) + ( appendResult331 * break344.z ) ) * (temp_output_71_0*1.0 + float4( ( ( WindDir317 + _AdditionalNoiseDir ) * ( _NoiseSpeed * _TimeParameters.x * _WindSpeed ) ) , 0.0 )) ) ).xyz*_NoiseScale );
				simplePerlin3D112 = simplePerlin3D112*0.5 + 0.5;
				float temp_output_118_0 = saturate(  (0.0 + ( simplePerlin3D112 - 0.0 ) * ( 1.0 - 0.0 ) / ( 1.0 - 0.0 ) ) );
				float temp_output_198_0 = ( Scale228 * _VoronoiScale );
				float time97 = 0.0;
				float2 voronoiSmoothId97 = 0;
				float4 break84 = (temp_output_71_0*1.0 + float4( ( ( _TimeParameters.x * _VorSpeed * _WindSpeed ) * ( WindDir317 + _AdditionalVoronoiDir ) ) , 0.0 ));
				float2 appendResult85 = (float2(break84.z , break84.y));
				float2 coords97 = appendResult85 * temp_output_198_0;
				float2 id97 = 0;
				float2 uv97 = 0;
				float fade97 = 0.5;
				float voroi97 = 0;
				float rest97 = 0;
				for( int it97 = 0; it97 <3; it97++ ){
				voroi97 += fade97 * voronoi97( coords97, time97, id97, uv97, 0,voronoiSmoothId97 );
				rest97 += fade97;
				coords97 *= 2;
				fade97 *= 0.5;
				}//Voronoi97
				voroi97 /= rest97;
				float3 objToWorldDir91 = mul( GetObjectToWorldMatrix(), float4( input.normalOS, 0.0 ) ).xyz;
				float3 temp_output_93_0 = pow( abs( objToWorldDir91 ) , 5.0 );
				float dotResult94 = dot( temp_output_93_0 , float3( 1,1,1 ) );
				float3 break104 = ( temp_output_93_0 / dotResult94 );
				float time96 = 217.0;
				float2 voronoiSmoothId96 = 0;
				float2 appendResult86 = (float2(break84.z , break84.x));
				float2 coords96 = appendResult86 * temp_output_198_0;
				float2 id96 = 0;
				float2 uv96 = 0;
				float fade96 = 0.5;
				float voroi96 = 0;
				float rest96 = 0;
				for( int it96 = 0; it96 <3; it96++ ){
				voroi96 += fade96 * voronoi96( coords96, time96, id96, uv96, 0,voronoiSmoothId96 );
				rest96 += fade96;
				coords96 *= 2;
				fade96 *= 0.5;
				}//Voronoi96
				voroi96 /= rest96;
				float time89 = 137.0;
				float2 voronoiSmoothId89 = 0;
				float2 appendResult87 = (float2(break84.x , break84.y));
				float2 coords89 = appendResult87 * temp_output_198_0;
				float2 id89 = 0;
				float2 uv89 = 0;
				float fade89 = 0.5;
				float voroi89 = 0;
				float rest89 = 0;
				for( int it89 = 0; it89 <3; it89++ ){
				voroi89 += fade89 * voronoi89( coords89, time89, id89, uv89, 0,voronoiSmoothId89 );
				rest89 += fade89;
				coords89 *= 2;
				fade89 *= 0.5;
				}//Voronoi89
				voroi89 /= rest89;
				float temp_output_190_0 = saturate(  (0.0 + ( ( 1.0 - ( ( ( voroi97 * break104.x ) + ( voroi96 * break104.y ) ) + ( voroi89 * break104.z ) ) ) - 0.65 ) * ( 1.0 - 0.0 ) / ( 1.0 - 0.65 ) ) );
				float temp_output_122_0 = saturate( ( ( temp_output_118_0 * _NoiseStrength ) + ( temp_output_190_0 * _VoronoiStrength ) ) );
				float3 ase_objectScale = float3( length( GetObjectToWorldMatrix()[ 0 ].xyz ), length( GetObjectToWorldMatrix()[ 1 ].xyz ), length( GetObjectToWorldMatrix()[ 2 ].xyz ) );
				float3 temp_output_184_0 = ( ( ( temp_output_122_0 - 0.5 ) * input.normalOS * ( _NoiseDisplacement / Scale228 ) ) / ase_objectScale );
				float4 temp_output_2_0_g146 = _Mask0;
				float3 worldToObj306 = mul( GetWorldToObjectMatrix(), float4( (temp_output_2_0_g146).xyz, 1 ) ).xyz;
				float3 ase_objectPosition = GetAbsolutePositionWS( UNITY_MATRIX_M._m03_m13_m23 );
				float4 appendResult313 = (float4((temp_output_2_0_g146).w , 0.0 , 0.0 , 0.0));
				float3 worldToObj307 = mul( GetWorldToObjectMatrix(), float4( ( float4( ase_objectPosition , 0.0 ) + appendResult313 ).xyz, 1 ) ).xyz;
				float3 temp_output_304_0 = ( temp_output_184_0 + ( min( ( distance( ( temp_output_184_0 + input.positionOS.xyz ) , worldToObj306 ) - length( worldToObj307 ) ), 0.0 ) * input.normalOS ) );
				float4 temp_output_2_0_g153 = _Mask1;
				float3 worldToObj357 = mul( GetWorldToObjectMatrix(), float4( (temp_output_2_0_g153).xyz, 1 ) ).xyz;
				float4 appendResult354 = (float4((temp_output_2_0_g153).w , 0.0 , 0.0 , 0.0));
				float3 worldToObj358 = mul( GetWorldToObjectMatrix(), float4( ( float4( ase_objectPosition , 0.0 ) + appendResult354 ).xyz, 1 ) ).xyz;
				float3 VertexOffset305 = ( temp_output_304_0 + ( min( ( distance( ( temp_output_304_0 + input.positionOS.xyz ) , worldToObj357 ) - length( worldToObj358 ) ), 0.0 ) * input.normalOS ) );
				
				float3 objectToViewPos = TransformWorldToView( TransformObjectToWorld( input.positionOS.xyz ) );
				float eyeDepth = -objectToViewPos.z;
				output.ase_texcoord1.x = eyeDepth;
				
				output.ase_color = input.ase_color;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord1.yzw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = VertexOffset305;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				output.positionCS = ASE_ADJUST_CLIP_POSITION( vertexInput.positionCS );
				output.positionWS = vertexInput.positionWS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.tangentOS = input.tangentOS;
				output.ase_color = input.ase_color;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.tangentOS = patch[0].tangentOS * bary.x + patch[1].tangentOS * bary.y + patch[2].tangentOS * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag( PackedVaryings input
				#if defined( ASE_WRITE_DEPTH )
				,out float outputDepth : ASE_SV_DEPTH
				#endif
				 ) : SV_Target
			{
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				float3 PositionWS = input.positionWS;
				float3 PositionRWS = GetCameraRelativePositionWS( PositionWS );
				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;

				float depth01_368 = SHADERGRAPH_SAMPLE_SCENE_DEPTH( ScreenPosNorm.xy );
				float temp_output_385_0 = abs( ( _ProjectionParams.z - _ProjectionParams.y ) );
				float eyeDepth = input.ase_texcoord1.x;
				float cameraDepthFade389 = (( eyeDepth -_ProjectionParams.y - 0.0 ) / temp_output_385_0);
				float temp_output_1_0_g160 = saturate( ( ( ( ( 1.0 - depth01_368 ) - cameraDepthFade389 ) * temp_output_385_0 ) / _DepthDistance ) );
				float DepthFade43 = saturate( ( ( ( pow( temp_output_1_0_g160 , 5.0 ) * 6.0 ) - ( pow( temp_output_1_0_g160 , 4.0 ) * 15.0 ) ) + ( pow( temp_output_1_0_g160 , 3.0 ) * 10.0 ) ) );
				float4 lerpResult161 = lerp( _BaseColor , input.ase_color , _VertexColorStrength);
				float4 temp_output_2_0_g155 = lerpResult161;
				float BaseColorAlpha162 = (temp_output_2_0_g155).a;
				

				surfaceDescription.Alpha = ( DepthFade43 * BaseColorAlpha162 );
				#if defined( _ALPHATEST_ON )
					surfaceDescription.AlphaClipThreshold = 0.0;
				#endif

				#if defined( ASE_WRITE_DEPTH )
					input.positionCS.z = input.positionCS.z;
				#endif

				#ifdef _ALPHATEST_ON
					clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
				#endif

				#if defined( ASE_WRITE_DEPTH )
					outputDepth = input.positionCS.z;
				#endif

				return unity_SelectionID;
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "MotionVectors"
			Tags { "LightMode"="MotionVectors" }

			ColorMask RG

			HLSLPROGRAM

			#define ASE_GEOMETRY
			#define _ALPHATEST_ON
			#define _NORMAL_DROPOFF_TS 1
			#define ASE_TIME_BASED_MOTION_VECTORS
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#define ASE_FOG 1
			#define ASE_WRITE_DEPTH
			#define ASE_TRANSLUCENCY 1
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define _EMISSION
			#define ASE_VERSION 19912
			#define ASE_SRP_VERSION 170300


			#pragma vertex vert
			#pragma fragment frag

			#if defined( _SPECULAR_SETUP ) && defined( ASE_LIGHTING_SIMPLE )
				#if defined( _SPECULARHIGHLIGHTS_OFF )
					#undef _SPECULAR_COLOR
				#else
					#define _SPECULAR_COLOR
				#endif
			#endif

            #define SHADERPASS SHADERPASS_MOTION_VECTORS

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
		    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
		    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
		    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
			#endif

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MotionVectorsCommon.hlsl"

			

			#if defined(ASE_WRITE_DEPTH_CONSERVATIVE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			#if ( UNITY_VERSION < 60010000 )
				#define APPLICATION_SPACE_WARP_MOTION APLICATION_SPACE_WARP_MOTION
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 positionOld : TEXCOORD4;
				#if _ADD_PRECOMPUTED_VELOCITY
					float3 alembicMotionVector : TEXCOORD5;
				#endif
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float4 positionCSNoJitter : TEXCOORD0;
				float4 previousPositionCSNoJitter : TEXCOORD1;
				float3 positionWS : TEXCOORD2;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _EmmissiveColor;
			float4 _BaseColor;
			float4 _Mask1;
			float4 _Mask0;
			float3 _NoiseScale3D;
			float3 _WindDirection;
			float3 _AdditionalNoiseDir;
			float3 _AdditionalVoronoiDir;
			float _Scale;
			float _EmmissiveNoiseColor;
			float _VertexColorStrength;
			float _NoiseColorStrength;
			float _NoiseColorMix;
			float _VoronoiStrength;
			float _DepthDistance;
			float _VorSpeed;
			float _VoronoiScale;
			float _NoiseStrength;
			float _NoiseScale;
			float _WindSpeed;
			float _NoiseSpeed;
			float _NoiseDisplacement;
			float _EmmissiveDepthFade;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TRANSMISSION
				float _TransmissionShadow;
			#endif
			#ifdef ASE_TRANSLUCENCY
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			

			
			// Applies the graph's vertex stage at a given time so the motion vector pass can
			// evaluate the current frame and re-evaluate the previous frame (procedural / time-based animation).
			Attributes ASEApplyVertexModification( Attributes input, float3 timeParameters, inout PackedVaryings output, out float3 customMotionVector  )
			{
				float3 currentTimeParameters = _TimeParameters.xyz;
				_TimeParameters.xyz = timeParameters;

				

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				customMotionVector = float3(0, 0, 0);

				_TimeParameters.xyz = currentTimeParameters;
				return input;
			}

			PackedVaryings VertexFunction( Attributes input )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				Attributes defaultInput = input;
				float3 currentMotionVector;
				input = ASEApplyVertexModification( input, _TimeParameters.xyz, output, currentMotionVector );

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				#if defined(APPLICATION_SPACE_WARP_MOTION)
					float4 positionCSNoJitter = mul(_NonJitteredViewProjMatrix, mul(UNITY_MATRIX_M, input.positionOS));
					float4 positionCS = positionCSNoJitter;
				#else
					float4 positionCS = vertexInput.positionCS;
					float4 positionCSNoJitter = mul(_NonJitteredViewProjMatrix, mul(UNITY_MATRIX_M, input.positionOS));
				#endif

				// Custom output and automatic time-based motion are mutually exclusive.
				#if defined(ASE_CUSTOM_MOTION_VECTOR)
					float3 prevPositionOS = ( unity_MotionVectorsParams.x == 1 ) ? input.positionOld : input.positionOS.xyz;
					prevPositionOS -= currentMotionVector;
				#else
					float3 prevPositionOS = ( unity_MotionVectorsParams.x == 1 ) ? input.positionOld : defaultInput.positionOS.xyz;
					#ifdef ASE_TIME_BASED_MOTION_VECTORS
						Attributes prevInput = defaultInput;
						prevInput.positionOS.xyz = prevPositionOS;
						PackedVaryings prevOutput = (PackedVaryings)0;
						float3 prevMotionVector;
						prevInput = ASEApplyVertexModification( prevInput, _LastTimeParameters.xyz, prevOutput, prevMotionVector );
						prevPositionOS = prevInput.positionOS.xyz;
					#endif
				#endif
				#if _ADD_PRECOMPUTED_VELOCITY
					prevPositionOS -= input.alembicMotionVector;
				#endif
				float4 previousPositionCSNoJitter = mul( _PrevViewProjMatrix, mul( UNITY_PREV_MATRIX_M, float4( prevPositionOS, 1 ) ) );

				output.positionCS = ASE_ADJUST_CLIP_POSITION( positionCS );
				output.positionCSNoJitter = ASE_ADJUST_CLIP_POSITION( positionCSNoJitter );
				output.previousPositionCSNoJitter = ASE_ADJUST_CLIP_POSITION( previousPositionCSNoJitter );
				output.positionWS = vertexInput.positionWS;

				return output;
			}

			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}

			half4 frag(	PackedVaryings input
				#if defined( ASE_WRITE_DEPTH )
				,out float outputDepth : ASE_SV_DEPTH
				#endif
				 ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				float3 PositionWS = input.positionWS;
				float3 PositionRWS = GetCameraRelativePositionWS( PositionWS );
				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;

				

				float Alpha = 1;
				#if defined( _ALPHATEST_ON )
					float AlphaClipThreshold = _Cutoff;
				#endif

				#if defined( ASE_WRITE_DEPTH )
					input.positionCS.z = input.positionCS.z;
				#endif

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				#if defined(ASE_CHANGES_WORLD_POS)
					float3 positionOS = mul( GetWorldToObjectMatrix(),  float4( PositionWS, 1.0 ) ).xyz;
					float3 previousPositionWS = mul( GetPrevObjectToWorldMatrix(),  float4( positionOS, 1.0 ) ).xyz;
					input.positionCSNoJitter = mul( _NonJitteredViewProjMatrix, float4( PositionWS, 1.0 ) );
					input.previousPositionCSNoJitter = mul( _PrevViewProjMatrix, float4( previousPositionWS, 1.0 ) );
				#endif

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined( ASE_WRITE_DEPTH )
					outputDepth = input.positionCS.z;
				#endif

				#if defined(APPLICATION_SPACE_WARP_MOTION)
					return float4( CalcAswNdcMotionVectorFromCsPositions( input.positionCSNoJitter, input.previousPositionCSNoJitter ), 1 );
				#else
					return float4( CalcNdcMotionVectorFromCsPositions( input.positionCSNoJitter, input.previousPositionCSNoJitter ), 0, 0 );
				#endif
			}
			ENDHLSL
		}

	
	}
	

	

	CustomEditor "UnityEditor.ShaderGraphLitGUI"
	FallBack "Hidden/Shader Graph/FallbackError"
	
	Fallback Off
}
/*ASEBEGIN
Version=19912
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":130,"pos":[-768,1424],"params":["Inherit","False","4809.925","1890.271","","80","342","344","343","348","346","347","330","331","143","114","152","151","128","127","239","238","184","183","123","124","232","181","229","125","122","121","115","120","119","116","118","190","191","117","112","192","196","113","111","195","145","109","110","78","89","108","107","77","96","97","87","322","198","74","85","86","320","76","84","88","197","72","228","79","227","83","71","323","91","80","70","134","325","82","90","81","69","329","345","367","Tri Planar Noise","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":293,"pos":[4112,1984],"params":["Inherit","False","1979.398","736.2354","","10","306","296","294","315","304","303","299","301","298","297","Sphere Mask 0","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":350,"pos":[6464,2000],"params":["Inherit","False","1780.413","726.1177","","10","365","364","363","362","361","359","357","353","352","351","Sphere Mask 1","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":394,"pos":[368,672],"params":["Inherit","False","1770.762","380.8553","","13","43","402","399","36","392","401","389","390","371","368","385","387","384","Depth Fade 2D","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":31,"pos":[1388.116,-224],"params":["Inherit","False","1507.612","770.0389","","15","1","162","13","16","160","161","20","19","146","147","21","30","28","29","163","Albedo","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":68,"pos":[1392,-1504],"params":["Inherit","False","1082.072","560.847","","9","150","140","149","141","60","66","62","61","67","Emmission","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":59,"pos":[1744,-704],"params":["Inherit","False","1151.964","403.8841","","7","56","58","57","55","54","136","137","Translucency","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":134,"pos":[48,2800],"params":["Inherit","False","999.4751","434.6895","Smooth Axis Fade ","5","93","92","104","95","94","","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":315,"pos":[4368,2416],"params":["Inherit","False","932","266.2","Radius to object space","5","314","307","312","311","313","","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":352,"pos":[6720,2432],"params":["Inherit","False","932","266.2","Radius to object space","5","360","358","356","355","354","","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.Vector3Node, AmplifyShaderEditor","id":316,"pos":[-1488,1952],"params":["Inherit","False","Property","_WindDirection","WindDirection","4","0","Create","True","0","0","0","False","0","False","Object","-1","","-1,-0.3,-0.3","-1,-0.3,-0.3","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.FlipNode, AmplifyShaderEditor","id":328,"pos":[-1232,1952],"params":["Inherit","False","True","True","True","True","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":317,"pos":[-1040,1952],"params":["Inherit","False","WindDir","-1","True","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":326,"pos":[-1968,1600],"params":["Inherit","False","317","WindDir","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.AbsOpNode, AmplifyShaderEditor","id":334,"pos":[-1616,1568],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.AbsOpNode, AmplifyShaderEditor","id":335,"pos":[-1616,1648],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.AbsOpNode, AmplifyShaderEditor","id":336,"pos":[-1616,1728],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor","id":332,"pos":[-1776,1600],"params":["Inherit","False","FLOAT3","1","0","FLOAT3","0,0,0","False","16","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT","5","FLOAT","6","FLOAT","7","FLOAT","8","FLOAT","9","FLOAT","10","FLOAT","11","FLOAT","12","FLOAT","13","FLOAT","14","FLOAT","15"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":333,"pos":[-1472,1616],"params":["Inherit","False","3","3","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor","id":337,"pos":[-1344,1568],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor","id":338,"pos":[-1344,1664],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor","id":339,"pos":[-1344,1760],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":325,"pos":[-624,2656],"params":["Inherit","False","317","WindDir","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":81,"pos":[-672,2544],"params":["Inherit","False","Property","_VorSpeed","Vor Speed","12","0","Create","True","0","0","0","False","0","False","Object","-1","","0.05","0.05","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Vector3Node, AmplifyShaderEditor","id":82,"pos":[-656,2736],"params":["Inherit","False","Property","_AdditionalVoronoiDir","AdditionalVoronoiDir","14","0","Create","True","0","0","0","False","0","False","Object","-1","","0.7,-0.3,-0.3","0.7,-0.3,-0.3","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.NormalVertexDataNode, AmplifyShaderEditor","id":90,"pos":[-400,3088],"params":["Inherit","False","0","5","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":340,"pos":[-1184,1632],"params":["Inherit","False","FLOAT4","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":80,"pos":[-448,2448],"params":["Inherit","False","3","3","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":323,"pos":[-400,2640],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.TransformDirectionNode, AmplifyShaderEditor","id":91,"pos":[-208,3088],"params":["Inherit","False","Object","World","False","Fast","False","1","0","FLOAT3","0,0,0","False","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":341,"pos":[-1024,1632],"params":["Inherit","False","WindDirWeights","-1","True","1","0","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":83,"pos":[-224,2448],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.AbsOpNode, AmplifyShaderEditor","id":92,"pos":[64,2880],"params":["Inherit","True","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.ScaleAndOffsetNode, AmplifyShaderEditor","id":79,"pos":[144,2368],"params":["Inherit","False","3","0","FLOAT4","0,0,0,0","False","1","FLOAT","1","False","2","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":342,"pos":[112,1808],"params":["Inherit","False","341","WindDirWeights","1","0","OBJECT","","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor","id":73,"pos":[-1008,2240],"params":["Inherit","False","1","0","FLOAT","1","False","5","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":319,"pos":[-1008,2144],"params":["Inherit","False","Property","_WindSpeed","WindSpeed","5","0","Create","True","0","0","0","False","0","False","Object","-1","","1","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.PosVertexDataNode, AmplifyShaderEditor","id":69,"pos":[-656,2208],"params":["Inherit","False","1","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.PowerNode, AmplifyShaderEditor","id":93,"pos":[272,2928],"params":["Inherit","False","False","2","0","FLOAT3","0,0,0","False","1","FLOAT","5","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.DotProductOpNode, AmplifyShaderEditor","id":94,"pos":[448,3008],"params":["Inherit","False","2","0","FLOAT3","0,0,0","False","1","FLOAT3","1,1,1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":197,"pos":[464,2096],"params":["Inherit","False","228","Scale","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor","id":84,"pos":[400,2368],"params":["Inherit","False","FLOAT4","1","0","FLOAT4","0,0,0,0","False","16","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT","5","FLOAT","6","FLOAT","7","FLOAT","8","FLOAT","9","FLOAT","10","FLOAT","11","FLOAT","12","FLOAT","13","FLOAT","14","FLOAT","15"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":320,"pos":[-608,1824],"params":["Inherit","False","317","WindDir","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.AbsOpNode, AmplifyShaderEditor","id":343,"pos":[320,1808],"params":["Inherit","False","1","0","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":72,"pos":[-656,2064],"params":["Inherit","False","Property","_NoiseSpeed","Noise Speed","11","0","Create","True","0","0","0","False","0","False","Object","-1","","0.2","0.2","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Vector3Node, AmplifyShaderEditor","id":76,"pos":[-640,1904],"params":["Inherit","False","Property","_AdditionalNoiseDir","Additional Noise Dir","13","0","Create","True","0","0","0","False","0","False","Object","-1","","0,0,0","0,0,0","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":88,"pos":[464,2176],"params":["Inherit","False","Property","_VoronoiScale","Voronoi Scale","15","0","Create","True","0","0","0","False","0","False","Object","-1","","1.4","1.4","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Vector3Node, AmplifyShaderEditor","id":143,"pos":[400,1648],"params":["Inherit","False","Property","_NoiseScale3D","Noise Scale 3D","20","0","Create","True","0","0","0","False","0","False","Object","-1","","1,0.2,0.6","1,0.2,0.6","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.ObjectToWorldTransfNode, AmplifyShaderEditor","id":70,"pos":[-448,2208],"params":["Inherit","False","1","0","FLOAT4","0,0,0,1","False","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":227,"pos":[-464,1632],"params":["Inherit","False","Property","_Scale","Scale","3","0","Create","True","0","0","0","False","0","False","Object","-1","","1","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":86,"pos":[608,2400],"params":["Inherit","False","FLOAT2","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":85,"pos":[608,2304],"params":["Inherit","False","FLOAT2","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor","id":95,"pos":[608,2928],"params":["Inherit","False","2","0","FLOAT3","0,0,0","False","1","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":74,"pos":[-448,2064],"params":["Inherit","False","3","3","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":198,"pos":[656,2096],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":322,"pos":[-368,1936],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":331,"pos":[624,1808],"params":["Inherit","False","FLOAT4","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":329,"pos":[624,1504],"params":["Inherit","False","FLOAT4","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":330,"pos":[624,1648],"params":["Inherit","False","FLOAT4","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor","id":344,"pos":[432,1808],"params":["Inherit","False","FLOAT4","1","0","FLOAT4","0,0,0,0","False","16","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT","5","FLOAT","6","FLOAT","7","FLOAT","8","FLOAT","9","FLOAT","10","FLOAT","11","FLOAT","12","FLOAT","13","FLOAT","14","FLOAT","15"]}
{"type":"AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor","id":71,"pos":[-192,2208],"params":["Inherit","False","True","True","True","True","1","0","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.ReciprocalOpNode, AmplifyShaderEditor","id":367,"pos":[-304,1632],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":87,"pos":[608,2496],"params":["Inherit","False","FLOAT2","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor","id":104,"pos":[832,2992],"params":["Inherit","True","FLOAT3","1","0","FLOAT3","0,0,0","False","16","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT","5","FLOAT","6","FLOAT","7","FLOAT","8","FLOAT","9","FLOAT","10","FLOAT","11","FLOAT","12","FLOAT","13","FLOAT","14","FLOAT","15"]}
{"type":"AmplifyShaderEditor.VoronoiNode, AmplifyShaderEditor","id":97,"pos":[912,1968],"params":["Inherit","True","0","0","1","0","3","False","1","False","False","False","4","0","FLOAT2","0,0","False","1","FLOAT","0","False","2","FLOAT","1","False","3","FLOAT","0","False","3","FLOAT","0","FLOAT2","1","FLOAT2","2"]}
{"type":"AmplifyShaderEditor.VoronoiNode, AmplifyShaderEditor","id":96,"pos":[912,2240],"params":["Inherit","True","0","0","1","0","3","False","1","False","False","False","4","0","FLOAT2","0,0","False","1","FLOAT","217","False","2","FLOAT","1","False","3","FLOAT","0","False","3","FLOAT","0","FLOAT2","1","FLOAT2","2"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":77,"pos":[-224,2000],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":347,"pos":[784,1808],"params":["Inherit","False","2","2","0","FLOAT4","0,0,0,0","False","1","FLOAT","0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":346,"pos":[784,1648],"params":["Inherit","False","2","2","0","FLOAT4","0,0,0,0","False","1","FLOAT","0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":345,"pos":[784,1504],"params":["Inherit","False","2","2","0","FLOAT4","0,0,0,0","False","1","FLOAT","0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":228,"pos":[-144,1632],"params":["Inherit","False","Scale","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":107,"pos":[1232,2032],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":108,"pos":[1216,2192],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.VoronoiNode, AmplifyShaderEditor","id":89,"pos":[912,2512],"params":["Inherit","True","0","0","1","0","3","False","1","False","False","False","4","0","FLOAT2","0,0","False","1","FLOAT","137","False","2","FLOAT","1","False","3","FLOAT","0","False","3","FLOAT","0","FLOAT2","1","FLOAT2","2"]}
{"type":"AmplifyShaderEditor.ScaleAndOffsetNode, AmplifyShaderEditor","id":78,"pos":[144,2016],"params":["Inherit","False","3","0","FLOAT4","0,0,0,0","False","1","FLOAT","1","False","2","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":348,"pos":[976,1616],"params":["Inherit","False","3","3","0","FLOAT4","0,0,0,0","False","1","FLOAT4","0,0,0,0","False","2","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":110,"pos":[1408,2128],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":109,"pos":[1184,2448],"params":["Inherit","True","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":145,"pos":[1328,1712],"params":["Inherit","False","2","2","0","FLOAT4","0,0,0,0","False","1","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":195,"pos":[1296,1632],"params":["Inherit","False","228","Scale","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":111,"pos":[1568,2256],"params":["Inherit","True","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":196,"pos":[1536,1680],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":113,"pos":[1536,1840],"params":["Inherit","False","Property","_NoiseScale","Noise Scale","16","0","Create","True","0","0","0","False","0","False","Object","-1","","20","20","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.NoiseGeneratorNode, AmplifyShaderEditor","id":112,"pos":[1760,1712],"params":["Inherit","True","Simplex3D","True","False","2","0","FLOAT3","0,0,0","False","1","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor","id":192,"pos":[1792,2144],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TFHCRemapNode, AmplifyShaderEditor","id":117,"pos":[2064,1728],"params":["Inherit","True","5","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","1","False","3","FLOAT","0","False","4","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TFHCRemapNode, AmplifyShaderEditor","id":191,"pos":[1824,2288],"params":["Inherit","True","5","0","FLOAT","0","False","1","FLOAT","0.65","False","2","FLOAT","1","False","3","FLOAT","0","False","4","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ProjectionParams, AmplifyShaderEditor","id":384,"pos":[400,896],"params":["Inherit","False","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":190,"pos":[2112,2304],"params":["Inherit","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":118,"pos":[2352,1728],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":119,"pos":[2336,1824],"params":["Inherit","False","Property","_NoiseStrength","Noise Strength","18","0","Create","True","0","0","0","False","0","False","Object","-1","","0.03","0.04","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":116,"pos":[2272,2192],"params":["Inherit","False","Property","_VoronoiStrength","Voronoi Strength","17","0","Create","True","0","0","0","False","0","False","Object","-1","","0.9","0.8","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Vector4Node, AmplifyShaderEditor","id":294,"pos":[4160,2224],"params":["Inherit","False","Property","_Mask0","Mask0","6","0","Create","True","0","0","0","False","0","False","Object","-1","","1.985208,0.440832,0.2055923,1.2","-0.07034397,0.4851084,-6.79,0.7","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":313,"pos":[4624,2496],"params":["Inherit","False","FLOAT4","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.ObjectPositionNode, AmplifyShaderEditor","id":311,"pos":[4416,2464],"params":["Inherit","False","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor","id":387,"pos":[624,896],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":120,"pos":[2544,1760],"params":["Inherit","True","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":115,"pos":[2544,2000],"params":["Inherit","True","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor","id":296,"pos":[4432,2224],"params":["Inherit","False","Alpha Split","-1","","146","07dab7960105b86429ac8eebd729ed6d","0","1","2","FLOAT4","0,0,0,0","False","2","FLOAT3","0","FLOAT","6"]}
{"type":"AmplifyShaderEditor.PosVertexDataNode, AmplifyShaderEditor","id":238,"pos":[3696,2160],"params":["Inherit","False","0","0","5","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":312,"pos":[4784,2464],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.Vector4Node, AmplifyShaderEditor","id":351,"pos":[6512,2240],"params":["Inherit","False","Property","_Mask1","Mask1","7","0","Create","True","0","0","0","False","0","False","Object","-1","","1.985208,0.440832,0.2055923,1.2","0.842,-0.28,0.51,0.7","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":354,"pos":[6976,2512],"params":["Inherit","False","FLOAT4","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.ObjectPositionNode, AmplifyShaderEditor","id":355,"pos":[6768,2480],"params":["Inherit","False","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.AbsOpNode, AmplifyShaderEditor","id":385,"pos":[768,896],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ScreenDepthNode, AmplifyShaderEditor","id":368,"pos":[768,720],"params":["Inherit","False","3","1","0","FLOAT4","0,0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":121,"pos":[2800,1904],"params":["Inherit","True","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":239,"pos":[3920,2112],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.TransformPositionNode, AmplifyShaderEditor","id":306,"pos":[4656,2224],"params":["Inherit","False","World","Object","False","Fast","True","1","0","FLOAT3","0,0,0","False","5","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.TransformPositionNode, AmplifyShaderEditor","id":307,"pos":[4912,2464],"params":["Inherit","False","World","Object","False","Fast","True","1","0","FLOAT3","0,0,0","False","5","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor","id":353,"pos":[6784,2240],"params":["Inherit","False","Alpha Split","-1","","153","07dab7960105b86429ac8eebd729ed6d","0","1","2","FLOAT4","0,0,0,0","False","2","FLOAT3","0","FLOAT","6"]}
{"type":"AmplifyShaderEditor.PosVertexDataNode, AmplifyShaderEditor","id":349,"pos":[6128,2208],"params":["Inherit","False","0","0","5","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":356,"pos":[7136,2480],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT4","0,0,0,0","False","1","FLOAT4","0"]}
{"type":"AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor","id":371,"pos":[992,720],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CameraDepthFade, AmplifyShaderEditor","id":389,"pos":[896,816],"params":["Inherit","False","3","2","FLOAT3","0,0,0","False","0","FLOAT","9","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":122,"pos":[3024,1904],"params":["Inherit","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":229,"pos":[3056,2272],"params":["Inherit","False","228","Scale","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":125,"pos":[3024,2192],"params":["Inherit","False","Property","_NoiseDisplacement","Noise Displacement","19","0","Create","True","0","0","0","False","0","False","Object","-1","","0.5","0.5","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.DistanceOpNode, AmplifyShaderEditor","id":297,"pos":[4880,2128],"params":["Inherit","False","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LengthOpNode, AmplifyShaderEditor","id":314,"pos":[5120,2464],"params":["Inherit","False","1","0","FLOAT3","0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":366,"pos":[6336,2144],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.TransformPositionNode, AmplifyShaderEditor","id":357,"pos":[7008,2240],"params":["Inherit","False","World","Object","False","Fast","True","1","0","FLOAT3","0,0,0","False","5","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.TransformPositionNode, AmplifyShaderEditor","id":358,"pos":[7264,2480],"params":["Inherit","False","World","Object","False","Fast","True","1","0","FLOAT3","0,0,0","False","5","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor","id":390,"pos":[1152,752],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor","id":181,"pos":[3264,1904],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","0.5","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor","id":232,"pos":[3264,2192],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.NormalVertexDataNode, AmplifyShaderEditor","id":124,"pos":[3232,2016],"params":["Inherit","False","0","5","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor","id":298,"pos":[5168,2112],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.DistanceOpNode, AmplifyShaderEditor","id":359,"pos":[7232,2144],"params":["Inherit","False","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LengthOpNode, AmplifyShaderEditor","id":360,"pos":[7472,2480],"params":["Inherit","False","1","0","FLOAT3","0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":392,"pos":[1312,848],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":36,"pos":[1280,944],"params":["Inherit","False","Property","_DepthDistance","Depth Distance","8","0","Create","True","0","0","0","False","0","False","Object","-1","","0.4","0.4","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":123,"pos":[3456,1920],"params":["Inherit","False","3","3","0","FLOAT","0","False","1","FLOAT3","0,0,0","False","2","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.ObjectScaleNode, AmplifyShaderEditor","id":183,"pos":[3472,2096],"params":["Inherit","False","False","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.SimpleMinOpNode, AmplifyShaderEditor","id":301,"pos":[5328,2112],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.NormalVertexDataNode, AmplifyShaderEditor","id":299,"pos":[5264,2240],"params":["Inherit","False","0","5","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor","id":361,"pos":[7648,2144],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.VertexColorNode, AmplifyShaderEditor","id":16,"pos":[1664,288],"params":["Inherit","False","0","5","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":13,"pos":[1440,176],"params":["Inherit","False","Property","_BaseColor","Base Color","0","0","Create","True","0","0","0","False","0","False","Object","-1","","1,1,1,1","1,1,1,1","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":160,"pos":[1872,368],"params":["Inherit","False","Property","_VertexColorStrength","Vertex Color Strength","23","0","Create","True","0","0","0","False","0","False","Object","-1","","0.7030082","0.7030082","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor","id":401,"pos":[1456,848],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor","id":184,"pos":[3648,2000],"params":["Inherit","False","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":303,"pos":[5456,2112],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT3","-1,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMinOpNode, AmplifyShaderEditor","id":362,"pos":[7808,2144],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.NormalVertexDataNode, AmplifyShaderEditor","id":363,"pos":[7744,2272],"params":["Inherit","False","0","5","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":161,"pos":[2160,240],"params":["Inherit","False","3","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","2","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":399,"pos":[1568,848],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":304,"pos":[5632,2048],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":364,"pos":[7936,2144],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT3","-1,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor","id":163,"pos":[2325.137,419.9323],"params":["Inherit","False","Alpha Split","-1","","155","07dab7960105b86429ac8eebd729ed6d","0","1","2","COLOR","0,0,0,0","False","2","FLOAT3","0","FLOAT","6"]}
{"type":"AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor","id":402,"pos":[1712,848],"params":["Inherit","False","SmootherStep","-1","","160","4a9458489eca43748a1c8f13823257ba","0","1","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":365,"pos":[8112,2080],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":162,"pos":[2528,448],"params":["Inherit","False","BaseColorAlpha","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":43,"pos":[1920,848],"params":["Inherit","False","DepthFade","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":305,"pos":[8336,2080],"params":["Inherit","False","VertexOffset","-1","True","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":164,"pos":[2608,-976],"params":["Inherit","False","162","BaseColorAlpha","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":133,"pos":[2640,-1056],"params":["Inherit","False","43","DepthFade","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":29,"pos":[2496,224],"params":["Inherit","False","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":28,"pos":[2336,224],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.FresnelNode, AmplifyShaderEditor","id":54,"pos":[1792,-656],"params":["Inherit","False","Standard","WorldNormal","ViewDir","False","False","5","0","FLOAT3","0,0,1","False","4","FLOAT3","0,0,0","False","1","FLOAT","0","False","2","FLOAT","1","False","3","FLOAT","5","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":55,"pos":[2208,-656],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":57,"pos":[2432,-656],"params":["Inherit","True","2","2","0","FLOAT","0","False","1","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":136,"pos":[2672,-656],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":137,"pos":[2464,-416],"params":["Inherit","False","43","DepthFade","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":30,"pos":[2672,224],"params":["Inherit","False","Albedo","-1","True","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":147,"pos":[2224,80],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":146,"pos":[2048,-32],"params":["Inherit","False","43","DepthFade","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":157,"pos":[2608,944],"params":["Inherit","False","3","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":158,"pos":[2768,944],"params":["Inherit","True","NoiseColorMix","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":154,"pos":[2336,720],"params":["Inherit","True","151","NoiseColor3D","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":155,"pos":[2304,912],"params":["Inherit","True","152","NoiseColorVoronoi","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor","id":127,"pos":[3232,1776],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":128,"pos":[3408,1776],"params":["Inherit","False","NoiseColor","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":151,"pos":[2544,1680],"params":["Inherit","False","NoiseColor3D","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":152,"pos":[2544,2224],"params":["Inherit","False","NoiseColorVoronoi","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor","id":114,"pos":[2272,1952],"params":["Inherit","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":53,"pos":[2832,-832],"params":["Inherit","False","305","VertexOffset","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":156,"pos":[2272,1104],"params":["Inherit","False","Property","_NoiseColorMix","NoiseColorMix","22","0","Create","True","0","0","0","False","0","False","Object","-1","","0.96","0.947578","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor","id":67,"pos":[1904,-1232],"params":["Inherit","False","Lerp White To","-1","","161","047d7c189c36a62438973bad9d37b1c2","0","2","1","FLOAT3","0,0,0","False","2","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":61,"pos":[2112,-1328],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","FLOAT3","0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":141,"pos":[2304,-1296],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","FLOAT3","1,1,1","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor","id":149,"pos":[2112,-1216],"params":["Inherit","False","Lerp White To","-1","","162","047d7c189c36a62438973bad9d37b1c2","0","2","1","FLOAT3","0,0,0","False","2","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":140,"pos":[1872,-1120],"params":["Inherit","False","43","DepthFade","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor","id":21,"pos":[2000,80],"params":["Inherit","False","Lerp White To","-1","","163","047d7c189c36a62438973bad9d37b1c2","0","2","1","FLOAT3","0,0,0","False","2","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":19,"pos":[1760,32],"params":["Inherit","False","158","NoiseColorMix","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":20,"pos":[1696,112],"params":["Inherit","False","Property","_NoiseColorStrength","Noise Color Strength","9","0","Create","True","0","0","0","False","0","False","Object","-1","","0.5565214","0.7018638","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":56,"pos":[2032,-576],"params":["Inherit","False","Constant","_Float0","Float 0","3","0","Create","True","0","0","0","False","0","False","Object","-1","","0.5","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":66,"pos":[1552,-1152],"params":["Inherit","False","Property","_EmmissiveNoiseColor","Emmissive Noise Color","10","0","Create","True","0","0","0","False","0","False","Object","-1","","0.2000003","0.2000003","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":150,"pos":[1776,-1040],"params":["Inherit","False","Property","_EmmissiveDepthFade","Emmissive Depth Fade","21","0","Create","True","0","0","0","False","0","False","Object","-1","","0.2086951","0.2086951","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":60,"pos":[1872,-1440],"params":["Inherit","False","Property","_EmmissiveColor","Emmissive Color","2","0","Create","True","0","0","0","False","0","False","Object","-1","","0.5490196,0.627451,0.7294118,0","0.5482876,0.6286016,0.7283019,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":58,"pos":[2192,-528],"params":["Inherit","False","Property","_TranslucencyColor","Translucency Color","1","0","Create","True","0","0","0","False","0","False","Object","-1","","1,1,1,0","1,1,1,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":131,"pos":[2880,-1104],"params":["Inherit","False","Constant","_MetalSmoothness","MetalSmoothness","14","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":165,"pos":[2880,-1024],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":32,"pos":[2912,-1184],"params":["Inherit","False","30","Albedo","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":62,"pos":[1616,-1232],"params":["Inherit","False","158","NoiseColorMix","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":132,"pos":[2880,-912],"params":["Inherit","False","Constant","_AlphaClipConst","Alpha Clip Const","18","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":1,"pos":[1472,-1184],"params":["Float","False","False","-1","2","UnityEditor.ShaderGraphLitGUI","0","12","New Amplify Shader","94348b07e5e8bab40bd6c8a1e3df54cd","True","ExtraPrePass","0","0","ExtraPrePass","6","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","False","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Lit","True","5","True","14","all","0","False","True","1","1","False","","0","False","","0","1","False","","0","False","","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","0","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":3,"pos":[48,-176],"params":["Float","False","False","-1","2","UnityEditor.ShaderGraphLitGUI","0","12","New Amplify Shader","94348b07e5e8bab40bd6c8a1e3df54cd","True","ShadowCaster","0","2","ShadowCaster","0","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","False","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Lit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","False","False","True","False","False","False","False","0","False","","False","False","False","False","False","False","False","False","False","True","1","False","","True","3","False","","False","False","True","1","LightMode=ShadowCaster","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":4,"pos":[48,-176],"params":["Float","False","False","-1","2","UnityEditor.ShaderGraphLitGUI","0","12","New Amplify Shader","94348b07e5e8bab40bd6c8a1e3df54cd","True","DepthOnly","0","3","DepthOnly","0","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","False","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Lit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","False","False","True","True","False","False","False","0","False","","False","False","False","False","False","False","False","False","False","True","1","False","","False","False","False","True","1","LightMode=DepthOnly","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":5,"pos":[48,-176],"params":["Float","False","False","-1","2","UnityEditor.ShaderGraphLitGUI","0","12","New Amplify Shader","94348b07e5e8bab40bd6c8a1e3df54cd","True","Meta","0","4","Meta","0","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","False","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Lit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","2","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","1","LightMode=Meta","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":6,"pos":[48,-176],"params":["Float","False","False","-1","2","UnityEditor.ShaderGraphLitGUI","0","12","New Amplify Shader","94348b07e5e8bab40bd6c8a1e3df54cd","True","Universal2D","0","5","Universal2D","0","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","False","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Lit","True","5","True","14","all","0","False","True","2","5","False","","10","False","","1","1","False","","10","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","False","False","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","1","LightMode=Universal2D","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":7,"pos":[48,-176],"params":["Float","False","False","-1","2","UnityEditor.ShaderGraphLitGUI","0","12","New Amplify Shader","94348b07e5e8bab40bd6c8a1e3df54cd","True","DepthNormals","0","6","DepthNormals","0","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","False","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Lit","True","5","True","14","all","0","False","True","1","1","False","","0","False","","0","1","False","","0","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","1","False","","True","3","False","","False","False","True","1","LightMode=DepthNormalsOnly","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":8,"pos":[48,-176],"params":["Float","False","False","-1","2","UnityEditor.ShaderGraphLitGUI","0","12","New Amplify Shader","94348b07e5e8bab40bd6c8a1e3df54cd","True","GBuffer","0","7","GBuffer","0","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","False","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Lit","True","5","True","14","all","0","False","True","2","5","False","","10","False","","1","1","False","","10","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","10","False","","10","False","","10","False","","6","False","","3","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","1","LightMode=UniversalGBuffer","False","True","12","d3d11","gles","metal","vulkan","xboxone","xboxseries","playstation","ps4","ps5","switch","switch2","webgpu","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":9,"pos":[48,-176],"params":["Float","False","False","-1","2","UnityEditor.ShaderGraphLitGUI","0","12","New Amplify Shader","94348b07e5e8bab40bd6c8a1e3df54cd","True","SceneSelectionPass","0","8","SceneSelectionPass","0","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","False","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Lit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","2","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","1","LightMode=SceneSelectionPass","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":10,"pos":[48,-176],"params":["Float","False","False","-1","2","UnityEditor.ShaderGraphLitGUI","0","12","New Amplify Shader","94348b07e5e8bab40bd6c8a1e3df54cd","True","ScenePickingPass","0","9","ScenePickingPass","0","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","False","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Lit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","1","LightMode=Picking","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":2,"pos":[3088,-1184],"params":["Float","False","True","-1","2","UnityEditor.ShaderGraphLitGUI","0","15","SkyClouds_2D","94348b07e5e8bab40bd6c8a1e3df54cd","True","Forward","0","1","Forward","22","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","False","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","2","False","","True","3","False","","True","True","0","False","","0","False","","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Transparent=RenderType","Queue=Transparent=Queue=0","UniversalMaterialType=Lit","True","5","True","14","all","0","False","True","2","5","False","","10","False","","1","1","False","","10","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","True","True","False","10","False","","10","False","","10","False","","6","False","","3","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","1","LightMode=UniversalForwardOnly","False","False","0","","0","0","Standard","52","Category","0","0","  Instanced Terrain Normals","1","0","Lighting Model","0","0","Workflow","1","638545903288036264","Surface","1","638661781710501321","  Keep Alpha","0","0","  Refraction Model","0","0","  Blend","0","0","Two Sided","1","0","Alpha Clipping","1","0","  Use Shadow Threshold","0","0","Fragment Normal Space","0","0","Forward Only","1","0","Transmission","0","0","  Transmission Shadow","0.5,False,","0","Translucency","1","638555967056712408","  Translucency Strength","1,False,","0","  Normal Distortion","0.5,False,","0","  Scattering","2,False,","0","  Direct","0.9,False,","0","  Ambient","0.1,False,","0","  Shadow","0.5,False,","0","Cast Shadows","0","638545778176623389","Receive Shadows","1","0","Specular Highlights","2","0","Environment Reflections","2","0","Receive SSAO","0","638545838208863654","Motion Vectors","1","0","  Additional Motion Vectors","1","0","  Alembic Motion Vectors","0","0","  XR Motion Vectors","0","0","GPU Instancing","1","0","LOD CrossFade","1","0","Built-in Fog","1","0","_FinalColorxAlpha","0","0","Meta Pass","1","0","Override Baked GI","0","0","Extra Pre Pass","0","0","Tessellation","0","638546589066032097","  Phong","1","638545776167988407","  Strength","0.5,False,","0","  Type","2","638572589216977204","  Tess","8,False,","638555223982842409","  Min","1,False,","638549165261763217","  Max","20,False,","638549165187283418","  Edge Length","7,False,","638546587549290024","  Max Displacement","10,False,","638546587107747633","Write Depth","1","638545930608142192","  Conservative","0","0","Vertex Position","1","0","Debug Display","0","0","Clear Coat","0","0","0","12","False","True","False","True","True","True","True","False","True","True","True","False","False","","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":403,"pos":[3088,-1084],"params":["Float","False","False","-1","3","UnityEditor.ShaderGraphLitGUI","0","1","New Amplify Shader","94348b07e5e8bab40bd6c8a1e3df54cd","True","MotionVectors","0","10","MotionVectors","0","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","False","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Lit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","True","True","False","False","0","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","True","1","LightMode=MotionVectors","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":404,"pos":[3088,-1084],"params":["Float","False","False","-1","3","UnityEditor.ShaderGraphLitGUI","0","1","New Amplify Shader","94348b07e5e8bab40bd6c8a1e3df54cd","True","XRMotionVectors","0","11","XRMotionVectors","0","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","False","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Lit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","True","1","False","","255","False","","1","False","","7","False","","3","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","False","False","False","False","True","1","LightMode=XRMotionVectors","False","False","0","","0","0","Standard","0","False","0"]}
{"wire":[328,0,316,0]}
{"wire":[317,0,328,0]}
{"wire":[334,0,332,0]}
{"wire":[335,0,332,1]}
{"wire":[336,0,332,2]}
{"wire":[332,0,326,0]}
{"wire":[333,0,334,0]}
{"wire":[333,1,335,0]}
{"wire":[333,2,336,0]}
{"wire":[337,0,332,0]}
{"wire":[337,1,333,0]}
{"wire":[338,0,332,1]}
{"wire":[338,1,333,0]}
{"wire":[339,0,332,2]}
{"wire":[339,1,333,0]}
{"wire":[340,0,337,0]}
{"wire":[340,1,338,0]}
{"wire":[340,2,339,0]}
{"wire":[80,0,73,0]}
{"wire":[80,1,81,0]}
{"wire":[80,2,319,0]}
{"wire":[323,0,325,0]}
{"wire":[323,1,82,0]}
{"wire":[91,0,90,0]}
{"wire":[341,0,340,0]}
{"wire":[83,0,80,0]}
{"wire":[83,1,323,0]}
{"wire":[92,0,91,0]}
{"wire":[79,0,71,0]}
{"wire":[79,2,83,0]}
{"wire":[93,0,92,0]}
{"wire":[94,0,93,0]}
{"wire":[84,0,79,0]}
{"wire":[343,0,342,0]}
{"wire":[70,0,69,0]}
{"wire":[86,0,84,2]}
{"wire":[86,1,84,0]}
{"wire":[85,0,84,2]}
{"wire":[85,1,84,1]}
{"wire":[95,0,93,0]}
{"wire":[95,1,94,0]}
{"wire":[74,0,72,0]}
{"wire":[74,1,73,0]}
{"wire":[74,2,319,0]}
{"wire":[198,0,197,0]}
{"wire":[198,1,88,0]}
{"wire":[322,0,320,0]}
{"wire":[322,1,76,0]}
{"wire":[331,0,143,2]}
{"wire":[331,1,143,3]}
{"wire":[331,2,143,1]}
{"wire":[329,0,143,1]}
{"wire":[329,1,143,2]}
{"wire":[329,2,143,3]}
{"wire":[330,0,143,2]}
{"wire":[330,1,143,1]}
{"wire":[330,2,143,3]}
{"wire":[344,0,343,0]}
{"wire":[71,0,70,0]}
{"wire":[367,0,227,0]}
{"wire":[87,0,84,0]}
{"wire":[87,1,84,1]}
{"wire":[104,0,95,0]}
{"wire":[97,0,85,0]}
{"wire":[97,2,198,0]}
{"wire":[96,0,86,0]}
{"wire":[96,2,198,0]}
{"wire":[77,0,322,0]}
{"wire":[77,1,74,0]}
{"wire":[347,0,331,0]}
{"wire":[347,1,344,2]}
{"wire":[346,0,330,0]}
{"wire":[346,1,344,1]}
{"wire":[345,0,329,0]}
{"wire":[345,1,344,0]}
{"wire":[228,0,367,0]}
{"wire":[107,0,97,0]}
{"wire":[107,1,104,0]}
{"wire":[108,0,96,0]}
{"wire":[108,1,104,1]}
{"wire":[89,0,87,0]}
{"wire":[89,2,198,0]}
{"wire":[78,0,71,0]}
{"wire":[78,2,77,0]}
{"wire":[348,0,345,0]}
{"wire":[348,1,346,0]}
{"wire":[348,2,347,0]}
{"wire":[110,0,107,0]}
{"wire":[110,1,108,0]}
{"wire":[109,0,89,0]}
{"wire":[109,1,104,2]}
{"wire":[145,0,348,0]}
{"wire":[145,1,78,0]}
{"wire":[111,0,110,0]}
{"wire":[111,1,109,0]}
{"wire":[196,0,195,0]}
{"wire":[196,1,145,0]}
{"wire":[112,0,196,0]}
{"wire":[112,1,113,0]}
{"wire":[192,0,111,0]}
{"wire":[117,0,112,0]}
{"wire":[191,0,192,0]}
{"wire":[190,0,191,0]}
{"wire":[118,0,117,0]}
{"wire":[313,0,296,6]}
{"wire":[387,0,384,3]}
{"wire":[387,1,384,2]}
{"wire":[120,0,118,0]}
{"wire":[120,1,119,0]}
{"wire":[115,0,190,0]}
{"wire":[115,1,116,0]}
{"wire":[296,2,294,0]}
{"wire":[312,0,311,0]}
{"wire":[312,1,313,0]}
{"wire":[354,0,353,6]}
{"wire":[385,0,387,0]}
{"wire":[121,0,120,0]}
{"wire":[121,1,115,0]}
{"wire":[239,0,184,0]}
{"wire":[239,1,238,0]}
{"wire":[306,0,296,0]}
{"wire":[307,0,312,0]}
{"wire":[353,2,351,0]}
{"wire":[356,0,355,0]}
{"wire":[356,1,354,0]}
{"wire":[371,0,368,0]}
{"wire":[389,0,385,0]}
{"wire":[122,0,121,0]}
{"wire":[297,0,239,0]}
{"wire":[297,1,306,0]}
{"wire":[314,0,307,0]}
{"wire":[366,0,304,0]}
{"wire":[366,1,349,0]}
{"wire":[357,0,353,0]}
{"wire":[358,0,356,0]}
{"wire":[390,0,371,0]}
{"wire":[390,1,389,0]}
{"wire":[181,0,122,0]}
{"wire":[232,0,125,0]}
{"wire":[232,1,229,0]}
{"wire":[298,0,297,0]}
{"wire":[298,1,314,0]}
{"wire":[359,0,366,0]}
{"wire":[359,1,357,0]}
{"wire":[360,0,358,0]}
{"wire":[392,0,390,0]}
{"wire":[392,1,385,0]}
{"wire":[123,0,181,0]}
{"wire":[123,1,124,0]}
{"wire":[123,2,232,0]}
{"wire":[301,0,298,0]}
{"wire":[361,0,359,0]}
{"wire":[361,1,360,0]}
{"wire":[401,0,392,0]}
{"wire":[401,1,36,0]}
{"wire":[184,0,123,0]}
{"wire":[184,1,183,0]}
{"wire":[303,0,301,0]}
{"wire":[303,1,299,0]}
{"wire":[362,0,361,0]}
{"wire":[161,0,13,0]}
{"wire":[161,1,16,0]}
{"wire":[161,2,160,0]}
{"wire":[399,0,401,0]}
{"wire":[304,0,184,0]}
{"wire":[304,1,303,0]}
{"wire":[364,0,362,0]}
{"wire":[364,1,363,0]}
{"wire":[163,2,161,0]}
{"wire":[402,1,399,0]}
{"wire":[365,0,304,0]}
{"wire":[365,1,364,0]}
{"wire":[162,0,163,6]}
{"wire":[43,0,402,0]}
{"wire":[305,0,365,0]}
{"wire":[29,0,28,0]}
{"wire":[28,0,21,0]}
{"wire":[28,1,161,0]}
{"wire":[55,0,54,0]}
{"wire":[55,1,56,0]}
{"wire":[57,0,55,0]}
{"wire":[57,1,58,0]}
{"wire":[136,0,57,0]}
{"wire":[136,1,137,0]}
{"wire":[30,0,29,0]}
{"wire":[147,0,146,0]}
{"wire":[147,1,21,0]}
{"wire":[157,0,154,0]}
{"wire":[157,1,155,0]}
{"wire":[157,2,156,0]}
{"wire":[158,0,157,0]}
{"wire":[127,0,122,0]}
{"wire":[128,0,127,0]}
{"wire":[151,0,118,0]}
{"wire":[152,0,190,0]}
{"wire":[114,0,190,0]}
{"wire":[67,1,62,0]}
{"wire":[67,2,66,0]}
{"wire":[61,0,60,0]}
{"wire":[61,1,67,0]}
{"wire":[141,0,61,0]}
{"wire":[141,1,149,0]}
{"wire":[149,1,140,0]}
{"wire":[149,2,150,0]}
{"wire":[21,1,19,0]}
{"wire":[21,2,20,0]}
{"wire":[165,0,133,0]}
{"wire":[165,1,164,0]}
{"wire":[2,0,32,0]}
{"wire":[2,3,131,0]}
{"wire":[2,4,131,0]}
{"wire":[2,2,141,0]}
{"wire":[2,6,165,0]}
{"wire":[2,7,132,0]}
{"wire":[2,8,53,0]}
ASEEND*/
//CHKSM=3EDEB0E5C9C2C7987C46079E3F19C998AB63A061