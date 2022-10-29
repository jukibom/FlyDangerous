Shader "Hidden/DPLayout/LatticeOutdated"
{
	Properties
	{
		[PerRendererData] _MainTex ("Texture", 2D) = "white" {} //not used

		_Color("Color", Vector) = (0.56, 0.56, 0.56, 0.56)
		_Background("Background", Vector) = (0, 0, 0, 0)

		_CellSize("Cell Size", Float) = 32
		_OffsetX("Offset X", Float) = 0 //grid offset, not rect one
		_OffsetY("Offset Y", Float) = 0

		_Rect("Rect", Vector) = (0, 0, 100, 100)
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

			float _CellSize;
			float _OffsetX;
			float _OffsetY;

			float4 _Rect;

			fixed4 frag (v2f i) : SV_Target
			{
				float2 rectSize = _Rect.zw;
				float2 cellSize = float2(_CellSize, _CellSize);
				float2 offset = float2(_OffsetX, _OffsetY);

				float2 pixelPos = i.uv * rectSize + offset;
				int2 cellNum = (int2)floor(pixelPos / cellSize);
				int2 nextCellNum = (int2)floor((pixelPos+1) / cellSize);

				fixed4 col = _Background;
				if (cellNum.x != nextCellNum.x  ||  cellNum.y != nextCellNum.y) 
					col = _Background*(1-_Color.a) + _Color*_Color.a;

				col.a *= tex2D(_GUIClipTexture, i.clipUV).a; //clipping

				return col;
			}
			ENDCG
		}
	}
}

