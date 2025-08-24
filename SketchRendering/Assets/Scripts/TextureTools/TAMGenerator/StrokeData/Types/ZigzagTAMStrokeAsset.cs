using System;
using UnityEngine;
using Random = UnityEngine.Random;

[CreateAssetMenu(fileName = "ZigzagStrokeAsset",
    menuName = "SketchRendering/Scriptable Objects/TAMStrokeAssets/ZigzagStrokeAsset")]
public class ZigzagTAMStrokeAsset : TAMStrokeAsset
{
    public override StrokeSDFType PatternType => StrokeSDFType.ZIGZAG;

    [Space(5)] [Header("Zigzag Specific")] 
    [Range(-1, 1f)]
    public float SubStrokeDirectionOffset;
    public float SubStrokeLengthMultiplier;
    public bool OnlyMultiplyZigStroke;
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
            x = SubStrokeDirectionOffset,
            y = SubStrokeLengthMultiplier,
            z = 0f,
            w = OnlyMultiplyZigStroke ? 1f : SubStrokeLengthMultiplier,
        };
        data.AdditionalPackedData = additonalData;
        data.Iterations = Repetitions;
        return data;
    }
}