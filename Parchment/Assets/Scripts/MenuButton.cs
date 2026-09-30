using UnityEngine;
using UnityEngine.Events;
using TMPro;

// Anything a MenuButton can display on/off state for.
public interface IToggleState
{
    bool IsOn { get; }
}

public class MenuButton : MonoBehaviour
{
    // A button is a fingertip inside a bubble. pressRadius in, releaseRadius out, plus rearmSeconds: hysteresis in space, same shape as the pinch.
    // Optional layers: enableSource grays it out, holdSeconds fills a ring before firing, repeatAfter auto-repeats. onPress is the Inspector wiring to the action.
    public float pressRadius = 0.025f;       // fingertip within this of the button center = pressing
    public float releaseRadius = 0.05f;      // once pressed, the tip must get this far away to re-arm (punch-through stays inside)
    public float rearmSeconds = 0.3f;        // and this long must have passed since the press
    public float repeatAfter = 0f;           // 0 = no auto-repeat; otherwise keeping the tip in the bubble fires again after this long
    public float repeatEvery = 0.25f;        // and then every this often until the tip leaves
    public Color idleColor = new Color(0.25f, 0.25f, 0.28f);   // used only when there is no stateSource
    public Color onColor = new Color(0.2f, 0.8f, 0.3f);        // feature on = solid green
    public Color offColor = new Color(0.16f, 0.16f, 0.18f);    // feature off = dim gray
    public Color pressedColor = new Color(0.95f, 0.8f, 0.2f);
    public Color disabledColor = new Color(0.11f, 0.11f, 0.12f); // enableSource says off: drawn like this, ignores fingertips
    public float flashSeconds = 0.15f;
    public UnityEvent onPress;                // wire the action here in the inspector
    public MonoBehaviour stateSource;         // optional: drag a component that implements IToggleState (green/gray)
    public MonoBehaviour enableSource;        // optional: IToggleState; while it's off this button is grayed out and can't be pressed
    public Renderer tintTarget;               // optional: what gets colored. Empty = this object's own renderer (the tile). Later: the Icon child.

    [Header("Hold to press")]
    public float holdSeconds = 0f;            // 0 = fires on touch; otherwise the tip must stay in the bubble this long
    public bool holdOnlyWhenOn = false;       // with a stateSource: only the on -> off direction needs the hold
    public Color ringColor = Color.white;
    public float ringRadius = 0.02f;
    public float ringWidth = 0.003f;
    public float ringLift = 0.01f;            // how far in front of the tile the ring floats (flip the sign if it draws behind)

    [Header("Label")]
    public TMP_Text label;                    // optional: swaps between onText / offText with the stateSource
    public string onText;
    public string offText;

    [System.NonSerialized] public HandPointer[] hands = new HandPointer[0];   // set by PalmMenu each frame

    private Renderer rend;
    private IToggleState state;
    private IToggleState enabler;
    private HandPointer heldBy;               // the hand that pressed and hasn't re-armed yet (null = armed)
    private float pressedAt = float.NegativeInfinity;
    private float flashUntil;
    private float nextRepeatAt = float.PositiveInfinity;
    private float holdProgress;               // 0..1, fills while a tip sits in the bubble, drains when it leaves
    private LineRenderer ring;
    private const int RingSegments = 32;

    void Awake()
    {
        rend = tintTarget != null ? tintTarget : GetComponent<Renderer>();
        state = stateSource as IToggleState;
        enabler = enableSource as IToggleState;
        if (stateSource != null && state == null)
            Debug.LogWarning($"MenuButton {name}: stateSource does not implement IToggleState");
        if (enableSource != null && enabler == null)
            Debug.LogWarning($"MenuButton {name}: enableSource does not implement IToggleState");
        Tint(RestColor());
        UpdateLabel();
    }

    void OnDisable()
    {
        heldBy = null;
        holdProgress = 0f;
        if (ring != null) ring.enabled = false;
    }

    // Treat this button as already pressed by `hand`: it won't fire until that tip has backed off
    // past releaseRadius and rearmSeconds have passed. Used when a page appears under a fingertip.
    public void HoldUntilClear(HandPointer hand)
    {
        heldBy = hand;
        pressedAt = Time.time;
    }

    bool Enabled => enabler == null || enabler.IsOn;
    bool NeedsHold => holdSeconds > 0f && (!holdOnlyWhenOn || state == null || state.IsOn);

