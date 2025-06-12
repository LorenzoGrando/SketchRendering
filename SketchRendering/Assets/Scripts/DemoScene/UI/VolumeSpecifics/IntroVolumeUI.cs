using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class IntroVolumeUI : RendererVolumeUIController
{
    [Header("Requirement Components")]
    [SerializeField] private UniversalRendererData URPData;
    [SerializeField] private Transform LightsHolder;
    [SerializeField] private Transform ObjectsHolder;
    
    [Header("Intro Config")]
    [SerializeField] private Toggle EffectToggle;
    [SerializeField] private Toggle AnimateObjectsToggle;
    [SerializeField] private Toggle AnimateLightsToggle;
    
    private SketchRendererFeature textureFeature;
    private ObjectAnimator[] lightAnimators;
    private ObjectAnimator[] objectAutoAnimators;
    private Animator[] objectRegularAnimators;

    public override void ConfigureVolume()
    {
        //Get the renderer feature from the current renderer data
        if (!URPData.TryGetRendererFeature(out textureFeature))
        {
            return;
        }
        
        textureFeature.SetActive(false);
        EffectToggle.SetIsOnWithoutNotify(false);
        AnimateObjectsToggle.SetIsOnWithoutNotify(false);
        AnimateLightsToggle.SetIsOnWithoutNotify(false);

        lightAnimators = LightsHolder.GetComponentsInChildren<ObjectAnimator>();
        for (int i = 0; i < lightAnimators.Length; i++)
        {
            lightAnimators[i].Active = false;
        }
        
        objectAutoAnimators = ObjectsHolder.GetComponentsInChildren<ObjectAnimator>();
        for (int i = 0; i < objectAutoAnimators.Length; i++)
        {
            objectAutoAnimators[i].Active = false;
        }
        
        objectRegularAnimators = ObjectsHolder.GetComponentsInChildren<Animator>();
        for (int i = 0; i < objectRegularAnimators.Length; i++)
        {
            objectRegularAnimators[i].enabled = false;
        }
    }

    public void EffectToggleChanged(bool value)
    {
        textureFeature.SetActive(value);
    }
    
    public void AnimateLightsToggleChanged(bool value)
    {
        for (int i = 0; i < lightAnimators.Length; i++)
        {
            lightAnimators[i].Active = value;
        }
    }

    public void AnimateObjectsToggleChanged(bool value)
    {
        for (int i = 0; i < objectAutoAnimators.Length; i++)
        {
            objectAutoAnimators[i].Active = value;
        }
        
        for (int i = 0; i < objectRegularAnimators.Length; i++)
        {
            objectRegularAnimators[i].enabled = value;
        }
    }
}
