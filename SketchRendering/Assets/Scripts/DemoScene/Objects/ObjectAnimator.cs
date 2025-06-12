using System;
using UnityEngine;

public class ObjectAnimator : MonoBehaviour
{
    public bool Active;
    
    public Vector3 PositionOffset;
    public float PositionOffsetSpeed;
    private bool posUp;

    public Vector3 ScaleOffset;
    public float ScaleOffsetSpeed;
    private bool scaleUp;
    
    public Vector3 RotationAxisSpeeds;
    
    private Vector3 originalPosition;
    private Vector3 originalScale;

    private void Start()
    {
        originalPosition = transform.position;
        originalScale = transform.localScale;
    }

    private void Update()
    {
        if (Active)
        {
            Vector3 targetPos = posUp ? originalPosition + PositionOffset : originalPosition - PositionOffset;
            Vector3 nextPos = Vector3.MoveTowards(transform.position, targetPos, PositionOffsetSpeed * Time.deltaTime);
            float change = Vector3.Distance(targetPos, nextPos);
            transform.position = nextPos;
            if(change <= 0.1f)
                posUp = !posUp;
            
            Vector3 targetScale = scaleUp ? originalScale + ScaleOffset : originalScale - ScaleOffset;
            Vector3 nextScale = Vector3.MoveTowards(transform.localScale, targetScale, ScaleOffsetSpeed * Time.deltaTime);
            float changeScale = Vector3.Distance(targetScale, nextScale);
            transform.localScale = nextScale;
            if(changeScale <= 0.1f)
                scaleUp = !scaleUp;
            
            transform.Rotate(RotationAxisSpeeds * Time.deltaTime);
        }
    }
}