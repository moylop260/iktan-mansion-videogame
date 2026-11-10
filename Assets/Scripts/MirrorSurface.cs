using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Espejo plano real: una cámara de reflejo se coloca donde estaría la cámara del jugador al otro lado del cristal,
/// renderiza a una textura con plano de recorte oblicuo, y el cristal la muestra en coordenadas de pantalla.
/// Solo renderiza cuando el espejo está cerca y al frente del jugador. Los reflejos muestran al avatar del jugador
/// (capa 8); el espejo "embrujado" muestra al avatar retrasado (capa 9).
/// </summary>
public class MirrorSurface : MonoBehaviour
{
    public const int AvatarLayer = 8, GhostLayer = 9;

    public Vector3 normal = Vector3.forward;   // hacia el cuarto
    public bool haunted;
    public Camera source;
    public float maxDistance = 14f;

    Camera rc;
    RenderTexture rt;
    Material mat;

    public Camera ReflectionCamera { get { return rc; } }

    public void Setup(Material baseMat, Camera main, Vector3 normalIntoRoom, bool isHaunted)
    {
        source = main; normal = normalIntoRoom.normalized; haunted = isHaunted;
        mat = new Material(baseMat);
        GetComponent<Renderer>().sharedMaterial = mat;
    }

    void Build()
    {
        var go = new GameObject("CamaraReflejo");
        go.transform.SetParent(transform, false);
        rc = go.AddComponent<Camera>();
        rc.clearFlags = CameraClearFlags.SolidColor;
        rc.backgroundColor = new Color(0.02f, 0.02f, 0.03f);
        rc.cullingMask = haunted ? ~(1 << AvatarLayer) : ~(1 << GhostLayer);
        rc.enabled = false;
        var ud = go.AddComponent<UniversalAdditionalCameraData>();
        ud.renderPostProcessing = false;
        ud.renderShadows = true;
    }

    void EnsureTexture()
    {
        int w = Mathf.Clamp(Screen.width / 2, 256, 1280), h = Mathf.Clamp(Screen.height / 2, 144, 720);
        if (rt != null && rt.width == w && rt.height == h) return;
        if (rt != null) { rc.targetTexture = null; rt.Release(); Destroy(rt); }
        rt = new RenderTexture(w, h, 16, RenderTextureFormat.Default) { name = "Reflejo" };
        rt.Create();
        rc.targetTexture = rt;
        mat.SetTexture("_MainTex", rt);
    }

    static Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 n)
    {
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cpos = m.MultiplyPoint(pos);
        Vector3 cn = m.MultiplyVector(n).normalized;
        return new Vector4(cn.x, cn.y, cn.z, -Vector3.Dot(cpos, cn));
    }

    void LateUpdate()
    {
        if (source == null) return;
        if (rc == null) Build();

        Vector3 c = source.transform.position, p = transform.position;
        float side = Vector3.Dot(c - p, normal);
        Vector3 to = p - c;
        bool visible = side > 0.05f && to.magnitude < maxDistance && Vector3.Dot(to.normalized, source.transform.forward) > 0.15f;
        rc.enabled = visible;
        if (!visible) return;

        EnsureTexture();
        // la cámara de reflejo: posición reflejada, dirección y arriba reflejados
        Vector3 rp = c - 2f * side * normal;
        Vector3 rf = Vector3.Reflect(source.transform.forward, normal);
        Vector3 ru = Vector3.Reflect(source.transform.up, normal);
        rc.transform.SetPositionAndRotation(rp, Quaternion.LookRotation(rf, ru));
        rc.fieldOfView = source.fieldOfView;
        rc.aspect = source.aspect;
        rc.nearClipPlane = source.nearClipPlane;
        rc.farClipPlane = source.farClipPlane;
        rc.depth = source.depth - 5f;
        rc.ResetProjectionMatrix();
        rc.projectionMatrix = rc.CalculateObliqueMatrix(CameraSpacePlane(rc, p - normal * 0.03f, normal));   // el plano de recorte queda un poco detrás del cristal
    }

    void OnDestroy()
    {
        if (rt != null) { rt.Release(); Destroy(rt); }
        if (mat != null) Destroy(mat);
    }
}
