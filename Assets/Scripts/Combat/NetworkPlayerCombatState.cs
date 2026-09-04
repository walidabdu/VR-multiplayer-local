using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayerCombatState : NetworkBehaviour
{
    [Header("Core References")]
    public NetworkPlayer networkPlayer;
    public AtomNetworkAnimator_V13 atomAnimator;
    public AudioSource localFeedbackSource;

    [Header("Combat State")]
    public bool autoBlockLocalLocomotion = true;
    public bool blockOutgoingPunchReportingWhileStunned = true;
    public float stunDurationSeconds = 1f;
    public float knockbackRecoverySpeed = 2.5f;
    public float maxCombatOffset = 0.6f;

    [Header("Local Feedback")]
    public AudioClip confirmedHitClip;
    public AudioClip receivedHeadHitClip;
    public AudioClip receivedBodyHitClip;
    [Range(0f, 1f)] public float attackerHapticAmplitude = 0.3f;
    public float attackerHapticDuration = 0.045f;
    [Range(0f, 1f)] public float defenderHapticAmplitude = 0.5f;
    public float defenderHapticDuration = 0.075f;
    public float heavyImpactHapticMultiplier = 1.35f;

    private readonly NetworkVariable<bool> isStunned = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<Vector3> combatOffset = new NetworkVariable<Vector3>(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<Vector3> hitReactionDirection = new NetworkVariable<Vector3>(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> hitReactionStrength = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<int> hitReactionHurtbox = new NetworkVariable<int>(
        (int)CombatHurtboxType.Chest,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<int> hitReactionSequence = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly Dictionary<CombatHurtboxType, CombatHurtbox> hurtboxesByType = new Dictionary<CombatHurtboxType, CombatHurtbox>();
    private readonly Dictionary<CombatGloveType, CombatGloveHitDetector> glovesByType = new Dictionary<CombatGloveType, CombatGloveHitDetector>();
    private readonly Dictionary<Behaviour, bool> locomotionStates = new Dictionary<Behaviour, bool>();
    private readonly List<Behaviour> trackedLocomotionBehaviours = new List<Behaviour>();

    private double stunEndServerTime;
    private bool lastAppliedOwnerStunState;
    private bool ownerStunStateInitialized;
    private bool resolverRegistered;

    public bool IsStunned => isStunned.Value;
    public Vector3 CurrentCombatOffset => combatOffset.Value;
    public bool CanReportOutgoingHits => IsSpawned && IsOwner && (!blockOutgoingPunchReportingWhileStunned || !IsStunned);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        CacheRuntimeReferences();
        CacheCombatRigReferences();

        isStunned.OnValueChanged += OnStunStateChanged;
        hitReactionSequence.OnValueChanged += OnHitReactionSequenceChanged;

        if (IsServer)
        {
            TryRegisterWithResolver();
        }

        ApplyOwnerLocomotionState(IsStunned);
    }

    public override void OnNetworkDespawn()
    {
        isStunned.OnValueChanged -= OnStunStateChanged;
        hitReactionSequence.OnValueChanged -= OnHitReactionSequenceChanged;

        if (IsServer)
        {
            TryUnregisterFromResolver();
        }

        RestoreLocomotionState();
        base.OnNetworkDespawn();
    }

    private void Awake()
    {
        CacheRuntimeReferences();
        CacheCombatRigReferences();
    }

    private void Update()
    {
        if (IsServer && IsSpawned)
        {
            TryRegisterWithResolver();
            UpdateServerCombatState();
        }

        if (IsOwner && autoBlockLocalLocomotion)
        {
            ApplyOwnerLocomotionState(IsStunned);
        }
    }

    public bool ReportHitCandidate(
        CombatHurtbox hurtbox,
        CombatGloveType gloveType,
        Vector3 contactPoint,
        float punchSpeed,
        Vector3 punchDirection)
    {
        if (!CanReportOutgoingHits || hurtbox == null || hurtbox.ownerCombatant == null)
        {
            return false;
        }

        ReportHitCandidateServerRpc(
            hurtbox.ownerCombatant.NetworkObjectId,
            hurtbox.hurtboxType,
            gloveType,
            punchSpeed,
            contactPoint,
            punchDirection);

        return true;
    }

    public bool TryGetHurtbox(CombatHurtboxType hurtboxType, out CombatHurtbox hurtbox)
    {
        if (hurtboxesByType.TryGetValue(hurtboxType, out hurtbox) && hurtbox != null)
        {
            return true;
        }

        CacheCombatRigReferences();
        return hurtboxesByType.TryGetValue(hurtboxType, out hurtbox) && hurtbox != null;
    }

    public bool TryGetGlove(CombatGloveType gloveType, out CombatGloveHitDetector glove)
    {
        if (glovesByType.TryGetValue(gloveType, out glove) && glove != null)
        {
            return true;
        }

        CacheCombatRigReferences();
        return glovesByType.TryGetValue(gloveType, out glove) && glove != null;
    }

    public void CacheCombatRigReferences()
    {
        hurtboxesByType.Clear();
        glovesByType.Clear();

        CombatHurtbox[] hurtboxes = GetComponentsInChildren<CombatHurtbox>(true);
        foreach (CombatHurtbox hurtbox in hurtboxes)
        {
            if (hurtbox == null)
            {
                continue;
            }

            hurtbox.ownerCombatant = this;
            hurtboxesByType[hurtbox.hurtboxType] = hurtbox;
        }

        CombatGloveHitDetector[] gloves = GetComponentsInChildren<CombatGloveHitDetector>(true);
        foreach (CombatGloveHitDetector glove in gloves)
        {
            if (glove == null)
            {
                continue;
            }

            glove.ownerCombatant = this;
            glovesByType[glove.gloveType] = glove;
        }
    }

    public void ApplyAuthoritativeHit(
        CombatHurtboxType hurtboxType,
        Vector3 direction,
        float knockbackMagnitude,
        float stunTimeSeconds,
        float reactionStrengthNormalized)
    {
        if (!IsServer)
        {
            return;
        }

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = transform.forward;
        }

        Vector3 nextOffset = combatOffset.Value + direction.normalized * knockbackMagnitude;
        combatOffset.Value = Vector3.ClampMagnitude(nextOffset, maxCombatOffset);
        hitReactionDirection.Value = direction.normalized;
        hitReactionStrength.Value = Mathf.Clamp01(reactionStrengthNormalized);
        hitReactionHurtbox.Value = (int)hurtboxType;
        hitReactionSequence.Value = hitReactionSequence.Value + 1;

        float resolvedStunDuration = stunTimeSeconds > 0f ? stunTimeSeconds : stunDurationSeconds;
        stunEndServerTime = Math.Max(stunEndServerTime, NetworkManager.ServerTime.Time + resolvedStunDuration);
        isStunned.Value = true;
    }

    private void CacheRuntimeReferences()
    {
        if (networkPlayer == null)
        {
            networkPlayer = GetComponent<NetworkPlayer>();
        }

        if (atomAnimator == null)
        {
            atomAnimator = GetComponentInChildren<AtomNetworkAnimator_V13>(true);
        }

        if (localFeedbackSource == null)
        {
            localFeedbackSource = GetComponent<AudioSource>();
        }
    }

    private void TryRegisterWithResolver()
    {
        if (resolverRegistered || BoxingHitResolver.Instance == null)
        {
            return;
        }

        BoxingHitResolver.Instance.RegisterCombatant(this);
        resolverRegistered = true;
    }

    private void TryUnregisterFromResolver()
    {
        if (!resolverRegistered || BoxingHitResolver.Instance == null)
        {
            return;
        }

        BoxingHitResolver.Instance.UnregisterCombatant(this);
        resolverRegistered = false;
    }

    private void UpdateServerCombatState()
    {
        double now = NetworkManager.ServerTime.Time;
        bool shouldBeStunned = now < stunEndServerTime;

        if (isStunned.Value != shouldBeStunned)
        {
            isStunned.Value = shouldBeStunned;
        }

        Vector3 nextOffset = Vector3.MoveTowards(combatOffset.Value, Vector3.zero, knockbackRecoverySpeed * Time.deltaTime);
        if ((nextOffset - combatOffset.Value).sqrMagnitude > 0.000001f)
        {
            combatOffset.Value = nextOffset;
        }
        else if (combatOffset.Value != Vector3.zero)
        {
            combatOffset.Value = Vector3.zero;
        }
    }

    private void OnStunStateChanged(bool previousValue, bool newValue)
    {
        if (IsOwner && autoBlockLocalLocomotion)
        {
            ApplyOwnerLocomotionState(newValue);
        }
    }

    private void OnHitReactionSequenceChanged(int previousValue, int newValue)
    {
        CombatHurtboxType hurtboxType = ResolveHurtboxType(hitReactionHurtbox.Value);
        Vector3 direction = hitReactionDirection.Value;
        float strength = hitReactionStrength.Value;

        if (atomAnimator == null)
        {
            CacheRuntimeReferences();
        }

        if (atomAnimator != null)
        {
            atomAnimator.TriggerHitReaction(hurtboxType, direction, strength);
        }

        if (IsOwner)
        {
            PlayReceivedHitFeedback(hurtboxType, strength);
        }
    }

    private void ApplyOwnerLocomotionState(bool shouldBlockLocomotion)
    {
        if (!IsOwner || !autoBlockLocalLocomotion)
        {
            return;
        }

        if (ownerStunStateInitialized && lastAppliedOwnerStunState == shouldBlockLocomotion)
        {
            return;
        }

        ownerStunStateInitialized = true;
        lastAppliedOwnerStunState = shouldBlockLocomotion;

        RefreshTrackedLocomotionBehaviours();

        foreach (Behaviour locomotionBehaviour in trackedLocomotionBehaviours)
        {
            if (locomotionBehaviour == null)
            {
                continue;
            }

            if (shouldBlockLocomotion)
            {
                if (!locomotionStates.ContainsKey(locomotionBehaviour))
                {
                    locomotionStates[locomotionBehaviour] = locomotionBehaviour.enabled;
                }

                locomotionBehaviour.enabled = false;
            }
            else if (locomotionStates.TryGetValue(locomotionBehaviour, out bool wasEnabled))
            {
                locomotionBehaviour.enabled = wasEnabled;
            }
        }
    }

    private void RestoreLocomotionState()
    {
        foreach (KeyValuePair<Behaviour, bool> pair in locomotionStates)
        {
            if (pair.Key != null)
            {
                pair.Key.enabled = pair.Value;
            }
        }

        locomotionStates.Clear();
    }

    private void RefreshTrackedLocomotionBehaviours()
    {
        trackedLocomotionBehaviours.Clear();

        Transform localRigRoot = VRRIgReferences.Singleton != null ? VRRIgReferences.Singleton.root : null;
        if (localRigRoot == null)
        {
            return;
        }

        Behaviour[] behaviours = localRigRoot.GetComponentsInParent<Behaviour>(true);
        foreach (Behaviour behaviour in behaviours)
        {
            if (behaviour == null)
            {
                continue;
            }

            string typeName = behaviour.GetType().Name;
            if (typeName.IndexOf("MoveProvider", StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf("TurnProvider", StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf("TeleportationProvider", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                trackedLocomotionBehaviours.Add(behaviour);
            }
        }
    }

    [ServerRpc]
    private void ReportHitCandidateServerRpc(
        ulong targetNetworkObjectId,
        CombatHurtboxType hurtboxType,
        CombatGloveType gloveType,
        float punchSpeed,
        Vector3 contactPoint,
        Vector3 punchDirection)
    {
        BoxingHitResolver.Instance?.TryResolveHit(
            this,
            targetNetworkObjectId,
            hurtboxType,
            gloveType,
            punchSpeed,
            contactPoint,
            punchDirection);
    }

    public void SendConfirmedHitFeedback(CombatGloveType gloveType, CombatHurtboxType hurtboxType, float severity)
    {
        if (!IsServer)
        {
            return;
        }

        ConfirmedHitFeedbackClientRpc((int)gloveType, (int)hurtboxType, Mathf.Clamp01(severity), CreateOwnerClientRpcParams(OwnerClientId));
    }

    [ClientRpc]
    private void ConfirmedHitFeedbackClientRpc(int gloveTypeValue, int hurtboxTypeValue, float severity, ClientRpcParams clientRpcParams = default)
    {
        if (!IsOwner)
        {
            return;
        }

        PlayConfirmedHitFeedback((CombatGloveType)gloveTypeValue, ResolveHurtboxType(hurtboxTypeValue), severity);
    }

    private void PlayConfirmedHitFeedback(CombatGloveType gloveType, CombatHurtboxType hurtboxType, float severity)
    {
        float scaledAmplitude = GetScaledHapticAmplitude(attackerHapticAmplitude, severity, hurtboxType);

        if (VRRIgReferences.Singleton != null)
        {
            VRRIgReferences.Singleton.SendGloveHaptics(gloveType, scaledAmplitude, attackerHapticDuration);
        }

        PlayFeedbackClip(confirmedHitClip);
    }

    private void PlayReceivedHitFeedback(CombatHurtboxType hurtboxType, float severity)
    {
        float scaledAmplitude = GetScaledHapticAmplitude(defenderHapticAmplitude, severity, hurtboxType);

        if (VRRIgReferences.Singleton != null)
        {
            VRRIgReferences.Singleton.SendBothControllerHaptics(scaledAmplitude, defenderHapticDuration);
        }

        AudioClip clip = hurtboxType == CombatHurtboxType.Head ? receivedHeadHitClip : receivedBodyHitClip;
        PlayFeedbackClip(clip);
    }

    private void PlayFeedbackClip(AudioClip clip)
    {
        if (clip == null || localFeedbackSource == null)
        {
            return;
        }

        localFeedbackSource.PlayOneShot(clip);
    }

    private float GetScaledHapticAmplitude(float baseAmplitude, float severity, CombatHurtboxType hurtboxType)
    {
        float hurtboxMultiplier = hurtboxType == CombatHurtboxType.Head ? 1.1f : 1f;
        float severityMultiplier = Mathf.Lerp(0.75f, heavyImpactHapticMultiplier, Mathf.Clamp01(severity));
        return Mathf.Clamp01(baseAmplitude * hurtboxMultiplier * severityMultiplier);
    }

    private static CombatHurtboxType ResolveHurtboxType(int hurtboxTypeValue)
    {
        if (Enum.IsDefined(typeof(CombatHurtboxType), hurtboxTypeValue))
        {
            return (CombatHurtboxType)hurtboxTypeValue;
        }

        return CombatHurtboxType.Chest;
    }

    private static ClientRpcParams CreateOwnerClientRpcParams(ulong ownerClientId)
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { ownerClientId }
            }
        };
    }
}
