Shader "MapMagic/ObjectPreview" 
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_BackColor("Background Color", Color) = (0,0,0,1)
		_Heightmap("Heightmap", 2D) = "black" {}
		_Size("Size", Float) = 5
		_Flip("Flip", Float) = 0
		_ClipRect("ClipRect", Vector) = (0,0,0,0)
		_Offset("Offset", Float) = 0.02
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
		ZTest [unity_GUIZTestMode]
		//Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			sampler2D _Heightmap;
			float4 _Heightmap_ST;
			fixed4 _Color;
			fixed4 _BackColor;
			float _Size;
			int _Flip;
			float4 _ClipRect;
			float _Offset;

			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float3 normal	: NORMAL;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color : COLOR;
			};

			v2f vert(appdata_t v)
			{
				v2f OUT;

				//applying heightmap
				float height = tex2Dlod(_Heightmap, float4(v.vertex.xz,0,0)).r;
				v.vertex.y += height;

				OUT.vertex = UnityObjectToClipPos(v.vertex);

				OUT.vertex.xyz /= OUT.vertex.w;
				float2 hpc = floor(_ScreenParams.xy)/2;
				OUT.vertex.xy *= hpc;

				OUT.vertex.xy = floor(OUT.vertex.xy + 0.5);
				//OUT.vertex.xy -= 0.5; //making sharp arrow
				if (_Flip > 0.5) OUT.vertex.xy -= v.normal.xy * _Size;
				else OUT.vertex.xy += v.normal.xy * _Size;

				OUT.vertex.xy /= hpc;
				OUT.vertex.xyz *= OUT.vertex.w;

				OUT.vertex.z += _Offset / _ZBufferParams.x * unity_OrthoParams.w; //orthographic offset
				OUT.vertex.z += _Offset * (1 - unity_OrthoParams.w);  //perspective offset

				OUT.color = _Color * (1- v.normal.z) + _BackColor * v.normal.z;

				return OUT;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				half4 color = IN.color;

				if (_ClipRect.z>0.5 && _ClipRect.w>0.5)
					color.a = UnityGet2DClipping(IN.vertex.xy, _ClipRect);
				clip(color.a - 0.001);

				return color;
			}
		ENDCG
		}
	} 
}