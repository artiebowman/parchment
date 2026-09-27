using UnityEngine;
using UnityEngine.Events;

// Anything a MenuButton can display on/off state for.
public interface IToggleState
{
    bool IsOn { get; }
}

public class MenuButton : MonoBehaviour
{
    public float pressRadius = 0.025f;       // fingertip within this of the button center = pressing
    public float releaseRadius = 0.05f;      // once pressed, the tip must get this far away to re-arm (punch-through stays inside)
    public float rearmSeconds = 0.3f;        // and this long must have passed since the press
    public Color idleColor = new Color(0.25f, 0.25f, 0.28f);   // used only when there is no stateSource
    public Color onColor = new Color(0.2f, 0.8f, 0.3f);        // feature on = solid green
    public Color offColor = new Color(0.16f, 0.16f, 0.18f);    // feature off = dim gray
    public Color pressedColor = new Color(0.95f, 0.8f, 0.2f);
    public float flashSeconds = 0.15f;
    public UnityEvent onPress;                // wire the action here in the inspector
    public MonoBehaviour stateSource;         // optional: drag a component that implements IToggleState
    public Renderer tintTarget;               // optional: what gets colored. Empty = this object's own renderer (the tile). Later: the Icon child.

    [System.NonSerialized] public HandPointer[] hands = new HandPointer[0];   // set by PalmMenu each frame

    private Renderer rend;
    private IToggleState state;
    private HandPointer heldBy;               // the hand that pressed and hasn't re-armed yet (null = armed)
    private float pressedAt = float.NegativeInfinity;
    private float flashUntil;

    void Awake()
    {
        rend = tintTarget != null ? tintTarget : GetComponent<Renderer>();
        state = stateSource as IToggleState;
        if (stateSource != null && state == null)
            Debug.LogWarning($"MenuButton {name}: stateSource does not implement IToggleState");
        Tint(RestColor());
    }

    void OnDisable()
    {
        heldBy = null;
    }

    // Treat this button as already pressed by `hand`: it won't fire until that tip has backed off
    // past releaseRadius and rearmSeconds have passed. Used when a page appears under a fingertip.
    public void HoldUntilClear(HandPointer hand)
    {
        heldBy = hand;
        pressedAt = Time.time;
    }

    void Update()
    {
        // Re-arm: the holding hand has backed off far enough (or vanished) and enough time has passed.
        if (heldBy != null && Time.time - pressedAt >= rearmSeconds)
        {
            bool gone = !heldBy.IsTracked || !heldBy.HasIndexTip ||
                        Vector3.Distance(heldBy.IndexTip, transform.position) > releaseRadius;
            if (gone) heldBy = null;
        }

        // Armed: any tip entering the press bubble fires once.
        if (heldBy == null)
        {
            foreach (HandPointer hand in hands)
            {
                if (hand == null || !hand.IsTracked || !hand.HasIndexTip) continue;
                if (Vector3.Distance(hand.IndexTip, transform.position) <= pressRadius)
                {
                    heldBy = hand;
                    pressedAt = Time.time;
                    flashUntil = Time.time + flashSeconds;
                    if (AudioFeedback.Instance != null) AudioFeedback.Instance.PlayBlip();
                    onPress.Invoke();
                    break;
                }
            }
        }

        Tint(Time.time < flashUntil ? pressedColor : RestColor());
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