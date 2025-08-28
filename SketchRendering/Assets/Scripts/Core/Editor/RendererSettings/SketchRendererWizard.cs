using UnityEditor;
using UnityEngine;

public static class SketchRendererWizard
{
    private const string DEFAULT_MENU_ITEM_PATH = "Tools/SketchRenderer/";
    
    public static bool IsSketchRendererPresent()
    {
        return SketchRendererDataWrapper.CheckHasActiveFeature(SketchRendererFeatureType.COMPOSITOR);
    }

    [MenuItem(DEFAULT_MENU_ITEM_PATH + "Initialize Sketch Renderer with default settings")]
    private static void InitializeInRenderer()
    {
        if (!IsSketchRendererPresent())
        {
            SketchRendererDataWrapper.AddRendererFeature(SketchRendererFeatureType.COMPOSITOR);
        }
    }
}