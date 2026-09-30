using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

[DefaultExecutionOrder(-10)]   // before CubeRaySelector, so "pinch to continue" can't also select here
public class ParchmentSelector : MonoBehaviour, IToggleState
{
    // The only place a board touch becomes a selection. Pinch selects the hovered bulb; poke selects the bulb just entered.
    // Ladder rung 4: Phantom live forces poke on and ignores pinch. Every selection goes through Send, once.
    public HandPointer[] hands;     // drag both hand objects here
    public ParchmentHover hover;    // drag TaskCube here
    public TrialManager trial;      // drag TaskCube here
    public PhantomMode phantom;     // drag TaskCube here; while Phantom is live, poke is forced on and pinch is ignored
    public bool pokeEnabled = true; // fingertip entering a bulb selects it; pinch always works regardless

    // True for the frame in which this selector sent a selection. CubeRaySelector yields that frame.
    public bool SelectedThisFrame { get; private set; }

    // IToggleState: the menu button shows green when poke is enabled.
    public bool IsOn => pokeEnabled;

    // Called by the palm menu's Poke button.
    public void TogglePoke()
    {
        pokeEnabled = !pokeEnabled;
    }

    void Update()
    {
        SelectedThisFrame = false;

        if (hover == null || trial == null) return;
        if (trial.RunFinished) return;              // CubeRaySelector handles "pinch to continue"
        bool phantomLive = phantom != null && phantom.Active;

        foreach (HandPointer hand in hands)
        {
            if (hand == null) continue;

            bool sent = false;

            // Pinch: selects whatever this hand is hovering.
            if (hand.ConfirmedThisFrame && !phantomLive)
            {
                Bulb bulb = hover.GetHovered(hand);
                if (bulb != null)
                {
                    Send(bulb);
                    sent = true;
                }
            }

            // Poke: selects the bulb the fingertip just entered. Skipped if the pinch already sent one this frame.
            if ((pokeEnabled || phantomLive) && !sent && hover.PokedThisFrame(hand))
            {
                Bulb bulb = hover.GetPoked(hand);
                if (bulb != null) Send(bulb);
            }
        }

#if UNITY_EDITOR
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Bulb bulb = hover.MouseHovered;
            if (bulb != null) Send(bulb);
        }
#endif
    }

    // One place every selection goes through. Sound comes from hover (soft) and TrialManager (ding on correct).
    void Send(Bulb bulb)
    {
        trial.OnSphereSelected(bulb.id);
        SelectedThisFrame = true;
    }
}