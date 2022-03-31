#include "Uniforms.glsl"
#include "Samplers.glsl"
#include "Transform.glsl"
#include "ScreenPos.glsl"
#include "Lighting.glsl"
#include "PostProcess.glsl"


varying vec2 vScreenPos;
varying vec2 vScreenPosInv;


void VS()
{
    mat4 modelMatrix = iModelMatrix;
    vec3 worldPos = GetWorldPos(modelMatrix);
    gl_Position = GetClipPos(worldPos);
    vScreenPos = GetScreenPosPreDiv(gl_Position);
    vScreenPosInv = vec2(vScreenPos.x, 1.0 - vScreenPos.y); // screen is vertically mirrored in opengl
}

void PS()
{
    vec3 rgb = texture2D(sDiffMap, vScreenPos).rgb;
    vec3 mask = texture2D(sNormalMap, vScreenPosInv).rgb;

    vec4 blurredMask = GaussianBlur(3, vec2(0.0, 1.0), vec2(0.004, 0.004), 2.0, sNormalMap, vScreenPosInv) * 0.5;
    blurredMask = blurredMask + GaussianBlur(3, vec2(1.0, 0.0), vec2(0.004, 0.004), 2.0, sNormalMap, vScreenPosInv) * 0.5;
    blurredMask = blurredMask + GaussianBlur(3, vec2(1.0, 1.0), vec2(0.004, 0.004), 2.0, sNormalMap, vScreenPosInv) * 0.5;
    blurredMask = blurredMask + GaussianBlur(3, vec2(1.0, -1.0), vec2(0.004, 0.004), 2.0, sNormalMap, vScreenPosInv) * 0.5;
        
    if (mask.rgb == vec3(1.0, 1.0, 1.0))
        gl_FragColor = vec4(rgb, 1.0);
    else
        gl_FragColor = vec4(rgb + blurredMask.rgb * vec3(0.0, 1.0, 1.0), 1.0);
}
