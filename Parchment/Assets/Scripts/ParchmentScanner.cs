using System.Collections.Generic;
using UnityEngine;

public class ParchmentScanner : MonoBehaviour
{
    public Transform taskCube;      // drag TaskCube here
    public Transform bulbParent;    // drag Bulbs here
    public GameObject bulbPrefab;   // drag the Bulb prefab here

    public int columns = 10;
    public float spacing = 0.045f;

    private readonly Dictionary<int, GameObject> bulbs = new Dictionary<int, GameObject>();

    void Start()
    {
        BuildGrid();
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

            GameObject bulb = Instantiate(bulbPrefab, bulbParent);
            bulb.name = "Bulb_" + id;
            bulb.transform.localPosition = localPos;

            bulbs[id] = bulb;
        }

        Debug.Log("ParchmentScanner built " + bulbs.Count + " bulbs.");
    }

    public GameObject GetBulb(int id)
    {
        return bulbs.TryGetValue(id, out GameObject b) ? b : null;
    }
}