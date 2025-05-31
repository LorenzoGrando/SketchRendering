using System;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "TonalArtMapAsset", menuName = "Scriptable Objects/TonalArtMapAsset")]
public class TonalArtMapAsset : ScriptableObject
{
    [Range(1, 10)]
    public int ExpectedTones;

    //TextureReferences
    public Texture2D[] Tones;

    private void OnEnable()
    {
        if(Tones == null)
            Tones = new Texture2D[ExpectedTones];
    }

    public float GetHomogenousFillRateThreshold()
    {
        return 1f/(float)ExpectedTones;
    }
}
