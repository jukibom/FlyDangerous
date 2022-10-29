Shader "Hidden/DPLayout/Background"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_TileX("Tile X", Float) = 0
		_TileY("Tile Y", Float) = 0
		_OffsetX ("Offset X", Float) = 0
		_OffsetY ("Offset Y", Float) = 0
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

			v2f vert (appdata v)
			{
				v2f o;
				o.worldPosition = v.vertex;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;
			float _TileX;
			float _TileY;
			float _OffsetX;
			float _OffsetY;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2Dbias(_MainTex, float4((i.uv.x + _OffsetX) *_TileX, (i.uv.y + _OffsetY ) *_TileY,0,1));
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

