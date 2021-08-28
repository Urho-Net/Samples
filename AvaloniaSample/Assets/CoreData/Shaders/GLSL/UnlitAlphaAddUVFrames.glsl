#include "Uniforms.glsl"
#include "Samplers.glsl"
#include "Transform.glsl"
#include "ScreenPos.glsl"
#include "Fog.glsl"

//uniform float cMinSumColor;
//uniform float cMaxAlpha;
uniform PRECISION float cMultAddEmission;
uniform PRECISION float cMaskEdges;
uniform PRECISION vec2 cCurRowCol;
uniform PRECISION vec2 cMaxRowCol;

varying vec2 vFrameTexCoord;

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
        vec4 diffColor = cMatDiffColor * texture2D(sDiffMap, vFrameTexCoord);
        #ifdef ALPHAMASK

            // add self emission
            if (cMultAddEmission > 0.0)
            {
                diffColor.rgb *= (1.0 + cMultAddEmission);
            }

            // clean up around the edges
            if (cMaskEdges > 0.0)
            {
                diffColor.rgb *= texture2D(sSpecMap, vTexCoord).a;
            }
        #endif
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
