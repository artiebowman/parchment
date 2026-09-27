using UnityEngine;

// Phantom: a gained ghost hand on the parchment sheet (extension of Parchment).
// Stub for now: holds the on/off state so the palm-menu button can wire up today.
// Behavior (gain, ghost hand, hand dimming, sheet clamp) lands in its own phase.
public class PhantomMode : MonoBehaviour, IToggleState
{
    public bool wanted = false;               // what the user asked for; survives parchment roll-up

    public bool IsOn => wanted;               // button shows green whenever wanted, even while rolled up

    public void Toggle()
    {
        wanted = !wanted;
    }
}