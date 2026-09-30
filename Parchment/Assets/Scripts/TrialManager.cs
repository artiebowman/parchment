using UnityEngine;

public class TrialManager : MonoBehaviour, IToggleState
{
    // The referee. Owns the sequence, the current target index, the clock, and the practice flag.
    // The clock starts on the first correct selection, not on run start: setup time is free. A wrong sphere is logged and ignored, no penalty.
    // IsCurrentTarget is the one target read feedback scripts use. Selection scripts never call it.
    // ---- State ----
    private SequenceLoader loader;
    private int[] sequence;
    private int currentIndex = 0;
    private float startTime;
    private bool running = false;
    private bool hasStarted = false;    // false until run 1 is kicked off by a double pinch
    private bool practicing = false;    // the run in progress (or just finished) was a practice run
    private Renderer currentTarget;
    private Color originalColor;

    // ---- Wiring and knobs ----
    public Scoreboard board;
    public RunLog log;                  // optional: the run-by-run list beside the cube
    public int totalRuns = 7;
    public bool practice = true;        // on by default. Runs started while on are unscored and don't advance the counter; off = live
    public bool RunFinished { get; private set; }
    public bool HasStarted => hasStarted;   // false until run 1 begins; CubeRaySelector asks for a double pinch until then

    // IToggleState for the Practice button.
    // ---- Practice: snapshotted at run start, so flipping it mid-run does not change what that run was ----
    public bool IsOn => practice;
    public void TogglePractice() { practice = !practice; }

    void Start()
    {
        loader = GetComponent<SequenceLoader>();
        ResetSession();
    }

    // Back to the very beginning: run 1 on deck, nothing highlighted, waiting for the double pinch.
    public void ResetSession()
    {
        ClearHighlight();
        sequence = null;
        currentIndex = 0;
        running = false;
        hasStarted = false;
        practicing = false;
        RunFinished = true;
        if (loader != null) loader.runNumber = 1;
        if (log != null) log.Clear();
        board.ShowPrompt("Double Pinch to Start");
    }

    public void StartRun(int run)
    {
        if (run > totalRuns)
        {
            board.ShowPrompt("All " + totalRuns + " Runs Complete");
            return;
        }

        loader.runNumber = run;
        sequence = loader.GetSequence();
        currentIndex = 0;
        running = false;
        hasStarted = true;
        practicing = practice;
        RunFinished = false;
        HighlightTarget();
        board.ShowProgress(run, 1, sequence.Length, practicing);
        Debug.Log("TrialManager: run " + run + (practicing ? " (practice)" : "") + " started, first target Sphere_" + sequence[0]);
    }

    // The run on deck: the same number again after a practice run (or before run 1), the next one after a real run.
    public void StartNextRun()
    {
        int run = (!hasStarted || practicing) ? loader.runNumber : loader.runNumber + 1;
        StartRun(run);
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
            board.ShowRunTime(loader.runNumber, elapsed, practicing);
            if (log != null) log.Add(loader.runNumber, elapsed, practicing);
            Debug.Log("TrialManager: run " + loader.runNumber + (practicing ? " (practice)" : "") + " complete in " + elapsed.ToString("F2") + " s");
        }
        else
        {
            board.ShowProgress(loader.runNumber, currentIndex + 1, sequence.Length, practicing);
        }
    }

    public bool IsCurrentTarget(int id)
    {
        if (sequence == null) return false;
        if (currentIndex >= sequence.Length) return false;
        return id == sequence[currentIndex];
    }

    private void ClearHighlight()
    {
        if (currentTarget != null) currentTarget.material.color = originalColor;
        currentTarget = null;
    }

    private void HighlightTarget()
    {
        ClearHighlight();
        if (sequence == null || currentIndex >= sequence.Length) return;

        Transform sphere = transform.Find("Sphere_" + sequence[currentIndex]);
        currentTarget = sphere.GetComponent<Renderer>();
        originalColor = currentTarget.material.color;
        currentTarget.material.color = Color.green;
    }
}
