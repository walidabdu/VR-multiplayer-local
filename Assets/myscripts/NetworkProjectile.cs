using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
public class NetworkProjectile : NetworkBehaviour
{
    private Rigidbody _rb;
    private Vector3 _initialVelocity;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    // Call this right after spawning from server
    public void Init(Vector3 velocity)
    {
        _initialVelocity = velocity;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _rb.velocity = _initialVelocity;
        }
    }
}
