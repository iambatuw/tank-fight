public static class DuelArenaCatalog
{
    public static readonly string[] Scenes =
    {
        "Duel_Jungle", "Duel_Desert", "Duel_Moon",
        "Duel_Canyon", "Duel_Harbor", "Duel_Arctic"
    };

    public static string ScenePath(string scene) => "Assets/Scenes/" + scene + ".unity";
}
