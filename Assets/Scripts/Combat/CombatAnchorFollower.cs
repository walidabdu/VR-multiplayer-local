using UnityEngine;

[DisallowMultipleComponent]
public class CombatAnchorFollower : MonoBehaviour
{
    public Transform anchor;
    public bool followPosition = true;
    public bool followRotation = true;
    public bool useFixedUpdate = true;
    public bool useRigidbodyMove = true;
    public Vector3 localPositionOffset;
    public Vector3 localEulerOffset;

    private Rigidbody followerRigidbody;

    private void Awake()
    {
        followerRigidbody = GetComponent<Rigidbody>();
        ConfigureRigidbody();
    }

    private void FixedUpdate()
    {
        if (useFixedUpdate)
        {
            ApplyFollow(useRigidbodyMove);
        }
    }

    private void LateUpdate()
    {
        if (!useFixedUpdate)
        {
            ApplyFollow(false);
        }
    }

    private void ConfigureRigidbody()
    {
        if (followerRigidbody == null)
        {
            return;
        }

        followerRigidbody.useGravity = false;
        followerRigidbody.isKinematic = true;
        followerRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    private void ApplyFollow(bool driveRigidbody)
    {
        if (anchor == null)
        {
            return;
        }

        Vector3 targetPosition = followPosition
            ? anchor.position + anchor.rotation * localPositionOffset
            : transform.position;

        Quaternion targetRotation = followRotation
            ? anchor.rotation * Quaternion.Euler(localEulerOffset)
            : transform.rotation;

        if (driveRigidbody && followerRigidbody != null)
        {
            followerRigidbody.MovePosition(targetPosition);
            followerRigidbody.MoveRotation(targetRotation);
            return;
        }

        transform.SetPositionAndRotation(targetPosition, targetRotation);
    }
}
