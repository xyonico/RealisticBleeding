﻿Shader "RealisticBleeding/Decal"
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
                float4 startPosAndRadius;
                float4 endPos;
            };

            struct Cell
            {
                float startBloodDropIndex;
                float count;
            };

            StructuredBuffer<BloodDrop> _BloodDrops;
            StructuredBuffer<Cell> _Cells;

            float3 _BoundsMinPosition;
            float3 _BoundsWorldToLocalSize;
            float3 _BoundsDimensions;
            float _BoundsVolume;

            inline float getCellIndex(float3 coord)
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

                o.boundsPos = (o.worldPos - _BoundsMinPosition) * _BoundsWorldToLocalSize;
                
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

                float3 coord = floor(o.boundsPos);
                
                float cellIndex = getCellIndex(coord);

                if (cellIndex < 0 || cellIndex > _BoundsVolume)
                {
                    discard;
                }

                Cell cell = _Cells[cellIndex];

                if (cell.count == 0)
                {
                    discard;
                }

                float count = min(cell.count, 8);
                
                for (float i = 0; i < count; i++)
                {
                    BloodDrop bloodDrop = _BloodDrops[cell.startBloodDropIndex + i];

                    float sqrDist = sqrDistanceFromLine(o.worldPos, bloodDrop.startPosAndRadius.xyz, bloodDrop.endPos);

                    float closeness = 1 - saturate(sqrDist * bloodDrop.startPosAndRadius.w);

                    output += closeness;
                }

                return output;
            }
            ENDCG
        }
    }
}