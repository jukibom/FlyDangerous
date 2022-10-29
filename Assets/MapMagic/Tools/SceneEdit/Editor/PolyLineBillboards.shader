
Shader "Hidden/DPLayout/PolyLineBillboards"
//Draws a line using specially prepared mesh with 4x points
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		//[Linear] _Color("Color", Color) = (0.56, 0.56, 0.56, 0.56) //using vector to make independent from gamma/linear
		_Color("Color", Vector) = (0.56, 0.56, 0.56, 0.56)
		_Size("Size", Float) = 5
		_Offset("Offset", Float) = 0.001
		_NumPoints("Num Points", Float) = 1024
		_ZTest("Z Test", Int) = 0
		_ClipRect("ClipRect", Vector) = (0,0,0,0)
	}



SubShader
{
	Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
	LOD 100

	ZWrite On
	ZTest [_ZTest]
	Blend SrcAlpha OneMinusSrcAlpha
	//Blend One Zero
	Cull Off

		//Cull Off
		//Lighting Off
		//ZWrite Off
		//ZTest[unity_GUIZTestMode]

	Pass {

		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma multi_compile_fog

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			sampler2D _MainTex;
			//float4 _MainTex_ST;
			half4 _Color;
			float _Size;
			float _Offset;
			float _NumPoints;
			int _ZTest;
			float _Dotted;
			float4 _ClipRect;

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;

				//UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;

				//UNITY_FOG_COORDS(1)
				//UNITY_VERTEX_OUTPUT_STEREO
			};


			inline float4 MoveVert(float4 vertex, float2 dir)
			{
				vertex.xyz /= vertex.w;
				float2 hpc = floor(_ScreenParams.xy) / 2;
				vertex.xy *= hpc;

				vertex.xy += dir*_Size;

				vertex.xy /= hpc;
				vertex.xyz *= vertex.w;

				return vertex;
			}


			v2f vert(appdata v)
			{
				v2f o;

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.vertex = MoveVert(o.vertex, v.uv);

				o.vertex.z += _Offset; //1 / (_ZBufferParams.x * _Offset);

				o.uv = (v.uv + 1) / 2;

				return o;
			}


			fixed4 frag(v2f IN) : SV_Target
			{
				half4 color = tex2D(_MainTex, IN.uv);

				color.rgb *= _Color.rgb;

				//clipping rect
				if (_ClipRect.z > 0.5 && _ClipRect.w > 0.5)
					color.a *= UnityGet2DClipping(IN.vertex.xy, _ClipRect);

				clip(color.a - 0.001);

				return color;
			}


		ENDCG
	}
}

}

