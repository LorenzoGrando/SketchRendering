#ifndef SKETCH_SOBEL_DEPTH_NORMALS
#define SKETCH_SOBEL_DEPTH_NORMALS

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

    //Matrices in HLSL are columns based, so this visual below is the transposed kernel
    static float3x3 BaseSobel3X3HorizontalKernel = {
        float3(-1, -2, -1),
        float3(0, 0, 0),
        float3(1, 2, 1)
    };

    static float3x3 BaseSobel3X3VerticalKernel = {
        float3(-1, 0, 1),
        float3(-2, 0, 2),
        float3(-1, 0, 1)
    };

    static float3x3 ModifiedSobel3X3HorizontalKernel = {
        float3(-3, -10, -3),
        float3(0, 0, 0),
        float3(3, 10, 3)
    };

    static float3x3 ModifiedSobel3X3VerticalKernel = {
        float3(-3, 0, 3),
        float3(-10, 0, 10),
        float3(-3, 0, 3)
    };

    static float3 Sobel1X3Kernel = {
        1, 0, -1
    };

    float SobelDepthHorizontal3X3(float3x3 kernel, float2 uL, float2 cL, float2 dL, float2 uR, float2 cR, float2 dR)
    {
        float vUL = LinearEyeDepth(SampleSceneDepth(uL), _ZBufferParams) * kernel._11;
        float vCL = LinearEyeDepth(SampleSceneDepth(cL), _ZBufferParams) * kernel._21;
        float vDL = LinearEyeDepth(SampleSceneDepth(dL), _ZBufferParams) * kernel._31;
        float vUR = LinearEyeDepth(SampleSceneDepth(uR), _ZBufferParams) * kernel._13;
        float vCR = LinearEyeDepth(SampleSceneDepth(cR), _ZBufferParams) * kernel._23;
        float vDR = LinearEyeDepth(SampleSceneDepth(dR), _ZBufferParams) * kernel._33;

        return clamp((vUL + vCL + vDL + vUR + vCR + vDR), -1, 1);
    }

    float SobelDepthVertical3X3(float3x3 kernel, float2 uL, float2 uC, float2 uR, float2 dL, float2 dC, float2 dR)
    {
        float vUL = LinearEyeDepth(SampleSceneDepth(uL), _ZBufferParams) * kernel._11;
        float vUC = LinearEyeDepth(SampleSceneDepth(uC), _ZBufferParams) * kernel._12;
        float vUR = LinearEyeDepth(SampleSceneDepth(uR), _ZBufferParams) * kernel._13;
        float vDL = LinearEyeDepth(SampleSceneDepth(dL), _ZBufferParams) * kernel._31;
        float vDC = LinearEyeDepth(SampleSceneDepth(dC), _ZBufferParams) * kernel._32;
        float vDR = LinearEyeDepth(SampleSceneDepth(dR), _ZBufferParams) * kernel._33;
                   
        return clamp((vUL + vUC + vUR + vDL + vDC + vDR), -1, 1);
    }

    float SobelDepth1X3(float3 kernel, float2 uv0, float2 uv1, float2 uv2)
    {
        float v0 = LinearEyeDepth(SampleSceneDepth(uv0), _ZBufferParams) * kernel.r;
        float v1 = LinearEyeDepth(SampleSceneDepth(uv1), _ZBufferParams) * kernel.g;
        float v2 = LinearEyeDepth(SampleSceneDepth(uv2), _ZBufferParams) * kernel.b;

        return clamp((v0 + v1 + v2), -1, 1);
    }

    float3 FullRangeNormal(float3 normal)
    {
        return (normal * 2) - 1;
    }

    float SobelNormalHorizontal3x3(float3x3 kernel, float2 c, float2 uL, float2 cL, float2 dL, float2 uR, float2 cR, float2 dR)
    {
        float3 vUL = FullRangeNormal(SampleSceneNormals(uL)) * kernel._11;
        float3 vCL = FullRangeNormal(SampleSceneNormals(cL)) * kernel._21;
        float3 vDL = FullRangeNormal(SampleSceneNormals(dL)) * kernel._31;
        float3 vUR = FullRangeNormal(SampleSceneNormals(uR)) * kernel._13;
        float3 vCR = FullRangeNormal(SampleSceneNormals(cR)) * kernel._23;
        float3 vDR = FullRangeNormal(SampleSceneNormals(dR)) * kernel._33;

        float x = vUL.x + vCL.x + vDL.x + vUR.x + vCR.x + vDR.x;
        float y = vUL.y + vCL.y + vDL.y + vUR.y + vCR.y + vDR.y;
        float z = vUL.z + vCL.z + vDL.z + vUR.z + vCR.z + vDR.z;

        float n = max(x, max(y, z))/6.0;
        
        return clamp(n, -1, 1);
    }

    float SobelNormalVertical3x3(float3x3 kernel, float2 c, float2 uL, float2 uC, float2 uR, float2 dL, float2 dC, float2 dR)
    {
        float3 vUL = FullRangeNormal(SampleSceneNormals(uL)) * kernel._11;
        float3 vUC = FullRangeNormal(SampleSceneNormals(uC)) * kernel._12;
        float3 vUR = FullRangeNormal(SampleSceneNormals(uR)) * kernel._13;
        float3 vDL = FullRangeNormal(SampleSceneNormals(dL)) * kernel._31;
        float3 vDC = FullRangeNormal(SampleSceneNormals(dC)) * kernel._32;
        float3 vDR = FullRangeNormal(SampleSceneNormals(dR)) * kernel._33;
        

        float x = vUL.x + vUC.x + vDL.x + vUR.x + vDC.x + vDR.x;
        float y = vUL.y + vUC.y + vDL.y + vUR.y + vDC.y + vDR.y;
        float z = vUL.z + vUC.z + vDL.z + vUR.z + vDC.z + vDR.z;

        float n = max(x, max(y, z))/6.0;
        
        return clamp(n, -1, 1);
    }

    float SobelNormal1X3(float3 kernel, float2 uv0, float2 uv1, float2 uv2)
    {
        float3 v0 = FullRangeNormal(SampleSceneNormals(uv0)) * kernel.r;
        //float3 v1 = FullRangeNormal(SampleSceneNormals(uv1));
        float3 v2 = FullRangeNormal(SampleSceneNormals(uv2)) * kernel.b;

        float x = v0.x + v2.x;
        float y = v0.y + v2.y;
        float z = v0.z + v2.z;

        float n = max(x, max(y, z))/2.0;

        return clamp(n, -1, 1);
    }

#endif