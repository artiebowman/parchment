using UnityEngine;
using TMPro;

public class Scoreboard : MonoBehaviour
{
    public TMP_Text text;

    public void ShowProgress(int run, int targetIndex, int total)
    {
        text.text = "Run " + run + "\nTarget " + targetIndex + " of " + total;
    }

    public void ShowRunTime(int run, float seconds)
    {
        text.text = "Run " + run + " complete\n" + seconds.ToString("F2") + " s\nPinch to continue";
    }

    public void ShowPrompt(string message)
    {
        text.text = message;
    }
}