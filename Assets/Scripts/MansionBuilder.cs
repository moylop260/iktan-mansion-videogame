using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Construye la mansión (2 pisos) con estilo "casa embrujada clásica": papel tapiz, madera, mármol,
/// molduras, marcos de puerta, ventanas con luz de luna, candelabro, cuadros y chimenea.
/// También define los objetos interactivos, los finales inesperados (game over), las llaves y los checkpoints.
///
/// Planta (x hacia la derecha, z hacia adelante), 24 x 16 m, igual en los dos pisos:
///   Piso 1:  Sala | Vestíbulo (escalera) | Cocina        Cuarto 1 | Baño | Biblioteca
///   Piso 2:  Cuarto 3 | Pasillo | Cuarto 4               Piano (oscuro) | Oficina
/// </summary>
public static partial class MansionBuilder
{
    const float T = 0.3f;     // grosor de pared
    const float H = 3.1f;     // alto de pared
    const float F2 = 3.3f;    // altura del piso 2
    const float DH = 2.3f;    // alto de puerta

    struct Surf { public Texture2D tex; public Color tint; public float tile; public float smooth; }

    static Transform root;
    static GameManager gm;
    static Material baseLit, baseEmit;
    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();

    // superficies
    static Surf sWood, sDark, sMarble, sPlaster, sStone, sCarpet, sBooks;
    static Surf wpSala, wpHall, wpHall2, wpCocina, wpC1, wpBano, wpBib, wpPiano, wpOfi, wpC3, wpC4;
    static Surf floorBlue, floorRed, floorTerracotta, floorPink, floorMarbleTeal;
    static readonly Color Warm = new Color(1f, 0.8f, 0.5f);
    static readonly Color Gold = new Color(0.75f, 0.58f, 0.2f);
    static readonly Color White = new Color(0.85f, 0.85f, 0.85f);

    static Surf S(Texture2D t, float tile, float smooth = 0.1f) { return new Surf { tex = t, tint = Color.white, tile = tile, smooth = smooth }; }
    static Surf ST(Texture2D t, float tile, Color tint, float smooth = 0.1f) { return new Surf { tex = t, tint = tint, tile = tile, smooth = smooth }; }
    static Surf C(Color c, float smooth = 0.1f) { return new Surf { tex = null, tint = c, tile = 1f, smooth = smooth }; }

    static void InitSurfaces()
    {
        sWood = S(TexGen.Wood(), 2.0f, 0.3f);
        sDark = S(TexGen.DarkWood(), 1.5f, 0.3f);
        sMarble = S(TexGen.Marble(), 2.0f, 0.6f);
        sPlaster = S(TexGen.Plaster(), 2.0f, 0.05f);
        sStone = S(TexGen.Stone(), 3.0f, 0.05f);
        sCarpet = S(TexGen.Carpet(), 2.5f, 0.05f);
        sBooks = S(TexGen.Books(), 2.0f, 0.05f);
        // Cada cuarto tiene su propio color para que nunca se confunda uno con otro:
        // vestíbulo vino, sala verde, cocina amarilla, cuarto 1 azul, baño aqua, biblioteca nogal con alfombra roja,
        // pasillo violeta, cuarto 3 rosa, piano negro y carmesí, cuarto 4 terracota, oficina verde pizarra.
        wpSala = S(TexGen.Wallpaper("sala", new Color(0.08f, 0.42f, 0.2f), new Color(0.05f, 0.3f, 0.14f)), 2.0f);
        wpHall = S(TexGen.Wallpaper("hall", new Color(0.46f, 0.07f, 0.1f), new Color(0.32f, 0.05f, 0.08f)), 2.0f);
        wpHall2 = S(TexGen.Wallpaper("hall2", new Color(0.42f, 0.2f, 0.58f), new Color(0.3f, 0.14f, 0.45f)), 2.0f);
        wpCocina = S(TexGen.Tile("cocina", new Color(0.97f, 0.84f, 0.3f), new Color(0.88f, 0.7f, 0.18f)), 2.0f, 0.4f);
        wpC1 = S(TexGen.Wallpaper("c1", new Color(0.13f, 0.28f, 0.7f), new Color(0.08f, 0.19f, 0.52f)), 2.0f);
        wpBano = S(TexGen.Tile("bano", new Color(0.3f, 0.78f, 0.82f), new Color(0.2f, 0.62f, 0.7f)), 2.0f, 0.5f);
        wpBib = S(TexGen.Panel(), 2.0f);
        wpPiano = S(TexGen.Wallpaper("piano", new Color(0.42f, 0.03f, 0.07f), new Color(0.1f, 0.015f, 0.03f)), 2.0f);
        wpOfi = S(TexGen.Wallpaper("ofi", new Color(0.2f, 0.34f, 0.3f), new Color(0.14f, 0.25f, 0.22f)), 2.0f);
        wpC3 = S(TexGen.Wallpaper("c3", new Color(0.88f, 0.52f, 0.62f), new Color(0.78f, 0.42f, 0.54f)), 2.0f);
        wpC4 = S(TexGen.Wallpaper("c4", new Color(0.82f, 0.43f, 0.18f), new Color(0.68f, 0.33f, 0.12f)), 2.0f);
        floorBlue = ST(TexGen.Carpet(), 2.5f, new Color(0.45f, 0.6f, 1.3f), 0.05f);
        floorRed = S(TexGen.Carpet(), 2.5f, 0.05f);
        floorPink = ST(TexGen.Carpet(), 2.5f, new Color(1.3f, 0.75f, 0.9f), 0.05f);
        floorTerracotta = S(TexGen.Tile("c4piso", new Color(0.72f, 0.36f, 0.2f), new Color(0.6f, 0.28f, 0.14f)), 2.0f, 0.3f);
    }

    // ------------------------------------------------------------------ materiales y mallas
    static Material BaseLit()
    {
        if (baseLit == null)
        {
            baseLit = Resources.Load<Material>("BaseLit");
            if (baseLit == null) baseLit = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        }
        return baseLit;
    }

    static Material BaseEmit()
    {
        if (baseEmit == null)
        {
            baseEmit = Resources.Load<Material>("BaseLitEmit");
            if (baseEmit == null) baseEmit = BaseLit();
        }
        return baseEmit;
    }

    static Material MatOf(Surf s)
    {
        string key = (s.tex != null ? System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(s.tex).ToString() : "-") + "_" + s.tint + "_" + s.smooth;
        Material m;
        if (mats.TryGetValue(key, out m) && m != null) return m;
        m = new Material(BaseLit());
        m.SetColor("_BaseColor", s.tint);
        if (s.tex != null) m.SetTexture("_BaseMap", s.tex);
        m.SetFloat("_Smoothness", s.smooth);
        m.SetFloat("_Metallic", 0f);
        mats[key] = m;
        return m;
    }

    static Material EmitMat(Color c, float k)
    {
        var m = new Material(BaseEmit());
        m.SetColor("_BaseColor", c * 0.4f);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", c * k);
        return m;
    }

