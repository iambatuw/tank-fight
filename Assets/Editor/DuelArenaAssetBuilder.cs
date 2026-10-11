using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Original, baked low-poly art. No mesh generation or decorative colliders at runtime.
public static class DuelArenaAssetBuilder
{
    public const string Folder = "Assets/ArenaArt/Prefabs/";
    private const string Meshes = "Assets/ArenaArt/Meshes/";
    private static GameObject root;
    private static Material sand, sandstone, pale, steel, yellow, blue, red, snow, ice, glass, wood;

    public static void Build()
    {
        Directory.CreateDirectory(Folder);
        Directory.CreateDirectory(Meshes);
        AssetDatabase.Refresh();
        sand = Mat("Ochre", "BE8050"); sandstone = Mat("Terracotta", "935335");
        pale = Mat("Limestone", "D6AD77"); steel = Mat("Steel", "344750");
        yellow = Mat("SafetyGold", "E2AD45"); blue = Mat("OceanBlue", "467E8D");
        red = Mat("CargoRed", "B75943"); snow = Mat("Snow", "E0ECEA");
        ice = Mat("Glacier", "7FBDC9"); glass = Mat("Window", "275665");
        wood = Mat("Wood", "A78054");
        Begin("CanyonMesa");
        Facet("Basalt foot", Vector3.zero, new Vector3(8, 2, 6), sandstone, 11, 0);
        Facet("Sediment band", new Vector3(.15f, 1.5f, 0), new Vector3(7.6f, 1.3f, 5.7f), pale, 11, 1);
        Facet("Upper sandstone", new Vector3(-.3f, 2.5f, .15f), new Vector3(6.7f, 2.2f, 5.1f), sand, 11, 2);
        Facet("Eroded crown", new Vector3(-.4f, 4.2f, .1f), new Vector3(5.8f, .65f, 4.4f), pale, 11, 3);
        ColliderBox(new Vector3(0, 2.4f, 0), new Vector3(7.2f, 4.8f, 5.3f)); Save();
        Begin("CanyonBoulder");
        Facet("Boulder", Vector3.zero, new Vector3(4, 2.7f, 3.5f), sand, 7, 2);
        Facet("Chip", new Vector3(1.5f, 0, .5f), new Vector3(1.3f, .9f, 1.1f), pale, 6, 5);
        ColliderBox(new Vector3(0, 1.1f, 0), new Vector3(3.6f, 2.2f, 3)); Save();
        Begin("StoneArch");
        foreach (int sign in new[] { -1, 1 })
        {
            Facet("Arch pillar", new Vector3(sign * 4, 0, 0), new Vector3(3, 5.5f, 3), sandstone, 7, 4);
            ColliderBox(new Vector3(sign * 4, 2.5f, 0), new Vector3(3, 5, 3));
        }
        for (int i = -2; i <= 2; i++)
            Facet("Arch crown", new Vector3(i * 1.7f, 5.1f + (2 - Mathf.Abs(i)) * .35f, 0), new Vector3(2.3f, 1.3f, 3), pale, 7, i + 7);
        ColliderBox(new Vector3(0, 6, 0), new Vector3(9.5f, 2, 3)); Save();
        foreach (bool warm in new[] { true, false })
        {
            Begin(warm ? "CargoContainerRed" : "CargoContainerBlue");
            var body = warm ? red : blue;
            Part("Shell", new Vector3(0, 1.5f, 0), new Vector3(10, 3, 4.4f), body);
            foreach (int side in new[] { -1, 1 })
            {
                for (int i = -9; i <= 9; i++) Part("Pressed rib", new Vector3(i * .49f, 1.5f, side * 2.24f), new Vector3(.12f, 2.75f, .12f), body);
                foreach (float y in new[] { .13f, 2.9f }) Part("Corner rail", new Vector3(0, y, side * 2.25f), new Vector3(10.1f, .18f, .18f), steel);
                foreach (int end in new[] { -1, 1 }) Part("Corner casting", new Vector3(end * 4.94f, 1.5f, side * 2.2f), new Vector3(.16f, 3.1f, .18f), steel);
                Part("Door", new Vector3(side * 5.02f, 1.5f, 0), new Vector3(.08f, 2.65f, 4.1f), body);
                foreach (float z in new[] { -1.2f, 1.2f }) Part("Door lock", new Vector3(side * 5.1f, 1.5f, z), new Vector3(.1f, 2.6f, .1f), pale);
                Part("Serial plate", new Vector3(-2, 1.9f, side * 2.33f), new Vector3(1.4f, .45f, .04f), snow);
            }
            ColliderBox(new Vector3(0, 1.55f, 0), new Vector3(10.2f, 3.1f, 4.6f)); Save();
        }
        Begin("CargoCrate");
        Part("Crate", new Vector3(0, 1.1f, 0), new Vector3(2.4f, 2.2f, 2.4f), wood);
        foreach (float x in new[] { -.85f, .85f }) Part("Metal strap", new Vector3(x, 1.1f, 0), new Vector3(.15f, 2.3f, 2.5f), steel);
        foreach (int side in new[] { -1, 1 }) Beam("Brace", new Vector3(-1, .2f, side * 1.22f), new Vector3(1, 2, side * 1.22f), .18f, pale);
        ColliderBox(new Vector3(0, 1.15f, 0), new Vector3(2.4f, 2.3f, 2.5f)); Save();
        Begin("DockCrane");
        Part("Foundation", new Vector3(0, .5f, 0), new Vector3(5, 1, 5), steel);
        foreach (float x in new[] { -1.5f, 1.5f }) foreach (float z in new[] { -1.5f, 1.5f })
            Beam("Tower leg", new Vector3(x, 1, z), new Vector3(x * .5f, 11, z * .5f), .3f, yellow);
        for (int y = 1; y < 10; y += 2)
            foreach (int side in new[] { -1, 1 }) Beam("Tower cross brace", new Vector3(-1.35f, y, side), new Vector3(1.35f, y + 2, side), .16f, yellow);
        foreach (float side in new[] { -.5f, .5f })
        {
            Beam("Jib chord", new Vector3(side, 11, -9), new Vector3(side, 11, 4), .25f, yellow);
            Beam("Jib chord upper", new Vector3(side, 12.5f, -9), new Vector3(side, 12.5f, 4), .2f, yellow);
            for (int z = -9; z < 4; z += 2) Beam("Jib web", new Vector3(side, 11, z), new Vector3(side, 12.5f, z + 2), .15f, yellow);
        }
        Part("Counterweight", new Vector3(0, 11, 3), new Vector3(2.4f, 2, 2.5f), steel);
        Part("Cab", new Vector3(0, 10, -1.5f), new Vector3(2.4f, 2, 2), yellow);
        Part("Cab windshield", new Vector3(0, 10.2f, -2.52f), new Vector3(1.9f, 1, .06f), glass);
        Beam("Hoist", new Vector3(0, 11, -7), new Vector3(0, 4, -7), .09f, steel);
        Part("Hook", new Vector3(0, 4, -7), new Vector3(.5f, .7f, .4f), yellow);
        ColliderBox(new Vector3(0, 5.5f, 0), new Vector3(3.1f, 11, 3.1f)); Save();
        Begin("IceCluster");
        Facet("Glacier face", Vector3.zero, new Vector3(5, 3.8f, 4), ice, 7, 12);
        Facet("Snow crust", new Vector3(-.1f, 3.1f, .1f), new Vector3(3.7f, .7f, 3), snow, 7, 12);
        Facet("Ice shard", new Vector3(2.3f, 0, .3f), new Vector3(1.8f, 2, 1.7f), ice, 5, 5);
        ColliderBox(new Vector3(0, 1.5f, 0), new Vector3(4.5f, 3, 3.5f)); Save();
        Begin("PolarStation");
        Part("Raised base", new Vector3(0, .25f, 0), new Vector3(8, .5f, 9), steel);
        Part("Insulated cabin", new Vector3(0, 2, 0), new Vector3(7.6f, 3.5f, 8.6f), snow);
        Part("Roof", new Vector3(0, 4, 0), new Vector3(8.2f, .5f, 9.2f), steel);
        Part("Snow roof", new Vector3(0, 4.3f, 0), new Vector3(8.1f, .15f, 9.1f), snow);
        foreach (int side in new[] { -1, 1 })
        {
            Part("Blue stripe", new Vector3(side * 3.82f, 1, 0), new Vector3(.08f, .65f, 8.5f), blue);
            foreach (int z in new[] { -2, 0, 2 }) Part("Window", new Vector3(side * 3.85f, 2.6f, z), new Vector3(.12f, 1.15f, 1.4f), glass);
            Part("Entrance", new Vector3(0, 1.7f, side * 4.33f), new Vector3(1.7f, 2.8f, .15f), blue);
            Part("Door window", new Vector3(0, 2.3f, side * 4.43f), new Vector3(1, .7f, .06f), glass);
            Part("Door light", new Vector3(0, 3.4f, side * 4.4f), new Vector3(1.1f, .18f, .25f), yellow);
        }
        Part("Roof vent", new Vector3(2.2f, 4.6f, 2.2f), new Vector3(1.5f, .6f, 2), blue);
        for (int z = 0; z < 5; z++) Part("Vent slat", new Vector3(2.2f, 4.92f, 1.5f + z * .35f), new Vector3(1.3f, .05f, .13f), steel);
        ColliderBox(new Vector3(0, 2.4f, 0), new Vector3(8.2f, 4.8f, 9.2f)); Save();
        Begin("PolarRadar");
        Part("Foot", new Vector3(0, .25f, 0), new Vector3(3, .5f, 3), steel);
        foreach (int side in new[] { -1, 1 }) Beam("Support", new Vector3(side, .5f, 0), new Vector3(0, 4.8f, 0), .3f, blue);
        Facet("Radome", new Vector3(0, 4, 0), new Vector3(3.8f, 3.4f, 3.8f), snow, 10, 0);
        Part("Radome equator", new Vector3(0, 5.1f, 0), new Vector3(3.4f, .16f, 3.4f), blue);
        Beam("Antenna", new Vector3(0, 7, 0), new Vector3(0, 8.4f, 0), .12f, steel);
        ColliderBox(new Vector3(0, 2, 0), new Vector3(2.6f, 4, 2.6f)); Save();
        AssetDatabase.SaveAssets();
    }

