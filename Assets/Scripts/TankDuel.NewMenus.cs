using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class TankDuel
{
    // These pages deliberately keep the game world, the match controls and the
    // vehicle collection on separate screens. Text stays native TMP at 1080p.
    private int settingsCategory;

    private static void ResizeStatFill(RectTransform fill, float ratio)
    {
        var track = fill.parent as RectTransform;
        if (track != null) fill.sizeDelta = new Vector2(track.rect.width * ratio, fill.sizeDelta.y);
    }

    private void BuildLandingMenu()
    {
        menuPanel = Panel("MainMenuPanel", UiBackdrop, true);
        UiLabel(menuPanel.transform, UiCopy("Tank Düellosu", "Tank Duel"), 75, UiText,
            new Vector2(-505, 315), new Vector2(730, 108), TextAlignmentOptions.Left, true);
        UiLabel(menuPanel.transform, UiCopy("Yerel tank savaşı", "Local tank battle"), 25,
            UiMuted, new Vector2(-505, 248), new Vector2(730, 45));

        LandingAction("Play", UiCopy("OYNA", "PLAY"),
            UiCopy("Maç kur ve düelloya başla", "Set up a match and start the duel"), 78,
            () => { ShowPanel(matchSetupPanel); UpdateModeCards(); });
        LandingAction("Garage", TankDuelLocalization.Get("GARAGE"),
            UiCopy("Tankını ve rengini seç", "Choose your tank and colour"), -82, ShowGarage);
        LandingAction("Settings", TankDuelLocalization.Get("SETTINGS"),
            UiCopy("Oyun, görüntü ve ses ayarları", "Match, display and sound options"),
            -242, () => ShowPanel(settingsPanel));

        UiLabel(menuPanel.transform,
            UiCopy("Tankını seç.\nDüelloya çık.", "Choose your tank.\nEnter the duel."),
            64, UiText, new Vector2(405, 164), new Vector2(810, 192),
            TextAlignmentOptions.Left, true, true);
        UiLabel(menuPanel.transform,
            UiCopy("Arkadaşına karşı aynı ekranda oyna.\nİstersen bilgisayara meydan oku.",
                "Play against a friend on one screen.\nOr challenge the computer."),
            27, UiMuted, new Vector2(405, -57), new Vector2(810, 140),
            TextAlignmentOptions.Left, false, true);
        UiLabel(menuPanel.transform,
            UiCopy("6 arena   ·   5 tank", "6 arenas   ·   5 tanks"),
            25, UiAccent, new Vector2(405, -235), new Vector2(810, 50));
        menuPanel.SetActive(false);
    }

    private void LandingAction(string name, string title, string description, float y, Action action)
    {
        var button = UiButton(menuPanel.transform, name, "", new Vector2(-505, y),
            new Vector2(680, 128), UiQuiet, UiText, action);
        UiLabel(button.transform, title, 31, UiText,
            new Vector2(0, 23), new Vector2(580, 48), TextAlignmentOptions.Left, true);
        UiLabel(button.transform, description, 21, UiMuted,
            new Vector2(0, -27), new Vector2(580, 37), TextAlignmentOptions.Left);
    }

    private void BuildMatchSetupPanel()
    {
        matchSetupPanel = Panel("MatchSetupPanel", UiBackdrop, true);
        UiBlock(matchSetupPanel.transform, "Top rule", new Vector2(0, 430),
            new Vector2(1770, 2), UiRaised, false);
        UiButton(matchSetupPanel.transform, "Back", UiCopy("‹  ANA MENÜ", "‹  MAIN MENU"),
            new Vector2(-770, 470), new Vector2(255, 60), UiQuiet, UiText,
            () => ShowPanel(menuPanel), 20);
        UiLabel(matchSetupPanel.transform, UiCopy("YENİ DÜELLO", "NEW DUEL"), 20, UiSage,
            new Vector2(-729, 348), new Vector2(380, 36), TextAlignmentOptions.Left, true);
        UiDisplay(matchSetupPanel.transform, UiCopy("MAÇI AYARLA", "SET UP MATCH"), 70, UiText,
            new Vector2(-454, 273), new Vector2(930, 104));
        UiLabel(matchSetupPanel.transform,
            UiCopy("Nasıl oynamak istersin?", "How would you like to play?"),
            25, UiMuted, new Vector2(-635, 211), new Vector2(550, 42));

        UiLabel(matchSetupPanel.transform, UiCopy("OYUN MODU", "GAME MODE"), 19, UiSage,
            new Vector2(-754, 127), new Vector2(270, 34), TextAlignmentOptions.Left, true);
        for (int i = 0; i < 2; i++)
        {
            int selected = i;
            string title = i == 0 ? UiCopy("BOTA KARŞI", "VERSUS BOT") :
                UiCopy("İKİ OYUNCU", "TWO PLAYERS");
            var button = UiButton(matchSetupPanel.transform, "Mode " + i, title,
                new Vector2(i == 0 ? -470 : 303, 45), new Vector2(720, 120),
                UiQuiet, UiText, () =>
                {
                    TankDuelData.AIModeEnabled = selected == 0;
                    UpdateModeCards();
                }, 30);
            modeCards[i] = button;
            modeCardTitles[i] = button.GetComponentInChildren<TextMeshProUGUI>();
        }
        modeHintLabel = UiLabel(matchSetupPanel.transform, "", 21, UiMuted,
            new Vector2(0, -37), new Vector2(1500, 40), TextAlignmentOptions.Center);

        difficultySection = new GameObject("Bot difficulty", typeof(RectTransform));
        difficultySection.transform.SetParent(matchSetupPanel.transform, false);
        var diffRect = difficultySection.GetComponent<RectTransform>();
        diffRect.anchorMin = diffRect.anchorMax = new Vector2(0.5f, 0.5f);
        diffRect.sizeDelta = new Vector2(1580, 225);
        diffRect.anchoredPosition = new Vector2(0, -170);
        UiLabel(difficultySection.transform, UiCopy("BOT ZORLUĞU", "BOT DIFFICULTY"), 19,
            UiSage, new Vector2(-674, 92), new Vector2(290, 34),
            TextAlignmentOptions.Left, true);
        for (int i = 0; i < 3; i++)
        {
            int selected = i;
            string title = TankDuelLocalization.Get(i == 0 ? "DIFF_EASY" : i == 1 ? "DIFF_MED" : "DIFF_HARD");
            var button = UiButton(difficultySection.transform, "Difficulty " + i, title,
                new Vector2((i - 1) * 485, 21), new Vector2(450, 72), UiQuiet,
                UiText, () => { TankDuelData.AIDifficulty = selected; UpdateDifficultyButtons(); }, 23);
            diffButtons[i] = button;
            diffButtonLabels[i] = button.GetComponentInChildren<TextMeshProUGUI>();
        }
        diffHintLabel = UiLabel(difficultySection.transform, "", 20, UiMuted,
            new Vector2(0, -57), new Vector2(1300, 38), TextAlignmentOptions.Center);

        twoPlayerInstructions = new GameObject("Two player instructions", typeof(RectTransform));
        twoPlayerInstructions.transform.SetParent(matchSetupPanel.transform, false);
        var pvpRect = twoPlayerInstructions.GetComponent<RectTransform>();
        pvpRect.anchorMin = pvpRect.anchorMax = new Vector2(0.5f, 0.5f);
        pvpRect.sizeDelta = new Vector2(1500, 160);
        pvpRect.anchoredPosition = new Vector2(0, -160);
        UiLabel(twoPlayerInstructions.transform, UiCopy("KONTROLLER", "CONTROLS"), 19,
            UiSage, new Vector2(-631, 64), new Vector2(240, 34),
            TextAlignmentOptions.Left, true);
        UiLabel(twoPlayerInstructions.transform,
            UiCopy("1. OYUNCU   WASD + Boşluk", "PLAYER 1   WASD + Space"),
            25, UiText, new Vector2(-344, -3), new Vector2(650, 42), TextAlignmentOptions.Center);
        UiLabel(twoPlayerInstructions.transform,
            UiCopy("2. OYUNCU   Yön tuşları + Enter", "PLAYER 2   Arrows + Enter"),
            25, UiText, new Vector2(344, -3), new Vector2(650, 42), TextAlignmentOptions.Center);

        UiBlock(matchSetupPanel.transform, "Bottom rule", new Vector2(0, -342),
            new Vector2(1770, 2), UiRaised, false);
        UiLabel(matchSetupPanel.transform,
            UiCopy("HARİTA  /  Her maçta rastgele altı arenadan biri", "MAP  /  One of six arenas each match"),
            22, UiMuted, new Vector2(-476, -407), new Vector2(850, 48));
        UiButton(matchSetupPanel.transform, "Start match", TankDuelLocalization.Get("START_MATCH") + "  ›",
            new Vector2(603, -412), new Vector2(552, 84), UiSage, UiInk,
            OnStartMatchClicked, 29);
        UpdateModeCards();
        UpdateDifficultyButtons();
        matchSetupPanel.SetActive(false);
    }

    private void BuildModernGarage()
    {
        garagePanel = Panel("GaragePanel", UiBackdrop, true);
        UiBlock(garagePanel.transform, "Stage band", new Vector2(165, 15),
            new Vector2(1470, 1010), UiSurface, false);
        var header = UiBlock(garagePanel.transform, "Garage navigation", new Vector2(0, 468),
            new Vector2(1800, 90), new Color(0.025f, 0.028f, 0.078f, 0.95f), false);
        UiButton(header.transform, "Back", UiCopy("‹  MENÜ", "‹  MENU"),
            new Vector2(-777, 0), new Vector2(194, 60), UiQuiet, UiText,
            () => ShowPanel(menuPanel), 20);
        UiDisplay(header.transform, TankDuelLocalization.Get("GARAGE"), 48, UiText,
            new Vector2(-521, 0), new Vector2(300, 65));
        garageTab1 = UiButton(header.transform, "Player one", TankDuelLocalization.Get("PLAYER_1"),
            new Vector2(248, 0), new Vector2(220, 58), UiQuiet, UiText, () =>
            {
                currentGaragePlayer = 1;
                inspectedTankIndex = TankDuelData.Player1TankIndex;
                UpdateGarageView();
            }, 20);
        garageTab2 = UiButton(header.transform, "Player two", TankDuelLocalization.Get("PLAYER_2"),
            new Vector2(482, 0), new Vector2(220, 58), UiQuiet, UiText, () =>
            {
                currentGaragePlayer = 2;
                inspectedTankIndex = TankDuelData.Player2TankIndex;
                UpdateGarageView();
            }, 20);

        UiLabel(garagePanel.transform, UiCopy("TANK KOLEKSİYONU", "TANK COLLECTION"), 18,
            UiSage, new Vector2(-744, 342), new Vector2(340, 36), TextAlignmentOptions.Center, true);
        for (int i = 0; i < 5; i++)
        {
            int selected = i;
            var choice = UiButton(garagePanel.transform, "Tank " + i, "",
                new Vector2(-741, 246 - i * 113), new Vector2(320, 92),
                UiQuiet, UiText, () => { inspectedTankIndex = selected; UpdateGarageView(); });
            UiLabel(choice.transform, (i + 1).ToString("00"), 18, UiSage,
                new Vector2(-111, 0), new Vector2(42, 36), TextAlignmentOptions.Center, true);
            garageDockLabels[i] = UiLabel(choice.transform, Archetypes[i].GetName(), 23, UiText,
                new Vector2(38, 15), new Vector2(230, 34), TextAlignmentOptions.Left, true);
            garageDockSublabels[i] = UiLabel(choice.transform, "", 17, UiMuted,
                new Vector2(38, -20), new Vector2(230, 30));
            garageDockButtons[i] = choice;
        }

        UiLabel(garagePanel.transform, UiCopy("ARAÇ ÖNİZLEMESİ", "VEHICLE PREVIEW"), 18,
            UiSage, new Vector2(-410, 352), new Vector2(290, 35), TextAlignmentOptions.Left, true);
        UiBlock(garagePanel.transform, "Vehicle stage", new Vector2(-112, 47),
            new Vector2(825, 538), UiBackdrop, false);
        var preview = new GameObject("Vehicle preview", typeof(RectTransform), typeof(RawImage));
        preview.transform.SetParent(garagePanel.transform, false);
        var previewRect = preview.GetComponent<RectTransform>();
        previewRect.anchorMin = previewRect.anchorMax = new Vector2(0.5f, 0.5f);
        previewRect.sizeDelta = new Vector2(800, 450);
        previewRect.anchoredPosition = new Vector2(-112, 47);
        garagePreviewImage = preview.GetComponent<RawImage>();
        garagePreviewImage.raycastTarget = false;
        if (garagePreviewTexture != null) garagePreviewImage.texture = garagePreviewTexture;

        dossierNameText = UiDisplay(garagePanel.transform, "", 55, UiText,
            new Vector2(603, 316), new Vector2(520, 76));
        dossierRoleText = UiLabel(garagePanel.transform, "", 20, UiSage,
            new Vector2(603, 263), new Vector2(520, 38), TextAlignmentOptions.Left, true);
        dossierDescText = UiLabel(garagePanel.transform, "", 20, UiMuted,
            new Vector2(603, 194), new Vector2(520, 90), TextAlignmentOptions.Left, false, true);
        UiBlock(garagePanel.transform, "Details rule", new Vector2(604, 131),
            new Vector2(520, 2), UiRaised, false);
        UiLabel(garagePanel.transform, TankDuelLocalization.Get("TANK_SPECS"), 19, UiAccent,
            new Vector2(475, 99), new Vector2(260, 32), TextAlignmentOptions.Left, true);
        ModernStatBar(TankDuelLocalization.Get("STAT_SPEED"), 48,
            out speedStatFill, out speedStatVal, UiSage);
        ModernStatBar(TankDuelLocalization.Get("STAT_ARMOR"), -33,
            out armorStatFill, out armorStatVal, UiSage);
        ModernStatBar(TankDuelLocalization.Get("STAT_DAMAGE"), -114,
            out damageStatFill, out damageStatVal, UiAccent);
        ModernStatBar(TankDuelLocalization.Get("STAT_FIRERATE"), -195,
            out firerateStatFill, out firerateStatVal, UiSage);

        UiLabel(garagePanel.transform, UiCopy("TANK RENGİ", "TANK COLOUR"), 19, UiSage,
            new Vector2(-376, -294), new Vector2(290, 36), TextAlignmentOptions.Left, true);
        garageColorNameText = UiLabel(garagePanel.transform, "", 21, UiText,
            new Vector2(73, -294), new Vector2(320, 36), TextAlignmentOptions.Right, true);
        garageColorButtons = new Button[TankDuelData.TankColorCount];
        for (int i = 0; i < garageColorButtons.Length; i++)
        {
            int selected = i;
            var button = UiButton(garagePanel.transform, "Paint " + i, "",
                new Vector2(-440 + i * 86, -364), new Vector2(68, 62), UiQuiet,
                UiText, () => SelectGarageColor(selected));
            UiBlock(button.transform, "Paint swatch", Vector2.zero, new Vector2(50, 44),
                TankDuelData.GetTankColor(i), false);
            garageColorButtons[i] = button;
        }
        dossierActionButton = UiButton(garagePanel.transform, "Equip or buy",
            TankDuelLocalization.Get("SELECT"), new Vector2(603, -388),
            new Vector2(520, 78), UiSage, UiInk, OnDossierActionClicked, 25);
        dossierActionText = dossierActionButton.GetComponentInChildren<TextMeshProUGUI>();
        garagePanel.SetActive(false);
    }

    private void ModernStatBar(string title, float y,
        out RectTransform fillRect, out TextMeshProUGUI value, Color color)
    {
        UiLabel(garagePanel.transform, title, 19, UiText,
            new Vector2(441, y), new Vector2(196, 30), TextAlignmentOptions.Left, true);
        value = UiLabel(garagePanel.transform, "", 18, UiMuted,
            new Vector2(774, y), new Vector2(178, 30), TextAlignmentOptions.Right);
        var track = UiBlock(garagePanel.transform, "Stat track", new Vector2(603, y - 23),
            new Vector2(520, 8), UiQuiet, false);
        var fill = UiBlock(track.transform, "Stat fill", Vector2.zero,
            new Vector2(520, 8), color, false);
        fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = fillRect.anchorMax = new Vector2(0, 0.5f);
        fillRect.pivot = new Vector2(0, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
    }

    private void BuildModernSettings()
    {
        settingsPanel = Panel("SettingsPanel", UiBackdrop, true);
        UiBlock(settingsPanel.transform, "Sidebar", new Vector2(-660, 0),
            new Vector2(600, 1080), UiSurface, false);
        UiButton(settingsPanel.transform, "Back", UiCopy("‹  ANA MENÜ", "‹  MAIN MENU"),
            new Vector2(-753, 466), new Vector2(230, 58), UiQuiet, UiText,
            () => ShowPanel(menuPanel), 20);
        UiDisplay(settingsPanel.transform, TankDuelLocalization.Get("SETTINGS"), 59, UiText,
            new Vector2(-674, 347), new Vector2(470, 80));
        UiLabel(settingsPanel.transform, UiCopy("OYUN SEÇENEKLERİ", "GAME OPTIONS"), 18,
            UiSage, new Vector2(-728, 287), new Vector2(360, 36), TextAlignmentOptions.Left, true);
        string[] categories = TankDuelLocalization.IsTurkish
            ? new[] { "MAÇ", "GÖRÜNTÜ", "SES", "DİL" }
            : new[] { "MATCH", "DISPLAY", "AUDIO", "LANGUAGE" };
        for (int i = 0; i < categories.Length; i++)
        {
            int selected = i;
            bool active = settingsCategory == i;
            var button = UiButton(settingsPanel.transform, "Category " + i, "",
                new Vector2(-656, 166 - 108 * i), new Vector2(490, 84),
                active ? UiSage : UiQuiet, active ? UiInk : UiText,
                () => { settingsCategory = selected; RefreshSettingsPanel(); });
            UiLabel(button.transform, "0" + (i + 1), 18, active ? UiInk : UiSage,
                new Vector2(-195, 0), new Vector2(50, 34), TextAlignmentOptions.Center, true);
            UiDisplay(button.transform, categories[i], 28, active ? UiInk : UiText,
                new Vector2(12, 0), new Vector2(316, 46));
        }
        UiLabel(settingsPanel.transform,
            UiCopy("Değişiklikler otomatik kaydedilir.", "Changes are saved automatically."),
            19, UiMuted, new Vector2(-673, -412), new Vector2(430, 50),
            TextAlignmentOptions.Left, false, true);

        var content = UiBlock(settingsPanel.transform, "Selected setting", new Vector2(302, 0),
            new Vector2(1150, 890), UiSurface, false);
        string[] captions = TankDuelLocalization.IsTurkish
            ? new[] { "RAUNT SÜRESİ", "GRAFİK KALİTESİ", "MÜZİK", "ARAYÜZ DİLİ" }
            : new[] { "ROUND TIME", "GRAPHICS QUALITY", "MUSIC", "INTERFACE LANGUAGE" };
        string[] descriptions = TankDuelLocalization.IsTurkish
            ? new[] { "Her raunt ne kadar sürsün?", "Görüntü kalitesini seç.",
                "Arka plan müziğini aç veya kapat.", "Menü dilini seç." }
            : new[] { "How long is each round?", "Choose visual quality.",
                "Turn background music on or off.", "Choose the menu language." };
        UiLabel(content.transform, "0" + (settingsCategory + 1) + " / 04", 20,
            UiSage, new Vector2(-442, 332), new Vector2(200, 34), TextAlignmentOptions.Left, true);
        UiDisplay(content.transform, captions[settingsCategory], 62, UiText,
            new Vector2(-2, 234), new Vector2(980, 100));
        UiLabel(content.transform, descriptions[settingsCategory], 26, UiMuted,
            new Vector2(-2, 165), new Vector2(980, 50));
        UiBlock(content.transform, "Options rule", new Vector2(0, 95),
            new Vector2(990, 2), UiRaised, false);

        if (settingsCategory == 0)
        {
            string[] names = { TankDuelLocalization.Get("60_SEC"),
                TankDuelLocalization.Get("90_SEC"), TankDuelLocalization.Get("120_SEC") };
            for (int i = 0; i < 3; i++)
            {
                int selected = i;
                SettingOption(content.transform, names[i], (i - 1) * 332, -40,
                    TankDuelData.DurationIndex == i, () =>
                    { TankDuelData.DurationIndex = selected; RefreshSettingsPanel(); });
            }
        }
        else if (settingsCategory == 1)
        {
            string[] names = { TankDuelLocalization.Get("GRAPHICS_LOW"),
                TankDuelLocalization.Get("GRAPHICS_MED"),
                TankDuelLocalization.Get("GRAPHICS_HIGH") };
            int quality = QualitySettings.GetQualityLevel();
            int selectedQuality = quality == 0 ? 0 :
                quality >= QualitySettings.names.Length - 1 ? 2 : 1;
            for (int i = 0; i < 3; i++)
            {
                int selected = i;
                SettingOption(content.transform, names[i], (i - 1) * 332, -40,
                    selectedQuality == i, () =>
                    {
                        int highest = Mathf.Max(0, QualitySettings.names.Length - 1);
                        int target = selected == 0 ? 0 : selected == 1 ?
                            Mathf.Clamp(QualitySettings.names.Length / 2, 0, highest) : highest;
                        ApplyGraphicsPreference(target);
                        PlayerPrefs.SetInt("TankDuel.Quality", target);
                        RefreshSettingsPanel();
                    });
            }
        }
        else if (settingsCategory == 2)
        {
            SettingOption(content.transform,
                TankDuelData.MusicEnabled ? TankDuelLocalization.Get("ON") : TankDuelLocalization.Get("OFF"),
                0, -40, TankDuelData.MusicEnabled, () =>
                {
                    TankDuelData.MusicEnabled = !TankDuelData.MusicEnabled;
                    ApplyMusicPreference();
                    RefreshSettingsPanel();
                });
        }
        else
        {
            SettingOption(content.transform, TankDuelLocalization.Get("LANGUAGE"),
                0, -40, true, () =>
                {
                    TankDuelLocalization.ToggleLanguage();
                    ReloadWholeInterface();
                });
        }
        UiLabel(content.transform,
            UiCopy("Seçimlerin bir sonraki maçta uygulanır.", "Your choices apply in the next match."),
            19, UiMuted, new Vector2(-4, -337), new Vector2(970, 44),
            TextAlignmentOptions.Center);
        settingsPanel.SetActive(false);
    }

    private void SettingOption(Transform parent, string title, float x, float y,
        bool selected, Action action)
    {
        UiButton(parent, "Option " + title, title,
            new Vector2(x, y), new Vector2(selected ? 310 : 300, 126),
            selected ? UiSage : UiQuiet, selected ? UiInk : UiText, action, 27);
    }
}
