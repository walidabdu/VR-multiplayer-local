using UnityEngine;
using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(NetworkObject))]
public class NetworkGrabHandler : NetworkBehaviour
{
    private XRGrabInteractable _grab;
    private NetworkObject _netObj;

    private void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();
        _netObj = GetComponent<NetworkObject>();
    }

    private void OnEnable()
    {
        _grab.selectEntered.AddListener(OnGrabbed);
        _grab.selectExited.AddListener(OnReleased);
    }

    private void OnDisable()
    {
        _grab.selectEntered.RemoveListener(OnGrabbed);
        _grab.selectExited.RemoveListener(OnReleased);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        // Find who grabbed it (try to locate their NetworkObject)
        ulong newOwner = NetworkManager.Singleton.LocalClientId; // fallback

        var interactorNet = args.interactorObject.transform.GetComponentInParent<NetworkObject>();
        if (interactorNet != null)
            newOwner = interactorNet.OwnerClientId;

        if (IsServer)
        {
            // Host/server can directly transfer ownership
            _netObj.ChangeOwnership(newOwner);
        }
        else
        {
            // Client must ask server to transfer ownership
            RequestOwnershipServerRpc(newOwner);
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (IsServer)
        {
            // Server takes back ownership so gun becomes neutral world object
            _netObj.RemoveOwnership();
        }
        else
        {
            // Client must ask server to remove ownership
            ReleaseOwnershipServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestOwnershipServerRpc(ulong clientId, ServerRpcParams rpcParams = default)
    {
        _netObj.ChangeOwnership(clientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReleaseOwnershipServerRpc(ServerRpcParams rpcParams = default)
    {
        _netObj.RemoveOwnership();
    }
}
