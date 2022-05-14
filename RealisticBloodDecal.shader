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
                float inverseSqrRadius;
            };

            struct Cell
            {
                uint startBloodDropIndex;
                uint count;
            };

            StructuredBuffer<BloodDrop> _BloodDrops;
            StructuredBuffer<Cell> _Cells;

            float3 _BoundsDimensions;
            uint _BoundsVolume;
            float4x4 _BoundsMatrix;

            inline uint getCellIndex(float3 coord)
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
                float3 boundsPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;

                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                v.vertex = float4(v.uv.xy, 0.0, 1.0);
                o.vertex = mul(UNITY_MATRIX_P, v.vertex);

                o.boundsPos = mul(_BoundsMatrix, float4(o.worldPos, 1)).xyz;
                
                return o;
            }

            float sqrDistanceFromLine(float3 p, float3 a, float3 b)
            {
                float3 pa = p - a, ba = b - a;
                float h = saturate(dot(pa, ba) / dot(ba, ba));
                float3 diff = pa - ba * h;
                
                return dot(diff, diff);
            }

            float frag(v2f o) : SV_Target
            {
                float output = 0;

                float3 coord = floor(o.boundsPos * _BoundsDimensions);
                
                uint cellIndex = getCellIndex(coord);

                if (cellIndex < 0 || cellIndex > _BoundsVolume)
                {
                    discard;
                }

                Cell cell = _Cells[cellIndex];

                if (cell.count == 0)
                {
                    discard;
                }
                
                for (uint i = 0; i < cell.count; i++)
                {
                    BloodDrop bloodDrop = _BloodDrops[cell.startBloodDropIndex + i];

                    float sqrDist = sqrDistanceFromLine(o.worldPos, bloodDrop.startPos, bloodDrop.endPos);

                    float closeness = 1 - saturate(sqrDist * bloodDrop.inverseSqrRadius);

                    output += closeness;
                }

                return output;
            }
            ENDCG
        }
    }
}