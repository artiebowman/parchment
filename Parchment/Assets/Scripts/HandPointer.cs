using UnityEngine;

public class HandPointer : MonoBehaviour
{
    private OVRHand hand;

    public Ray PointerRay;
    public bool ConfirmedThisFrame;

    void Awake()
    {
        hand = GetComponent<OVRHand>();
    }

    void Update()
    {
        ConfirmedThisFrame = false;

        if (!hand.IsTracked) return;

        Transform pose = hand.PointerPose;
        PointerRay = new Ray(pose.position, pose.forward);
    }
}