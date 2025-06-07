using System.Collections;
using UnityEngine;

public class CharacterMovementController : MonoBehaviour
{
    public CameraMouseFollower cameraController;
    public float Speed;
    private bool inputAllowed = false;
    
    private readonly Vector3 movementMask = new Vector3(1, 0, 1);
    
    public void OnEnable()
    {
        StartCoroutine(AwaitStart());
        cameraController.enabled = false;
    }

    private IEnumerator AwaitStart()
    {
        while (UIStateMachine.Instance == null)
            yield return null;
        
        UIStateMachine.Instance.OnInputValidationChanged += OnInputValidationChanged;
    }
    
    public void Update()
    {
        if (inputAllowed)
        {
            Vector2 movement = InputManager.Instance.Movement.normalized * Speed;   
            Debug.Log("Movement" + movement);
            transform.position += Vector3.Scale((transform.forward * movement.y + transform.right * movement.x), movementMask);
        }
    }

    private void OnInputValidationChanged(bool enabled)
    {
        inputAllowed = !enabled && !UIStateMachine.Instance.IsUIVisible;
        cameraController.enabled = inputAllowed;
    }
}
