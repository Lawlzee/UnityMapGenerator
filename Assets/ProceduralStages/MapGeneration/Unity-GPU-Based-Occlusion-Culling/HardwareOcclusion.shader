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

			RWStructuredBuffer<float4> _Writer : register(u1);
			StructuredBuffer<float4> _Reader;
			int _Debug;

			float4 VSMain (float4 vertex : POSITION, out uint instance : TEXCOORD0, uint id : SV_VertexID) : SV_POSITION
			{
				instance = _Reader[id].w;
				return mul (UNITY_MATRIX_VP, float4(_Reader[id].xyz, 1.0));
			}

			[earlydepthstencil]
			float4 PSMain (float4 vertex : SV_POSITION, uint instance : TEXCOORD0) : SV_TARGET
			{
				_Writer[instance] = vertex;
				return float4(0.0, 0.0, 1.0, 0.2 * _Debug);
			}
			ENDCG
		}
	}
}