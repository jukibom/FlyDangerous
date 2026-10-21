#ifndef SHADERGRAPH_PREVIEW
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
#pragma multi_compile_fragment _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#endif

void SampleMainLightShadow_float(in float3 WorldPos, out float Shadow) // WorldNormal might not be needed if not used elsewhere
{

    #ifdef SHADERGRAPH_PREVIEW
    Shadow = 1.0;
    #else

    float4 shadowCoord;

    // Calculate shadow coordinates
    #if SHADOWS_SCREEN
    float4 positionCS = TransformWorldToHClip(WorldPos);
    shadowCoord = ComputeScreenPos(positionCS);
    #else
    shadowCoord = TransformWorldToShadowCoord(WorldPos);
    #endif

    Light mainLight = GetMainLight(shadowCoord, WorldPos, 1.0);
    Shadow = mainLight.shadowAttenuation; // GetMainLight already calculates this!

    // Alternatively, if you specifically need to call MainLightRealtimeShadow:
    // Shadow = MainLightRealtimeShadow(shadowCoord);
    // In most up-to-date URP versions, mainLight.shadowAttenuation is preferred.
    #endif
}