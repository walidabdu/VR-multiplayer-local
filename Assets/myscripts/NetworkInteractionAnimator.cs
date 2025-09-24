using UnityEngine;
using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Playables;

namespace UnityEngine.XR.Content.Interaction{

    [RequireComponent(typeof(InteractionAnimator))]
    public class NetworkInteractionAnimator : NetworkBehaviour
    {
        private InteractionAnimator _animator;
        private IXRSelectInteractable _interactable;

        private void Awake()
        {
            _animator = GetComponent<InteractionAnimator>();
            _interactable = GetComponent<IXRSelectInteractable>();
        }

        private void OnEnable()
        {
            if (_interactable != null)
            {
                _interactable.selectEntered.AddListener(OnSelectLocal);
                _interactable.selectExited.AddListener(OnDeselectLocal);
            }
        }

        private void OnDisable()
        {
            if (_interactable != null)
            {
                _interactable.selectEntered.RemoveListener(OnSelectLocal);
                _interactable.selectExited.RemoveListener(OnDeselectLocal);
            }
        }

        private void OnSelectLocal(SelectEnterEventArgs args)
        {
            // Local animation already plays from InteractionAnimator.
            // We only notify the network to mirror it for others.
            if (IsOwner)
                NotifySelectServerRpc();
        }

        private void OnDeselectLocal(SelectExitEventArgs args)
        {
            if (IsOwner)
                NotifyDeselectServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void NotifySelectServerRpc(ServerRpcParams rpcParams = default)
        {
            NotifySelectClientRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void NotifyDeselectServerRpc(ServerRpcParams rpcParams = default)
        {
            NotifyDeselectClientRpc();
        }

        [ClientRpc]
        private void NotifySelectClientRpc(ClientRpcParams rpcParams = default)
        {
            if (!IsOwner && _animator != null && _animator.m_ToAnimate != null)
            {
                _animator.m_ToAnimate.Play();
            }
        }

        [ClientRpc]
        private void NotifyDeselectClientRpc(ClientRpcParams rpcParams = default)
        {
            if (!IsOwner && _animator != null && _animator.m_ToAnimate != null)
            {
                _animator.m_ToAnimate.Stop();
            }
        }
    }
}
