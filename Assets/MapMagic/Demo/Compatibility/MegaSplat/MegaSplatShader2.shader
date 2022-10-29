//////////////////////////////////////////////////////
// MegaSplat - 256 texture splat mapping
// Copyright (c) Jason Booth, slipster216@gmail.com
//
// Auto-generated shader code, don't hand edit!
//   Compiled with MegaSplat 1.6
//   Unity : 2019.3.0f5
//   Platform : WindowsEditor
//////////////////////////////////////////////////////

Shader "MegaSplat/MegaSplatShader2" {
   Properties {
      // Splats
      _Glossiness("Smoothness", Range(0.0, 1.0)) = 0.0
      [Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0

      [NoScaleOffset]_Diffuse ("Diffuse Array", 2DArray) = "white" {}
      [NoScaleOffset]_Normal ("Normal Array", 2DArray) = "bump" {}
      [NoScaleOffset]_AlphaArray("Alpha Array", 2DArray) = "white" {}

      _TexScales("Texure Scales",  Vector) = (10,10,10,10)

      _Contrast("Blend Contrast", Range(0.01, 0.99)) = 0.4
      _Parallax ("Parallax Scale", Vector) = (0.08, 20, 0, 0)

      _DistanceFades("Detail Fade Start/End", Vector) = (500,2000, 500, 2000)
      _DistanceFadesCached("Detail Fade Start/End", Vector) = (500,2000, 500, 2000)

      _GlobalPorosityWetness("Default Porosity and Wetness", Vector) = (0.4, 0.0, 0.0, 0.0)
      _Cutoff("Alpha Clip", Float) = 0.5


   }
   SubShader {
      Tags {"RenderType"="Opaque"}
      CGPROGRAM
      #pragma exclude_renderers d3d9
      #pragma multi_compile_fog
      #pragma surface surf Standard vertex:vert fullforwardshadows addshadow nofog finalcolor:fogcolor
      #pragma target 3.5

      #define _NORMALMAP 1



         struct MegaSplatLayer
         {
            half3 Albedo;
            half3 Normal;
            half3 Emission;
            half  Metallic;
            half  Smoothness;
            half  Occlusion;
            half  Height;
            half  Alpha;
         };

         struct LightingTerms
         {
            half3 Albedo;
            half3 Normal;
            half  Smoothness;
            half  Metallic;
            half  Occlusion;
            half3 Emission;
            half Alpha;
         };

         struct SplatInput
         {
            float3 weights;
            float2 splatUV;
            float2 macroUV;
            float3 valuesMain;
            half3 viewDir;
            float4 camDist;

            #if _TWOLAYER || _ALPHALAYER
            float3 valuesSecond;
            half layerBlend;
            #endif

            #if _TRIPLANAR
            float3 triplanarUVW;
            #endif
            half3 triplanarBlend; // passed to func, so always present

            #if _FLOW || _FLOWREFRACTION || _PUDDLES || _PUDDLEFLOW || _PUDDLEREFRACT || _LAVA
            half2 flowDir;
            #endif

            #if _PUDDLES || _PUDDLEFLOW || _PUDDLEREFRACT || _LAVA
            half puddleHeight;
            #endif

            #if _PUDDLES || _PUDDLEFLOW || _PUDDLEREFRACT
            float3 waterNormalFoam;
            #endif

            #if _WETNESS
            half wetness;
            #endif

            #if _TESSDAMPENING
            half displacementDampening;
            #endif

            #if _SNOW
            half snowHeightFade;
            #endif

            #if _SNOW || _SNOWGLITTER || _PUDDLEGLITTER || _PERTEXGLITTER
            float3 wsNormal;
            #endif

            #if _SNOWGLITTER || _PUDDLEGLITTER || _PERTEXGLITTER
            float3 wsView;
            #endif

            #if _GEOMAP
            float3 worldPos;
            #endif
         };


         struct LayerParams
         {
            float3 uv0, uv1, uv2;
            float2 mipUV; // uv for mip selection
            #if _TRIPLANAR
            float3 tpuv0_x, tpuv0_y, tpuv0_z;
            float3 tpuv1_x, tpuv1_y, tpuv1_z;
            float3 tpuv2_x, tpuv2_y, tpuv2_z;
            #endif
            #if _FLOW || _FLOWREFRACTION
            float3 fuv0a, fuv0b;
            float3 fuv1a, fuv1b;
            float3 fuv2a, fuv2b;
            #endif

            #if _DISTANCERESAMPLE
               half distanceBlend;
               float3 db_uv0, db_uv1, db_uv2;
               #if _TRIPLANAR
               float3 db_tpuv0_x, db_tpuv0_y, db_tpuv0_z;
               float3 db_tpuv1_x, db_tpuv1_y, db_tpuv1_z;
               float3 db_tpuv2_x, db_tpuv2_y, db_tpuv2_z;
               #endif
               float resampleMip;
            #endif

            half layerBlend;
            half3 metallic;
            half3 smoothness;
            half3 porosity;
            #if _FLOW || _FLOWREFRACTION
            half3 flowIntensity;
            half flowOn;
            half3 flowAlphas;
            half3 flowRefracts;
            half3 flowInterps;
            #endif
            half3 weights;

            #if _TESSDISTANCE || _TESSEDGE
            half3 displacementScale;
            half3 upBias;
            #endif

            #if _PERTEXNOISESTRENGTH
            half3 detailNoiseStrength;
            #endif

            #if _PERTEXNORMALSTRENGTH
            half3 normalStrength;
            #endif

            #if _PERTEXPARALLAXSTRENGTH
            half3 parallaxStrength;
            #endif

            #if _PERTEXAOSTRENGTH
            half3 aoStrength;
            #endif

            #if _PERTEXGLITTER
            half3 perTexGlitterReflect;
            #endif

            half3 contrast;
         };

         struct VirtualMapping
         {
            float3 weights;
            fixed4 c0, c1, c2;
            fixed4 param;
         };



         #include "UnityCG.cginc"
         #include "AutoLight.cginc"
         #include "Lighting.cginc"
         #include "UnityPBSLighting.cginc"
         #include "UnityStandardBRDF.cginc"

         // splat
         UNITY_DECLARE_TEX2DARRAY(_Diffuse);
         float4 _Diffuse_TexelSize;
         UNITY_DECLARE_TEX2DARRAY(_Normal);
         float4 _Normal_TexelSize;
         #if _EMISMAP
         UNITY_DECLARE_TEX2DARRAY(_Emissive);
         float4 _Emissive_TexelSize;
         #endif
         #if _DETAILMAP
         UNITY_DECLARE_TEX2DARRAY(_DetailAlbedo);
         float4 _DetailAlbedo_TexelSize;
         UNITY_DECLARE_TEX2DARRAY(_DetailNormal);
         float4 _DetailNormal_TexelSize;
         half _DetailTextureStrength;
         #endif

         #if _ALPHA || _ALPHATEST
         UNITY_DECLARE_TEX2DARRAY(_AlphaArray);
         float4 _AlphaArray_TexelSize;
         #endif

         #if _LOWPOLY
         half _EdgeHardness;
         #endif

         #if _TERRAIN
         sampler2D _SplatControl;
         sampler2D _SplatParams;
         #endif

         #if _ALPHAHOLE
         int _AlphaHoleIdx;
         #endif

         #if _RAMPLIGHTING
         sampler2D _Ramp;
         #endif

         #if _UVLOCALTOP || _UVWORLDTOP || _UVLOCALFRONT || _UVWORLDFRONT || _UVLOCALSIDE || _UVWORLDSIDE
         float4 _UVProjectOffsetScale;
         #endif
         #if _UVLOCALTOP2 || _UVWORLDTOP2 || _UVLOCALFRONT2 || _UVWORLDFRONT2 || _UVLOCALSIDE2 || _UVWORLDSIDE2
         float4 _UVProjectOffsetScale2;
         #endif

         half _Contrast;

         #if _ALPHALAYER || _USEMACROTEXTURE
         // macro texturing
         sampler2D _MacroDiff;
         sampler2D _MacroBump;
         sampler2D _MetallicGlossMap;
         sampler2D _MacroAlpha;
         half2 _MacroTexScale;
         half _MacroTextureStrength;
         half2 _MacroTexNormAOScales;
         #endif

         // default spec
         half _Glossiness;
         half _Metallic;

         #if _DISTANCERESAMPLE
         float3  _ResampleDistanceParams;
         #endif

         #if _FLOW || _FLOWREFRACTION
         // flow
         half _FlowSpeed;
         half _FlowAlpha;
         half _FlowIntensity;
         half _FlowRefraction;
         #endif

         half2 _PerTexScaleRange;
         sampler2D _PropertyTex;

         // etc
         half2 _Parallax;
         half4 _TexScales;

         float4 _DistanceFades;
         float4 _DistanceFadesCached;
         int _ControlSize;

         #if _TRIPLANAR
         half _TriplanarTexScale;
         half _TriplanarContrast;
         float3 _TriplanarOffset;
         #endif

         #if _GEOMAP
         sampler2D _GeoTex;
         float3 _GeoParams;
         #endif

         #if _PROJECTTEXTURE_LOCAL || _PROJECTTEXTURE_WORLD
         half3 _ProjectTexTop;
         half3 _ProjectTexSide;
         half3 _ProjectTexBottom;
         half3 _ProjectTexThresholdFreq;
         #endif

         #if _PROJECTTEXTURE2_LOCAL || _PROJECTTEXTURE2_WORLD
         half3 _ProjectTexTop2;
         half3 _ProjectTexSide2;
         half3 _ProjectTexBottom2;
         half3 _ProjectTexThresholdFreq2;
         half4 _ProjectTexBlendParams;
         #endif

         #if _TESSDISTANCE || _TESSEDGE
         float4 _TessData1; // distance tessellation, displacement, edgelength
         float4 _TessData2; // min, max
         #endif

         #if _DETAILNOISE
         sampler2D _DetailNoise;
         half3 _DetailNoiseScaleStrengthFade;
         #endif

         #if _DISTANCENOISE
         sampler2D _DistanceNoise;
         half4 _DistanceNoiseScaleStrengthFade;
         #endif

         #if _PUDDLES || _PUDDLEFLOW || _PUDDLEREFRACT
         half3 _PuddleTint;
         half _PuddleBlend;
         half _MaxPuddles;
         half4 _PuddleFlowParams;
         half2 _PuddleNormalFoam;
         #endif

         #if _RAINDROPS
         sampler2D _RainDropTexture;
         half _RainIntensity;
         float2 _RainUVScales;
         #endif

         #if _PUDDLEFLOW || _PUDDLEREFRACT
         float2 _PuddleUVScales;
         sampler2D _PuddleNormal;
         #endif

         #if _LAVA
         sampler2D _LavaDiffuse;
         sampler2D _LavaNormal;
         half4 _LavaParams;
         half4 _LavaParams2;
         half3 _LavaEdgeColor;
         half3 _LavaColorLow;
         half3 _LavaColorHighlight;
         float2 _LavaUVScale;
         #endif

         #if _WETNESS
         half _MaxWetness;
         #endif

         half2 _GlobalPorosityWetness;

         #if _SNOW
         sampler2D _SnowDiff;
         sampler2D _SnowNormal;
         half4 _SnowParams; // influence, erosion, crystal, melt
         half _SnowAmount;
         half2 _SnowUVScales;
         half2 _SnowHeightRange;
         half3 _SnowUpVector;
         #endif

         #if _SNOWDISTANCENOISE
         sampler2D _SnowDistanceNoise;
         float4 _SnowDistanceNoiseScaleStrengthFade;
         #endif

         #if _SNOWDISTANCERESAMPLE
         float4 _SnowDistanceResampleScaleStrengthFade;
         #endif

         #if _PERTEXGLITTER || _SNOWGLITTER || _PUDDLEGLITTER
         sampler2D _GlitterTexture;
         half4 _GlitterParams;
         half4 _GlitterSurfaces;
         #endif


         //// FIXES TO UNITY 2017.2 BUGS

         // Problem 1: SHADOW_COORDS - undefined identifier.
         // Why: Using SHADOWS_DEPTH without SPOT.
         // The file AutoLight.cginc only takes into account the case where you use SHADOWS_DEPTH + SPOT (to enable SPOT just add a Spot Light in the scene).
         // So, if your scene doesn't have a Spot Light, it will skip the SHADOW_COORDS definition and shows the error.
         // Now, to workaround this you can:
         // 1. Add a Spot Light to your scene 
         // 2. Use this CGINC to workaround this scase.  Also, you can copy this in your own shader.
         #if defined (SHADOWS_DEPTH) && !defined (SPOT)
         #       define SHADOW_COORDS(idx1) unityShadowCoord2 _ShadowCoord : TEXCOORD##idx1;
         #endif


         // Problem 2: _ShadowCoord - invalid subscript.
         // Why: nor Shadow screen neighter Shadow Depth or Shadow Cube and trying to use _ShadowCoord attribute.
         // The file AutoLight.cginc defines SHADOW_COORDS to empty when no one of these options are enabled (SHADOWS_SCREEN, SHADOWS_DEPTH and SHADOWS_CUBE), 
         // So, if you try to call "o._ShadowCoord = ..." it will break because _ShadowCoord isn't an attribute in your structure.
         // To workaround this you can:
         // 1. Check if one of those defines actually exists in any place where you have "o._ShadowCoord...".
         // 2. Use the define SHADOWS_ENABLED from this file to perform the same check.
         #if defined (SHADOWS_SCREEN) || defined (SHADOWS_DEPTH) || defined (SHADOWS_CUBE) 
         #  define SHADOWS_ENABLED
         #endif

         struct VertexOutput 
         {
             float4 pos          : SV_POSITION;
             #if !_TERRAIN
             fixed3 weights      : TEXCOORD0;
             float4 valuesMain   : TEXCOORD1;      //index rgb, triplanar W
                #if _TWOLAYER || _ALPHALAYER
                float4 valuesSecond : TEXCOORD2;      //index rgb + alpha
                #endif
             #elif _TRIPLANAR
             float3 triplanarUVW : TEXCOORD3;
             #endif
             float2 coords       : TEXCOORD4;      // uv, or triplanar UV
             float3 posWorld     : TEXCOORD5;
             float3 normal       : TEXCOORD6;

             float4 camDist      : TEXCOORD7;      // distance from camera (for fades) and fog
             float4 extraData    : TEXCOORD8;      // flowdir + fades, or if triplanar triplanarView + detailFade
             float3 tangent      : TEXCOORD9;
             float3 bitangent    : TEXCOORD10;
              
             float4 ambientOrLightmapUV : TEXCOORD14;

             UNITY_SHADOW_COORDS(12)
             UNITY_FOG_COORDS(13)

             #if _PASSSHADOWCASTER
             float3 vec : TEXCOORD11;  // nice naming, Unity...
             #endif

             #if _WETNESS
             half wetness : TEXCOORD15;   //wetness
             #endif

             #if _SECONDUV
             float2 macroUV : TEXCOORD16;
             #endif

         };

         void OffsetUVs(inout LayerParams p, float2 offset)
         {
            p.uv0.xy += offset;
            p.uv1.xy += offset;
            p.uv2.xy += offset;
            #if _TRIPLANAR
            p.tpuv0_x.xy += offset;
            p.tpuv0_y.xy += offset;
            p.tpuv0_z.xy += offset;
            p.tpuv1_x.xy += offset;
            p.tpuv1_y.xy += offset;
            p.tpuv1_z.xy += offset;
            p.tpuv2_x.xy += offset;
            p.tpuv2_y.xy += offset;
            p.tpuv2_z.xy += offset;
            #endif
         }


         void InitDistanceResample(inout LayerParams lp, float dist)
         {
            #if _DISTANCERESAMPLE
               lp.distanceBlend = saturate((dist - _ResampleDistanceParams.y) / (_ResampleDistanceParams.z - _ResampleDistanceParams.y));
               lp.db_uv0 = lp.uv0;
               lp.db_uv1 = lp.uv1;
               lp.db_uv2 = lp.uv2;

               lp.db_uv0.xy *= _ResampleDistanceParams.xx;
               lp.db_uv1.xy *= _ResampleDistanceParams.xx;
               lp.db_uv2.xy *= _ResampleDistanceParams.xx;


               #if _TRIPLANAR
               lp.db_tpuv0_x = lp.tpuv0_x;
               lp.db_tpuv1_x = lp.tpuv1_x;
               lp.db_tpuv2_x = lp.tpuv2_x;
               lp.db_tpuv0_y = lp.tpuv0_y;
               lp.db_tpuv1_y = lp.tpuv1_y;
               lp.db_tpuv2_y = lp.tpuv2_y;
               lp.db_tpuv0_z = lp.tpuv0_z;
               lp.db_tpuv1_z = lp.tpuv1_z;
               lp.db_tpuv2_z = lp.tpuv2_z;

               lp.db_tpuv0_x.xy *= _ResampleDistanceParams.xx;
               lp.db_tpuv1_x.xy *= _ResampleDistanceParams.xx;
               lp.db_tpuv2_x.xy *= _ResampleDistanceParams.xx;
               lp.db_tpuv0_y.xy *= _ResampleDistanceParams.xx;
               lp.db_tpuv1_y.xy *= _ResampleDistanceParams.xx;
               lp.db_tpuv2_y.xy *= _ResampleDistanceParams.xx;
               lp.db_tpuv0_z.xy *= _ResampleDistanceParams.xx;
               lp.db_tpuv1_z.xy *= _ResampleDistanceParams.xx;
               lp.db_tpuv2_z.xy *= _ResampleDistanceParams.xx;
               #endif
            #endif
         }


         LayerParams NewLayerParams()
         {
            LayerParams l = (LayerParams)0;
            l.metallic = _Metallic.xxx;
            l.smoothness = _Glossiness.xxx;
            l.porosity = _GlobalPorosityWetness.xxx;

            l.layerBlend = 0;
            #if _FLOW || _FLOWREFRACTION
            l.flowIntensity = 0;
            l.flowOn = 0;
            l.flowAlphas = half3(1,1,1);
            l.flowRefracts = half3(1,1,1);
            #endif

            #if _TESSDISTANCE || _TESSEDGE
            l.displacementScale = half3(1,1,1);
            l.upBias = half3(0,0,0);
            #endif

            #if _PERTEXNOISESTRENGTH
            l.detailNoiseStrength = half3(1,1,1);
            #endif

            #if _PERTEXNORMALSTRENGTH
            l.normalStrength = half3(1,1,1);
            #endif

            #if _PERTEXAOSTRENGTH
            l.aoStrength = half3(1,1,1);
            #endif

            #if _PERTEXPARALLAXSTRENGTH
            l.parallaxStrength = half3(1,1,1);
            #endif

            l.contrast = _Contrast;

            return l;
         }

         half AOContrast(half ao, half scalar)
         {
            scalar += 0.5;  // 0.5 -> 1.5
            scalar *= scalar; // 0.25 -> 2.25
            return pow(ao, scalar);
         }

         half MacroAOContrast(half ao, half scalar)
         {
            #if _MACROAOSCALE
            return AOContrast(ao, scalar);
            #else
            return ao;
            #endif
         }
         #if _USEMACROTEXTURE || _ALPHALAYER
         MegaSplatLayer SampleMacro(float2 uv)
         {
             MegaSplatLayer o = (MegaSplatLayer)0;
             float2 macroUV = uv * _MacroTexScale.xy;
             half4 macAlb = tex2D(_MacroDiff, macroUV);
             o.Albedo = macAlb.rgb;
             o.Height = macAlb.a;
             // defaults
             o.Normal = half3(0,0,1);
             o.Occlusion = 1;
             o.Smoothness = _Glossiness;
             o.Metallic = _Metallic;
             o.Emission = half3(0,0,0);

             // unpack normal
             #if !_NOSPECTEX
             half4 normSample = tex2D(_MacroBump, macroUV);
             o.Normal = UnpackNormal(normSample);
             o.Normal.xy *= _MacroTexNormAOScales.x;
             #else
             o.Normal = half3(0,0,1);
             #endif


             #if _ALPHA || _ALPHATEST
             o.Alpha = tex2D(_MacroAlpha, macroUV).r;
             #endif
             return o;
         }
         #endif


         #if _NOSPECTEX
           #define SAMPLESPEC(o, params) \
              half4 specFinal = half4(0,0,0,1); \
              specFinal.x = params.metallic.x * weights.x + params.metallic.y * weights.y + params.metallic.z * weights.z; \
              specFinal.y = params.smoothness.x * weights.x + params.smoothness.y * weights.y + params.smoothness.z * weights.z; \
              o.Normal = UnpackNormal(norm); 

         #elif _NOSPECNORMAL   
           #define SAMPLESPEC(o, params) \
              half4 specFinal = half4(0,0,0,1); \
              specFinal.x = params.metallic.x * weights.x + params.metallic.y * weights.y + params.metallic.z * weights.z; \
              specFinal.y = params.smoothness.x * weights.x + params.smoothness.y * weights.y + params.smoothness.z * weights.z; \
              o.Normal = half3(0,0,1);

         #else
            #define SAMPLESPEC(o, params) \
              half4 spec0, spec1, spec2; \
              half4 specFinal = half4(0,0,0,1); \
              specFinal.yw =  norm.zw; \
              specFinal.x = (params.metallic.x * weights.x + params.metallic.y * weights.y + params.metallic.z * weights.z); \
              norm.xy *= 2; \
              norm.xy -= 1; \
              o.Normal = half3(norm.x, norm.y, sqrt(1 - saturate(dot(norm.xy, norm.xy)))); \

         #endif

         void SamplePerTex(sampler2D pt, inout LayerParams params, float2 scaleRange)
         {
            const half cent = 1.0 / 512.0;
            const half pixelStep = 1.0 / 256.0;
            const half vertStep = 1.0 / 8.0;

            // pixel layout for per tex properties
            // metal/smooth/porosity/uv scale
            // flow speed, intensity, alpha, refraction
            // detailNoiseStrength, contrast, displacementAmount, displaceUpBias

            #if _PERTEXMATPARAMS || _PERTEXUV
            {
               half4 props0 = tex2Dlod(pt, half4(params.uv0.z * pixelStep + cent, 0, 0, 0));
               half4 props1 = tex2Dlod(pt, half4(params.uv1.z * pixelStep + cent, 0, 0, 0));
               half4 props2 = tex2Dlod(pt, half4(params.uv2.z * pixelStep + cent, 0, 0, 0));
               params.porosity = half3(0.4, 0.4, 0.4);
               #if _PERTEXMATPARAMS
               params.metallic = half3(props0.r, props1.r, props2.r);
               params.smoothness = half3(props0.g, props1.g, props2.g);
               params.porosity = half3(props0.b, props1.b, props2.b);
               #endif
               #if _PERTEXUV
               float3 uvScale = float3(props0.a, props1.a, props2.a);
               uvScale = lerp(scaleRange.xxx, scaleRange.yyy, uvScale);
               params.uv0.xy *= uvScale.x;
               params.uv1.xy *= uvScale.y;
               params.uv2.xy *= uvScale.z;

                  #if _TRIPLANAR
                  params.tpuv0_x.xy *= uvScale.xx; params.tpuv0_y.xy *= uvScale.xx; params.tpuv0_z.xy *= uvScale.xx;
                  params.tpuv1_x.xy *= uvScale.yy; params.tpuv1_y.xy *= uvScale.yy; params.tpuv1_z.xy *= uvScale.yy;
                  params.tpuv2_x.xy *= uvScale.zz; params.tpuv2_y.xy *= uvScale.zz; params.tpuv2_z.xy *= uvScale.zz;
                  #endif

               #endif
            }
            #endif


            #if _FLOW || _FLOWREFRACTION
            {
               half4 props0 = tex2Dlod(pt, half4(params.uv0.z * pixelStep + cent, vertStep * 3, 0, 0));
               half4 props1 = tex2Dlod(pt, half4(params.uv1.z * pixelStep + cent, vertStep * 3, 0, 0));
               half4 props2 = tex2Dlod(pt, half4(params.uv2.z * pixelStep + cent, vertStep * 3, 0, 0));

               params.flowIntensity = half3(props0.r, props1.r, props2.r);
               params.flowOn = params.flowIntensity.x + params.flowIntensity.y + params.flowIntensity.z;

               params.flowAlphas = half3(props0.b, props1.b, props2.b);
               params.flowRefracts = half3(props0.a, props1.a, props2.a);
            }
            #endif

            #if _PERTEXDISPLACEPARAMS || _PERTEXCONTRAST || _PERTEXNOISESTRENGTH
            {
               half4 props0 = tex2Dlod(pt, half4(params.uv0.z * pixelStep + cent, vertStep * 5, 0, 0));
               half4 props1 = tex2Dlod(pt, half4(params.uv1.z * pixelStep + cent, vertStep * 5, 0, 0));
               half4 props2 = tex2Dlod(pt, half4(params.uv2.z * pixelStep + cent, vertStep * 5, 0, 0));

               #if _PERTEXDISPLACEPARAMS && (_TESSDISTANCE || _TESSEDGE)
               params.displacementScale = half3(props0.b, props1.b, props2.b);
               params.upBias = half3(props0.a, props1.a, props2.a);
               #endif

               #if _PERTEXCONTRAST
               params.contrast = half3(props0.g, props1.g, props2.g);
               #endif

               #if _PERTEXNOISESTRENGTH
               params.detailNoiseStrength = half3(props0.r, props1.r, props2.r);
               #endif
            }
            #endif

            #if _PERTEXNORMALSTRENGTH || _PERTEXPARALLAXSTRENGTH || _PERTEXAOSTRENGTH || _PERTEXGLITTER
            {
               half4 props0 = tex2Dlod(pt, half4(params.uv0.z * pixelStep + cent, vertStep * 7, 0, 0));
               half4 props1 = tex2Dlod(pt, half4(params.uv1.z * pixelStep + cent, vertStep * 7, 0, 0));
               half4 props2 = tex2Dlod(pt, half4(params.uv2.z * pixelStep + cent, vertStep * 7, 0, 0));

               #if _PERTEXNORMALSTRENGTH
               params.normalStrength = half3(props0.r, props1.r, props2.r);
               #endif

               #if _PERTEXPARALLAXSTRENGTH
               params.parallaxStrength = half3(props0.g, props1.g, props2.g);
               #endif

               #if _PERTEXAOSTRENGTH
               params.aoStrength = half3(props0.b, props1.b, props2.b);
               #endif

               #if _PERTEXGLITTER
               params.perTexGlitterReflect = half3(props0.a, props1.a, props2.a);
               #endif

            }
            #endif
         }

         float FlowRefract(MegaSplatLayer tex, inout LayerParams main, inout LayerParams second, half3 weights)
         {
            #if _FLOWREFRACTION
            float totalFlow = second.flowIntensity.x * weights.x + second.flowIntensity.y * weights.y + second.flowIntensity.z * weights.z;
            float falpha = second.flowAlphas.x * weights.x + second.flowAlphas.y * weights.y + second.flowAlphas.z * weights.z;
            float frefract = second.flowRefracts.x * weights.x + second.flowRefracts.y * weights.y + second.flowRefracts.z * weights.z;
            float refractOn = min(1, totalFlow * 10000);
            float ratio = lerp(1.0, _FlowAlpha * falpha, refractOn);
            float2 rOff = tex.Normal.xy * _FlowRefraction * frefract * ratio;
            main.uv0.xy += rOff;
            main.uv1.xy += rOff;
            main.uv2.xy += rOff;
            return ratio;
            #endif
            return 1;
         }

         float MipLevel(float2 uv, float2 textureSize)
         {
            #if _PERTEXUV
            uv *= textureSize;
            float2  dx_vtc        = ddx(uv);
            float2  dy_vtc        = ddy(uv);
            float delta_max_sqr = max(dot(dx_vtc, dx_vtc), dot(dy_vtc, dy_vtc));
            return 0.5 * log2(delta_max_sqr);
            #endif
            return 0;
         }

         #if _PERTEXUV
            #define MEGASPLAT_SAMPLE(TA, uv, l) UNITY_SAMPLE_TEX2DARRAY_LOD(TA, uv, l)
         #else
            #define MEGASPLAT_SAMPLE(TA, uv, l) UNITY_SAMPLE_TEX2DARRAY(TA, uv)
         #endif


         #if _DISTANCERESAMPLE
            #if _TRIPLANAR && !_FLOW && !_FLOWREFRACTION
               #define SAMPLETEXARRAY(t0, t1, t2, TA, lp, l) \
                  t0  = tpw.x * MEGASPLAT_SAMPLE(TA, lp.tpuv0_x, l) + tpw.y * MEGASPLAT_SAMPLE(TA, lp.tpuv0_y, l) + tpw.z * MEGASPLAT_SAMPLE(TA, lp.tpuv0_z, l); \
                  t1  = tpw.x * MEGASPLAT_SAMPLE(TA, lp.tpuv1_x, l) + tpw.y * MEGASPLAT_SAMPLE(TA, lp.tpuv1_y, l) + tpw.z * MEGASPLAT_SAMPLE(TA, lp.tpuv1_z, l); \
                  t2  = tpw.x * MEGASPLAT_SAMPLE(TA, lp.tpuv2_x, l) + tpw.y * MEGASPLAT_SAMPLE(TA, lp.tpuv2_y, l) + tpw.z * MEGASPLAT_SAMPLE(TA, lp.tpuv2_z, l); \
                  { \
                     lp.resampleMip = MipLevel(lp.mipUV * _ResampleDistanceParams.xx, _Diffuse_TexelSize.zw);\
                     half4 st0  = tpw.x * MEGASPLAT_SAMPLE(TA, lp.db_tpuv0_x, lp.resampleMip) + tpw.y * MEGASPLAT_SAMPLE(TA, lp.db_tpuv0_y, lp.resampleMip) + tpw.z * MEGASPLAT_SAMPLE(TA, lp.db_tpuv0_z, lp.resampleMip); \
                     half4 st1  = tpw.x * MEGASPLAT_SAMPLE(TA, lp.db_tpuv1_x, lp.resampleMip) + tpw.y * MEGASPLAT_SAMPLE(TA, lp.db_tpuv1_y, lp.resampleMip) + tpw.z * MEGASPLAT_SAMPLE(TA, lp.db_tpuv1_z, lp.resampleMip); \
                     half4 st2  = tpw.x * MEGASPLAT_SAMPLE(TA, lp.db_tpuv2_x, lp.resampleMip) + tpw.y * MEGASPLAT_SAMPLE(TA, lp.db_tpuv2_y, lp.resampleMip) + tpw.z * MEGASPLAT_SAMPLE(TA, lp.db_tpuv2_z, lp.resampleMip); \
                     t0 = lerp(t0, st0, lp.distanceBlend); \
                     t1 = lerp(t1, st1, lp.distanceBlend); \
                     t2 = lerp(t2, st2, lp.distanceBlend); \
                  }
            #else
               #if _FLOW || _FLOWREFRACTION
                  #define SAMPLETEXARRAY(t0, t1, t2, TA, lp, l) \
                  lp.resampleMip = MipLevel(lp.mipUV * _ResampleDistanceParams.xx, _Diffuse_TexelSize.zw);\
                  if (lp.flowOn > 0) \
                  { \
                     t0 = lerp(MEGASPLAT_SAMPLE(TA, lp.fuv0a, l), MEGASPLAT_SAMPLE(TA, lp.fuv0b, lp.resampleMip), lp.flowInterps.x); \
                     t1 = lerp(MEGASPLAT_SAMPLE(TA, lp.fuv1a, l), MEGASPLAT_SAMPLE(TA, lp.fuv1b, lp.resampleMip), lp.flowInterps.y); \
                     t2 = lerp(MEGASPLAT_SAMPLE(TA, lp.fuv2a, l), MEGASPLAT_SAMPLE(TA, lp.fuv2b, lp.resampleMip), lp.flowInterps.z); \
                  } \
                  else \
                  { \
                     t0 = lerp(MEGASPLAT_SAMPLE(TA, lp.uv0, l), MEGASPLAT_SAMPLE(TA, lp.db_uv0, lp.resampleMip), lp.distanceBlend); \
                     t1 = lerp(MEGASPLAT_SAMPLE(TA, lp.uv1, l), MEGASPLAT_SAMPLE(TA, lp.db_uv1, lp.resampleMip), lp.distanceBlend); \
                     t2 = lerp(MEGASPLAT_SAMPLE(TA, lp.uv2, l), MEGASPLAT_SAMPLE(TA, lp.db_uv2, lp.resampleMip), lp.distanceBlend); \
                  }
               #else
                  #define SAMPLETEXARRAY(t0, t1, t2, TA, lp, l) \
                     lp.resampleMip = MipLevel(lp.mipUV * _ResampleDistanceParams.xx, _Diffuse_TexelSize.zw);\
                     t0 = lerp(MEGASPLAT_SAMPLE(TA, lp.uv0, l), MEGASPLAT_SAMPLE(TA, lp.db_uv0, lp.resampleMip), lp.distanceBlend); \
                     t1 = lerp(MEGASPLAT_SAMPLE(TA, lp.uv1, l), MEGASPLAT_SAMPLE(TA, lp.db_uv1, lp.resampleMip), lp.distanceBlend); \
                     t2 = lerp(MEGASPLAT_SAMPLE(TA, lp.uv2, l), MEGASPLAT_SAMPLE(TA, lp.db_uv2, lp.resampleMip), lp.distanceBlend); 
               #endif
            #endif
         #else // not distance resample
            #if _TRIPLANAR && !_FLOW && !_FLOWREFRACTION
               #define SAMPLETEXARRAY(t0, t1, t2, TA, lp, l) \
                  t0  = tpw.x * MEGASPLAT_SAMPLE(TA, lp.tpuv0_x, l) + tpw.y * MEGASPLAT_SAMPLE(TA, lp.tpuv0_y, l) + tpw.z * MEGASPLAT_SAMPLE(TA, lp.tpuv0_z, l); \
                  t1  = tpw.x * MEGASPLAT_SAMPLE(TA, lp.tpuv1_x, l) + tpw.y * MEGASPLAT_SAMPLE(TA, lp.tpuv1_y, l) + tpw.z * MEGASPLAT_SAMPLE(TA, lp.tpuv1_z, l); \
                  t2  = tpw.x * MEGASPLAT_SAMPLE(TA, lp.tpuv2_x, l) + tpw.y * MEGASPLAT_SAMPLE(TA, lp.tpuv2_y, l) + tpw.z * MEGASPLAT_SAMPLE(TA, lp.tpuv2_z, l);
            #else
               #if _FLOW || _FLOWREFRACTION
                  #define SAMPLETEXARRAY(t0, t1, t2, TA, lp, l) \
                  if (lp.flowOn > 0) \
                  { \
                     t0 = lerp(MEGASPLAT_SAMPLE(TA, lp.fuv0a, l), MEGASPLAT_SAMPLE(TA, lp.fuv0b, l), lp.flowInterps.x); \
                     t1 = lerp(MEGASPLAT_SAMPLE(TA, lp.fuv1a, l), MEGASPLAT_SAMPLE(TA, lp.fuv1b, l), lp.flowInterps.y); \
                     t2 = lerp(MEGASPLAT_SAMPLE(TA, lp.fuv2a, l), MEGASPLAT_SAMPLE(TA, lp.fuv2b, l), lp.flowInterps.z); \
                  } \
                  else \
                  { \
                     t0 = MEGASPLAT_SAMPLE(TA, lp.uv0, l); \
                     t1 = MEGASPLAT_SAMPLE(TA, lp.uv1, l); \
                     t2 = MEGASPLAT_SAMPLE(TA, lp.uv2, l); \
                  }
               #else
                  #define SAMPLETEXARRAY(t0, t1, t2, TA, lp, l) \
                     t0 = MEGASPLAT_SAMPLE(TA, lp.uv0, l); \
                     t1 = MEGASPLAT_SAMPLE(TA, lp.uv1, l); \
                     t2 = MEGASPLAT_SAMPLE(TA, lp.uv2, l);
               #endif
            #endif
         #endif


         #if _TRIPLANAR && !_FLOW && !_FLOWREFRACTION
            #define SAMPLETEXARRAYLOD(t0, t1, t2, TA, lp, lod) \
               t0  = tpw.x * UNITY_SAMPLE_TEX2DARRAY_LOD(TA, lp.tpuv0_x, lod) + tpw.y * UNITY_SAMPLE_TEX2DARRAY_LOD(TA, lp.tpuv0_y, lod) + tpw.z * UNITY_SAMPLE_TEX2DARRAY_LOD(TA, lp.tpuv0_z, lod); \
               t1  = tpw.x * UNITY_SAMPLE_TEX2DARRAY_LOD(TA, lp.tpuv1_x, lod) + tpw.y * UNITY_SAMPLE_TEX2DARRAY_LOD(TA, lp.tpuv1_y, lod) + tpw.z * UNITY_SAMPLE_TEX2DARRAY_LOD(TA, lp.tpuv1_z, lod); \
               t2  = tpw.x * UNITY_SAMPLE_TEX2DARRAY_LOD(TA, lp.tpuv2_x, lod) + tpw.y * UNITY_SAMPLE_TEX2DARRAY_LOD(TA, lp.tpuv2_y, lod) + tpw.z * UNITY_SAMPLE_TEX2DARRAY_LOD(TA, lp.tpuv2_z, lod);
         #else
            #define SAMPLETEXARRAYLOD(t0, t1, t2, TA, lp, lod) \
               t0 = UNITY_SAMPLE_TEX2DARRAY_LOD(TA, lp.uv0, lod); \
               t1 = UNITY_SAMPLE_TEX2DARRAY_LOD(TA, lp.uv1, lod); \
               t2 = UNITY_SAMPLE_TEX2DARRAY_LOD(TA, lp.uv2, lod);
         #endif

         #if _TRIPLANAR && !_FLOW && !_FLOWREFRACTION
            #define SAMPLETEXARRAYLODOFFSET(t0, t1, t2, TA, lp, lod, offset) \
               t0  = tpw.x * UNITY_SAMPLE_TEX2DARRAY_LOD(TA, lp.tpuv0_x + offset, lod) + tpw.y * UNITY_SAMPLE_TEX2DARRAY_LOD(TA, lp.tpuv0_y + offset, lod) + tpw.z * UNITY_SAMPLE_TEX2DARRAY_LOD(TA, lp.tpuv0_z + offset, lod); \
               t1  = tpw.x * UNITY_SAMPLE_TEX2DARRAY_LOD(TA, lp.tpuv1_x + offset, lod) + tpw.y * UNITY_SAMPLE_TEX2DARRAY_LOD(TA, lp.tpuv1_y + offset, lod) + tpw.z * UNITY_SAMPLE_TEX2DARRAY_LOD(TA, lp.tpuv1_z + offset, lod); \
               t2  = tpw.x * UNITY_SAMPLE_TEX2DARRAY_LOD(TA, lp.tpuv2_x + offset, lod) + tpw.y * UNITY_SAMPLE_TEX2DARRAY_LOD(TA, lp.tpuv2_y + offset, lod) + tpw.z * UNITY_SAMPLE_TEX2DARRAY_LOD(TA, lp.tpuv2_z + offset, lod);
         #else
            #define SAMPLETEXARRAYLODOFFSET(t0, t1, t2, TA, lp, lod, offset) \
               t0 = UNITY_SAMPLE_TEX2DARRAY_LOD(TA, lp.uv0 + offset, lod); \
               t1 = UNITY_SAMPLE_TEX2DARRAY_LOD(TA, lp.uv1 + offset, lod); \
               t2 = UNITY_SAMPLE_TEX2DARRAY_LOD(TA, lp.uv2 + offset, lod);
         #endif

         void Flow(float3 uv, half2 flow, half speed, float intensity, out float3 uv1, out float3 uv2, out half interp)
         {
            float2 flowVector = (flow * 2.0 - 1.0) * intensity;
            
            float timeScale = _Time.y * speed;
            float2 phase = frac(float2(timeScale, timeScale + .5));

            uv1.xy = (uv.xy - flowVector * half2(phase.x, phase.x));
            uv2.xy = (uv.xy - flowVector * half2(phase.y, phase.y));
            uv1.z = uv.z;
            uv2.z = uv.z;

            interp = abs(0.5 - phase.x) / 0.5;
         }

         void Flow(float2 uv, half2 flow, half speed, float intensity, out float2 uv1, out float2 uv2, out half interp)
         {
            float2 flowVector = (flow * 2.0 - 1.0) * intensity;
            
            float timeScale = _Time.y * speed;
            float2 phase = frac(float2(timeScale, timeScale + .5));

            uv1.xy = (uv.xy - flowVector * half2(phase.x, phase.x));
            uv2.xy = (uv.xy - flowVector * half2(phase.y, phase.y));

            interp = abs(0.5 - phase.x) / 0.5;
         }

         half3 ComputeWeights(half3 iWeights, half4 tex0, half4 tex1, half4 tex2, half contrast)
         {
             // compute weight with height map
             const half epsilon = 1.0f / 1024.0f;
             half3 weights = half3(iWeights.x * (tex0.a + epsilon), 
                                      iWeights.y * (tex1.a + epsilon),
                                      iWeights.z * (tex2.a + epsilon));

             // Contrast weights
             half maxWeight = max(weights.x, max(weights.y, weights.z));
             half transition = contrast * maxWeight;
             half threshold = maxWeight - transition;
             half scale = 1.0f / transition;
             weights = saturate((weights - threshold) * scale);
             // Normalize weights.
             half weightScale = 1.0f / (weights.x + weights.y + weights.z);
             weights *= weightScale;
             #if _LINEARBIAS
             weights = lerp(weights, iWeights, contrast);
             #endif
             return weights;
         }

         float2 ProjectUVs(float3 vertex, float2 coords, float3 worldPos, float3 worldNormal, inout float4 tangent)
         {
            #if _UVLOCALTOP
            return (vertex.xz * _UVProjectOffsetScale.zw) + _UVProjectOffsetScale.xy;
            #elif _UVLOCALSIDE
            return (vertex.zy * _UVProjectOffsetScale.zw) + _UVProjectOffsetScale.xy;
            #elif _UVLOCALFRONT
            return (vertex.xy * _UVProjectOffsetScale.zw) + _UVProjectOffsetScale.xy;
            #elif _UVWORLDTOP
               #if _PROJECTTANGENTS
               tangent.xyz = mul((float3x3)unity_WorldToObject, float3(1,0,0));
               tangent.w = worldNormal.y > 0 ? -1 : 1;
               #endif
            return (worldPos.xz * _UVProjectOffsetScale.zw) + _UVProjectOffsetScale.xy;
            #elif _UVWORLDFRONT
               #if _PROJECTTANGENTS
               tangent.xyz = mul((float3x3)unity_WorldToObject, float3(0,0,1));
               tangent.w = worldNormal.x > 0 ? 1 : -1;
               #endif
            return (worldPos.zy * _UVProjectOffsetScale.zw) + _UVProjectOffsetScale.xy;
            #elif _UVWORLDSIDE
               #if _PROJECTTANGENTS
               tangent.xyz = mul((float3x3)unity_WorldToObject, float3(0,1,0));
               tangent.w = worldNormal.z > 0 ? 1 : -1;
               #endif
            return (worldPos.xy * _UVProjectOffsetScale.zw) + _UVProjectOffsetScale.xy;
            #endif
            return coords;
         }

         float2 ProjectUV2(float3 vertex, float2 coords, float3 worldPos, float3 worldNormal, inout float4 tangent)
         {
            #if _UVLOCALTOP2
            return (vertex.xz * _UVProjectOffsetScale2.zw) + _UVProjectOffsetScale2.xy;
            #elif _UVLOCALSIDE2
            return (vertex.zy * _UVProjectOffsetScale2.zw) + _UVProjectOffsetScale2.xy;
            #elif _UVLOCALFRONT2
            return (vertex.xy * _UVProjectOffsetScale2.zw) + _UVProjectOffsetScale2.xy;
            #elif _UVWORLDTOP2
            return (worldPos.xz * _UVProjectOffsetScale2.zw) + _UVProjectOffsetScale2.xy;
            #elif _UVWORLDFRONT2
            return (worldPos.zy * _UVProjectOffsetScale2.zw) + _UVProjectOffsetScale2.xy;
            #elif _UVWORLDSIDE2
            return (worldPos.xy * _UVProjectOffsetScale2.zw) + _UVProjectOffsetScale2.xy;
            #endif
            return coords;
         }

         void WaterBRDF (inout half3 Albedo, inout half Smoothness, half metalness, half wetFactor, half surfPorosity) 
         {
            #if _PUDDLES || _PUDDLEFLOW || _PUDDLEREFRACT || _WETNESS
            half porosity = saturate((( (1 - Smoothness) - 0.5)) / max(surfPorosity, 0.001));
            half factor = lerp(1, 0.2, (1 - metalness) * porosity);
            Albedo *= lerp(1.0, factor, wetFactor);
            Smoothness = lerp(1.0, Smoothness, lerp(1.0, factor, wetFactor));
            #endif
         }

         #if _RAINDROPS
         float3 ComputeRipple(float2 uv, float time, float weight)
         {
            float4 ripple = tex2D(_RainDropTexture, uv);
            ripple.yz = ripple.yz * 2 - 1;

            float dropFrac = frac(ripple.w + time);
            float timeFrac = dropFrac - 1.0 + ripple.x;
            float dropFactor = saturate(0.2f + weight * 0.8 - dropFrac);
            float finalFactor = dropFactor * ripple.x * 
                                 sin( clamp(timeFrac * 9.0f, 0.0f, 3.0f) * 3.14159265359);

            return float3(ripple.yz * finalFactor * 0.35f, 1.0f);
         }
         #endif

         // water normal only
         half2 DoPuddleRefract(float3 waterNorm, half puddleLevel, float2 flowDir, half height)
         {
            #if _PUDDLEFLOW || _PUDDLEREFRACT
            puddleLevel *= _MaxPuddles;
            float waterBlend = saturate((puddleLevel - height) * _PuddleBlend);
            waterBlend *= waterBlend;

            waterNorm.xy *= puddleLevel * waterBlend;
               #if _PUDDLEDEPTHDAMPEN
               return lerp(waterNorm.xy, waterNorm.xy * height, _PuddleFlowParams.w);
               #endif
            return waterNorm.xy;

            #endif
            return half2(0,0);
         }

         // modity lighting terms for water..
         float DoPuddles(inout MegaSplatLayer o, float2 uv, half3 waterNormFoam, half puddleLevel, half2 flowDir, half porosity, float3 worldNormal)
         {
            #if _PUDDLES || _PUDDLEFLOW || _PUDDLEREFRACT
            puddleLevel *= _MaxPuddles;
            float waterBlend = saturate((puddleLevel - o.Height) * _PuddleBlend);

            half3 waterNorm = half3(0,0,1);
            #if _PUDDLEFLOW || _PUDDLEREFRACT

               waterNorm = half3(waterNormFoam.x, waterNormFoam.y, sqrt(1 - saturate(dot(waterNormFoam.xy, waterNormFoam.xy))));

               #if _PUDDLEFOAM
               half pmh = puddleLevel - o.Height;
               // refactor to compute flow UVs in previous step?
               float2 foamUV0;
               float2 foamUV1;
               half foamInterp;
               Flow(uv * 1.75 + waterNormFoam.xy * waterNormFoam.b, flowDir, _PuddleFlowParams.y/3, _PuddleFlowParams.z/3, foamUV0, foamUV1, foamInterp);
               half foam0 = tex2D(_PuddleNormal, foamUV0).b;
               half foam1 = tex2D(_PuddleNormal, foamUV1).b;
               half foam = lerp(foam0, foam1, foamInterp);
               foam = foam * abs(pmh) + (foam * o.Height);
               foam *= 1.0 - (saturate(pmh * 1.5));
               foam *= foam;
               foam *= _PuddleNormalFoam.y;
               #endif // foam
            #endif // flow, refract

            half3 wetAlbedo = o.Albedo * _PuddleTint * 2;
            half wetSmoothness = o.Smoothness;

            WaterBRDF(wetAlbedo, wetSmoothness, o.Metallic, waterBlend, porosity);

            #if _RAINDROPS
               float dropStrength = _RainIntensity;
               #if _RAINDROPFLATONLY
               dropStrength = saturate(dot(float3(0,1,0), worldNormal));
               #endif
               const float4 timeMul = float4(1.0f, 0.85f, 0.93f, 1.13f); 
               float4 timeAdd = float4(0.0f, 0.2f, 0.45f, 0.7f);
               float4 times = _Time.yyyy;
               times = frac((times * float4(1, 0.85, 0.93, 1.13) + float4(0, 0.2, 0.45, 0.7)) * 1.6);

               float2 ruv1 = uv * _RainUVScales.xy;
               float2 ruv2 = ruv1;

               float4 weights = _RainIntensity.xxxx - float4(0, 0.25, 0.5, 0.75);
               float3 ripple1 = ComputeRipple(ruv1 + float2( 0.25f,0.0f), times.x, weights.x);
               float3 ripple2 = ComputeRipple(ruv2 + float2(-0.55f,0.3f), times.y, weights.y);
               float3 ripple3 = ComputeRipple(ruv1 + float2(0.6f, 0.85f), times.z, weights.z);
               float3 ripple4 = ComputeRipple(ruv2 + float2(0.5f,-0.75f), times.w, weights.w);
               weights = saturate(weights * 4);

               float4 z = lerp(float4(1,1,1,1), float4(ripple1.z, ripple2.z, ripple3.z, ripple4.z), weights);
               float3 rippleNormal = float3( weights.x * ripple1.xy +
                           weights.y * ripple2.xy + 
                           weights.z * ripple3.xy + 
                           weights.w * ripple4.xy, 
                           z.x * z.y * z.z * z.w);

               waterNorm = lerp(waterNorm, normalize(rippleNormal+waterNorm), _RainIntensity * dropStrength);                         
            #endif

            #if _PUDDLEFOAM
            wetAlbedo += foam;
            wetSmoothness -= foam;
            #endif

            o.Normal = lerp(o.Normal, waterNorm, waterBlend * _PuddleNormalFoam.x);
            o.Occlusion = lerp(o.Occlusion, 1, waterBlend);
            o.Smoothness = lerp(o.Smoothness, wetSmoothness, waterBlend);
            o.Albedo = lerp(o.Albedo, wetAlbedo, waterBlend);
            return waterBlend;
            #endif
            return 0;
         }

         float DoLava(inout MegaSplatLayer o, float2 uv, half lavaLevel, half2 flowDir)
         {
            #if _LAVA

            half distortionSize = _LavaParams2.x;
            half distortionRate = _LavaParams2.y;
            half distortionScale = _LavaParams2.z;
            half darkening = _LavaParams2.w;
            half3 edgeColor = _LavaEdgeColor;
            half3 lavaColorLow = _LavaColorLow;
            half3 lavaColorHighlight = _LavaColorHighlight;


            half maxLava = _LavaParams.y;
            half lavaSpeed = _LavaParams.z;
            half lavaInterp = _LavaParams.w;

            lavaLevel *= maxLava;
            float lvh = lavaLevel - o.Height;
            float lavaBlend = saturate(lvh * _LavaParams.x);

            float2 uv1;
            float2 uv2;
            half interp;
            half drag = lerp(0.1, 1, saturate(lvh));
            Flow(uv, flowDir, lavaInterp, lavaSpeed * drag, uv1, uv2, interp);

            float2 dist_uv1;
            float2 dist_uv2;
            half dist_interp;
            Flow(uv * distortionScale, flowDir, distortionRate, distortionSize, dist_uv1, dist_uv2, dist_interp);

            half4 lavaDist = lerp(tex2D(_LavaDiffuse, dist_uv1*0.51), tex2D(_LavaDiffuse, dist_uv2), dist_interp);
            half4 dist = lavaDist * (distortionSize * 2) - distortionSize;

            half4 lavaTex = lerp(tex2D(_LavaDiffuse, uv1*1.1 + dist.xy), tex2D(_LavaDiffuse, uv2 + dist.zw), interp);

            lavaTex.xy = lavaTex.xy * 2 - 1;
            half3 lavaNorm = half3(lavaTex.xy, sqrt(1 - saturate(dot(lavaTex.xy, lavaTex.xy))));

            // base lava color, based on heights
            half3 lavaColor = lerp(lavaColorLow, lavaColorHighlight, lavaTex.b);

            // edges
            float lavaBlendWide = saturate((lavaLevel - o.Height) * _LavaParams.x * 0.5);
            float edge = saturate((1 - lavaBlendWide) * 3);

            // darkening
            darkening = saturate(lavaTex.a * darkening * saturate(lvh*2));
            lavaColor = lerp(lavaColor, lavaDist.bbb * 0.3, darkening);
            // edges
            lavaColor = lerp(lavaColor, edgeColor, edge);

            o.Albedo = lerp(o.Albedo, lavaColor, lavaBlend);
            o.Normal = lerp(o.Normal, lavaNorm, lavaBlend);
            o.Smoothness = lerp(o.Smoothness, 0.3, lavaBlend * darkening);

            half3 emis = lavaColor * lavaBlend;
            o.Emission = lerp(o.Emission, emis, lavaBlend);
            // bleed
            o.Emission += edgeColor * 0.3 * (saturate((lavaLevel*1.2 - o.Height) * _LavaParams.x) - lavaBlend);
            return lavaBlend;
            #endif
            return 0;
         }

         // no trig based hash, not a great hash, but fast..
         float Hash(float3 p)
         {
             p  = frac( p*0.3183099+.1 );
             p *= 17.0;
             return frac( p.x*p.y*p.z*(p.x+p.y+p.z) );
         }

         float Noise( float3 x )
         {
             float3 p = floor(x);
             float3 f = frac(x);
             f = f*f*(3.0-2.0*f);
            
             return lerp(lerp(lerp( Hash(p+float3(0,0,0)), Hash(p+float3(1,0,0)),f.x), lerp( Hash(p+float3(0,1,0)), Hash(p+float3(1,1,0)),f.x),f.y),
                       lerp(lerp(Hash(p+float3(0,0,1)), Hash(p+float3(1,0,1)),f.x), lerp(Hash(p+float3(0,1,1)), Hash(p+float3(1,1,1)),f.x),f.y),f.z);
         }

         // given 4 texture choices for each projection, return texture index based on normal
         // seems like we could remove some branching here.. hmm..
         float ProjectTexture(float3 worldPos, float3 normal, half3 threshFreq, half3 top, half3 side, half3 bottom)
         {
            half d = dot(normal, float3(0, 1, 0));
            half3 cvec = side;
            if (d < threshFreq.x)
            {
               cvec = bottom;
            }
            else if (d > threshFreq.y)
            {
               cvec = top;
            }

            float n = Noise(worldPos * threshFreq.z);
            if (n < 0.333)
               return cvec.x;
            else if (n < 0.666)
               return cvec.y;
            else
               return cvec.z;
         }

         void ProceduralTexture(float3 localPos, float3 worldPos, float3 normal, float3 worldNormal, inout float4 valuesMain, inout float4 valuesSecond, half3 weights)
         {
            #if _PROJECTTEXTURE_LOCAL
            half choice = ProjectTexture(localPos, normal, _ProjectTexThresholdFreq, _ProjectTexTop, _ProjectTexSide, _ProjectTexBottom);
            valuesMain.xyz = weights.rgb * choice;
            #endif
            #if _PROJECTTEXTURE_WORLD
            half choice = ProjectTexture(worldPos, worldNormal, _ProjectTexThresholdFreq, _ProjectTexTop, _ProjectTexSide, _ProjectTexBottom);
            valuesMain.xyz = weights.rgb * choice;
            #endif

            #if _PROJECTTEXTURE2_LOCAL
            half choice2 = ProjectTexture(localPos, normal, _ProjectTexThresholdFreq2, _ProjectTexTop2, _ProjectTexSide2, _ProjectTexBottom2);
            valuesSecond.xyz = weights.rgb * choice2;
            #endif
            #if _PROJECTTEXTURE2_WORLD
            half choice2 = ProjectTexture(worldPos, worldNormal, _ProjectTexThresholdFreq2, _ProjectTexTop2, _ProjectTexSide2, _ProjectTexBottom2);
            valuesSecond.xyz = weights.rgb * choice2;
            #endif

            #if _PROJECTTEXTURE2_LOCAL || _PROJECTTEXTURE2_WORLD
            float blendNoise = Noise(worldPos * _ProjectTexBlendParams.x) - 0.5;
            blendNoise *= _ProjectTexBlendParams.y;
            blendNoise += 0.5;
            blendNoise = min(max(blendNoise, _ProjectTexBlendParams.z), _ProjectTexBlendParams.w);
            valuesSecond.a = saturate(blendNoise);
            #endif
         }


         // manually compute barycentric coordinates
         float3 Barycentric(float2 p, float2 a, float2 b, float2 c)
         {
             float2 v0 = b - a;
             float2 v1 = c - a;
             float2 v2 = p - a;
             float d00 = dot(v0, v0);
             float d01 = dot(v0, v1);
             float d11 = dot(v1, v1);
             float d20 = dot(v2, v0);
             float d21 = dot(v2, v1);
             float denom = d00 * d11 - d01 * d01;
             float v = (d11 * d20 - d01 * d21) / denom;
             float w = (d00 * d21 - d01 * d20) / denom;
             float u = 1.0f - v - w;
             return float3(u, v, w);
         }

         // given two height values (from textures) and a height value for the current pixel (from vertex)
         // compute the blend factor between the two with a small blending area between them.
         half HeightBlend(half h1, half h2, half slope, half contrast)
         {
            h2 = 1 - h2;
            half tween = saturate((slope - min(h1, h2)) / max(abs(h1 - h2), 0.001)); 
            half blend = saturate( ( tween - (1-contrast) ) / max(contrast, 0.001));
            #if _LINEARBIAS
            blend = lerp(slope, blend, contrast);
            #endif
            return blend;
         }

         void BlendSpec(inout MegaSplatLayer base, MegaSplatLayer macro, half r, float3 albedo)
         {
            base.Metallic = lerp(base.Metallic, macro.Metallic, r);
            base.Smoothness = lerp(base.Smoothness, macro.Smoothness, r);
            base.Emission = albedo * lerp(base.Emission, macro.Emission, r);
         }

         half3 BlendOverlay(half3 base, half3 blend) { return (base < 0.5 ? (2.0 * base * blend) : (1.0 - 2.0 * (1.0 - base) * (1.0 - blend))); }
         half3 BlendMult2X(half3  base, half3 blend) { return (base * (blend * 2)); }


         MegaSplatLayer SampleDetail(half3 weights, float3 viewDir, inout LayerParams params, half3 tpw)
         {
            MegaSplatLayer o = (MegaSplatLayer)0;
            half4 tex0, tex1, tex2;
            half4 norm0, norm1, norm2;

            float mipLevel = MipLevel(params.mipUV, _Diffuse_TexelSize.zw);
            SAMPLETEXARRAY(tex0, tex1, tex2, _Diffuse, params, mipLevel);
            fixed4 albedo = tex0 * weights.x + tex1 * weights.y + tex2 * weights.z;

            #if _NOSPECNORMAL
            half4 norm = half4(0,0,1,1);
            #else
            mipLevel = MipLevel(params.mipUV, _Normal_TexelSize.zw);
            SAMPLETEXARRAY(norm0, norm1, norm2, _Normal, params, mipLevel);
               #if _PERTEXNORMALSTRENGTH
               norm0.ga *= params.normalStrength.x;
               norm1.ga *= params.normalStrength.y;
               norm2.ga *= params.normalStrength.z;
               #endif
            half4 norm = norm0 * weights.x + norm1 * weights.y + norm2 * weights.z;
            norm = norm.garb;
            #endif

            SAMPLESPEC(o, params);

            o.Emission = albedo.rgb * specFinal.z;
            o.Albedo = albedo.rgb;
            o.Height = albedo.a;
            o.Metallic = specFinal.x;
            o.Smoothness = specFinal.y;
            o.Occlusion = specFinal.w;

            return o;
         }


         float SampleLayerHeight(half3 biWeights, float3 viewDir, inout LayerParams params, half3 tpw, float lod, float contrast)
         { 
            #if _TESSDISTANCE || _TESSEDGE
               half4 tex0, tex1, tex2;

               SAMPLETEXARRAYLOD(tex0, tex1, tex2, _Diffuse, params, lod);
               half3 weights = ComputeWeights(biWeights, tex0, tex1, tex2, contrast);
               params.weights = weights;

               #if _TESSCENTERBIAS
                  tex0.a -= 0.5;
                  tex1.a -= 0.5;
                  tex2.a -= 0.5;
               #endif

               float off = (tex0.a * params.displacementScale.x * weights.x + 
                       tex1.a * params.displacementScale.y * weights.y + 
                       tex2.a * params.displacementScale.z * weights.z);

               #if _TESSCENTERBIAS
                  off += 0.5;
               #endif
               return off;
            #endif
            return 0.5;
         }

         #if _TESSDISTANCE || _TESSEDGE
         float4 MegaSplatDistanceBasedTess (float d0, float d1, float d2, float tess)
         {
            float3 f;
            f.x = clamp(d0, 0.01, 1.0) * tess;
            f.y = clamp(d1, 0.01, 1.0) * tess;
            f.z = clamp(d2, 0.01, 1.0) * tess;

            return UnityCalcTriEdgeTessFactors (f);
         }
         #endif

         #if _PUDDLEFLOW || _PUDDLEREFRACT
         half3 GetWaterNormal(float2 uv, float2 flowDir)
         {
            float2 uv1;
            float2 uv2;
            half interp;
            Flow(uv, flowDir, _PuddleFlowParams.y, _PuddleFlowParams.z, uv1, uv2, interp);

            half3 fd = lerp(tex2D(_PuddleNormal, uv1), tex2D(_PuddleNormal, uv2), interp).xyz;
            fd.xy = fd.xy * 2 - 1;
            return fd;
         }
         #endif

         MegaSplatLayer SampleLayer(inout LayerParams params, SplatInput si)
         { 
            half3 biWeights = si.weights;
            float3 viewDir = si.viewDir;
            half3 tpw = si.triplanarBlend;

            MegaSplatLayer o = (MegaSplatLayer)0;
            half4 tex0, tex1, tex2;
            half4 norm0, norm1, norm2;

            float mipLevel = MipLevel(params.mipUV, _Diffuse_TexelSize.zw);
            SAMPLETEXARRAY(tex0, tex1, tex2, _Diffuse, params, mipLevel);
            half3 weights = ComputeWeights(biWeights, tex0, tex1, tex2, params.contrast);
            params.weights = weights;
            fixed4 albedo = tex0 * weights.x + tex1 * weights.y + tex2 * weights.z;

            #if _PARALLAX || _PUDDLEREFRACT
               bool resample = false;
               #if _PUDDLEREFRACT

               resample = si.puddleHeight > 0 && si.camDist.y < 30;
               if (resample)
               {
                  float2 refractOffset = DoPuddleRefract(si.waterNormalFoam, si.puddleHeight, si.flowDir, albedo.a);
                  refractOffset *= _PuddleFlowParams.x;
                  params.uv0.xy += refractOffset;
                  params.uv1.xy += refractOffset;
                  params.uv2.xy += refractOffset;
               }
               #endif

               #if _PARALLAX
                  resample = resample || si.camDist.y < _Parallax.y*2;
                  float pamt = _Parallax.x * (1.0 - saturate((si.camDist.y - _Parallax.y) / _Parallax.y));
                  #if _PERTEXPARALLAXSTRENGTH
                  // can't really do per-tex, because that would require parallaxing each texture independently. So blend..
                  pamt *= (params.parallaxStrength.x * biWeights.x + params.parallaxStrength.y * biWeights.y + params.parallaxStrength.z * biWeights.z); 
                  #endif
                  float2 pOffset = ParallaxOffset (albedo.a, pamt, viewDir);
                  params.uv0.xy += pOffset;
                  params.uv1.xy += pOffset;
                  params.uv2.xy += pOffset;
               #endif

               //  resample
               if (resample)
               {
                  SAMPLETEXARRAY(tex0, tex1, tex2, _Diffuse, params, mipLevel);
                  weights = ComputeWeights(biWeights, tex0, tex1, tex2, params.contrast);
                  albedo = tex0 * weights.x + tex1 * weights.y + tex2 * weights.z;
               }
            #endif

            #if _ALPHA || _ALPHATEST
            half4 alpha0, alpha1, alpha2;
            mipLevel = MipLevel(params.mipUV, _AlphaArray_TexelSize.zw);
            SAMPLETEXARRAY(alpha0, alpha1, alpha2, _AlphaArray, params, mipLevel);
            o.Alpha = alpha0.r * weights.x + alpha1.r * weights.y + alpha2.r * weights.z;
            #endif

            #if _NOSPECNORMAL
            half4 norm = half4(0,0,1,1);
            #else
            mipLevel = MipLevel(params.mipUV, _Normal_TexelSize.zw);
            SAMPLETEXARRAY(norm0, norm1, norm2, _Normal, params, mipLevel);
               #if _PERTEXNORMALSTRENGTH
               norm0.ga -= 0.5; norm1.ga -= 0.5; norm2.ga -= 0.5;
               norm0.ga *= params.normalStrength.x;
               norm1.ga *= params.normalStrength.y;
               norm2.ga *= params.normalStrength.z;
               norm0.ga += 0.5; norm1.ga += 0.5; norm2.ga += 0.5;
               #endif
            
            half4 norm = norm0 * weights.x + norm1 * weights.y + norm2 * weights.z;
            norm = norm.garb;
            #endif


            SAMPLESPEC(o, params);
            o.Emission = albedo.rgb * specFinal.z;
            #if _EMISMAP
            half4 emis0, emis1, emis2;
            mipLevel = MipLevel(params.mipUV, _Emissive_TexelSize.zw);
            SAMPLETEXARRAY(emis0, emis1, emis2, _Emissive, params, mipLevel);
            half4 emis = emis0 * weights.x + emis1 * weights.y + emis2 * weights.z;
            o.Emission = emis.rgb;
            #endif

            o.Albedo = albedo.rgb;
            o.Height = albedo.a;
            o.Metallic = specFinal.x;
            o.Smoothness = specFinal.y;
            o.Occlusion = specFinal.w;

            #if _EMISMAP
            o.Metallic = emis.a;
            #endif

            #if _PERTEXAOSTRENGTH
            float aoStr = params.aoStrength.x * params.weights.x + params.aoStrength.y * params.weights.y + params.aoStrength.z * params.weights.z;
            o.Occlusion = AOContrast(o.Occlusion, aoStr);
            #endif
            return o;
         }


         MegaSplatLayer SampleLayerNoBlend(inout LayerParams params, SplatInput si)
         { 
            MegaSplatLayer o = (MegaSplatLayer)0;
            half4 tex0;
            half4 norm0 = half4(0,0,1,1);

            tex0 = UNITY_SAMPLE_TEX2DARRAY(_Diffuse, params.uv0); 


             #if _PARALLAX || _PUDDLEREFRACT
               bool resample = false;
               #if _PUDDLEREFRACT
  

               resample = si.puddleHeight > 0 && si.camDist.y < 30;
               if (resample)
               {
                  float2 refractOffset = DoPuddleRefract(si.waterNormalFoam, si.puddleHeight, si.flowDir, tex0.a);
                  refractOffset *= _PuddleFlowParams.x;
                  params.uv0.xy += refractOffset;
               }
               #endif

               #if _PARALLAX
                  resample = resample || si.camDist.y < _Parallax.y*2;
                  float pamt = _Parallax.x * (1.0 - saturate((si.camDist.y - _Parallax.y) / _Parallax.y));
                  #if _PERTEXPARALLAXSTRENGTH
                  // can't really do per-tex, because that would require parallaxing each texture independently. So blend..
                  pamt *= (params.parallaxStrength.x * si.weights.x + params.parallaxStrength.y * si.weights.y + params.parallaxStrength.z * si.weights.z); 
                  #endif
                  float2 pOffset = ParallaxOffset (tex0.a, pamt, si.viewDir);
                  params.uv0.xy += pOffset;
               #endif

               //  resample
               if (resample)
               {
                  tex0 = UNITY_SAMPLE_TEX2DARRAY(_Diffuse, params.uv0); 
               }
            #endif

            #if _ALPHA || _ALPHATEST
            half4 alpha0 = UNITY_SAMPLE_TEX2DARRAY(_AlphaArray, params.uv0); 
            o.Alpha = alpha0.r;
            #endif


            o.Metallic = params.metallic.x;
            o.Smoothness = params.smoothness.x;
            o.Normal = half3(0,0,1);

            #if !_NOSPECNORMAL
               norm0 = UNITY_SAMPLE_TEX2DARRAY(_Normal, params.uv0); 
               norm0 = norm0.garb;
               #if _PERTEXNORMALSTRENGTH
               norm0.xy -= 0.5;
               norm0.xy *= params.normalStrength.x;
               norm0.xy += 0.5;
               #endif
               norm0.xy *= 2;
               norm0.xy -= 1;
               o.Normal = half3(norm0.x, norm0.y, sqrt(1 - saturate(dot(norm0.xy, norm0.xy))));
               o.Smoothness = norm0.z;
               o.Occlusion = norm0.w;
            #endif

            o.Albedo = tex0.rgb;
            o.Height = tex0.a;


            #if _EMISMAP
               half4 emismetal = UNITY_SAMPLE_TEX2DARRAY(_Emissive, params.uv0);
               o.Emission = emismetal.xyz;
               o.Metallic = emismetal.w;
            #endif

            #if _PERTEXAOSTRENGTH
               float aoStr = params.aoStrength.x;
               o.Occlusion = AOContrast(o.Occlusion, aoStr);
            #endif

            return o;
         }

         MegaSplatLayer BlendResults(MegaSplatLayer a, MegaSplatLayer b, half r)
         {
            a.Height = lerp(a.Height, b.Height, r);
            a.Albedo = lerp(a.Albedo, b.Albedo, r);
            #if !_NOSPECNORMAL
            a.Normal = lerp(a.Normal, b.Normal, r);
            #endif
            a.Metallic = lerp(a.Metallic, b.Metallic, r);
            a.Smoothness = lerp(a.Smoothness, b.Smoothness, r);
            a.Occlusion = lerp(a.Occlusion, b.Occlusion, r);
            a.Emission = lerp(a.Emission, b.Emission, r);
            #if _ALPHA || _ALPHATEST
            a.Alpha = lerp(a.Alpha, b.Alpha, r);
            #endif
            return a;
         }

         MegaSplatLayer OverlayResults(MegaSplatLayer splats, MegaSplatLayer macro, half r)
         {
            #if !_SPLATSONTOP
            r = 1 - r;
            #endif

            #if _ALPHA || _ALPHATEST
            splats.Alpha = min(macro.Alpha, splats.Alpha);
            #endif

            #if _MACROMULT2X
               splats.Albedo = lerp(BlendMult2X(macro.Albedo, splats.Albedo), splats.Albedo, r);
               #if !_NOSPECNORMAL
               splats.Normal = lerp(BlendNormals(macro.Normal, splats.Normal), splats.Normal, r); 
               #endif
               splats.Occlusion = lerp((macro.Occlusion + splats.Occlusion) * 0.5, splats.Occlusion, r);
               BlendSpec(macro, splats, r, splats.Albedo);
            #elif _MACROOVERLAY
               splats.Albedo = lerp(BlendOverlay(macro.Albedo, splats.Albedo), splats.Albedo, r);
               #if !_NOSPECNORMAL
               splats.Normal  = lerp(BlendNormals(macro.Normal, splats.Normal), splats.Normal, r); 
               #endif
               splats.Occlusion = lerp((macro.Occlusion + splats.Occlusion) * 0.5, splats.Occlusion, r);
               BlendSpec(macro, splats, r, splats.Albedo);
            #elif _MACROMULT
               splats.Albedo = lerp(splats.Albedo * macro.Albedo, splats.Albedo, r);
               #if !_NOSPECNORMAL
               splats.Normal  = lerp(BlendNormals(macro.Normal, splats.Normal), splats.Normal, r); 
               #endif
               splats.Occlusion = lerp((macro.Occlusion + splats.Occlusion) * 0.5, splats.Occlusion, r);
               BlendSpec(macro, splats, r, splats.Albedo);
            #else
               splats.Albedo = lerp(macro.Albedo, splats.Albedo, r);
               #if !_NOSPECNORMAL
               splats.Normal  = lerp(macro.Normal, splats.Normal, r); 
               #endif
               splats.Occlusion = lerp(macro.Occlusion, splats.Occlusion, r);
               BlendSpec(macro, splats, r, splats.Albedo);
            #endif

            return splats;
         }

         MegaSplatLayer BlendDetail(MegaSplatLayer splats, MegaSplatLayer detail, float detailBlend)
         {
            #if _DETAILMAP
            detailBlend *= _DetailTextureStrength;
            #endif
            #if _DETAILMULT2X
               splats.Albedo = lerp(detail.Albedo, BlendMult2X(splats.Albedo, detail.Albedo), detailBlend);
            #elif _DETAILOVERLAY
               splats.Albedo = lerp(detail.Albedo, BlendOverlay(splats.Albedo, detail.Albedo), detailBlend);
            #elif _DETAILMULT
               splats.Albedo = lerp(detail.Albedo, splats.Albedo * detail.Albedo, detailBlend);
            #else
               splats.Albedo = lerp(detail.Albedo, splats.Albedo, detailBlend);
            #endif 

            #if !_NOSPECNORMAL
            splats.Normal = lerp(splats.Normal, BlendNormals(splats.Normal, detail.Normal), detailBlend);
            #endif
            return splats;
         }

         float DoSnowDisplace(float splat_height, float2 uv, float3 worldNormal, half snowHeightFade, float puddleHeight)
         {
            // could force a branch and avoid texsamples
            #if _SNOW
            uv *= _SnowUVScales.xy;
            half4 snowAlb = tex2D(_SnowDiff, uv);
            half4 snowNsao = tex2D(_SnowNormal, uv);

            float snowAmount, wetnessMask, snowNormalAmount;
            float snowFade = saturate((_SnowAmount - puddleHeight) * snowHeightFade);

            float height = splat_height * _SnowParams.x;
            float erosion = lerp(0, height, _SnowParams.y);
            float snowMask = saturate((snowFade - erosion));
            float snowMask2 = saturate((snowFade - erosion) * 8);
            snowMask *= snowMask * snowMask * snowMask * snowMask * snowMask2;
            snowAmount = snowMask * saturate(dot(worldNormal, _SnowUpVector));

            return snowAmount;
            #endif
            return 0;
         }

         float DoSnow(inout MegaSplatLayer o, float2 uv, float3 worldNormal, half snowHeightFade, float puddleHeight, half surfPorosity, float camDist)
         {
            // could force a branch and avoid texsamples
            #if _SNOW
            uv *= _SnowUVScales.xy;
            half4 snowAlb = tex2D(_SnowDiff, uv);
            half4 snowNsao = tex2D(_SnowNormal, uv);

            #if _SNOWDISTANCERESAMPLE
            float2 snowResampleUV = uv * _SnowDistanceResampleScaleStrengthFade.x;

            if (camDist > _SnowDistanceResampleScaleStrengthFade.z)
               {
                  half4 snowAlb2 = tex2D(_SnowDiff, snowResampleUV);
                  half4 snowNsao2 = tex2D(_SnowNormal, snowResampleUV);
                  float fade = saturate ((camDist - _SnowDistanceResampleScaleStrengthFade.z) / _SnowDistanceResampleScaleStrengthFade.w);
                  fade *= _SnowDistanceResampleScaleStrengthFade.y;

                  snowAlb.rgb = lerp(snowAlb, snowAlb2, fade);
                  snowNsao = lerp(snowNsao, snowNsao2, fade);
               }
            #endif

            #if _SNOWDISTANCENOISE
               float2 snowNoiseUV = uv * _SnowDistanceNoiseScaleStrengthFade.x;
               if (camDist > _SnowDistanceNoiseScaleStrengthFade.z)
               {
                  half4 noise = tex2D(_SnowDistanceNoise, uv * _SnowDistanceNoiseScaleStrengthFade.x);
                  float fade = saturate ((camDist - _SnowDistanceNoiseScaleStrengthFade.z) / _SnowDistanceNoiseScaleStrengthFade.w);
                  fade *= _SnowDistanceNoiseScaleStrengthFade.y;

                  snowAlb.rgb = lerp(snowAlb.rgb, BlendMult2X(snowAlb.rgb, noise.zzz), fade);
                  noise *= 0.5;
                  #if !_NOSPECNORMAL
                  snowNsao.xy += ((noise.xy-0.25) * fade);
                  #endif
               }
            #endif



            half3 snowNormal = half3(snowNsao.xy * 2 - 1, 1);
            snowNormal.z = sqrt(1 - saturate(dot(snowNormal.xy, snowNormal.xy)));

            float snowAmount, wetnessMask, snowNormalAmount;
            float snowFade = saturate((_SnowAmount - puddleHeight) * snowHeightFade);
            float ao = o.Occlusion;
            if (snowFade > 0)
            {
               float height = o.Height * _SnowParams.x;
               float erosion = lerp(1-ao, (height + ao) * 0.5, _SnowParams.y);
               float snowMask = saturate((snowFade - erosion) * 8);
               snowMask *= snowMask * snowMask * snowMask;
               snowAmount = snowMask * saturate(dot(worldNormal, _SnowUpVector));  // up
               wetnessMask = saturate((_SnowParams.w * (4.0 * snowFade) - (height + snowNsao.b) * 0.5));
               snowAmount = saturate(snowAmount * 8);
               snowNormalAmount = snowAmount * snowAmount;

               float porosity = saturate((((1.0 - o.Smoothness) - 0.5)) / max(surfPorosity, 0.001));
               float factor = lerp(1, 0.4, porosity);

               o.Albedo *= lerp(1.0, factor, wetnessMask);
               o.Normal = lerp(o.Normal, float3(0,0,1), wetnessMask);
               o.Smoothness = lerp(o.Smoothness, 0.8, wetnessMask);

            }
            o.Albedo = lerp(o.Albedo, snowAlb.rgb, snowAmount);
            o.Normal = lerp(o.Normal, snowNormal, snowNormalAmount);
            o.Smoothness = lerp(o.Smoothness, (snowNsao.b) * _SnowParams.z, snowAmount);
            o.Occlusion = lerp(o.Occlusion, snowNsao.w, snowAmount);
            o.Height = lerp(o.Height, snowAlb.a, snowAmount);
            o.Metallic = lerp(o.Metallic, 0.01, snowAmount);
            float crystals = saturate(0.65 - snowNsao.b);
            o.Smoothness = lerp(o.Smoothness, crystals * _SnowParams.z, snowAmount);
            return snowAmount;
            #endif
            return 0;
         }

         half4 LightingUnlit(SurfaceOutput s, half3 lightDir, half atten)
         {
            return half4(s.Albedo, 1);
         }

         #if _RAMPLIGHTING
         half3 DoLightingRamp(half3 albedo, float3 normal, float3 emission, half3 lightDir, half atten)
         {
            half NdotL = dot (normal, lightDir);
            half diff = NdotL * 0.5 + 0.5;
            half3 ramp = tex2D (_Ramp, diff.xx).rgb;
            return (albedo * _LightColor0.rgb * ramp * atten) + emission;
         }

         half4 LightingRamp (SurfaceOutput s, half3 lightDir, half atten) 
         {
            half4 c;
            c.rgb = DoLightingRamp(s.Albedo, s.Normal, s.Emission, lightDir, atten);
            c.a = s.Alpha;
            return c;
         }
         #endif

         void ApplyDetailNoise(inout half3 albedo, inout half3 norm, inout half smoothness, in LayerParams data, float camDist, float3 tpw)
         {
            #if _DETAILNOISE
            {
               #if _TRIPLANAR
               float2 uv0 = data.tpuv0_x.xy * _DetailNoiseScaleStrengthFade.x;
               float2 uv1 = data.tpuv1_y.xy * _DetailNoiseScaleStrengthFade.x;
               float2 uv2 = data.tpuv2_z.xy * _DetailNoiseScaleStrengthFade.x;
               #else
               float2 uv = data.uv0.xy * _DetailNoiseScaleStrengthFade.x;
               #endif


               if (camDist < _DetailNoiseScaleStrengthFade.z)
               {
                  #if _TRIPLANAR
                  half3 noise = (tex2D(_DetailNoise, uv0) * tpw.x + 
                     tex2D(_DetailNoise, uv1) * tpw.y + 
                     tex2D(_DetailNoise, uv2) * tpw.z).rgb; 
                  #else
                  half3 noise = tex2D(_DetailNoise, uv).rgb;
                  #endif

                  float fade = 1.0 - ((_DetailNoiseScaleStrengthFade.z - camDist) / _DetailNoiseScaleStrengthFade.z);
                  fade = 1.0 - (fade*fade);
                  fade *= _DetailNoiseScaleStrengthFade.y;

                  #if _PERTEXNOISESTRENGTH
                  fade *= (data.detailNoiseStrength.x * data.weights.x + data.detailNoiseStrength.y * data.weights.y + data.detailNoiseStrength.z * data.weights.z);
                  #endif

                  albedo = lerp(albedo, BlendMult2X(albedo, noise.zzz), fade);
                  noise *= 0.5;
                  #if !_NOSPECNORMAL
                  norm.xy += ((noise.xy-0.25) * fade);
                  #endif
                  #if !_NOSPECNORMAL || _NOSPECTEX
                  smoothness += (abs(noise.x-0.25) * fade);
                  #endif
               }

            }
            #endif // detail normal

            #if _DISTANCENOISE
            {
               #if _TRIPLANAR
               float2 uv0 = data.tpuv0_x.xy * _DistanceNoiseScaleStrengthFade.x;
               float2 uv1 = data.tpuv0_y.xy * _DistanceNoiseScaleStrengthFade.x;
               float2 uv2 = data.tpuv1_z.xy * _DistanceNoiseScaleStrengthFade.x;
               #else
               float2 uv = data.uv0.xy * _DistanceNoiseScaleStrengthFade.x;
               #endif


               if (camDist > _DistanceNoiseScaleStrengthFade.z)
               {
                  #if _TRIPLANAR
                  half3 noise = (tex2D(_DistanceNoise, uv0) * tpw.x + 
                     tex2D(_DistanceNoise, uv1) * tpw.y + 
                     tex2D(_DistanceNoise, uv2) * tpw.z).rgb; 
                  #else
                  half3 noise = tex2D(_DistanceNoise, uv).rgb;
                  #endif

                  float fade = saturate ((camDist - _DistanceNoiseScaleStrengthFade.z) / _DistanceNoiseScaleStrengthFade.w);
                  fade *= _DistanceNoiseScaleStrengthFade.y;

                  albedo = lerp(albedo, BlendMult2X(albedo, noise.zzz), fade);
                  noise *= 0.5;
                  #if !_NOSPECNORMAL
                  norm.xy += ((noise.xy-0.25) * fade);
                  #endif
                  #if !_NOSPECNORMAL || _NOSPECTEX
                  smoothness += (abs(noise.x-0.25) * fade);
                  #endif
               }
            }
            #endif
         }

         void DoGlitter(inout MegaSplatLayer splats, half shine, half reflectAmt, float3 wsNormal, float3 wsView, float2 uv)
         {
            #if _SNOWGLITTER || _PUDDLEGLITTER || _PERTEXGLITTER
            half3 lightDir = normalize(_WorldSpaceLightPos0);

            half3 viewDir = normalize(wsView);
            half NdotL = saturate(dot(splats.Normal, lightDir));
            lightDir = reflect(lightDir, splats.Normal);
            half specular = saturate(abs(dot(lightDir, viewDir)));


            //float2 offset = float2(viewDir.z, wsNormal.x) + splats.Normal.xy * 0.1;
            float2 offset = splats.Normal * 0.1;
            offset = fmod(offset, 1.0);

            half detail = tex2D(_GlitterTexture, uv * _GlitterParams.xy + offset).x;

            #if _GLITTERMOVES
               float2 uv0 = uv * _GlitterParams.w + _Time.y * _GlitterParams.z;
               float2 uv1 = uv * _GlitterParams.w + _Time.y  * 1.04 * -_GlitterParams.z;
               half detail2 = tex2D(_GlitterTexture, uv0).y;
               half detail3 = tex2D(_GlitterTexture, uv1).y;
               detail *= sqrt(detail2 * detail3);
            #else
               float2 uv0 = uv * _GlitterParams.z;
               half detail2 = tex2D(_GlitterTexture, uv0).y;
               detail *= detail2;
            #endif
            detail *= saturate(splats.Height + 0.5);

            specular = pow(specular, shine) * floor(detail * reflectAmt);

            splats.Smoothness += specular;
            splats.Smoothness = min(splats.Smoothness, 1);
            #endif
         }


         LayerParams InitLayerParams(SplatInput si, float3 values, half2 texScale)
         {
            LayerParams data = NewLayerParams();
            #if _TERRAIN
            int i0 = round(values.x * 255);
            int i1 = round(values.y * 255);
            int i2 = round(values.z * 255);
            #else
            int i0 = round(values.x / max(si.weights.x, 0.00001));
            int i1 = round(values.y / max(si.weights.y, 0.00001));
            int i2 = round(values.z / max(si.weights.z, 0.00001));
            #endif

            #if _TRIPLANAR
            float3 coords = si.triplanarUVW * texScale.x;
            data.tpuv0_x = float3(coords.zy, i0);
            data.tpuv0_y = float3(coords.xz, i0);
            data.tpuv0_z = float3(coords.xy, i0);
            data.tpuv1_x = float3(coords.zy, i1);
            data.tpuv1_y = float3(coords.xz, i1);
            data.tpuv1_z = float3(coords.xy, i1);
            data.tpuv2_x = float3(coords.zy, i2);
            data.tpuv2_y = float3(coords.xz, i2);
            data.tpuv2_z = float3(coords.xy, i2);
            data.mipUV = coords.xz;

            float2 splatUV = si.splatUV * texScale.xy;
            data.uv0 = float3(splatUV, i0);
            data.uv1 = float3(splatUV, i1);
            data.uv2 = float3(splatUV, i2);

            #else
            float2 splatUV = si.splatUV.xy * texScale.xy;
            data.uv0 = float3(splatUV, i0);
            data.uv1 = float3(splatUV, i1);
            data.uv2 = float3(splatUV, i2);
            data.mipUV = splatUV.xy;
            #endif



            #if _FLOW || _FLOWREFRACTION
            data.flowOn = 0;
            #endif

            #if _DISTANCERESAMPLE
            InitDistanceResample(data, si.camDist.y);
            #endif

            return data;
         }

         MegaSplatLayer DoSurf(inout SplatInput si, MegaSplatLayer macro, float3x3 tangentToWorld)
         {
            #if _ALPHALAYER
            LayerParams mData = InitLayerParams(si, si.valuesSecond.xyz, _TexScales.xy);
            #else
            LayerParams mData = InitLayerParams(si, si.valuesMain.xyz, _TexScales.xy);
            #endif

            #if _ALPHAHOLE
            if (mData.uv0.z == _AlphaHoleIdx || mData.uv1.z == _AlphaHoleIdx || mData.uv2.z == _AlphaHoleIdx)
            {
               clip(-1);
            } 
            #endif

            #if _PARALLAX && (_TESSEDGE || _TESSDISTANCE || _LOWPOLY || _ALPHATEST)
            si.viewDir = mul(si.viewDir, tangentToWorld);
            #endif

            #if _PUDDLEFLOW || _PUDDLEREFRACT
            float2 puddleUV = si.macroUV * _PuddleUVScales.xy;
            #elif _PUDDLES
            float2 puddleUV = 0;
            #endif

            #if _PUDDLEFLOW || _PUDDLEREFRACT

            si.waterNormalFoam = float3(0,0,0);
            if (si.puddleHeight > 0 && si.camDist.y < 40)
            {
               float str = 1.0 - ((si.camDist.y - 20) / 20);
               si.waterNormalFoam = GetWaterNormal(puddleUV, si.flowDir) * str;
            }
            #endif

            SamplePerTex(_PropertyTex, mData, _PerTexScaleRange);

            #if _FLOWREFRACTION
            // see through
            //sampleBottom = sampleBottom || mData.flowOn > 0;
            #endif

            half porosity = _GlobalPorosityWetness.x;
            #if _PERTEXMATPARAMS
            porosity = mData.porosity.x * mData.weights.x + mData.porosity.y * mData.weights.y + mData.porosity.z * mData.weights.z;
            #endif

            #if _TWOLAYER
               LayerParams sData = InitLayerParams(si, si.valuesSecond.xyz, _TexScales.zw);

               #if _ALPHAHOLE
               if (sData.uv0.z == _AlphaHoleIdx || sData.uv1.z == _AlphaHoleIdx || sData.uv2.z == _AlphaHoleIdx)
               {
                  clip(-1);
               } 
               #endif

               sData.layerBlend = si.layerBlend;
               SamplePerTex(_PropertyTex, sData, _PerTexScaleRange);

               #if (_FLOW || _FLOWREFRACTION)
                  Flow(sData.uv0, si.flowDir, _FlowSpeed * sData.flowIntensity.x, _FlowIntensity, sData.fuv0a, sData.fuv0b, sData.flowInterps.x);
                  Flow(sData.uv1, si.flowDir, _FlowSpeed * sData.flowIntensity.y, _FlowIntensity, sData.fuv1a, sData.fuv1b, sData.flowInterps.y);
                  Flow(sData.uv2, si.flowDir, _FlowSpeed * sData.flowIntensity.z, _FlowIntensity, sData.fuv2a, sData.fuv2b, sData.flowInterps.z);
                  mData.flowOn = 0;
               #endif

               MegaSplatLayer second = SampleLayer(sData, si);

               #if _FLOWREFRACTION
                  float hMod = FlowRefract(second, mData, sData, si.weights);
               #endif
            #else // _TWOLAYER

               #if (_FLOW || _FLOWREFRACTION)
                  Flow(mData.uv0, si.flowDir, _FlowSpeed * mData.flowIntensity.x, _FlowIntensity, mData.fuv0a, mData.fuv0b, mData.flowInterps.x);
                  Flow(mData.uv1, si.flowDir, _FlowSpeed * mData.flowIntensity.y, _FlowIntensity, mData.fuv1a, mData.fuv1b, mData.flowInterps.y);
                  Flow(mData.uv2, si.flowDir, _FlowSpeed * mData.flowIntensity.z, _FlowIntensity, mData.fuv2a, mData.fuv2b, mData.flowInterps.z);
               #endif
            #endif

            #if _NOBLENDBOTTOM
               MegaSplatLayer splats = SampleLayerNoBlend(mData, si);
            #else
               MegaSplatLayer splats = SampleLayer(mData, si);
            #endif

            #if _TWOLAYER
               // blend layers together..
               float hfac = HeightBlend(splats.Height, second.Height, sData.layerBlend, _Contrast);
               #if _FLOWREFRACTION
                  hfac *= hMod;
               #endif
               splats = BlendResults(splats, second, hfac);
               porosity = lerp(porosity, 
                     sData.porosity.x * sData.weights.x + 
                     sData.porosity.y * sData.weights.y + 
                     sData.porosity.z * sData.weights.z,
                     hfac);
            #endif

            #if _GEOMAP
            float2 geoUV = float2(0, si.worldPos.y * _GeoParams.y + _GeoParams.z);
            half4 geoTex = tex2D(_GeoTex, geoUV);
            splats.Albedo = lerp(splats.Albedo, BlendMult2X(splats.Albedo, geoTex), _GeoParams.x * geoTex.a);
            #endif

            half macroBlend = 1;
            #if _USEMACROTEXTURE
            macroBlend = saturate(_MacroTextureStrength * si.camDist.x);
            #endif

            #if _DETAILMAP
               float dist = si.camDist.y;
               if (dist > _DistanceFades.w)
               {
                  MegaSplatLayer o = (MegaSplatLayer)0;
                  UNITY_INITIALIZE_OUTPUT(MegaSplatLayer,o);
                  #if _USEMACROTEXTURE
                  splats = OverlayResults(splats, macro, macroBlend);
                  #endif
                  o.Albedo = splats.Albedo;
                  o.Normal = splats.Normal;
                  o.Emission = splats.Emission;
                  o.Occlusion = splats.Occlusion;
                  #if !_RAMPLIGHTING
                  o.Metallic = splats.Metallic;
                  o.Smoothness = splats.Smoothness;
                  #endif
                  #if _CUSTOMUSERFUNCTION
                  CustomMegaSplatFunction_Final(si, o);
                  #endif
                  return o;
               }

               LayerParams sData = InitLayerParams(si, si.valuesMain, _TexScales.zw);
               MegaSplatLayer second = SampleDetail(mData.weights, si.viewDir, sData, si.triplanarBlend);   // use prev weights for detail

               float detailBlend = 1.0 - saturate((dist - _DistanceFades.z) / (_DistanceFades.w - _DistanceFades.z));
               splats = BlendDetail(splats, second, detailBlend); 
            #endif

            ApplyDetailNoise(splats.Albedo, splats.Normal, splats.Smoothness, mData, si.camDist.y, si.triplanarBlend);

            float3 worldNormal = float3(0,0,1);
            #if _SNOW || _RAINDROPFLATONLY
            worldNormal = mul(tangentToWorld, normalize(splats.Normal));
            #endif

            #if _SNOWGLITTER || _PERTEXGLITTER || _PUDDLEGLITTER
            half glitterShine = 0;
            half glitterReflect = 0;
            #endif

            #if _PERTEXGLITTER
            glitterReflect = mData.perTexGlitterReflect.x * mData.weights.x + mData.perTexGlitterReflect.y * mData.weights.y + mData.perTexGlitterReflect.z * mData.weights.z;
               #if _TWOLAYER || _ALPHALAYER
               float glitterReflect2 = sData.perTexGlitterReflect.x * sData.weights.x + sData.perTexGlitterReflect.y * sData.weights.y + sData.perTexGlitterReflect.z * sData.weights.z;
               glitterReflect = lerp(glitterReflect, glitterReflect2, hfac);
               #endif
            glitterReflect *= 14;
            glitterShine = 1.5;
            #endif


            float pud = 0;
            #if _PUDDLES || _PUDDLEFLOW || _PUDDLEREFRACT
               if (si.puddleHeight > 0)
               {
                  pud = DoPuddles(splats, puddleUV, si.waterNormalFoam, si.puddleHeight, si.flowDir, porosity, worldNormal);
               }
            #endif

            #if _LAVA
               float2 lavaUV = si.macroUV * _LavaUVScale.xy;


               if (si.puddleHeight > 0)
               {
                  pud = DoLava(splats, lavaUV, si.puddleHeight, si.flowDir);
               }
            #endif

            #if _PUDDLEGLITTER
            glitterShine = lerp(glitterShine, _GlitterSurfaces.z, pud);
            glitterReflect = lerp(glitterReflect, _GlitterSurfaces.w, pud);
            #endif


            #if _SNOW && !_SNOWOVERMACRO
               float snwAmt = DoSnow(splats, si.macroUV, worldNormal, si.snowHeightFade, pud, porosity, si.camDist.y);
               #if _SNOWGLITTER
               glitterShine = lerp(glitterShine, _GlitterSurfaces.x, snwAmt);
               glitterReflect = lerp(glitterReflect, _GlitterSurfaces.y, snwAmt);
               #endif
            #endif

            #if _SNOW && _SNOWOVERMACRO
            half preserveAO = splats.Occlusion;
            #endif

            #if _WETNESS
            WaterBRDF(splats.Albedo, splats.Smoothness, splats.Metallic, max( si.wetness, _GlobalPorosityWetness.y) * _MaxWetness, porosity); 
            #endif


            #if _ALPHALAYER
            half blend = HeightBlend(splats.Height, 1 - macro.Height, si.layerBlend * macroBlend, _Contrast);
            splats = OverlayResults(splats, macro, blend);
            #elif _USEMACROTEXTURE
            splats = OverlayResults(splats, macro, macroBlend);
            #endif

            #if _SNOW && _SNOWOVERMACRO
               splats.Occlusion = preserveAO;
               float snwAmt = DoSnow(splats, si.macroUV, worldNormal, si.snowHeightFade, pud, porosity, si.camDist.y);
               #if _SNOWGLITTER
               glitterShine = lerp(glitterShine, _GlitterSurfaces.x, snwAmt);
               glitterReflect = lerp(glitterReflect, _GlitterSurfaces.y, snwAmt);
               #endif
            #endif

            #if _SNOWGLITTER || _PUDDLEGLITTER || _PERTEXGLITTER
            DoGlitter(splats, glitterShine, glitterReflect, si.wsNormal, si.wsView, si.macroUV);
            #endif

            #if _CUSTOMUSERFUNCTION
            CustomMegaSplatFunction_Final(si, splats);
            #endif

            return splats;
         }

         #if _DEBUG_OUTPUT_SPLATDATA
         float3 DebugSplatOutput(SplatInput si)
         {
            float3 data = float3(0,0,0);
            int i0 = round(si.valuesMain.x / max(si.weights.x, 0.00001));
            int i1 = round(si.valuesMain.y / max(si.weights.y, 0.00001));
            int i2 = round(si.valuesMain.z / max(si.weights.z, 0.00001));

            #if _TWOLAYER || _ALPHALAYER
            int i3 = round(si.valuesSecond.x / max(si.weights.x, 0.00001));
            int i4 = round(si.valuesSecond.y / max(si.weights.y, 0.00001));
            int i5 = round(si.valuesSecond.z / max(si.weights.z, 0.00001));
            data.z = si.layerBlend;
            #endif

            if (si.weights.x > si.weights.y && si.weights.x > si.weights.z)
            {
               data.x = i0 / 255.0;
               #if _TWOLAYER || _ALPHALAYER
               data.y = i3 / 255.0;
               #endif
            }
            else if (si.weights.y > si.weights.x && si.weights.y > si.weights.z)
            {
               data.x = i1 / 255.0;
               #if _TWOLAYER || _ALPHALAYER
               data.y = i4 / 255.0;
               #endif
            }
            else
            {
               data.x = i2 / 255.0;
               #if _TWOLAYER || _ALPHALAYER
               data.y = i5 / 255.0;
               #endif
            }
            return data;
         }
         #endif

 
      // heavility packed structure
      struct Input
      {
          // avoid naming UV because unity magic..
          float2 coords;               // uv, or triplanar UV
          float4 valuesMain;           //index rgb, triplanar W
          #if _TWOLAYER || _ALPHALAYER
          float4 valuesSecond;         //index rgb + alpha
          #endif
          fixed3 weights : COLOR0;     // Causes unity to automagically map this from vertex color, erasing your values.. grr..

          float2 camDist;              // distance from camera (for fades) and fog
          float4 extraData;            // flowdir + fade, or if triplanar triplanarView, .w contains puddle height

          float3 viewDir;              // auto unity view dir, which gets compiled out in some cases, grrr..

          // everything after this requires > 3.5 shader model :(
          #if _SECONDUV
          float2 macroUV;              // special macro UV only used in alphalayer mode
          #endif


          #if _SNOW || _SNOWGLITTER || _PUDDLEGLITTER || _PERTEXGLITTER
          float3 wsNormal;
          #endif

          #if _SNOW
          half snowHeightFade;
          float4 wsTangent;
          #endif

          #if _SNOWGLITTER || _PUDDLEGLITTER || _PERTEXGLITTER
          float3 wsView;
          #endif

          #if _WETNESS
          half wetness;
          #endif

          #if _GEOMAP
          float3 worldPos;
          #endif

      };

      SplatInput ToSplatInput(Input i)
      {
         SplatInput o = (SplatInput)0;
         UNITY_INITIALIZE_OUTPUT(SplatInput,o);
         o.weights = i.weights.xyz;
         o.valuesMain = i.valuesMain.xyz;
         o.viewDir = i.viewDir;
         o.camDist.xy = i.camDist.xy;
         #if _TWOLAYER || _ALPHALAYER
         o.valuesSecond = i.valuesSecond.xyz;
         o.layerBlend = i.valuesSecond.w;
         #endif
         o.splatUV = i.coords.xy;
         o.macroUV = i.coords.xy;
         #if _SECONDUV
         o.macroUV = i.macroUV.xy;
         #endif
         #if _TRIPLANAR
         o.triplanarUVW = float3(i.coords.xy, i.valuesMain.w);
         o.triplanarBlend = i.extraData.xyz;
         #endif
         #if _FLOW || _FLOWREFRACTION || _PUDDLEFLOW || _PUDDLEREFRACT || _LAVA
         o.flowDir = i.extraData.xy;
         #endif
         #if _PUDDLES || _PUDDLEFLOW || _PUDDLEREFRACT || _LAVA
         o.puddleHeight = i.extraData.w;
         #endif

         #if _TESSDAMPENING
         o.displacementDampening = i.weights.w;
         #endif

         #if _SNOW
         o.snowHeightFade = i.snowHeightFade;
         #endif

         #if _WETNESS
         o.wetness = i.wetness;
         #endif

         #if _SNOW || _SNOWGLITTER || _PUDDLEGLITTER || _PERTEXGLITTER
         o.wsNormal = i.wsNormal;
         #endif

         #if _SNOWGLITTER || _PUDDLEGLITTER || _PERTEXGLITTER
         o.wsView = i.wsView;
         #endif

         #if _GEOMAP
         o.worldPos = i.worldPos;
         #endif

         return o;
      }

      void vert (inout appdata_full i, out Input o) 
      {
          UNITY_INITIALIZE_OUTPUT(Input,o);

          #if _CUSTOMUSERFUNCTION
          CustomMegaSplatFunction_PreVertex(i.vertex, i.normal, i.tangent, i.texcoord.xy);
          #endif

          // select the texture coordinate for the splat texture
          #if _USECURVEDWORLD
          V_CW_TransformPointAndNormal(i.vertex, i.normal, i.tangent);
          #endif

          o.coords.xy = i.texcoord.xy;
          #if _UVFROMSECOND
          o.coords.xy = i.texcoord1.xy;
          #endif

          #if _SECONDUV
             o.macroUV = i.texcoord.xy;
             #if _UVFROMSECOND2
             o.macroUV = i.texcoord1.xy;
             #endif
          #endif

          float3 worldPos = mul (unity_ObjectToWorld, i.vertex).xyz;

          #if _GEOMAP
          o.worldPos = worldPos;
          #endif

          float3 worldNormal = i.normal;
          #if (_TRIPLANAR_WORLDSPACE || _SNOW || _PROJECTTEXTURE_WORLD || _PROJECTTEXTURE2_WORLD || _UVWORLDTOP || _UVWORLDFRONT || _UVWORLDSIDE || _UVWORLDTOP2 || _UVWORLDFRONT2 || _UVWORLDSIDE2)
          worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, i.normal));
          #endif

          o.coords.xy = ProjectUVs(i.vertex.xyz, o.coords.xy, worldPos, worldNormal, i.tangent);
          #if _SECONDUV
          o.macroUV.xy = ProjectUV2(i.vertex.xyz, o.macroUV.xy, worldPos, worldNormal, i.tangent);
          #endif

          // filters in vertex color, main splat in color.a, secondary in uv2.a
          o.weights = i.color.rgb;
          o.valuesMain.xyz = i.color.rgb * i.color.a * 255;
          #if _TWOLAYER || _ALPHALAYER
          o.valuesSecond.xyz = i.color.rgb * i.texcoord3.a * 255;
          o.valuesSecond.a = i.texcoord3.x;
          #endif





          #if _TWOLAYER || _ALPHALAYER
          ProceduralTexture(i.vertex.xyz, worldPos, i.normal, worldNormal, o.valuesMain, o.valuesSecond, i.color.rgb);
          #else
          float4 fakeSecond = float4(0,0,0,0);
          ProceduralTexture(i.vertex.xyz, worldPos, i.normal, worldNormal, o.valuesMain, fakeSecond, i.color.rgb);
          #endif


          float dist = distance(_WorldSpaceCameraPos, worldPos);
          o.camDist.x = 1.0 - saturate((dist - _DistanceFades.x) / (_DistanceFades.y - _DistanceFades.x));
          o.camDist.y = length(UnityObjectToViewPos(i.vertex.xyz));

          #if _FLOW || _FLOWREFRACTION || _PUDDLEFLOW || _PUDDLEREFRACT || _LAVA
          o.extraData.xy = (i.texcoord2.zw * 2.0 - 1.0);
          #endif

          #if _PUDDLES || _PUDDLEFLOW || _PUDDLEREFRACT || _LAVA
          o.extraData.w = i.texcoord3.y;
          #endif

          #if _WETNESS
          o.wetness = i.texcoord1.w;
          #endif

          #if _TRIPLANAR
             float3 norm = i.normal;
             #if _TRIPLANAR_WORLDSPACE
             float3 uvw = worldPos * _TriplanarTexScale + _TriplanarOffset;
             norm = worldNormal;
             #else
             float3 uvw = i.vertex.xyz * _TriplanarTexScale + _TriplanarOffset;
             #endif
             o.coords.xy = uvw.xy;
             o.valuesMain.w = uvw.z;
             o.extraData.xyz = pow(abs(norm), _TriplanarContrast);
             o.extraData.xyz /= dot(o.extraData.xyz, float3(1,1,1));
          #endif

         #if _SNOW || _SNOWGLITTER || _PUDDLEGLITTER || _PERTEXGLITTER
         o.wsNormal = worldNormal;
         #endif

         #if _SNOW
         o.snowHeightFade = saturate((worldPos.y - _SnowHeightRange.x) / max(_SnowHeightRange.y, 0.001));
         o.wsTangent = mul(unity_ObjectToWorld, i.tangent);
         #endif

         #if _SNOWGLITTER || _PUDDLEGLITTER || _PERTEXGLITTER
         o.wsView = WorldSpaceViewDir(i.vertex);
         #endif

      }  

      void surf (Input i, inout SurfaceOutputStandard o) 
      {
         SplatInput si = ToSplatInput(i);
         float3x3 tangentToWorld = (float3x3)0;
         #if _SNOW
         float3 tangent = normalize(i.wsTangent.xyz);
         float3 normal = normalize(i.wsNormal);
         float3 binormal = normalize(cross(normal, tangent) * i.wsTangent.w);
         tangentToWorld = transpose(float3x3(tangent, binormal, normal));
         #endif

         MegaSplatLayer macro = (MegaSplatLayer)0;
         #if _USEMACROTEXTURE || _ALPHALAYER
            macro = SampleMacro(si.macroUV.xy);
            #if _SNOW && _SNOWOVERMACRO
            float snwAmt = DoSnow(macro, si.macroUV.xy, mul(tangentToWorld, normalize(macro.Normal)), si.snowHeightFade, 0, _GlobalPorosityWetness.x, si.camDist.y);
            // TODO: Handle glitter?
            #endif
            #if _DISABLESPLATSINDISTANCE
            UNITY_BRANCH
            if (i.camDist.x <= 0.0)
            {
               #if _CUSTOMUSERFUNCTION
               CustomMegaSplatFunction_Final(si, macro);
               #endif
               o.Albedo = macro.Albedo;
               o.Normal = macro.Normal;
               o.Emission = macro.Emission;
               #if !_RAMPLIGHTING
               o.Smoothness = macro.Smoothness;
               o.Metallic = macro.Metallic;
               o.Occlusion = macro.Occlusion;
               #endif
               #if _ALPHA || _ALPHATEST
               o.Alpha = macro.Alpha;
               #endif

               return;
            }
            #endif
         #endif


         MegaSplatLayer splats = DoSurf(si, macro, tangentToWorld);

         // hack around unity compiler stripping bug
         #if _PARALLAX
         splats.Albedo *= saturate(i.viewDir + 999);
         #endif

         #if _DEBUG_OUTPUT_ALBEDO
         o.Albedo = splats.Albedo;
         #elif _DEBUG_OUTPUT_HEIGHT
         o.Albedo = splats.Height.xxx * saturate(splats.Albedo+1);
         #elif _DEBUG_OUTPUT_NORMAL
         o.Albedo = splats.Normal * 0.5 + 0.5 * saturate(splats.Albedo+1);
         #elif _DEBUG_OUTPUT_SMOOTHNESS
         o.Albedo = splats.Smoothness.xxx * saturate(splats.Albedo+1);
         #elif _DEBUG_OUTPUT_METAL
         o.Albedo = splats.Metallic.xxx * saturate(splats.Albedo+1);
         #elif _DEBUG_OUTPUT_AO
         o.Albedo = splats.Occlusion.xxx * saturate(splats.Albedo+1);
         #elif _DEBUG_OUTPUT_EMISSION
         o.Albedo = splats.Emission * saturate(splats.Albedo+1);
         #elif _DEBUG_OUTPUT_SPLATDATA
         o.Albedo = DebugSplatOutput(si);
         #elif _RAMPLIGHTING
         o.Albedo = splats.Albedo;
         o.Emission = splats.Emission;
         o.Normal = splats.Normal;

         #else
         o.Albedo = splats.Albedo;
         o.Normal = splats.Normal;
         o.Metallic = splats.Metallic;
         o.Smoothness = splats.Smoothness;
         o.Occlusion = splats.Occlusion;
         o.Emission = splats.Emission;
            #if _ALPHA || _ALPHATEST
               o.Alpha = splats.Alpha;
            #endif

            #if _ALPHATEST
            clip(o.Alpha - 0.5);
            #endif
         #endif
      }

      void ApplyFog(float dist, inout half4 col)
      {
         half4 fogColor = half4(0,0,0,0);

         #ifndef UNITY_PASS_FORWARDADD
            fogColor = unity_FogColor;
         #endif

         #if FOG_LINEAR
             float unityFogFactor = dist * unity_FogParams.z + unity_FogParams.w;
             UNITY_FOG_LERP_COLOR(col, fogColor, unityFogFactor);
         #endif
         #if FOG_EXP
            float unityFogFactor = unity_FogParams.y * dist; 
            unityFogFactor = exp2(-unityFogFactor);
            UNITY_FOG_LERP_COLOR(col, fogColor, unityFogFactor);
         #endif
         #if FOG_EXP2
            float unityFogFactor = unity_FogParams.x * dist; 
            unityFogFactor = exp2(-unityFogFactor*unityFogFactor);
            UNITY_FOG_LERP_COLOR(col, fogColor, unityFogFactor);
         #endif


      }

      void fogcolor(Input i, SurfaceOutputStandard o, inout fixed4 col)
      {
         ApplyFog(i.camDist.y, col);
      }

      ENDCG
   }
   CustomEditor "SplatArrayShaderGUI"
   FallBack "Diffuse"
}
