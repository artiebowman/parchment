using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

// The run-by-run list beside the cube: one line per finished run, practice runs dimmed and left out of the average.
public class RunLog : MonoBehaviour
{
    public TMP_Text text;
    public int totalRuns = 7;
    public string title = "RUNS";

    private struct Entry { public int run; public float seconds; public bool practice; }
    private readonly List<Entry> entries = new List<Entry>();

    void Start() { Render(); }

    public void Clear() { entries.Clear(); Render(); }

    public void Add(int run, float seconds, bool practice)
    {
        entries.Add(new Entry { run = run, seconds = seconds, practice = practice });
        Render();
    }

    void Render()
    {
        if (text == null) return;
        StringBuilder sb = new StringBuilder();
        sb.Append(title);
        float sum = 0f;
        int n = 0;
        foreach (Entry e in entries)
        {
            sb.Append('\n');
            if (e.practice) sb.Append("<alpha=#66>P  ").Append(e.seconds.ToString("F2")).Append(" s<alpha=#FF>");
            else
            {
                sb.Append(e.run).Append("  ").Append(e.seconds.ToString("F2")).Append(" s");
                sum += e.seconds;
                n++;
            }
        }
        if (n > 0) sb.Append("\n\navg ").Append((sum / n).ToString("F2")).Append(" s  (").Append(n).Append('/').Append(totalRuns).Append(')');
        text.text = sb.ToString();
    }
}
