using UnityEngine;

public static class ScreenUVRenderUtils
{
    public const string TextureName = "_CameraUVsTexture";
    private static readonly int textureShaderID = Shader.PropertyToID(TextureName);
    
    public static int GetUVTextureID => textureShaderID;
}
