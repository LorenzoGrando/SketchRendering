#pragma once

#define SAMPLE_TEXTURE2D_CONSTANT_SCALE(tex, samp, texelSize, uv, offset) ConstantScaleTexture2DSample(tex, samp, texelSize, uv, offset)

#define CONSTANT_SCALE
//Kyle Halladay and bgolus:
//https://discussions.unity.com/t/making-per-object-uvs-in-screen-space/672066/10

//This returns the scales in X, Y and the blend factor in Z
float3 ConstantScaleUVs2D(float texelSize, float2 uv)
{
    float maxDer = max(length(ddx(uv)), length(ddy(uv)));
    float pixScale = 1.0 / (texelSize * maxDer);

    float2 pixScales = float2(
        exp2(floor(log2(pixScale))),
        exp2(ceil(log2(pixScale)))
    );

    float blend = frac(log2(pixScale));

    return float3(pixScales, blend);
}

float4 ConstantScaleTexture2DSample(Texture2D tex, SamplerState samp, float texelSize, float2 uv, float2 scaleOffset)
{
    float3 scales = ConstantScaleUVs2D(texelSize, uv);

    float4 s1 = SAMPLE_TEXTURE2D_X(tex, samp, scales.x * uv * scaleOffset);
    float4 s2 = SAMPLE_TEXTURE2D_X(tex, samp, scales.y * uv * scaleOffset);
    float4 sample = lerp(s1, s2, scales.z);
    return sample;
}