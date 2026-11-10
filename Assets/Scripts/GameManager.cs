using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public enum Mode { Title, Playing, Choice, Paused, GameOver, Win, Ended, Inventory, Death }

[Serializable]
public class SaveData
{
    public string checkpoint;
    public float px, py, pz, yaw;
    public List<string> items = new List<string>();
    public List<string> doors = new List<string>();
    public int deaths;
}

public class CheckpointInfo
{
    public string name;
    public Vector3 lampPos, spawn;
    public float yaw;
    public bool inside;
    public Renderer rend;
    public Light light;
}

public class Zone { public string name; public float x0, x1, z0, z1, y0; }

public static class Bootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (GameManager.I == null) new GameObject("GameManager").AddComponent<GameManager>();
    }
}

/// <summary>Estado del juego, checkpoints, game over, menús e interfaz (IMGUI).</summary>
public class GameManager : MonoBehaviour
{
    public static GameManager I;
    const string KEY = "mansion_save_v1";
    const float CheckpointRadius = 1.6f;

    public Mode mode = Mode.Title;
    public readonly List<string> items = new List<string>();
    public readonly HashSet<string> doors = new HashSet<string>();
    public readonly Dictionary<string, Door> doorMap = new Dictionary<string, Door>();
    public readonly List<CheckpointInfo> checkpoints = new List<CheckpointInfo>();
    public readonly List<Zone> zones = new List<Zone>();
    public Vector3 startPos = new Vector3(12f, 0.05f, 2.2f);

    PlayerController player;
    Transform camT;
    Light flash;
    GameObject worldRoot;
    SaveData last;
    int deaths;
    string currentCp = "";
    string msg = ""; float msgUntil;
    string chTitle = "", chBody = ""; List<Opt> chOpts = new List<Opt>(); bool chCancel;
    string endText = "", deathText = ""; float deathTime;
    Interactable hover;
    public AudioManager audio;
    public Camera cam;
    Cinema cinema;
    // capas de pantalla para las escenas de muerte
    public Color fadeColor = Color.black, flashColor = Color.white;
    public float fadeAlpha, flashAlpha, staticAlpha;
    Texture2D[] noiseTex;
    Vignette vg; ChromaticAberration ca; LensDistortion lens;
    float fxPulse;
    Light moon;
    static readonly Color AmbientDefault = new Color(0.10f, 0.10f, 0.14f);
    int chSel, invSel;
    string zoneName = "", bannerText = ""; float bannerStart = -10f;
    string popupItem = ""; float popupStart = -10f;
    Font serif;

    static readonly Dictionary<string, string> ItemDesc = new Dictionary<string, string>
    {
        { "Linterna", "Una linterna vieja. Con pilas, ilumina los lugares oscuros (tecla F)." },
        { "Pilas", "Dos pilas casi nuevas. Quizá sirvan para una linterna." },
        { "Llave verde", "Una llave con cinta verde. Abre la biblioteca." },
        { "Llave azul", "Una llave de cabeza azul. Abre la reja de la escalera." },
        { "Partituras", "Partituras de piano escritas a mano. Las firma «J.»." },
        { "Llave amarilla", "Una llave amarilla escondida en el piano. Abre la oficina." },
        { "Llave dorada", "La llave de la puerta principal. ¡Es la salida!" },
    };

