using UnityEngine;
using Unity.Netcode;

public class AtomNetworkAnimator_V13 : NetworkBehaviour
{
    [Header("--- 1. Network References (Drag from Prefab) ---")]
    public Transform networkHead;
    public Transform networkLeftHand;
    public Transform networkRightHand;
    public Transform networkBody; // This MUST be the XR Origin/Floor Sync

    [Header("--- 2. Atom Internal Parts ---")]
    public Transform bodyPivot;   // The Hips/Waist
    public Transform atomRigRoot; // The parent of the bones (Optional, helps with scale)

    [Header("--- 3. Movement Fixes ---")]
    public float movementScale = 1.3f;
    [Tooltip("Where Atom stands when you are in the center of your room.")]
    public Vector3 atomArenaStartPos = Vector3.zero; 
    
    [Header("--- 4. Rotation Fixes ---")]
    [Tooltip("If hands point up, try (90, 0, 0) or (-90, 0, 0)")]
    public Vector3 globalHandRotationOffset = Vector3.zero;
    [Range(-90, 90)] public float bodyStanceAngle = 30f;
    public float bodyTurnSpeed = 8.0f;
    public float ForwardHeadTurnAngle = 0.4f;
    public float HeightDifferenceToStartTurning = 0.2f;

    [Header("--- 5. Ducking & Rolling ---")]
    public float rollLeanAngle = 35f; 
    public float duckSmoothness = 10.0f;

    [Header("--- 6. Combat ---")]
    public NetworkPlayerCombatState combatState;
    public float minimumValidTrackingHeight = 0.5f;
    public float hitReactionRecoverSpeed = 10f;
    public float maxReactionTilt = 28f;
    public float maxReactionTwist = 18f;
    public float maxReactionOffset = 0.12f;
    public float headReactionMultiplier = 1.2f;
    public float chestReactionMultiplier = 1f;
    public float bellyReactionMultiplier = 0.85f;

    // --- IK Settings ---
    [System.Serializable]
    public class LimbIK {
        public Transform atomIKTarget;
        public Vector3 positionOffset;
        public Vector3 rotationOffset;
    }
    public LimbIK headIK;
    public LimbIK leftHandIK;
    public LimbIK rightHandIK;

    // Internal State
    private Vector3 initialPivotLocalPos;
    private Quaternion initialPivotLocalRot;
    private float playerStandHeight = 1.6f; // Default safety height
    private float currentLean = 0f;
    private float currentYDrop = 0f;
    private Vector3 runtimeVisualOffset;
    private Vector3 runtimeVisualLocalOffset;
    private bool runtimeVisualOffsetInitialized;
    private bool hasCalibratedTrackingHeight;
    private Vector3 hitReactionEuler;
    private Vector3 hitReactionPosition;

    public Transform HeadAnchorTransform => headIK.atomIKTarget != null ? headIK.atomIKTarget : networkHead;
    public Transform LeftGloveAnchorTransform => leftHandIK.atomIKTarget != null ? leftHandIK.atomIKTarget : networkLeftHand;
    public Transform RightGloveAnchorTransform => rightHandIK.atomIKTarget != null ? rightHandIK.atomIKTarget : networkRightHand;
    public Transform BodyPivotTransform => bodyPivot;

    public override void OnNetworkSpawn()
    {
        if (combatState == null)
        {
            combatState = GetComponentInParent<NetworkPlayerCombatState>();
        }

        // 1. Capture Hip "Home" Position
        if (bodyPivot != null)
        {
            initialPivotLocalPos = bodyPivot.localPosition;
            initialPivotLocalRot = bodyPivot.localRotation;
        }

        if (!runtimeVisualOffsetInitialized)
        {
            runtimeVisualOffset = atomArenaStartPos;
            runtimeVisualLocalOffset = transform.localPosition;

            if (runtimeVisualOffset == Vector3.zero && networkBody != null)
            {
                runtimeVisualOffset = transform.position - networkBody.position;
            }

            runtimeVisualOffsetInitialized = true;
        }

        // 2. Calibrate Height (Only if we have a valid head height)
        if (IsTrackingPoseValid())
        {
            playerStandHeight = networkHead.position.y;
            hasCalibratedTrackingHeight = true;
        }
    }

