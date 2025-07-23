using System;
using UnityEngine;

[CreateAssetMenu(fileName = "MaterialDataAsset", menuName = "SketchRendering/Scriptable Objects/MaterialDataAsset")]
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
}