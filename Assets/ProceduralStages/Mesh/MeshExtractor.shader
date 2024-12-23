Shader "Custom/MeshExtractor"
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
            //#pragma require geometry

            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #pragma target 5.0

            RWStructuredBuffer<float3> _VertexBuffer: register(u1);
            RWStructuredBuffer<float3> _NormalBuffer: register(u2);
            RWStructuredBuffer<int> _TriangleBuffer: register(u3);

            struct appdata_t
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                uint id : SV_VertexID;
            };

            struct v2f
            {
                float4 pos : POSITION;
                uint id: TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                _VertexBuffer[v.id] = v.vertex.xyz;
                _NormalBuffer[v.id] = v.normal;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.id = v.id;

                return o;
            }

            [maxvertexcount(3)] 
            void geom(triangle v2f input[3], inout TriangleStream<v2f> triStream, uint primitiveID : SV_PrimitiveID)
            {
                uint baseIndex = primitiveID;
            
                for (int i = 0; i < 3; ++i)
                {
                    _TriangleBuffer[baseIndex * 3 + i] = (int)input[i].id;

                    triStream.Append(input[i]);
                }
            
                triStream.RestartStrip();
            }

            half4 frag(v2f i) : SV_Target
            {
                return half4(1, 0, 0, 1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
