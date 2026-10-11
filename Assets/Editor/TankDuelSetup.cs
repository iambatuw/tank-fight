using System;
using System.IO;
using Tanks.Complete;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class TankDuelSetup
{
    private const string InputPath = "Assets/_Tanks/Settings/Tank_Actions.inputactions";
    private const string SourceFolder = "Assets/_Tanks/Tutorial_Demo/Demo_Scenes/";
    private const string TargetFolder = "Assets/Scenes/";
    private static readonly string[] Arenas = { "Jungle", "Desert", "Moon" };

    static TankDuelSetup()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            // Close any accidental multi-scene loaded states before Play mode begins
            if (SceneManager.sceneCount > 1)
            {
                Debug.LogWarning("Tank Duel: Multiple scenes detected in hierarchy. Closing background scenes to prevent duplicate audio, UI and performance lag.");
                Scene active = SceneManager.GetActiveScene();
                for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
                {
                    Scene s = SceneManager.GetSceneAt(i);
                    if (s != active && s.isLoaded)
                    {
                        EditorSceneManager.CloseScene(s, true);
                    }
                }
            }
        }
    }

    [MenuItem("Tank Duel/Reset & Open Single Clean Scene")]
    public static void ResetAndOpenCleanScene()
    {
        if (EditorApplication.isPlaying)
            EditorApplication.isPlaying = false;

        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        Scene scene = EditorSceneManager.OpenScene(TargetFolder + "Duel_Jungle.unity", OpenSceneMode.Single);
        StripLegacyBannersFromScene(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Tank Duel: Cleaned hierarchy and opened Duel_Jungle as a single, isolated scene with all 3D legacy banners removed.");
    }

    [MenuItem("Tank Duel/Clean All 3D Legacy Banners (All Arenas)")]
    public static void CleanAllBannersFromAllScenes()
    {
        if (EditorApplication.isPlaying)
            EditorApplication.isPlaying = false;

        foreach (var arena in Arenas)
        {
            string path = TargetFolder + "Duel_" + arena + ".unity";
            if (File.Exists(path))
            {
                Scene s = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                StripLegacyBannersFromScene(s);
                EditorSceneManager.MarkSceneDirty(s);
                EditorSceneManager.SaveScene(s);
                Debug.Log($"Tank Duel: Stripped legacy TitleScreen and banners from {path}");
            }
        }
        EditorSceneManager.OpenScene(TargetFolder + "Duel_Jungle.unity", OpenSceneMode.Single);
    }

    public static void StripLegacyBannersFromScene(Scene scene)
    {
        string[] targetNames = { "TitleScreen", "Title Screen", "Title", "Player Select", "Tank Select", "MobileControlCanvas", "GameUICanvas" };
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            if (root == null) continue;
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = transforms.Length - 1; i >= 0; i--)
            {
                var tr = transforms[i];
                if (tr == null) continue;
                string gName = tr.gameObject.name;

                if (gName.Equals("Menus", StringComparison.OrdinalIgnoreCase))
                {
                    var uiHandler = tr.GetComponentInChildren<GameUIHandler>(true);
                    if (uiHandler != null)
                    {
                        uiHandler.enabled = false;
                        if (uiHandler.m_StartMenuRoot != null) uiHandler.m_StartMenuRoot.gameObject.SetActive(false);
                        if (uiHandler.m_PauseMenuButton != null) uiHandler.m_PauseMenuButton.gameObject.SetActive(false);
                    }
                    continue;
                }

                bool shouldDestroy = false;
                foreach (var t in targetNames)
                {
                    if (gName.Equals(t, StringComparison.OrdinalIgnoreCase) ||
                        gName.IndexOf("TitleScreen", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        gName.IndexOf("Player Select", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        shouldDestroy = true;
                        break;
                    }
                }
                if (shouldDestroy)
                {
                    UnityEngine.Object.DestroyImmediate(tr.gameObject);
                }
            }
        }

        // Also check all TextMeshPro 3D objects in the scene
        var tmps = UnityEngine.Object.FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include);
        foreach (var tmp in tmps)
        {
            if (tmp == null) continue;
            string t = tmp.text ?? "";
            if (t.Contains("Player Select") || t.Contains("TANKS!") || t.Contains("Tank Select") || t.Contains("Select Tank"))
            {
                if (tmp.transform.parent != null && (tmp.transform.parent.name.Contains("Title") || tmp.transform.parent.name.Contains("Select")))
                {
                    UnityEngine.Object.DestroyImmediate(tmp.transform.parent.gameObject);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(tmp.gameObject);
                }
            }
        }
    }

    [MenuItem("Tank Duel/Open Jungle Arena")]
    public static void OpenJungle()
    {
        ResetAndOpenCleanScene();
    }

    [MenuItem("Tank Duel/Build Windows EXE")]
    public static void BuildWindowsExe()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Tank Duel: Please stop Play mode before building Windows EXE.");
            return;
        }

        string buildFolder = Path.Combine(Directory.GetCurrentDirectory(), "Build");
        Directory.CreateDirectory(buildFolder);
        string exePath = Path.Combine(buildFolder, "TankDuel.exe");

        var scenes = Array.ConvertAll(DuelArenaCatalog.Scenes, DuelArenaCatalog.ScenePath);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        Debug.Log("Tank Duel: Starting Windows Standalone (x64) build...");
        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Tank Duel: Build succeeded! Size: {report.summary.totalSize / (1024 * 1024)} MB. Path: {exePath}");
            EditorUtility.RevealInFinder(exePath);
        }
        else
        {
            Debug.LogError($"Tank Duel: Build failed: {report.summary.result}");
        }
    }

    [MenuItem("Tank Duel/Set Up Playable Arenas")]
    public static void SetUp()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play mode before setting up Tank Duel scenes.");
            return;
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).isDirty)
            {
                Debug.LogWarning("Tank Duel setup postponed: save or discard unsaved scene changes first, then run Tank Duel > Set Up Playable Arenas.");
                return;
            }
        }

        var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
        if (actions == null || actions.FindAction("Vertical") == null ||
            actions.FindAction("Horizontal") == null || actions.FindAction("Fire") == null)
        {
            Debug.LogError("Tank Duel input actions are missing; playable scene setup stopped.");
            return;
        }

        InputSystem.actions = actions;
        Directory.CreateDirectory(TargetFolder);
        var scenes = new EditorBuildSettingsScene[Arenas.Length];

        TMP_FontAsset defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Tanks/Settings/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (defaultFont == null)
            defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        try
        {
            for (int index = 0; index < Arenas.Length; index++)
            {
                string source = SourceFolder + "Demo_Game_" + Arenas[index] + ".unity";
                string destination = TargetFolder + "Duel_" + Arenas[index] + ".unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(source) == null)
                    throw new FileNotFoundException("Missing demo arena", source);

                Scene scene = EditorSceneManager.OpenScene(source, OpenSceneMode.Single);

                var manager = FindInScene<GameManager>(scene);
                if (manager == null || manager.m_CameraControl == null ||
                    manager.m_SpawnPoints == null || manager.m_SpawnPoints.Length < 2 ||
                    manager.m_Tank1Prefab == null || manager.m_Tank2Prefab == null)
                    throw new InvalidOperationException(source + " is missing a GameManager prefab, camera or spawn reference.");

                for (int i = 0; i < 2; i++)
                    if (manager.m_SpawnPoints[i].m_SpawnPoint == null)
                        throw new InvalidOperationException(source + " is missing player " + (i + 1) + " spawn point.");

                if (FindInScene<MessageTextReference>(scene) == null)
                    throw new InvalidOperationException(source + " is missing its game message label.");
                if (FindInScene<Camera>(scene) == null)
                    throw new InvalidOperationException(source + " is missing its arena camera.");

                foreach (var legacyMenu in FindAllInScene<GameUIHandler>(scene))
                {
                    legacyMenu.enabled = false;
                    if (legacyMenu.m_StartMenuRoot != null)
                        legacyMenu.m_StartMenuRoot.gameObject.SetActive(false);
                    if (legacyMenu.m_PauseMenuButton != null)
                        legacyMenu.m_PauseMenuButton.gameObject.SetActive(false);
                }
                foreach (var legacyPause in FindAllInScene<PauseMenu>(scene))
                {
                    legacyPause.enabled = false;
                    if (legacyPause.m_PauseMenuRoot != null)
                        legacyPause.m_PauseMenuRoot.gameObject.SetActive(false);
                }

                var duel = FindInScene<TankDuel>(scene);
                if (duel == null)
                {
                    var go = new GameObject("Tank Duel Controller");
                    SceneManager.MoveGameObjectToScene(go, scene);
                    duel = go.AddComponent<TankDuel>();
                }
                duel.gameManager = manager;
                duel.titleFont = defaultFont;
                duel.bodyFont = defaultFont;

                string[] tankPaths = new[]
                {
                    "Assets/_Tanks/Tutorial_Demo/Demo_Prefabs/Demo_Tanks/Demo_Tank.prefab",
                    "Assets/_Tanks/Tutorial_Demo/Demo_Prefabs/Demo_Tanks/Demo_Tank - Medium Variant.prefab",
                    "Assets/_Tanks/Tutorial_Demo/Demo_Prefabs/Demo_Tanks/Demo_Tank - Heavy Variant.prefab",
                    "Assets/_Tanks/Tutorial_Demo/Demo_Prefabs/Demo_Tanks/Demo_Tank - ATV Variant.prefab",
                    "Assets/_Tanks/Tutorial_Demo/Demo_Prefabs/Demo_Tanks/Demo_Tank - Shark Variant.prefab"
                };
                var tanksList = new System.Collections.Generic.List<GameObject>();
                foreach (var p in tankPaths)
                {
                    var obj = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                    if (obj != null) tanksList.Add(obj);
                }
                duel.availableTanks = tanksList.ToArray();

                StripLegacyBannersFromScene(scene);

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, destination, false))
                    throw new IOException("Cannot save playable arena copy: " + destination);
                scenes[index] = new EditorBuildSettingsScene(destination, true);
            }

            var existing = EditorBuildSettings.scenes;
            var combined = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes);
            foreach (var entry in existing)
            {
                bool isDuelScene = Array.Exists(scenes, duel => duel.path == entry.path);
                if (isDuelScene)
                    continue;
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(entry.path) == null)
                {
                    Debug.LogWarning("Skipping missing build scene: " + entry.path);
                    continue;
                }
                combined.Add(entry);
            }
            EditorBuildSettings.scenes = combined.ToArray();
            AssetDatabase.SaveAssets();

            // Reopen clean Jungle as single scene
            EditorSceneManager.OpenScene(TargetFolder + "Duel_Jungle.unity", OpenSceneMode.Single);
            Debug.Log("Tank Duel: created 3 playable arena copies, configured build scenes. Duel_Jungle opened as single scene.");
        }
        catch (Exception e)
        {
            Debug.LogError("Tank Duel setup failed: " + e);
        }
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            T result = root.GetComponentInChildren<T>(true);
            if (result != null)
                return result;
        }
        return null;
    }

    private static System.Collections.Generic.IEnumerable<T> FindAllInScene<T>(Scene scene) where T : Component
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var component in root.GetComponentsInChildren<T>(true))
                yield return component;
    }
}
