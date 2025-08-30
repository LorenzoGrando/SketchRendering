using System;
using UnityEngine;

[CreateAssetMenu(fileName = "MaterialDataAsset", menuName = SketchRendererPackageData.PackageAssetItemPath + "MaterialDataAsset")]
public class MaterialDataAsset : ScriptableObject
{
    public bool UseGranularity;
    public GranularityData Granularity;

    [Space(10)] 
    public bool UseLaidLines;
    public LaidLineData LaidLines;
    
    [Space(10)]
    public bool UseCrumples;
    public CrumpleData Crumples;
    
    [Space(10)]
    public bool UseNotebookLines;
    public NotebookLineData NotebookLines;
}