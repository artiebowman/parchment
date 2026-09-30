using UnityEngine;

public class CubeRaySelector : MonoBehaviour
{
    // Cube mode: board rolled up, hand ray + pinch picks a sphere directly. Also where a pinch starts the next run.
    // ---- Wiring ----
    public HandPointer[] hands;
    public GameObject[] reticles;           // drag CubeReticle_L then CubeReticle_R here (same order as hands)
    public float maxDistance = 5f;
    public ParchmentHover hover;            // optional; found on this object if empty
    public ParchmentSelector parchment;     // optional; found on this object if empty
    public ParchmentMode parchmentMode;     // drag TaskCube here; while the board is out, the cube is off limits

    public Color hoverColor = new Color(1f, 0.85f, 0.2f);          // same yellow as a hovered bulb
    public Color hoverTargetColor = new Color(0.6f, 1f, 0.6f);     // same pale green as a hovered target bulb
    public float reticleLift = 0.001f;                             // sit just off the sphere surface

    // ---- Per-hand state carried between frames ----
    private TrialManager trial;
    private bool wasFinishedLastFrame;      // run must be finished for a full frame before a pinch continues it
    private int[] overSphere = new int[0];  // per hand: sphere id the ray was over last frame (-1 = none)
    private Renderer[] overRenderer = new Renderer[0];
    private MaterialPropertyBlock block;

    void Awake()
    {
        trial = GetComponent<TrialManager>();
        if (hover == null) hover = GetComponent<ParchmentHover>();
        if (parchment == null) parchment = GetComponent<ParchmentSelector>();

        overSphere = new int[hands.Length];
        overRenderer = new Renderer[hands.Length];
        for (int i = 0; i < overSphere.Length; i++) overSphere[i] = -1;
        block = new MaterialPropertyBlock();
    }

    void Update()
    {
        for (int i = 0; i < hands.Length; i++)
        {
            HandPointer hand = hands[i];
            GameObject reticle = i < reticles.Length ? reticles[i] : null;
            if (hand == null) continue;

            // Which sphere is this hand's ray over right now? (-1 if none, or if the hand is busy elsewhere.)
            int id = -1;
            Renderer rend = null;
            RaycastHit hit = new RaycastHit();
            // Mode ladder: no cube while the hand is parked, on the sheet, or the board is out at all.
            bool busy = !hand.IsTracked || hand.Resting || (hover != null && hover.IsOnSheet(hand))
                        || (parchmentMode != null && parchmentMode.IsOn);
            bool gotHit = false;
            if (!busy && Physics.Raycast(hand.PointerRay, out hit, maxDistance)
                && hit.collider.name.StartsWith("Sphere_"))
            {
                gotHit = true;
                id = int.Parse(hit.collider.name.Substring(7));
                rend = hit.collider.GetComponent<Renderer>();
            }

            // Hover changed: clear the old sphere's tint, tint the new one, soft click.
            if (id != overSphere[i])
            {
                if (overRenderer[i] != null) overRenderer[i].SetPropertyBlock(null);
                if (id >= 0 && AudioFeedback.Instance != null) AudioFeedback.Instance.PlayClick();
            }
            overSphere[i] = id;
            overRenderer[i] = rend;

            if (rend != null)
            {
                Color c = trial.IsCurrentTarget(id) ? hoverTargetColor : hoverColor;
                block.SetColor("_Color", c);
                block.SetColor("_BaseColor", c);
                rend.SetPropertyBlock(block);
            }

            // Reticle on the sphere surface, facing out along the hit normal.
            if (reticle != null)
            {
                reticle.SetActive(gotHit);
                if (gotHit)
                {
                    reticle.transform.position = hit.point + hit.normal * reticleLift;
                    reticle.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                }
            }

            // Pinch handling. A finished run turns the pinch into "continue" instead of a selection; otherwise one pinch = one sphere, and only if the sheet didn't already take it.
            if (!hand.ConfirmedThisFrame) continue;

            if (trial.RunFinished)
            {
                if (wasFinishedLastFrame && (trial.HasStarted || hand.DoublePinchedThisFrame)) trial.StartNextRun();   // double pinch for run 1, single pinch after; the pinch that finished a run never counts
                return;
            }

            if (parchment != null && parchment.SelectedThisFrame) return;   // sheet took this frame's pinch
            if (busy) continue;                                             // this hand is working the sheet, not the cube

            if (id >= 0) trial.OnSphereSelected(id);
        }
    }

    void LateUpdate()
    {
        wasFinishedLastFrame = trial.RunFinished;
    }
}