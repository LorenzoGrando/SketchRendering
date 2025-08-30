using System;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "TonalArtMapAsset", menuName = SketchRendererPackageData.PackageAssetItemPath + "TonalArtMapAsset")]
public class TonalArtMapAsset : ScriptableObject
{
    [Range(1, 9)]
    public int ExpectedTones;
    public Texture2D[] Tones = new Texture2D[1];
    
    [SerializeField] [HideInInspector] public bool isPrePacked = false;
    [SerializeField] [HideInInspector] public Vector4 TAMBasisDirection;
    
    public bool IsPacked {get {return isPrePacked;}}

    private void OnEnable()
    {
        if(Tones == null)
            Tones = new Texture2D[ExpectedTones];
    }

    public float GetHomogenousFillRateThreshold()
    {
        return 1f/(float)ExpectedTones;
    }

    public void ResetTones()
    {
        Tones = new Texture2D[ExpectedTones];
        isPrePacked = false;
    }

    public void SetPackedTams(Texture2D[] packedTams)
    {
        Tones = packedTams;
        isPrePacked = true;
    }
}
