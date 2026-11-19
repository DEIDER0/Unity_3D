using Dungeon.Controller;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Dungeon.EditorTools
{
    /// <summary>
    /// EDITOR. Menú principal de automatización. Dungeon > Build Everything hace TODO en orden:
    ///   1. Configura URP (si falta).
    ///   2. Crea los datos de los 3 niveles (Assets/Resources/Data) si no existen.
    ///   3. Crea los prefabs (Assets/Prefabs).
    ///   4. Crea las escenas MenuPrincipal, Nivel1-3 y GameOver y las agrega a Build Settings.
    /// </summary>
    public static class DungeonMenu
    {
        [MenuItem("Dungeon/Build Everything", priority = 0)]
        public static void BuildEverything()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            try
            {
                EditorUtility.DisplayProgressBar("Dungeon", "Configurando URP...", 0.05f);
                RenderPipelineSetup.EnsureUrp();

                EditorUtility.DisplayProgressBar("Dungeon", "Creando datos...", 0.15f);
                var data = DataAssetBuilder.EnsureAll();

                EditorUtility.DisplayProgressBar("Dungeon", "Creando prefabs...", 0.3f);
                var bosses = PrefabBuilder.BuildAll(data.Player);
                DataAssetBuilder.AssignBossPrefabs(data, bosses);

                BuildScenes(data);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            EditorSceneManager.OpenScene(SceneBuilder.ScenePath(SceneNames.MainMenu));
            Debug.Log("[Dungeon] ¡Listo! Escenas creadas en Assets/Scenes. Pulsa Play en MenuPrincipal para jugar.");
            EditorUtility.DisplayDialog("Dungeon Depths",
                "Todo listo.\n\nSe crearon los datos, los prefabs y las escenas (MenuPrincipal, Nivel1-3, GameOver).\n\nPulsa Play para jugar.",
                "¡A jugar!");
        }

        [MenuItem("Dungeon/Rebuild Scenes Only", priority = 1)]
        public static void RebuildScenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var data = DataAssetBuilder.EnsureAll();
            try { BuildScenes(data); }
            finally { EditorUtility.ClearProgressBar(); }
            EditorSceneManager.OpenScene(SceneBuilder.ScenePath(SceneNames.MainMenu));
        }

        private static void BuildScenes(DataAssetBuilder.GameData data)
        {
            EditorUtility.DisplayProgressBar("Dungeon", "Creando menú principal...", 0.5f);
            SceneBuilder.BuildMainMenu();

            for (int i = 0; i < SceneNames.LevelCount; i++)
            {
                EditorUtility.DisplayProgressBar("Dungeon", $"Creando Nivel {i + 1}...", 0.55f + 0.1f * i);
                SceneBuilder.BuildLevel(i + 1);
            }

            EditorUtility.DisplayProgressBar("Dungeon", "Creando GameOver...", 0.9f);
            SceneBuilder.BuildGameOver();
            SceneBuilder.RegisterBuildScenes();
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayProgressBar("Dungeon", "Verificando referencias...", 0.95f);
            SceneReferenceFixer.FixAll();
        }
    }
}
