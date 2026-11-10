using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class DataTests
{
    [Test]
    public void SaveData_round_trips_through_json()
    {
        var d = new SaveData
        {
            checkpoint = "Segundo piso", px = 11.5f, py = 3.35f, pz = 6f, yaw = 90f, deaths = 3,
            items = new List<string> { "Linterna", "Pilas", "Llave verde" },
            doors = new List<string> { "reja", "biblioteca" }
        };
        var back = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(d));
        Assert.AreEqual(d.checkpoint, back.checkpoint);
        Assert.AreEqual(d.px, back.px); Assert.AreEqual(d.py, back.py); Assert.AreEqual(d.pz, back.pz);
        Assert.AreEqual(d.yaw, back.yaw); Assert.AreEqual(3, back.deaths);
        CollectionAssert.AreEqual(d.items, back.items);
        CollectionAssert.AreEqual(d.doors, back.doors);
    }

    [Test]
    public void SaveData_defaults_are_empty_lists_not_null()
    {
        var d = new SaveData();
        Assert.IsNotNull(d.items); Assert.IsNotNull(d.doors);
        Assert.AreEqual(0, d.deaths);
    }

    [Test]
    public void Opt_keeps_its_label_and_runs_its_action()
    {
        int n = 0;
        var o = new Opt("Abrir", () => n++);
        Assert.AreEqual("Abrir", o.label);
        o.run();
        Assert.AreEqual(1, n);
    }

    [Test]
    public void Interactable_computes_its_options_every_time_it_is_asked()
    {
        var go = new GameObject("prop");
        try
        {
            bool has = false;
            var it = Interactable.Add(go, "Cajón", () => new List<Opt> { new Opt(has ? "Vacío" : "Abrir", null) }, "desc");
            Assert.AreEqual("Cajón", it.title);
            Assert.AreEqual("desc", it.description);
            Assert.AreEqual("Abrir", it.options()[0].label);
            has = true;
            Assert.AreEqual("Vacío", it.options()[0].label);
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void CheckpointInfo_and_Zone_are_plain_data()
    {
        var cp = new CheckpointInfo { name = "Vestíbulo", lampPos = Vector3.one, spawn = Vector3.up, yaw = 90f };
        Assert.AreEqual("Vestíbulo", cp.name); Assert.IsFalse(cp.inside);
        var z = new Zone { name = "Sala", x0 = 0, x1 = 8, z0 = 0, z1 = 8, y0 = 0 };
        Assert.AreEqual(8f, z.x1);
    }
}
