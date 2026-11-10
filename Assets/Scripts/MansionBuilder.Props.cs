using UnityEngine;

/// <summary>Objetos pequeños que los textos nombran y que deben verse en el mundo (ADR-0006).</summary>
public static partial class MansionBuilder
{
    /// <summary>Caja o cilindro girado con yaw alrededor del centro del diario y puesto como hijo del grupo (o de la raíz).</summary>
    static GameObject DiaryPart(GameObject grp, PrimitiveType type, Vector3 origin, Quaternion rot, Vector3 offset, Vector3 scale, Material m, Quaternion extra)
    {
        var g = GameObject.CreatePrimitive(type);
        Object.Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(grp != null ? grp.transform : root, false);
        g.transform.localPosition = origin + rot * offset;
        g.transform.localRotation = rot * extra;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g;
    }

    /// <summary>
    /// Libreta / diario abierto sobre una mesa: tapa de piel, dos bloques de hojas con renglones y letra a mano,
    /// listón rojo y una pluma. La hoja derecha trae el año 1973 encerrado en rojo (la clave de la caja fuerte).
    /// yaw 0: el texto se lee desde -z; -90: desde +x; 90: desde -x; 180: desde +z.
    /// </summary>
    static void OpenDiary(GameObject grp, float cx, float yTop, float cz, float yaw)
    {
        var rot = Quaternion.Euler(0f, yaw, 0f);
        Vector3 o = new Vector3(cx, yTop, cz);
        var leather = Mon.Lit(new Color(0.22f, 0.1f, 0.06f), 0.45f);
        var pages = Mon.Lit(new Color(0.9f, 0.85f, 0.7f), 0.1f);
        var dark = Mon.Lit(new Color(0.05f, 0.03f, 0.02f), 0.2f);
        var red = Mon.Lit(new Color(0.6f, 0.05f, 0.07f), 0.5f);
        var gold = MatOf(C(Gold, 0.85f));

        // tapa abierta (piel) con borde
        DiaryPart(grp, PrimitiveType.Cube, o, rot, new Vector3(0f, 0.006f, 0f), new Vector3(0.46f, 0.012f, 0.31f), leather, Quaternion.identity);
        // filete dorado en la tapa
        DiaryPart(grp, PrimitiveType.Cube, o, rot, new Vector3(0f, 0.0125f, 0.147f), new Vector3(0.43f, 0.001f, 0.004f), gold, Quaternion.identity);
        DiaryPart(grp, PrimitiveType.Cube, o, rot, new Vector3(0f, 0.0125f, -0.147f), new Vector3(0.43f, 0.001f, 0.004f), gold, Quaternion.identity);
        // bloques de hojas, un poco más gruesos del lado izquierdo (lo ya leído) y cubiertos de líneas en el canto
        DiaryPart(grp, PrimitiveType.Cube, o, rot, new Vector3(-0.108f, 0.022f, 0f), new Vector3(0.205f, 0.02f, 0.272f), pages, Quaternion.identity);
        DiaryPart(grp, PrimitiveType.Cube, o, rot, new Vector3(0.108f, 0.019f, 0f), new Vector3(0.205f, 0.014f, 0.272f), pages, Quaternion.identity);
        for (int i = 0; i < 5; i++)   // cantos de las hojas
        {
            DiaryPart(grp, PrimitiveType.Cube, o, rot, new Vector3(-0.2105f, 0.014f + i * 0.0035f, 0f), new Vector3(0.002f, 0.001f, 0.268f), dark, Quaternion.identity);
            DiaryPart(grp, PrimitiveType.Cube, o, rot, new Vector3(0.2105f, 0.014f + i * 0.0028f, 0f), new Vector3(0.002f, 0.001f, 0.268f), dark, Quaternion.identity);
        }
        // lomo
        DiaryPart(grp, PrimitiveType.Cube, o, rot, new Vector3(0f, 0.016f, 0f), new Vector3(0.012f, 0.026f, 0.275f), dark, Quaternion.identity);

        // hojas con letra (quads con textura): +Z es "arriba" del texto
        var qLeft = MatOf(S(TexGen.DiaryPage(true), 1f, 0.15f));
        var qRight = MatOf(S(TexGen.DiaryPage(false), 1f, 0.15f));
        var l = DiaryPart(grp, PrimitiveType.Quad, o, rot, new Vector3(-0.108f, 0.0328f, 0f), new Vector3(0.2f, 0.268f, 1f), qLeft, Quaternion.Euler(90f, 0f, 0f));
        var r = DiaryPart(grp, PrimitiveType.Quad, o, rot, new Vector3(0.108f, 0.0268f, 0f), new Vector3(0.2f, 0.268f, 1f), qRight, Quaternion.Euler(90f, 0f, 0f));
        l.name = "HojaIzquierda"; r.name = "HojaDerecha";

        // listón rojo: sale del lomo y cae por el borde delantero
        DiaryPart(grp, PrimitiveType.Cube, o, rot, new Vector3(0.012f, 0.0285f, -0.1f), new Vector3(0.012f, 0.002f, 0.2f), red, Quaternion.identity);
        DiaryPart(grp, PrimitiveType.Cube, o, rot, new Vector3(0.012f, 0.012f, -0.2f), new Vector3(0.012f, 0.002f, 0.07f), red, Quaternion.identity);

        // pluma estilográfica atravesada sobre la hoja derecha
        var penRot = Quaternion.Euler(0f, 24f, 90f);
        DiaryPart(grp, PrimitiveType.Cylinder, o, rot, new Vector3(0.13f, 0.0395f, -0.05f), new Vector3(0.012f, 0.07f, 0.012f), Mon.Lit(new Color(0.03f, 0.03f, 0.04f), 0.8f), penRot);
        DiaryPart(grp, PrimitiveType.Cylinder, o, rot, new Vector3(0.13f, 0.0395f, -0.05f) + Quaternion.Euler(0f, 24f, 0f) * new Vector3(0.082f, 0f, 0f), new Vector3(0.009f, 0.015f, 0.009f), gold, penRot);
        DiaryPart(grp, PrimitiveType.Cylinder, o, rot, new Vector3(0.13f, 0.0395f, -0.05f) + Quaternion.Euler(0f, 24f, 0f) * new Vector3(-0.045f, 0.0f, 0f), new Vector3(0.014f, 0.004f, 0.014f), gold, penRot);
    }
}

