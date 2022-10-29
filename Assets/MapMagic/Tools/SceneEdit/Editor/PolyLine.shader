
Shader "Hidden/DPLayout/PolyLine"
//Draws a line using specially prepared mesh with 4x points
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		//[Linear] _Color("Color", Color) = (0.56, 0.56, 0.56, 0.56) //using vector to make independent from gamma/linear
		_Color("Color", Vector) = (0.56, 0.56, 0.56, 0.56)
		_Width("Width", Float) = 5
		_Offset("Offset", Float) = 0.001
		_NumPoints("Num Points", Float) = 1024
		_ZTest("Z Test", Int) = 0
		_Dotted("Dotted Dist", Float) = 0
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
			float _Width;
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
				float2 uv2 : TEXCOORD1;

				//UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float pointNum : TEXCOORD1;
				float distance : TEXCOORD2;  //rename to length?

				//UNITY_FOG_COORDS(1)
				//UNITY_VERTEX_OUTPUT_STEREO
			};
			

			inline float4 MoveVert(float4 vertex, float4 neigVertex, float2 dir)
			{
				vertex.xyz /= vertex.w;
				float2 hpc = floor(_ScreenParams.xy) / 2;
				vertex.xy *= hpc;
				neigVertex.xy *= hpc;

				neigVertex.xyz /= neigVertex.w;
				float3 neigDir = normalize(neigVertex.xyz - vertex.xyz);
				neigDir *= (sign(neigVertex.w)*sign(vertex.w)); //in case neig vert behind the screen

				//shifting initial vertex a bit to make corners smooth
				vertex.xy -= neigDir.xy * _Width/4;

				//width perpendicular movement
				float2 perp = float2(neigDir.y, -neigDir.x); //  normalize(cross(neigDir.xyz, float3(0,0,-1)));
				vertex.xy += perp.xy * dir.y * _Width/2;

				//pixel perfect
				//OUT.vertex.xy = floor(OUT.vertex.xy + 0.5);
				//if (_Flip > 0.5) OUT.vertex.xy -= v.normal.xy * _Size;
				//else OUT.vertex.xy += v.normal.xy * _Size;

				vertex.xy /= hpc;
				vertex.xyz *= vertex.w;

				return vertex;
			}


			v2f vert(appdata v)
			{
				v2f o;

				o.vertex = UnityObjectToClipPos(v.vertex);
				float4 neigVertex = UnityObjectToClipPos(v.normal);
				o.vertex = MoveVert(o.vertex, neigVertex, v.uv);

				o.vertex.z += _Offset / _ZBufferParams.x * unity_OrthoParams.w; //orthographic offset
				o.vertex.z += _Offset * (1-unity_OrthoParams.w);  //perspective offset

				o.uv = float2(v.uv.x, v.uv.y*v.uv.x);
				o.uv = (o.uv + 1) / 2;

				o.pointNum = v.uv2.x;
				o.distance = v.uv2.y;

				return o;
			}


			fixed4 frag(v2f IN) : SV_Target
			{
				half4 color = tex2D(_MainTex, IN.uv);

				color.rgb *= _Color.rgb;
				color.rgb *= color.a;
				//color.a = 1;

				//color.a = 0;
				//color.rgb = 0;

				//clip(_NumPoints - IN.pointNum);

				//clipping dotted line
				if (_Dotted >= 0.00001f)
				{
					float distStep = IN.distance % _Dotted;
					clip(_Dotted - distStep*2);
				}

				//clipping rect
				if (_ClipRect.z > 0.5 && _ClipRect.w > 0.5)
				{
					color.a *= UnityGet2DClipping(IN.vertex.xy, _ClipRect);
					//float2 inside = step(_ClipRect.xy, IN.vertex.xy) * step(IN.vertex.xy, _ClipRect.zw);
					//color.a *= inside.x * inside.y;
				}

				clip(color.a - 0.001);

				return color;
			}


		ENDCG
	}
}

}

