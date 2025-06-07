using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private static InputManager instance;
    public static InputManager Instance
    {
        get
        {
            return instance;
        }
        private set => instance = value;
    }
    
    [SerializeField] private PlayerInput Input;
    [SerializeField] private bool LockMouse = true;
    [HideInInspector] public Mouse Mouse => Mouse.current;
    [HideInInspector] public Keyboard Keyboard => Keyboard.current;
    
    #region Actions
    
    private InputAction MovementAction; 
    
    #endregion
    void OnEnable()
    {
        if(Instance == null)
           Instance = this;
        else
            Destroy(gameObject);
        
        Input ??= GetComponent<PlayerInput>();
        MovementAction = Input.currentActionMap.FindAction("Move");
    }
    
    #region KeyboardButtons
    
    public bool SpaceBarPressed => Keyboard != null && Keyboard.spaceKey.wasPressedThisFrame;

    public Vector2 Movement => MovementAction.ReadValue<Vector2>();
    
    #endregion
    
    #region Cursor

    public void UpdateCursorLockState(bool locked)
    {
        LockMouse = locked;
        Cursor.lockState = LockMouse ? CursorLockMode.Locked : CursorLockMode.None;
    }
    
    #endregion
}
