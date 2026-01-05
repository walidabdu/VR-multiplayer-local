using UnityEngine;

public class AtomUltimateController : MonoBehaviour
{
    [Header("--- Main Setup ---")]
    public Transform xrOrigin;       // Your XR Origin/Rig
    public Transform atomRoot;       // The Robot's Root Parent
    public Transform vrHead;         // Your Main Camera

    [Header("--- Sizing & Lag ---")]
    [Tooltip("Size Multiplier. Increase (1.2 - 1.5) if Atom is bigger/wider than you.")]
    public float movementScale = 1.0f; 

    [Tooltip("How fast the Body follows the Head. Lower = More Lag/Weight. Higher = Snappy.")]
    public float bodyTurnSpeed = 3.0f; 

    [Tooltip("Global offset to place Atom in front of you.")]
    public Vector3 rootSpawnOffset = new Vector3(0, 0, 2);


    // We use this Class to make the Inspector look clean and give you Offset fields back
    [System.Serializable]
    public class LimbSettings
    {
        public string name; 
        public Transform vrTarget;
        public Transform atomIKTarget;

        [Header("Adjustments")]
        [Tooltip("Local position offset (X, Y, Z). Useful to slide hand forward/back.")]
        public Vector3 positionOffset;
        
        [Tooltip("Local rotation fix (e.g., 90, 0, 0) if wrist is twisted.")]
        public Vector3 rotationOffset;
    }

    [Header("--- Limb Configuration ---")]
    public LimbSettings head = new LimbSettings { name = "Head" };
    public LimbSettings leftHand = new LimbSettings { name = "Left Hand" };
    public LimbSettings rightHand = new LimbSettings { name = "Right Hand" };


    void LateUpdate()
    {
        if (xrOrigin == null || atomRoot == null || vrHead == null) return;

        // 1. Handle Body Movement (With Lag)
        MoveAtomBody();

        // 2. Handle Head (Instant Rotation)
        UpdateHead();

        // 3. Handle Hands (Relative to Head + Scaling)
        UpdateHand(leftHand);
        UpdateHand(rightHand);
    }

    void MoveAtomBody()
    {
        // POSITION: Follow player exactly + Offset
        Vector3 targetPos = xrOrigin.position + rootSpawnOffset;
        atomRoot.position = targetPos;

        // ROTATION (The Mirror Effect):
        // We want the body to rotate towards where the Head is looking, but SLOWLY.
        Quaternion targetBodyRot = Quaternion.Euler(0, vrHead.eulerAngles.y, 0);
        
        // Slerp creates the smooth "Lag" effect
        atomRoot.rotation = Quaternion.Slerp(atomRoot.rotation, targetBodyRot, Time.deltaTime * bodyTurnSpeed);
    }

    void UpdateHead()
    {
        if (head.vrTarget == null || head.atomIKTarget == null) return;

        // The Head rotates INSTANTLY to match the VR headset (plus manual offsets)
        // Since the Body is lagging, this will cause the neck to twist naturally!
        Quaternion targetRot = head.vrTarget.rotation * Quaternion.Euler(head.rotationOffset);
        
        head.atomIKTarget.rotation = targetRot;
        
        // Optional: If you want to adjust head height manually
        // head.atomIKTarget.localPosition += head.positionOffset;
    }

    void UpdateHand(LimbSettings limb)
    {
        if (limb.vrTarget == null || limb.atomIKTarget == null) return;

        // --- STEP A: Calculate Position Relative to HEAD (Fixes crossing arms) ---
        
        // 1. Where is the hand relative to your face?
        Vector3 distFromHead = limb.vrTarget.position - vrHead.position;
        
        // 2. Scale that distance (Fixes T-Rex arms)
        distFromHead *= movementScale;

        // 3. Apply that relative distance to the Robot's Head
        // Note: We use the Robot Head's current position as the anchor
        Vector3 targetWorldPos = head.atomIKTarget.position + distFromHead;


        // --- STEP B: Calculate Rotation ---
        
        // Copy the absolute rotation of the controller
        Quaternion targetWorldRot = limb.vrTarget.rotation;


        // --- STEP C: Apply Manual Offsets (The fields you wanted back) ---

        // Apply Rotation Offset
        Quaternion finalRot = targetWorldRot * Quaternion.Euler(limb.rotationOffset);

        // Apply Position Offset (Local to the hand's facing direction)
        // e.g. If you set Z = 0.1, it moves the hand 10cm "forward" along the fingers
        Vector3 finalPos = targetWorldPos + (finalRot * limb.positionOffset);


        // --- STEP D: Set Values ---
        limb.atomIKTarget.position = finalPos;
        limb.atomIKTarget.rotation = finalRot;
    }
}