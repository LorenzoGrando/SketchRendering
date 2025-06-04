using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "SimpleStrokeAsset", menuName = "Scriptable Objects/TAMStrokeAssets/SimpleStrokeAsset")]
public class TAMStrokeAsset : ScriptableObject
{
    public TAMStrokeData StrokeData;
    public FalloffFunction SelectedFalloffFunction;
    
    [Space(5)]
    [Header("Per Iteration Variability")]
    [Range(0f, 1f)]
    public float DirectionVariationRange = 0;
    [Range(0f, 1f)]
    public float ThicknessVariationRange = 0;
    [Range(0f, 1f)]
    public float LengthVariationRange = 0;
    [Range(0f, 1f)]
    public float PressureVariationRange = 0;
    
    public virtual StrokeSDFType PatternType => StrokeSDFType.SIMPLE;

    public virtual TAMStrokeData Randomize(float fillRate)
    {
        TAMStrokeData output = new TAMStrokeData()
        {
            OriginPoint = StrokeData.OriginPoint,
            Direction = DirectionVariationRange == 0 ? StrokeData.Direction : new Vector4(GetRangeConstrainedRandom(StrokeData.Direction.x, DirectionVariationRange, -1, 1), GetRangeConstrainedRandom(StrokeData.Direction.y, DirectionVariationRange, -1, 1), 0, 0),
            Thickness = ThicknessVariationRange == 0 ? StrokeData.Thickness : GetRangeConstrainedRandom(StrokeData.Thickness, ThicknessVariationRange),
            ThicknessFalloffConstraint = StrokeData.ThicknessFalloffConstraint,
            Length = LengthVariationRange == 0 ? StrokeData.Length : GetRangeConstrainedRandom(StrokeData.Length, LengthVariationRange),
            LengthThicknessFalloff = StrokeData.LengthThicknessFalloff,
            Pressure = PressureVariationRange == 0 ? StrokeData.Pressure : GetRangeConstrainedRandom(StrokeData.Pressure, PressureVariationRange),
            PressureFalloff = StrokeData.PressureFalloff
        };
        
        output.OriginPoint = new Vector4(Random.value, Random.value, 0, 0);
        return output;
    }
    
    public TAMStrokeData PreviewDisplay()
    {
        TAMStrokeData output = new TAMStrokeData()
        {
            OriginPoint = new Vector4(0.25f, 0.5f, 0f, 0f),
            Direction = new Vector4(1, 0, 0f, 0f),
            Thickness = StrokeData.Thickness,
            ThicknessFalloffConstraint = StrokeData.ThicknessFalloffConstraint,
            Length = StrokeData.Length,
            LengthThicknessFalloff = StrokeData.LengthThicknessFalloff,
            Pressure = StrokeData.Pressure,
            PressureFalloff = StrokeData.PressureFalloff
        };
        
        return output;
    }

    protected float GetRangeConstrainedRandom(float original, float range, float min = 0, float max = 1)
    {
        float minStep = Mathf.Max(min, original - range);
        float maxStep = Mathf.Min(max, original + range);
        return Mathf.Lerp(minStep, maxStep, Random.value);
    }
}
