using System;
using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
[VolumeComponentMenu("Post-processing/SketchRendering/QuantizeLuminance")]
public class QuantizeLuminanceVolumeComponent : VolumeComponent
{
    public BoolParameter SmoothTransitions = new BoolParameter(false);
    public Vector2Parameter ToneScales = new Vector2Parameter(Vector2.one);
    public ClampedFloatParameter LuminanceOffset = new ClampedFloatParameter(0f, -1f, 1f);
}
