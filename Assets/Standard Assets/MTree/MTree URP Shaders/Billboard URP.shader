// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Mtree/SRP/Billboard URP"
{
	Properties
	{
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[Header(Albedo)]_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo", 2D) = "white" {}
		[Enum(Off,0,Front,1,Back,2)]_CullMode("Cull Mode", Int) = 2
		_Cutoff("Cutoff", Range( 0 , 1)) = 0.5
		_Metallic("Metallic", Range( 0 , 1)) = 0
		_Glossiness("Smoothness", Float) = 0
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
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define ASE_ABSOLUTE_VERTEX_POS 1
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
			#pragma multi_compile __ LOD_FADE_CROSSFADE


			float _WindStrength;
			float _RandomWindOffset;
			float _WindPulse;
			float _WindDirection;
			int BillboardWindEnabled;
			sampler2D _MainTex;
			CBUFFER_START( UnityPerMaterial )
			int _CullMode;
			float _GlobalWindInfluence;
			float4 _MainTex_ST;
			float4 _Color;
			float _Metallic;
			float _Glossiness;
			half _Cutoff;
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

				float3 VAR_VertexPosition21_g1 = mul( GetObjectToWorldMatrix(), float4( v.vertex.xyz , 0.0 ) ).xyz;
				float3 break109_g1 = VAR_VertexPosition21_g1;
				float VAR_WindStrength43_g1 = ( _WindStrength * _GlobalWindInfluence );
				float4 transform37_g1 = mul(GetObjectToWorldMatrix(),float4( 0,0,0,1 ));
				float2 appendResult38_g1 = (float2(transform37_g1.x , transform37_g1.z));
				float dotResult2_g2 = dot( appendResult38_g1 , float2( 12.9898,78.233 ) );
				float lerpResult8_g2 = lerp( 0.8 , ( ( _RandomWindOffset / 2.0 ) + 0.9 ) , frac( ( sin( dotResult2_g2 ) * 43758.55 ) ));
				float VAR_RandomTime16_g1 = ( ( _TimeParameters.x * 0.05 ) * lerpResult8_g2 );
				float FUNC_Turbulence36_g1 = ( sin( ( ( VAR_RandomTime16_g1 * 40.0 ) - ( VAR_VertexPosition21_g1.z / 15.0 ) ) ) * 0.5 );
				float VAR_WindPulse274_g1 = _WindPulse;
				float FUNC_Angle73_g1 = ( VAR_WindStrength43_g1 * ( 1.0 + sin( ( ( ( ( VAR_RandomTime16_g1 * 2.0 ) + FUNC_Turbulence36_g1 ) - ( VAR_VertexPosition21_g1.z / 50.0 ) ) - ( v.ase_color.r / 20.0 ) ) ) ) * sqrt( v.ase_color.r ) * 0.2 * VAR_WindPulse274_g1 );
				float VAR_SinA80_g1 = sin( FUNC_Angle73_g1 );
				float VAR_CosA78_g1 = cos( FUNC_Angle73_g1 );
				float _WindDirection164_g1 = _WindDirection;
				float2 localDirectionalEquation164_g1 = DirectionalEquation( _WindDirection164_g1 );
				float2 break165_g1 = localDirectionalEquation164_g1;
				float VAR_xLerp83_g1 = break165_g1.x;
				float lerpResult118_g1 = lerp( break109_g1.x , ( ( break109_g1.y * VAR_SinA80_g1 ) + ( break109_g1.x * VAR_CosA78_g1 ) ) , VAR_xLerp83_g1);
				float3 break98_g1 = VAR_VertexPosition21_g1;
				float3 break105_g1 = VAR_VertexPosition21_g1;
				float VAR_zLerp95_g1 = break165_g1.y;
				float lerpResult120_g1 = lerp( break105_g1.z , ( ( break105_g1.y * VAR_SinA80_g1 ) + ( break105_g1.z * VAR_CosA78_g1 ) ) , VAR_zLerp95_g1);
				float3 appendResult122_g1 = (float3(lerpResult118_g1 , ( ( break98_g1.y * VAR_CosA78_g1 ) - ( break98_g1.z * VAR_SinA80_g1 ) ) , lerpResult120_g1));
				float3 FUNC_vertexPos123_g1 = appendResult122_g1;
				float3 temp_output_5_0_g1 = mul( GetWorldToObjectMatrix(), float4( FUNC_vertexPos123_g1 , 0.0 ) ).xyz;
				float3 lerpResult61 = lerp( temp_output_5_0_g1 , v.vertex.xyz , (float)BillboardWindEnabled);
				float3 OUT_VertexPos43 = lerpResult61;
				
				float4 ase_clipPos = TransformObjectToHClip((v.vertex).xyz);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				o.ase_texcoord7 = screenPos;
				
				o.ase_texcoord6.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord6.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = OUT_VertexPos43;
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

				float2 uv_MainTex = IN.ase_texcoord6.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				float4 tex2DNode3 = tex2D( _MainTex, uv_MainTex );
				float4 VAR_AlbedoTexture53 = tex2DNode3;
				float4 OUT_Albedo45 = ( VAR_AlbedoTexture53 * _Color );
				
				float lerpResult55 = lerp( 0.0 , VAR_AlbedoTexture53.r , _Glossiness);
				float OUT_Smoothness58 = lerpResult55;
				
				clip( tex2DNode3.a - _Cutoff);
				float temp_output_41_0_g3 = tex2DNode3.a;
				float4 screenPos = IN.ase_texcoord7;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float2 clipScreen45_g3 = ase_screenPosNorm.xy * _ScreenParams.xy;
				float dither45_g3 = Dither8x8Bayer( fmod(clipScreen45_g3.x, 8), fmod(clipScreen45_g3.y, 8) );
				dither45_g3 = step( dither45_g3, unity_LODFade.x );
				#ifdef LOD_FADE_CROSSFADE
				float staticSwitch40_g3 = ( temp_output_41_0_g3 * dither45_g3 );
				#else
				float staticSwitch40_g3 = temp_output_41_0_g3;
				#endif
				float OUT_Alpha46 = staticSwitch40_g3;
				
				float3 Albedo = OUT_Albedo45.rgb;
				float3 Normal = float3(0, 0, 1);
				float3 Emission = 0;
				float3 Specular = 0.5;
				float Metallic = _Metallic;
				float Smoothness = OUT_Smoothness58;
				float Occlusion = OUT_Alpha46;
				float Alpha = 1;
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
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define ASE_ABSOLUTE_VERTEX_POS 1
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
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			float _WindStrength;
			float _RandomWindOffset;
			float _WindPulse;
			float _WindDirection;
			int BillboardWindEnabled;
			CBUFFER_START( UnityPerMaterial )
			int _CullMode;
			float _GlobalWindInfluence;
			float4 _MainTex_ST;
			float4 _Color;
			float _Metallic;
			float _Glossiness;
			half _Cutoff;
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
			

			float3 _LightDirection;

			VertexOutput ShadowPassVertex( VertexInput v )
			{
				VertexOutput o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );

				float3 VAR_VertexPosition21_g1 = mul( GetObjectToWorldMatrix(), float4( v.vertex.xyz , 0.0 ) ).xyz;
				float3 break109_g1 = VAR_VertexPosition21_g1;
				float VAR_WindStrength43_g1 = ( _WindStrength * _GlobalWindInfluence );
				float4 transform37_g1 = mul(GetObjectToWorldMatrix(),float4( 0,0,0,1 ));
				float2 appendResult38_g1 = (float2(transform37_g1.x , transform37_g1.z));
				float dotResult2_g2 = dot( appendResult38_g1 , float2( 12.9898,78.233 ) );
				float lerpResult8_g2 = lerp( 0.8 , ( ( _RandomWindOffset / 2.0 ) + 0.9 ) , frac( ( sin( dotResult2_g2 ) * 43758.55 ) ));
				float VAR_RandomTime16_g1 = ( ( _TimeParameters.x * 0.05 ) * lerpResult8_g2 );
				float FUNC_Turbulence36_g1 = ( sin( ( ( VAR_RandomTime16_g1 * 40.0 ) - ( VAR_VertexPosition21_g1.z / 15.0 ) ) ) * 0.5 );
				float VAR_WindPulse274_g1 = _WindPulse;
				float FUNC_Angle73_g1 = ( VAR_WindStrength43_g1 * ( 1.0 + sin( ( ( ( ( VAR_RandomTime16_g1 * 2.0 ) + FUNC_Turbulence36_g1 ) - ( VAR_VertexPosition21_g1.z / 50.0 ) ) - ( v.ase_color.r / 20.0 ) ) ) ) * sqrt( v.ase_color.r ) * 0.2 * VAR_WindPulse274_g1 );
				float VAR_SinA80_g1 = sin( FUNC_Angle73_g1 );
				float VAR_CosA78_g1 = cos( FUNC_Angle73_g1 );
				float _WindDirection164_g1 = _WindDirection;
				float2 localDirectionalEquation164_g1 = DirectionalEquation( _WindDirection164_g1 );
				float2 break165_g1 = localDirectionalEquation164_g1;
				float VAR_xLerp83_g1 = break165_g1.x;
				float lerpResult118_g1 = lerp( break109_g1.x , ( ( break109_g1.y * VAR_SinA80_g1 ) + ( break109_g1.x * VAR_CosA78_g1 ) ) , VAR_xLerp83_g1);
				float3 break98_g1 = VAR_VertexPosition21_g1;
				float3 break105_g1 = VAR_VertexPosition21_g1;
				float VAR_zLerp95_g1 = break165_g1.y;
				float lerpResult120_g1 = lerp( break105_g1.z , ( ( break105_g1.y * VAR_SinA80_g1 ) + ( break105_g1.z * VAR_CosA78_g1 ) ) , VAR_zLerp95_g1);
				float3 appendResult122_g1 = (float3(lerpResult118_g1 , ( ( break98_g1.y * VAR_CosA78_g1 ) - ( break98_g1.z * VAR_SinA80_g1 ) ) , lerpResult120_g1));
				float3 FUNC_vertexPos123_g1 = appendResult122_g1;
				float3 temp_output_5_0_g1 = mul( GetWorldToObjectMatrix(), float4( FUNC_vertexPos123_g1 , 0.0 ) ).xyz;
				float3 lerpResult61 = lerp( temp_output_5_0_g1 , v.vertex.xyz , (float)BillboardWindEnabled);
				float3 OUT_VertexPos43 = lerpResult61;
				
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = OUT_VertexPos43;
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

				
				float Alpha = 1;
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
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define ASE_ABSOLUTE_VERTEX_POS 1
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
			int BillboardWindEnabled;
			CBUFFER_START( UnityPerMaterial )
			int _CullMode;
			float _GlobalWindInfluence;
			float4 _MainTex_ST;
			float4 _Color;
			float _Metallic;
			float _Glossiness;
			half _Cutoff;
			CBUFFER_END


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_color : COLOR;
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
			

			VertexOutput vert( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float3 VAR_VertexPosition21_g1 = mul( GetObjectToWorldMatrix(), float4( v.vertex.xyz , 0.0 ) ).xyz;
				float3 break109_g1 = VAR_VertexPosition21_g1;
				float VAR_WindStrength43_g1 = ( _WindStrength * _GlobalWindInfluence );
				float4 transform37_g1 = mul(GetObjectToWorldMatrix(),float4( 0,0,0,1 ));
				float2 appendResult38_g1 = (float2(transform37_g1.x , transform37_g1.z));
				float dotResult2_g2 = dot( appendResult38_g1 , float2( 12.9898,78.233 ) );
				float lerpResult8_g2 = lerp( 0.8 , ( ( _RandomWindOffset / 2.0 ) + 0.9 ) , frac( ( sin( dotResult2_g2 ) * 43758.55 ) ));
				float VAR_RandomTime16_g1 = ( ( _TimeParameters.x * 0.05 ) * lerpResult8_g2 );
				float FUNC_Turbulence36_g1 = ( sin( ( ( VAR_RandomTime16_g1 * 40.0 ) - ( VAR_VertexPosition21_g1.z / 15.0 ) ) ) * 0.5 );
				float VAR_WindPulse274_g1 = _WindPulse;
				float FUNC_Angle73_g1 = ( VAR_WindStrength43_g1 * ( 1.0 + sin( ( ( ( ( VAR_RandomTime16_g1 * 2.0 ) + FUNC_Turbulence36_g1 ) - ( VAR_VertexPosition21_g1.z / 50.0 ) ) - ( v.ase_color.r / 20.0 ) ) ) ) * sqrt( v.ase_color.r ) * 0.2 * VAR_WindPulse274_g1 );
				float VAR_SinA80_g1 = sin( FUNC_Angle73_g1 );
				float VAR_CosA78_g1 = cos( FUNC_Angle73_g1 );
				float _WindDirection164_g1 = _WindDirection;
				float2 localDirectionalEquation164_g1 = DirectionalEquation( _WindDirection164_g1 );
				float2 break165_g1 = localDirectionalEquation164_g1;
				float VAR_xLerp83_g1 = break165_g1.x;
				float lerpResult118_g1 = lerp( break109_g1.x , ( ( break109_g1.y * VAR_SinA80_g1 ) + ( break109_g1.x * VAR_CosA78_g1 ) ) , VAR_xLerp83_g1);
				float3 break98_g1 = VAR_VertexPosition21_g1;
				float3 break105_g1 = VAR_VertexPosition21_g1;
				float VAR_zLerp95_g1 = break165_g1.y;
				float lerpResult120_g1 = lerp( break105_g1.z , ( ( break105_g1.y * VAR_SinA80_g1 ) + ( break105_g1.z * VAR_CosA78_g1 ) ) , VAR_zLerp95_g1);
				float3 appendResult122_g1 = (float3(lerpResult118_g1 , ( ( break98_g1.y * VAR_CosA78_g1 ) - ( break98_g1.z * VAR_SinA80_g1 ) ) , lerpResult120_g1));
				float3 FUNC_vertexPos123_g1 = appendResult122_g1;
				float3 temp_output_5_0_g1 = mul( GetWorldToObjectMatrix(), float4( FUNC_vertexPos123_g1 , 0.0 ) ).xyz;
				float3 lerpResult61 = lerp( temp_output_5_0_g1 , v.vertex.xyz , (float)BillboardWindEnabled);
				float3 OUT_VertexPos43 = lerpResult61;
				
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = OUT_VertexPos43;
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

				
				float Alpha = 1;
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
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define ASE_ABSOLUTE_VERTEX_POS 1
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
			int BillboardWindEnabled;
			sampler2D _MainTex;
			CBUFFER_START( UnityPerMaterial )
			int _CullMode;
			float _GlobalWindInfluence;
			float4 _MainTex_ST;
			float4 _Color;
			float _Metallic;
			float _Glossiness;
			half _Cutoff;
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
			

			VertexOutput vert( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float3 VAR_VertexPosition21_g1 = mul( GetObjectToWorldMatrix(), float4( v.vertex.xyz , 0.0 ) ).xyz;
				float3 break109_g1 = VAR_VertexPosition21_g1;
				float VAR_WindStrength43_g1 = ( _WindStrength * _GlobalWindInfluence );
				float4 transform37_g1 = mul(GetObjectToWorldMatrix(),float4( 0,0,0,1 ));
				float2 appendResult38_g1 = (float2(transform37_g1.x , transform37_g1.z));
				float dotResult2_g2 = dot( appendResult38_g1 , float2( 12.9898,78.233 ) );
				float lerpResult8_g2 = lerp( 0.8 , ( ( _RandomWindOffset / 2.0 ) + 0.9 ) , frac( ( sin( dotResult2_g2 ) * 43758.55 ) ));
				float VAR_RandomTime16_g1 = ( ( _TimeParameters.x * 0.05 ) * lerpResult8_g2 );
				float FUNC_Turbulence36_g1 = ( sin( ( ( VAR_RandomTime16_g1 * 40.0 ) - ( VAR_VertexPosition21_g1.z / 15.0 ) ) ) * 0.5 );
				float VAR_WindPulse274_g1 = _WindPulse;
				float FUNC_Angle73_g1 = ( VAR_WindStrength43_g1 * ( 1.0 + sin( ( ( ( ( VAR_RandomTime16_g1 * 2.0 ) + FUNC_Turbulence36_g1 ) - ( VAR_VertexPosition21_g1.z / 50.0 ) ) - ( v.ase_color.r / 20.0 ) ) ) ) * sqrt( v.ase_color.r ) * 0.2 * VAR_WindPulse274_g1 );
				float VAR_SinA80_g1 = sin( FUNC_Angle73_g1 );
				float VAR_CosA78_g1 = cos( FUNC_Angle73_g1 );
				float _WindDirection164_g1 = _WindDirection;
				float2 localDirectionalEquation164_g1 = DirectionalEquation( _WindDirection164_g1 );
				float2 break165_g1 = localDirectionalEquation164_g1;
				float VAR_xLerp83_g1 = break165_g1.x;
				float lerpResult118_g1 = lerp( break109_g1.x , ( ( break109_g1.y * VAR_SinA80_g1 ) + ( break109_g1.x * VAR_CosA78_g1 ) ) , VAR_xLerp83_g1);
				float3 break98_g1 = VAR_VertexPosition21_g1;
				float3 break105_g1 = VAR_VertexPosition21_g1;
				float VAR_zLerp95_g1 = break165_g1.y;
				float lerpResult120_g1 = lerp( break105_g1.z , ( ( break105_g1.y * VAR_SinA80_g1 ) + ( break105_g1.z * VAR_CosA78_g1 ) ) , VAR_zLerp95_g1);
				float3 appendResult122_g1 = (float3(lerpResult118_g1 , ( ( break98_g1.y * VAR_CosA78_g1 ) - ( break98_g1.z * VAR_SinA80_g1 ) ) , lerpResult120_g1));
				float3 FUNC_vertexPos123_g1 = appendResult122_g1;
				float3 temp_output_5_0_g1 = mul( GetWorldToObjectMatrix(), float4( FUNC_vertexPos123_g1 , 0.0 ) ).xyz;
				float3 lerpResult61 = lerp( temp_output_5_0_g1 , v.vertex.xyz , (float)BillboardWindEnabled);
				float3 OUT_VertexPos43 = lerpResult61;
				
				o.ase_texcoord2.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord2.zw = 0;
				
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = OUT_VertexPos43;
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

				float2 uv_MainTex = IN.ase_texcoord2.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				float4 tex2DNode3 = tex2D( _MainTex, uv_MainTex );
				float4 VAR_AlbedoTexture53 = tex2DNode3;
				float4 OUT_Albedo45 = ( VAR_AlbedoTexture53 * _Color );
				
				
				float3 Albedo = OUT_Albedo45.rgb;
				float3 Emission = 0;
				float Alpha = 1;
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
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define ASE_ABSOLUTE_VERTEX_POS 1
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
			int BillboardWindEnabled;
			sampler2D _MainTex;
			CBUFFER_START( UnityPerMaterial )
			int _CullMode;
			float _GlobalWindInfluence;
			float4 _MainTex_ST;
			float4 _Color;
			float _Metallic;
			float _Glossiness;
			half _Cutoff;
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
			

			VertexOutput vert( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );

				float3 VAR_VertexPosition21_g1 = mul( GetObjectToWorldMatrix(), float4( v.vertex.xyz , 0.0 ) ).xyz;
				float3 break109_g1 = VAR_VertexPosition21_g1;
				float VAR_WindStrength43_g1 = ( _WindStrength * _GlobalWindInfluence );
				float4 transform37_g1 = mul(GetObjectToWorldMatrix(),float4( 0,0,0,1 ));
				float2 appendResult38_g1 = (float2(transform37_g1.x , transform37_g1.z));
				float dotResult2_g2 = dot( appendResult38_g1 , float2( 12.9898,78.233 ) );
				float lerpResult8_g2 = lerp( 0.8 , ( ( _RandomWindOffset / 2.0 ) + 0.9 ) , frac( ( sin( dotResult2_g2 ) * 43758.55 ) ));
				float VAR_RandomTime16_g1 = ( ( _TimeParameters.x * 0.05 ) * lerpResult8_g2 );
				float FUNC_Turbulence36_g1 = ( sin( ( ( VAR_RandomTime16_g1 * 40.0 ) - ( VAR_VertexPosition21_g1.z / 15.0 ) ) ) * 0.5 );
				float VAR_WindPulse274_g1 = _WindPulse;
				float FUNC_Angle73_g1 = ( VAR_WindStrength43_g1 * ( 1.0 + sin( ( ( ( ( VAR_RandomTime16_g1 * 2.0 ) + FUNC_Turbulence36_g1 ) - ( VAR_VertexPosition21_g1.z / 50.0 ) ) - ( v.ase_color.r / 20.0 ) ) ) ) * sqrt( v.ase_color.r ) * 0.2 * VAR_WindPulse274_g1 );
				float VAR_SinA80_g1 = sin( FUNC_Angle73_g1 );
				float VAR_CosA78_g1 = cos( FUNC_Angle73_g1 );
				float _WindDirection164_g1 = _WindDirection;
				float2 localDirectionalEquation164_g1 = DirectionalEquation( _WindDirection164_g1 );
				float2 break165_g1 = localDirectionalEquation164_g1;
				float VAR_xLerp83_g1 = break165_g1.x;
				float lerpResult118_g1 = lerp( break109_g1.x , ( ( break109_g1.y * VAR_SinA80_g1 ) + ( break109_g1.x * VAR_CosA78_g1 ) ) , VAR_xLerp83_g1);
				float3 break98_g1 = VAR_VertexPosition21_g1;
				float3 break105_g1 = VAR_VertexPosition21_g1;
				float VAR_zLerp95_g1 = break165_g1.y;
				float lerpResult120_g1 = lerp( break105_g1.z , ( ( break105_g1.y * VAR_SinA80_g1 ) + ( break105_g1.z * VAR_CosA78_g1 ) ) , VAR_zLerp95_g1);
				float3 appendResult122_g1 = (float3(lerpResult118_g1 , ( ( break98_g1.y * VAR_CosA78_g1 ) - ( break98_g1.z * VAR_SinA80_g1 ) ) , lerpResult120_g1));
				float3 FUNC_vertexPos123_g1 = appendResult122_g1;
				float3 temp_output_5_0_g1 = mul( GetWorldToObjectMatrix(), float4( FUNC_vertexPos123_g1 , 0.0 ) ).xyz;
				float3 lerpResult61 = lerp( temp_output_5_0_g1 , v.vertex.xyz , (float)BillboardWindEnabled);
				float3 OUT_VertexPos43 = lerpResult61;
				
				o.ase_texcoord2.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord2.zw = 0;
				
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = OUT_VertexPos43;
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

				float2 uv_MainTex = IN.ase_texcoord2.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				float4 tex2DNode3 = tex2D( _MainTex, uv_MainTex );
				float4 VAR_AlbedoTexture53 = tex2DNode3;
				float4 OUT_Albedo45 = ( VAR_AlbedoTexture53 * _Color );
				
				
				float3 Albedo = OUT_Albedo45.rgb;
				float Alpha = 1;
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
532;63;1008;682;1735.445;746.8516;1.48034;True;False
Node;AmplifyShaderEditor.CommentaryNode;1;-1358.361,4.554405;Inherit;False;1661.862;599.5559;;10;45;46;47;12;9;4;3;8;2;53;Albedo / Alpha;1,0.1254902,0.1254902,1;0;0
Node;AmplifyShaderEditor.TexturePropertyNode;2;-1308.361,54.55442;Float;True;Property;_MainTex;Albedo;1;0;Create;False;0;0;False;0;None;None;False;white;Auto;Texture2D;-1;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.SamplerNode;3;-988.5101,68.42551;Inherit;True;Property;_TextureSample0;Texture Sample 0;0;0;Create;True;0;0;False;0;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;42;-1336.592,755.2562;Inherit;False;1173.464;527.1923;;5;41;25;23;43;61;Vertex Pos;0,1,0.08965516,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;53;-625.1459,69.20842;Inherit;False;VAR_AlbedoTexture;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;8;-897.5212,258.5437;Inherit;False;Property;_Color;Color;0;0;Create;True;0;0;False;1;Header(Albedo);1,1,1,1;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.IntNode;25;-1289.592,1110.256;Inherit;False;Global;BillboardWindEnabled;BillboardWindEnabled;3;1;[Enum];Create;True;2;On;0;Off;1;0;False;0;0;1;0;1;INT;0
Node;AmplifyShaderEditor.FunctionNode;41;-1227.852,900.655;Inherit;False;Mtree Wind;6;;1;d710ffc7589a70c42a3e6c5220c6279d;7,282,0,280,0,278,0,255,0,269,1,281,0,272,0;0;1;FLOAT3;0
Node;AmplifyShaderEditor.PosVertexDataNode;23;-1242.157,969.1607;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;9;-290.8997,132.6499;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;61;-926.8926,932.368;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;51;-1297.001,-467.6064;Inherit;False;275;251.3827;;2;6;28;Variables;1,0,0.7241378,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;59;-1309.772,1486.662;Inherit;False;1107.456;340.4749;;5;54;55;57;58;56;Smoothness;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;45;-74.94881,127.5761;Inherit;False;OUT_Albedo;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode;50;890.5921,57.31693;Inherit;False;787.6993;508.11;;6;40;48;44;49;60;68;Output;0,0,0,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;43;-644.763,926.1811;Inherit;False;OUT_VertexPos;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.IntNode;28;-1236.955,-331.2237;Inherit;False;Property;_CullMode;Cull Mode;2;1;[Enum];Create;True;3;Off;0;Front;1;Back;2;0;True;0;2;0;0;1;INT;0
Node;AmplifyShaderEditor.GetLocalVarNode;56;-1259.772,1536.662;Inherit;False;53;VAR_AlbedoTexture;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;6;-1247.001,-417.6064;Inherit;False;Constant;_MaskClipValue;Mask Clip Value;14;1;[HideInInspector];Create;True;0;0;False;0;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;54;-1258.275,1712.136;Inherit;False;Property;_Glossiness;Smoothness;5;0;Create;False;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;40;940.592,205.6661;Inherit;False;Property;_Metallic;Metallic;4;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;55;-703.8078,1652.054;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;58;-442.3161,1648.562;Inherit;False;OUT_Smoothness;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;47;-283.9381,373.024;Inherit;False;LOD CrossFade;-1;;3;bbfabe35be0e79d438adaa880ee1b0aa;1,44,1;1;41;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;60;977.1042,293.0038;Inherit;False;58;OUT_Smoothness;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;46;-82.57029,367.8114;Inherit;False;OUT_Alpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;4;-957.0894,434.1565;Half;False;Property;_Cutoff;Cutoff;3;0;Create;True;0;0;False;0;0.5;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClipNode;12;-529.3309,372.2194;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;49;1014.521,369.8521;Inherit;False;46;OUT_Alpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;48;1015.524,116.2522;Inherit;False;45;OUT_Albedo;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.BreakToComponentsNode;57;-975.6139,1575.474;Inherit;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.GetLocalVarNode;44;984.2899,450.4269;Inherit;False;43;OUT_VertexPos;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;67;1411.291,107.3169;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;ExtraPrePass;0;0;ExtraPrePass;5;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;True;0;False;-1;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;0;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;69;1411.291,107.3169;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;False;False;True;1;False;-1;True;3;False;-1;False;True;1;LightMode=ShadowCaster;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;70;1411.291,107.3169;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;DepthOnly;0;3;DepthOnly;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;True;False;False;False;False;0;False;-1;False;True;1;False;-1;False;False;True;1;LightMode=DepthOnly;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;71;1411.291,107.3169;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;Meta;0;4;Meta;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;True;2;False;-1;False;False;False;False;False;True;1;LightMode=Meta;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;68;1411.291,107.3169;Float;False;True;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;2;Mtree/SRP/Billboard URP;94348b07e5e8bab40bd6c8a1e3df54cd;True;Forward;0;1;Forward;12;False;False;False;True;0;True;28;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;True;1;1;False;-1;0;False;-1;1;1;False;-1;0;False;-1;False;False;False;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;LightMode=UniversalForward;False;0;Hidden/InternalErrorShader;0;0;Standard;13;Workflow;1;Surface;0;  Blend;0;Two Sided;1;Cast Shadows;1;Receive Shadows;1;GPU Instancing;1;LOD CrossFade;1;Built-in Fog;1;Meta Pass;1;Override Baked GI;0;Extra Pre Pass;0;Vertex Position,InvertActionOnDeselection;0;0;6;False;True;True;True;True;True;False;;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;72;1411.291,107.3169;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;Universal2D;0;5;Universal2D;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;True;1;1;False;-1;0;False;-1;1;1;False;-1;0;False;-1;False;False;False;True;True;True;True;True;0;False;-1;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;LightMode=Universal2D;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
WireConnection;3;0;2;0
WireConnection;53;0;3;0
WireConnection;9;0;53;0
WireConnection;9;1;8;0
WireConnection;61;0;41;0
WireConnection;61;1;23;0
WireConnection;61;2;25;0
WireConnection;45;0;9;0
WireConnection;43;0;61;0
WireConnection;55;1;57;0
WireConnection;55;2;54;0
WireConnection;58;0;55;0
WireConnection;47;41;12;0
WireConnection;46;0;47;0
WireConnection;12;0;3;4
WireConnection;12;1;3;4
WireConnection;12;2;4;0
WireConnection;57;0;56;0
WireConnection;68;0;48;0
WireConnection;68;3;40;0
WireConnection;68;4;60;0
WireConnection;68;5;49;0
WireConnection;68;8;44;0
ASEEND*/
//CHKSM=6032DBB7BEBA81C62645D15C54B56E2D80831C76