public static partial class MansionBuilder
{
    /// <summary>Marco local para diseñar un mueble una vez: x derecha, y arriba, z = hacia donde mira el frente.</summary>
    struct KFrame
    {
        public Vector3 o, right, fwd;
        public KFrame(float cx, float y0, float cz, Vector3 facing)
        {
            o = new Vector3(cx, y0, cz);
            fwd = facing.sqrMagnitude < 0.001f ? Vector3.forward : facing.normalized;
            right = Vector3.Cross(Vector3.up, fwd);
        }
        public Vector3 P(float x, float y, float z) { return o + right * x + Vector3.up * y + fwd * z; }
        public Quaternion Rot { get { return Quaternion.LookRotation(fwd, Vector3.up); } }
    }

    /// <summary>Caja en el marco local, con rotación opcional (grados, en ejes locales). El tamaño va en ejes locales.</summary>
    static GameObject KBox(GameObject grp, KFrame f, string name, float x, float y, float z, float sx, float sy, float sz, Surf s, bool collide = true, float rx = 0f, float ry = 0f, float rz = 0f)
    {
        var b = BoxIn(grp, name, f.P(x, y, z), new Vector3(sx, sy, sz), s, collide);
        b.transform.rotation = f.Rot * Quaternion.Euler(rx, ry, rz);
        return b;
    }

