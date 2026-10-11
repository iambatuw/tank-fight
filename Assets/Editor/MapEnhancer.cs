using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

[InitializeOnLoad]
public static class MapEnhancer
{
    private const string TargetFolder = "Assets/Scenes/";
    private static readonly string[] Arenas = { "Jungle", "Desert", "Moon" };
    private const string Request = "Temp/MapEnhancer.request";

    static MapEnhancer()
    {
        EditorApplication.update += CheckRequest;
    }

    private static void CheckRequest()
    {
        if (File.Exists(Request))
        {
            File.Delete(Request); EditorApplication.update -= CheckRequest;
            AssetDatabase.Refresh(); EnhanceMaps();
        }
    }

    [MenuItem("Tank Duel/Enhance Maps")]
    public static void EnhanceMaps()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play mode before enhancing maps.");
            return;
        }

        Scene original = SceneManager.GetActiveScene();
        if (original.isDirty && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        try
        {
            // Enhance the existing arena scenes; their TankDuel controller provides the menu.
            var tankPaths = new[] {
                "Assets/_Tanks/Tutorial_Demo/Demo_Prefabs/Demo_Tanks/Demo_Tank.prefab",
                "Assets/_Tanks/Tutorial_Demo/Demo_Prefabs/Demo_Tanks/Demo_Tank - Medium Variant.prefab",
                "Assets/_Tanks/Tutorial_Demo/Demo_Prefabs/Demo_Tanks/Demo_Tank - Heavy Variant.prefab",
                "Assets/_Tanks/Tutorial_Demo/Demo_Prefabs/Demo_Tanks/Demo_Tank - ATV Variant.prefab",
                "Assets/_Tanks/Tutorial_Demo/Demo_Prefabs/Demo_Tanks/Demo_Tank - Shark Variant.prefab"
            };

            foreach (var arena in Arenas)
            {
                string path = TargetFolder + "Duel_" + arena + ".unity";
                if (!File.Exists(path))
                    continue;

                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

                // Assign tanks to TankDuel script
                var duel = GameObject.FindAnyObjectByType<TankDuel>();
                if (duel != null)
                {
                    duel.availableTanks = new GameObject[5];
                    for (int i = 0; i < 5; i++)
                        duel.availableTanks[i] = AssetDatabase.LoadAssetAtPath<GameObject>(tankPaths[i]);
                }

                Transform envRoot = new GameObject("EnhancedEnvironment").transform;

                if (arena == "Jungle")
                {
                    Populate(scene, envRoot, "Jungle", new[] {
                        "Bush01", "Bush02", "TreesThree", "PalmTree03", "RocksJungle01", "RuinsJungle", "BuildingJungle01"
                    }, 45);
                    AddPowerUpSpawners(scene, envRoot, 4);
                }
                else if (arena == "Desert")
                {
                    Populate(scene, envRoot, "Desert", new[] {
                        "Cactus", "RocksSand01", "RocksSand02", "RuinsSand", "OasisSand", "BuildingSand01"
                    }, 40);
                    AddPowerUpSpawners(scene, envRoot, 4);
                }
                else if (arena == "Moon")
                {
                    Populate(scene, envRoot, "Moon", new[] {
                        "RocksMoon01", "CraterMoon01", "CratorsMoon01", "BuildingMoon01", "RocketMoon", "HelipadMoon"
                    }, 35);
                    AddPowerUpSpawners(scene, envRoot, 4);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            // Update Build Settings
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            foreach (var arena in DuelArenaCatalog.Scenes)
            {
                string path = DuelArenaCatalog.ScenePath(arena);
                if (File.Exists(path)) scenes.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();

            Debug.Log("Tank Duel maps enhanced successfully.");
        }
        finally
        {
            if (original.IsValid() && !string.IsNullOrEmpty(original.path))
                EditorSceneManager.OpenScene(original.path, OpenSceneMode.Single);
        }
    }

    private static void Populate(Scene scene, Transform root, string theme, string[] prefabNames, int count)
    {
        // First destroy any previous EnhancedEnvironment
        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.name == "EnhancedEnvironment" && go != root.gameObject)
                GameObject.DestroyImmediate(go);
        }

        for (int i = 0; i < count; i++)
        {
            string prefabName = prefabNames[Random.Range(0, prefabNames.Length)];
            string path = $"Assets/_Tanks/Prefabs/Environment/{theme}/{prefabName}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            // Pick a random position avoiding the absolute center (where tanks fight)
            Vector2 randCircle = Random.insideUnitCircle;
            if (randCircle.magnitude < 0.3f) randCircle = randCircle.normalized * 0.4f; // Push away from center
            Vector3 pos = new Vector3(randCircle.x * 35f, 0, randCircle.y * 35f);

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.transform.position = pos;
            inst.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

            // Random scaling between 0.8 and 1.5
            float scale = Random.Range(0.8f, 1.5f);
            inst.transform.localScale = new Vector3(scale, scale, scale);
            inst.transform.SetParent(root);
        }
    }

    private static void AddPowerUpSpawners(Scene scene, Transform root, int count)
    {
        string path = "Assets/_Tanks/Prefabs/PowerUps/PowerUpSpawner.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) return;

        for (int i = 0; i < count; i++)
        {
            Vector2 randCircle = Random.insideUnitCircle.normalized * Random.Range(10f, 30f);
            Vector3 pos = new Vector3(randCircle.x, 1.09f, randCircle.y);

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.transform.position = pos;
            inst.transform.SetParent(root);
        }
    }
}
// update
