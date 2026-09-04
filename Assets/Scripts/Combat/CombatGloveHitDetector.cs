using System.Collections.Generic;
using UnityEngine;

public enum CombatGloveType
{
    Left = 0,
    Right = 1
}

[DisallowMultipleComponent]
public class CombatGloveHitDetector : MonoBehaviour
{
    public CombatGloveType gloveType = CombatGloveType.Left;
    public NetworkPlayerCombatState ownerCombatant;
    public Collider gloveTrigger;
    public float minimumPunchSpeed = 1.25f;
    [Range(-1f, 1f)] public float minimumApproachDot = 0.15f;
    public float repeatHitCooldown = 0.15f;

    private readonly Dictionary<int, float> nextAllowedHitTimeByTarget = new Dictionary<int, float>();
    private Vector3 previousPosition;
    private bool hasPreviousPosition;

    public Vector3 CurrentVelocity { get; private set; }
    public float CurrentPunchSpeed { get; private set; }
    public float ValidationRadius => gloveTrigger != null ? gloveTrigger.bounds.extents.magnitude : 0.12f;
    public Vector3 WorldCenter => gloveTrigger != null ? gloveTrigger.bounds.center : transform.position;

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

    private void FixedUpdate()
    {
        float deltaTime = Mathf.Max(Time.fixedDeltaTime, 0.0001f);
        Vector3 currentPosition = WorldCenter;

        if (!hasPreviousPosition)
        {
            previousPosition = currentPosition;
            hasPreviousPosition = true;
            return;
        }

        CurrentVelocity = (currentPosition - previousPosition) / deltaTime;
        CurrentPunchSpeed = CurrentVelocity.magnitude;
        previousPosition = currentPosition;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryReportHit(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryReportHit(other);
    }

    private void CacheReferences()
    {
        if (ownerCombatant == null)
        {
            ownerCombatant = GetComponentInParent<NetworkPlayerCombatState>();
        }

        if (gloveTrigger == null)
        {
            gloveTrigger = GetComponent<Collider>();
        }
    }

    private void ConfigureCollider()
    {
        if (gloveTrigger != null)
        {
            gloveTrigger.isTrigger = true;
        }
    }

    private void TryReportHit(Collider other)
    {
        if (ownerCombatant == null || !ownerCombatant.CanReportOutgoingHits || CurrentPunchSpeed < minimumPunchSpeed)
        {
            return;
        }

        CombatHurtbox hurtbox = other.GetComponent<CombatHurtbox>();
        if (hurtbox == null)
        {
            hurtbox = other.GetComponentInParent<CombatHurtbox>();
        }

        if (hurtbox == null || hurtbox.ownerCombatant == null || hurtbox.ownerCombatant == ownerCombatant)
        {
            return;
        }

        Vector3 toTarget = hurtbox.WorldCenter - WorldCenter;
        if (toTarget.sqrMagnitude > 0.0001f)
        {
            float approachDot = Vector3.Dot(CurrentVelocity.normalized, toTarget.normalized);
            if (approachDot < minimumApproachDot)
            {
                return;
            }
        }

        int cooldownKey = ((int)gloveType * 10_000) + hurtbox.GetInstanceID();
        if (nextAllowedHitTimeByTarget.TryGetValue(cooldownKey, out float nextAllowedTime) && Time.time < nextAllowedTime)
        {
            return;
        }

        Vector3 punchDirection = CurrentVelocity.sqrMagnitude > 0.0001f ? CurrentVelocity.normalized : transform.forward;
        Vector3 contactPoint = other.ClosestPoint(WorldCenter);

        if (ownerCombatant.ReportHitCandidate(hurtbox, gloveType, contactPoint, CurrentPunchSpeed, punchDirection))
        {
            nextAllowedHitTimeByTarget[cooldownKey] = Time.time + repeatHitCooldown;
        }
    }
}
