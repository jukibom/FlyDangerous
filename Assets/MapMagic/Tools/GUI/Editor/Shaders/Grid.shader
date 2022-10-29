Shader "Hidden/DPLayout/Grid"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {} //not used

		_Color("Color", Vector) = (0.56, 0.56, 0.56, 0.56)
		_Background("Background", Vector) = (0, 0, 0, 0)

		_CellOffsetX("Cells Offset X", Float) = 0
		_CellOffsetY("Cells Offset Y", Float) = 0
		_CellSizeX("Cells Size X", Float) = 16
		_CellSizeY("Cells Size Y", Float) = 16

		_LineOpacity("Line Opacity", Float) = 0.5
		_BordersOpacity("Borders Opacity", Float) = 1

		_ViewRect("ViewRect", Vector) = (0,0,100,100) //first two values are not used, actually, but just in case we want to know it's position relative to window
			//leaving the name _Rect will lead to "Property _Rect already exists in the property sheet with a different type: 3" error 
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
			
			half4 _Color;
			half4 _Background;
			float _CellOffsetX;
			float _CellOffsetY;
			float _CellSizeX;
			float _CellSizeY;
			float4 _ViewRect;
			half _LineOpacity;
			half _BordersOpacity;

			fixed4 frag (v2f i) : SV_Target
			{
				float2 rectSize = _ViewRect.zw;  //can't use texel size because there's no texture
				float2 rectOffset = float2(_CellOffsetX, _CellOffsetY);
				float2 cellSize = float2(_CellSizeX, _CellSizeY);

				int2 pixelPos = (int2)(i.uv * rectSize) + rectOffset;

				int2 gridLineNum = (int2)(pixelPos / cellSize);
				int2 nextLineNum = (int2)((pixelPos+1) / cellSize);

				float opacity = 0;
				//if (pixelPos.x == gridLinePos.x  ||  pixelPos.x == nextGridLinePos.x  ||  pixelPos.y == gridLinePos.y  ||  pixelPos.y == nextGridLinePos.y)
				if (gridLineNum.x != nextLineNum.x || gridLineNum.y != nextLineNum.y)
					opacity = _LineOpacity;

				//borders
				if (pixelPos.x == 0 || pixelPos.x == (int)rectSize.x-1 || pixelPos.y == 0 || pixelPos.y == (int)rectSize.y - 1)
					opacity += _BordersOpacity;

				fixed4 col = _Background*(1-_Color.a*opacity) + _Color*_Color.a*opacity;
				
				//clipping
				col.a *= step(0, i.worldPosition.y);
				clip(col.a - 0.001);

				return col;
				//return (pixelPos.x % 2.0)/2.0;  //testing pixelpos
			}
			ENDCG
		}
	}
}

