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
    public PhantomMode phantom;         // drag TaskCube here (its PhantomMode); empty = no Phantom
    public PalmMenu menu;               // drag TaskCube here (its PalmMenu); while the menu is up, hands are off the sheet
    public float snapRadius = 0.03f;
    public float bulbRadius = 0.015f;   // half the Bulb prefab's scale
    public float pressRadius = 0.02f;   // fingertip within this distance of a bulb center = pressing that bulb (a bit bigger than the glass)
    public float phantomPressRadius = 0.03f; // Phantom only: covers a whole 0.045 cell corner to corner, so the phantom is always in exactly one bulb
    public float releaseRadius = 0.03f; // once pressing, the tip must get this far from the center to release (hysteresis; keep it below the bulb spacing)
    public float sheetHalfSize = 0.25f; // half the Slab's width; hits outside this are ignored
    public float lineGap = 0.02f;       // beam stops this far short of the coin
    public float nearDistance = 0.15f;  // fingertip within this height above the sheet = near mode (ray off)
    public float belowTolerance = 0.05f; // how far below the bulb plane a finger can dip and still count

    // Per hand, index matches hands[]: what it's hovering (null if none) and whether it's over the sheet at all.
    private Bulb[] hovered = new Bulb[0];
    private bool[] onSheet = new bool[0];

    // Poke-to-select, per hand. Like keys on a keyboard: entering a bulb's ball presses it, leaving releases it.
    private Bulb[] pressing = new Bulb[0];        // bulb the tip is inside right now (null if none)
    private bool[] pokedThisFrame = new bool[0];
    private Bulb[] pokedBulb = new Bulb[0];
    private float[] lastHeight = new float[0];    // tip height above the plane last frame; used to refuse entry from below

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

    // True on the single frame this hand's fingertip entered a bulb it wasn't inside before.
    public bool PokedThisFrame(HandPointer hand)
    {
        for (int i = 0; i < hands.Length; i++)
            if (hands[i] == hand) return pokedThisFrame[i];
        return false;
    }

    // The bulb entered on that frame (null when no poke this frame).
    public Bulb GetPoked(HandPointer hand)
    {
        for (int i = 0; i < hands.Length; i++)
            if (hands[i] == hand) return pokedThisFrame[i] ? pokedBulb[i] : null;
        return null;
    }

    // The bulb this hand's fingertip is currently inside, held down (null if none).
    public Bulb IsPressing(HandPointer hand)
    {
        for (int i = 0; i < hands.Length; i++)
            if (hands[i] == hand) return pressing[i];
        return null;
    }

    public bool AnyHovered()
    {
        foreach (Bulb b in hovered) if (b != null) return true;
        return MouseHovered != null;
    }

    void OnEnable()
    {
        ResetArrays();
    }

    void OnDisable()
    {
        foreach (Bulb b in hovered) if (b != null) b.SetState(Bulb.State.Idle);
        if (MouseHovered != null) MouseHovered.SetState(Bulb.State.Idle);

        ResetArrays();
        MouseHovered = null;

        foreach (GameObject r in reticles) if (r != null) r.SetActive(false);
    }

    void ResetArrays()
    {
        int n = hands.Length;
        hovered = new Bulb[n];
        onSheet = new bool[n];
        pressing = new Bulb[n];
        pokedThisFrame = new bool[n];
        pokedBulb = new Bulb[n];
        lastHeight = new float[n];
        for (int i = 0; i < n; i++) lastHeight[i] = float.NegativeInfinity;   // "unknown" = treat as from below, no poke
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

            Bulb wasHovered = hovered[i];   // last frame, for the hover sound

            hovered[i] = null;
            onSheet[i] = false;
            pokedThisFrame[i] = false;
            pokedBulb[i] = null;
            bool hit = false;
            Vector3 point = Vector3.zero;
            float dist = 0f;

            bool menuUp = menu != null && menu.IsShown;   // menu open: no reticle, hover, poke or phantom on the sheet
            if (hand != null && hand.IsTracked && !menuUp)
            {
                hand.Resting = !sheet.GetSide(hand.PointerRay.origin);   // hand below the sheet = at rest

                bool near = false;
                bool ghost = false;   // Phantom is driving this hand this frame

                // Phantom: fingertip on the trackpad maps corner-for-corner onto the bulb plane.
                // Sliding that point through a bulb pokes it.
                if (phantom != null && phantom.Active && phantom.pad != null && hand.HasIndexTip)
                {
                    if (phantom.pad.TryMap(hand.IndexTip, out Vector2 uv))
                    {
                        Vector3 tip = sheet.ClosestPointOnPlane(phantom.OnBoard(uv));   // always on the bulb plane
                        ghost = true;
                        near = true;
                        hit = true;
                        point = tip;
                        hovered[i] = NearestBulb(tip, snapRadius);
                        hand.LineClip = 0f;
                        phantom.pad.ShowReticle(i, hand.IndexTip);

                        // Press with the same hysteresis as a real poke; entering a bulb is the confirm.
                        Bulb inside = null;
                        if (pressing[i] != null && Vector3.Distance(pressing[i].transform.position, tip) < releaseRadius)
                            inside = pressing[i];
                        Bulb entered = NearestBulb(tip, phantomPressRadius);
                        if (entered != null && entered != inside) inside = entered;

                        if (inside != null && inside != pressing[i])
                        {
                            pokedThisFrame[i] = true;
                            pokedBulb[i] = inside;
                        }
                        pressing[i] = inside;
                        lastHeight[i] = 0f;
                        phantom.Report(hand, true, tip);
                    }
                    else phantom.Report(hand, false, Vector3.zero);
                }

                if (!ghost)
                {
                    if (hand.HasIndexTip)
                    {
                        float height = sheet.GetDistanceToPoint(hand.IndexTip);   // + above, - below
                        Vector3 projected = sheet.ClosestPointOnPlane(hand.IndexTip);

                        // Near mode: fingertip projected straight down onto the sheet, height ignored.
                        if (height <= nearDistance && height >= -belowTolerance && InsideSheet(projected))
                        {
                            near = true;
                            hit = true;
                            point = projected;
                            hovered[i] = NearestBulb(projected, snapRadius);
                            hand.LineClip = 0f;                 // no beam in near mode
                        }

                        // Press, with hysteresis. Only meaningful in near mode.
                        Bulb inside = null;
                        if (near)
                        {
                            // Still holding the bulb from last frame? Keep it until the tip clears releaseRadius.
                            if (pressing[i] != null &&
                                Vector3.Distance(pressing[i].transform.position, hand.IndexTip) < releaseRadius)
                                inside = pressing[i];

                            // Entering a different bulb's ball always wins (drag-through).
                            Bulb entered = NearestBulb(hand.IndexTip, pressRadius);
                            if (entered != null && entered != inside) inside = entered;
                        }

                        // Entering a bulb we weren't inside last frame is a poke, unless we came up from under the sheet.
                        bool cameFromBelow = lastHeight[i] < -belowTolerance;
                        if (inside != null && inside != pressing[i] && !cameFromBelow)
                        {
                            pokedThisFrame[i] = true;
                            pokedBulb[i] = inside;
                        }

                        pressing[i] = inside;
                        lastHeight[i] = height;
                    }
                    else
                    {
                        pressing[i] = null;
                        lastHeight[i] = float.NegativeInfinity;   // lost the tip; whatever it reappears inside doesn't count
                    }
                }

                if (!near)
                {
                    hovered[i] = FindNearest(hand.PointerRay, sheet, out hit, out point, out dist);
                    if (hit) hand.LineClip = Mathf.Min(hand.LineClip, dist - lineGap);
                }

                onSheet[i] = hit;
            }
            else
            {
                pressing[i] = null;
                lastHeight[i] = float.NegativeInfinity;
            }

            // Soft click whenever this hand lands on a different bulb.
            if (hovered[i] != null && hovered[i] != wasHovered && AudioFeedback.Instance != null)
                AudioFeedback.Instance.PlayClick();

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

    // Nearest bulb center to a world point, within radius. Works in 3D, so it serves both
    // the hover snap (point on the plane) and the press check (the fingertip itself).
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
