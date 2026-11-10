using UnityEngine;

/// <summary>Personaje en primera persona (POV): WASD + ratón, Shift para correr.</summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public Transform camT;
    CharacterController cc;
    float yaw, pitch, vy, bob;

    void Awake() { cc = GetComponent<CharacterController>(); }

    public void Teleport(Vector3 pos, float yawDeg)
    {
        cc.enabled = false;
        transform.position = pos;
        yaw = yawDeg; pitch = 0f; vy = 0f;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (camT != null) camT.localRotation = Quaternion.identity;
        cc.enabled = true;
    }

    void Update()
    {
        var gm = GameManager.I;
        if (gm == null || gm.mode != Mode.Playing) return;

        Vector2 look = Inp.Look();
        yaw += look.x;
        pitch = Mathf.Clamp(pitch - look.y, -80f, 80f);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        camT.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        Vector2 m = Inp.Move();
        Vector3 dir = Vector3.ClampMagnitude(transform.right * m.x + transform.forward * m.y, 1f);
        float speed = Inp.Run() ? 5.2f : 3.0f;
        Vector3 vel = dir * speed;

        if (cc.isGrounded && vy < 0f) vy = -2f;
        vy -= 18f * Time.deltaTime;
        vel.y = vy;
        cc.Move(vel * Time.deltaTime);

        float moving = m.magnitude > 0.1f ? 1f : 0f;
        bob += Time.deltaTime * speed * 2.2f * moving;
        camT.localPosition = new Vector3(0f, 1.65f + Mathf.Sin(bob) * 0.035f * moving, 0f);
    }
}
