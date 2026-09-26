using UnityEngine;

// Synthesized UI sounds: a soft hover tock, a menu blip, and a two-note ding. No audio assets needed.
[RequireComponent(typeof(AudioSource))]
public class AudioFeedback : MonoBehaviour
{
    public static AudioFeedback Instance { get; private set; }

    // Settings-panel candidates. Hover must stay very soft; it's a texture, not an alert.
    [Range(0f, 1f)] public float clickVolume = 0.02f;
    [Range(0f, 1f)] public float blipVolume = 0.25f;
    [Range(0f, 1f)] public float dingVolume = 0.5f;

    // Fast sweeps: clicks closer than minGap are dropped; closer than fullGap are scaled down toward minScale.
    public float clickMinGap = 0.025f;
    public float clickFullGap = 0.15f;
    [Range(0f, 1f)] public float clickMinScale = 0.25f;

    private AudioSource source;
    private AudioClip click;
    private AudioClip blip;
    private AudioClip ding;
    private float lastClickTime = float.NegativeInfinity;

    const int SampleRate = 44100;

    void Awake()
    {
        Instance = this;
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;          // UI sound, not positioned in the world

        click = MakeClick();
        blip = MakeBlip();
        ding = MakeDing();
    }

    public void PlayClick()
    {
        if (click == null) return;
        if (Time.time < HandPointer.SuppressConfirmUntil) return;   // palm menu is up: no hover ticks

        float gap = Time.time - lastClickTime;
        if (gap < clickMinGap) return;

        // Rapid clicks get quieter: full volume at fullGap and above, minScale at minGap.
        float t = Mathf.InverseLerp(clickMinGap, clickFullGap, gap);
        float scale = Mathf.Lerp(clickMinScale, 1f, t);

        lastClickTime = Time.time;
        source.PlayOneShot(click, clickVolume * scale);
    }

    // Menu button pressed. Not gated by the palm menu (that's exactly when it plays).
    public void PlayBlip()
    {
        if (blip != null) source.PlayOneShot(blip, blipVolume);
    }

    public void PlayDing()
    {
        if (ding != null) source.PlayOneShot(ding, dingVolume);
    }

    // Raised-cosine fade to zero over the last `portion` of a clip, so nothing ends on a snap.
    static float Tail(float u, float portion)
    {
        float start = 1f - portion;
        if (u <= start) return 1f;
        return 0.5f * (1f + Mathf.Cos(Mathf.PI * (u - start) / portion));
    }

    // 30 ms: a dull, padded tock. 450 Hz tone, soft attack, almost no noise, smooth fade over the second half.
    AudioClip MakeClick()
    {
        float seconds = 0.03f;
        int n = Mathf.RoundToInt(SampleRate * seconds);
        float[] data = new float[n];
        System.Random rng = new System.Random(12345);

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SampleRate;
            float u = i / (float)(n - 1);
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            float tone = Mathf.Sin(2f * Mathf.PI * 450f * t);
            float attack = Mathf.Min(1f, t / 0.004f);
            float decay = Mathf.Exp(-t / 0.006f);
            data[i] = (0.08f * noise + 0.92f * tone) * attack * decay * Tail(u, 0.5f);
        }

        AudioClip c = AudioClip.Create("Click", n, 1, SampleRate, false);
        c.SetData(data, 0);
        return c;
    }

    // 90 ms: a single 660 Hz (E5) tone with a soft attack. Confirmation, not celebration.
    AudioClip MakeBlip()
    {
        float seconds = 0.09f;
        int n = Mathf.RoundToInt(SampleRate * seconds);
        float[] data = new float[n];

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SampleRate;
            float u = i / (float)(n - 1);
            float tone = Mathf.Sin(2f * Mathf.PI * 660f * t);
            float attack = Mathf.Min(1f, t / 0.006f);
            float decay = Mathf.Exp(-t / 0.035f);
            data[i] = tone * attack * decay * Tail(u, 0.4f);
        }

        AudioClip c = AudioClip.Create("Blip", n, 1, SampleRate, false);
        c.SetData(data, 0);
        return c;
    }

    // 400 ms: A5 (880 Hz) and E6 (1319 Hz) together, gentle 8 ms attack, ringing out, cosine fade over the last third.
    AudioClip MakeDing()
    {
        float seconds = 0.4f;
        int n = Mathf.RoundToInt(SampleRate * seconds);
        float[] data = new float[n];

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SampleRate;
            float u = i / (float)(n - 1);
            float a = Mathf.Sin(2f * Mathf.PI * 880f * t);
            float e = Mathf.Sin(2f * Mathf.PI * 1318.5f * t);
            float attack = Mathf.Min(1f, t / 0.008f);
            float decay = Mathf.Exp(-t / 0.13f);
            data[i] = (0.65f * a + 0.35f * e) * attack * decay * Tail(u, 0.35f);
        }

        AudioClip c = AudioClip.Create("Ding", n, 1, SampleRate, false);
        c.SetData(data, 0);
        return c;
    }
}