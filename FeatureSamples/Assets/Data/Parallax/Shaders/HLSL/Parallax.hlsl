// Parallax mapping
// DirectX9 ParallaxOcclusionMapping sample
#line 10100

#ifdef PARALLAXMAP
// error checking
#ifdef NORMALMAP
#error PARALLAXMAP and NORMALMAP must be exclusive
#endif

// height scale
#ifndef cHeightScale
uniform float cHeightScale;
#endif

#ifdef COMPILEPS

#ifdef PARALLAX_OFFSET
float2 ParallaxOffsetLimit(sampler2D depthMap, float2 texCoords, float3 viewDir)
{ 
    float height = tex2D(depthMap, texCoords).r;    
    float2 p = viewDir.xy / viewDir.z * (height * cHeightScale);
    return texCoords - p;    
}
#endif

#ifdef PARALLAX_OCCLUSION
float2 ParallaxOcclusionMapping(sampler2D depthMap, float2 texCoords, float3 viewDir, float VdotN)
{
    const float minLayers = 8.0;
    const float maxLayers = 36.0;

    // the amount to shift the texture coordinates per layer (from vector P)
    float2 vParallaxDirection = normalize(viewDir.xy);
       
    // The length of this vector determines the furthest amount of displacement:
    float fLength         = length(viewDir);
    float fParallaxLength = sqrt(fLength * fLength - viewDir.z * viewDir.z) / abs(viewDir.z);
       
    // Compute the actual reverse parallax displacement vector:
    float2 vParallaxOffsetTS = vParallaxDirection * fParallaxLength;
       
    // Need to scale the amount of displacement to account for different height ranges
    // in height maps. This is controlled by an artist-editable parameter:
    vParallaxOffsetTS *= cHeightScale;

    int nNumSteps = (int)lerp(maxLayers, minLayers, abs(VdotN));
    float fCurrHeight = 0.0;
    float fStepSize   = 1.0 / (float) nNumSteps;
    float fPrevHeight = 1.0;
    float fNextHeight = 0.0;
    int    nStepIndex = 0;

    float2 vTexOffsetPerStep = fStepSize * vParallaxOffsetTS;
    float2 vTexCurrentOffset = texCoords;
    float  fCurrentBound     = 1.0;
    float  fParallaxAmount   = 0.0;

    float2 pt1 = 0;
    float2 pt2 = 0;
     
    float2 texOffset2 = 0;

    [unroll(36)]while ( nStepIndex < nNumSteps ) 
    {
       vTexCurrentOffset -= vTexOffsetPerStep;

       // Sample height map
       fCurrHeight = tex2D(depthMap, vTexCurrentOffset).r;

       fCurrentBound -= fStepSize;

       if ( fCurrHeight > fCurrentBound ) 
       {   
          pt1 = float2( fCurrentBound, fCurrHeight );
          pt2 = float2( fCurrentBound + fStepSize, fPrevHeight );

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
    
    float2 vParallaxOffset = vParallaxOffsetTS * (1.0 - fParallaxAmount);

    // The computed texture offset for the displaced point on the pseudo-extruded surface:
    return (texCoords - vParallaxOffset);
}
#endif

#endif //COMPILEPS
#endif //PARALLAXMAP
