using System.IO;
using Dungeon.Controller;
using Dungeon.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Dungeon.EditorTools
{
    /// <summary>
    /// EDITOR. Abre cada escena del juego, asigna los assets de datos (LevelConfig y PlayerConfig)
    /// a GameController y MenuController, guarda la escena y muestra en la consola lo que quedó asignado.
    /// Menú: Dungeon > Fix Scene References (también se ejecuta al final de Build Everything).
    /// </summary>
    public static class SceneReferenceFixer
    {
        [MenuItem("Dungeon/Fix Scene References", priority = 2)]
        public static void FixMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            bool ok = FixAll();
            EditorSceneManager.OpenScene(SceneBuilder.ScenePath(SceneNames.MainMenu));
            EditorUtility.DisplayDialog("Dungeon Depths",
                ok ? "Referencias reparadas en todas las escenas." : "Hubo problemas: revisa los mensajes [Fix] en la Console.",
                "OK");
        }

        /// <summary>Devuelve true si todas las escenas quedaron con sus datos asignados.</summary>
        public static bool FixAll()
        {
            bool allOk = FixScene(SceneNames.MainMenu, 0) & FixScene(SceneNames.GameOver, 0);
            for (int n = 1; n <= SceneNames.LevelCount; n++) allOk &= FixScene(SceneNames.Level(n), n);
            return allOk;
        }

        private static bool FixScene(string sceneName, int levelNumber)
        {
            string path = SceneBuilder.ScenePath(sceneName);
            if (!File.Exists(path))
            {
                Debug.LogError($"[Fix] No existe {path}. Ejecuta Dungeon > Build Everything.");
                return false;
            }

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            // Los datos se cargan DESPUÉS de abrir la escena (abrirla descarga los assets no usados).
            PlayerConfig player = DataAssetBuilder.LoadPlayer();
            LevelConfig level = levelNumber > 0 ? DataAssetBuilder.LoadLevel(levelNumber) : null;
            bool ok = player != null && (levelNumber == 0 || level != null);
            if (player == null) Debug.LogError($"[Fix] No se pudo cargar {DataAssetBuilder.PlayerPath}.");
            if (levelNumber > 0 && level == null) Debug.LogError($"[Fix] No se pudo cargar {DataAssetBuilder.LevelPath(levelNumber)}.");

            int controllers = 0;
            foreach (var game in Object.FindObjectsByType<GameController>(FindObjectsSortMode.None))
            {
                EditorUtil.Set(game, "levelConfig", level);
                EditorUtil.Set(game, "playerConfig", player);
                ok &= Report(sceneName, game, "levelConfig") & Report(sceneName, game, "playerConfig");
                controllers++;
            }
            foreach (var menu in Object.FindObjectsByType<MenuController>(FindObjectsSortMode.None))
            {
                EditorUtil.Set(menu, "playerConfig", player);
                ok &= Report(sceneName, menu, "playerConfig");
                controllers++;
            }
            if (controllers == 0)
            {
                Debug.LogError($"[Fix] {sceneName}: no tiene GameController ni MenuController. Ejecuta Dungeon > Build Everything.");
                ok = false;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return ok;
        }

        private static bool Report(string sceneName, Object target, string field)
        {
            var value = new SerializedObject(target).FindProperty(field).objectReferenceValue;
            if (value != null) Debug.Log($"[Fix] {sceneName} · {target.GetType().Name}.{field} = {value.name} ✔");
            else Debug.LogError($"[Fix] {sceneName} · {target.GetType().Name}.{field} sigue vacío ✘");
            return value != null;
        }
    }
}
