﻿Shader "HardwareOcclusion"
{
    SubShader
    {
        Cull Off
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            CGPROGRAM
            #pragma vertex VSMain
            #pragma fragment PSMain
            #pragma target 5.0

            RWStructuredBuffer<uint> _VisibleClusters : register(u1);
            //StructuredBuffer<float4> _Reader;
            int _Debug;

            struct v2f
            {
                float4 vertex : SV_POSITION;
                uint clusterId : TEXCOORD1;
            };

            v2f VSMain (float3 vertex : POSITION, uint id : SV_VertexID)
            {
                v2f result;
                result.vertex = mul (UNITY_MATRIX_VP, float4(vertex, 1.0));
                result.clusterId = id / 8;

                return result;
            }

            [earlydepthstencil]
            float4 PSMain (v2f i) : SV_TARGET
            {
                _VisibleClusters[i.clusterId] = 1;
                return float4(0.0, 0.0, 1.0, 0.1 * _Debug);
            }
            ENDCG
        }
    }
}