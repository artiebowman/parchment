using UnityEngine;
using UnityEngine.Events;

public class MenuButton : MonoBehaviour
{
    public float pressRadius = 0.025f;       // fingertip within this of the button center = pressing
    public Color idleColor = new Color(0.25f, 0.25f, 0.28f);
    public Color pressedColor = new Color(0.95f, 0.8f, 0.2f);
    public float flashSeconds = 0.15f;
    public UnityEvent onPress;                // wire the action here in the inspector

    [System.NonSerialized] public HandPointer[] hands = new HandPointer[0];   // set by PalmMenu each frame

    private Renderer rend;
    private HandPointer insideHand;           // the hand currently pressing (null = none)
    private float flashUntil;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        if (rend != null) rend.material.color = idleColor;
    }

    void OnDisable()
    {
        insideHand = null;
    }

    void Update()
    {
        HandPointer nowInside = null;

        foreach (HandPointer hand in hands)
        {
            if (hand == null || !hand.IsTracked || !hand.HasIndexTip) continue;
            if (Vector3.Distance(hand.IndexTip, transform.position) <= pressRadius)
            {
                nowInside = hand;
                break;
            }
        }

        // Entering counts once; you have to leave and come back to press again.
        if (nowInside != null && nowInside != insideHand)
        {
            flashUntil = Time.time + flashSeconds;
            onPress.Invoke();
        }
        insideHand = nowInside;

        if (rend != null)
            rend.material.color = Time.time < flashUntil ? pressedColor : idleColor;
    }
}