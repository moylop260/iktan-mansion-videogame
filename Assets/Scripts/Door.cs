using UnityEngine;

/// <summary>Puerta (gira) o reja (desaparece). Queda registrada como abierta para los checkpoints.</summary>
public class Door : MonoBehaviour
{
    public string id;
    public GameObject leaf;
    public bool hide;
    public float openAngle = 90f;
    bool open;
    float cur;

    public bool IsOpen { get { return open; } }

    public void Open(bool instant = false)
    {
        if (open) return;
        open = true;
        if (GameManager.I != null) GameManager.I.doors.Add(id);
        var it = leaf.GetComponent<Interactable>();
        if (it != null) it.enabled = false;
        if (GameManager.I != null && GameManager.I.audio != null && !instant) GameManager.I.audio.At("creak", transform.position, 0.9f, hide ? 1.4f : 1f);
        if (hide) { leaf.SetActive(false); return; }
        if (instant) { cur = openAngle; transform.localRotation = Quaternion.Euler(0f, cur, 0f); }
    }

    void Update()
    {
        if (hide || !open) return;
        cur = Mathf.MoveTowards(cur, openAngle, 140f * Time.deltaTime);
        transform.localRotation = Quaternion.Euler(0f, cur, 0f);
    }
}
