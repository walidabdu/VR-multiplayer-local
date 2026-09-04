using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(IKTargetFollowVRRig))]
public class VRAnimatorController2 : MonoBehaviour
{
    [Tooltip("Minimum headset movement speed before the walk animation begins.")]
    public float speedThreshold = 0.1f;

    [Range(0f, 1f)]
    [Tooltip("How quickly the movement-direction animation parameters change.")]
    public float smoothing = 1f;

    private Animator animator;
    private Vector3 previousPos;
    private IKTargetFollowVRRig vrRig;

    private void Start()
    {
        animator = GetComponent<Animator>();
        vrRig = GetComponent<IKTargetFollowVRRig>();

        previousPos = vrRig.head.vrTarget.position;
    }

    private void Update()
    {
        // Calculate headset world movement speed.
        Vector3 headsetSpeed =
            (vrRig.head.vrTarget.position - previousPos) / Time.deltaTime;

        // Ignore vertical headset movement, such as crouching.
        headsetSpeed.y = 0f;

        // Convert movement direction into the avatar's local space.
        Vector3 headsetLocalSpeed =
            transform.InverseTransformDirection(headsetSpeed);

        previousPos = vrRig.head.vrTarget.position;

        // Get the current values so direction changes can be smoothed.
        float previousDirectionX = animator.GetFloat("DirectionX");
        float previousDirectionY = animator.GetFloat("DirectionY");

        // Send movement values to the Animator Controller.
        animator.SetBool("isMoving", headsetLocalSpeed.magnitude > speedThreshold);
        animator.SetFloat(
            "DirectionX",
            Mathf.Lerp(previousDirectionX, Mathf.Clamp(headsetLocalSpeed.x, -1f, 1f), smoothing));
        animator.SetFloat(
            "DirectionY",
            Mathf.Lerp(previousDirectionY, Mathf.Clamp(headsetLocalSpeed.z, -1f, 1f), smoothing));
    }
}
