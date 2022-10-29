Shader "Hidden/DPLayout/GridOutdated"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {} //not used

		_Color("Color", Vector) = (0.56, 0.56, 0.56, 0.56)
		_Background("Background", Vector) = (0, 0, 0, 0)

		_CellsNumX("Cells Num X", Int) = 4
		_CellsNumY("Cells Num Y", Int) = 4

		_Rect("Rect", Vector) = (0,0,100,100)
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

			//clipping matrix (thanks Thomas: http://vertx.xyz/unity-better-texture-previews/)
			sampler2D _GUIClipTexture;
			uniform float4x4 unity_GUIClipTextureMatrix;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;

				float2 clipUV : TEXCOORD1; //for clipping
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;

				//clipping
				float3 eyePos = UnityObjectToViewPos(v.vertex);
				o.clipUV = mul(unity_GUIClipTextureMatrix, float4(eyePos.xy, 0, 1.0));

				return o;
			}
			
			half4 _Color;
			half4 _Background;
			int _CellsNumX;
			int _CellsNumY;
			float4 _Rect;

			fixed4 frag (v2f i) : SV_Target
			{
				float2 rectSize = _Rect.zw;  //can't use texel size because there's no texture
				float2 cellSize = (rectSize-1) / float2(_CellsNumX, _CellsNumY);

				int2 pixelPos = (int2)(i.uv * rectSize);

				int2 gridLineNum = (int2)(pixelPos / cellSize);
				int2 gridLinePos = (int2)(gridLineNum * cellSize);
				int2 nextGridLinePos = (int2)((gridLineNum+1) * cellSize);


				float opacity = 0;
				if (pixelPos.x == gridLinePos.x  ||  pixelPos.x == nextGridLinePos.x  ||  pixelPos.y == gridLinePos.y  ||  pixelPos.y == nextGridLinePos.y)
					opacity = 0.5;

				//borders
				if (pixelPos.x == 0 || pixelPos.x == (int)rectSize.x-1 || pixelPos.y == 0 || pixelPos.y == (int)rectSize.y - 1)
					opacity = 1;


				fixed4 col = _Background*(1-_Color.a*opacity) + _Color*_Color.a*opacity;
				col.a *= tex2D(_GUIClipTexture, i.clipUV).a; //clipping

				return col;
			}
			ENDCG
		}
	}
}

