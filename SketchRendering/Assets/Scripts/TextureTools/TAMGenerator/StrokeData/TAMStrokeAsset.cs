using Unity.Mathematics.Geometry;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "SimpleStrokeAsset", menuName = SketchRendererPackageData.PackageAssetItemPath + "TAMStrokeAssets/SimpleStrokeAsset")]
public class TAMStrokeAsset : ScriptableObject
{
    public TAMStrokeData StrokeData;
    public FalloffFunction SelectedFalloffFunction;
    
    [Space(5)]
    [Header("Per Iteration Variability")]
    public TAMVariationData VariationData;
    
    public virtual StrokeSDFType PatternType => StrokeSDFType.SIMPLE;

    public virtual TAMStrokeData UpdatedDataByFillRate(float fillRate)
    {
        return PackAdditionalData(StrokeData);
    }
    
    public virtual TAMStrokeData GetSampleReadyData() => UpdatedDataByFillRate(0f);
    
    public virtual TAMStrokeData Randomize(float fillRate)
    {
        TAMStrokeData output = new TAMStrokeData()
        {
            OriginPoint = StrokeData.OriginPoint,
            Direction = VariationData.DirectionVariationRange == 0 ? StrokeData.Direction : 
                new Vector4(GetRangeConstrainedSmoothRandom(StrokeData.Direction.x, VariationData.DirectionVariationRange, -1, 1), 
                    GetRangeConstrainedSmoothRandom(StrokeData.Direction.y, VariationData.DirectionVariationRange, -1, 1), 0, 0),
            AdditionalPackedData = Vector4.zero,
            Thickness = VariationData.ThicknessVariationRange == 0 ? StrokeData.Thickness : GetRangeConstrainedSmoothRandom(StrokeData.Thickness, VariationData.ThicknessVariationRange),
            ThicknessFalloffConstraint = StrokeData.ThicknessFalloffConstraint,
            Length = VariationData.LengthVariationRange == 0 ? StrokeData.Length : GetRangeConstrainedSmoothRandom(StrokeData.Length, VariationData.LengthVariationRange),
            LengthThicknessFalloff = StrokeData.LengthThicknessFalloff,
            Pressure = VariationData.PressureVariationRange == 0 ? StrokeData.Pressure : GetRangeConstrainedSmoothRandom(StrokeData.Pressure, VariationData.PressureVariationRange),
            PressureFalloff = StrokeData.PressureFalloff,
            Iterations = StrokeData.Iterations
        };
        
        output.OriginPoint = new Vector4(Random.value, Random.value, 0, 0);
        output = PackAdditionalData(output);
        
        return StrokeData;
    }
    
    public TAMStrokeData PreviewDisplay()
    {
        TAMStrokeData output = new TAMStrokeData()
        {
            OriginPoint = new Vector4(0.25f, 0.5f, 0f, 0f),
            Direction = StrokeData.Direction,
            AdditionalPackedData = StrokeData.AdditionalPackedData,
            Thickness = StrokeData.Thickness,
            ThicknessFalloffConstraint = StrokeData.ThicknessFalloffConstraint,
            Length = StrokeData.Length,
            LengthThicknessFalloff = StrokeData.LengthThicknessFalloff,
            Pressure = StrokeData.Pressure,
            PressureFalloff = StrokeData.PressureFalloff,
            Iterations = StrokeData.Iterations
        };
        output = PackAdditionalData(output);
        
        return output;
    }

    protected float GetRangeConstrainedRandom(float original, float range, float min = 0, float max = 1)
    {
        float minStep = Mathf.Max(min, original - range);
        float maxStep = Mathf.Min(max, original + range);
        return Mathf.Lerp(minStep, maxStep, Random.value);
    }

    protected float GetRangeConstrainedSmoothRandom(float original, float range, float min = 0, float max = 1)
    {
        float clampedOrigin = Mathf.InverseLerp(min, max, original);
        float smoothFactor =  1f - Mathf.Abs(clampedOrigin - 0.5f) * 2f;
        float minStep = Mathf.Max(0, clampedOrigin - (range - ((range/2f) * smoothFactor)));
        float maxStep = Mathf.Min(1, clampedOrigin + (range - ((range/2f) * smoothFactor)));
        float t = Mathf.Lerp(minStep, maxStep, Random.value);
        float rand = Mathf.Lerp(min, max, t);
        return rand;
    }
    
    protected virtual TAMStrokeData PackAdditionalData(TAMStrokeData data) =>  data;
}
