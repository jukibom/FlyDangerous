Shader "Hidden/DPLayout/Curve" 
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {} 
		_Backcolor("Backcolor", Vector) = (0, 0, 0, 0)
		_Forecolor("Forecolor", Vector) = (1, 1, 1, 1) 
		_CurveRect("CurveRect", Vector) = (0, 0, 100, 100) //leaving the name _Rect will lead to "Property _Rect already exists in the property sheet with a different type: 3" error
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
				float4 worldPosition : TEXCOORD1;
				float4 vertex : SV_POSITION;
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
			uniform float _Curve[256];
			half4 _Backcolor;
			half4 _Forecolor;
			half4 _CurveRect;

			inline bool isLine (float2 uv) //uv in pixel coordinates
			{
				if (uv.x+1.001 > _CurveRect.z)  return false;
				if (uv.x-1 < 0) return false;

				fixed val = _Curve[(int)(uv.x/_CurveRect.z * 256)] * _CurveRect.w;
				fixed prevVal = _Curve[(int)((uv.x-1)/_CurveRect.z * 256)] * _CurveRect.w;
				fixed nextVal = _Curve[(int)((uv.x+1)/_CurveRect.z * 256)] * _CurveRect.w;

				bool filled = val > uv.y;
				bool upFilled = val > uv.y + 1;
				bool downFilled = val > uv.y - 1;
				bool prevFilled = prevVal > uv.y;
				bool nextFilled = nextVal > uv.y;

				return  (filled && (!upFilled || !prevFilled || !nextFilled)) ||
						(uv.y > _CurveRect.w-1  &&  val > _CurveRect.w-1) ||
						(uv.y < 1  &&  val < 1);
			}


			fixed4 frag(v2f i) : SV_Target
			{
				fixed val = _Curve[ (int)(i.uv.x*256) ];
				fixed prevVal = _Curve[(int)(i.uv.x*256 - 1*(256.0/_CurveRect.z))];
				fixed nextVal = _Curve[(int)(i.uv.x*256 + 1*(256.0/_CurveRect.z))];
				
				bool filled = val > i.uv.y;
				bool upFilled = val > i.uv.y + 1/_CurveRect.w;
				bool downFilled = val > i.uv.y - 1/_CurveRect.w;
				bool prevFilled = prevVal > i.uv.y;
				bool nextFilled = nextVal > i.uv.y;
				bool prevUpFilled = prevVal > i.uv.y + 1/_CurveRect.w;

				float2 pixelUV = float2(i.uv.x*_CurveRect.z, i.uv.y*_CurveRect.w);

				half4 col = _Forecolor;
				col.a = 0;
				//if (isLine(pixelUV)) col.a = 1;

				if (isLine(pixelUV)) col.a += 0.25;
				if (isLine( float2(pixelUV.x+0.5, pixelUV.y) )) col.a += 0.125;
				if (isLine( float2(pixelUV.x, pixelUV.y+0.5) )) col.a += 0.125;
				if (isLine( float2(pixelUV.x+0.5, pixelUV.y+0.5) )) col.a += 0.125;

				if (isLine( float2(pixelUV.x-0.5, pixelUV.y) )) col.a += 0.125;
				if (isLine( float2(pixelUV.x, pixelUV.y-0.5) )) col.a += 0.125;
				if (isLine( float2(pixelUV.x-0.5, pixelUV.y-0.5) )) col.a += 0.125;

				//clipping
				col.a *= step(0, i.worldPosition.y);
				clip(col.a - 0.001);

				return col;
			}
			ENDCG
		}
	}
}

