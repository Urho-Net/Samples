#include "Uniforms.glsl"
#include "Samplers.glsl"
#include "Transform.glsl"
#include "ScreenPos.glsl"
#include "Lighting.glsl"
#include "Fog.glsl"

uniform PRECISION float cBRDFAttenuation;

varying vec4 vEyeVec;
varying vec2 vTexCoord;
varying vec3 vNormal;
varying vec4 vWorldPos;
varying vec4 vScreenPos;
#ifdef NORMALMAP
    varying vec4 vTangent;
#endif
#ifdef PERPIXEL
    #ifdef SHADOW
        #ifndef GL_ES
            varying vec4 vShadowPos[NUMCASCADES];
        #else
            varying highp vec4 vShadowPos[NUMCASCADES];
        #endif
    #endif
#endif

//=============================================================================
// reference
// https://alastaira.wordpress.com/2013/11/26/lighting-models-and-brdf-maps/
//
// **NOTE** original code from the reference was ajusted to get the desired effect
//=============================================================================
vec4 GetBRDFMapColor(sampler2D brdfmap, vec3 normal, vec3 eyeVec, vec3 lightDir, float atten)
{
    float NdotL = dot(normal, lightDir);
    NdotL = NdotL * 0.5 + 0.5;

    // use the eqn as given in the reference if uv's are clamped - see MaterialEffects/Textures/fakeBRDF/brdfBlkRed.xml and brdfRainbow.xml
    float NdotV = dot(normal, eyeVec);
    vec3 brdf = texture2D(brdfmap, vec2(NdotL, 1.0 - NdotV)).rgb;
     
    return vec4(brdf * atten, atten);
}

void VS()
{
    mat4 modelMatrix = iModelMatrix;
    vec3 worldPos = GetWorldPos(modelMatrix);
    gl_Position = GetClipPos(worldPos);
    vNormal = GetWorldNormal(modelMatrix);
    vWorldPos = vec4(worldPos, GetDepth(gl_Position));

    #ifdef NORMALMAP
        vec3 tangent = GetWorldTangent(modelMatrix);
        vec3 bitangent = cross(tangent, vNormal) * iTangent.w;
        vTexCoord = vec4(GetTexCoord(iTexCoord), bitangent.xy);
        vTangent = vec4(tangent, bitangent.z);
    #else
        vTexCoord = GetTexCoord(iTexCoord);
    #endif
    #ifdef PERPIXEL
        // Per-pixel forward lighting
        vec4 projWorldPos = vec4(worldPos, 1.0);

        #ifdef SHADOW
            // Shadow projection: transform from world space to shadow space
            for (int i = 0; i < NUMCASCADES; i++)
                vShadowPos[i] = GetShadowPos(i, vNormal, projWorldPos);
        #endif
    #endif
    vScreenPos = GetScreenPos(gl_Position);
    vEyeVec = vec4(cCameraPos - worldPos, GetDepth(gl_Position));
}

void PS()
{
    #ifdef DIFFMAP
        vec4 diffInput = texture2D(sDiffMap, vTexCoord.xy);
        vec4 diffColor = cMatDiffColor * diffInput;
    #else
        vec4 diffColor = cMatDiffColor;
    #endif

    #ifdef NORMALMAP
        mat3 tbn = mat3(vTangent.xyz, vec3(vTexCoord.zw, vTangent.w), vNormal);
        vec3 normal = normalize(tbn * DecodeNormal(texture2D(sNormalMap, vTexCoord.xy)));
    #else
        vec3 normal = normalize(vNormal);
    #endif

        float diff = 1.0;
        vec3 lightDir = vec3(0, -1, 0);
    #ifdef PERPIXEL
        // Per-pixel forward lighting
        // fake BRDF objects do not get diffused by light, but do get shadowed, and will not use the
        // diff var returned from GetDiffuse() function, just need the lightDir
        GetDiffuse(normal, vWorldPos.xyz, lightDir);

        #ifdef SHADOW
            diff *= GetShadow(vShadowPos, vWorldPos.w);
        #endif
    #endif

    vec3 eyeVec = normalize(vEyeVec.xyz);
    vec4 brdfCol = GetBRDFMapColor(sEmissiveMap, normal, eyeVec, lightDir, cBRDFAttenuation);
    vec3 finalColor = diff * diffColor.rgb * brdfCol.rgb;

    #ifdef HEIGHTFOG
        float fogFactor = GetHeightFogFactor(vWorldPos.w, vWorldPos.y);
    #else
        float fogFactor = GetFogFactor(vWorldPos.w);
    #endif

    gl_FragColor = vec4(GetLitFog(finalColor, fogFactor), 1.0);
}
