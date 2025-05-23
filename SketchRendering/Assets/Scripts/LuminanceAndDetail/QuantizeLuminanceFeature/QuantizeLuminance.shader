Shader "Hidden/QuantizeLuminance"
{
    SubShader
   {
       Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
       ZWrite Off Cull Off
       Pass
       {
           Name "Image Luminance"

           HLSLPROGRAM
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
           #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
           #include "Assets/Scripts/Includes/NeighborhoodSample.hlsl"
           #include "Assets/Scripts/EdgeDetection/DepthNormals/SobelDepthNormalsInclude.hlsl"
           
           #pragma vertex Vert
           #pragma fragment Frag

           int _NumTones;
           
           float4 Frag(Varyings input) : SV_Target0
           {
               UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
               float2 uv = input.texcoord;

               //get pixel luminance: https://stackoverflow.com/questions/596216/formula-to-determine-perceived-brightness-of-rgb-color
               float4 col = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, _BlitMipLevel);
               //simple luminance
               //float lum = (col.r * 2 + col.b + + col.g * 3)/6.0;
               //perceived luminance
               float lum = col.r * 0.299 + col.g * 0.587 + col.b * 0.114;
               float quant = floor(lum * _NumTones)/_NumTones;
               
               return float4(quant.rrr,1);
           }

           ENDHLSL
       }
   }
}