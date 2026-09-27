using System.Collections.Generic;
using UnityEngine;

// First-unroll sync: bulbs arrive red (not wired yet); a beat later the scanner "reads" each one, so the bulb
// and its sphere flash green together with a tick per column; then the bulb settles to white. Runs once per
// session unless Rearm() is called. Purely visual: never touches selection or the target.
public class SyncSweep : MonoBehaviour
{
    public ParchmentMode mode;          // drag TaskCube here (ParchmentMode)
    public ParchmentScanner scanner;    // drag TaskCube here (ParchmentScanner): column count and the cube
    public Material haloMaterial;       // HaloMat: URP Unlit, Transparent, green with alpha (sphere flash)

    [Header("Timing")]
    public float sweepSeconds = 2f;     // bulb arrival is stretched to this for the sync run
    public float readLag = 0.35f;       // how long a bulb stays red before the scanner reads it
    public float flashSeconds = 0.25f;  // green on the bulb, halo on the sphere

    [Header("Look")]
    public Color unsyncedColor = new Color(0.9f, 0.25f, 0.2f);
    public Color syncColor = new Color(0.2f, 0.9f, 0.3f);
    public float haloScale = 1.7f;      // relative to the sphere

    [Header("Sound")]
    [Range(0f, 1f)] public float tickVolume = 0.08f;
    public float tickPitchLow = 0.8f;   // first column
    public float tickPitchHigh = 1.6f;  // last column
    [Range(0f, 1f)] public float chimeVolume = 0.3f;   // the "synced" chime: two rising notes, distinct from the selection ding
    public float chimeDelay = 0.35f;   // beat of silence between the last tick and the chime

    [Header("Glow")]
    public Color glowColor = Color.white;   // every bulb flares to this on the chime
    public float glowSeconds = 0.6f;        // and eases back to its own color over this long

    private class Entry
    {
        public Bulb bulb;
        public Transform sphere;
        public float shownAt;
        public int stage;               // 0 = red, 1 = reading (green)
    }

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private readonly List<Entry> pending = new List<Entry>();
    private readonly List<GameObject> halos = new List<GameObject>();
    private readonly List<float> haloStart = new List<float>();
    private MaterialPropertyBlock haloBlock;
    private Color haloColor;
    private AudioSource tickSource;
    private AudioClip tick;
    private AudioClip chime;
    private float normalSeconds;
    private float lastTickX = float.NaN;
    private int tickIndex;
    private bool sawAny;
    private bool done;
    private bool glowing;
    private float glowStart;

    void Start()
    {
        if (mode == null) { enabled = false; return; }
        normalSeconds = mode.bulbsInSeconds;

        haloBlock = new MaterialPropertyBlock();
        haloColor = haloMaterial != null && haloMaterial.HasProperty(BaseColorId) ? haloMaterial.GetColor(BaseColorId) : syncColor;

        tickSource = gameObject.AddComponent<AudioSource>();
        tickSource.playOnAwake = false;
        tickSource.spatialBlend = 0f;
        tick = MakeTick();
        chime = MakeChime();

        Rearm();
    }

    void OnDestroy()
    {
        if (mode != null) mode.BulbShown -= OnBulbShown;
    }

    // Arm, or re-arm from a Settings "reset" button, so the next unroll runs the sync again.
    public void Rearm()
    {
        if (mode == null) return;
        foreach (Entry e in pending) if (e.bulb != null) e.bulb.ClearOverride();
        pending.Clear();
        foreach (GameObject h in halos) if (h != null) h.SetActive(false);
        mode.BulbShown -= OnBulbShown;
        mode.BulbShown += OnBulbShown;
        mode.bulbsInSeconds = sweepSeconds;
        mode.bulbsRide = false;
        lastTickX = float.NaN;
        tickIndex = 0;
        sawAny = false;
        done = false;
        glowing = false;
    }

    // A bulb just popped in: paint it red and queue its read.
    void OnBulbShown(Transform t)
    {
        if (done) return;
        Bulb b = t.GetComponent<Bulb>();
        if (b == null) return;
        b.SetOverride(unsyncedColor);
        Transform cube = scanner != null && scanner.taskCube != null ? scanner.taskCube : transform;
        Entry e = new Entry();
        e.bulb = b;
        e.sphere = cube.Find("Sphere_" + b.id);
        e.shownAt = Time.time;
        pending.Add(e);
        sawAny = true;
    }

    void Update()
    {
        if (glowing) UpdateGlow();
        if (done) return;

        // Rolled up mid-sweep: stop quietly.
        if (sawAny && !mode.Unrolled) { Finish(false); return; }

        // ParchmentMode hands interaction back when the bulbs land; hold it until the reads have played out too.
        if (sawAny && mode.Ready)
        {
            if (mode.hover != null) mode.hover.enabled = false;
            if (mode.selector != null) mode.selector.enabled = false;
        }

        float now = Time.time;
        for (int i = pending.Count - 1; i >= 0; i--)
        {
            Entry e = pending[i];
            if (e.stage == 0 && now - e.shownAt >= readLag)
            {
                e.stage = 1;
                if (e.bulb != null) e.bulb.SetOverride(syncColor);
                if (e.sphere != null) Halo(e.sphere);
                Tick(e.bulb);
            }
            else if (e.stage == 1 && now - e.shownAt >= readLag + flashSeconds)
            {
                if (e.bulb != null) e.bulb.ClearOverride();
                pending.RemoveAt(i);
            }
        }

        UpdateHalos();

        if (sawAny && mode.Ready && pending.Count == 0) Finish(true);
    }

