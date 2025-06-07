using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMouseFollower : MonoBehaviour
{
    [SerializeField] private Vector2 SpeedXY;
    private float yaw;
    private float pitch;
    
    private void Update()
    {
        Vector2 mouseDelta = InputManager.Instance.Mouse.delta.value;
        yaw += mouseDelta.x * SpeedXY.x;
        pitch -= mouseDelta.y * SpeedXY.y;
        
        transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);
    }
}
