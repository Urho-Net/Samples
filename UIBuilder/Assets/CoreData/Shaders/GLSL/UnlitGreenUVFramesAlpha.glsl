#include "Uniforms.glsl"
#include "Samplers.glsl"
#include "Transform.glsl"
#include "ScreenPos.glsl"
#include "Fog.glsl"

uniform PRECISION float cMultAddEmission;
uniform PRECISION float cMaskEdges;
uniform PRECISION vec2 cCurRowCol;
uniform PRECISION vec2 cMaxRowCol;
uniform PRECISION vec4 cColorToReplace;
uniform PRECISION float cThresholdSensitivity;
uniform PRECISION float cSmoothing;
varying PRECISION vec2 vFrameTexCoord;

varying vec2 vTexCoord;
varying vec4 vWorldPos;
#ifdef VERTEXCOLOR
    varying vec4 vColor;
#endif

vec2 GetFrameTexCoord(vec2 texCoord)
{
    float u = texCoord.x/cMaxRowCol.y + cCurRowCol.y/cMaxRowCol.y;
    float v = texCoord.y/cMaxRowCol.x + cCurRowCol.x/cMaxRowCol.x;

    return vec2(u, v);
}

void VS()
{
    mat4 modelMatrix = iModelMatrix;
    vec3 worldPos = GetWorldPos(modelMatrix);
    gl_Position = GetClipPos(worldPos);
    vTexCoord = GetTexCoord(iTexCoord);
    vFrameTexCoord = GetFrameTexCoord(iTexCoord);
    vWorldPos = vec4(worldPos, GetDepth(gl_Position));

    #ifdef VERTEXCOLOR
        vColor = iColor;
    #endif
}

void PS()
{
    // Get material diffuse albedo
    #ifdef DIFFMAP
        vec4 texColor = texture2D(sDiffMap, vFrameTexCoord);

        // copied from
        // https://stackoverflow.com/questions/16909816/can-someone-please-explain-this-fragment-shader-it-is-a-chroma-key-filter-gree
        float maskY = 0.2989 * cColorToReplace.r + 0.5866 * cColorToReplace.g + 0.1145 * cColorToReplace.b;
        float maskCr = 0.7132 * (cColorToReplace.r - maskY);
        float maskCb = 0.5647 * (cColorToReplace.b - maskY);

        float Y = 0.2989 * texColor.r + 0.5866 * texColor.g + 0.1145 * texColor.b;
        float Cr = 0.7132 * (texColor.r - Y);
        float Cb = 0.5647 * (texColor.b - Y);

        float blendValue = smoothstep(cThresholdSensitivity, cThresholdSensitivity + cSmoothing, distance(vec2(Cr, Cb), vec2(maskCr, maskCb)));

        texColor = vec4(texColor.rgb * blendValue, texColor.a * blendValue);

        // masked transparency
        if (cMaskEdges > 0.0)
        {
            texColor.a *= texture2D(sSpecMap, vTexCoord).a;
        }

        // add self emission
        if (cMultAddEmission > 0.0)
        {
            texColor.rgb *= (1.0 + cMultAddEmission);
        }
        vec4 diffColor = cMatDiffColor * texColor;

    #else
        vec4 diffColor = cMatDiffColor;
    #endif

    #ifdef VERTEXCOLOR
        diffColor *= vColor;
    #endif

    // Get fog factor
    #ifdef HEIGHTFOG
        float fogFactor = GetHeightFogFactor(vWorldPos.w, vWorldPos.y);
    #else
        float fogFactor = GetFogFactor(vWorldPos.w);
    #endif

    #if defined(PREPASS)
        // Fill light pre-pass G-Buffer
        gl_FragData[0] = vec4(0.5, 0.5, 0.5, 1.0);
        gl_FragData[1] = vec4(EncodeDepth(vWorldPos.w), 0.0);
    #elif defined(DEFERRED)
        gl_FragData[0] = vec4(GetFog(diffColor.rgb, fogFactor), diffColor.a);
        gl_FragData[1] = vec4(0.0, 0.0, 0.0, 0.0);
        gl_FragData[2] = vec4(0.5, 0.5, 0.5, 1.0);
        gl_FragData[3] = vec4(EncodeDepth(vWorldPos.w), 0.0);
    #else
        gl_FragColor = vec4(GetFog(diffColor.rgb, fogFactor), diffColor.a);
    #endif
}
