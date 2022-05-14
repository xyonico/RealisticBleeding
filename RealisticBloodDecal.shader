Shader "RealisticBleeding/Decal"
{
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        Cull Off
        ZTest Always
        ZWrite Off

        Blend One One
        ColorMask R

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct BloodDrop
            {
                float3 startPos;
                float3 endPos;
                float inverseRadius;
            };

            StructuredBuffer<BloodDrop> _BloodDrops;
            int _BloodDropCount;
            float _Multiplier;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;

                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                v.vertex = float4(v.uv.xy, 0.0, 1.0);
                o.vertex = mul(UNITY_MATRIX_P, v.vertex);

                return o;
            }

            float distanceFromLine(float3 p, float3 a, float3 b)
            {
                float3 pa = p - a, ba = b - a;
                float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
                return length(pa - ba * h);
            }

            float frag(v2f o) : SV_Target
            {
                float output = 0;

                for (int i = 0; i < _BloodDropCount; i++)
                {
                    BloodDrop bloodDrop = _BloodDrops[i];

                    float dist = distanceFromLine(o.worldPos, bloodDrop.startPos, bloodDrop.endPos);

                    float closeness = 1 - saturate(dist * bloodDrop.inverseRadius);

                    output += _Multiplier * closeness;
                }

                return output;
            }
            ENDCG
        }
    }
}