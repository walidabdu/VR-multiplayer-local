using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class NetworkProjectile : NetworkBehaviour
{
    private Rigidbody _rb;
    private Vector3 _initialVelocity;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    // Called by the gun right after spawn
    public void Init(Vector3 velocity)
    {
        _initialVelocity = velocity;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Apply velocity on server
            _rb.velocity = _initialVelocity;
        }
    }
}
