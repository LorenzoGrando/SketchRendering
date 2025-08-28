using UnityEngine;

public static class TextureProjectionGlobalData
{
    public enum TextureProjectionMethod
    {
        SCREEN_SPACE, OBJECT_SPACE, OBJECT_SPACE_CONSTANT_SCALE, OBJECT_SPACE_REVERSED_CONSTANT_SCALE
    }
    
    public static readonly string UVS_SCREEN_SPACE_KEYWORD = "UVS_SCREEN_SPACE";
    public static readonly string UVS_OBJECT_SPACE_KEYWORD = "UVS_OBJECT_SPACE";
    public static readonly string UVS_OBJECT_SPACE_CONSTANT_KEYWORD = "UVS_OBJECT_SPACE_CONSTANT";
    public static readonly string UVS_OBJECT_SPACE_REVERSED_CONSTANT_KEYWORD = "UVS_OBJECT_SPACE_REVERSED_CONSTANT";

    public static readonly int CONSTANT_SCALE_FALLOFF_SHADER_ID = Shader.PropertyToID("_ConstantScaleFalloff");
}
