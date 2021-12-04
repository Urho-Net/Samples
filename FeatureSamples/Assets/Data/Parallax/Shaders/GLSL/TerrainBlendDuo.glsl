#include "Uniforms.glsl"
#include "Samplers.glsl"
#include "Transform.glsl"
#include "ScreenPos.glsl"
#include "Lighting.glsl"
#include "Fog.glsl"

varying vec2 vTexCoord;

#ifndef GL_ES
varying vec2 vDetailTexCoord;
#else
varying mediump vec2 vDetailTexCoord;
#endif

varying vec3 vNormal;
varying vec3 vTangent;
varying vec3 vBitangent;
varying vec4 vWorldPos;
#ifdef PERPIXEL
    #ifdef SHADOW
        #ifndef GL_ES
            varying vec4 vShadowPos[NUMCASCADES];
        #else
            varying highp vec4 vShadowPos[NUMCASCADES];
        #endif
    #endif
    #ifdef SPOTLIGHT
        varying vec4 vSpotPos;
    #endif
    #ifdef POINTLIGHT
        varying vec3 vCubeMaskVec;
    #endif
#else
    varying vec3 vVertexLight;
    varying vec4 vScreenPos;
    #ifdef ENVCUBEMAP
        varying vec3 vReflectionVec;
    #endif
    #if defined(LIGHTMAP) || defined(AO)
        varying vec2 vTexCoord2;
    #endif
#endif

uniform sampler2D sWeightMap0;
uniform sampler2D sDetailMap1;
uniform sampler2D sDetailMap2;
uniform sampler2D sDetailMap3;
uniform sampler2D sDetailMap4;
uniform float cHeightScale;

#ifndef GL_ES
uniform vec2 cDetailTiling;
#else
uniform mediump vec2 cDetailTiling;
#endif


