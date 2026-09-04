using UnityEngine;

public enum CombatHurtboxType
{
    Head = 0,
    Chest = 1,
    Belly = 2
}

[DisallowMultipleComponent]
public class CombatHurtbox : MonoBehaviour
{
    public CombatHurtboxType hurtboxType = CombatHurtboxType.Head;
    public NetworkPlayerCombatState ownerCombatant;
    public Collider hurtboxCollider;
    public float validationRadius = 0.18f;

    public Vector3 WorldCenter => hurtboxCollider != null ? hurtboxCollider.bounds.center : transform.position;

    private void Reset()
    {
        CacheReferences();
        ConfigureCollider();
    }

    private void Awake()
    {
        CacheReferences();
        ConfigureCollider();
    }

    private void CacheReferences()
    {
        if (ownerCombatant == null)
        {
            ownerCombatant = GetComponentInParent<NetworkPlayerCombatState>();
        }

        if (hurtboxCollider == null)
        {
            hurtboxCollider = GetComponent<Collider>();
        }
    }

    private void ConfigureCollider()
    {
        if (hurtboxCollider != null)
        {
            hurtboxCollider.isTrigger = true;
        }
    }
}
