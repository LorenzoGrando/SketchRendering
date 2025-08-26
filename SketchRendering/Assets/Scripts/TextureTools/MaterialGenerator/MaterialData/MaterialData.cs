using UnityEngine;

[System.Serializable]
public struct GranularityData
{
    public Vector2Int Scale;
    [Range(1, 10)]
    public int DetailLevel;
    public int DetailFrequency;
    [Range(0, 1)]
    public float DetailPersistence;
    [Range(0, 1)] 
    public float MinimumGranularity;
    [Range(0, 1)]
    public float MaximumGranularity;
    public Color GranularityTint;
}

[System.Serializable]
public struct LaidLineData
{
    public int LineFrequency;
    [Range(0, 1)]
    public float LineThickness;
    [Range(0, 1)]
    public float LineStrength;
    [Range(0, 1)]
    public float LineGranularityDisplacement;
    [Range(0, 1)]
    public float LineGranularityMasking;
    public Color LineTint;
}

[System.Serializable]
public struct CrumpleData
{
    public Vector2Int CrumpleScale;
    [Range(0, 1)]
    public float CrumplesJitter;
    [Range(0, 1)]
    public float CrumpleStrength;
    [Range(1, 10)]
    public int CrumpleDetailLevel;
    public int CrumpleDetailFrequency;
    [Range(0, 1)]
    public float CrumpleDetailPersistence;
    [Range(0, 1)]
    public float CrumpleTintStrength;
    public float CrumpleTintSharpness;
    public Color CrumpleTint;
}

[System.Serializable]
public struct NotebookLineData
{
    [Header("Common")]
    [Range(0, 1)]
    public float NotebookLineGranularitySensitivity;
    
    [Header("Horizontal Lines")]
    public float HorizontalLineFrequency;
    [Range(0, 1)]
    public float HorizontalLineOffset;
    [Range(0, 1)]
    public float HorizontalLineThickness;
    public Color HorizontalLineTint;
    
    [Header("Vertical Lines")]
    public float VerticalLineFrequency;
    [Range(0, 1)]
    public float VerticalLineOffset;
    [Range(0, 1)]
    public float VerticalLineThickness;
    public Color VerticalLineTint;
}