    void Update()
    {
        bool enabledNow = Enabled;

        // Re-arm: the holding hand has backed off far enough (or vanished) and enough time has passed.
        if (heldBy != null && Time.time - pressedAt >= rearmSeconds)
        {
            bool gone = !heldBy.IsTracked || !heldBy.HasIndexTip ||
                        Vector3.Distance(heldBy.IndexTip, transform.position) > releaseRadius;
            if (gone) heldBy = null;
        }

        // Auto-repeat: still held in the bubble past repeatAfter, so fire again on a cadence.
        if (heldBy != null && enabledNow && Time.time >= nextRepeatAt && heldBy.IsTracked && heldBy.HasIndexTip
            && Vector3.Distance(heldBy.IndexTip, transform.position) <= pressRadius)
        {
            nextRepeatAt = Time.time + repeatEvery;
            flashUntil = Time.time + flashSeconds;
            if (AudioFeedback.Instance != null) AudioFeedback.Instance.PlayBlip();
            onPress.Invoke();
        }

        // Which hand, if any, has its tip in the bubble while the button is armed and enabled.
        HandPointer inBubble = null;
        if (enabledNow && heldBy == null)
        {
            foreach (HandPointer hand in hands)
            {
                if (hand == null || !hand.IsTracked || !hand.HasIndexTip) continue;
                if (Vector3.Distance(hand.IndexTip, transform.position) <= pressRadius) { inBubble = hand; break; }
            }
        }

        if (inBubble != null && !NeedsHold)
        {
            Fire(inBubble);
        }
        else if (inBubble != null)
        {
            // Hold: the ring fills while the tip stays put; full ring = press.
            holdProgress = Mathf.Min(1f, holdProgress + Time.deltaTime / holdSeconds);
            if (holdProgress >= 1f)
            {
                holdProgress = 0f;
                Fire(inBubble);
            }
        }
        else if (holdSeconds > 0f)
        {
            holdProgress = Mathf.Max(0f, holdProgress - Time.deltaTime / holdSeconds);   // left early: drain back
        }

        UpdateRing();
        UpdateLabel();
        Tint(!enabledNow ? disabledColor : (Time.time < flashUntil ? pressedColor : RestColor()));
    }

    void Fire(HandPointer hand)
    {
        heldBy = hand;
        pressedAt = Time.time;
        nextRepeatAt = repeatAfter > 0f ? Time.time + repeatAfter : float.PositiveInfinity;
        flashUntil = Time.time + flashSeconds;
        if (AudioFeedback.Instance != null) AudioFeedback.Instance.PlayBlip();
        onPress.Invoke();
    }

    // The hold ring: an arc around the tile drawn in world space, so the tile's flat scale can't squash it.
    void UpdateRing()
    {
        bool show = holdSeconds > 0f && holdProgress > 0f;
        if (!show) { if (ring != null) ring.enabled = false; return; }
        if (ring == null)
        {
            GameObject go = new GameObject("HoldRing");
            go.transform.SetParent(transform, false);
            ring = go.AddComponent<LineRenderer>();
            ring.useWorldSpace = true;
            Material m = rend != null ? new Material(rend.sharedMaterial) : new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            m.color = ringColor;
            ring.material = m;
            ring.startWidth = ringWidth;
            ring.endWidth = ringWidth;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        int n = Mathf.Max(2, Mathf.CeilToInt(RingSegments * holdProgress) + 1);
        ring.positionCount = n;
        Vector3 c = transform.position - transform.forward * ringLift;
        for (int i = 0; i < n; i++)
        {
            float a = (i / (float)(n - 1)) * holdProgress * Mathf.PI * 2f;   // from the top, clockwise
            ring.SetPosition(i, c + transform.up * Mathf.Cos(a) * ringRadius + transform.right * Mathf.Sin(a) * ringRadius);
        }
        ring.enabled = true;
    }

    void UpdateLabel()
    {
        if (label == null || state == null) return;
        string want = state.IsOn ? onText : offText;
        if (!string.IsNullOrEmpty(want) && label.text != want) label.text = want;
    }

    // What the button looks like when not being pressed.
    Color RestColor()
    {
        if (state == null) return idleColor;
        return state.IsOn ? onColor : offColor;
    }

    // Color whatever we're tinting. A SpriteRenderer (the future Icon) tints through .color;
    // a mesh (today's tile) tints through its material.
    void Tint(Color c)
    {
        if (rend == null) return;
        if (rend is SpriteRenderer sprite) sprite.color = c;
        else rend.material.color = c;
    }
}
