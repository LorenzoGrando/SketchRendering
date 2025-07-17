using System;
using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
[VolumeComponentMenu("Post-processing/SketchRendering/Sketch Outline")]
public class SketchOutlineVolumeComponent : OutlineVolumeComponent
{
    public EnumParameter<ComputeData.KernelSize2D> StrokeArea = new (ComputeData.KernelSize2D.SIZE_8X8);
    public ClampedIntParameter StrokeScale = new (1, 1, 4);
    public BoolParameter DoDownscale = new (true);
    public ClampedIntParameter DownscaleFactor = new (2, 2, 4);
    public ClampedFloatParameter MinThresholdForStroke = new (0.1f, 0, 1);
    public ClampedFloatParameter FrameSmoothingFactor = new (0, 0, 1);
}