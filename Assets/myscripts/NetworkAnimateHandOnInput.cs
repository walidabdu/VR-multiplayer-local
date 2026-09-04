using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkAnimateHandOnInput : NetworkBehaviour
{
    private static readonly int TriggerHash = Animator.StringToHash("Trigger");
    private static readonly int GripHash = Animator.StringToHash("Grip");

    public InputActionProperty pinchAnimationAction;
    public InputActionProperty gripAnimationAction;
    public Animator handAnimator;
    [Range(0.01f, 0.25f)] public float animationStep = 0.05f;

    private float lastTriggerValue = float.NaN;
    private float lastGripValue = float.NaN;

    void Update()
    {
        if (!IsOwner || handAnimator == null)
        {
            return;
        }

        UpdateAnimatorFloat(pinchAnimationAction, TriggerHash, ref lastTriggerValue);
        UpdateAnimatorFloat(gripAnimationAction, GripHash, ref lastGripValue);
    }

    private void UpdateAnimatorFloat(InputActionProperty actionProperty, int parameterHash, ref float lastValue)
    {
        var action = actionProperty.action;
        if (action == null)
        {
            return;
        }

        float value = Mathf.Clamp01(action.ReadValue<float>());
        float quantizedValue = animationStep > 0f
            ? Mathf.Round(value / animationStep) * animationStep
            : value;

        if (Mathf.Approximately(lastValue, quantizedValue))
        {
            return;
        }

        handAnimator.SetFloat(parameterHash, quantizedValue);
        lastValue = quantizedValue;
    }
}
