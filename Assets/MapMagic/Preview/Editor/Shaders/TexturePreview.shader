Shader "Hidden/MapMagic/TexturePreview"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		[Gamma] _GammaGray("Gray", Range(0.0, 1.0)) = 0.5

		_Colorize("Colorize", Float) = 1
		_Relief("Relief", Float) = 1
		_MinValue("Min Value", Float) = 0
		_MaxValue("Max Value", Float) = 1
		_NormHeight("Normals Height", Float) = 10
		_Margins("Margins", Int) = 16
		_Extrude("Extrude", Float) = 0
	}
	SubShader
	{
		// No culling or depth
		//Cull Off ZWrite Off ZTest Always

		ZWrite On
		ZTest Off
		Cull Off

		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			#include "Preview.cginc"

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			float _GammaGray;
			float _Colorize;
			float _Relief;
			float _MinValue;
			float _MaxValue;
			float _NormHeight;
			int _Margins;
			float _Extrude;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float4 screenPos : TEXCOORD1;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.screenPos = v.vertex;
				v.vertex.y += tex2Dlod(_MainTex, float4(v.uv, 0, 0)) * _Extrude;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float2 uv = i.uv;
				uv *= (_MainTex_TexelSize.xy  * (_MainTex_TexelSize.zw - _Margins * 2));
				uv += 1.0 / (_MainTex_TexelSize.zw - _Margins * 2) * _Margins;

				half4 col = tex2Dlod(_MainTex, float4(uv,0,0));
				half val = col.r;

				//adjusting min/max
				val = (val-_MinValue) / (_MaxValue-_MinValue);

				//colorizing
				col.rgb = val*(1-_Colorize) + colorize(val,true)*_Colorize;

				//clamping min/max
				if (val < 0) col.rgb = half3(1,0,0);
				if (val > 1) col.rgb = half3(0,1,0);

				//relief
				col.rgb -= 0.15  * val * _Relief;
				col.rgb *= ( 1 + (2*relief(_MainTex, _MainTex_TexelSize.xy, uv)-1)*_NormHeight*_Relief );

				//clipping
				col.a = step(0, i.screenPos.y);
				clip(col.a - 0.001);

				return col;
			}
			ENDCG
		}
	}
}

