using UnityEngine;

[CreateAssetMenu(fileName = "HatchingStrokeAsset", menuName = "Scriptable Objects/TAMStrokeAssets/HatchingStrokeAsset")]
public class HatchingTAMStrokeAsset : TAMStrokeAsset
{
    public override StrokeSDFType PatternType => StrokeSDFType.HATCHING;
    [Space(5)]
    [Header("Hatching Specific")]
    [Range(0, 1)]
    public float MinCrossHatchingThreshold;
    [Range(0, 1)]
    public float MaxCrossHatchingThreshold;

    public override TAMStrokeData Randomize(float fillRate)
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
        if(ShouldCrossHatch(fillRate))
            output.Direction = GetPerpendicularCrosshatch(output.Direction);
        output.OriginPoint = new Vector4(Random.value, Random.value, 0, 0);
        return output;
    }

    private bool ShouldCrossHatch(float fillRate)
    {
        if (fillRate > MinCrossHatchingThreshold && fillRate < MaxCrossHatchingThreshold)
            return true;
        else if (fillRate > MaxCrossHatchingThreshold)
            return Random.value > 0.5f;
        else
            return false;
    }

    private Vector4 GetPerpendicularCrosshatch(Vector4 Direction)
    {
        Vector3 Perpendicular = Vector3.Cross(new Vector3(Direction.x, Direction.y, Direction.z), Vector3.forward);
        return new Vector4(Perpendicular.x, Perpendicular.y, 0, 0);
    }
}
