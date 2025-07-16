#pragma once

float invLerp(float from, float to, float value){
    return (value - from) / (to - from);
}

float RangeConstrainedSmoothRandom(float current, float range, float rand, float minValue, float maxValue)
{
    float clampedOrigin = invLerp(minValue, maxValue, current);
    float smoothFactor = 1.0 - abs(clampedOrigin - 0.5f) * 2.0;
    float smoothRange = range - ((range/2.0) * smoothFactor);
    float minStep = min(1.0, max(0.0, clampedOrigin - smoothRange));
    float maxStep = max(0.0, min(1.0, clampedOrigin + smoothRange));
    float t = lerp(minStep, maxStep, saturate(rand));
    return lerp(minValue, maxValue, t);
}