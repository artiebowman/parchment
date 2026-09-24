using UnityEngine;

public class CubeRaySelector : MonoBehaviour
{
    public HandPointer[] hands;
    public float maxDistance = 5f;
    public ParchmentHover hover;            // drag TaskCube here
    public ParchmentSelector parchment;     // drag TaskCube here

    private TrialManager trial;
    private bool wasFinishedLastFrame;      // run must be finished for a full frame before a pinch continues it

    void Awake()
    {
        trial = GetComponent<TrialManager>();
    }

    void Update()
    {
        foreach (HandPointer hand in hands)
        {
            if (!hand.ConfirmedThisFrame) continue;

            if (trial.RunFinished)
            {
                if (wasFinishedLastFrame) trial.StartNextRun();   // ignore the pinch that finished the run
                return;
            }

            if (parchment != null && parchment.SelectedThisFrame) return;   // sheet took this frame's pinch
            if (hover != null && hover.IsOnSheet(hand)) continue;          // this hand is working the sheet, not the cube

            RaycastHit hit;
            if (Physics.Raycast(hand.PointerRay, out hit, maxDistance))
            {
                if (hit.collider.name.StartsWith("Sphere_"))
                {
                    int id = int.Parse(hit.collider.name.Substring(7));
                    trial.OnSphereSelected(id);
                }
            }
        }
    }

    void LateUpdate()
    {
        wasFinishedLastFrame = trial.RunFinished;
    }
}