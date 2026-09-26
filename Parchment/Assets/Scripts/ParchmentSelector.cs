using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

[DefaultExecutionOrder(-10)]   // before CubeRaySelector, so "pinch to continue" can't also select here
public class ParchmentSelector : MonoBehaviour, IToggleState
{
    public HandPointer[] hands;     // drag both hand objects here
    public ParchmentHover hover;    // drag TaskCube here
    public TrialManager trial;      // drag TaskCube here
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

        foreach (HandPointer hand in hands)
        {
            if (hand == null) continue;

            bool sent = false;

            // Pinch: selects whatever this hand is hovering.
            if (hand.ConfirmedThisFrame)
            {
                Bulb bulb = hover.GetHovered(hand);
                if (bulb != null)
                {
                    Send(bulb);
                    sent = true;
                }
            }

            // Poke: selects the bulb the fingertip just entered. Skipped if the pinch already sent one this frame.
            if (pokeEnabled && !sent && hover.PokedThisFrame(hand))
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