#include "Uniforms.hlsl"
#include "Samplers.hlsl"
#include "Transform.hlsl"
#include "ScreenPos.hlsl"
#include "Lighting.hlsl"
#include "Fog.hlsl"

#ifndef D3D11

// D3D9 uniforms and samplers
#ifdef COMPILEVS
uniform float2 cDetailTiling;
#else
sampler2D sWeightMap0 : register(s0);
sampler2D sDetailMap1 : register(s1);
sampler2D sDetailMap2 : register(s2);
sampler2D sDetailMap3 : register(s3);
sampler2D sDetailMap4 : register(s4);
uniform float cHeightScale;
#endif

#else

// D3D11 constant buffers and samplers
#ifdef COMPILEVS
cbuffer CustomVS : register(b6)
{
    float2 cDetailTiling;
}
#else
Texture2D tWeightMap0 : register(t0);
Texture2D tDetailMap1 : register(t1);
Texture2D tDetailMap2 : register(t2);
Texture2D tDetailMap3 : register(t3);
Texture2D tDetailMap4 : register(t4);
SamplerState sWeightMap0 : register(s0);
SamplerState sDetailMap1 : register(s1);
SamplerState sDetailMap2 : register(s2);
SamplerState sDetailMap3 : register(s3);
SamplerState sDetailMap4 : register(s4);
#endif

#endif

#ifdef COMPILEPS
float2 ParallaxOcclusionMapping(float2 weights, float2 texCoords, float3 viewDir, float VdotN)
{
    const float minLayers = 8.0;
    const float maxLayers = 36.0;

    // the amount to shift the texture coordinates per layer (from vector P)
    float2 vParallaxDirection = normalize(viewDir.xy);
       
    // The length of this vector determines the furthest amount of displacement:
    float fLength         = length(viewDir);
    float fParallaxLength = sqrt(fLength * fLength - viewDir.z * viewDir.z) / abs(viewDir.z);
       
    // Compute the actual reverse parallax displacement vector:
    float2 vParallaxOffsetTS = vParallaxDirection * fParallaxLength;
       
    // Need to scale the amount of displacement to account for different height ranges
    // in height maps. This is controlled by an artist-editable parameter:
    vParallaxOffsetTS *= cHeightScale;

    int nNumSteps = (int)lerp(maxLayers, minLayers, abs(VdotN));
    float fCurrHeight = 0.0;
    float fStepSize   = 1.0 / (float) nNumSteps;
    float fPrevHeight = 1.0;
    float fNextHeight = 0.0;
    int    nStepIndex = 0;

    float2 vTexOffsetPerStep = fStepSize * vParallaxOffsetTS;
    float2 vTexCurrentOffset = texCoords;
    float  fCurrentBound     = 1.0;
    float  fParallaxAmount   = 0.0;

    float2 pt1 = 0;
    float2 pt2 = 0;
     
    float2 texOffset2 = 0;

    [unroll(36)]while ( nStepIndex < nNumSteps ) 
    {
       vTexCurrentOffset -= vTexOffsetPerStep;

       // Sample height map
       fCurrHeight = weights.x * tex2D(sDetailMap1, vTexCurrentOffset).a +
                     weights.y * tex2D(sDetailMap2, vTexCurrentOffset).a;

       fCurrentBound -= fStepSize;

       if ( fCurrHeight > fCurrentBound ) 
       {   
          pt1 = float2( fCurrentBound, fCurrHeight );
          pt2 = float2( fCurrentBound + fStepSize, fPrevHeight );

          texOffset2 = vTexCurrentOffset - vTexOffsetPerStep;

          nStepIndex = nNumSteps + 1;
          fPrevHeight = fCurrHeight;
       }
       else
       {
          nStepIndex++;
          fPrevHeight = fCurrHeight;
       }
    }   

    float fDelta2 = pt2.x - pt2.y;
    float fDelta1 = pt1.x - pt1.y;
    
    float fDenominator = fDelta2 - fDelta1;
    
    // SM 3.0 requires a check for divide by zero, since that operation will generate
    // an 'Inf' number instead of 0, as previous models (conveniently) did:
    if ( fDenominator == 0.0 )
    {
       fParallaxAmount = 0.0;
    }
    else
    {
       fParallaxAmount = (pt1.x * fDelta2 - pt2.x * fDelta1 ) / fDenominator;
    }
    
    float2 vParallaxOffset = vParallaxOffsetTS * (1.0 - fParallaxAmount);

    // The computed texture offset for the displaced point on the pseudo-extruded surface:
    return (texCoords - vParallaxOffset);
}
#endif

