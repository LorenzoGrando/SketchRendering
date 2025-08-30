using System;
using UnityEngine;
using Random = UnityEngine.Random;

[CreateAssetMenu(fileName = "FeatheringStrokeAsset", menuName = SketchRendererPackageData.PackageAssetItemPath + "TAMStrokeAssets/FeatheringStrokeAsset")]
public class FeatheringTAMStrokeAsset : TAMStrokeAsset
{
    public override StrokeSDFType PatternType => StrokeSDFType.FEATHERING;

    [Space(5)] [Header("Feathering Specific")] 
    [Range(-1, 1f)]
    public float FirstSubStrokeDirectionOffset;
    public float FirstSubStrokeLengthMultiplier;
    [Range(-1, 1f)]
    public float SecondSubStrokeDirectionOffset;
    public float SecondSubStrokeLengthMultiplier;
    [Range(1, 5)]
    public int Repetitions = 1;


    public override TAMStrokeData UpdatedDataByFillRate(float fillRate)
    {
        TAMStrokeData output = new TAMStrokeData()
        {
            OriginPoint = StrokeData.OriginPoint,
            Direction = StrokeData.Direction,
            AdditionalPackedData = StrokeData.AdditionalPackedData,
            Thickness = StrokeData.Thickness,
            ThicknessFalloffConstraint = StrokeData.ThicknessFalloffConstraint,
            Length = StrokeData.Length,
            LengthThicknessFalloff = StrokeData.LengthThicknessFalloff,
            Pressure = StrokeData.Pressure,
            PressureFalloff = StrokeData.PressureFalloff,
            Iterations = StrokeData.Iterations,
        };
        output = PackAdditionalData(output);
        return output;
    }

    public override TAMStrokeData Randomize(float fillRate)
    {
        TAMStrokeData output = new TAMStrokeData()
        {
            OriginPoint = StrokeData.OriginPoint,
            Direction = VariationData.DirectionVariationRange == 0
                ? StrokeData.Direction
                : new Vector4(
                    GetRangeConstrainedSmoothRandom(StrokeData.Direction.x, VariationData.DirectionVariationRange, -1,
                        1),
                    GetRangeConstrainedSmoothRandom(StrokeData.Direction.y, VariationData.DirectionVariationRange, -1,
                        1), 0, 0),
            AdditionalPackedData = StrokeData.AdditionalPackedData,
            Thickness = VariationData.ThicknessVariationRange == 0
                ? StrokeData.Thickness
                : GetRangeConstrainedSmoothRandom(StrokeData.Thickness, VariationData.ThicknessVariationRange),
            ThicknessFalloffConstraint = StrokeData.ThicknessFalloffConstraint,
            Length = VariationData.LengthVariationRange == 0
                ? StrokeData.Length
                : GetRangeConstrainedSmoothRandom(StrokeData.Length, VariationData.LengthVariationRange),
            LengthThicknessFalloff = StrokeData.LengthThicknessFalloff,
            Pressure = VariationData.PressureVariationRange == 0
                ? StrokeData.Pressure
                : GetRangeConstrainedSmoothRandom(StrokeData.Pressure, VariationData.PressureVariationRange),
            PressureFalloff = StrokeData.PressureFalloff,
            Iterations = StrokeData.Iterations,
        };
        output.OriginPoint = new Vector4(Random.value, Random.value, 0, 0);
        output = PackAdditionalData(output);
        return output;
    }

    protected override TAMStrokeData PackAdditionalData(TAMStrokeData data)
    {
        Vector4 additonalData = new Vector4
        {
            x = FirstSubStrokeDirectionOffset,
            y = FirstSubStrokeLengthMultiplier,
            z = SecondSubStrokeDirectionOffset,
            w = SecondSubStrokeLengthMultiplier
        };
        data.AdditionalPackedData = additonalData;
        data.Iterations = Repetitions;
        return data;
    }
}