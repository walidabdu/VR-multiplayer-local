using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace UnityEngine.XR.Content.Interaction
{
    [RequireComponent(typeof(XRGrabInteractable))]
    public class NetworkVRGun : NetworkBehaviour
    {
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform startPoint;
        [SerializeField] private float launchSpeed = 10f;

        private XRGrabInteractable grabInteractable;

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            grabInteractable.activated.AddListener(OnTriggerPulled);
        }

        private void OnDestroy()
        {
            grabInteractable.activated.RemoveListener(OnTriggerPulled);
        }

        private void OnTriggerPulled(ActivateEventArgs args)
        {
            if (!IsOwner) return; // Only the person holding the gun can shoot

            // Tell server to spawn projectile
            SpawnProjectileServerRpc(startPoint.position, startPoint.rotation);
        }

        [ServerRpc]
private void SpawnProjectileServerRpc(Vector3 pos, Quaternion rot, ServerRpcParams rpcParams = default)
{
    GameObject proj = Instantiate(projectilePrefab, pos, rot);
    var netObj = proj.GetComponent<NetworkObject>();
    var rb = proj.GetComponent<Rigidbody>();

    if (rb != null)
    {
        Vector3 velocity = rot * Vector3.forward * launchSpeed;
        proj.GetComponent<NetworkProjectile>()?.Init(velocity); // set velocity
    }

    if (netObj != null)
        netObj.Spawn(true);
}

    }
}
