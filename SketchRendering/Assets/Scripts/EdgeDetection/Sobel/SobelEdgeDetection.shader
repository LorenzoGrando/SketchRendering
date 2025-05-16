Shader "SobelEdgeDetection"
{
   SubShader
   {
       Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
       ZWrite Off Cull Off
       Pass
       {
           Name "SobelEdge"

           HLSLPROGRAM
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
           #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

           #pragma vertex Vert
           #pragma fragment Frag

           float _OutlineThreshold;

           float SobelSample(Texture2D tex, SamplerState samp, float2 uv)
           {
               //Keep z as 0 to signify no offset
               float3 coordinateOffset = float3(1.0/_ScreenParams.x, 1.0/_ScreenParams.y, 0);
               
               float center = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_LOD(tex, samp, uv, _BlitMipLevel), _ZBufferParams).r;
               float up = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_LOD(tex, samp, uv + coordinateOffset.zy, _BlitMipLevel), _ZBufferParams).r;
               float down = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_LOD(tex, samp, uv - coordinateOffset.zy, _BlitMipLevel), _ZBufferParams).r;
               float right = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_LOD(tex, samp, uv + coordinateOffset.zx, _BlitMipLevel), _ZBufferParams).r;
               float left = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_LOD(tex, samp, uv - coordinateOffset.zx, _BlitMipLevel), _ZBufferParams).r;

               float sobel = abs(up - center) + abs(down - center) + abs(left - center) + abs(right - center);
               return sobel;
           }
           
           float4 Frag(Varyings input) : SV_Target0
           {
               UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

               // sample depth
               float2 uv = input.texcoord.xy;
               float sobelEdge = SobelSample(_BlitTexture, sampler_LinearRepeat, uv);

               sobelEdge = (sobelEdge > _OutlineThreshold) ? 1 : 0;
               
               return float4(sobelEdge.rrr, 1);
           }

           ENDHLSL
       }
   }
}