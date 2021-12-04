// Parallax mapping
// DirectX9 ParallaxOcclusionMapping sample
#line 10100

#ifdef PARALLAXMAP
// error checking
#ifdef NORMALMAP
#error PARALLAXMAP and NORMALMAP must be exclusive
#endif
#ifdef MOBILE_GRAPHICS
    precision mediump float;
#else
    precision highp float;
#endif
// height scale
#ifndef cHeightScale
uniform float cHeightScale;
#endif

#ifdef COMPILEPS

#ifdef PARALLAX_OFFSET
vec2 ParallaxOffsetLimit(sampler2D depthMap, vec2 texCoords, vec3 viewDir)
{ 
    float height = texture2D(depthMap, texCoords).r;
    vec2 p = viewDir.xy / viewDir.z * (height * cHeightScale);
    return texCoords - p;    
}
#endif

#ifdef PARALLAX_OCCLUSION
vec2 ParallaxOcclusionMapping(sampler2D depthMap, vec2 texCoords, vec3 viewDir, float VdotN)
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
       fCurrHeight = texture2D(depthMap, vTexCurrentOffset).r;

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
#endif

#endif //COMPILEPS
#endif //PARALLAXMAP