    void Finish(bool completed)
    {
        done = true;
        mode.BulbShown -= OnBulbShown;
        mode.bulbsInSeconds = normalSeconds;
        mode.bulbsRide = true;
        foreach (Entry e in pending) if (e.bulb != null) e.bulb.ClearOverride();
        pending.Clear();
        foreach (GameObject h in halos) if (h != null) h.SetActive(false);
        if (!completed) return;
        if (mode.hover != null) mode.hover.enabled = true;
        if (mode.selector != null) mode.selector.enabled = true;
        Invoke(nameof(PlayChime), chimeDelay);
    }

    void PlayChime()
    {
        glowing = true;                       // the flare lands with the sound
        glowStart = Time.time;
        if (tickSource == null || chime == null) return;
        tickSource.pitch = 1f;
        tickSource.PlayOneShot(chime, chimeVolume);
    }

    // Chime flare: every bulb goes to glowColor at once, then eases back to its own color. Purely visual.
    void UpdateGlow()
    {
        if (scanner == null || scanner.bulbParent == null) { glowing = false; return; }
        float k = Mathf.Clamp01((Time.time - glowStart) / Mathf.Max(glowSeconds, 0.01f));
        float ease = 1f - (1f - k) * (1f - k);   // fast out, slow settle
        foreach (Transform t in scanner.bulbParent)
        {
            Bulb b = t.GetComponent<Bulb>();
            if (b == null) continue;
            if (k >= 1f) b.ClearOverride();
            else b.SetOverride(Color.Lerp(glowColor, b.idleColor, ease));
        }
        if (k >= 1f) glowing = false;
    }

    // One tick per column: fires when the read moves to a new X on the sheet, pitch rising left to right.
    void Tick(Bulb b)
    {
        if (b == null || mode.sheet == null || tickSource == null) return;
        float x = mode.sheet.InverseTransformPoint(b.transform.position).x;
        if (!float.IsNaN(lastTickX) && Mathf.Abs(x - lastTickX) < 0.004f) return;
        lastTickX = x;
        int cols = scanner != null ? Mathf.Max(scanner.columns, 2) : 10;
        tickSource.pitch = Mathf.Lerp(tickPitchLow, tickPitchHigh, Mathf.Clamp01((float)tickIndex / (cols - 1)));
        tickSource.PlayOneShot(tick, tickVolume);
        tickIndex++;
    }

    // A pooled transparent sphere parented to the real sphere; it grows a little and fades over flashSeconds.
    void Halo(Transform sphere)
    {
        if (haloMaterial == null) return;
        int idx = -1;
        for (int i = 0; i < halos.Count; i++) if (!halos[i].activeSelf) { idx = i; break; }
        if (idx < 0)
        {
            GameObject h = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            h.name = "SyncHalo";
            Destroy(h.GetComponent<Collider>());                      // never block a ray to the real sphere
            Renderer r = h.GetComponent<Renderer>();
            r.sharedMaterial = haloMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            halos.Add(h);
            haloStart.Add(0f);
            idx = halos.Count - 1;
        }
        GameObject halo = halos[idx];
        halo.transform.SetParent(sphere, false);
        halo.transform.localPosition = Vector3.zero;
        halo.transform.localScale = Vector3.one * haloScale * 0.7f;
        halo.SetActive(true);
        haloStart[idx] = Time.time;
    }

    void UpdateHalos()
    {
        for (int i = 0; i < halos.Count; i++)
        {
            GameObject h = halos[i];
            if (h == null || !h.activeSelf) continue;
            float k = Mathf.Clamp01((Time.time - haloStart[i]) / Mathf.Max(flashSeconds, 0.01f));
            h.transform.localScale = Vector3.one * haloScale * (0.7f + 0.3f * k);
            Renderer r = h.GetComponent<Renderer>();
            Color c = haloColor;
            c.a = haloColor.a * (1f - k);
            r.GetPropertyBlock(haloBlock);
            haloBlock.SetColor(BaseColorId, c);
            r.SetPropertyBlock(haloBlock);
            if (k >= 1f) h.SetActive(false);
        }
    }

    // A 40 ms sine blip with a fast decay, made in code so no audio asset is needed.
    AudioClip MakeTick()
    {
        const int rate = 44100;
        int n = (int)(rate * 0.04f);
        float[] data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            data[i] = Mathf.Sin(2f * Mathf.PI * 1100f * t) * Mathf.Exp(-t * 110f);
        }
        AudioClip clip = AudioClip.Create("SyncTick", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Two rising notes a fifth apart, soft attack and decay: reads as "paired", not as the selection ding.
    AudioClip MakeChime()
    {
        const int rate = 44100;
        int n = (int)(rate * 0.45f);
        float[] data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            float a = Note(660f, t) * 0.5f;
            float b = Note(990f, t - 0.11f) * 0.5f;
            data[i] = a + b;
        }
        AudioClip clip = AudioClip.Create("SyncChime", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // One sine note with a touch of second harmonic, 3 ms attack, exponential decay. Silent before t = 0.
    static float Note(float freq, float t)
    {
        if (t < 0f) return 0f;
        float attack = Mathf.Clamp01(t / 0.003f);
        float decay = Mathf.Exp(-t * 8f);
        float wave = Mathf.Sin(2f * Mathf.PI * freq * t) + 0.3f * Mathf.Sin(4f * Mathf.PI * freq * t);
        return wave * attack * decay / 1.3f;
    }
}
