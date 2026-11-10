using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Detailed procedural furniture and fixtures (primitives and cached box meshes only, no asset files).
///
/// Conventions shared by every builder:
///  - (cx, y0, cz) is the centre of the footprint on the floor, y0 the floor height.
///  - facing is one of Vector3.forward/back/right/left: the direction the FRONT of the piece looks at.
///  - w = extent perpendicular to facing, d = extent along facing, h = total height.
///  - Each piece is designed once in a local frame (x = right, y = up, z = front) and mapped to the world
///    with Frame: right = Cross(up, fwd). Local +x is the piece's own right (stage right), which a viewer
///    standing in front of the piece sees on their LEFT.
///  - Builders return a Group at the origin without rotation; solid parts carry colliders, small decoration
///    does not. The caller attaches the Interactable to the group.
/// </summary>
public static partial class MansionBuilder
{
    // ------------------------------------------------------------------ local frame
    struct Frame
    {
        public Vector3 o, fwd, right;
        public Frame(float cx, float y0, float cz, Vector3 facing)
        {
            Vector3 fw = new Vector3(facing.x, 0f, facing.z);
            if (fw.sqrMagnitude < 0.0001f) fw = Vector3.forward;
            fw.Normalize();
            fwd = fw;
            right = Vector3.Cross(Vector3.up, fw);
            o = new Vector3(cx, y0, cz);
        }
        public Vector3 At(float lx, float ly, float lz) { return o + right * lx + Vector3.up * ly + fwd * lz; }
        public Vector3 Dir(float lx, float ly, float lz) { return right * lx + Vector3.up * ly + fwd * lz; }
        public Vector3 Size(float sx, float sy, float sz)
        {
            return new Vector3(Mathf.Abs(right.x) * sx + Mathf.Abs(fwd.x) * sz, sy, Mathf.Abs(right.z) * sx + Mathf.Abs(fwd.z) * sz);
        }
        public Quaternion Rot { get { return Quaternion.LookRotation(fwd, Vector3.up); } }
    }

    /// <summary>Box centred at local (lx, ly, lz) with local size (sx, sy, sz).</summary>
    static GameObject FB(GameObject g, Frame f, string n, float lx, float ly, float lz, float sx, float sy, float sz, Surf s, bool col = true)
    {
        return BoxIn(g, n, f.At(lx, ly, lz), f.Size(sx, sy, sz), s, col);
    }

    /// <summary>Box whose BOTTOM sits at local yb (centre lx, lz).</summary>
    static GameObject FP(GameObject g, Frame f, string n, float lx, float yb, float lz, float sx, float sh, float sz, Surf s, bool col = true)
    {
        return BoxIn(g, n, f.At(lx, yb + sh / 2f, lz), f.Size(sx, sh, sz), s, col);
    }

    static GameObject Mat(GameObject b, Material m) { b.GetComponent<Renderer>().sharedMaterial = m; return b; }

    static GameObject FBm(GameObject g, Frame f, string n, float lx, float ly, float lz, float sx, float sy, float sz, Material m, bool col = true)
    {
        return Mat(FB(g, f, n, lx, ly, lz, sx, sy, sz, fIron, col), m);
    }

    static GameObject FPm(GameObject g, Frame f, string n, float lx, float yb, float lz, float sx, float sh, float sz, Material m, bool col = true)
    {
        return Mat(FP(g, f, n, lx, yb, lz, sx, sh, sz, fIron, col), m);
    }

    static GameObject FBall(GameObject g, Frame f, string n, float lx, float ly, float lz, float dia, Material m)
    {
        return BallIn(g, n, f.At(lx, ly, lz), dia, m);
    }

    /// <summary>Ellipsoid (scaled sphere) with local scale (sx, sy, sz), no collider.</summary>
    static GameObject FEll(GameObject g, Frame f, string n, float lx, float ly, float lz, float sx, float sy, float sz, Material m)
    {
        var b = BallIn(g, n, f.At(lx, ly, lz), 1f, m);
        b.transform.localRotation = f.Rot;
        b.transform.localScale = new Vector3(sx, sy, sz);
        return b;
    }

    /// <summary>Rotated box: centre at local (lx, ly, lz), raw size (sx, sy, sz), extra local Euler rotation.</summary>
    static GameObject FRot(GameObject g, Frame f, string n, float lx, float ly, float lz, float sx, float sy, float sz, Vector3 euler, Surf s, bool col = false)
    {
        var b = BoxIn(g, n, f.At(lx, ly, lz), new Vector3(sx, sy, sz), s, col);
        b.transform.localRotation = f.Rot * Quaternion.Euler(euler);
        return b;
    }

    /// <summary>Rotated box placed by a pivot plus an offset expressed in the rotated axes (for leaves, rods, arms).</summary>
    static GameObject FRotOff(GameObject g, Frame f, string n, float px, float py, float pz, Vector3 off, float sx, float sy, float sz, Vector3 euler, Surf s, bool col = false)
    {
        Quaternion q = f.Rot * Quaternion.Euler(euler);
        var b = BoxIn(g, n, f.At(px, py, pz) + q * off, new Vector3(sx, sy, sz), s, col);
        b.transform.localRotation = q;
        return b;
    }

    static GameObject FRotm(GameObject g, Frame f, string n, float lx, float ly, float lz, float sx, float sy, float sz, Vector3 euler, Material m, bool col = false)
    {
        return Mat(FRot(g, f, n, lx, ly, lz, sx, sy, sz, euler, fIron, col), m);
    }

