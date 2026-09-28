using UnityEngine;

// Phantom: a fingertip on the floating trackpad (PhantomPad) is mirrored corner-for-corner onto the bulb grid, always
// on the bulb plane, so sliding it through a bulb pokes it. A small ghost of the real hand is drawn where it lands,
// and the board goes dark underneath so your eyes stay on the pad.
public class PhantomMode : MonoBehaviour, IToggleState
{
    [System.Serializable]
    public class GhostHand
    {
        public HandPointer hand;               // Left or Right hand object
        public HandTint tint;                  // the HandTint on that same hand
        public SkinnedMeshRenderer handMesh;   // that hand's Skinned Mesh Renderer (the one HandTint uses)
        [System.NonSerialized] public Mesh baked;
        [System.NonSerialized] public bool shown;           // ParchmentHover sets this each frame: the phantom is on the sheet
        [System.NonSerialized] public Vector3 phantomTip;   // and where it is
    }

    public ParchmentMode mode;          // drag TaskCube here
    public Transform parchment;         // drag Parchment here; its origin is the sheet center, x/z are the in-plane axes
    public PhantomPad pad;              // drag Trackpad here
    public float boardHalf = 0.225f;    // half the bulb grid edge to edge: spacing 0.045 x 10 columns / 2. Pad corners land here.
    public Material ghostMaterial;      // drag GhostHandMat here
    public GhostHand[] hands = new GhostHand[2];

    public bool wanted;                 // the button state; only takes effect while the sheet is unrolled
    public float ghostScale = 0.3f;     // ghost hand size, 1 = life size; shrinks around its own fingertip
    public float ghostLift = 0.01f;     // ghost fingertip drawn this far above the bulb plane so it reads as touching, not buried

    [Header("Night over the board")]
    public float shadeAlpha = 0.3f;     // a black solid at this opacity sits just above the slab while Phantom is live
    public float shadeSize = 0.5f;      // in Sheet units; the slab is 0.5
    public float shadeLift = 0.011f;    // just above the slab's top face, below the bulbs

    public bool IsOn => wanted;
    public bool Active => wanted && mode != null && mode.Ready;

    public void Toggle() { wanted = !wanted; }

    // Sensitivity is the pad's size: smaller pad, same board. Settings buttons call this with +/- a centimeter or so.
    public void Nudge(float delta) { if (pad != null) pad.padWidth = Mathf.Clamp(pad.padWidth + delta, 0.05f, 0.5f); }

    // Pad spot (-1..1 each way) in, world point on the bulb grid out. Pad corners land on grid corners.
    public Vector3 OnBoard(Vector2 uv)
    {
        return parchment.TransformPoint(new Vector3(uv.x * boardHalf, 0f, uv.y * boardHalf));
    }

    // ParchmentHover reports each hand's phantom every frame, or that it has none.
    public void Report(HandPointer hand, bool shown, Vector3 phantomTip)
    {
        foreach (GhostHand g in hands)
            if (g != null && g.hand == hand) { g.shown = shown; g.phantomTip = phantomTip; }
    }

    private GameObject shade;
    private MaterialPropertyBlock mpb;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    void LateUpdate()
    {
        bool active = Active;
        Bulb.SetPhantomLook(active);   // bulbs go dark, target goes violet, hover shows nothing
        UpdateShade(active);
        foreach (GhostHand g in hands)
        {
            if (g == null) continue;
            if (g.tint != null) g.tint.phantom = active;   // real hand goes violet while Phantom is live

            bool show = active && g.shown && g.hand != null && g.hand.IsTracked && g.hand.HasIndexTip
                        && g.handMesh != null && ghostMaterial != null && parchment != null;
            if (show)
            {
                Transform t = g.handMesh.transform;
                Vector3 realTip = g.hand.IndexTip;
                Vector3 ghostTip = g.phantomTip + parchment.up * ghostLift;

                // Freeze this frame's hand pose into a plain mesh, then draw it shrunk around its index tip,
                // with that tip landing on the phantom point.
                if (g.baked == null) g.baked = new Mesh();
                g.handMesh.BakeMesh(g.baked, true);
                Vector3 tipLocal = Quaternion.Inverse(t.rotation) * (realTip - t.position);   // the tip's spot inside the baked mesh
                Matrix4x4 m = Matrix4x4.TRS(ghostTip, t.rotation, Vector3.one * ghostScale) * Matrix4x4.Translate(-tipLocal);
                Graphics.DrawMesh(g.baked, m, ghostMaterial, t.gameObject.layer);
            }

            g.shown = false;   // ParchmentHover sets it again next frame
        }
    }

    // The black solid: a see-through dark quad parented to the Sheet so it rolls and unrolls with it. Shown only while live.
    void UpdateShade(bool active)
    {
        if (mode == null || mode.sheet == null || ghostMaterial == null) return;
        if (shade == null)
        {
            shade = GameObject.CreatePrimitive(PrimitiveType.Quad);
            shade.name = "NightShade";
            Destroy(shade.GetComponent<Collider>());
            shade.transform.SetParent(mode.sheet, false);
            shade.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);   // lie flat, face up
            shade.transform.localScale = new Vector3(shadeSize, shadeSize, 1f);
            Renderer r = shade.GetComponent<Renderer>();
            r.sharedMaterial = ghostMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        shade.transform.localPosition = new Vector3(0f, shadeLift, 0f);
        Renderer sr = shade.GetComponent<Renderer>();
        if (mpb == null) mpb = new MaterialPropertyBlock();
        sr.GetPropertyBlock(mpb);
        mpb.SetColor(BaseColorId, new Color(0f, 0f, 0f, shadeAlpha));
        sr.SetPropertyBlock(mpb);
        shade.SetActive(active);
    }
}
