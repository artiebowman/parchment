using UnityEngine;

public class PalmMenu : MonoBehaviour
{
    public HandPointer menuHand;    // drag the LEFT hand here
    public HandPointer pressHand;   // drag the RIGHT hand here
    public Transform head;          // drag CenterEyeAnchor here
    public GameObject panel;        // drag the PalmMenu object (with the buttons under it)

    public float aboveDistance = 0.10f;     // how far above the palm the panel floats
    public float minBelowEyes = 0.10f;      // palm must be at least this far below eye height
    public float maxReach = 0.75f;          // palm must be within this distance of the head
    public float showDelay = 0.25f;         // palm must be up this long before the menu appears
    public float hideDelay = 0.40f;         // and down this long before it goes away
    public float confirmGrace = 0.3f;       // pinches stay ignored this long after the palm drops / menu closes
    public float followSpeed = 10f;
    [Range(0f, 1f)] public float palmToFaceThreshold = 0.5f;   // palm turned this much toward the head hides its beam

    // True while the panel is visible. Others (ParchmentMode) defer actions until this drops.
    public bool IsShown => shown;

    private float upSince = -1f;
    private float downSince = -1f;
    private bool shown;
    private MenuButton[] buttons = new MenuButton[0];

    void Start()
    {
        if (panel != null)
        {
            buttons = panel.GetComponentsInChildren<MenuButton>(true);
            panel.SetActive(false);
        }
    }

    void Update()
    {
        if (menuHand == null || panel == null) return;

        Transform h = head;
        if (h == null && Camera.main != null) h = Camera.main.transform;
        if (h == null) return;

        // A palm turned toward your face is never pointing at anything: no beam from that hand.
        HidePalmFacingBeam(menuHand, h);
        HidePalmFacingBeam(pressHand, h);

        bool palmReady = menuHand.IsTracked && menuHand.HasPalm && menuHand.PalmUp
                         && menuHand.PalmCenter.y <= h.position.y - minBelowEyes
                         && Vector3.Distance(menuHand.PalmCenter, h.position) <= maxReach;

        // Debounce: hold the pose briefly to open, drop it briefly to close.
        if (palmReady)
        {
            downSince = -1f;
            if (upSince < 0f) upSince = Time.time;
            if (!shown && Time.time - upSince >= showDelay) SetShown(true);
        }
        else
        {
            upSince = -1f;
            if (downSince < 0f) downSince = Time.time;
            if (shown && Time.time - downSince >= hideDelay) SetShown(false);
        }

        // While the menu pose is held or the panel is up, pinches are not game confirms (plus a short grace after).
        if (palmReady || shown)
            HandPointer.SuppressConfirmUntil = Time.time + confirmGrace;

        if (!shown) return;
        if (menuHand != null) menuHand.Resting = true;
        if (pressHand != null) pressHand.Resting = true;

        // Float above the palm, facing the head. Glide so hand jitter doesn't shake the buttons.
        Vector3 targetPos = menuHand.PalmCenter + menuHand.PalmNormal * aboveDistance;
        Quaternion targetRot = Quaternion.LookRotation(targetPos - h.position, Vector3.up);

        float t = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
        panel.transform.position = Vector3.Lerp(panel.transform.position, targetPos, t);
        panel.transform.rotation = Quaternion.Slerp(panel.transform.rotation, targetRot, t);

        // Only the other hand presses buttons.
        HandPointer[] pressers = pressHand != null ? new HandPointer[] { pressHand } : new HandPointer[0];
        foreach (MenuButton b in buttons) b.hands = pressers;
    }

    void HidePalmFacingBeam(HandPointer hand, Transform h)
    {
        if (hand == null || !hand.IsTracked || !hand.HasPalm) return;
        Vector3 toHead = (h.position - hand.PalmCenter).normalized;
        if (Vector3.Dot(hand.PalmNormal, toHead) >= palmToFaceThreshold) hand.Resting = true;
    }

    void SetShown(bool value)
    {
        shown = value;
        panel.SetActive(value);

        if (value && menuHand.HasPalm)
        {
            // Snap into place on open instead of gliding from wherever it last was.
            Transform h = head != null ? head : Camera.main.transform;
            Vector3 p = menuHand.PalmCenter + menuHand.PalmNormal * aboveDistance;
            panel.transform.position = p;
            panel.transform.rotation = Quaternion.LookRotation(p - h.position, Vector3.up);
        }
    }
}