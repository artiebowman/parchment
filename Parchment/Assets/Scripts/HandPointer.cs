using UnityEngine;

[DefaultExecutionOrder(-100)]
public class HandPointer : MonoBehaviour
{
    private OVRHand hand;

    [System.NonSerialized] public Ray PointerRay;
    public bool ConfirmedThisFrame;
    public bool IsTracked;

    // Others can shorten the drawn line this frame (e.g. hover sets it to the sheet hit). Reset every frame.
    [System.NonSerialized] public float LineClip = float.PositiveInfinity;

    [Range(0f, 1f)] public float pinchOnThreshold = 0.8f;
    [Range(0f, 1f)] public float pinchOffThreshold = 0.5f;
    public float cooldownSeconds = 0.15f;

    private bool isPinching;
    private float nextAllowedTime;

    public Material rayMaterial;
    public float rayLength = 2f;
    public TrialManager trial;

    private LineRenderer line;
    private bool onTarget;

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
        LineClip = float.PositiveInfinity;
        IsTracked = hand.IsTracked;

        if (!IsTracked)
        {
            line.enabled = false;
            return;
        }
        line.enabled = true;

        Transform pose = hand.PointerPose;
        PointerRay = new Ray(pose.position, pose.forward);

        onTarget = false;
        RaycastHit hit;
        if (Physics.Raycast(PointerRay, out hit, rayLength))
        {
            if (hit.collider.name.StartsWith("Sphere_"))
            {
                int id = int.Parse(hit.collider.name.Substring(7));
                onTarget = trial.IsCurrentTarget(id);
                LineClip = hit.distance;
            }
        }

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

    void LateUpdate()
    {
        if (!IsTracked) return;

        float len = Mathf.Min(rayLength, LineClip);
        line.SetPosition(0, PointerRay.origin);
        line.SetPosition(1, PointerRay.origin + PointerRay.direction * len);
        line.material.color = onTarget ? Color.green : Color.white;
    }
}