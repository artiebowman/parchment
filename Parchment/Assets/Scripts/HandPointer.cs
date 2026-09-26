using UnityEngine;

[DefaultExecutionOrder(-100)]
public class HandPointer : MonoBehaviour
{
    // Shared by all hands: no pinch counts as a confirm until this time. PalmMenu pushes it forward while open.
    public static float SuppressConfirmUntil = float.NegativeInfinity;

    private OVRHand hand;
    private OVRSkeleton skeleton;
    private Transform indexTip;
    private Transform wristBone, indexKnuckle, littleKnuckle;   // palm

    [System.NonSerialized] public Ray PointerRay;
    public bool ConfirmedThisFrame;
    public bool IsTracked;

    // Beam starts at the index fingertip (direction still comes from Meta's stabilized pointer pose).
    // Off = beam starts at the pointer pose near the wrist, matching Meta's system UI. Tried on, rejected: felt like it leaned.
    public bool rayFromFingertip = false;

    // Index fingertip in world space, valid only when HasIndexTip is true.
    [System.NonSerialized] public Vector3 IndexTip;
    public bool HasIndexTip;

    // Palm center and the direction the palm faces, valid only when HasPalm is true.
    [System.NonSerialized] public Vector3 PalmCenter;
    [System.NonSerialized] public Vector3 PalmNormal;
    public bool HasPalm;
    public bool PalmUp;                                  // palm facing the ceiling
    public bool flipPalmNormal;                          // tick on whichever hand comes out mirrored
    [Range(0f, 1f)] public float palmUpThreshold = 0.7f; // 1 = dead flat, lower = more forgiving

    // Others can shorten the drawn line this frame (e.g. hover sets it to the sheet hit). Reset every frame.
    // Infinity means nothing clipped it, and the beam is not drawn.
    [System.NonSerialized] public float LineClip = float.PositiveInfinity;

    // Set each frame by ParchmentHover/PalmMenu: true when the hand is at rest or busy with the menu. Beam hidden.
    [System.NonSerialized] public bool Resting;

    [Range(0f, 1f)] public float pinchOnThreshold = 0.8f;
    [Range(0f, 1f)] public float pinchOffThreshold = 0.5f;
    public float cooldownSeconds = 0.15f;
    public bool requireHighConfidence = true;   // ignore pinches from a poorly tracked hand

    private bool isPinching;
    private float nextAllowedTime;

    public Material rayMaterial;
    public float rayLength = 2f;
    public TrialManager trial;
    public Transform cubeRoot;      // drag TaskCube here; beam shows when pointing into the cube's volume
    public float cubeSize = 1f;

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
        Resting = false;
        IsTracked = hand.IsTracked;
        HasIndexTip = false;
        HasPalm = false;
        PalmUp = false;

        if (!IsTracked)
        {
            line.enabled = false;
            isPinching = false;
            return;
        }

        Transform pose = hand.PointerPose;
        PointerRay = new Ray(pose.position, pose.forward);

        if (indexTip == null) FindBones();
        if (indexTip != null)
        {
            IndexTip = indexTip.position;
            HasIndexTip = true;

            // Same aim, but the beam leaves from the fingertip instead of passing through the hand.
            if (rayFromFingertip) PointerRay = new Ray(IndexTip, pose.forward);
        }

        // Palm: plane through wrist and the two outer knuckles; its normal is the way the palm faces.
        if (wristBone != null && indexKnuckle != null && littleKnuckle != null)
        {
            Vector3 w = wristBone.position;
            Vector3 toIndex = indexKnuckle.position - w;
            Vector3 toLittle = littleKnuckle.position - w;

            PalmCenter = (w + indexKnuckle.position + littleKnuckle.position) / 3f;
            PalmNormal = Vector3.Cross(toIndex, toLittle).normalized;
            if (flipPalmNormal) PalmNormal = -PalmNormal;

            HasPalm = true;
            PalmUp = Vector3.Dot(PalmNormal, Vector3.up) >= palmUpThreshold;
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

        // No sphere under the ray: if the ray passes through the cube, run the beam to where it leaves the glass.
        if (float.IsPositiveInfinity(LineClip) && cubeRoot != null)
        {
            Bounds box = new Bounds(cubeRoot.position, Vector3.one * cubeSize);
            if (box.IntersectRay(PointerRay, out float enter) && enter <= rayLength)
            {
                // Fire a ray back from the far end to find the exit face.
                Ray back = new Ray(PointerRay.origin + PointerRay.direction * rayLength, -PointerRay.direction);
                if (box.IntersectRay(back, out float fromEnd))
                    LineClip = rayLength - fromEnd;
                else
                    LineClip = enter;
            }
        }

        bool confident = !requireHighConfidence
                         || hand.HandConfidence == OVRHand.TrackingConfidence.High;

        float strength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
        bool suppressed = Time.time < SuppressConfirmUntil;   // menu is up (or just closed): pinches are not confirms

        if (!isPinching)
        {
            if (confident && strength >= pinchOnThreshold && Time.time >= nextAllowedTime)
            {
                isPinching = true;                             // track the pinch either way so it can't fire when suppression ends
                if (!suppressed)
                {
                    ConfirmedThisFrame = true;
                    nextAllowedTime = Time.time + cooldownSeconds;
                }
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
        bool show = !Resting && len > 0.01f && !float.IsPositiveInfinity(LineClip);
        line.enabled = show;
        if (!show) return;

        line.SetPosition(0, PointerRay.origin);
        line.SetPosition(1, PointerRay.origin + PointerRay.direction * len);
        line.material.color = onTarget ? Color.green : Color.white;
    }

    void FindBones()
    {
        if (skeleton == null || !skeleton.IsInitialized) return;

        // Bone ids collide across Meta's hand/body enums, so pick by index, not name.
        // OpenXR hand skeleton (26 bones): index tip 10, wrist 1, index knuckle 7, little knuckle 22.
        // Legacy hand skeleton (24 bones): index tip 20, wrist 0, index knuckle 6, little knuckle 16.
        bool openXR = skeleton.Bones.Count == 26;
        int tipIndex     = openXR ? 10 : 20;
        int wristIndex   = openXR ? 1  : 0;
        int indexKIndex  = openXR ? 7  : 6;
        int littleKIndex = openXR ? 22 : 16;

        if (tipIndex >= skeleton.Bones.Count) return;

        indexTip = skeleton.Bones[tipIndex].Transform;
        wristBone = skeleton.Bones[wristIndex].Transform;
        indexKnuckle = skeleton.Bones[indexKIndex].Transform;
        littleKnuckle = skeleton.Bones[littleKIndex].Transform;
        Debug.Log(name + ": index tip bound to bone " + tipIndex);
    }
}