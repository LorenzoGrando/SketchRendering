using UnityEditor;
using UnityEngine;

public static class SketchRendererWizard
{
    [MenuItem(SketchRendererPackageData.PackageMenuItemPath + "Initialize Sketch Renderer with default settings", true)]
    private static bool InitializeDefaultInRendererValidation()
    {
        return !Application.isPlaying;
    }

    [MenuItem(SketchRendererPackageData.PackageMenuItemPath + "Initialize Sketch Renderer with default settings")]
    private static void InitializeDefaultInRenderer()
    {
        SketchRenderer.UpdateRendererToCurrentContext();
    }
}