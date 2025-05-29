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
    [Range(0, 1)] 
    public float Pressure;
    [Range(0, 1)] 
    public float PressureFalloff;
    
    public int GetStrideLength()
    {
        //Vector4 + float + float
        return (sizeof(float) * 4) * 2 + sizeof(float) * 6;
    }

    public TAMStrokeData Randomize()
    {
        TAMStrokeData output = new TAMStrokeData()
        {
            OriginPoint = OriginPoint,
            Direction = Direction,
            Thickness = Thickness,
            ThicknessFalloffConstraint = ThicknessFalloffConstraint,
            Length = Length,
            LengthThicknessFalloff = LengthThicknessFalloff,
            Pressure = Pressure,
            PressureFalloff = PressureFalloff
        };
        
        output.OriginPoint = new Vector4(Random.value, Random.value, 0, 0);
        return output;
    }
}