    /// <summary>Quad with 0..1 UVs (whole texture on the face). localNormal is the direction the face looks at, in frame axes.</summary>
    static GameObject FQuad(GameObject g, Frame f, string n, float lx, float ly, float lz, float w, float h, Vector3 localNormal, Material m)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = n;
        q.transform.SetParent(root, false);
        q.transform.localPosition = f.At(lx, ly, lz);
        Vector3 nrm = f.Dir(localNormal.x, localNormal.y, localNormal.z).normalized;
        Vector3 upHint = Mathf.Abs(nrm.y) > 0.9f ? f.fwd : Vector3.up;
        q.transform.localRotation = Quaternion.LookRotation(-nrm, upHint);   // Unity quads look along -z
        q.transform.localScale = new Vector3(w, h, 1f);
        UnityEngine.Object.Destroy(q.GetComponent<Collider>());
        q.GetComponent<Renderer>().sharedMaterial = m;
        q.transform.SetParent(g.transform, true);
        return q;
    }

    /// <summary>Invisible BoxCollider child (no renderer) so the player can aim at or bump into a soft/hollow piece.</summary>
    static GameObject HitboxIn(GameObject g, string n, Vector3 center, Vector3 size)
    {
        var hb = new GameObject(n);
        hb.transform.SetParent(root, false);
        hb.transform.localPosition = center;
        hb.AddComponent<BoxCollider>().size = size;
        hb.transform.SetParent(g.transform, true);
        return hb;
    }

    static GameObject FHit(GameObject g, Frame f, string n, float lx, float ly, float lz, float sx, float sy, float sz)
    {
        return HitboxIn(g, n, f.At(lx, ly, lz), f.Size(sx, sy, sz));
    }

    /// <summary>Four thin boxes forming a rectangular rim (outer size w x d) centred at local (lx, ly, lz).</summary>
    static void Rim4(GameObject g, Frame f, string n, float lx, float ly, float lz, float w, float d, float t, float hgt, Surf s, bool col = false)
    {
        FB(g, f, n, lx, ly, lz + d / 2f - t / 2f, w, hgt, t, s, col);
        FB(g, f, n, lx, ly, lz - d / 2f + t / 2f, w, hgt, t, s, col);
        FB(g, f, n, lx + w / 2f - t / 2f, ly, lz, t, hgt, d - 2f * t, s, col);
        FB(g, f, n, lx - w / 2f + t / 2f, ly, lz, t, hgt, d - 2f * t, s, col);
    }

    static void Rim4m(GameObject g, Frame f, string n, float lx, float ly, float lz, float w, float d, float t, float hgt, Material m)
    {
        FBm(g, f, n, lx, ly, lz + d / 2f - t / 2f, w, hgt, t, m, false);
        FBm(g, f, n, lx, ly, lz - d / 2f + t / 2f, w, hgt, t, m, false);
        FBm(g, f, n, lx + w / 2f - t / 2f, ly, lz, t, hgt, d - 2f * t, m, false);
        FBm(g, f, n, lx - w / 2f + t / 2f, ly, lz, t, hgt, d - 2f * t, m, false);
    }

    /// <summary>Four thin boxes forming a VERTICAL frame (in the local x-y plane) of outer size w x hgt, centred at (lx, ly, lz).</summary>
    static void RimV4(GameObject g, Frame f, string n, float lx, float ly, float lz, float w, float hgt, float t, float depth, Surf s)
    {
        FB(g, f, n, lx, ly + hgt / 2f - t / 2f, lz, w, t, depth, s, false);
        FB(g, f, n, lx, ly - hgt / 2f + t / 2f, lz, w, t, depth, s, false);
        FB(g, f, n, lx + w / 2f - t / 2f, ly, lz, t, hgt - 2f * t, depth, s, false);
        FB(g, f, n, lx - w / 2f + t / 2f, ly, lz, t, hgt - 2f * t, depth, s, false);
    }

    static void RimV4m(GameObject g, Frame f, string n, float lx, float ly, float lz, float w, float hgt, float t, float depth, Material m)
    {
        FBm(g, f, n, lx, ly + hgt / 2f - t / 2f, lz, w, t, depth, m, false);
        FBm(g, f, n, lx, ly - hgt / 2f + t / 2f, lz, w, t, depth, m, false);
        FBm(g, f, n, lx + w / 2f - t / 2f, ly, lz, t, hgt - 2f * t, depth, m, false);
        FBm(g, f, n, lx - w / 2f + t / 2f, ly, lz, t, hgt - 2f * t, depth, m, false);
    }

    /// <summary>Metallic material (URP Lit) cached by colour/smoothness/metallic.</summary>
    static Material Metal(Color c, float smooth, float metallic)
    {
        string key = "metal_" + c + "_" + smooth + "_" + metallic;
        Material m;
        if (mats.TryGetValue(key, out m) && m != null) return m;
        m = new Material(BaseLit());
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", metallic);
        mats[key] = m;
        return m;
    }

    /// <summary>Textured material that also glows faintly with its own texture (clock dials, screens).</summary>
    static Material TexEmit(Texture2D t, float k, float smooth)
    {
        var m = EmitMat(Color.white, k);
        m.SetColor("_BaseColor", Color.white);
        m.SetTexture("_BaseMap", t);
        m.SetTexture("_EmissionMap", t);
        m.SetFloat("_Smoothness", smooth);
        return m;
    }

    static float Hash01(int a, int b)
    {
        int hh = a * 374761393 + b * 668265263;
        hh = (hh ^ (hh >> 13)) * 1274126177;
        return ((hh ^ (hh >> 16)) & 0xffff) / 65535f;
    }

    // ------------------------------------------------------------------ shared surfaces and materials
    static bool fxReady;
    static Surf fDamaskRed, fDamaskGreen, fVelvetBlue, fLeather, fLinen, fQuiltBlue, fQuiltGreen, fLace, fSteel, fTowel, fTowelBlue;
    static Surf fPorcelain, fBlackLacquer, fIron, fTerracotta, fGlassDark, fMirror, fPaper, fIvory, fCream, fTileSplash, fGranite, fPlasticGrey, fWhiteEnamel, fGreenGlass;
    static Material mBrass, mChrome, mSteel, mFlame, mLampShade, mBlackGloss, mGlassEye, mDial, mScreen;
    static readonly Color[] bookPal =
    {
        new Color(0.52f, 0.1f, 0.1f), new Color(0.12f, 0.22f, 0.42f), new Color(0.14f, 0.34f, 0.2f), new Color(0.45f, 0.3f, 0.12f),
        new Color(0.28f, 0.1f, 0.3f), new Color(0.2f, 0.18f, 0.16f), new Color(0.6f, 0.52f, 0.36f), new Color(0.1f, 0.1f, 0.12f), new Color(0.55f, 0.25f, 0.1f)
    };

    static void Fx()
    {
        if (fxReady) return;
        fxReady = true;
        fIron = C(new Color(0.12f, 0.12f, 0.13f), 0.35f);
        fDamaskRed = S(TexGen.Fabric("damaskred", new Color(0.45f, 0.08f, 0.10f), new Color(0.30f, 0.05f, 0.07f)), 0.5f, 0.08f);
        fDamaskGreen = S(TexGen.Fabric("damaskgreen", new Color(0.12f, 0.30f, 0.18f), new Color(0.08f, 0.2f, 0.12f)), 0.5f, 0.08f);
        fVelvetBlue = S(TexGen.Fabric("velvetblue", new Color(0.14f, 0.18f, 0.40f), new Color(0.10f, 0.12f, 0.30f)), 0.5f, 0.05f);
        fLeather = S(TexGen.Leather(), 0.8f, 0.45f);
        fLinen = S(TexGen.Fabric("linen", new Color(0.88f, 0.86f, 0.80f), new Color(0.82f, 0.80f, 0.74f)), 0.4f, 0.05f);
        fQuiltBlue = S(TexGen.Quilt("quiltblue", new Color(0.22f, 0.26f, 0.48f), new Color(0.65f, 0.62f, 0.55f)), 0.6f, 0.05f);
        fQuiltGreen = S(TexGen.Quilt("quiltgreen", new Color(0.26f, 0.36f, 0.26f), new Color(0.6f, 0.55f, 0.42f)), 0.6f, 0.05f);
        fLace = S(TexGen.Lace(), 0.25f, 0.05f);
        fSteel = S(TexGen.Steel(), 1.0f, 0.75f);
        fTowel = S(TexGen.Fabric("towel", new Color(0.9f, 0.9f, 0.88f), new Color(0.84f, 0.84f, 0.82f)), 0.3f, 0.02f);
        fTowelBlue = S(TexGen.Fabric("towelblue", new Color(0.35f, 0.5f, 0.6f), new Color(0.28f, 0.42f, 0.52f)), 0.3f, 0.02f);
        fPorcelain = C(new Color(0.92f, 0.93f, 0.92f), 0.85f);
        fWhiteEnamel = C(new Color(0.9f, 0.9f, 0.88f), 0.6f);
        fBlackLacquer = C(new Color(0.03f, 0.03f, 0.035f), 0.92f);
        fTerracotta = C(new Color(0.62f, 0.33f, 0.2f), 0.15f);
        fGlassDark = C(new Color(0.08f, 0.1f, 0.1f), 0.95f);
        fGreenGlass = C(new Color(0.1f, 0.35f, 0.2f), 0.9f);
        fMirror = C(new Color(0.72f, 0.8f, 0.9f), 0.95f);
        fPaper = C(new Color(0.9f, 0.87f, 0.78f), 0.05f);
        fIvory = C(new Color(0.9f, 0.86f, 0.74f), 0.4f);
        fCream = C(new Color(0.85f, 0.8f, 0.7f), 0.1f);
        fTileSplash = S(TexGen.Tile("splash", new Color(0.85f, 0.85f, 0.8f), new Color(0.75f, 0.8f, 0.78f)), 0.5f, 0.5f);
        fGranite = C(new Color(0.13f, 0.13f, 0.14f), 0.7f);
        fPlasticGrey = C(new Color(0.14f, 0.14f, 0.15f), 0.4f);
        mBrass = Metal(new Color(0.85f, 0.66f, 0.3f), 0.8f, 0.7f);
        mChrome = Metal(new Color(0.8f, 0.82f, 0.85f), 0.9f, 0.8f);
        mSteel = Metal(new Color(0.55f, 0.56f, 0.58f), 0.6f, 0.6f);
        mFlame = EmitMat(new Color(1f, 0.72f, 0.32f), 2.5f);
        mLampShade = EmitMat(new Color(1f, 0.85f, 0.6f), 0.9f);
        mBlackGloss = MatOf(fBlackLacquer);
        mGlassEye = MatOf(C(Color.black, 0.95f));
        mDial = TexEmit(TexGen.ClockFace(), 0.12f, 0.3f);
        mScreen = EmitMat(new Color(0.35f, 0.42f, 0.4f), 0.08f);
    }

    // ------------------------------------------------------------------ small reusable parts
    /// <summary>Turned leg from yb to yt: square top block, bead, shaft, bead, foot.</summary>
    static void TurnedLeg(GameObject g, Frame f, float lx, float lz, float yb, float yt, float t, Surf s)
    {
        float len = yt - yb;
        FP(g, f, "Pata", lx, yt - 0.08f, lz, t, 0.08f, t, s, false);
        FBall(g, f, "PataBola", lx, yt - 0.11f, lz, t * 1.3f, MatOf(s));
        FP(g, f, "PataFuste", lx, yb + 0.06f, lz, t * 0.7f, len - 0.2f, t * 0.7f, s, false);
        FBall(g, f, "PataBola", lx, yb + 0.07f, lz, t * 1.15f, MatOf(s));
        FP(g, f, "PataPie", lx, yb, lz, t * 0.8f, 0.04f, t * 0.8f, s, false);
    }

    /// <summary>Tapered leg from yb to yt (three stacked blocks, thinner at the bottom).</summary>
    static void TaperedLeg(GameObject g, Frame f, float lx, float lz, float yb, float yt, float t, Surf s, bool caster = false)
    {
        float len = yt - yb;
        FP(g, f, "Pata", lx, yb + len * 0.66f, lz, t, len * 0.34f, t, s, false);
        FP(g, f, "Pata", lx, yb + len * 0.33f, lz, t * 0.8f, len * 0.34f, t * 0.8f, s, false);
        FP(g, f, "Pata", lx, yb + (caster ? 0.04f : 0f), lz, t * 0.62f, len * 0.33f - (caster ? 0.04f : 0f), t * 0.62f, s, false);
        if (caster) FBall(g, f, "Rueda", lx, yb + 0.03f, lz, 0.06f, mBrass);
    }

    /// <summary>Closed book lying flat: cover, pages, spine band. Rotated about the vertical by yaw degrees.</summary>
    static void BookFlat(GameObject g, Frame f, float lx, float yb, float lz, float bw, float bh, float bd, Color cover, float yaw)
    {
        Surf cs = C(cover, 0.35f);
        FRot(g, f, "Libro", lx, yb + bh / 2f, lz, bw, bh, bd, new Vector3(0f, yaw, 0f), cs, false);
        FRot(g, f, "Paginas", lx, yb + bh / 2f, lz, bw - 0.015f, bh - 0.012f, bd - 0.01f, new Vector3(0f, yaw, 0f), fPaper, false);
        FRotOff(g, f, "Lomo", lx, yb + bh / 2f, lz, new Vector3(-bw / 2f + 0.004f, 0f, 0f), 0.012f, bh + 0.004f, bd + 0.004f, new Vector3(0f, yaw, 0f), cs, false);
    }

    /// <summary>Brass candlestick with a lit candle (emissive flame, no Light component).</summary>
    static void CandleStick(GameObject g, Frame f, float lx, float yb, float lz, float hgt)
    {
        FEll(g, f, "BaseVela", lx, yb + 0.01f, lz, 0.1f, 0.02f, 0.1f, mBrass);
        FPm(g, f, "FusteVela", lx, yb + 0.02f, lz, 0.02f, hgt * 0.5f, 0.02f, mBrass, false);
        FBall(g, f, "BolaVela", lx, yb + 0.02f + hgt * 0.25f, lz, 0.04f, mBrass);
        FEll(g, f, "PlatoVela", lx, yb + 0.02f + hgt * 0.5f, lz, 0.07f, 0.012f, 0.07f, mBrass);
        FP(g, f, "Vela", lx, yb + 0.03f + hgt * 0.5f, lz, 0.025f, hgt * 0.45f, 0.025f, C(new Color(0.95f, 0.92f, 0.82f), 0.3f), false);
        FBall(g, f, "Llama", lx, yb + 0.03f + hgt * 0.95f + 0.02f, lz, 0.035f, mFlame);
    }

    /// <summary>Table lamp parts: brass base, stem, harp, pleated emissive shade and finial. yb is the surface height.</summary>
    static void TableLampIn(GameObject g, Frame f, float lx, float yb, float lz)
    {
        FEll(g, f, "BaseLampara", lx, yb + 0.015f, lz, 0.18f, 0.03f, 0.18f, mBrass);
        FPm(g, f, "PieLampara", lx, yb + 0.03f, lz, 0.05f, 0.04f, 0.05f, mBrass, false);
        FP(g, f, "CuerpoLampara", lx, yb + 0.07f, lz, 0.07f, 0.2f, 0.07f, C(new Color(0.2f, 0.28f, 0.22f), 0.7f), false);
        FBall(g, f, "BolaLampara", lx, yb + 0.29f, lz, 0.09f, MatOf(C(new Color(0.2f, 0.28f, 0.22f), 0.7f)));
        FPm(g, f, "CuelloLampara", lx, yb + 0.32f, lz, 0.02f, 0.1f, 0.02f, mBrass, false);
        FPm(g, f, "Casquillo", lx, yb + 0.41f, lz, 0.04f, 0.03f, 0.04f, mBrass, false);
        // tapered pleated shade: three stacked tiers
        FPm(g, f, "Pantalla", lx, yb + 0.36f, lz, 0.22f, 0.07f, 0.22f, mLampShade, false);
        FPm(g, f, "Pantalla", lx, yb + 0.43f, lz, 0.26f, 0.07f, 0.26f, mLampShade, false);
        FPm(g, f, "Pantalla", lx, yb + 0.50f, lz, 0.30f, 0.07f, 0.30f, mLampShade, false);
        FBall(g, f, "Remate", lx, yb + 0.59f, lz, 0.03f, mBrass);
    }

    /// <summary>Porcelain vase with dried flowers.</summary>
    static void VaseIn(GameObject g, Frame f, float lx, float yb, float lz, float k)
    {
        Material por = MatOf(C(new Color(0.2f, 0.3f, 0.55f), 0.8f));
        FEll(g, f, "PieJarron", lx, yb + 0.012f * k, lz, 0.12f * k, 0.025f * k, 0.12f * k, por);
        FEll(g, f, "Jarron", lx, yb + 0.15f * k, lz, 0.22f * k, 0.26f * k, 0.22f * k, por);
        FPm(g, f, "CuelloJarron", lx, yb + 0.26f * k, lz, 0.08f * k, 0.1f * k, 0.08f * k, por, false);
        FEll(g, f, "LabioJarron", lx, yb + 0.36f * k, lz, 0.12f * k, 0.03f * k, 0.12f * k, por);
        Surf stem = C(new Color(0.3f, 0.25f, 0.12f), 0.1f);
        for (int i = 0; i < 3; i++)
        {
            float ang = i * 120f;
            FRotOff(g, f, "Tallo", lx, yb + 0.36f * k, lz, new Vector3(0f, 0.14f * k, 0f), 0.006f, 0.28f * k, 0.006f, new Vector3(18f, ang, 0f), stem, false);
            Vector3 tip = Quaternion.Euler(18f, ang, 0f) * new Vector3(0f, 0.28f * k, 0f);
            FBall(g, f, "Flor", lx + tip.x, yb + 0.36f * k + tip.y, lz + tip.z, 0.045f * k, MatOf(C(i == 1 ? new Color(0.6f, 0.5f, 0.3f) : new Color(0.5f, 0.2f, 0.2f), 0.1f)));
        }
    }

    /// <summary>Human skull on a surface (decoration).</summary>
    static void SkullIn(GameObject g, Frame f, float lx, float yb, float lz)
    {
        Material bone = MatOf(C(new Color(0.85f, 0.8f, 0.7f), 0.3f));
        FEll(g, f, "Craneo", lx, yb + 0.1f, lz, 0.15f, 0.15f, 0.17f, bone);
        FB(g, f, "Mandibula", lx, yb + 0.03f, lz + 0.02f, 0.11f, 0.05f, 0.1f, C(new Color(0.85f, 0.8f, 0.7f), 0.3f), false);
        FBall(g, f, "Ojo", lx - 0.035f, yb + 0.1f, lz + 0.07f, 0.04f, MatOf(C(Color.black, 0.2f)));
        FBall(g, f, "Ojo", lx + 0.035f, yb + 0.1f, lz + 0.07f, 0.04f, MatOf(C(Color.black, 0.2f)));
        FB(g, f, "Nariz", lx, yb + 0.06f, lz + 0.08f, 0.02f, 0.03f, 0.01f, C(Color.black, 0.2f), false);
    }

    // ================================================================== 1. GRANDFATHER CLOCK
    /// <summary>Longcase clock stopped at 3:00. Footprint w x d x h (0.6 x 0.5 x 2.2), dial on the facing side.</summary>
    static GameObject GrandfatherClock(float cx, float y0, float cz, Vector3 facing, float w, float d, float h)
    {
        Fx();
        var g = Group("RelojDePie");
        var f = new Frame(cx, y0, cz, facing);
        float hw = w / 2f, hd = d / 2f;
        float yPlinth = 0.045f * h, yBase = 0.23f * h, yHood = h - 0.2f, yTrunk = yHood - Mathf.Min(0.55f, 0.25f * h);
        // plinth and base cabinet
        FP(g, f, "Zocalo", 0f, 0f, 0f, w, yPlinth, d, sDark);
        FP(g, f, "Moldura", 0f, yPlinth, 0f, w + 0.04f, 0.04f, d + 0.04f, sDark, false);
        FP(g, f, "Base", 0f, yPlinth + 0.04f, 0f, w, yBase - yPlinth - 0.04f, d, sDark);
        FP(g, f, "PanelBase", 0f, yPlinth + 0.1f, hd + 0.01f, w - 0.14f, yBase - yPlinth - 0.2f, 0.02f, sWood, false);
        FP(g, f, "PanelBase", 0f, yPlinth + 0.13f, hd + 0.02f, w - 0.2f, yBase - yPlinth - 0.26f, 0.01f, sDark, false);
        FP(g, f, "MolduraCintura", 0f, yBase, 0f, w + 0.02f, 0.05f, d + 0.02f, sDark, false);
        // trunk: open-front case so the pendulum and weights are visible through the "glass" door
        float tw = w - 0.12f, td = d - 0.06f, ty0 = yBase + 0.05f, th = yTrunk - ty0;
        FP(g, f, "TroncoFondo", 0f, ty0, -td / 2f + 0.015f, tw, th, 0.03f, sDark);
        FP(g, f, "TroncoLado", -tw / 2f + 0.02f, ty0, 0f, 0.04f, th, td, sDark);
        FP(g, f, "TroncoLado", tw / 2f - 0.02f, ty0, 0f, 0.04f, th, td, sDark);
        FP(g, f, "InteriorTronco", 0f, ty0, -td / 2f + 0.035f, tw - 0.08f, th, 0.01f, C(new Color(0.08f, 0.05f, 0.03f), 0.2f), false);
        float dz = td / 2f - 0.015f;
        FP(g, f, "MarcoPuerta", -tw / 2f + 0.065f, ty0, dz, 0.05f, th, 0.03f, sDark, false);
        FP(g, f, "MarcoPuerta", tw / 2f - 0.065f, ty0, dz, 0.05f, th, 0.03f, sDark, false);
        FP(g, f, "MarcoPuerta", 0f, ty0, dz, tw - 0.08f, 0.06f, 0.03f, sDark, false);
        FP(g, f, "MarcoPuerta", 0f, yTrunk - 0.06f, dz, tw - 0.08f, 0.06f, 0.03f, sDark, false);
        FBm(g, f, "Bisagra", tw / 2f - 0.06f, ty0 + 0.15f, dz + 0.02f, 0.012f, 0.07f, 0.012f, mBrass, false);
        FBm(g, f, "Bisagra", tw / 2f - 0.06f, yTrunk - 0.15f, dz + 0.02f, 0.012f, 0.07f, 0.012f, mBrass, false);
        FBall(g, f, "Perilla", -tw / 2f + 0.065f, ty0 + th * 0.5f, dz + 0.025f, 0.025f, mBrass);
        // pendulum and weights
        float bobY = ty0 + 0.22f;
        FPm(g, f, "Varilla", 0f, bobY, 0f, 0.012f, yTrunk - 0.02f - bobY, 0.012f, mBrass, false);
        FEll(g, f, "Pendulo", 0f, bobY, 0.005f, 0.16f, 0.16f, 0.03f, mBrass);
        FEll(g, f, "PenduloCentro", 0f, bobY, 0.022f, 0.06f, 0.06f, 0.01f, MatOf(C(new Color(0.5f, 0.38f, 0.15f), 0.9f)));
        for (int s = -1; s <= 1; s += 2)
        {
            float wx = s * (tw / 2f - 0.12f);
            FPm(g, f, "Pesa", wx, ty0 + 0.45f, -0.05f, 0.06f, 0.22f, 0.06f, mBrass, false);
            FBall(g, f, "PesaTapa", wx, ty0 + 0.68f, -0.05f, 0.05f, mBrass);
            FP(g, f, "Cadena", wx, ty0 + 0.7f, -0.05f, 0.008f, yTrunk - ty0 - 0.7f, 0.008f, fIron, false);
        }
        // hood with front columns
        float hh = yHood - yTrunk;
        FP(g, f, "MolduraCapitel", 0f, yTrunk, 0f, w + 0.02f, 0.05f, d + 0.02f, sDark, false);
        FP(g, f, "Capitel", 0f, yTrunk + 0.05f, -0.04f, w, hh - 0.05f, d - 0.08f, sDark);
        for (int s = -1; s <= 1; s += 2)
        {
            float colx = s * (hw - 0.05f), colz = hd - 0.05f;
            FP(g, f, "ColumnaBase", colx, yTrunk + 0.05f, colz, 0.08f, 0.05f, 0.08f, sDark, false);
            FP(g, f, "Columna", colx, yTrunk + 0.1f, colz, 0.05f, hh - 0.2f, 0.05f, sWood, false);
            FP(g, f, "ColumnaCapitel", colx, yHood - 0.1f, colz, 0.08f, 0.05f, 0.08f, sDark, false);
            FBall(g, f, "Remate", colx, h - 0.12f, colz, 0.06f, mBrass);
        }
        // dial: brass plate, bezel, textured face stopped at 3:00
        float dialY = yTrunk + 0.05f + (hh - 0.05f) / 2f, frontZ = hd - 0.08f;
        float plate = Mathf.Min(w - 0.18f, hh - 0.12f);
        FPm(g, f, "PlacaCaratula", 0f, dialY - plate / 2f, frontZ + 0.008f, plate, plate, 0.015f, mBrass, false);
        FBm(g, f, "FondoCaratula", 0f, dialY, frontZ + 0.017f, plate - 0.04f, plate - 0.04f, 0.004f, MatOf(fIvory), false);
        FQuad(g, f, "Caratula", 0f, dialY, frontZ + 0.021f, plate - 0.06f, plate - 0.06f, new Vector3(0f, 0f, 1f), mDial);
        RimV4m(g, f, "Bisel", 0f, dialY, frontZ + 0.02f, plate - 0.04f, plate - 0.04f, 0.012f, 0.012f, mBrass);
        FBall(g, f, "Perilla", -hw + 0.12f, yTrunk + 0.12f, frontZ + 0.02f, 0.02f, mBrass);
        // crown: cornice, broken pediment and finial
        FP(g, f, "Cornisa", 0f, yHood, 0f, w + 0.06f, 0.05f, d + 0.04f, sDark, false);
        FP(g, f, "Cornisa", 0f, yHood + 0.05f, 0f, w - 0.04f, 0.04f, d - 0.04f, sDark, false);
        FP(g, f, "Fronton", 0f, yHood + 0.09f, 0f, w * 0.7f, 0.04f, d * 0.5f, sDark, false);
        FP(g, f, "Fronton", 0f, yHood + 0.13f, 0f, w * 0.45f, 0.03f, d * 0.4f, sDark, false);
        FBall(g, f, "RemateCentral", 0f, h - 0.035f, 0f, 0.07f, mBrass);
        return g;
    }

    // ================================================================== 2. SEATING AND TABLES
    /// <summary>Three-seat sofa. style 0 = wine damask, 1 = green damask, 2 = leather.</summary>
    static GameObject Sofa(float cx, float y0, float cz, Vector3 facing, float w, float d, float h, int style = 0)
    {
        Fx();
        var g = Group("Sofa");
        var f = new Frame(cx, y0, cz, facing);
        Surf fab = style == 1 ? fDamaskGreen : style == 2 ? fLeather : fDamaskRed;
        Surf pip = C(style == 2 ? new Color(0.25f, 0.14f, 0.08f) : Gold, 0.4f);
        float aw = 0.22f, legH = 0.1f, baseTop = 0.40f, seatTop = 0.54f;
        float hw = w / 2f, hd = d / 2f;
        for (int i = 0; i < 4; i++)
        {
            float lx = (i % 2 == 0 ? -1f : 1f) * (hw - 0.12f), lz = (i < 2 ? -1f : 1f) * (hd - 0.12f);
            FP(g, f, "Pata", lx, 0f, lz, 0.07f, legH * 0.6f, 0.07f, sDark, false);
            FBall(g, f, "PataBola", lx, legH * 0.8f, lz, 0.09f, MatOf(sDark));
        }
        FP(g, f, "Base", 0f, legH, 0f, w, baseTop - legH, d - 0.04f, fab);
        FB(g, f, "RibeteBase", 0f, legH + 0.015f, hd - 0.02f + 0.006f, w - 0.04f, 0.014f, 0.014f, pip, false);
        for (int s = -1; s <= 1; s += 2)
        {
            float ax = s * (hw - aw / 2f);
            FP(g, f, "Brazo", ax, baseTop, 0f, aw, 0.24f, d - 0.04f, fab);
            FEll(g, f, "BrazoRollo", ax, baseTop + 0.25f, 0.02f, aw + 0.02f, 0.18f, d - 0.12f, MatOf(fab));
            FB(g, f, "RibeteBrazo", ax, baseTop + 0.25f, hd - 0.05f, aw + 0.03f, 0.014f, 0.014f, pip, false);
        }
        float inner = w - 2f * aw - 0.02f, cw = (inner - 0.04f) / 3f, cd = d - 0.30f;
        for (int i = 0; i < 3; i++)
        {
            float lx = -inner / 2f + 0.01f + cw / 2f + i * (cw + 0.02f);
            FP(g, f, "Cojin", lx, baseTop, 0.05f, cw, seatTop - baseTop, cd, fab);
            FB(g, f, "RibeteCojin", lx, seatTop, 0.05f + cd / 2f, cw, 0.014f, 0.014f, pip, false);
            FRot(g, f, "Respaldo", lx, seatTop + (h - seatTop - 0.1f) / 2f, -hd + 0.19f, cw - 0.01f, h - seatTop - 0.12f, 0.12f, new Vector3(-4f, 0f, 0f), fab, true);
            for (int bx = 0; bx < 2; bx++)
                for (int by = 0; by < 2; by++)
                    FBall(g, f, "Boton", lx + (bx - 0.5f) * cw * 0.45f, seatTop + 0.1f + by * (h - seatTop - 0.3f), -hd + 0.26f, 0.03f, MatOf(pip));
        }
        FP(g, f, "RespaldoMarco", 0f, baseTop, -hd + 0.07f, w - 2f * aw + 0.02f, h - baseTop, 0.12f, fab);
        FP(g, f, "Cresta", 0f, h - 0.04f, -hd + 0.07f, w - 2f * aw + 0.02f, 0.04f, 0.13f, sDark, false);
        FEll(g, f, "Talla", 0f, h - 0.02f, -hd + 0.07f, 0.3f, 0.07f, 0.12f, MatOf(sDark));
        // throw over the right arm and a loose pillow in the left corner
        FP(g, f, "Manta", hw - aw / 2f, baseTop + 0.33f, 0f, aw + 0.04f, 0.03f, d - 0.3f, fQuiltGreen, false);
        FP(g, f, "MantaCaida", hw + 0.012f, baseTop - 0.1f, 0f, 0.025f, 0.43f, d - 0.34f, fQuiltGreen, false);
        FRot(g, f, "Almohadon", -inner / 2f + 0.2f, seatTop + 0.17f, -hd + 0.3f, 0.34f, 0.34f, 0.1f, new Vector3(-20f, 0f, 0f), fVelvetBlue, false);
        return g;
    }

    /// <summary>Wing armchair (0.9 x 0.9 x 1.15). The ottoman, when requested, sits in front of the footprint.</summary>
    static GameObject Armchair(float cx, float y0, float cz, Vector3 facing, bool ottoman = false, int style = 0)
    {
        Fx();
        var g = Group("Sillon");
        var f = new Frame(cx, y0, cz, facing);
        Surf fab = style == 1 ? fDamaskRed : style == 2 ? fVelvetBlue : fLeather;
        Surf pip = C(Gold, 0.5f);
        float h = 1.15f;
        for (int i = 0; i < 4; i++)
        {
            float lx = (i % 2 == 0 ? -1f : 1f) * 0.36f, lz = (i < 2 ? -1f : 1f) * 0.36f;
            FP(g, f, "Pata", lx, 0f, lz, 0.06f, 0.12f, 0.06f, sDark, false);
            if (i >= 2) FBall(g, f, "PataBola", lx, 0.06f, lz, 0.08f, MatOf(sDark));
        }
        FP(g, f, "Asiento", 0f, 0.12f, 0.03f, 0.78f, 0.28f, 0.8f, fab);
        FP(g, f, "Cojin", 0f, 0.40f, 0.08f, 0.5f, 0.1f, 0.6f, fab);
        FB(g, f, "Ribete", 0f, 0.5f, 0.38f, 0.5f, 0.014f, 0.014f, pip, false);
        for (int s = -1; s <= 1; s += 2)
        {
            FP(g, f, "Brazo", s * 0.32f, 0.40f, -0.05f, 0.14f, 0.22f, 0.7f, fab);
            FEll(g, f, "BrazoRollo", s * 0.32f, 0.63f, 0.25f, 0.16f, 0.14f, 0.18f, MatOf(fab));
            FRot(g, f, "Oreja", s * 0.34f, 0.85f, -0.22f, 0.08f, 0.5f, 0.3f, new Vector3(0f, s * 25f, 0f), fab, true);
        }
        FRot(g, f, "Respaldo", 0f, 0.5f + (h - 0.5f) / 2f, -0.33f, 0.62f, h - 0.5f, 0.14f, new Vector3(-5f, 0f, 0f), fab, true);
        FEll(g, f, "Copete", 0f, h - 0.02f, -0.36f, 0.5f, 0.1f, 0.14f, MatOf(fab));
        for (int bx = 0; bx < 2; bx++)
            for (int by = 0; by < 3; by++)
                FBall(g, f, "Boton", (bx - 0.5f) * 0.26f, 0.62f + by * 0.16f, -0.26f + by * 0.012f, 0.028f, MatOf(pip));
        if (ottoman)
        {
            float oz = 0.45f + 0.3f;
            for (int i = 0; i < 4; i++)
                FP(g, f, "PataOtomana", (i % 2 == 0 ? -1f : 1f) * 0.22f, 0f, oz + (i < 2 ? -1f : 1f) * 0.15f, 0.05f, 0.12f, 0.05f, sDark, false);
            FP(g, f, "Otomana", 0f, 0.12f, oz, 0.55f, 0.25f, 0.42f, fab);
            FP(g, f, "CojinOtomana", 0f, 0.37f, oz, 0.5f, 0.06f, 0.38f, fab, false);
        }
        return g;
    }

    /// <summary>Coffee table with turned legs, lower shelf, a diary and a candle on top.</summary>
    static GameObject CoffeeTable(float cx, float y0, float cz, Vector3 facing, float w, float d, float h, bool glass = false)
    {
        Fx();
        var g = Group("MesaCentro");
        var f = new Frame(cx, y0, cz, facing);
        float hw = w / 2f, hd = d / 2f;
        Surf top = glass ? C(new Color(0.55f, 0.7f, 0.68f), 0.95f) : sWood;
        FP(g, f, "Tapa", 0f, h - 0.03f, 0f, w, 0.03f, d, top);
        Rim4(g, f, "Canto", 0f, h - 0.045f, 0f, w, d, 0.03f, 0.03f, sDark);
        FP(g, f, "Faldon", 0f, h - 0.12f, 0f, w - 0.14f, 0.08f, d - 0.14f, sDark);
        for (int i = 0; i < 4; i++)
            TurnedLeg(g, f, (i % 2 == 0 ? -1f : 1f) * (hw - 0.07f), (i < 2 ? -1f : 1f) * (hd - 0.07f), 0f, h - 0.12f, 0.06f, sDark);
        FP(g, f, "Repisa", 0f, 0.14f, 0f, w - 0.2f, 0.025f, d - 0.2f, sWood, false);
        Rim4(g, f, "Travesano", 0f, 0.13f, 0f, w - 0.14f, d - 0.14f, 0.03f, 0.04f, sDark);
        // decoration
        BookFlat(g, f, hw * 0.35f, h, -hd * 0.1f, 0.22f, 0.035f, 0.3f, new Color(0.3f, 0.12f, 0.08f), 12f);
        FRot(g, f, "Liston", hw * 0.35f, h + 0.037f, -hd * 0.1f, 0.012f, 0.004f, 0.36f, new Vector3(0f, 12f, 0f), C(new Color(0.6f, 0.1f, 0.1f), 0.3f), false);
        CandleStick(g, f, -hw * 0.45f, h, hd * 0.2f, 0.22f);
        BookFlat(g, f, -hw * 0.3f, 0.1525f, 0f, 0.25f, 0.04f, 0.32f, new Color(0.12f, 0.2f, 0.35f), -8f);
        BookFlat(g, f, -hw * 0.3f, 0.1925f, 0f, 0.22f, 0.03f, 0.3f, new Color(0.35f, 0.3f, 0.12f), 5f);
        FEll(g, f, "Platito", hw * 0.1f, h + 0.006f, hd * 0.3f, 0.14f, 0.012f, 0.14f, MatOf(fPorcelain));
        FP(g, f, "Taza", hw * 0.1f, h + 0.01f, hd * 0.3f, 0.07f, 0.06f, 0.07f, fPorcelain, false);
        return g;
    }

    /// <summary>Dining table with runner, plates, glasses, fruit bowl and jug on top.</summary>
    static GameObject DiningTable(float cx, float y0, float cz, Vector3 facing, float w, float d, float h)
    {
        Fx();
        var g = Group("MesaComedor");
        var f = new Frame(cx, y0, cz, facing);
        float hw = w / 2f, hd = d / 2f;
        FP(g, f, "Tapa", 0f, h - 0.04f, 0f, w, 0.04f, d, sWood);
        Rim4(g, f, "Canto", 0f, h - 0.06f, 0f, w + 0.02f, d + 0.02f, 0.03f, 0.03f, sDark);
        FP(g, f, "Faldon", 0f, h - 0.14f, 0f, w - 0.2f, 0.08f, d - 0.2f, sDark);
        for (int i = 0; i < 4; i++)
            TurnedLeg(g, f, (i % 2 == 0 ? -1f : 1f) * (hw - 0.12f), (i < 2 ? -1f : 1f) * (hd - 0.12f), 0f, h - 0.14f, 0.09f, sDark);
        FP(g, f, "Camino", 0f, h, 0f, w * 0.35f, 0.006f, d - 0.1f, fDamaskRed, false);
        Material por = MatOf(fPorcelain);
        FEll(g, f, "Frutero", 0f, h + 0.03f, 0f, 0.3f, 0.06f, 0.3f, por);
        FBall(g, f, "Fruta", -0.05f, h + 0.08f, 0.02f, 0.08f, MatOf(C(new Color(0.6f, 0.1f, 0.1f), 0.5f)));
        FBall(g, f, "Fruta", 0.05f, h + 0.08f, -0.03f, 0.075f, MatOf(C(new Color(0.4f, 0.5f, 0.15f), 0.5f)));
        FBall(g, f, "Fruta", 0.0f, h + 0.12f, 0.03f, 0.07f, MatOf(C(new Color(0.75f, 0.45f, 0.1f), 0.5f)));
        for (int s = -1; s <= 1; s += 2)
        {
            float px = s * (hw - 0.3f);
            FEll(g, f, "Plato", px, h + 0.004f, 0f, 0.26f, 0.008f, 0.26f, por);
            FEll(g, f, "PlatoCentro", px, h + 0.007f, 0f, 0.16f, 0.006f, 0.16f, MatOf(C(new Color(0.8f, 0.82f, 0.8f), 0.6f)));
            FBm(g, f, "Cuchillo", px + 0.17f, h + 0.005f, 0f, 0.015f, 0.004f, 0.2f, mChrome, false);
            FBm(g, f, "Tenedor", px - 0.17f, h + 0.005f, 0f, 0.02f, 0.004f, 0.18f, mChrome, false);
            FPm(g, f, "PieCopa", px + 0.1f, h, -0.2f, 0.012f, 0.07f, 0.012f, MatOf(C(new Color(0.7f, 0.8f, 0.8f), 0.95f)), false);
            FEll(g, f, "Copa", px + 0.1f, h + 0.11f, -0.2f, 0.07f, 0.09f, 0.07f, MatOf(C(new Color(0.7f, 0.8f, 0.8f), 0.95f)));
            FEll(g, f, "Servilleta", px, h + 0.012f, 0.0f, 0.1f, 0.01f, 0.1f, MatOf(fLinen));
        }
        FEll(g, f, "Jarra", hw * 0.15f, h + 0.11f, hd * 0.45f, 0.15f, 0.22f, 0.15f, por);
        FP(g, f, "AsaJarra", hw * 0.15f + 0.09f, h + 0.06f, hd * 0.45f, 0.015f, 0.1f, 0.03f, fPorcelain, false);
        CandleStick(g, f, -hw * 0.15f, h, -hd * 0.45f, 0.2f);
        return g;
    }

    /// <summary>Dining chair: seat at 0.45, slatted back, turned front legs.</summary>
    static GameObject Chair(float cx, float y0, float cz, Vector3 facing)
    {
        Fx();
        var g = Group("Silla");
        var f = new Frame(cx, y0, cz, facing);
        for (int s = -1; s <= 1; s += 2)
        {
            FP(g, f, "PataTrasera", s * 0.19f, 0f, -0.19f, 0.04f, 0.95f, 0.04f, sDark, false);
            FP(g, f, "PataDelantera", s * 0.19f, 0f, 0.19f, 0.04f, 0.43f, 0.04f, sDark, false);
            FBall(g, f, "PataBola", s * 0.19f, 0.2f, 0.19f, 0.055f, MatOf(sDark));
            FBall(g, f, "Remate", s * 0.19f, 0.97f, -0.19f, 0.05f, MatOf(sDark));
        }
        Rim4(g, f, "Faldon", 0f, 0.40f, 0f, 0.42f, 0.42f, 0.03f, 0.05f, sDark);
        FP(g, f, "Asiento", 0f, 0.43f, 0f, 0.44f, 0.03f, 0.44f, sWood);
        FP(g, f, "Cojin", 0f, 0.46f, 0.01f, 0.38f, 0.03f, 0.36f, fDamaskRed, false);
        FP(g, f, "Copete", 0f, 0.86f, -0.19f, 0.42f, 0.08f, 0.04f, sDark);
        FP(g, f, "Travesano", 0f, 0.55f, -0.19f, 0.38f, 0.03f, 0.03f, sDark, false);
        for (int k = -1; k <= 1; k++) FP(g, f, "Barrote", k * 0.11f, 0.58f, -0.19f, 0.04f, 0.28f, 0.02f, sWood, false);
        FEll(g, f, "Talla", 0f, 0.9f, -0.19f, 0.14f, 0.05f, 0.05f, MatOf(sDark));
        return g;
    }

    /// <summary>Three-legged stool with a padded round-ish seat.</summary>
    static GameObject Stool(float cx, float y0, float cz)
    {
        Fx();
        var g = Group("Banquillo");
        var f = new Frame(cx, y0, cz, Vector3.forward);
        FEll(g, f, "Asiento", 0f, 0.42f, 0f, 0.36f, 0.05f, 0.36f, MatOf(sDark));
        FEll(g, f, "Cojin", 0f, 0.45f, 0f, 0.32f, 0.05f, 0.32f, MatOf(fLeather));
        FHit(g, f, "Hitbox", 0f, 0.3f, 0f, 0.34f, 0.46f, 0.34f);
        for (int i = 0; i < 3; i++)
        {
            float az = i * 120f, rad = az * Mathf.Deg2Rad;
            float px = Mathf.Sin(rad) * 0.11f, pz = Mathf.Cos(rad) * 0.11f;
            FRotOff(g, f, "Pata", px, 0.42f, pz, new Vector3(0f, -0.21f, 0f), 0.035f, 0.42f, 0.035f, new Vector3(-10f, az, 0f), sDark, false);
            float am = (az + 60f) * Mathf.Deg2Rad;
            FRot(g, f, "Travesano", Mathf.Sin(am) * 0.1f, 0.18f, Mathf.Cos(am) * 0.1f, 0.2f, 0.02f, 0.02f, new Vector3(0f, az + 60f, 0f), sDark, false);
        }
        return g;
    }

    /// <summary>Console table against a wall: drawer with knobs, tapered legs, stretcher shelf, vase and key dish.</summary>
    static GameObject ConsoleTable(float cx, float y0, float cz, Vector3 facing, float w, float d, float h)
    {
        Fx();
        var g = Group("Consola");
        var f = new Frame(cx, y0, cz, facing);
        float hw = w / 2f, hd = d / 2f;
        FP(g, f, "Tapa", 0f, h - 0.03f, 0f, w, 0.03f, d, sDark);
        Rim4(g, f, "Canto", 0f, h - 0.045f, 0f, w + 0.02f, d + 0.02f, 0.025f, 0.03f, sDark);
        FP(g, f, "Faldon", 0f, h - 0.12f, -0.01f, w - 0.1f, 0.09f, d - 0.08f, sDark);
        float fz = -0.01f + (d - 0.08f) / 2f;
        FP(g, f, "Cajon", 0f, h - 0.11f, fz + 0.006f, w * 0.5f, 0.07f, 0.012f, sWood, false);
        FBall(g, f, "Perilla", -w * 0.12f, h - 0.075f, fz + 0.025f, 0.03f, mBrass);
        FBall(g, f, "Perilla", w * 0.12f, h - 0.075f, fz + 0.025f, 0.03f, mBrass);
        for (int i = 0; i < 4; i++)
            TaperedLeg(g, f, (i % 2 == 0 ? -1f : 1f) * (hw - 0.06f), (i < 2 ? -1f : 1f) * (hd - 0.06f), 0f, h - 0.12f, 0.06f, sDark);
        FP(g, f, "Repisa", 0f, 0.15f, 0f, w - 0.16f, 0.02f, d - 0.16f, sWood, false);
        VaseIn(g, f, -w * 0.25f, h, 0f, 0.9f);
        FEll(g, f, "Platon", w * 0.2f, h + 0.008f, 0.02f, 0.16f, 0.016f, 0.12f, mBrass);
        FBm(g, f, "LlaveVieja", w * 0.2f - 0.02f, h + 0.02f, 0.02f, 0.05f, 0.005f, 0.012f, mBrass, false);
        FBm(g, f, "LlaveVieja", w * 0.2f + 0.02f, h + 0.02f, 0.03f, 0.04f, 0.005f, 0.012f, mBrass, false);
        CandleStick(g, f, w * 0.38f, h, -0.05f, 0.18f);
        return g;
    }

    /// <summary>Stand-alone vase (its own group) on a surface at height y.</summary>
    static GameObject Vase(float cx, float y, float cz)
    {
        Fx();
        var g = Group("Jarron");
        var f = new Frame(cx, y, cz, Vector3.forward);
        VaseIn(g, f, 0f, 0f, 0f, 1f);
        return g;
    }

    // ================================================================== 3. BEDROOM
    /// <summary>Bed. facing points from the head toward the foot. w = width, d = length, h = headboard height.</summary>
    static GameObject Bed(float cx, float y0, float cz, Vector3 facing, float w, float d, float h, int style = 0)
    {
        Fx();
        var g = Group("Cama");
        var f = new Frame(cx, y0, cz, facing);
        float hw = w / 2f, hd = d / 2f;
        Surf quilt = style == 1 ? fQuiltGreen : fQuiltBlue;
        Surf border = C(style == 1 ? new Color(0.45f, 0.35f, 0.15f) : new Color(0.5f, 0.12f, 0.15f), 0.1f);
        Surf sheet = C(new Color(0.93f, 0.93f, 0.9f), 0.05f);
        float footH = 0.75f, top = 0.57f;
        for (int i = 0; i < 4; i++)
        {
            bool head = i < 2;
            float lx = (i % 2 == 0 ? -1f : 1f) * (hw - 0.05f), lz = head ? -hd + 0.05f : hd - 0.05f;
            float ph = head ? h - 0.1f : footH - 0.08f;
            FP(g, f, "Poste", lx, 0f, lz, 0.1f, ph, 0.1f, sDark);
            FP(g, f, "Collar", lx, ph - 0.08f, lz, 0.12f, 0.03f, 0.12f, sDark, false);
            FBall(g, f, "Remate", lx, ph + 0.05f, lz, 0.11f, MatOf(sDark));
        }
        // headboard
        float hbz = -hd + 0.07f;
        FP(g, f, "Cabecera", 0f, 0.35f, hbz, w - 0.2f, h - 0.55f, 0.06f, sDark);
        FP(g, f, "CabeceraPanel", 0f, 0.5f, hbz + 0.035f, w - 0.4f, h - 0.95f, 0.015f, sWood, false);
        FP(g, f, "CabeceraPanelInt", 0f, 0.56f, hbz + 0.045f, w - 0.52f, h - 1.07f, 0.01f, sDark, false);
        FP(g, f, "Cresta", 0f, h - 0.2f, hbz, w - 0.2f, 0.1f, 0.07f, sDark, false);
        FP(g, f, "Cresta", 0f, h - 0.1f, hbz, (w - 0.2f) * 0.6f, 0.07f, 0.07f, sDark, false);
        FP(g, f, "Cresta", 0f, h - 0.03f, hbz, (w - 0.2f) * 0.25f, 0.03f, 0.07f, sDark, false);
        FEll(g, f, "Roseton", 0f, h - 0.15f, hbz + 0.04f, 0.18f, 0.08f, 0.03f, MatOf(sDark));
        // footboard
        float fbz = hd - 0.07f;
        FP(g, f, "Piecera", 0f, 0.3f, fbz, w - 0.2f, footH - 0.4f, 0.05f, sDark);
        FP(g, f, "PieceraPanel", 0f, 0.36f, fbz - 0.03f, w - 0.4f, footH - 0.55f, 0.012f, sWood, false);
        FP(g, f, "PieceraRiel", 0f, footH - 0.1f, fbz, w - 0.2f, 0.05f, 0.08f, sDark, false);
        // rails, skirt, box spring, mattress, bottom sheet
        for (int s = -1; s <= 1; s += 2) FP(g, f, "Larguero", s * (hw - 0.07f), 0.27f, 0f, 0.04f, 0.15f, d - 0.2f, sDark);
        FP(g, f, "Faldon", 0f, 0.08f, 0f, w - 0.18f, 0.2f, d - 0.24f, fLinen, false);
        FP(g, f, "BoxSpring", 0f, 0.27f, 0f, w - 0.16f, 0.13f, d - 0.22f, fLinen);
        FP(g, f, "Colchon", 0f, 0.40f, 0f, w - 0.18f, 0.17f, d - 0.26f, C(new Color(0.9f, 0.9f, 0.86f), 0.1f));
        FP(g, f, "Sabana", 0f, top, 0f, w - 0.16f, 0.012f, d - 0.26f, sheet, false);
        // quilt from below the pillows to the foot, draped over both sides and the foot end
        float qz0 = -hd + 0.95f, qz1 = hd - 0.12f, qc = (qz0 + qz1) / 2f, ql = qz1 - qz0;
        FP(g, f, "Edredon", 0f, top + 0.01f, qc, w - 0.1f, 0.06f, ql, quilt, false);
        for (int s = -1; s <= 1; s += 2)
        {
            FP(g, f, "EdredonLado", s * (hw - 0.062f), 0.3f, qc, 0.025f, top - 0.29f, ql, quilt, false);
            FB(g, f, "EdredonBorde", s * (hw - 0.09f), top + 0.075f, qc, 0.08f, 0.012f, ql, border, false);
        }
        FP(g, f, "EdredonPie", 0f, 0.3f, qz1 + 0.012f, w - 0.1f, top - 0.29f, 0.025f, quilt, false);
        FB(g, f, "EdredonBorde", 0f, top + 0.075f, qz0 + 0.05f, w - 0.1f, 0.012f, 0.1f, border, false);
        FP(g, f, "SabanaDoblada", 0f, top + 0.07f, qz0 + 0.17f, w - 0.12f, 0.03f, 0.34f, sheet, false);
        // pillows, decorative cushions and bolster
        for (int s = -1; s <= 1; s += 2)
        {
            FRot(g, f, "Almohada", s * (w * 0.22f), top + 0.09f, -hd + 0.5f, w * 0.38f, 0.16f, 0.45f, new Vector3(-12f, 0f, 0f), fLinen, false);
            FRot(g, f, "CojinDecorativo", s * (w * 0.2f), top + 0.24f, -hd + 0.68f, 0.38f, 0.38f, 0.1f, new Vector3(-20f, 0f, 0f), style == 1 ? fDamaskGreen : fVelvetBlue, false);
            FB(g, f, "RibeteCojin", s * (w * 0.2f), top + 0.24f + 0.19f, -hd + 0.68f - 0.06f, 0.38f, 0.012f, 0.012f, border, false);
        }
        FEll(g, f, "Rodillo", 0f, top + 0.1f, -hd + 0.24f, w - 0.3f, 0.2f, 0.2f, MatOf(quilt));
        return g;
    }

    /// <summary>Night stand with drawer, knob, lower shelf, a table lamp and a glass on top.</summary>
    static GameObject Nightstand(float cx, float y0, float cz, Vector3 facing, float w, float d, float h)
    {
        Fx();
        var g = Group("Mesita");
        var f = new Frame(cx, y0, cz, facing);
        float hw = w / 2f, hd = d / 2f;
        for (int i = 0; i < 4; i++)
            TurnedLeg(g, f, (i % 2 == 0 ? -1f : 1f) * (hw - 0.05f), (i < 2 ? -1f : 1f) * (hd - 0.05f), 0f, 0.2f, 0.05f, sDark);
        FP(g, f, "Repisa", 0f, 0.2f, 0f, w - 0.04f, 0.025f, d - 0.04f, sWood, false);
        FP(g, f, "Lado", -hw + 0.015f, 0.22f, 0f, 0.03f, h - 0.25f, d - 0.02f, sDark);
        FP(g, f, "Lado", hw - 0.015f, 0.22f, 0f, 0.03f, h - 0.25f, d - 0.02f, sDark);
        FP(g, f, "Fondo", 0f, 0.22f, -hd + 0.01f, w - 0.06f, h - 0.25f, 0.02f, sDark);
        FP(g, f, "Tapa", 0f, h - 0.03f, 0f, w + 0.03f, 0.03f, d + 0.03f, sWood);
        FP(g, f, "Moldura", 0f, h - 0.05f, 0f, w + 0.01f, 0.02f, d + 0.01f, sDark, false);
        FP(g, f, "Cajon", 0f, h - 0.19f, hd - 0.008f, w - 0.07f, 0.13f, 0.016f, sWood);
        FP(g, f, "Sombra", 0f, h - 0.205f, hd - 0.004f, w - 0.07f, 0.012f, 0.006f, C(Color.black, 0.1f), false);
        FBm(g, f, "Placa", 0f, h - 0.125f, hd + 0.004f, 0.06f, 0.03f, 0.006f, mBrass, false);
        FBall(g, f, "Perilla", 0f, h - 0.125f, hd + 0.018f, 0.03f, mBrass);
        FP(g, f, "Puertita", 0f, 0.245f, hd - 0.008f, w - 0.07f, h - 0.46f, 0.016f, sDark);
        FP(g, f, "PanelPuertita", 0f, 0.3f, hd + 0.003f, w - 0.17f, h - 0.57f, 0.008f, sWood, false);
        FBall(g, f, "Perilla", w * 0.3f, h - 0.3f, hd + 0.014f, 0.025f, mBrass);
        TableLampIn(g, f, 0f, h, -0.03f);
        FP(g, f, "Vaso", hw * 0.6f, h, hd * 0.5f, 0.06f, 0.09f, 0.06f, C(new Color(0.75f, 0.85f, 0.85f), 0.9f), false);
        BookFlat(g, f, -hw * 0.55f, h, hd * 0.45f, 0.13f, 0.025f, 0.18f, new Color(0.2f, 0.1f, 0.3f), 20f);
        return g;
    }

    /// <summary>Stand-alone table lamp (its own group) standing on a surface at height y. No Light component.</summary>
    static GameObject TableLamp(float cx, float y, float cz)
    {
        Fx();
        var g = Group("LamparaMesa");
        var f = new Frame(cx, y, cz, Vector3.forward);
        TableLampIn(g, f, 0f, 0f, 0f);
        return g;
    }

    /// <summary>Chest of drawers: four drawers with knobs, mouldings, doily and toiletries. mirror adds ~0.8 m above h.</summary>
    static GameObject Dresser(float cx, float y0, float cz, Vector3 facing, float w, float d, float h, bool mirror = false)
    {
        Fx();
        var g = Group("Comoda");
        var f = new Frame(cx, y0, cz, facing);
        float hw = w / 2f, hd = d / 2f;
        for (int i = 0; i < 4; i++)
            FP(g, f, "Pie", (i % 2 == 0 ? -1f : 1f) * (hw - 0.06f), 0f, (i < 2 ? -1f : 1f) * (hd - 0.06f), 0.09f, 0.07f, 0.09f, sDark, false);
        FP(g, f, "Zocalo", 0f, 0.07f, -0.01f, w - 0.02f, 0.07f, d - 0.04f, sDark);
        FP(g, f, "Cuerpo", 0f, 0.14f, -0.01f, w, h - 0.17f, d - 0.02f, sDark);
        FP(g, f, "Tapa", 0f, h - 0.03f, 0f, w + 0.04f, 0.03f, d + 0.03f, sWood);
        FP(g, f, "Moldura", 0f, h - 0.05f, 0f, w + 0.02f, 0.02f, d + 0.015f, sDark, false);
        float frontZ = -0.01f + (d - 0.02f) / 2f;
        float avail = h - 0.25f, gap = 0.02f, dh = (avail - 3f * gap) / 4f;
        for (int k = 0; k < 4; k++)
        {
            float y = 0.18f + k * (dh + gap);
            FP(g, f, "Cajon", 0f, y, frontZ + 0.008f, w - 0.12f, dh, 0.016f, sWood, false);
            FP(g, f, "Sombra", 0f, y - 0.016f, frontZ + 0.003f, w - 0.12f, 0.012f, 0.006f, C(Color.black, 0.1f), false);
            FP(g, f, "Bisel", 0f, y + 0.025f, frontZ + 0.02f, w - 0.22f, dh - 0.05f, 0.008f, sDark, false);
            for (int s = -1; s <= 1; s += 2)
            {
                FBm(g, f, "Placa", s * w * 0.25f, y + dh / 2f, frontZ + 0.027f, 0.05f, 0.05f, 0.006f, mBrass, false);
                FBall(g, f, "Perilla", s * w * 0.25f, y + dh / 2f, frontZ + 0.04f, 0.032f, mBrass);
            }
        }
        for (int s = -1; s <= 1; s += 2)
            FB(g, f, "PanelLado", s * (hw + 0.004f), 0.14f + (h - 0.17f) / 2f, -0.01f, 0.008f, h - 0.4f, d - 0.22f, sWood, false);
        // top: doily, jewellery box, brush, perfume, candle, framed photo
        FP(g, f, "Carpeta", 0f, h, 0f, w * 0.45f, 0.004f, d * 0.7f, fLace, false);
        FP(g, f, "Joyero", -w * 0.28f, h, 0f, 0.17f, 0.08f, 0.12f, C(new Color(0.4f, 0.08f, 0.1f), 0.4f), false);
        FP(g, f, "JoyeroTapa", -w * 0.28f, h + 0.08f, 0f, 0.175f, 0.015f, 0.125f, sDark, false);
        FBm(g, f, "Broche", -w * 0.28f, h + 0.05f, 0.062f, 0.02f, 0.02f, 0.004f, mBrass, false);
        FRot(g, f, "Cepillo", w * 0.05f, h + 0.012f, hd * 0.3f, 0.06f, 0.025f, 0.1f, new Vector3(0f, 25f, 0f), sDark, false);
        FRotOff(g, f, "MangoCepillo", w * 0.05f, h + 0.01f, hd * 0.3f, new Vector3(0f, 0f, -0.1f), 0.02f, 0.02f, 0.1f, new Vector3(0f, 25f, 0f), sDark, false);
        FP(g, f, "Perfume", w * 0.22f, h, -hd * 0.2f, 0.05f, 0.09f, 0.04f, fGreenGlass, false);
        FBall(g, f, "TapaPerfume", w * 0.22f, h + 0.11f, -hd * 0.2f, 0.035f, mBrass);
        CandleStick(g, f, w * 0.38f, h, 0.02f, 0.16f);
        FRot(g, f, "Portarretrato", -w * 0.08f, h + 0.08f, -hd * 0.45f, 0.13f, 0.16f, 0.01f, new Vector3(-12f, 0f, 0f), C(Gold, 0.6f), false);
        FRot(g, f, "Foto", -w * 0.08f, h + 0.08f, -hd * 0.45f + 0.007f, 0.1f, 0.13f, 0.002f, new Vector3(-12f, 0f, 0f), C(new Color(0.45f, 0.4f, 0.32f), 0.2f), false);
        if (mirror)
        {
            float mz = -hd + 0.06f, mw = w * 0.55f;
            for (int s = -1; s <= 1; s += 2)
            {
                FP(g, f, "PosteEspejo", s * (mw / 2f + 0.03f), h, mz, 0.05f, 0.75f, 0.05f, sDark, false);
                FBall(g, f, "RemateEspejo", s * (mw / 2f + 0.03f), h + 0.78f, mz, 0.06f, MatOf(sDark));
            }
            FP(g, f, "MarcoEspejo", 0f, h + 0.08f, mz, mw, 0.65f, 0.04f, C(Gold, 0.6f), false);
            FP(g, f, "Espejo", 0f, h + 0.11f, mz + 0.025f, mw - 0.08f, 0.59f, 0.01f, fMirror, true);
            FEll(g, f, "CrestaEspejo", 0f, h + 0.76f, mz, 0.3f, 0.09f, 0.05f, MatOf(C(Gold, 0.6f)));
        }
        return g;
    }

    // ================================================================== 4. WARDROBE / CLOSET
    /// <summary>
    /// Two-door wardrobe with a FLAT front (door plane at the facing side; handles protrude at most 3 cm so the
    /// death-scene overlay sits on the doors). ornate = carved pediment, columns and appliques.
    /// </summary>
    static GameObject Wardrobe(float cx, float y0, float cz, Vector3 facing, float w, float d, float h, bool ornate)
    {
        Fx();
        var g = Group(ornate ? "Armario" : "Closet");
        var f = new Frame(cx, y0, cz, facing);
        float hw = w / 2f, hd = d / 2f;
        float baseH = 0.18f, crownH = ornate ? 0.26f : 0.14f, colW = ornate ? 0.1f : 0f;
        for (int i = 0; i < 4; i++)
            FP(g, f, "Pie", (i % 2 == 0 ? -1f : 1f) * (hw - 0.08f), 0f, (i < 2 ? -1f : 1f) * (hd - 0.08f), 0.12f, 0.08f, 0.12f, sDark, false);
        FP(g, f, "Zocalo", 0f, 0.08f, -0.01f, w, baseH - 0.08f, d - 0.02f, sDark);
        FP(g, f, "MolduraZocalo", 0f, baseH - 0.03f, -0.005f, w + 0.02f, 0.03f, d - 0.01f, sDark, false);
        FP(g, f, "Cuerpo", 0f, baseH, -0.03f, w, h - baseH - crownH, d - 0.06f, sDark);
        float usable = w - 2f * colW - 0.04f, doorW = (usable - 0.015f) / 2f;
        float doorH = h - baseH - crownH - 0.04f, doorY = baseH + 0.02f;
        FP(g, f, "Ranura", 0f, doorY, hd - 0.03f, 0.03f, doorH, 0.01f, C(Color.black, 0.1f), false);
        FP(g, f, "Travesano", 0f, baseH, hd - 0.015f, w - 2f * colW, 0.02f, 0.03f, sDark, false);
        FP(g, f, "Travesano", 0f, h - crownH - 0.02f, hd - 0.015f, w - 2f * colW, 0.02f, 0.03f, sDark, false);
        for (int s = -1; s <= 1; s += 2)
        {
            float dx = s * (doorW / 2f + 0.0075f);
            FP(g, f, "Larguero", s * (hw - 0.01f - colW), doorY - 0.02f, hd - 0.015f, 0.02f, doorH + 0.04f, 0.03f, sDark, false);
            FP(g, f, "Puerta", dx, doorY, hd - 0.025f, doorW, doorH, 0.05f, sDark);
            FP(g, f, "PanelPuerta", dx, doorY + doorH * 0.36f, hd + 0.004f, doorW - 0.14f, doorH * 0.58f, 0.008f, sWood, false);
            FP(g, f, "BiselPuerta", dx, doorY + doorH * 0.39f, hd + 0.009f, doorW - 0.2f, doorH * 0.52f, 0.004f, sDark, false);
            FP(g, f, "PanelPuerta", dx, doorY + 0.06f, hd + 0.004f, doorW - 0.14f, doorH * 0.24f, 0.008f, sWood, false);
            FP(g, f, "BiselPuerta", dx, doorY + 0.09f, hd + 0.009f, doorW - 0.2f, doorH * 0.24f - 0.06f, 0.004f, sDark, false);
            FBm(g, f, "Placa", s * 0.07f, doorY + doorH * 0.42f, hd + 0.004f, 0.03f, 0.1f, 0.008f, mBrass, false);
            FBall(g, f, "Manija", s * 0.07f, doorY + doorH * 0.42f, hd + 0.016f, 0.03f, mBrass);
            float hx = s * (doorW + 0.0075f - 0.012f);
            FBm(g, f, "Bisagra", hx, doorY + 0.1f, hd + 0.004f, 0.012f, 0.08f, 0.008f, mBrass, false);
            FBm(g, f, "Bisagra", hx, doorY + doorH / 2f, hd + 0.004f, 0.012f, 0.08f, 0.008f, mBrass, false);
            FBm(g, f, "Bisagra", hx, doorY + doorH - 0.1f, hd + 0.004f, 0.012f, 0.08f, 0.008f, mBrass, false);
            if (ornate) FEll(g, f, "Talla", dx, doorY + doorH * 0.7f, hd + 0.012f, 0.22f, 0.11f, 0.02f, MatOf(sWood));
        }
        FBm(g, f, "Bocallave", 0.07f, doorY + doorH * 0.42f - 0.09f, hd + 0.004f, 0.025f, 0.05f, 0.006f, mBrass, false);
        FB(g, f, "Ojo", 0.07f, doorY + doorH * 0.42f - 0.09f, hd + 0.008f, 0.008f, 0.016f, 0.004f, C(Color.black, 0.1f), false);
        // crown kept flush with the door plane
        FP(g, f, "Cornisa", 0f, h - crownH, -0.005f, w + 0.04f, 0.06f, d - 0.01f, sDark, false);
        FP(g, f, "Cornisa", 0f, h - crownH + 0.06f, -0.01f, w + 0.08f, 0.05f, d - 0.02f, sDark, false);
        FP(g, f, "Cornisa", 0f, h - crownH + 0.11f, -0.02f, w + 0.02f, (ornate ? 0.03f : crownH - 0.11f), d - 0.04f, sDark, false);
        if (ornate)
        {
            for (int s = -1; s <= 1; s += 2)
            {
                float colx = s * (hw - 0.02f - colW / 2f), colz = hd - 0.05f;
                FP(g, f, "ColumnaBase", colx, baseH, colz, 0.1f, 0.1f, 0.1f, sDark, false);
                FP(g, f, "Columna", colx, baseH + 0.1f, colz, 0.07f, h - baseH - crownH - 0.22f, 0.07f, sWood, false);
                FP(g, f, "Estria", colx - 0.015f, baseH + 0.15f, colz + 0.036f, 0.006f, h - baseH - crownH - 0.32f, 0.004f, sDark, false);
                FP(g, f, "Estria", colx + 0.015f, baseH + 0.15f, colz + 0.036f, 0.006f, h - baseH - crownH - 0.32f, 0.004f, sDark, false);
                FP(g, f, "ColumnaCapitel", colx, h - crownH - 0.12f, colz, 0.1f, 0.1f, 0.1f, sDark, false);
            }
            FP(g, f, "Fronton", 0f, h - 0.12f, -0.04f, w * 0.7f, 0.07f, d * 0.5f, sDark, false);
            FP(g, f, "Fronton", 0f, h - 0.05f, -0.04f, w * 0.45f, 0.03f, d * 0.4f, sDark, false);
            FEll(g, f, "Concha", 0f, h - 0.07f, -0.04f + d * 0.25f, 0.26f, 0.1f, 0.04f, MatOf(sWood));
            FBall(g, f, "Remate", 0f, h - 0.03f, -0.04f, 0.06f, MatOf(sDark));
            FBall(g, f, "Remate", -w * 0.4f, h - 0.03f, -0.04f, 0.05f, MatOf(sDark));
            FBall(g, f, "Remate", w * 0.4f, h - 0.03f, -0.04f, 0.05f, MatOf(sDark));
        }
        return g;
    }

    // ================================================================== 5. KITCHEN
    /// <summary>Two-door fridge/freezer with a flat front (handles protrude 3 cm at most).</summary>
    static GameObject Fridge(float cx, float y0, float cz, Vector3 facing, float w, float d, float h)
    {
        Fx();
        var g = Group("Refrigerador");
        var f = new Frame(cx, y0, cz, facing);
        float hw = w / 2f, hd = d / 2f;
        Surf body = C(new Color(0.88f, 0.89f, 0.88f), 0.55f);
        Surf edge = C(new Color(0.6f, 0.6f, 0.6f), 0.4f);
        FP(g, f, "Zocalo", 0f, 0f, -0.03f, w - 0.1f, 0.1f, d - 0.12f, fPlasticGrey);
        FP(g, f, "Rejilla", 0f, 0.02f, hd - 0.07f, w - 0.3f, 0.06f, 0.01f, C(Color.black, 0.2f), false);
        for (int k = 0; k < 5; k++) FP(g, f, "Lama", 0f, 0.025f + k * 0.012f, hd - 0.064f, w - 0.32f, 0.004f, 0.004f, fPlasticGrey, false);
        FP(g, f, "Cuerpo", 0f, 0.1f, -0.02f, w, h - 0.1f, d - 0.04f, body);
        float split = 0.1f + (h - 0.1f) * 0.7f;
        float lowY = 0.12f, lowH = split - 0.13f, upY = split + 0.01f, upH = h - split - 0.03f;
        FP(g, f, "PuertaRefri", 0f, lowY, hd - 0.02f, w - 0.02f, lowH, 0.04f, body);
        FP(g, f, "PuertaCongelador", 0f, upY, hd - 0.02f, w - 0.02f, upH, 0.04f, body);
        FP(g, f, "Ranura", 0f, split - 0.012f, hd - 0.01f, w - 0.02f, 0.024f, 0.012f, C(Color.black, 0.1f), false);
        RimV4(g, f, "BordePuerta", 0f, lowY + lowH / 2f, hd + 0.001f, w - 0.02f, lowH, 0.006f, 0.002f, edge);
        RimV4(g, f, "BordePuerta", 0f, upY + upH / 2f, hd + 0.001f, w - 0.02f, upH, 0.006f, 0.002f, edge);
        // vertical bar handles on the left edge, hinges on the right
        float hx = -hw + 0.1f;
        FBm(g, f, "Manija", hx, lowY + lowH * 0.72f, hd + 0.02f, 0.025f, lowH * 0.45f, 0.02f, mChrome, false);
        FBm(g, f, "SoporteManija", hx, lowY + lowH * 0.72f + lowH * 0.2f, hd + 0.006f, 0.02f, 0.03f, 0.012f, mChrome, false);
        FBm(g, f, "SoporteManija", hx, lowY + lowH * 0.72f - lowH * 0.2f, hd + 0.006f, 0.02f, 0.03f, 0.012f, mChrome, false);
        FBm(g, f, "Manija", hx, upY + upH * 0.5f, hd + 0.02f, 0.025f, upH * 0.6f, 0.02f, mChrome, false);
        FBm(g, f, "SoporteManija", hx, upY + upH * 0.5f + upH * 0.26f, hd + 0.006f, 0.02f, 0.03f, 0.012f, mChrome, false);
        FBm(g, f, "SoporteManija", hx, upY + upH * 0.5f - upH * 0.26f, hd + 0.006f, 0.02f, 0.03f, 0.012f, mChrome, false);
        FBm(g, f, "Bisagra", hw - 0.04f, lowY - 0.01f, hd - 0.02f, 0.06f, 0.02f, 0.05f, mChrome, false);
        FBm(g, f, "Bisagra", hw - 0.04f, split, hd - 0.02f, 0.06f, 0.02f, 0.05f, mChrome, false);
        FBm(g, f, "Bisagra", hw - 0.04f, h - 0.01f, hd - 0.02f, 0.06f, 0.02f, 0.05f, mChrome, false);
        // badge, nameplate, a note held by a magnet
        FEll(g, f, "Emblema", 0f, upY + upH * 0.5f, hd + 0.003f, 0.1f, 0.03f, 0.005f, mChrome);
        FBm(g, f, "Placa", 0f, lowY + lowH * 0.9f, hd + 0.003f, 0.14f, 0.025f, 0.005f, mChrome, false);
        FP(g, f, "Nota", w * 0.15f, lowY + lowH * 0.55f, hd + 0.001f, 0.08f, 0.1f, 0.002f, fPaper, false);
        FBall(g, f, "Iman", w * 0.15f, lowY + lowH * 0.55f + 0.045f, hd + 0.004f, 0.02f, MatOf(C(new Color(0.7f, 0.1f, 0.1f), 0.5f)));
        // condenser grille at the back, things on top
        FP(g, f, "Condensador", 0f, 0.15f, -hd - 0.005f, w - 0.2f, h - 0.4f, 0.02f, C(new Color(0.1f, 0.1f, 0.1f), 0.2f), false);
        FP(g, f, "CajaCarton", -w * 0.2f, h, -0.05f, 0.25f, 0.18f, 0.2f, C(new Color(0.6f, 0.45f, 0.25f), 0.1f), false);
        FP(g, f, "Botella", w * 0.25f, h, 0f, 0.07f, 0.22f, 0.07f, fGreenGlass, false);
        FPm(g, f, "Tapon", w * 0.25f, h + 0.22f, 0f, 0.03f, 0.03f, 0.03f, mBrass, false);
        return g;
    }

    /// <summary>
    /// Straight kitchen run (about 5 m): base cabinets with panelled doors and a drawer row, stone countertop,
    /// tiled backsplash, a hollow stainless sink with a gooseneck faucet, and a 4-burner range with oven at the
    /// end on the viewer's right (local -x). Small items on top.
    /// </summary>
    static GameObject KitchenCounter(float cx, float y0, float cz, Vector3 facing, float w, float d, float h)
    {
        Fx();
        var g = Group("Mostrador");
        var f = new Frame(cx, y0, cz, facing);
        float hw = w / 2f, hd = d / 2f, cth = 0.04f;
        float stoveW = 0.76f, sx0 = -hw, sx1 = -hw + stoveW;
        float cab0 = sx1 + 0.02f, runW = hw - cab0, runC = (cab0 + hw) / 2f;
        FP(g, f, "Zocalo", runC, 0f, -0.03f, runW, 0.1f, d - 0.1f, fPlasticGrey);
        FP(g, f, "Carcasa", runC, 0.1f, -0.015f, runW, h - cth - 0.1f, d - 0.03f, sWood);
        // sections
        float secW = 0.5f;
        int n = Mathf.Max(1, Mathf.FloorToInt(runW / secW));
        int idxS = n / 2;
        float sinkX = cab0 + (idxS + 0.5f) * secW;
        // countertop in four slabs around the sink opening (0.56 x 0.42 at z = -0.02)
        float bz = -0.02f, bw = 0.56f, bd = 0.42f, bdep = 0.19f;
        float oL = sinkX - bw / 2f - 0.01f, oR = sinkX + bw / 2f + 0.01f;
        FP(g, f, "Encimera", (cab0 - 0.02f + oL) / 2f, h - cth, 0f, oL - (cab0 - 0.02f), cth, d + 0.02f, fGranite);
        FP(g, f, "Encimera", (oR + hw + 0.02f) / 2f, h - cth, 0f, hw + 0.02f - oR, cth, d + 0.02f, fGranite);
        float zb0 = -hd - 0.01f, zb1 = bz - bd / 2f - 0.01f, zf0 = bz + bd / 2f + 0.01f, zf1 = hd + 0.01f;
        FP(g, f, "Encimera", sinkX, h - cth, (zb0 + zb1) / 2f, oR - oL, cth, zb1 - zb0, fGranite);
        FP(g, f, "Encimera", sinkX, h - cth, (zf0 + zf1) / 2f, oR - oL, cth, zf1 - zf0, fGranite);
        FB(g, f, "Canto", runC, h - cth / 2f, hd + 0.02f, runW + 0.04f, cth + 0.01f, 0.02f, fGranite, false);
        // sink basin: four walls, floor, drain, a little dark water
        FBm(g, f, "Tarja", sinkX, h - bdep / 2f, bz + bd / 2f - 0.006f, bw, bdep, 0.012f, mSteel);
        FBm(g, f, "Tarja", sinkX, h - bdep / 2f, bz - bd / 2f + 0.006f, bw, bdep, 0.012f, mSteel);
        FBm(g, f, "Tarja", sinkX - bw / 2f + 0.006f, h - bdep / 2f, bz, 0.012f, bdep, bd, mSteel);
        FBm(g, f, "Tarja", sinkX + bw / 2f - 0.006f, h - bdep / 2f, bz, 0.012f, bdep, bd, mSteel);
        FBm(g, f, "FondoTarja", sinkX, h - bdep + 0.006f, bz, bw - 0.02f, 0.012f, bd - 0.02f, mSteel);
        FEll(g, f, "Desague", sinkX, h - bdep + 0.013f, bz, 0.08f, 0.006f, 0.08f, MatOf(C(new Color(0.05f, 0.05f, 0.05f), 0.5f)));
        FEll(g, f, "AguaSucia", sinkX + 0.08f, h - bdep + 0.014f, bz + 0.05f, 0.22f, 0.004f, 0.15f, MatOf(C(new Color(0.1f, 0.12f, 0.1f), 0.9f)));
        Rim4m(g, f, "BordeTarja", sinkX, h + 0.004f, bz, bw + 0.03f, bd + 0.03f, 0.02f, 0.008f, mSteel);
        // gooseneck faucet with two cross handles
        float fz0 = bz - bd / 2f - 0.07f;
        FPm(g, f, "BaseGrifo", sinkX, h, fz0, 0.06f, 0.03f, 0.06f, mChrome, false);
        FBall(g, f, "BaseGrifoBola", sinkX, h + 0.04f, fz0, 0.05f, mChrome);
        FPm(g, f, "TuboGrifo", sinkX, h + 0.04f, fz0, 0.025f, 0.25f, 0.025f, mChrome, false);
        FRotm(g, f, "CuelloGrifo", sinkX, h + 0.29f + 0.049f, fz0 + 0.034f, 0.025f, 0.12f, 0.025f, new Vector3(35f, 0f, 0f), mChrome, false);
        float yB = h + 0.29f + 0.12f * Mathf.Cos(35f * Mathf.Deg2Rad), zB = fz0 + 0.12f * Mathf.Sin(35f * Mathf.Deg2Rad);
        FBm(g, f, "CuelloGrifo", sinkX, yB, zB + 0.06f, 0.025f, 0.025f, 0.12f, mChrome, false);
        FRotm(g, f, "CuelloGrifo", sinkX, yB - 0.047f, zB + 0.12f + 0.017f, 0.025f, 0.1f, 0.025f, new Vector3(-20f, 0f, 0f), mChrome, false);
        FBall(g, f, "PicoGrifo", sinkX, yB - 0.094f, zB + 0.12f + 0.034f, 0.032f, mChrome);
        for (int s = -1; s <= 1; s += 2)
        {
            float tx = sinkX + s * 0.1f;
            FPm(g, f, "Llave", tx, h, fz0, 0.03f, 0.05f, 0.03f, mChrome, false);
            FBm(g, f, "Cruz", tx, h + 0.06f, fz0, 0.08f, 0.012f, 0.012f, mChrome, false);
            FBm(g, f, "Cruz", tx, h + 0.06f, fz0, 0.012f, 0.012f, 0.08f, mChrome, false);
            FBall(g, f, "Indicador", tx, h + 0.072f, fz0, 0.016f, MatOf(C(s < 0 ? new Color(0.8f, 0.1f, 0.1f) : new Color(0.1f, 0.3f, 0.8f), 0.6f)));
        }
        // backsplash
        FP(g, f, "Salpicadero", 0f, h, -hd + 0.01f, w, 0.5f, 0.02f, fTileSplash, false);
        FP(g, f, "RemateSalpicadero", 0f, h + 0.5f, -hd + 0.015f, w, 0.02f, 0.03f, fGranite, false);
        // doors and drawers
        float drawerH = 0.15f, doorY = 0.12f, doorTop = h - cth - 0.04f - drawerH, doorH = doorTop - doorY - 0.02f;
        float fr = hd - 0.03f;
        for (int i = 0; i < n; i++)
        {
            float xc = cab0 + (i + 0.5f) * secW;
            FP(g, f, "PuertaGabinete", xc, doorY, fr + 0.009f, secW - 0.03f, doorH, 0.018f, sWood, false);
            FP(g, f, "PanelGabinete", xc, doorY + 0.07f, fr + 0.02f, secW - 0.15f, doorH - 0.14f, 0.006f, sDark, false);
            FBall(g, f, "Perilla", xc + (i % 2 == 0 ? 1f : -1f) * (secW / 2f - 0.06f), doorY + doorH - 0.08f, hd, 0.025f, mBrass);
            FP(g, f, "Cajon", xc, doorTop, fr + 0.009f, secW - 0.03f, drawerH, 0.018f, sWood, false);
            if (i != idxS) FBm(g, f, "Tirador", xc, doorTop + drawerH / 2f, hd + 0.004f, 0.16f, 0.012f, 0.014f, mChrome, false);
        }
        float left = hw - (cab0 + n * secW);
        if (left > 0.03f) FP(g, f, "Relleno", hw - left / 2f, doorY, fr + 0.006f, left - 0.01f, doorTop + drawerH - doorY, 0.012f, sWood, false);
        // range with oven at the -x end
        float sc = (sx0 + sx1) / 2f;
        FP(g, f, "ZocaloEstufa", sc, 0f, -0.03f, stoveW - 0.1f, 0.1f, d - 0.1f, fPlasticGrey);
        FP(g, f, "Estufa", sc, 0.1f, -0.015f, stoveW - 0.02f, h - 0.13f, d - 0.03f, fWhiteEnamel);
        FP(g, f, "Parrilla", sc, h - 0.03f, -0.015f, stoveW, 0.03f, d - 0.03f, fBlackLacquer);
        Material grate = MatOf(fIron);
        for (int b = 0; b < 4; b++)
        {
            float bxp = sc + (b % 2 == 0 ? -0.17f : 0.17f), bzp = -0.02f + (b < 2 ? -0.13f : 0.13f);
            FEll(g, f, "Aro", bxp, h + 0.004f, bzp, 0.2f, 0.006f, 0.2f, MatOf(C(new Color(0.2f, 0.2f, 0.2f), 0.3f)));
            FEll(g, f, "Quemador", bxp, h + 0.008f, bzp, 0.11f, 0.012f, 0.11f, grate);
            FB(g, f, "Rejilla", bxp, h + 0.014f, bzp, 0.24f, 0.012f, 0.025f, fIron, false);
            FB(g, f, "Rejilla", bxp, h + 0.014f, bzp, 0.025f, 0.012f, 0.24f, fIron, false);
        }
        FP(g, f, "RespaldoEstufa", sc, h, -hd + 0.02f, stoveW, 0.14f, 0.05f, fWhiteEnamel, false);
        FPm(g, f, "RelojEstufa", sc, h + 0.05f, -hd + 0.046f, 0.12f, 0.03f, 0.004f, EmitMat(new Color(0.2f, 1f, 0.3f), 0.5f), false);
        for (int k = 0; k < 5; k++) FBm(g, f, "PerillaEstufa", sc - 0.28f + k * 0.14f, h - 0.09f, hd - 0.003f, 0.035f, 0.035f, 0.025f, mChrome, false);
        FP(g, f, "PuertaHorno", sc, 0.22f, hd - 0.007f, stoveW - 0.08f, 0.42f, 0.016f, fWhiteEnamel);
        FP(g, f, "VentanaHorno", sc, 0.3f, hd + 0.002f, stoveW - 0.3f, 0.22f, 0.006f, fGlassDark, false);
        FBm(g, f, "ManijaHorno", sc, 0.6f, hd + 0.03f, stoveW - 0.16f, 0.025f, 0.025f, mChrome, false);
        FBm(g, f, "SoporteHorno", sc - 0.25f, 0.6f, hd + 0.012f, 0.02f, 0.02f, 0.02f, mChrome, false);
        FBm(g, f, "SoporteHorno", sc + 0.25f, 0.6f, hd + 0.012f, 0.02f, 0.02f, 0.02f, mChrome, false);
        FP(g, f, "CajonHorno", sc, 0.11f, hd - 0.009f, stoveW - 0.08f, 0.09f, 0.012f, fWhiteEnamel, false);
        FBm(g, f, "ManijaCajon", sc, 0.155f, hd + 0.01f, 0.3f, 0.02f, 0.02f, mChrome, false);
        FP(g, f, "Trapo", sc - 0.15f, 0.3f, hd + 0.05f, 0.22f, 0.31f, 0.012f, fTowelBlue, false);
        FB(g, f, "TrapoDoblez", sc - 0.15f, 0.615f, hd + 0.03f, 0.22f, 0.03f, 0.06f, fTowelBlue, false);
        // kettle on a back burner
        FEll(g, f, "Tetera", sc - 0.17f, h + 0.1f, -0.15f, 0.2f, 0.17f, 0.2f, mSteel);
        FBall(g, f, "TapaTetera", sc - 0.17f, h + 0.19f, -0.15f, 0.045f, grate);
        FRotm(g, f, "PicoTetera", sc - 0.17f - 0.12f, h + 0.13f, -0.15f, 0.1f, 0.03f, 0.03f, new Vector3(0f, 0f, 35f), mSteel, false);
        FB(g, f, "AsaTetera", sc - 0.17f, h + 0.23f, -0.15f, 0.12f, 0.02f, 0.02f, fIron, false);
        FB(g, f, "AsaTetera", sc - 0.17f - 0.05f, h + 0.2f, -0.15f, 0.015f, 0.05f, 0.015f, fIron, false);
        FB(g, f, "AsaTetera", sc - 0.17f + 0.05f, h + 0.2f, -0.15f, 0.015f, 0.05f, 0.015f, fIron, false);
        // small items along the counter
        FP(g, f, "Tabla", cab0 + 0.45f, h, 0.02f, 0.35f, 0.02f, 0.25f, sWood, false);
        FBm(g, f, "Cuchillo", cab0 + 0.45f - 0.04f, h + 0.022f, -0.04f, 0.18f, 0.004f, 0.025f, mChrome, false);
        FB(g, f, "MangoCuchillo", cab0 + 0.45f + 0.1f, h + 0.026f, -0.04f, 0.09f, 0.014f, 0.025f, sDark, false);
        for (int k = 0; k < 3; k++)
        {
            float jx = hw - 0.22f - k * 0.13f, jh = 0.16f - k * 0.02f;
            FP(g, f, "Frasco", jx, h, -0.17f, 0.1f, jh, 0.1f, C(new Color(0.62f, 0.55f, 0.4f), 0.6f), false);
            FP(g, f, "TapaFrasco", jx, h + jh, -0.17f, 0.1f, 0.02f, 0.1f, sDark, false);
            FP(g, f, "Etiqueta", jx, h + jh * 0.35f, -0.17f + 0.05f, 0.07f, 0.05f, 0.003f, fPaper, false);
        }
        FP(g, f, "Jabon", sinkX + 0.4f, h, -0.22f, 0.06f, 0.14f, 0.06f, fGreenGlass, false);
        FPm(g, f, "Bomba", sinkX + 0.4f, h + 0.14f, -0.22f, 0.015f, 0.05f, 0.015f, mChrome, false);
        FBm(g, f, "CabezaBomba", sinkX + 0.4f, h + 0.195f, -0.2f, 0.02f, 0.015f, 0.05f, mChrome, false);
        FP(g, f, "Toalla", sinkX - 0.5f, h, 0.1f, 0.25f, 0.03f, 0.18f, fTowel, false);
        FP(g, f, "Toalla", sinkX - 0.5f, h + 0.03f, 0.1f, 0.23f, 0.025f, 0.16f, fTowel, false);
        return g;
    }

    /// <summary>Tall pantry cabinet: panelled lower doors, glazed (open-frame) upper doors showing shelves of cans and jars.</summary>
    static GameObject PantryCabinet(float cx, float y0, float cz, Vector3 facing, float w, float d, float h)
    {
        Fx();
        var g = Group("Alacena");
        var f = new Frame(cx, y0, cz, facing);
        float hw = w / 2f, hd = d / 2f;
        for (int i = 0; i < 4; i++)
            FP(g, f, "Pie", (i % 2 == 0 ? -1f : 1f) * (hw - 0.07f), 0f, (i < 2 ? -1f : 1f) * (hd - 0.07f), 0.1f, 0.07f, 0.1f, sDark, false);
        FP(g, f, "Zocalo", 0f, 0.07f, -0.01f, w, 0.1f, d - 0.02f, sDark);
        float split = 0.17f + (h - 0.3f) * 0.42f;
        // lower cabinet: solid carcass and two panelled doors
        FP(g, f, "CuerpoBajo", 0f, 0.17f, -0.02f, w, split - 0.17f, d - 0.04f, sDark);
        float ldH = split - 0.17f - 0.04f, ldW = (w - 0.06f - 0.015f) / 2f;
        for (int s = -1; s <= 1; s += 2)
        {
            float dx = s * (ldW / 2f + 0.0075f);
            FP(g, f, "Puerta", dx, 0.19f, hd - 0.012f, ldW, ldH, 0.024f, sDark);
            FP(g, f, "PanelPuerta", dx, 0.19f + 0.06f, hd + 0.004f, ldW - 0.12f, ldH - 0.12f, 0.008f, sWood, false);
            FBall(g, f, "Perilla", s * 0.06f, 0.19f + ldH * 0.6f, hd + 0.016f, 0.03f, mBrass);
            FBm(g, f, "Bisagra", s * (ldW + 0.0075f - 0.012f), 0.19f + 0.08f, hd + 0.003f, 0.012f, 0.07f, 0.006f, mBrass, false);
            FBm(g, f, "Bisagra", s * (ldW + 0.0075f - 0.012f), 0.19f + ldH - 0.08f, hd + 0.003f, 0.012f, 0.07f, 0.006f, mBrass, false);
        }
        FP(g, f, "Cintura", 0f, split, 0f, w + 0.03f, 0.04f, d + 0.02f, sDark, false);
        // upper cabinet: sides, back, top, shelves with goods, open-frame glazed doors
        float uy = split + 0.04f, uh = h - 0.12f - uy;
        FP(g, f, "LadoAlto", -hw + 0.02f, uy, -0.01f, 0.04f, uh, d - 0.02f, sDark);
        FP(g, f, "LadoAlto", hw - 0.02f, uy, -0.01f, 0.04f, uh, d - 0.02f, sDark);
        FP(g, f, "FondoAlto", 0f, uy, -hd + 0.015f, w - 0.08f, uh, 0.03f, sDark);
        FP(g, f, "InteriorAlto", 0f, uy, -hd + 0.035f, w - 0.1f, uh, 0.005f, C(new Color(0.1f, 0.07f, 0.05f), 0.1f), false);
        FP(g, f, "TechoAlto", 0f, uy + uh, -0.01f, w, 0.04f, d - 0.02f, sDark);
        Material tin = MatOf(fSteel);
        for (int k = 0; k < 2; k++)
        {
            float sy = uy + 0.04f + k * (uh - 0.08f) / 2f;
            FP(g, f, "Repisa", 0f, sy, -0.03f, w - 0.08f, 0.025f, d - 0.12f, sWood, false);
            for (int c = 0; c < 5; c++)
            {
                float cxp = -hw + 0.14f + c * ((w - 0.28f) / 4f);
                if (k == 0 && c % 2 == 1)
                {
                    FP(g, f, "Frasco", cxp, sy + 0.025f, -0.06f, 0.09f, 0.14f, 0.09f, C(new Color(0.5f, 0.42f, 0.25f), 0.6f), false);
                    FP(g, f, "TapaFrasco", cxp, sy + 0.165f, -0.06f, 0.09f, 0.02f, 0.09f, C(new Color(0.5f, 0.1f, 0.1f), 0.3f), false);
                }
                else
                {
                    FPm(g, f, "Lata", cxp, sy + 0.025f, -0.06f, 0.075f, 0.11f, 0.075f, tin, false);
                    FP(g, f, "EtiquetaLata", cxp, sy + 0.05f, -0.06f + 0.038f, 0.06f, 0.06f, 0.003f, C(c % 3 == 0 ? new Color(0.7f, 0.15f, 0.1f) : c % 3 == 1 ? new Color(0.2f, 0.4f, 0.2f) : new Color(0.75f, 0.65f, 0.3f), 0.1f), false);
                }
            }
        }
        float udW = (w - 0.08f - 0.015f) / 2f;
        for (int s = -1; s <= 1; s += 2)
        {
            float dx = s * (udW / 2f + 0.0075f);
            RimV4(g, f, "MarcoVitrina", dx, uy + uh / 2f, hd - 0.012f, udW, uh, 0.05f, 0.024f, sDark);
            FB(g, f, "Parteluz", dx, uy + uh / 2f, hd - 0.012f, 0.02f, uh - 0.1f, 0.015f, sDark, false);
            FB(g, f, "Parteluz", dx, uy + uh / 2f, hd - 0.012f, udW - 0.1f, 0.02f, 0.015f, sDark, false);
            FBall(g, f, "Perilla", s * 0.06f, uy + uh * 0.45f, hd + 0.012f, 0.025f, mBrass);
        }
        FP(g, f, "Cornisa", 0f, h - 0.08f, -0.005f, w + 0.04f, 0.05f, d - 0.01f, sDark, false);
        FP(g, f, "Cornisa", 0f, h - 0.03f, -0.02f, w + 0.02f, 0.03f, d - 0.04f, sDark, false);
        return g;
    }

    /// <summary>Range hood: tapered steel body, filter, two lamps and a chimney of height hChimney above it.</summary>
    static GameObject RangeHood(float cx, float yBottom, float cz, Vector3 facing, float w, float hChimney = 1.3f)
    {
        Fx();
        var g = Group("Campana");
        var f = new Frame(cx, yBottom, cz, facing);
        FP(g, f, "Campana", 0f, 0f, 0f, w, 0.12f, 0.5f, fSteel);
        FP(g, f, "CampanaAlta", 0f, 0.12f, -0.05f, w - 0.2f, 0.1f, 0.4f, fSteel, false);
        FP(g, f, "Chimenea", 0f, 0.22f, -0.1f, 0.32f, hChimney, 0.3f, fSteel, false);
        FP(g, f, "Filtro", 0f, -0.004f, 0f, w - 0.1f, 0.008f, 0.4f, C(new Color(0.3f, 0.3f, 0.3f), 0.3f), false);
        Material lamp = EmitMat(new Color(1f, 0.9f, 0.7f), 1.0f);
        FPm(g, f, "Foco", -w * 0.3f, -0.008f, 0.1f, 0.1f, 0.008f, 0.05f, lamp, false);
        FPm(g, f, "Foco", w * 0.3f, -0.008f, 0.1f, 0.1f, 0.008f, 0.05f, lamp, false);
        for (int k = 0; k < 3; k++) FBm(g, f, "Boton", -0.1f + k * 0.1f, 0.03f, 0.252f, 0.03f, 0.02f, 0.006f, mChrome, false);
        FB(g, f, "Canto", 0f, 0.0f, 0.25f, w, 0.02f, 0.01f, fSteel, false);
        return g;
    }

    /// <summary>Hanging pot rack (rail along world x at height y): chains, frame, hooks, pans, pot, ladle and colander.</summary>
    static GameObject HangingPots(float cx, float y, float cz, float w)
    {
        Fx();
        var g = Group("Ollas");
        var f = new Frame(cx, y, cz, Vector3.forward);
        Material copper = Metal(new Color(0.72f, 0.42f, 0.25f), 0.7f, 0.7f);
        Material iron = MatOf(fIron);
        FP(g, f, "Cadena", -w / 2f + 0.1f, 0f, 0f, 0.012f, 0.6f, 0.012f, fIron, false);
        FP(g, f, "Cadena", w / 2f - 0.1f, 0f, 0f, 0.012f, 0.6f, 0.012f, fIron, false);
        Rim4m(g, f, "Barra", 0f, 0f, 0f, w, 0.36f, 0.025f, 0.025f, iron);
        FBm(g, f, "Barra", 0f, 0f, 0f, w - 0.05f, 0.02f, 0.02f, iron, false);
        int n = Mathf.Max(3, Mathf.FloorToInt(w / 0.3f));
        for (int i = 0; i < n; i++)
        {
            float x = -w / 2f + 0.15f + i * ((w - 0.3f) / Mathf.Max(1, n - 1));
            float z = (i % 2 == 0) ? 0.17f : -0.17f;
            FBm(g, f, "Gancho", x, -0.05f, z, 0.01f, 0.08f, 0.01f, mChrome, false);
            switch (i % 4)
            {
                case 0:
                    FEll(g, f, "Sarten", x, -0.3f, z, 0.26f, 0.26f, 0.05f, copper);
                    FBm(g, f, "MangoSarten", x, -0.13f, z, 0.03f, 0.14f, 0.012f, iron, false);
                    break;
                case 1:
                    FEll(g, f, "Olla", x, -0.25f, z, 0.2f, 0.2f, 0.2f, iron);
                    FBm(g, f, "AsaOlla", x, -0.12f, z, 0.08f, 0.06f, 0.012f, iron, false);
                    break;
                case 2:
                    FBm(g, f, "Cucharon", x, -0.22f, z, 0.012f, 0.26f, 0.012f, mChrome, false);
                    FBall(g, f, "CucharonCabeza", x, -0.37f, z, 0.07f, mChrome);
                    break;
                default:
                    FEll(g, f, "Colador", x, -0.26f, z, 0.22f, 0.22f, 0.1f, mSteel);
                    FBm(g, f, "AsaColador", x, -0.12f, z, 0.06f, 0.06f, 0.012f, mSteel, false);
                    break;
            }
        }
        return g;
    }

    // ================================================================== 6. BATHROOM
    /// <summary>Toilet (0.4 x 0.72 x 0.83): pedestal, bowl, rim, seat and lid, tank with lid, flush lever, bolts, supply pipe.</summary>
    static GameObject Toilet(float cx, float y0, float cz, Vector3 facing)
    {
        Fx();
        var g = Group("Inodoro");
        var f = new Frame(cx, y0, cz, facing);
        Material por = MatOf(fPorcelain);
        Material seat = MatOf(C(new Color(0.95f, 0.95f, 0.93f), 0.6f));
        FP(g, f, "PiePedestal", 0f, 0f, 0f, 0.3f, 0.04f, 0.42f, fPorcelain, false);
        FP(g, f, "Pedestal", 0f, 0f, 0f, 0.24f, 0.3f, 0.36f, fPorcelain);
        FEll(g, f, "Taza", 0f, 0.32f, 0.06f, 0.38f, 0.2f, 0.5f, por);
        FEll(g, f, "Borde", 0f, 0.405f, 0.06f, 0.4f, 0.035f, 0.52f, por);
        FEll(g, f, "Asiento", 0f, 0.425f, 0.06f, 0.38f, 0.03f, 0.48f, seat);
        FEll(g, f, "Tapa", 0f, 0.45f, 0.06f, 0.4f, 0.03f, 0.5f, seat);
        FBm(g, f, "BisagraAsiento", -0.07f, 0.44f, -0.17f, 0.03f, 0.02f, 0.03f, mChrome, false);
        FBm(g, f, "BisagraAsiento", 0.07f, 0.44f, -0.17f, 0.03f, 0.02f, 0.03f, mChrome, false);
        FP(g, f, "Tanque", 0f, 0.42f, -0.27f, 0.42f, 0.38f, 0.18f, fPorcelain);
        FP(g, f, "TapaTanque", 0f, 0.80f, -0.27f, 0.44f, 0.03f, 0.2f, fPorcelain, false);
        FBm(g, f, "PalancaBase", -0.215f, 0.74f, -0.27f, 0.012f, 0.03f, 0.03f, mChrome, false);
        FBm(g, f, "Palanca", -0.225f, 0.74f, -0.22f, 0.012f, 0.015f, 0.08f, mChrome, false);
        FBall(g, f, "Tornillo", -0.09f, 0.045f, 0.12f, 0.03f, por);
        FBall(g, f, "Tornillo", 0.09f, 0.045f, 0.12f, 0.03f, por);
        FPm(g, f, "Tubo", 0.17f, 0f, -0.33f, 0.015f, 0.42f, 0.015f, mChrome, false);
        FBall(g, f, "Valvula", 0.17f, 0.2f, -0.33f, 0.04f, mChrome);
        FHit(g, f, "Cuerpo", 0f, 0.26f, 0.06f, 0.4f, 0.4f, 0.5f);
        return g;
    }

    /// <summary>Pedestal sink (0.6 x 0.5 x 0.85): hollow basin, faucet with two cross taps, drain, overflow, soap.</summary>
    static GameObject PedestalSink(float cx, float y0, float cz, Vector3 facing)
    {
        Fx();
        var g = Group("Lavabo");
        var f = new Frame(cx, y0, cz, facing);
        FP(g, f, "PiePedestal", 0f, 0f, -0.05f, 0.28f, 0.03f, 0.3f, fPorcelain, false);
        FP(g, f, "Pedestal", 0f, 0f, -0.05f, 0.2f, 0.3f, 0.22f, fPorcelain);
        FP(g, f, "Pedestal", 0f, 0.3f, -0.05f, 0.24f, 0.3f, 0.26f, fPorcelain);
        FP(g, f, "Pedestal", 0f, 0.6f, -0.05f, 0.3f, 0.13f, 0.32f, fPorcelain);
        FP(g, f, "LavaboFondo", 0f, 0.71f, 0f, 0.56f, 0.03f, 0.41f, fPorcelain);
        FP(g, f, "LavaboPared", 0f, 0.71f, 0.21f, 0.6f, 0.14f, 0.03f, fPorcelain);
        FP(g, f, "Plataforma", 0f, 0.71f, -0.19f, 0.6f, 0.14f, 0.07f, fPorcelain);
        FP(g, f, "LavaboPared", -0.285f, 0.71f, 0f, 0.03f, 0.14f, 0.45f, fPorcelain);
        FP(g, f, "LavaboPared", 0.285f, 0.71f, 0f, 0.03f, 0.14f, 0.45f, fPorcelain);
        Rim4(g, f, "BordeLavabo", 0f, 0.857f, 0f, 0.62f, 0.47f, 0.035f, 0.015f, fPorcelain);
        FEll(g, f, "Mancha", 0f, 0.742f, 0.02f, 0.3f, 0.004f, 0.2f, MatOf(C(new Color(0.25f, 0.25f, 0.22f), 0.5f)));
        FEll(g, f, "Desague", 0f, 0.745f, 0.02f, 0.05f, 0.006f, 0.05f, mChrome);
        FB(g, f, "Rebosadero", 0f, 0.8f, -0.153f, 0.03f, 0.015f, 0.004f, C(Color.black, 0.2f), false);
        FPm(g, f, "Grifo", 0f, 0.85f, -0.19f, 0.035f, 0.08f, 0.035f, mChrome, false);
        FBm(g, f, "CanoGrifo", 0f, 0.93f, -0.14f, 0.03f, 0.025f, 0.12f, mChrome, false);
        FBm(g, f, "PicoGrifo", 0f, 0.905f, -0.08f, 0.03f, 0.03f, 0.03f, mChrome, false);
        for (int s = -1; s <= 1; s += 2)
        {
            FPm(g, f, "Llave", s * 0.1f, 0.85f, -0.19f, 0.025f, 0.04f, 0.025f, mChrome, false);
            FBm(g, f, "Cruz", s * 0.1f, 0.895f, -0.19f, 0.07f, 0.01f, 0.01f, mChrome, false);
            FBm(g, f, "Cruz", s * 0.1f, 0.895f, -0.19f, 0.01f, 0.01f, 0.07f, mChrome, false);
            FBall(g, f, "Indicador", s * 0.1f, 0.905f, -0.19f, 0.015f, MatOf(C(s < 0 ? new Color(0.8f, 0.1f, 0.1f) : new Color(0.1f, 0.3f, 0.8f), 0.6f)));
        }
        FP(g, f, "Jabon", 0.2f, 0.865f, -0.19f, 0.07f, 0.025f, 0.05f, C(new Color(0.9f, 0.6f, 0.6f), 0.4f), false);
        return g;
    }

    /// <summary>Wall mirror cabinet. (cx, cy, cz) is the centre ON the wall surface; the cabinet projects 0.12 toward facing.</summary>
    static GameObject MirrorCabinet(float cx, float cy, float cz, Vector3 facing, float w, float h)
    {
        Fx();
        var g = Group("Botiquin");
        var f = new Frame(cx, cy, cz, facing);
        FB(g, f, "Cuerpo", 0f, 0f, 0.06f, w, h, 0.12f, fWhiteEnamel);
        FB(g, f, "Espejo", 0f, 0f, 0.125f, w - 0.06f, h - 0.06f, 0.01f, fMirror, true);
        RimV4m(g, f, "MarcoEspejo", 0f, 0f, 0.125f, w - 0.02f, h - 0.02f, 0.02f, 0.012f, mChrome);
        FBm(g, f, "Perilla", w / 2f - 0.05f, -h / 2f + 0.1f, 0.14f, 0.02f, 0.04f, 0.012f, mChrome, false);
        FB(g, f, "Repisa", 0f, -h / 2f - 0.08f, 0.07f, w, 0.012f, 0.14f, C(new Color(0.7f, 0.75f, 0.75f), 0.9f), false);
        FBm(g, f, "Mensula", -w / 2f + 0.05f, -h / 2f - 0.095f, 0.03f, 0.02f, 0.02f, 0.06f, mChrome, false);
        FBm(g, f, "Mensula", w / 2f - 0.05f, -h / 2f - 0.095f, 0.03f, 0.02f, 0.02f, 0.06f, mChrome, false);
        FP(g, f, "Vaso", -w * 0.3f, -h / 2f - 0.074f, 0.07f, 0.06f, 0.09f, 0.06f, C(new Color(0.75f, 0.85f, 0.85f), 0.9f), false);
        FRot(g, f, "Cepillo", -w * 0.3f - 0.01f, -h / 2f + 0.03f, 0.07f, 0.012f, 0.18f, 0.012f, new Vector3(0f, 0f, 12f), C(new Color(0.2f, 0.5f, 0.8f), 0.5f), false);
        FRot(g, f, "Cepillo", -w * 0.3f + 0.012f, -h / 2f + 0.03f, 0.07f, 0.012f, 0.18f, 0.012f, new Vector3(0f, 0f, -10f), C(new Color(0.8f, 0.2f, 0.2f), 0.5f), false);
        FBm(g, f, "Luminaria", 0f, h / 2f + 0.06f, 0.06f, w * 0.7f, 0.05f, 0.1f, mChrome, false);
        Material bulb = EmitMat(new Color(1f, 0.9f, 0.75f), 1.2f);
        for (int k = -1; k <= 1; k++) FBall(g, f, "Foco", k * w * 0.22f, h / 2f + 0.03f, 0.1f, 0.05f, bulb);
        return g;
    }

    /// <summary>Chrome towel bar on the wall with two folded towels. (cx, cy, cz) is the centre ON the wall.</summary>
    static GameObject TowelRack(float cx, float cy, float cz, Vector3 facing, float w)
    {
        Fx();
        var g = Group("Toallero");
        var f = new Frame(cx, cy, cz, facing);
        for (int s = -1; s <= 1; s += 2)
        {
            FBm(g, f, "Brida", s * (w / 2f - 0.03f), 0f, 0.005f, 0.05f, 0.05f, 0.01f, mChrome, false);
            FBm(g, f, "Soporte", s * (w / 2f - 0.03f), 0f, 0.045f, 0.03f, 0.03f, 0.07f, mChrome, false);
        }
        FBm(g, f, "Barra", 0f, 0f, 0.085f, w, 0.022f, 0.022f, mChrome, false);
        float tx = -w * 0.22f;
        FB(g, f, "Toalla", tx, -0.21f, 0.11f, 0.34f, 0.42f, 0.04f, fTowel, false);
        FB(g, f, "Toalla", tx, -0.2f, 0.06f, 0.34f, 0.4f, 0.035f, fTowel, false);
        FB(g, f, "Toalla", tx, 0.005f, 0.085f, 0.34f, 0.03f, 0.09f, fTowel, false);
        tx = w * 0.22f;
        FB(g, f, "Toalla", tx, -0.21f, 0.11f, 0.34f, 0.42f, 0.04f, fTowelBlue, false);
        FB(g, f, "Toalla", tx, -0.2f, 0.06f, 0.34f, 0.4f, 0.035f, fTowelBlue, false);
        FB(g, f, "Toalla", tx, 0.005f, 0.085f, 0.34f, 0.03f, 0.09f, fTowelBlue, false);
        FB(g, f, "Franja", tx, -0.33f, 0.131f, 0.34f, 0.03f, 0.002f, C(new Color(0.9f, 0.9f, 0.88f), 0.1f), false);
        return g;
    }

    /// <summary>Bath mat on the floor with a border and a pair of slippers at one edge.</summary>
    static GameObject BathMat(float cx, float y0, float cz, float w, float d)
    {
        Fx();
        var g = Group("Tapete");
        var f = new Frame(cx, y0, cz, Vector3.forward);
        FP(g, f, "Tapete", 0f, 0.004f, 0f, w, 0.02f, d, fTowel, false);
        Rim4(g, f, "BordeTapete", 0f, 0.02f, 0f, w, d, 0.03f, 0.012f, fTowelBlue);
        Material slip = MatOf(C(new Color(0.45f, 0.1f, 0.12f), 0.2f));
        FEll(g, f, "Pantufla", -0.08f, 0.04f, d / 2f - 0.16f, 0.1f, 0.05f, 0.26f, slip);
        FEll(g, f, "Pantufla", 0.08f, 0.04f, d / 2f - 0.15f, 0.1f, 0.05f, 0.26f, slip);
        return g;
    }

    /// <summary>
    /// Real hollow bathtub. facing = the open long side (where you step in); w = length, d = width, h = rim height.
    /// Shell of four 7 cm walls with a rolled rim, inner floor well below the rim, sloped ends, drain with chain plug,
    /// two cross taps and a spout at the -x end, optional shower riser, claw feet or tiled apron, optional murky water.
    /// </summary>
    static GameObject Bathtub(float cx, float y0, float cz, Vector3 facing, float w, float d, float h, bool clawfoot = true, bool shower = true, bool water = true)
    {
        Fx();
        var g = Group("Banera");
        var f = new Frame(cx, y0, cz, facing);
        float hw = w / 2f, hd = d / 2f;
        float wall = 0.07f, bottom = clawfoot ? 0.12f : 0.02f, rimH = 0.04f, wallH = h - rimH - bottom;
        Material por = MatOf(fPorcelain);
        Surf outer = clawfoot ? C(new Color(0.12f, 0.3f, 0.28f), 0.6f) : fPorcelain;
        FP(g, f, "Fondo", 0f, bottom - 0.02f, 0f, w - 0.04f, 0.02f, d - 0.04f, outer);
        FP(g, f, "PisoBanera", 0f, bottom, 0f, w - 2f * wall, 0.05f, d - 2f * wall, fPorcelain);
        FP(g, f, "ParedBanera", 0f, bottom, -hd + wall / 2f, w, wallH, wall, fPorcelain);
        FP(g, f, "ParedBanera", 0f, bottom, hd - wall / 2f, w, wallH, wall, fPorcelain);
        FP(g, f, "ParedBanera", -hw + wall / 2f, bottom, 0f, wall, wallH, d - 2f * wall, fPorcelain);
        FP(g, f, "ParedBanera", hw - wall / 2f, bottom, 0f, wall, wallH, d - 2f * wall, fPorcelain);
        if (clawfoot)
        {
            FP(g, f, "Exterior", 0f, bottom, -hd - 0.006f, w + 0.024f, wallH, 0.012f, outer, false);
            FP(g, f, "Exterior", 0f, bottom, hd + 0.006f, w + 0.024f, wallH, 0.012f, outer, false);
            FP(g, f, "Exterior", -hw - 0.006f, bottom, 0f, 0.012f, wallH, d, outer, false);
            FP(g, f, "Exterior", hw + 0.006f, bottom, 0f, 0.012f, wallH, d, outer, false);
        }
        // rolled rim (two stepped layers), collides
        Rim4(g, f, "Borde", 0f, h - rimH + 0.015f, 0f, w + 0.06f, d + 0.06f, wall + 0.06f, 0.03f, fPorcelain, true);
        Rim4(g, f, "Borde", 0f, h - 0.005f, 0f, w + 0.02f, d + 0.02f, wall + 0.02f, 0.01f, fPorcelain);
        // sloped ends and floor fillets approximate the curved interior
        for (int s = -1; s <= 1; s += 2)
            FRot(g, f, "Respaldo", s * (hw - wall - 0.12f), bottom + 0.05f + wallH * 0.45f, 0f, 0.03f, wallH * 0.95f, d - 2f * wall - 0.02f, new Vector3(0f, 0f, -s * 22f), fPorcelain, false);
        FRot(g, f, "Filete", 0f, bottom + 0.07f, -hd + wall + 0.02f, w - 2f * wall - 0.3f, 0.04f, 0.04f, new Vector3(45f, 0f, 0f), fPorcelain, false);
        FRot(g, f, "Filete", 0f, bottom + 0.07f, hd - wall - 0.02f, w - 2f * wall - 0.3f, 0.04f, 0.04f, new Vector3(45f, 0f, 0f), fPorcelain, false);
        // drain, plug on a chain, overflow plate
        float dxp = -hw + wall + 0.3f;
        FEll(g, f, "Desague", dxp, bottom + 0.053f, 0f, 0.09f, 0.008f, 0.09f, MatOf(C(new Color(0.05f, 0.05f, 0.05f), 0.5f)));
        FBall(g, f, "Tapon", dxp + 0.1f, bottom + 0.075f, 0.05f, 0.05f, mChrome);
        FRotOff(g, f, "Cadena", dxp + 0.1f, bottom + 0.09f, 0.05f, new Vector3(0f, 0.17f, 0f), 0.006f, 0.34f, 0.006f, new Vector3(-10f, 0f, -35f), fIron, false);
        FEll(g, f, "Rebosadero", -hw + wall + 0.006f, bottom + wallH * 0.8f, 0f, 0.012f, 0.08f, 0.08f, mChrome);
        FBall(g, f, "RebosaderoOjo", -hw + wall + 0.012f, bottom + wallH * 0.8f, 0f, 0.03f, MatOf(C(Color.black, 0.2f)));
        // taps and spout on the rim at the -x end
        float fx = -hw + 0.1f;
        for (int s = -1; s <= 1; s += 2)
        {
            FPm(g, f, "Llave", fx, h, s * 0.12f, 0.03f, 0.05f, 0.03f, mChrome, false);
            FBm(g, f, "Cruz", fx, h + 0.06f, s * 0.12f, 0.08f, 0.012f, 0.012f, mChrome, false);
            FBm(g, f, "Cruz", fx, h + 0.06f, s * 0.12f, 0.012f, 0.012f, 0.08f, mChrome, false);
            FBall(g, f, "Indicador", fx, h + 0.072f, s * 0.12f, 0.018f, MatOf(C(s < 0 ? new Color(0.8f, 0.1f, 0.1f) : new Color(0.1f, 0.3f, 0.8f), 0.6f)));
        }
        FPm(g, f, "BaseCano", fx, h, 0f, 0.05f, 0.08f, 0.05f, mChrome, false);
        FBm(g, f, "Cano", fx + 0.09f, h + 0.08f, 0f, 0.18f, 0.03f, 0.03f, mChrome, false);
        FBm(g, f, "PicoCano", fx + 0.17f, h + 0.055f, 0f, 0.03f, 0.05f, 0.03f, mChrome, false);
        if (shower)
        {
            float rx = -hw + 0.08f, rz = -hd + 0.12f;
            FPm(g, f, "TuboRegadera", rx, h, rz, 0.025f, 1.9f - h, 0.025f, mChrome, false);
            FBm(g, f, "Abrazadera", rx, 1.2f, rz - 0.03f, 0.04f, 0.03f, 0.04f, mChrome, false);
            FBm(g, f, "BrazoRegadera", rx + 0.17f, 1.9f, rz, 0.34f, 0.025f, 0.025f, mChrome, false);
            FBall(g, f, "Rotula", rx + 0.34f, 1.9f, rz, 0.04f, mChrome);
            FRotm(g, f, "Regadera", rx + 0.36f, 1.86f, rz + 0.03f, 0.16f, 0.03f, 0.16f, new Vector3(20f, 0f, 0f), mChrome, false);
        }
        if (clawfoot)
        {
            for (int i = 0; i < 4; i++)
            {
                float px = (i % 2 == 0 ? -1f : 1f) * (hw - 0.18f), pz = (i < 2 ? -1f : 1f) * (hd - 0.14f);
                FBall(g, f, "Pata", px, 0.065f, pz, 0.12f, mBrass);
                FPm(g, f, "Garra", px, 0f, pz, 0.1f, 0.03f, 0.13f, mBrass, false);
            }
        }
        else
        {
            FP(g, f, "Faldon", 0f, 0f, hd - 0.015f, w, h - rimH, 0.03f, fTileSplash);
            FP(g, f, "Faldon", -hw + 0.015f, 0f, 0f, 0.03f, h - rimH, d, fTileSplash);
            FP(g, f, "Faldon", hw - 0.015f, 0f, 0f, 0.03f, h - rimH, d, fTileSplash);
        }
        if (water) FP(g, f, "Agua", 0f, bottom + 0.051f, 0f, w - 2f * wall - 0.02f, 0.14f, d - 2f * wall - 0.02f, C(new Color(0.08f, 0.13f, 0.13f), 0.9f), false);
        FEll(g, f, "Jabonera", hw - 0.3f, h + 0.01f, -hd + 0.02f, 0.12f, 0.02f, 0.09f, por);
        FP(g, f, "Jabon", hw - 0.3f, h + 0.02f, -hd + 0.02f, 0.07f, 0.025f, 0.05f, C(new Color(0.9f, 0.6f, 0.6f), 0.4f), false);
        return g;
    }

    /// <summary>
    /// Hotel-style sliding shower curtain along the open long side of a tub with the same (cx, y0, cz, facing, w, d, h)
    /// as Bathtub: chrome rail at 1.9 m with end flanges and a ceiling support, 14 ring hooks and 14 pleated strips
    /// (two shades, rippled depth) with header and weighted hem. Group "Cortina" carries a SlidingCurtain component
    /// (closed at start) and a "Hitbox" child. gatherSide = +1 bunches the strips at local +x, -1 at local -x.
    /// </summary>
    static GameObject ShowerCurtain(float cx, float y0, float cz, Vector3 facing, float w, float d, float h, int gatherSide = 1)
    {
        Fx();
        var g = Group("Cortina");
        var f = new Frame(cx, y0, cz, facing);
        float hw = w / 2f, hd = d / 2f;
        float railY = 1.9f, zc = hd + 0.035f;
        FBm(g, f, "Riel", 0f, railY, zc, w + 0.1f, 0.03f, 0.03f, mChrome, false);
        for (int s = -1; s <= 1; s += 2)
        {
            FBm(g, f, "Brida", s * (hw + 0.06f), railY, zc, 0.02f, 0.1f, 0.1f, mChrome, false);
            FBall(g, f, "Remate", s * (hw + 0.075f), railY, zc, 0.045f, mChrome);
        }
        FPm(g, f, "SoporteTecho", 0f, railY + 0.015f, zc, 0.015f, 1.15f, 0.015f, mChrome, false);
        int n = 14;
        float sw = w / n;
        float stripTop = railY - 0.06f, stripBottom = 0.15f, stripH = stripTop - stripBottom;
        Surf ca = S(TexGen.Curtain("ducha_a", new Color(0.85f, 0.88f, 0.86f), new Color(0.72f, 0.78f, 0.76f)), 0.4f, 0.35f);
        Surf cb = S(TexGen.Curtain("ducha_b", new Color(0.78f, 0.82f, 0.8f), new Color(0.65f, 0.7f, 0.7f)), 0.4f, 0.35f);
        Surf hem = C(new Color(0.6f, 0.65f, 0.64f), 0.4f);
        float gatherW = 0.36f;
        int side = gatherSide >= 0 ? 1 : -1;
        float gx = side * (hw - gatherW / 2f);
        bool railX = Mathf.Abs(f.right.x) > 0.5f;
        float kx = (gatherW / n) / (sw + 0.004f);
        Vector3 bunch = railX ? new Vector3(kx, 1f, 2.5f) : new Vector3(2.5f, 1f, kx);
        var strips = new List<Transform>();
        var cPos = new List<Vector3>(); var oPos = new List<Vector3>(); var cScl = new List<Vector3>(); var oScl = new List<Vector3>();
        for (int i = 0; i < n; i++)
        {
            float x = -hw + sw / 2f + i * sw;
            float ripple = (i % 2 == 0) ? 0.012f : -0.012f;
            float ox = gx + (i - (n - 1) / 2f) * (gatherW / n);
            float oz = ((i % 3) - 1) * 0.03f;
            Surf cs = (i % 2 == 0) ? ca : cb;
            GameObject[] parts =
            {
                FP(g, f, "Pliegue", x, stripBottom, zc + ripple, sw + 0.004f, stripH, 0.012f, cs, false),
                FP(g, f, "Cabecera", x, stripTop - 0.06f, zc + ripple, sw + 0.004f, 0.06f, 0.016f, hem, false),
                FP(g, f, "Dobladillo", x, stripBottom, zc + ripple, sw + 0.004f, 0.05f, 0.02f, hem, false),
                FEll(g, f, "Anillo", x, railY, zc, 0.05f, 0.05f, 0.012f, mChrome),
                FBm(g, f, "Gancho", x, railY - 0.035f, zc, 0.006f, 0.05f, 0.006f, mChrome, false),
            };
            for (int p = 0; p < parts.Length; p++)
            {
                var t = parts[p].transform;
                bool cloth = p < 3;
                strips.Add(t);
                cPos.Add(t.localPosition);
                oPos.Add(t.localPosition + f.Dir(ox - x, 0f, cloth ? oz : 0f));
                cScl.Add(t.localScale);
                oScl.Add(cloth ? bunch : t.localScale);
            }
            if (i % 2 == 0)
            {
                var wgt = FBm(g, f, "Plomo", x, stripBottom + 0.012f, zc + ripple + 0.012f, 0.03f, 0.02f, 0.006f, mSteel, false);
                strips.Add(wgt.transform);
                cPos.Add(wgt.transform.localPosition);
                oPos.Add(wgt.transform.localPosition + f.Dir(ox - x, 0f, oz));
                cScl.Add(Vector3.one);
                oScl.Add(Vector3.one);
            }
        }
        FHit(g, f, "Hitbox", 0f, (stripTop + stripBottom) / 2f, zc, w, stripH, 0.06f);
        var sc = g.AddComponent<SlidingCurtain>();
        sc.strips = strips.ToArray();
        sc.closedPos = cPos.ToArray(); sc.openPos = oPos.ToArray();
        sc.closedScale = cScl.ToArray(); sc.openScale = oScl.ToArray();
        sc.SetOpen(0f);
        return g;
    }

    // ================================================================== 7. LIBRARY / OFFICE
    /// <summary>
    /// Bookcase with plinth, sides, back, cornice and 5 bays. Each bay holds up to 16 individual books (varied
    /// width/height/colour, some leaning on a bookend); longer shelves finish the row with a textured run of spines.
    /// Books stay 3 cm behind the front plane. One invisible collider covers the whole body.
    /// </summary>
    static GameObject Bookcase(float cx, float y0, float cz, Vector3 facing, float w, float d, float h, bool topDecor = true)
    {
        Fx();
        var g = Group("Librero");
        var f = new Frame(cx, y0, cz, facing);
        float hw = w / 2f, hd = d / 2f;
        FP(g, f, "Zocalo", 0f, 0f, 0f, w, 0.12f, d, sDark);
        FP(g, f, "MolduraBase", 0f, 0.12f, 0f, w + 0.02f, 0.03f, d + 0.01f, sDark, false);
        FP(g, f, "Lado", -hw + 0.02f, 0.12f, 0f, 0.04f, h - 0.12f, d, sDark);
        FP(g, f, "Lado", hw - 0.02f, 0.12f, 0f, 0.04f, h - 0.12f, d, sDark);
        FP(g, f, "Fondo", 0f, 0.12f, -hd + 0.01f, w - 0.08f, h - 0.2f, 0.02f, sDark);
        FP(g, f, "Techo", 0f, h - 0.08f, 0f, w, 0.08f, d, sDark);
        FP(g, f, "Cornisa", 0f, h - 0.06f, 0f, w + 0.04f, 0.06f, d + 0.02f, sDark, false);
        FHit(g, f, "Cuerpo", 0f, h / 2f, 0f, w - 0.08f, h, d);
        const int shelves = 5;
        float pitch = (h - 0.2f) / shelves;
        float usable = w - 0.1f, xStart = -hw + 0.05f;
        float frontZ = hd - 0.03f;
        for (int k = 0; k < shelves; k++)
        {
            float yb = 0.12f + k * pitch;
            if (k > 0) FP(g, f, "Repisa", 0f, yb - 0.025f, 0f, w - 0.08f, 0.025f, d - 0.03f, sWood, false);
            int seed = k * 131 + Mathf.RoundToInt(w * 10f) + Mathf.RoundToInt(cx * 7f + cz * 3f);
            float x = xStart;
            int count = 0;
            while (count < 16 && x < xStart + usable - 0.05f)
            {
                float r1 = Hash01(count, seed), r2 = Hash01(count + 7, seed), r3 = Hash01(count + 13, seed);
                if (r3 > 0.9f && count > 0 && x < xStart + usable - 0.25f)
                {
                    // bookend with a book leaning on it
                    FP(g, f, "Sujetalibros", x + 0.02f, yb, frontZ - 0.1f, 0.03f, 0.13f, 0.12f, fIron, false);
                    FP(g, f, "Sujetalibros", x + 0.06f, yb, frontZ - 0.1f, 0.1f, 0.008f, 0.12f, fIron, false);
                    Color lc = bookPal[(int)(r1 * (bookPal.Length - 0.01f))];
                    FRot(g, f, "LibroInclinado", x + 0.1f, yb + 0.125f, frontZ - 0.09f, 0.03f, 0.24f, 0.17f, new Vector3(0f, 0f, 14f), C(lc, 0.35f), false);
                    x += 0.2f;
                    count += 2;
                    continue;
                }
                float bw = 0.03f + 0.045f * r1;
                float bh = Mathf.Min(pitch - 0.07f, 0.17f + 0.14f * r2);
                float bd = 0.14f + 0.06f * r2;
                Color col = bookPal[(int)(Hash01(count + 3, seed) * (bookPal.Length - 0.01f))];
                float zs = frontZ - 0.012f * Hash01(count + 5, seed);
                FP(g, f, "Libro", x + bw / 2f, yb, zs - bd / 2f, bw, bh, bd, C(col, 0.35f), false);
                if (count % 3 == 0) FB(g, f, "Dorado", x + bw / 2f, yb + bh * 0.8f, zs + 0.002f, bw - 0.008f, 0.012f, 0.004f, C(Gold, 0.6f), false);
                x += bw + 0.004f;
                count++;
            }
            float restL = xStart + usable - x - 0.03f;
            if (restL > 0.15f)
            {
                float rowH = Mathf.Min(0.3f, pitch - 0.07f);
                Surf run = S(TexGen.BookRow("fila" + k), rowH, 0.2f);
                FP(g, f, "Libros", x + 0.015f + restL / 2f, yb, frontZ - 0.1f, restL, rowH, 0.2f, run, false);
            }
        }
        if (topDecor)
        {
            SkullIn(g, f, -w * 0.3f, h, 0f);
            CandleStick(g, f, w * 0.3f, h, 0f, 0.25f);
            BookFlat(g, f, w * 0.05f, h, -0.02f, 0.24f, 0.05f, 0.32f, new Color(0.2f, 0.12f, 0.08f), -15f);
        }
        return g;
    }

    /// <summary>Banker's lamp parts: brass base and stem, tilted green glass shade, bulb glow, pull chain.</summary>
    static void BankerLampIn(GameObject g, Frame f, float lx, float yb, float lz)
    {
        FPm(g, f, "BaseLampara", lx, yb, lz, 0.2f, 0.02f, 0.12f, mBrass, false);
        FPm(g, f, "BaseLampara", lx, yb + 0.02f, lz, 0.1f, 0.02f, 0.07f, mBrass, false);
        FPm(g, f, "Fuste", lx, yb + 0.04f, lz, 0.015f, 0.26f, 0.015f, mBrass, false);
        FBm(g, f, "Brazo", lx, yb + 0.3f, lz + 0.04f, 0.015f, 0.015f, 0.1f, mBrass, false);
        FRotm(g, f, "PantallaVerde", lx, yb + 0.3f, lz + 0.07f, 0.28f, 0.1f, 0.15f, new Vector3(-12f, 0f, 0f), EmitMat(new Color(0.15f, 0.6f, 0.3f), 0.7f), false);
        FRotm(g, f, "TapaPantalla", lx - 0.145f, yb + 0.3f, lz + 0.07f, 0.01f, 0.1f, 0.15f, new Vector3(-12f, 0f, 0f), mBrass, false);
        FRotm(g, f, "TapaPantalla", lx + 0.145f, yb + 0.3f, lz + 0.07f, 0.01f, 0.1f, 0.15f, new Vector3(-12f, 0f, 0f), mBrass, false);
        FBall(g, f, "Foco", lx, yb + 0.265f, lz + 0.08f, 0.04f, mLampShade);
        FP(g, f, "Cadenita", lx + 0.1f, yb + 0.18f, lz + 0.05f, 0.004f, 0.1f, 0.004f, fIron, false);
        FBall(g, f, "Tirador", lx + 0.1f, yb + 0.175f, lz + 0.05f, 0.015f, mBrass);
    }

    /// <summary>Stand-alone banker's lamp on a surface at height y; facing = where the shade opening points.</summary>
    static GameObject BankerLamp(float cx, float y, float cz, Vector3 facing)
    {
        Fx();
        var g = Group("LamparaBanquero");
        var f = new Frame(cx, y, cz, facing);
        BankerLampIn(g, f, 0f, 0f, 0f);
        return g;
    }

    /// <summary>Pedestal desk: leather inlay top, two 3-drawer pedestals, modesty panel; blotter, inkwell, pens, papers, banker's lamp. facing = the sitter's side.</summary>
    static GameObject Desk(float cx, float y0, float cz, Vector3 facing, float w, float d, float h)
    {
        Fx();
        var g = Group("Escritorio");
        var f = new Frame(cx, y0, cz, facing);
        float hw = w / 2f, hd = d / 2f;
        FP(g, f, "Tapa", 0f, h - 0.04f, 0f, w, 0.04f, d, sDark);
        Rim4(g, f, "Canto", 0f, h - 0.055f, 0f, w + 0.02f, d + 0.02f, 0.025f, 0.03f, sDark);
        FP(g, f, "Cuero", 0f, h, 0f, w - 0.3f, 0.004f, d - 0.3f, fLeather, false);
        float dh = (h - 0.18f) / 3f - 0.02f;
        for (int s = -1; s <= 1; s += 2)
        {
            float px = s * (hw - 0.25f);
            FP(g, f, "ZocaloPedestal", px, 0f, 0f, 0.48f, 0.06f, d - 0.08f, sDark, false);
            FP(g, f, "Pedestal", px, 0.06f, 0f, 0.46f, h - 0.1f, d - 0.1f, sDark);
            for (int k = 0; k < 3; k++)
            {
                float y = 0.1f + k * (dh + 0.02f);
                FP(g, f, "Cajon", px, y, hd - 0.05f + 0.008f, 0.38f, dh, 0.016f, sWood, false);
                FP(g, f, "Sombra", px, y - 0.014f, hd - 0.05f + 0.003f, 0.38f, 0.01f, 0.006f, C(Color.black, 0.1f), false);
                FBm(g, f, "Placa", px, y + dh / 2f, hd - 0.05f + 0.018f, 0.05f, 0.04f, 0.004f, mBrass, false);
                FBall(g, f, "Perilla", px, y + dh / 2f, hd - 0.05f + 0.03f, 0.028f, mBrass);
            }
        }
        FP(g, f, "Panel", 0f, 0.3f, -hd + 0.08f, w - 0.98f, h - 0.4f, 0.03f, sDark);
        FP(g, f, "PanelInt", 0f, 0.4f, -hd + 0.1f, w - 1.2f, h - 0.62f, 0.01f, sWood, false);
        // desk top items
        FP(g, f, "Carpeta", 0f, h + 0.004f, 0.08f, 0.5f, 0.008f, 0.35f, C(new Color(0.1f, 0.25f, 0.15f), 0.3f), false);
        for (int i = 0; i < 4; i++)
            FP(g, f, "Esquinera", (i % 2 == 0 ? -1f : 1f) * 0.22f, h + 0.012f, 0.08f + (i < 2 ? -1f : 1f) * 0.15f, 0.06f, 0.004f, 0.06f, fLeather, false);
        FP(g, f, "Tintero", w * 0.25f, h + 0.004f, -hd * 0.45f, 0.06f, 0.05f, 0.06f, fGlassDark, false);
        FBall(g, f, "TapaTintero", w * 0.25f, h + 0.07f, -hd * 0.45f, 0.04f, mBrass);
        FP(g, f, "Lapicero", w * 0.25f + 0.12f, h, -hd * 0.45f, 0.06f, 0.09f, 0.06f, fIron, false);
        FRot(g, f, "Pluma", w * 0.25f + 0.12f, h + 0.14f, -hd * 0.45f, 0.008f, 0.16f, 0.008f, new Vector3(6f, 0f, 10f), C(new Color(0.1f, 0.1f, 0.1f), 0.5f), false);
        FRot(g, f, "Pluma", w * 0.25f + 0.11f, h + 0.14f, -hd * 0.45f, 0.008f, 0.16f, 0.008f, new Vector3(-5f, 0f, -8f), C(new Color(0.5f, 0.1f, 0.1f), 0.5f), false);
        FRot(g, f, "Pluma", w * 0.25f + 0.13f, h + 0.14f, -hd * 0.45f + 0.01f, 0.008f, 0.15f, 0.008f, new Vector3(2f, 0f, 3f), C(Gold, 0.7f), false);
        FRot(g, f, "Papel", -w * 0.22f, h + 0.005f, 0.1f, 0.21f, 0.002f, 0.3f, new Vector3(0f, 0f, 0f), fPaper, false);
        FRot(g, f, "Papel", -w * 0.22f + 0.02f, h + 0.008f, 0.12f, 0.21f, 0.002f, 0.3f, new Vector3(0f, -9f, 0f), fPaper, false);
        FRot(g, f, "Papel", -w * 0.22f - 0.01f, h + 0.011f, 0.09f, 0.21f, 0.002f, 0.3f, new Vector3(0f, 14f, 0f), fPaper, false);
        FBm(g, f, "Abrecartas", w * 0.08f, h + 0.005f, -hd * 0.3f, 0.015f, 0.004f, 0.2f, mChrome, false);
        BankerLampIn(g, f, -w * 0.33f, h, -hd * 0.4f);
        return g;
    }

    /// <summary>Captain's office chair: five-star base with casters, column, leather seat and back, wooden armrests.</summary>
    static GameObject OfficeChair(float cx, float y0, float cz, Vector3 facing)
    {
        Fx();
        var g = Group("SillaOficina");
        var f = new Frame(cx, y0, cz, facing);
        Material iron = MatOf(fIron);
        for (int i = 0; i < 5; i++)
        {
            float az = i * 72f, rad = az * Mathf.Deg2Rad;
            FRotOff(g, f, "BrazoBase", 0f, 0.045f, 0f, new Vector3(0f, 0f, 0.15f), 0.04f, 0.03f, 0.3f, new Vector3(0f, az, 0f), fIron, false);
            FBall(g, f, "Rueda", Mathf.Sin(rad) * 0.29f, 0.03f, Mathf.Cos(rad) * 0.29f, 0.06f, iron);
        }
        FPm(g, f, "Columna", 0f, 0.05f, 0f, 0.06f, 0.35f, 0.06f, mChrome, false);
        FP(g, f, "Mecanismo", 0f, 0.4f, 0f, 0.3f, 0.04f, 0.3f, fIron, false);
        FP(g, f, "Asiento", 0f, 0.44f, 0.02f, 0.52f, 0.09f, 0.5f, fLeather);
        FB(g, f, "Ribete", 0f, 0.53f, 0.27f, 0.5f, 0.012f, 0.012f, C(new Color(0.25f, 0.14f, 0.08f), 0.4f), false);
        FRot(g, f, "Respaldo", 0f, 0.78f, -0.22f, 0.48f, 0.55f, 0.09f, new Vector3(-6f, 0f, 0f), fLeather, true);
        FRot(g, f, "Copete", 0f, 1.06f, -0.245f, 0.5f, 0.06f, 0.1f, new Vector3(-6f, 0f, 0f), sDark, false);
        for (int bx = 0; bx < 2; bx++)
            for (int by = 0; by < 2; by++)
                FBall(g, f, "Boton", (bx - 0.5f) * 0.2f, 0.65f + by * 0.22f, -0.175f + by * 0.023f, 0.025f, MatOf(C(new Color(0.25f, 0.14f, 0.08f), 0.4f)));
        for (int s = -1; s <= 1; s += 2)
        {
            FP(g, f, "Apoyabrazos", s * 0.27f, 0.68f, 0f, 0.06f, 0.04f, 0.4f, sDark, false);
            FP(g, f, "SoporteBrazo", s * 0.27f, 0.5f, 0.12f, 0.04f, 0.18f, 0.04f, sDark, false);
        }
        return g;
    }

    /// <summary>Floor globe on a wooden tripod stand with a horizon ring and brass meridian; the sphere is tilted 23 degrees.</summary>
    static GameObject Globe(float cx, float y0, float cz)
    {
        Fx();
        var g = Group("GloboTerraqueo");
        var f = new Frame(cx, y0, cz, Vector3.forward);
        for (int i = 0; i < 3; i++)
        {
            float az = i * 120f, rad = az * Mathf.Deg2Rad;
            FRotOff(g, f, "Pata", 0f, 0.6f, 0f, new Vector3(0f, -0.3f, 0f), 0.035f, 0.6f, 0.035f, new Vector3(-14f, az, 0f), sDark, false);
            FBall(g, f, "Pie", Mathf.Sin(rad) * 0.15f, 0.03f, Mathf.Cos(rad) * 0.15f, 0.06f, mBrass);
        }
        FP(g, f, "Columna", 0f, 0.6f, 0f, 0.05f, 0.1f, 0.05f, sDark, false);
        FBall(g, f, "Nudo", 0f, 0.62f, 0f, 0.1f, MatOf(sDark));
        float ry = 0.98f;
        for (int k = 0; k < 8; k++)
        {
            float a = k * 45f, rad = a * Mathf.Deg2Rad;
            FRot(g, f, "Horizonte", Mathf.Sin(rad) * 0.3f, ry, Mathf.Cos(rad) * 0.3f, 0.26f, 0.025f, 0.035f, new Vector3(0f, a, 0f), sDark, false);
            FRotm(g, f, "Meridiano", -Mathf.Sin(rad) * 0.235f, ry + Mathf.Cos(rad) * 0.235f, 0f, 0.19f, 0.02f, 0.012f, new Vector3(0f, 0f, a), mBrass, false);
        }
        FP(g, f, "SoporteAro", 0f, 0.7f, 0f, 0.03f, ry - 0.7f - 0.22f, 0.03f, sDark, false);
        FP(g, f, "SoporteAro", 0.15f, ry - 0.02f, 0f, 0.3f, 0.02f, 0.02f, sDark, false);
        var globe = FEll(g, f, "Globo", 0f, ry, 0f, 0.42f, 0.42f, 0.42f, MatOf(S(TexGen.GlobeMap(), 1f, 0.3f)));
        globe.transform.localRotation = f.Rot * Quaternion.Euler(0f, 0f, 23f);
        FBall(g, f, "Eje", 0f, ry + 0.235f, 0f, 0.035f, mBrass);
        FBall(g, f, "Eje", 0f, ry - 0.235f, 0f, 0.035f, mBrass);
        FHit(g, f, "Hitbox", 0f, 0.62f, 0f, 0.5f, 1.24f, 0.5f);
        return g;
    }

    /// <summary>Steel filing cabinet (0.45 x 0.6 x 1.3): three drawers with recessed pulls and label holders, lock, folders on top.</summary>
    static GameObject FileCabinet(float cx, float y0, float cz, Vector3 facing)
    {
        Fx();
        var g = Group("Archivero");
        var f = new Frame(cx, y0, cz, facing);
        Surf st = C(new Color(0.35f, 0.37f, 0.38f), 0.5f);
        FP(g, f, "Zocalo", 0f, 0f, 0f, 0.41f, 0.06f, 0.56f, fPlasticGrey);
        FP(g, f, "Cuerpo", 0f, 0.06f, -0.01f, 0.45f, 1.24f, 0.58f, st);
        float dh = (1.24f - 0.08f) / 3f - 0.02f;
        for (int k = 0; k < 3; k++)
        {
            float y = 0.1f + k * (dh + 0.02f);
            FP(g, f, "Cajon", 0f, y, 0.286f, 0.41f, dh, 0.012f, st, false);
            FP(g, f, "Sombra", 0f, y - 0.015f, 0.283f, 0.41f, 0.012f, 0.006f, C(Color.black, 0.1f), false);
            FB(g, f, "Hueco", 0f, y + dh * 0.5f, 0.288f, 0.13f, 0.04f, 0.004f, C(Color.black, 0.2f), false);
            FBm(g, f, "Tirador", 0f, y + dh * 0.5f + 0.01f, 0.298f, 0.12f, 0.02f, 0.016f, mChrome, false);
            FBm(g, f, "Marco", 0.13f, y + dh * 0.5f, 0.289f, 0.08f, 0.035f, 0.004f, mChrome, false);
            FP(g, f, "Etiqueta", 0.13f, y + dh * 0.5f - 0.0125f, 0.291f, 0.065f, 0.025f, 0.002f, fPaper, false);
        }
        FBall(g, f, "Cerradura", 0.17f, 1.24f, 0.289f, 0.025f, mChrome);
        FP(g, f, "Carpetas", 0f, 1.3f, -0.02f, 0.32f, 0.06f, 0.25f, C(new Color(0.75f, 0.65f, 0.4f), 0.1f), false);
        FP(g, f, "Carpetas", 0.02f, 1.36f, -0.01f, 0.3f, 0.03f, 0.24f, C(new Color(0.6f, 0.5f, 0.3f), 0.1f), false);
        FP(g, f, "Taza", -0.14f, 1.3f, 0.15f, 0.08f, 0.09f, 0.08f, fPorcelain, false);
        FB(g, f, "AsaTaza", -0.085f, 1.345f, 0.15f, 0.015f, 0.05f, 0.03f, fPorcelain, false);
        return g;
    }

    /// <summary>
    /// Floor safe (0.9 x 0.9 x 1.0): heavy steel body, recessed bevelled door, two barrel hinges, spoked handle wheel,
    /// combination dial with ticks, a 3x4 numeric keypad with a small display, brass nameplate, corner bolts and feet.
    /// </summary>
    static GameObject Safe(float cx, float y0, float cz, Vector3 facing, float w, float d, float h)
    {
        Fx();
        var g = Group("CajaFuerte");
        var f = new Frame(cx, y0, cz, facing);
        float hw = w / 2f, hd = d / 2f;
        Surf st = C(new Color(0.25f, 0.26f, 0.29f), 0.6f);
        Surf stLight = C(new Color(0.4f, 0.42f, 0.45f), 0.6f);
        for (int i = 0; i < 4; i++)
            FP(g, f, "Pie", (i % 2 == 0 ? -1f : 1f) * (hw - 0.08f), 0f, (i < 2 ? -1f : 1f) * (hd - 0.08f), 0.1f, 0.05f, 0.1f, fIron, false);
        FP(g, f, "Cuerpo", 0f, 0.05f, -0.02f, w, h - 0.05f, d - 0.04f, st);
        float yc = 0.05f + (h - 0.05f) / 2f;
        RimV4(g, f, "Marco", 0f, yc, hd - 0.02f, w, h - 0.05f, 0.08f, 0.04f, st);
        RimV4(g, f, "Bisel", 0f, yc, hd - 0.012f, w - 0.16f, h - 0.21f, 0.02f, 0.016f, stLight);
        float doorY = 0.15f, doorH = h - 0.25f;
        FP(g, f, "Puerta", 0f, doorY, hd - 0.03f, w - 0.2f, doorH, 0.02f, st);
        FP(g, f, "Sombra", 0f, doorY - 0.012f, hd - 0.03f, w - 0.2f, 0.01f, 0.02f, C(Color.black, 0.1f), false);
        FBm(g, f, "Bisagra", -hw + 0.09f, h * 0.3f, hd - 0.01f, 0.05f, 0.14f, 0.05f, mSteel, false);
        FBm(g, f, "Bisagra", -hw + 0.09f, h * 0.72f, hd - 0.01f, 0.05f, 0.14f, 0.05f, mSteel, false);
        // handle wheel
        float wx = -0.15f, wy = h * 0.5f;
        FBm(g, f, "Eje", wx, wy, hd - 0.015f, 0.03f, 0.03f, 0.04f, mSteel, false);
        FBall(g, f, "Cubo", wx, wy, hd + 0.0f, 0.07f, mSteel);
        for (int k = 0; k < 4; k++)
        {
            float a = k * 45f, rad = a * Mathf.Deg2Rad;
            FRotm(g, f, "Rayo", wx, wy, hd, 0.26f, 0.022f, 0.022f, new Vector3(0f, 0f, a), mSteel, false);
            FBall(g, f, "Pomo", wx + Mathf.Cos(rad) * 0.13f, wy + Mathf.Sin(rad) * 0.13f, hd, 0.035f, mSteel);
            FBall(g, f, "Pomo", wx - Mathf.Cos(rad) * 0.13f, wy - Mathf.Sin(rad) * 0.13f, hd, 0.035f, mSteel);
        }
        // combination dial
        float dx = 0.17f, dy = h * 0.64f;
        FEll(g, f, "Dial", dx, dy, hd - 0.008f, 0.13f, 0.13f, 0.025f, mChrome);
        FBall(g, f, "PerillaDial", dx, dy, hd + 0.008f, 0.045f, mSteel);
        for (int k = 0; k < 12; k++)
        {
            float a = k * 30f * Mathf.Deg2Rad;
            FBm(g, f, "Marca", dx + Mathf.Cos(a) * 0.055f, dy + Mathf.Sin(a) * 0.055f, hd + 0.005f, 0.006f, 0.006f, 0.004f, MatOf(C(Color.black, 0.2f)), false);
        }
        // keypad and display
        float kyb = h * 0.22f;
        FP(g, f, "Teclado", dx, kyb - 0.02f, hd - 0.015f, 0.17f, 0.25f, 0.012f, fPlasticGrey, false);
        for (int r = 0; r < 4; r++)
            for (int c = 0; c < 3; c++)
                FP(g, f, "Tecla", dx + (c - 1) * 0.045f, kyb + (3 - r) * 0.045f, hd - 0.009f, 0.035f, 0.035f, 0.012f, C(new Color(0.85f, 0.85f, 0.8f), 0.3f), false);
        FBm(g, f, "Pantalla", dx, kyb + 0.2f, hd - 0.009f, 0.14f, 0.03f, 0.012f, EmitMat(new Color(0.2f, 1f, 0.3f), 0.4f), false);
        FBm(g, f, "Placa", 0f, h - 0.1f, hd - 0.005f, 0.3f, 0.05f, 0.008f, mBrass, false);
        for (int i = 0; i < 4; i++)
            FBall(g, f, "Perno", (i % 2 == 0 ? -1f : 1f) * (hw - 0.04f), 0.05f + 0.04f + (i < 2 ? 0f : h - 0.13f), hd, 0.025f, mSteel);
        return g;
    }

    // ================================================================== 8. PIANO ROOM
    /// <summary>
    /// Black grand piano in a w x d x h footprint (2.2 x 1.2 x 1.2); facing = keyboard side. Boxed rim with the
    /// curved tail approximated on the -x side, closed lid with brass hinge, 52 white + 36 black keys (A0..C8),
    /// cheek blocks, open fallboard, music desk with two sheets, three tapered legs on casters, pedal lyre.
    /// </summary>
    static GameObject GrandPiano(float cx, float y0, float cz, Vector3 facing, float w, float d, float h)
    {
        Fx();
        var g = Group("Piano");
        var f = new Frame(cx, y0, cz, facing);
        float kx = w / 2.2f, kz = d / 1.2f, ky = h / 1.2f;
        Surf lac = fBlackLacquer;
        float rimB = 0.7f * ky, rimT = 1.0f * ky, rimH = rimT - rimB, lidT = rimT + 0.04f * ky;
        FP(g, f, "Caja", 0f, rimB, 0.075f * kz, 2.2f * kx, rimH, 0.55f * kz, lac);
        FP(g, f, "Caja", 0.1f * kx, rimB, -0.325f * kz, 2.0f * kx, rimH, 0.25f * kz, lac);
        FP(g, f, "Caja", 0.25f * kx, rimB, -0.525f * kz, 1.7f * kx, rimH, 0.15f * kz, lac);
        FP(g, f, "Caja", -0.95f * kx, rimB, -0.25f * kz, 0.1f * kx, rimH, 0.1f * kz, lac, false);
        FP(g, f, "Caja", -0.675f * kx, rimB, -0.475f * kz, 0.15f * kx, rimH, 0.05f * kz, lac, false);
        FP(g, f, "Tapa", 0f, rimT, 0.075f * kz, 2.2f * kx, 0.04f * ky, 0.55f * kz, lac);
        FP(g, f, "Tapa", 0.1f * kx, rimT, -0.325f * kz, 2.0f * kx, 0.04f * ky, 0.25f * kz, lac);
        FP(g, f, "Tapa", 0.25f * kx, rimT, -0.525f * kz, 1.7f * kx, 0.04f * ky, 0.15f * kz, lac);
        FP(g, f, "Tapa", -0.95f * kx, rimT, -0.25f * kz, 0.1f * kx, 0.04f * ky, 0.1f * kz, lac, false);
        FP(g, f, "Tapa", -0.675f * kx, rimT, -0.475f * kz, 0.15f * kx, 0.04f * ky, 0.05f * kz, lac, false);
        FBm(g, f, "Bisagra", 0.25f * kx, lidT + 0.006f, -0.59f * kz, 1.6f * kx, 0.012f, 0.03f, mBrass, false);
        FRot(g, f, "TapaFrontal", 0f, lidT + 0.03f, 0.2f * kz, 2.2f * kx, 0.03f, 0.32f * kz, new Vector3(-10f, 0f, 0f), lac, false);
        // keybed, cheeks, key slip, open fallboard
        float kbW = 1.5f * kx, kbZ0 = 0.35f * kz, kbZ1 = 0.6f * kz, kbZc = (kbZ0 + kbZ1) / 2f, kbD = kbZ1 - kbZ0;
        float keyY = 0.9f * ky;
        FP(g, f, "Teclado", 0f, rimB, kbZc, kbW, keyY - rimB, kbD, lac);
        FP(g, f, "Mejilla", -kbW / 2f + 0.035f, keyY, kbZc, 0.07f, 0.06f, kbD, lac, false);
        FP(g, f, "Mejilla", kbW / 2f - 0.035f, keyY, kbZc, 0.07f, 0.06f, kbD, lac, false);
        FP(g, f, "Frente", 0f, keyY - 0.08f, kbZ1 - 0.01f, kbW, 0.08f, 0.02f, lac, false);
        FRot(g, f, "Tapaboca", 0f, keyY + 0.07f, kbZ0 + 0.02f, kbW - 0.14f, 0.025f, 0.14f, new Vector3(-60f, 0f, 0f), lac, false);
        Surf ivory = C(new Color(0.93f, 0.9f, 0.82f), 0.5f);
        Surf ebony = C(new Color(0.05f, 0.05f, 0.05f), 0.6f);
        const int whites = 52;
        float kw = 1.2f * kx / whites, keyZ = kbZ0 + 0.1f, keyD = 0.14f;
        string letters = "ABCDEFG";
        for (int i = 0; i < whites; i++)
        {
            float x = -0.6f * kx + kw / 2f + i * kw;
            FP(g, f, "TeclaBlanca", x, keyY, keyZ, kw - 0.0015f, 0.015f, keyD, ivory, false);
            char note = letters[i % 7];
            if (i < whites - 1 && (note == 'A' || note == 'C' || note == 'D' || note == 'F' || note == 'G'))
                FP(g, f, "TeclaNegra", x + kw / 2f, keyY + 0.015f, keyZ - 0.03f, kw * 0.55f, 0.012f, keyD * 0.6f, ebony, false);
        }
        // music desk with sheet music
        FRot(g, f, "Atril", 0f, lidT + 0.17f, 0.12f * kz, 0.6f, 0.26f, 0.012f, new Vector3(-20f, 0f, 0f), lac, false);
        FRot(g, f, "Bandeja", 0f, lidT + 0.045f, 0.12f * kz + 0.05f, 0.6f, 0.012f, 0.07f, new Vector3(-20f, 0f, 0f), lac, false);
        FRot(g, f, "Partitura", -0.12f, lidT + 0.18f, 0.12f * kz + 0.007f, 0.22f, 0.28f, 0.003f, new Vector3(-20f, 0f, 0f), fPaper, false);
        FRot(g, f, "Partitura", 0.12f, lidT + 0.18f, 0.12f * kz + 0.007f, 0.22f, 0.28f, 0.003f, new Vector3(-20f, 0f, 0f), fPaper, false);
        // legs and pedal lyre
        TaperedLeg(g, f, -0.95f * kx, 0.3f * kz, 0f, rimB, 0.1f, lac, true);
        TaperedLeg(g, f, 0.95f * kx, 0.3f * kz, 0f, rimB, 0.1f, lac, true);
        TaperedLeg(g, f, 0.3f * kx, -0.45f * kz, 0f, rimB, 0.1f, lac, true);
        FRot(g, f, "Lira", -0.12f, rimB / 2f + 0.05f, 0.3f * kz, 0.03f, rimB - 0.1f, 0.03f, new Vector3(0f, 0f, 8f), lac, false);
        FRot(g, f, "Lira", 0.12f, rimB / 2f + 0.05f, 0.3f * kz, 0.03f, rimB - 0.1f, 0.03f, new Vector3(0f, 0f, -8f), lac, false);
        FP(g, f, "BaseLira", 0f, 0.05f, 0.33f * kz, 0.38f, 0.04f, 0.14f, lac, false);
        FP(g, f, "TravesanoLira", 0f, rimB - 0.06f, 0.3f * kz, 0.3f, 0.03f, 0.03f, lac, false);
        for (int k = -1; k <= 1; k++) FBm(g, f, "Pedal", k * 0.08f, 0.09f, 0.4f * kz, 0.03f, 0.012f, 0.1f, mBrass, false);
        return g;
    }

    /// <summary>Piano bench (0.9 x 0.35 x 0.5) with a padded velvet seat.</summary>
    static GameObject PianoBench(float cx, float y0, float cz, Vector3 facing)
    {
        Fx();
        var g = Group("BancoPiano");
        var f = new Frame(cx, y0, cz, facing);
        for (int i = 0; i < 4; i++)
            TaperedLeg(g, f, (i % 2 == 0 ? -1f : 1f) * 0.4f, (i < 2 ? -1f : 1f) * 0.13f, 0f, 0.42f, 0.06f, fBlackLacquer);
        FP(g, f, "Marco", 0f, 0.42f, 0f, 0.9f, 0.04f, 0.35f, fBlackLacquer);
        FP(g, f, "Asiento", 0f, 0.46f, 0f, 0.86f, 0.06f, 0.32f, fVelvetBlue);
        Rim4(g, f, "Ribete", 0f, 0.52f, 0f, 0.86f, 0.32f, 0.012f, 0.012f, C(Gold, 0.5f));
        FBall(g, f, "Boton", -0.2f, 0.52f, 0f, 0.025f, MatOf(C(Gold, 0.5f)));
        FBall(g, f, "Boton", 0.2f, 0.52f, 0f, 0.025f, MatOf(C(Gold, 0.5f)));
        return g;
    }

    /// <summary>Small child's table (0.8 x 0.7 x 0.8) with a lace cloth and a porcelain tea set.</summary>
    static GameObject ChildTable(float cx, float y0, float cz)
    {
        Fx();
        var g = Group("MesaMuneca");
        var f = new Frame(cx, y0, cz, Vector3.forward);
        for (int i = 0; i < 4; i++)
            TurnedLeg(g, f, (i % 2 == 0 ? -1f : 1f) * 0.33f, (i < 2 ? -1f : 1f) * 0.33f, 0f, 0.62f, 0.05f, sWood);
        FP(g, f, "Faldon", 0f, 0.62f, 0f, 0.7f, 0.06f, 0.7f, sWood);
        FP(g, f, "Tapa", 0f, 0.68f, 0f, 0.8f, 0.02f, 0.8f, sWood);
        FP(g, f, "Mantel", 0f, 0.70f, 0f, 0.84f, 0.006f, 0.84f, fLace, false);
        FB(g, f, "Mantel", -0.42f, 0.6f, 0f, 0.006f, 0.2f, 0.84f, fLace, false);
        FB(g, f, "Mantel", 0.42f, 0.6f, 0f, 0.006f, 0.2f, 0.84f, fLace, false);
        FB(g, f, "Mantel", 0f, 0.6f, -0.42f, 0.84f, 0.2f, 0.006f, fLace, false);
        FB(g, f, "Mantel", 0f, 0.6f, 0.42f, 0.84f, 0.2f, 0.006f, fLace, false);
        Material por = MatOf(fPorcelain);
        FEll(g, f, "Tetera", -0.15f, 0.765f, -0.1f, 0.12f, 0.11f, 0.12f, por);
        FBall(g, f, "TapaTetera", -0.15f, 0.825f, -0.1f, 0.03f, por);
        FRot(g, f, "PicoTetera", -0.23f, 0.78f, -0.1f, 0.07f, 0.02f, 0.02f, new Vector3(0f, 0f, 30f), fPorcelain, false);
        FB(g, f, "AsaTetera", -0.07f, 0.775f, -0.1f, 0.012f, 0.06f, 0.02f, fPorcelain, false);
        FEll(g, f, "Platito", 0.12f, 0.709f, 0.15f, 0.09f, 0.006f, 0.09f, por);
        FP(g, f, "Taza", 0.12f, 0.712f, 0.15f, 0.05f, 0.04f, 0.05f, fPorcelain, false);
        FB(g, f, "AsaTaza", 0.155f, 0.732f, 0.15f, 0.01f, 0.025f, 0.015f, fPorcelain, false);
        FEll(g, f, "Platito", -0.1f, 0.709f, 0.2f, 0.09f, 0.006f, 0.09f, por);
        FP(g, f, "Taza", -0.1f, 0.712f, 0.2f, 0.05f, 0.04f, 0.05f, fPorcelain, false);
        FB(g, f, "AsaTaza", -0.065f, 0.732f, 0.2f, 0.01f, 0.025f, 0.015f, fPorcelain, false);
        FEll(g, f, "Azucarera", 0.05f, 0.735f, -0.15f, 0.07f, 0.06f, 0.07f, por);
        FBall(g, f, "TapaAzucarera", 0.05f, 0.77f, -0.15f, 0.02f, por);
        return g;
    }

    /// <summary>
    /// Porcelain doll seated on a surface at height y0 (0.3 x 0.5 x 0.3): tiered ruffled dress, legs forward with
    /// socks and buckled shoes, arms, head with hair, curls, bangs and two ribbon bows, black glass eyes, red lips.
    /// No colliders on parts; a "Hitbox" child (0.4 x 0.7 x 0.4) lets the player aim at it.
    /// </summary>
    static GameObject DollSeated(float cx, float y0, float cz, Vector3 facing)
    {
        Fx();
        var g = Group("Muneca");
        var f = new Frame(cx, y0, cz, facing);
        Surf skin = C(new Color(0.92f, 0.86f, 0.8f), 0.5f);
        Material skinM = MatOf(skin);
        Surf dress = C(new Color(0.55f, 0.1f, 0.15f), 0.3f);
        Surf white = C(new Color(0.95f, 0.94f, 0.9f), 0.2f);
        Surf shoe = C(new Color(0.05f, 0.05f, 0.05f), 0.85f);
        Material hair = MatOf(C(new Color(0.25f, 0.14f, 0.08f), 0.5f));
        Surf ribbon = C(new Color(0.75f, 0.1f, 0.15f), 0.4f);
        // skirt tiers with lace ruffles
        FP(g, f, "Falda", 0f, 0f, 0.02f, 0.26f, 0.06f, 0.24f, dress, false);
        FP(g, f, "Volante", 0f, 0f, 0.02f, 0.27f, 0.015f, 0.25f, fLace, false);
        FP(g, f, "Falda", 0f, 0.06f, 0f, 0.2f, 0.07f, 0.18f, dress, false);
        FP(g, f, "Volante", 0f, 0.06f, 0f, 0.21f, 0.012f, 0.19f, fLace, false);
        FP(g, f, "Falda", 0f, 0.13f, -0.01f, 0.16f, 0.08f, 0.13f, dress, false);
        FP(g, f, "Volante", 0f, 0.13f, -0.01f, 0.17f, 0.012f, 0.14f, fLace, false);
        FP(g, f, "Corpino", 0f, 0.21f, -0.01f, 0.14f, 0.12f, 0.09f, dress, false);
        FP(g, f, "Cuello", 0f, 0.32f, -0.01f, 0.1f, 0.02f, 0.07f, fLace, false);
        for (int k = 0; k < 3; k++) FBall(g, f, "Perla", 0f, 0.24f + k * 0.03f, 0.036f, 0.009f, MatOf(fPorcelain));
        for (int s = -1; s <= 1; s += 2)
        {
            FP(g, f, "Pierna", s * 0.04f, 0.02f, 0.12f, 0.035f, 0.035f, 0.16f, skin, false);
            FP(g, f, "Calcetin", s * 0.04f, 0.015f, 0.18f, 0.04f, 0.045f, 0.05f, white, false);
            FP(g, f, "Zapato", s * 0.04f, 0.01f, 0.22f, 0.045f, 0.035f, 0.07f, shoe, false);
            FBall(g, f, "Hebilla", s * 0.04f, 0.03f, 0.235f, 0.012f, mBrass);
            Quaternion q = Quaternion.Euler(-25f, 0f, s * 15f);
            FRotOff(g, f, "Brazo", s * 0.08f, 0.3f, -0.01f, new Vector3(0f, -0.1f, 0f), 0.03f, 0.2f, 0.03f, new Vector3(-25f, 0f, s * 15f), dress, false);
            Vector3 hp = q * new Vector3(0f, -0.21f, 0f);
            FBall(g, f, "Mano", s * 0.08f + hp.x, 0.3f + hp.y, -0.01f + hp.z, 0.035f, skinM);
            FEll(g, f, "Rizo", s * 0.075f, 0.38f, -0.01f, 0.035f, 0.09f, 0.04f, hair);
            FRot(g, f, "Mono", s * 0.07f, 0.49f, 0f, 0.05f, 0.025f, 0.012f, new Vector3(0f, 0f, s * 25f), ribbon, false);
            FBall(g, f, "Nudo", s * 0.07f, 0.49f, 0.004f, 0.016f, MatOf(ribbon));
            FBall(g, f, "Ojo", s * 0.025f, 0.43f, 0.06f, 0.022f, mGlassEye);
            FBall(g, f, "Brillo", s * 0.028f, 0.436f, 0.068f, 0.006f, MatOf(white));
            FEll(g, f, "Mejilla", s * 0.04f, 0.4f, 0.05f, 0.03f, 0.02f, 0.012f, MatOf(C(new Color(0.9f, 0.6f, 0.6f), 0.3f)));
            FRot(g, f, "Ceja", s * 0.025f, 0.452f, 0.064f, 0.025f, 0.004f, 0.004f, new Vector3(0f, 0f, s * 12f), C(new Color(0.2f, 0.1f, 0.06f), 0.2f), false);
        }
        FBall(g, f, "Cabeza", 0f, 0.42f, 0f, 0.14f, skinM);
        FEll(g, f, "Cabello", 0f, 0.445f, -0.015f, 0.15f, 0.13f, 0.15f, hair);
        FB(g, f, "Fleco", 0f, 0.47f, 0.055f, 0.1f, 0.03f, 0.02f, C(new Color(0.25f, 0.14f, 0.08f), 0.5f), false);
        FBall(g, f, "Nariz", 0f, 0.41f, 0.068f, 0.012f, skinM);
        FB(g, f, "Labios", 0f, 0.385f, 0.064f, 0.03f, 0.008f, 0.01f, C(new Color(0.6f, 0.05f, 0.08f), 0.4f), false);
        FHit(g, f, "Hitbox", 0f, 0.3f, 0.05f, 0.4f, 0.7f, 0.4f);
        return g;
    }

    // ================================================================== 9. OLD TV
    /// <summary>
    /// 1990s CRT television on a wooden stand (w x d x h = 1.6 x 0.5 x 1.1 for the cabinet; the antenna rises ~0.35 above h).
    /// Screen child named "Pantalla" with a very dim emissive material; layered glossy boxes give the convex look.
    /// </summary>
    static GameObject TV(float cx, float y0, float cz, Vector3 facing, float w, float d, float h)
    {
        Fx();
        var g = Group("Television");
        var f = new Frame(cx, y0, cz, facing);
        float hw = w / 2f, hd = d / 2f;
        float standTop = h - 0.52f;
        for (int i = 0; i < 4; i++)
            FP(g, f, "Pata", (i % 2 == 0 ? -1f : 1f) * (hw - 0.06f), 0f, (i < 2 ? -1f : 1f) * (hd - 0.06f), 0.07f, 0.1f, 0.07f, sDark, false);
        FP(g, f, "Lado", -hw + 0.02f, 0.1f, 0f, 0.04f, standTop - 0.13f, d, sDark);
        FP(g, f, "Lado", hw - 0.02f, 0.1f, 0f, 0.04f, standTop - 0.13f, d, sDark);
        FP(g, f, "Fondo", 0f, 0.1f, -hd + 0.01f, w - 0.08f, standTop - 0.13f, 0.02f, sDark);
        FP(g, f, "Piso", 0f, 0.1f, 0f, w - 0.08f, 0.03f, d - 0.02f, sDark);
        FP(g, f, "Repisa", 0f, 0.1f + (standTop - 0.13f) * 0.5f, 0f, w - 0.08f, 0.025f, d - 0.04f, sWood, false);
        FP(g, f, "Tapa", 0f, standTop - 0.03f, 0f, w + 0.02f, 0.03f, d + 0.02f, sDark);
        // VCR on the shelf, tapes below, remote on top
        float shelfY = 0.1f + (standTop - 0.13f) * 0.5f + 0.025f;
        FP(g, f, "Video", 0f, shelfY, 0f, 0.42f, 0.09f, 0.35f, fPlasticGrey, false);
        FB(g, f, "RanuraVideo", 0f, shelfY + 0.06f, 0.176f, 0.2f, 0.025f, 0.003f, C(Color.black, 0.2f), false);
        FBall(g, f, "Led", 0.15f, shelfY + 0.03f, 0.177f, 0.012f, EmitMat(new Color(1f, 0.1f, 0.05f), 2f));
        for (int k = 0; k < 3; k++) FB(g, f, "BotonVideo", -0.1f + k * 0.04f, shelfY + 0.025f, 0.177f, 0.025f, 0.012f, 0.004f, C(new Color(0.3f, 0.3f, 0.3f), 0.4f), false);
        FP(g, f, "Casete", -w * 0.3f, 0.13f, 0f, 0.19f, 0.025f, 0.1f, C(new Color(0.05f, 0.05f, 0.05f), 0.4f), false);
        FP(g, f, "Casete", -w * 0.3f + 0.01f, 0.155f, 0.01f, 0.19f, 0.025f, 0.1f, C(new Color(0.08f, 0.08f, 0.1f), 0.4f), false);
        FP(g, f, "Control", w * 0.4f, standTop, hd * 0.4f, 0.05f, 0.02f, 0.16f, fPlasticGrey, false);
        for (int k = 0; k < 4; k++) FB(g, f, "Tecla", w * 0.4f + (k % 2 == 0 ? -0.012f : 0.012f), standTop + 0.022f, hd * 0.4f - 0.03f + (k / 2) * 0.03f, 0.012f, 0.004f, 0.012f, C(new Color(0.5f, 0.5f, 0.5f), 0.3f), false);
        FP(g, f, "Carpeta", w * 0.38f, standTop, -0.02f, 0.22f, 0.004f, 0.22f, fLace, false);
        // CRT set: front cabinet, narrower rear, bezel, layered screen, speaker grille, knobs, nameplate, antenna
        FP(g, f, "Gabinete", 0f, standTop, 0.07f, 0.8f, 0.52f, 0.26f, fPlasticGrey);
        FP(g, f, "Trasera", 0f, standTop + 0.04f, -0.13f, 0.62f, 0.44f, 0.22f, fPlasticGrey);
        float sy = standTop + 0.29f, frontZ = 0.2f;
        FB(g, f, "Bisel", -0.08f, sy, frontZ + 0.005f, 0.6f, 0.46f, 0.01f, C(new Color(0.08f, 0.08f, 0.09f), 0.5f), false);
        FB(g, f, "PantallaVidrio", -0.07f, sy, frontZ + 0.012f, 0.52f, 0.4f, 0.006f, fGlassDark, false);
        FBm(g, f, "Pantalla", -0.07f, sy, frontZ + 0.017f, 0.46f, 0.35f, 0.006f, mScreen, false);
        FBm(g, f, "PantallaCurva", -0.07f, sy, frontZ + 0.022f, 0.38f, 0.29f, 0.006f, mScreen, false);
        FBm(g, f, "PantallaCurva", -0.07f, sy, frontZ + 0.026f, 0.26f, 0.2f, 0.005f, mScreen, false);
        FB(g, f, "Bocina", 0.3f, sy, frontZ + 0.005f, 0.14f, 0.4f, 0.01f, C(new Color(0.05f, 0.05f, 0.05f), 0.2f), false);
        for (int k = 0; k < 8; k++) FB(g, f, "Lama", 0.3f, sy - 0.16f + k * 0.045f, frontZ + 0.011f, 0.12f, 0.006f, 0.003f, fPlasticGrey, false);
        for (int k = 0; k < 4; k++) FBall(g, f, "Perilla", 0.245f + k * 0.035f, standTop + 0.05f, frontZ + 0.007f, 0.02f, MatOf(C(new Color(0.3f, 0.3f, 0.3f), 0.5f)));
        FB(g, f, "BotonEncendido", -0.25f, standTop + 0.05f, frontZ + 0.006f, 0.04f, 0.015f, 0.006f, C(new Color(0.3f, 0.3f, 0.3f), 0.5f), false);
        FBm(g, f, "Placa", -0.07f, standTop + 0.05f, frontZ + 0.006f, 0.12f, 0.015f, 0.004f, mChrome, false);
        FP(g, f, "BaseAntena", 0.1f, h, -0.05f, 0.1f, 0.04f, 0.08f, fPlasticGrey, false);
        for (int s = -1; s <= 1; s += 2)
        {
            FRotOff(g, f, "Antena", 0.1f, h + 0.04f, -0.05f, new Vector3(0f, 0.17f, 0f), 0.008f, 0.34f, 0.008f, new Vector3(0f, 0f, s * 28f), C(new Color(0.75f, 0.75f, 0.78f), 0.8f), false);
            Vector3 tip = Quaternion.Euler(0f, 0f, s * 28f) * new Vector3(0f, 0.34f, 0f);
            FBall(g, f, "PuntaAntena", 0.1f + tip.x, h + 0.04f + tip.y, -0.05f + tip.z, 0.016f, mChrome);
        }
        return g;
    }

    // ================================================================== 10. DECORATION
    /// <summary>Potted plant scaled by k (k = 1 is a 0.46 m pot with ~1.1 m of foliage): saucer, tapered pot, rim, soil, stems, leaves.</summary>
    static GameObject Plant(float cx, float y0, float cz, float k)
    {
        Fx();
        var g = Group("Planta");
        var f = new Frame(cx, y0, cz, Vector3.forward);
        Material terra = MatOf(fTerracotta);
        FEll(g, f, "Plato", 0f, 0.01f * k, 0f, 0.46f * k, 0.02f * k, 0.46f * k, terra);
        FP(g, f, "Maceta", 0f, 0.02f * k, 0f, 0.3f * k, 0.12f * k, 0.3f * k, fTerracotta);
        FP(g, f, "Maceta", 0f, 0.14f * k, 0f, 0.35f * k, 0.12f * k, 0.35f * k, fTerracotta);
        FP(g, f, "Maceta", 0f, 0.26f * k, 0f, 0.4f * k, 0.1f * k, 0.4f * k, fTerracotta);
        FP(g, f, "BordeMaceta", 0f, 0.36f * k, 0f, 0.44f * k, 0.04f * k, 0.44f * k, fTerracotta, false);
        FP(g, f, "Tierra", 0f, 0.37f * k, 0f, 0.38f * k, 0.02f * k, 0.38f * k, C(new Color(0.12f, 0.08f, 0.05f), 0.05f), false);
        Surf stem = C(new Color(0.25f, 0.2f, 0.1f), 0.1f);
        Surf leafA = C(new Color(0.12f, 0.35f, 0.14f), 0.25f), leafB = C(new Color(0.2f, 0.45f, 0.18f), 0.25f);
        for (int i = 0; i < 3; i++)
            FRotOff(g, f, "Tallo", 0f, 0.39f * k, 0f, new Vector3(0f, 0.2f * k, 0f), 0.015f * k, 0.4f * k, 0.015f * k, new Vector3(8f, i * 120f, 0f), stem, false);
        for (int i = 0; i < 18; i++)
        {
            float yaw = i * 20f + Hash01(i, 3) * 14f;
            float tilt = 35f + 25f * Hash01(i, 5);
            float len = (0.35f + 0.2f * Hash01(i, 7)) * k;
            float py = (0.45f + 0.25f * Hash01(i, 9)) * k;
            FRotOff(g, f, "Hoja", 0f, py, 0f, new Vector3(0f, 0f, len / 2f + 0.05f * k), 0.07f * k, 0.008f * k, len, new Vector3(-tilt, yaw, 0f), (i % 2 == 0) ? leafA : leafB, false);
        }
        for (int i = 0; i < 6; i++)
        {
            float yaw = i * 60f + 30f;
            float len = (0.3f + 0.15f * Hash01(i, 11)) * k;
            FRotOff(g, f, "HojaCaida", 0f, 0.72f * k, 0f, new Vector3(0f, 0f, len / 2f + 0.08f * k), 0.065f * k, 0.008f * k, len, new Vector3(25f, yaw, 0f), leafB, false);
        }
        return g;
    }

    /// <summary>Five-arm brass candelabra of total height h with emissive flames (no Light components).</summary>
    static GameObject Candelabra(float cx, float y0, float cz, float h)
    {
        Fx();
        var g = Group("Candelabro");
        var f = new Frame(cx, y0, cz, Vector3.forward);
        float k = h / 0.62f;
        Surf wax = C(new Color(0.95f, 0.92f, 0.82f), 0.3f);
        Surf brassS = C(Gold, 0.8f);
        FEll(g, f, "Base", 0f, 0.012f * k, 0f, 0.26f * k, 0.025f * k, 0.26f * k, mBrass);
        FEll(g, f, "Base", 0f, 0.035f * k, 0f, 0.16f * k, 0.03f * k, 0.16f * k, mBrass);
        FPm(g, f, "Fuste", 0f, 0.05f * k, 0f, 0.03f * k, 0.25f * k, 0.03f * k, mBrass, false);
        FBall(g, f, "Nudo", 0f, 0.18f * k, 0f, 0.07f * k, mBrass);
        FBall(g, f, "Nudo", 0f, 0.3f * k, 0f, 0.06f * k, mBrass);
        for (int i = 0; i < 4; i++)
        {
            float yaw = i * 90f;
            Vector3 end = Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0f, 0.2f * k);
            FRotOff(g, f, "Brazo", 0f, 0.3f * k, 0f, new Vector3(0f, 0f, 0.1f * k), 0.02f * k, 0.02f * k, 0.2f * k, new Vector3(0f, yaw, 0f), brassS, false);
            FPm(g, f, "BrazoAlto", end.x, 0.3f * k, end.z, 0.02f * k, 0.1f * k, 0.02f * k, mBrass, false);
            FEll(g, f, "Arandela", end.x, 0.41f * k, end.z, 0.07f * k, 0.012f * k, 0.07f * k, mBrass);
            float ch = (0.13f + 0.04f * Hash01(i, 2)) * k;
            FP(g, f, "Vela", end.x, 0.415f * k, end.z, 0.025f * k, ch, 0.025f * k, wax, false);
            FBall(g, f, "Llama", end.x, 0.415f * k + ch + 0.02f * k, end.z, 0.035f * k, mFlame);
        }
        FPm(g, f, "BrazoCentral", 0f, 0.3f * k, 0f, 0.025f * k, 0.15f * k, 0.025f * k, mBrass, false);
        FEll(g, f, "Arandela", 0f, 0.455f * k, 0f, 0.07f * k, 0.012f * k, 0.07f * k, mBrass);
        FP(g, f, "Vela", 0f, 0.46f * k, 0f, 0.025f * k, 0.12f * k, 0.025f * k, wax, false);
        FBall(g, f, "Llama", 0f, 0.6f * k, 0f, 0.035f * k, mFlame);
        return g;
    }

    /// <summary>Floor lamp (1.85 m): brass base, turned stem alternating brass and dark wood, harp, pleated emissive shade, finial.</summary>
    static GameObject FloorLamp(float cx, float y0, float cz)
    {
        Fx();
        var g = Group("LamparaPie");
        var f = new Frame(cx, y0, cz, Vector3.forward);
        FEll(g, f, "Base", 0f, 0.015f, 0f, 0.36f, 0.03f, 0.36f, mBrass);
        FEll(g, f, "Base", 0f, 0.04f, 0f, 0.2f, 0.03f, 0.2f, mBrass);
        FHit(g, f, "Hitbox", 0f, 0.9f, 0f, 0.3f, 1.8f, 0.3f);
        FPm(g, f, "Fuste", 0f, 0.05f, 0f, 0.03f, 0.4f, 0.03f, mBrass, false);
        FBall(g, f, "Nudo", 0f, 0.47f, 0f, 0.07f, mBrass);
        FP(g, f, "FusteMadera", 0f, 0.5f, 0f, 0.05f, 0.5f, 0.05f, sDark, false);
        FBall(g, f, "Nudo", 0f, 1.02f, 0f, 0.08f, MatOf(sDark));
        FPm(g, f, "Fuste", 0f, 1.05f, 0f, 0.03f, 0.4f, 0.03f, mBrass, false);
        FBall(g, f, "Nudo", 0f, 1.47f, 0f, 0.05f, mBrass);
        FPm(g, f, "Casquillo", 0f, 1.48f, 0f, 0.04f, 0.06f, 0.04f, mBrass, false);
        FBm(g, f, "Arpa", -0.14f, 1.66f, 0f, 0.008f, 0.26f, 0.008f, mBrass, false);
        FBm(g, f, "Arpa", 0.14f, 1.66f, 0f, 0.008f, 0.26f, 0.008f, mBrass, false);
        FBm(g, f, "Arpa", 0f, 1.79f, 0f, 0.29f, 0.008f, 0.008f, mBrass, false);
        for (int t = 0; t < 4; t++)
            FPm(g, f, "Pantalla", 0f, 1.5f + t * 0.075f, 0f, 0.44f - t * 0.04f, 0.075f, 0.44f - t * 0.04f, mLampShade, false);
        FBall(g, f, "Remate", 0f, 1.83f, 0f, 0.035f, mBrass);
        return g;
    }

    /// <summary>Suit of armour on a stone plinth (0.6 x 0.6 footprint, ~2.05 m plus halberd): sabatons to plumed helmet, halberd in the right hand, shield on the left arm.</summary>
    static GameObject Armor(float cx, float y0, float cz, Vector3 facing)
    {
        Fx();
        var g = Group("Armadura");
        var f = new Frame(cx, y0, cz, facing);
        Material steel = Metal(new Color(0.62f, 0.64f, 0.66f), 0.75f, 0.8f);
        FP(g, f, "Pedestal", 0f, 0f, 0f, 0.6f, 0.15f, 0.6f, sStone);
        FP(g, f, "MolduraPedestal", 0f, 0.15f, 0f, 0.64f, 0.03f, 0.64f, sDark, false);
        float b = 0.18f;
        for (int s = -1; s <= 1; s += 2)
        {
            FPm(g, f, "Escarpe", s * 0.1f, b, 0.1f, 0.12f, 0.07f, 0.24f, steel, false);
            FPm(g, f, "Greba", s * 0.1f, b, 0.02f, 0.13f, 0.45f, 0.16f, steel, false);
            FBall(g, f, "Rodillera", s * 0.1f, b + 0.47f, 0.06f, 0.13f, steel);
            FPm(g, f, "Quijote", s * 0.1f, b + 0.5f, 0f, 0.15f, 0.38f, 0.17f, steel, false);
            FRotm(g, f, "Escarcela", s * 0.14f, b + 0.78f, 0.1f, 0.16f, 0.2f, 0.02f, new Vector3(-15f, 0f, 0f), steel, false);
            FBall(g, f, "Hombrera", s * 0.25f, b + 1.42f, 0f, 0.2f, steel);
            FPm(g, f, "Brazal", s * 0.3f, b + 1.05f, 0.02f, 0.1f, 0.3f, 0.1f, steel, false);
            FBall(g, f, "Codal", s * 0.3f, b + 1.03f, 0.02f, 0.11f, steel);
            FPm(g, f, "Avambrazo", s * 0.3f, b + 0.75f, 0.03f, 0.09f, 0.28f, 0.09f, steel, false);
            FPm(g, f, "Guantelete", s * 0.3f, b + 0.6f, 0.06f, 0.11f, 0.16f, 0.12f, steel, false);
        }
        FPm(g, f, "Faldar", 0f, b + 0.85f, 0f, 0.42f, 0.14f, 0.26f, steel, false);
        FPm(g, f, "Peto", 0f, b + 0.95f, 0.02f, 0.44f, 0.5f, 0.26f, steel, false);
        FBm(g, f, "Arista", 0f, b + 1.2f, 0.155f, 0.015f, 0.45f, 0.015f, steel, false);
        FPm(g, f, "Gola", 0f, b + 1.45f, 0f, 0.3f, 0.08f, 0.24f, steel, false);
        FBall(g, f, "Yelmo", 0f, b + 1.65f, 0f, 0.26f, steel);
        FPm(g, f, "Visera", 0f, b + 1.58f, 0.08f, 0.26f, 0.09f, 0.1f, steel, false);
        for (int k = 0; k < 3; k++) FB(g, f, "Rendija", 0f, b + 1.6f + k * 0.02f, 0.135f, 0.2f, 0.006f, 0.004f, C(Color.black, 0.2f), false);
        FBm(g, f, "Cresta", 0f, b + 1.78f, 0f, 0.03f, 0.06f, 0.24f, steel, false);
        Surf plume = C(new Color(0.6f, 0.08f, 0.1f), 0.1f);
        FRot(g, f, "Penacho", 0f, b + 1.9f, -0.06f, 0.05f, 0.24f, 0.05f, new Vector3(-20f, 0f, 0f), plume, false);
        FRot(g, f, "Penacho", -0.03f, b + 1.88f, -0.08f, 0.04f, 0.2f, 0.04f, new Vector3(-32f, 0f, 10f), plume, false);
        FRot(g, f, "Penacho", 0.03f, b + 1.88f, -0.08f, 0.04f, 0.2f, 0.04f, new Vector3(-32f, 0f, -10f), plume, false);
        // halberd in the right hand (local +x), shield on the left arm
        FP(g, f, "Asta", 0.3f, b, 0.09f, 0.025f, 2.1f, 0.025f, sDark, false);
        FPm(g, f, "HojaHacha", 0.4f, b + 1.7f, 0.09f, 0.2f, 0.3f, 0.015f, steel, false);
        FBm(g, f, "Punta", 0.3f, b + 2.2f, 0.09f, 0.02f, 0.2f, 0.02f, steel, false);
        FRot(g, f, "Escudo", -0.34f, b + 0.95f, 0.12f, 0.4f, 0.5f, 0.03f, new Vector3(0f, -15f, 0f), C(new Color(0.4f, 0.08f, 0.1f), 0.5f), false);
        FRotm(g, f, "CruzEscudo", -0.34f, b + 0.95f, 0.14f, 0.05f, 0.36f, 0.01f, new Vector3(0f, -15f, 0f), mBrass, false);
        FRotm(g, f, "CruzEscudo", -0.34f, b + 1.0f, 0.14f, 0.28f, 0.05f, 0.01f, new Vector3(0f, -15f, 0f), mBrass, false);
        FBall(g, f, "Umbo", -0.34f, b + 0.95f, 0.15f, 0.07f, mBrass);
        FB(g, f, "Sombra", 0f, 0.185f, 0f, 0.5f, 0.01f, 0.4f, C(Color.black, 0.1f), false);
        FHit(g, f, "Hitbox", 0f, 0.18f + 0.95f, 0f, 0.6f, 1.9f, 0.5f);
        return g;
    }

    /// <summary>Ornate gilt wall mirror. (cx, cy, cz) is the centre ON the wall; the crest adds ~0.3 above and the apron ~0.2 below h. Glass child "Espejo" collides.</summary>
    static GameObject WallMirror(float cx, float cy, float cz, Vector3 facing, float w, float h)
    {
        Fx();
        var g = Group("EspejoPared");
        var f = new Frame(cx, cy, cz, facing);
        Surf gold = C(Gold, 0.6f), goldD = C(new Color(0.55f, 0.42f, 0.15f), 0.6f);
        Material goldM = MatOf(gold);
        FB(g, f, "MarcoExterior", 0f, 0f, 0.02f, w + 0.2f, h + 0.2f, 0.04f, gold, false);
        FB(g, f, "MarcoMedio", 0f, 0f, 0.035f, w + 0.12f, h + 0.12f, 0.05f, goldD, false);
        FB(g, f, "MarcoInterior", 0f, 0f, 0.045f, w + 0.04f, h + 0.04f, 0.06f, gold, false);
        FB(g, f, "Espejo", 0f, 0f, 0.076f, w, h, 0.01f, fMirror, true);
        RimV4(g, f, "Biselado", 0f, 0f, 0.082f, w - 0.02f, h - 0.02f, 0.03f, 0.002f, C(new Color(0.85f, 0.9f, 0.95f), 0.98f));
        for (int i = 0; i < 4; i++)
            FEll(g, f, "Roseton", (i % 2 == 0 ? -1f : 1f) * (w / 2f + 0.05f), (i < 2 ? -1f : 1f) * (h / 2f + 0.05f), 0.07f, 0.1f, 0.1f, 0.04f, goldM);
        FB(g, f, "Cresta", 0f, h / 2f + 0.13f, 0.03f, w * 0.5f, 0.08f, 0.05f, gold, false);
        FB(g, f, "Cresta", 0f, h / 2f + 0.2f, 0.03f, w * 0.3f, 0.07f, 0.05f, gold, false);
        FEll(g, f, "Concha", 0f, h / 2f + 0.27f, 0.035f, 0.18f, 0.12f, 0.06f, goldM);
        FBall(g, f, "Bola", -w * 0.22f, h / 2f + 0.17f, 0.035f, 0.06f, goldM);
        FBall(g, f, "Bola", w * 0.22f, h / 2f + 0.17f, 0.035f, 0.06f, goldM);
        FB(g, f, "Faldon", 0f, -h / 2f - 0.13f, 0.03f, w * 0.4f, 0.06f, 0.05f, gold, false);
        FEll(g, f, "Gota", 0f, -h / 2f - 0.19f, 0.035f, 0.12f, 0.08f, 0.05f, goldM);
        return g;
    }

    /// <summary>Hall coat stand (1.8 m pole, ~2.1 m with the hat): four feet, hooks with a coat and a top hat.</summary>
    static GameObject CoatRack(float cx, float y0, float cz)
    {
        Fx();
        var g = Group("Perchero");
        var f = new Frame(cx, y0, cz, Vector3.forward);
        Surf brassS = C(Gold, 0.8f);
        for (int k = 0; k < 4; k++)
        {
            FRotOff(g, f, "Pie", 0f, 0.03f, 0f, new Vector3(0f, 0f, 0.14f), 0.05f, 0.05f, 0.28f, new Vector3(0f, k * 90f + 45f, 0f), sDark, false);
            Vector3 e = Quaternion.Euler(0f, k * 90f + 45f, 0f) * new Vector3(0f, 0f, 0.28f);
            FBall(g, f, "PieBola", e.x, 0.035f, e.z, 0.06f, MatOf(sDark));
        }
        FP(g, f, "Poste", 0f, 0f, 0f, 0.06f, 1.8f, 0.06f, sDark);
        FBall(g, f, "Nudo", 0f, 0.9f, 0f, 0.09f, MatOf(sDark));
        FBall(g, f, "Nudo", 0f, 1.5f, 0f, 0.09f, MatOf(sDark));
        for (int k = 0; k < 6; k++)
        {
            float yaw = k * 60f;
            FRotOff(g, f, "Gancho", 0f, 1.62f, 0f, new Vector3(0f, 0f, 0.1f), 0.02f, 0.02f, 0.2f, new Vector3(-25f, yaw, 0f), brassS, false);
            Vector3 e = Quaternion.Euler(-25f, yaw, 0f) * new Vector3(0f, 0f, 0.2f);
            FBall(g, f, "PuntaGancho", e.x, 1.62f + e.y, e.z, 0.03f, mBrass);
        }
        Surf coat = C(new Color(0.15f, 0.12f, 0.1f), 0.1f);
        FP(g, f, "Abrigo", 0f, 0.78f, 0.17f, 0.42f, 0.85f, 0.14f, coat, false);
        FP(g, f, "Cuello", 0f, 1.58f, 0.17f, 0.3f, 0.08f, 0.16f, coat, false);
        FRot(g, f, "Manga", -0.24f, 1.25f, 0.17f, 0.1f, 0.6f, 0.12f, new Vector3(0f, 0f, 6f), coat, false);
        FRot(g, f, "Manga", 0.24f, 1.25f, 0.17f, 0.1f, 0.6f, 0.12f, new Vector3(0f, 0f, -6f), coat, false);
        for (int k = 0; k < 3; k++) FBall(g, f, "Boton", 0.02f, 1.2f + k * 0.12f, 0.245f, 0.02f, mBrass);
        FEll(g, f, "Ala", 0f, 1.93f, 0f, 0.36f, 0.03f, 0.36f, MatOf(C(new Color(0.06f, 0.06f, 0.06f), 0.4f)));
        FP(g, f, "Copa", 0f, 1.94f, 0f, 0.2f, 0.14f, 0.2f, C(new Color(0.06f, 0.06f, 0.06f), 0.4f), false);
        FB(g, f, "Cinta", 0f, 1.97f, 0f, 0.205f, 0.025f, 0.205f, C(new Color(0.3f, 0.05f, 0.05f), 0.3f), false);
        return g;
    }

    /// <summary>Umbrella stand (0.26 x 0.26 x 0.62) with two umbrellas and a cane.</summary>
    static GameObject UmbrellaStand(float cx, float y0, float cz)
    {
        Fx();
        var g = Group("Paraguero");
        var f = new Frame(cx, y0, cz, Vector3.forward);
        FP(g, f, "Paraguero", 0f, 0f, 0f, 0.26f, 0.6f, 0.26f, fIron);
        Rim4(g, f, "Aro", 0f, 0.61f, 0f, 0.28f, 0.28f, 0.02f, 0.02f, C(Gold, 0.8f));
        FB(g, f, "Interior", 0f, 0.6f, 0f, 0.22f, 0.01f, 0.22f, C(Color.black, 0.1f), false);
        FRot(g, f, "Paraguas", 0.05f, 0.62f, 0.03f, 0.05f, 0.85f, 0.05f, new Vector3(4f, 0f, -6f), C(new Color(0.05f, 0.05f, 0.08f), 0.3f), false);
        FBall(g, f, "MangoParaguas", 0.09f, 1.05f, 0.06f, 0.05f, MatOf(sDark));
        FRot(g, f, "Paraguas", -0.05f, 0.6f, -0.04f, 0.045f, 0.8f, 0.045f, new Vector3(-5f, 0f, 5f), C(new Color(0.3f, 0.06f, 0.08f), 0.3f), false);
        FBall(g, f, "MangoParaguas", -0.085f, 1.0f, -0.075f, 0.045f, MatOf(sDark));
        FRot(g, f, "Baston", 0.0f, 0.55f, -0.07f, 0.025f, 0.95f, 0.025f, new Vector3(-6f, 0f, 2f), sDark, false);
        FBall(g, f, "PomoBaston", -0.015f, 1.03f, -0.12f, 0.04f, mBrass);
        return g;
    }

    /// <summary>Hall bench of width w: turned legs, slatted back with top rail, armrests, a cushion.</summary>
    static GameObject HallBench(float cx, float y0, float cz, Vector3 facing, float w)
    {
        Fx();
        var g = Group("Banca");
        var f = new Frame(cx, y0, cz, facing);
        float hw = w / 2f;
        for (int i = 0; i < 4; i++)
            TurnedLeg(g, f, (i % 2 == 0 ? -1f : 1f) * (hw - 0.08f), (i < 2 ? -1f : 1f) * 0.17f, 0f, 0.41f, 0.06f, sDark);
        Rim4(g, f, "Faldon", 0f, 0.38f, 0f, w - 0.06f, 0.4f, 0.03f, 0.05f, sDark);
        FP(g, f, "Asiento", 0f, 0.41f, 0f, w, 0.04f, 0.45f, sWood);
        FP(g, f, "PosteRespaldo", -(hw - 0.08f), 0.45f, -0.19f, 0.06f, 0.55f, 0.06f, sDark, false);
        FP(g, f, "PosteRespaldo", hw - 0.08f, 0.45f, -0.19f, 0.06f, 0.55f, 0.06f, sDark, false);
        FP(g, f, "Copete", 0f, 0.94f, -0.19f, w, 0.06f, 0.05f, sDark);
        FP(g, f, "Travesano", 0f, 0.55f, -0.19f, w - 0.16f, 0.04f, 0.03f, sDark, false);
        int n = Mathf.Max(3, Mathf.FloorToInt((w - 0.3f) / 0.14f));
        for (int k = 0; k < n; k++)
        {
            float x = -(w - 0.3f) / 2f + 0.07f + k * ((w - 0.3f - 0.14f) / Mathf.Max(1, n - 1));
            FP(g, f, "Barrote", x, 0.59f, -0.19f, 0.05f, 0.35f, 0.02f, sWood, false);
        }
        for (int s = -1; s <= 1; s += 2)
        {
            FP(g, f, "Brazo", s * (hw - 0.05f), 0.65f, 0f, 0.05f, 0.04f, 0.42f, sDark, false);
            FP(g, f, "SoporteBrazo", s * (hw - 0.05f), 0.45f, 0.17f, 0.04f, 0.2f, 0.04f, sDark, false);
        }
        FP(g, f, "Cojin", 0f, 0.45f, 0.02f, w - 0.1f, 0.05f, 0.36f, fDamaskGreen, false);
        FEll(g, f, "Talla", 0f, 0.97f, -0.19f, 0.2f, 0.06f, 0.05f, MatOf(sDark));
        return g;
    }

    // ================================================================== EXTRAS
    /// <summary>Two-tier brass tea trolley (0.7 x 0.45 x 0.8) on wheels with a tea service.</summary>
    static GameObject TeaTrolley(float cx, float y0, float cz, Vector3 facing)
    {
        Fx();
        var g = Group("Carrito");
        var f = new Frame(cx, y0, cz, facing);
        Material por = MatOf(fPorcelain);
        for (int i = 0; i < 4; i++)
        {
            float lx = (i % 2 == 0 ? -1f : 1f) * 0.32f, lz = (i < 2 ? -1f : 1f) * 0.2f;
            FPm(g, f, "Poste", lx, 0.05f, lz, 0.025f, 0.75f, 0.025f, mBrass, false);
            FBall(g, f, "Rueda", lx, 0.04f, lz, 0.08f, MatOf(fIron));
        }
        FP(g, f, "Charola", 0f, 0.78f, 0f, 0.7f, 0.02f, 0.45f, sWood);
        Rim4m(g, f, "BordeCharola", 0f, 0.81f, 0f, 0.7f, 0.45f, 0.015f, 0.025f, mBrass);
        FP(g, f, "Charola", 0f, 0.3f, 0f, 0.7f, 0.02f, 0.45f, sWood);
        Rim4m(g, f, "BordeCharola", 0f, 0.33f, 0f, 0.7f, 0.45f, 0.015f, 0.025f, mBrass);
        FBm(g, f, "Asa", 0.36f, 0.86f, 0f, 0.02f, 0.02f, 0.3f, mBrass, false);
        FEll(g, f, "Tetera", -0.2f, 0.88f, -0.05f, 0.16f, 0.15f, 0.16f, por);
        FBall(g, f, "TapaTetera", -0.2f, 0.965f, -0.05f, 0.04f, por);
        FRot(g, f, "PicoTetera", -0.3f, 0.9f, -0.05f, 0.09f, 0.025f, 0.025f, new Vector3(0f, 0f, 30f), fPorcelain, false);
        FB(g, f, "AsaTetera", -0.1f, 0.89f, -0.05f, 0.015f, 0.08f, 0.025f, fPorcelain, false);
        for (int k = 0; k < 3; k++)
        {
            float px = -0.02f + k * 0.14f, pz = 0.1f - (k % 2) * 0.18f;
            FEll(g, f, "Platito", px, 0.804f, pz, 0.11f, 0.008f, 0.11f, por);
            FP(g, f, "Taza", px, 0.808f, pz, 0.06f, 0.05f, 0.06f, fPorcelain, false);
            FB(g, f, "AsaTaza", px + 0.04f, 0.833f, pz, 0.012f, 0.03f, 0.018f, fPorcelain, false);
        }
        FEll(g, f, "Azucarera", 0.25f, 0.84f, -0.12f, 0.08f, 0.07f, 0.08f, por);
        FEll(g, f, "Plato", 0.2f, 0.326f, 0f, 0.2f, 0.01f, 0.2f, por);
        for (int k = 0; k < 3; k++) FBall(g, f, "Galleta", 0.2f + (k - 1) * 0.05f, 0.345f, (k % 2) * 0.03f, 0.045f, MatOf(C(new Color(0.7f, 0.5f, 0.25f), 0.1f)));
        FP(g, f, "Servilletas", -0.2f, 0.32f, 0f, 0.18f, 0.03f, 0.18f, fLinen, false);
        return g;
    }

    /// <summary>Round pedestal side table (0.5 dia x 0.7) with a 1990s rotary telephone on top; facing = where the dial faces.</summary>
    static GameObject SideTablePhone(float cx, float y0, float cz, Vector3 facing)
    {
        Fx();
        var g = Group("MesaTelefono");
        var f = new Frame(cx, y0, cz, facing);
        FEll(g, f, "Base", 0f, 0.025f, 0f, 0.36f, 0.05f, 0.36f, MatOf(sDark));
        for (int k = 0; k < 3; k++)
        {
            float rad = (k * 120f + 60f) * Mathf.Deg2Rad;
            FBall(g, f, "Pie", Mathf.Sin(rad) * 0.16f, 0.03f, Mathf.Cos(rad) * 0.16f, 0.06f, MatOf(sDark));
        }
        FP(g, f, "Columna", 0f, 0.05f, 0f, 0.07f, 0.6f, 0.07f, sDark, false);
        FBall(g, f, "Nudo", 0f, 0.35f, 0f, 0.12f, MatOf(sDark));
        FEll(g, f, "Tapa", 0f, 0.68f, 0f, 0.5f, 0.04f, 0.5f, MatOf(sDark));
        FHit(g, f, "Hitbox", 0f, 0.35f, 0f, 0.45f, 0.7f, 0.45f);
        Surf bake = C(new Color(0.04f, 0.04f, 0.045f), 0.85f);
        FRot(g, f, "Telefono", 0f, 0.74f, 0.03f, 0.2f, 0.08f, 0.17f, new Vector3(-15f, 0f, 0f), bake, false);
        FRot(g, f, "Disco", 0f, 0.766f, 0.075f, 0.1f, 0.006f, 0.1f, new Vector3(-15f, 0f, 0f), C(new Color(0.85f, 0.85f, 0.8f), 0.5f), false);
        FRotm(g, f, "AroDisco", 0f, 0.77f, 0.076f, 0.11f, 0.004f, 0.11f, new Vector3(-15f, 0f, 0f), mChrome, false);
        for (int k = 0; k < 10; k++)
        {
            float a = (k * 36f + 18f) * Mathf.Deg2Rad;
            Vector3 p = Quaternion.Euler(-15f, 0f, 0f) * new Vector3(Mathf.Cos(a) * 0.035f, 0.006f, Mathf.Sin(a) * 0.035f);
            FBall(g, f, "Agujero", p.x, 0.766f + p.y, 0.075f + p.z, 0.014f, MatOf(bake));
        }
        FB(g, f, "Horquilla", -0.07f, 0.805f, -0.03f, 0.03f, 0.04f, 0.04f, bake, false);
        FB(g, f, "Horquilla", 0.07f, 0.805f, -0.03f, 0.03f, 0.04f, 0.04f, bake, false);
        FB(g, f, "Auricular", 0f, 0.84f, -0.03f, 0.22f, 0.03f, 0.04f, bake, false);
        FEll(g, f, "Auricular", -0.11f, 0.83f, -0.03f, 0.055f, 0.06f, 0.055f, MatOf(bake));
        FEll(g, f, "Auricular", 0.11f, 0.83f, -0.03f, 0.055f, 0.06f, 0.055f, MatOf(bake));
        for (int k = 0; k < 4; k++) FRot(g, f, "Cable", 0.11f + k * 0.012f, 0.76f - k * 0.02f, 0.03f + (k % 2) * 0.02f, 0.012f, 0.05f, 0.012f, new Vector3(20f, 0f, 30f), bake, false);
        BookFlat(g, f, -0.12f, 0.7f, -0.1f, 0.12f, 0.02f, 0.16f, new Color(0.1f, 0.1f, 0.3f), 30f);
        return g;
    }

    /// <summary>Round wooden wall clock with pendulum box, stopped at 3:00 like every clock in the house. (cx, cy, cz) on the wall.</summary>
    static GameObject WallClock(float cx, float cy, float cz, Vector3 facing)
    {
        Fx();
        var g = Group("RelojPared");
        var f = new Frame(cx, cy, cz, facing);
        FEll(g, f, "Caja", 0f, 0f, 0.03f, 0.38f, 0.38f, 0.06f, MatOf(sDark));
        FEll(g, f, "Aro", 0f, 0f, 0.056f, 0.33f, 0.33f, 0.012f, mBrass);
        FQuad(g, f, "Caratula", 0f, 0f, 0.063f, 0.29f, 0.29f, new Vector3(0f, 0f, 1f), mDial);
        FP(g, f, "CajaPendulo", 0f, -0.5f, 0.03f, 0.16f, 0.36f, 0.06f, sDark, false);
        FB(g, f, "VentanaPendulo", 0f, -0.33f, 0.06f, 0.1f, 0.26f, 0.004f, C(new Color(0.08f, 0.05f, 0.03f), 0.3f), false);
        FBm(g, f, "Varilla", 0f, -0.3f, 0.062f, 0.008f, 0.22f, 0.004f, mBrass, false);
        FEll(g, f, "Pendulo", 0f, -0.42f, 0.062f, 0.07f, 0.07f, 0.008f, mBrass);
        FB(g, f, "Remate", 0f, 0.22f, 0.03f, 0.08f, 0.06f, 0.05f, sDark, false);
        FBall(g, f, "Bola", 0f, 0.27f, 0.03f, 0.05f, MatOf(sDark));
        return g;
    }

    /// <summary>Leather steamer trunk (w x 0.5 x 0.55): straps, brass corner caps, latches, lock, end handles, feet.</summary>
    static GameObject Trunk(float cx, float y0, float cz, Vector3 facing, float w)
    {
        Fx();
        var g = Group("Baul");
        var f = new Frame(cx, y0, cz, facing);
        float hw = w / 2f;
        Surf strap = C(new Color(0.12f, 0.08f, 0.05f), 0.3f);
        for (int i = 0; i < 4; i++)
            FP(g, f, "Pie", (i % 2 == 0 ? -1f : 1f) * (hw - 0.06f), 0f, (i < 2 ? -1f : 1f) * 0.19f, 0.06f, 0.04f, 0.06f, fIron, false);
        FP(g, f, "Baul", 0f, 0.04f, 0f, w, 0.36f, 0.5f, fLeather);
        FP(g, f, "TapaBaul", 0f, 0.4f, 0f, w + 0.02f, 0.1f, 0.52f, fLeather);
        FP(g, f, "Cupula", 0f, 0.5f, 0f, w - 0.1f, 0.05f, 0.42f, fLeather, false);
        for (int k = 0; k < 3; k++)
        {
            float x = -hw + 0.12f + k * (hw - 0.12f);
            FB(g, f, "Correa", x, 0.3f, 0.258f, 0.06f, 0.52f, 0.01f, strap, false);
            FB(g, f, "Correa", x, 0.3f, -0.258f, 0.06f, 0.52f, 0.01f, strap, false);
            FB(g, f, "Correa", x, 0.555f, 0f, 0.06f, 0.012f, 0.44f, strap, false);
        }
        for (int i = 0; i < 8; i++)
            FBm(g, f, "Cantonera", (i % 2 == 0 ? -1f : 1f) * (hw - 0.02f), (i < 4 ? 0.07f : 0.47f), ((i / 2) % 2 == 0 ? -1f : 1f) * 0.23f, 0.07f, 0.07f, 0.07f, mBrass, false);
        FBm(g, f, "Cerrojo", -0.2f, 0.4f, 0.262f, 0.06f, 0.09f, 0.02f, mBrass, false);
        FBm(g, f, "Cerrojo", 0.2f, 0.4f, 0.262f, 0.06f, 0.09f, 0.02f, mBrass, false);
        FBm(g, f, "Cerradura", 0f, 0.38f, 0.262f, 0.08f, 0.1f, 0.02f, mBrass, false);
        FB(g, f, "Ojo", 0f, 0.37f, 0.273f, 0.012f, 0.025f, 0.004f, C(Color.black, 0.2f), false);
        FB(g, f, "AsaBaul", -hw - 0.015f, 0.25f, 0f, 0.03f, 0.04f, 0.16f, strap, false);
        FB(g, f, "AsaBaul", hw + 0.015f, 0.25f, 0f, 0.03f, 0.04f, 0.16f, strap, false);
        return g;
    }

    /// <summary>Banjo barometer on the wall: mahogany case, ivory dial with a brass needle, thermometer strip, finial.</summary>
    static GameObject Barometer(float cx, float cy, float cz, Vector3 facing)
    {
        Fx();
        var g = Group("Barometro");
        var f = new Frame(cx, cy, cz, facing);
        FEll(g, f, "Cuerpo", 0f, 0f, 0.03f, 0.26f, 0.26f, 0.05f, MatOf(sDark));
        FP(g, f, "Cuello", 0f, 0.12f, 0.03f, 0.1f, 0.33f, 0.04f, sDark, false);
        FEll(g, f, "Cabeza", 0f, 0.5f, 0.03f, 0.14f, 0.16f, 0.04f, MatOf(sDark));
        FEll(g, f, "Esfera", 0f, 0f, 0.056f, 0.2f, 0.2f, 0.01f, MatOf(fIvory));
        FEll(g, f, "AroEsfera", 0f, 0f, 0.055f, 0.22f, 0.22f, 0.008f, mBrass);
        FRotm(g, f, "Aguja", 0f, 0f, 0.063f, 0.012f, 0.16f, 0.003f, new Vector3(0f, 0f, 35f), mBrass, false);
        FBall(g, f, "Centro", 0f, 0f, 0.063f, 0.02f, mBrass);
        FB(g, f, "Termometro", 0f, 0.15f, 0.052f, 0.04f, 0.26f, 0.004f, fIvory, false);
        FB(g, f, "Mercurio", 0f, 0.12f, 0.055f, 0.006f, 0.16f, 0.002f, C(new Color(0.8f, 0.1f, 0.1f), 0.5f), false);
        FEll(g, f, "Higrometro", 0f, 0.5f, 0.052f, 0.09f, 0.09f, 0.006f, MatOf(fIvory));
        FBall(g, f, "Remate", 0f, 0.6f, 0.03f, 0.05f, mBrass);
        return g;
    }
}
