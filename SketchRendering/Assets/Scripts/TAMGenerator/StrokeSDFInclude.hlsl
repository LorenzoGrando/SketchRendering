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
    float pressure;
    float pressureFalloff;
};

struct VariationData
{
    float directionVariation;
    float thicknessVariation;
    float lengthVariation;
    float pressureVariation;
};

float2 GetOriginCoordinate(float2 coords, float dimension)
{
    return float2((coords.x % 1) * (float)dimension, (coords.y % 1) * (float)dimension);
}

float2 GetOriginCoordinate(float2 coords, float2 dimensions)
{
    return float2((coords.x % 1) * (float)dimensions.x, (coords.y % 1) * (float)dimensions.y);
}

float GetInterpolatedFloatValue(float data, float dimension, float rate)
{
    return data * (float)dimension/rate;
}

uint SampleBaseSDF(StrokeData data, float2 pointID, float dimension) {
    float2 origin = GetOriginCoordinate(data.coords.xy, dimension);
    float thickness = GetInterpolatedFloatValue(data.thickness, dimension, 8.0);
    float length = GetInterpolatedFloatValue(data.length, dimension, 2.0);
    
    float2 endPoint = origin + normalize(data.direction).xy * length;
    
    //check closest point in all adjacent toroidal space grids
    float minDist = dimension;
    float interpolation = 0;
    [unroll(3)]
    for(int offsetX = -1; offsetX <= 1; offsetX++)
    {
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
    //TODO: To alter the starting position of the curve, pass interpolation through another curve function to alter where 0 and where 1 are in relation to t
        
    float minThickness = lerp(0, thickness, data.thicknessFalloffConstraint);
    //attenuate falloff if length is too short
    float lengthFalloff = data.lengthThicknessFalloff * step(thickness/2, length + minThickness/2);
    float fallOff = lerp(thickness, minThickness, FalloffFunction(lengthFalloff * interpolation));
    float sample = step(fallOff, minDist);
    float expectedPressure = data.pressure * 1 - FalloffFunction(data.pressureFalloff * interpolation);
    sample = (1 - sample) * expectedPressure;
    
    return (1 - sample) * 255;
}

uint SampleBaseSDFClamp(StrokeData data, float2 pointID, float dimension)
{
    float2 origin = GetOriginCoordinate(data.coords.xy, dimension);
    float thickness = GetInterpolatedFloatValue(data.thickness, dimension, 8.0);
    float length = GetInterpolatedFloatValue(data.length, dimension, 2.0);
    
    float2 endPoint = origin + normalize(data.direction).xy * length;
    float2 v = endPoint - origin;
    float2 u = pointID - origin;
    
    float t = saturate(dot(v, u)/dot(v, v));
    float2 closestPoint = origin + t * v;
    float2 dir = pointID - closestPoint;
    float minDist = sqrt(dir.x * dir.x + dir.y * dir.y);
    //TODO: To alter the starting position of the curve, pass interpolation through another curve function to alter where 0 and where 1 are in relation to t
        
    float minThickness = lerp(0, thickness, data.thicknessFalloffConstraint);
    //attenuate falloff if length is too short
    float lengthFalloff = data.lengthThicknessFalloff * step(thickness/2, length + minThickness/2);
    float fallOff = lerp(thickness, minThickness, FalloffFunction(lengthFalloff * t));
    float sample = step(fallOff, minDist);
    float expectedPressure = data.pressure * 1 - FalloffFunction(data.pressureFalloff * t);
    sample = (1 - sample) * expectedPressure;
    
    return (1 - sample) * 255;
}

uint SampleBaseSDFClamp(StrokeData data, float2 pointID, float2 dimensions)
{
    float largestDimension = max(dimensions.x, dimensions.y);
    float2 origin = GetOriginCoordinate(data.coords.xy, dimensions);
    float thickness = GetInterpolatedFloatValue(data.thickness, largestDimension, 8.0);
    float length = GetInterpolatedFloatValue(data.length, largestDimension, 2.0);
    
    float2 endPoint = origin + normalize(data.direction).xy * length;
    float2 v = endPoint - origin;
    float2 u = pointID - origin;
    
    float t = saturate(dot(v, u)/dot(v, v));
    float2 closestPoint = origin + t * v;
    float2 dir = pointID - closestPoint;
    float minDist = sqrt(dir.x * dir.x + dir.y * dir.y);
    //TODO: To alter the starting position of the curve, pass interpolation through another curve function to alter where 0 and where 1 are in relation to t
        
    float minThickness = lerp(0, thickness, data.thicknessFalloffConstraint);
    //attenuate falloff if length is too short
    float lengthFalloff = data.lengthThicknessFalloff * step(thickness/2, length + minThickness/2);
    float fallOff = lerp(thickness, minThickness, FalloffFunction(lengthFalloff * t));
    float sample = step(fallOff, minDist);
    float expectedPressure = data.pressure * 1 - FalloffFunction(data.pressureFalloff * t);
    sample = (1 - sample) * expectedPressure;
    
    return (1 - sample) * 255;
}

uint SampleBaseSDFClampParamScalar(StrokeData data, float2 pointID, float dimension, float thicknessScalar, float lengthScalar)
{
    float2 origin = GetOriginCoordinate(data.coords.xy, dimension);
    float thickness = GetInterpolatedFloatValue(data.thickness, dimension, 8.0 * thicknessScalar);
    float length = GetInterpolatedFloatValue(data.length, dimension, 2.0 * lengthScalar);
    
    float2 endPoint = origin + normalize(data.direction).xy * length;
    float2 v = endPoint - origin;
    float2 u = pointID - origin;
    
    float t = saturate(dot(v, u)/dot(v, v));
    float2 closestPoint = origin + t * v;
    float2 dir = pointID - closestPoint;
    float minDist = sqrt(dir.x * dir.x + dir.y * dir.y);
    //TODO: To alter the starting position of the curve, pass interpolation through another curve function to alter where 0 and where 1 are in relation to t
        
    float minThickness = lerp(0, thickness, data.thicknessFalloffConstraint);
    //attenuate falloff if length is too short
    float lengthFalloff = data.lengthThicknessFalloff * step(thickness/2, length + minThickness/2);
    float fallOff = lerp(thickness, minThickness, FalloffFunction(lengthFalloff * t));
    float sample = step(fallOff, minDist);
    float expectedPressure = data.pressure * 1 - FalloffFunction(data.pressureFalloff * t);
    sample = (1 - sample) * expectedPressure;
    
    return (1 - sample) * 255;
}

uint SampleBaseSDFClampParamScalar(StrokeData data, float2 pointID, float2 dimensions,  float thicknessScalar, float lengthScalar)
{
    float largestDimension = max(dimensions.x, dimensions.y);
    float2 origin = GetOriginCoordinate(data.coords.xy, dimensions);
    float thickness = GetInterpolatedFloatValue(data.thickness, largestDimension, 8.0 * thicknessScalar);
    float length = GetInterpolatedFloatValue(data.length, largestDimension, 2.0 * lengthScalar);
    
    float2 endPoint = origin + normalize(data.direction).xy * length;
    float2 v = endPoint - origin;
    float2 u = pointID - origin;
    
    float t = saturate(dot(v, u)/dot(v, v));
    float2 closestPoint = origin + t * v;
    float2 dir = pointID - closestPoint;
    float minDist = sqrt(dir.x * dir.x + dir.y * dir.y);
    //TODO: To alter the starting position of the curve, pass interpolation through another curve function to alter where 0 and where 1 are in relation to t
        
    float minThickness = lerp(0, thickness, data.thicknessFalloffConstraint);
    //attenuate falloff if length is too short
    float lengthFalloff = data.lengthThicknessFalloff * step(thickness/2, length + minThickness/2);
    float fallOff = lerp(thickness, minThickness, FalloffFunction(lengthFalloff * t));
    float sample = step(fallOff, minDist);
    float expectedPressure = data.pressure * 1 - FalloffFunction(data.pressureFalloff * t);
    sample = (1 - sample) * expectedPressure;
    
    return (1 - sample) * 255;
}

#endif