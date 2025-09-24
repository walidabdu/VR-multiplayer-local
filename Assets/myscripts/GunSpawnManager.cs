// GunSpawnManager.cs
using Unity.Netcode;
using UnityEngine;

public class GunSpawnManager : NetworkBehaviour
{
    [Header("Assign a gun prefab (NetworkObject + NetworkTransform)")]
    [SerializeField] private GameObject gunPrefab;

    [Header("Put spawn point transforms (Spawn_0, Spawn_1, ...)")]
    [SerializeField] private Transform[] spawnPoints;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // spawn guns for any clients already connected (host included)
        var clients = NetworkManager.Singleton.ConnectedClientsList;
        for (int i = 0; i < clients.Count; i++)
        {
            SpawnForClient(clients[i].ClientId, i);
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        // choose spawn index: simple: use count of existing clients or clientId modulo
        int spawnIndex = Mathf.Min((int)clientId, spawnPoints.Length - 1);
        SpawnForClient(clientId, spawnIndex);
    }

    private void SpawnForClient(ulong clientId, int index)
    {
        if (gunPrefab == null) { Debug.LogError("gunPrefab missing in GunSpawnManager"); return; }
        if (spawnPoints == null || spawnPoints.Length == 0) { Debug.LogError("No spawnPoints assigned"); return; }

        Transform sp = spawnPoints[Mathf.Clamp(index, 0, spawnPoints.Length - 1)];
        GameObject go = Instantiate(gunPrefab, sp.position, sp.rotation);
        var netObj = go.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("Gun prefab must have NetworkObject on root.");
            Destroy(go);
            return;
        }

        // Spawn on server; not automatically owned by the client — ownership will be given on grab
        netObj.Spawn();
    }
}
