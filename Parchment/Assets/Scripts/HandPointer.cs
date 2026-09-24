using UnityEngine;

public class HandPointer : MonoBehaviour
{
    private OVRHand hand;

    [System.NonSerialized] public Ray PointerRay;
    public bool ConfirmedThisFrame;
    public bool IsTracked;

    [Range(0f, 1f)] public float pinchOnThreshold = 0.8f;
    [Range(0f, 1f)] public float pinchOffThreshold = 0.5f;
    public float cooldownSeconds = 0.15f;

    private bool isPinching;
    private float nextAllowedTime;

    public Material rayMaterial;
    public float rayLength = 2f;
    public TrialManager trial;

    private LineRenderer line;

    void Awake()
    {
        hand = GetComponent<OVRHand>();

        line = gameObject.AddComponent<LineRenderer>();
        line.material = rayMaterial;
        line.positionCount = 2;
        line.startWidth = 0.004f;
        line.endWidth = 0.004f;
        line.useWorldSpace = true;
    }

    void Update()
    {
        ConfirmedThisFrame = false;
        IsTracked = hand.IsTracked;

        if (!IsTracked)
        {
            line.enabled = false;
            return;
        }
        line.enabled = true;

        Transform pose = hand.PointerPose;
        PointerRay = new Ray(pose.position, pose.forward);

        line.SetPosition(0, PointerRay.origin);
        line.SetPosition(1, PointerRay.origin + PointerRay.direction * rayLength);

        bool onTarget = false;
        RaycastHit hit;
        if (Physics.Raycast(PointerRay, out hit, rayLength))
        {
            if (hit.collider.name.StartsWith("Sphere_"))
            {
                int id = int.Parse(hit.collider.name.Substring(7));
                onTarget = trial.IsCurrentTarget(id);
            }
        }
        line.material.color = onTarget ? Color.green : Color.white;

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