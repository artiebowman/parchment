using UnityEngine;
using System.Collections.Generic;

// The phantom trackpad: a small slab floating above the board, drawn as a wire outline with a 10x10 grid.
// A fingertip over it maps corner-for-corner onto the bulb grid, so the pad's size IS the sensitivity.
[DefaultExecutionOrder(-55)]   // after ParchmentFollow (-60) moves the sheet, before ParchmentHover (-50) reads us
public class PhantomPad : MonoBehaviour
{
    public Transform parchment;        // drag Parchment here; the pad follows its pose
    public PhantomMode phantom;        // drag TaskCube here; pad is only drawn while Phantom is live
    public ParchmentScanner scanner;   // drag TaskCube here; lit bulbs get mirrored onto the pad
    public Material lineMaterial;      // drag PadMat here
    public Vector3 offset = new Vector3(0f, 0.10f, 0f);   // meters from the sheet center: right, above the plane, toward the far edge
    public float padWidth = 0.065f;    // side length; smaller = more sensitive
    public float padThickness = 0.01f; // drawn depth of the slab, a guide for the eye only
    public float activeHeight = 0.15f; // fingertip counts while within this height above or below the pad's plane
    public int columns = 10;           // grid lines match the bulbs
    public float reticleSize = 0.01f;  // the exact finger dot
    public Color reticleColor = Color.white;
    public float markerSize = 0.012f;  // mirrored bulb dots
    public Color targetColor = new Color(0.78f, 0.55f, 1f, 1f);     // mirrored target bulb: bright purple
    public Color hoverColor = new Color(0.6f, 0.45f, 0.9f, 0.35f);  // mirrored hovered bulb: dim, see-through (alpha 0 hides it)
    public float lineWidth = 0.002f;   // finger-to-target guide line
    public Color lineColor = new Color(1f, 0.85f, 0.2f, 1f);         // finger-to-target guide line: warm yellow

    private MeshFilter filter;
    private MeshRenderer rend;
    private LineRenderer line;
    private float builtWidth = -1f, builtThickness = -1f;
    private int builtColumns = -1;
    private GameObject[] reticles = new GameObject[2];
    private bool[] reticleShown = new bool[2];
    private Vector3[] reticleLocal = new Vector3[2];
    private List<GameObject> markers = new List<GameObject>();
    private MaterialPropertyBlock mpb;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    void Awake()
    {
        filter = gameObject.AddComponent<MeshFilter>();
        rend = gameObject.AddComponent<MeshRenderer>();
        rend.sharedMaterial = lineMaterial;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
        mpb = new MaterialPropertyBlock();

        GameObject lo = new GameObject("PadLine");
        lo.transform.SetParent(transform, false);
        line = lo.AddComponent<LineRenderer>();
        line.sharedMaterial = lineMaterial;
        line.useWorldSpace = false;
        line.positionCount = 2;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.enabled = false;
    }

    // Fingertip in; its spot on the pad out, -1..1 across and -1..1 up the pad. False when the finger isn't over the pad.
    public bool TryMap(Vector3 fingertip, out Vector2 uv)
    {
        uv = Vector2.zero;
        Vector3 local = transform.InverseTransformPoint(fingertip);
        float half = padWidth * 0.5f;
        if (Mathf.Abs(local.x) > half || Mathf.Abs(local.z) > half || Mathf.Abs(local.y) > activeHeight) return false;
        uv = new Vector2(local.x / half, local.z / half);
        return true;
    }

    // The exact dot on the pad's top face under this hand's fingertip (slot 0 = left, 1 = right). Made on first use. Never snaps.
    public void ShowReticle(int slot, Vector3 fingertip)
    {
        if (slot < 0 || slot >= reticles.Length) return;
        if (reticles[slot] == null)
        {
            GameObject r = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            r.name = "PadReticle_" + slot;
            Destroy(r.GetComponent<Collider>());
            r.GetComponent<Renderer>().sharedMaterial = lineMaterial;
            r.transform.SetParent(transform, false);
            reticles[slot] = r;
        }
        Vector3 local = transform.InverseTransformPoint(fingertip);
        float half = padWidth * 0.5f;
        local.x = Mathf.Clamp(local.x, -half, half);
        local.z = Mathf.Clamp(local.z, -half, half);
        local.y = padThickness * 0.5f;
        reticles[slot].transform.localPosition = local;
        reticles[slot].transform.localScale = Vector3.one * reticleSize;
        Renderer rr = reticles[slot].GetComponent<Renderer>();
        rr.GetPropertyBlock(mpb);
        mpb.SetColor(BaseColorId, reticleColor);
        rr.SetPropertyBlock(mpb);
        reticles[slot].SetActive(true);
        reticleShown[slot] = true;
        reticleLocal[slot] = local;
    }

