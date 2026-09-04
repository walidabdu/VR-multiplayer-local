using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkPlayer : NetworkBehaviour
{
    public Transform head;
    public Transform leftHand; 
    public Transform rightHand;
    public Transform body;
    public Renderer[] MESHTODISABLE;
    public Rigidbody rootRigidbody;
    public float minimumValidHeadHeight = 0.5f;

    public Transform HeadTransform => head;
    public Transform LeftHandTransform => leftHand;
    public Transform RightHandTransform => rightHand;
    public Transform BodyTransform => body != null ? body : transform;

    private void Awake()
    {
        CacheRootRigidbody();
        ConfigureAvatarRootPhysics();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        CacheRootRigidbody();
        ConfigureAvatarRootPhysics();

        if (IsOwner)
        {
            foreach (Renderer r in MESHTODISABLE)
            {
                r.enabled = false;
            }
        }
    }

    private void CacheRootRigidbody()
    {
        if (rootRigidbody == null)
        {
            rootRigidbody = GetComponent<Rigidbody>();
        }
    }

    private void ConfigureAvatarRootPhysics()
    {
        if (rootRigidbody == null)
        {
            return;
        }

        // The tracked network avatar must never run as a dynamic combat body.
        rootRigidbody.useGravity = false;
        rootRigidbody.isKinematic = true;
        rootRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        rootRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner || VRRIgReferences.Singleton == null)
        {
            return;
        }

        if (!HasValidTrackingPose(VRRIgReferences.Singleton))
        {
            return;
        }

        head.position = VRRIgReferences.Singleton.head.position;
        head.rotation = VRRIgReferences.Singleton.head.rotation;

        leftHand.position = VRRIgReferences.Singleton.leftHand.position;
        leftHand.rotation = VRRIgReferences.Singleton.leftHand.rotation;

        rightHand.position = VRRIgReferences.Singleton.rightHand.position;
        rightHand.rotation = VRRIgReferences.Singleton.rightHand.rotation;

        BodyTransform.position = VRRIgReferences.Singleton.root.position;
        BodyTransform.rotation = VRRIgReferences.Singleton.root.rotation;
    }

    private bool HasValidTrackingPose(VRRIgReferences references)
    {
        if (references == null || references.head == null || references.leftHand == null || references.rightHand == null || references.root == null)
        {
            return false;
        }

        return references.head.position.y > minimumValidHeadHeight;
    }
}
