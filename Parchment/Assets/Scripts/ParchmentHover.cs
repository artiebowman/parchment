using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

public class ParchmentHover : MonoBehaviour
{
    public HandPointer[] hands;         // drag both hand objects here
    public Transform parchment;         // drag Parchment here
    public ParchmentScanner scanner;    // drag TaskCube here
    public GameObject reticle;          // drag Reticle here
    public float snapRadius = 0.03f;

    public Bulb HoveredBulb { get; private set; }
    public bool HasHitPoint { get; private set; }
    public Vector3 HitPoint { get; private set; }

    void Update()
    {
        Bulb best = null;
        float bestDist = snapRadius;
        HasHitPoint = false;

        Plane sheet = new Plane(parchment.up, parchment.position);

        foreach (HandPointer hand in hands)
        {
            if (hand == null || !hand.IsTracked) continue;
            TryRay(hand.PointerRay, sheet, ref best, ref bestDist);
        }

#if UNITY_EDITOR
        if (hands.Length == 0 || !AnyHandTracked())
        {
            Camera cam = Camera.main;
            if (cam != null && Mouse.current != null)
            {
                Ray mouseRay = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
                TryRay(mouseRay, sheet, ref best, ref bestDist);
            }
        }
#endif

        if (HoveredBulb != null && HoveredBulb != best)
        {
            HoveredBulb.SetState(Bulb.State.Idle);   // scanner re-colors it next frame
        }

        HoveredBulb = best;

        if (HoveredBulb != null)
        {
            bool onTarget = scanner.trial != null && scanner.trial.IsCurrentTarget(HoveredBulb.id);
            HoveredBulb.SetState(onTarget ? Bulb.State.HoverTarget : Bulb.State.Hover);
        }

        if (reticle != null)
        {
            reticle.SetActive(HasHitPoint);
            if (HasHitPoint)
            {
                reticle.transform.position = HitPoint + parchment.up * 0.005f;
            }
        }
    }

    void TryRay(Ray ray, Plane sheet, ref Bulb best, ref float bestDist)
    {
        if (!sheet.Raycast(ray, out float enter)) return;

        Vector3 point = ray.GetPoint(enter);
        HasHitPoint = true;
        HitPoint = point;

        foreach (Transform child in scanner.bulbParent)
        {
            float d = Vector3.Distance(child.position, point);
            if (d < bestDist)
            {
                Bulb b = child.GetComponent<Bulb>();
                if (b != null)
                {
                    best = b;
                    bestDist = d;
                }
            }
        }
    }

    bool AnyHandTracked()
    {
        foreach (HandPointer hand in hands)
            if (hand != null && hand.IsTracked) return true;
        return false;
    }
}