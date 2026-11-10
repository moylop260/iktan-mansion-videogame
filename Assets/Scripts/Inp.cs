using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Entrada de teclado/ratón que funciona con el Input System nuevo o con el antiguo.</summary>
public static class Inp
{
#if ENABLE_INPUT_SYSTEM
    static bool Held(Key k) { var kb = Keyboard.current; return kb != null && kb[k].isPressed; }
    static bool Down(Key k) { var kb = Keyboard.current; return kb != null && kb[k].wasPressedThisFrame; }

    public static Vector2 Look() { var m = Mouse.current; return m == null ? Vector2.zero : m.delta.ReadValue() * 0.09f; }
    public static Vector2 Move()
    {
        float x = ((Held(Key.D) || Held(Key.RightArrow)) ? 1f : 0f) - ((Held(Key.A) || Held(Key.LeftArrow)) ? 1f : 0f);
        float y = ((Held(Key.W) || Held(Key.UpArrow)) ? 1f : 0f) - ((Held(Key.S) || Held(Key.DownArrow)) ? 1f : 0f);
        return new Vector2(x, y);
    }
    public static bool Run() { return Held(Key.LeftShift) || Held(Key.RightShift); }
    public static bool Interact() { return Down(Key.E); }
    public static bool Flashlight() { return Down(Key.F); }
    public static bool Hint() { return Down(Key.H); }
    public static bool Pause() { return Down(Key.Escape) || Down(Key.P); }
    public static bool Confirm() { return Down(Key.Enter) || Down(Key.NumpadEnter) || Down(Key.Space); }
    public static bool Retry() { return Down(Key.R); }
    public static bool NewKey() { return Down(Key.N); }
    public static bool Inventory() { return Down(Key.Tab) || Down(Key.I); }
    public static int MenuMove() { return (Down(Key.S) || Down(Key.DownArrow) ? 1 : 0) - (Down(Key.W) || Down(Key.UpArrow) ? 1 : 0); }
    public static int MenuSide() { return (Down(Key.D) || Down(Key.RightArrow) ? 1 : 0) - (Down(Key.A) || Down(Key.LeftArrow) ? 1 : 0); }
    public static bool Digit(int n) { return Down((Key)((int)Key.Digit1 + n - 1)); }
#else
    static bool Held(KeyCode k) { return Input.GetKey(k); }
    static bool Down(KeyCode k) { return Input.GetKeyDown(k); }

    public static Vector2 Look() { return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 2f; }
    public static Vector2 Move()
    {
        float x = ((Held(KeyCode.D) || Held(KeyCode.RightArrow)) ? 1f : 0f) - ((Held(KeyCode.A) || Held(KeyCode.LeftArrow)) ? 1f : 0f);
        float y = ((Held(KeyCode.W) || Held(KeyCode.UpArrow)) ? 1f : 0f) - ((Held(KeyCode.S) || Held(KeyCode.DownArrow)) ? 1f : 0f);
        return new Vector2(x, y);
    }
    public static bool Run() { return Held(KeyCode.LeftShift) || Held(KeyCode.RightShift); }
    public static bool Interact() { return Down(KeyCode.E); }
    public static bool Flashlight() { return Down(KeyCode.F); }
    public static bool Hint() { return Down(KeyCode.H); }
    public static bool Pause() { return Down(KeyCode.Escape) || Down(KeyCode.P); }
    public static bool Confirm() { return Down(KeyCode.Return) || Down(KeyCode.KeypadEnter) || Down(KeyCode.Space); }
    public static bool Retry() { return Down(KeyCode.R); }
    public static bool NewKey() { return Down(KeyCode.N); }
    public static bool Inventory() { return Down(KeyCode.Tab) || Down(KeyCode.I); }
    public static int MenuMove() { return (Down(KeyCode.S) || Down(KeyCode.DownArrow) ? 1 : 0) - (Down(KeyCode.W) || Down(KeyCode.UpArrow) ? 1 : 0); }
    public static int MenuSide() { return (Down(KeyCode.D) || Down(KeyCode.RightArrow) ? 1 : 0) - (Down(KeyCode.A) || Down(KeyCode.LeftArrow) ? 1 : 0); }
    public static bool Digit(int n) { return Down(KeyCode.Alpha1 + n - 1); }
#endif
}
