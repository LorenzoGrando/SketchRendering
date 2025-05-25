#ifndef FALLOFF_FUNCTIONS
#define FALLOFF_FUNCTIONS

static const float PI = 3.14159265f;

float LinearFalloff(float t)
{
    return t;
}

float EaseInOutSineFalloff(float t)
{
    return -(cos(PI * t) - 1)/ 2;
}

#endif