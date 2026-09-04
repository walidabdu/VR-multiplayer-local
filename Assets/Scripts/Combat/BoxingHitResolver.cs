using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BoxingHitResolver : MonoBehaviour
{
    public static BoxingHitResolver Instance { get; private set; }

    [Header("Validation")]
    public float minimumPunchSpeed = 1.25f;
    public float gloveToHurtboxValidationPadding = 0.12f;
    public float repeatedHitCooldownSeconds = 0.18f;

    [Header("Reaction")]
    public float stunDurationSeconds = 1f;
    public float baseKnockbackOffset = 0.18f;
    public float punchSpeedToKnockbackScale = 0.05f;
    public float maxKnockbackOffset = 0.55f;

    [Header("Hurtbox Profiles")]
    public float headKnockbackMultiplier = 1.2f;
    public float chestKnockbackMultiplier = 1f;
    public float bellyKnockbackMultiplier = 0.85f;
    public float headStunMultiplier = 1.15f;
    public float chestStunMultiplier = 1f;
    public float bellyStunMultiplier = 0.9f;
    public float headReactionMultiplier = 1.25f;
    public float chestReactionMultiplier = 1f;
    public float bellyReactionMultiplier = 0.9f;
    public float heavyPunchSpeed = 3.5f;

    private readonly Dictionary<ulong, NetworkPlayerCombatState> combatantsByNetworkObjectId = new Dictionary<ulong, NetworkPlayerCombatState>();
    private readonly Dictionary<string, double> nextAllowedHitTimes = new Dictionary<string, double>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple BoxingHitResolver instances found. Keeping the first one.");
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void RegisterCombatant(NetworkPlayerCombatState combatState)
    {
        if (combatState == null || combatState.NetworkObject == null)
        {
            return;
        }

        combatantsByNetworkObjectId[combatState.NetworkObjectId] = combatState;
    }

    public void UnregisterCombatant(NetworkPlayerCombatState combatState)
    {
        if (combatState == null)
        {
            return;
        }

        combatantsByNetworkObjectId.Remove(combatState.NetworkObjectId);
    }

    public bool TryResolveHit(
        NetworkPlayerCombatState attacker,
        ulong targetNetworkObjectId,
        CombatHurtboxType hurtboxType,
        CombatGloveType gloveType,
        float punchSpeed,
        Vector3 reportedGlovePosition,
        Vector3 reportedPunchDirection)
    {
        if (attacker == null || punchSpeed < minimumPunchSpeed)
        {
            return false;
        }

        if (!combatantsByNetworkObjectId.TryGetValue(targetNetworkObjectId, out NetworkPlayerCombatState target) || target == null)
        {
            return false;
        }

        if (attacker.OwnerClientId == target.OwnerClientId)
        {
            return false;
        }

        if (!target.TryGetHurtbox(hurtboxType, out CombatHurtbox hurtbox))
        {
            return false;
        }

        CombatGloveHitDetector glove = null;
        attacker.TryGetGlove(gloveType, out glove);

        string cooldownKey = $"{attacker.NetworkObjectId}:{targetNetworkObjectId}:{(int)hurtboxType}:{(int)gloveType}";
        double now = attacker.NetworkManager.ServerTime.Time;

        if (nextAllowedHitTimes.TryGetValue(cooldownKey, out double nextAllowedTime) && now < nextAllowedTime)
        {
            return false;
        }

        Vector3 serverGlovePosition = glove != null ? glove.WorldCenter : reportedGlovePosition;
        Vector3 hurtboxCenter = hurtbox.WorldCenter;
        float allowedDistance = hurtbox.validationRadius + (glove != null ? glove.ValidationRadius : 0.12f) + gloveToHurtboxValidationPadding;

        if ((serverGlovePosition - hurtboxCenter).sqrMagnitude > allowedDistance * allowedDistance)
        {
            return false;
        }

        Vector3 knockbackDirection = reportedPunchDirection;
        knockbackDirection.y = 0f;

        if (knockbackDirection.sqrMagnitude < 0.0001f)
        {
            knockbackDirection = target.transform.position - attacker.transform.position;
            knockbackDirection.y = 0f;
        }

        if (knockbackDirection.sqrMagnitude < 0.0001f)
        {
            knockbackDirection = attacker.transform.forward;
        }

        float knockbackMagnitude = Mathf.Clamp(
            baseKnockbackOffset + Mathf.Max(0f, punchSpeed - minimumPunchSpeed) * punchSpeedToKnockbackScale,
            baseKnockbackOffset,
            maxKnockbackOffset);

        float knockbackMultiplier = GetKnockbackMultiplier(hurtboxType);
        float stunMultiplier = GetStunMultiplier(hurtboxType);
        float reactionMultiplier = GetReactionMultiplier(hurtboxType);
        float reactionStrength = Mathf.Clamp01(punchSpeed / Mathf.Max(heavyPunchSpeed, minimumPunchSpeed + 0.01f)) * reactionMultiplier;

        target.ApplyAuthoritativeHit(
            hurtboxType,
            knockbackDirection.normalized,
            knockbackMagnitude * knockbackMultiplier,
            stunDurationSeconds * stunMultiplier,
            reactionStrength);

        attacker.SendConfirmedHitFeedback(gloveType, hurtboxType, reactionStrength);
        nextAllowedHitTimes[cooldownKey] = now + repeatedHitCooldownSeconds;
        return true;
    }

    private float GetKnockbackMultiplier(CombatHurtboxType hurtboxType)
    {
        switch (hurtboxType)
        {
            case CombatHurtboxType.Head:
                return headKnockbackMultiplier;
            case CombatHurtboxType.Belly:
                return bellyKnockbackMultiplier;
            default:
                return chestKnockbackMultiplier;
        }
    }

    private float GetStunMultiplier(CombatHurtboxType hurtboxType)
    {
        switch (hurtboxType)
        {
            case CombatHurtboxType.Head:
                return headStunMultiplier;
            case CombatHurtboxType.Belly:
                return bellyStunMultiplier;
            default:
                return chestStunMultiplier;
        }
    }

    private float GetReactionMultiplier(CombatHurtboxType hurtboxType)
    {
        switch (hurtboxType)
        {
            case CombatHurtboxType.Head:
                return headReactionMultiplier;
            case CombatHurtboxType.Belly:
                return bellyReactionMultiplier;
            default:
                return chestReactionMultiplier;
        }
    }
}
