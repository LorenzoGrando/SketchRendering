#ifndef STROKE_SDFS
#define STROKE_SDFS

#include "Assets/Scripts/Includes/Falloff/FalloffFunctions.hlsl"

struct StrokeData
{
    //Variables here have different formats to account for Unity`s own recomendation
    //See Using buffers with GPU buffers: https://docs.unity3d.com/6000.1/Documentation/Manual/SL-PlatformDifferences.html
    float4 coords;
    float4 direction;
    float thickness;
    float thicknessFalloffConstraint;
    float length;
    float lengthThicknessFalloff;
};

float2 GetOriginCoordinate(float2 coords, float dimension)
{
    return float2((coords.x % 1) * (float)dimension, (coords.y % 1) * (float)dimension);
}

float GetInterpolatedFloatValue(float data, float dimension)
{
    return data * (float)dimension/4.0;
}

float FalloffFunction(float t)
{
    #if defined(FALLOFF_LINEAR)
    t = LinearFalloff(t);
    #elif defined(FALLOFF_EASE_INOUT_SINE)
    t = EaseInOutSineFalloff(t);
    #endif
    
    return t;
}

float SampleBaseSDF(StrokeData data, float2 pointID, float dimension) {
    float2 origin = GetOriginCoordinate(data.coords.xy, dimension);
    float thickness = GetInterpolatedFloatValue(data.thickness, dimension);
    float length = GetInterpolatedFloatValue(data.length, dimension);
    
    float2 endPoint = origin + normalize(data.direction).xy * length;
    
    //check closest point in all adjacent toroidal space grids
    float minDist = dimension;
    float interpolation = 0;
    [unroll(3)]
    for(int offsetX = -1; offsetX <= 1; offsetX++) {
        [unroll(3)]
        for(int offsetY = -1; offsetY <= 1; offsetY++) {
            float2 offset = float2(offsetX * dimension, offsetY * dimension);
            float2 offsetOrigin = origin + offset;
            float2 offsetLength = endPoint + offset;
            
            float2 v = offsetLength - offsetOrigin;
            float2 u = pointID - offsetOrigin;
            
            float t = saturate(dot(v, u)/dot(v, v));
            float2 closestPoint = offsetOrigin + t * v;
            float2 dir = pointID - closestPoint;
            float dist = sqrt(dir.x * dir.x + dir.y * dir.y);
            if(dist < minDist) {
                minDist = dist;
                interpolation = t;
            }
        }
    }
    float minThickness = lerp(0, thickness, data.thicknessFalloffConstraint);
    //attenuate falloff if length is too short
    float lengthFalloff = data.lengthThicknessFalloff * step(thickness/2, length + minThickness/2);
    float fallOff = lerp(thickness, minThickness, FalloffFunction(lengthFalloff * interpolation));
    float sample = step(fallOff, minDist);
    
    return sample;
}

#endif