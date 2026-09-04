using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;

/// <summary>
/// Adds limited, Creed-style reach assistance on top of existing Two Bone IK arms.
/// Attach this to the same GameObject as the Animator/RigBuilder.
///
/// The component does not replace or retarget the existing constraints. It changes
/// the two segment lengths immediately before Animation Rigging solves the arms.
/// </summary>
[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class VRArmReachController : MonoBehaviour
{
    [Serializable]
    public sealed class Arm
    {
        [Tooltip("The existing Two Bone IK constraint for this arm.")]
        public TwoBoneIKConstraint twoBoneIK;

        [Tooltip("Optional raw controller transform. If empty, the existing IK target is used.")]
        public Transform controllerTarget;

        [Range(0.85f, 1.30f)]
        [Tooltip("Permanent arm-length calibration. Leave at 1 until doing a T-pose calibration.")]
        public float calibratedLengthMultiplier = 1f;

        [NonSerialized] internal Transform upperArm;
        [NonSerialized] internal Transform forearm;
        [NonSerialized] internal Transform hand;
        [NonSerialized] internal Vector3 originalForearmLocalPosition;
        [NonSerialized] internal Vector3 originalHandLocalPosition;
        [NonSerialized] internal float currentDynamicStretch = 1f;
        [NonSerialized] internal bool initialized;
    }

    [Header("Existing arm rig")]
    public Arm leftArm = new Arm();
    public Arm rightArm = new Arm();

    [Header("Dynamic reach")]
    [Range(1f, 1.30f)]
    [Tooltip("Maximum temporary length multiplier after calibration.")]
    public float maximumDynamicStretch = 1.15f;

    [Min(0f)]
    [Tooltip("How quickly an extended arm returns to its calibrated length.")]
    public float stretchReturnSpeed = 12f;

    [Header("Chest assistance")]
    [Tooltip("Allows a small upper-body shift when a target is still beyond maximum arm reach.")]
    public bool enableChestAssist = true;

    [Tooltip("Chest or upper-chest bone. It is found automatically for a Humanoid Animator.")]
    public Transform chest;

    [Range(0f, 0.10f)]
    [Tooltip("Maximum distance the chest can help the shoulders reach, in metres.")]
    public float maximumChestReach = 0.05f;

    [Range(0f, 1f)]
    [Tooltip("Percentage of unreachable hand distance transferred to the chest.")]
    public float chestReachWeight = 0.35f;

    [Min(0f)]
    public float chestReturnSpeed = 10f;

    [Header("Setup")]
    [Tooltip("Find the left/right Two Bone IK constraints and Humanoid chest automatically.")]
    public bool autoFindReferences = true;

    private Animator animator;
    private Vector3 originalChestLocalPosition;
    private Vector3 currentChestWorldOffset;
    private bool chestInitialized;
    private bool initialized;
    private bool warnedAboutSetup;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        if (!initialized)
            Initialize();
    }

    private void OnDisable()
    {
        RestoreOriginalLengths(leftArm);
        RestoreOriginalLengths(rightArm);

        if (chestInitialized && chest != null)
            chest.localPosition = originalChestLocalPosition;

        currentChestWorldOffset = Vector3.zero;
    }

    /// <summary>
    /// Animation Rigging evaluates after this Animator IK callback, so the existing
    /// Two Bone IK jobs see the corrected segment lengths in the same frame.
    /// Keep IK Pass enabled on the Animator's base layer.
    /// </summary>
    private void OnAnimatorIK(int layerIndex)
    {
        if (layerIndex != 0 || !enabled)
            return;

        if (!initialized)
            Initialize();

        if (!leftArm.initialized && !rightArm.initialized)
        {
            WarnAboutSetupOnce();
            return;
        }

        // First remove last frame's dynamic stretch and apply only calibration.
        PrepareCalibratedArm(leftArm);
        PrepareCalibratedArm(rightArm);

        ApplyChestAssist();

        // Chest assistance changes shoulder positions, so calculate final stretching after it.
        ApplyDynamicStretch(leftArm);
        ApplyDynamicStretch(rightArm);
    }

    [ContextMenu("Auto-Find Arm Rig References")]
    public void AutoFindArmRigReferences()
    {
        animator = GetComponent<Animator>();

        Transform leftUpperArm = animator != null && animator.isHuman
            ? animator.GetBoneTransform(HumanBodyBones.LeftUpperArm)
            : null;
        Transform rightUpperArm = animator != null && animator.isHuman
            ? animator.GetBoneTransform(HumanBodyBones.RightUpperArm)
            : null;

        TwoBoneIKConstraint[] constraints = GetComponentsInChildren<TwoBoneIKConstraint>(true);
        foreach (TwoBoneIKConstraint constraint in constraints)
        {
            if (constraint == null || constraint.data.root == null)
                continue;

            string rootName = constraint.data.root.name.ToLowerInvariant();
            string constraintName = constraint.name.ToLowerInvariant();

            bool looksLeft = constraint.data.root == leftUpperArm ||
                             rootName.Contains("leftarm") || rootName.Contains("left arm") ||
                             constraintName.Contains("left arm");
            bool looksRight = constraint.data.root == rightUpperArm ||
                              rootName.Contains("rightarm") || rootName.Contains("right arm") ||
                              constraintName.Contains("right arm");

            if (looksLeft && leftArm.twoBoneIK == null)
                leftArm.twoBoneIK = constraint;
            else if (looksRight && rightArm.twoBoneIK == null)
                rightArm.twoBoneIK = constraint;
        }

        if (chest == null && animator != null && animator.isHuman)
        {
            chest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
            if (chest == null)
                chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            if (chest == null)
                chest = animator.GetBoneTransform(HumanBodyBones.Spine);
        }

        UseConstraintTargetWhenControllerIsMissing(leftArm);
        UseConstraintTargetWhenControllerIsMissing(rightArm);
    }

    /// <summary>
    /// Run this while the game is playing and the player holds both arms straight
    /// in a comfortable T-pose. The calculated multipliers are printed to Console.
    /// Copy them into the two calibratedLengthMultiplier fields after leaving Play Mode.
    /// </summary>
    [ContextMenu("Calibrate From Current T-Pose")]
    public void CalibrateFromCurrentTPose()
    {
        Initialize();

        float left = CalculateCalibrationMultiplier(leftArm);
        float right = CalculateCalibrationMultiplier(rightArm);

        if (left > 0f)
            leftArm.calibratedLengthMultiplier = left;
        if (right > 0f)
            rightArm.calibratedLengthMultiplier = right;

        Debug.Log(
            $"VR arm calibration complete. Left: {leftArm.calibratedLengthMultiplier:F3}, " +
            $"Right: {rightArm.calibratedLengthMultiplier:F3}. " +
            "Copy these values into the component after leaving Play Mode.",
            this);
    }

    private void Initialize()
    {
        animator = GetComponent<Animator>();

        if (autoFindReferences)
            AutoFindArmRigReferences();

        InitializeArm(leftArm);
        InitializeArm(rightArm);

        if (chest != null && !chestInitialized)
        {
            originalChestLocalPosition = chest.localPosition;
            chestInitialized = true;
        }

        initialized = leftArm.initialized || rightArm.initialized;
    }

    private static void UseConstraintTargetWhenControllerIsMissing(Arm arm)
    {
        if (arm.controllerTarget == null && arm.twoBoneIK != null)
            arm.controllerTarget = arm.twoBoneIK.data.target;
    }

    private static void InitializeArm(Arm arm)
    {
        if (arm == null || arm.initialized || arm.twoBoneIK == null)
            return;

        arm.upperArm = arm.twoBoneIK.data.root;
        arm.forearm = arm.twoBoneIK.data.mid;
        arm.hand = arm.twoBoneIK.data.tip;

        if (arm.upperArm == null || arm.forearm == null || arm.hand == null)
            return;

        if (arm.controllerTarget == null)
            arm.controllerTarget = arm.twoBoneIK.data.target;

        if (arm.controllerTarget == null)
            return;

        arm.originalForearmLocalPosition = arm.forearm.localPosition;
        arm.originalHandLocalPosition = arm.hand.localPosition;
        arm.currentDynamicStretch = 1f;
        arm.initialized = true;
    }

    private static void PrepareCalibratedArm(Arm arm)
    {
        if (arm == null || !arm.initialized)
            return;

        float calibration = Mathf.Clamp(arm.calibratedLengthMultiplier, 0.85f, 1.30f);
        arm.forearm.localPosition = arm.originalForearmLocalPosition * calibration;
        arm.hand.localPosition = arm.originalHandLocalPosition * calibration;
    }

    private void ApplyDynamicStretch(Arm arm)
    {
        if (arm == null || !arm.initialized || arm.controllerTarget == null)
            return;

        float calibratedLength = GetCurrentArmLength(arm);
        if (calibratedLength <= Mathf.Epsilon)
            return;

        float targetDistance = Vector3.Distance(arm.upperArm.position, arm.controllerTarget.position);
        float desiredStretch = Mathf.Clamp(targetDistance / calibratedLength, 1f, maximumDynamicStretch);

        // Stretch outward immediately so fast punches do not leave the glove behind.
        // Smooth only the return, which avoids a visible snap after the punch.
        if (desiredStretch >= arm.currentDynamicStretch)
        {
            arm.currentDynamicStretch = desiredStretch;
        }
        else
        {
            arm.currentDynamicStretch = Mathf.MoveTowards(
                arm.currentDynamicStretch,
                desiredStretch,
                stretchReturnSpeed * Time.deltaTime);
        }

        float totalMultiplier = arm.calibratedLengthMultiplier * arm.currentDynamicStretch;
        arm.forearm.localPosition = arm.originalForearmLocalPosition * totalMultiplier;
        arm.hand.localPosition = arm.originalHandLocalPosition * totalMultiplier;
    }

    private void ApplyChestAssist()
    {
        if (!enableChestAssist || !chestInitialized || chest == null)
            return;

        Vector3 desiredOffset = GetUnreachableOffset(leftArm) + GetUnreachableOffset(rightArm);
        int activeArms = 0;
        if (GetUnreachableDistance(leftArm) > 0f) activeArms++;
        if (GetUnreachableDistance(rightArm) > 0f) activeArms++;

        if (activeArms > 1)
            desiredOffset /= activeArms;

        desiredOffset *= chestReachWeight;
        desiredOffset = Vector3.ClampMagnitude(desiredOffset, maximumChestReach);

        float blend = 1f - Mathf.Exp(-chestReturnSpeed * Time.deltaTime);
        currentChestWorldOffset = Vector3.Lerp(currentChestWorldOffset, desiredOffset, blend);

        Vector3 localOffset = chest.parent != null
            ? chest.parent.InverseTransformVector(currentChestWorldOffset)
            : currentChestWorldOffset;

        chest.localPosition = originalChestLocalPosition + localOffset;
    }

    private Vector3 GetUnreachableOffset(Arm arm)
    {
        float excess = GetUnreachableDistance(arm);
        if (excess <= 0f || arm.controllerTarget == null)
            return Vector3.zero;

        Vector3 direction = arm.controllerTarget.position - arm.upperArm.position;
        return direction.sqrMagnitude > Mathf.Epsilon
            ? direction.normalized * excess
            : Vector3.zero;
    }

    private float GetUnreachableDistance(Arm arm)
    {
        if (arm == null || !arm.initialized || arm.controllerTarget == null)
            return 0f;

        float calibratedLength = GetCurrentArmLength(arm);
        float maximumLength = calibratedLength * maximumDynamicStretch;
        float targetDistance = Vector3.Distance(arm.upperArm.position, arm.controllerTarget.position);
        return Mathf.Max(0f, targetDistance - maximumLength);
    }

    private static float GetCurrentArmLength(Arm arm)
    {
        return Vector3.Distance(arm.upperArm.position, arm.forearm.position) +
               Vector3.Distance(arm.forearm.position, arm.hand.position);
    }

    private float CalculateCalibrationMultiplier(Arm arm)
    {
        if (arm == null || !arm.initialized || arm.controllerTarget == null)
            return -1f;

        arm.forearm.localPosition = arm.originalForearmLocalPosition;
        arm.hand.localPosition = arm.originalHandLocalPosition;

        float originalLength = GetCurrentArmLength(arm);
        if (originalLength <= Mathf.Epsilon)
            return -1f;

        float playerReach = Vector3.Distance(arm.upperArm.position, arm.controllerTarget.position);
        return Mathf.Clamp(playerReach / originalLength, 0.85f, 1.30f);
    }

    private static void RestoreOriginalLengths(Arm arm)
    {
        if (arm == null || !arm.initialized)
            return;

        if (arm.forearm != null)
            arm.forearm.localPosition = arm.originalForearmLocalPosition;
        if (arm.hand != null)
            arm.hand.localPosition = arm.originalHandLocalPosition;

        arm.currentDynamicStretch = 1f;
    }

    private void WarnAboutSetupOnce()
    {
        if (warnedAboutSetup)
            return;

        warnedAboutSetup = true;
        Debug.LogWarning(
            "VRArmReachController could not find the arm Two Bone IK constraints. " +
            "Assign Left Arm and Right Arm constraints in the Inspector, or enable Auto Find References.",
            this);
    }

    private void OnValidate()
    {
        maximumDynamicStretch = Mathf.Max(1f, maximumDynamicStretch);
        stretchReturnSpeed = Mathf.Max(0f, stretchReturnSpeed);
        chestReturnSpeed = Mathf.Max(0f, chestReturnSpeed);
    }
}
