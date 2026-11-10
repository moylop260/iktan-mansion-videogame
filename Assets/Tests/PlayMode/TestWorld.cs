using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Utilidades compartidas: arranca un GameManager propio y busca objetos de la mansión por nombre.</summary>
public static class TestWorld
{
    public const string SaveKey = "mansion_save_v1";

    public static GameManager Boot()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        Time.timeScale = 1f;
        Cleanup();   // el juego crea su propio GameManager al arrancar: se reemplaza por uno limpio
        var go = new GameObject("GM");
        return go.AddComponent<GameManager>();
    }

    /// <summary>Entra al juego como lo hace el jugador: nueva partida y "Comprar la mansión".</summary>
    public static GameManager BootAndStart()
    {
        var gm = Boot();
        gm.NewGame();
        gm.PickOption(0);
        return gm;
    }

    public static void Cleanup()
    {
        Time.timeScale = 1f;
        PlayerPrefs.DeleteKey(SaveKey);
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name.StartsWith("Code-based")) continue;
            Object.DestroyImmediate(root);
        }
    }

    public static GameObject World() { return GameObject.Find("Mansion"); }

    public static GameObject Prop(string name)
    {
        var t = World().transform.Find(name);
        return t != null ? t.gameObject : null;
    }

    /// <summary>Usa un objeto como el jugador (tecla E) eligiendo la opción dada.</summary>
    public static void Use(GameObject go, int option = 0)
    {
        Assert.IsNotNull(go, "the prop does not exist");
        var it = go.GetComponent<Interactable>();
        Assert.IsNotNull(it, go.name + " has no Interactable");
        it.options()[option].run();
    }

    /// <summary>Abre un contenedor (cajón, cómoda...) si está cerrado y toma lo que hay dentro, como el jugador.</summary>
    public static void TakeFrom(string propName)
    {
        var go = Prop(propName);
        Assert.IsNotNull(go, propName + " does not exist");
        var it = go.GetComponent<Interactable>();
        Assert.IsNotNull(it, propName + " has no Interactable");
        var o = it.options();
        if (o[0].label.StartsWith("Abrir")) o[0].run();
        o = it.options();
        var take = o.Find(x => x.label.StartsWith("Tomar"));
        Assert.IsNotNull(take, propName + " has nothing to take");
        take.run();
    }

    public static PlayerController Player() { return Object.FindFirstObjectByType<PlayerController>(); }
}
