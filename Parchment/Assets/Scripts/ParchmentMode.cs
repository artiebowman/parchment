using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

public class ParchmentMode : MonoBehaviour, IToggleState
{
    public GameObject parchment;        // drag Parchment here
    public ParchmentHover hover;        // drag TaskCube here
    public ParchmentSelector selector;  // drag TaskCube here
    public PalmMenu menu;               // drag the PalmMenu object here; unrolling waits until it closes

    public bool startUnrolled = true;

    public bool Unrolled { get; private set; }

    // What the sheet should be. Same as Unrolled unless an unroll is waiting for the menu to close.
    private bool wanted;

    // IToggleState: the button shows what was asked for, so it turns green the moment you press it.
    public bool IsOn => wanted;

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
        if (parchment != null) parchment.SetActive(value);
        if (hover != null) hover.enabled = value;
        if (selector != null) selector.enabled = value;
    }
}