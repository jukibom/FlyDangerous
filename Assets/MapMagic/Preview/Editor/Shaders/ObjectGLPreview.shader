Shader "MapMagic/ObjectGLPreview" 
{
	Properties
	{
		_Color("Tint", Color) = (1,1,1,1)
		_ClipRect("Clip Rect", Vector) = (0,0,100,100)
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

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			fixed4 _Color;
			float4 _ClipRect;

			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color : COLOR;
				float4 screenPosition : TEXCOORD1;
			};

			v2f vert(appdata_t v)
			{
				v2f OUT;
				//v.vertex = (int4)(v.vertex);
				//v.vertex -= 0.45;
				OUT.screenPosition = v.vertex;
				OUT.vertex = UnityObjectToClipPos(OUT.screenPosition);
				OUT.color = v.color * _Color;
				return OUT;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				half4 color = IN.color;

				color.a = UnityGet2DClipping(IN.screenPosition.xy, _ClipRect);
				color.a *= step(0, IN.screenPosition.y);

				clip(color.a - 0.001);

				return color;
			}
		ENDCG
		}
	} 
}