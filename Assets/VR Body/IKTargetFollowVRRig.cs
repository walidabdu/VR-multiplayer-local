using UnityEngine;

[System.Serializable]
public class VRMap
{
    public Transform vrTarget;
    public Transform ikTarget;
    public Vector3 trackingPositionOffset;
    public Vector3 trackingRotationOffset;

    public void Map()
    {
        ikTarget.position = vrTarget.TransformPoint(trackingPositionOffset);
        ikTarget.rotation = vrTarget.rotation * Quaternion.Euler(trackingRotationOffset);
    }
}

public class IKTargetFollowVRRig : MonoBehaviour
{
    [Range(0f, 1f)]
    public float turnSmoothness = 0.1f;

    public VRMap head;
    public VRMap leftHand;
    public VRMap rightHand;

    public Vector3 headBodyPositionOffset;
    public float headBodyYawOffset;

    void LateUpdate()
    {
        transform.position = head.ikTarget.position + headBodyPositionOffset;

        float yaw = head.vrTarget.eulerAngles.y + headBodyYawOffset;
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            Quaternion.Euler(0f, yaw, 0f),
            turnSmoothness);

        head.Map();
        leftHand.Map();
        rightHand.Map();
    }
}
