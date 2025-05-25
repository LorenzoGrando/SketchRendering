using UnityEngine;

[CreateAssetMenu(fileName = "TonalArtMapAsset", menuName = "Scriptable Objects/TonalArtMapAsset")]
public class TonalArtMapAsset : ScriptableObject
{
    [Range(1, 10)]
    public int ExpectedTones;

    //TextureReferences
}
