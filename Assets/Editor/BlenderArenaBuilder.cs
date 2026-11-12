using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Tanks.Complete;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Places the Blender-made Canyon and Harbor arenas (Blender/Arenas.blend → Assets/ArenaArt/Blender/*.fbx)
// into their scenes. Naming contract with the Blender export:
//   ART_<material>  visual mesh, material named "<name>_<RRGGBB>"
//   COL_<name>      box collider (invisible), cover blocks the NavMesh
//   MARK_<name>     axis/scale check points, removed after validation
[InitializeOnLoad]
public static class BlenderArenaBuilder
{
    private const string Folder = "Assets/ArenaArt/Blender/";
    private const string MaterialFolder = Folder + "Materials/";
    private const string Request = "Temp/BlenderArenas.request";
    private const string ResultFile = "Temp/BlenderArenas.result";

    private struct Arena
    {
        public string Scene, Model;
        public Color Background, Sun;
    }

    private static readonly Arena[] Arenas =
    {
        new Arena { Scene = "Duel_Canyon", Model = "Canyon", Background = new Color(0.89f, 0.67f, 0.42f), Sun = new Color(1f, 0.91f, 0.78f) },
        new Arena { Scene = "Duel_Harbor", Model = "Harbor", Background = new Color(0.16f, 0.43f, 0.50f), Sun = Color.white },
    };

    private static readonly Vector3[] Starts =
        { new Vector3(-25, 0, -14), new Vector3(25, 0, 14), new Vector3(-25, 0, 14), new Vector3(25, 0, -14) };
    private static readonly Vector3[] Supplies =
        { new Vector3(-20, 0, 0), new Vector3(20, 0, 0), new Vector3(0, 0, -19), new Vector3(0, 0, 19) };

    static BlenderArenaBuilder()
    {
        EditorApplication.update += CheckRequest;
    }

    private static void CheckRequest()
    {
        if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            return;
        File.Delete(Request);
        EditorApplication.update -= CheckRequest;
        BuildAll();
    }

    [MenuItem("Tank Duel/Build Blender Arenas (Canyon + Harbor)")]
    public static void BuildAll()
    {
        var log = new List<string>();
        try
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play before building arenas.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save the open scene first.");
            string originalPath = SceneManager.GetActiveScene().path;
            Directory.CreateDirectory(MaterialFolder);
            AssetDatabase.Refresh();
            try
            {
                foreach (var arena in Arenas) log.Add(Build(arena));
                AssetDatabase.SaveAssets();
            }
            finally
            {
                if (!string.IsNullOrEmpty(originalPath)) EditorSceneManager.OpenScene(originalPath);
            }
            log.Add("OK");
            Debug.Log("Blender arenas ready:\n" + string.Join("\n", log));
        }
        catch (Exception e)
        {
            log.Add("ERROR: " + e.Message);
            Debug.LogError("Blender arena build failed: " + e);
        }
        File.WriteAllLines(ResultFile, log);
    }

    private static string Build(Arena arena)
    {
        string modelPath = Folder + arena.Model + ".fbx";
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (model == null) throw new InvalidOperationException("Missing model: " + modelPath);

        var scene = EditorSceneManager.OpenScene(DuelArenaCatalog.ScenePath(arena.Scene));
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == "Designed Arena" || root.name.StartsWith("Level") || root.name == "EnhancedEnvironment")
                UnityEngine.Object.DestroyImmediate(root);

        var environment = new GameObject("Designed Arena").transform;
        var surface = environment.gameObject.AddComponent<NavMeshSurface>();
        surface.agentTypeID = -1372625422;
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.overrideVoxelSize = true;
        surface.voxelSize = 0.2f;

        var art = (GameObject)PrefabUtility.InstantiatePrefab(model, environment);
        PrefabUtility.UnpackPrefabInstance(art, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        art.name = arena.Model + " (Blender)";
        art.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        art.transform.localScale = Vector3.one;

        CheckMarkers(art.transform, arena.Model);

        int colliders = 0, visuals = 0;
        foreach (var t in art.GetComponentsInChildren<Transform>(true).ToArray())
        {
            if (t == null || t == art.transform) continue;
            // Blender suffixes duplicate names across scenes (".001"); the contract ignores it.
            t.name = BaseName(t.name);
            if (t.name.StartsWith("MARK_")) { UnityEngine.Object.DestroyImmediate(t.gameObject); continue; }
            var filter = t.GetComponent<MeshFilter>();
            if (t.name.StartsWith("COL_"))
            {
                var bounds = filter != null && filter.sharedMesh != null ? filter.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one);
                UnityEngine.Object.DestroyImmediate(t.GetComponent<MeshRenderer>());
                if (filter != null) UnityEngine.Object.DestroyImmediate(filter);
                var box = t.gameObject.AddComponent<BoxCollider>();
                box.center = bounds.center;
                box.size = bounds.size;
                if (t.name != "COL_Ground")
                {
                    var modifier = t.gameObject.AddComponent<NavMeshModifier>();
                    modifier.overrideArea = true;
                    modifier.area = 1;
                }
                t.gameObject.isStatic = true;
                colliders++;
                continue;
            }
            var renderer = t.GetComponent<MeshRenderer>();
            if (renderer == null) continue;
            renderer.sharedMaterials = renderer.sharedMaterials.Select(RemapMaterial).ToArray();
            // Paint, flecks and lines lie flat on the ground and should not cast shadows.
            if (renderer.bounds.size.y < 0.2f) renderer.shadowCastingMode = ShadowCastingMode.Off;
            t.gameObject.isStatic = true;
            visuals++;
        }
        if (colliders == 0) throw new InvalidOperationException(arena.Model + ": no COL_ objects found.");

        var manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        if (manager == null) throw new InvalidOperationException(arena.Scene + ": missing GameManager.");
        for (int i = 0; i < manager.m_SpawnPoints.Length && i < Starts.Length; i++)
        {
            var spawn = manager.m_SpawnPoints[i].m_SpawnPoint;
            spawn.position = Starts[i];
            spawn.rotation = Quaternion.LookRotation(-Starts[i].normalized, Vector3.up);
        }
        var spawners = UnityEngine.Object.FindObjectsByType<PowerUpSpawner>(FindObjectsInactive.Include)
            .OrderBy(s => s.name).ThenBy(s => s.transform.GetSiblingIndex()).ToArray();
        if (spawners.Length != 4) throw new InvalidOperationException("Expected four power-up spawners.");
        for (int i = 0; i < spawners.Length; i++) spawners[i].transform.position = Supplies[i];

        var light = UnityEngine.Object.FindFirstObjectByType<Light>();
        if (light != null)
        {
            light.transform.rotation = Quaternion.Euler(52, -35, 0);
            light.color = arena.Sun;
            light.intensity = 1.0f;
            light.shadows = LightShadows.Soft;
        }
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.42f, 0.45f, 0.48f);
        RenderSettings.fog = false;
        var camera = Camera.main;
        if (camera != null)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = arena.Background;
        }

        Physics.SyncTransforms();
        surface.BuildNavMesh();
        if (surface.navMeshData == null) throw new InvalidOperationException("Navigation build failed: " + arena.Scene);
        string navPath = "Assets/ArenaArt/" + arena.Scene + "_Navigation.asset";
        var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
        var data = surface.navMeshData;
        surface.RemoveData();
        if (existing == null) AssetDatabase.CreateAsset(data, navPath);
        else { EditorUtility.CopySerialized(data, existing); surface.navMeshData = existing; }
        surface.AddData();

        Validate();
        EditorSceneManager.SaveScene(scene);
        RenderPreview(camera, "UI-Preview/arena-" + arena.Model.ToLowerInvariant() + ".png");
        return $"{arena.Scene}: {visuals} meshes, {colliders} colliders, routes OK";
    }

    // Renders the arena from the two-player match framing so the README preview matches the game.
    private static void RenderPreview(Camera camera, string file)
    {
        if (camera == null) return;
        var position = camera.transform.position;
        var rotation = camera.transform.rotation;
        float size = camera.orthographicSize;
        bool ortho = camera.orthographic;
        var targetsRoot = new GameObject("Preview targets");
        try
        {
            var targets = new[] { Starts[0], Starts[1] }.Select(p =>
            {
                var t = new GameObject("target").transform;
                t.SetParent(targetsRoot.transform);
                t.position = p;
                return t;
            }).ToArray();
            camera.aspect = 16f / 9f;
            new DuelCameraFraming().Update(camera, targets, false, 0f, true);
            var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var previous = camera.targetTexture;
            camera.targetTexture = rt;
            camera.Render();
            camera.targetTexture = previous;
            var active = RenderTexture.active;
            RenderTexture.active = rt;
            var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            image.Apply();
            RenderTexture.active = active;
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllBytes(file, image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(targetsRoot);
            camera.ResetAspect();
            camera.transform.SetPositionAndRotation(position, rotation);
            camera.orthographicSize = size;
            camera.orthographic = ortho;
        }
    }

    // The Blender file places MARK_ cubes at known game coordinates; a wrong axis or unit
    // setting in the FBX export would move them, so stop before saving a broken arena.
    private static void CheckMarkers(Transform root, string model)
    {
        var expected = new Dictionary<string, Vector3>
        {
            { "MARK_Origin", new Vector3(0, 0.1f, 0) }, { "MARK_East20", new Vector3(20, 0.1f, 0) },
            { "MARK_North20", new Vector3(0, 0.1f, 20) }, { "MARK_Up5", new Vector3(0, 5.1f, 0) },
        };
        foreach (var pair in expected)
        {
            var marker = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => BaseName(t.name) == pair.Key);
            if (marker == null) throw new InvalidOperationException(model + ": missing " + pair.Key);
            var r = marker.GetComponent<Renderer>();
            Vector3 actual = r != null ? r.bounds.center : marker.position;
            if (Vector3.Distance(actual, pair.Value) > 0.3f)
                throw new InvalidOperationException($"{model}: {pair.Key} is at {actual}, expected {pair.Value}. Check FBX axis/scale.");
        }
    }

    private static string BaseName(string name) => Regex.Replace(name, @"\.\d{3}$", "");

    private static Material RemapMaterial(Material source)
    {
        if (source == null) return null;
        var match = Regex.Match(BaseName(source.name), @"^(.*)_([0-9A-Fa-f]{6})$");
        if (!match.Success) return source;
        string name = match.Groups[1].Value;
        string path = MaterialFolder + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        ColorUtility.TryParseHtmlString("#" + match.Groups[2].Value, out var color);
        // Material colours are authored in sRGB; Unity converts them for linear rendering.
        mat.color = color;
        float smoothness = name.Contains("Water") ? 0.62f : name.Contains("Glass") ? 0.8f :
            name.Contains("Wave") ? 0.5f : 0.12f;
        mat.SetFloat("_Smoothness", smoothness);
        if (name.Contains("Beacon") || name.Contains("Lamp"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            mat.SetColor("_EmissionColor", color * 1.6f);
        }
        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void Validate()
    {
        foreach (var start in Starts)
            if (Physics.CheckBox(start + Vector3.up * 1.25f, new Vector3(2.5f, 1, 2.5f),
                Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                throw new InvalidOperationException("Blocked tank spawn: " + start);
        var points = Starts.Concat(Supplies).ToArray();
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

// Import settings for the Blender arena models: no cameras, lights, animation or generated colliders.
public class BlenderArenaImport : AssetPostprocessor
{
    private void OnPreprocessModel()
    {
        if (!assetPath.StartsWith("Assets/ArenaArt/Blender/")) return;
        var importer = (ModelImporter)assetImporter;
        importer.globalScale = 1f;
        importer.useFileScale = true;
        importer.bakeAxisConversion = true;
        importer.importCameras = false;
        importer.importLights = false;
        importer.importAnimation = false;
        importer.animationType = ModelImporterAnimationType.None;
        importer.addCollider = false;
        importer.isReadable = false;
        importer.meshCompression = ModelImporterMeshCompression.Off;
        importer.importNormals = ModelImporterNormals.Import;
        importer.indexFormat = ModelImporterIndexFormat.Auto;
    }
}
