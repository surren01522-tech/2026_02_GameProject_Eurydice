Shader "InfiniteGrass/Modifiers/GrassSlopeShader"
{
    Properties
    {
        [MainTexture]_MainTex ("Texture", 2D) = "white" {}
        [MainColor]_BaseColor("BaseColor", Color) = (1,1,1,1)
        [Toggle]_UseHeightCheck("Use Height Check", Float) = 0
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
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                half4 color     : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _BaseColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv) * _BaseColor * i.color;
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
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                half4 color : COLOR;
                float normY : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _BaseColor;
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
                fixed4 col = tex2D(_MainTex, i.uv) * _BaseColor * i.color;
                clip(col.a - 0.005);
                return float4(0, 0, i.normY, 0);
            }
            ENDCG
        }
    }
}
