using System.Collections.Generic;
using UnityEngine;

public class SequenceLoader : MonoBehaviour
{
    [Range(1, 7)]
    public int runNumber = 1;
    public bool testMode = false;

    private List<int[]> sequences = new List<int[]>();

    void Awake()
    {
        TextAsset file = Resources.Load<TextAsset>("test_sequences");
        string[] lines = file.text.Split('\n');

        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0) continue;

            string[] parts = trimmed.Split(',');
            int[] ids = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                ids[i] = int.Parse(parts[i].Trim());
            }
            sequences.Add(ids);
        }

        Debug.Log("SequenceLoader: loaded " + sequences.Count + " sequences");
        Debug.Log("SequenceLoader: run " + runNumber + " = " + string.Join(", ", GetSequence()));
    }

    public int[] GetSequence()
    {
        if (testMode)
        {
            return RandomSequence(7);
        }
        return sequences[runNumber - 1];
    }

    private int[] RandomSequence(int count)
    {
        List<int> pool = new List<int>();
        for (int i = 0; i < 100; i++) pool.Add(i);

        int[] result = new int[count];
        for (int i = 0; i < count; i++)
        {
            int pick = Random.Range(0, pool.Count);
            result[i] = pool[pick];
            pool.RemoveAt(pick);
        }
        return result;
    }
}