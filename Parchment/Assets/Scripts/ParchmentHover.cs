using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

[DefaultExecutionOrder(-50)]
public class ParchmentHover : MonoBehaviour
{
    public HandPointer[] hands;         // drag Left then Right hand objects here
    public GameObject[] reticles;       // drag Reticle_L then Reticle_R here (same order as hands)
    public Transform parchment;         // drag Parchment here
    public ParchmentScanner scanner;    // drag TaskCube here
    public float snapRadius = 0.03f;
    public float bulbRadius = 0.015f;   // half the Bulb prefab's scale
    public float sheetHalfSize = 0.25f; // half the Slab's width; hits outside this are ignored
    public float lineGap = 0.02f;       // beam stops this far short of the coin
    public float nearDistance = 0.15f;  // fingertip within this height above the sheet = near mode (ray off)
    public float belowTolerance = 0.05f; // how far below the bulb plane a finger can dip and still count

    // Per hand, index matches hands[]: what it's hovering (null if none) and whether it's over the sheet at all.
    private Bulb[] hovered = new Bulb[0];
    private bool[] onSheet = new bool[0];

    // Editor mouse acts like an extra hand.
    public Bulb MouseHovered { get; private set; }

    public Bulb GetHovered(HandPointer hand)
    {
        for (int i = 0; i < hands.Length; i++)
            if (hands[i] == hand) return hovered[i];
        return null;
    }

    public bool IsOnSheet(HandPointer hand)
    {
        for (int i = 0; i < hands.Length; i++)
            if (hands[i] == hand) return onSheet[i];
        return false;
    }

    public bool AnyHovered()
    {
        foreach (Bulb b in hovered) if (b != null) return true;
        return MouseHovered != null;
    }

    void OnEnable()
    {
        hovered = new Bulb[hands.Length];
        onSheet = new bool[hands.Length];
    }

    void OnDisable()
    {
        foreach (Bulb b in hovered) if (b != null) b.SetState(Bulb.State.Idle);
        if (MouseHovered != null) MouseHovered.SetState(Bulb.State.Idle);

        hovered = new Bulb[hands.Length];
        onSheet = new bool[hands.Length];
        MouseHovered = null;

        foreach (GameObject r in reticles) if (r != null) r.SetActive(false);
    }

    void Update()
    {
        // Plane through the bulb centers, not the slab surface, so a ray through a bulb lands on that bulb.
        Vector3 planePoint = scanner.bulbParent.position + parchment.up * bulbRadius;
        Plane sheet = new Plane(parchment.up, planePoint);

        HashSet<Bulb> previous = new HashSet<Bulb>();
        foreach (Bulb b in hovered) if (b != null) previous.Add(b);
        if (MouseHovered != null) previous.Add(MouseHovered);

        HashSet<Bulb> current = new HashSet<Bulb>();

        for (int i = 0; i < hands.Length; i++)
        {
            HandPointer hand = hands[i];
            GameObject reticle = i < reticles.Length ? reticles[i] : null;

            hovered[i] = null;
            onSheet[i] = false;
            bool hit = false;
            Vector3 point = Vector3.zero;
            float dist = 0f;

            if (hand != null && hand.IsTracked)
            {
                hand.Resting = !sheet.GetSide(hand.PointerRay.origin);   // hand below the sheet = at rest

                bool near = false;

                if (hand.HasIndexTip)
                {
                    // Near mode: fingertip projected straight down onto the sheet, height ignored.
                    float height = sheet.GetDistanceToPoint(hand.IndexTip);   // + above, - below
                    Vector3 projected = sheet.ClosestPointOnPlane(hand.IndexTip);

                    if (height <= nearDistance && height >= -belowTolerance && InsideSheet(projected))
                    {
                        near = true;
                        hit = true;
                        point = projected;
                        hovered[i] = NearestBulb(projected, snapRadius);
                        hand.LineClip = 0f;                 // no beam in near mode
                    }
                }

                if (!near)
                {
                    hovered[i] = FindNearest(hand.PointerRay, sheet, out hit, out point, out dist);
                    if (hit) hand.LineClip = Mathf.Min(hand.LineClip, dist - lineGap);
                }

                onSheet[i] = hit;
            }

            if (hovered[i] != null) current.Add(hovered[i]);

            if (reticle != null)
            {
                reticle.SetActive(hit);
                if (hit)
                {
                    reticle.transform.position = point;
                    reticle.transform.rotation = parchment.rotation;
                }
            }
        }

        MouseHovered = null;
#if UNITY_EDITOR
        if (!AnyHandTracked())
        {
            Camera cam = Camera.main;
            if (cam != null && Mouse.current != null)
            {
                Ray mouseRay = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
                MouseHovered = FindNearest(mouseRay, sheet, out bool mHit, out Vector3 mPoint, out float _);
                if (MouseHovered != null) current.Add(MouseHovered);

                if (reticles.Length > 0 && reticles[0] != null)
                {
                    reticles[0].SetActive(mHit);
                    if (mHit)
                    {
                        reticles[0].transform.position = mPoint;
                        reticles[0].transform.rotation = parchment.rotation;
                    }
                }
            }
        }
#endif

        // Bulbs that were hovered last frame but aren't now go back to Idle.
        foreach (Bulb b in previous)
            if (!current.Contains(b)) b.SetState(Bulb.State.Idle);

        // Color everything hovered now.
        foreach (Bulb b in current)
        {
            bool onTarget = scanner.trial != null && scanner.trial.IsCurrentTarget(b.id);
            b.SetState(onTarget ? Bulb.State.HoverTarget : Bulb.State.Hover);
        }
    }

    bool InsideSheet(Vector3 worldPoint)
    {
        Vector3 local = parchment.InverseTransformPoint(worldPoint);
        return Mathf.Abs(local.x) <= sheetHalfSize && Mathf.Abs(local.z) <= sheetHalfSize;
    }

    Bulb FindNearest(Ray ray, Plane sheet, out bool hit, out Vector3 point, out float dist)
    {
        hit = false;
        point = Vector3.zero;
        dist = 0f;

        if (!sheet.Raycast(ray, out float enter)) return null;
        if (!sheet.GetSide(ray.origin)) return null;   // hand is under the sheet; ignore

        Vector3 p = ray.GetPoint(enter);
        if (!InsideSheet(p)) return null;   // the plane itself is infinite

        hit = true;
        dist = enter;
        point = p;

        return NearestBulb(point, snapRadius);
    }

    Bulb NearestBulb(Vector3 point, float radius)
    {
        Bulb best = null;
        float bestDist = radius;

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
        return best;
    }

    bool AnyHandTracked()
    {
        foreach (HandPointer hand in hands)
            if (hand != null && hand.IsTracked) return true;
        return false;
    }
}