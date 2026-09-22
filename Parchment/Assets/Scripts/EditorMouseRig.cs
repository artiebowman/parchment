using UnityEngine;
using UnityEngine.InputSystem;

public class EditorMouseRig : MonoBehaviour
{
    private Camera cam;
    private TrialManager trial;

    void Start()
    {
        if (!Application.isEditor)
        {
            enabled = false;
            return;
        }

        cam = Camera.main;
        trial = GetComponent<TrialManager>();
        cam.transform.root.position = new Vector3(0f, 1.5f, -0.4f);
    }

    void Update()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        if (trial.RunFinished)
        {
            trial.StartNextRun();
            return;
        }

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            string name = hit.transform.name;
            if (!name.StartsWith("Sphere_")) return;

            int id = int.Parse(name.Substring(7));
            trial.OnSphereSelected(id);
        }
    }
}