    static GameObject KBall(GameObject grp, KFrame f, string name, float x, float y, float z, float sx, float sy, float sz, Material m, float ry = 0f)
    {
        var g = BallIn(grp, name, f.P(x, y, z), 1f, m);
        g.transform.localScale = new Vector3(sx, sy, sz);
        g.transform.rotation = f.Rot * Quaternion.Euler(0f, ry, 0f);
        return g;
    }

    static Material webMat;
    /// <summary>Material con recorte (alfa) para telarañas: doble cara.</summary>
    static Material WebMat()
    {
        if (webMat != null) return webMat;
        var baseM = Resources.Load<Material>("BaseLitCutout");
        webMat = baseM != null ? new Material(baseM) : new Material(BaseLit());
        webMat.SetTexture("_BaseMap", TexGen.Cobweb());
        webMat.SetColor("_BaseColor", new Color(0.95f, 0.95f, 0.92f, 1f));
        webMat.SetFloat("_Smoothness", 0f);
        return webMat;
    }

    /// <summary>Telaraña en una esquina: un quad que mira hacia 'normal'; flipX/flipY elige qué esquina del cuadro es el origen del hilo.</summary>
    static void Cobweb(GameObject grp, Vector3 pos, Vector3 normal, float size, bool flipX, bool flipY, float roll = 0f)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.Destroy(q.GetComponent<Collider>());
        q.name = "Telarana";
        q.transform.SetParent(grp != null ? grp.transform : root, false);
        q.transform.localPosition = pos;
        q.transform.localRotation = Quaternion.LookRotation(-normal) * Quaternion.Euler(0f, 0f, roll);
        q.transform.localScale = new Vector3(flipX ? -size : size, flipY ? -size : size, 1f);
        var r = q.GetComponent<Renderer>();
        r.sharedMaterial = WebMat();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    /// <summary>
    /// Cama deshecha, como la deja alguien que se acaba de levantar: cobijas revueltas y echadas hacia atrás, sábana a la vista,
    /// almohadas chuecas y hundidas, una orilla colgando. variant 0 = tibia (recién usada, hueco de cuerpo cálido);
    /// 1 = sin hacer y fría (cobija arrastrada al piso, sábanas azul gris); 2 = vieja y polvosa, con el hueco de un cuerpo y telarañas.
    /// facing = hacia dónde está el pie de la cama (la cabecera queda del lado contrario, contra la pared).
    /// </summary>
    static GameObject UnmadeBed(string name, float cx, float y0, float cz, Vector3 facing, float w, float d, int variant)
    {
        var g = Group(name);
        var f = new KFrame(cx, y0, cz, facing);
        var rng = new System.Random(1000 + variant * 37);
        System.Func<float, float, float> R = (a, b) => a + (float)rng.NextDouble() * (b - a);

        Color sheet = variant == 1 ? new Color(0.6f, 0.68f, 0.78f) : variant == 2 ? new Color(0.55f, 0.53f, 0.48f) : new Color(0.93f, 0.9f, 0.85f);
        Color duvet = variant == 1 ? new Color(0.28f, 0.34f, 0.5f) : variant == 2 ? new Color(0.4f, 0.38f, 0.34f) : new Color(0.55f, 0.12f, 0.16f);
        Color duvetIn = variant == 2 ? new Color(0.45f, 0.43f, 0.4f) : new Color(0.85f, 0.82f, 0.75f);   // revés de la cobija
        Color pillow = variant == 2 ? new Color(0.62f, 0.6f, 0.55f) : new Color(0.95f, 0.93f, 0.88f);
        var wood = sDark;

        // estructura: patas, largueros, cabecera alta con postes y pie bajo
        float hw = w / 2f, hd = d / 2f;
        foreach (int sx in new[] { -1, 1 })
            foreach (int sz in new[] { -1, 1 })
                KBox(g, f, "Pata", sx * (hw - 0.04f), 0.1f, sz * (hd - 0.04f), 0.07f, 0.2f, 0.07f, wood);
        KBox(g, f, "Larguero", -hw + 0.03f, 0.3f, 0f, 0.05f, 0.18f, d - 0.06f, wood);
        KBox(g, f, "Larguero", hw - 0.03f, 0.3f, 0f, 0.05f, 0.18f, d - 0.06f, wood);
        KBox(g, f, "Cabecera", 0f, 0.62f, -hd + 0.03f, w, 0.95f, 0.06f, wood);
        KBox(g, f, "CabeceraMolduraAlta", 0f, 1.12f, -hd + 0.03f, w + 0.06f, 0.05f, 0.08f, wood, false);
        KBox(g, f, "PosteCabecera", -hw + 0.04f, 0.65f, -hd + 0.04f, 0.08f, 1.15f, 0.08f, wood);
        KBox(g, f, "PosteCabecera", hw - 0.04f, 0.65f, -hd + 0.04f, 0.08f, 1.15f, 0.08f, wood);
        KBox(g, f, "Pie", 0f, 0.46f, hd - 0.025f, w, 0.4f, 0.05f, wood);
        // colchón y sábana ajustable
        KBox(g, f, "Colchon", 0f, 0.5f, 0f, w - 0.1f, 0.22f, d - 0.12f, C(new Color(0.9f, 0.88f, 0.8f), 0.05f));
        KBox(g, f, "Sabana", 0f, 0.613f, 0f, w - 0.1f, 0.012f, d - 0.12f, C(sheet, 0.1f), false);

        // hueco del cuerpo en el colchón: óvalos más oscuros y hundidos
        float hollowX = variant == 0 ? -0.22f : (variant == 2 ? 0f : 0.1f);
        Color hollow = Color.Lerp(sheet, Color.black, variant == 2 ? 0.42f : 0.22f);
        KBall(g, f, "HuecoTorso", hollowX, 0.616f, -0.2f, 0.62f, 0.022f, 0.75f, MatOf(C(hollow, 0.05f)));
        KBall(g, f, "HuecoPiernas", hollowX, 0.616f, 0.55f, 0.4f, 0.022f, 0.9f, MatOf(C(hollow, 0.05f)));
        if (variant == 0) KBall(g, f, "CalorTibio", hollowX, 0.62f, -0.1f, 0.5f, 0.012f, 0.6f, Mon.Glow(new Color(1f, 0.55f, 0.3f), 0.18f));

        // cobijas revueltas: un bulto de fondo y muchos pliegues girados
        float baseZ = variant == 2 ? 0.1f : 0.4f;
        KBox(g, f, "CobijaBase", variant == 2 ? 0f : 0.25f, 0.65f, baseZ, w - 0.25f, 0.06f, d * (variant == 2 ? 0.8f : 0.55f), C(duvet, 0.05f), false, R(-3f, 3f), R(-9f, 9f), R(-4f, 4f));
        int lumps = variant == 2 ? 3 : 10;
        for (int i = 0; i < lumps; i++)
            KBox(g, f, "Pliegue", R(-hw + 0.3f, hw - 0.3f), R(0.68f, 0.76f), R(-0.1f, hd - 0.25f), R(0.35f, 0.8f), R(0.07f, 0.17f), R(0.3f, 0.6f),
                 C(Color.Lerp(duvet, duvetIn, R(0f, 0.18f)), 0.05f), false, R(-14f, 14f), R(-40f, 40f), R(-12f, 12f));
        // esquina echada hacia atrás mostrando el revés claro de la cobija
        KBox(g, f, "EsquinaVolteada", variant == 0 ? 0.5f : -0.5f, 0.72f, -0.55f, 0.95f, 0.05f, 0.8f, C(duvetIn, 0.05f), false, 16f, R(10f, 40f), variant == 0 ? 20f : -20f);
        // orilla colgando por un lado
        float side = variant == 1 ? -1f : 1f;
        KBox(g, f, "OrillaColgando", side * (hw + 0.02f), 0.42f, 0.35f, 0.05f, 0.42f, 1.1f, C(duvet, 0.05f), false, 0f, 0f, side * -8f);
        if (variant == 1)
        {
            // la cobija arrastrada hacia el pie y amontonada en el piso
            KBox(g, f, "CobijaCayendo", 0.3f, 0.45f, hd + 0.1f, w * 0.6f, 0.05f, 0.7f, C(duvet, 0.05f), false, 62f, 4f, 0f);
            foreach (var p in new[] { new Vector3(0.15f, 0.07f, hd + 0.5f), new Vector3(0.55f, 0.06f, hd + 0.4f), new Vector3(0.3f, 0.1f, hd + 0.7f) })
                KBox(g, f, "CobijaEnPiso", p.x, p.y, p.z, 0.55f, 0.1f, 0.45f, C(Color.Lerp(duvet, duvetIn, 0.1f), 0.05f), false, R(-8f, 8f), R(-50f, 50f), R(-8f, 8f));
        }
        // almohadas chuecas y hundidas
        var pillowS = C(pillow, 0.1f);
        float pz = -hd + 0.34f;
        var p1 = KBox(g, f, "Almohada", -0.45f, 0.72f, pz + 0.05f, 0.64f, 0.16f, 0.4f, pillowS, false, -6f, 15f, 4f);
        var p2 = KBox(g, f, "Almohada", 0.5f, 0.71f, pz - 0.03f, 0.62f, 0.15f, 0.4f, pillowS, false, 8f, -22f, -5f);
        KBall(g, f, "HuecoAlmohada", hollowX, 0.805f, pz + 0.04f, 0.3f, 0.045f, 0.24f, MatOf(C(Color.Lerp(pillow, Color.black, 0.25f), 0.05f)), 12f);
        if (variant == 2)
        {
            // polvo gris sobre todo y telarañas en el rincón de la cabecera
            KBox(g, f, "Polvo", 0f, 0.77f, 0f, w - 0.2f, 0.004f, d - 0.3f, C(new Color(0.6f, 0.6f, 0.58f), 0f), false);
            Cobweb(g, f.P(-hw + 0.1f, 1.08f, -hd + 0.08f), f.fwd, 0.55f, false, true);
            Cobweb(g, f.P(hw - 0.1f, 1.08f, -hd + 0.08f), f.fwd, 0.55f, true, true);
            Cobweb(g, f.P(0f, 0.7f, hd - 0.05f), f.right * -1f, 0.35f, false, false, 20f);
        }
        return g;
    }
}

