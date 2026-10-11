using System;
using System.Collections;
using System.Collections.Generic;
using Tanks.Complete;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class TankDuel : MonoBehaviour
{
    public static TankDuel Instance { get; private set; }

    public GameManager gameManager;
    public GameObject[] availableTanks;

    public TMP_FontAsset titleFont;
    public TMP_FontAsset bodyFont;

    [Serializable]
    public struct TankArchetype
    {
        public string nameTR;
        public string nameEN;
        public string roleTR;
        public string roleEN;
        public string descTR;
        public string descEN;
        public float speed;
        public float turnSpeed;
        public float health;
        public float damage;
        public float cooldown;
        public int price;

        public string GetName() => TankDuelLocalization.IsTurkish ? nameTR : nameEN;
        public string GetRole() => TankDuelLocalization.IsTurkish ? roleTR : roleEN;
        public string GetDesc() => TankDuelLocalization.IsTurkish ? descTR : descEN;
    }

    public static readonly TankArchetype[] Archetypes = new TankArchetype[]
    {
        new TankArchetype
        {
            nameTR = "STANDART",
            nameEN = "STANDARD",
            roleTR = "DENGELİ MUHARİP",
            roleEN = "BALANCED FIGHTER",
            descTR = "Hız, zırh ve ateş gücünde kusursuz denge. Her arena için güvenilir seçim.",
            descEN = "Perfect balance of speed, armor, and firepower. Reliable in every arena.",
            speed = 12f,
            turnSpeed = 180f,
            health = 50f,
            damage = 50f,
            cooldown = 1.0f,
            price = 0
        },
        new TankArchetype
        {
            nameTR = "ORTA",
            nameEN = "MEDIUM",
            roleTR = "TAKTİK TAARRUZ",
            roleEN = "TACTICAL ASSAULT",
            descTR = "Güçlendirilmiş gövde ve dengeli manevra kabiliyeti. Taktiksel vuruşlar için ideal.",
            descEN = "Reinforced hull and balanced mobility. Built for calculated tactical strikes.",
            speed = 11f,
            turnSpeed = 165f,
            health = 55f,
            damage = 55f,
            cooldown = 1.1f,
            price = 100
        },
        new TankArchetype
        {
            nameTR = "AĞIR",
            nameEN = "HEAVY",
            roleTR = "ZIRHLI KALE",
            roleEN = "ARMORED FORTRESS",
            descTR = "En yüksek can havuzu ve devasa patlama hasarı. Ağır gövde fakat durdurulamaz.",
            descEN = "Maximum health pool and massive shell damage. Slow, but an unstoppable juggernaut.",
            speed = 9f,
            turnSpeed = 130f,
            health = 75f,
            damage = 70f,
            cooldown = 1.4f,
            price = 200
        },
        new TankArchetype
        {
            nameTR = "ATV",
            nameEN = "ATV SCOUT",
            roleTR = "HIZLI KEŞİF",
            roleEN = "RAPID STRIKER",
            descTR = "Rakipsiz hız ve seri atış yeteneği. Düşman mermilerinden kaçarak vur-kaç yapar.",
            descEN = "Unrivaled speed and rapid fire rate. Specializes in hit-and-run tactics.",
            speed = 16f,
            turnSpeed = 230f,
            health = 40f,
            damage = 40f,
            cooldown = 0.7f,
            price = 300
        },
        new TankArchetype
        {
            nameTR = "KÖPEKBALIĞI",
            nameEN = "SHARK",
            roleTR = "AVCI KESKİN NİŞANCI",
            roleEN = "PREDATOR SNIPER",
            descTR = "Yüksek hasar ve keskin dönüş kabiliyeti. Agresif avcılar için tasarlandı.",
            descEN = "Devastating firepower and sharp handling. Engineered for aggressive predator play.",
            speed = 13f,
            turnSpeed = 195f,
            health = 45f,
            damage = 65f,
            cooldown = 1.2f,
            price = 400
        }
    };

    private static readonly string[] ArenaScenes = { "Duel_Jungle", "Duel_Desert", "Duel_Moon" };
    private static readonly string[] ArenaNamesTR = { "ORMAN", "ÇÖL", "AY ÜSSÜ" };
    private static readonly string[] ArenaNamesEN = { "JUNGLE", "DESERT", "MOON" };
    private static readonly int[] RoundLengths = { 60, 90, 120 };

    private static readonly Color DarkBg = new Color(0.04f, 0.07f, 0.10f, 0.98f);
    private static readonly Color CardBg = new Color(0.08f, 0.13f, 0.18f, 0.98f);
    private static readonly Color Ink = new Color(0.03f, 0.05f, 0.07f, 1f);
    private static readonly Color Cyan = new Color(0.00f, 0.80f, 0.92f, 1f);
    private static readonly Color Amber = new Color(0.98f, 0.64f, 0.05f, 1f);
    private static readonly Color Emerald = new Color(0.07f, 0.76f, 0.53f, 1f);
    private static readonly Color Crimson = new Color(0.92f, 0.22f, 0.22f, 1f);
    private static readonly Color Muted = new Color(0.60f, 0.68f, 0.77f, 1f);
    private static readonly Color OffWhite = new Color(0.96f, 0.97f, 0.99f, 1f);

    private Canvas canvas;
    private GameObject topBar;
    private GameObject menuPanel;
    private GameObject garagePanel;
    private GameObject settingsPanel;
    private GameObject hud;
    private GameObject pause;
    private GameObject result;

    // HUD Elements
    private TextMeshProUGUI timerText;
    private TextMeshProUGUI roundText;
    private TextMeshProUGUI scoreText;

    private RectTransform p1HealthFill;
    private Image p1HealthFillImg;
    private TextMeshProUGUI p1HealthText;
    private TextMeshProUGUI p1NameText;
    private TextMeshProUGUI p1TankText;
    private TextMeshProUGUI p1WinsText;

    private RectTransform p2HealthFill;
    private Image p2HealthFillImg;
    private TextMeshProUGUI p2HealthText;
    private TextMeshProUGUI p2NameText;
    private TextMeshProUGUI p2TankText;
    private TextMeshProUGUI p2WinsText;

    // Toast Banner
    private GameObject toastPanel;
    private TextMeshProUGUI toastText;
    private TextMeshProUGUI toastContext;
    private Image toastAccent;
    private Coroutine activeToastCoroutine;

    // Menu Elements
    private TextMeshProUGUI coinsText;
    private Button[] modeCards = new Button[2];
    private TextMeshProUGUI[] modeCardTitles = new TextMeshProUGUI[2];
    private TextMeshProUGUI modeHintLabel;

    private GameObject difficultySection;
    private Button[] diffButtons = new Button[3];
    private TextMeshProUGUI[] diffButtonLabels = new TextMeshProUGUI[3];
    private TextMeshProUGUI diffHintLabel;

    // Garage Details
    private int currentGaragePlayer = 1;
    private int inspectedTankIndex = 0;
    private GameObject previewTankInstance;
    private GameObject previewPedestalInstance;
    private GameObject previewPedestalRingInstance;
    private GameObject previewLightingRig;
    private Light previewKeyLight;
    private Light previewFillLight;
    private Light previewRimLight;
    private Light previewUnderLight;
    private TextMeshProUGUI dossierNameText;
    private TextMeshProUGUI dossierRoleText;
    private TextMeshProUGUI dossierDescText;
    private RectTransform speedStatFill;
    private TextMeshProUGUI speedStatVal;
    private RectTransform armorStatFill;
    private TextMeshProUGUI armorStatVal;
    private RectTransform damageStatFill;
    private TextMeshProUGUI damageStatVal;
    private RectTransform firerateStatFill;
    private TextMeshProUGUI firerateStatVal;
    private Button dossierActionButton;
    private TextMeshProUGUI dossierActionText;
    private Button[] garageDockButtons = new Button[5];
    private TextMeshProUGUI[] garageDockLabels = new TextMeshProUGUI[5];
    private TextMeshProUGUI[] garageDockSublabels = new TextMeshProUGUI[5];
    private Button garageTab1;
    private Button garageTab2;

    private TankHealth[] cachedHealth = new TankHealth[2];
    private bool started;
    private bool paused;
    private bool isStartingMatch;
    private int botTankIndex = -1;
    private float hudUpdateTimer;

    private int CurrentSceneIndex
    {
        get
        {
            string sceneName = SceneManager.GetActiveScene().name;
            for (int i = 0; i < ArenaScenes.Length; i++)
            {
                if (sceneName.Contains(ArenaScenes[i]) || sceneName.EndsWith(ArenaScenes[i]))
                    return i;
            }
            return 0;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureControllerInScene()
    {
        var existing = FindAnyObjectByType<TankDuel>(FindObjectsInactive.Include);
        if (existing == null)
        {
            var manager = FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
            if (manager != null)
            {
                var go = new GameObject("Tank Duel Controller");
                var duel = go.AddComponent<TankDuel>();
                duel.gameManager = manager;
            }
        }
    }

    public static void CleanNullOverlayCameras()
    {
        var mainCam = Camera.main;
        if (mainCam != null)
        {
            var uData = mainCam.GetUniversalAdditionalCameraData();
            if (uData != null && uData.cameraStack != null)
            {
                uData.cameraStack.RemoveAll(c => c == null || c.gameObject == null);
            }
        }
    }

    private static void SafeDestroyObjectWithCameras(GameObject go)
    {
        if (go == null) return;
        var cams = go.GetComponentsInChildren<Camera>(true);
        if (cams != null && cams.Length > 0 && Camera.main != null)
        {
            var uData = Camera.main.GetUniversalAdditionalCameraData();
            if (uData != null && uData.cameraStack != null)
            {
                foreach (var c in cams)
                {
                    if (c != null)
                        uData.cameraStack.Remove(c);
                }
                uData.cameraStack.RemoveAll(c => c == null || c.gameObject == null);
            }
        }
        go.SetActive(false);
        Destroy(go);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Time.timeScale = 1f;
        isStartingMatch = false;

        CleanNullOverlayCameras();

        // Clean up accidental multi-scene loaded hierarchies
        if (SceneManager.sceneCount > 1)
        {
            Scene active = SceneManager.GetActiveScene();
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s != active && s.name.StartsWith("Duel_") && s.isLoaded)
                {
                    SceneManager.UnloadSceneAsync(s);
                }
            }
        }

        // Clean up duplicate AudioListeners
        var listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
        bool keptListener = false;
        foreach (var l in listeners)
        {
            if (!keptListener && l.gameObject.activeInHierarchy)
            {
                l.enabled = true;
                keptListener = true;
            }
            else
            {
                l.enabled = false;
            }
        }

        // Clean up duplicate background music
        var allAudios = FindObjectsByType<AudioSource>(FindObjectsInactive.Include);
        int musicCount = 0;
        foreach (var a in allAudios)
        {
            if (a.gameObject.name.Contains("BackgroundMusic") || a.gameObject.name.Contains("Music"))
            {
                musicCount++;
                if (musicCount > 1)
                {
                    a.Stop();
                    Destroy(a.gameObject);
                }
            }
        }

        // Eradicate in-world 3D banners ("TANKS! Player Select")
        CleanupLegacyBanners();

        // Clean up existing stale Canvases
        var oldCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        foreach (var c in oldCanvases)
        {
            if (c.gameObject.name == "Tank Duel Interface")
                Destroy(c.gameObject);
        }

        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1;

        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>();

        foreach (var oldMenu in FindObjectsByType<GameUIHandler>(FindObjectsInactive.Include))
        {
            oldMenu.enabled = false;
            if (oldMenu.m_StartMenuRoot != null) oldMenu.m_StartMenuRoot.gameObject.SetActive(false);
            if (oldMenu.m_PauseMenuButton != null) oldMenu.m_PauseMenuButton.gameObject.SetActive(false);
        }
        foreach (var oldPause in FindObjectsByType<PauseMenu>(FindObjectsInactive.Include))
        {
            oldPause.enabled = false;
            if (oldPause.m_PauseMenuRoot != null) oldPause.m_PauseMenuRoot.gameObject.SetActive(false);
        }

        ApplyMusicPreference();

        if (titleFont == null)
        {
            if (TMP_Settings.defaultFontAsset != null)
                titleFont = TMP_Settings.defaultFontAsset;
            else
                titleFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }
        if (bodyFont == null)
            bodyFont = titleFont;

        var interfaceFont = Resources.Load<TMP_FontAsset>("UI/Inter SDF");
        if (interfaceFont != null) titleFont = bodyFont = interfaceFont;

        TankDuelLocalization.EnsureTurkishSupport(titleFont);
        if (bodyFont != titleFont)
            TankDuelLocalization.EnsureTurkishSupport(bodyFont);
        if (TMP_Settings.defaultFontAsset != null)
            TankDuelLocalization.EnsureTurkishSupport(TMP_Settings.defaultFontAsset);

#if UNITY_EDITOR
        if (availableTanks == null || availableTanks.Length < 5)
        {
            string[] paths = {
                "Assets/_Tanks/Tutorial_Demo/Demo_Prefabs/Demo_Tanks/Demo_Tank.prefab",
                "Assets/_Tanks/Tutorial_Demo/Demo_Prefabs/Demo_Tanks/Demo_Tank - Medium Variant.prefab",
                "Assets/_Tanks/Tutorial_Demo/Demo_Prefabs/Demo_Tanks/Demo_Tank - Heavy Variant.prefab",
                "Assets/_Tanks/Tutorial_Demo/Demo_Prefabs/Demo_Tanks/Demo_Tank - ATV Variant.prefab",
                "Assets/_Tanks/Tutorial_Demo/Demo_Prefabs/Demo_Tanks/Demo_Tank - Shark Variant.prefab"
            };
            var list = new List<GameObject>();
            foreach (var p in paths)
            {
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (prefab != null) list.Add(prefab);
            }
            if (list.Count >= 5)
                availableTanks = list.ToArray();
        }
#endif
    }

    private void Start()
    {
        CleanNullOverlayCameras();
        CleanupLegacyBanners();

        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);

        if (gameManager == null)
            return;

        int defaultQuality = Mathf.Clamp(QualitySettings.names.Length / 2, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        int q = Mathf.Clamp(PlayerPrefs.GetInt("TankDuel.Quality", defaultQuality), 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        ApplyGraphicsPreference(q);

        ApplyTankSelection();
        EnsureEventSystem();
        CreateInterface();
        gameManager.MatchCompleted += ShowResult;

        if (DuelSceneTransition.ConsumeMatchStart(gameObject.scene.name))
        {
            isStartingMatch = true;
            HideAllMenuPanels();
            StartCoroutine(StartMatchAfterSceneReady());
        }
        else
        {
            DuelSceneTransition.Complete(gameObject.scene.name);
            ShowMainMenu();
        }
    }

    public static void ShowToast(string message, string context = null, Color? color = null)
    {
        if (Instance != null)
        {
            Instance.DisplayToast(message, context, color);
        }
    }

    private void DisplayToast(string message, string context, Color? color)
    {
        if (toastPanel == null || toastText == null) return;
        toastText.text = message;
        toastContext.text = context ?? UiCopy("GARAJ", "GARAGE");
        toastAccent.color = color ?? UiAccent;
        toastPanel.SetActive(true);

        if (activeToastCoroutine != null)
            StopCoroutine(activeToastCoroutine);
        activeToastCoroutine = StartCoroutine(ToastFadeRoutine());
    }

    private IEnumerator ToastFadeRoutine()
    {
        var group = toastPanel.GetComponent<CanvasGroup>();
        if (group == null) group = toastPanel.AddComponent<CanvasGroup>();

        group.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(elapsed / 0.15f);
            yield return null;
        }
        group.alpha = 1f;

        yield return new WaitForSecondsRealtime(2.0f);

        elapsed = 0f;
        while (elapsed < 0.35f)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = 1f - Mathf.Clamp01(elapsed / 0.35f);
            yield return null;
        }
        group.alpha = 0f;
        toastPanel.SetActive(false);
        activeToastCoroutine = null;
    }

    public void CleanupLegacyBanners()
    {
        CleanNullOverlayCameras();

        string[] targetNames = { "TitleScreen", "Title Screen", "Title", "Player Select", "Tank Select", "MobileControlCanvas", "GameUICanvas" };
        var allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var go in allObjects)
        {
            if (go == null) continue;
            if (canvas != null && go.transform.IsChildOf(canvas.transform)) continue;
            if (go.name.Contains("Tank Duel")) continue;

            string gName = go.name;

            // Handle legacy Menus cleanly without destroying camera or text references
            if (gName.Equals("Menus", StringComparison.OrdinalIgnoreCase))
            {
                var uiHandler = go.GetComponentInChildren<GameUIHandler>(true);
                if (uiHandler != null)
                {
                    uiHandler.enabled = false;
                    if (uiHandler.m_StartMenuRoot != null)
                        uiHandler.m_StartMenuRoot.gameObject.SetActive(false);
                    if (uiHandler.m_PauseMenuButton != null)
                        uiHandler.m_PauseMenuButton.gameObject.SetActive(false);
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
                SafeDestroyObjectWithCameras(go);
            }
        }

        var tmps = FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include);
        foreach (var tmp in tmps)
        {
            if (tmp == null) continue;
            string t = tmp.text ?? "";
            if (t.Contains("Player Select") || t.Contains("TANKS!") || t.Contains("Tank Select") || t.Contains("Select Tank"))
            {
                if (tmp.transform.parent != null && (tmp.transform.parent.name.Contains("Title") || tmp.transform.parent.name.Contains("Select")))
                {
                    SafeDestroyObjectWithCameras(tmp.transform.parent.gameObject);
                }
                else
                {
                    SafeDestroyObjectWithCameras(tmp.gameObject);
                }
            }
        }

        var tmpUGUIs = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include);
        foreach (var tmp in tmpUGUIs)
        {
            if (tmp == null) continue;
            if (canvas != null && tmp.transform.IsChildOf(canvas.transform)) continue;
            if (tmp.GetComponentInParent<MessageTextReference>() != null) continue;

            string t = tmp.text ?? "";
            if (t.Contains("Player Select") || t.Contains("TANKS!") || t.Contains("Tank Select"))
            {
                if (tmp.transform.parent != null && (tmp.transform.parent.name.Contains("Title") || tmp.transform.parent.name.Contains("Select")))
                {
                    SafeDestroyObjectWithCameras(tmp.transform.parent.gameObject);
                }
                else
                {
                    SafeDestroyObjectWithCameras(tmp.gameObject);
                }
            }
        }

        CleanNullOverlayCameras();
    }

    private void ApplyTankSelection()
    {
        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
        if (gameManager == null) return;

        if (availableTanks != null && availableTanks.Length >= 5)
        {
            int p1 = Mathf.Clamp(TankDuelData.Player1TankIndex, 0, availableTanks.Length - 1);
            int p2 = Mathf.Clamp(GetOpponentTankIndex(), 0, availableTanks.Length - 1);
            if (!TankDuelData.IsTankUnlocked(p1)) p1 = 0;
            if (!TankDuelData.AIModeEnabled && !TankDuelData.IsTankUnlocked(p2)) p2 = 0;
            if (availableTanks[p1] != null) gameManager.m_Tank1Prefab = availableTanks[p1];
            if (availableTanks[p2] != null) gameManager.m_Tank2Prefab = availableTanks[p2];
        }
    }

    private int GetOpponentTankIndex()
    {
        // A computer opponent uses a tank suited to its difficulty, independently of purchases.
        return TankDuelData.AIModeEnabled
            ? Mathf.Max(0, botTankIndex)
            : Mathf.Clamp(TankDuelData.Player2TankIndex, 0, Archetypes.Length - 1);
    }

    private static float GetBotMaxHealth()
    {
        return TankDuelData.AIDifficulty == 0 ? 35f : TankDuelData.AIDifficulty == 1 ? 45f : 50f;
    }

    private void ApplyArchetypesToSpawnedTanks()
    {
        if (gameManager == null || gameManager.m_SpawnPoints == null) return;

        int p1Tank = Mathf.Clamp(TankDuelData.Player1TankIndex, 0, Archetypes.Length - 1);
        int p2Tank = GetOpponentTankIndex();

        if (gameManager.m_SpawnPoints.Length > 0 && gameManager.m_SpawnPoints[0].m_Instance != null)
        {
            ApplyStatsToTank(gameManager.m_SpawnPoints[0].m_Instance, Archetypes[p1Tank]);
        }
        if (gameManager.m_SpawnPoints.Length > 1 && gameManager.m_SpawnPoints[1].m_Instance != null)
        {
            ApplyStatsToTank(gameManager.m_SpawnPoints[1].m_Instance, Archetypes[p2Tank],
                TankDuelData.AIModeEnabled ? GetBotMaxHealth() : Archetypes[p2Tank].health);
        }
    }

    private void ApplyStatsToTank(GameObject tankGo, TankArchetype arch, float healthOverride = -1f)
    {
        if (tankGo == null) return;

        var mov = tankGo.GetComponent<TankMovement>();
        if (mov != null)
        {
            mov.m_Speed = arch.speed;
            mov.m_TurnSpeed = arch.turnSpeed;
        }

        var shooting = tankGo.GetComponent<TankShooting>();
        if (shooting != null)
        {
            shooting.m_ShotCooldown = arch.cooldown;
            shooting.m_MaxDamage = arch.damage;
        }

        var health = tankGo.GetComponent<TankHealth>();
        if (health != null)
        {
            health.SetCustomMaxHealth(healthOverride > 0f ? healthOverride : arch.health);
        }
    }

    private IEnumerator StartMatchAfterSceneReady()
    {
        yield return null;
        StartMatch();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (gameManager != null) gameManager.MatchCompleted -= ShowResult;
        ClearGarageVisual();
        if (previewPedestalInstance != null)
        {
            Destroy(previewPedestalInstance.GetComponent<Renderer>().material);
            Destroy(previewPedestalInstance);
        }
        if (previewPedestalRingInstance != null)
        {
            Destroy(previewPedestalRingInstance.GetComponent<Renderer>().material);
            Destroy(previewPedestalRingInstance);
        }
        if (previewLightingRig != null) Destroy(previewLightingRig);
        ReleaseGaragePreviewCamera();
        Time.timeScale = 1f;
    }

    private void Update()
    {
        // 3D Garage Showcase Pedestal Rotation
        if (garagePanel != null && garagePanel.activeSelf)
        {
            if (previewTankInstance != null)
            {
                previewTankInstance.transform.Rotate(Vector3.up, 36f * Time.deltaTime, Space.World);
                if (previewPedestalInstance != null)
                {
                    previewPedestalInstance.transform.position = previewTankInstance.transform.position - new Vector3(0, 0.08f, 0);
                    previewPedestalInstance.transform.Rotate(Vector3.up, 18f * Time.deltaTime, Space.World);
                }
                if (previewPedestalRingInstance != null)
                {
                    previewPedestalRingInstance.transform.position = previewTankInstance.transform.position - new Vector3(0, 0.12f, 0);
                    previewPedestalRingInstance.transform.Rotate(Vector3.up, -14f * Time.deltaTime, Space.World);
                }
            }
        }

        if (!started || gameManager == null) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && !gameManager.IsMatchOver)
            SetPaused(!paused);

        if (hud == null || !hud.activeSelf) return;

        hudUpdateTimer += Time.deltaTime;
        if (hudUpdateTimer >= 0.04f)
        {
            hudUpdateTimer = 0f;
            UpdateHudTexts();
        }
    }

    private void UpdateHudTexts()
    {
        // Countdown timer & tension pulse
        if (gameManager.IsRoundPlaying)
        {
            float rem = gameManager.RemainingTime;
            timerText.text = Mathf.CeilToInt(rem).ToString("00");

            if (rem <= 10f)
            {
                float pulse = 1f + 0.12f * Mathf.Sin(Time.time * 12f);
                timerText.transform.localScale = new Vector3(pulse, pulse, 1f);
                timerText.color = Crimson;
            }
            else
            {
                timerText.transform.localScale = Vector3.one;
                timerText.color = OffWhite;
            }
        }
        else
        {
            timerText.text = "--";
            timerText.transform.localScale = Vector3.one;
            timerText.color = Muted;
        }

        // Score & Round
        int leftWins = gameManager.m_SpawnPoints[0].m_Wins;
        int rightWins = gameManager.m_SpawnPoints[1].m_Wins;
        scoreText.text = $"{leftWins}  –  {rightWins}";
        roundText.text = $"{TankDuelLocalization.Get("ROUND")} {gameManager.RoundNumber} / {gameManager.m_NumRoundsToWin}";

        if (p1WinsText != null) p1WinsText.text = $"{leftWins} {TankDuelLocalization.Get("VICTORIES")}";
        if (p2WinsText != null) p2WinsText.text = $"{rightWins} {TankDuelLocalization.Get("VICTORIES")}";

        // P1 Health Bar
        float curH1 = Health(0);
        float maxH1 = MaxHealth(0);
        float ratio1 = Mathf.Clamp01(curH1 / Mathf.Max(1f, maxH1));
        if (p1HealthFill != null)
        {
            p1HealthFill.sizeDelta = new Vector2(440f * ratio1, 8f);
        }
        if (p1HealthFillImg != null)
        {
            p1HealthFillImg.color = Color.Lerp(Crimson, gameManager.m_SpawnPoints[0].m_PlayerColor, ratio1);
        }
        if (p1HealthText != null)
        {
            p1HealthText.text = $"{Mathf.CeilToInt(curH1)} / {Mathf.CeilToInt(maxH1)}";
        }

        // P2 Health Bar
        float curH2 = Health(1);
        float maxH2 = MaxHealth(1);
        float ratio2 = Mathf.Clamp01(curH2 / Mathf.Max(1f, maxH2));
        if (p2HealthFill != null)
        {
            p2HealthFill.sizeDelta = new Vector2(440f * ratio2, 8f);
        }
        if (p2HealthFillImg != null)
        {
            p2HealthFillImg.color = Color.Lerp(Crimson, gameManager.m_SpawnPoints[1].m_PlayerColor, ratio2);
        }
        if (p2HealthText != null)
        {
            p2HealthText.text = $"{Mathf.CeilToInt(curH2)} / {Mathf.CeilToInt(maxH2)}";
        }
    }

    private float Health(int player)
    {
        if (cachedHealth[player] != null && cachedHealth[player].gameObject.activeInHierarchy)
            return cachedHealth[player].CurrentHealth;

        if (gameManager == null || gameManager.m_SpawnPoints == null || player >= gameManager.m_SpawnPoints.Length) return 0f;
        var instance = gameManager.m_SpawnPoints[player].m_Instance;
        if (instance == null || !instance.activeSelf) return 0f;
        cachedHealth[player] = instance.GetComponent<TankHealth>();
        return cachedHealth[player] == null ? 0f : cachedHealth[player].CurrentHealth;
    }

    private float MaxHealth(int player)
    {
        if (cachedHealth[player] != null)
            return cachedHealth[player].MaxHealth;
        if (gameManager == null || gameManager.m_SpawnPoints == null || player >= gameManager.m_SpawnPoints.Length) return 50f;
        var instance = gameManager.m_SpawnPoints[player].m_Instance;
        if (instance == null) return 50f;
        cachedHealth[player] = instance.GetComponent<TankHealth>();
        return cachedHealth[player] != null ? cachedHealth[player].MaxHealth : 50f;
    }

    private void EnsureEventSystem()
    {
        var current = FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (current == null)
        {
            var root = new GameObject("Duel Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
            root.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            return;
        }

        current.gameObject.SetActive(true);
        current.enabled = true;
        foreach (var module in current.GetComponents<BaseInputModule>())
            if (module is not InputSystemUIInputModule) module.enabled = false;

        var inputModule = current.GetComponent<InputSystemUIInputModule>();
        if (inputModule == null) inputModule = current.gameObject.AddComponent<InputSystemUIInputModule>();
        inputModule.enabled = true;
        inputModule.AssignDefaultActions();
    }

    private void CreateInterface()
    {
        var root = new GameObject("Tank Duel Interface", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.dynamicPixelsPerUnit = 1f;
        scaler.referencePixelsPerUnit = 100f;

        CreateTopBar();
        CreateMainMenuPanel();
        CreateGaragePanel();
        CreateSettingsPanel();
        CreateHudPanel();
        CreateToastPanel();
        CreatePausePanel();
        CreateResultPanel();
    }

    private void CreateTopBar() => BuildTopBar();

    private void CreateMainMenuPanel() => BuildMainMenuPanel();

    private void UpdateModeCards() => RefreshModeChoices();

    private void UpdateDifficultyButtons() => RefreshDifficultyChoices();

    private void OnStartMatchClicked()
    {
        if (started || isStartingMatch || DuelSceneTransition.IsLoading ||
            menuPanel == null || !menuPanel.activeInHierarchy) return;
        isStartingMatch = true;

        int chosen = TankDuelData.SelectRandomArena(ArenaScenes.Length);
        if (chosen == CurrentSceneIndex)
        {
            ApplyTankSelection();
            HideAllMenuPanels();
            StartCoroutine(StartMatchAfterSceneReady());
        }
        else
        {
            HideAllMenuPanels();
            LoadArena(ArenaScenes[chosen], true);
        }
    }

    private void HideAllMenuPanels()
    {
        SetMenuInput(false);
        if (menuPanel != null) menuPanel.SetActive(false);
        if (topBar != null) topBar.SetActive(false);
        if (garagePanel != null) garagePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (previewTankInstance != null) previewTankInstance.SetActive(false);
        if (previewPedestalInstance != null) previewPedestalInstance.SetActive(false);
        if (previewPedestalRingInstance != null) previewPedestalRingInstance.SetActive(false);
        if (previewLightingRig != null) previewLightingRig.SetActive(false);
        if (garagePreviewCamera != null) garagePreviewCamera.gameObject.SetActive(false);
    }

    private void ShowGarage()
    {
        currentGaragePlayer = 1;
        inspectedTankIndex = TankDuelData.Player1TankIndex;
        ShowPanel(garagePanel);
        UpdateGarageView();
    }

    private void CreateGaragePanel() => BuildGaragePanel();

    private void CreateStatBar(Transform parent, string title, float yPos, out RectTransform fillRect, out TextMeshProUGUI valText, Color barColor) => BuildStatBar(parent, title, yPos, out fillRect, out valText, barColor);

    private void CreateFrameBorder(Transform parent, Vector2 size, Color borderColor)
    {
        float w = size.x;
        float h = size.y;
        float thickness = 2f;
        float bracketLength = 36f;

        // Corner Bracket Top-Left
        CreateLine(parent, new Vector2(-w / 2 + bracketLength / 2, h / 2 - thickness / 2), new Vector2(bracketLength, thickness), borderColor);
        CreateLine(parent, new Vector2(-w / 2 + thickness / 2, h / 2 - bracketLength / 2), new Vector2(thickness, bracketLength), borderColor);

        // Corner Bracket Top-Right
        CreateLine(parent, new Vector2(w / 2 - bracketLength / 2, h / 2 - thickness / 2), new Vector2(bracketLength, thickness), borderColor);
        CreateLine(parent, new Vector2(w / 2 - thickness / 2, h / 2 - bracketLength / 2), new Vector2(thickness, bracketLength), borderColor);

        // Corner Bracket Bottom-Left
        CreateLine(parent, new Vector2(-w / 2 + bracketLength / 2, -h / 2 + thickness / 2), new Vector2(bracketLength, thickness), borderColor);
        CreateLine(parent, new Vector2(-w / 2 + thickness / 2, -h / 2 + bracketLength / 2), new Vector2(thickness, bracketLength), borderColor);

        // Corner Bracket Bottom-Right
        CreateLine(parent, new Vector2(w / 2 - bracketLength / 2, -h / 2 + thickness / 2), new Vector2(bracketLength, thickness), borderColor);
        CreateLine(parent, new Vector2(w / 2 - thickness / 2, -h / 2 + bracketLength / 2), new Vector2(thickness, bracketLength), borderColor);

        // Subtle faint perimeter guide lines
        Color faint = new Color(borderColor.r, borderColor.g, borderColor.b, 0.22f);
        CreateLine(parent, new Vector2(0, h / 2 - thickness / 2), new Vector2(w - bracketLength * 2, 1f), faint);
        CreateLine(parent, new Vector2(0, -h / 2 + thickness / 2), new Vector2(w - bracketLength * 2, 1f), faint);
        CreateLine(parent, new Vector2(-w / 2 + thickness / 2, 0), new Vector2(1f, h - bracketLength * 2), faint);
        CreateLine(parent, new Vector2(w / 2 - thickness / 2, 0), new Vector2(1f, h - bracketLength * 2), faint);
    }

    private GameObject CreateLine(Transform parent, Vector2 anchoredPos, Vector2 size, Color color)
    {
        var obj = new GameObject("Line", typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPos;
        obj.GetComponent<Image>().color = color;
        return obj;
    }

    private void UpdateGarageView()
    {
        if (TankDuelData.AIModeEnabled && currentGaragePlayer == 2)
        {
            currentGaragePlayer = 1;
            inspectedTankIndex = TankDuelData.Player1TankIndex;
        }
        if (garageTab2 != null)
            garageTab2.gameObject.SetActive(!TankDuelData.AIModeEnabled);
        // Update Tab highlights
        if (garageTab1 != null)
        {
            garageTab1.GetComponent<Image>().color = currentGaragePlayer == 1 ? UiSage : UiQuiet;
            garageTab1.GetComponentInChildren<TextMeshProUGUI>().color = currentGaragePlayer == 1 ? UiBackdrop : UiText;
        }
        if (garageTab2 != null)
        {
            garageTab2.GetComponent<Image>().color = currentGaragePlayer == 2 ? UiSage : UiQuiet;
            var tabLabel = garageTab2.GetComponentInChildren<TextMeshProUGUI>();
            tabLabel.color = currentGaragePlayer == 2 ? UiBackdrop : UiText;
            tabLabel.text = TankDuelData.AIModeEnabled
                ? UiCopy("BİLGİSAYAR", "COMPUTER")
                : TankDuelLocalization.Get("PLAYER_2");
        }

        // Update Dossier info for inspected tank
        var arch = Archetypes[inspectedTankIndex];
        if (dossierNameText != null) dossierNameText.text = arch.GetName();
        if (dossierRoleText != null)
        {
            dossierRoleText.text = arch.GetRole();
            dossierRoleText.color = UiSage;
        }
        if (dossierDescText != null) dossierDescText.text = arch.GetDesc();

        // 4 Visual Stat Bars
        float speedRatio = Mathf.Clamp01(arch.speed / 18f);
        if (speedStatFill != null) speedStatFill.sizeDelta = new Vector2(620f * speedRatio, 14f);
        if (speedStatVal != null) speedStatVal.text = $"{arch.speed:0} KM/S";

        float armorRatio = Mathf.Clamp01(arch.health / 80f);
        if (armorStatFill != null) armorStatFill.sizeDelta = new Vector2(620f * armorRatio, 14f);
        if (armorStatVal != null) armorStatVal.text = $"{arch.health:0} {TankDuelLocalization.Get("HEALTH_UNIT")}";

        float damageRatio = Mathf.Clamp01(arch.damage / 80f);
        if (damageStatFill != null) damageStatFill.sizeDelta = new Vector2(620f * damageRatio, 14f);
        if (damageStatVal != null) damageStatVal.text = $"{arch.damage:0} {TankDuelLocalization.Get("STAT_DAMAGE")}";

        float firerateRatio = Mathf.Clamp01((1.6f - arch.cooldown) / 1.0f);
        if (firerateStatFill != null) firerateStatFill.sizeDelta = new Vector2(620f * firerateRatio, 14f);
        if (firerateStatVal != null) firerateStatVal.text = $"{arch.cooldown:0.0}s {(TankDuelLocalization.IsTurkish ? "BEKLEME" : "COOLDOWN")}";

        // Action button state
        bool isUnlocked = TankDuelData.IsTankUnlocked(inspectedTankIndex);
        int equippedIndex = currentGaragePlayer == 1 ? TankDuelData.Player1TankIndex : TankDuelData.Player2TankIndex;
        bool isEquipped = (equippedIndex == inspectedTankIndex);

        if (isEquipped)
        {
            dossierActionButton.GetComponent<Image>().color = UiSage;
            dossierActionText.text = TankDuelLocalization.Get("SELECTED");
            dossierActionText.color = UiBackdrop;
        }
        else if (isUnlocked)
        {
            dossierActionButton.GetComponent<Image>().color = UiAccent;
            dossierActionText.text = TankDuelLocalization.Get("SELECT");
            dossierActionText.color = UiBackdrop;
        }
        else
        {
            bool canAfford = TankDuelData.Coins >= arch.price;
            dossierActionButton.GetComponent<Image>().color = canAfford ? UiAccent : UiQuiet;
            dossierActionText.text = $"{TankDuelLocalization.Get("BUY")}  •  {arch.price} {TankDuelLocalization.Get("COINS")}";
            dossierActionText.color = canAfford ? UiBackdrop : UiMuted;
        }

        // Bottom Dock highlights
        for (int i = 0; i < 5; i++)
        {
            bool btnInspected = (i == inspectedTankIndex);
            bool btnUnlocked = TankDuelData.IsTankUnlocked(i);
            bool btnEquipped = (equippedIndex == i);

            garageDockButtons[i].GetComponent<Image>().color = btnInspected ? UiRaised : UiQuiet;

            garageDockLabels[i].text = Archetypes[i].GetName();
            garageDockLabels[i].color = btnInspected ? UiText : UiMuted;

            if (btnEquipped)
            {
                garageDockSublabels[i].text = TankDuelLocalization.Get("SELECTED");
                garageDockSublabels[i].color = UiSage;
            }
            else if (btnUnlocked)
            {
                garageDockSublabels[i].text = TankDuelLocalization.Get("AVAILABLE");
                garageDockSublabels[i].color = UiMuted;
            }
            else
            {
                garageDockSublabels[i].text = $"{Archetypes[i].price} {TankDuelLocalization.Get("COINS")}";
                garageDockSublabels[i].color = UiAccent;
            }
        }

        if (coinsText != null) coinsText.text = GetCoinsLabel();

        // Update 3D showcase preview model
        UpdateGarageColorChoices();
        UpdateGarage3DPreview();
    }

    private void UpdateGarage3DPreview()
    {
        if (availableTanks == null || availableTanks.Length <= inspectedTankIndex) return;

        ClearGarageVisual();

        var prefab = availableTanks[inspectedTankIndex];
        if (prefab == null) return;

        previewTankInstance = CreateGarageVisual(prefab);

        // Apply team color to preview model with full URP property binding and rich armor sheen
        Color teamCol = TankDuelData.GetPlayerColor(currentGaragePlayer);
        ApplyGarageTankColor(teamCol);

        // Render the vehicle into the showroom's UI rectangle, independent of the arena camera.
        PrepareGaragePreviewCamera();
        Camera cam = garagePreviewCamera;
        Vector3 targetWorldPos = GaragePreviewOrigin;
        if (cam != null)
        {
            previewTankInstance.transform.position = targetWorldPos;
            previewTankInstance.transform.rotation = Quaternion.Euler(0f, 135f, 0f);
            previewTankInstance.transform.localScale = Vector3.one * 2.1f;
        }

        // 1. Emissive Turntable Pedestal
        if (previewPedestalInstance == null)
        {
            previewPedestalInstance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            previewPedestalInstance.name = "Garage_Showcase_Pedestal";
            var col = previewPedestalInstance.GetComponent<Collider>();
            if (col != null) Destroy(col);
            previewPedestalInstance.transform.localScale = new Vector3(5.0f, 0.08f, 5.0f);
        }

        var pedRend = previewPedestalInstance.GetComponent<Renderer>();
        if (pedRend != null)
        {
            var mat = pedRend.material;
            Color darkPlinth = new Color(0.09f, 0.13f, 0.19f);
            mat.color = darkPlinth;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", darkPlinth);
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", 0.80f);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0.60f);
        }

        previewPedestalInstance.SetActive(true);
            previewPedestalInstance.transform.position = targetWorldPos - new Vector3(0, 0.08f, 0);

        // 2. Glowing Outer Turntable Ring
        if (previewPedestalRingInstance == null)
        {
            previewPedestalRingInstance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            previewPedestalRingInstance.name = "Garage_Showcase_PedestalRing";
            var col = previewPedestalRingInstance.GetComponent<Collider>();
            if (col != null) Destroy(col);
            previewPedestalRingInstance.transform.localScale = new Vector3(5.6f, 0.04f, 5.6f);
        }

        var ringRend = previewPedestalRingInstance.GetComponent<Renderer>();
        if (ringRend != null)
        {
            var mat = ringRend.material;
            mat.color = teamCol;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", teamCol);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", teamCol * 0.15f);
            }
        }

        previewPedestalRingInstance.SetActive(true);
        previewPedestalRingInstance.transform.position = targetWorldPos - new Vector3(0, 0.12f, 0);

        SetGaragePreviewLayer(previewTankInstance);
        SetGaragePreviewLayer(previewPedestalInstance);
        SetGaragePreviewLayer(previewPedestalRingInstance);
        UpdateGarageStudioLighting(cam, targetWorldPos, teamCol);
        foreach (var light in previewLightingRig.GetComponentsInChildren<Light>())
            light.cullingMask = 1 << GaragePreviewLayer;
    }

    private void ApplyGarageTankColor(Color teamCol)
    {
        if (previewTankInstance == null) return;
        MeshRenderer[] renderers = previewTankInstance.GetComponentsInChildren<MeshRenderer>();
        foreach (var rend in renderers)
        {
            foreach (var mat in rend.sharedMaterials)
            {
                if (mat == null) continue;
                if (mat.name.Contains("TankColor") || mat.name.Contains("TankRed") ||
                    (mat.name.Contains("Tank") && !mat.name.Contains("Grey") && !mat.name.Contains("Window") && !mat.name.Contains("Lights")))
                {
                    mat.color = teamCol;
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", teamCol);
                    if (mat.HasProperty("_Color"))
                        mat.SetColor("_Color", teamCol);
                    if (mat.HasProperty("_Metallic"))
                        mat.SetFloat("_Metallic", 0.45f);
                    if (mat.HasProperty("_Smoothness"))
                        mat.SetFloat("_Smoothness", 0.65f);
                }
                else if (mat.name.Contains("TankLights"))
                {
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", teamCol * 2.5f);
                    }
                }
                else if (mat.name.Contains("TankGrey"))
                {
                    Color gunmetal = new Color(0.35f, 0.38f, 0.42f);
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", gunmetal);
                    if (mat.HasProperty("_Metallic"))
                        mat.SetFloat("_Metallic", 0.65f);
                    if (mat.HasProperty("_Smoothness"))
                        mat.SetFloat("_Smoothness", 0.50f);
                }
            }
        }
    }

    private void UpdateGarageStudioLighting(Camera cam, Vector3 tankPos, Color teamColor)
    {
        if (previewLightingRig == null)
        {
            previewLightingRig = new GameObject("Garage_Studio_Lighting");

            // Key Light
            var keyObj = new GameObject("Showcase_KeyLight");
            keyObj.transform.SetParent(previewLightingRig.transform);
            previewKeyLight = keyObj.AddComponent<Light>();
            previewKeyLight.type = LightType.Point;
            previewKeyLight.range = 25f;
            previewKeyLight.intensity = 3.6f;
            previewKeyLight.color = new Color(1f, 0.98f, 0.95f);

            // Fill Light
            var fillObj = new GameObject("Showcase_FillLight");
            fillObj.transform.SetParent(previewLightingRig.transform);
            previewFillLight = fillObj.AddComponent<Light>();
            previewFillLight.type = LightType.Point;
            previewFillLight.range = 25f;
            previewFillLight.intensity = 2.0f;
            previewFillLight.color = new Color(0.75f, 0.85f, 1.0f);

            // Team-Colored Rim Light
            var rimObj = new GameObject("Showcase_RimLight");
            rimObj.transform.SetParent(previewLightingRig.transform);
            previewRimLight = rimObj.AddComponent<Light>();
            previewRimLight.type = LightType.Point;
            previewRimLight.range = 20f;
            previewRimLight.intensity = 4.5f;

            // Under-Glow Light
            var underObj = new GameObject("Showcase_UnderLight");
            underObj.transform.SetParent(previewLightingRig.transform);
            previewUnderLight = underObj.AddComponent<Light>();
            previewUnderLight.type = LightType.Point;
            previewUnderLight.range = 10f;
            previewUnderLight.intensity = 3.0f;
        }

        previewLightingRig.SetActive(true);

        Vector3 camFwd = cam != null ? cam.transform.forward : Vector3.forward;
        Vector3 camRight = cam != null ? cam.transform.right : Vector3.right;
        Vector3 camUp = cam != null ? cam.transform.up : Vector3.up;

        if (previewKeyLight != null)
        {
            previewKeyLight.transform.position = tankPos - camFwd * 3.0f + camRight * 3.5f + camUp * 2.8f;
        }
        if (previewFillLight != null)
        {
            previewFillLight.transform.position = tankPos - camFwd * 2.5f - camRight * 3.5f + camUp * 1.5f;
        }
        if (previewRimLight != null)
        {
            previewRimLight.transform.position = tankPos + camFwd * 3.0f + camUp * 2.5f;
            previewRimLight.color = Color.Lerp(teamColor, Color.white, 0.35f);
        }
        if (previewUnderLight != null)
        {
            previewUnderLight.transform.position = tankPos - camUp * 0.8f;
            previewUnderLight.color = teamColor;
        }
    }

    private void OnDossierActionClicked()
    {
        if (TankDuelData.AIModeEnabled && currentGaragePlayer == 2) return;
        var arch = Archetypes[inspectedTankIndex];
        if (!TankDuelData.IsTankUnlocked(inspectedTankIndex))
        {
            if (TankDuelData.Coins < arch.price)
            {
                ShowToast(TankDuelLocalization.IsTurkish ?
                    $"YETERSİZ ALTIN! ({TankDuelData.Coins} / {arch.price} Altın)" :
                    $"NOT ENOUGH COINS! ({TankDuelData.Coins} / {arch.price})");
                return;
            }
            if (TankDuelData.TrySpendCoins(arch.price))
            {
                TankDuelData.UnlockTank(inspectedTankIndex);
                if (currentGaragePlayer == 1) TankDuelData.Player1TankIndex = inspectedTankIndex;
                else TankDuelData.Player2TankIndex = inspectedTankIndex;
                ShowToast($"{arch.GetName()} {TankDuelLocalization.Get("SELECTED")}");
                UpdateGarageView();
            }
            else
            {
                ShowToast(TankDuelLocalization.Get("NOT_ENOUGH_COINS"));
            }
        }
        else
        {
            if (currentGaragePlayer == 1) TankDuelData.Player1TankIndex = inspectedTankIndex;
            else TankDuelData.Player2TankIndex = inspectedTankIndex;
            UpdateGarageView();
        }
    }

    private void CreateSettingsPanel() => BuildSettingsPanel();

    private void RefreshSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            Destroy(settingsPanel);
            CreateSettingsPanel();
            ShowPanel(settingsPanel);
        }
    }

    private void ReloadWholeInterface()
    {
        if (canvas != null)
        {
            canvas.gameObject.SetActive(false);
            Destroy(canvas.gameObject);
        }
        CreateInterface();
        ShowPanel(settingsPanel);
    }

    private void ShowPanel(GameObject panel)
    {
        SetMenuInput(true);
        if (menuPanel != null) menuPanel.SetActive(panel == menuPanel);
        if (garagePanel != null) garagePanel.SetActive(panel == garagePanel);
        if (settingsPanel != null) settingsPanel.SetActive(panel == settingsPanel);
        if (hud != null) hud.SetActive(false);
        if (topBar != null)
        {
            bool showCurrency = panel == menuPanel || panel == garagePanel;
            if (showCurrency)
            {
                var host = panel == menuPanel ? menuPanel.transform.Find("Match setup") :
                    garagePanel.transform.Find("Garage navigation");
                topBar.transform.SetParent(host, false);
                topBar.GetComponent<RectTransform>().anchoredPosition =
                    panel == menuPanel ? new Vector2(282, 362) : new Vector2(744, 0);
                coinsText.text = GetCoinsLabel();
            }
            topBar.SetActive(showCurrency);
            topBar.transform.SetAsLastSibling();
        }

        if (panel != garagePanel)
        {
            if (garagePreviewCamera != null) garagePreviewCamera.gameObject.SetActive(false);
            if (previewTankInstance != null) previewTankInstance.SetActive(false);
            if (previewPedestalInstance != null) previewPedestalInstance.SetActive(false);
            if (previewPedestalRingInstance != null) previewPedestalRingInstance.SetActive(false);
            if (previewLightingRig != null) previewLightingRig.SetActive(false);
        }
    }

    private void ShowMainMenu()
    {
        ShowPanel(menuPanel);
        UpdateModeCards();
    }

    private void CreateHudPanel() => BuildMatchHud();

    private void CreateToastPanel()
    {
        toastPanel = UiBlock(canvas.transform, "ToastPanel", Vector2.zero,
            new Vector2(460, 88), new Color(0.045f, 0.07f, 0.06f, 0.70f));
        var tpr = toastPanel.GetComponent<RectTransform>();
        tpr.anchorMin = tpr.anchorMax = new Vector2(0.5f, 1f);
        tpr.pivot = new Vector2(0.5f, 1f);
        tpr.anchoredPosition = new Vector2(0, -174);
        var group = toastPanel.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        toastAccent = UiBlock(toastPanel.transform, "Accent", new Vector2(-226, 0),
            new Vector2(4, 52), UiAccent, false).GetComponent<Image>();
        toastContext = UiLabel(toastPanel.transform, "", 17, UiMuted,
            new Vector2(0, 21), new Vector2(410, 28));
        toastText = UiLabel(toastPanel.transform, "", 25, UiText,
            new Vector2(0, -14), new Vector2(410, 38), bold: true);
        toastPanel.SetActive(false);
    }

    private void CreatePausePanel() => BuildPausePanel();

    private void CreateResultPanel() => BuildResultPanel();

    private void StartMatch()
    {
        if (started) return;
        if (TankDuelData.AIModeEnabled && botTankIndex < 0)
            botTankIndex = TankDuelData.SelectBotTank(TankDuelData.AIDifficulty);
        Time.timeScale = 1f;

        CleanNullOverlayCameras();

        // Eradicate in-world 3D banners ("TANKS! Player Select")
        CleanupLegacyBanners();

        ApplyTankSelection();
        ApplyMusicPreference();

        // Clean up stray duplicate tanks
        var existingTanks = FindObjectsByType<TankMovement>(FindObjectsInactive.Include);
        foreach (var t in existingTanks)
        {
            if (t != null) Destroy(t.gameObject);
        }

        // Clean up duplicate Canvases if any exist
        var existingCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        foreach (var c in existingCanvases)
        {
            if (c != canvas && c.gameObject.name == "Tank Duel Interface")
                Destroy(c.gameObject);
        }

        HideAllMenuPanels();

        gameManager.m_RoundDuration = RoundLengths[TankDuelData.DurationIndex];
        bool isAI = TankDuelData.AIModeEnabled;

        // Ensure prefabs are valid
        if (gameManager.m_Tank1Prefab == null && availableTanks != null && availableTanks.Length > 0)
            gameManager.m_Tank1Prefab = availableTanks[0];
        if (gameManager.m_Tank2Prefab == null && availableTanks != null && availableTanks.Length > 1)
            gameManager.m_Tank2Prefab = availableTanks[1];

        var players = new[]
        {
            new GameManager.PlayerData { UsedPrefab = gameManager.m_Tank1Prefab, TankColor = TankDuelData.GetPlayerColor(1), ControlIndex = 1, IsComputer = false },
            new GameManager.PlayerData { UsedPrefab = gameManager.m_Tank2Prefab, TankColor = TankDuelData.GetOpponentColor(), ControlIndex = 2, IsComputer = isAI }
        };

        gameManager.StartGame(players);
        if (!gameManager.HasStarted)
        {
            isStartingMatch = false;
            DuelSceneTransition.Complete(gameObject.scene.name);
            ShowMainMenu();
            return;
        }

        started = true;
        DuelSceneTransition.Complete(gameObject.scene.name);
        cachedHealth[0] = null;
        cachedHealth[1] = null;

        if (isAI && hud != null)
        {
            var marker = hud.transform.parent.GetComponentInChildren<DuelOpponentMarker>(true);
            if (marker == null)
            {
                var markerObject = new GameObject("Opponent direction", typeof(RectTransform));
                markerObject.transform.SetParent(hud.transform.parent, false);
                marker = markerObject.AddComponent<DuelOpponentMarker>();
            }
            marker.Configure(Camera.main, gameManager.m_SpawnPoints[1].m_Instance.transform,
                hud, bodyFont, gameManager.m_SpawnPoints[1].m_PlayerColor);
        }

        // Apply asymmetric vehicle archetype performance stats
        ApplyArchetypesToSpawnedTanks();

        if (p1NameText != null)
        {
            int p1Idx = Mathf.Clamp(TankDuelData.Player1TankIndex, 0, Archetypes.Length - 1);
            p1NameText.text = UiCopy("1. oyuncu", "Player 1");
            p1TankText.text = HudTankName(p1Idx);
        }
        if (p2NameText != null)
        {
            int p2Idx = GetOpponentTankIndex();
            p2NameText.text = TankDuelData.AIModeEnabled ? UiCopy("Bilgisayar", "Computer") : UiCopy("2. oyuncu", "Player 2");
            p2TankText.text = HudTankName(p2Idx);
        }

        if (hud != null)
        {
            for (int i = 0; i < hudAccents.Length; i++)
                if (hudAccents[i] != null) hudAccents[i].color = gameManager.m_SpawnPoints[i].m_PlayerColor;
            hud.SetActive(true);
            UpdateHudTexts();
        }
    }

    private void SetPaused(bool value)
    {
        if (!started || gameManager == null || (value && gameManager.IsMatchOver) ||
            DuelSceneTransition.IsLoading) return;
        SetMenuInput(value);
        if (value && toastPanel != null) toastPanel.SetActive(false);
        paused = value;
        pause.SetActive(value);
        Time.timeScale = value ? 0f : 1f;
    }

    private void ShowResult()
    {
        if (result.activeSelf || DuelSceneTransition.IsLoading) return;
        if (paused) SetPaused(false);
        var winner = gameManager.GameWinner;

        var headingText = resultHeadingText;
        var subText = resultSubtitleText;

        if (winner == null)
        {
            headingText.text = TankDuelLocalization.Get("DRAW");
            if (subText != null)
                subText.text = TankDuelLocalization.IsTurkish ? "RAUNDLAR EŞİT BİTTİ" : "ROUNDS TIED";
        }
        else
        {
            if (winner.m_PlayerNumber == 1)
            {
                headingText.text = TankDuelLocalization.Get("P1_WINS");
                if (subText != null)
                {
                    string bonus = TankDuelLocalization.Get("PU_COIN");
                    subText.text = TankDuelLocalization.IsTurkish ?
                        $"TEBRİKLER! ZAFER SENİN\n<color=#F59E0B>{bonus}</color>" :
                        $"CONGRATULATIONS! VICTORY\n<color=#F59E0B>{bonus}</color>";
                }
            }
            else
            {
                headingText.text = TankDuelData.AIModeEnabled ?
                    TankDuelLocalization.Get("BOT_WINS") :
                    TankDuelLocalization.Get("P2_WINS");
                if (subText != null)
                {
                    string bonus = TankDuelLocalization.Get("PU_COIN");
                    subText.text = TankDuelData.AIModeEnabled ?
                        (TankDuelLocalization.IsTurkish ? $"BİLGİSAYAR KAZANDI\n<color=#F59E0B>{bonus}</color>" : $"BOT WON\n<color=#F59E0B>{bonus}</color>") :
                        (TankDuelLocalization.IsTurkish ? $"2. OYUNCU KAZANDI\n<color=#F59E0B>{bonus}</color>" : $"PLAYER 2 WON\n<color=#F59E0B>{bonus}</color>");
                }
            }
        }

        TankDuelData.AddCoins(50);
        result.SetActive(true);
        SetMenuInput(true);
        if (toastPanel != null) toastPanel.SetActive(false);
    }

    private void Rematch()
    {
        if (DuelSceneTransition.IsLoading || result == null || !result.activeInHierarchy ||
            gameManager == null || !gameManager.IsMatchOver) return;
        result.SetActive(false);
        int chosen = TankDuelData.SelectRandomArena(ArenaScenes.Length);
        LoadArena(ArenaScenes[chosen], true);
    }

    private void ReturnToMenu()
    {
        if (DuelSceneTransition.IsLoading || (!paused && !result.activeInHierarchy)) return;
        LoadArena(SceneManager.GetActiveScene().name, false);
    }

    private void LoadArena(string scene, bool play)
    {
        if (!DuelSceneTransition.TryBegin(scene, play)) return;
        SetMenuInput(false);
        if (canvas != null) canvas.gameObject.SetActive(false);
        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
    }

    private static void SetMenuInput(bool enabled)
    {
        if (EventSystem.current == null) return;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.sendNavigationEvents = enabled;
    }

    private GameObject Panel(string name, Color background, bool fullScreen)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(canvas.transform, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = fullScreen ? Vector2.zero : new Vector2(0.5f, 0.5f);
        rect.anchorMax = fullScreen ? Vector2.one : new Vector2(0.5f, 0.5f);
        if (fullScreen) { rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; }
        else { rect.sizeDelta = new Vector2(1100, 200); }
        go.GetComponent<Image>().color = background;
        go.GetComponent<Image>().raycastTarget = fullScreen;
        return go;
    }

    private TextMeshProUGUI Heading(Transform parent, string content, float size, Color color, Vector2 position) =>
        Label(parent, content, size, color, position, new Vector2(1300, 100), true);

    private TextMeshProUGUI Label(Transform parent, string content, float size, Color color, Vector2 position, Vector2 bounds, bool isHeading = false)
    {
        var go = new GameObject(content.Length > 24 ? "Label" : content, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = bounds; rect.anchoredPosition = position;
        var text = go.GetComponent<TextMeshProUGUI>();

        var selectedFont = isHeading ? titleFont : bodyFont;
        if (selectedFont != null)
        {
            TankDuelLocalization.EnsureTurkishSupport(selectedFont);
            text.font = selectedFont;
            text.fontSharedMaterial = selectedFont.material;
        }

        text.text = content;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.enableAutoSizing = false;
        text.raycastTarget = false;
        text.extraPadding = true;
        return text;
    }

    private GameObject Button(Transform parent, string title, Vector2 position, Color color, Action click, Vector2? size = null, float fontSize = 24f, bool isHeading = false)
    {
        var go = new GameObject(title, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        Vector2 s = size ?? new Vector2(430, 72);
        rect.sizeDelta = s; rect.anchoredPosition = position;
        var img = go.GetComponent<Image>();
        img.color = color;
        var button = go.GetComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(() => click?.Invoke());
        Color textCol = (color == CardBg) ? OffWhite : Ink;
        Label(go.transform, title, fontSize, textCol, Vector2.zero, s - new Vector2(10, 8), isHeading);
        return go;
    }
}