    void LateUpdate()
    {
        // Safety Check
        if (networkHead == null || networkBody == null || bodyPivot == null) return;

        UpdateHitReactionDecay();

        if (!IsTrackingPoseValid())
        {
            return;
        }

        if (!hasCalibratedTrackingHeight)
        {
            playerStandHeight = networkHead.position.y;
            hasCalibratedTrackingHeight = true;
        }

        HandleRoomScaleMovement();
        HandleDuckingAndRolling();
        
        // IK Handling
        HandleHead(networkHead, headIK);
        HandleHand(networkLeftHand, leftHandIK);
        HandleHand(networkRightHand, rightHandIK);
    }

    void HandleRoomScaleMovement()
    {
        // --- THE LOGIC FIX ---
        // We calculate the offset of the Head relative to the Floor (NetworkBody).
        // Joystick Slide -> Head & Body move together -> Offset stays same -> Atom DOES NOT move.
        // Room Walk -> Head moves away from Body -> Offset changes -> Atom WALKS.
        
        Vector3 roomOffset = networkHead.position - networkBody.position;
        roomOffset.y = 0; // Ignore height
        roomOffset *= movementScale;

        Vector3 resolvedCombatOffset = combatState != null ? combatState.CurrentCombatOffset : Vector3.zero;
        Vector3 totalWorldOffset = roomOffset + resolvedCombatOffset;

        if (transform.parent == networkBody)
        {
            // In the live prefab the Atom puppet is parented under the tracked avatar root.
            // Drive it in local space so the parent body/root placement remains the world anchor.
            transform.localPosition = runtimeVisualLocalOffset + Quaternion.Inverse(networkBody.rotation) * totalWorldOffset;
        }
        else
        {
            // Fallback for detached or re-parented visual rigs.
            transform.position = networkBody.position + runtimeVisualOffset + totalWorldOffset;
        }

        // Apply Rotation (Facing)
        float lookY = networkHead.eulerAngles.y;
        Quaternion targetFacing = Quaternion.Euler(0, lookY + bodyStanceAngle, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetFacing, Time.deltaTime * bodyTurnSpeed);
    }

    void HandleDuckingAndRolling()
    {
        // 1. Calculate Drop
        float heightDiff = playerStandHeight - networkHead.position.y;
        if (heightDiff < 0.1f) heightDiff = 0; // Deadzone

        float targetYDrop = heightDiff * movementScale;
        currentYDrop = Mathf.Lerp(currentYDrop, targetYDrop, Time.deltaTime * duckSmoothness);

        // 2. Calculate Lean
        // Only lean if we are ducking AND looking down
        float targetLean = 0f;
        if (heightDiff > HeightDifferenceToStartTurning && networkHead.forward.y < -ForwardHeadTurnAngle)
        {
            targetLean = rollLeanAngle;
        }
        currentLean = Mathf.Lerp(currentLean, targetLean, Time.deltaTime * duckSmoothness);

        // 3. Apply to Hips (The Reset & Rotate Trick)
        bodyPivot.localRotation = initialPivotLocalRot; // Reset rotation
        bodyPivot.localPosition = initialPivotLocalPos - new Vector3(0, currentYDrop, 0); // Apply Drop
        
        // Rotate around Hips (Gizmo Style)
        bodyPivot.RotateAround(bodyPivot.position, bodyPivot.right, currentLean);
        ApplyHitReaction();
    }

    bool IsTrackingPoseValid()
    {
        return networkHead != null && networkHead.position.y > minimumValidTrackingHeight;
    }

