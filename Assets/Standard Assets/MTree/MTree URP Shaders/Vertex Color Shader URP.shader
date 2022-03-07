// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Hidden/Mtree/SRP/VertexColorShader URP"
{
	Properties
	{
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[Header(Wind)]_GlobalWindInfluence("Global Wind Influence", Range( 0 , 1)) = 1
		[Enum(Off,0,Front,1,Back,2)]_CullMode("Cull Mode", Int) = 2

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


			float _WindStrength;
			float _RandomWindOffset;
			float _WindPulse;
			float _WindDirection;
			CBUFFER_START( UnityPerMaterial )
			int _CullMode;
			float _GlobalWindInfluence;
			CBUFFER_END


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_tangent : TANGENT;
				float4 texcoord1 : TEXCOORD1;
				float4 ase_color : COLOR;
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
				float4 ase_color : COLOR;
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
			

			VertexOutput vert ( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float3 VAR_VertexPosition21_g2 = mul( GetObjectToWorldMatrix(), float4( v.vertex.xyz , 0.0 ) ).xyz;
				float3 break109_g2 = VAR_VertexPosition21_g2;
				float VAR_WindStrength43_g2 = ( _WindStrength * _GlobalWindInfluence );
				float4 transform37_g2 = mul(GetObjectToWorldMatrix(),float4( 0,0,0,1 ));
				float2 appendResult38_g2 = (float2(transform37_g2.x , transform37_g2.z));
				float dotResult2_g3 = dot( appendResult38_g2 , float2( 12.9898,78.233 ) );
				float lerpResult8_g3 = lerp( 0.8 , ( ( _RandomWindOffset / 2.0 ) + 0.9 ) , frac( ( sin( dotResult2_g3 ) * 43758.55 ) ));
				float VAR_RandomTime16_g2 = ( ( _TimeParameters.x * 0.05 ) * lerpResult8_g3 );
				float FUNC_Turbulence36_g2 = ( sin( ( ( VAR_RandomTime16_g2 * 40.0 ) - ( VAR_VertexPosition21_g2.z / 15.0 ) ) ) * 0.5 );
				float VAR_WindPulse274_g2 = _WindPulse;
				float FUNC_Angle73_g2 = ( VAR_WindStrength43_g2 * ( 1.0 + sin( ( ( ( ( VAR_RandomTime16_g2 * 2.0 ) + FUNC_Turbulence36_g2 ) - ( VAR_VertexPosition21_g2.z / 50.0 ) ) - ( v.ase_color.r / 20.0 ) ) ) ) * sqrt( v.ase_color.r ) * 0.2 * VAR_WindPulse274_g2 );
				float VAR_SinA80_g2 = sin( FUNC_Angle73_g2 );
				float VAR_CosA78_g2 = cos( FUNC_Angle73_g2 );
				float _WindDirection164_g2 = _WindDirection;
				float2 localDirectionalEquation164_g2 = DirectionalEquation( _WindDirection164_g2 );
				float2 break165_g2 = localDirectionalEquation164_g2;
				float VAR_xLerp83_g2 = break165_g2.x;
				float lerpResult118_g2 = lerp( break109_g2.x , ( ( break109_g2.y * VAR_SinA80_g2 ) + ( break109_g2.x * VAR_CosA78_g2 ) ) , VAR_xLerp83_g2);
				float3 break98_g2 = VAR_VertexPosition21_g2;
				float3 break105_g2 = VAR_VertexPosition21_g2;
				float VAR_zLerp95_g2 = break165_g2.y;
				float lerpResult120_g2 = lerp( break105_g2.z , ( ( break105_g2.y * VAR_SinA80_g2 ) + ( break105_g2.z * VAR_CosA78_g2 ) ) , VAR_zLerp95_g2);
				float3 appendResult122_g2 = (float3(lerpResult118_g2 , ( ( break98_g2.y * VAR_CosA78_g2 ) - ( break98_g2.z * VAR_SinA80_g2 ) ) , lerpResult120_g2));
				float3 FUNC_vertexPos123_g2 = appendResult122_g2;
				float3 break221_g2 = FUNC_vertexPos123_g2;
				half FUNC_SinFunction195_g2 = sin( ( ( VAR_RandomTime16_g2 * 200.0 * ( 0.2 + v.ase_color.g ) ) + ( v.ase_color.g * 10.0 ) + FUNC_Turbulence36_g2 + ( VAR_VertexPosition21_g2.z / 2.0 ) ) );
				float temp_output_202_0_g2 = ( FUNC_SinFunction195_g2 * v.ase_color.b * ( FUNC_Angle73_g2 + ( VAR_WindStrength43_g2 / 200.0 ) ) );
				float lerpResult203_g2 = lerp( 0.0 , temp_output_202_0_g2 , VAR_xLerp83_g2);
				float lerpResult196_g2 = lerp( 0.0 , temp_output_202_0_g2 , VAR_zLerp95_g2);
				float3 appendResult197_g2 = (float3(( break221_g2.x + lerpResult203_g2 ) , break221_g2.y , ( break221_g2.z + lerpResult196_g2 )));
				float3 OUT_Grass_Standalone245_g2 = appendResult197_g2;
				float3 temp_output_5_0_g2 = mul( GetWorldToObjectMatrix(), float4( OUT_Grass_Standalone245_g2 , 0.0 ) ).xyz;
				
				o.ase_color = v.ase_color;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = temp_output_5_0_g2;
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

				
				float3 Albedo = IN.ase_color.rgb;
				float3 Normal = float3(0, 0, 1);
				float3 Emission = 0;
				float3 Specular = 0.5;
				float Metallic = 0;
				float Smoothness = 0.5;
				float Occlusion = 1;
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
			CBUFFER_START( UnityPerMaterial )
			int _CullMode;
			float _GlobalWindInfluence;
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

				float3 VAR_VertexPosition21_g2 = mul( GetObjectToWorldMatrix(), float4( v.vertex.xyz , 0.0 ) ).xyz;
				float3 break109_g2 = VAR_VertexPosition21_g2;
				float VAR_WindStrength43_g2 = ( _WindStrength * _GlobalWindInfluence );
				float4 transform37_g2 = mul(GetObjectToWorldMatrix(),float4( 0,0,0,1 ));
				float2 appendResult38_g2 = (float2(transform37_g2.x , transform37_g2.z));
				float dotResult2_g3 = dot( appendResult38_g2 , float2( 12.9898,78.233 ) );
				float lerpResult8_g3 = lerp( 0.8 , ( ( _RandomWindOffset / 2.0 ) + 0.9 ) , frac( ( sin( dotResult2_g3 ) * 43758.55 ) ));
				float VAR_RandomTime16_g2 = ( ( _TimeParameters.x * 0.05 ) * lerpResult8_g3 );
				float FUNC_Turbulence36_g2 = ( sin( ( ( VAR_RandomTime16_g2 * 40.0 ) - ( VAR_VertexPosition21_g2.z / 15.0 ) ) ) * 0.5 );
				float VAR_WindPulse274_g2 = _WindPulse;
				float FUNC_Angle73_g2 = ( VAR_WindStrength43_g2 * ( 1.0 + sin( ( ( ( ( VAR_RandomTime16_g2 * 2.0 ) + FUNC_Turbulence36_g2 ) - ( VAR_VertexPosition21_g2.z / 50.0 ) ) - ( v.ase_color.r / 20.0 ) ) ) ) * sqrt( v.ase_color.r ) * 0.2 * VAR_WindPulse274_g2 );
				float VAR_SinA80_g2 = sin( FUNC_Angle73_g2 );
				float VAR_CosA78_g2 = cos( FUNC_Angle73_g2 );
				float _WindDirection164_g2 = _WindDirection;
				float2 localDirectionalEquation164_g2 = DirectionalEquation( _WindDirection164_g2 );
				float2 break165_g2 = localDirectionalEquation164_g2;
				float VAR_xLerp83_g2 = break165_g2.x;
				float lerpResult118_g2 = lerp( break109_g2.x , ( ( break109_g2.y * VAR_SinA80_g2 ) + ( break109_g2.x * VAR_CosA78_g2 ) ) , VAR_xLerp83_g2);
				float3 break98_g2 = VAR_VertexPosition21_g2;
				float3 break105_g2 = VAR_VertexPosition21_g2;
				float VAR_zLerp95_g2 = break165_g2.y;
				float lerpResult120_g2 = lerp( break105_g2.z , ( ( break105_g2.y * VAR_SinA80_g2 ) + ( break105_g2.z * VAR_CosA78_g2 ) ) , VAR_zLerp95_g2);
				float3 appendResult122_g2 = (float3(lerpResult118_g2 , ( ( break98_g2.y * VAR_CosA78_g2 ) - ( break98_g2.z * VAR_SinA80_g2 ) ) , lerpResult120_g2));
				float3 FUNC_vertexPos123_g2 = appendResult122_g2;
				float3 break221_g2 = FUNC_vertexPos123_g2;
				half FUNC_SinFunction195_g2 = sin( ( ( VAR_RandomTime16_g2 * 200.0 * ( 0.2 + v.ase_color.g ) ) + ( v.ase_color.g * 10.0 ) + FUNC_Turbulence36_g2 + ( VAR_VertexPosition21_g2.z / 2.0 ) ) );
				float temp_output_202_0_g2 = ( FUNC_SinFunction195_g2 * v.ase_color.b * ( FUNC_Angle73_g2 + ( VAR_WindStrength43_g2 / 200.0 ) ) );
				float lerpResult203_g2 = lerp( 0.0 , temp_output_202_0_g2 , VAR_xLerp83_g2);
				float lerpResult196_g2 = lerp( 0.0 , temp_output_202_0_g2 , VAR_zLerp95_g2);
				float3 appendResult197_g2 = (float3(( break221_g2.x + lerpResult203_g2 ) , break221_g2.y , ( break221_g2.z + lerpResult196_g2 )));
				float3 OUT_Grass_Standalone245_g2 = appendResult197_g2;
				float3 temp_output_5_0_g2 = mul( GetWorldToObjectMatrix(), float4( OUT_Grass_Standalone245_g2 , 0.0 ) ).xyz;
				
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = temp_output_5_0_g2;
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


			float _WindStrength;
			float _RandomWindOffset;
			float _WindPulse;
			float _WindDirection;
			CBUFFER_START( UnityPerMaterial )
			int _CullMode;
			float _GlobalWindInfluence;
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

				float3 VAR_VertexPosition21_g2 = mul( GetObjectToWorldMatrix(), float4( v.vertex.xyz , 0.0 ) ).xyz;
				float3 break109_g2 = VAR_VertexPosition21_g2;
				float VAR_WindStrength43_g2 = ( _WindStrength * _GlobalWindInfluence );
				float4 transform37_g2 = mul(GetObjectToWorldMatrix(),float4( 0,0,0,1 ));
				float2 appendResult38_g2 = (float2(transform37_g2.x , transform37_g2.z));
				float dotResult2_g3 = dot( appendResult38_g2 , float2( 12.9898,78.233 ) );
				float lerpResult8_g3 = lerp( 0.8 , ( ( _RandomWindOffset / 2.0 ) + 0.9 ) , frac( ( sin( dotResult2_g3 ) * 43758.55 ) ));
				float VAR_RandomTime16_g2 = ( ( _TimeParameters.x * 0.05 ) * lerpResult8_g3 );
				float FUNC_Turbulence36_g2 = ( sin( ( ( VAR_RandomTime16_g2 * 40.0 ) - ( VAR_VertexPosition21_g2.z / 15.0 ) ) ) * 0.5 );
				float VAR_WindPulse274_g2 = _WindPulse;
				float FUNC_Angle73_g2 = ( VAR_WindStrength43_g2 * ( 1.0 + sin( ( ( ( ( VAR_RandomTime16_g2 * 2.0 ) + FUNC_Turbulence36_g2 ) - ( VAR_VertexPosition21_g2.z / 50.0 ) ) - ( v.ase_color.r / 20.0 ) ) ) ) * sqrt( v.ase_color.r ) * 0.2 * VAR_WindPulse274_g2 );
				float VAR_SinA80_g2 = sin( FUNC_Angle73_g2 );
				float VAR_CosA78_g2 = cos( FUNC_Angle73_g2 );
				float _WindDirection164_g2 = _WindDirection;
				float2 localDirectionalEquation164_g2 = DirectionalEquation( _WindDirection164_g2 );
				float2 break165_g2 = localDirectionalEquation164_g2;
				float VAR_xLerp83_g2 = break165_g2.x;
				float lerpResult118_g2 = lerp( break109_g2.x , ( ( break109_g2.y * VAR_SinA80_g2 ) + ( break109_g2.x * VAR_CosA78_g2 ) ) , VAR_xLerp83_g2);
				float3 break98_g2 = VAR_VertexPosition21_g2;
				float3 break105_g2 = VAR_VertexPosition21_g2;
				float VAR_zLerp95_g2 = break165_g2.y;
				float lerpResult120_g2 = lerp( break105_g2.z , ( ( break105_g2.y * VAR_SinA80_g2 ) + ( break105_g2.z * VAR_CosA78_g2 ) ) , VAR_zLerp95_g2);
				float3 appendResult122_g2 = (float3(lerpResult118_g2 , ( ( break98_g2.y * VAR_CosA78_g2 ) - ( break98_g2.z * VAR_SinA80_g2 ) ) , lerpResult120_g2));
				float3 FUNC_vertexPos123_g2 = appendResult122_g2;
				float3 break221_g2 = FUNC_vertexPos123_g2;
				half FUNC_SinFunction195_g2 = sin( ( ( VAR_RandomTime16_g2 * 200.0 * ( 0.2 + v.ase_color.g ) ) + ( v.ase_color.g * 10.0 ) + FUNC_Turbulence36_g2 + ( VAR_VertexPosition21_g2.z / 2.0 ) ) );
				float temp_output_202_0_g2 = ( FUNC_SinFunction195_g2 * v.ase_color.b * ( FUNC_Angle73_g2 + ( VAR_WindStrength43_g2 / 200.0 ) ) );
				float lerpResult203_g2 = lerp( 0.0 , temp_output_202_0_g2 , VAR_xLerp83_g2);
				float lerpResult196_g2 = lerp( 0.0 , temp_output_202_0_g2 , VAR_zLerp95_g2);
				float3 appendResult197_g2 = (float3(( break221_g2.x + lerpResult203_g2 ) , break221_g2.y , ( break221_g2.z + lerpResult196_g2 )));
				float3 OUT_Grass_Standalone245_g2 = appendResult197_g2;
				float3 temp_output_5_0_g2 = mul( GetWorldToObjectMatrix(), float4( OUT_Grass_Standalone245_g2 , 0.0 ) ).xyz;
				
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = temp_output_5_0_g2;
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


			float _WindStrength;
			float _RandomWindOffset;
			float _WindPulse;
			float _WindDirection;
			CBUFFER_START( UnityPerMaterial )
			int _CullMode;
			float _GlobalWindInfluence;
			CBUFFER_END


			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord2 : TEXCOORD2;
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
				float4 ase_color : COLOR;
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

				float3 VAR_VertexPosition21_g2 = mul( GetObjectToWorldMatrix(), float4( v.vertex.xyz , 0.0 ) ).xyz;
				float3 break109_g2 = VAR_VertexPosition21_g2;
				float VAR_WindStrength43_g2 = ( _WindStrength * _GlobalWindInfluence );
				float4 transform37_g2 = mul(GetObjectToWorldMatrix(),float4( 0,0,0,1 ));
				float2 appendResult38_g2 = (float2(transform37_g2.x , transform37_g2.z));
				float dotResult2_g3 = dot( appendResult38_g2 , float2( 12.9898,78.233 ) );
				float lerpResult8_g3 = lerp( 0.8 , ( ( _RandomWindOffset / 2.0 ) + 0.9 ) , frac( ( sin( dotResult2_g3 ) * 43758.55 ) ));
				float VAR_RandomTime16_g2 = ( ( _TimeParameters.x * 0.05 ) * lerpResult8_g3 );
				float FUNC_Turbulence36_g2 = ( sin( ( ( VAR_RandomTime16_g2 * 40.0 ) - ( VAR_VertexPosition21_g2.z / 15.0 ) ) ) * 0.5 );
				float VAR_WindPulse274_g2 = _WindPulse;
				float FUNC_Angle73_g2 = ( VAR_WindStrength43_g2 * ( 1.0 + sin( ( ( ( ( VAR_RandomTime16_g2 * 2.0 ) + FUNC_Turbulence36_g2 ) - ( VAR_VertexPosition21_g2.z / 50.0 ) ) - ( v.ase_color.r / 20.0 ) ) ) ) * sqrt( v.ase_color.r ) * 0.2 * VAR_WindPulse274_g2 );
				float VAR_SinA80_g2 = sin( FUNC_Angle73_g2 );
				float VAR_CosA78_g2 = cos( FUNC_Angle73_g2 );
				float _WindDirection164_g2 = _WindDirection;
				float2 localDirectionalEquation164_g2 = DirectionalEquation( _WindDirection164_g2 );
				float2 break165_g2 = localDirectionalEquation164_g2;
				float VAR_xLerp83_g2 = break165_g2.x;
				float lerpResult118_g2 = lerp( break109_g2.x , ( ( break109_g2.y * VAR_SinA80_g2 ) + ( break109_g2.x * VAR_CosA78_g2 ) ) , VAR_xLerp83_g2);
				float3 break98_g2 = VAR_VertexPosition21_g2;
				float3 break105_g2 = VAR_VertexPosition21_g2;
				float VAR_zLerp95_g2 = break165_g2.y;
				float lerpResult120_g2 = lerp( break105_g2.z , ( ( break105_g2.y * VAR_SinA80_g2 ) + ( break105_g2.z * VAR_CosA78_g2 ) ) , VAR_zLerp95_g2);
				float3 appendResult122_g2 = (float3(lerpResult118_g2 , ( ( break98_g2.y * VAR_CosA78_g2 ) - ( break98_g2.z * VAR_SinA80_g2 ) ) , lerpResult120_g2));
				float3 FUNC_vertexPos123_g2 = appendResult122_g2;
				float3 break221_g2 = FUNC_vertexPos123_g2;
				half FUNC_SinFunction195_g2 = sin( ( ( VAR_RandomTime16_g2 * 200.0 * ( 0.2 + v.ase_color.g ) ) + ( v.ase_color.g * 10.0 ) + FUNC_Turbulence36_g2 + ( VAR_VertexPosition21_g2.z / 2.0 ) ) );
				float temp_output_202_0_g2 = ( FUNC_SinFunction195_g2 * v.ase_color.b * ( FUNC_Angle73_g2 + ( VAR_WindStrength43_g2 / 200.0 ) ) );
				float lerpResult203_g2 = lerp( 0.0 , temp_output_202_0_g2 , VAR_xLerp83_g2);
				float lerpResult196_g2 = lerp( 0.0 , temp_output_202_0_g2 , VAR_zLerp95_g2);
				float3 appendResult197_g2 = (float3(( break221_g2.x + lerpResult203_g2 ) , break221_g2.y , ( break221_g2.z + lerpResult196_g2 )));
				float3 OUT_Grass_Standalone245_g2 = appendResult197_g2;
				float3 temp_output_5_0_g2 = mul( GetWorldToObjectMatrix(), float4( OUT_Grass_Standalone245_g2 , 0.0 ) ).xyz;
				
				o.ase_color = v.ase_color;
				
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = temp_output_5_0_g2;
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

				
				
				float3 Albedo = IN.ase_color.rgb;
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


			float _WindStrength;
			float _RandomWindOffset;
			float _WindPulse;
			float _WindDirection;
			CBUFFER_START( UnityPerMaterial )
			int _CullMode;
			float _GlobalWindInfluence;
			CBUFFER_END


			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

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
				float4 ase_color : COLOR;
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

				float3 VAR_VertexPosition21_g2 = mul( GetObjectToWorldMatrix(), float4( v.vertex.xyz , 0.0 ) ).xyz;
				float3 break109_g2 = VAR_VertexPosition21_g2;
				float VAR_WindStrength43_g2 = ( _WindStrength * _GlobalWindInfluence );
				float4 transform37_g2 = mul(GetObjectToWorldMatrix(),float4( 0,0,0,1 ));
				float2 appendResult38_g2 = (float2(transform37_g2.x , transform37_g2.z));
				float dotResult2_g3 = dot( appendResult38_g2 , float2( 12.9898,78.233 ) );
				float lerpResult8_g3 = lerp( 0.8 , ( ( _RandomWindOffset / 2.0 ) + 0.9 ) , frac( ( sin( dotResult2_g3 ) * 43758.55 ) ));
				float VAR_RandomTime16_g2 = ( ( _TimeParameters.x * 0.05 ) * lerpResult8_g3 );
				float FUNC_Turbulence36_g2 = ( sin( ( ( VAR_RandomTime16_g2 * 40.0 ) - ( VAR_VertexPosition21_g2.z / 15.0 ) ) ) * 0.5 );
				float VAR_WindPulse274_g2 = _WindPulse;
				float FUNC_Angle73_g2 = ( VAR_WindStrength43_g2 * ( 1.0 + sin( ( ( ( ( VAR_RandomTime16_g2 * 2.0 ) + FUNC_Turbulence36_g2 ) - ( VAR_VertexPosition21_g2.z / 50.0 ) ) - ( v.ase_color.r / 20.0 ) ) ) ) * sqrt( v.ase_color.r ) * 0.2 * VAR_WindPulse274_g2 );
				float VAR_SinA80_g2 = sin( FUNC_Angle73_g2 );
				float VAR_CosA78_g2 = cos( FUNC_Angle73_g2 );
				float _WindDirection164_g2 = _WindDirection;
				float2 localDirectionalEquation164_g2 = DirectionalEquation( _WindDirection164_g2 );
				float2 break165_g2 = localDirectionalEquation164_g2;
				float VAR_xLerp83_g2 = break165_g2.x;
				float lerpResult118_g2 = lerp( break109_g2.x , ( ( break109_g2.y * VAR_SinA80_g2 ) + ( break109_g2.x * VAR_CosA78_g2 ) ) , VAR_xLerp83_g2);
				float3 break98_g2 = VAR_VertexPosition21_g2;
				float3 break105_g2 = VAR_VertexPosition21_g2;
				float VAR_zLerp95_g2 = break165_g2.y;
				float lerpResult120_g2 = lerp( break105_g2.z , ( ( break105_g2.y * VAR_SinA80_g2 ) + ( break105_g2.z * VAR_CosA78_g2 ) ) , VAR_zLerp95_g2);
				float3 appendResult122_g2 = (float3(lerpResult118_g2 , ( ( break98_g2.y * VAR_CosA78_g2 ) - ( break98_g2.z * VAR_SinA80_g2 ) ) , lerpResult120_g2));
				float3 FUNC_vertexPos123_g2 = appendResult122_g2;
				float3 break221_g2 = FUNC_vertexPos123_g2;
				half FUNC_SinFunction195_g2 = sin( ( ( VAR_RandomTime16_g2 * 200.0 * ( 0.2 + v.ase_color.g ) ) + ( v.ase_color.g * 10.0 ) + FUNC_Turbulence36_g2 + ( VAR_VertexPosition21_g2.z / 2.0 ) ) );
				float temp_output_202_0_g2 = ( FUNC_SinFunction195_g2 * v.ase_color.b * ( FUNC_Angle73_g2 + ( VAR_WindStrength43_g2 / 200.0 ) ) );
				float lerpResult203_g2 = lerp( 0.0 , temp_output_202_0_g2 , VAR_xLerp83_g2);
				float lerpResult196_g2 = lerp( 0.0 , temp_output_202_0_g2 , VAR_zLerp95_g2);
				float3 appendResult197_g2 = (float3(( break221_g2.x + lerpResult203_g2 ) , break221_g2.y , ( break221_g2.z + lerpResult196_g2 )));
				float3 OUT_Grass_Standalone245_g2 = appendResult197_g2;
				float3 temp_output_5_0_g2 = mul( GetWorldToObjectMatrix(), float4( OUT_Grass_Standalone245_g2 , 0.0 ) ).xyz;
				
				o.ase_color = v.ase_color;
				
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = temp_output_5_0_g2;
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

				
				
				float3 Albedo = IN.ase_color.rgb;
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
425;266;1008;676;1968.34;665.6494;2.064041;True;False
Node;AmplifyShaderEditor.CommentaryNode;22;-976.9675,61.41877;Inherit;False;275.1868;247.6812;;1;19;Vertex Position;0,1,0.09019608,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;24;-1409.197,-326.6083;Inherit;False;674.4768;271.9942;;1;1;Albedo;1,0.1254902,0.1254902,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;23;-1397.893,68.09306;Inherit;False;275;165;;1;17;Variables;1,0,0.7254902,1;0;0
Node;AmplifyShaderEditor.IntNode;17;-1372.171,118.5078;Inherit;False;Property;_CullMode;Cull Mode;9;1;[Enum];Create;True;3;Off;0;Front;1;Back;2;0;True;0;2;0;0;1;INT;0
Node;AmplifyShaderEditor.VertexColorNode;1;-1338.694,-258.3241;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;19;-925.9949,163.2096;Inherit;False;Mtree Wind;0;;2;d710ffc7589a70c42a3e6c5220c6279d;7,282,0,280,0,278,0,255,4,269,1,281,0,272,0;0;1;FLOAT3;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;31;-284.9035,-258.0691;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;ExtraPrePass;0;0;ExtraPrePass;5;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;True;0;False;-1;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;0;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;32;-284.9035,-258.0691;Float;False;True;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;2;Hidden/Mtree/SRP/VertexColorShader URP;94348b07e5e8bab40bd6c8a1e3df54cd;True;Forward;0;1;Forward;12;False;False;False;True;0;True;17;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;True;1;1;False;-1;0;False;-1;1;1;False;-1;0;False;-1;False;False;False;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;LightMode=UniversalForward;False;0;Hidden/InternalErrorShader;0;0;Standard;13;Workflow;1;Surface;0;  Blend;0;Two Sided;1;Cast Shadows;1;Receive Shadows;1;GPU Instancing;1;LOD CrossFade;1;Built-in Fog;1;Meta Pass;1;Override Baked GI;0;Extra Pre Pass;0;Vertex Position,InvertActionOnDeselection;0;0;6;False;True;True;True;True;True;False;;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;33;-284.9035,-258.0691;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;False;False;True;1;False;-1;True;3;False;-1;False;True;1;LightMode=ShadowCaster;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;34;-284.9035,-258.0691;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;DepthOnly;0;3;DepthOnly;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;True;False;False;False;False;0;False;-1;False;True;1;False;-1;False;False;True;1;LightMode=DepthOnly;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;35;-284.9035,-258.0691;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;Meta;0;4;Meta;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;True;2;False;-1;False;False;False;False;False;True;1;LightMode=Meta;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;36;-284.9035,-258.0691;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;Universal2D;0;5;Universal2D;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;True;1;1;False;-1;0;False;-1;1;1;False;-1;0;False;-1;False;False;False;True;True;True;True;True;0;False;-1;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;LightMode=Universal2D;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.CommentaryNode;25;-334.9035,-308.0691;Inherit;False;313;505;;0;Output;0,0,0,1;0;0
WireConnection;32;0;1;0
WireConnection;32;8;19;0
ASEEND*/
//CHKSM=379D9DCA9F0B2E0ED4840B1A20F5EA6A8E1D0B6E