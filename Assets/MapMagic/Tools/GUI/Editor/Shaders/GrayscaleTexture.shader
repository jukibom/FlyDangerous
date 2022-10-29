Shader "Hidden/DPLayout/GrayscaleTexture"
///Displays the grayscale R8 texture using the given colors
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {} 
		_Black("Black", Vector) = (0, 0, 0, 0)
		_White("White", Vector) = (1, 1, 1, 1)
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
			half4 _Black;
			half4 _White;

			fixed4 frag(v2f i) : SV_Target
			{
				fixed p = tex2Dlod(_MainTex, float4(i.uv,0,0)).r;
				fixed4 col = _White*p + _Black*(1-p);
				
				//clipping
				col.a *= step(0, i.worldPosition.y);
				clip(col.a - 0.001);

				return col;
			}
			ENDCG
		}
	}
}

