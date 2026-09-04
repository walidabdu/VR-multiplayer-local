using UnityEngine;

public class VRFootIK : MonoBehaviour
{
    private Animator animator;

    public Vector3 footOffset;

    [Range(0f, 1f)]
    public float rightFootPosWeight = 1f;

    [Range(0f, 1f)]
    public float rightFootRotWeight = 1f;

    [Range(0f, 1f)]
    public float leftFootPosWeight = 1f;

    [Range(0f, 1f)]
    public float leftFootRotWeight = 1f;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void OnAnimatorIK(int layerIndex)
    {
        // Right foot
        Vector3 rightFootPos = animator.GetIKPosition(AvatarIKGoal.RightFoot);

        if (Physics.Raycast(rightFootPos + Vector3.up, Vector3.down, out RaycastHit hit))
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, rightFootPosWeight);
            animator.SetIKPosition(AvatarIKGoal.RightFoot, hit.point + footOffset);

            Quaternion rightFootRotation = Quaternion.LookRotation(
                Vector3.ProjectOnPlane(transform.forward, hit.normal),
                hit.normal
            );

            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, rightFootRotWeight);
            animator.SetIKRotation(AvatarIKGoal.RightFoot, rightFootRotation);
        }
        else
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0f);
        }

        // Left foot
        Vector3 leftFootPos = animator.GetIKPosition(AvatarIKGoal.LeftFoot);

        if (Physics.Raycast(leftFootPos + Vector3.up, Vector3.down, out hit))
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, leftFootPosWeight);
            animator.SetIKPosition(AvatarIKGoal.LeftFoot, hit.point + footOffset);

            Quaternion leftFootRotation = Quaternion.LookRotation(
                Vector3.ProjectOnPlane(transform.forward, hit.normal),
                hit.normal
            );

            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, leftFootRotWeight);
            animator.SetIKRotation(AvatarIKGoal.LeftFoot, leftFootRotation);
        }
        else
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0f);
        }
    }
}