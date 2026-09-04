using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;

public class VRRIgReferences : MonoBehaviour
{
    public static VRRIgReferences Singleton;

    public Transform root;
    public Transform head;
    public Transform leftHand;
    public Transform rightHand;
    public XRBaseController leftController;
    public XRBaseController rightController;

    [Header("Startup Diagnostics")]
    [SerializeField] private bool forceDeviceTrackingOriginInEditor = true;
    [SerializeField] private float editorCameraYOffset = 1.6f;
    [SerializeField] private float minimumExpectedHeadHeight = 0.5f;
    [SerializeField] private float startupDiagnosticDelay = 1.0f;
    [SerializeField] private bool logStartupDiagnostics = true;

    private XROrigin xrOrigin;
    private bool startupDiagnosticsLogged;

    private void Awake()
    {
        Singleton = this;
        xrOrigin = GetComponent<XROrigin>();
        CacheControllers();

#if UNITY_EDITOR
        ConfigureEditorTrackingOrigin();
#endif
    }

    private void Start()
    {
        if (!Application.isPlaying || !logStartupDiagnostics)
        {
            return;
        }

        Invoke(nameof(LogStartupDiagnostics), startupDiagnosticDelay);
    }

#if UNITY_EDITOR
    private void ConfigureEditorTrackingOrigin()
    {
        if (!forceDeviceTrackingOriginInEditor || xrOrigin == null)
        {
            return;
        }

        xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
        xrOrigin.CameraYOffset = Mathf.Max(xrOrigin.CameraYOffset, editorCameraYOffset);
    }
#endif

    private void LogStartupDiagnostics()
    {
        if (startupDiagnosticsLogged)
        {
            return;
        }

        startupDiagnosticsLogged = true;

        if (root == null || head == null || leftHand == null || rightHand == null)
        {
            Debug.LogWarning("[VRRigReferences] Missing XR rig references. Verify root/head/leftHand/rightHand assignments on the XR Origin object.");
            return;
        }

        if (head.position.y > minimumExpectedHeadHeight)
        {
            return;
        }

        Debug.LogWarning(
            $"[VRRigReferences] Head pose is still invalid after startup (headY={head.position.y:0.###}). " +
            "The network avatar will stay in T-pose until XR tracking is live. " +
            "If you are testing in the editor, use either the XR Device Simulator or a live headset runtime, not a dead input path.");
    }

    public bool SendGloveHaptics(CombatGloveType gloveType, float amplitude, float duration)
    {
        XRBaseController controller = gloveType == CombatGloveType.Left ? GetLeftController() : GetRightController();
        return controller != null && controller.SendHapticImpulse(Mathf.Clamp01(amplitude), Mathf.Max(0f, duration));
    }

    public void SendBothControllerHaptics(float amplitude, float duration)
    {
        float clampedAmplitude = Mathf.Clamp01(amplitude);
        float clampedDuration = Mathf.Max(0f, duration);

        XRBaseController cachedLeftController = GetLeftController();
        XRBaseController cachedRightController = GetRightController();

        if (cachedLeftController != null)
        {
            cachedLeftController.SendHapticImpulse(clampedAmplitude, clampedDuration);
        }

        if (cachedRightController != null)
        {
            cachedRightController.SendHapticImpulse(clampedAmplitude, clampedDuration);
        }
    }

    private XRBaseController GetLeftController()
    {
        if (leftController == null)
        {
            CacheControllers();
        }

        return leftController;
    }

    private XRBaseController GetRightController()
    {
        if (rightController == null)
        {
            CacheControllers();
        }

        return rightController;
    }

    private void CacheControllers()
    {
        if (leftController == null && leftHand != null)
        {
            leftController = leftHand.GetComponent<XRBaseController>();
            if (leftController == null)
            {
                leftController = leftHand.GetComponentInParent<XRBaseController>();
            }
        }

        if (rightController == null && rightHand != null)
        {
            rightController = rightHand.GetComponent<XRBaseController>();
            if (rightController == null)
            {
                rightController = rightHand.GetComponentInParent<XRBaseController>();
            }
        }
    }
}
