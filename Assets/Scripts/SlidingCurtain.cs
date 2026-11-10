using System.Collections;
using UnityEngine;

/// <summary>
/// Sliding shower curtain made of vertical strips (plus their ring hooks). u = 0 is fully closed (strips spread
/// along the rail), u = 1 is fully open (strips bunched up at one end of the rail). Positions are stored in the
/// coordinates of the group that owns the strips, which sits at the origin without rotation, so they equal
/// world coordinates and are applied through localPosition / localScale.
/// </summary>
public class SlidingCurtain : MonoBehaviour
{
    public Transform[] strips = new Transform[0];
    public Vector3[] closedPos = new Vector3[0];
    public Vector3[] openPos = new Vector3[0];
    public Vector3[] closedScale = new Vector3[0];
    public Vector3[] openScale = new Vector3[0];
    /// <summary>Current opening, 0 = closed, 1 = open.</summary>
    public float open;
    public float duration = 0.9f;
    Coroutine anim;

    public bool IsOpen { get { return open > 0.5f; } }

    public void SetOpen(float u)
    {
        open = Mathf.Clamp01(u);
        int n = Mathf.Min(strips.Length, Mathf.Min(closedPos.Length, Mathf.Min(openPos.Length, Mathf.Min(closedScale.Length, openScale.Length))));
        for (int i = 0; i < n; i++)
        {
            var t = strips[i];
            if (t == null) continue;
            // strips travel in a slight arc so the bunching reads as cloth gathering, not a flat slide
            float k = Smooth(open);
            t.localPosition = Vector3.Lerp(closedPos[i], openPos[i], k);
            t.localScale = Vector3.Lerp(closedScale[i], openScale[i], k);
        }
    }

    static float Smooth(float u) { return u * u * (3f - 2f * u); }

    /// <summary>Animates toward the target opening (0 or 1).</summary>
    public void Slide(float target)
    {
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(SlideR(Mathf.Clamp01(target)));
    }

    public void Toggle() { Slide(IsOpen ? 0f : 1f); }

    IEnumerator SlideR(float target)
    {
        float from = open, t = 0f, dur = Mathf.Max(0.05f, duration);
        while (t < dur)
        {
            t += Time.deltaTime;
            SetOpen(Mathf.Lerp(from, target, Mathf.Clamp01(t / dur)));
            yield return null;
        }
        SetOpen(target);
        anim = null;
    }
}
