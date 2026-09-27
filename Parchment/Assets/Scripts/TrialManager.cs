using UnityEngine;

public class TrialManager : MonoBehaviour
{
    private SequenceLoader loader;
    private int[] sequence;
    private int currentIndex = 0;
    private float startTime;
    private bool running = false;
    private bool hasStarted = false;    // false until the first run is kicked off by a pinch
    private Renderer currentTarget;
    private Color originalColor;

    public Scoreboard board;
    public int totalRuns = 7;
    public bool RunFinished { get; private set; }
    public bool HasStarted => hasStarted;   // false until run 1 begins; CubeRaySelector asks for a double pinch until then

    void Start()
    {
        loader = GetComponent<SequenceLoader>();

        // Wait for a pinch before run 1, same as between runs. Nothing is highlighted yet.
        RunFinished = true;
        board.ShowPrompt("Double pinch to start");
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
        hasStarted = true;
        RunFinished = false;
        HighlightTarget();
        board.ShowProgress(run, 1, sequence.Length);
        Debug.Log("TrialManager: run " + run + " started, first target Sphere_" + sequence[0]);
    }

    public void StartNextRun()
    {
        if (!hasStarted) StartRun(loader.runNumber);   // first pinch starts whatever run the slider is on
        else StartRun(loader.runNumber + 1);
    }

    public void OnSphereSelected(int id)
    {
        if (sequence == null) return;
        if (currentIndex >= sequence.Length) return;

        if (id != sequence[currentIndex])
        {
            Debug.Log("TrialManager: wrong sphere " + id + ", wanted " + sequence[currentIndex]);
            return;
        }

        if (AudioFeedback.Instance != null) AudioFeedback.Instance.PlayDing();   // correct sphere

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
        if (sequence == null) return false;
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