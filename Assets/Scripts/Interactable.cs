using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Una opción que el jugador puede elegir al interactuar.</summary>
public class Opt
{
    public string label;
    public Action run;
    public Opt(string label, Action run) { this.label = label; this.run = run; }
}

/// <summary>Objeto con el que se puede interactuar (tecla E). Las opciones se calculan al momento de usarlo.</summary>
public class Interactable : MonoBehaviour
{
    public string title = "Objeto";
    public string description = "";
    public Func<List<Opt>> options;

    public static Interactable Add(GameObject go, string title, Func<List<Opt>> opts, string desc = "")
    {
        var i = go.AddComponent<Interactable>();
        i.title = title; i.options = opts; i.description = desc;
        return i;
    }
}
