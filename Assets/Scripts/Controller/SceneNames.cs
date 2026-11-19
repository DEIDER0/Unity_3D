namespace Dungeon.Controller
{
    /// <summary>CONTROLADOR (constantes). Nombres de las escenas (deben estar en Build Settings).</summary>
    public static class SceneNames
    {
        public const string MainMenu = "MenuPrincipal";
        public const string GameOver = "GameOver";
        public const int LevelCount = 3;

        public static string Level(int number) => "Nivel" + number;
    }
}
