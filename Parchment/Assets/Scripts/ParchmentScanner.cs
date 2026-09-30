using System.Collections.Generic;
using UnityEngine;

public class ParchmentScanner : MonoBehaviour
{
    // The one-time projection: at Start, every Sphere_N under TaskCube becomes a bulb on a 10x10 grid, laid out by sorted id.
    // Maps by layout, never by sequence: it does not know which sphere is next. After Start it only does feedback (which bulb is the target).
    public Transform taskCube;      // drag TaskCube here
    public Transform bulbParent;    // drag Bulbs here
    public GameObject bulbPrefab;   // drag the Bulb prefab here
    public TrialManager trial;      // drag TaskCube here (it has TrialManager)

    public int columns = 10;
    public float spacing = 0.045f;

    private readonly Dictionary<int, Bulb> bulbs = new Dictionary<int, Bulb>();

    // The bulb matching the current target sphere, refreshed every frame. Feedback only.
    public Bulb TargetBulb { get; private set; }

    void Start()
    {
        BuildGrid();
    }

    void Update()
    {
        RefreshStates();
    }

    void BuildGrid()
    {
        List<int> ids = new List<int>();

        foreach (Transform child in taskCube)
        {
            if (child.name.StartsWith("Sphere_"))
            {
                ids.Add(int.Parse(child.name.Substring(7)));
            }
        }

        ids.Sort();

        float offset = (columns - 1) * spacing * 0.5f;

        for (int i = 0; i < ids.Count; i++)
        {
            int id = ids[i];
            int col = i % columns;
            int row = i / columns;

            Vector3 localPos = new Vector3(col * spacing - offset, 0f, offset - row * spacing);

            GameObject go = Instantiate(bulbPrefab, bulbParent);
            go.name = "Bulb_" + id;
            go.transform.localPosition = localPos;

            Bulb bulb = go.GetComponent<Bulb>();
            bulb.id = id;
            bulbs[id] = bulb;
        }

        Debug.Log("ParchmentScanner built " + bulbs.Count + " bulbs.");
    }

    void RefreshStates()
    {
        if (trial == null) return;

        TargetBulb = null;

        foreach (var pair in bulbs)
        {
            Bulb bulb = pair.Value;
            bool isTarget = trial.IsCurrentTarget(bulb.id);
            if (isTarget) TargetBulb = bulb;

            if (bulb.IsHovered) continue;   // ParchmentHover owns both hover states

            bulb.SetState(isTarget ? Bulb.State.Target : Bulb.State.Idle);
        }
    }

    public Bulb GetBulb(int id)
    {
        return bulbs.TryGetValue(id, out Bulb b) ? b : null;
    }
}