    // ---------------------------------------------------------------- ciclo de vida
    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None)) Destroy(c.gameObject);
        foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) Destroy(l.gameObject);

        Application.runInBackground = false;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = AmbientDefault;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.025f;
        RenderSettings.fogColor = new Color(0.03f, 0.03f, 0.05f);
        RenderSettings.skybox = null;

        moon = new GameObject("Luna").AddComponent<Light>();
        moon.type = LightType.Directional; moon.intensity = 0.25f; moon.color = new Color(0.55f, 0.65f, 1f);
        moon.transform.rotation = Quaternion.Euler(50f, 30f, 0f);

        serif = Font.CreateDynamicFontFromOSFont("Times New Roman", 32);
        BuildPlayer();
        audio = gameObject.AddComponent<AudioManager>();
        audio.Init();
        BuildPostFx();
        cinema = gameObject.AddComponent<Cinema>();
        cinema.Setup(this, player, cam);
        RebuildWorld();
        player.Teleport(startPos, 0f);
        mode = Mode.Title;
    }

    void BuildPlayer()
    {
        var p = new GameObject("Jugador");
        p.layer = 2; // Ignore Raycast
        var cc = p.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = 0.3f; cc.center = new Vector3(0f, 0.9f, 0f);
        cc.stepOffset = 0.35f; cc.slopeLimit = 50f; cc.skinWidth = 0.05f;

        var cam = new GameObject("Camara");
        cam.transform.SetParent(p.transform, false);
        cam.transform.localPosition = new Vector3(0f, 1.65f, 0f);
        cam.tag = "MainCamera";
        var c = cam.AddComponent<Camera>();
        this.cam = c;
        c.fieldOfView = 72f; c.nearClipPlane = 0.05f; c.farClipPlane = 120f;
        c.clearFlags = CameraClearFlags.SolidColor; c.backgroundColor = new Color(0.02f, 0.02f, 0.03f);
        cam.AddComponent<AudioListener>();
        var ud = cam.AddComponent<UniversalAdditionalCameraData>();
        ud.renderPostProcessing = true;
        ud.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        camT = cam.transform;

        var fl = new GameObject("Linterna");
        fl.transform.SetParent(cam.transform, false);
        flash = fl.AddComponent<Light>();
        flash.type = LightType.Spot; flash.range = 20f; flash.spotAngle = 60f;
        flash.intensity = 5f; flash.color = new Color(1f, 0.95f, 0.8f); flash.shadows = LightShadows.Soft; flash.enabled = false;

        player = p.AddComponent<PlayerController>();
        player.camT = camT;
    }

    void BuildPostFx()
    {
        var go = new GameObject("Efectos");
        var vol = go.AddComponent<Volume>();
        vol.isGlobal = true;
        var prof = ScriptableObject.CreateInstance<VolumeProfile>();
        vol.profile = prof;
        vg = prof.Add<Vignette>(true); vg.intensity.Override(0.42f); vg.smoothness.Override(0.45f);
        ca = prof.Add<ChromaticAberration>(true); ca.intensity.Override(0f);
        lens = prof.Add<LensDistortion>(true); lens.intensity.Override(0f);
        var gr = prof.Add<FilmGrain>(true); gr.type.Override(FilmGrainLookup.Thin1); gr.intensity.Override(0.35f);
        var colorAdj = prof.Add<ColorAdjustments>(true); colorAdj.saturation.Override(-18f); colorAdj.contrast.Override(14f); colorAdj.postExposure.Override(0.35f);
        var bl = prof.Add<Bloom>(true); bl.threshold.Override(0.9f); bl.intensity.Override(0.55f); bl.scatter.Override(0.6f);
        var tm = prof.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.ACES);
    }

    void RebuildWorld()
    {
        if (worldRoot != null) { worldRoot.SetActive(false); Destroy(worldRoot); }
        doorMap.Clear(); checkpoints.Clear(); zones.Clear();
        worldRoot = new GameObject("Mansion");
        MansionBuilder.Build(worldRoot.transform, this);
        foreach (var id in doors.ToList()) { Door d; if (doorMap.TryGetValue(id, out d)) d.Open(true); }
        SetLamps();
    }

    // ---------------------------------------------------------------- API para el mundo
    public bool Has(string item) { return items.Contains(item); }
    public void Give(string item)
    {
        if (items.Contains(item)) return;
        items.Add(item);
        popupItem = item; popupStart = Time.time;
        if (audio != null) audio.Sfx("pickup", 0.7f);
    }
    public bool FlashReady { get { return Has("Linterna") && Has("Pilas"); } }
    public bool FlashOn { get { return flash != null && flash.enabled; } }

    public void Say(string t) { msg = t; msgUntil = Time.time + Mathf.Max(3.5f, t.Length * 0.06f); }

    /// <summary>Muerte animada: kind elige la escena (closet, wardrobe, fridge, mirror, bed, tub, pit, tv, doll, alarm).</summary>
    public void Die(string kind, GameObject focus)
    {
        if (mode == Mode.Death || mode == Mode.GameOver) return;
        mode = Mode.Death; deaths++;
        if (flash != null) flash.enabled = false;
        cinema.Play(kind, focus);
    }

    public void Win(string t)
    {
        mode = Mode.Win; endText = t;
        audio.Sfx("win", 0.8f);
        PlayerPrefs.DeleteKey(KEY); last = null;
    }

    public void ShowChoice(string title, string body, List<Opt> opts, bool cancellable)
    {
        chTitle = title; chBody = body; chOpts = opts; chCancel = cancellable; chSel = 0; mode = Mode.Choice;
    }

    public void SetLamps()
    {
        foreach (var cp in checkpoints)
        {
            bool active = cp.name == currentCp;
            Color c = active ? new Color(0.3f, 1f, 0.4f) : new Color(0.35f, 0.6f, 1f);
            if (cp.rend != null) { cp.rend.material.color = c; cp.rend.material.SetColor("_EmissionColor", c * 1.5f); }
            if (cp.light != null) cp.light.color = c;
        }
    }

    // ---------------------------------------------------------------- guardado
    bool HasSave { get { return PlayerPrefs.HasKey(KEY); } }

    void Persist()
    {
        if (last == null) return;
        PlayerPrefs.SetString(KEY, JsonUtility.ToJson(last));
        PlayerPrefs.Save();
    }

    void SaveCheckpoint(CheckpointInfo cp)
    {
        last = new SaveData
        {
            checkpoint = cp.name, px = cp.spawn.x, py = cp.spawn.y, pz = cp.spawn.z, yaw = cp.yaw,
            items = items.ToList(), doors = doors.ToList(), deaths = deaths
        };
        currentCp = cp.name;
        Persist(); SetLamps();
        audio.Sfx("checkpoint", 0.5f);
        Say("Checkpoint guardado: " + cp.name);
    }

    void SyncInside(Vector3 pos)
    {
        foreach (var cp in checkpoints)
            cp.inside = Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(cp.lampPos.x, cp.lampPos.z)) < CheckpointRadius
                        && Mathf.Abs(pos.y - cp.lampPos.y) < 2f;
    }

    void LoadSave(SaveData d, bool restoreDeaths)
    {
        Time.timeScale = 1f;
        ResetFx();
        items.Clear(); foreach (var i in d.items) if (!items.Contains(i)) items.Add(i);
        doors.Clear(); foreach (var i in d.doors) doors.Add(i);
        if (restoreDeaths) deaths = d.deaths;
        currentCp = d.checkpoint; last = d;
        RebuildWorld();
        var pos = new Vector3(d.px, d.py, d.pz);
        player.Teleport(pos, d.yaw);
        flash.enabled = false;
        SyncInside(pos);
        zoneName = "";
        mode = Mode.Playing;
        Say("Reintentando desde: " + d.checkpoint);
    }

    public void Retry()
    {
        if (last != null) LoadSave(last, false); else NewGame();
    }

    public void Continue()
    {
        if (!HasSave) { NewGame(); return; }
        LoadSave(JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(KEY)), true);
    }

    public void NewGame()
    {
        Time.timeScale = 1f;
        ResetFx();
        PlayerPrefs.DeleteKey(KEY);
        items.Clear(); doors.Clear(); deaths = 0; currentCp = ""; last = null; zoneName = "";
        RebuildWorld();
        player.Teleport(startPos, 0f);
        flash.enabled = false;
        SyncInside(startPos);
        ShowChoice("LA MANSIÓN",
            "Una mansión enorme, vieja y muy barata. El vendedor te mira en silencio mientras te entrega las llaves...\n¿La compras?",
            new List<Opt>
            {
                new Opt("Comprar la mansión", BeginGame),
                new Opt("No comprar", () =>
                {
                    endText = "FINAL 1: No compraste la mansión.\n\nVives una vida tranquila... aunque a veces te preguntas qué habría pasado.";
                    mode = Mode.Ended;
                })
            }, false);
    }

    void BeginGame()
    {
        last = new SaveData { checkpoint = "Entrada", px = startPos.x, py = startPos.y, pz = startPos.z, yaw = 0f };
        currentCp = "Entrada"; Persist();
        mode = Mode.Playing;
        Say("Entras a la mansión y la puerta se cierra detrás de ti. Busca cómo salir... y ten cuidado con lo que abres. (H = pista)");
    }

    void ToTitle() { Time.timeScale = 1f; ResetFx(); mode = Mode.Title; }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------------------------------------------------------------- efectos de pantalla
    /// <summary>Golpe visual (viñeta, aberración cromática, distorsión) que se desvanece solo.</summary>
    public void Pulse(float v) { fxPulse = Mathf.Max(fxPulse, v); }

    /// <summary>Apaga (o enciende) todas las luces de la mansión y la luz ambiente.</summary>
    public void SetDark(bool dark)
    {
        RenderSettings.ambientLight = dark ? new Color(0.008f, 0.008f, 0.012f) : AmbientDefault;
        if (moon != null) moon.enabled = !dark;
        if (worldRoot != null) foreach (var l in worldRoot.GetComponentsInChildren<Light>(true)) l.enabled = !dark;
    }

    public void EndDeath() { mode = Mode.GameOver; deathTime = Time.unscaledTime; }

    void ResetFx()
    {
        fadeAlpha = 0f; flashAlpha = 0f; staticAlpha = 0f; fxPulse = 0f;
        SetDark(false);
        audio.SetHeart(0f); audio.HeartSpeed(1f);
        if (cam != null) cam.fieldOfView = 72f;
        if (cinema != null) cinema.Cleanup();
    }

    string Hint()
    {
        if (Has("Llave dorada")) return "Tienes la llave dorada: ¡sal por la puerta principal!";
        if (Has("Llave amarilla")) return "La llave amarilla abre la oficina del segundo piso. La caja fuerte guarda la salida.";
        if (Has("Partituras")) return "Busca un piano en el segundo piso... y lleva tu linterna encendida.";
        if (doors.Contains("reja")) return "Ya subiste. Busca unas partituras en la cómoda de un cuarto del segundo piso.";
        if (Has("Llave azul")) return "La llave azul abre la reja de la escalera, en el vestíbulo.";
        if (Has("Llave verde")) return "La llave verde abre la biblioteca. Cuidado con los libros.";
        if (!FlashReady) return "Revisa cajones y mesitas: necesitas luz. Y algo brilla en el baño.";
        return "Revisa el baño. Algo brilla en el lavabo.";
    }

    // ---------------------------------------------------------------- actualización
    void Update()
    {
        bool lockCursor = mode == Mode.Playing || mode == Mode.Death;
        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockCursor;

        float amb = 0.3f;
        switch (mode)
        {
            case Mode.Title: amb = 0.25f; break;
            case Mode.Paused: amb = 0.08f; break;
            case Mode.GameOver: amb = 0.05f; break;
            case Mode.Win:
            case Mode.Ended: amb = 0.1f; break;
        }
        fxPulse = Mathf.MoveTowards(fxPulse, 0f, Time.unscaledDeltaTime * 1.2f);
        if (vg != null) { vg.intensity.value = 0.42f + 0.3f * fxPulse; ca.intensity.value = fxPulse; lens.intensity.value = -0.35f * fxPulse; }
        audio.SetAmbient(amb);
        audio.Tick(mode == Mode.Playing, player.transform.position);

        switch (mode)
        {
            case Mode.Playing: UpdatePlaying(); break;
            case Mode.Choice:
                if (chCancel && Inp.Pause()) { mode = Mode.Playing; break; }
                chSel = Mathf.Clamp(chSel + Inp.MenuMove(), 0, chOpts.Count - 1);
                if (Inp.Confirm() || Inp.Interact()) { Pick(chSel); break; }
                for (int i = 0; i < chOpts.Count && i < 9; i++) if (Inp.Digit(i + 1)) { Pick(i); break; }
                break;
            case Mode.Inventory:
                if (Inp.Inventory() || Inp.Pause()) { mode = Mode.Playing; break; }
                if (items.Count > 0)
                {
                    invSel = Mathf.Clamp(invSel + Inp.MenuSide() + 4 * Inp.MenuMove(), 0, items.Count - 1);
                }
                break;
            case Mode.Paused:
                if (Inp.Pause()) { Time.timeScale = 1f; mode = Mode.Playing; }
                break;
            case Mode.GameOver:
                if (Time.unscaledTime - deathTime > 0.6f)
                {
                    if (Inp.Retry() || Inp.Confirm()) Retry();
                    else if (Inp.NewKey()) NewGame();
                }
                break;
            case Mode.Win:
            case Mode.Ended:
                if (Inp.Confirm()) ToTitle();
                break;
            case Mode.Title:
                if (Inp.Confirm()) { if (HasSave) Continue(); else NewGame(); }
                break;
        }
    }

    void Pick(int i)
    {
        var o = chOpts[i];
        mode = Mode.Playing;
        if (o.run != null) o.run();
    }

    void UpdatePlaying()
    {
        if (Inp.Pause()) { Time.timeScale = 0f; mode = Mode.Paused; return; }
        if (Inp.Inventory()) { invSel = 0; mode = Mode.Inventory; return; }
        if (Inp.Hint()) Say(Hint());
        if (Inp.Flashlight())
        {
            if (FlashReady) flash.enabled = !flash.enabled;
            else Say(Has("Linterna") ? "La linterna no tiene pilas." : "No tienes una linterna.");
        }

        Vector3 pp = player.transform.position;
        foreach (var cp in checkpoints)
        {
            bool near = Vector2.Distance(new Vector2(pp.x, pp.z), new Vector2(cp.lampPos.x, cp.lampPos.z)) < CheckpointRadius
                        && Mathf.Abs(pp.y - cp.lampPos.y) < 2f;
            if (near && !cp.inside) SaveCheckpoint(cp);
            cp.inside = near;
        }

        string zn = "";
        foreach (var z in zones)
            if (pp.x > z.x0 && pp.x < z.x1 && pp.z > z.z0 && pp.z < z.z1 && pp.y > z.y0 - 0.5f && pp.y < z.y0 + 3.4f) { zn = z.name; break; }
        if (zn != zoneName) { zoneName = zn; if (zn != "") { bannerText = zn; bannerStart = Time.time; } }

        hover = null;
        RaycastHit h;
        if (Physics.Raycast(new Ray(camT.position, camT.forward), out h, 2.8f, ~(1 << 2), QueryTriggerInteraction.Ignore))
        {
            var it = h.collider.GetComponentInParent<Interactable>();
            if (it != null && it.enabled) hover = it;
        }
        if (hover != null && Inp.Interact()) Interact(hover);
    }

    void Interact(Interactable it)
    {
        var opts = it.options();
        if (opts.Count == 1 && string.IsNullOrEmpty(it.description)) { if (opts[0].run != null) opts[0].run(); }
        else ShowChoice(it.title, it.description, opts, true);
    }

    // ---------------------------------------------------------------- interfaz
    GUIStyle sLabel, sTitle, sBtn, sSmall;

    void EnsureStyles()
    {
        if (sLabel != null) return;
        sLabel = new GUIStyle(GUI.skin.label) { fontSize = 22, wordWrap = true, alignment = TextAnchor.MiddleCenter };
        sLabel.normal.textColor = Color.white;
        sTitle = new GUIStyle(sLabel) { fontSize = 64, fontStyle = FontStyle.Bold };
        sSmall = new GUIStyle(sLabel) { fontSize = 16, alignment = TextAnchor.MiddleLeft };
        sBtn = new GUIStyle(GUI.skin.button) { fontSize = 24 };
        sSerif = new GUIStyle(sLabel);
        if (serif != null) sSerif.font = serif;
    }

    GUIStyle sSerif;
    static readonly Color Gold = new Color(0.85f, 0.68f, 0.28f);

    GUIStyle Serif(int size, TextAnchor a = TextAnchor.MiddleCenter, Color? c = null)
    {
        var st = new GUIStyle(sSerif) { fontSize = size, alignment = a, wordWrap = true };
        st.normal.textColor = c ?? Color.white;
        return st;
    }

    static void Frame(Rect r, float a = 0.9f)
    {
        Rect2(r, new Color(0.03f, 0.02f, 0.02f, a));
        Rect2(new Rect(r.x, r.y, r.width, 2f), Gold);
        Rect2(new Rect(r.x, r.yMax - 2f, r.width, 2f), Gold);
        Rect2(new Rect(r.x, r.y, 2f, r.height), Gold);
        Rect2(new Rect(r.xMax - 2f, r.y, 2f, r.height), Gold);
    }

    void Shadowed(Rect r, string text, GUIStyle st)
    {
        var o = new GUIStyle(st); o.normal.textColor = new Color(0, 0, 0, 0.85f);
        GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), text, o);
        GUI.Label(r, text, st);
    }

    static void Rect2(Rect r, Color c)
    {
        var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old;
    }

    void OnGUI()
    {
        EnsureStyles();
        float s = Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
        float W = Screen.width / s, H = 720f;
        switch (mode)
        {
            case Mode.Playing: DrawHud(W, H); break;
            case Mode.Title: DrawTitle(W, H); break;
            case Mode.Choice: DrawHud(W, H); DrawChoice(W, H); break;
            case Mode.Inventory: DrawInventory(W, H); break;
            case Mode.Paused: DrawPause(W, H); break;
            case Mode.GameOver: DrawGameOver(W, H); break;
            case Mode.Win: DrawEnd(W, H, new Color(0f, 0.15f, 0.05f, 0.92f), "¡ESCAPASTE!", "Volver al menú (Enter)"); break;
            case Mode.Ended: DrawEnd(W, H, new Color(0f, 0f, 0f, 0.92f), "FIN", "Volver al menú (Enter)"); break;
        }
        DrawOverlays(W, H);
    }

    void DrawOverlays(float W, float H)
    {
        if (staticAlpha > 0.01f)
        {
            if (noiseTex == null)
            {
                noiseTex = new Texture2D[4];
                for (int k = 0; k < 4; k++)
                {
                    var t = new Texture2D(96, 96, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                    var px = new Color[96 * 96];
                    for (int i = 0; i < px.Length; i++) { float v = UnityEngine.Random.value; px[i] = new Color(v, v, v, 1f); }
                    t.SetPixels(px); t.Apply(); noiseTex[k] = t;
                }
            }
            var old = GUI.color; GUI.color = new Color(1f, 1f, 1f, staticAlpha);
            GUI.DrawTexture(new Rect(0, 0, W, H), noiseTex[(int)(Time.unscaledTime * 24f) % 4]);
            GUI.color = old;
        }
        if (flashAlpha > 0.01f) Rect2(new Rect(0, 0, W, H), new Color(flashColor.r, flashColor.g, flashColor.b, flashAlpha));
        if (fadeAlpha > 0.01f) Rect2(new Rect(0, 0, W, H), new Color(fadeColor.r, fadeColor.g, fadeColor.b, fadeAlpha));
    }

    void DrawHud(float W, float H)
    {
        Rect2(new Rect(W / 2f - 2f, H / 2f - 2f, 4f, 4f), new Color(1f, 1f, 1f, 0.7f));
        if (mode == Mode.Playing && hover != null)
        {
            Rect2(new Rect(W / 2f - 12f, H / 2f - 12f, 24f, 24f), new Color(0.85f, 0.68f, 0.28f, 0.0f));
            Frame(new Rect(W / 2f - 230f, H * 0.64f, 460f, 44f), 0.7f);
            GUI.Label(new Rect(W / 2f - 230f, H * 0.64f, 460f, 44f), "E   " + hover.title, Serif(24, TextAnchor.MiddleCenter, Gold));
        }
        // nombre del cuarto (estilo survival horror)
        float bt = Time.time - bannerStart;
        if (bt >= 0f && bt < 3.2f && bannerText != "")
        {
            float a = Mathf.Clamp01(bt / 0.5f) * Mathf.Clamp01((3.2f - bt) / 0.9f);
            var c = new Color(1f, 0.9f, 0.65f, a);
            Rect2(new Rect(W / 2f - 260f, H * 0.78f, 520f, 1.5f), new Color(Gold.r, Gold.g, Gold.b, a));
            Shadowed(new Rect(0, H * 0.78f + 6f, W, 64f), bannerText.ToUpper(), Serif(46, TextAnchor.MiddleCenter, c));
            Rect2(new Rect(W / 2f - 260f, H * 0.78f + 70f, 520f, 1.5f), new Color(Gold.r, Gold.g, Gold.b, a));
        }
        // objeto obtenido
        float pt = Time.time - popupStart;
        if (popupItem != "" && pt >= 0f && pt < 4f)
        {
            float a = Mathf.Clamp01(pt / 0.3f) * Mathf.Clamp01((4f - pt) / 0.6f);
            var old = GUI.color; GUI.color = new Color(1f, 1f, 1f, a);
            Frame(new Rect(W / 2f - 250f, 36f, 500f, 100f), 0.85f * a);
            var ic = TexGen.Icon(popupItem);
            if (ic != null) GUI.DrawTexture(new Rect(W / 2f - 236f, 46f, 80f, 80f), ic, ScaleMode.ScaleToFit, true);
            GUI.Label(new Rect(W / 2f - 140f, 44f, 380f, 30f), "Obtuviste", Serif(18, TextAnchor.MiddleLeft, new Color(0.8f, 0.8f, 0.8f, a)));
            GUI.Label(new Rect(W / 2f - 140f, 70f, 380f, 50f), popupItem, Serif(34, TextAnchor.MiddleLeft, new Color(Gold.r, Gold.g, Gold.b, a)));
            GUI.color = old;
        }
        if (Time.time < msgUntil && !string.IsNullOrEmpty(msg))
        {
            float top = (popupItem != "" && pt < 4f) ? 150f : 36f;
            Frame(new Rect(W / 2f - 400f, top, 800f, 96f), 0.8f);
            GUI.Label(new Rect(W / 2f - 385f, top, 770f, 96f), msg, Serif(22));
        }
        Rect2(new Rect(10f, H - 40f, 330f, 30f), new Color(0f, 0f, 0f, 0.4f));
        GUI.Label(new Rect(16f, H - 40f, 320f, 30f), "Tab: inventario (" + items.Count + ")  ·  Muertes: " + deaths, sSmall);
        string help = "E interactuar · F linterna" + (FlashReady ? " (lista)" : "") + " · H pista · Esc pausa";
        GUI.Label(new Rect(W - 520f, H - 40f, 510f, 30f), help, new GUIStyle(sSmall) { alignment = TextAnchor.MiddleRight });
        if (currentCp != "") GUI.Label(new Rect(16f, 10f, 400f, 26f), "Checkpoint: " + currentCp, sSmall);
    }

    void DrawInventory(float W, float H)
    {
        Rect2(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.82f));
        Frame(new Rect(W / 2f - 480f, 90f, 960f, 540f), 0.95f);
        GUI.Label(new Rect(W / 2f - 480f, 98f, 960f, 50f), "INVENTARIO", Serif(36, TextAnchor.MiddleCenter, Gold));
        const int cols = 4;
        for (int i = 0; i < 8; i++)
        {
            var r = new Rect(W / 2f - 450f + (i % cols) * 120f, 170f + (i / cols) * 120f, 110f, 110f);
            Frame(r, 0.9f);
            if (i == invSel && items.Count > 0) Rect2(new Rect(r.x + 3, r.y + 3, r.width - 6, r.height - 6), new Color(0.85f, 0.68f, 0.28f, 0.25f));
            if (i < items.Count)
            {
                var ic = TexGen.Icon(items[i]);
                if (ic != null) GUI.DrawTexture(new Rect(r.x + 10, r.y + 10, 90, 90), ic, ScaleMode.ScaleToFit, true);
                if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition)) invSel = i;
            }
        }
        var d = new Rect(W / 2f + 50f, 170f, 410f, 330f);
        Frame(d, 0.9f);
        if (items.Count == 0)
        {
            GUI.Label(d, "No llevas nada todavía.\nExplora la mansión.", Serif(24));
        }
        else
        {
            string it = items[Mathf.Clamp(invSel, 0, items.Count - 1)];
            var ic = TexGen.Icon(it);
            if (ic != null) GUI.DrawTexture(new Rect(d.x + d.width / 2f - 70f, d.y + 14f, 140f, 140f), ic, ScaleMode.ScaleToFit, true);
            GUI.Label(new Rect(d.x + 10f, d.y + 160f, d.width - 20f, 44f), it, Serif(32, TextAnchor.MiddleCenter, Gold));
            string desc; if (!ItemDesc.TryGetValue(it, out desc)) desc = "";
            GUI.Label(new Rect(d.x + 20f, d.y + 208f, d.width - 40f, 110f), desc, Serif(21, TextAnchor.UpperCenter));
        }
        GUI.Label(new Rect(W / 2f - 480f, 540f, 960f, 40f), "Hint: " + Hint(), Serif(20, TextAnchor.MiddleCenter, new Color(0.8f, 0.8f, 0.8f)));
        GUI.Label(new Rect(W / 2f - 480f, 585f, 960f, 30f), "Flechas / WASD: elegir  ·  Tab o Esc: cerrar", Serif(16, TextAnchor.MiddleCenter, new Color(0.65f, 0.65f, 0.65f)));
    }

    void DrawTitle(float W, float H)
    {
        Rect2(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.75f));
        Shadowed(new Rect(0, 110f, W, 100f), "LA MANSIÓN", Serif(84, TextAnchor.MiddleCenter, Gold));
        GUI.Label(new Rect(0, 210f, W, 40f), "Una idea original de Iktan", sLabel);
        float y = 300f;
        if (HasSave && GUI.Button(new Rect(W / 2f - 180f, y, 360f, 52f), "Continuar", sBtn)) Continue();
        if (HasSave) y += 66f;
        if (GUI.Button(new Rect(W / 2f - 180f, y, 360f, 52f), "Nuevo juego", sBtn)) NewGame();
        y += 66f;
        if (GUI.Button(new Rect(W / 2f - 180f, y, 360f, 52f), "Salir", sBtn)) Quit();
        GUI.Label(new Rect(W / 2f - 380f, H - 110f, 760f, 80f),
            "WASD mover · Ratón mirar · Shift correr · E interactuar · F linterna · H pista · Esc pausa\nCada checkpoint (lámpara azul) guarda tu avance. Si mueres, vuelves al último.",
            new GUIStyle(sLabel) { fontSize = 16 });
    }

    void DrawChoice(float W, float H)
    {
        float h = 120f + (string.IsNullOrEmpty(chBody) ? 0f : 54f) + chOpts.Count * 46f;
        var box = new Rect(W / 2f - 460f, H - h - 50f, 920f, h);
        Frame(box, 0.88f);
        GUI.Label(new Rect(box.x + 24f, box.y + 8f, box.width - 48f, 44f), chTitle, Serif(32, TextAnchor.MiddleLeft, Gold));
        float y = box.y + 54f;
        if (!string.IsNullOrEmpty(chBody))
        {
            GUI.Label(new Rect(box.x + 24f, y, box.width - 48f, 50f), chBody, Serif(21, TextAnchor.UpperLeft));
            y += 54f;
        }
        for (int i = 0; i < chOpts.Count; i++)
        {
            var r = new Rect(box.x + 24f, y, box.width - 48f, 40f);
            var e = Event.current;
            if (e.type == EventType.MouseMove && r.Contains(e.mousePosition)) chSel = i;
            if (e.type == EventType.MouseDown && r.Contains(e.mousePosition)) { Pick(i); e.Use(); return; }
            bool sel = i == chSel;
            if (sel) Rect2(r, new Color(0.85f, 0.68f, 0.28f, 0.22f));
            GUI.Label(r, (sel ? ">  " : "    ") + (i + 1) + ".  " + chOpts[i].label, Serif(26, TextAnchor.MiddleLeft, sel ? new Color(1f, 0.9f, 0.55f) : new Color(0.85f, 0.85f, 0.85f)));
            y += 46f;
        }
        if (chCancel) GUI.Label(new Rect(box.xMax - 260f, box.yMax - 28f, 250f, 24f), "Esc: alejarse", Serif(15, TextAnchor.MiddleRight, new Color(0.6f, 0.6f, 0.6f)));
    }

    void DrawPause(float W, float H)
    {
        Rect2(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.8f));
        GUI.Label(new Rect(0, 130f, W, 70f), "PAUSA", new GUIStyle(sTitle) { fontSize = 48 });
        float y = 240f;
        if (GUI.Button(new Rect(W / 2f - 220f, y, 440f, 50f), "Continuar (Esc)", sBtn)) { Time.timeScale = 1f; mode = Mode.Playing; return; }
        y += 62f;
        if (GUI.Button(new Rect(W / 2f - 220f, y, 440f, 50f), "Reintentar desde el checkpoint", sBtn)) { Retry(); return; }
        y += 62f;
        if (GUI.Button(new Rect(W / 2f - 220f, y, 440f, 50f), "Reiniciar desde cero", sBtn)) { NewGame(); return; }
        y += 62f;
        if (GUI.Button(new Rect(W / 2f - 220f, y, 440f, 50f), "Menú principal", sBtn)) { ToTitle(); return; }
        y += 62f;
        if (GUI.Button(new Rect(W / 2f - 220f, y, 440f, 50f), "Salir del juego", sBtn)) Quit();
    }

    void DrawGameOver(float W, float H)
    {
        Rect2(new Rect(0, 0, W, H), new Color(0.25f, 0f, 0f, 0.9f));
        GUI.Label(new Rect(0, 150f, W, 100f), "GAME OVER", Serif(84, TextAnchor.MiddleCenter, new Color(0.8f, 0.05f, 0.05f)));
        GUI.Label(new Rect(0, 330f, W, 30f), "Último checkpoint: " + (currentCp == "" ? "inicio" : currentCp) + "   ·   Muertes: " + deaths, sLabel);
        if (GUI.Button(new Rect(W / 2f - 270f, 450f, 540f, 54f), "Reintentar desde el checkpoint (R)", sBtn)) Retry();
        else if (GUI.Button(new Rect(W / 2f - 270f, 516f, 540f, 54f), "Empezar de cero (N)", sBtn)) NewGame();
    }

    void DrawEnd(float W, float H, Color bg, string head, string btn)
    {
        Rect2(new Rect(0, 0, W, H), bg);
        GUI.Label(new Rect(0, 130f, W, 90f), head, new GUIStyle(sTitle) { fontSize = 66 });
        GUI.Label(new Rect(W / 2f - 380f, 240f, 760f, 200f), endText, new GUIStyle(sLabel) { fontSize = 24 });
        if (mode == Mode.Win) GUI.Label(new Rect(0, 450f, W, 30f), "Muertes en total: " + deaths, sLabel);
        if (GUI.Button(new Rect(W / 2f - 220f, 500f, 440f, 54f), btn, sBtn)) ToTitle();
    }
}