    /// <summary>Caja con UV en metros (la textura no se estira).</summary>
    static Mesh BoxMesh(Vector3 size, float tile)
    {
        string key = size.x.ToString("F3") + "_" + size.y.ToString("F3") + "_" + size.z.ToString("F3") + "_" + tile.ToString("F2");
        Mesh m;
        if (meshes.TryGetValue(key, out m) && m != null) return m;
        m = new Mesh { name = "Caja" };
        var verts = new List<Vector3>(); var norms = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
        Vector3 h = size / 2f;
        Vector3[] ns = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        Vector3[] ts = { new Vector3(0, 0, -1), new Vector3(0, 0, 1), new Vector3(1, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0), new Vector3(-1, 0, 0) };
        Vector3[] bs = { new Vector3(0, 1, 0), new Vector3(0, 1, 0), new Vector3(0, 0, -1), new Vector3(0, 0, 1), new Vector3(0, 1, 0), new Vector3(0, 1, 0) };
        for (int f = 0; f < 6; f++)
        {
            Vector3 n = ns[f], t = ts[f], b = bs[f];
            float ht = Mathf.Abs(Vector3.Dot(t, h)), hb = Mathf.Abs(Vector3.Dot(b, h)), hn = Mathf.Abs(Vector3.Dot(n, h));
            int i0 = verts.Count;
            Vector2[] c = { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1) };
            for (int k = 0; k < 4; k++)
            {
                Vector3 p = n * hn + t * (c[k].x * ht) + b * (c[k].y * hb);
                verts.Add(p); norms.Add(n);
                uvs.Add(new Vector2(Vector3.Dot(p, t) / tile, Vector3.Dot(p, b) / tile));
            }
            tris.AddRange(new[] { i0, i0 + 1, i0 + 2, i0, i0 + 2, i0 + 3 });
        }
        m.SetVertices(verts); m.SetNormals(norms); m.SetUVs(0, uvs); m.SetTriangles(tris, 0);
        m.RecalculateBounds();
        meshes[key] = m;
        return m;
    }

    static GameObject Box(string name, Vector3 center, Vector3 size, Surf s, bool collide = true)
    {
        var g = new GameObject(name);
        g.transform.SetParent(root, false);
        g.transform.localPosition = center;
        g.AddComponent<MeshFilter>().sharedMesh = BoxMesh(size, s.tile);
        var r = g.AddComponent<MeshRenderer>();
        r.sharedMaterial = MatOf(s);
        if (collide) g.AddComponent<BoxCollider>().size = size;
        return g;
    }

    static GameObject Box(string name, Vector3 center, Vector3 size, Color c, bool collide = true)
    {
        return Box(name, center, size, C(c), collide);
    }

    /// <summary>Mueble: y0 = base, (cx,cz) = centro en planta.</summary>
    static GameObject P(string name, float cx, float y0, float cz, float sx, float sh, float sz, Color c)
    {
        return Box(name, new Vector3(cx, y0 + sh / 2f, cz), new Vector3(sx, sh, sz), c);
    }
    static GameObject P(string name, float cx, float y0, float cz, float sx, float sh, float sz, Surf s)
    {
        return Box(name, new Vector3(cx, y0 + sh / 2f, cz), new Vector3(sx, sh, sz), s);
    }

    static Light AddLight(Vector3 pos, Color col, float intensity, float range, bool shadows = false)
    {
        var g = new GameObject("Luz");
        g.transform.SetParent(root, false);
        g.transform.localPosition = pos;
        var l = g.AddComponent<Light>();
        l.type = LightType.Point; l.color = col; l.intensity = intensity; l.range = range;
        l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
        return l;
    }

    static GameObject Ball(string name, Vector3 pos, float d, Material m)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        g.name = name; g.transform.SetParent(root, false); g.transform.localPosition = pos; g.transform.localScale = Vector3.one * d;
        g.GetComponent<Renderer>().sharedMaterial = m;
        Object.Destroy(g.GetComponent<Collider>());
        return g;
    }

    /// <summary>Grupo para muebles compuestos: el Interactable va en el grupo y las piezas son hijos con collider.</summary>
    static GameObject Group(string name)
    {
        var g = new GameObject(name);
        g.transform.SetParent(root, false);
        return g;
    }

    /// <summary>Caja (en coordenadas del mundo) como hija de un grupo. El grupo está en el origen sin rotación.</summary>
    static GameObject BoxIn(GameObject grp, string name, Vector3 center, Vector3 size, Surf s, bool collide = true)
    {
        var b = Box(name, center, size, s, collide);
        b.transform.SetParent(grp.transform, true);
        return b;
    }

    static GameObject BallIn(GameObject grp, string name, Vector3 pos, float d, Material m)
    {
        var b = Ball(name, pos, d, m);
        b.transform.SetParent(grp.transform, true);
        return b;
    }

    static Vector2 G(float center, float width) { return new Vector2(center, width); }

    // ------------------------------------------------------------------ paredes
    /// <summary>Segmento de pared de dos capas: A (lado menor de la coordenada fija) y B (lado mayor).</summary>
    static void Seg(bool alongX, float f, float a, float b, float y0, float yFrom, float yTo, Surf A, Surf B, bool trim)
    {
        if (b - a < 0.02f) return;
        float len = b - a, mid = (a + b) / 2f, h = yTo - yFrom, cy = y0 + yFrom + h / 2f;
        for (int side = 0; side < 2; side++)
        {
            float off = (side == 0 ? -1f : 1f) * T / 4f;
            Vector3 c = alongX ? new Vector3(mid, cy, f + off) : new Vector3(f + off, cy, mid);
            Vector3 sz = alongX ? new Vector3(len, h, T / 2f) : new Vector3(T / 2f, h, len);
            Box("Pared", c, sz, side == 0 ? A : B);
        }
        if (!trim) return;
        // zócalo y cornisa
        Vector3 bs = alongX ? new Vector3(len, 0.16f, T + 0.06f) : new Vector3(T + 0.06f, 0.16f, len);
        Vector3 bc = alongX ? new Vector3(mid, y0 + yFrom + 0.08f, f) : new Vector3(f, y0 + yFrom + 0.08f, mid);
        if (yFrom == 0f) Box("Zocalo", bc, bs, sDark);
        Vector3 cs = alongX ? new Vector3(len, 0.12f, T + 0.05f) : new Vector3(T + 0.05f, 0.12f, len);
        Vector3 cc = alongX ? new Vector3(mid, y0 + yTo - 0.06f, f) : new Vector3(f, y0 + yTo - 0.06f, mid);
        Box("Cornisa", cc, cs, sPlaster, false);
    }

    /// <summary>Pared a lo largo de X (z fija) o de Z (x fija) con huecos de puerta (centro, ancho) y marcos.</summary>
    static void Wall(bool alongX, float fixedC, float from, float to, float y0, Surf A, Surf B, params Vector2[] gaps)
    {
        System.Array.Sort(gaps, (p, q) => p.x.CompareTo(q.x));
        float cur = from - T / 2f;
        foreach (var g in gaps)
        {
            float a = g.x - g.y / 2f, b = g.x + g.y / 2f;
            Seg(alongX, fixedC, cur, a, y0, 0f, H, A, B, true);
            Seg(alongX, fixedC, a, b, y0, DH, H, A, B, true);   // dintel
            DoorFrame(alongX, fixedC, g.x, g.y, y0);
            cur = b;
        }
        Seg(alongX, fixedC, cur, to + T / 2f, y0, 0f, H, A, B, true);
    }

    static void DoorFrame(bool alongX, float f, float c, float w, float y0)
    {
        float d = T + 0.08f, j = 0.12f;
        Vector3 sj = alongX ? new Vector3(j, DH, d) : new Vector3(d, DH, j);
        Vector3 sh = alongX ? new Vector3(w + 2 * j, j, d) : new Vector3(d, j, w + 2 * j);
        Vector3 p1 = alongX ? new Vector3(c - w / 2f - j / 2f, y0 + DH / 2f, f) : new Vector3(f, y0 + DH / 2f, c - w / 2f - j / 2f);
        Vector3 p2 = alongX ? new Vector3(c + w / 2f + j / 2f, y0 + DH / 2f, f) : new Vector3(f, y0 + DH / 2f, c + w / 2f + j / 2f);
        Vector3 p3 = alongX ? new Vector3(c, y0 + DH + j / 2f, f) : new Vector3(f, y0 + DH + j / 2f, c);
        Box("MarcoPuerta", p1, sj, sDark); Box("MarcoPuerta", p2, sj, sDark); Box("MarcoPuerta", p3, sh, sDark);
    }

    // ------------------------------------------------------------------ decoración
    static Vector3 Pos(bool alongX, float along, float y, float perp)
    {
        return alongX ? new Vector3(along, y, perp) : new Vector3(perp, y, along);
    }
    static Vector3 Sz(bool alongX, float a, float h, float d)
    {
        return alongX ? new Vector3(a, h, d) : new Vector3(d, h, a);
    }

    /// <summary>Ventana con marco, cortinas y luz de luna. Devuelve el cristal (por si es interactivo).</summary>
    static GameObject Window(bool alongX, float f, float along, float y0, int sign, bool curtains = true)
    {
        float inner = f + sign * (T / 2f);
        var pane = Box("Cristal", Pos(alongX, along, y0 + 1.9f, inner + sign * 0.03f), Sz(alongX, 1.2f, 1.7f, 0.05f), C(new Color(0.1f, 0.12f, 0.2f)), true);
        pane.GetComponent<Renderer>().sharedMaterial = EmitMat(new Color(0.25f, 0.36f, 0.62f), 0.9f);
        float pz = inner + sign * 0.05f;
        Box("MarcoV", Pos(alongX, along - 0.64f, y0 + 1.9f, pz), Sz(alongX, 0.1f, 1.9f, 0.1f), sDark, false);
        Box("MarcoV", Pos(alongX, along + 0.64f, y0 + 1.9f, pz), Sz(alongX, 0.1f, 1.9f, 0.1f), sDark, false);
        Box("MarcoH", Pos(alongX, along, y0 + 2.8f, pz), Sz(alongX, 1.4f, 0.1f, 0.1f), sDark, false);
        Box("MarcoH", Pos(alongX, along, y0 + 1.0f, pz), Sz(alongX, 1.4f, 0.12f, 0.14f), sDark, false);
        Box("TravesanoV", Pos(alongX, along, y0 + 1.9f, pz + sign * 0.01f), Sz(alongX, 0.05f, 1.7f, 0.06f), sDark, false);
        Box("TravesanoH", Pos(alongX, along, y0 + 1.9f, pz + sign * 0.01f), Sz(alongX, 1.2f, 0.05f, 0.06f), sDark, false);
        if (curtains)
        {
            var red = C(new Color(0.35f, 0.04f, 0.06f), 0.05f);
            Box("Cortina", Pos(alongX, along - 0.85f, y0 + 1.85f, pz + sign * 0.07f), Sz(alongX, 0.35f, 2.3f, 0.1f), red, false);
            Box("Cortina", Pos(alongX, along + 0.85f, y0 + 1.85f, pz + sign * 0.07f), Sz(alongX, 0.35f, 2.3f, 0.1f), red, false);
            Box("Galeria", Pos(alongX, along, y0 + 3.0f, pz + sign * 0.07f), Sz(alongX, 2.3f, 0.12f, 0.12f), sDark, false);
        }
        AddLight(Pos(alongX, along, y0 + 1.9f, inner + sign * 0.7f).ToVec(), new Color(0.45f, 0.58f, 1f), 1.1f, 5.5f);
        return pane;
    }

    static Vector3 ToVec(this Vector3 v) { return v; }

    static void Painting(bool alongX, float f, float along, float y, int sign, float w, float h, int variant)
    {
        float inner = f + sign * (T / 2f);
        Box("MarcoCuadro", Pos(alongX, along, y, inner + sign * 0.03f), Sz(alongX, w + 0.16f, h + 0.16f, 0.06f), C(Gold, 0.5f), false);
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = "Cuadro"; q.transform.SetParent(root, false);
        q.transform.localPosition = Pos(alongX, along, y, inner + sign * 0.065f);
        q.transform.localScale = new Vector3(w, h, 1f);
        float rot = alongX ? (sign > 0 ? 180f : 0f) : (sign > 0 ? -90f : 90f);
        q.transform.localRotation = Quaternion.Euler(0f, rot, 0f);
        Object.Destroy(q.GetComponent<Collider>());
        q.GetComponent<Renderer>().sharedMaterial = MatOf(S(TexGen.Painting(variant), 1f, 0.2f));
    }

    static void Sconce(bool alongX, float f, float along, float y, int sign)
    {
        float inner = f + sign * (T / 2f);
        Box("Aplique", Pos(alongX, along, y, inner + sign * 0.08f), Sz(alongX, 0.14f, 0.3f, 0.16f), C(Gold, 0.6f), false);
        var flame = Ball("Llama", Pos(alongX, along, y + 0.22f, inner + sign * 0.1f), 0.12f, EmitMat(new Color(1f, 0.7f, 0.3f), 2.5f));
        var l = AddLight(Pos(alongX, along, y + 0.4f, inner + sign * 0.5f), new Color(1f, 0.7f, 0.35f), 1.0f, 6f);
        var fl = l.gameObject.AddComponent<Flicker>(); fl.baseIntensity = 1.0f; fl.amount = 0.18f;
    }

    static void Chandelier(float x, float yCeil, float z)
    {
        Box("Cadena", new Vector3(x, yCeil - 0.25f, z), new Vector3(0.04f, 0.5f, 0.04f), C(Gold, 0.6f), false);
        Box("BrazoX", new Vector3(x, yCeil - 0.55f, z), new Vector3(1.3f, 0.06f, 0.06f), C(Gold, 0.6f), false);
        Box("BrazoZ", new Vector3(x, yCeil - 0.55f, z), new Vector3(0.06f, 0.06f, 1.3f), C(Gold, 0.6f), false);
        var em = EmitMat(new Color(1f, 0.8f, 0.45f), 3f);
        Ball("Vela", new Vector3(x + 0.65f, yCeil - 0.45f, z), 0.14f, em);
        Ball("Vela", new Vector3(x - 0.65f, yCeil - 0.45f, z), 0.14f, em);
        Ball("Vela", new Vector3(x, yCeil - 0.45f, z + 0.65f), 0.14f, em);
        Ball("Vela", new Vector3(x, yCeil - 0.45f, z - 0.65f), 0.14f, em);
        var l = AddLight(new Vector3(x, yCeil - 0.7f, z), Warm, 2.6f, 13f, true);
        var fl = l.gameObject.AddComponent<Flicker>(); fl.baseIntensity = 2.6f; fl.amount = 0.08f; fl.speed = 3f;
    }

    static void Fireplace(float x, float z)
    {
        // chimenea contra la pared z = 8 (lado -z)
        Box("Chimenea", new Vector3(x, 0.85f, z - 0.25f), new Vector3(2.4f, 1.7f, 0.5f), sStone);
        Box("Repisa", new Vector3(x, 1.75f, z - 0.3f), new Vector3(2.8f, 0.1f, 0.6f), sDark);
        Box("Hogar", new Vector3(x, 0.5f, z - 0.52f), new Vector3(1.2f, 0.9f, 0.05f), C(Color.black), false);
        var fire = Box("Fuego", new Vector3(x, 0.35f, z - 0.56f), new Vector3(0.9f, 0.5f, 0.04f), C(Color.black), false);
        fire.GetComponent<Renderer>().sharedMaterial = EmitMat(new Color(1f, 0.45f, 0.1f), 2.4f);
        var l = AddLight(new Vector3(x, 0.8f, z - 1.2f), new Color(1f, 0.5f, 0.15f), 2.2f, 8f);
        var fl = l.gameObject.AddComponent<Flicker>(); fl.baseIntensity = 2.2f; fl.amount = 0.35f; fl.speed = 9f;
    }

    static void Rug(float cx, float y0, float cz, float w, float d)
    {
        Box("Alfombra", new Vector3(cx, y0 + 0.03f, cz), new Vector3(w, 0.02f, d), sCarpet, false);
    }

    static void Room(string name, float x0, float x1, float z0, float z1, float y0, Surf floor, bool light, float intensity = 1.6f, bool zone = true, Color lightColor = default(Color))
    {
        Box("PisoCuarto", new Vector3((x0 + x1) / 2f, y0 + 0.01f, (z0 + z1) / 2f), new Vector3(x1 - x0, 0.02f, z1 - z0), floor, false);
        if (light) AddLight(new Vector3((x0 + x1) / 2f, y0 + 2.6f, (z0 + z1) / 2f), lightColor.a > 0f ? lightColor : Warm, intensity, 9f);
        if (zone) gm.zones.Add(new Zone { name = name, x0 = x0, x1 = x1, z0 = z0, z1 = z1, y0 = y0 });
    }

    /// <summary>
    /// Reja de hierro: marco, barrotes verticales con punta de lanza, travesaños y adornos. Se ve a través de ella.
    /// Las piezas son hijas de la hoja (centro local = centro de la reja) y no tienen collider propio.
    /// </summary>
    static void BarredGate(Transform leaf, float w, float h)
    {
        var iron = Mon.Lit(new Color(0.07f, 0.07f, 0.09f), 0.55f);
        var ironLight = Mon.Lit(new Color(0.16f, 0.16f, 0.19f), 0.6f);
        float hw = w / 2f, hh = h / 2f;
        // marco: dos postes, travesaño superior, inferior y uno intermedio
        Mon.Part(leaf, PrimitiveType.Cube, new Vector3(-hw + 0.04f, 0f, 0f), new Vector3(0.08f, h, 0.07f), iron);
        Mon.Part(leaf, PrimitiveType.Cube, new Vector3(hw - 0.04f, 0f, 0f), new Vector3(0.08f, h, 0.07f), iron);
        Mon.Part(leaf, PrimitiveType.Cube, new Vector3(0f, hh - 0.35f, 0f), new Vector3(w, 0.06f, 0.05f), iron);
        Mon.Part(leaf, PrimitiveType.Cube, new Vector3(0f, -hh + 0.18f, 0f), new Vector3(w, 0.07f, 0.05f), iron);
        Mon.Part(leaf, PrimitiveType.Cube, new Vector3(0f, -0.1f, 0f), new Vector3(w, 0.05f, 0.04f), iron);
        // barrotes verticales cada 12 cm con punta de lanza arriba (cubo girado 45 grados)
        int n = Mathf.FloorToInt((w - 0.16f) / 0.12f);
        float x0 = -(n - 1) * 0.06f;
        for (int i = 0; i < n; i++)
        {
            float x = x0 + i * 0.12f;
            Mon.Part(leaf, PrimitiveType.Cube, new Vector3(x, 0f, 0f), new Vector3(0.022f, h - 0.06f, 0.022f), iron);
            var tip = Mon.Part(leaf, PrimitiveType.Cube, new Vector3(x, hh + 0.01f, 0f), new Vector3(0.05f, 0.05f, 0.03f), ironLight);
            tip.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }
        // adornos: aros en el tercio superior y bolas en los postes
        for (int i = 3; i < n - 3; i += 6)
        {
            float x = x0 + i * 0.12f + 0.06f;
            Mon.Part(leaf, PrimitiveType.Sphere, new Vector3(x, hh - 0.35f, 0f), new Vector3(0.16f, 0.16f, 0.03f), ironLight);
            Mon.Part(leaf, PrimitiveType.Sphere, new Vector3(x, hh - 0.35f, 0f), new Vector3(0.09f, 0.09f, 0.04f), iron);
        }
        Mon.Part(leaf, PrimitiveType.Sphere, new Vector3(-hw + 0.04f, hh + 0.04f, 0f), Vector3.one * 0.12f, ironLight);
        Mon.Part(leaf, PrimitiveType.Sphere, new Vector3(hw - 0.04f, hh + 0.04f, 0f), Vector3.one * 0.12f, ironLight);
        // bisagras en el poste del lado de la bisagra y pasador del cierre
        for (int i = -1; i <= 1; i += 2)
            Mon.Part(leaf, PrimitiveType.Cube, new Vector3(-hw + 0.06f, i * (hh - 0.4f), 0.045f), new Vector3(0.16f, 0.07f, 0.02f), ironLight);
        Mon.Part(leaf, PrimitiveType.Cube, new Vector3(hw - 0.12f, -0.1f, 0.045f), new Vector3(0.2f, 0.04f, 0.025f), ironLight);
    }

    /// <summary>Cilindro con el eje a lo largo del espesor de la puerta (el cilindro de Unity tiene el eje en Y y mide 2 de alto).</summary>
    static GameObject Disc(Transform leaf, bool alongX, Vector3 pos, float diameter, float depth, Material m)
    {
        var g = Mon.Part(leaf, PrimitiveType.Cylinder, pos, new Vector3(diameter, depth / 2f, diameter), m);
        g.transform.localRotation = alongX ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.Euler(0f, 0f, 90f);
        return g;
    }

    /// <summary>
    /// Herrajes de una cara de la puerta: placa de latón, cerradura de pasador (cilindro del color de la llave con su
    /// ojo de cerradura: círculo y ranura) y manija de palanca con roseta. Sin color de llave solo lleva manija.
    /// </summary>
    static void DoorHardware(Transform leaf, bool alongX, int side, float w, float h, Color lockColor, Material brass, Material dark)
    {
        Vector3 along = alongX ? Vector3.right : Vector3.forward;       // hacia el borde libre
        Vector3 thick = (alongX ? Vector3.forward : Vector3.right) * side;
        Vector3 c = along * (w / 2f - 0.17f) + Vector3.up * (-h / 2f + 1.0f);   // centro de la placa (manija a ~1 m)
        float s = 0.012f;                                                // grosor de la placa
        // placa larga de latón
        Mon.Part(leaf, PrimitiveType.Cube, c + thick * (0.05f + s / 2f),
                 alongX ? new Vector3(0.085f, 0.34f, s) : new Vector3(s, 0.34f, 0.085f), brass);
        // manija de palanca: roseta, palanca horizontal hacia la bisagra y remate redondo
        Vector3 lever = c + Vector3.down * 0.045f;
        Disc(leaf, alongX, lever + thick * (0.05f + s + 0.012f), 0.058f, 0.024f, brass);
        Mon.Part(leaf, PrimitiveType.Cube, lever + thick * (0.05f + s + 0.03f) - along * 0.06f,
                 alongX ? new Vector3(0.13f, 0.022f, 0.022f) : new Vector3(0.022f, 0.022f, 0.13f), brass);
        Mon.Part(leaf, PrimitiveType.Sphere, lever + thick * (0.05f + s + 0.03f) - along * 0.125f, Vector3.one * 0.03f, brass);
        if (lockColor.a <= 0f) return;
        // cerradura de pasador, arriba de la manija: aro de latón + cilindro del color de la llave
        Vector3 bolt = c + Vector3.up * 0.105f;
        Disc(leaf, alongX, bolt + thick * (0.05f + s + 0.008f), 0.092f, 0.016f, brass);
        Disc(leaf, alongX, bolt + thick * (0.05f + s + 0.02f), 0.072f, 0.024f, Mon.Glow(lockColor, 1.2f));
        // ojo de la cerradura: círculo + ranura hacia abajo
        Vector3 eye = bolt + thick * (0.05f + s + 0.033f);
        Disc(leaf, alongX, eye + Vector3.up * 0.006f, 0.016f, 0.006f, dark);
        Mon.Part(leaf, PrimitiveType.Cube, eye + Vector3.down * 0.012f,
                 alongX ? new Vector3(0.008f, 0.026f, 0.006f) : new Vector3(0.006f, 0.026f, 0.008f), dark);
    }

    /// <summary>Candado de la reja: cuerpo del color de la llave, arco en U de acero, ojo de cerradura y cierre en la reja.</summary>
    static void Padlock(Transform leaf, bool alongX, int side, float w, Color lockColor, Material brass, Material dark)
    {
        Vector3 along = alongX ? Vector3.right : Vector3.forward;
        Vector3 thick = (alongX ? Vector3.forward : Vector3.right) * side;
        var steel = Mon.Lit(new Color(0.65f, 0.67f, 0.7f), 0.85f);
        Vector3 post = along * (w / 2f - 0.28f);                           // junto al poste del cierre, donde la reja se une a la pared
        Vector3 c = post + thick * 0.075f + Vector3.up * (-0.06f);        // centro del cuerpo
        // pletina del cierre (en el travesaño del centro) y argolla
        Mon.Part(leaf, PrimitiveType.Cube, post + thick * 0.045f + Vector3.up * 0.1f,
                 alongX ? new Vector3(0.22f, 0.07f, 0.02f) : new Vector3(0.02f, 0.07f, 0.22f), brass);
        // cuerpo (caja ancha con hombros) en el color de la llave
        var body = Mon.Glow(lockColor, 1.0f);
        Mon.Part(leaf, PrimitiveType.Cube, c, alongX ? new Vector3(0.17f, 0.13f, 0.05f) : new Vector3(0.05f, 0.13f, 0.17f), body);
        Mon.Part(leaf, PrimitiveType.Cube, c + Vector3.up * 0.075f, alongX ? new Vector3(0.14f, 0.03f, 0.045f) : new Vector3(0.045f, 0.03f, 0.14f), body);
        // arco en U: dos patas y un travesaño con esquinas a 45 grados
        float leg = 0.045f, top = 0.17f;
        Mon.Part(leaf, PrimitiveType.Cube, c + Vector3.up * 0.115f + along * leg, alongX ? new Vector3(0.022f, 0.08f, 0.022f) : new Vector3(0.022f, 0.08f, 0.022f), steel);
        Mon.Part(leaf, PrimitiveType.Cube, c + Vector3.up * 0.115f - along * leg, alongX ? new Vector3(0.022f, 0.08f, 0.022f) : new Vector3(0.022f, 0.08f, 0.022f), steel);
        Mon.Part(leaf, PrimitiveType.Cube, c + Vector3.up * top, alongX ? new Vector3(0.1f, 0.022f, 0.022f) : new Vector3(0.022f, 0.022f, 0.1f), steel);
        var cornerA = Mon.Part(leaf, PrimitiveType.Cube, c + Vector3.up * 0.16f + along * (leg - 0.01f), Vector3.one * 0.03f, steel);
        var cornerB = Mon.Part(leaf, PrimitiveType.Cube, c + Vector3.up * 0.16f - along * (leg - 0.01f), Vector3.one * 0.03f, steel);
        cornerA.transform.localRotation = Quaternion.Euler(45f, 45f, 0f); cornerB.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
        // ojo de la cerradura abajo + brillo
        Disc(leaf, alongX, c + Vector3.down * 0.02f + thick * 0.027f, 0.022f, 0.006f, dark);
        Mon.Part(leaf, PrimitiveType.Cube, c + Vector3.down * 0.05f + thick * 0.027f,
                 alongX ? new Vector3(0.01f, 0.04f, 0.006f) : new Vector3(0.006f, 0.04f, 0.01f), dark);
        Mon.Part(leaf, PrimitiveType.Cube, c + Vector3.up * 0.02f - along * 0.05f + thick * 0.027f,
                 alongX ? new Vector3(0.012f, 0.07f, 0.004f) : new Vector3(0.004f, 0.07f, 0.012f), brass);
    }

    /// <summary>
    /// Puerta con bisagra (o reja que desaparece). Si lockColor tiene alfa, lleva una placa de cerradura de ese
    /// color (en las dos caras) o un candado colgando (reja): el color debe coincidir con la llave y con el texto.
    /// </summary>
    static Door MakeDoor(string id, Vector3 hinge, bool alongX, float w, float h, Surf s, bool hide, Color lockColor = default(Color))
    {
        var pv = new GameObject("Puerta_" + id);
        pv.transform.SetParent(root, false);
        pv.transform.localPosition = hinge;
        var leaf = new GameObject("Hoja_" + id);
        leaf.transform.SetParent(pv.transform, false);
        Vector3 size = alongX ? new Vector3(w, h, 0.1f) : new Vector3(0.1f, h, w);
        leaf.transform.localPosition = alongX ? new Vector3(w / 2f, h / 2f, 0f) : new Vector3(0f, h / 2f, w / 2f);
        if (!hide)
        {
            leaf.AddComponent<MeshFilter>().sharedMesh = BoxMesh(size, s.tile);
            leaf.AddComponent<MeshRenderer>().sharedMaterial = MatOf(s);
        }
        leaf.AddComponent<BoxCollider>().size = size;   // en la reja es invisible: bloquea el paso y se puede apuntar

        Vector3 along = alongX ? Vector3.right : Vector3.forward;     // a lo largo de la hoja
        Vector3 thick = alongX ? Vector3.forward : Vector3.right;     // espesor
        var brass = MatOf(C(Gold, 0.8f));
        var dark = Mon.Lit(Color.black, 0.1f);
        if (!hide)
        {
            for (int side = -1; side <= 1; side += 2)
                DoorHardware(leaf.transform, alongX, side, w, h, lockColor, brass, dark);
        }
        else
        {
            BarredGate(leaf.transform, w, h);
            if (lockColor.a > 0f)
            {
                for (int side = -1; side <= 1; side += 2)
                    Padlock(leaf.transform, alongX, side, w, lockColor, brass, dark);
            }
        }
        var d = pv.AddComponent<Door>();
        d.id = id; d.leaf = leaf; d.hide = hide; d.openAngle = alongX ? -95f : 95f;
        gm.doorMap[id] = d;
        return d;
    }

    // Atajos para definir interacciones
    static List<Opt> One(string label, System.Action a) { return new List<Opt> { new Opt(label, a) }; }
    static Opt O(string label, System.Action a) { return new Opt(label, a); }
    static void Act(GameObject g, string title, string label, System.Action a) { Interactable.Add(g, title, () => One(label, a)); }
    static void Choose(GameObject g, string title, string desc, params Opt[] opts) { Interactable.Add(g, title, () => new List<Opt>(opts), desc); }
    static void Look(GameObject g, string title, string text) { Act(g, title, "Examinar", () => gm.Say(text)); }

    static void Take(string item, string first, string again)
    {
        if (gm.Has(item)) { gm.Say(again); return; }
        gm.Give(item);
        string extra = "";
        if (gm.FlashReady && (item == "Linterna" || item == "Pilas")) extra = " ¡Linterna lista! Presiona F para encenderla.";
        else if (item == "Linterna") extra = " Pero no tiene pilas.";
        else if (item == "Pilas") extra = " Quizá sirvan para una linterna.";
        gm.Say(first + extra);
    }

    static void KeyDoor(Door d, string key, string title, string lockedMsg, string openMsg)
    {
        Act(d.leaf, title, "Abrir", () =>
        {
            if (gm.Has(key)) { d.Open(); gm.Say(openMsg); }
            else gm.Say(lockedMsg);
        });
    }

    static void Lamp(string name, float x, float y0, float z, float sx, float sz, float yaw)
    {
        Box("BaseLampara", new Vector3(x, y0 + 0.4f, z), new Vector3(0.35f, 0.8f, 0.35f), C(new Color(0.2f, 0.2f, 0.25f), 0.5f));
        var orb = Ball("Lampara_" + name, new Vector3(x, y0 + 1.0f, z), 0.4f, EmitMat(new Color(0.35f, 0.6f, 1f), 1.5f));
        var l = AddLight(new Vector3(x, y0 + 1.3f, z), new Color(0.35f, 0.6f, 1f), 1.6f, 5f);
        gm.checkpoints.Add(new CheckpointInfo
        {
            name = name, lampPos = new Vector3(x, y0, z), spawn = new Vector3(sx, y0 + 0.05f, sz), yaw = yaw,
            rend = orb.GetComponent<Renderer>(), light = l
        });
    }

    // ------------------------------------------------------------------ construcción
    public static void Build(Transform parent, GameManager g)
    {
        root = parent; gm = g;
        InitSurfaces();
        Structure();
        GroundFloor();
        SecondFloor();
        Lamp("Vestíbulo", 9.0f, 0f, 2.0f, 11.2f, 2.0f, 90f);
        Lamp("Biblioteca", 17.2f, 0f, 9.2f, 19.4f, 9.2f, 90f);
        Lamp("Segundo piso", 9.3f, F2, 6.0f, 11.5f, 6.0f, 90f);
        Lamp("Oficina", 17.2f, F2, 9.2f, 19.4f, 9.2f, 90f);
    }

    static void Slab(float x0, float x1, float z0, float z1, float yc, float th, Surf s)
    {
        Box("Losa", new Vector3((x0 + x1) / 2f, yc, (z0 + z1) / 2f), new Vector3(x1 - x0, th, z1 - z0), s);
    }

    static void Structure()
    {
        // terreno, piso de planta baja, techo
        Box("Terreno", new Vector3(12f, -0.65f, 8f), new Vector3(90f, 1f, 90f), C(new Color(0.05f, 0.09f, 0.05f), 0.02f));
        Box("PisoPB", new Vector3(12f, -0.1f, 8f), new Vector3(24.6f, 0.2f, 16.6f), sDark);
        Box("Techo", new Vector3(12f, 6.5f, 8f), new Vector3(24.6f, 0.2f, 16.6f), sPlaster);

        // losa del piso 2 con hueco para la escalera
        const float hx0 = 12.4f, hx1 = 15.85f, hz0 = 3.9f, hz1 = 10.0f;
        Slab(-0.15f, hx0, -0.15f, 16.15f, 3.2f, 0.2f, sPlaster);
        Slab(hx1, 24.15f, -0.15f, 16.15f, 3.2f, 0.2f, sPlaster);
        Slab(hx0, hx1, -0.15f, hz0, 3.2f, 0.2f, sPlaster);
        Slab(hx0, hx1, hz1, 16.15f, 3.2f, 0.2f, sPlaster);

        // escalera (11 escalones de 0.3 m) con alfombra
        for (int i = 0; i < 11; i++)
        {
            float top = 0.3f * (i + 1), z = 4.0f + 0.55f * i + 0.275f;
            Box("Escalon", new Vector3((hx0 + hx1) / 2f, top / 2f, z), new Vector3(hx1 - hx0, top, 0.55f), sWood);
            Box("AlfombraEscalon", new Vector3((hx0 + hx1) / 2f, top + 0.012f, z), new Vector3(hx1 - hx0 - 0.5f, 0.024f, 0.55f), sCarpet, false);
        }
        // pared lateral de la escalera y barandales del piso 2
        Box("LateralEscalera", new Vector3(12.3f, H / 2f, (3.7f + 10.0f) / 2f), new Vector3(0.2f, H, 6.3f), sDark);
        Box("Barandal", new Vector3(hx0, F2 + 0.5f, (hz0 + hz1) / 2f), new Vector3(0.1f, 1f, hz1 - hz0), sDark);
        Box("Barandal", new Vector3((hx0 + hx1) / 2f, F2 + 0.5f, hz0), new Vector3(hx1 - hx0, 1f, 0.1f), sDark);
        for (int i = 0; i < 7; i++)   // balaustres
        {
            Box("Balaustre", new Vector3(hx0, F2 + 0.55f, hz0 + 0.4f + i * 0.85f), new Vector3(0.08f, 1.1f, 0.08f), sDark, false);
        }
    }

    static void GroundFloor()
    {
        float y = 0f;
        // paredes exteriores (A = lado menor de la coordenada fija, B = lado mayor)
        Wall(true, 0f, 0f, 8f, y, sStone, wpSala);
        Wall(true, 0f, 8f, 16f, y, sStone, wpHall, G(12f, 2f));
        Wall(true, 0f, 16f, 24f, y, sStone, wpCocina);
        Wall(true, 16f, 0f, 8f, y, wpC1, sStone);
        Wall(true, 16f, 8f, 12f, y, wpBano, sStone);
        Wall(true, 16f, 12f, 16f, y, wpHall, sStone);
        Wall(true, 16f, 16f, 24f, y, wpBib, sStone);
        Wall(false, 0f, 0f, 8f, y, sStone, wpSala);
        Wall(false, 0f, 8f, 16f, y, sStone, wpC1);
        Wall(false, 24f, 0f, 8f, y, wpCocina, sStone);
        Wall(false, 24f, 8f, 16f, y, wpBib, sStone);
        // interiores
        Wall(false, 8f, 0f, 8f, y, wpSala, wpHall, G(3f, 1.6f));
        Wall(false, 8f, 8f, 12f, y, wpC1, wpHall, G(10f, 1.6f));
        Wall(false, 8f, 12f, 16f, y, wpC1, wpBano);
        Wall(false, 16f, 0f, 8f, y, wpHall, wpCocina, G(1.8f, 1.6f));
        Wall(false, 16f, 8f, 16f, y, wpHall, wpBib, G(13f, 1.6f));
        Wall(true, 8f, 0f, 8f, y, wpSala, wpC1);
        Wall(true, 8f, 16f, 24f, y, wpCocina, wpBib);
        Wall(true, 12f, 8f, 12f, y, wpHall, wpBano, G(10f, 1.2f));
        Wall(false, 12f, 12f, 16f, y, wpBano, wpHall);

        // cuartos: piso + luz + zona
        Room("Sala", 0f, 8f, 0f, 8f, y, sWood, true, 1.6f, true, new Color(1f, 0.7f, 0.4f));
        Room("Cuarto 1", 0f, 8f, 8f, 16f, y, floorBlue, true, 1.5f, true, new Color(0.6f, 0.7f, 1f));
        Room("Cocina", 16f, 24f, 0f, 8f, y, S(TexGen.Tile("cocinapiso", new Color(0.92f, 0.92f, 0.88f), new Color(0.12f, 0.12f, 0.14f)), 2.0f, 0.5f), true, 2.0f, true, new Color(1f, 1f, 0.92f));
        Room("Biblioteca", 16f, 24f, 8f, 16f, y, floorRed, true, 1.4f, true, new Color(1f, 0.72f, 0.3f));
        Room("Baño", 8f, 12f, 12f, 16f, y, S(TexGen.Tile("banopiso", new Color(0.95f, 0.97f, 0.97f), new Color(0.35f, 0.7f, 0.75f)), 2.0f, 0.6f), true, 1.6f, true, new Color(0.6f, 1f, 1f));
        Room("Vestíbulo", 8f, 16f, 0f, 12f, y, sMarble, false);
        Room("Vestíbulo", 12f, 16f, 12f, 16f, y, sMarble, false, 1f, false);
        Chandelier(12f, 3.1f, 3.5f);
        Chandelier(11f, 3.1f, 10.5f);

        // ---- Vestíbulo
        var front = MakeDoor("principal", new Vector3(11f, y, 0f), true, 2f, DH, sDark, false, new Color(1f, 0.72f, 0.12f));
        Act(front.leaf, "Puerta principal", "Intentar salir", () =>
        {
            if (gm.Has("Llave dorada")) gm.Win("FINAL: ¡ESCAPASTE!\n\nGiras la llave dorada y la puerta se abre. Corres hasta que la mansión desaparece detrás de los árboles... Nunca volverás a comprar una casa barata.");
            else gm.Say("La puerta principal está cerrada con una cerradura dorada. Necesitas la llave dorada.");
        });
        var gate = MakeDoor("reja", new Vector3(12.4f, y, 3.7f), true, 3.45f, 2.5f, C(new Color(0.2f, 0.2f, 0.25f), 0.6f), true, new Color(0.3f, 0.5f, 1f));
        KeyDoor(gate, "Llave azul", "Reja de la escalera", "La reja de la escalera está cerrada con un candado azul.", "Usas la llave azul y la reja de la escalera se abre. Ya puedes subir.");
        Look(P("Reloj", 15.5f, y, 15.4f, 0.6f, 2.0f, 0.5f, sDark), "Reloj de pie", "El reloj está detenido a las 3:00 de la madrugada.");
        Box("Alfombra", new Vector3(12f, 0.03f, 6f), new Vector3(2.4f, 0.02f, 11.5f), sCarpet, false);
        P("Pilastra", 8.4f, y, 5.0f, 0.5f, H, 0.5f, sMarble);
        P("Pilastra", 8.4f, y, 8.6f, 0.5f, H, 0.5f, sMarble);
        Window(true, 0f, 9.5f, y, 1);
        Window(true, 0f, 14.5f, y, 1);
        Painting(false, 8f, 6.8f, 1.9f, 1, 0.9f, 1.2f, 0);
        Painting(true, 16f, 14f, 1.9f, -1, 1.6f, 1.2f, 1);
        Sconce(false, 8f, 3.3f, 1.8f, 1);
        Sconce(false, 16f, 14.5f, 1.8f, -1);

        // ---- Sala
        Window(true, 0f, 4f, y, 1);
        Fireplace(4f, 8f);
        Painting(false, 0f, 2.5f, 1.9f, 1, 0.9f, 1.2f, 2);
        Rug(4.2f, y, 4.0f, 3.6f, 2.8f);
        Look(P("Sofa", 2.5f, y, 1.0f, 3.0f, 0.8f, 0.9f, new Color(0.45f, 0.1f, 0.12f)), "Sofá viejo", "Un sofá cubierto de polvo. No hay nada.");
        OpenDiary(null, 4.7f, 0.8f, 5.0f, -90f);   // el diario que nombra el texto se ve abierto sobre la mesa
        Act(P("MesaSala", 4.5f, y, 5.0f, 2.0f, 0.8f, 1.0f, sWood), "Mesa con un diario", "Leer el diario", () =>
            gm.Say("Diario de Javier, 29 de octubre de 1993: «Compré esta casa en 1973. Nunca debí hacerlo. Hay algo en los espejos.»"));
        var mirror = OrnateMirror("Espejo", 0.15f, 1.6f, 5.5f, Vector3.right, 1.2f, 1.8f, true);   // espejo real: muestra tu reflejo (retrasado un segundo)
        Choose(mirror, "Espejo", "Tu reflejo te mira... un segundo tarde.",
            O("Mirarte en el espejo", () => gm.Die("mirror", mirror)),
            O("Apartar la mirada", () => gm.Say("Mejor no mirar el espejo.")));

        // ---- Cocina
        Window(true, 0f, 20f, y, 1, false);
        var fridge = P("Refri", 23.2f, y, 6.8f, 1.2f, 2.1f, 1.0f, C(White, 0.5f));
        FridgeDetails(23.2f, y, 6.8f, Vector3.left, 1.0f, 1.2f);   // zumba fuerte y gotea algo oscuro
        Choose(fridge, "Refrigerador", "Zumba muy fuerte y gotea algo oscuro.",
            O("Abrir el refri", () => gm.Die("fridge", fridge)),
            O("Dejarlo cerrado", () => gm.Say("Mejor no abrir el refri...")));
        P("MostradorCocina", 20f, y, 0.7f, 5f, 1.0f, 0.9f, C(new Color(0.55f, 0.55f, 0.58f), 0.5f));
        var cajon = KitchenDrawerUnit("Mostrador", 20f, y, 1.15f, Vector3.forward, 0.9f, 0.2f);   // el cajón se desliza y deja ver la linterna
        ContainerInteract(cajon, "Cajón del mostrador", cajon.GetComponent<Openable>(), "Linterna", "Abrir el cajón",
            "Dentro del cajón hay cubiertos, fósforos y una linterna vieja.", "Tomar la linterna", "Encontraste una linterna.", "El cajón está vacío.", "Cerrar el cajón");
        var alacena = OpenablePantry("Alacena", 17.0f, y, 7.4f, Vector3.back, 1.4f, 0.9f, 2.0f);
        ContainerInteract(alacena, "Alacena", alacena.GetComponent<Openable>(), null, "Abrir la alacena",
            "Solo latas vencidas y telarañas.", "", "", "Solo latas vencidas y telarañas.", "Cerrar la alacena");
        P("MesaCocina", 20f, y, 4.5f, 1.6f, 0.8f, 1.6f, sWood);
        Sconce(false, 24f, 4f, 1.8f, -1);

        // ---- Cuarto 1
        Window(true, 16f, 4f, y, -1);
        Rug(4.0f, y, 12.0f, 3.0f, 2.4f);
        var cama1 = UnmadeBed("Cama1", 2.0f, y, 14.4f, Vector3.back, 2.0f, 3.0f, 0);   // deshecha y todavía tibia
        Choose(cama1, "Cama", "Está tibia, como si alguien acabara de levantarse.",
            O("Acostarte a dormir", () => gm.Die("bed", cama1)),
            O("Mejor no", () => gm.Say("No es buen momento para dormir.")));
        var closet = P("Closet", 7.35f, y, 12.5f, 1.0f, 2.4f, 2.2f, sDark);
        Choose(closet, "Closet", "Algo respira del otro lado de la puerta.",
            O("Abrir el closet", () => gm.Die("closet", closet)),
            O("Dejarlo cerrado", () => gm.Say("Prefieres no averiguar qué hay dentro.")));
        var mesita = NightstandWithBatteries("Mesita", 3.9f, y, 15.6f, Vector3.back, 0.7f, 0.55f, 0.65f);
        ContainerInteract(mesita, "Mesita de noche", mesita.GetComponent<Openable>(), "Pilas", "Abrir el cajón",
            "En el cajón hay unas pilas, un rosario y recibos viejos.", "Tomar las pilas", "Encontraste unas pilas.", "La mesita ya no tiene nada.", "Cerrar el cajón");

        // ---- Baño
        var banera = P("Banera", 10.9f, y, 14.4f, 1.4f, 0.7f, 2.6f, C(White, 0.8f));
        Choose(banera, "Bañera con cortina", "La cortina se mueve sola. Gotea agua.",
            O("Correr la cortina", () => gm.Die("tub", banera)),
            O("No tocarla", () => gm.Say("Mejor dejar la cortina como está.")));
        Act(P("Lavabo", 8.7f, y, 13.0f, 0.6f, 0.9f, 1.0f, C(White, 0.8f)), "Lavabo", "Mirar dentro",
            () => Take("Llave verde", "Dentro del lavabo hay una llave verde.", "Solo queda agua oscura."));

        // ---- Biblioteca (cerrada con llave verde)
        var bib = MakeDoor("biblioteca", new Vector3(16f, y, 12.2f), false, 1.6f, DH, sDark, false, new Color(0.2f, 0.85f, 0.3f));
        KeyDoor(bib, "Llave verde", "Puerta de la biblioteca", "Está cerrada. La cerradura es verde.", "La llave verde encaja. La puerta se abre con un chirrido.");
        Window(true, 16f, 20f, y, -1);
        Rug(20.5f, y, 12.5f, 3.4f, 2.6f);
        P("Estante", 23.55f, y, 12.0f, 0.5f, 2.6f, 6.0f, sBooks);
        P("EstanteNorte", 18.0f, y, 15.55f, 3.2f, 2.6f, 0.5f, sBooks);
        var libroRojo = Box("LibroRojo", new Vector3(23.15f, 1.5f, 10.8f), new Vector3(0.25f, 0.45f, 0.2f), C(new Color(0.85f, 0.08f, 0.08f), 0.4f));
        Choose(libroRojo,
            "Libro rojo", "Una nota pegada dice: «No tocar. J.»",
            O("Sacar el libro rojo", () => gm.Die("pit", libroRojo)),
            O("Dejarlo", () => gm.Say("Mejor no lo toco.")));
        Act(Box("LibroAzul", new Vector3(23.15f, 1.5f, 12.0f), new Vector3(0.25f, 0.45f, 0.2f), C(new Color(0.15f, 0.3f, 0.95f), 0.4f)),
            "Libro azul", "Abrir el libro", () => Take("Llave azul", "Entre las páginas del libro azul hay una llave azul.", "Ya tomaste la llave azul."));
        Look(Box("LibroVerde", new Vector3(23.15f, 1.5f, 13.2f), new Vector3(0.25f, 0.45f, 0.2f), C(new Color(0.1f, 0.55f, 0.2f), 0.4f)),
            "Libro verde", "Un libro de recetas muy raras. Nada útil.");
        Look(P("EscritorioBib", 21.5f, y, 9.0f, 2.0f, 0.8f, 1.0f, sWood), "Escritorio", "Una nota: «Las llaves están escondidas en lugares que parecen inofensivos. — J.»");
    }

    static void SecondFloor()
    {
        float y = F2;
        // paredes exteriores
        Wall(true, 0f, 0f, 8f, y, sStone, wpC3);
        Wall(true, 0f, 8f, 16f, y, sStone, wpHall2);
        Wall(true, 0f, 16f, 24f, y, sStone, wpC4);
        Wall(true, 16f, 0f, 8f, y, wpPiano, sStone);
        Wall(true, 16f, 8f, 16f, y, wpHall2, sStone);
        Wall(true, 16f, 16f, 24f, y, wpOfi, sStone);
        Wall(false, 0f, 0f, 8f, y, sStone, wpC3);
        Wall(false, 0f, 8f, 16f, y, sStone, wpPiano);
        Wall(false, 24f, 0f, 8f, y, wpC4, sStone);
        Wall(false, 24f, 8f, 16f, y, wpOfi, sStone);
        // interiores
        Wall(false, 8f, 0f, 8f, y, wpC3, wpHall2, G(3f, 1.6f));
        Wall(false, 8f, 8f, 16f, y, wpPiano, wpHall2, G(10f, 1.6f));
        Wall(false, 16f, 0f, 8f, y, wpHall2, wpC4, G(1.8f, 1.6f));
        Wall(false, 16f, 8f, 16f, y, wpHall2, wpOfi, G(13f, 1.6f));
        Wall(true, 8f, 0f, 8f, y, wpC3, wpPiano);
        Wall(true, 8f, 16f, 24f, y, wpC4, wpOfi);

        Room("Cuarto 3", 0f, 8f, 0f, 8f, y, floorPink, true, 1.5f, true, new Color(1f, 0.72f, 0.85f));
        Room("Salón del piano", 0f, 8f, 8f, 16f, y, sDark, false);
        Room("Cuarto 4", 16f, 24f, 0f, 8f, y, floorTerracotta, true, 1.5f, true, new Color(1f, 0.6f, 0.3f));
        Room("Oficina", 16f, 24f, 8f, 16f, y, sDark, true, 1.5f, true, new Color(0.7f, 1f, 0.8f));
        // El piso del pasillo rodea el hueco de la escalera (x 12.4..15.85, z 3.9..10.0): si lo cubriera, la escalera se
        // vería techada desde abajo y se atravesaría al subir.
        gm.zones.Add(new Zone { name = "Pasillo del segundo piso", x0 = 8f, x1 = 16f, z0 = 0f, z1 = 16f, y0 = y });
        Room("Pasillo del segundo piso", 8f, 12.4f, 0f, 16f, y, sWood, false, 1f, false);
        Room("Pasillo del segundo piso", 12.4f, 15.85f, 0f, 3.9f, y, sWood, false, 1f, false);
        Room("Pasillo del segundo piso", 12.4f, 15.85f, 10.0f, 16f, y, sWood, false, 1f, false);
        Room("Pasillo del segundo piso", 15.85f, 16f, 0f, 16f, y, sWood, false, 1f, false);
        Chandelier(12f, y + H, 2.5f);
        AddLight(new Vector3(12f, y + 2.7f, 13f), Warm, 1.6f, 10f);
        Box("Alfombra", new Vector3(12f, y + 0.03f, 13f), new Vector3(2.2f, 0.02f, 5.5f), sCarpet, false);
        Window(true, 16f, 12f, y, -1);
        Painting(false, 8f, 6.0f, y + 1.9f, 1, 0.9f, 1.2f, 1);
        Sconce(false, 8f, 12.5f, y + 1.8f, 1);

        // ---- Cuarto 3
        Window(true, 0f, 4f, y, 1);
        Rug(4.0f, y, 4.0f, 3.2f, 2.4f);
        Look(UnmadeBed("Cama3", 2.0f, y, 1.6f, Vector3.forward, 2.0f, 3.0f, 1), "Cama sin hacer", "Una cama sin hacer. Las sábanas están frías.");
        var comoda = DresserWithContents("Comoda", 4.0f, y, 7.45f, Vector3.back, 2.0f, 0.8f, 1.0f);
        ContainerInteract(comoda, "Cómoda", comoda.GetComponent<Openable>(), "Partituras", "Abrir los cajones",
            "Dentro hay ropa vieja y unas partituras de piano.", "Tomar las partituras", "Entre la ropa vieja hay unas partituras de piano.", "Solo queda ropa vieja.", "Cerrar los cajones");
        var tele = P("Tele", 7.3f, y, 5.2f, 0.5f, 1.1f, 1.6f, C(new Color(0.04f, 0.04f, 0.04f), 0.6f));
        Choose(tele, "Televisión", "La pantalla muestra estática sin estar conectada.",
            O("Encender la tele", () => gm.Die("tv", tele)),
            O("Dejarla apagada", () => gm.Say("Mejor la dejo así.")));

        // ---- Piano (oscuro: hace falta linterna encendida)
        Window(true, 16f, 4f, y, -1);
        var piano = P("Piano", 2.0f, y, 13.2f, 2.2f, 1.2f, 1.2f, C(new Color(0.04f, 0.04f, 0.05f), 0.8f));
        Act(piano, "Piano", "Tocar el piano", () =>
        {
            if (!gm.FlashOn) { gm.Say("Está demasiado oscuro para ver bien. Necesitas la linterna con pilas encendida (F)."); return; }
            if (gm.Has("Llave amarilla")) { gm.Say("Ya tocaste la melodía."); return; }
            if (!gm.Has("Partituras")) { gm.Say("Es un piano antiguo. Faltan las partituras."); return; }
            gm.Give("Llave amarilla");
            gm.audio.Sfx("piano", 0.8f);
            gm.Say("Tocas la melodía de las partituras. Un compartimento secreto se abre: ¡una llave amarilla!");
        });
        P("MesaMuneca", 6.8f, y, 15.3f, 0.8f, 0.7f, 0.8f, sWood);
        var muneca = P("Muneca", 6.8f, y + 0.7f, 15.3f, 0.3f, 0.5f, 0.3f, C(new Color(0.85f, 0.75f, 0.65f), 0.3f));
        Choose(muneca, "Muñeca", "Sus ojos de vidrio brillan con tu luz.",
            O("Tomar la muñeca", () => { if (!gm.FlashOn) gm.Say("No ves casi nada aquí. Enciende la linterna."); else gm.Die("doll", muneca); }),
            O("Dejarla", () => gm.Say("Mejor la dejo donde está.")));

        // ---- Cuarto 4
        var armario = P("Armario", 23.3f, y, 5.0f, 1.0f, 2.4f, 2.0f, sDark);
        Choose(armario, "Armario", "Se oyen rasguños desde adentro.",
            O("Abrir el armario", () => gm.Die("wardrobe", armario)),
            O("Alejarte", () => gm.Say("Retrocedes despacio.")));
        Look(Window(false, 24f, 2.0f, y, -1, false), "Ventana", "Afuera solo ves el bosque en la oscuridad... y algo que te observa.");
        Look(UnmadeBed("Cama4", 18.5f, y, 6.65f, Vector3.back, 2.0f, 2.4f, 2), "Cama", "Una cama con un hueco como de alguien que dormía aquí hace años.");

        // ---- Oficina (cerrada con llave amarilla)
        var ofi = MakeDoor("oficina", new Vector3(16f, y, 12.2f), false, 1.6f, DH, sDark, false, new Color(1f, 0.9f, 0.25f));
        KeyDoor(ofi, "Llave amarilla", "Puerta de la oficina", "Está cerrada con una cerradura amarilla.", "La llave amarilla abre la oficina.");
        Window(true, 16f, 20f, y, -1);
        Rug(20.0f, y, 12.5f, 3.4f, 2.6f);
        Painting(false, 24f, 11.0f, y + 1.9f, -1, 1.0f, 1.2f, 2);
        Look(P("EscritorioOfi", 21.5f, y, 9.0f, 2.0f, 0.8f, 1.0f, sWood), "Escritorio", "Una hoja: «La caja fuerte usa el año en que compré la casa.»");
        var safe = P("CajaFuerte", 23.3f, y, 13.0f, 0.9f, 1.0f, 0.9f, C(new Color(0.28f, 0.28f, 0.32f), 0.7f));
        Interactable.Add(safe, "Caja fuerte", () =>
        {
            if (gm.Has("Llave dorada")) return One("Mirar", () => gm.Say("La caja fuerte está vacía."));
            return new List<Opt>
            {
                O("Código 1937", WrongCode),
                O("Código 1973", () => { gm.Give("Llave dorada"); gm.Say("¡Clic! La caja se abre y dentro hay una llave dorada. ¡Es la llave de la puerta principal!"); }),
                O("Código 1793", WrongCode),
            };
        }, "Un teclado de cuatro dígitos. ¿Cuál es el código?");
    }

    static void WrongCode()
    {
        gm.Die("alarm", null);
    }
}
