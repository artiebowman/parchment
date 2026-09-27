using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

public class ParchmentMode : MonoBehaviour, IToggleState
{
    public enum BulbOrder { Id, LeftToRight, CenterOut }

    public GameObject parchment;        // drag Parchment here
    public ParchmentHover hover;        // drag TaskCube here
    public ParchmentSelector selector;  // drag TaskCube here
    public PalmMenu menu;               // drag the PalmMenu object here; unrolling waits until it closes

    [Header("Unroll animation")]
    public Transform sheet;             // drag Parchment > Sheet here: the pivot null holding Slab, Bulbs and the reticles
    public Transform slab;              // drag Parchment > Sheet > Slab here (a Cube; it opens along its local X)
    public Transform bulbsRoot;         // drag Parchment > Sheet > Bulbs here (bulbs are spawned under it at runtime)
    public float unrollSeconds = 0.5f;  // progress runs 0 -> 1 over this long
    public float rollUpSeconds = 0.35f; // and 1 -> 0 over this long
    // One progress value drives every curve. Each curve's Y axis is in real units, so shape them like a graph editor.
    public AnimationCurve widthCurve = new AnimationCurve(new Keyframe(0f, 0f, 0f, 2f), new Keyframe(1f, 1f, 0f, 0f));            // slab width, 0..1, ease-out
    public AnimationCurve scaleCurve = new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.75f, 1.06f), new Keyframe(1f, 1f)); // Sheet scale: small, overshoot, settle
    public AnimationCurve pitchCurve = new AnimationCurve(new Keyframe(0f, 35f), new Keyframe(0.85f, -3f), new Keyframe(1f, 0f));   // Sheet pitch, degrees: top edge down, swing past, settle
    public AnimationCurve riseCurve = new AnimationCurve(new Keyframe(0f, -0.30f), new Keyframe(0.8f, 0.02f), new Keyframe(1f, 0f)); // Sheet local Y, meters: up from the hip, slight overshoot, settle
    public AnimationCurve approachCurve = new AnimationCurve(new Keyframe(0f, -0.10f), new Keyframe(1f, 0f));                        // Sheet local Z, meters: starts near the body, moves out to rest
    public AnimationCurve alphaCurve = new AnimationCurve(new Keyframe(0f, 0f, 0f, 2f), new Keyframe(1f, 1f, 0f, 0f));            // slab alpha 0..1 (SlabMat must be Transparent)

    [Header("Bulb arrival")]
    [Range(0f, 1f)] public float bulbsStartAt = 0.8f;   // bulbs begin at this progress, while the sheet is still landing (overlapping action)
    public BulbOrder bulbOrder = BulbOrder.LeftToRight;
    public float bulbsInSeconds = 0.65f; // stagger across the whole grid
    public float popSeconds = 0.08f;     // each bulb's own grow-in
    public AnimationCurve popCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.6f, 1.12f), new Keyframe(1f, 1f));   // slight overshoot
    public bool bulbsRide = false;      // set by SyncSweep once synced: bulbs are part of the sheet, revealed by the opening edge, no stagger

    public bool startUnrolled = true;

    public bool Unrolled { get; private set; }

    // True once an unroll has fully landed (sheet open, bulbs in). Hover/selection (and Phantom later) only run when this is set.
    public bool Ready { get; private set; }

    // Fired as each bulb pops in. The first-unroll sync sweep hooks this to flash the matching sphere.
    public event System.Action<Transform> BulbShown;

    // What the sheet should be. Same as Unrolled unless an unroll is waiting for the menu to close.
    private bool wanted;

    // IToggleState: the button shows what was asked for, so it turns green the moment you press it.
    public bool IsOn => wanted;

    // Animation state
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private Vector3 slabFullScale;
    private Renderer slabRenderer;
    private MaterialPropertyBlock slabBlock;
    private Color slabBaseColor = Color.white;
    private float progress;             // 0 = closed, 1 = open; the master value every curve reads
    private bool sheetMoving;
    private Coroutine anim;
    private Coroutine sheetAnim;

    private class BulbView
    {
        public Transform t;
        public Renderer[] rends;
        public Vector3 fullScale;
        public bool shown;
        public float key;               // sort value for the arrival order
        public float x;                 // position across the sheet, in Sheet space (for reveal-by-width)
    }
    private readonly List<BulbView> bulbs = new List<BulbView>();

    void Awake()
    {
        if (slab == null) return;
        slabFullScale = slab.localScale;
        slabRenderer = slab.GetComponent<Renderer>();
        slabBlock = new MaterialPropertyBlock();
        if (slabRenderer != null && slabRenderer.sharedMaterial != null && slabRenderer.sharedMaterial.HasProperty(BaseColorId))
            slabBaseColor = slabRenderer.sharedMaterial.GetColor(BaseColorId);
    }

    void Start()
    {
        wanted = startUnrolled;
        SetUnrolled(startUnrolled);
    }

    void Update()
    {
#if UNITY_EDITOR
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            Toggle();
        }
