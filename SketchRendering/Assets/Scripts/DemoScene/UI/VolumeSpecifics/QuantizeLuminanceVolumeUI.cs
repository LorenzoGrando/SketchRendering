using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class QuantizeLuminanceVolumeUI : RendererVolumeUIController
{
    [Header("Quantize Luminance")] 
    [SerializeField] private Toggle SmoothToggle;
    [SerializeField] private TMP_InputField ScalesXInputField;
    [SerializeField] private TMP_InputField ScalesYInputField;
    [SerializeField] private Slider LuminanceOffsetSlider;
    [SerializeField] private TextMeshProUGUI LuminanceOffsetText;
    
    private QuantizeLuminanceVolumeComponent volume;

    public override void ConfigureVolume()
    {
        Volume activeProfile = FindAnyObjectByType<Volume>();
        if(activeProfile == null)
            return;
        
        if(!activeProfile.profile.TryGet<QuantizeLuminanceVolumeComponent>(out volume))
            return;
        
        SmoothToggle.SetIsOnWithoutNotify(volume.SmoothTransitions.value);
        ScalesXInputField.characterValidation = TMP_InputField.CharacterValidation.Integer;
        ScalesYInputField.characterValidation = TMP_InputField.CharacterValidation.Integer;
        ScalesXInputField.SetTextWithoutNotify(((int)volume.ToneScales.value.x).ToString());
        ScalesYInputField.SetTextWithoutNotify(((int)volume.ToneScales.value.y).ToString());
        LuminanceOffsetSlider.SetValueWithoutNotify(GetLerpedValue(volume.LuminanceOffset));
        LuminanceOffsetText.text = GetFormattedSliderValue(volume.LuminanceOffset);
    }

    public void SmoothTransitionToggleChanged(bool value)
    {
        volume.SmoothTransitions.Override(value);
    }

    public void TonesScalesXValueChanged(string text)
    {
        Vector2 currentScales = volume.ToneScales.value;
        if (int.TryParse(text, out int x))
        {
            currentScales.x = x;
            volume.ToneScales.Override(currentScales);
        }
        else
            ScalesXInputField.SetTextWithoutNotify(((int)volume.ToneScales.value.x).ToString());
    }
    
    public void TonesScalesYValueChanged(string text)
    {
        Vector2 currentScales = volume.ToneScales.value;
        if (int.TryParse(text, out int y))
        {
            currentScales.y = y;
            volume.ToneScales.Override(currentScales);
        }
        else
            ScalesXInputField.SetTextWithoutNotify(((int)volume.ToneScales.value.y).ToString());
    }
    
    public void LuminanceOffsetSliderChanged(float value)
    {
        float interpolatedValue = Mathf.Lerp(volume.LuminanceOffset.min, volume.LuminanceOffset.max, value);
        volume.LuminanceOffset.Override(interpolatedValue);
        LuminanceOffsetText.text = GetFormattedSliderValue(volume.LuminanceOffset);
    }
}
