using UnityEngine;

[DefaultExecutionOrder(-90)]   // after HandPointer (-100) so PalmUp is fresh
public class HandTint : MonoBehaviour
{
    public HandPointer pointer;        // leave empty; found on this object
    public Renderer handRenderer;      // drag this hand object here; picks its Skinned Mesh Renderer
    public Color idleColor = Color.white;
    public Color palmUpColor = new Color(0.25f, 0.5f, 1f);
    public Color phantomColor = new Color(0.55f, 0.35f, 0.95f);   // real hand while Phantom is live
    [System.NonSerialized] public bool phantom;                     // set by PhantomMode every frame
    public float blendSpeed = 12f;

    private OVRSkeleton skeleton;
    private Transform wrist;
    private MaterialPropertyBlock block;
    private Color current;

    private static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");
    private static readonly int WristPos = Shader.PropertyToID("_WristPos");
    private static readonly int ForearmDir = Shader.PropertyToID("_ForearmDir");

    void Awake()
    {
        if (pointer == null) pointer = GetComponent<HandPointer>();
        skeleton = GetComponent<OVRSkeleton>();
        block = new MaterialPropertyBlock();
        current = idleColor;
    }

    void LateUpdate()
    {
        if (handRenderer == null || pointer == null) return;

        if (wrist == null && skeleton != null && skeleton.IsInitialized && skeleton.Bones.Count > 1)
            wrist = skeleton.Bones[skeleton.Bones.Count == 26 ? 1 : 0].Transform;

        Color target = pointer.PalmUp ? palmUpColor : (phantom ? phantomColor : idleColor);
        float t = 1f - Mathf.Exp(-blendSpeed * Time.deltaTime);
        Color keepAlpha = current;
        current = Color.Lerp(current, target, t);
        current.a = target.a;

        handRenderer.GetPropertyBlock(block);
        block.SetColor(OutlineColor, current);

        if (wrist != null && pointer.HasPalm)
        {
            Vector3 forearm = (wrist.position - pointer.PalmCenter).normalized;   // wrist toward elbow
            block.SetVector(WristPos, wrist.position);
            block.SetVector(ForearmDir, forearm);
        }

        handRenderer.SetPropertyBlock(block);
    }
}