    public void TriggerHitReaction(CombatHurtboxType hurtboxType, Vector3 worldDirection, float strength)
    {
        if (bodyPivot == null)
        {
            return;
        }

        Vector3 recoilLocalDirection = transform.InverseTransformDirection(-worldDirection.normalized);
        float hurtboxMultiplier = GetHurtboxReactionMultiplier(hurtboxType);
        float resolvedStrength = Mathf.Clamp01(strength) * hurtboxMultiplier;

        float frontalImpact = Mathf.Max(0f, recoilLocalDirection.z);
        float lateralImpact = Mathf.Clamp(recoilLocalDirection.x, -1f, 1f);

        Vector3 eulerKick = new Vector3(
            frontalImpact * maxReactionTilt,
            lateralImpact * maxReactionTwist * 0.45f,
            -lateralImpact * maxReactionTilt * 0.65f) * resolvedStrength;

        Vector3 positionKick = new Vector3(
            lateralImpact * maxReactionOffset * 0.35f,
            hurtboxType == CombatHurtboxType.Head ? maxReactionOffset * 0.25f : hurtboxType == CombatHurtboxType.Belly ? -maxReactionOffset * 0.18f : 0f,
            -frontalImpact * maxReactionOffset) * resolvedStrength;

        hitReactionEuler = Vector3.ClampMagnitude(hitReactionEuler + eulerKick, maxReactionTilt * 1.35f);
        hitReactionPosition = Vector3.ClampMagnitude(hitReactionPosition + positionKick, maxReactionOffset * 1.5f);
    }

    void HandleHead(Transform netTarget, LimbIK ik)
    {
        if (ik.atomIKTarget == null) return;
        // Head only needs rotation
        ik.atomIKTarget.rotation = netTarget.rotation * Quaternion.Euler(ik.rotationOffset);
    }

    void HandleHand(Transform netTarget, LimbIK ik)
    {
        if (ik.atomIKTarget == null) return;
        // 1. Position Logic (Same as V13)
        Vector3 distFromHead = (netTarget.position - networkHead.position) * movementScale;
        Vector3 targetPos = headIK.atomIKTarget.position + distFromHead;

        // 2. Rotation Logic (Fix for gimbal lock)
        // Apply rotations per-axis using AngleAxis so inspector Euler edits never lock up.
        Quaternion baseRot = netTarget.rotation;

        Quaternion gX = Quaternion.AngleAxis(globalHandRotationOffset.x, Vector3.right);
        Quaternion gY = Quaternion.AngleAxis(globalHandRotationOffset.y, Vector3.up);
        Quaternion gZ = Quaternion.AngleAxis(globalHandRotationOffset.z, Vector3.forward);

        Quaternion xRot = Quaternion.AngleAxis(ik.rotationOffset.x, Vector3.right);
        Quaternion yRot = Quaternion.AngleAxis(ik.rotationOffset.y, Vector3.up);
        Quaternion zRot = Quaternion.AngleAxis(ik.rotationOffset.z, Vector3.forward);

        // Order: base * global adjustments * per-IK adjustments
        ik.atomIKTarget.rotation = baseRot * gX * gY * gZ * xRot * yRot * zRot;
        ik.atomIKTarget.position = targetPos + (ik.atomIKTarget.rotation * ik.positionOffset);
    }

    private void UpdateHitReactionDecay()
    {
        hitReactionEuler = Vector3.Lerp(hitReactionEuler, Vector3.zero, Time.deltaTime * hitReactionRecoverSpeed);
        hitReactionPosition = Vector3.Lerp(hitReactionPosition, Vector3.zero, Time.deltaTime * hitReactionRecoverSpeed);
    }

    private void ApplyHitReaction()
    {
        if (hitReactionPosition.sqrMagnitude > 0.000001f)
        {
            bodyPivot.localPosition += hitReactionPosition;
        }

        if (hitReactionEuler.sqrMagnitude > 0.000001f)
        {
            bodyPivot.localRotation = bodyPivot.localRotation * Quaternion.Euler(hitReactionEuler);
        }
    }

    private float GetHurtboxReactionMultiplier(CombatHurtboxType hurtboxType)
    {
        switch (hurtboxType)
        {
            case CombatHurtboxType.Head:
                return headReactionMultiplier;
            case CombatHurtboxType.Belly:
                return bellyReactionMultiplier;
            default:
                return chestReactionMultiplier;
        }
    }
}
