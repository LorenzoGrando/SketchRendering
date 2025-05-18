Shader "Hidden/DepthNormalsSilhouette"
{
   SubShader
   {
       Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
       ZWrite Off Cull Off
       Pass
       {
           Name "Horizontal Sobel"

           HLSLPROGRAM
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
           #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
           #include "Assets/Scripts/Includes/NeighborhoodSample.hlsl"
           #include "Assets/Scripts/Outlining/DepthNormals/SobelDepthNormalsInclude.hlsl"

           #pragma multi_compile _ SOURCE_DEPTH SOURCE_DEPTH_NORMALS
           
           #pragma vertex Vert
           #pragma fragment Frag
           
           float4 Frag(Varyings input) : SV_Target0
           {
               UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
               float2 uv = input.texcoord;
               
               //DepthSamples
               //UV positions for samples
               float2 dUL, dUC, dUR, dCL, dCR, dDL, dDC, dDR;
               Get3X3NeighborhoodPositions(uv, _CameraDepthTexture_TexelSize.xy, dUL, dUC, dUR, dCL, dCR, dDL, dDC, dDR);
               float depthEdge = SobelDepthHorizontalKernel(dUL, dCL, dDL, dUR, dCR, dDR);

               

               #if defined(SOURCE_DEPTH_NORMALS)
               //NormalsSamples
               //UV positions for samples
               float2 nUL, nUC, nUR, nCL, nCR, nDL, nDC, nDR;
               Get3X3NeighborhoodPositions(uv, _CameraNormalsTexture_TexelSize.xy, nUL, nUC, nUR, nCL, nCR, nDL, nDC, nDR);
               float3 normalEdge = SobelNormalHorizontalKernel(nUL, nCL, nDL, nUR, nCR, nDR);
               #endif

               //Store for use in vertical pass
               #if defined(SOURCE_DEPTH)
               return float4(depthEdge, 0, 0, 1);
               #elif defined(SOURCE_DEPTH_NORMALS)
               return float4(depthEdge, normalEdge.rgb);
               #endif
               return 1;
           }

           ENDHLSL
       }

       Pass
       {
           Name "Vertical Sobel"

           HLSLPROGRAM
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
           #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
           #include "Assets/Scripts/Includes/NeighborhoodSample.hlsl"
           #include "Assets/Scripts/Outlining/DepthNormals/SobelDepthNormalsInclude.hlsl"

           #pragma vertex Vert
           #pragma fragment Frag

           #pragma multi_compile_local _ SOURCE_DEPTH SOURCE_DEPTH_NORMALS

           float _OutlineThreshold;
           
           float4 Frag(Varyings input) : SV_Target0
           {
               UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
               float2 uv = input.texcoord;

               //DepthSamples
               //UV positions for samples
               float2 dUL, dUC, dUR, dCL, dCR, dDL, dDC, dDR;
               Get3X3NeighborhoodPositions(uv, _CameraDepthTexture_TexelSize.xy, dUL, dUC, dUR, dCL, dCR, dDL, dDC, dDR);
               float depthEdge = SobelDepthVerticalKernel(dUL, dUC, dUR, dDL, dDC, dDR);

               #if defined(SOURCE_DEPTH_NORMALS)
               //NormalsSamples
               //UV positions for samples
               float2 nUL, nUC, nUR, nCL, nCR, nDL, nDC, nDR;
               Get3X3NeighborhoodPositions(uv, _CameraNormalsTexture_TexelSize.xy, nUL, nUC, nUR, nCL, nCR, nDL, nDC, nDR);
               float3 normalEdge = SobelNormalVerticalKernel(nUL, nUC, nUR, nDL, nDC, nDR);
               
               #endif
               
               
               //Sample horizontal pass (passed as blit texture) and get filter results in RG (depth, normal)
               float4 horizontalValue = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, _BlitMipLevel);
               //Get gradient of each image, and threshold for silhouette)
               float depthGradient = step(_OutlineThreshold, length(float2(horizontalValue.r, depthEdge)));
               
               #if defined(SOURCE_DEPTH_NORMALS)
               float gradX = length(float2(horizontalValue.g, normalEdge.r));
               float gradY = length(float2(horizontalValue.b, normalEdge.g));
               float gradZ = length(float2(horizontalValue.a, normalEdge.b));
               float normalGradient = step(_OutlineThreshold, gradX + gradY + gradZ);
               #endif

               #if defined(SOURCE_DEPTH)
               return float4(max(0, depthGradient).rrr, 1);
               #elif defined(SOURCE_DEPTH_NORMALS)
               return float4(max(0, max(depthGradient, normalGradient)).rrr, 1);
               #endif
               return 1;
           }

           ENDHLSL
       }

        Pass
       {
           Name "Roberts Cross"

           HLSLPROGRAM
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
           #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
           #include "Assets/Scripts/Includes/NeighborhoodSample.hlsl"

           #pragma vertex Vert
           #pragma fragment Frag

           #pragma multi_compile_local_fragment _ SOURCE_DEPTH SOURCE_DEPTH_NORMALS

           float _OutlineThreshold;
           
           
           float4 Frag(Varyings input) : SV_Target0
           {
               UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
               
               return float4(1, 1, 1, 1);
           }

           ENDHLSL
       }
   }
}