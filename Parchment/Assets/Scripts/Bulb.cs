using System.Collections.Generic;
using UnityEngine;

public class Bulb : MonoBehaviour
{
    public enum State { Idle, Target, Hover, HoverTarget }

    public int id;
    public State state = State.Idle;

    public Color idleColor = new Color(0.85f, 0.85f, 0.85f);
    public Color targetColor = new Color(0.2f, 0.9f, 0.3f);
    public Color hoverColor = new Color(1f, 0.85f, 0.2f);
    public Color hoverTargetColor = new Color(0.5f, 1f, 0.5f);

    [Header("Phantom look")]
    public Color phantomIdleColor = new Color(0.3f, 0.3f, 0.34f);     // night over the board: everything dims
    public Color phantomTargetColor = new Color(0.62f, 0.45f, 0.9f);  // and the target goes violet; hover shows nothing

    public bool IsHovered => state == State.Hover || state == State.HoverTarget;

    // One switch for every bulb: PhantomMode flips it, and every bulb repaints at once.
    private static readonly List<Bulb> all = new List<Bulb>();
    public static bool PhantomLook { get; private set; }
    public static void SetPhantomLook(bool on)
    {
        if (on == PhantomLook) return;
        PhantomLook = on;
        foreach (Bulb b in all) b.Apply();
    }

    private Renderer rend;
    private bool hasOverride;         // an override color wins over the state color until cleared (used by the sync sweep)
    private Color overrideColor;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        Apply();
    }

    void OnEnable() { all.Add(this); }
    void OnDisable() { all.Remove(this); }

    public void SetState(State s)
    {
        if (state == s) return;
        state = s;
        Apply();
    }

    // Paint this bulb a color regardless of state. State changes still happen underneath and show again on ClearOverride.
    public void SetOverride(Color c)
    {
        hasOverride = true;
        overrideColor = c;
        Apply();
    }

    public void ClearOverride()
    {
        if (!hasOverride) return;
        hasOverride = false;
        Apply();
    }

    private void Apply()
    {
        if (rend == null) return;

        if (hasOverride)
        {
            rend.material.color = overrideColor;
            return;
        }

        if (PhantomLook)
        {
            bool isTarget = state == State.Target || state == State.HoverTarget;
            rend.material.color = isTarget ? phantomTargetColor : phantomIdleColor;
            return;
        }

        switch (state)
        {
            case State.Target:      rend.material.color = targetColor;      break;
            case State.Hover:       rend.material.color = hoverColor;       break;
            case State.HoverTarget: rend.material.color = hoverTargetColor; break;
            default:                rend.material.color = idleColor;        break;
        }
    }
}
