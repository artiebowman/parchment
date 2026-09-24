using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

public class ParchmentSelector : MonoBehaviour
{
    public HandPointer[] hands;     // drag both hand objects here
    public ParchmentHover hover;    // drag TaskCube here
    public TrialManager trial;      // drag TaskCube here

    void Update()
    {
        if (hover == null || trial == null) return;
        if (trial.RunFinished) return;              // CubeRaySelector handles "pinch to continue"

        foreach (HandPointer hand in hands)
        {
            if (hand == null || !hand.ConfirmedThisFrame) continue;

            Bulb bulb = hover.GetHovered(hand);
            if (bulb != null)
            {
                trial.OnSphereSelected(bulb.id);
            }
        }

#if UNITY_EDITOR
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Bulb bulb = hover.MouseHovered;
            if (bulb != null)
            {
                trial.OnSphereSelected(bulb.id);
            }
        }
#endif
    }
}