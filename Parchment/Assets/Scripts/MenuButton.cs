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

    [System.NonSerialized] public HandPointer[] hands = new HandPointer[0];   // set by PalmMenu each frame

    private Renderer rend;
    private IToggleState state;
    private HandPointer heldBy;               // the hand that pressed and hasn't re-armed yet (null = armed)
    private float pressedAt = float.NegativeInfinity;
    private float flashUntil;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        state = stateSource as IToggleState;
        if (stateSource != null && state == null)
            Debug.LogWarning($"MenuButton {name}: stateSource does not implement IToggleState");
        if (rend != null) rend.material.color = RestColor();
    }

    void OnDisable()
    {
        heldBy = null;
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

        if (rend != null)
            rend.material.color = Time.time < flashUntil ? pressedColor : RestColor();
    }

    // What the button looks like when not being pressed.
    Color RestColor()
    {
        if (state == null) return idleColor;
        return state.IsOn ? onColor : offColor;
    }
}