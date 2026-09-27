using UnityEngine;
using TMPro;

public class Scoreboard : MonoBehaviour
{
    public TMP_Text text;

    public void ShowProgress(int run, int targetIndex, int total, bool practice = false)
    {
        text.text = Label(run, practice) + "\nTarget " + targetIndex + " of " + total;
    }

    public void ShowRunTime(int run, float seconds, bool practice = false)
    {
        text.text = Label(run, practice) + " complete\n" + seconds.ToString("F2") + " s\nPinch to continue";
    }

    public void ShowPrompt(string message)
    {
        text.text = message;
    }

    static string Label(int run, bool practice) { return (practice ? "Practice run " : "Run ") + run; }
}
