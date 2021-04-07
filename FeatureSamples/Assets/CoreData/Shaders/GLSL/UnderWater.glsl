#include "Uniforms.glsl"
#include "Samplers.glsl"
#include "Transform.glsl"
#include "ScreenPos.glsl"

#ifdef GL_ES
#ifdef MOBILE_GRAPHICS
    precision mediump float;
#else
    precision highp float;
#endif
#endif

uniform vec3 cWaterTint;
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
    // water distortion effect - from opengl website
    vec2 uv = vScreenPos;
    float offset = (cElapsedTimePS) * 2.0 * 3.14159 * 0.75;
    uv.x += sin(uv.y * 3.14159 * 8.0 + offset)/500.0;

    vec4 finalColor = texture2D(sDiffMap, uv);
    finalColor.rgb *= cWaterTint;

    gl_FragColor = finalColor;
}

