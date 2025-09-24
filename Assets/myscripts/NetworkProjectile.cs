// NetworkProjectile.cs
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
public class NetworkProjectile : NetworkBehaviour
{
    private Rigidbody _rb;
    public float lifeTime = 5f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        // If owner (client) spawned this and wants immediate local velocity, they can set it locally.
        // But we already set initial velocity on server in NetworkGun.FireServerRpc (rb.velocity = ...),
        // so this method is optional. Keep for safety or for visual-only tweaks.
        Invoke(nameof(SelfDestruct), lifeTime);
    }

    private void SelfDestruct()
    {
        if (IsServer)
            NetworkObject.Despawn(true);
        else
            Destroy(gameObject);
    }
}
