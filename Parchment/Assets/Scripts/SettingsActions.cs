using UnityEngine;

// The Settings page's actions, one public method per button, wired through each MenuButton's On Press.
// Also the Mute button's state (IToggleState). Reset returns everything to what the Inspector said at launch.
public class SettingsActions : MonoBehaviour, IToggleState
{
    public ParchmentFollow follow;   // drag the object with ParchmentFollow here
    public PhantomMode phantom;      // drag TaskCube here
    public TrialManager trial;       // drag TaskCube here
    public SyncSweep sweep;          // drag TaskCube here
    public ParchmentMode parchment;  // drag TaskCube here; Reset rolls the board up

    public float distanceStep = 0.03f;   // metres per Dist press
    public float tiltStep = 5f;          // degrees per Tilt press
    public float padStep = 0.01f;        // metres per Pad press
    public float minDistance = 0.15f;
    public float maxDistance = 0.8f;
    public float minTilt = 0f;
    public float maxTilt = 75f;

    public bool muted;
    public bool IsOn => muted;

    // Defaults captured at launch: whatever the Inspector said when the app started.
    private float defaultDistance, defaultTilt, defaultPad;

    void Start()
    {
        if (follow != null) { defaultDistance = follow.distance; defaultTilt = follow.tilt; }
        if (phantom != null && phantom.pad != null) defaultPad = phantom.pad.padWidth;
        AudioListener.volume = muted ? 0f : 1f;
    }

    public void DistMinus() { Dist(-distanceStep); }
    public void DistPlus()  { Dist(distanceStep); }
    public void TiltMinus() { Tilt(-tiltStep); }
    public void TiltPlus()  { Tilt(tiltStep); }
    public void PadMinus()  { if (phantom != null) phantom.Nudge(-padStep); }
    public void PadPlus()   { if (phantom != null) phantom.Nudge(padStep); }

    public void ToggleMute()
    {
        muted = !muted;
        AudioListener.volume = muted ? 0f : 1f;
    }

    // Everything back to launch values, trial back to "Double pinch to start" in practice, sync sweep plays again on the next unroll.
    public void ResetAll()
    {
        if (follow != null) { follow.distance = defaultDistance; follow.tilt = defaultTilt; }
        if (phantom != null && phantom.pad != null) phantom.pad.padWidth = defaultPad;
        muted = false;
        AudioListener.volume = 1f;
        if (trial != null) { trial.practice = true; trial.ResetSession(); }
        if (phantom != null) phantom.wanted = false;
        if (parchment != null) parchment.SetUnrolled(false);
        if (sweep != null) sweep.Rearm();
    }

    void Dist(float d) { if (follow != null) follow.distance = Mathf.Clamp(follow.distance + d, minDistance, maxDistance); }
    void Tilt(float d) { if (follow != null) follow.tilt = Mathf.Clamp(follow.tilt + d, minTilt, maxTilt); }
}
