#include "Uniforms.hlsl"
#include "Samplers.hlsl"
#include "Transform.hlsl"
#include "ScreenPos.hlsl"
#include "Lighting.hlsl"
#include "Fog.hlsl"

uniform float cBRDFAttenuation;

//=============================================================================
// reference
// https://alastaira.wordpress.com/2013/11/26/lighting-models-and-brdf-maps/
//
// **NOTE** original code from the reference was ajusted to get the desired effect
//=============================================================================
float4 GetBRDFMapColor(sampler2D brdfmap, float3 normal, float3 eyeVec, float3 lightDir, float atten)
{
    float NdotL = dot(normal, lightDir);
    NdotL = NdotL * 0.5 + 0.5;

    // use the eqn as given in the reference if uv's are clamped - see MaterialEffects/Textures/fakeBRDF/brdfBlkRed.xml and brdfRainbow.xml
    float NdotV = dot(normal, eyeVec);
    float3 brdf = tex2D(brdfmap, float2(NdotL, 1.0 - NdotV)).rgb;
     
    return float4(brdf * atten, atten);
}

void VS(float4 iPos : POSITION,
        float3 iNormal : NORMAL,
        float2 iTexCoord : TEXCOORD0,
    #ifdef NORMALMAP
        float4 iTangent : TANGENT,
    #endif
    #ifndef NORMALMAP
        out float2 oTexCoord : TEXCOORD0,
    #else
        out float4 oTexCoord : TEXCOORD0,
        out float4 oTangent : TEXCOORD3,
    #endif
    out float3 oNormal : TEXCOORD1,
    out float4 oWorldPos : TEXCOORD2,
    #ifdef PERPIXEL
        #ifdef SHADOW
            out float4 oShadowPos[NUMCASCADES] : TEXCOORD4,
        #endif
    #endif
    out float4 oPos : OUTPOSITION)
{
    float4x3 modelMatrix = iModelMatrix;
    float3 worldPos = GetWorldPos(modelMatrix);
    oPos = GetClipPos(worldPos);
    oNormal = GetWorldNormal(modelMatrix);
    oWorldPos = float4(worldPos, GetDepth(oPos));

    #ifdef NORMALMAP
        float4 tangent = GetWorldTangent(modelMatrix);
        float3 bitangent = cross(tangent.xyz, oNormal) * tangent.w;
        oTexCoord = float4(GetTexCoord(iTexCoord), bitangent.xy);
        oTangent = float4(tangent.xyz, bitangent.z);
    #else
        oTexCoord = GetTexCoord(iTexCoord);
    #endif

    #ifdef PERPIXEL
        // Per-pixel forward lighting
        float4 projWorldPos = float4(worldPos.xyz, 1.0);

        #ifdef SHADOW
            // Shadow projection: transform from world space to shadow space
            GetShadowPos(projWorldPos, oNormal, oShadowPos);
        #endif
    #endif

}

void PS(
    #ifndef NORMALMAP
        float2 iTexCoord : TEXCOORD0,
    #else
        float4 iTexCoord : TEXCOORD0,
        float4 iTangent : TEXCOORD3,
    #endif

    float3 iNormal : TEXCOORD1,
    float4 iWorldPos : TEXCOORD2,

    #ifdef PERPIXEL
        #ifdef SHADOW
            float4 iShadowPos[NUMCASCADES] : TEXCOORD4,
        #endif
    #endif
    out float4 oColor : OUTCOLOR0)
{
    // Get material diffuse albedo
    #ifdef DIFFMAP
        float4 diffInput = Sample2D(DiffMap, iTexCoord.xy);
        float4 diffColor = cMatDiffColor * diffInput;
    #else
        float4 diffColor = cMatDiffColor;
    #endif

    // Get normal
    #ifdef NORMALMAP
        float3x3 tbn = float3x3(iTangent.xyz, float3(iTexCoord.zw, iTangent.w), iNormal);
        float3 normal = normalize(mul(DecodeNormal(Sample2D(NormalMap, iTexCoord.xy)), tbn));
    #else
        float3 normal = normalize(iNormal);
    #endif

        float diff = 1.0;
        float3 lightDir = float3(0, -1, 0);
    #ifdef PERPIXEL
        // Per-pixel forward lighting
        // fake BRDF objects do not get diffused by light, but do get shadowed, and will not use the
        // diff var returned from GetDiffuse() function, just need the lightDir
        GetDiffuse(normal, iWorldPos.xyz, lightDir);

        #ifdef SHADOW
            diff *= GetShadow(iShadowPos, iWorldPos.w);
        #endif
    #endif

    float3 eyeVec = normalize(cCameraPosPS - iWorldPos.xyz);
    float4 brdfCol = GetBRDFMapColor(sEmissiveMap, normal, eyeVec, lightDir, cBRDFAttenuation);
    float3 finalColor = diff * diffColor.rgb * brdfCol.rgb;

    // Get fog factor
    #ifdef HEIGHTFOG
        float fogFactor = GetHeightFogFactor(iWorldPos.w, iWorldPos.y);
    #else
        float fogFactor = GetFogFactor(iWorldPos.w);
    #endif

    oColor = float4(GetFog(finalColor, fogFactor), 1.0);

}

