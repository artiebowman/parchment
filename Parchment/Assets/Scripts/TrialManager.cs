using UnityEngine;

public class TrialManager : MonoBehaviour
{
    private SequenceLoader loader;
    private int[] sequence;
    private int currentIndex = 0;
    private float startTime;
    private bool running = false;
    private Renderer currentTarget;
    private Color originalColor;

    public Scoreboard board;
    public int totalRuns = 7;
    public bool RunFinished { get; private set; }

    void Start()
    {
        loader = GetComponent<SequenceLoader>();
        StartRun(loader.runNumber);
    }

    public void StartRun(int run)
    {
        if (run > totalRuns)
        {
            board.ShowPrompt("All " + totalRuns + " runs complete");
            return;
        }

        loader.runNumber = run;
        sequence = loader.GetSequence();
        currentIndex = 0;
        running = false;
        RunFinished = false;
        HighlightTarget();
        board.ShowProgress(run, 1, sequence.Length);
        Debug.Log("TrialManager: run " + run + " started, first target Sphere_" + sequence[0]);
    }

    public void StartNextRun()
    {
        StartRun(loader.runNumber + 1);
    }

    public void OnSphereSelected(int id)
    {
        if (currentIndex >= sequence.Length) return;

        if (id != sequence[currentIndex])
        {
            Debug.Log("TrialManager: wrong sphere " + id + ", wanted " + sequence[currentIndex]);
            return;
        }

        if (!running)
        {
            running = true;
            startTime = Time.time;
        }

        currentIndex++;
        HighlightTarget();
        Debug.Log("TrialManager: hit " + id + " (" + currentIndex + " of " + sequence.Length + ")");

        if (currentIndex >= sequence.Length)
        {
            float elapsed = Time.time - startTime;
            running = false;
            RunFinished = true;
            board.ShowRunTime(loader.runNumber, elapsed);
            Debug.Log("TrialManager: run " + loader.runNumber + " complete in " + elapsed.ToString("F2") + " s");
        }
        else
        {
            board.ShowProgress(loader.runNumber, currentIndex + 1, sequence.Length);
        }
    }

    public bool IsCurrentTarget(int id)
    {
        if (currentIndex >= sequence.Length) return false;
        return id == sequence[currentIndex];
    }

    private void HighlightTarget()
    {
        if (currentTarget != null)
        {
            currentTarget.material.color = originalColor;
        }

        if (currentIndex >= sequence.Length) return;

        Transform sphere = transform.Find("Sphere_" + sequence[currentIndex]);
        currentTarget = sphere.GetComponent<Renderer>();
        originalColor = currentTarget.material.color;
        currentTarget.material.color = Color.green;
    }
}