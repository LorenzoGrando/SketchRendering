using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class SmoothOutlineVolumeUI : RendererVolumeUIController
{
    
    [Header("Smooth Outline")] 
    [SerializeField] private TMP_Dropdown SourceDropdown;
    [SerializeField] private TMP_Dropdown MethodDropdown;
    [SerializeField] private Slider ThresholdSlider;
    [SerializeField] private TextMeshProUGUI ThresholdText;
    [SerializeField] private Slider OffsetSlider;
    [SerializeField] private TextMeshProUGUI OffsetText;
    [SerializeField] private GameObject NormalsRequired;
    [SerializeField] private Slider SensitivitySlider;
    [SerializeField] private TextMeshProUGUI SensitivityText;
    [SerializeField] private Slider ConstraintSlider;
    [SerializeField] private TextMeshProUGUI ConstraintText;
    [SerializeField] private Slider DilationRangeSlider;
    [SerializeField] private TextMeshProUGUI DilationRangeText;
    [SerializeField] private Slider DilationStrengthSlider;
    [SerializeField] private TextMeshProUGUI DilationStrengthText;
    
    private SmoothOutlineVolumeComponent volume;

    public override void ConfigureVolume()
    {
        Volume activeProfile = FindAnyObjectByType<Volume>();
        if(activeProfile == null)
            return;
        
        if(!activeProfile.profile.TryGet<SmoothOutlineVolumeComponent>(out volume))
            return;
        
        string[] sources = Enum.GetNames(typeof(EdgeDetectionGlobalData.EdgeDetectionSource));
        SourceDropdown.ClearOptions();
        List<TMP_Dropdown.OptionData> sourceData = new List<TMP_Dropdown.OptionData>();
        for (int i = 0; i < sources.Length; i++)
        {
            sourceData.Add(new TMP_Dropdown.OptionData(sources[i]));
        }
        SourceDropdown.AddOptions(sourceData);
        SourceDropdown.SetValueWithoutNotify(1);
        
        string[] methods = Enum.GetNames(typeof(EdgeDetectionGlobalData.EdgeDetectionMethod));
        MethodDropdown.ClearOptions();
        List<TMP_Dropdown.OptionData> methodData = new List<TMP_Dropdown.OptionData>();
        for (int i = 0; i < methods.Length; i++)
        {
            methodData.Add(new TMP_Dropdown.OptionData(methods[i]));
        }
        MethodDropdown.AddOptions(methodData);
        
        ThresholdSlider.SetValueWithoutNotify(GetLerpedValue(volume.Threshold));
        ThresholdText.text = GetFormattedSliderValue(volume.Threshold);
        OffsetSlider.SetValueWithoutNotify(GetLerpedValue(volume.Offset));
        OffsetText.text = GetFormattedSliderValue(volume.Offset);
        SensitivitySlider.SetValueWithoutNotify(GetLerpedValue(volume.AngleSensitivity));
        SensitivityText.text = GetFormattedSliderValue(volume.AngleSensitivity);
        ConstraintSlider.SetValueWithoutNotify(GetLerpedValue(volume.AngleConstraint));
        ConstraintText.text = GetFormattedSliderValue(volume.AngleConstraint);
        DilationRangeSlider.SetValueWithoutNotify(GetLerpedValue(volume.ThicknessRange));
        DilationRangeText.text = GetFormattedSliderValue(volume.ThicknessRange);
        DilationStrengthSlider.SetValueWithoutNotify(GetLerpedValue(volume.ThicknessStrength));
        DilationStrengthText.text = GetFormattedSliderValue(volume.ThicknessStrength);
        
        DisplayNormalsRequired();
    }

    public void SourceDropdownChanged(int index)
    {
        EdgeDetectionGlobalData.EdgeDetectionSource source = (EdgeDetectionGlobalData.EdgeDetectionSource)index;
        volume.Source.Override(source);
        
        DisplayNormalsRequired();
    }
    
    public void MethodDropdownChanged(int index)
    {
        EdgeDetectionGlobalData.EdgeDetectionMethod method = (EdgeDetectionGlobalData.EdgeDetectionMethod)index;
        volume.Method.Override(method);
    }

    public void ThresholdSliderChanged(float value)
    {
        float interpolatedValue = Mathf.Lerp(volume.Threshold.min, volume.Threshold.max, value);
        volume.Threshold.Override(interpolatedValue);
        ThresholdText.text = GetFormattedSliderValue(value);
    }

    public void OffsetSliderChanged(float value)
    {
        volume.Offset.Override((int)value);
        OffsetText.text = GetFormattedSliderValue((int)value);
    }

    public void SensitivitySliderChanged(float value)
    {
        float interpolatedValue = Mathf.Lerp(volume.AngleSensitivity.min, volume.AngleSensitivity.max, value);
        volume.AngleSensitivity.Override(interpolatedValue);
        SensitivityText.text = GetFormattedSliderValue(volume.AngleSensitivity);
    }
    
    public void ConstraintSliderChanged(float value)
    {
        float interpolatedValue = Mathf.Lerp(volume.AngleConstraint.min, volume.AngleConstraint.max, value);
        volume.AngleConstraint.Override(interpolatedValue);
        ConstraintText.text = GetFormattedSliderValue(volume.AngleConstraint);
    }

    public void DilationRangeSliderChanged(float value)
    {
        volume.ThicknessRange.Override((int)value);
        DilationRangeText.text = GetFormattedSliderValue((int)value);
    }

    public void DilationStrengthSliderChanged(float value)
    {
        volume.ThicknessStrength.Override(value);
        DilationStrengthText.text = GetFormattedSliderValue(value);
    }

    private void DisplayNormalsRequired()
    {
        NormalsRequired.SetActive(volume.Source == EdgeDetectionGlobalData.EdgeDetectionSource.DEPTH_NORMALS);
    }
}
