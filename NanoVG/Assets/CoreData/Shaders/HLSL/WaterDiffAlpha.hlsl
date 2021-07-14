#include "Uniforms.hlsl"
#include "Samplers.hlsl"
#include "Transform.hlsl"
#include "ScreenPos.hlsl"
#include "Fog.hlsl"

#ifndef D3D11

// D3D9 uniforms
uniform float4 cNoiseSpeed;
uniform float cNoiseTiling;
uniform float cNoiseStrength;
uniform float cFresnelPower;
uniform float3 cWaterTint;
uniform float cDiffTiling;
uniform float cBumpWaveOpacity;
#else

// D3D11 constant buffers
#ifdef COMPILEVS
cbuffer CustomVS : register(b6)
{
    float4 cNoiseSpeed;
    float cNoiseTiling;
}
#else
cbuffer CustomPS : register(b6)
{
    float cNoiseStrength;
    float cFresnelPower;
    float3 cWaterTint;
    float cBumpWaveOpacity;
}
#endif

#endif

void VS(float4 iPos : POSITION,
    float3 iNormal: NORMAL,
    float2 iTexCoord : TEXCOORD0,
    #ifdef INSTANCED
        float4x3 iModelInstance : TEXCOORD4,
    #endif
    out float4 oScreenPos : TEXCOORD0,
    out float4 oReflectUV : TEXCOORD1,
    out float4 oWaterUV : TEXCOORD2,
    out float3 oNormal : TEXCOORD3,
    out float4 oEyeVec : TEXCOORD4,
    #if defined(D3D11) && defined(CLIPPLANE)
        out float oClip : SV_CLIPDISTANCE0,
    #endif
    out float4 oPos : OUTPOSITION)
{
    float4x3 modelMatrix = iModelMatrix;
    float3 worldPos = GetWorldPos(modelMatrix);
    oPos = GetClipPos(worldPos);

    oScreenPos = GetScreenPos(oPos);
    // GetQuadTexCoord() returns a float2 that is OK for quad rendering; multiply it with output W
    // coordinate to make it work with arbitrary meshes such as the water plane (perform divide in pixel shader)
    oReflectUV.xy = GetQuadTexCoord(oPos) * oPos.w;

    // water now has its own var and is vec4
    oWaterUV.xy = iTexCoord * cNoiseTiling + cElapsedTime * cNoiseSpeed.xy;
    oWaterUV.zw = iTexCoord * cNoiseTiling + cElapsedTime * cNoiseSpeed.zw;
    oNormal = GetWorldNormal(modelMatrix);
    oEyeVec = float4(cCameraPos - worldPos, GetDepth(oPos));

    // TexCoord is now stored in Reflect
    oReflectUV.zw = GetTexCoord(iTexCoord) * cDiffTiling + cElapsedTime * cNoiseSpeed.xy;

    #if defined(D3D11) && defined(CLIPPLANE)
        oClip = dot(oPos, cClipPlane);
    #endif
}

void PS(
    float4 iScreenPos : TEXCOORD0,
    float4 iReflectUV : TEXCOORD1,
    float4 iWaterUV : TEXCOORD2,
    float3 iNormal : TEXCOORD3,
    float4 iEyeVec : TEXCOORD4,
    #if defined(D3D11) && defined(CLIPPLANE)
        float iClip : SV_CLIPDISTANCE0,
    #endif
    out float4 oColor : OUTCOLOR0)
{
    float2 refractUV = iScreenPos.xy / iScreenPos.w;
    float2 reflectUV = iReflectUV.xy / iScreenPos.w;

    // assign texcoord for clarity
    float2 iTexCoord = iReflectUV.zw;

    float4 nbump = Sample2D(NormalMap, iWaterUV.xy);
    float4 nbump2 = Sample2D(NormalMap, iWaterUV.zw);
    nbump = (nbump + nbump2) * 0.5;
    float2 noise = (nbump.rg - 0.5) * cNoiseStrength;
    refractUV += noise;
    // Do not shift reflect UV coordinate upward, because it will reveal the clipping of geometry below water
    if (noise.y < 0.0)
        noise.y = 0.0;
    reflectUV += noise;

    float fresnel = pow(1.0 - clamp(dot(normalize(iEyeVec.xyz), iNormal), 0.0, 1.0), cFresnelPower);
    float fresBump = dot(iNormal, nbump.xyz);
    float3 bumpWave = float3(fresBump, fresBump, fresBump);
    float4 diffColor = cMatDiffColor * Sample2D(DiffMap, iTexCoord);
    float3 surfaceCol = lerp(diffColor.rgb, bumpWave, cBumpWaveOpacity) * cWaterTint;

    float3 refractColor = Sample2D(EnvMap, refractUV).rgb * cWaterTint;
    float3 reflectColor = Sample2D(SpecMap, reflectUV).rgb;
    float3 finalColor = lerp(surfaceCol, reflectColor, fresnel);
    finalColor = lerp(finalColor, refractColor, 1.0 - cMatDiffColor.a);

    oColor = float4(GetFog(finalColor, GetFogFactor(iEyeVec.w)), cMatDiffColor.a);
}