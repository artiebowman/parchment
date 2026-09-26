using UnityEngine;

public class CubeRaySelector : MonoBehaviour
{
    public HandPointer[] hands;
    public float maxDistance = 5f;
    public ParchmentHover hover;            // optional; found on this object if empty
    public ParchmentSelector parchment;     // optional; found on this object if empty

    private TrialManager trial;
    private bool wasFinishedLastFrame;      // run must be finished for a full frame before a pinch continues it
    private int[] overSphere = new int[0];  // per hand: sphere id the ray was over last frame (-1 = none)

    void Awake()
    {
        trial = GetComponent<TrialManager>();
        if (hover == null) hover = GetComponent<ParchmentHover>();
        if (parchment == null) parchment = GetComponent<ParchmentSelector>();

        overSphere = new int[hands.Length];
        for (int i = 0; i < overSphere.Length; i++) overSphere[i] = -1;
    }

    void Update()
    {
        for (int i = 0; i < hands.Length; i++)
        {
            HandPointer hand = hands[i];
            if (hand == null) continue;

            // Which sphere is this hand's ray over right now? (-1 if none, or if the hand is busy elsewhere.)
            int id = -1;
            bool busy = !hand.IsTracked || hand.Resting || (hover != null && hover.IsOnSheet(hand));
            if (!busy && Physics.Raycast(hand.PointerRay, out RaycastHit hit, maxDistance)
                && hit.collider.name.StartsWith("Sphere_"))
            {
                id = int.Parse(hit.collider.name.Substring(7));
            }

            // Soft click when the ray lands on a different sphere.
            if (id >= 0 && id != overSphere[i] && AudioFeedback.Instance != null)
                AudioFeedback.Instance.PlayClick();
            overSphere[i] = id;

            if (!hand.ConfirmedThisFrame) continue;

            if (trial.RunFinished)
            {
                if (wasFinishedLastFrame) trial.StartNextRun();   // ignore the pinch that finished the run
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