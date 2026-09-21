using UnityEngine;

public class TrialManager : MonoBehaviour
{
    private SequenceLoader loader;
    private int[] sequence;
    private int currentIndex = 0;
    private float startTime;
    private bool running = false;

    void Start()
    {
        loader = GetComponent<SequenceLoader>();
        sequence = loader.GetSequence();
        Debug.Log("TrialManager: first target is Sphere_" + sequence[0]);
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
        Debug.Log("TrialManager: hit " + id + " (" + currentIndex + " of " + sequence.Length + ")");

        if (currentIndex >= sequence.Length)
        {
            float elapsed = Time.time - startTime;
            running = false;
            Debug.Log("TrialManager: run " + loader.runNumber + " complete in " + elapsed.ToString("F2") + " s");
        }
    }
}