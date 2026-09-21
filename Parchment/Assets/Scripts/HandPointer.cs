using UnityEngine;

public class HandPointer : MonoBehaviour
{
    private OVRHand hand;

    public Ray PointerRay;
    public bool ConfirmedThisFrame;

    [Range(0f, 1f)] public float pinchOnThreshold = 0.8f;
    [Range(0f, 1f)] public float pinchOffThreshold = 0.5f;
    public float cooldownSeconds = 0.15f;

    private bool isPinching;
    private float nextAllowedTime;

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

        float strength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);

        if (!isPinching)
        {
            if (strength >= pinchOnThreshold && Time.time >= nextAllowedTime)
            {
                isPinching = true;
                ConfirmedThisFrame = true;
                nextAllowedTime = Time.time + cooldownSeconds;
            }
        }
        else
        {
            if (strength <= pinchOffThreshold)
            {
                isPinching = false;
            }
        }
    }
}