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

            struct Cell
            {
                int startBloodDropIndex;
                int count;
            };

            StructuredBuffer<BloodDrop> _BloodDrops;
            StructuredBuffer<Cell> _Cells;

            float3 _BoundsDimensions;
            float4x4 _BoundsMatrix;
            float _Multiplier;

            inline int getCellIndex(int3 coord)
            {
                return coord.z * _BoundsDimensions.x * _BoundsDimensions.y + coord.y * _BoundsDimensions.x + coord.x;
            }

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

                float3 boundsPos = mul(_BoundsMatrix, float4(o.worldPos, 1)).xyz;
                
                int3 coord = floor(boundsPos * _BoundsDimensions);
                
                int cellIndex = getCellIndex(coord);

                Cell cell = _Cells[cellIndex];
                
                for (int i = cell.startBloodDropIndex; i < cell.count; i++)
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