vec2 ParallaxOcclusionMapping(vec2 weights, vec2 texCoords, vec3 viewDir, float VdotN)
{
    float minLayers = 8.0;
    float maxLayers = 36.0;

    // the amount to shift the texture coordinates per layer (from vector P)
    vec2 vParallaxDirection = normalize(viewDir.xy);
       
    // The length of this vector determines the furthest amount of displacement:
    float fLength         = length(viewDir);
    float fParallaxLength = sqrt(fLength * fLength - viewDir.z * viewDir.z) / abs(viewDir.z); 
       
    // Compute the actual reverse parallax displacement vector:
    vec2 vParallaxOffsetTS = vParallaxDirection * fParallaxLength;
       
    // Need to scale the amount of displacement to account for different height ranges
    // in height maps. This is controlled by an artist-editable parameter:
    vParallaxOffsetTS *= cHeightScale;

    int nNumSteps = int(mix(maxLayers, minLayers, abs(VdotN)));
    float fCurrHeight = 0.0;
    float fStepSize   = 1.0 / float(nNumSteps);
    float fPrevHeight = 1.0;
    float fNextHeight = 0.0;

    int   nStepIndex = 0;
    bool  bCondition = true;

    vec2 vTexOffsetPerStep = fStepSize * vParallaxOffsetTS;
    vec2 vTexCurrentOffset = texCoords;
    float fCurrentBound    = 1.0;
    float fParallaxAmount  = 0.0;

    vec2 pt1 = vec2(0);
    vec2 pt2 = vec2(0);
     
    vec2 texOffset2 = vec2(0);

    while ( nStepIndex < nNumSteps ) 
    {
       vTexCurrentOffset -= vTexOffsetPerStep;

       // Sample height map
       fCurrHeight = weights.x * texture2D(sDetailMap1, vTexCurrentOffset).a +
                     weights.y * texture2D(sDetailMap2, vTexCurrentOffset).a;

       fCurrentBound -= fStepSize;

       if ( fCurrHeight > fCurrentBound ) 
       {   
          pt1 = vec2( fCurrentBound, fCurrHeight );
          pt2 = vec2( fCurrentBound + fStepSize, fPrevHeight );

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
    
    vec2 vParallaxOffset = vParallaxOffsetTS * (1.0 - fParallaxAmount);

    // The computed texture offset for the displaced point on the pseudo-extruded surface:
    return (texCoords - vParallaxOffset);
}

void VS()
{
    mat4 modelMatrix = iModelMatrix;
    vec3 worldPos = GetWorldPos(modelMatrix);
    gl_Position = GetClipPos(worldPos);
    vNormal = GetWorldNormal(modelMatrix);
    vWorldPos = vec4(worldPos, GetDepth(gl_Position));
    vTexCoord = GetTexCoord(iTexCoord);
    vDetailTexCoord = cDetailTiling * vTexCoord;

    #ifdef PERPIXEL
        // Per-pixel forward lighting
        vec4 projWorldPos = vec4(worldPos, 1.0);

        #ifdef SHADOW
            // Shadow projection: transform from world space to shadow space
            for (int i = 0; i < NUMCASCADES; i++)
                vShadowPos[i] = GetShadowPos(i, vNormal, projWorldPos);
        #endif

        #ifdef SPOTLIGHT
            // Spotlight projection: transform from world space to projector texture coordinates
            vSpotPos = projWorldPos * cLightMatrices[0];
        #endif
    
        #ifdef POINTLIGHT
            vCubeMaskVec = (worldPos - cLightPos.xyz) * mat3(cLightMatrices[0][0].xyz, cLightMatrices[0][1].xyz, cLightMatrices[0][2].xyz);
        #endif
    #else
        // Ambient & per-vertex lighting
        #if defined(LIGHTMAP) || defined(AO)
            // If using lightmap, disregard zone ambient light
            // If using AO, calculate ambient in the PS
            vVertexLight = vec3(0.0, 0.0, 0.0);
            vTexCoord2 = iTexCoord1;
        #else
            vVertexLight = GetAmbient(GetZonePos(worldPos));
        #endif
        
        #ifdef NUMVERTEXLIGHTS
            for (int i = 0; i < NUMVERTEXLIGHTS; ++i)
                vVertexLight += GetVertexLight(i, worldPos, vNormal) * cVertexLights[i * 3].rgb;
        #endif
        
        vScreenPos = GetScreenPos(gl_Position);

        #ifdef ENVCUBEMAP
            vReflectionVec = worldPos - cCameraPos;
        #endif
    #endif

    vec4 tangent = GetWorldTangent(modelMatrix);
    vBitangent = normalize(cross(vNormal, tangent.xyz)) * tangent.w;
    vTangent = tangent.xyz;

}

void PS()
{
    // Get material diffuse albedo
    vec3 weights = texture2D(sWeightMap0, vTexCoord).rgb;
    float sumWeights = weights.r + weights.g;
    weights /= sumWeights;

    // parallax
    vec2 detailTexCoord = vDetailTexCoord;
    mat3 tbn = transpose(mat3(vTangent, vBitangent, vNormal));
    vec3 viewDirWS = cCameraPosPS.xyz - vWorldPos.xyz;
    float VdotN = dot(normalize(viewDirWS), vNormal);
    vec3 viewDir = tbn * viewDirWS;
    detailTexCoord = ParallaxOcclusionMapping(vec2(weights.r, weights.g), detailTexCoord, viewDir, VdotN);
    tbn = mat3(vTangent, -vBitangent, vNormal);
    vec3 normal = weights.r * (tbn * DecodeNormal(texture2D(sDetailMap3, detailTexCoord))) +
                  weights.g * (tbn * DecodeNormal(texture2D(sDetailMap4, detailTexCoord)));
         normal = normalize(normal);
    
    vec4 diffColor = cMatDiffColor * vec4(weights.r * texture2D(sDetailMap1, detailTexCoord).rgb +
                                          weights.g * texture2D(sDetailMap2, detailTexCoord).rgb, 1);

    // Get material specular albedo
    vec3 specColor = cMatSpecColor.rgb;

    // Get fog factor
    #ifdef HEIGHTFOG
        float fogFactor = GetHeightFogFactor(vWorldPos.w, vWorldPos.y);
    #else
        float fogFactor = GetFogFactor(vWorldPos.w);
    #endif

    #if defined(PERPIXEL)
        // Per-pixel forward lighting
        vec3 lightColor;
        vec3 lightDir;
        vec3 finalColor;
        
        float diff = GetDiffuse(normal, vWorldPos.xyz, lightDir);

        #ifdef SHADOW
            diff *= GetShadow(vShadowPos, vWorldPos.w);
        #endif
    
        #if defined(SPOTLIGHT)
            lightColor = vSpotPos.w > 0.0 ? texture2DProj(sLightSpotMap, vSpotPos).rgb * cLightColor.rgb : vec3(0.0, 0.0, 0.0);
        #elif defined(CUBEMASK)
            lightColor = textureCube(sLightCubeMap, vCubeMaskVec).rgb * cLightColor.rgb;
        #else
            lightColor = cLightColor.rgb;
        #endif
    
        #ifdef SPECULAR
            float spec = GetSpecular(normal, cCameraPosPS - vWorldPos.xyz, lightDir, cMatSpecColor.a);
            finalColor = diff * lightColor * (diffColor.rgb + spec * specColor * cLightColor.a);
        #else
            finalColor = diff * lightColor * diffColor.rgb;
        #endif

        #ifdef AMBIENT
            finalColor += cAmbientColor.rgb * diffColor.rgb;
            finalColor += cMatEmissiveColor;
            gl_FragColor = vec4(GetFog(finalColor, fogFactor), diffColor.a);
        #else
            gl_FragColor = vec4(GetLitFog(finalColor, fogFactor), diffColor.a);
        #endif
    #elif defined(PREPASS)
        // Fill light pre-pass G-Buffer
        float specPower = cMatSpecColor.a / 255.0;

        gl_FragData[0] = vec4(normal * 0.5 + 0.5, specPower);
        gl_FragData[1] = vec4(EncodeDepth(vWorldPos.w), 0.0);
    #elif defined(DEFERRED)
        // Fill deferred G-buffer
        float specIntensity = specColor.g;
        float specPower = cMatSpecColor.a / 255.0;

        gl_FragData[0] = vec4(GetFog(vVertexLight * diffColor.rgb, fogFactor), 1.0);
        gl_FragData[1] = fogFactor * vec4(diffColor.rgb, specIntensity);
        gl_FragData[2] = vec4(normal * 0.5 + 0.5, specPower);
        gl_FragData[3] = vec4(EncodeDepth(vWorldPos.w), 0.0);
    #else
        // Ambient & per-vertex lighting
        vec3 finalColor = vVertexLight * diffColor.rgb;

        #ifdef MATERIAL
            // Add light pre-pass accumulation result
            // Lights are accumulated at half intensity. Bring back to full intensity now
            vec4 lightInput = 2.0 * texture2DProj(sLightBuffer, vScreenPos);
            vec3 lightSpecColor = lightInput.a * lightInput.rgb / max(GetIntensity(lightInput.rgb), 0.001);

            finalColor += lightInput.rgb * diffColor.rgb + lightSpecColor * specColor;
        #endif

        gl_FragColor = vec4(GetFog(finalColor, fogFactor), diffColor.a);
    #endif
}
