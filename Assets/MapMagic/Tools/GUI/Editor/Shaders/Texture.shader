Shader "Hidden/DPLayout/Texture"
{
	Properties
	{
		[PerRendererData] _MainTex ("Texture", 2D) = "white" {}
		_Mode ("Mode", Int) = 0

		[Gamma] _GammaGray("Gray", Range(0.0, 1.0)) = 0.5

		//_ColorMask("Color Mask", Float) = 15
		//[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
	}
	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
			"CanUseSpriteAtlas" = "True"
		}

		Stencil
		{
			Ref[_Stencil]
			Comp[_StencilComp]
			Pass[_StencilOp]
			ReadMask[_StencilReadMask]
			WriteMask[_StencilWriteMask]
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest[unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		//ColorMask[_ColorMask]

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			//#include "UnityUI.cginc"

			//#pragma multi_compile __ UNITY_UI_CLIP_RECT
			//#pragma multi_compile __ UNITY_UI_ALPHACLIP

			////clipping matrix
			////sampler2D _GUIClipTexture;
			////uniform float4x4 unity_GUIClipTextureMatrix;

			struct appdata
			{
				float4 vertex   : POSITION;
				float2 texcoord : TEXCOORD0;
				//float4 color    : COLOR;
				//UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord  : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
				////float2 clipUV : TEXCOORD1; //clipping matrix
				//UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;
			int _Mode;
			float _GammaGray;
			float4 _ClipRect;

			v2f vert (appdata v)
			{
				v2f o;
				//UNITY_SETUP_INSTANCE_ID(v);
				//UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.worldPosition = v.vertex;
				o.vertex = UnityObjectToClipPos(o.worldPosition);
				o.texcoord = v.texcoord; //TRANSFORM_TEX(v.texcoord, _MainTex);
				o.color = (0,0,0,0);

				////clipping matrix
				////float3 eyePos = UnityObjectToViewPos(v.vertex);
				////o.clipUV = mul(unity_GUIClipTextureMatrix, float4(eyePos.xy, 0, 1.0));

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2Dlod(_MainTex, float4(i.texcoord,0,0));

				if (_Mode == 0) 
				{
					//if (_GammaGray < 0.45) col.rgb = pow(col.rgb, 0.454545);
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

				////clipping matrix
				////col.a *= tex2D(_GUIClipTexture, i.clipUV).a;

				return col;
			}
			ENDCG
		}
	}
}

