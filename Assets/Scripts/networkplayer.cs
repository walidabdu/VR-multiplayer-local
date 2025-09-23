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
    // Start is called before the first frame update


    public Renderer[] MESHTODISABLE;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            foreach (Renderer r in MESHTODISABLE)
            {
                r.enabled = false;
            }
        }
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (IsOwner)
        {
            head.position = VRRIgReferences.Singleton.head.position;
            head.rotation = VRRIgReferences.Singleton.head.rotation;

            leftHand.position = VRRIgReferences.Singleton.leftHand.position;
            leftHand.rotation = VRRIgReferences.Singleton.leftHand.rotation;

            rightHand.position = VRRIgReferences.Singleton.rightHand.position;
            rightHand.rotation = VRRIgReferences.Singleton.rightHand.rotation;

            body.position = VRRIgReferences.Singleton.root.position;
            body.rotation = VRRIgReferences.Singleton.root.rotation;
        }
        
    }
}
