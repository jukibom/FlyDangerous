Shader "MapMagic/TerrainPreview" 
{
	Properties
	{
		[HideInInspector] _Control("Control (RGBA)", 2D) = "red" {}
		_Preview("Control (RGBA)", 2D) = "red" {}

		_Colorize("Colorize", Float) = 1

		_Margins("Margins", Int) = 16

		[HDR] _Color0("Color Black", Color) = (0.666, 0.2, 0.000, 1)  //(0.000, 0.426, 0.000, 1) //0, 109, 0
		[HDR] _Color1("Color Dark", Color) = (0.95, 0.65, 0.1, 1)	//(0.664, 0.793, 0.476, 1)  //170, 203, 122
		[HDR] _Color2("Color Gray", Color) = (1, 1, 0.75, 1)			//(0.949, 0.925, 0.695, 1)  //243, 237, 178
		[HDR] _Color3("Color Bright", Color) = (0.55, 0.85, 0.2, 1)	//(0.902, 0.578, 0.171, 1)  //231, 148, 44
		[HDR] _Color4("Color White", Color) = (0.000, 0.666, 0.2, 1)  //(0.726, 0.000, 0.000, 1)  //186, 0, 0 

		_Smoothness("Smoothness", Float) = 0.25
		_Specular("Specular", Float) = 0.75
		_SpecColorize("Spec Colorize", Float) = 1
		_Saturation("Saturation", Float) = 1.25
		_Brightness("Brightness", Float) = 0.9
	}


	SubShader {
		Tags {
			"Queue" = "Geometry-100"
			"RenderType" = "Opaque"
		}

        CGPROGRAM
        #pragma surface surf Standard vertex:SplatmapVert finalcolor:SplatmapFinalColor finalgbuffer:SplatmapFinalGBuffer addshadow fullforwardshadows
        #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap forwardadd
        #pragma multi_compile_fog // needed because finalcolor oppresses fog code generation.
        #pragma target 3.0
        // needs more than 8 texcoords
        #pragma exclude_renderers gles
        #include "UnityPBSLighting.cginc"

       // #pragma multi_compile __ _NORMALMAP

		#define TERRAIN_STANDARD_SHADER
		#define TERRAIN_INSTANCED_PERPIXEL_NORMAL
		#define TERRAIN_SURFACE_OUTPUT SurfaceOutputStandard
		#include "TerrainSplatmapCommon.cginc" //wish I could use SplatmapMix and SplatmapVert directly from here, but it's got Input defined with no worldPos
		
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


		void surf (Input IN, inout SurfaceOutputStandard o)
		{
			half4 splat_control;
			half weight;
			fixed4 mixedDiffuse;
			half4 defaultSmoothness;
			SplatmapMix(IN, defaultSmoothness, splat_control, weight, mixedDiffuse, o.Normal);
			o.Albedo = mixedDiffuse.rgb;
			o.Alpha = weight;

			float2 uv = IN.tc;
			float2 pxTexSize = _Preview_TexelSize.zw - _Margins * 2 - 1; //texture size without margins. Don't actually know why -1, related with 513
			float2 ratio = pxTexSize / _Preview_TexelSize.zw;
			float2 uvMargins = (1-ratio) / 2;
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

			//o.Normal = normalize(tex2D(_TerrainNormalmapTexture, uv).xyz * 2 - 1).xzy;
		}
		ENDCG

		UsePass "Hidden/Nature/Terrain/Utilities/PICKING"
		UsePass "Hidden/Nature/Terrain/Utilities/SELECTION"
	}

	//Dependency "BaseMapShader" = "MapMagic/TerrainPreview"

	Dependency "AddPassShader" = "MapMagic/TerrainPreviewEmpty"
	//Dependency "BaseMapShader" = "Hidden/TerrainEngine/Splatmap/Standard-Base"
	//Dependency "BaseMapGenShader" = "Hidden/TerrainEngine/Splatmap/Standard-BaseGen"

	Fallback "Nature/Terrain/Diffuse"
}