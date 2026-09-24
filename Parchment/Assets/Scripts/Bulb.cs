using UnityEngine;

public class Bulb : MonoBehaviour
{
    public enum State { Idle, Target, Hover }

    public int id;
    public State state = State.Idle;

    public Color idleColor = new Color(0.85f, 0.85f, 0.85f);
    public Color targetColor = new Color(0.2f, 0.9f, 0.3f);
    public Color hoverColor = new Color(1f, 0.85f, 0.2f);

    private Renderer rend;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        Apply();
    }

    public void SetState(State s)
    {
        if (state == s) return;
        state = s;
        Apply();
    }

    private void Apply()
    {
        if (rend == null) return;

        switch (state)
        {
            case State.Target: rend.material.color = targetColor; break;
            case State.Hover:  rend.material.color = hoverColor;  break;
            default:           rend.material.color = idleColor;   break;
        }
    }
}