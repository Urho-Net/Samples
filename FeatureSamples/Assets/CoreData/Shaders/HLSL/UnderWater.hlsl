#include "Uniforms.hlsl"
#include "Samplers.hlsl"
#include "Transform.hlsl"
#include "ScreenPos.hlsl"
#include "PostProcess.hlsl"

uniform float3 cWaterTint;

void VS(float4 iPos : POSITION,
    out float2 oScreenPos : TEXCOORD0,
    out float4 oPos : OUTPOSITION)
{
    float4x3 modelMatrix = iModelMatrix;
    float3 worldPos = GetWorldPos(modelMatrix);
    oPos = GetClipPos(worldPos);
    oScreenPos = GetScreenPosPreDiv(oPos);
}

void PS(float2 iScreenPos : TEXCOORD0,
        out float4 oColor : OUTCOLOR0)
{
    float2 uv = iScreenPos;
    float offset = (cElapsedTimePS) * 2.0 * 3.14159 * 0.75;
    uv.x += sin(uv.y * 3.14159 * 8.0 + offset)/500;

    float4 finalColor =  Sample2D(DiffMap, uv);

    finalColor.rgb *= cWaterTint;

    oColor = finalColor;
}