void VS(float4 iPos : POSITION,
    float3 iNormal : NORMAL,
    float2 iTexCoord : TEXCOORD0,
    #if (defined(NORMALMAP) || defined(PARALLAXMAP) || defined(TRAILFACECAM) || defined(TRAILBONE)) && !defined(BILLBOARD) && !defined(DIRBILLBOARD)
        float4 iTangent : TANGENT,
    #endif
    #ifdef SKINNED
        float4 iBlendWeights : BLENDWEIGHT,
        int4 iBlendIndices : BLENDINDICES,
    #endif
    #ifdef INSTANCED
        float4x3 iModelInstance : TEXCOORD4,
    #endif
    #if defined(BILLBOARD) || defined(DIRBILLBOARD)
        float2 iSize : TEXCOORD1,
    #endif
    #if defined(TRAILFACECAM) || defined(TRAILBONE)
        float4 iTangent : TANGENT,
    #endif
    #if defined(NORMALMAP) || defined(PARALLAXMAP)
        out float4 oTexCoord : TEXCOORD0,
        out float4 oTangent : TEXCOORD3,
    #else
        out float2 oTexCoord : TEXCOORD0,
    #endif
    out float3 oNormal : TEXCOORD1,
    out float4 oWorldPos : TEXCOORD2,
    out float2 oDetailTexCoord : TEXCOORD6,
    #ifdef PERPIXEL
        #ifdef SHADOW
            out float4 oShadowPos[NUMCASCADES] : TEXCOORD4,
        #endif
        #ifdef SPOTLIGHT
            out float4 oSpotPos : TEXCOORD5,
        #endif
        #ifdef POINTLIGHT
            out float3 oCubeMaskVec : TEXCOORD5,
        #endif
    #else
        out float3 oVertexLight : TEXCOORD4,
        out float4 oScreenPos : TEXCOORD5,
    #endif
    #if defined(D3D11) && defined(CLIPPLANE)
        out float oClip : SV_CLIPDISTANCE0,
    #endif
    out float4 oPos : OUTPOSITION)
{
    float4x3 modelMatrix = iModelMatrix;
    float3 worldPos = GetWorldPos(modelMatrix);
    oPos = GetClipPos(worldPos);
    oNormal = GetWorldNormal(modelMatrix);
    oWorldPos = float4(worldPos, GetDepth(oPos));
    #if defined(NORMALMAP) || defined(PARALLAXMAP)
        float4 tangent = GetWorldTangent(modelMatrix);
        // bitangent calculation changed to bitangent = cross(normal, tangent) based on:
        // https://www.gamasutra.com/blogs/RobertBasler/20131122/205462/Three_Normal_Mapping_Techniques_Explained_For_the_Mathematically_Uninclined.php?print=1
        float3 bitangent = normalize(cross(oNormal, tangent.xyz)) * tangent.w;
        oTexCoord = float4(GetTexCoord(iTexCoord), bitangent.xy);
        oTangent = float4(tangent.xyz, bitangent.z);
    #else
        oTexCoord = GetTexCoord(iTexCoord);
    #endif

    oDetailTexCoord = cDetailTiling * oTexCoord.xy;

    #if defined(D3D11) && defined(CLIPPLANE)
        oClip = dot(oPos, cClipPlane);
    #endif

    #ifdef PERPIXEL
        // Per-pixel forward lighting
        float4 projWorldPos = float4(worldPos.xyz, 1.0);

        #ifdef SHADOW
            // Shadow projection: transform from world space to shadow space
            GetShadowPos(projWorldPos, oNormal, oShadowPos);
        #endif

        #ifdef SPOTLIGHT
            // Spotlight projection: transform from world space to projector texture coordinates
            oSpotPos = mul(projWorldPos, cLightMatrices[0]);
        #endif

        #ifdef POINTLIGHT
            oCubeMaskVec = mul(worldPos - cLightPos.xyz, (float3x3)cLightMatrices[0]);
        #endif
    #else
        // Ambient & per-vertex lighting
        oVertexLight = GetAmbient(GetZonePos(worldPos));

        #ifdef NUMVERTEXLIGHTS
            for (int i = 0; i < NUMVERTEXLIGHTS; ++i)
                oVertexLight += GetVertexLight(i, worldPos, oNormal) * cVertexLights[i * 3].rgb;
        #endif
        
        oScreenPos = GetScreenPos(oPos);
    #endif
}
#line 13000
void PS(
    #if defined(NORMALMAP) || defined(PARALLAXMAP)
        float4 iTexCoord : TEXCOORD0,
        float4 iTangent : TEXCOORD3,
    #else
        float2 iTexCoord : TEXCOORD0,
    #endif
    float3 iNormal : TEXCOORD1,
    float4 iWorldPos : TEXCOORD2,
    float2 iDetailTexCoord : TEXCOORD6,
    #ifdef PERPIXEL
        #ifdef SHADOW
            float4 iShadowPos[NUMCASCADES] : TEXCOORD4,
        #endif
        #ifdef SPOTLIGHT
            float4 iSpotPos : TEXCOORD5,
        #endif
        #ifdef POINTLIGHT
            float3 iCubeMaskVec : TEXCOORD5,
        #endif
    #else
        float3 iVertexLight : TEXCOORD4,
        float4 iScreenPos : TEXCOORD5,
    #endif
    #if defined(D3D11) && defined(CLIPPLANE)
        float iClip : SV_CLIPDISTANCE0,
    #endif
    #ifdef PREPASS
        out float4 oDepth : OUTCOLOR1,
    #endif
    #ifdef DEFERRED
        out float4 oAlbedo : OUTCOLOR1,
        out float4 oNormal : OUTCOLOR2,
        out float4 oDepth : OUTCOLOR3,
    #endif
    out float4 oColor : OUTCOLOR0)
{
    // Get material diffuse albedo
    float3 weights = Sample2D(WeightMap0, iTexCoord.xy).rgb;
    float sumWeights = weights.r + weights.g + weights.b;
    weights /= sumWeights;

    float2 detailTexCoord = iDetailTexCoord;
    float3 bitangent = float3(iTexCoord.zw, iTangent.w);
    float3x3 tbn = float3x3(iTangent.xyz, bitangent, iNormal);
    float3 viewDirWS = cCameraPosPS.xyz - iWorldPos.xyz;
    float VdotN = dot(normalize(viewDirWS), iNormal);
    float3 viewDir = mul(tbn, viewDirWS);
    detailTexCoord = ParallaxOcclusionMapping(float2(weights.r, weights.g), detailTexCoord, viewDir, VdotN);
    tbn = float3x3(iTangent.xyz, -bitangent, iNormal);
    float3 normal = weights.r * (mul(tbn, DecodeNormal(Sample2D(DetailMap3, detailTexCoord)))) +
                    weights.g * (mul(tbn, DecodeNormal(Sample2D(DetailMap4, detailTexCoord))));
           normal = normalize(normal);
    
    float4 diffColor = cMatDiffColor * float4(weights.r * Sample2D(DetailMap1, detailTexCoord).rgb +
                                              weights.g * Sample2D(DetailMap2, detailTexCoord).rgb, 1);

    // Get material specular albedo
    float3 specColor = cMatSpecColor.rgb;

    // Get fog factor
    #ifdef HEIGHTFOG
        float fogFactor = GetHeightFogFactor(iWorldPos.w, iWorldPos.y);
    #else
        float fogFactor = GetFogFactor(iWorldPos.w);
    #endif

    #if defined(PERPIXEL)
        // Per-pixel forward lighting
        float3 lightDir;
        float3 lightColor;
        float3 finalColor;
        
        float diff = GetDiffuse(normal, iWorldPos.xyz, lightDir);

        #ifdef SHADOW
            diff *= GetShadow(iShadowPos, iWorldPos.w);
        #endif
    
        #if defined(SPOTLIGHT)
            lightColor = iSpotPos.w > 0.0 ? Sample2DProj(LightSpotMap, iSpotPos).rgb * cLightColor.rgb : 0.0;
        #elif defined(CUBEMASK)
            lightColor = SampleCube(LightCubeMap, iCubeMaskVec).rgb * cLightColor.rgb;
        #else
            lightColor = cLightColor.rgb;
        #endif
    
        #ifdef SPECULAR
            float spec = GetSpecular(normal, cCameraPosPS - iWorldPos.xyz, lightDir, cMatSpecColor.a);
            finalColor = diff * lightColor * (diffColor.rgb + spec * specColor * cLightColor.a);
        #else
            finalColor = diff * lightColor * diffColor.rgb;
        #endif

        #ifdef AMBIENT
            finalColor += cAmbientColor.rgb * diffColor.rgb;
            finalColor += cMatEmissiveColor;
            oColor = float4(GetFog(finalColor, fogFactor), diffColor.a);
        #else
            oColor = float4(GetLitFog(finalColor, fogFactor), diffColor.a);
        #endif
    #elif defined(PREPASS)
        // Fill light pre-pass G-Buffer
        float specPower = cMatSpecColor.a / 255.0;

        oColor = float4(normal * 0.5 + 0.5, specPower);
        oDepth = iWorldPos.w;
    #elif defined(DEFERRED)
        // Fill deferred G-buffer
        float specIntensity = specColor.g;
        float specPower = cMatSpecColor.a / 255.0;

        float3 finalColor = iVertexLight * diffColor.rgb;

        oColor = float4(GetFog(finalColor, fogFactor), 1.0);
        oAlbedo = fogFactor * float4(diffColor.rgb, specIntensity);
        oNormal = float4(normal * 0.5 + 0.5, specPower);
        oDepth = iWorldPos.w;
    #else
        // Ambient & per-vertex lighting
        float3 finalColor = iVertexLight * diffColor.rgb;

        #ifdef MATERIAL
            // Add light pre-pass accumulation result
            // Lights are accumulated at half intensity. Bring back to full intensity now
            float4 lightInput = 2.0 * Sample2DProj(LightBuffer, iScreenPos);
            float3 lightSpecColor = lightInput.a * (lightInput.rgb / GetIntensity(lightInput.rgb));

            finalColor += lightInput.rgb * diffColor.rgb + lightSpecColor * specColor;
        #endif

        oColor = float4(GetFog(finalColor, fogFactor), diffColor.a);
    #endif
}
