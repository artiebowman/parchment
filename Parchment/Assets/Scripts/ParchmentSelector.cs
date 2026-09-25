using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

[DefaultExecutionOrder(-10)]   // before CubeRaySelector, so "pinch to continue" can't also select here
public class ParchmentSelector : MonoBehaviour
{
    public HandPointer[] hands;     // drag both hand objects here
    public ParchmentHover hover;    // drag TaskCube here
    public TrialManager trial;      // drag TaskCube here
    public bool pokeEnabled = true; // fingertip entering a bulb selects it; pinch always works regardless

    // True for the frame in which this selector sent a selection. CubeRaySelector yields that frame.
    public bool SelectedThisFrame { get; private set; }

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
                    trial.OnSphereSelected(bulb.id);
                    SelectedThisFrame = true;
                    sent = true;
                }
            }

            // Poke: selects the bulb the fingertip just entered. Skipped if the pinch already sent one this frame.
            if (pokeEnabled && !sent && hover.PokedThisFrame(hand))
            {
                Bulb bulb = hover.GetPoked(hand);
                if (bulb != null)
                {
                    trial.OnSphereSelected(bulb.id);
                    SelectedThisFrame = true;
                }
            }
        }

#if UNITY_EDITOR
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Bulb bulb = hover.MouseHovered;
            if (bulb != null)
            {
                trial.OnSphereSelected(bulb.id);
                SelectedThisFrame = true;
            }
        }
#endif
    }
}