using UnityEngine;

[System.Serializable]
public class VRMap
{
    public Transform vrTarget;   // XR head / hand
    public Transform ikTarget;   // IK target on Atom
    public Vector3 positionOffset;
    public Vector3 rotationOffset;

    public void Map(Transform charlieRoot, Transform atomRoot)
    {
        // 1️⃣ Get motion relative to Charlie
        Vector3 localPos = charlieRoot.InverseTransformPoint(vrTarget.position);

        // 2️⃣ Apply same motion relative to Atom
        ikTarget.position = atomRoot.TransformPoint(localPos + positionOffset);

        // 3️⃣ Rotation (direct mirror)
        ikTarget.rotation = vrTarget.rotation * Quaternion.Euler(rotationOffset);
    }
}

public class IKTargetFollowVRRig : MonoBehaviour
{
    [Header("Roots")]
    public Transform charlieRoot; // XR Origin (Camera Offset)
    public Transform atomRoot;    // Atom hips / root

    [Header("Mappings")]
    public VRMap head;
    public VRMap leftHand;
    public VRMap rightHand;

    void LateUpdate()
    {
        if (!charlieRoot || !atomRoot) return;

        head.Map(charlieRoot, atomRoot);
        leftHand.Map(charlieRoot, atomRoot);
        rightHand.Map(charlieRoot, atomRoot);
    }
}
