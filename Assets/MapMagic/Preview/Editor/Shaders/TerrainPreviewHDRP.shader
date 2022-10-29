Shader "MapMagic/TerrainPreviewHDRP"
{
    Properties
    {
        [HideInInspector] [ToggleUI] _EnableHeightBlend("EnableHeightBlend", Float) = 0.0
        _HeightTransition("Height Transition", Range(0, 1.0)) = 0.0
        [Enum(Off, 0, From Ambient Occlusion, 1)]  _SpecularOcclusionMode("Specular Occlusion Mode", Int) = 1

        // TODO: support tri-planar?
        // TODO: support more maps?
        //[HideInInspector] _TexWorldScale0("Tiling", Float) = 1.0
        //[HideInInspector] _TexWorldScale1("Tiling", Float) = 1.0
        //[HideInInspector] _TexWorldScale2("Tiling", Float) = 1.0
        //[HideInInspector] _TexWorldScale3("Tiling", Float) = 1.0

        // Following are builtin properties

        // Stencil state
        // Forward
        [HideInInspector] _StencilRef("_StencilRef", Int) = 2 // StencilLightingUsage.RegularLighting
        [HideInInspector] _StencilWriteMask("_StencilWriteMask", Int) = 3 // StencilMask.Lighting
        // GBuffer
        [HideInInspector] _StencilRefGBuffer("_StencilRefGBuffer", Int) = 2 // StencilLightingUsage.RegularLighting
        [HideInInspector] _StencilWriteMaskGBuffer("_StencilWriteMaskGBuffer", Int) = 3 // StencilMask.Lighting
        // Depth prepass
        [HideInInspector] _StencilRefDepth("_StencilRefDepth", Int) = 0 // Nothing
        [HideInInspector] _StencilWriteMaskDepth("_StencilWriteMaskDepth", Int) = 32 // DoesntReceiveSSR

        // Blending state
        [HideInInspector] _ZWrite ("__zw", Float) = 1.0
        [HideInInspector] _CullMode("__cullmode", Float) = 2.0
        [HideInInspector] _ZTestDepthEqualForOpaque("_ZTestDepthEqualForOpaque", Int) = 4 // Less equal
        [HideInInspector] _ZTestGBuffer("_ZTestGBuffer", Int) = 4

        [ToggleUI] _EnableInstancedPerPixelNormal("Instanced per pixel normal", Float) = 1.0

		[HideInInspector] _TerrainHolesTexture("Holes Map (RGB)", 2D) = "white" {}

        // Caution: C# code in BaseLitUI.cs call LightmapEmissionFlagsProperty() which assume that there is an existing "_EmissionColor"
        // value that exist to identify if the GI emission need to be enabled.
        // In our case we don't use such a mechanism but need to keep the code quiet. We declare the value and always enable it.
        // TODO: Fix the code in legacy unity so we can customize the behavior for GI
        [HideInInspector] _EmissionColor("Color", Color) = (1, 1, 1)

        // HACK: GI Baking system relies on some properties existing in the shader ("_MainTex", "_Cutoff" and "_Color") for opacity handling, so we need to store our version of those parameters in the hard-coded name the GI baking system recognizes.
        [HideInInspector] _MainTex("Albedo", 2D) = "white" {}
        [HideInInspector] _Color("Color", Color) = (1,1,1,1)

        [HideInInspector] [ToggleUI] _SupportDecals("Support Decals", Float) = 1.0
        [HideInInspector] [ToggleUI] _ReceivesSSR("Receives SSR", Float) = 1.0
        [HideInInspector] [ToggleUI] _AddPrecomputedVelocity("AddPrecomputedVelocity", Float) = 0.0


		_Preview("Control (RGBA)", 2D) = "red" {}
		_Colorize("Colorize", Float) = 1
		_Margins("Margins", Int) = 16

		[HDR] _Color0("Color Black", Color) = (0.666, 0.2, 0.000, 1)  //(0.000, 0.426, 0.000, 1) //0, 109, 0
		[HDR] _Color1("Color Dark", Color) = (0.95, 0.65, 0.1, 1)	//(0.664, 0.793, 0.476, 1)  //170, 203, 122
		[HDR] _Color2("Color Gray", Color) = (1, 1, 0.75, 1)			//(0.949, 0.925, 0.695, 1)  //243, 237, 178
		[HDR] _Color3("Color Bright", Color) = (0.55, 0.85, 0.2, 1)	//(0.902, 0.578, 0.171, 1)  //231, 148, 44
		[HDR] _Color4("Color White", Color) = (0.000, 0.666, 0.2, 1)  //(0.726, 0.000, 0.000, 1)  //186, 0, 0 

		_Saturation("Saturation", Float) = 1.25
		_Brightness("Brightness", Float) = 0.9

    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

    // Terrain builtin keywords
    #pragma shader_feature_local _TERRAIN_8_LAYERS
    #pragma shader_feature_local _NORMALMAP
    #pragma shader_feature_local _MASKMAP
    #pragma shader_feature_local _SPECULAR_OCCLUSION_NONE

    #pragma shader_feature_local _TERRAIN_BLEND_HEIGHT
    // Sample normal in pixel shader when doing instancing
    #pragma shader_feature_local _TERRAIN_INSTANCED_PERPIXEL_NORMAL

    //#pragma shader_feature _ _LAYER_MAPPING_PLANAR0 _LAYER_MAPPING_TRIPLANAR0
    //#pragma shader_feature _ _LAYER_MAPPING_PLANAR1 _LAYER_MAPPING_TRIPLANAR1
    //#pragma shader_feature _ _LAYER_MAPPING_PLANAR2 _LAYER_MAPPING_TRIPLANAR2
    //#pragma shader_feature _ _LAYER_MAPPING_PLANAR3 _LAYER_MAPPING_TRIPLANAR3

    #pragma shader_feature_local _DISABLE_DECALS
    #pragma shader_feature_local _ADD_PRECOMPUTED_VELOCITY

    //enable GPU instancing support
    #pragma multi_compile_instancing
    #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

	#pragma multi_compile _ _ALPHATEST_ON

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLit_Splatmap_Includes.hlsl"

    ENDHLSL

    SubShader
    {
        // This tags allow to use the shader replacement features
        Tags
        {
            "RenderPipeline" = "HDRenderPipeline"
            "RenderType" = "Opaque"
            "SplatCount" = "8"
            "MaskMapR" = "Metallic"
            "MaskMapG" = "AO"
            "MaskMapB" = "Height"
            "MaskMapA" = "Smoothness"
            "DiffuseA" = "Smoothness (becomes Density when Mask map is assigned)"   // when MaskMap is disabled
            "DiffuseA_MaskMapUsed" = "Density"                                      // when MaskMap is enabled
        }

        // Caution: The outline selection in the editor use the vertex shader/hull/domain shader of the first pass declare. So it should not bethe  meta pass.
        Pass
        {
            Name "GBuffer"
            Tags { "LightMode" = "GBuffer" } // This will be only for opaque object based on the RenderQueue index

            Cull [_CullMode]
            ZTest [_ZTestGBuffer]

            Stencil
            {
                WriteMask [_StencilWriteMaskGBuffer]
                Ref [_StencilRefGBuffer]
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM

			#pragma vertex Vert
			#pragma fragment Frag_Preview

            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            // Setup DECALS_OFF so the shader stripper can remove variants
            #pragma multi_compile DECALS_OFF DECALS_3RT DECALS_4RT
            #pragma multi_compile _ LIGHT_LAYERS

			sampler2D _Preview;
			float4 _Preview_ST;
			float4 _Preview_TexelSize;
			int _Margins;
			float _Colorize;
			float _Saturation;
			float _Brightness;

            #define SHADERPASS SHADERPASS_GBUFFER
			#include "Preview.cginc" 
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLitTemplate.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLit_Splatmap.hlsl"

			void Frag_Preview(PackedVaryingsToPS packedInput,
				OUTPUT_GBUFFER(outGBuffer)
				#ifdef _DEPTHOFFSET_ON
				, out float outputDepth : SV_Depth
				#endif
				)
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
				FragInputs input = UnpackVaryingsMeshToFragInputs(packedInput.vmesh);

				// input.positionSS is SV_Position
				PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS);

				#ifdef VARYINGS_NEED_POSITION_WS
					float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);
				#else
					// Unused
					float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0
				#endif

				SurfaceData surfaceData;
				BuiltinData builtinData;
				GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

				//surfaceData.baseColor = float4(1,0,0,1); 

				float2 uv = input.texCoord0;
				float2 pxTexSize = _Preview_TexelSize.zw - _Margins * 2 - 1; //texture size without margins. Don't actually know why -1, related with 513
				float2 ratio = pxTexSize / _Preview_TexelSize.zw;
				float2 uvMargins = (1 - ratio) / 2;
				uv *= ratio;
				uv += uvMargins;

				half4 col = tex2D(_Preview, uv);
				col.rgb = col.r*(1 - _Colorize) + colorize(col.r, false)*_Colorize;
				col.rgb = saturation(col.rgb, _Saturation) * _Brightness;
				col.rgb = col.rgb * (col.rgb * (col.rgb * 0.305306011h + 0.682171111h) + 0.012522878h); //GammaToLinearSpace(col.rgb);

				surfaceData.baseColor = overlay(col.rgb, surfaceData.baseColor.rgb)*0.8 + col.rgb*0.2;

				ENCODE_INTO_GBUFFER(surfaceData, builtinData, posInput.positionSS, outGBuffer);

				#ifdef _DEPTHOFFSET_ON
					outputDepth = posInput.deviceDepth;
				#endif
			}

            ENDHLSL
        }

        // Extracts information for lightmapping, GI (emission, albedo, ...)
        // This pass it not used during regular rendering.
        Pass
        {
            Name "META"
            Tags{ "LightMode" = "META" }

            Cull Off

            HLSLPROGRAM

			#pragma vertex Vert
			#pragma fragment Frag

            // Lightmap memo
            // DYNAMICLIGHTMAP_ON is used when we have an "enlighten lightmap" ie a lightmap updated at runtime by enlighten.This lightmap contain indirect lighting from realtime lights and realtime emissive material.Offline baked lighting(from baked material / light,
            // both direct and indirect lighting) will hand up in the "regular" lightmap->LIGHTMAP_ON.

            #define SHADERPASS SHADERPASS_LIGHT_TRANSPORT
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLitTemplate.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLit_Splatmap.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{ "LightMode" = "ShadowCaster" }

            Cull[_CullMode]

            ZClip [_ZClip]
            ZWrite On
            ZTest LEqual

            ColorMask 0

            HLSLPROGRAM

			#pragma vertex Vert
			#pragma fragment Frag

            #define SHADERPASS SHADERPASS_SHADOWS
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLitTemplate.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLit_Splatmap.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags{ "LightMode" = "DepthOnly" }

            Cull[_CullMode]

            // To be able to tag stencil with disableSSR information for forward
            Stencil
            {
                WriteMask [_StencilWriteMaskDepth]
                Ref [_StencilRefDepth]
                Comp Always
                Pass Replace
            }

            ZWrite On

            HLSLPROGRAM

			#pragma vertex Vert
			#pragma fragment Frag

            // In deferred, depth only pass don't output anything.
            // In forward it output the normal buffer
            #pragma multi_compile _ WRITE_NORMAL_BUFFER
            #pragma multi_compile _ WRITE_MSAA_DEPTH

            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLitTemplate.hlsl"
            #ifdef WRITE_NORMAL_BUFFER
                #if defined(_NORMALMAP)
                    #define OVERRIDE_SPLAT_SAMPLER_NAME sampler_Normal0
                #elif defined(_MASKMAP)
                    #define OVERRIDE_SPLAT_SAMPLER_NAME sampler_Mask0
                #endif
            #endif
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLit_Splatmap.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "Forward" } // This will be only for transparent object based on the RenderQueue index

            Stencil
            {
                WriteMask [_StencilWriteMask]
                Ref [_StencilRef]
                Comp Always
                Pass Replace
            }

            // In case of forward we want to have depth equal for opaque mesh
            ZTest [_ZTestDepthEqualForOpaque]
            ZWrite [_ZWrite]
            Cull [_CullMode]

            HLSLPROGRAM

			#pragma vertex Vert
			#pragma fragment Frag_Preview

            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            // Setup DECALS_OFF so the shader stripper can remove variants
            #pragma multi_compile DECALS_OFF DECALS_3RT DECALS_4RT

            // Supported shadow modes per light type
            #pragma multi_compile SHADOW_LOW SHADOW_MEDIUM SHADOW_HIGH

            #pragma multi_compile USE_FPTL_LIGHTLIST USE_CLUSTERED_LIGHTLIST

            #define SHADERPASS SHADERPASS_FORWARD

			sampler2D _Preview;
			float4 _Preview_ST;
			float4 _Preview_TexelSize;
			int _Margins;
			float _Colorize;
			float _Saturation;
			float _Brightness;

			#include "Preview.cginc" 
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLitTemplate.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLit_Splatmap.hlsl"

			void Frag_Preview(PackedVaryingsToPS packedInput,
				#ifdef OUTPUT_SPLIT_LIGHTING
					out float4 outColor : SV_Target0,  // outSpecularLighting
					out float4 outDiffuseLighting : SV_Target1,
					OUTPUT_SSSBUFFER(outSSSBuffer)
				#else
					out float4 outColor : SV_Target0
				#ifdef _WRITE_TRANSPARENT_MOTION_VECTOR
					, out float4 outMotionVec : SV_Target1
				#endif // _WRITE_TRANSPARENT_MOTION_VECTOR
				#endif // OUTPUT_SPLIT_LIGHTING
				#ifdef _DEPTHOFFSET_ON
					, out float outputDepth : SV_Depth
				#endif
				)
			{
				/*Frag(packedInput,
					#ifdef OUTPUT_SPLIT_LIGHTING
						out outColor,
						out outDiffuseLighting,
						OUTPUT_SSSBUFFER(outSSSBuffer)
					#else
						outColor
					#ifdef _WRITE_TRANSPARENT_MOTION_VECTOR
						, out outMotionVec
					#endif // _WRITE_TRANSPARENT_MOTION_VECTOR
					#endif // OUTPUT_SPLIT_LIGHTING
					#ifdef _DEPTHOFFSET_ON
						, out outputDepth
					#endif
					);*/

				/*float2 uv = IN.uvMainAndLM;
				float2 pxTexSize = _Preview_TexelSize.zw - _Margins * 2 - 1; //texture size without margins. Don't actually know why -1, related with 513
				float2 ratio = pxTexSize / _Preview_TexelSize.zw;
				float2 uvMargins = (1 - ratio) / 2;
				uv *= ratio;
				uv += uvMargins;

				half4 col = tex2D(_Preview, uv);
				col.rgb = col.r*(1 - _Colorize) + colorize(col.r, false)*_Colorize;
				col.rgb = saturation(col.rgb, _Saturation) * _Brightness;
				col.rgb = col.rgb * (col.rgb * (col.rgb * 0.305306011h + 0.682171111h) + 0.012522878h); //GammaToLinearSpace(col.rgb);

				src.rgb = overlay(col.rgb, src.rgb)*0.8 + col.rgb*0.2;*/
			}

            ENDHLSL
        }

        Pass
        {
            Name "SceneSelectionPass"
            Tags { "LightMode" = "SceneSelectionPass" }

            HLSLPROGRAM

			#pragma vertex Vert
			#pragma fragment Frag

            #pragma editor_sync_compilation
            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #define SCENESELECTIONPASS
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLitTemplate.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLit_Splatmap.hlsl"

            ENDHLSL
        }

        UsePass "Hidden/Nature/Terrain/Utilities/PICKING"
    }

    Dependency "BaseMapShader" = "Hidden/HDRP/TerrainLit_Basemap"
    Dependency "BaseMapGenShader" = "Hidden/HDRP/TerrainLit_BasemapGen"
    CustomEditor "UnityEditor.Rendering.HighDefinition.TerrainLitGUI"
}
