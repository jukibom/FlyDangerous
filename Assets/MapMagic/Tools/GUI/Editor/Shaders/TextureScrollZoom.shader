Shader "Hidden/DPLayout/TextureScrollZoom"
{
	Properties
	{
		_MainTex ("Temp", 2D) = "white" {}
		_Mode ("Mode", Int) = 0

		_OffsetX ("Offset X", Float) = 0
		_OffsetY ("Offset Y", Float) = 0
		_Scale ("Scale", Float) = 1

		_CellSizeX("Cells Size X", Float) = 16
		_CellSizeY("Cells Size Y", Float) = 16

		[Gamma] _GammaGray("Gray", Range(0.0, 1.0)) = 0.5 //use uniform float4 _MainTex_HDR instead?
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float4 worldPosition : TEXCOORD1;
			};
			
			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			int _Mode;
			float _GammaGray;

			float _OffsetX;
			float _OffsetY;
			float _Scale;

			float _CellSizeX;
			float _CellSizeY;

			v2f vert(appdata v)
			{
				v2f o;
				o.worldPosition = v.vertex;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float2 uv = (i.uv - float2(_OffsetX, _OffsetY)) / _Scale;
				uv /= float2(_MainTex_TexelSize.z, _MainTex_TexelSize.w);
				uv *= float2(_CellSizeX, _CellSizeY);

				fixed4 col = tex2Dlod(_MainTex, float4(uv,0,0));

				if (uv.x < 0 || uv.y < -1 || uv.x > 1 || uv.y > 0) col = fixed4(0.1,0.1,0.1,1);

				if (_Mode == 0) 
				{
					if (_GammaGray < 0.45) col.rgb = pow(col.rgb, 0.454545);
				}

				if (_Mode == 1) col = col.a;

				if (_Mode == 2) 
				{
					float3 norm = 0;
					norm.xy = col.wy * 2 - 1;
					norm.z = sqrt(1 - saturate(dot(norm.xy, norm.xy)));
					col = float4(norm/2 + 0.5, 1);
				}

				col.a = 1;

				//clipping
				col.a *= step(0, i.worldPosition.y);
				clip(col.a - 0.001);

				return col;
			}
			ENDCG
		}
	}
}

