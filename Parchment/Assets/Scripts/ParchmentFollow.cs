using UnityEngine;

[DefaultExecutionOrder(-60)]   // before ParchmentHover (-50), so hover reads this frame's pose
public class ParchmentFollow : MonoBehaviour
{
    // Keeps the board ahead of the body: head forward flattened to the floor, fixed distance and height, tilted back. Glides so jitter never shakes it; snaps on unroll.
    public Transform parchment;     // drag Parchment here
    public Transform head;          // drag CenterEyeAnchor here (falls back to Camera.main)

    public float distance = 0.35f;      // metres ahead of the head, along body yaw
    public float heightOffset = -0.35f; // metres relative to eye height (negative = below)
    public float tilt = 30f;            // degrees the sheet leans back toward you
    public float followSpeed = 6f;      // higher = snappier glide; 0 = hard lock

    private bool wasActive;

    void Update()
    {
        if (parchment == null) return;

        Transform h = head;
        if (h == null && Camera.main != null) h = Camera.main.transform;
        if (h == null) return;

        // Body yaw: head forward flattened onto the floor. Looking up or down doesn't move the sheet.
        Vector3 flatForward = Vector3.ProjectOnPlane(h.forward, Vector3.up);
        if (flatForward.sqrMagnitude < 0.0001f) flatForward = parchment.forward;   // looking straight up/down: keep last heading
        flatForward.Normalize();

        Vector3 targetPos = h.position + flatForward * distance + Vector3.up * heightOffset;
        Quaternion targetRot = Quaternion.LookRotation(flatForward, Vector3.up) * Quaternion.Euler(-tilt, 0f, 0f);

        bool active = parchment.gameObject.activeSelf;
        bool justUnrolled = active && !wasActive;
        wasActive = active;

        if (!active) return;

        if (justUnrolled || followSpeed <= 0f)
        {
            parchment.position = targetPos;
            parchment.rotation = targetRot;
        }
        else
        {
            float t = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);   // frame-rate independent glide
            parchment.position = Vector3.Lerp(parchment.position, targetPos, t);
            parchment.rotation = Quaternion.Slerp(parchment.rotation, targetRot, t);
        }
    }
}