#endif

        if (wanted == Unrolled) return;

        // Rolling up is immediate. Unrolling waits until the palm menu is out of the way.
        bool menuOpen = menu != null && menu.IsShown;
        if (!wanted || !menuOpen) SetUnrolled(wanted);
    }

    // Called by the palm menu's Parchment button (and P in the editor).
    public void Toggle()
    {
        wanted = !wanted;
    }

    public void SetUnrolled(bool value)
    {
        wanted = value;
        Unrolled = value;
        Ready = false;

        // Interaction is off while the sheet moves; the unroll turns it back on when it lands.
        if (hover != null) hover.enabled = false;
        if (selector != null) selector.enabled = false;

        if (anim != null) StopCoroutine(anim);
        if (sheetAnim != null) StopCoroutine(sheetAnim);
        sheetMoving = false;

        // Nothing to animate: fall back to the old instant switch.
        if (slab == null || sheet == null || parchment == null)
        {
            if (parchment != null) parchment.SetActive(value);
            progress = value ? 1f : 0f;
            Finish(value);
            return;
        }

        if (value) parchment.SetActive(true);
        anim = StartCoroutine(value ? Unroll() : RollUp());
    }

    // Sheet opens. Before the sync, bulbs pop in staggered once progress passes bulbsStartAt; after it, they ride the sheet.
    IEnumerator Unroll()
    {
        GatherBulbs(true);
        foreach (BulbView b in bulbs) SetShown(b, false);
        if (bulbsRide)
        {
            yield return MoveSheet(1f, unrollSeconds);        // RevealByWidth shows them as the edge passes
        }
        else
        {
            sheetAnim = StartCoroutine(MoveSheet(1f, unrollSeconds));
            while (sheetMoving && progress < bulbsStartAt) yield return null;
            yield return BulbsIn();
            while (sheetMoving) yield return null;
        }
        anim = null;
        Finish(true);
    }

    // Bulbs leave first (pre-sync), then the sheet closes; after the sync the closing edge hides them.
    IEnumerator RollUp()
    {
        GatherBulbs(true);
        if (!bulbsRide) yield return BulbsOut();
        yield return MoveSheet(0f, rollUpSeconds);
        anim = null;
        Finish(false);
    }

    void Finish(bool unrolled)
    {
        foreach (BulbView b in bulbs) SetShown(b, true);
        if (unrolled)
        {
            if (hover != null) hover.enabled = true;
            if (selector != null) selector.enabled = true;
            Ready = true;
        }
        else
        {
            // Leave everything at rest pose while hidden, so nothing depends on where an animation stopped.
            if (slab != null) slab.localScale = slabFullScale;
            ApplySlabAlpha(1f);
            if (sheet != null)
            {
                sheet.localPosition = Vector3.zero;
                sheet.localScale = Vector3.one;
                sheet.localRotation = Quaternion.identity;
            }
            if (parchment != null) parchment.SetActive(false);
        }
    }

    // Runs progress from wherever it is to the target, so a toggle mid-move just reverses smoothly.
    IEnumerator MoveSheet(float target, float fullSeconds)
    {
        sheetMoving = true;
        float start = progress;
        float seconds = fullSeconds * Mathf.Abs(target - start);
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            progress = Mathf.Lerp(start, target, Mathf.Clamp01(t / seconds));
            ApplyPose();
            yield return null;
        }
        progress = target;
        ApplyPose();
        sheetMoving = false;
    }

    // Every animated property reads the same progress through its own curve.
    void ApplyPose()
    {
        float width = Mathf.Max(widthCurve.Evaluate(progress), 0.001f);
        slab.localScale = new Vector3(slabFullScale.x * width, slabFullScale.y, slabFullScale.z);
        sheet.localScale = Vector3.one * Mathf.Max(scaleCurve.Evaluate(progress), 0.001f);
        sheet.localRotation = Quaternion.Euler(pitchCurve.Evaluate(progress), 0f, 0f);
        sheet.localPosition = new Vector3(0f, riseCurve.Evaluate(progress), approachCurve.Evaluate(progress));
        ApplySlabAlpha(alphaCurve.Evaluate(progress));
        if (bulbsRide) RevealByWidth(width);
    }

    // After the sync, bulbs are printed on the sheet: visible exactly when the open part of the slab covers them.
    void RevealByWidth(float width)
    {
        float half = slabFullScale.x * 0.5f * width;
        float shrink = Mathf.Clamp01(alphaCurve.Evaluate(progress));   // bulbs are opaque, so they fade with the sheet by shrinking
        foreach (BulbView b in bulbs)
        {
            if (b.t == null) continue;
            bool vis = Mathf.Abs(b.x) <= half + 0.001f;
            if (vis != b.shown) SetShown(b, vis);
            if (vis) b.t.localScale = b.fullScale * shrink;
        }
    }

    // Alpha goes through a property block so the shared material is never edited.
    void ApplySlabAlpha(float a)
    {
        if (slabRenderer == null) return;
        Color c = slabBaseColor;
        c.a = Mathf.Clamp01(a);
        slabRenderer.GetPropertyBlock(slabBlock);
        slabBlock.SetColor(BaseColorId, c);
        slabRenderer.SetPropertyBlock(slabBlock);
    }

    // Each bulb starts its pop at its share of bulbsInSeconds, by arrival order; ties (a whole column) start together.
    IEnumerator BulbsIn()
    {
        float t = 0f;
        float total = bulbsInSeconds + popSeconds;
        while (t < total)
        {
            t += Time.deltaTime;
            if (bulbsRoot != null && bulbsRoot.childCount != bulbs.Count) GatherBulbs(false);   // spawned after we started
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (BulbView b in bulbs) { lo = Mathf.Min(lo, b.key); hi = Mathf.Max(hi, b.key); }
            float span = Mathf.Max(hi - lo, 0.0001f);
            foreach (BulbView b in bulbs)
            {
                if (b.t == null) continue;
                float k = t - bulbsInSeconds * (b.key - lo) / span;
                if (k < 0f) continue;
                if (!b.shown)
                {
                    SetShown(b, true);
                    BulbShown?.Invoke(b.t);
                }
                b.t.localScale = b.fullScale * popCurve.Evaluate(popSeconds > 0f ? Mathf.Clamp01(k / popSeconds) : 1f);
            }
            yield return null;
        }
    }

    // Every visible bulb shrinks away together, then they're all hidden.
    IEnumerator BulbsOut()
    {
        float t = 0f;
        while (t < popSeconds)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(t / popSeconds);
            foreach (BulbView b in bulbs)
                if (b.shown && b.t != null) b.t.localScale = b.fullScale * k * k;
            yield return null;
        }
        foreach (BulbView b in bulbs) SetShown(b, false);
    }

    // Show or hide a bulb; either way it goes back to full size so nothing is ever left mid-pop.
    void SetShown(BulbView b, bool on)
    {
        b.shown = on;
        if (b.t == null) return;
        foreach (Renderer r in b.rends) if (r != null) r.enabled = on;
        b.t.localScale = b.fullScale;
    }

    // Snapshot the bulbs under bulbsRoot (they're spawned at runtime). Bulbs we already track keep their state;
    // new ones are treated as visible, or hidden right away when newAreShown is false.
    void GatherBulbs(bool newAreShown)
    {
        if (bulbsRoot == null) return;
        var known = new Dictionary<Transform, BulbView>();
        foreach (BulbView b in bulbs) if (b.t != null) known[b.t] = b;
        bulbs.Clear();
        int i = 0;
        foreach (Transform child in bulbsRoot)
        {
            BulbView b;
            if (!known.TryGetValue(child, out b))
            {
                b = new BulbView();
                b.t = child;
                b.rends = child.GetComponentsInChildren<Renderer>(true);
                b.fullScale = child.localScale;
                b.shown = true;
                if (!newAreShown) SetShown(b, false);
            }
            b.x = sheet.InverseTransformPoint(child.position).x;
            b.key = OrderKey(child, i);
            bulbs.Add(b);
            i++;
        }
    }

    // What "arrival order" means for one bulb. Positions are read in Sheet space, so the sheet's own scale and tilt don't matter.
    float OrderKey(Transform bulb, int index)
    {
        switch (bulbOrder)
        {
            case BulbOrder.LeftToRight: return sheet.InverseTransformPoint(bulb.position).x;
            case BulbOrder.CenterOut:   return Mathf.Abs(sheet.InverseTransformPoint(bulb.position).x);
            default:                    return index;
        }
    }
}
