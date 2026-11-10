using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// El cuerpo del jugador (en primera persona no se ve): un personaje armado con primitivas que solo aparece
/// en los espejos. Imita tu posición, giro y cabeza, camina moviendo brazos y piernas y sostiene la linterna
/// cuando está encendida. Con delay > 0 repite tus movimientos con retraso (el espejo embrujado).
/// </summary>
public class PlayerAvatar : MonoBehaviour
{
    struct Sample { public float t; public Vector3 pos; public float yaw, pitch; public bool torch; }

    PlayerController pc;
    GameManager gm;
    float delay;
    Transform head, armL, armR, legL, legR;
    GameObject torchObj;
    readonly List<Sample> hist = new List<Sample>();
    Vector3 lastPos; float phase; bool hasLast;

    public static PlayerAvatar Create(GameManager gm, PlayerController pc, int layer, float delay)
    {
        var go = new GameObject(delay > 0f ? "AvatarRetrasado" : "Avatar");
        var a = go.AddComponent<PlayerAvatar>();
        a.gm = gm; a.pc = pc; a.delay = delay;
        a.Build(layer);
        return a;
    }

    void Build(int layer)
    {
        var skin = Mon.Lit(new Color(0.82f, 0.62f, 0.48f), 0.3f);
        var hoodie = Mon.Lit(new Color(0.55f, 0.1f, 0.12f), 0.15f);
        var jeans = Mon.Lit(new Color(0.15f, 0.2f, 0.35f), 0.1f);
        var shoe = Mon.Lit(new Color(0.9f, 0.9f, 0.9f), 0.3f);
        var hair = Mon.Lit(new Color(0.06f, 0.04f, 0.03f), 0.2f);
        var t = transform;
        // torso con sudadera, capucha caída y mochila
        Mon.Part(t, PrimitiveType.Cube, new Vector3(0f, 1.15f, 0f), new Vector3(0.42f, 0.56f, 0.22f), hoodie);
        Mon.Part(t, PrimitiveType.Cube, new Vector3(0f, 1.42f, -0.1f), new Vector3(0.3f, 0.1f, 0.1f), hoodie);
        Mon.Part(t, PrimitiveType.Cube, new Vector3(0f, 1.12f, -0.18f), new Vector3(0.3f, 0.36f, 0.12f), Mon.Lit(new Color(0.12f, 0.12f, 0.14f), 0.1f));
        Mon.Part(t, PrimitiveType.Cube, new Vector3(0f, 0.8f, 0f), new Vector3(0.4f, 0.16f, 0.22f), jeans);
        // cabeza: cuello, cráneo, pelo, orejas
        var headRoot = new GameObject("Cabeza").transform;
        headRoot.SetParent(t, false); headRoot.localPosition = new Vector3(0f, 1.52f, 0f); head = headRoot;
        Mon.Part(headRoot, PrimitiveType.Cylinder, new Vector3(0f, -0.1f, 0f), new Vector3(0.1f, 0.06f, 0.1f), skin);
        Mon.Part(headRoot, PrimitiveType.Sphere, new Vector3(0f, 0.1f, 0f), new Vector3(0.23f, 0.26f, 0.24f), skin);
        Mon.Part(headRoot, PrimitiveType.Sphere, new Vector3(0f, 0.15f, -0.02f), new Vector3(0.25f, 0.2f, 0.26f), hair);
        Mon.Part(headRoot, PrimitiveType.Cube, new Vector3(0f, 0.2f, 0.12f), new Vector3(0.22f, 0.05f, 0.05f), hair);
        Mon.Part(headRoot, PrimitiveType.Sphere, new Vector3(-0.115f, 0.08f, 0f), new Vector3(0.04f, 0.07f, 0.05f), skin);
        Mon.Part(headRoot, PrimitiveType.Sphere, new Vector3(0.115f, 0.08f, 0f), new Vector3(0.04f, 0.07f, 0.05f), skin);
        Mon.Part(headRoot, PrimitiveType.Sphere, new Vector3(-0.045f, 0.1f, 0.105f), new Vector3(0.035f, 0.04f, 0.02f), Mon.Lit(Color.black, 0.8f));
        Mon.Part(headRoot, PrimitiveType.Sphere, new Vector3(0.045f, 0.1f, 0.105f), new Vector3(0.035f, 0.04f, 0.02f), Mon.Lit(Color.black, 0.8f));
        // brazos con manga y mano; piernas con zapatos
        armL = Limb(t, new Vector3(-0.28f, 1.38f, 0f), hoodie, skin, 0.5f, 0.09f);
        armR = Limb(t, new Vector3(0.28f, 1.38f, 0f), hoodie, skin, 0.5f, 0.09f);
        legL = Limb(t, new Vector3(-0.11f, 0.78f, 0f), jeans, shoe, 0.78f, 0.12f);
        legR = Limb(t, new Vector3(0.11f, 0.78f, 0f), jeans, shoe, 0.78f, 0.12f);
        // linterna en la mano derecha (solo cuando está encendida)
        torchObj = new GameObject("Linterna").gameObject;
        torchObj.transform.SetParent(armR, false);
        torchObj.transform.localPosition = new Vector3(0f, 0f, 0.5f);
        var body = Mon.Part(torchObj.transform, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.1f), new Vector3(0.06f, 0.1f, 0.06f), Mon.Lit(new Color(0.2f, 0.2f, 0.25f), 0.5f));
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var lens = Mon.Part(torchObj.transform, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.21f), new Vector3(0.07f, 0.015f, 0.07f), Mon.Glow(new Color(1f, 0.95f, 0.7f), 3f));
        lens.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        foreach (var tr in GetComponentsInChildren<Transform>(true)) tr.gameObject.layer = layer;
        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;
    }

    /// <summary>Extremidad: pivote en la articulación, pieza larga hacia abajo y extremo (mano o zapato).</summary>
    static Transform Limb(Transform parent, Vector3 pivotPos, Material main, Material end, float length, float thick)
    {
        var piv = new GameObject("Extremidad").transform;
        piv.SetParent(parent, false); piv.localPosition = pivotPos;
        Mon.Part(piv, PrimitiveType.Cube, new Vector3(0f, -length / 2f, 0f), new Vector3(thick, length, thick), main);
        Mon.Part(piv, PrimitiveType.Cube, new Vector3(0f, -length - 0.02f, thick * 0.4f), new Vector3(thick * 1.05f, 0.06f, thick * 1.7f), end);
        return piv;
    }

    void LateUpdate()
    {
        if (pc == null) return;
        float now = Time.time;
        hist.Add(new Sample { t = now, pos = pc.transform.position, yaw = pc.Yaw, pitch = pc.Pitch, torch = gm != null && gm.FlashOn });
        float want = now - delay;
        while (hist.Count > 2 && hist[1].t <= want) hist.RemoveAt(0);
        Sample s = hist[0];
        transform.position = s.pos;
        transform.rotation = Quaternion.Euler(0f, s.yaw, 0f);
        head.localRotation = Quaternion.Euler(Mathf.Clamp(s.pitch, -50f, 50f) * 0.8f, 0f, 0f);

        // andar: la fase avanza con la distancia recorrida
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        float speed = hasLast ? Vector3.Distance(new Vector3(s.pos.x, 0f, s.pos.z), new Vector3(lastPos.x, 0f, lastPos.z)) / dt : 0f;
        lastPos = s.pos; hasLast = true;
        speed = Mathf.Min(speed, 7f);
        phase += speed * dt * 2.4f;
        float swing = Mathf.Clamp01(speed / 2f) * 32f;
        legL.localRotation = Quaternion.Euler(Mathf.Sin(phase) * swing, 0f, 0f);
        legR.localRotation = Quaternion.Euler(-Mathf.Sin(phase) * swing, 0f, 0f);
        armL.localRotation = Quaternion.Euler(-Mathf.Sin(phase) * swing * 0.8f, 0f, 3f);
        // brazo derecho: adelantado con la linterna si está encendida
        armR.localRotation = s.torch ? Quaternion.Euler(-78f, 6f, 0f) : Quaternion.Euler(Mathf.Sin(phase) * swing * 0.8f, 0f, -3f);
        torchObj.SetActive(s.torch);
    }
}
