Shader "MapMagic/TerrainPreviewEmpty" 
{
	SubShader{
		Tags {
			"Queue" = "Geometry-99"
			"IgnoreProjector" = "True"
			"RenderType" = "Opaque"
		}

		CGPROGRAM
		#pragma surface surf Standard decal:add vertex:SplatmapVert finalcolor:SplatmapFinalColor finalgbuffer:SplatmapFinalGBuffer fullforwardshadows nometa
		#pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap forwardadd
		#pragma multi_compile_fog
		#pragma target 3.0
		// needs more than 8 texcoords
		#pragma exclude_renderers gles
		#include "UnityPBSLighting.cginc"

		#pragma multi_compile_local __ _NORMALMAP

		#define TERRAIN_SPLAT_ADDPASS
		#define TERRAIN_STANDARD_SHADER
		#define TERRAIN_INSTANCED_PERPIXEL_NORMAL
		#define TERRAIN_SURFACE_OUTPUT SurfaceOutputStandard
		#include "TerrainSplatmapCommon.cginc"

		half _Metallic0;
		half _Metallic1;
		half _Metallic2;
		half _Metallic3;

		half _Smoothness0;
		half _Smoothness1;
		half _Smoothness2;
		half _Smoothness3;

		#include "Preview.cginc"

		sampler2D _Preview;
		float4 _Preview_ST;
		float4 _Preview_TexelSize;
		int _Margins;
		float _Colorize;

		float _Smoothness;
		float _Specular;
		float _SpecColorize;
		float _Saturation;
		float _Brightness;

		void surf(Input IN, inout SurfaceOutputStandard o) 
		{
			half4 splat_control;
			half weight;
			fixed4 mixedDiffuse;
			half4 defaultSmoothness = half4(_Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3);
			SplatmapMix(IN, defaultSmoothness, splat_control, weight, mixedDiffuse, o.Normal);
			o.Albedo = mixedDiffuse.rgb;
			o.Alpha = weight;
			o.Smoothness = mixedDiffuse.a;
			o.Metallic = dot(splat_control, half4(_Metallic0, _Metallic1, _Metallic2, _Metallic3));

			float2 uv = IN.tc;
			float2 pxTexSize = _Preview_TexelSize.zw - _Margins * 2 - 1; //texture size without margins. Don't actually know why -1, related with 513
			float2 ratio = pxTexSize / _Preview_TexelSize.zw;
			float2 uvMargins = (1 - ratio) / 2;
			uv *= ratio;
			uv += uvMargins;

			//float2 pixelSize = 1 / texSize;
			//uv *= _Preview_TexelSize.zw / texSize; //don't know about why +1 and -0.5, when get rid of res 513 check this once more
			//uv += _Margins * pixelSize / 2;

			half4 col = tex2D(_Preview, uv);
			col.rgb = col.r*(1 - _Colorize) + colorize(col.r, false)*_Colorize;
			col.rgb = saturation(col.rgb, _Saturation) * _Brightness;
			col.rgb = GammaToLinearSpace(col.rgb);

			o.Albedo = overlay(col.rgb, mixedDiffuse.rgb)*0.8 + col.rgb * 0.2; //mixedDiffuse.rgb*mixedDiffuse.rgb*0.5 + col.rgb;
			//o.Smoothness = _Smoothness;
			//o.Specular = (1 - _SpecColorize + col.rgb*_SpecColorize) * _Specular;
			o.Emission = col.rgb * 0.2;
		}
		ENDCG
	}

		Fallback "Hidden/TerrainEngine/Splatmap/Diffuse-AddPass"
}