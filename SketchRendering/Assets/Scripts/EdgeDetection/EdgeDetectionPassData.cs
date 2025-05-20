using System;
using UnityEngine;

[Serializable]
public class EdgeDetectionPassData : ISketchRenderPassData
{
    public EdgeDetectionGlobalData.EdgeDetectionMethod Method;
    public EdgeDetectionGlobalData.EdgeDetectionSource Source;
    [Range(0,1)]
    public float OutlineThreshold;
    [Range(0,1)]
    public float OutlineAngleSensitivity;
    [Range(0,1)]
    public float OutlineAngleConstraint;
    
    public EdgeDetectionPassData()
    {
        this.Method = EdgeDetectionGlobalData.EdgeDetectionMethod.SOBEL_3X3;
        this.Source = EdgeDetectionGlobalData.EdgeDetectionSource.DEPTH;
        this.OutlineThreshold = 0.5f;
        this.OutlineAngleSensitivity = 1;
        this.OutlineAngleConstraint = 1;
    }

    public EdgeDetectionPassData(EdgeDetectionGlobalData.EdgeDetectionMethod method, EdgeDetectionGlobalData.EdgeDetectionSource source, float outlineThreshold, float outlineAngleSensitivity, float outlineAngleConstraint)
    {
        this.Method = method;
        this.Source = source;
        this.OutlineThreshold = outlineThreshold;
        this.OutlineAngleSensitivity = outlineAngleSensitivity;
        this.OutlineAngleConstraint = outlineAngleConstraint;
    }
}
