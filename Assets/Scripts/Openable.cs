using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Puertas y cajones que se abren de verdad (animados) y dejan ver lo que hay dentro. Las piezas móviles son
/// hijas de un grupo en el origen; cada una guarda su pose cerrada y abierta.
/// </summary>
public class Openable : MonoBehaviour
{
    public class Part
    {
        public Transform t;
        public Vector3 closedPos, openPos;
        public Quaternion closedRot, openRot;
    }

    public readonly List<Part> parts = new List<Part>();
    public bool IsOpen;
    public float speed = 2.4f;
    public float Amount { get { return u; } }
    float u;

    public void Add(Transform t, Vector3 openPos, Quaternion openRot)
    {
        parts.Add(new Part { t = t, closedPos = t.position, openPos = openPos, closedRot = t.rotation, openRot = openRot });
    }

    public void SetOpen(bool open, bool instant = false)
    {
        IsOpen = open;
        if (instant) { u = open ? 1f : 0f; Apply(); }
    }

    public void Toggle() { IsOpen = !IsOpen; }

    void Update()
    {
        float target = IsOpen ? 1f : 0f;
        if (Mathf.Approximately(u, target)) return;
        u = Mathf.MoveTowards(u, target, speed * Time.deltaTime);
        Apply();
    }

    void Apply()
    {
        float e = u * u * (3f - 2f * u);
        foreach (var p in parts)
        {
            p.t.position = Vector3.Lerp(p.closedPos, p.openPos, e);
            p.t.rotation = Quaternion.Slerp(p.closedRot, p.openRot, e);
        }
    }
}

/// <summary>Oculta el objeto cuando el jugador ya tiene ese objeto en el inventario (lo que se ve dentro del cajón desaparece al tomarlo).</summary>
public class HideWhenOwned : MonoBehaviour
{
    public string item;
    void Update()
    {
        var gm = GameManager.I;
        if (gm != null && gm.Has(item)) gameObject.SetActive(false);
    }
    void Start()
    {
        var gm = GameManager.I;
        if (gm != null && gm.Has(item)) gameObject.SetActive(false);
    }
}

/// <summary>Una gota oscura que cae de un punto a un charco cada cierto tiempo y suena al caer.</summary>
public class Dripper : MonoBehaviour
{
    public Transform drop;
    public Vector3 from, to;
    public float period = 2.3f;
    float t;

    void Update()
    {
        t += Time.deltaTime;
        float cycle = t % period;
        float fall = 0.45f;
        if (cycle < fall)
        {
            float u = cycle / fall;
            drop.gameObject.SetActive(true);
            drop.position = Vector3.Lerp(from, to, u * u);
        }
        else
        {
            if (drop.gameObject.activeSelf)
            {
                drop.gameObject.SetActive(false);
                var gm = GameManager.I;
                if (gm != null && gm.audio != null) gm.audio.At("drip", to, 0.35f, Random.Range(0.9f, 1.1f));
            }
        }
    }
}
