using UnityEngine;

/// <summary>Criatura armada con primitivas, usada en las escenas de muerte.</summary>
public class Creature : MonoBehaviour
{
    public Transform armL, armR, handL, handR, head;
    public Renderer[] eyes = new Renderer[0];
    public Transform[] legs = new Transform[0];
    public Material eyeMat;
    float eyeBase = 7f;
    public Transform jaw;

    /// <summary>Extiende los brazos (0 = relajados hacia abajo, 1 = estirados hacia adelante).</summary>
    public void Reach(float u, float maxLen = 2.4f)
    {
        float len = Mathf.Lerp(0.9f, maxLen, u);
        SetArm(armL, handL, len, Mathf.Lerp(75f, 0f, u));
        SetArm(armR, handR, len, Mathf.Lerp(75f, 0f, u));
    }

    void SetArm(Transform arm, Transform hand, float len, float pitch)
    {
        if (arm == null) return;
        var rot = Quaternion.Euler(pitch, arm.localEulerAngles.y > 180f ? arm.localEulerAngles.y - 360f : arm.localEulerAngles.y, 0f);
        arm.localRotation = rot;
        arm.localScale = new Vector3(1f, 1f, Mathf.Max(0.01f, len));
        if (hand != null) hand.localPosition = arm.localPosition + rot * (Vector3.forward * len);
    }

    /// <summary>Estira un brazo recto hacia adelante a la longitud dada (brazo pálido del espejo).</summary>
    public void Extend(float len) { SetArm(armL, handL, len, 0f); }

    /// <summary>Abre la boca (0 = cerrada, 1 = muy abierta).</summary>
    public void Mouth(float u)
    {
        if (jaw == null) return;
        jaw.localScale = new Vector3(0.2f, Mathf.Lerp(0.05f, 0.3f, u), 0.05f);
        jaw.localPosition = new Vector3(0f, 1.78f - 0.1f * u, 0.17f);
    }

    public void SetEyes(float k)
    {
        if (eyeMat == null) return;
        eyeMat.SetColor("_EmissionColor", eyeColor * eyeBase * k);
        foreach (var e in eyes) if (e != null) e.enabled = k > 0.01f;
    }
    public Color eyeColor = Color.white;

    /// <summary>Anima las patas de la araña.</summary>
    public void Wiggle(float t, float speed = 18f)
    {
        for (int i = 0; i < legs.Length; i++)
        {
            if (legs[i] == null) continue;
            var e = legs[i].localEulerAngles;
            float yaw = legs[i].localEulerAngles.y;
            legs[i].localRotation = Quaternion.Euler(-40f + Mathf.Sin(t * speed + i * 1.7f) * 14f, yaw, 0f);
        }
    }
}

public static class Mon
{
    static Material baseLit, baseEmit;

    static Material BaseLit()
    {
        if (baseLit == null) { baseLit = Resources.Load<Material>("BaseLit"); if (baseLit == null) baseLit = new Material(Shader.Find("Universal Render Pipeline/Lit")); }
        return baseLit;
    }

