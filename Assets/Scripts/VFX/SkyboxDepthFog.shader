Shader "Hidden/URP/SkyboxDepthFog"
{
    Properties
    {
        _SkyCubemap ("Sky Cubemap", Cube) = "" {}
        _FogStart ("Fog Start", Float) = 100
        _FogEnd ("Fog End", Float) = 1000
        _FogIntensity ("Fog Intensity", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "Skybox Depth Fog"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
                {
                    uint vertexID : SV_VertexID;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct Varyings
                {
                    float4 positionCS : SV_POSITION;
                    float2 texcoord : TEXCOORD0;
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                Varyings Vert(Attributes input)
                {
                    Varyings output;

                    UNITY_SETUP_INSTANCE_ID(input);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                    output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                    output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);

                    return output;
                }

            TEXTURE2D_X(_SkyboxDepthFogSource);
            SAMPLER(sampler_SkyboxDepthFogSource);

            TEXTURECUBE(_SkyCubemap);
            SAMPLER(sampler_SkyCubemap);

            float _FogStart;
            float _FogEnd;
            float _FogIntensity;
            float _SkyRotation;

            float4x4 GetInverseViewProjectionMatrix()
            {
                #if defined(USING_STEREO_MATRICES)
                    return unity_StereoMatrixInvVP[unity_StereoEyeIndex];
                #else
                    return UNITY_MATRIX_I_VP;
                #endif
            }

            float3 GetCameraWorldPosition()
            {
                #if defined(USING_STEREO_MATRICES)
                    return unity_StereoWorldSpaceCameraPos[unity_StereoEyeIndex];
                #else
                    return _WorldSpaceCameraPos;
                #endif
            }
            
            float3 GetCameraForward()
                {
                    #if defined(USING_STEREO_MATRICES)
                        return -unity_StereoMatrixV[unity_StereoEyeIndex]._m20_m21_m22;
                    #else
                        return -UNITY_MATRIX_V._m20_m21_m22;
                    #endif
                }

                float3 GetSkyDirection(float2 uv)
                {
                    float4x4 invViewProjection = GetInverseViewProjectionMatrix();

                    #if UNITY_REVERSED_Z
                        float farDepth = 0.0;
                    #else
                        float farDepth = 1.0;
                    #endif

                    float3 farWorldPos = ComputeWorldSpacePosition(
                        uv,
                        farDepth,
                        invViewProjection
                    );

                    return normalize(farWorldPos - GetCameraWorldPosition());
                }
            
            float3 RotateAroundYInDegrees(float3 direction, float degrees)
            {
                float radians = degrees * PI / -180.0;
                float s = sin(radians);
                float c = cos(radians);

                float3 result;
                result.x = direction.x * c - direction.z * s;
                result.y = direction.y;
                result.z = direction.x * s + direction.z * c;
                return result;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;

                half4 sceneColor = SAMPLE_TEXTURE2D_X(
                    _SkyboxDepthFogSource,
                    sampler_LinearClamp,
                    uv
                );

               float rawDepth = SampleSceneDepth(uv);

                // do not fog pixels that are already the skybox/background.
                #if UNITY_REVERSED_Z
                    if (rawDepth <= 0.0)
                        return sceneColor;
                #else
                    if (rawDepth >= 1.0)
                        return sceneColor;
                #endif

                float linearEyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
            
                float3 skyDirection = GetSkyDirection(uv);
                float3 cameraForward = normalize(GetCameraForward());

                float forwardAmount = max(dot(skyDirection, cameraForward), 0.0001);
                float radialDistance = linearEyeDepth / forwardAmount;

                float fogFactor = smoothstep(_FogStart, _FogEnd, radialDistance);
                fogFactor *= _FogIntensity;

                skyDirection = RotateAroundYInDegrees(skyDirection, _SkyRotation);
            
                half3 skyColor = SAMPLE_TEXTURECUBE(
                    _SkyCubemap,
                    sampler_SkyCubemap,
                    skyDirection
                ).rgb;

                half3 finalColor = lerp(sceneColor.rgb, skyColor, fogFactor);

                return half4(finalColor, sceneColor.a);
            }

            ENDHLSL
        }
    }
}