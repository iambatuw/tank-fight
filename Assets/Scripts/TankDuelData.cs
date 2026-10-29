using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class TankDuelLocalization
{
    public static bool IsTurkish => PlayerPrefs.GetInt("TankDuel.Lang", 0) == 0;

    public static void ToggleLanguage()
    {
        PlayerPrefs.SetInt("TankDuel.Lang", IsTurkish ? 1 : 0);
        PlayerPrefs.Save();
    }

    private static TMP_FontAsset s_CachedTurkishFallback = null;

    public static void EnsureTurkishSupport(TMP_FontAsset font)
    {
        if (font == null) return;
        bool hasTurkishGlyphs = true;
        foreach (char c in "ÇĞİÖŞÜçğıöşü")
            if (!font.HasCharacter(c)) { hasTurkishGlyphs = false; break; }
        if (hasTurkishGlyphs) return;
        try
        {
            if (font.fallbackFontAssetTable == null)
                font.fallbackFontAssetTable = new List<TMP_FontAsset>();

            if (s_CachedTurkishFallback != null)
            {
                if (s_CachedTurkishFallback != font && !font.fallbackFontAssetTable.Contains(s_CachedTurkishFallback))
                    font.fallbackFontAssetTable.Add(s_CachedTurkishFallback);
                return;
            }

            // Check if font already has Turkish fallback registered
            foreach (var fb in font.fallbackFontAssetTable)
            {
                if (fb != null && (fb.name.Contains("Inter") || fb.name.Contains("Ubuntu") || fb.name.Contains("Liberation") || fb.name.StartsWith("TurkishFallback")))
                {
                    s_CachedTurkishFallback = fb;
                    return;
                }
            }

            TMP_FontAsset candidate = null;

#if UNITY_EDITOR
            // Load embedded in-project TrueType SDF assets that contain complete Latin-Extended Turkish glyphs
            string[] editorFontPaths = {
                "Assets/_Tanks/Art/Fonts/Inter/Inter SDF.asset",
                "Assets/_Tanks/Art/Fonts/Ubuntu/Ubuntu-Regular SDF.asset",
                "Assets/_Tanks/Settings/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset"
            };
            foreach (var p in editorFontPaths)
            {
                var loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(p);
                if (loaded != null && loaded != font)
                {
                    candidate = loaded;
                    break;
                }
            }
#endif

            // Runtime fallback from Resources
            if (candidate == null)
            {
                var resFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                if (resFont != null && resFont != font)
                    candidate = resFont;
            }

            // Runtime fallback from loaded assets
            if (candidate == null)
            {
                var allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                foreach (var f in allFonts)
                {
                    if (f != null && f != font && (f.name.Contains("Inter") || f.name.Contains("Ubuntu") || f.name.Contains("Liberation")))
                    {
                        candidate = f;
                        break;
                    }
                }
            }

            if (candidate != null)
            {
                s_CachedTurkishFallback = candidate;
                if (!font.fallbackFontAssetTable.Contains(candidate))
                    font.fallbackFontAssetTable.Add(candidate);

                if (TMP_Settings.fallbackFontAssets != null && !TMP_Settings.fallbackFontAssets.Contains(candidate))
                {
                    TMP_Settings.fallbackFontAssets.Add(candidate);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Tank Duel: Turkish font fallback setup: " + e.Message);
        }
    }

    private static readonly Dictionary<string, string[]> dict = new Dictionary<string, string[]>
    {
        // Ana Menü
        { "TITLE", new[] { "TANK DÜELLOSU", "TANK DUEL" } },
        { "SUBTITLE", new[] { "YEREL TANK SAVAŞI", "LOCAL TANK COMBAT" } },
        { "GAME_MODE", new[] { "OYUN MODU", "GAME MODE" } },
        { "START_MATCH", new[] { "SAVAŞA BAŞLA", "START MATCH" } },
        { "GARAGE", new[] { "GARAJ", "GARAGE" } },
        { "SETTINGS", new[] { "AYARLAR", "SETTINGS" } },
        { "SELECT_ARENA", new[] { "ARENA SEÇİMİ", "CHOOSE ARENA" } },
        { "ROUND_TIME", new[] { "RAUND SÜRESİ", "ROUND TIME" } },
        { "GRAPHICS", new[] { "GRAFİKLER", "GRAPHICS" } },
        { "LANGUAGE", new[] { "DİL: TÜRKÇE", "LANGUAGE: ENGLISH" } },
        { "MUSIC", new[] { "MÜZİK", "MUSIC" } },
        { "ON", new[] { "AÇIK", "ON" } },
        { "OFF", new[] { "KAPALI", "OFF" } },
        { "BACK", new[] { "GERİ", "BACK" } },
        { "RANDOM_ARENA", new[] { "RASTGELE ARENA", "RANDOM ARENA" } },
        { "JUNGLE", new[] { "ORMAN", "JUNGLE" } },
        { "DESERT", new[] { "ÇÖL", "DESERT" } },
        { "MOON", new[] { "AY ÜSSÜ", "MOON" } },
        { "60_SEC", new[] { "60 SANİYE", "60 SECONDS" } },
        { "90_SEC", new[] { "90 SANİYE", "90 SECONDS" } },
        { "120_SEC", new[] { "120 SANİYE", "120 SECONDS" } },
        { "MODE_PVP", new[] { "MOD: 2 KİŞİLİK (WASD / OKLAR)", "MODE: 2 PLAYERS (WASD / ARROWS)" } },
        { "MODE_PVE", new[] { "MOD: BOTA KARŞI (TEK KİŞİLİK)", "MODE: VS COMPUTER (AI)" } },

        // Garaj & Araçlar
        { "PLAYER_1", new[] { "1. OYUNCU (SOL)", "PLAYER 1 (LEFT)" } },
        { "PLAYER_2", new[] { "2. OYUNCU (SAĞ)", "PLAYER 2 (RIGHT)" } },
        { "SELECT", new[] { "SEÇ", "SELECT" } },
        { "SELECTED", new[] { "SEÇİLDİ", "SELECTED" } },
        { "BUY", new[] { "SATIN AL", "BUY" } },
        { "COINS", new[] { "ALTIN", "COINS" } },
        { "NOT_ENOUGH_COINS", new[] { "YETERSİZ ALTIN!", "NOT ENOUGH COINS!" } },
        { "AVAILABLE", new[] { "KULLANILABİLİR", "AVAILABLE" } },
        { "SHOWCASE_LIVE", new[] { "[ CANLI 3D ARAÇ GÖRÜNÜMÜ ]", "[ 3D LIVE SHOWCASE ]" } },
        { "SHOWCASE_HINT", new[] { "360 DERECE OTOMATİK DÖNÜŞ  •  CANLI 3D ÖNİZLEME", "360 DEGREE ROTATION  •  LIVE 3D PREVIEW" } },
        { "HEALTH_UNIT", new[] { "CAN", "HP" } },
        { "COOLDOWN_UNIT", new[] { "sn BEKLEME", "s COOLDOWN" } },

        // Oyun İçi & HUD
        { "PAUSED", new[] { "DURAKLATILDI", "PAUSED" } },
        { "RESUME", new[] { "DEVAM ET", "RESUME" } },
        { "MAIN_MENU", new[] { "ANA MENÜ", "MAIN MENU" } },
        { "REMATCH", new[] { "YENİDEN OYNA", "REMATCH" } },
        { "DRAW", new[] { "BERABERE!", "DRAW!" } },
        { "P1_WINS", new[] { "1. OYUNCU KAZANDI!", "PLAYER 1 WINS!" } },
        { "P2_WINS", new[] { "2. OYUNCU KAZANDI!", "PLAYER 2 WINS!" } },
        { "BOT_WINS", new[] { "BİLGİSAYAR KAZANDI!", "BOT WINS!" } },
        { "ROUND", new[] { "RAUND", "ROUND" } },
        { "FIRST_TO", new[] { "5 OLAN KAZANIR", "FIRST TO 5" } },
        { "CONTROLS_P1", new[] { "1. OYUNCU: WASD + BOŞLUK", "P1: WASD + SPACE" } },
        { "CONTROLS_P2", new[] { "2. OYUNCU: YÖN TUŞLARI + ENTER", "P2: ARROWS + ENTER" } },
        { "CONTROLS_HINT_PVP", new[] { "1. OYUNCU: WASD + BOŞLUK  •  2. OYUNCU: YÖN TUŞLARI + ENTER", "P1: WASD + SPACE  •  P2: ARROWS + ENTER" } },
        { "CONTROLS_HINT_PVE", new[] { "1. OYUNCU: WASD + BOŞLUK  •  RAKİP: YAPAY ZEKA (BOT)", "P1: WASD + SPACE  •  P2: COMPUTER (AI)" } },
        { "MODE_PVE_TITLE", new[] { "TEK KİŞİLİK (BOTA KARŞI)", "1 PLAYER (VS BOT)" } },
        { "MODE_PVP_TITLE", new[] { "İKİ KİŞİLİK (DÜELLO)", "2 PLAYERS (DUEL)" } },
        { "DIFFICULTY", new[] { "BOT ZORLUĞU", "BOT DIFFICULTY" } },
        { "DIFF_EASY", new[] { "KOLAY", "EASY" } },
        { "DIFF_MED", new[] { "ORTA", "MEDIUM" } },
        { "DIFF_HARD", new[] { "ZOR", "HARD" } },
        { "DIFF_HINT_EASY", new[] { "Kolay Mod: Yavaş nişan alma, düşük sürat ve uzun atış beklemesi", "Easy: Slow aiming, lower speed and long shot cooldown" } },
        { "DIFF_HINT_MED", new[] { "Orta Mod: Dengeli taktiksel manevra, standart hız ve atış", "Medium: Balanced tactical movement, standard speed and fire" } },
        { "DIFF_HINT_HARD", new[] { "Zor Mod: Agresif manevra, seri atış ve yüksek isabet", "Hard: Aggressive maneuvers, rapid fire and high accuracy" } },
        { "GRAPHICS_LOW", new[] { "DÜŞÜK", "LOW" } },
        { "GRAPHICS_MED", new[] { "ORTA", "MEDIUM" } },
        { "GRAPHICS_HIGH", new[] { "YÜKSEK", "HIGH" } },
        { "TIP", new[] { "Basılı tutarak atış menzilini artır  •  ESC ile duraklat", "Hold fire to charge  •  ESC to pause" } },
        { "VICTORIES", new[] { "ZAFER", "WINS" } },

        // Güçlendirmeler (Powerups)
        { "PU_SPEED", new[] { "HIZ ARTIŞI!", "SPEED BOOST!" } },
        { "PU_SHIELD", new[] { "KALKAN!", "SHIELD!" } },
        { "PU_FIRE", new[] { "SERİ ATIŞ!", "RAPID FIRE!" } },
        { "PU_HEAL", new[] { "CAN YENİLENDİ!", "HEALED!" } },
        { "PU_INVINCIBLE", new[] { "ÖLÜMSÜZLÜK!", "INVINCIBLE!" } },
        { "PU_DAMAGE", new[] { "SÜPER MERMİ!", "SUPER SHELL!" } },
        { "PU_COIN", new[] { "+50 ALTIN", "+50 COINS" } },

        // Araç Özellikleri
        { "STAT_SPEED", new[] { "HIZ", "SPEED" } },
        { "STAT_ARMOR", new[] { "ZIRH", "ARMOR" } },
        { "STAT_DAMAGE", new[] { "HASAR", "FIREPOWER" } },
        { "STAT_FIRERATE", new[] { "SERİ ATIŞ", "FIRE RATE" } },
        { "TANK_SPECS", new[] { "TEKNİK ÖZELLİKLER", "SPECIFICATIONS" } },
        { "TANK_ROLE", new[] { "SINIF / ROL", "CLASS / ROLE" } }
    };

    public static string Get(string key)
    {
        if (dict.TryGetValue(key, out string[] values))
        {
            return values[IsTurkish ? 0 : 1];
        }
        return key;
    }
}

public static class TankDuelData
{
    private static readonly Color[] TankColors =
    {
        new Color(0f, 0.80f, 0.92f),
        new Color(0.98f, 0.49f, 0.10f),
        new Color(0.32f, 0.70f, 0.34f),
        new Color(0.90f, 0.22f, 0.22f),
        new Color(0.24f, 0.46f, 0.94f),
        new Color(0.64f, 0.36f, 0.86f),
        new Color(0.80f, 0.69f, 0.45f),
        new Color(0.88f, 0.90f, 0.86f)
    };
    private static readonly string[] ColorNamesTr =
        { "Turkuaz", "Turuncu", "Yeşil", "Kırmızı", "Mavi", "Mor", "Kum", "Beyaz" };
    private static readonly string[] ColorNamesEn =
        { "Cyan", "Orange", "Green", "Red", "Blue", "Purple", "Sand", "White" };

    public static int TankColorCount => TankColors.Length;

    public static Color GetTankColor(int index) => TankColors[Mathf.Clamp(index, 0, TankColors.Length - 1)];

    public static string GetTankColorName(int index) =>
        (TankDuelLocalization.IsTurkish ? ColorNamesTr : ColorNamesEn)[Mathf.Clamp(index, 0, TankColors.Length - 1)];

    public static int GetPlayerColorIndex(int player)
    {
        int fallback = player == 2 ? 1 : 0;
        int index = PlayerPrefs.GetInt("TankDuel.P" + player + "Color", fallback);
        return index >= 0 && index < TankColors.Length ? index : fallback;
    }

    public static Color GetPlayerColor(int player) => GetTankColor(GetPlayerColorIndex(player));

    public static void SetPlayerColorIndex(int player, int index)
    {
        if ((player != 1 && player != 2) || index < 0 || index >= TankColors.Length) return;
        PlayerPrefs.SetInt("TankDuel.P" + player + "Color", index);
        PlayerPrefs.Save();
    }

    public static Color GetOpponentColor() => AIModeEnabled
        ? GetTankColor(GetPlayerColorIndex(1) == 1 ? 0 : 1)
        : GetPlayerColor(2);

    public static int Coins
    {
        get
        {
            if (PlayerPrefs.GetInt("TankDuel.CoinsInitialized", 0) == 0)
            {
                PlayerPrefs.SetInt("TankDuel.CoinsInitialized", 1);
                PlayerPrefs.SetInt("TankDuel.Coins", 0);
                PlayerPrefs.Save();
                return 0;
            }
            return PlayerPrefs.GetInt("TankDuel.Coins", 0);
        }
        set
        {
            PlayerPrefs.SetInt("TankDuel.CoinsInitialized", 1);
            PlayerPrefs.SetInt("TankDuel.Coins", Mathf.Max(0, value));
            PlayerPrefs.Save();
        }
    }

    public static void AddCoins(int amount)
    {
        if (amount > 0)
        {
            Coins = (int)Math.Min(int.MaxValue, (long)Coins + amount);
        }
    }

    public static bool TrySpendCoins(int amount)
    {
        if (amount < 0) return false;
        if (amount == 0) return true;
        if (Coins >= amount)
        {
            Coins -= amount;
            return true;
        }
        return false;
    }

    public static bool IsTankUnlocked(int index)
    {
        if (index == 0) return true;
        if (index < 0 || index >= TankDuel.Archetypes.Length) return false;
        return PlayerPrefs.GetInt("TankDuel.UnlockedTank_" + index, 0) == 1;
    }

    public static void UnlockTank(int index)
    {
        if (index > 0 && index < TankDuel.Archetypes.Length)
        {
            PlayerPrefs.SetInt("TankDuel.UnlockedTank_" + index, 1);
            PlayerPrefs.Save();
        }
    }

    public static int Player1TankIndex
    {
        get
        {
            int idx = Mathf.Clamp(PlayerPrefs.GetInt("TankDuel.P1Tank", 0), 0, TankDuel.Archetypes.Length - 1);
            return IsTankUnlocked(idx) ? idx : 0;
        }
        set
        {
            if (IsTankUnlocked(value))
            {
                PlayerPrefs.SetInt("TankDuel.P1Tank", value);
                PlayerPrefs.Save();
            }
        }
    }

    public static int Player2TankIndex
    {
        get
        {
            int idx = Mathf.Clamp(PlayerPrefs.GetInt("TankDuel.P2Tank", 0), 0, TankDuel.Archetypes.Length - 1);
            return IsTankUnlocked(idx) ? idx : 0;
        }
        set
        {
            if (IsTankUnlocked(value))
            {
                PlayerPrefs.SetInt("TankDuel.P2Tank", value);
                PlayerPrefs.Save();
            }
        }
    }

    public static bool MusicEnabled
    {
        get => PlayerPrefs.GetInt("TankDuel.Music", 1) == 1;
        set
        {
            PlayerPrefs.SetInt("TankDuel.Music", value ? 1 : 0);
            PlayerPrefs.Save();
            TankDuel.ApplyMusicPreference();
        }
    }

    public static bool AIModeEnabled
    {
        get => PlayerPrefs.GetInt("TankDuel.AIMode", 1) == 1;
        set { PlayerPrefs.SetInt("TankDuel.AIMode", value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public static int AIDifficulty
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt("TankDuel.AIDifficulty", 1), 0, 2);
        set { PlayerPrefs.SetInt("TankDuel.AIDifficulty", Mathf.Clamp(value, 0, 2)); PlayerPrefs.Save(); }
    }

    private static readonly int[][] BotTankPools =
    {
        new[] { 0, 1 },
        new[] { 0, 1, 3 },
        new[] { 1, 2, 3, 4 }
    };

    public static int SelectBotTank(int difficulty)
    {
        var pool = BotTankPools[Mathf.Clamp(difficulty, 0, 2)];
        int previous = PlayerPrefs.GetInt("TankDuel.LastBotTank", -1);
        int excluded = Array.IndexOf(pool, previous);
        int choice = UnityEngine.Random.Range(0, pool.Length - (excluded >= 0 ? 1 : 0));
        if (excluded >= 0 && choice >= excluded) choice++;
        int tank = pool[choice];
        PlayerPrefs.SetInt("TankDuel.LastBotTank", tank);
        PlayerPrefs.Save();
        return tank;
    }

    public static int SelectedArena
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt("TankDuel.Arena", 0), 0, DuelArenaCatalog.Scenes.Length - 1);
        set { PlayerPrefs.SetInt("TankDuel.Arena", Mathf.Clamp(value, 0, DuelArenaCatalog.Scenes.Length - 1)); PlayerPrefs.Save(); }
    }

    public static int SelectRandomArena(int arenaCount)
    {
        if (arenaCount < 1) throw new ArgumentOutOfRangeException(nameof(arenaCount));
        int previous = PlayerPrefs.GetInt("TankDuel.LastRandomArena", -1);
        bool excludePrevious = arenaCount > 1 && previous >= 0 && previous < arenaCount;
        int chosen = UnityEngine.Random.Range(0, excludePrevious ? arenaCount - 1 : arenaCount);
        if (excludePrevious && chosen >= previous) chosen++;
        PlayerPrefs.SetInt("TankDuel.LastRandomArena", chosen);
        SelectedArena = chosen;
        return chosen;
    }

    public static int DurationIndex
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt("TankDuel.Duration", 1), 0, 2);
        set { PlayerPrefs.SetInt("TankDuel.Duration", Mathf.Clamp(value, 0, 2)); PlayerPrefs.Save(); }
    }
}
