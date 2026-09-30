using UnityEngine;
using UnityEngine.InputSystem;

public class EditorMouseRig : MonoBehaviour
{
    // Editor only: the mouse stands in for a hand so the task can be tested without a headset. Disables itself in a build.
    private Camera cam;
    private TrialManager trial;
    private ParchmentHover hover;
    private bool wasFinishedLastFrame;

    void Start()
    {
        if (!Application.isEditor)
        {
            enabled = false;
            return;
        }

        cam = Camera.main;
        trial = GetComponent<TrialManager>();
        hover = GetComponent<ParchmentHover>();
        cam.transform.root.position = new Vector3(0f, 1.5f, -0.4f);
    }

    void Update()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        if (trial.RunFinished)
        {
            if (wasFinishedLastFrame) trial.StartNextRun();
            return;
        }

        if (hover != null && hover.MouseHovered != null) return;   // parchment owns this click

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            string name = hit.transform.name;
            if (!name.StartsWith("Sphere_")) return;

            int id = int.Parse(name.Substring(7));
            trial.OnSphereSelected(id);
        }
    }

    void LateUpdate()
    {
        wasFinishedLastFrame = trial.RunFinished;
    }
}