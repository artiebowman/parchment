using UnityEngine;

public class CubeRaySelector : MonoBehaviour
{
    public HandPointer[] hands;
    public float maxDistance = 5f;

    private TrialManager trial;

    void Awake()
    {
        trial = GetComponent<TrialManager>();
    }

    void Update()
    {
        foreach (HandPointer hand in hands)
        {
            if (!hand.ConfirmedThisFrame) continue;

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
}