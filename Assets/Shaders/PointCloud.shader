Shader "Bibcam/PointCloud"
{
	Properties
	{
		[PowerSlider(5)]
		_ParticleSize ("Size", Range(0,1)) = .1
	}
	SubShader
	{

		Pass
		{

			Tags
			{
				"LightMode"="ForwardBase"
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
			#pragma target 4.5

			#include "UnityCG.cginc"
			// #include "AutoLight.cginc"

			float _ParticleSize;

			#if SHADER_TARGET >= 45
			StructuredBuffer<float3> _Points;
			StructuredBuffer<float3> _Colors;
			#endif

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 color : COLOR;
			};

			v2f vert(appdata_full v, uint instanceID : SV_InstanceID)
			{
				#if SHADER_TARGET >= 45
				float3 data = _Points[instanceID];
				float3 color = _Colors[instanceID];
				#else
                float3 data = 0;
				float3 color = 1;
				#endif

				float3 pos = v.vertex.xyz;

				// // billboard mesh towards camera
				// float3 vpos = mul((float3x3)unity_ObjectToWorld, pos);
				// float4 worldCoord = float4(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23, 1);
				// float4 viewPos = mul(UNITY_MATRIX_V, worldCoord) + float4(vpos, 0);
				// pos = mul(UNITY_MATRIX_P, viewPos).xyz;
				
				float3 worldPosition = data.xyz + pos * _ParticleSize * .1;
				v2f o;
				o.pos = mul(UNITY_MATRIX_VP, float4(worldPosition, 1.0f));
				o.color = color;
				// TRANSFER_SHADOW(o)
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 albedo = float4(i.color,1);;
				return albedo;
			}
			ENDCG
		}
	}
}