    void Update()
    {
        if (parchment != null)
        {
            transform.rotation = parchment.rotation;
            transform.position = parchment.position + parchment.right * offset.x + parchment.up * offset.y + parchment.forward * offset.z;
        }
        if (padWidth != builtWidth || padThickness != builtThickness || columns != builtColumns) Build();
        rend.enabled = phantom != null && phantom.Active;
    }

    void LateUpdate()
    {
        MirrorBulbs();   // reads this frame's reticles, so it runs before they're cleared
        for (int i = 0; i < reticles.Length; i++)
        {
            if (reticles[i] != null && !reticleShown[i]) reticles[i].SetActive(false);
            reticleShown[i] = false;   // ParchmentHover sets it again next frame
        }
    }

    // Any bulb that's lit on the board (target, hovered) gets a matching dot on the pad, so you can aim without looking down,
    // plus a guide line from your finger dot to the target dot.
    void MirrorBulbs()
    {
        int used = 0;
        bool haveTarget = false;
        Vector3 targetLocal = Vector3.zero;
        bool live = phantom != null && phantom.Active && scanner != null && scanner.bulbParent != null && parchment != null;
        if (live)
        {
            float half = padWidth * 0.5f;
            foreach (Transform child in scanner.bulbParent)
            {
                Bulb b = child.GetComponent<Bulb>();
                if (b == null || b.state == Bulb.State.Idle) continue;
                Vector3 local = parchment.InverseTransformPoint(child.position);
                Vector2 uv = new Vector2(local.x / phantom.boardHalf, local.z / phantom.boardHalf);
                Vector3 pos = new Vector3(uv.x * half, padThickness * 0.5f, uv.y * half);
                bool isTarget = b.state == Bulb.State.Target || b.state == Bulb.State.HoverTarget;
                if (isTarget) { haveTarget = true; targetLocal = pos; }
                GameObject m = Marker(used++);
                m.transform.localPosition = pos;
                m.transform.localScale = Vector3.one * markerSize;
                Renderer r = m.GetComponent<Renderer>();
                r.GetPropertyBlock(mpb);
                mpb.SetColor(BaseColorId, isTarget ? targetColor : hoverColor);
                r.SetPropertyBlock(mpb);
                m.SetActive(true);
            }
        }
        for (int i = used; i < markers.Count; i++) markers[i].SetActive(false);

        int slot = reticleShown[1] ? 1 : (reticleShown[0] ? 0 : -1);   // right hand's dot if it's on the pad, else left
        bool showLine = live && haveTarget && slot >= 0;
        line.enabled = showLine;
        if (showLine)
        {
            line.SetPosition(0, reticleLocal[slot]);
            line.SetPosition(1, targetLocal);
            line.GetPropertyBlock(mpb);
            mpb.SetColor(BaseColorId, lineColor);
            line.SetPropertyBlock(mpb);
        }
    }

    GameObject Marker(int i)
    {
        while (markers.Count <= i)
        {
            GameObject m = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m.name = "PadMarker_" + markers.Count;
            Destroy(m.GetComponent<Collider>());
            m.GetComponent<Renderer>().sharedMaterial = lineMaterial;
            m.transform.SetParent(transform, false);
            markers.Add(m);
        }
        return markers[i];
    }

    // Outline of the slab plus the grid on its top face, as one mesh made of line segments. Rebuilt when a size changes.
    void Build()
    {
        builtWidth = padWidth; builtThickness = padThickness; builtColumns = columns;
        float h = padWidth * 0.5f, t = padThickness * 0.5f;
        List<Vector3> verts = new List<Vector3>();
        List<int> idx = new List<int>();

        void Seg(Vector3 a, Vector3 b) { idx.Add(verts.Count); verts.Add(a); idx.Add(verts.Count); verts.Add(b); }

        // Twelve edges of the box.
        for (int s = -1; s <= 1; s += 2)
        {
            float y = t * s;
            Seg(new Vector3(-h, y, -h), new Vector3(h, y, -h));
            Seg(new Vector3(h, y, -h), new Vector3(h, y, h));
            Seg(new Vector3(h, y, h), new Vector3(-h, y, h));
            Seg(new Vector3(-h, y, h), new Vector3(-h, y, -h));
        }
        Seg(new Vector3(-h, -t, -h), new Vector3(-h, t, -h));
        Seg(new Vector3(h, -t, -h), new Vector3(h, t, -h));
        Seg(new Vector3(h, -t, h), new Vector3(h, t, h));
        Seg(new Vector3(-h, -t, h), new Vector3(-h, t, h));

        // Grid on the top face: one cell per bulb.
        for (int c = 1; c < columns; c++)
        {
            float p = -h + padWidth * c / columns;
            Seg(new Vector3(p, t, -h), new Vector3(p, t, h));
            Seg(new Vector3(-h, t, p), new Vector3(h, t, p));
        }

        Mesh m = new Mesh();
        m.name = "PhantomPad";
        m.SetVertices(verts);
        m.SetIndices(idx.ToArray(), MeshTopology.Lines, 0);
        m.RecalculateBounds();
        filter.sharedMesh = m;
    }
}
