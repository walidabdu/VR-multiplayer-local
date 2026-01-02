using UnityEngine;

public class AtomController : MonoBehaviour
{
    [Header("Main Settings")]
    public Transform xrOrigin;      // The VR Player's Root (XR Origin/Rig)
    public Transform atomRoot;      // The Robot's Root
    public Vector3 rootPositionOffset = new Vector3(0, 0, 2); // Atom is 2m in front

    [Header("Scale Calibration")]
    [Tooltip("If Atom is bigger than you, set this > 1. If smaller, < 1.")]
    public float movementScale = 1.0f;

    // We use a custom class to make the Inspector clean and organized
    [System.Serializable]
    public class LimbMap
    {
        public string name; // Just for labeling in Inspector
        public Transform vrTarget;
        public Transform atomTarget;
        
        [Header("Offsets")]
        [Tooltip("Position offset relative to the hand's rotation (e.g. forward/back along the arm)")]
        public Vector3 positionOffset;
        
        [Tooltip("Rotation offset in degrees (X, Y, Z)")]
        public Vector3 rotationOffset;
    }

    [Header("Limb Mappings")]
    public LimbMap head = new LimbMap { name = "Head" };
    public LimbMap leftHand = new LimbMap { name = "Left Hand" };
    public LimbMap rightHand = new LimbMap { name = "Right Hand" };

    void LateUpdate()
    {
        if (xrOrigin == null || atomRoot == null) return;

        MoveAtomRoot();
        
        // Map the limbs using the settings
        MapLimb(head);
        MapLimb(leftHand);
        MapLimb(rightHand);
    }

    void MoveAtomRoot()
    {
        // 1. Position: XR Origin + Offset
        atomRoot.position = xrOrigin.position + rootPositionOffset;

        // 2. Rotation: Copy XR Origin's Y rotation only
        Vector3 playerEuler = xrOrigin.eulerAngles;
        atomRoot.rotation = Quaternion.Euler(0, playerEuler.y, 0);
    }

    void MapLimb(LimbMap limb)
    {
        if (limb.vrTarget == null || limb.atomTarget == null) return;

        // --- STEP 1: Calculate the "Perfect Copy" (Relative Logic) ---

        // Get VR Target's local pos/rot relative to the VR Player (XR Origin)
        Vector3 localPos = xrOrigin.InverseTransformPoint(limb.vrTarget.position);
        Quaternion localRot = Quaternion.Inverse(xrOrigin.rotation) * limb.vrTarget.rotation;

        // Scale the movement (if robot is giant/small)
        localPos *= movementScale;

        // Convert that local space into Atom's World space
        Vector3 targetPos = atomRoot.TransformPoint(localPos);
        Quaternion targetRot = atomRoot.rotation * localRot;


        // --- STEP 2: Apply the Manual Offsets ---

        // Apply Rotation Offset (Rotate 'offset' degrees around the target's new axes)
        Quaternion finalRot = targetRot * Quaternion.Euler(limb.rotationOffset);

        // Apply Position Offset (Move 'offset' distance relative to the NEW rotation)
        // This means if you increase Z, it moves "Forward" relative to where the hand is facing
        Vector3 finalPos = targetPos + (finalRot * limb.positionOffset);


        // --- STEP 3: Apply to Atom ---
        limb.atomTarget.position = finalPos;
        limb.atomTarget.rotation = finalRot;
    }
}