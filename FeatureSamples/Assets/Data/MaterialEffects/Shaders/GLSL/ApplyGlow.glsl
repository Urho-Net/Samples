#include "Uniforms.glsl"
#include "Samplers.glsl"
#include "Transform.glsl"
#include "ScreenPos.glsl"

varying vec2 vScreenPos;

void VS()
{
    mat4 modelMatrix = iModelMatrix;
    vec3 worldPos = GetWorldPos(modelMatrix);
    gl_Position = GetClipPos(worldPos);
    vScreenPos = GetScreenPosPreDiv(gl_Position);
}

void PS()
{
    vec4 finalColor =  texture2D(sDiffMap, vScreenPos);
    vec4 glowCol = texture2D(sEnvMap, vScreenPos);

    finalColor += glowCol;

    gl_FragColor = finalColor;
}

