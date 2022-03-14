// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "GPUInstancer/Mtree/SRP/Bark URP"
{
	Properties
	{
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		[Header(Albedo Texture)]_Color("Color", Color) = (1,1,1,0)
		_MainTex("Albedo", 2D) = "white" {}
		[Enum(Off,0,Front,1,Back,2)]_CullMode("Cull Mode", Int) = 2
		[Header(Normal Texture)]_BumpMap("Normal", 2D) = "bump" {}
		_BumpScale("Normal Strength", Float) = 1
		[Enum(On,0,Off,1)][Header(Detail Settings)]_BaseDetail("Base Detail", Int) = 1
		_DetailColor("Detail Color", Color) = (1,1,1,0)
		_DetailAlbedoMap("Detail", 2D) = "white" {}
		_DetailNormalMap("Detail Normal", 2D) = "bump" {}
		_Height("Height", Range( 0 , 1)) = 0
		_Smooth("Smooth", Range( 0.01 , 0.5)) = 0.02
		_TextureInfluence("Texture Influence", Range( 0 , 1)) = 0.5
		[Header(Other Settings)]_OcclusionStrength("AO strength", Range( 0 , 1)) = 0.6
		_Metallic("Metallic", Range( 0 , 1)) = 0
		_Glossiness("Smoothness", Range( 0 , 1)) = 0
		[Header(Wind)]_GlobalWindInfluence("Global Wind Influence", Range( 0 , 1)) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}

	}

	SubShader
	{
		LOD 0

		
		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
		
		Cull [_CullMode]
		HLSLINCLUDE
		#pragma target 2.0
		ENDHLSL

		
		Pass
		{
			
			Name "Forward"
			Tags { "LightMode"="UniversalForward" }
			
			Blend One Zero , One Zero
			ZWrite On
			ZTest LEqual
			Offset 0 , 0
			ColorMask RGBA
			

			HLSLPROGRAM
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#include "./../../GPUInstancer/Shaders/Include/GPUInstancerInclude.cginc"
#pragma instancing_options procedural:setupGPUI
#pragma multi_compile_instancing

			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define ASE_ABSOLUTE_VERTEX_POS 1
			#define _NORMALMAP 1
			#define ASE_SRP_VERSION 70108

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile _ _SHADOWS_SOFT
			#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
			
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile _ LIGHTMAP_ON

			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS_FORWARD

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			
			#if ASE_SRP_VERSION <= 70108
			#define REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
			#endif

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_FRAG_COLOR
			#pragma multi_compile __ LOD_FADE_CROSSFADE


			float _WindStrength;
			float _RandomWindOffset;
			float _WindPulse;
			float _WindDirection;
			sampler2D _DetailAlbedoMap;
			sampler2D _MainTex;
			sampler2D _DetailNormalMap;
			sampler2D _BumpMap;
			CBUFFER_START( UnityPerMaterial )
			int _CullMode;
			float _GlobalWindInfluence;
			float4 _DetailColor;
			float4 _DetailAlbedoMap_ST;
			float4 _MainTex_ST;
			float4 _Color;
			half _Height;
			half _TextureInfluence;
			half _Smooth;
			int _BaseDetail;
			float4 _DetailNormalMap_ST;
			half _BumpScale;
			float4 _BumpMap_ST;
			float _Metallic;
			float _Glossiness;
			half _OcclusionStrength;
			CBUFFER_END


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_tangent : TANGENT;
				float4 texcoord1 : TEXCOORD1;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				float4 lightmapUVOrVertexSH : TEXCOORD0;
				half4 fogFactorAndVertexLight : TEXCOORD1;
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				float4 shadowCoord : TEXCOORD2;
				#endif
				float4 tSpace0 : TEXCOORD3;
				float4 tSpace1 : TEXCOORD4;
				float4 tSpace2 : TEXCOORD5;
				float4 ase_texcoord6 : TEXCOORD6;
				float4 ase_color : COLOR;
				float4 ase_texcoord7 : TEXCOORD7;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			float2 DirectionalEquation( float _WindDirection )
			{
				float d = _WindDirection * 0.0174532924;
				float xL = cos(d) + 1 / 2;
				float zL = sin(d) + 1 / 2;
				return float2(zL,xL);
			}
			
			inline float Dither8x8Bayer( int x, int y )
			{
				const float dither[ 64 ] = {
			 1, 49, 13, 61,  4, 52, 16, 64,
			33, 17, 45, 29, 36, 20, 48, 32,
			 9, 57,  5, 53, 12, 60,  8, 56,
			41, 25, 37, 21, 44, 28, 40, 24,
			 3, 51, 15, 63,  2, 50, 14, 62,
			35, 19, 47, 31, 34, 18, 46, 30,
			11, 59,  7, 55, 10, 58,  6, 54,
			43, 27, 39, 23, 42, 26, 38, 22};
				int r = y * 8 + x;
				return dither[r] / 64; // same # of instructions as pre-dividing due to compiler magic
			}
			

			VertexOutput vert ( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float3 VAR_VertexPosition21_g23 = mul( GetObjectToWorldMatrix(), float4( v.vertex.xyz , 0.0 ) ).xyz;
				float3 break109_g23 = VAR_VertexPosition21_g23;
				float VAR_WindStrength43_g23 = ( _WindStrength * _GlobalWindInfluence );
				float4 transform37_g23 = mul(GetObjectToWorldMatrix(),float4( 0,0,0,1 ));
				float2 appendResult38_g23 = (float2(transform37_g23.x , transform37_g23.z));
				float dotResult2_g24 = dot( appendResult38_g23 , float2( 12.9898,78.233 ) );
				float lerpResult8_g24 = lerp( 0.8 , ( ( _RandomWindOffset / 2.0 ) + 0.9 ) , frac( ( sin( dotResult2_g24 ) * 43758.55 ) ));
				float VAR_RandomTime16_g23 = ( ( _TimeParameters.x * 0.05 ) * lerpResult8_g24 );
				float FUNC_Turbulence36_g23 = ( sin( ( ( VAR_RandomTime16_g23 * 40.0 ) - ( VAR_VertexPosition21_g23.z / 15.0 ) ) ) * 0.5 );
				float VAR_WindPulse274_g23 = _WindPulse;
				float FUNC_Angle73_g23 = ( VAR_WindStrength43_g23 * ( 1.0 + sin( ( ( ( ( VAR_RandomTime16_g23 * 2.0 ) + FUNC_Turbulence36_g23 ) - ( VAR_VertexPosition21_g23.z / 50.0 ) ) - ( v.ase_color.r / 20.0 ) ) ) ) * sqrt( v.ase_color.r ) * 0.2 * VAR_WindPulse274_g23 );
				float VAR_SinA80_g23 = sin( FUNC_Angle73_g23 );
				float VAR_CosA78_g23 = cos( FUNC_Angle73_g23 );
				float _WindDirection164_g23 = _WindDirection;
				float2 localDirectionalEquation164_g23 = DirectionalEquation( _WindDirection164_g23 );
				float2 break165_g23 = localDirectionalEquation164_g23;
				float VAR_xLerp83_g23 = break165_g23.x;
				float lerpResult118_g23 = lerp( break109_g23.x , ( ( break109_g23.y * VAR_SinA80_g23 ) + ( break109_g23.x * VAR_CosA78_g23 ) ) , VAR_xLerp83_g23);
				float3 break98_g23 = VAR_VertexPosition21_g23;
				float3 break105_g23 = VAR_VertexPosition21_g23;
				float VAR_zLerp95_g23 = break165_g23.y;
				float lerpResult120_g23 = lerp( break105_g23.z , ( ( break105_g23.y * VAR_SinA80_g23 ) + ( break105_g23.z * VAR_CosA78_g23 ) ) , VAR_zLerp95_g23);
				float3 appendResult122_g23 = (float3(lerpResult118_g23 , ( ( break98_g23.y * VAR_CosA78_g23 ) - ( break98_g23.z * VAR_SinA80_g23 ) ) , lerpResult120_g23));
				float3 FUNC_vertexPos123_g23 = appendResult122_g23;
				float3 temp_output_5_0_g23 = mul( GetWorldToObjectMatrix(), float4( FUNC_vertexPos123_g23 , 0.0 ) ).xyz;
				float3 OUT_VertexPos212 = temp_output_5_0_g23;
				
				float4 ase_clipPos = TransformObjectToHClip((v.vertex).xyz);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				o.ase_texcoord7 = screenPos;
				
				o.ase_texcoord6.xy = v.ase_texcoord.xy;
				o.ase_color = v.ase_color;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord6.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = OUT_VertexPos212;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif
				v.ase_normal = v.ase_normal;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				float3 positionVS = TransformWorldToView( positionWS );
				float4 positionCS = TransformWorldToHClip( positionWS );

				VertexNormalInputs normalInput = GetVertexNormalInputs( v.ase_normal, v.ase_tangent );

				o.tSpace0 = float4( normalInput.normalWS, positionWS.x);
				o.tSpace1 = float4( normalInput.tangentWS, positionWS.y);
				o.tSpace2 = float4( normalInput.bitangentWS, positionWS.z);

				OUTPUT_LIGHTMAP_UV( v.texcoord1, unity_LightmapST, o.lightmapUVOrVertexSH.xy );
				OUTPUT_SH( normalInput.normalWS.xyz, o.lightmapUVOrVertexSH.xyz );

				half3 vertexLight = VertexLighting( positionWS, normalInput.normalWS );
				#ifdef ASE_FOG
					half fogFactor = ComputeFogFactor( positionCS.z );
				#else
					half fogFactor = 0;
				#endif
				o.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
				
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				VertexPositionInputs vertexInput = (VertexPositionInputs)0;
				vertexInput.positionWS = positionWS;
				vertexInput.positionCS = positionCS;
				o.shadowCoord = GetShadowCoord( vertexInput );
				#endif
				
				o.clipPos = positionCS;
				return o;
			}

			half4 frag ( VertexOutput IN  ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				float3 WorldNormal = normalize( IN.tSpace0.xyz );
				float3 WorldTangent = IN.tSpace1.xyz;
				float3 WorldBiTangent = IN.tSpace2.xyz;
				float3 WorldPosition = float3(IN.tSpace0.w,IN.tSpace1.w,IN.tSpace2.w);
				float3 WorldViewDirection = _WorldSpaceCameraPos.xyz  - WorldPosition;
				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
					ShadowCoords = IN.shadowCoord;
				#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
					ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
				#endif
	
				#if SHADER_HINT_NICE_QUALITY
					WorldViewDirection = SafeNormalize( WorldViewDirection );
				#endif

				float2 uv_DetailAlbedoMap = IN.ase_texcoord6.xy * _DetailAlbedoMap_ST.xy + _DetailAlbedoMap_ST.zw;
				float2 uv_MainTex = IN.ase_texcoord6.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				float4 VAR_AlbedoTexture132 = tex2D( _MainTex, uv_MainTex );
				float4 temp_output_94_0 = ( VAR_AlbedoTexture132 * _Color );
				float4 break93 = VAR_AlbedoTexture132;
				float clampResult70 = clamp( ( ( ( IN.ase_color.r - _Height ) + ( ( ( break93.r + break93.g + break93.b ) - 0.5 ) * _TextureInfluence ) ) / _Smooth ) , 0.0 , 1.0 );
				float FUNC_BarkDamageBlend137 = clampResult70;
				float4 lerpResult89 = lerp( ( _DetailColor * tex2D( _DetailAlbedoMap, uv_DetailAlbedoMap ) ) , temp_output_94_0 , FUNC_BarkDamageBlend137);
				int VAR_BaseDetail220 = _BaseDetail;
				float4 lerpResult238 = lerp( lerpResult89 , temp_output_94_0 , (float)VAR_BaseDetail220);
				float4 OUT_Albedo215 = lerpResult238;
				
				float2 uv_DetailNormalMap = IN.ase_texcoord6.xy * _DetailNormalMap_ST.xy + _DetailNormalMap_ST.zw;
				float2 uv_BumpMap = IN.ase_texcoord6.xy * _BumpMap_ST.xy + _BumpMap_ST.zw;
				float3 lerpResult73 = lerp( UnpackNormalScale( tex2D( _DetailNormalMap, uv_DetailNormalMap ), _BumpScale ) , UnpackNormalScale( tex2D( _BumpMap, uv_BumpMap ), _BumpScale ) , FUNC_BarkDamageBlend137);
				float3 lerpResult239 = lerp( lerpResult73 , UnpackNormalScale( tex2D( _BumpMap, uv_BumpMap ), _BumpScale ) , (float)VAR_BaseDetail220);
				float3 OUT_Normal210 = lerpResult239;
				
				float lerpResult231 = lerp( 0.0 , VAR_AlbedoTexture132.r , _Glossiness);
				float OUT_Smoothness105 = lerpResult231;
				
				float lerpResult40 = lerp( 1.0 , IN.ase_color.a , _OcclusionStrength);
				float OUT_AO204 = lerpResult40;
				
				float temp_output_41_0_g22 = VAR_AlbedoTexture132.a;
				float4 screenPos = IN.ase_texcoord7;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float2 clipScreen45_g22 = ase_screenPosNorm.xy * _ScreenParams.xy;
				float dither45_g22 = Dither8x8Bayer( fmod(clipScreen45_g22.x, 8), fmod(clipScreen45_g22.y, 8) );
				dither45_g22 = step( dither45_g22, unity_LODFade.x );
				#ifdef LOD_FADE_CROSSFADE
				float staticSwitch40_g22 = ( temp_output_41_0_g22 * dither45_g22 );
				#else
				float staticSwitch40_g22 = temp_output_41_0_g22;
				#endif
				float OUT_Alpha228 = staticSwitch40_g22;
				
				float3 Albedo = OUT_Albedo215.rgb;
				float3 Normal = OUT_Normal210;
				float3 Emission = 0;
				float3 Specular = 0.5;
				float Metallic = _Metallic;
				float Smoothness = OUT_Smoothness105;
				float Occlusion = OUT_AO204;
				float Alpha = OUT_Alpha228;
				float AlphaClipThreshold = 0.5;
				float3 BakedGI = 0;

				InputData inputData;
				inputData.positionWS = WorldPosition;
				inputData.viewDirectionWS = WorldViewDirection;
				inputData.shadowCoord = ShadowCoords;

				#ifdef _NORMALMAP
					inputData.normalWS = normalize(TransformTangentToWorld(Normal, half3x3( WorldTangent, WorldBiTangent, WorldNormal )));
				#else
					#if !SHADER_HINT_NICE_QUALITY
						inputData.normalWS = WorldNormal;
					#else
						inputData.normalWS = normalize( WorldNormal );
					#endif
				#endif

				#ifdef ASE_FOG
					inputData.fogCoord = IN.fogFactorAndVertexLight.x;
				#endif

				inputData.vertexLighting = IN.fogFactorAndVertexLight.yzw;
				inputData.bakedGI = SAMPLE_GI( IN.lightmapUVOrVertexSH.xy, IN.lightmapUVOrVertexSH.xyz, inputData.normalWS );
				#ifdef _ASE_BAKEDGI
					inputData.bakedGI = BakedGI;
				#endif
				half4 color = UniversalFragmentPBR(
					inputData, 
					Albedo, 
					Metallic, 
					Specular, 
					Smoothness, 
					Occlusion, 
					Emission, 
					Alpha);

				#ifdef ASE_FOG
					#ifdef TERRAIN_SPLAT_ADDPASS
						color.rgb = MixFogColor(color.rgb, half3( 0, 0, 0 ), IN.fogFactorAndVertexLight.x );
					#else
						color.rgb = MixFog(color.rgb, IN.fogFactorAndVertexLight.x);
					#endif
				#endif
				
				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif
				
				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif

				return color;
			}

			ENDHLSL
		}

		
		Pass
		{
			
			Name "ShadowCaster"
			Tags { "LightMode"="ShadowCaster" }

			ZWrite On
			ZTest LEqual

			HLSLPROGRAM
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#include "./../../GPUInstancer/Shaders/Include/GPUInstancerInclude.cginc"
#pragma instancing_options procedural:setupGPUI
#pragma multi_compile_instancing

			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define ASE_ABSOLUTE_VERTEX_POS 1
			#define _NORMALMAP 1
			#define ASE_SRP_VERSION 70108

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex ShadowPassVertex
			#pragma fragment ShadowPassFragment

			#define SHADERPASS_SHADOWCASTER

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			#define ASE_NEEDS_VERT_POSITION
			#pragma multi_compile __ LOD_FADE_CROSSFADE


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			float _WindStrength;
			float _RandomWindOffset;
			float _WindPulse;
			float _WindDirection;
			sampler2D _MainTex;
			CBUFFER_START( UnityPerMaterial )
			int _CullMode;
			float _GlobalWindInfluence;
			float4 _DetailColor;
			float4 _DetailAlbedoMap_ST;
			float4 _MainTex_ST;
			float4 _Color;
			half _Height;
			half _TextureInfluence;
			half _Smooth;
			int _BaseDetail;
			float4 _DetailNormalMap_ST;
			half _BumpScale;
			float4 _BumpMap_ST;
			float _Metallic;
			float _Glossiness;
			half _OcclusionStrength;
			CBUFFER_END


			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 worldPos : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				float4 shadowCoord : TEXCOORD1;
				#endif
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			float2 DirectionalEquation( float _WindDirection )
			{
				float d = _WindDirection * 0.0174532924;
				float xL = cos(d) + 1 / 2;
				float zL = sin(d) + 1 / 2;
				return float2(zL,xL);
			}
			
			inline float Dither8x8Bayer( int x, int y )
			{
				const float dither[ 64 ] = {
			 1, 49, 13, 61,  4, 52, 16, 64,
			33, 17, 45, 29, 36, 20, 48, 32,
			 9, 57,  5, 53, 12, 60,  8, 56,
			41, 25, 37, 21, 44, 28, 40, 24,
			 3, 51, 15, 63,  2, 50, 14, 62,
			35, 19, 47, 31, 34, 18, 46, 30,
			11, 59,  7, 55, 10, 58,  6, 54,
			43, 27, 39, 23, 42, 26, 38, 22};
				int r = y * 8 + x;
				return dither[r] / 64; // same # of instructions as pre-dividing due to compiler magic
			}
			

			float3 _LightDirection;

			VertexOutput ShadowPassVertex( VertexInput v )
			{
				VertexOutput o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );

				float3 VAR_VertexPosition21_g23 = mul( GetObjectToWorldMatrix(), float4( v.vertex.xyz , 0.0 ) ).xyz;
				float3 break109_g23 = VAR_VertexPosition21_g23;
				float VAR_WindStrength43_g23 = ( _WindStrength * _GlobalWindInfluence );
				float4 transform37_g23 = mul(GetObjectToWorldMatrix(),float4( 0,0,0,1 ));
				float2 appendResult38_g23 = (float2(transform37_g23.x , transform37_g23.z));
				float dotResult2_g24 = dot( appendResult38_g23 , float2( 12.9898,78.233 ) );
				float lerpResult8_g24 = lerp( 0.8 , ( ( _RandomWindOffset / 2.0 ) + 0.9 ) , frac( ( sin( dotResult2_g24 ) * 43758.55 ) ));
				float VAR_RandomTime16_g23 = ( ( _TimeParameters.x * 0.05 ) * lerpResult8_g24 );
				float FUNC_Turbulence36_g23 = ( sin( ( ( VAR_RandomTime16_g23 * 40.0 ) - ( VAR_VertexPosition21_g23.z / 15.0 ) ) ) * 0.5 );
				float VAR_WindPulse274_g23 = _WindPulse;
				float FUNC_Angle73_g23 = ( VAR_WindStrength43_g23 * ( 1.0 + sin( ( ( ( ( VAR_RandomTime16_g23 * 2.0 ) + FUNC_Turbulence36_g23 ) - ( VAR_VertexPosition21_g23.z / 50.0 ) ) - ( v.ase_color.r / 20.0 ) ) ) ) * sqrt( v.ase_color.r ) * 0.2 * VAR_WindPulse274_g23 );
				float VAR_SinA80_g23 = sin( FUNC_Angle73_g23 );
				float VAR_CosA78_g23 = cos( FUNC_Angle73_g23 );
				float _WindDirection164_g23 = _WindDirection;
				float2 localDirectionalEquation164_g23 = DirectionalEquation( _WindDirection164_g23 );
				float2 break165_g23 = localDirectionalEquation164_g23;
				float VAR_xLerp83_g23 = break165_g23.x;
				float lerpResult118_g23 = lerp( break109_g23.x , ( ( break109_g23.y * VAR_SinA80_g23 ) + ( break109_g23.x * VAR_CosA78_g23 ) ) , VAR_xLerp83_g23);
				float3 break98_g23 = VAR_VertexPosition21_g23;
				float3 break105_g23 = VAR_VertexPosition21_g23;
				float VAR_zLerp95_g23 = break165_g23.y;
				float lerpResult120_g23 = lerp( break105_g23.z , ( ( break105_g23.y * VAR_SinA80_g23 ) + ( break105_g23.z * VAR_CosA78_g23 ) ) , VAR_zLerp95_g23);
				float3 appendResult122_g23 = (float3(lerpResult118_g23 , ( ( break98_g23.y * VAR_CosA78_g23 ) - ( break98_g23.z * VAR_SinA80_g23 ) ) , lerpResult120_g23));
				float3 FUNC_vertexPos123_g23 = appendResult122_g23;
				float3 temp_output_5_0_g23 = mul( GetWorldToObjectMatrix(), float4( FUNC_vertexPos123_g23 , 0.0 ) ).xyz;
				float3 OUT_VertexPos212 = temp_output_5_0_g23;
				
				float4 ase_clipPos = TransformObjectToHClip((v.vertex).xyz);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				o.ase_texcoord3 = screenPos;
				
				o.ase_texcoord2.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord2.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = OUT_VertexPos212;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = v.ase_normal;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				o.worldPos = positionWS;
				#endif
				float3 normalWS = TransformObjectToWorldDir(v.ase_normal);

				float4 clipPos = TransformWorldToHClip( ApplyShadowBias( positionWS, normalWS, _LightDirection ) );

				#if UNITY_REVERSED_Z
					clipPos.z = min(clipPos.z, clipPos.w * UNITY_NEAR_CLIP_VALUE);
				#else
					clipPos.z = max(clipPos.z, clipPos.w * UNITY_NEAR_CLIP_VALUE);
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
					vertexInput.positionWS = positionWS;
					vertexInput.positionCS = clipPos;
					o.shadowCoord = GetShadowCoord( vertexInput );
				#endif
				o.clipPos = clipPos;
				return o;
			}

			half4 ShadowPassFragment(VertexOutput IN  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );
				
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 WorldPosition = IN.worldPos;
				#endif
				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = IN.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				float2 uv_MainTex = IN.ase_texcoord2.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				float4 VAR_AlbedoTexture132 = tex2D( _MainTex, uv_MainTex );
				float temp_output_41_0_g22 = VAR_AlbedoTexture132.a;
				float4 screenPos = IN.ase_texcoord3;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float2 clipScreen45_g22 = ase_screenPosNorm.xy * _ScreenParams.xy;
				float dither45_g22 = Dither8x8Bayer( fmod(clipScreen45_g22.x, 8), fmod(clipScreen45_g22.y, 8) );
				dither45_g22 = step( dither45_g22, unity_LODFade.x );
				#ifdef LOD_FADE_CROSSFADE
				float staticSwitch40_g22 = ( temp_output_41_0_g22 * dither45_g22 );
				#else
				float staticSwitch40_g22 = temp_output_41_0_g22;
				#endif
				float OUT_Alpha228 = staticSwitch40_g22;
				
				float Alpha = OUT_Alpha228;
				float AlphaClipThreshold = 0.5;

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif
				return 0;
			}

			ENDHLSL
		}

		
		Pass
		{
			
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }

			ZWrite On
			ColorMask 0

			HLSLPROGRAM
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#include "./../../GPUInstancer/Shaders/Include/GPUInstancerInclude.cginc"
#pragma instancing_options procedural:setupGPUI
#pragma multi_compile_instancing

			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define ASE_ABSOLUTE_VERTEX_POS 1
			#define _NORMALMAP 1
			#define ASE_SRP_VERSION 70108

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS_DEPTHONLY

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			#define ASE_NEEDS_VERT_POSITION
			#pragma multi_compile __ LOD_FADE_CROSSFADE


			float _WindStrength;
			float _RandomWindOffset;
			float _WindPulse;
			float _WindDirection;
			sampler2D _MainTex;
			CBUFFER_START( UnityPerMaterial )
			int _CullMode;
			float _GlobalWindInfluence;
			float4 _DetailColor;
			float4 _DetailAlbedoMap_ST;
			float4 _MainTex_ST;
			float4 _Color;
			half _Height;
			half _TextureInfluence;
			half _Smooth;
			int _BaseDetail;
			float4 _DetailNormalMap_ST;
			half _BumpScale;
			float4 _BumpMap_ST;
			float _Metallic;
			float _Glossiness;
			half _OcclusionStrength;
			CBUFFER_END


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 worldPos : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				float4 shadowCoord : TEXCOORD1;
				#endif
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			float2 DirectionalEquation( float _WindDirection )
			{
				float d = _WindDirection * 0.0174532924;
				float xL = cos(d) + 1 / 2;
				float zL = sin(d) + 1 / 2;
				return float2(zL,xL);
			}
			
			inline float Dither8x8Bayer( int x, int y )
			{
				const float dither[ 64 ] = {
			 1, 49, 13, 61,  4, 52, 16, 64,
			33, 17, 45, 29, 36, 20, 48, 32,
			 9, 57,  5, 53, 12, 60,  8, 56,
			41, 25, 37, 21, 44, 28, 40, 24,
			 3, 51, 15, 63,  2, 50, 14, 62,
			35, 19, 47, 31, 34, 18, 46, 30,
			11, 59,  7, 55, 10, 58,  6, 54,
			43, 27, 39, 23, 42, 26, 38, 22};
				int r = y * 8 + x;
				return dither[r] / 64; // same # of instructions as pre-dividing due to compiler magic
			}
			

			VertexOutput vert( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float3 VAR_VertexPosition21_g23 = mul( GetObjectToWorldMatrix(), float4( v.vertex.xyz , 0.0 ) ).xyz;
				float3 break109_g23 = VAR_VertexPosition21_g23;
				float VAR_WindStrength43_g23 = ( _WindStrength * _GlobalWindInfluence );
				float4 transform37_g23 = mul(GetObjectToWorldMatrix(),float4( 0,0,0,1 ));
				float2 appendResult38_g23 = (float2(transform37_g23.x , transform37_g23.z));
				float dotResult2_g24 = dot( appendResult38_g23 , float2( 12.9898,78.233 ) );
				float lerpResult8_g24 = lerp( 0.8 , ( ( _RandomWindOffset / 2.0 ) + 0.9 ) , frac( ( sin( dotResult2_g24 ) * 43758.55 ) ));
				float VAR_RandomTime16_g23 = ( ( _TimeParameters.x * 0.05 ) * lerpResult8_g24 );
				float FUNC_Turbulence36_g23 = ( sin( ( ( VAR_RandomTime16_g23 * 40.0 ) - ( VAR_VertexPosition21_g23.z / 15.0 ) ) ) * 0.5 );
				float VAR_WindPulse274_g23 = _WindPulse;
				float FUNC_Angle73_g23 = ( VAR_WindStrength43_g23 * ( 1.0 + sin( ( ( ( ( VAR_RandomTime16_g23 * 2.0 ) + FUNC_Turbulence36_g23 ) - ( VAR_VertexPosition21_g23.z / 50.0 ) ) - ( v.ase_color.r / 20.0 ) ) ) ) * sqrt( v.ase_color.r ) * 0.2 * VAR_WindPulse274_g23 );
				float VAR_SinA80_g23 = sin( FUNC_Angle73_g23 );
				float VAR_CosA78_g23 = cos( FUNC_Angle73_g23 );
				float _WindDirection164_g23 = _WindDirection;
				float2 localDirectionalEquation164_g23 = DirectionalEquation( _WindDirection164_g23 );
				float2 break165_g23 = localDirectionalEquation164_g23;
				float VAR_xLerp83_g23 = break165_g23.x;
				float lerpResult118_g23 = lerp( break109_g23.x , ( ( break109_g23.y * VAR_SinA80_g23 ) + ( break109_g23.x * VAR_CosA78_g23 ) ) , VAR_xLerp83_g23);
				float3 break98_g23 = VAR_VertexPosition21_g23;
				float3 break105_g23 = VAR_VertexPosition21_g23;
				float VAR_zLerp95_g23 = break165_g23.y;
				float lerpResult120_g23 = lerp( break105_g23.z , ( ( break105_g23.y * VAR_SinA80_g23 ) + ( break105_g23.z * VAR_CosA78_g23 ) ) , VAR_zLerp95_g23);
				float3 appendResult122_g23 = (float3(lerpResult118_g23 , ( ( break98_g23.y * VAR_CosA78_g23 ) - ( break98_g23.z * VAR_SinA80_g23 ) ) , lerpResult120_g23));
				float3 FUNC_vertexPos123_g23 = appendResult122_g23;
				float3 temp_output_5_0_g23 = mul( GetWorldToObjectMatrix(), float4( FUNC_vertexPos123_g23 , 0.0 ) ).xyz;
				float3 OUT_VertexPos212 = temp_output_5_0_g23;
				
				float4 ase_clipPos = TransformObjectToHClip((v.vertex).xyz);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				o.ase_texcoord3 = screenPos;
				
				o.ase_texcoord2.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord2.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = OUT_VertexPos212;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = v.ase_normal;
				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				float4 positionCS = TransformWorldToHClip( positionWS );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				o.worldPos = positionWS;
				#endif

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
					vertexInput.positionWS = positionWS;
					vertexInput.positionCS = positionCS;
					o.shadowCoord = GetShadowCoord( vertexInput );
				#endif
				o.clipPos = positionCS;
				return o;
			}

			half4 frag(VertexOutput IN  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 WorldPosition = IN.worldPos;
				#endif
				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = IN.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				float2 uv_MainTex = IN.ase_texcoord2.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				float4 VAR_AlbedoTexture132 = tex2D( _MainTex, uv_MainTex );
				float temp_output_41_0_g22 = VAR_AlbedoTexture132.a;
				float4 screenPos = IN.ase_texcoord3;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float2 clipScreen45_g22 = ase_screenPosNorm.xy * _ScreenParams.xy;
				float dither45_g22 = Dither8x8Bayer( fmod(clipScreen45_g22.x, 8), fmod(clipScreen45_g22.y, 8) );
				dither45_g22 = step( dither45_g22, unity_LODFade.x );
				#ifdef LOD_FADE_CROSSFADE
				float staticSwitch40_g22 = ( temp_output_41_0_g22 * dither45_g22 );
				#else
				float staticSwitch40_g22 = temp_output_41_0_g22;
				#endif
				float OUT_Alpha228 = staticSwitch40_g22;
				
				float Alpha = OUT_Alpha228;
				float AlphaClipThreshold = 0.5;

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif
				return 0;
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "Meta"
			Tags { "LightMode"="Meta" }

			Cull Off

			HLSLPROGRAM
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#include "./../../GPUInstancer/Shaders/Include/GPUInstancerInclude.cginc"
#pragma instancing_options procedural:setupGPUI
#pragma multi_compile_instancing

			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define ASE_ABSOLUTE_VERTEX_POS 1
			#define _NORMALMAP 1
			#define ASE_SRP_VERSION 70108

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS_META

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			#define ASE_NEEDS_VERT_POSITION
			#pragma multi_compile __ LOD_FADE_CROSSFADE


			float _WindStrength;
			float _RandomWindOffset;
			float _WindPulse;
			float _WindDirection;
			sampler2D _DetailAlbedoMap;
			sampler2D _MainTex;
			CBUFFER_START( UnityPerMaterial )
			int _CullMode;
			float _GlobalWindInfluence;
			float4 _DetailColor;
			float4 _DetailAlbedoMap_ST;
			float4 _MainTex_ST;
			float4 _Color;
			half _Height;
			half _TextureInfluence;
			half _Smooth;
			int _BaseDetail;
			float4 _DetailNormalMap_ST;
			half _BumpScale;
			float4 _BumpMap_ST;
			float _Metallic;
			float _Glossiness;
			half _OcclusionStrength;
			CBUFFER_END


			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 worldPos : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				float4 shadowCoord : TEXCOORD1;
				#endif
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;
				float4 ase_texcoord3 : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			float2 DirectionalEquation( float _WindDirection )
			{
				float d = _WindDirection * 0.0174532924;
				float xL = cos(d) + 1 / 2;
				float zL = sin(d) + 1 / 2;
				return float2(zL,xL);
			}
			
			inline float Dither8x8Bayer( int x, int y )
			{
				const float dither[ 64 ] = {
			 1, 49, 13, 61,  4, 52, 16, 64,
			33, 17, 45, 29, 36, 20, 48, 32,
			 9, 57,  5, 53, 12, 60,  8, 56,
			41, 25, 37, 21, 44, 28, 40, 24,
			 3, 51, 15, 63,  2, 50, 14, 62,
			35, 19, 47, 31, 34, 18, 46, 30,
			11, 59,  7, 55, 10, 58,  6, 54,
			43, 27, 39, 23, 42, 26, 38, 22};
				int r = y * 8 + x;
				return dither[r] / 64; // same # of instructions as pre-dividing due to compiler magic
			}
			

			VertexOutput vert( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float3 VAR_VertexPosition21_g23 = mul( GetObjectToWorldMatrix(), float4( v.vertex.xyz , 0.0 ) ).xyz;
				float3 break109_g23 = VAR_VertexPosition21_g23;
				float VAR_WindStrength43_g23 = ( _WindStrength * _GlobalWindInfluence );
				float4 transform37_g23 = mul(GetObjectToWorldMatrix(),float4( 0,0,0,1 ));
				float2 appendResult38_g23 = (float2(transform37_g23.x , transform37_g23.z));
				float dotResult2_g24 = dot( appendResult38_g23 , float2( 12.9898,78.233 ) );
				float lerpResult8_g24 = lerp( 0.8 , ( ( _RandomWindOffset / 2.0 ) + 0.9 ) , frac( ( sin( dotResult2_g24 ) * 43758.55 ) ));
				float VAR_RandomTime16_g23 = ( ( _TimeParameters.x * 0.05 ) * lerpResult8_g24 );
				float FUNC_Turbulence36_g23 = ( sin( ( ( VAR_RandomTime16_g23 * 40.0 ) - ( VAR_VertexPosition21_g23.z / 15.0 ) ) ) * 0.5 );
				float VAR_WindPulse274_g23 = _WindPulse;
				float FUNC_Angle73_g23 = ( VAR_WindStrength43_g23 * ( 1.0 + sin( ( ( ( ( VAR_RandomTime16_g23 * 2.0 ) + FUNC_Turbulence36_g23 ) - ( VAR_VertexPosition21_g23.z / 50.0 ) ) - ( v.ase_color.r / 20.0 ) ) ) ) * sqrt( v.ase_color.r ) * 0.2 * VAR_WindPulse274_g23 );
				float VAR_SinA80_g23 = sin( FUNC_Angle73_g23 );
				float VAR_CosA78_g23 = cos( FUNC_Angle73_g23 );
				float _WindDirection164_g23 = _WindDirection;
				float2 localDirectionalEquation164_g23 = DirectionalEquation( _WindDirection164_g23 );
				float2 break165_g23 = localDirectionalEquation164_g23;
				float VAR_xLerp83_g23 = break165_g23.x;
				float lerpResult118_g23 = lerp( break109_g23.x , ( ( break109_g23.y * VAR_SinA80_g23 ) + ( break109_g23.x * VAR_CosA78_g23 ) ) , VAR_xLerp83_g23);
				float3 break98_g23 = VAR_VertexPosition21_g23;
				float3 break105_g23 = VAR_VertexPosition21_g23;
				float VAR_zLerp95_g23 = break165_g23.y;
				float lerpResult120_g23 = lerp( break105_g23.z , ( ( break105_g23.y * VAR_SinA80_g23 ) + ( break105_g23.z * VAR_CosA78_g23 ) ) , VAR_zLerp95_g23);
				float3 appendResult122_g23 = (float3(lerpResult118_g23 , ( ( break98_g23.y * VAR_CosA78_g23 ) - ( break98_g23.z * VAR_SinA80_g23 ) ) , lerpResult120_g23));
				float3 FUNC_vertexPos123_g23 = appendResult122_g23;
				float3 temp_output_5_0_g23 = mul( GetWorldToObjectMatrix(), float4( FUNC_vertexPos123_g23 , 0.0 ) ).xyz;
				float3 OUT_VertexPos212 = temp_output_5_0_g23;
				
				float4 ase_clipPos = TransformObjectToHClip((v.vertex).xyz);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				o.ase_texcoord3 = screenPos;
				
				o.ase_texcoord2.xy = v.ase_texcoord.xy;
				o.ase_color = v.ase_color;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord2.zw = 0;
				
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = OUT_VertexPos212;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = v.ase_normal;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				o.worldPos = positionWS;
				#endif

				o.clipPos = MetaVertexPosition( v.vertex, v.texcoord1.xy, v.texcoord1.xy, unity_LightmapST, unity_DynamicLightmapST );
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
					vertexInput.positionWS = positionWS;
					vertexInput.positionCS = o.clipPos;
					o.shadowCoord = GetShadowCoord( vertexInput );
				#endif
				return o;
			}

			half4 frag(VertexOutput IN  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 WorldPosition = IN.worldPos;
				#endif
				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = IN.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				float2 uv_DetailAlbedoMap = IN.ase_texcoord2.xy * _DetailAlbedoMap_ST.xy + _DetailAlbedoMap_ST.zw;
				float2 uv_MainTex = IN.ase_texcoord2.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				float4 VAR_AlbedoTexture132 = tex2D( _MainTex, uv_MainTex );
				float4 temp_output_94_0 = ( VAR_AlbedoTexture132 * _Color );
				float4 break93 = VAR_AlbedoTexture132;
				float clampResult70 = clamp( ( ( ( IN.ase_color.r - _Height ) + ( ( ( break93.r + break93.g + break93.b ) - 0.5 ) * _TextureInfluence ) ) / _Smooth ) , 0.0 , 1.0 );
				float FUNC_BarkDamageBlend137 = clampResult70;
				float4 lerpResult89 = lerp( ( _DetailColor * tex2D( _DetailAlbedoMap, uv_DetailAlbedoMap ) ) , temp_output_94_0 , FUNC_BarkDamageBlend137);
				int VAR_BaseDetail220 = _BaseDetail;
				float4 lerpResult238 = lerp( lerpResult89 , temp_output_94_0 , (float)VAR_BaseDetail220);
				float4 OUT_Albedo215 = lerpResult238;
				
				float temp_output_41_0_g22 = VAR_AlbedoTexture132.a;
				float4 screenPos = IN.ase_texcoord3;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float2 clipScreen45_g22 = ase_screenPosNorm.xy * _ScreenParams.xy;
				float dither45_g22 = Dither8x8Bayer( fmod(clipScreen45_g22.x, 8), fmod(clipScreen45_g22.y, 8) );
				dither45_g22 = step( dither45_g22, unity_LODFade.x );
				#ifdef LOD_FADE_CROSSFADE
				float staticSwitch40_g22 = ( temp_output_41_0_g22 * dither45_g22 );
				#else
				float staticSwitch40_g22 = temp_output_41_0_g22;
				#endif
				float OUT_Alpha228 = staticSwitch40_g22;
				
				
				float3 Albedo = OUT_Albedo215.rgb;
				float3 Emission = 0;
				float Alpha = OUT_Alpha228;
				float AlphaClipThreshold = 0.5;

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				MetaInput metaInput = (MetaInput)0;
				metaInput.Albedo = Albedo;
				metaInput.Emission = Emission;
				
				return MetaFragment(metaInput);
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "Universal2D"
			Tags { "LightMode"="Universal2D" }

			Blend One Zero , One Zero
			ZWrite On
			ZTest LEqual
			Offset 0 , 0
			ColorMask RGBA

			HLSLPROGRAM
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#include "./../../GPUInstancer/Shaders/Include/GPUInstancerInclude.cginc"
#pragma instancing_options procedural:setupGPUI
#pragma multi_compile_instancing

			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define ASE_ABSOLUTE_VERTEX_POS 1
			#define _NORMALMAP 1
			#define ASE_SRP_VERSION 70108

			#pragma enable_d3d11_debug_symbols
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS_2D

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			
			#define ASE_NEEDS_VERT_POSITION
			#pragma multi_compile __ LOD_FADE_CROSSFADE


			float _WindStrength;
			float _RandomWindOffset;
			float _WindPulse;
			float _WindDirection;
			sampler2D _DetailAlbedoMap;
			sampler2D _MainTex;
			CBUFFER_START( UnityPerMaterial )
			int _CullMode;
			float _GlobalWindInfluence;
			float4 _DetailColor;
			float4 _DetailAlbedoMap_ST;
			float4 _MainTex_ST;
			float4 _Color;
			half _Height;
			half _TextureInfluence;
			half _Smooth;
			int _BaseDetail;
			float4 _DetailNormalMap_ST;
			half _BumpScale;
			float4 _BumpMap_ST;
			float _Metallic;
			float _Glossiness;
			half _OcclusionStrength;
			CBUFFER_END


			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 worldPos : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				float4 shadowCoord : TEXCOORD1;
				#endif
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;
				float4 ase_texcoord3 : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			float2 DirectionalEquation( float _WindDirection )
			{
				float d = _WindDirection * 0.0174532924;
				float xL = cos(d) + 1 / 2;
				float zL = sin(d) + 1 / 2;
				return float2(zL,xL);
			}
			
			inline float Dither8x8Bayer( int x, int y )
			{
				const float dither[ 64 ] = {
			 1, 49, 13, 61,  4, 52, 16, 64,
			33, 17, 45, 29, 36, 20, 48, 32,
			 9, 57,  5, 53, 12, 60,  8, 56,
			41, 25, 37, 21, 44, 28, 40, 24,
			 3, 51, 15, 63,  2, 50, 14, 62,
			35, 19, 47, 31, 34, 18, 46, 30,
			11, 59,  7, 55, 10, 58,  6, 54,
			43, 27, 39, 23, 42, 26, 38, 22};
				int r = y * 8 + x;
				return dither[r] / 64; // same # of instructions as pre-dividing due to compiler magic
			}
			

			VertexOutput vert( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );

				float3 VAR_VertexPosition21_g23 = mul( GetObjectToWorldMatrix(), float4( v.vertex.xyz , 0.0 ) ).xyz;
				float3 break109_g23 = VAR_VertexPosition21_g23;
				float VAR_WindStrength43_g23 = ( _WindStrength * _GlobalWindInfluence );
				float4 transform37_g23 = mul(GetObjectToWorldMatrix(),float4( 0,0,0,1 ));
				float2 appendResult38_g23 = (float2(transform37_g23.x , transform37_g23.z));
				float dotResult2_g24 = dot( appendResult38_g23 , float2( 12.9898,78.233 ) );
				float lerpResult8_g24 = lerp( 0.8 , ( ( _RandomWindOffset / 2.0 ) + 0.9 ) , frac( ( sin( dotResult2_g24 ) * 43758.55 ) ));
				float VAR_RandomTime16_g23 = ( ( _TimeParameters.x * 0.05 ) * lerpResult8_g24 );
				float FUNC_Turbulence36_g23 = ( sin( ( ( VAR_RandomTime16_g23 * 40.0 ) - ( VAR_VertexPosition21_g23.z / 15.0 ) ) ) * 0.5 );
				float VAR_WindPulse274_g23 = _WindPulse;
				float FUNC_Angle73_g23 = ( VAR_WindStrength43_g23 * ( 1.0 + sin( ( ( ( ( VAR_RandomTime16_g23 * 2.0 ) + FUNC_Turbulence36_g23 ) - ( VAR_VertexPosition21_g23.z / 50.0 ) ) - ( v.ase_color.r / 20.0 ) ) ) ) * sqrt( v.ase_color.r ) * 0.2 * VAR_WindPulse274_g23 );
				float VAR_SinA80_g23 = sin( FUNC_Angle73_g23 );
				float VAR_CosA78_g23 = cos( FUNC_Angle73_g23 );
				float _WindDirection164_g23 = _WindDirection;
				float2 localDirectionalEquation164_g23 = DirectionalEquation( _WindDirection164_g23 );
				float2 break165_g23 = localDirectionalEquation164_g23;
				float VAR_xLerp83_g23 = break165_g23.x;
				float lerpResult118_g23 = lerp( break109_g23.x , ( ( break109_g23.y * VAR_SinA80_g23 ) + ( break109_g23.x * VAR_CosA78_g23 ) ) , VAR_xLerp83_g23);
				float3 break98_g23 = VAR_VertexPosition21_g23;
				float3 break105_g23 = VAR_VertexPosition21_g23;
				float VAR_zLerp95_g23 = break165_g23.y;
				float lerpResult120_g23 = lerp( break105_g23.z , ( ( break105_g23.y * VAR_SinA80_g23 ) + ( break105_g23.z * VAR_CosA78_g23 ) ) , VAR_zLerp95_g23);
				float3 appendResult122_g23 = (float3(lerpResult118_g23 , ( ( break98_g23.y * VAR_CosA78_g23 ) - ( break98_g23.z * VAR_SinA80_g23 ) ) , lerpResult120_g23));
				float3 FUNC_vertexPos123_g23 = appendResult122_g23;
				float3 temp_output_5_0_g23 = mul( GetWorldToObjectMatrix(), float4( FUNC_vertexPos123_g23 , 0.0 ) ).xyz;
				float3 OUT_VertexPos212 = temp_output_5_0_g23;
				
				float4 ase_clipPos = TransformObjectToHClip((v.vertex).xyz);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				o.ase_texcoord3 = screenPos;
				
				o.ase_texcoord2.xy = v.ase_texcoord.xy;
				o.ase_color = v.ase_color;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord2.zw = 0;
				
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = OUT_VertexPos212;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = v.ase_normal;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				float4 positionCS = TransformWorldToHClip( positionWS );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				o.worldPos = positionWS;
				#endif

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
					vertexInput.positionWS = positionWS;
					vertexInput.positionCS = positionCS;
					o.shadowCoord = GetShadowCoord( vertexInput );
				#endif

				o.clipPos = positionCS;
				return o;
			}

			half4 frag(VertexOutput IN  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 WorldPosition = IN.worldPos;
				#endif
				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = IN.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				float2 uv_DetailAlbedoMap = IN.ase_texcoord2.xy * _DetailAlbedoMap_ST.xy + _DetailAlbedoMap_ST.zw;
				float2 uv_MainTex = IN.ase_texcoord2.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				float4 VAR_AlbedoTexture132 = tex2D( _MainTex, uv_MainTex );
				float4 temp_output_94_0 = ( VAR_AlbedoTexture132 * _Color );
				float4 break93 = VAR_AlbedoTexture132;
				float clampResult70 = clamp( ( ( ( IN.ase_color.r - _Height ) + ( ( ( break93.r + break93.g + break93.b ) - 0.5 ) * _TextureInfluence ) ) / _Smooth ) , 0.0 , 1.0 );
				float FUNC_BarkDamageBlend137 = clampResult70;
				float4 lerpResult89 = lerp( ( _DetailColor * tex2D( _DetailAlbedoMap, uv_DetailAlbedoMap ) ) , temp_output_94_0 , FUNC_BarkDamageBlend137);
				int VAR_BaseDetail220 = _BaseDetail;
				float4 lerpResult238 = lerp( lerpResult89 , temp_output_94_0 , (float)VAR_BaseDetail220);
				float4 OUT_Albedo215 = lerpResult238;
				
				float temp_output_41_0_g22 = VAR_AlbedoTexture132.a;
				float4 screenPos = IN.ase_texcoord3;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float2 clipScreen45_g22 = ase_screenPosNorm.xy * _ScreenParams.xy;
				float dither45_g22 = Dither8x8Bayer( fmod(clipScreen45_g22.x, 8), fmod(clipScreen45_g22.y, 8) );
				dither45_g22 = step( dither45_g22, unity_LODFade.x );
				#ifdef LOD_FADE_CROSSFADE
				float staticSwitch40_g22 = ( temp_output_41_0_g22 * dither45_g22 );
				#else
				float staticSwitch40_g22 = temp_output_41_0_g22;
				#endif
				float OUT_Alpha228 = staticSwitch40_g22;
				
				
				float3 Albedo = OUT_Albedo215.rgb;
				float Alpha = OUT_Alpha228;
				float AlphaClipThreshold = 0.5;

				half4 color = half4( Albedo, Alpha );

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				return color;
			}
			ENDHLSL
		}
		
	}
	CustomEditor "UnityEditor.ShaderGraph.PBRMasterGUI"
	Fallback "Hidden/InternalErrorShader"
	
}
/*ASEBEGIN
Version=17800
425;266;1008;676;-599.9413;617.0234;1;True;False
Node;AmplifyShaderEditor.CommentaryNode;62;-1910.43,-1397.629;Inherit;False;2338.832;816.1819;;14;215;89;94;138;88;75;67;65;78;132;79;80;221;238;Albedo;1,0.125,0.125,1;0;0
Node;AmplifyShaderEditor.TexturePropertyNode;80;-1884.27,-934.2155;Float;True;Property;_MainTex;Albedo;1;0;Create;False;0;0;False;0;None;None;False;white;Auto;Texture2D;-1;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.SamplerNode;79;-1646.472,-935.0974;Inherit;True;Property;_TextureSample0;Texture Sample 0;1;0;Create;True;0;0;False;0;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;132;-1337.993,-935.4142;Inherit;False;VAR_AlbedoTexture;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode;63;-1914.402,236.3903;Inherit;False;1716.032;775.5377;;14;137;70;66;85;87;84;90;68;83;81;82;92;93;133;Bark Damage Blend;0.5441177,0.3039554,0,1;0;0
Node;AmplifyShaderEditor.GetLocalVarNode;133;-1876.222,664.4685;Inherit;False;132;VAR_AlbedoTexture;1;0;OBJECT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.BreakToComponentsNode;93;-1637.486,669.0856;Inherit;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleAddOpNode;92;-1339.968,602.8804;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;81;-1225.52,292.4143;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;82;-1347.507,735.5916;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;68;-1431.689,897.0052;Half;False;Property;_Height;Height;9;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;83;-1403.783,409.6715;Half;False;Property;_TextureInfluence;Texture Influence;11;0;Create;True;0;0;False;0;0.5;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;84;-1047.45,390.9706;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;90;-1105.822,720.4196;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;87;-1224.069,541.4816;Half;False;Property;_Smooth;Smooth;10;0;Create;True;0;0;False;0;0.02;0.02;0.01;0.5;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;85;-940.9781,731.3996;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;66;-804.7661,732.3817;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;223;757.7206,-1396.159;Inherit;False;528.8771;359.1725;;3;181;220;176;Variables;1,0,0.7254902,1;0;0
Node;AmplifyShaderEditor.TexturePropertyNode;78;-1736.553,-1182.568;Float;True;Property;_DetailAlbedoMap;Detail;7;0;Create;False;0;0;False;0;None;None;False;white;Auto;Texture2D;-1;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.ClampOpNode;70;-678.6691,733.6096;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;67;-1498.401,-1179.096;Inherit;True;Property;_TextureSample3;Texture Sample 3;1;0;Create;True;0;0;False;0;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.IntNode;176;807.7206,-1166.386;Inherit;False;Property;_BaseDetail;Base Detail;5;1;[Enum];Create;True;2;On;0;Off;1;0;False;1;Header(Detail Settings);1;0;0;1;INT;0
Node;AmplifyShaderEditor.ColorNode;65;-1734.939,-1352.974;Float;False;Property;_DetailColor;Detail Color;6;0;Create;True;0;0;False;0;1,1,1,0;1,1,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;229;-1882.205,2669.325;Inherit;False;1029.764;229;;4;227;226;225;228;Alpha;1,1,0.534,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;137;-506.3039,736.2026;Inherit;False;FUNC_BarkDamageBlend;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;75;-1291.41,-844.7188;Float;False;Property;_Color;Color;0;0;Create;True;0;0;False;1;Header(Albedo Texture);1,1,1,0;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;88;-1101.673,-1346.463;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;138;-1187.753,-1067.079;Inherit;False;137;FUNC_BarkDamageBlend;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;94;-1048.915,-929.5502;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;220;1018.491,-1162.579;Inherit;False;VAR_BaseDetail;-1;True;1;0;INT;0;False;1;INT;0
Node;AmplifyShaderEditor.GetLocalVarNode;226;-1832.205,2720.789;Inherit;False;132;VAR_AlbedoTexture;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;89;-816.1126,-1037.168;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;221;-840.8629,-824.021;Inherit;False;220;VAR_BaseDetail;1;0;OBJECT;;False;1;INT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;227;-1581.677,2719.325;Inherit;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.CommentaryNode;213;-1887.001,2292.522;Inherit;False;533.4861;173.9739;;2;212;236;Vertex Pos;0,1,0.09019608,1;0;0
Node;AmplifyShaderEditor.LerpOp;238;-492.7258,-951.9901;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;225;-1319.028,2788.047;Inherit;False;LOD CrossFade;-1;;22;bbfabe35be0e79d438adaa880ee1b0aa;1,44,1;1;41;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;236;-1844.001,2348.839;Inherit;False;Mtree Wind;15;;23;d710ffc7589a70c42a3e6c5220c6279d;7,282,0,280,0,278,0,255,0,269,1,281,0,272,0;0;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;217;760.597,-870.1332;Inherit;False;1099.843;840.2675;;7;216;214;108;195;211;230;237;Output;0,0,0,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;212;-1596.254,2342.522;Inherit;False;OUT_VertexPos;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;109;-1906.097,1217.689;Inherit;False;1288.315;497.5828;;5;100;105;231;232;233;Smoothness;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;111;-1894.282,1820.737;Inherit;False;789.0012;362.734;;4;40;38;39;204;AO;0.5367647,0.355212,0.355212,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;215;35.51168,-958.5592;Inherit;False;OUT_Albedo;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;228;-1095.442,2782.931;Inherit;False;OUT_Alpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;64;-1916.846,-459.413;Inherit;False;1816.454;548.9569;;12;73;139;72;71;76;77;69;74;86;210;222;239;Normals;0,0.6275859,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;195;814.5529,-473.1964;Inherit;False;Property;_Metallic;Metallic;13;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;214;860.1408,-130.5753;Inherit;False;212;OUT_VertexPos;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;211;881.0494,-550.5397;Inherit;False;210;OUT_Normal;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;222;-844.5163,-28.19589;Inherit;False;220;VAR_BaseDetail;1;0;OBJECT;;False;1;INT;0
Node;AmplifyShaderEditor.LerpOp;231;-1291.721,1350.71;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;105;-1050.699,1347.364;Inherit;False;OUT_Smoothness;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.IntNode;181;811.1584,-1346.159;Inherit;False;Property;_CullMode;Cull Mode;2;1;[Enum];Create;True;3;Off;0;Front;1;Back;2;0;True;0;2;0;0;1;INT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;204;-1338.069,1942.672;Inherit;False;OUT_AO;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;230;885.9947,-202.7518;Inherit;False;228;OUT_Alpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;108;850.7147,-400.8286;Inherit;False;105;OUT_Smoothness;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;210;-315.2967,-196.1565;Inherit;False;OUT_Normal;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;237;907.5122,-315.1853;Inherit;False;204;OUT_AO;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;216;889.3597,-624.7513;Inherit;False;215;OUT_Albedo;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;76;-1573.367,-409.4128;Inherit;True;Property;_TextureSample1;Texture Sample 1;1;0;Create;True;0;0;False;0;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;239;-497.2007,-132.0802;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.UnpackScaleNormalNode;71;-1166.938,-317.9154;Inherit;False;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TexturePropertyNode;86;-1815.917,-117.044;Float;True;Property;_BumpMap;Normal;3;0;Create;False;0;0;False;1;Header(Normal Texture);None;None;True;bump;Auto;Texture2D;-1;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.RangedFloatNode;100;-1661.109,1489.689;Float;False;Property;_Glossiness;Smoothness;14;0;Create;False;0;0;False;0;0;0.209;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;73;-826.826,-264.6131;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;69;-1484.952,-209.7666;Half;False;Property;_BumpScale;Normal Strength;4;0;Create;False;0;0;False;0;1;1.22;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;38;-1852.334,2037.402;Half;False;Property;_OcclusionStrength;AO strength;12;0;Create;False;0;0;False;1;Header(Other Settings);0.6;0.682;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;39;-1849.282,1872.071;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.UnpackScaleNormalNode;72;-1164.148,-111.1343;Inherit;False;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.LerpOp;40;-1520.743,1948.201;Inherit;False;3;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;232;-1855.721,1345.118;Inherit;False;132;VAR_AlbedoTexture;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;139;-1174.484,-402.0046;Inherit;False;137;FUNC_BarkDamageBlend;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;74;-1829.845,-409.541;Float;True;Property;_DetailNormalMap;Detail Normal;8;0;Create;False;0;0;False;0;None;None;True;bump;Auto;Texture2D;-1;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.SamplerNode;77;-1575.864,-115.7556;Inherit;True;Property;_TextureSample2;Texture Sample 2;1;0;Create;True;0;0;False;0;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.BreakToComponentsNode;233;-1619.721,1350.118;Inherit;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;247;1462.745,-600.17;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;False;False;True;1;False;-1;True;3;False;-1;False;True;1;LightMode=ShadowCaster;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;248;1462.745,-600.17;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;DepthOnly;0;3;DepthOnly;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;True;False;False;False;False;0;False;-1;False;True;1;False;-1;False;False;True;1;LightMode=DepthOnly;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;249;1462.745,-600.17;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;Meta;0;4;Meta;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;True;2;False;-1;False;False;False;False;False;True;1;LightMode=Meta;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;245;1462.745,-600.17;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;ExtraPrePass;0;0;ExtraPrePass;5;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;True;0;False;-1;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;0;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;246;1462.745,-600.17;Float;False;True;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;2;GPUInstancer/Mtree/SRP/Bark URP;94348b07e5e8bab40bd6c8a1e3df54cd;True;Forward;0;1;Forward;12;False;False;False;True;0;True;181;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;True;1;1;False;-1;0;False;-1;1;1;False;-1;0;False;-1;False;False;False;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;LightMode=UniversalForward;False;0;Hidden/InternalErrorShader;0;0;Standard;13;Workflow;1;Surface;0;  Blend;0;Two Sided;1;Cast Shadows;1;Receive Shadows;1;GPU Instancing;1;LOD CrossFade;1;Built-in Fog;1;Meta Pass;1;Override Baked GI;0;Extra Pre Pass;0;Vertex Position,InvertActionOnDeselection;0;0;6;False;True;True;True;True;True;False;;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;250;1462.745,-600.17;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;Universal2D;0;5;Universal2D;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;True;1;1;False;-1;0;False;-1;1;1;False;-1;0;False;-1;False;False;False;True;True;True;True;True;0;False;-1;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;LightMode=Universal2D;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
WireConnection;79;0;80;0
WireConnection;132;0;79;0
WireConnection;93;0;133;0
WireConnection;92;0;93;0
WireConnection;92;1;93;1
WireConnection;92;2;93;2
WireConnection;81;0;92;0
WireConnection;84;0;81;0
WireConnection;84;1;83;0
WireConnection;90;0;82;1
WireConnection;90;1;68;0
WireConnection;85;0;90;0
WireConnection;85;1;84;0
WireConnection;66;0;85;0
WireConnection;66;1;87;0
WireConnection;70;0;66;0
WireConnection;67;0;78;0
WireConnection;137;0;70;0
WireConnection;88;0;65;0
WireConnection;88;1;67;0
WireConnection;94;0;132;0
WireConnection;94;1;75;0
WireConnection;220;0;176;0
WireConnection;89;0;88;0
WireConnection;89;1;94;0
WireConnection;89;2;138;0
WireConnection;227;0;226;0
WireConnection;238;0;89;0
WireConnection;238;1;94;0
WireConnection;238;2;221;0
WireConnection;225;41;227;3
WireConnection;212;0;236;0
WireConnection;215;0;238;0
WireConnection;228;0;225;0
WireConnection;231;1;233;0
WireConnection;231;2;100;0
WireConnection;105;0;231;0
WireConnection;204;0;40;0
WireConnection;210;0;239;0
WireConnection;76;0;74;0
WireConnection;239;0;73;0
WireConnection;239;1;72;0
WireConnection;239;2;222;0
WireConnection;71;0;76;0
WireConnection;71;1;69;0
WireConnection;73;0;71;0
WireConnection;73;1;72;0
WireConnection;73;2;139;0
WireConnection;72;0;77;0
WireConnection;72;1;69;0
WireConnection;40;1;39;4
WireConnection;40;2;38;0
WireConnection;77;0;86;0
WireConnection;233;0;232;0
WireConnection;246;0;216;0
WireConnection;246;1;211;0
WireConnection;246;3;195;0
WireConnection;246;4;108;0
WireConnection;246;5;237;0
WireConnection;246;6;230;0
WireConnection;246;8;214;0
ASEEND*/
//CHKSM=BEC504928CBFFEE7D0C6B7571F296883B4426C6C
