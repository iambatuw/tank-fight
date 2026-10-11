using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class TankDuel
{
    // A restrained field-guide palette, separate from the two team colours used in battle.
    private static readonly Color UiBackdrop = new Color(0.055f, 0.075f, 0.071f, 0.96f);
    private static readonly Color UiSurface = new Color(0.105f, 0.145f, 0.133f, 0.98f);
    private static readonly Color UiRaised = new Color(0.145f, 0.190f, 0.169f, 1f);
    private static readonly Color UiQuiet = new Color(0.19f, 0.235f, 0.208f, 1f);
    private static readonly Color UiText = new Color(0.965f, 0.953f, 0.905f, 1f);
    private static readonly Color UiMuted = new Color(0.70f, 0.75f, 0.69f, 1f);
    private static readonly Color UiAccent = new Color(0.93f, 0.66f, 0.31f, 1f);
    private static readonly Color UiSage = new Color(0.63f, 0.76f, 0.54f, 1f);
    private static readonly Color UiRule = new Color(0.35f, 0.41f, 0.35f, 0.65f);

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
        var label = Label(parent, content, size, color, position, bounds, bold);
        label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        label.alignment = alignment;
        label.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        label.enableAutoSizing = true;
        label.fontSizeMin = size * 0.79f;
        label.fontSizeMax = size;
        label.overflowMode = TextOverflowModes.Truncate;
        return label;
    }

    private Button UiButton(Transform parent, string name, string title, Vector2 position, Vector2 size,
        Color background, Color foreground, Action click, float textSize = 23f)
    {
        var go = UiBlock(parent, name, position, size, background);
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
        label.color = selected ? UiBackdrop : UiText;
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
        rect.sizeDelta = new Vector2(236, 76);
        UiBlock(topBar.transform, "Currency plate", Vector2.zero, rect.sizeDelta, UiRaised);
        UiLabel(topBar.transform, "●", 42, UiAccent, new Vector2(-81, 0),
            new Vector2(44, 52), TextAlignmentOptions.Center);
        UiLabel(topBar.transform, TankDuelLocalization.Get("COINS"), 16, UiMuted,
            new Vector2(20, 20), new Vector2(142, 24), bold: true);
        coinsText = UiLabel(topBar.transform, GetCoinsLabel(), 28, UiText,
            new Vector2(20, -10), new Vector2(142, 38), bold: true);
        coinsText.fontSizeMin = 14f;
    }

    private void BuildMainMenuPanel()
    {
        menuPanel = Panel("MainMenuPanel", UiBackdrop, true);

        // The left half serves as the game's identity; match controls live together on the right.
        UiBlock(menuPanel.transform, "Hero rule", new Vector2(-816, 214), new Vector2(6, 275), UiAccent, false);
        UiLabel(menuPanel.transform, UiCopy("YEREL TANK SAVAŞI", "LOCAL TANK BATTLE"), 19, UiAccent,
            new Vector2(-470, 415), new Vector2(700, 36), bold: true);
        UiLabel(menuPanel.transform, UiCopy("Tank Düellosu", "Tank Duel"), 78, UiText,
            new Vector2(-420, 244), new Vector2(730, 122), bold: true);
        UiLabel(menuPanel.transform,
            UiCopy("Tankını hazırla, arenaya çık\nve düelloya gir.", "Ready your tank, enter the arena\nand face your opponent."),
            29, UiMuted, new Vector2(-418, 112), new Vector2(710, 94), wrap: true);
        UiRuleLine(menuPanel.transform, new Vector2(-467, -252), 680);
        UiLabel(menuPanel.transform,
            UiCopy("Tek ekranda tank düellosu", "Tank duel on one screen"),
            18, UiMuted, new Vector2(-462, -292), new Vector2(700, 34));
        UiButton(menuPanel.transform, "Open garage", TankDuelLocalization.Get("GARAGE"),
            new Vector2(-641, -376), new Vector2(325, 70), UiRaised, UiText, ShowGarage);
        UiButton(menuPanel.transform, "Open settings", TankDuelLocalization.Get("SETTINGS"),
            new Vector2(-291, -376), new Vector2(325, 70), UiRaised, UiText,
            () => ShowPanel(settingsPanel));

        var card = UiBlock(menuPanel.transform, "Match setup", new Vector2(429, 0),
            new Vector2(900, 900), UiSurface);
        UiLabel(card.transform, UiCopy("MAÇ KURULUMU", "MATCH SETUP"), 35, UiText,
            new Vector2(-141, 376), new Vector2(518, 52), bold: true);
        UiLabel(card.transform, UiCopy("Nasıl oynamak istersin?", "How would you like to play?"),
            23, UiMuted, new Vector2(-141, 333), new Vector2(518, 35));
        UiRuleLine(card.transform, new Vector2(0, 299), 800);

        UiLabel(card.transform, UiCopy("OYUN MODU", "GAME MODE"), 19, UiMuted,
            new Vector2(-120, 263), new Vector2(560, 34), bold: true);
        for (int i = 0; i < 2; i++)
        {
            int index = i;
            string title = i == 0 ? UiCopy("Tek oyuncu", "Solo") : UiCopy("İki oyuncu", "Two players");
            var choice = UiButton(card.transform, "Mode " + i, title, new Vector2(i == 0 ? -204 : 204, 204),
                new Vector2(390, 72), UiQuiet, UiText, () =>
                {
                    TankDuelData.AIModeEnabled = index == 0;
                    UpdateModeCards();
                }, 25);
            modeCards[i] = choice;
            modeCardTitles[i] = choice.GetComponentInChildren<TextMeshProUGUI>();
        }
        modeHintLabel = UiLabel(card.transform, "", 21, UiMuted,
            new Vector2(0, 149), new Vector2(790, 36), TextAlignmentOptions.Center);

        difficultySection = new GameObject("Difficulty", typeof(RectTransform));
        difficultySection.transform.SetParent(card.transform, false);
        var diffRect = difficultySection.GetComponent<RectTransform>();
        diffRect.anchorMin = diffRect.anchorMax = new Vector2(0.5f, 0.5f);
        diffRect.sizeDelta = new Vector2(800, 145);
        diffRect.anchoredPosition = new Vector2(0, 31);
        UiLabel(difficultySection.transform, TankDuelLocalization.Get("DIFFICULTY"), 19, UiMuted,
            new Vector2(-120, 58), new Vector2(560, 34), bold: true);
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            string title = TankDuelLocalization.Get(i == 0 ? "DIFF_EASY" : i == 1 ? "DIFF_MED" : "DIFF_HARD");
            var choice = UiButton(difficultySection.transform, "Difficulty " + i, title,
                new Vector2((i - 1) * 273, 0), new Vector2(255, 56), UiQuiet, UiText,
                () => { TankDuelData.AIDifficulty = index; UpdateDifficultyButtons(); }, 21);
            diffButtons[i] = choice;
            diffButtonLabels[i] = choice.GetComponentInChildren<TextMeshProUGUI>();
        }
        diffHintLabel = UiLabel(difficultySection.transform, "", 20, UiMuted,
            new Vector2(0, -52), new Vector2(790, 29), TextAlignmentOptions.Center);

        UiLabel(card.transform, UiCopy("RASTGELE HARİTA", "RANDOM ARENA"), 19, UiMuted,
            new Vector2(-120, -105), new Vector2(560, 34), bold: true);
        UiLabel(card.transform, UiCopy("Orman  •  Çöl  •  Ay Üssü\nKanyon  •  Liman  •  Kutup İstasyonu",
            "Jungle  •  Desert  •  Moon Base\nCanyon  •  Harbor  •  Arctic Station"),
            23, UiText, new Vector2(0, -162), new Vector2(800, 72), TextAlignmentOptions.Center, true, true);
        UiLabel(card.transform, UiCopy("Her yeni maçta farklı bir arena", "A different arena for every new match"),
            18, UiMuted, new Vector2(0, -216), new Vector2(800, 26), TextAlignmentOptions.Center);
        UiRuleLine(card.transform, new Vector2(0, -234), 800);
        UiButton(card.transform, "Start match", TankDuelLocalization.Get("START_MATCH"),
            new Vector2(0, -334), new Vector2(800, 82), UiAccent, UiBackdrop, OnStartMatchClicked, 30);

        UpdateModeCards();
        UpdateDifficultyButtons();
        menuPanel.SetActive(false);
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

    private void BuildGaragePanel()
    {
        garagePanel = Panel("GaragePanel", UiBackdrop, true);
        var header = UiBlock(garagePanel.transform, "Garage navigation", new Vector2(0, 440),
            new Vector2(1800, 94), UiSurface);
        UiButton(header.transform, "Back", UiCopy("←  GERİ", "←  BACK"),
            new Vector2(-785, 0), new Vector2(175, 61), UiQuiet, UiText,
            () => ShowPanel(menuPanel), 22);
        UiLabel(header.transform, TankDuelLocalization.Get("GARAGE"), 46, UiText,
            new Vector2(-507, 0), new Vector2(310, 68), bold: true);
        garageTab1 = UiButton(header.transform, "Player one", TankDuelLocalization.Get("PLAYER_1"),
            new Vector2(192, 0), new Vector2(220, 60), UiQuiet, UiText, () =>
            {
                currentGaragePlayer = 1;
                inspectedTankIndex = TankDuelData.Player1TankIndex;
                UpdateGarageView();
            }, 21);
        garageTab2 = UiButton(header.transform, "Player two",
            TankDuelData.AIModeEnabled ? UiCopy("BİLGİSAYAR", "COMPUTER") : TankDuelLocalization.Get("PLAYER_2"),
            new Vector2(425, 0), new Vector2(220, 60), UiQuiet, UiText, () =>
            {
                currentGaragePlayer = 2;
                inspectedTankIndex = TankDuelData.Player2TankIndex;
                UpdateGarageView();
            }, 21);

        var fleet = UiBlock(garagePanel.transform, "Fleet list", new Vector2(-728, -4),
            new Vector2(344, 736), UiSurface);
        UiLabel(fleet.transform, UiCopy("TANK FİLOSU", "TANK FLEET"), 25, UiText,
            new Vector2(2, 316), new Vector2(286, 42), TextAlignmentOptions.Center, true);
        UiRuleLine(fleet.transform, new Vector2(0, 280), 282);
        for (int i = 0; i < 5; i++)
        {
            int index = i;
            var choice = UiButton(fleet.transform, "Tank " + i, "",
                new Vector2(0, 210 - i * 122), new Vector2(298, 105), UiQuiet, UiText, () =>
                {
                    inspectedTankIndex = index;
                    UpdateGarageView();
                });
            garageDockLabels[i] = UiLabel(choice.transform, Archetypes[i].GetName(), 24, UiText,
                new Vector2(0, 17), new Vector2(268, 36), TextAlignmentOptions.Center, true);
            garageDockSublabels[i] = UiLabel(choice.transform, "", 20, UiMuted,
                new Vector2(0, -22), new Vector2(268, 31), TextAlignmentOptions.Center);
            garageDockButtons[i] = choice;
        }

        var showcase = UiBlock(garagePanel.transform, "Tank showroom", new Vector2(-193, -4),
            new Vector2(678, 736), UiSurface);
        var preview = new GameObject("Vehicle preview", typeof(RectTransform), typeof(RawImage));
        preview.transform.SetParent(showcase.transform, false);
        var previewRect = preview.GetComponent<RectTransform>();
        previewRect.anchorMin = previewRect.anchorMax = new Vector2(0.5f, 0.5f);
        previewRect.sizeDelta = new Vector2(440, 440);
        previewRect.anchoredPosition = new Vector2(0, 55);
        garagePreviewImage = preview.GetComponent<RawImage>();
        garagePreviewImage.raycastTarget = false;
        if (garagePreviewTexture != null) garagePreviewImage.texture = garagePreviewTexture;
        UiLabel(showcase.transform, UiCopy("SEÇİLİ ARAÇ", "SELECTED VEHICLE"), 23, UiText,
            new Vector2(0, 316), new Vector2(585, 38), TextAlignmentOptions.Center, true);
        UiRuleLine(showcase.transform, new Vector2(0, 281), 580);
        UiRuleLine(showcase.transform, new Vector2(0, -178), 580);
        UiLabel(showcase.transform, UiCopy("TANK RENGİ", "TANK COLOR"), 21, UiMuted,
            new Vector2(-157, -208), new Vector2(270, 34), bold: true);
        garageColorNameText = UiLabel(showcase.transform, "", 23, UiText,
            new Vector2(185, -208), new Vector2(210, 34), TextAlignmentOptions.Right, true);
        garageColorButtons = new Button[TankDuelData.TankColorCount];
        for (int i = 0; i < garageColorButtons.Length; i++)
        {
            int colorIndex = i;
            var choice = UiButton(showcase.transform, "Paint " + i, "",
                new Vector2((i - (garageColorButtons.Length - 1) * 0.5f) * 78, -267),
                new Vector2(64, 64), UiQuiet, UiText, () => SelectGarageColor(colorIndex));
            UiBlock(choice.transform, "Color swatch", Vector2.zero, new Vector2(48, 48),
                TankDuelData.GetTankColor(i));
            garageColorButtons[i] = choice;
        }
        UiLabel(showcase.transform,
            UiCopy("Renk tüm tanklarına uygulanır • Ücretsiz", "Color applies to all your tanks • Free"),
            20, UiMuted, new Vector2(0, -327), new Vector2(610, 32), TextAlignmentOptions.Center);

        var dossier = UiBlock(garagePanel.transform, "Vehicle details", new Vector2(535, -4),
            new Vector2(730, 736), UiSurface);
        UiLabel(dossier.transform, UiCopy("ARAÇ DOSYASI", "VEHICLE PROFILE"),
            21, UiAccent, new Vector2(-180, 314), new Vector2(300, 36), bold: true);
        dossierNameText = UiLabel(dossier.transform, "", 46, UiText,
            new Vector2(-10, 253), new Vector2(620, 65), bold: true);
        dossierRoleText = UiLabel(dossier.transform, "", 23, UiSage,
            new Vector2(-10, 206), new Vector2(620, 38), bold: true);
        dossierDescText = UiLabel(dossier.transform, "", 22, UiMuted,
            new Vector2(0, 144), new Vector2(650, 78), wrap: true);
        UiRuleLine(dossier.transform, new Vector2(0, 92), 650);
        UiLabel(dossier.transform, TankDuelLocalization.Get("TANK_SPECS"), 21, UiAccent,
            new Vector2(-150, 60), new Vector2(340, 34), bold: true);
        CreateStatBar(dossier.transform, TankDuelLocalization.Get("STAT_SPEED"), 11,
            out speedStatFill, out speedStatVal, UiSage);
        CreateStatBar(dossier.transform, TankDuelLocalization.Get("STAT_ARMOR"), -51,
            out armorStatFill, out armorStatVal, UiSage);
        CreateStatBar(dossier.transform, TankDuelLocalization.Get("STAT_DAMAGE"), -113,
            out damageStatFill, out damageStatVal, UiAccent);
        CreateStatBar(dossier.transform, TankDuelLocalization.Get("STAT_FIRERATE"), -175,
            out firerateStatFill, out firerateStatVal, UiSage);
        dossierActionButton = UiButton(dossier.transform, "Equip or buy", TankDuelLocalization.Get("SELECT"),
            new Vector2(0, -291), new Vector2(650, 73), UiSage, UiBackdrop,
            OnDossierActionClicked, 25);
        dossierActionText = dossierActionButton.GetComponentInChildren<TextMeshProUGUI>();
        garagePanel.SetActive(false);
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
            garagePreviewTexture = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32);
            garagePreviewTexture.name = "Garage vehicle preview";
            garagePreviewTexture.antiAliasing = 2;
            garagePreviewTexture.Create();
        }
        if (garagePreviewCamera == null)
        {
            var cameraObject = new GameObject("Garage preview camera", typeof(Camera));
            garagePreviewCamera = cameraObject.GetComponent<Camera>();
            garagePreviewCamera.clearFlags = CameraClearFlags.SolidColor;
            garagePreviewCamera.backgroundColor = new Color(UiSurface.r, UiSurface.g, UiSurface.b, 1f);
            garagePreviewCamera.cullingMask = 1 << GaragePreviewLayer;
            garagePreviewCamera.orthographic = true;
            garagePreviewCamera.orthographicSize = 4.8f;
            garagePreviewCamera.nearClipPlane = 0.1f;
            garagePreviewCamera.farClipPlane = 60f;
            garagePreviewCamera.allowHDR = false;
            garagePreviewCamera.targetTexture = garagePreviewTexture;
            cameraObject.transform.position = GaragePreviewOrigin + new Vector3(8f, 10f, -12f);
            cameraObject.transform.LookAt(GaragePreviewOrigin + Vector3.up * 1.2f);
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

    private void BuildStatBar(Transform parent, string title, float yPos,
        out RectTransform fillRect, out TextMeshProUGUI valText, Color barColor)
    {
        var row = new GameObject("Stat " + title, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        var rect = row.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(620, 54);
        rect.anchoredPosition = new Vector2(0, yPos);
        UiLabel(row.transform, title, 21, UiText, new Vector2(-185, 11),
            new Vector2(230, 31), bold: true);
        valText = UiLabel(row.transform, "", 20, UiMuted, new Vector2(195, 11),
            new Vector2(220, 31), TextAlignmentOptions.Right);
        var track = UiBlock(row.transform, "Track", new Vector2(0, -14),
            new Vector2(620, 10), UiQuiet, false);
        var fill = UiBlock(track.transform, "Fill", Vector2.zero,
            new Vector2(310, 10), barColor, false);
        fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = fillRect.anchorMax = new Vector2(0, 0.5f);
        fillRect.pivot = new Vector2(0, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
    }

    private void BuildSettingsPanel()
    {
        settingsPanel = Panel("SettingsPanel", UiBackdrop, true);
        UiButton(settingsPanel.transform, "Back", UiCopy("←  GERİ", "←  BACK"),
            new Vector2(-762, 446), new Vector2(190, 62), UiRaised, UiText,
            () => ShowPanel(menuPanel), 21);
        UiLabel(settingsPanel.transform, UiCopy("OYUN SEÇENEKLERİ", "GAME OPTIONS"), 19, UiAccent,
            new Vector2(-476, 300), new Vector2(700, 35), bold: true);
        UiLabel(settingsPanel.transform, UiCopy("Ayarlar", "Settings"), 82, UiText,
            new Vector2(-468, 213), new Vector2(710, 116), bold: true);
        UiLabel(settingsPanel.transform,
            UiCopy("Oyun deneyimini kendine göre ayarla.\nDeğişiklikler hemen uygulanır.",
                "Tune the game to your preference.\nChanges apply immediately."),
            28, UiMuted, new Vector2(-470, 105), new Vector2(710, 104), wrap: true);
        UiRuleLine(settingsPanel.transform, new Vector2(-480, -228), 680);
        UiLabel(settingsPanel.transform, UiCopy("Hazır olduğunda arenaya dön.", "Return to the arena when ready."),
            21, UiMuted, new Vector2(-480, -271), new Vector2(685, 41));

        var card = UiBlock(settingsPanel.transform, "Options", new Vector2(428, 0),
            new Vector2(900, 880), UiSurface);
        UiLabel(card.transform, UiCopy("MAÇ", "MATCH"), 18, UiAccent,
            new Vector2(-315, 351), new Vector2(210, 32), bold: true);
        UiLabel(card.transform, TankDuelLocalization.Get("ROUND_TIME"), 26, UiText,
            new Vector2(-156, 300), new Vector2(460, 42), bold: true);
        string[] times = { TankDuelLocalization.Get("60_SEC"), TankDuelLocalization.Get("90_SEC"), TankDuelLocalization.Get("120_SEC") };
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            UiButton(card.transform, "Duration " + i, times[i],
                new Vector2((i - 1) * 270, 232), new Vector2(252, 63),
                TankDuelData.DurationIndex == i ? UiSage : UiQuiet,
                TankDuelData.DurationIndex == i ? UiBackdrop : UiText,
                () => { TankDuelData.DurationIndex = index; RefreshSettingsPanel(); }, 21);
        }
        UiRuleLine(card.transform, new Vector2(0, 144), 792);
        UiLabel(card.transform, UiCopy("GÖRÜNTÜ", "DISPLAY"), 18, UiAccent,
            new Vector2(-315, 107), new Vector2(210, 32), bold: true);
        UiLabel(card.transform, TankDuelLocalization.Get("GRAPHICS"), 26, UiText,
            new Vector2(-156, 58), new Vector2(460, 42), bold: true);
        string[] qualities = { TankDuelLocalization.Get("GRAPHICS_LOW"), TankDuelLocalization.Get("GRAPHICS_MED"),
            TankDuelLocalization.Get("GRAPHICS_HIGH") };
        int quality = QualitySettings.GetQualityLevel();
        int selected = quality == 0 ? 0 : (quality >= QualitySettings.names.Length - 1 ? 2 : 1);
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            UiButton(card.transform, "Graphics " + i, qualities[i],
                new Vector2((i - 1) * 270, -10), new Vector2(252, 63),
                selected == i ? UiSage : UiQuiet, selected == i ? UiBackdrop : UiText,
                () =>
                {
                    int highest = Mathf.Max(0, QualitySettings.names.Length - 1);
                    int target = index == 0 ? 0 : index == 1 ?
                        Mathf.Clamp(QualitySettings.names.Length / 2, 0, highest) : highest;
                    ApplyGraphicsPreference(target);
                    PlayerPrefs.SetInt("TankDuel.Quality", target);
                    RefreshSettingsPanel();
                }, 21);
        }
        UiRuleLine(card.transform, new Vector2(0, -98), 792);
        UiLabel(card.transform, UiCopy("SES VE DİL", "AUDIO & LANGUAGE"), 18, UiAccent,
            new Vector2(-284, -135), new Vector2(280, 32), bold: true);
        UiButton(card.transform, "Language", TankDuelLocalization.Get("LANGUAGE"),
            new Vector2(-204, -202), new Vector2(388, 66), UiQuiet, UiText, () =>
            {
                TankDuelLocalization.ToggleLanguage();
                ReloadWholeInterface();
            }, 21);
        string music = TankDuelData.MusicEnabled ? TankDuelLocalization.Get("ON") : TankDuelLocalization.Get("OFF");
        UiButton(card.transform, "Music", TankDuelLocalization.Get("MUSIC") + ": " + music,
            new Vector2(204, -202), new Vector2(388, 66), UiQuiet, UiText, () =>
            {
                TankDuelData.MusicEnabled = !TankDuelData.MusicEnabled;
                ApplyMusicPreference();
                RefreshSettingsPanel();
            }, 21);
        UiLabel(card.transform, UiCopy("Değişiklikler otomatik kaydedilir.", "Changes are saved automatically."),
            18, UiMuted, new Vector2(0, -342), new Vector2(790, 35), TextAlignmentOptions.Center);
        settingsPanel.SetActive(false);
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
