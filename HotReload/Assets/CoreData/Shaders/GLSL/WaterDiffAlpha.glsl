#include "Uniforms.glsl"
#include "Samplers.glsl"
#include "Transform.glsl"
#include "ScreenPos.glsl"
#include "Fog.glsl"

#ifndef GL_ES
varying vec4 vScreenPos;
varying vec2 vReflectUV;
varying vec4 vWaterUV;
varying vec4 vEyeVec;
varying vec2 vTexCoord;
varying vec3 vWorldPos;
#else
varying highp vec4 vScreenPos;
varying highp vec2 vReflectUV;
varying highp vec4 vWaterUV;
varying highp vec4 vEyeVec;
varying highp vec2 vTexCoord;
varying highp vec3 vWorldPos;
#endif
varying vec3 vNormal;

#ifdef COMPILEVS
uniform vec4 cNoiseSpeed;
uniform float cNoiseTiling;
uniform float cDiffTiling;
#endif
#ifdef COMPILEPS
uniform float cNoiseStrength;
uniform float cFresnelPower;
uniform vec3 cWaterTint;
uniform float cBumpWaveOpacity;
#endif

void VS()
{
    mat4 modelMatrix = iModelMatrix;
    vec3 worldPos = GetWorldPos(modelMatrix);
    gl_Position = GetClipPos(worldPos);
    vScreenPos = GetScreenPos(gl_Position);
    // GetQuadTexCoord() returns a vec2 that is OK for quad rendering; multiply it with output W
    // coordinate to make it work with arbitrary meshes such as the water plane (perform divide in pixel shader)
    // Also because the quadTexCoord is based on the clip position, and Y is flipped when rendering to a texture
    // on OpenGL, must flip again to cancel it out
    vReflectUV = GetQuadTexCoord(gl_Position);
    vReflectUV.y = 1.0 - vReflectUV.y;
    vReflectUV *= gl_Position.w;
    vWaterUV.xy = iTexCoord * cNoiseTiling + cElapsedTime * cNoiseSpeed.xy;
    vWaterUV.zw = iTexCoord * cNoiseTiling + cElapsedTime * cNoiseSpeed.zw;
    vNormal = GetWorldNormal(modelMatrix);
    vEyeVec = vec4(cCameraPos - worldPos, GetDepth(gl_Position));
    vTexCoord = GetTexCoord(iTexCoord) * cDiffTiling + cElapsedTime * cNoiseSpeed.xy;
}

void PS()
{
    vec2 refractUV = vScreenPos.xy / vScreenPos.w;
    vec2 reflectUV = vReflectUV.xy / vScreenPos.w;

    vec4 nbump = texture2D(sNormalMap, vWaterUV.xy);
    vec4 nbump2 = texture2D(sNormalMap, vWaterUV.zw);
    nbump = (nbump  + nbump2) * 0.5;
    vec2 noise = (nbump.rg - 0.5) * cNoiseStrength;
    refractUV += noise;
    // Do not shift reflect UV coordinate upward, because it will reveal the clipping of geometry below water
    if (noise.y < 0.0)
        noise.y = 0.0;
    reflectUV += noise;

    float fresnel = pow(1.0 - clamp(dot(normalize(vEyeVec.xyz), vNormal), 0.0, 1.0), cFresnelPower);
    float fresBump = dot(vNormal, nbump.xyz);
    vec3 bumpWave = vec3(fresBump, fresBump, fresBump);
    vec4 diffColor = cMatDiffColor * texture2D(sDiffMap, vTexCoord);
    vec3 surfaceCol = mix(diffColor.rgb, bumpWave, cBumpWaveOpacity) * cWaterTint;

    vec3 refractColor = texture2D(sEnvMap, refractUV).rgb * cWaterTint;
    vec3 reflectColor = texture2D(sSpecMap, reflectUV).rgb;
    vec3 finalColor = mix(surfaceCol, reflectColor, fresnel);
    finalColor = mix(finalColor, refractColor, 1.0 - cMatDiffColor.a);

    gl_FragColor = vec4(GetFog(finalColor, GetFogFactor(vEyeVec.w)), cMatDiffColor.a);
}
