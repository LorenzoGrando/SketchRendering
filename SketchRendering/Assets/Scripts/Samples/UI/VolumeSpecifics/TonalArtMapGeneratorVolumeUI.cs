using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TonalArtMapGeneratorVolumeUI : RendererVolumeUIController
{
    [Header("Hidden TAMGenerator Settings")] 
    [SerializeField] private CanvasGroup interactionBlocker;
    [SerializeField] private UniversalRendererData URPData;
    [SerializeField] private TAMGenerator TAMGenerator;
    [SerializeField] private TonalArtMapAsset TAMAsset;
    [SerializeField] private TAMStrokeAsset SimpleStrokeAsset;
    [SerializeField] private HatchingTAMStrokeAsset HatchingStrokeAsset;
    
    [Header("TAM Stroke Settings")]
    [Header("Tam Configuration")]
    [SerializeField] private Slider NumTonesSlider;
    [SerializeField] private TextMeshProUGUI NumTonesText;
    [SerializeField] private TMP_Dropdown ResolutionDropdown;

    [Space(5)] [Header("Stroke Asset Settings")] 
    [SerializeField] private TMP_Dropdown StrokeSDFDropdown;
    [SerializeField] private TMP_InputField DirectionXInputField;
    [SerializeField] private TMP_InputField DirectionYInputField;
    [SerializeField] private Slider ThicknessSlider;
    [SerializeField] private TextMeshProUGUI ThicknessText;
    [SerializeField] private Slider ThicknessFalloffConstraintSlider;
    [SerializeField] private TextMeshProUGUI ThicknessFalloffConstraintText;
    [SerializeField] private Slider LengthSlider;
    [SerializeField] private TextMeshProUGUI LengthText;
    [SerializeField] private Slider LengthFalloffSlider;
    [SerializeField] private TextMeshProUGUI LengthFalloffText;
    [SerializeField] private Slider PressureSlider;
    [SerializeField] private TextMeshProUGUI PressureText;
    [SerializeField] private Slider PressureFalloffSlider;
    [SerializeField] private TextMeshProUGUI PressureFalloffText;
    [SerializeField] private TMP_Dropdown FalloffFunctionDropdown;
    
    [Space(5)]
    [Header("Hatching Specific")]
    [SerializeField] GameObject HatchingSpecific;
    [SerializeField] private Slider MinHatchingSlider;
    [SerializeField] private TextMeshProUGUI MinHatchingText;


    [Space(10)] 
    [Header("Stroke Application Settings")] 
    [SerializeField] private Slider IterationsPerStrokeSlider;
    [SerializeField] private TextMeshProUGUI IterationsPerStrokeText;
    [SerializeField] private RawImage PreviewImage;
    [Space(5)]
    [Header("Variation Settings")]
    [SerializeField] private Slider DirectionVariationSlider;
    [SerializeField] private TextMeshProUGUI DirectionVariationText;
    [SerializeField] private Slider ThicnkessVariationSlider;
    [SerializeField] private TextMeshProUGUI ThicnkessVariationText;
    [SerializeField] private Slider LengthVariationSlider;
    [SerializeField] private TextMeshProUGUI LengthVariationText;
    [SerializeField] private Slider PressureVariationSlider;
    [SerializeField] private TextMeshProUGUI PressureVariationText;
    
    private QuantizeLuminanceRendererFeature textureFeature;
    private TAMStrokeAsset currentStrokeAsset;
    private TAMStrokeData sharedStrokeData;
    private bool updatedSinceGenerated;

    public override void ConfigureVolume()
    {
        //Get the renderer feature from the current renderer data
        if(!URPData.TryGetRendererFeature(out textureFeature))
            return;
        
        UpdateActiveTAMAssetInFeature();

        currentStrokeAsset = SimpleStrokeAsset;
        
        string[] sdfs = Enum.GetNames(typeof(StrokeSDFType));
        StrokeSDFDropdown.ClearOptions();
        List<TMP_Dropdown.OptionData> sdfData = new List<TMP_Dropdown.OptionData>();
        for (int i = 0; i < sdfs.Length; i++)
        {
            sdfData.Add(new TMP_Dropdown.OptionData(sdfs[i]));
        }
        StrokeSDFDropdown.AddOptions(sdfData);
        StrokeSDFChanged(0);

        sharedStrokeData = TAMGenerator.StrokeDataAsset.PreviewDisplay();
        
        NumTonesSlider.SetValueWithoutNotify(TAMAsset.ExpectedTones);
        NumTonesText.text = TAMAsset.ExpectedTones.ToString();
        
        string[] resolutions = Enum.GetNames(typeof(TextureResolution));
        ResolutionDropdown.ClearOptions();
        List<TMP_Dropdown.OptionData> resolutionData = new List<TMP_Dropdown.OptionData>();
        for (int i = 0; i < resolutions.Length; i++)
        {
            resolutionData.Add(new TMP_Dropdown.OptionData(resolutions[i]));
        }
        ResolutionDropdown.AddOptions(resolutionData);
        
        DirectionXInputField.characterValidation = TMP_InputField.CharacterValidation.Decimal;
        DirectionYInputField.characterValidation = TMP_InputField.CharacterValidation.Decimal;
        currentStrokeAsset.StrokeData.Direction = new Vector4(1, 0, 0, 0);
        DirectionXInputField.SetTextWithoutNotify((GetFormattedSliderValue(sharedStrokeData.Direction.x)).ToString());
        DirectionYInputField.SetTextWithoutNotify((GetFormattedSliderValue(sharedStrokeData.Direction.y)).ToString());
        
        ThicknessSlider.SetValueWithoutNotify(sharedStrokeData.Thickness);
        ThicknessText.text = GetFormattedSliderValue(sharedStrokeData.Thickness);
        ThicknessFalloffConstraintSlider.SetValueWithoutNotify(sharedStrokeData.ThicknessFalloffConstraint);
        ThicknessFalloffConstraintText.text = GetFormattedSliderValue(sharedStrokeData.ThicknessFalloffConstraint);
        LengthSlider.SetValueWithoutNotify(sharedStrokeData.Length);
        LengthText.text = GetFormattedSliderValue(sharedStrokeData.Length);
        LengthFalloffSlider.SetValueWithoutNotify(sharedStrokeData.LengthThicknessFalloff);
        LengthFalloffText.text = GetFormattedSliderValue(sharedStrokeData.LengthThicknessFalloff);
        PressureSlider.SetValueWithoutNotify(sharedStrokeData.Pressure);
        PressureText.text = GetFormattedSliderValue(sharedStrokeData.Pressure);
        PressureFalloffSlider.SetValueWithoutNotify(sharedStrokeData.PressureFalloff);
        PressureFalloffText.text = GetFormattedSliderValue(sharedStrokeData.PressureFalloff);
        
        string[] falloffs = Enum.GetNames(typeof(FalloffFunction));
        FalloffFunctionDropdown.ClearOptions();
        List<TMP_Dropdown.OptionData> falloffsData = new List<TMP_Dropdown.OptionData>();
        for (int i = 0; i < falloffs.Length; i++)
        {
            string trim = falloffs[i].Replace("FALLOFF_", "");
            falloffsData.Add(new TMP_Dropdown.OptionData(trim));
        }
        FalloffFunctionDropdown.AddOptions(falloffsData);
        
        MinHatchingSlider.SetValueWithoutNotify(HatchingStrokeAsset.MinCrossHatchingThreshold);
        MinHatchingText.text = GetFormattedSliderValue(HatchingStrokeAsset.MinCrossHatchingThreshold);
        HatchingStrokeAsset.MaxCrossHatchingThreshold = 1;
        IterationsPerStrokeSlider.SetValueWithoutNotify(TAMGenerator.IterationsPerStroke);
        IterationsPerStrokeText.text = GetFormattedSliderValue(IterationsPerStrokeSlider.value);
        DirectionVariationSlider.SetValueWithoutNotify(currentStrokeAsset.VariationData.DirectionVariationRange);
        DirectionVariationText.text = GetFormattedSliderValue(DirectionVariationSlider.value);
        ThicnkessVariationSlider.SetValueWithoutNotify(currentStrokeAsset.VariationData.ThicknessVariationRange);
        ThicnkessVariationText.text = GetFormattedSliderValue(ThicnkessVariationSlider.value);
        LengthVariationSlider.SetValueWithoutNotify(currentStrokeAsset.VariationData.LengthVariationRange);
        LengthVariationText.text = GetFormattedSliderValue(LengthVariationSlider.value);
        PressureVariationSlider.SetValueWithoutNotify(currentStrokeAsset.VariationData.PressureVariationRange);
        PressureVariationText.text = GetFormattedSliderValue(PressureVariationSlider.value);
        
        UpdateStrokeAssetDatas();
        DisplaySDF();
    }

    public void Update()
    {
        if (TAMGenerator.CanRequest && updatedSinceGenerated)
        {
            PreviewImage.material = TAMGenerator.GetRTMaterial;
        }
    }

    public override void EnableUI()
    {
        base.EnableUI();
        if (currentStrokeAsset == null)
            currentStrokeAsset = TAMGenerator.StrokeDataAsset;
        updatedSinceGenerated = true;
    }

    private void UpdateActiveTAMAssetInFeature()
    {
        TAMGenerator.TAMAsset = TAMAsset;
        textureFeature.LuminanceData.ActiveTonalMap = TAMAsset;
    }

    private void UpdateStrokeAssetDatas()
    {
        SimpleStrokeAsset.StrokeData = sharedStrokeData;
        HatchingStrokeAsset.StrokeData = sharedStrokeData;
        DisplaySDF();
    }
    
    #region TAM & Stroke Data

    public void NumTonesSliderChanged(Single value)
    {
        TAMAsset.ExpectedTones = (int)value;
        NumTonesText.text = value.ToString();
    }

    public void ResolutionValueChanged(int value)
    {
        TAMGenerator.Resolution = (TextureResolution)value;
        DisplaySDF();
    }

    public void StrokeSDFChanged(int value)
    {
        StrokeSDFType type = (StrokeSDFType)value;
        switch (type)
        {
            case StrokeSDFType.SIMPLE:
                currentStrokeAsset = SimpleStrokeAsset;
                TAMGenerator.StrokeDataAsset = SimpleStrokeAsset;
                HatchingSpecific.SetActive(false);
                break;
            case StrokeSDFType.HATCHING:
                currentStrokeAsset = HatchingStrokeAsset;
                TAMGenerator.StrokeDataAsset = HatchingStrokeAsset;
                HatchingSpecific.SetActive(true);
                break;
        }
        DisplaySDF();
    }
    
    public void DirectionXValueChanged(string text)
    {
        Vector2 currentDirection = TAMGenerator.StrokeDataAsset.StrokeData.Direction;
        if (float.TryParse(text, out float x))
        {
            x = Mathf.Clamp(x, -1f, 1f);
            currentDirection.x = x;
            sharedStrokeData.Direction = currentDirection;
        }
        
        DirectionXInputField.SetTextWithoutNotify((currentDirection.x).ToString());
        UpdateStrokeAssetDatas();
    }
    
    public void DirectionYValueChanged(string text)
    {
        Vector2 currentDirection = sharedStrokeData.Direction;
        if (float.TryParse(text, out float y))
        {
            currentDirection.y = y;
            sharedStrokeData.Direction = currentDirection;
        }
        
        DirectionYInputField.SetTextWithoutNotify((currentDirection.y).ToString());
        UpdateStrokeAssetDatas();
    }
    
    public void ThicknessSliderChanged(float value)
    {
        sharedStrokeData.Thickness = value;
        ThicknessText.text = GetFormattedSliderValue(value);
        UpdateStrokeAssetDatas();
    }
    
    public void ThicknessFalloffConstraintSliderChanged(float value)
    {
        sharedStrokeData.ThicknessFalloffConstraint = value;
        ThicknessFalloffConstraintText.text = GetFormattedSliderValue(value);
        UpdateStrokeAssetDatas();
    }
    
    public void LengthSliderChanged(float value)
    {
        sharedStrokeData.Length = value;
        LengthText.text = GetFormattedSliderValue(value);
        UpdateStrokeAssetDatas();
    }
    
    public void LengthFalloffSliderChanged(float value)
    {
        sharedStrokeData.LengthThicknessFalloff = value;
        LengthFalloffText.text = GetFormattedSliderValue(value);
        UpdateStrokeAssetDatas();
    }
    
    public void PressureSliderChanged(float value)
    {
        sharedStrokeData.Pressure = value;
        PressureText.text = GetFormattedSliderValue(value);
        UpdateStrokeAssetDatas();
    }
    
    public void PressureFalloffChanged(float value)
    {
        sharedStrokeData.PressureFalloff = value;
        PressureFalloffText.text = GetFormattedSliderValue(value);
        UpdateStrokeAssetDatas();
    }
    
    public void FalloffFunctionChanged(int value)
    {
        FalloffFunction type = (FalloffFunction)value;
        SimpleStrokeAsset.SelectedFalloffFunction = type;
        HatchingStrokeAsset.SelectedFalloffFunction = type;
        DisplaySDF();
    }

    public void IterationsPerStrokeChanged(Single value)
    {
        TAMGenerator.IterationsPerStroke = (int)value;
        IterationsPerStrokeText.text = value.ToString();
    }

    public void DirectionVariationChanged(float value)
    {
        SimpleStrokeAsset.VariationData.DirectionVariationRange = value;
        HatchingStrokeAsset.VariationData.DirectionVariationRange = value;
        DirectionVariationText.text = GetFormattedSliderValue(value);
    }
    
    public void ThicknessVariationChanged(float value)
    {
        SimpleStrokeAsset.VariationData.ThicknessVariationRange = value;
        HatchingStrokeAsset.VariationData.ThicknessVariationRange = value;
        ThicnkessVariationText.text = GetFormattedSliderValue(value);
    }
    
    public void LengthVariationChanged(float value)
    {
        SimpleStrokeAsset.VariationData.LengthVariationRange = value;
        HatchingStrokeAsset.VariationData.LengthVariationRange = value;
        LengthVariationText.text = GetFormattedSliderValue(value);
    }
    
    public void PressureVariationChanged(float value)
    {
        SimpleStrokeAsset.VariationData.PressureVariationRange = value;
        HatchingStrokeAsset.VariationData.PressureVariationRange = value;
        PressureVariationText.text = GetFormattedSliderValue(value);
    }
    
    public void MinHatchingChanged(float value)
    {
        HatchingStrokeAsset.MinCrossHatchingThreshold = value;
        MinHatchingText.text = GetFormattedSliderValue(value);
    }
    
    public void DisplaySDF()
    {
        if(interactionBlocker.interactable == false)
            return;
        
        TAMGenerator.TAMAsset = TAMAsset;
        TAMGenerator.StrokeDataAsset = currentStrokeAsset;
        TAMGenerator.DisplaySDF();
        updatedSinceGenerated = true;
        StartCoroutine(AwaitUpdate(5));
    }

    public void GenerateTextures()
    {
        TAMGenerator.PackTAMTextures = true;
        TAMGenerator.GenerateTAMToneTextures();
        StartCoroutine(AwaitGeneration());
        updatedSinceGenerated = false;
    }

    private IEnumerator AwaitGeneration()
    {
        SetInteractionAllowed(false);
        
        while (!TAMGenerator.CanRequest)
            yield return null;

        SetInteractionAllowed(true);
    }
    
    private IEnumerator AwaitUpdate(int durationFrames)
    {
        int framesElapsed = 0;
        SetInteractionAllowed(false);

        while (framesElapsed < durationFrames)
        {
            yield return null;
            framesElapsed++;
        }

        SetInteractionAllowed(true);
    }

    private void SetInteractionAllowed(bool enabled)
    {
        interactionBlocker.interactable = enabled;
        UIStateMachine.Instance.SetInputValidation(enabled);
    }
    
    
    #endregion
}
