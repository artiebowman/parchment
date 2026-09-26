using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

public class ParchmentMode : MonoBehaviour, IToggleState
{
    public GameObject parchment;        // drag Parchment here
    public ParchmentHover hover;        // drag TaskCube here
    public ParchmentSelector selector;  // drag TaskCube here

    public bool startUnrolled = true;

    public bool Unrolled { get; private set; }

    // IToggleState: the menu button shows green when the sheet is unrolled.
    public bool IsOn => Unrolled;

    void Start()
    {
        SetUnrolled(startUnrolled);
    }

    void Update()
    {
#if UNITY_EDITOR
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            Toggle();
        }
#endif
    }

    public void Toggle()
    {
        SetUnrolled(!Unrolled);
    }

    public void SetUnrolled(bool value)
    {
        Unrolled = value;
        if (parchment != null) parchment.SetActive(value);
        if (hover != null) hover.enabled = value;
        if (selector != null) selector.enabled = value;
    }
}