public static partial class MansionBuilder
{
    /// <summary>
    /// Espejo de pared con marco dorado tallado y un cristal que es un espejo real (MirrorSurface): se ve el cuarto y al personaje.
    /// wallX/wallZ: punto de la pared donde está el centro; normal: hacia el cuarto (eje); haunted = el reflejo llega un segundo tarde.
    /// </summary>
    static GameObject OrnateMirror(string name, float wallCoord, float cy, float along, Vector3 normal, float w, float h, bool haunted)
    {
        var g = Group(name);
        // centro del cristal: pegado a la pared (coordenada fija en el eje de la normal), 'along' sobre el otro eje horizontal
        bool nx = Mathf.Abs(normal.x) > 0.5f;
        float inner = wallCoord + 0.05f;                                         // un poco despegado de la pared
        Vector3 centre = nx ? new Vector3(inner * 1f + (normal.x > 0 ? 0.0f : 0f), cy, along) : new Vector3(along, cy, inner);
        if (nx) centre.x = wallCoord + normal.x * 0.05f; else centre.z = wallCoord + normal.z * 0.05f;
        var f = new KFrame(centre.x, 0f, centre.z, normal);
        var gold = C(Gold, 0.75f);
        var goldDark = C(new Color(0.45f, 0.33f, 0.1f), 0.6f);
        // marco: tres capas de molduras alrededor del cristal, rosetas en las esquinas y remate con corona
        float bw = 0.1f;
        KBox(g, f, "MarcoIzq", -(w / 2f + bw / 2f), cy, 0f, bw, h + 2f * bw, 0.07f, gold, false);
        KBox(g, f, "MarcoDer", (w / 2f + bw / 2f), cy, 0f, bw, h + 2f * bw, 0.07f, gold, false);
        KBox(g, f, "MarcoSup", 0f, cy + h / 2f + bw / 2f, 0f, w + 2f * bw, bw, 0.07f, gold, false);
        KBox(g, f, "MarcoInf", 0f, cy - h / 2f - bw / 2f, 0f, w + 2f * bw, bw, 0.07f, gold, false);
        // moldura interior: cuatro filetes alrededor del cristal (no debe tapar el cristal ni tocar su plano)
        KBox(g, f, "FileteIzq", -(w / 2f + 0.01f), cy, 0.02f, 0.02f, h + 0.04f, 0.04f, goldDark, false);
        KBox(g, f, "FileteDer", (w / 2f + 0.01f), cy, 0.02f, 0.02f, h + 0.04f, 0.04f, goldDark, false);
        KBox(g, f, "FileteSup", 0f, cy + h / 2f + 0.01f, 0.02f, w + 0.04f, 0.02f, 0.04f, goldDark, false);
        KBox(g, f, "FileteInf", 0f, cy - h / 2f - 0.01f, 0.02f, w + 0.04f, 0.02f, 0.04f, goldDark, false);
        foreach (int sx in new[] { -1, 1 })
            foreach (int sy in new[] { -1, 1 })
                KBall(g, f, "Roseta", sx * (w / 2f + bw / 2f), cy + sy * (h / 2f + bw / 2f), 0.045f, 0.13f, 0.13f, 0.05f, MatOf(gold));
        KBox(g, f, "CoronaBase", 0f, cy + h / 2f + bw + 0.04f, 0f, 0.5f, 0.08f, 0.07f, gold, false);
        KBall(g, f, "CoronaCentro", 0f, cy + h / 2f + bw + 0.14f, 0.02f, 0.18f, 0.18f, 0.06f, MatOf(gold));
        KBox(g, f, "CoronaIzq", -0.2f, cy + h / 2f + bw + 0.1f, 0f, 0.14f, 0.14f, 0.05f, gold, false, 0f, 0f, 30f);
        KBox(g, f, "CoronaDer", 0.2f, cy + h / 2f + bw + 0.1f, 0f, 0.14f, 0.14f, 0.05f, gold, false, 0f, 0f, -30f);
        // cristal: quad de espejo real; se orienta hacia el cuarto
        var glass = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.Destroy(glass.GetComponent<Collider>());
        glass.name = "CristalEspejo";
        glass.transform.SetParent(g.transform, false);
        glass.transform.position = f.P(0f, cy, 0.05f);
        glass.transform.rotation = Quaternion.LookRotation(-normal);
        glass.transform.localScale = new Vector3(w, h, 1f);
        var ms = glass.AddComponent<MirrorSurface>();
        var mm = Resources.Load<Material>("MirrorMat");
        if (mm == null) mm = new Material(Shader.Find("Mansion/MirrorScreen") ?? Shader.Find("Universal Render Pipeline/Lit"));
        ms.Setup(mm, gm.cam, normal, haunted);
        // collider invisible para apuntar al espejo
        var hit = new GameObject("Hitbox");
        hit.transform.SetParent(g.transform, false);
        hit.transform.position = f.P(0f, cy, 0.03f);
        hit.transform.rotation = f.Rot;
        hit.AddComponent<BoxCollider>().size = new Vector3(w + 0.3f, h + 0.3f, 0.12f);
        return g;
    }
}
