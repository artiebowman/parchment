using UnityEngine;

[DefaultExecutionOrder(-100)]
public class HandPointer : MonoBehaviour
{
    private OVRHand hand;
    private OVRSkeleton skeleton;
    private Transform indexTip;

    [System.NonSerialized] public Ray PointerRay;
    public bool ConfirmedThisFrame;
    public bool IsTracked;

    // Index fingertip in world space, valid only when HasIndexTip is true.
    [System.NonSerialized] public Vector3 IndexTip;
    public bool HasIndexTip;

    // Others can shorten the drawn line this frame (e.g. hover sets it to the sheet hit). Reset every frame.
    [System.NonSerialized] public float LineClip = float.PositiveInfinity;

    [Range(0f, 1f)] public float pinchOnThreshold = 0.8f;
    [Range(0f, 1f)] public float pinchOffThreshold = 0.5f;
    public float cooldownSeconds = 0.15f;
    public bool requireHighConfidence = true;   // ignore pinches from a poorly tracked hand

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
        skeleton = GetComponent<OVRSkeleton>();

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
        HasIndexTip = false;

        if (!IsTracked)
        {
            line.enabled = false;
            isPinching = false;
            return;
        }
        line.enabled = true;

        Transform pose = hand.PointerPose;
        PointerRay = new Ray(pose.position, pose.forward);

        if (indexTip == null) FindIndexTip();
        if (indexTip != null)
        {
            IndexTip = indexTip.position;
            HasIndexTip = true;
        }

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

        bool confident = !requireHighConfidence
                         || hand.HandConfidence == OVRHand.TrackingConfidence.High;

        float strength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);

        if (!isPinching)
        {
            if (confident && strength >= pinchOnThreshold && Time.time >= nextAllowedTime)
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
        line.enabled = len > 0.01f;
        line.SetPosition(0, PointerRay.origin);
        line.SetPosition(1, PointerRay.origin + PointerRay.direction * len);
        line.material.color = onTarget ? Color.green : Color.white;
    }

    void FindIndexTip()
    {
        if (skeleton == null || !skeleton.IsInitialized) return;

        // Bone ids collide across Meta's hand/body enums, so pick by index, not name.
        // OpenXR hand skeleton (26 bones): index tip is bone 10. Legacy hand skeleton (24 bones): bone 20.
        int tipIndex = skeleton.Bones.Count == 26 ? 10 : 20;
        if (tipIndex >= skeleton.Bones.Count) return;

        indexTip = skeleton.Bones[tipIndex].Transform;
        Debug.Log(name + ": index tip bound to bone " + tipIndex);
    }
}