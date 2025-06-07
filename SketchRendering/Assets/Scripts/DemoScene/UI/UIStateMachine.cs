using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class UIStateMachine : MonoBehaviour
{
    private static UIStateMachine instance;
    public static UIStateMachine Instance
    {
        get 
        {
            return instance;
        }
        private set
        {
            if (instance == null)
            {
                instance = value;
            }
        }
    }
    
    public event Action<bool> OnInputValidationChanged;
    
    [SerializeField] private RendererVolumeUIController[] OrderedUIStates;
    private int currentUIState = 0;
    
    private bool hidden = false;

    public bool IsUIVisible
    {
        get { return !hidden; }
    }

    private bool configured = false;

    public void Start()
    {
        UpdateVisibility(false);
        configured = false;
        StartCoroutine(AwaitVolumeInitialization());
    }

    public void OnEnable()
    {
        if(Instance != null && Instance != this)
            Destroy(gameObject);
        else if(Instance == null)
            Instance = this;
    }

    private void Update()
    {
        if(!configured)
            return;
        
        if (InputManager.Instance.SpaceBarPressed)
        {
            hidden = !hidden;
            InputManager.Instance.UpdateCursorLockState(hidden);
            UpdateVisibility(!hidden);
            SetInputValidation(!hidden);
        }
    }

    public void SetInputValidation(bool enabled)
    {
        OnInputValidationChanged?.Invoke(enabled);
    }

    private IEnumerator AwaitVolumeInitialization()
    {
        while(VolumeManager.instance == null || !VolumeManager.instance.isInitialized)
            yield return null;
        
        ConfigureUIs();
        UpdateVisibility(true);
        hidden = false;
        configured = true;
    }

    private void ChangeUIState(int newUIState)
    {
        OrderedUIStates[currentUIState].DisableUI();
        currentUIState = newUIState;
        OrderedUIStates[currentUIState].EnableUI();
    }

    public void NextState()
    {
        ChangeUIState(GetClampedUIIndex(currentUIState + 1));
    }

    public void PreviousState()
    {
        ChangeUIState(GetClampedUIIndex(currentUIState - 1));
    }

    private int GetClampedUIIndex(int newIndex)
    {
        if (newIndex >= OrderedUIStates.Length)
            newIndex = 0;
        else if(newIndex < 0)
            newIndex = OrderedUIStates.Length - 1;
        
        return newIndex;
    }

    private void UpdateVisibility(bool show)
    {
        if (show)
            OrderedUIStates[currentUIState].EnableUI();
        else
        {
            for (int i = 0; i < OrderedUIStates.Length; i++)
                OrderedUIStates[i].DisableUI();
        }
    }

    private void ConfigureUIs()
    {
        for (int i = 0; i < OrderedUIStates.Length; i++)
        {
            OrderedUIStates[i].ConfigureVolume();
        }
    }
}
