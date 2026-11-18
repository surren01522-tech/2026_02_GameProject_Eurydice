Shader "InfiniteGrass/GrassHeightMapShader"
{
    Properties
    {
    }
    SubShader
    {
        Tags { 
            "RenderType"="Opaque"
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                half4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 color : TEXCOORD0;
            };

            float2 _BoundsYMinMax;

            float Remap(float In, float2 InMinMax, float2 OutMinMax)
            {
                return OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / max(InMinMax.y - InMinMax.x, 1e-5);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                float rChannel = saturate(Remap(worldPos.y, _BoundsYMinMax, float2(0, 1)));
                float gChannel = v.color.r;

                o.color = float2(rChannel, gChannel);

                return o;
            }

            float2 frag (v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}
