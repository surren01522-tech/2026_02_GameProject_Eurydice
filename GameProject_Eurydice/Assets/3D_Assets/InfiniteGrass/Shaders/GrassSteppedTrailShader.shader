Shader "InfiniteGrass/Modifiers/GrassSteppedTrailShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Strength ("Strength", Range(0, 1)) = 0.85
        [Toggle]_UseHeightCheck("Use Height Check", Float) = 1
    }
    SubShader
    {
        Tags {
            "Queue" = "Transparent" 
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent" 
            "LightMode" = "GrassSlope"
        }

        ZWrite Off
        Cull Off

        Pass
        {
            ColorMask RGA
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 tangent : TANGENT;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 tangent : TANGENT;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Strength;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.tangent = v.tangent;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                float2 trailForwardXZ = -i.tangent.xz;
                float lenSq = dot(trailForwardXZ, trailForwardXZ);
                float2 dirXZ = lenSq > 1e-6 ? trailForwardXZ * rsqrt(lenSq) : float2(0, 0);

                float2 encodeToSlope = dirXZ * 0.5 + 0.5;
                float strength = col.r * col.a * i.color.a * _Strength;

                return float4(encodeToSlope, 0, strength);
            }
            ENDCG
        }

        Pass
        {
            Name "SlopeHeight"
            ColorMask B
            Blend One Zero

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float normY : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Strength;
            float _UseHeightCheck;
            float2 _SlopeBoundsYMinMax;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                float worldY = mul(unity_ObjectToWorld, v.vertex).y;
                float t = saturate((worldY - _SlopeBoundsYMinMax.x) / max(_SlopeBoundsYMinMax.y - _SlopeBoundsYMinMax.x, 1e-5));
                o.normY = lerp(0.01, 1.0, t);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                if (_UseHeightCheck < 0.5)
                    discard;
                fixed4 col = tex2D(_MainTex, i.uv);
                float strength = col.r * col.a * i.color.a * _Strength;
                clip(strength - 0.005);
                return float4(0, 0, i.normY, 0);
            }
            ENDCG
        }
    }
}
