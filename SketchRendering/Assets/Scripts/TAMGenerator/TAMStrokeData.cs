using UnityEngine;

[System.Serializable]
public struct TAMStrokeData
{
    public Vector4 OriginPoint;
    [Range(0, 1)]
    public float Length;
    
    public int GetStrideLength()
    {
        //Vector2 + float
        return sizeof(float) * 4 + sizeof(float);
    }
}
