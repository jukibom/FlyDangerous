Shader "Hidden/DPLayout/Histogram"
///Displays the histogram using given array (texture not used)
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {} 
		_Backcolor("Backcolor", Vector) = (0, 0, 0, 0)
		_Forecolor("Forecolor", Vector) = (1, 1, 1, 1)
		_HistogramLength("Histogram Length", Int) = 256 
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
			uniform float _Histogram[256];
			int _HistogramLength;
			half4 _Backcolor;
			half4 _Forecolor;

			fixed4 frag(v2f i) : SV_Target
			{
				//fixed val = tex2Dlod(_MainTex, float4(i.uv.x,0,0,0)).r;
				fixed val = _Histogram[ (int)(i.uv.x*_HistogramLength) ];
				
				fixed4 col;
				if (val > i.uv.y) col = _Forecolor;
				else col = _Backcolor;

				//clipping
				col.a *= step(0, i.worldPosition.y);
				clip(col.a - 0.001);

				return col;
			}
			ENDCG
		}
	}
}

