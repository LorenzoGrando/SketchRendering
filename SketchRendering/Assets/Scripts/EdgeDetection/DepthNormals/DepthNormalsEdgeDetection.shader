Shader "DepthNormalsEdgeDetection"
{
   SubShader
   {
       Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
       ZWrite Off Cull Off
       Pass
       {
           Name "DepthEdges"

           HLSLPROGRAM
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
           #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

           #pragma vertex Vert
           #pragma fragment Frag

           float _OutlineThreshold;
           
           float DepthSample(Texture2D tex, SamplerState samp, float2 uv)
           {
               //Keep z as 0 to signify no offset
               float3 coordinateOffset = float3(1.0/_ScreenParams.x, 1.0/_ScreenParams.y, 0);
               
               float center = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_LOD(tex, samp, uv, _BlitMipLevel), _ZBufferParams).r;
               float up = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_LOD(tex, samp, uv + coordinateOffset.zy, _BlitMipLevel), _ZBufferParams).r;
               float down = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_LOD(tex, samp, uv - coordinateOffset.zy, _BlitMipLevel), _ZBufferParams).r;
               float right = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_LOD(tex, samp, uv + coordinateOffset.zx, _BlitMipLevel), _ZBufferParams).r;
               float left = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_LOD(tex, samp, uv - coordinateOffset.zx, _BlitMipLevel), _ZBufferParams).r;

               float sample = abs(up - center) + abs(down - center) + abs(left - center) + abs(right - center);
               return sample;
           }
           
           float4 Frag(Varyings input) : SV_Target0
           {
               UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

               // sample depth
               float2 uv = input.texcoord.xy;
               float depth = DepthSample(_BlitTexture, sampler_LinearRepeat, uv);

               depth = (depth > _OutlineThreshold) ? 1 : 0;
               
               return float4(depth.rrr, 1);
           }

           ENDHLSL
       }

       Pass
       {
           Name "NormalsEdges"

           HLSLPROGRAM
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
           #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

           #pragma vertex Vert
           #pragma fragment Frag

           float _OutlineThreshold;

           float3 ConvertNormalsToFullRange(float3 normal)
           {
               return normal * 2 - 1;
           }
           
           float NormalsSample(Texture2D tex, SamplerState samp, float2 uv)
           {
               //Keep z as 0 to signify no offset
               float3 coordinateOffset = float3(1.0/_ScreenParams.x, 1.0/_ScreenParams.y, 0);
               
               float3 center = ConvertNormalsToFullRange(SAMPLE_TEXTURE2D_X_LOD(tex, samp, uv, _BlitMipLevel).rgb);
               float3 up = ConvertNormalsToFullRange(SAMPLE_TEXTURE2D_X_LOD(tex, samp, uv + coordinateOffset.zy, _BlitMipLevel).rgb);
               float3 down = ConvertNormalsToFullRange(SAMPLE_TEXTURE2D_X_LOD(tex, samp, uv - coordinateOffset.zy, _BlitMipLevel).rgb);
               float3 right = ConvertNormalsToFullRange(SAMPLE_TEXTURE2D_X_LOD(tex, samp, uv + coordinateOffset.zx, _BlitMipLevel).rgb);
               float3 left = ConvertNormalsToFullRange(SAMPLE_TEXTURE2D_X_LOD(tex, samp, uv - coordinateOffset.zx, _BlitMipLevel).rgb);

               float sampleX = abs(up.x- center.x) + abs(down.x - center.x) + abs(left.x - center.x) + abs(right.x - center.x);
               float sampleY = abs(up.y- center.y) + abs(down.y - center.y) + abs(left.y - center.y) + abs(right.y - center.y);
               float sampleZ = abs(up.z- center.z) + abs(down.z - center.z) + abs(left.z - center.z) + abs(right.z - center.z);
               return max(sampleX, max(sampleY, sampleZ));
           }
           
           float4 Frag(Varyings input) : SV_Target0
           {
               UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    
               // sample depth
               float2 uv = input.texcoord.xy;
               float normal = NormalsSample(_BlitTexture, sampler_LinearRepeat, uv);

               normal = (normal > _OutlineThreshold) ? 1 : 0;
               
               return float4(normal.rrr, 1);
           }

           ENDHLSL
       }

        Pass
       {
           Name "Combine Edges"

           HLSLPROGRAM
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
           #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

           #pragma vertex Vert
           #pragma fragment Frag

           Texture2D _DepthOutlinesTexture;
           Texture2D _NormalsOutlinesTexture;
           
           float4 Frag(Varyings input) : SV_Target0
           {
               UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
               
               float2 uv = input.texcoord.xy;
               float depth = SAMPLE_TEXTURE2D_X_LOD(_DepthOutlinesTexture, sampler_LinearRepeat, uv, _BlitMipLevel);
               float normal = SAMPLE_TEXTURE2D_X_LOD(_NormalsOutlinesTexture, sampler_LinearRepeat, uv, _BlitMipLevel);
               
               
               return float4(max(depth, normal).rrr, 1);
           }

           ENDHLSL
       }
   }
}