using System;
using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
[VolumeComponentMenu("Post-processing/SketchRendering/Smooth Outline")]
public class SmoothOutlineVolumeComponent : OutlineVolumeComponent
{
    public ClampedIntParameter ThicknessRange = new ClampedIntParameter(0, 0, 5);
    public ClampedFloatParameter ThicknessStrength = new ClampedFloatParameter(0, 0, 1);
    public BoolParameter BakeDistortion = new BoolParameter(false);
    public FloatParameter DistortionRate = new FloatParameter(20f);
    public ClampedFloatParameter DistortionStrength = new ClampedFloatParameter(0, 0, 1);
}