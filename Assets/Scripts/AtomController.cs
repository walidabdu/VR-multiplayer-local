using UnityEngine;

public class AtomRealSteel_PivotFix_V10 : MonoBehaviour
{
    [Header("--- Main Assignments ---")]
    public Transform xrOrigin;       
    public Transform vrHead;         
    public Transform atomRoot;       
    public Transform bodyPivot;      

    [Header("--- 1. Locomotion ---")]
    public float movementScale = 1.3f;
    public Vector3 atomArenaStartPos;
    public bool recalibrateOnStart = true;

    [Header("--- 2. Boxing Stance ---")]
    [Range(-90, 90)]
    public float bodyStanceAngle = 30f; 

    [Header("--- 3. Ducking & Rolling ---")]
    public float playerStandHeight = 1.7f;
    public float duckDeadzone = 0.15f; 
    public float rollLeanAngle = 35f; 

    [Header("--- Smoothness ---")]
    public float bodyTurnSpeed = 8.0f;
    public float duckSmoothness = 10.0f;

    // Internal Variables to remember your Inspector settings
    private Vector3 initialPivotLocalPos;
    private Quaternion initialPivotLocalRot;
    private float currentLean = 0f;
    private float currentYDrop = 0f;

    void Start()
    {
        if (bodyPivot != null)
        {
            // NEW: We capture where you put the hips in the Inspector
            initialPivotLocalPos = bodyPivot.localPosition;
            initialPivotLocalRot = bodyPivot.localRotation;
        }
        
        if (recalibrateOnStart)
        {
            playerStandHeight = vrHead.position.y;
            atomArenaStartPos = atomRoot.position;
        }
    }

    void LateUpdate()
    {
        if (xrOrigin == null || atomRoot == null || bodyPivot == null) return;

        HandleMovementAndStance();
        HandleDuckingAndRolling();
        HandleHead();
        HandleLimb(leftHand);
        HandleLimb(rightHand);
    }

    void HandleMovementAndStance()
    {
        // ROOT POSITION (Floor)
        Vector3 physicalOffset = vrHead.position - xrOrigin.position;
        physicalOffset.y = 0; 
        physicalOffset *= movementScale;
        atomRoot.position = atomArenaStartPos + physicalOffset;

        // ROOT ROTATION (Facing)
        float lookY = vrHead.eulerAngles.y;
        Quaternion targetFacing = Quaternion.Euler(0, lookY + bodyStanceAngle, 0);
        atomRoot.rotation = Quaternion.Slerp(atomRoot.rotation, targetFacing, Time.deltaTime * bodyTurnSpeed);
    }

    void HandleDuckingAndRolling()
    {
        // 1. Calculate the Drop
        float heightDiff = playerStandHeight - vrHead.position.y;
        if (heightDiff < duckDeadzone) heightDiff = 0;
        float targetYDrop = heightDiff * movementScale;
        currentYDrop = Mathf.Lerp(currentYDrop, targetYDrop, Time.deltaTime * duckSmoothness);

        // 2. Calculate the Lean
        float targetLean = 0f;
        if (heightDiff > duckDeadzone && vrHead.forward.y < -0.2f)
        {
            targetLean = rollLeanAngle;
        }
        currentLean = Mathf.Lerp(currentLean, targetLean, Time.deltaTime * duckSmoothness);

        // 3. RESET TO SAVED INSPECTOR VALUES
        // Instead of (0,0,0), we use the initialPivotLocalPos we saved in Start()
        bodyPivot.localRotation = initialPivotLocalRot;
        bodyPivot.localPosition = initialPivotLocalPos - new Vector3(0, currentYDrop, 0);

        // 4. ROTATE AROUND HIPS
        // We use bodyPivot.right so it always leans "forward" relative to where Atom is facing
        bodyPivot.RotateAround(bodyPivot.position, bodyPivot.right, currentLean);
    }

    // --- Limb Config ---
    [Header("--- Limb Config ---")]
    public LimbSettings head = new LimbSettings { name = "Head" };
    public LimbSettings leftHand = new LimbSettings { name = "Left Hand" };
    public LimbSettings rightHand = new LimbSettings { name = "Right Hand" };

    [System.Serializable]
    public class LimbSettings {
        public string name;
        public Transform vrTarget;
        public Transform atomIKTarget;
        public Vector3 positionOffset;
        public Vector3 rotationOffset;
    }

    void HandleHead() {
        if (head.vrTarget == null) return;
        head.atomIKTarget.rotation = head.vrTarget.rotation * Quaternion.Euler(head.rotationOffset);
    }

    void HandleLimb(LimbSettings limb) {
        if (limb.vrTarget == null) return;
        Vector3 distFromHead = limb.vrTarget.position - vrHead.position;
        distFromHead *= movementScale;
        Vector3 targetPos = head.atomIKTarget.position + distFromHead;
        Quaternion targetRot = limb.vrTarget.rotation * Quaternion.Euler(limb.rotationOffset);
        limb.atomIKTarget.position = targetPos + (targetRot * limb.positionOffset);
        limb.atomIKTarget.rotation = targetRot;
    }
}