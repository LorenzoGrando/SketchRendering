using System;
using UnityEditor;
using UnityEngine;

public static class SketchRenderer
{
    private static SketchRendererContext currentRendererContext;
    public static SketchRendererContext CurrentRendererContext
    {
        get
        {
            if (currentRendererContext == null) currentRendererContext = Resources.Load<SketchRendererContext>("SketchRendererContext/DefaultSketchRendererContext");
            
            return currentRendererContext;
        }
    }
    private static readonly SketchRendererFeatureType[] featureTypesInPackage = Enum.GetValues(typeof(SketchRendererFeatureType)) as SketchRendererFeatureType[];
    private static readonly int totalFeatureTypes = featureTypesInPackage.Length;
        
    public static bool IsSketchRendererPresent()
    {
        return SketchRendererDataWrapper.CheckHasActiveFeature(SketchRendererFeatureType.COMPOSITOR);
    }

    public static void UpdateRendererToCurrentContext()
    {
        if (CurrentRendererContext == null)
            throw new NullReferenceException("[SketchRenderer] Current renderer context is not set.");

        UpdateRendererByContext(CurrentRendererContext);
    }

    private static void UpdateRendererByContext(SketchRendererContext rendererContext)
    {
        if (rendererContext == null)
            throw new NullReferenceException("[SketchRenderer] Renderer context used to configure is not set.");
        
        Span<(SketchRendererFeatureType Feature, bool Active)> features = stackalloc (SketchRendererFeatureType, bool)[totalFeatureTypes];
        for (int i = 0; i < totalFeatureTypes; i++)
        {
            features[i] = (featureTypesInPackage[i], rendererContext.IsFeaturePresent(featureTypesInPackage[i]));
        }
        
        for (int i = 0; i < totalFeatureTypes; i++)
        {
            if (features[i].Active)
                SketchRendererDataWrapper.ConfigureRendererFeature(features[i].Feature, rendererContext);
            else
                SketchRendererDataWrapper.RemoveRendererFeature(features[i].Feature);
        }
    }
}
