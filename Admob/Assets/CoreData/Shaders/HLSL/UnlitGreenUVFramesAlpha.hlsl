#include "Uniforms.hlsl"
#include "Samplers.hlsl"
#include "Transform.hlsl"
#include "Fog.hlsl"

uniform float cMultAddEmission;
uniform float cMaskEdges;
uniform float2 cCurRowCol;
uniform float2 cMaxRowCol;
uniform float4 cColorToReplace;
uniform float cThresholdSensitivity;
uniform float cSmoothing;


float2 GetFrameTexCoord(float2 texCoord)
{
    float u = texCoord.x/cMaxRowCol.y + cCurRowCol.y/cMaxRowCol.y;
    float v = texCoord.y/cMaxRowCol.x + cCurRowCol.x/cMaxRowCol.x;

    return float2(u, v);
}


void VS(float4 iPos : POSITION,
    #ifndef NOUV
        float2 iTexCoord : TEXCOORD0,
    #endif
    #ifdef VERTEXCOLOR
        float4 iColor : COLOR0,
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
    #if defined(DIRBILLBOARD) || defined(TRAILBONE)
        float3 iNormal : NORMAL,
    #endif
    #if defined(TRAILFACECAM) || defined(TRAILBONE)
        float4 iTangent : TANGENT,
    #endif
    out float2 oTexCoord : TEXCOORD0,
    out float4 oWorldPos : TEXCOORD2,
    out float2 oFrameTexCoord : TEXCOORD3,
    #ifdef VERTEXCOLOR
        out float4 oColor : COLOR0,
    #endif
    #if defined(D3D11) && defined(CLIPPLANE)
        out float oClip : SV_CLIPDISTANCE0,
    #endif
    out float4 oPos : OUTPOSITION)
{
    // Define a 0,0 UV coord if not expected from the vertex data
    #ifdef NOUV
    float2 iTexCoord = float2(0.0, 0.0);
    #endif

    float4x3 modelMatrix = iModelMatrix;
    float3 worldPos = GetWorldPos(modelMatrix);
    oPos = GetClipPos(worldPos);
    //oTexCoord = GetTexCoord(iTexCoord);
    oTexCoord = GetTexCoord(iTexCoord);
    oFrameTexCoord = GetFrameTexCoord(iTexCoord);
    oWorldPos = float4(worldPos, GetDepth(oPos));

    #if defined(D3D11) && defined(CLIPPLANE)
        oClip = dot(oPos, cClipPlane);
    #endif
    
    #ifdef VERTEXCOLOR
        oColor = iColor;
    #endif
}

void PS(float2 iTexCoord : TEXCOORD0,
    float4 iWorldPos: TEXCOORD2,
    float2 iFrameTexCoord: TEXCOORD3,
    #ifdef VERTEXCOLOR
        float4 iColor : COLOR0,
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
    #ifdef DIFFMAP
        float4 texColor = Sample2D(DiffMap, iFrameTexCoord);

        // copied from
        // https://stackoverflow.com/questions/16909816/can-someone-please-explain-this-fragment-shader-it-is-a-chroma-key-filter-gree
        float maskY = 0.2989 * cColorToReplace.r + 0.5866 * cColorToReplace.g + 0.1145 * cColorToReplace.b;
        float maskCr = 0.7132 * (cColorToReplace.r - maskY);
        float maskCb = 0.5647 * (cColorToReplace.b - maskY);

        float Y = 0.2989 * texColor.r + 0.5866 * texColor.g + 0.1145 * texColor.b;
        float Cr = 0.7132 * (texColor.r - Y);
        float Cb = 0.5647 * (texColor.b - Y);

        float blendValue = smoothstep(cThresholdSensitivity, cThresholdSensitivity + cSmoothing, distance(float2(Cr, Cb), float2(maskCr, maskCb)));

        texColor = float4(texColor.rgb * blendValue, texColor.a * blendValue);

        // clean up around the edges
        if (cMaskEdges > 0.0)
        {
            texColor.a *= Sample2D(SpecMap, iTexCoord).a;
        }

        // add self emission
        if (cMultAddEmission > 0.0)
        {
            texColor.rgb *= (1.0 + cMultAddEmission);
        }

        float4 diffColor = cMatDiffColor * texColor;
    #else
        float4 diffColor = cMatDiffColor;
    #endif

    #ifdef VERTEXCOLOR
        diffColor *= iColor;
    #endif

    // Get fog factor
    #ifdef HEIGHTFOG
        float fogFactor = GetHeightFogFactor(iWorldPos.w, iWorldPos.y);
    #else
        float fogFactor = GetFogFactor(iWorldPos.w);
    #endif

    #if defined(PREPASS)
        // Fill light pre-pass G-Buffer
        oColor = float4(0.5, 0.5, 0.5, 1.0);
        oDepth = iWorldPos.w;
    #elif defined(DEFERRED)
        // Fill deferred G-buffer
        oColor = float4(GetFog(diffColor.rgb, fogFactor), diffColor.a);
        oAlbedo = float4(0.0, 0.0, 0.0, 0.0);
        oNormal = float4(0.5, 0.5, 0.5, 1.0);
        oDepth = iWorldPos.w;
    #else
        oColor = float4(GetFog(diffColor.rgb, fogFactor), diffColor.a);
    #endif
}
