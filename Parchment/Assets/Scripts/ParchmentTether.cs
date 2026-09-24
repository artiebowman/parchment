using UnityEngine;

public class ParchmentTether : MonoBehaviour
{
    public ParchmentHover hover;        // drag TaskCube here
    public ParchmentScanner scanner;    // drag TaskCube here
    public Transform parchment;         // drag Parchment here
    public Material lineMaterial;       // drag RayLine here

    public Color[] handColors = { new Color(0.3f, 0.65f, 1f), new Color(1f, 0.17f, 0.17f) };   // L blue, R red
    public float width = 0.003f;
    public float lift = 0.012f;         // height above the bulb plane
    public float hideDistance = 0.05f;  // closer than this: no tether, you're basically there
    public float fadeDistance = 0.15f;  // full strength beyond hideDistance + this

    private LineRenderer line;

    void Awake()
    {
        line = gameObject.AddComponent<LineRenderer>();
        line.material = lineMaterial;
        line.positionCount = 2;
        line.startWidth = width;
        line.endWidth = width * 0.5f;
        line.useWorldSpace = true;
        line.enabled = false;
    }

    void Update()
    {
        line.enabled = false;

        if (hover == null || scanner == null || !hover.enabled) return;

        Bulb target = scanner.TargetBulb;
        if (target == null) return;

        // Pick the coin that's closest to the target — that hand has the shortest trip.
        int best = -1;
        float bestDist = float.PositiveInfinity;
        Vector3 bestPos = Vector3.zero;

        for (int i = 0; i < hover.reticles.Length; i++)
        {
            GameObject coin = hover.reticles[i];
            if (coin == null || !coin.activeSelf) continue;

            float d = Vector3.Distance(coin.transform.position, target.transform.position);
            if (d < bestDist)
            {
                best = i;
                bestDist = d;
                bestPos = coin.transform.position;
            }
        }

        if (best < 0 || bestDist < hideDistance) return;

        float t = Mathf.Clamp01((bestDist - hideDistance) / fadeDistance);
        Color c = best < handColors.Length ? handColors[best] : Color.white;
        c.a = t;

        Vector3 up = parchment.up * lift;
        line.SetPosition(0, bestPos + up);
        line.SetPosition(1, target.transform.position + up);
        line.material.color = c;
        line.enabled = true;
    }
}