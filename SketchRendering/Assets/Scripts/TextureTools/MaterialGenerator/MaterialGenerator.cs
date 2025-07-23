using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class MaterialGenerator : TextureGenerator
{
    protected override string DefaultFileOutputName => "MaterialAlbedoTexture";
    
    [Header("MaterialGenerator Settings")]
    public Shader MaterialGeneratorShader;
    public MaterialDataAsset MaterialData;
    
    private Material blitMaterial;
    
    //Shader Data
    private const int ALBEDO_PASS_ID = 0;
    
    //Shader Properties
    //Granularity
    private readonly int GRANULARITY_SCALE_ID = Shader.PropertyToID("_GranularityScale");
    private readonly int GRANULARITY_OCTAVES_ID = Shader.PropertyToID("_GranularityOctaves");
    private readonly int GRANULARITY_LACUNARITY_ID = Shader.PropertyToID("_GranularityLacunarity");
    private readonly int GRANULARITY_PERSISTENCE_ID = Shader.PropertyToID("_GranularityPersistence");
    private readonly int GRANULARITY_VALUE_RANGES_ID = Shader.PropertyToID("_GranularityValueRange");
    
    //Laid Lines
    private readonly int LAID_LINE_FREQUENCY_ID = Shader.PropertyToID("_LaidLineFrequency");
    private readonly int LAID_LINE_THICKNESS_ID = Shader.PropertyToID("_LaidLineThickness");
    private readonly int LAID_LINE_STRENGTH_ID = Shader.PropertyToID("_LaidLineStrength");
    private readonly int LAID_LINE_GRANULARITY_DISPLACEMENT_ID = Shader.PropertyToID("_LaidLineDisplacement");
    private readonly int LAID_LINE_GRANULARITY_MASK_ID = Shader.PropertyToID("_LaidLineMask");
    
    //Crumples
    private readonly int CRUMPLES_SCALE_ID = Shader.PropertyToID("_CrumplesScale");
    private readonly int CRUMPLES_JITTER_ID = Shader.PropertyToID("_CrumplesJitter");
    private readonly int CRUMPLES_STRENGTH_ID = Shader.PropertyToID("_CrumplesStrength");
    private readonly int CRUMPLES_OCTAVES_ID = Shader.PropertyToID("_CrumplesOctaves");
    private readonly int CRUMPLES_LACUNARITY_ID = Shader.PropertyToID("_CrumplesLacunarity");
    private readonly int CRUMPLES_PERSISTENCE_ID = Shader.PropertyToID("_CrumplesPersistence");
    private readonly int CRUMPLES_TINT_STRENGTH_ID = Shader.PropertyToID("_CrumplesTintStrength");
    
    //Shader Keywords
    private readonly string USE_GRANULARITY_KEYWORD = "USE_GRANULARITY";
    private readonly string USE_LAID_LINES_KEYWORD = "USE_LAID_LINES";
    private readonly string USE_CRUMPLES_KEYWORD = "USE_CRUMPLES";

    private LocalKeyword GranularityKeyword;
    private LocalKeyword LaidLineKeyword;
    private LocalKeyword CrumpleKeyword;
    
    public override void ConfigureGeneratorData()
    {
        if(MaterialGeneratorShader == null || MaterialData == null)
            return;
        
        base.ConfigureGeneratorData();
        
        PrepareData();
    }

    protected override void CreateMaterial()
    {
        if (blitMaterial == null)
        {
            blitMaterial = new Material(MaterialGeneratorShader);
            blitMaterial.hideFlags = HideFlags.HideAndDontSave;
        }
        
        base.CreateMaterial();
    }

    private void PrepareData()
    {
        if(blitMaterial == null)
            return;
        
        ManageMaterialDataKeywords();
        UpdateShaderMaterialData();
    }

    private void ManageMaterialDataKeywords()
    {
        GranularityKeyword = new LocalKeyword(MaterialGeneratorShader, USE_GRANULARITY_KEYWORD);
        blitMaterial.SetKeyword(GranularityKeyword, MaterialData.UseGranularity);

        LaidLineKeyword = new LocalKeyword(MaterialGeneratorShader, USE_LAID_LINES_KEYWORD);
        blitMaterial.SetKeyword(LaidLineKeyword, MaterialData.UseLaidLines);
        
        CrumpleKeyword = new LocalKeyword(MaterialGeneratorShader, USE_CRUMPLES_KEYWORD);
        blitMaterial.SetKeyword(CrumpleKeyword, MaterialData.UseCrumples);
    }

    private void UpdateShaderMaterialData()
    {
        if (MaterialData.UseGranularity)
        {
            blitMaterial.SetVector(GRANULARITY_SCALE_ID, new Vector4(MaterialData.Granularity.Scale.x, MaterialData.Granularity.Scale.y, 0, 0));
            blitMaterial.SetInt(GRANULARITY_OCTAVES_ID, MaterialData.Granularity.DetailLevel);
            blitMaterial.SetFloat(GRANULARITY_LACUNARITY_ID, MaterialData.Granularity.DetailFrequency);
            blitMaterial.SetFloat(GRANULARITY_PERSISTENCE_ID, MaterialData.Granularity.DetailPersistence);
            blitMaterial.SetVector(GRANULARITY_VALUE_RANGES_ID, new Vector4(MaterialData.Granularity.MinimumGranularity, MaterialData.Granularity.MaximumGranularity, 0, 0));
        }

        if (MaterialData.UseLaidLines)
        {
            blitMaterial.SetFloat(LAID_LINE_FREQUENCY_ID, MaterialData.LaidLines.LineFrequency);
            blitMaterial.SetFloat(LAID_LINE_THICKNESS_ID, MaterialData.LaidLines.LineThickness);
            blitMaterial.SetFloat(LAID_LINE_STRENGTH_ID, MaterialData.LaidLines.LineStrength);
            blitMaterial.SetFloat(LAID_LINE_GRANULARITY_DISPLACEMENT_ID, MaterialData.LaidLines.LineGranularityDisplacement);
            blitMaterial.SetFloat(LAID_LINE_GRANULARITY_MASK_ID, MaterialData.LaidLines.LineGranularityMasking);
        }

        if (MaterialData.UseCrumples)
        {
            blitMaterial.SetVector(CRUMPLES_SCALE_ID, new Vector4(MaterialData.Crumples.CrumpleScale.x, MaterialData.Crumples.CrumpleScale.y, 0, 0));
            blitMaterial.SetFloat(CRUMPLES_JITTER_ID, MaterialData.Crumples.CrumplesJitter);
            blitMaterial.SetFloat(CRUMPLES_STRENGTH_ID, MaterialData.Crumples.CrumpleStrength);
            blitMaterial.SetInt(CRUMPLES_OCTAVES_ID, MaterialData.Crumples.CrumpleDetailLevel);
            blitMaterial.SetFloat(CRUMPLES_LACUNARITY_ID, MaterialData.Crumples.CrumpleDetailFrequency);
            blitMaterial.SetFloat(CRUMPLES_PERSISTENCE_ID, MaterialData.Crumples.CrumpleDetailPersistence);
            blitMaterial.SetFloat(CRUMPLES_TINT_STRENGTH_ID, MaterialData.Crumples.CrumpleTintStrength);
        }
    }
    
    #region Create Texture

    public void UpdateMaterialAlbedoTexture()
    {
        CreateOrUpdateTarget();
        ConfigureGeneratorData();
        BlitAlbedoTexture();
    }

    private void BlitAlbedoTexture()
    {
        Graphics.Blit(null, targetRT,  blitMaterial, ALBEDO_PASS_ID);
    }
    
    #endregion
}
