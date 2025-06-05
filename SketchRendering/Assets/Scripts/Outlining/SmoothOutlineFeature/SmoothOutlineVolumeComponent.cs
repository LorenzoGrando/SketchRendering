using System;
using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
[VolumeComponentMenu("Post-processing/SketchRendering/Smooth Outline")]
public class SmoothOutlineVolumeComponent : VolumeComponent
{
    public EnumParameter<EdgeDetectionGlobalData.EdgeDetectionMethod> Method =
        new EnumParameter<EdgeDetectionGlobalData.EdgeDetectionMethod>(EdgeDetectionGlobalData.EdgeDetectionMethod.SOBEL_3X3);
    public EnumParameter<EdgeDetectionGlobalData.EdgeDetectionSource> Source =
        new EnumParameter<EdgeDetectionGlobalData.EdgeDetectionSource>(EdgeDetectionGlobalData.EdgeDetectionSource.DEPTH);
    public ClampedFloatParameter Threshold = new ClampedFloatParameter(0, 0, 1);
    public ClampedFloatParameter AngleSensitivity = new ClampedFloatParameter(0, 0, 1);
    public ClampedFloatParameter AngleConstraint = new ClampedFloatParameter(0, 0, 1);
}