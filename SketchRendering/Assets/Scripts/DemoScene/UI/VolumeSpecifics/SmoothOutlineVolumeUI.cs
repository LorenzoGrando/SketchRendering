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
    [SerializeField] private GameObject NormalsRequired;
    [SerializeField] private Slider SensitivitySlider;
    [SerializeField] private TextMeshProUGUI SensitivityText;
    [SerializeField] private Slider ConstraintSlider;
    [SerializeField] private TextMeshProUGUI ConstraintText;
    
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
        SensitivitySlider.SetValueWithoutNotify(GetLerpedValue(volume.AngleSensitivity));
        SensitivityText.text = GetFormattedSliderValue(volume.AngleSensitivity);
        ConstraintSlider.SetValueWithoutNotify(GetLerpedValue(volume.AngleConstraint));
        ConstraintText.text = GetFormattedSliderValue(volume.AngleConstraint);
        
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

    private void DisplayNormalsRequired()
    {
        NormalsRequired.SetActive(volume.Source == EdgeDetectionGlobalData.EdgeDetectionSource.DEPTH_NORMALS);
    }
}