    private static Material Mat(string name, string hex)
    {
        string path = "Assets/ArenaArt/Art_" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
        ColorUtility.TryParseHtmlString("#" + hex, out var color);
        material.color = QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color;
        material.SetFloat("_Smoothness", .08f); material.enableInstancing = true;
        EditorUtility.SetDirty(material); return material;
    }
    private static void Begin(string name) { root = new GameObject(name); }
    private static void ColliderBox(Vector3 center, Vector3 size)
    { var c = root.AddComponent<BoxCollider>(); c.center = center; c.size = size; }
    private static GameObject Part(string name, Vector3 position, Vector3 size, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
        go.transform.SetParent(root.transform, false); go.transform.localPosition = position; go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
    }
    private static void Beam(string name, Vector3 a, Vector3 b, float width, Material material)
    {
        var go = Part(name, (a + b) / 2, new Vector3(width, (b - a).magnitude, width), material);
        go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
    }
    private static void Facet(string name, Vector3 position, Vector3 size, Material material, int sides, int seed)
    {
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        var random = new System.Random(seed); var rings = new Vector3[4, sides];
        float[] radius = { .85f, 1, .78f, .5f }; float[] heights = { 0, .2f, .78f, 1 };
        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.PI * 2 / sides; float irregular = .86f + (float)random.NextDouble() * .22f;
            for (int r = 0; r < 4; r++) rings[r, i] = Vector3.Scale(new Vector3(Mathf.Cos(angle) * radius[r] * irregular * .5f, heights[r], Mathf.Sin(angle) * radius[r] * irregular * .5f), size);
        }
        Action<Vector3, Vector3, Vector3> tri = (a, b, c) => { int n = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c); triangles.Add(n); triangles.Add(n + 1); triangles.Add(n + 2); };
        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides;
            for (int r = 0; r < 3; r++) { tri(rings[r, i], rings[r + 1, i], rings[r + 1, next]); tri(rings[r, i], rings[r + 1, next], rings[r, next]); }
            tri(Vector3.up * size.y, rings[3, next], rings[3, i]);
            tri(Vector3.zero, rings[0, i], rings[0, next]);
        }
        var mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(root.transform, false); go.transform.localPosition = position;
        go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = material;
    }
    private static void Save()
    {
        // Merge by material: dozens of decorative ribs become a handful of draw calls.
        var filters = root.GetComponentsInChildren<MeshFilter>();
        var transientMeshes = filters.Select(f => f.sharedMesh).Where(m => !EditorUtility.IsPersistent(m)).Distinct().ToArray();
        var materials = filters.Select(f => f.GetComponent<Renderer>().sharedMaterial).Distinct().ToArray();
        for (int i = 0; i < materials.Length; i++)
        {
            var instances = filters.Where(f => f.GetComponent<Renderer>().sharedMaterial == materials[i]).Select(f => new CombineInstance { mesh = f.sharedMesh, transform = f.transform.localToWorldMatrix }).ToArray();
            var mesh = new Mesh { name = root.name + "_" + i }; mesh.CombineMeshes(instances, true, true);
            string path = Meshes + mesh.name + ".asset"; var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) AssetDatabase.CreateAsset(mesh, path); else { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); mesh = existing; }
            var go = new GameObject(materials[i].name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(root.transform, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<Renderer>().sharedMaterial = materials[i]; go.isStatic = true;
        }
        foreach (var filter in filters) UnityEngine.Object.DestroyImmediate(filter.gameObject);
        foreach (var mesh in transientMeshes) UnityEngine.Object.DestroyImmediate(mesh);
        root.isStatic = true; PrefabUtility.SaveAsPrefabAsset(root, Folder + root.name + ".prefab"); UnityEngine.Object.DestroyImmediate(root);
    }
}
