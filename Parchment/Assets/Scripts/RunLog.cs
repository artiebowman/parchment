using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

// The run-by-run list beside the cube. It reads the practice flag off TrialManager and shows one of two layouts:
// practice: a dimmed P line per practice run and the practice average underneath.
// live: seven fixed slots, a dash until that run is in, and the live average pinned under slot seven.
// The mode is written small beside the title so the two boards never look the same.
public class RunLog : MonoBehaviour
{
    public TMP_Text text;
    public TrialManager trial;        // drag TaskCube here; its practice flag decides the layout
    public int totalRuns = 7;
    public string title = "RUNS";
    public string emptyMark = "--";   // what an unfinished slot shows

    private struct Entry { public int run; public float seconds; public bool practice; }
    private readonly List<Entry> entries = new List<Entry>();
    private bool lastPractice;

    void Start() { lastPractice = Practicing; Render(); }

    // Practice can flip from the menu between runs, so redraw when it does.
    void Update()
    {
        if (Practicing == lastPractice) return;
        lastPractice = Practicing;
        Render();
    }

    bool Practicing => trial == null || trial.practice;

    public void Clear() { entries.Clear(); Render(); }

    public void Add(int run, float seconds, bool practice)
    {
        entries.Add(new Entry { run = run, seconds = seconds, practice = practice });
        Render();
    }

    void Render()
    {
        if (text == null) return;
        bool practicing = Practicing;
        StringBuilder sb = new StringBuilder();
        sb.Append("<u>").Append(title).Append("</u>\n");

        float sum = 0f;
        int n = 0;
        if (practicing)
        {
            foreach (Entry e in entries)
            {
                if (!e.practice) continue;
                sb.Append("\n<alpha=#66>P  ").Append(e.seconds.ToString("F2")).Append(" s<alpha=#FF>");
                sum += e.seconds;
                n++;
            }
        }
        else
        {
            for (int run = 1; run <= totalRuns; run++)
            {
                sb.Append('\n').Append(run).Append("  ");
                bool found = false;
                foreach (Entry e in entries)
                {
                    if (e.practice || e.run != run) continue;
                    sb.Append(e.seconds.ToString("F2")).Append(" s");
                    sum += e.seconds;
                    n++;
                    found = true;
                    break;
                }
                if (!found) sb.Append("<alpha=#66>").Append(emptyMark).Append("<alpha=#FF>");
            }
        }

        sb.Append("\n\navg ");
        if (n > 0) sb.Append((sum / n).ToString("F2")).Append(" s");
        else sb.Append("<alpha=#66>").Append(emptyMark).Append("<alpha=#FF>");
        if (practicing) sb.Append("  (").Append(n).Append(n == 1 ? " run)" : " runs)");
        else sb.Append("  (").Append(n).Append('/').Append(totalRuns).Append(')');
        text.text = sb.ToString();
    }
}