    public static Material Lit(Color c, float smooth = 0.2f)
    {
        var m = new Material(BaseLit());
        m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth);
        return m;
    }

    public static Material Glow(Color c, float k)
    {
        if (baseEmit == null) { baseEmit = Resources.Load<Material>("BaseLitEmit"); if (baseEmit == null) baseEmit = BaseLit(); }
        var m = new Material(baseEmit);
        m.SetColor("_BaseColor", c * 0.3f);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", c * k);
        return m;
    }

    public static GameObject Part(Transform parent, PrimitiveType t, Vector3 lp, Vector3 ls, Material m)
    {
        var g = GameObject.CreatePrimitive(t);
        Object.Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(parent, false);
        g.transform.localPosition = lp; g.transform.localScale = ls;
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g;
    }

    static Transform Pivot(Transform parent, Vector3 lp, Vector3 euler)
    {
        var g = new GameObject("Pivote");
        g.transform.SetParent(parent, false);
        g.transform.localPosition = lp; g.transform.localRotation = Quaternion.Euler(euler);
        return g.transform;
    }

    /// <summary>Figura humanoide alta, de brazos largos. Mira hacia 'facing'.</summary>
    public static Creature Figure(Vector3 feet, Vector3 facing, Color skin, Color eye, float scale)
    {
        var root = new GameObject("Criatura");
        root.transform.position = feet;
        root.transform.rotation = Quaternion.LookRotation(facing.sqrMagnitude > 0.001f ? facing : Vector3.forward);
        root.transform.localScale = Vector3.one * scale;
        var c = root.AddComponent<Creature>();
        var sk = Lit(skin, 0.5f);
        Part(root.transform, PrimitiveType.Cube, new Vector3(0, 0.4f, 0), new Vector3(0.34f, 0.8f, 0.22f), sk);
        Part(root.transform, PrimitiveType.Cube, new Vector3(0, 1.2f, 0), new Vector3(0.5f, 0.85f, 0.28f), sk);
        var h = Part(root.transform, PrimitiveType.Sphere, new Vector3(0, 1.85f, 0.02f), new Vector3(0.34f, 0.44f, 0.34f), sk);
        c.head = h.transform;
        var jawObj = Part(root.transform, PrimitiveType.Cube, new Vector3(0, 1.73f, 0.17f), new Vector3(0.2f, 0.14f, 0.05f), Lit(new Color(0.25f, 0f, 0f), 0f));
        c.jaw = jawObj.transform;
        var teeth = Lit(new Color(0.9f, 0.88f, 0.8f), 0.5f);
        for (int t = 0; t < 5; t++)
        {
            Part(root.transform, PrimitiveType.Cube, new Vector3(-0.08f + t * 0.04f, 1.79f, 0.2f), new Vector3(0.02f, 0.05f, 0.015f), teeth);
            Part(root.transform, PrimitiveType.Cube, new Vector3(-0.08f + t * 0.04f, 1.67f, 0.2f), new Vector3(0.02f, 0.04f, 0.015f), teeth);
        }
        c.eyeMat = Glow(eye, 1f); c.eyeColor = eye;
        var e1 = Part(root.transform, PrimitiveType.Sphere, new Vector3(-0.095f, 1.9f, 0.16f), new Vector3(0.13f, 0.09f, 0.08f), c.eyeMat);
        var e2 = Part(root.transform, PrimitiveType.Sphere, new Vector3(0.095f, 1.9f, 0.16f), new Vector3(0.13f, 0.09f, 0.08f), c.eyeMat);
        c.eyes = new[] { e1.GetComponent<Renderer>(), e2.GetComponent<Renderer>() };
        // brazos: pivote en el hombro, la malla crece en +z
        for (int s = -1; s <= 1; s += 2)
        {
            var piv = Pivot(root.transform, new Vector3(0.32f * s, 1.5f, 0.05f), new Vector3(75f, 8f * s, 0f));
            Part(piv, PrimitiveType.Cube, new Vector3(0, 0, 0.5f), new Vector3(0.1f, 0.1f, 1f), sk);
            var hand = Part(root.transform, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.2f, 0.12f, 0.3f), sk);
            for (int f = -1; f <= 1; f++) Part(hand.transform, PrimitiveType.Cube, new Vector3(f * 0.3f, 0, 0.7f), new Vector3(0.12f, 0.3f, 1.4f), sk);
            if (s < 0) { c.armL = piv; c.handL = hand.transform; } else { c.armR = piv; c.handR = hand.transform; }
        }
        c.Reach(0f);
        c.SetEyes(0f);
        return c;
    }

    /// <summary>Araña gigante: cuerpo, cabeza con ojos rojos, colmillos y 8 patas.</summary>
    public static Creature Spider(Vector3 pos, Vector3 facing, float scale)
    {
        var root = new GameObject("Arana");
        root.transform.position = pos;
        root.transform.rotation = Quaternion.LookRotation(facing.sqrMagnitude > 0.001f ? facing : Vector3.forward);
        root.transform.localScale = Vector3.one * scale;
        var c = root.AddComponent<Creature>();
        var sk = Lit(new Color(0.07f, 0.05f, 0.04f), 0.4f);
        Part(root.transform, PrimitiveType.Sphere, new Vector3(0, 0.45f, -0.25f), new Vector3(0.6f, 0.5f, 0.8f), sk);
        Part(root.transform, PrimitiveType.Sphere, new Vector3(0, 0.45f, 0.3f), new Vector3(0.35f, 0.3f, 0.35f), sk);
        c.eyeMat = Glow(new Color(1f, 0.05f, 0.05f), 1f); c.eyeColor = new Color(1f, 0.05f, 0.05f);
        var list = new System.Collections.Generic.List<Renderer>();
        for (int i = 0; i < 4; i++)
        {
            var e = Part(root.transform, PrimitiveType.Sphere, new Vector3(-0.1f + (i % 2) * 0.2f, 0.52f + (i / 2) * 0.07f, 0.46f), Vector3.one * 0.07f, c.eyeMat);
            list.Add(e.GetComponent<Renderer>());
        }
        c.eyes = list.ToArray();
        var white = Lit(new Color(0.9f, 0.9f, 0.85f), 0.5f);
        Part(root.transform, PrimitiveType.Cube, new Vector3(-0.07f, 0.34f, 0.5f), new Vector3(0.03f, 0.18f, 0.03f), white);
        Part(root.transform, PrimitiveType.Cube, new Vector3(0.07f, 0.34f, 0.5f), new Vector3(0.03f, 0.18f, 0.03f), white);
        var legs = new System.Collections.Generic.List<Transform>();
        for (int s = -1; s <= 1; s += 2)
            for (int i = 0; i < 4; i++)
            {
                float yaw = s * (90f - (1.5f - i) * 28f);
                var up = Pivot(root.transform, new Vector3(s * 0.2f, 0.5f, 0.15f - i * 0.17f), new Vector3(-40f, yaw, 0f));
                Part(up, PrimitiveType.Cube, new Vector3(0, 0, 0.45f), new Vector3(0.06f, 0.06f, 0.9f), sk);
                var low = Pivot(up, new Vector3(0, 0, 0.9f), new Vector3(105f, 0f, 0f));
                Part(low, PrimitiveType.Cube, new Vector3(0, 0, 0.5f), new Vector3(0.05f, 0.05f, 1.0f), sk);
                legs.Add(up);
            }
        c.legs = legs.ToArray();
        return c;
    }

    /// <summary>Muñeca / cara pálida con ojos negros y sonrisa roja.</summary>
    public static Creature Doll(Vector3 pos, Vector3 facing, float scale)
    {
        var root = new GameObject("Muneca");
        root.transform.position = pos;
        root.transform.rotation = Quaternion.LookRotation(facing.sqrMagnitude > 0.001f ? facing : Vector3.forward);
        root.transform.localScale = Vector3.one * scale;
        var c = root.AddComponent<Creature>();
        var skin = Lit(new Color(0.88f, 0.8f, 0.72f), 0.5f);
        Part(root.transform, PrimitiveType.Cube, new Vector3(0, 0.2f, 0), new Vector3(0.22f, 0.32f, 0.14f), Lit(new Color(0.5f, 0.1f, 0.15f), 0.1f));
        var headRoot = new GameObject("Cabeza").transform;
        headRoot.SetParent(root.transform, false); headRoot.localPosition = new Vector3(0, 0.55f, 0);
        c.head = headRoot;
        Part(headRoot, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.3f, 0.32f, 0.3f), skin);
        Part(headRoot, PrimitiveType.Sphere, new Vector3(-0.07f, 0.03f, 0.13f), new Vector3(0.07f, 0.08f, 0.04f), Lit(Color.black, 0.9f));
        Part(headRoot, PrimitiveType.Sphere, new Vector3(0.07f, 0.03f, 0.13f), new Vector3(0.07f, 0.08f, 0.04f), Lit(Color.black, 0.9f));
        c.eyeMat = Glow(new Color(1f, 0.1f, 0.05f), 1f); c.eyeColor = new Color(1f, 0.1f, 0.05f);
        var p1 = Part(headRoot, PrimitiveType.Sphere, new Vector3(-0.07f, 0.03f, 0.15f), Vector3.one * 0.025f, c.eyeMat);
        var p2 = Part(headRoot, PrimitiveType.Sphere, new Vector3(0.07f, 0.03f, 0.15f), Vector3.one * 0.025f, c.eyeMat);
        c.eyes = new[] { p1.GetComponent<Renderer>(), p2.GetComponent<Renderer>() };
        Part(headRoot, PrimitiveType.Cube, new Vector3(0, -0.07f, 0.14f), new Vector3(0.14f, 0.03f, 0.02f), Lit(new Color(0.6f, 0.02f, 0.05f), 0.3f));
        c.SetEyes(0f);
        return c;
    }

    /// <summary>Brazo pálido que sale de una superficie (espejo).</summary>
    public static Creature PaleArm(Vector3 pos, Vector3 facing)
    {
        var root = new GameObject("BrazoPalido");
        root.transform.position = pos;
        root.transform.rotation = Quaternion.LookRotation(facing.sqrMagnitude > 0.001f ? facing : Vector3.forward);
        var c = root.AddComponent<Creature>();
        var skin = Lit(new Color(0.75f, 0.78f, 0.75f), 0.4f);
        var piv = Pivot(root.transform, Vector3.zero, Vector3.zero);
        Part(piv, PrimitiveType.Cube, new Vector3(0, 0, 0.5f), new Vector3(0.12f, 0.12f, 1f), skin);
        var hand = Part(root.transform, PrimitiveType.Cube, Vector3.zero, new Vector3(0.18f, 0.05f, 0.2f), skin);
        for (int f = 0; f < 4; f++)
            Part(hand.transform, PrimitiveType.Cube, new Vector3(-0.36f + f * 0.24f, 0, 0.9f), new Vector3(0.14f, 0.5f, 1.2f), skin);
        c.armL = piv; c.handL = hand.transform;
        return c;
    }
}
