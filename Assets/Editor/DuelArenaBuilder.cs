using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tanks.Complete;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

// Deterministic layouts: rebuilding never moves cover or changes a player's advantage.
public static class DuelArenaBuilder
{
    private const string Art = "Assets/ArenaArt";
    private static Transform environment;
    private static Material ground, rock, trim, dark, accent, road, white;

    [MenuItem("Tank Duel/Build New Arenas")]
    public static void BuildAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play before building arenas.");
        var original = SceneManager.GetActiveScene();
        if (original.isDirty) throw new InvalidOperationException("Save the open scene first.");
        string originalPath = original.path;
        Directory.CreateDirectory(Art);
        AssetDatabase.Refresh();
        try
        {
            DuelArenaAssetBuilder.Build();
            for (int theme = 0; theme < 3; theme++) Build(theme);
            EditorBuildSettings.scenes = DuelArenaCatalog.Scenes.Select(s =>
                new EditorBuildSettingsScene(DuelArenaCatalog.ScenePath(s), true)).ToArray();
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (!string.IsNullOrEmpty(originalPath)) EditorSceneManager.OpenScene(originalPath);
        }
    }

    private static void Build(int theme)
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Duel_Jungle.unity");
        foreach (var root in scene.GetRootGameObjects())
            if (root.name.StartsWith("Level") || root.name == "EnhancedEnvironment")
                UnityEngine.Object.DestroyImmediate(root);
        environment = new GameObject("Designed Arena").transform;
        var surface = environment.gameObject.AddComponent<NavMeshSurface>();
        surface.agentTypeID = -1372625422;
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.overrideVoxelSize = true;
        surface.voxelSize = 0.2f;

        string name = DuelArenaCatalog.Scenes[theme + 3];
        ground = Mat(name + "_Ground", theme == 0 ? "BD8E58" : theme == 1 ? "576B70" : "D4E7E9");
        rock = Mat(name + "_Stone", theme == 0 ? "A76640" : theme == 1 ? "34474C" : "8CACBC");
        trim = Mat(name + "_Trim", theme == 0 ? "E2B477" : theme == 1 ? "839694" : "EFF7F5");
        dark = Mat(name + "_Dark", theme == 0 ? "654D37" : "253C48");
        accent = Mat(name + "_Accent", theme == 0 ? "779371" : theme == 1 ? "E5AA43" : "39AFB8");
        road = Mat(name + "_Road", theme == 0 ? "D2AA76" : theme == 1 ? "3D4F55" : "B7D1DA");
        white = Mat("Shared_White", "DDE3D6");

        Box("Foundation", new Vector3(0, -2.5f, 0), new Vector3(70, 4, 58), dark, true);
        Box("Arena ground", new Vector3(0, -0.25f, 0), new Vector3(66, 0.5f, 54), ground, true);
        Box("Surroundings", new Vector3(0, -3, 0), new Vector3(200, 0.2f, 200),
            theme == 1 ? Mat("Harbor_Sea", "255966") : ground, false);
        // Continuous collision boundaries; decorative gaps never let a tank leave the floor.
        Cover("West boundary", -33, 0, 2, 56, 1.2f, rock);
        Cover("East boundary", 33, 0, 2, 56, 1.2f, rock);
        Cover("North boundary", 0, 27, 64, 2, 1.2f, rock);
        Cover("South boundary", 0, -27, 64, 2, 1.2f, rock);

        if (theme == 0) Canyon();
        else if (theme == 1) Harbor();
        else Arctic();
        GroundDetails(theme, name);

        var manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var starts = new[] { new Vector3(-25, 0, -14), new Vector3(25, 0, 14),
            new Vector3(-25, 0, 14), new Vector3(25, 0, -14) };
        for (int i = 0; i < manager.m_SpawnPoints.Length; i++)
        {
            var spawn = manager.m_SpawnPoints[i].m_SpawnPoint;
            spawn.position = starts[i];
            spawn.rotation = Quaternion.LookRotation(-starts[i].normalized, Vector3.up);
            Box("Spawn pad " + (i + 1), starts[i] + Vector3.up * 0.025f,
                new Vector3(6, 0.03f, 6), road, false);
            Box("Spawn stripe " + (i + 1), starts[i] + new Vector3(0, 0.05f, -2.5f),
                new Vector3(5, 0.02f, 0.22f), white, false);
        }
        var pickupPositions = new[] { new Vector3(-20, 0, 0), new Vector3(20, 0, 0),
            new Vector3(0, 0, -19), new Vector3(0, 0, 19) };
        var spawners = UnityEngine.Object.FindObjectsByType<PowerUpSpawner>(FindObjectsInactive.Include)
            .OrderBy(s => s.name).ThenBy(s => s.transform.GetSiblingIndex()).ToArray();
        if (spawners.Length != 4) throw new InvalidOperationException("Expected four power-up spawners.");
        for (int i = 0; i < spawners.Length; i++)
        {
            spawners[i].transform.position = pickupPositions[i];
            Box("Supply pad " + i, pickupPositions[i] + Vector3.up * 0.04f,
                new Vector3(3.5f, 0.03f, 3.5f), accent, false);
            Box("Supply cross A " + i, pickupPositions[i] + Vector3.up * 0.07f,
                new Vector3(1.7f, 0.02f, 0.35f), white, false);
            Box("Supply cross B " + i, pickupPositions[i] + Vector3.up * 0.07f,
                new Vector3(0.35f, 0.02f, 1.7f), white, false);
        }

        var light = UnityEngine.Object.FindFirstObjectByType<Light>();
        light.transform.rotation = Quaternion.Euler(55, -35, 0);
        light.color = theme == 0 ? new Color(1, 0.90f, 0.76f) : theme == 2 ? new Color(0.87f, 0.94f, 1) : Color.white;
        light.intensity = 0.9f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.38f, 0.42f, 0.45f);
        RenderSettings.fog = false;
        var camera = Camera.main;
        camera.transform.position = new Vector3(0, 58, -38);
        camera.transform.rotation = Quaternion.Euler(57, 0, 0);
        camera.orthographicSize = 38;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = theme == 1 ? new Color(0.14f, 0.32f, 0.38f) : ground.color;

        Physics.SyncTransforms();
        surface.BuildNavMesh();
        if (surface.navMeshData == null) throw new InvalidOperationException("Navigation build failed: " + name);
        string navPath = Art + "/" + name + "_Navigation.asset";
        var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
        var data = surface.navMeshData;
        surface.RemoveData();
        if (existing == null) AssetDatabase.CreateAsset(data, navPath);
        else { EditorUtility.CopySerialized(data, existing); surface.navMeshData = existing; }
        surface.AddData();
        Validate(starts, pickupPositions);
        EditorSceneManager.SaveScene(scene, DuelArenaCatalog.ScenePath(name));
        Debug.Log("Arena ready: " + name + " — all spawn/supply routes connected.");
    }

    private static void Canyon()
    {
        Box("Dry riverbed", new Vector3(0, 0.015f, 0), new Vector3(16, 0.02f, 49), road, false);
        Prop("@CanyonMesa", 0, 0, new Vector3(12, 5, 9), null);
        Prop("@CanyonMesa", -13, 8, new Vector3(7, 4, 9), null);
        Prop("@CanyonMesa", 13, -8, new Vector3(7, 4, 9), null, 180);
        Prop("@CanyonBoulder", -14, -17, new Vector3(6, 2.5f, 3), null);
        Prop("@CanyonBoulder", 14, 17, new Vector3(6, 2.5f, 3), null, 180);
        Prop("@StoneArch", -18, 31, new Vector3(12, 7, 4), null);
        Prop("@StoneArch", 18, -31, new Vector3(12, 7, 4), null);
        for (int i = -2; i <= 2; i++)
        {
            Prop("@CanyonMesa", i * 13, -29, new Vector3(13, 5 + (i & 1), 7), null);
            Prop("@CanyonMesa", i * 13, 29, new Vector3(13, 5 + (i & 1), 7), null, 180);
        }
        foreach (int sign in new[] { -1, 1 })
        {
            Prop("Desert/Cactus", sign * 28, sign * 5, new Vector3(2, 3, 2), null);
            Prop("Desert/Cactus", sign * 7, sign * 23, new Vector3(1.5f, 2.2f, 1.5f), null);
            for (int z = -21; z <= 21; z += 7)
                Prop("@CanyonBoulder", sign * 34, z, new Vector3(6, 3 + (z & 1), 8), null, z * 7);
        }
    }

    private static void Harbor()
    {
        Box("Central roadway", new Vector3(0, 0.02f, 0), new Vector3(60, 0.03f, 10), road, false);
        for (int x = -28; x <= 28; x += 4)
            foreach (int z in new[] { -5, 5 })
                Box("Lane dash", new Vector3(x, 0.05f, z), new Vector3(2, 0.02f, 0.18f), white, false);
        var red = Mat("Harbor_Container_Red", "A75643");
        var blue = Mat("Harbor_Container_Blue", "407A86");
        foreach (int sign in new[] { -1, 1 })
        {
            Prop(sign < 0 ? "@CargoContainerRed" : "@CargoContainerBlue", -11, sign * 10, new Vector3(10, 3.1f, 4.6f), null);
            Prop(sign < 0 ? "@CargoContainerBlue" : "@CargoContainerRed", 11, sign * 10, new Vector3(10, 3.1f, 4.6f), null);
            Prop("@DockCrane", sign * 38, sign * 13, new Vector3(6, 14, 16), null, sign < 0 ? 0 : 180);
            Prop("@CargoCrate", sign * 13, sign * 21, new Vector3(3, 2.7f, 3), null);
            Prop("@CargoCrate", sign * 17, sign * 22, new Vector3(2.4f, 2.2f, 2.4f), null);
            for (int j = -2; j <= 2; j++)
                Cover("Dock bollard", sign * 29, j * 9, 1.2f, 1.2f, 1.4f, accent);
        }
        Cover("Central control room", 0, 0, 5, 5, 4, trim);
        Box("Control roof", new Vector3(0, 4.2f, 0), new Vector3(5.4f, 0.4f, 5.4f), dark, false);
        Box("Control windows", new Vector3(0, 2.8f, -2.52f), new Vector3(3.5f, 0.9f, 0.04f), blue, false);
    }

    private static void Arctic()
    {
        Box("Station approach", new Vector3(0, 0.02f, 0), new Vector3(60, 0.03f, 8), road, false);
        foreach (int sign in new[] { -1, 1 })
        {
            Prop("@PolarStation", sign * 10, -sign * 8, new Vector3(8, 4.8f, 9), null, sign < 0 ? 0 : 180);
            Prop("@IceCluster", sign * 18, -sign * 19, new Vector3(5, 3, 5), null);
            Prop("@PolarRadar", sign * 28, -sign * 24, new Vector3(3, 7, 3), null);
            for (int i = -2; i <= 2; i++)
                Prop("@IceCluster", i * 13, sign * 29, new Vector3(14, 5, 7), null);
            for (int z = -20; z <= 20; z += 8)
                Prop("@IceCluster", sign * 34, z, new Vector3(6, 4, 9), null, z * 4);
            Cover("Ice barricade", sign * 11, sign * 17, 6, 2, 1.8f, rock);
            Box("Snow cap", new Vector3(sign * 11, 1.9f, sign * 17), new Vector3(6.2f, 0.2f, 2.2f), trim, false);
        }
    }

    private static void GroundDetails(int theme, string name)
    {
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        var random = new System.Random(716 + theme);
        for (int i = 0; i < 450; i++)
        {
            float x = (float)random.NextDouble() * 62 - 31;
            float z = (float)random.NextDouble() * 50 - 25;
            // Keep gameplay markings and the main travel lane clear.
            if (Mathf.Abs(x) < 8 || Mathf.Abs(z) < 5 ||
                (Mathf.Abs(x) > 21 && Mathf.Abs(Mathf.Abs(z) - 14) < 4)) continue;
            float width = .12f + (float)random.NextDouble() * .65f;
            float depth = width * (theme == 2 ? 2.5f : .8f);
            int n = vertices.Count;
            vertices.Add(new Vector3(x - width, .035f, z));
            vertices.Add(new Vector3(x, .035f, z + depth));
            vertices.Add(new Vector3(x + width, .035f, z));
            vertices.Add(new Vector3(x + width * .4f, .035f, z - depth));
            triangles.AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 });
        }
        var mesh = new Mesh { name = name + "_GroundDetails" }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals();
        string path = Art + "/Meshes/" + mesh.name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null) AssetDatabase.CreateAsset(mesh, path);
        else { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); mesh = existing; }
        var go = new GameObject("Ground flecks", typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(environment, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.GetComponent<Renderer>().sharedMaterial = Mat(name + "_Flecks", theme == 0 ? "CCA26D" : theme == 1 ? "62777A" : "B3D2D8");
        go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; go.isStatic = true;
    }

    private static Material Mat(string name, string hex)
    {
        string path = Art + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        ColorUtility.TryParseHtmlString("#" + hex, out var color);
        mat.color = QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color;
        mat.SetFloat("_Smoothness", 0.12f);
        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static GameObject Box(string name, Vector3 position, Vector3 scale, Material mat, bool collision)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(environment, false);
        go.transform.position = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collision) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        go.isStatic = true;
        return go;
    }

    private static void Cover(string name, float x, float z, float width, float depth, float height, Material mat)
    {
        var go = Box(name, new Vector3(x, height / 2, z), new Vector3(width, height, depth), mat, true);
        var modifier = go.AddComponent<NavMeshModifier>();
        modifier.overrideArea = true;
        modifier.area = 1;
    }

    private static void Prop(string name, float x, float z, Vector3 size, Material material, float yaw = 0)
    {
        string path = name.StartsWith("@") ? DuelArenaAssetBuilder.Folder + name.Substring(1) + ".prefab" :
            "Assets/_Tanks/Prefabs/Environment/" + name + ".prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) throw new InvalidOperationException("Missing arena prop: " + name);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, environment);
        go.transform.rotation = Quaternion.Euler(0, yaw, 0);
        var renderers = go.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        go.transform.localScale = Vector3.Scale(go.transform.localScale,
            new Vector3(size.x / bounds.size.x, size.y / bounds.size.y, size.z / bounds.size.z));
        bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        go.transform.position += new Vector3(x - bounds.center.x, -bounds.min.y, z - bounds.center.z);
        foreach (var r in renderers)
        {
            if (material != null) r.sharedMaterials = r.sharedMaterials.Select(m => material).ToArray();
            r.gameObject.isStatic = true;
        }
        var modifier = go.AddComponent<NavMeshModifier>();
        modifier.overrideArea = true;
        modifier.area = 1;
    }

    private static void Validate(Vector3[] starts, Vector3[] supplies)
    {
        foreach (var start in starts)
            if (Physics.CheckBox(start + Vector3.up * 1.25f, new Vector3(2.5f, 1, 2.5f),
                Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                throw new InvalidOperationException("Blocked tank spawn: " + start);
        var points = starts.Concat(supplies).ToArray();
        for (int a = 0; a < points.Length; a++)
        {
            if (!NavMesh.SamplePosition(points[a], out var source, 1.5f, NavMesh.AllAreas))
                throw new InvalidOperationException("Unreachable spawn/supply: " + points[a]);
            for (int b = a + 1; b < points.Length; b++)
            {
                if (!NavMesh.SamplePosition(points[b], out var target, 1.5f, NavMesh.AllAreas))
                    throw new InvalidOperationException("Unreachable destination: " + points[b]);
                var route = new NavMeshPath();
                if (!NavMesh.CalculatePath(source.position, target.position, NavMesh.AllAreas, route) ||
                    route.status != NavMeshPathStatus.PathComplete)
                    throw new InvalidOperationException("Disconnected route: " + points[a] + " -> " + points[b]);
            }
        }
    }
}
