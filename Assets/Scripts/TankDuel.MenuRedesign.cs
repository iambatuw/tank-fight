using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class TankDuel
{
    // Restrained charcoal and blue menu colours; team paint remains independent.
    private static readonly Color UiBackdrop = new Color(0.043f, 0.051f, 0.067f, 1f);
    private static readonly Color UiSurface = new Color(0.062f, 0.074f, 0.094f, 1f);
    private static readonly Color UiRaised = new Color(0.160f, 0.205f, 0.260f, 1f);
    private static readonly Color UiQuiet = new Color(0.105f, 0.128f, 0.163f, 1f);
    private static readonly Color UiText = new Color(0.860f, 0.893f, 0.935f, 1f);
    private static readonly Color UiMuted = new Color(0.600f, 0.670f, 0.750f, 1f);
    private static readonly Color UiAccent = new Color(0.460f, 0.610f, 0.790f, 1f);
    private static readonly Color UiSage = new Color(0.320f, 0.510f, 0.730f, 1f);
    private static readonly Color UiRule = new Color(0.390f, 0.480f, 0.590f, 0.50f);
    private static readonly Color UiInk = new Color(0.038f, 0.050f, 0.068f, 1f);

    private static Sprite uiRoundedSprite;
    private TextMeshProUGUI resultHeadingText;
    private TextMeshProUGUI resultSubtitleText;
    private Camera garagePreviewCamera;
    private RenderTexture garagePreviewTexture;
    private RawImage garagePreviewImage;
    private static readonly Vector3 GaragePreviewOrigin = new Vector3(2000f, 2000f, 2000f);
    private const int GaragePreviewLayer = 31;
    private readonly List<Material> garagePreviewMaterials = new List<Material>();
    private Button[] garageColorButtons;
    private TextMeshProUGUI garageColorNameText;
    private readonly Image[] hudAccents = new Image[2];

    private string HudTankName(int index)
    {
        var culture = System.Globalization.CultureInfo.GetCultureInfo(TankDuelLocalization.IsTurkish ? "tr-TR" : "en-US");
        return culture.TextInfo.ToTitleCase(Archetypes[index].GetName().ToLower(culture));
    }

    private void BuildMatchHud()
    {
        hud = new GameObject("MATCH HUD", typeof(RectTransform));
        hud.transform.SetParent(canvas.transform, false);
        var rect = hud.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(1760, 144);
        rect.anchoredPosition = new Vector2(0, -20);

        BuildPlayerHud(0, -600, out p1NameText, out p1TankText, out p1WinsText,
            out p1HealthText, out p1HealthFill, out p1HealthFillImg);
        BuildPlayerHud(1, 600, out p2NameText, out p2TankText, out p2WinsText,
            out p2HealthText, out p2HealthFill, out p2HealthFillImg);
        var center = UiBlock(hud.transform, "Center_Card", Vector2.zero,
            new Vector2(280, 126), new Color(0.045f, 0.07f, 0.06f, 0.62f));
        timerText = UiLabel(center.transform, "90", 40, UiText,
            new Vector2(0, 30), new Vector2(252, 50), TextAlignmentOptions.Center, true);
        timerText.fontSizeMin = 36;
        scoreText = UiLabel(center.transform, "0  –  0", 24, UiText,
            new Vector2(0, -14), new Vector2(240, 34), TextAlignmentOptions.Center, true);
        roundText = UiLabel(center.transform, TankDuelLocalization.Get("ROUND") + " 1 / 3", 16,
            new Color(0.85f, 0.88f, 0.83f), new Vector2(0, -44), new Vector2(240, 26), TextAlignmentOptions.Center);
        hud.SetActive(false);
    }

    private void BuildPlayerHud(int player, float x, out TextMeshProUGUI nameText,
        out TextMeshProUGUI tankText, out TextMeshProUGUI winsText,
        out TextMeshProUGUI healthText, out RectTransform healthFill, out Image healthImage)
    {
        Color color = player == 0 ? TankDuelData.GetPlayerColor(1) : TankDuelData.GetOpponentColor();
        var card = UiBlock(hud.transform, player == 0 ? "P1_Card" : "P2_Card", new Vector2(x, 0),
            new Vector2(480, 126), new Color(0.045f, 0.07f, 0.06f, 0.60f));
        hudAccents[player] = UiBlock(card.transform, "Player color", new Vector2(-238, 0),
            new Vector2(4, 84), color, false).GetComponent<Image>();
        nameText = UiLabel(card.transform, player == 0 ? UiCopy("1. oyuncu", "Player 1") : UiCopy("Rakip", "Opponent"),
            24, UiText, new Vector2(-68, 32), new Vector2(304, 34), bold: true);
        var badge = UiBlock(card.transform, "Victories", new Vector2(165, 32),
            new Vector2(110, 34), new Color(1, 1, 1, 0.08f));
        winsText = UiLabel(badge.transform, "0 " + TankDuelLocalization.Get("VICTORIES"), 17,
            UiText, Vector2.zero, new Vector2(102, 30), TextAlignmentOptions.Center);
        tankText = UiLabel(card.transform, "", 21, new Color(0.87f, 0.90f, 0.86f),
            new Vector2(-74, -7), new Vector2(292, 32));
        healthText = UiLabel(card.transform, "50 / 50", 21, UiText,
            new Vector2(162, -7), new Vector2(116, 32), TextAlignmentOptions.Right, true);
        var track = UiBlock(card.transform, "Health track", new Vector2(0, -40),
            new Vector2(440, 8), new Color(1, 1, 1, 0.16f));
        var fill = UiBlock(track.transform, "Health fill", Vector2.zero, new Vector2(440, 8), color);
        healthFill = fill.GetComponent<RectTransform>();
        healthFill.anchorMin = healthFill.anchorMax = new Vector2(0, 0.5f);
        healthFill.pivot = new Vector2(0, 0.5f);
        healthFill.anchoredPosition = Vector2.zero;
        healthImage = fill.GetComponent<Image>();
    }

    public static void ApplyMusicPreference()
    {
        AudioListener.volume = 1f;
        foreach (var source in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include))
        {
            if (source == null) continue;
            bool isMusic = source.name.IndexOf("Music", StringComparison.OrdinalIgnoreCase) >= 0 ||
                (source.clip != null && source.clip.name.IndexOf("Music", StringComparison.OrdinalIgnoreCase) >= 0);
            if (isMusic) source.mute = !TankDuelData.MusicEnabled;
        }
    }

    private static void ApplyGraphicsPreference(int quality)
    {
        QualitySettings.SetQualityLevel(quality, true);
        int highest = Mathf.Max(0, QualitySettings.names.Length - 1);
        QualitySettings.shadowDistance = quality == 0 ? 25f : quality >= highest ? 65f : 45f;
    }

    private GameObject CreateGarageVisual(GameObject prefab)
    {
        var visual = new GameObject("Garage_Showcase_Tank");
        CopyGarageMeshes(prefab.transform, visual.transform);
        return visual;
    }

    private void CopyGarageMeshes(Transform source, Transform parent)
    {
        if (!source.gameObject.activeSelf) return;
        var node = new GameObject(source.name);
        node.transform.SetParent(parent, false);
        node.transform.localPosition = source.localPosition;
        node.transform.localRotation = source.localRotation;
        node.transform.localScale = source.localScale;
        var mesh = source.GetComponent<MeshFilter>();
        var renderer = source.GetComponent<MeshRenderer>();
        if (mesh != null && renderer != null)
        {
            node.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
            var copy = node.AddComponent<MeshRenderer>();
            var original = renderer.sharedMaterials;
            var materials = new Material[original.Length];
            for (int i = 0; i < original.Length; i++)
            {
                if (original[i] == null) continue;
                materials[i] = new Material(original[i]);
                garagePreviewMaterials.Add(materials[i]);
            }
            copy.sharedMaterials = materials;
            copy.shadowCastingMode = renderer.shadowCastingMode;
            copy.receiveShadows = renderer.receiveShadows;
        }
        foreach (Transform child in source)
            CopyGarageMeshes(child, node.transform);
    }

    private void ClearGarageVisual()
    {
        if (previewTankInstance != null)
        {
            previewTankInstance.SetActive(false);
            Destroy(previewTankInstance);
            previewTankInstance = null;
        }
        foreach (var material in garagePreviewMaterials)
            if (material != null) Destroy(material);
        garagePreviewMaterials.Clear();
    }

    private static string UiCopy(string turkish, string english) =>
        TankDuelLocalization.IsTurkish ? turkish : english;

    private string GetCoinsLabel() => TankDuelData.Coins.ToString("N0",
        System.Globalization.CultureInfo.GetCultureInfo(TankDuelLocalization.IsTurkish ? "tr-TR" : "en-US"));

    internal static Sprite RoundedSprite()
    {
        if (uiRoundedSprite != null) return uiRoundedSprite;
        const int side = 32;
        const float radius = 8f;
        var texture = new Texture2D(side, side, TextureFormat.RGBA32, false);
        texture.name = "TankDuel UI rounded rectangle";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < side; y++)
        {
            for (int x = 0; x < side; x++)
            {
                float px = Mathf.Clamp(x + 0.5f, radius, side - radius);
                float py = Mathf.Clamp(y + 0.5f, radius, side - radius);
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(px, py));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius + 0.5f - distance)));
            }
        }
        texture.Apply();
        uiRoundedSprite = Sprite.Create(texture, new Rect(0, 0, side, side), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(10, 10, 10, 10));
        return uiRoundedSprite;
    }

    private GameObject UiBlock(Transform parent, string name, Vector2 position, Vector2 size, Color color, bool rounded = true)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        if (rounded)
        {
            image.sprite = RoundedSprite();
            image.type = Image.Type.Sliced;
        }
        return go;
    }

    private TextMeshProUGUI UiLabel(Transform parent, string content, float size, Color color,
        Vector2 position, Vector2 bounds, TextAlignmentOptions alignment = TextAlignmentOptions.Left,
        bool bold = false, bool wrap = false)
    {
        var label = Label(parent, content, size, color, position, bounds);
        label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        label.alignment = alignment;
        label.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        label.enableAutoSizing = true;
        label.fontSizeMin = size * 0.79f;
        label.fontSizeMax = size;
        label.overflowMode = TextOverflowModes.Truncate;
        return label;
    }

    private TextMeshProUGUI UiDisplay(Transform parent, string content, float size, Color color,
        Vector2 position, Vector2 bounds, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        var label = UiLabel(parent, content, size, color, position, bounds, alignment, false, true);
        if (titleFont != null)
        {
            label.font = titleFont;
            label.fontSharedMaterial = titleFont.material;
        }
        label.fontStyle = FontStyles.Bold;
        label.fontSizeMin = size * 0.88f;
        return label;
    }

    private Button UiButton(Transform parent, string name, string title, Vector2 position, Vector2 size,
        Color background, Color foreground, Action click, float textSize = 23f)
    {
        var go = UiBlock(parent, name, position, size, background, false);
        var image = go.GetComponent<Image>();
        image.raycastTarget = true;
        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.13f, 1.13f, 1.13f, 1f);
        colors.pressedColor = new Color(0.81f, 0.81f, 0.81f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;
        button.onClick.AddListener(() =>
        {
            if (button != null && button.isActiveAndEnabled && button.interactable &&
                !DuelSceneTransition.IsLoading) click?.Invoke();
        });
        if (!string.IsNullOrEmpty(title))
            UiLabel(go.transform, title, textSize, foreground, Vector2.zero,
                size - new Vector2(24, 12), TextAlignmentOptions.Center, true);
        return button;
    }

    private static void PaintChoice(Button button, TextMeshProUGUI label, bool selected, Color selectedColor)
    {
        button.GetComponent<Image>().color = selected ? selectedColor : UiQuiet;
        label.color = selected ? UiInk : UiText;
    }

    private void UiRuleLine(Transform parent, Vector2 position, float width) =>
        UiBlock(parent, "Divider", position, new Vector2(width, 2), UiRule, false);

    private void BuildTopBar()
    {
        topBar = new GameObject("Currency", typeof(RectTransform));
        topBar.transform.SetParent(canvas.transform, false);
        var rect = topBar.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(218, 68);
        UiBlock(topBar.transform, "Currency plate", Vector2.zero, rect.sizeDelta, UiSurface, false);
        UiBlock(topBar.transform, "Coin", new Vector2(-84, 0), new Vector2(18, 18), UiAccent);
        UiLabel(topBar.transform, TankDuelLocalization.Get("COINS"), 15, UiMuted,
            new Vector2(19, 17), new Vector2(142, 24), TextAlignmentOptions.Left, true);
        coinsText = UiLabel(topBar.transform, GetCoinsLabel(), 27, UiText,
            new Vector2(19, -12), new Vector2(142, 36), TextAlignmentOptions.Left, true);
        coinsText.fontSizeMin = 15f;
    }

    private void RefreshModeChoices()
    {
        bool solo = TankDuelData.AIModeEnabled;
        for (int i = 0; i < modeCards.Length; i++)
            PaintChoice(modeCards[i], modeCardTitles[i], (i == 0) == solo, UiSage);
        modeHintLabel.text = solo
            ? UiCopy("WASD + Boşluk  •  Rakip: bilgisayar",
                "WASD + Space  •  Opponent: computer")
            : UiCopy("WASD + Boşluk  •  Yön tuşları + Enter",
                "WASD + Space  •  Arrows + Enter");
        if (difficultySection != null) difficultySection.SetActive(solo);
        if (twoPlayerInstructions != null) twoPlayerInstructions.SetActive(!solo);
        RefreshDifficultyChoices();
    }

    private void RefreshDifficultyChoices()
    {
        for (int i = 0; i < diffButtons.Length; i++)
            PaintChoice(diffButtons[i], diffButtonLabels[i], i == TankDuelData.AIDifficulty, UiSage);
        diffHintLabel.text = TankDuelData.AIDifficulty == 0 ?
            UiCopy("35 can • Sakin tempo, uzun atış aralığı", "35 health • Calm pace, slower shots") :
            TankDuelData.AIDifficulty == 1 ?
                UiCopy("45 can • Dengeli hız ve isabet", "45 health • Balanced speed and accuracy") :
                UiCopy("50 can • Hızlı, agresif rakip", "50 health • Fast, aggressive opponent");
    }

    private void UpdateGarageColorChoices()
    {
        int selected = TankDuelData.GetPlayerColorIndex(currentGaragePlayer);
        if (garageColorNameText != null)
            garageColorNameText.text = TankDuelData.GetTankColorName(selected);
        if (garageColorButtons == null) return;
        for (int i = 0; i < garageColorButtons.Length; i++)
            garageColorButtons[i].GetComponent<Image>().color = i == selected ? UiText : UiQuiet;
    }

    private void SelectGarageColor(int index)
    {
        if (TankDuelData.AIModeEnabled && currentGaragePlayer != 1) return;
        TankDuelData.SetPlayerColorIndex(currentGaragePlayer, index);
        UpdateGarageColorChoices();
        // Reuse the existing preview and materials so repainting does not rebuild the vehicle.
        Color color = TankDuelData.GetPlayerColor(currentGaragePlayer);
        ApplyGarageTankColor(color);
        if (previewPedestalRingInstance != null)
        {
            var material = previewPedestalRingInstance.GetComponent<Renderer>().material;
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color * 0.15f);
        }
        if (garagePreviewCamera != null)
            UpdateGarageStudioLighting(garagePreviewCamera, GaragePreviewOrigin, color);
    }

    private void PrepareGaragePreviewCamera()
    {
        if (garagePreviewTexture == null)
        {
            garagePreviewTexture = new RenderTexture(1024, 576, 24, RenderTextureFormat.ARGB32);
            garagePreviewTexture.name = "Garage vehicle preview";
            garagePreviewTexture.antiAliasing = 2;
            garagePreviewTexture.Create();
        }
        if (garagePreviewCamera == null)
        {
            var cameraObject = new GameObject("Garage preview camera", typeof(Camera));
            garagePreviewCamera = cameraObject.GetComponent<Camera>();
            garagePreviewCamera.clearFlags = CameraClearFlags.SolidColor;
            garagePreviewCamera.backgroundColor = UiBackdrop;
            garagePreviewCamera.cullingMask = 1 << GaragePreviewLayer;
            garagePreviewCamera.orthographic = true;
            garagePreviewCamera.orthographicSize = 3.0f;
            garagePreviewCamera.nearClipPlane = 0.1f;
            garagePreviewCamera.farClipPlane = 60f;
            garagePreviewCamera.allowHDR = false;
            garagePreviewCamera.targetTexture = garagePreviewTexture;
            cameraObject.transform.position = GaragePreviewOrigin + new Vector3(9f, 5.4f, -13f);
            cameraObject.transform.LookAt(GaragePreviewOrigin + Vector3.up * 1.0f);
        }
        garagePreviewCamera.gameObject.SetActive(true);
        if (garagePreviewImage != null) garagePreviewImage.texture = garagePreviewTexture;
    }

    private static void SetGaragePreviewLayer(GameObject root)
    {
        foreach (var child in root.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = GaragePreviewLayer;
    }

    private void ReleaseGaragePreviewCamera()
    {
        if (garagePreviewCamera != null)
        {
            garagePreviewCamera.targetTexture = null;
            Destroy(garagePreviewCamera.gameObject);
        }
        if (garagePreviewTexture != null)
        {
            garagePreviewTexture.Release();
            Destroy(garagePreviewTexture);
        }
    }

    private void BuildPausePanel()
    {
        pause = Panel("PAUSED", new Color(UiBackdrop.r, UiBackdrop.g, UiBackdrop.b, 0.88f), true);
        var card = UiBlock(pause.transform, "Pause card", Vector2.zero, new Vector2(900, 670), UiSurface);
        UiLabel(card.transform, UiCopy("OYUN DURAKLATILDI", "GAME PAUSED"), 19, UiAccent,
            new Vector2(0, 251), new Vector2(760, 34), TextAlignmentOptions.Center, true);
        UiLabel(card.transform, TankDuelLocalization.Get("PAUSED"), 68, UiText,
            new Vector2(0, 180), new Vector2(760, 92), TextAlignmentOptions.Center, true);
        UiRuleLine(card.transform, new Vector2(0, 113), 730);
        string opponent = TankDuelData.AIModeEnabled ?
            UiCopy("Rakip: bilgisayar", "Opponent: computer") : TankDuelLocalization.Get("CONTROLS_P2");
        UiLabel(card.transform, TankDuelLocalization.Get("CONTROLS_P1"), 22, UiMuted,
            new Vector2(0, 61), new Vector2(750, 39), TextAlignmentOptions.Center);
        UiLabel(card.transform, opponent, 22, UiMuted,
            new Vector2(0, 23), new Vector2(750, 39), TextAlignmentOptions.Center);
        UiButton(card.transform, "Resume", TankDuelLocalization.Get("RESUME"),
            new Vector2(0, -95), new Vector2(720, 74), UiAccent, UiBackdrop,
            () => SetPaused(false), 26);
        UiButton(card.transform, "Main menu", TankDuelLocalization.Get("MAIN_MENU"),
            new Vector2(0, -191), new Vector2(720, 66), UiQuiet, UiText, ReturnToMenu, 23);
        UiLabel(card.transform, "ESC", 17, UiMuted, new Vector2(0, -273),
            new Vector2(150, 28), TextAlignmentOptions.Center);
        pause.SetActive(false);
    }

    private void BuildResultPanel()
    {
        result = Panel("MATCH COMPLETE", new Color(UiBackdrop.r, UiBackdrop.g, UiBackdrop.b, 0.89f), true);
        var card = UiBlock(result.transform, "Result card", Vector2.zero, new Vector2(980, 690), UiSurface);
        UiLabel(card.transform, UiCopy("MAÇ SONU", "MATCH COMPLETE"), 19, UiAccent,
            new Vector2(0, 271), new Vector2(760, 34), TextAlignmentOptions.Center, true);
        resultHeadingText = UiLabel(card.transform, TankDuelLocalization.Get("DRAW"), 64, UiText,
            new Vector2(0, 175), new Vector2(850, 132), TextAlignmentOptions.Center, true, true);
        UiRuleLine(card.transform, new Vector2(0, 92), 790);
        resultSubtitleText = UiLabel(card.transform, "", 31, UiMuted,
            new Vector2(0, 2), new Vector2(830, 118), TextAlignmentOptions.Center, false, true);
        UiButton(card.transform, "Rematch", TankDuelLocalization.Get("REMATCH"),
            new Vector2(0, -144), new Vector2(770, 75), UiAccent, UiBackdrop, Rematch, 26);
        UiButton(card.transform, "Main menu", TankDuelLocalization.Get("MAIN_MENU"),
            new Vector2(0, -240), new Vector2(770, 65), UiQuiet, UiText, ReturnToMenu, 23);
        result.SetActive(false);
    }
}
