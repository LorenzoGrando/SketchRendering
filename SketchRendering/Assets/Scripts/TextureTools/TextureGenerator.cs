using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

public abstract class TextureGenerator : MonoBehaviour
{
    [Header("Base Settings")]
    public Shader DisplayShader;
    public TextureResolution Resolution;
    public string OverwriteTexturesOutputPath;
    
    protected int Dimension;
    
    //Editor assets
    protected RenderTexture targetRT;
    protected Material displayMaterial;
    public Material GetRTMaterial { get { return displayMaterial; } }
    
    //File Management
    protected abstract string DefaultFileOutputName { get; }
    protected virtual string DefaultFileOutputPath
    {
        get
        {
            return "Assets";
        }
    }
    
#if UNITY_EDITOR
    private TextureImporterType textureOutputType = TextureImporterType.Default;
    public TextureImporterType TextureOutputType
    {
        get { return textureOutputType; } protected set { textureOutputType = value; }
    }
#endif
    
    public virtual void OnEnable()
    {
        if(DisplayShader == null)
            return;
        CreateMaterial();
    }
    
    public virtual void OnValidate()
    {
        ConfigureGeneratorData();
    }

    public virtual void ConfigureGeneratorData()
    {
        Dimension = TextureAssetManager.GetTextureResolution(Resolution);
        
        if(targetRT == null || Dimension != targetRT.width)
            CreateOrUpdateTarget();
    }
    
    #region Asset Prep
    public void CreateOrUpdateTarget()
    {
        if (targetRT != null)
        {
            targetRT.Release();
            targetRT = null;
        }

        targetRT = CreateRT(Dimension);
        
        //TEMP
        if (targetRT && displayMaterial)
        {
            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr)
            {
                displayMaterial.mainTexture = targetRT;
                mr.material = displayMaterial;
            }
        }
    }

    protected virtual void CreateMaterial()
    {
        if(displayMaterial != null)
            return;
        
        displayMaterial = new Material(DisplayShader);
        displayMaterial.hideFlags = HideFlags.HideAndDontSave;
    }
    
    protected RenderTexture CreateRT(int dimension)
    {
        RenderTexture rt = new RenderTexture(dimension, dimension, GraphicsFormat.R8G8B8A8_SRGB, GraphicsFormat.None);
        rt.enableRandomWrite = true;
        rt.hideFlags = HideFlags.HideAndDontSave;
        Graphics.Blit(Texture2D.whiteTexture, rt);
        return rt;
    }
    
    protected RenderTexture CreateTempTargetCopy()
    {
        if (targetRT == null)
            return null;

        return CopyRT(targetRT);
    }
    
    protected RenderTexture CopyRT(RenderTexture copy)
    {
        RenderTexture rt = new RenderTexture(copy);
        rt.enableRandomWrite = true;
        rt.hideFlags = HideFlags.HideAndDontSave;
        
        return rt;
    }
    
    protected RenderTexture CopyToRT(Texture2D copy)
    {
        RenderTexture rt = new RenderTexture(copy.width, copy.height, 32);
        rt.enableRandomWrite = true;
        rt.hideFlags = HideFlags.HideAndDontSave;
        RenderTexture.active = rt;
        Graphics.Blit(copy, rt);
        RenderTexture.active = null;
        return rt;
    }
    
    #endregion
    
    #region Editor Asset Management
    
    public Texture2D SaveCurrentTargetTexture(bool overwrite, string fileName = null)
    {
        if (targetRT == null)
            return null;

        string path = GetTextureOutputPath();
        
        if(fileName == null)
            fileName = DefaultFileOutputName;
        
        return TextureAssetManager.OutputToAssetTexture(targetRT, path, fileName, overwrite);
    }
    
#if UNITY_EDITOR
    public Texture2D SaveCurrentTargetTexture(TextureImporterType texType, bool overwrite, string fileName = null)
    {
        if (targetRT == null)
            return null;

        string path = GetTextureOutputPath();
        
        if(fileName == null)
            fileName = DefaultFileOutputName;
        
        return TextureAssetManager.OutputToAssetTexture(targetRT, path, fileName, overwrite, texType);
    }
#endif
    
    protected string GetTextureOutputPath()
    {
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(OverwriteTexturesOutputPath))
            return OverwriteTexturesOutputPath;
        else return DefaultFileOutputPath;
#elif !UNITY_EDITOR
        return string.Empty;
#endif
    }
    #endregion
}