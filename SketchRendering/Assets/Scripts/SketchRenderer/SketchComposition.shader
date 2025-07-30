Shader "Hidden/SketchComposition"
{
    SubShader
   {
       Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
       ZWrite Off Cull Off
       Pass
       {
           Name "Final Sketch Composition"

           HLSLPROGRAM
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
           #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
           #include "Assets/Scripts/Includes/NeighborhoodSample.hlsl"
           #include "Assets/Scripts/EdgeDetection/DepthNormals/SobelDepthNormalsInclude.hlsl"
           
           #pragma vertex Vert
           #pragma fragment Frag

           #pragma multi_compile_local_fragment _ DEBUG_MATERIAL DEBUG_OUTLINES DEBUG_LUMINANCE

           Texture2D _MaterialTex;
           Texture2D _DirectionalTex;
           Texture2D _OutlineTex;
           Texture2D _LuminanceTex;

           float4 _OutlineColor;
           float4 _ShadingColor;

           float _MaterialAccumulationStrength;
           float4 _LuminanceBasisDirection;
           
           float4 Frag(Varyings input) : SV_Target0
           {
               UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
               float2 uv = input.texcoord;

               float4 material = SAMPLE_TEXTURE2D_X_LOD(_MaterialTex, sampler_LinearClamp, uv, _BlitMipLevel);
               float4 direction = (SAMPLE_TEXTURE2D_X_LOD(_DirectionalTex, sampler_LinearClamp, uv, _BlitMipLevel) - 0.5) * 2.0;
               float4 outline = SAMPLE_TEXTURE2D_X_LOD(_OutlineTex, sampler_LinearClamp, uv, _BlitMipLevel);
               float4 luminance = SAMPLE_TEXTURE2D_X_LOD(_LuminanceTex, sampler_LinearClamp, uv, _BlitMipLevel);

               #if defined DEBUG_MATERIAL
               return float4(direction.rgb, 1);
               return float4(material.rgb, 1);
               #elif defined(DEBUG_OUTLINES)
               outline = 1 - outline;
               return float4(outline.rrr, 1);
               #elif defined(DEBUG_LUMINANCE)
               return float4(luminance.rgb, 1);
               #endif
               float4 white = float4(1,1,1,1);

               float isOutlineStroke = outline.a;
               float4 outlineShade = outline.rrrr * _OutlineColor;
               float4 outlineDirection = float4(outline.gb, 0, 0);
               outline = lerp(white, outlineShade, isOutlineStroke * _OutlineColor.a);
               float outlineAccumulation = 1.0 - dot(outlineDirection, direction);
               outline *= lerp(1.0, 1.0 - _MaterialAccumulationStrength, outlineAccumulation * isOutlineStroke);

               float isLuminanceStroke = (1 - luminance.a);
               float4 lumShade = (1 - luminance) * _ShadingColor;
               luminance = lerp(white, lumShade, isLuminanceStroke * _ShadingColor.a);
               float luminanceAccumulation = 1.0 - dot(_LuminanceBasisDirection, direction) ;
               luminance *= lerp(1.0, 1.0 - _MaterialAccumulationStrength, luminanceAccumulation * isLuminanceStroke);


               //TODO: Make this an option
               //Outlines always on top
               float3 blend = (outline * isOutlineStroke) + luminance * !isOutlineStroke;

               //Blended values
               //float3 blend = outline * luminance;
               
               blend *= material;
               return float4(blend.rgb, 1);
           }

           ENDHLSL
       }
   }
}