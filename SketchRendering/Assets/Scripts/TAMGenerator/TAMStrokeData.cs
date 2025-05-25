using UnityEngine;

[System.Serializable]
public struct TAMStrokeData
{
    public Vector4 OriginPoint;
    public Vector4 Direction;
    [Range(0, 1)]
    public float Thickness;
    [Range(0, 1)] 
    public float ThicknessFalloffConstraint;
    [Range(0, 1)]
    public float Length;
    [Range(0, 1)]
    public float LengthThicknessFalloff;
    
    public int GetStrideLength()
    {
        //Vector4 + float + float
        return sizeof(float) * 4 + sizeof(float) * 4 + sizeof(float) + sizeof(float) + sizeof(float) + sizeof(float);
    }
}
