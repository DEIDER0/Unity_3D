using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Dungeon.EditorTools
{
    /// <summary>
    /// EDITOR. Crea una escena de prueba con todos los modelos, para revisar escala,
    /// orientación y materiales. Menú: Dungeon > Build Model Showcase Scene.
    /// Los personajes se giran 180° para mirar a la cámara: si los ves de frente,
    /// los ejes del FBX están bien.
    /// </summary>
    public static class ModelShowcaseBuilder
    {
        private const string ScenePath = "Assets/Scenes/Test/ModelShowcase.unity";

        [MenuItem("Dungeon/Build Model Showcase Scene", priority = 20)]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            PrefabBuilder.BuildAll(DataAssetBuilder.EnsureAll().Player);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Piso: rejilla de baldosas de 4 m
            for (int x = -3; x < 3; x++)
                for (int z = -1; z < 3; z++)
                    Place($"{PrefabBuilder.EnvironmentFolder}/Room_Floor.prefab", new Vector3(x * 4f + 2f, 0f, z * 4f + 2f), 0f);

            // Fila 1: personajes y cofre, mirando a la cámara
            string[] row1 =
            {
                $"{PrefabBuilder.CharactersFolder}/Player.prefab",
                $"{PrefabBuilder.CharactersFolder}/Enemy_Goblin.prefab",
                $"{PrefabBuilder.CharactersFolder}/Boss_Ogre.prefab",
                $"{PrefabBuilder.CharactersFolder}/Boss_Golem.prefab",
                $"{PrefabBuilder.CharactersFolder}/Boss_Demon.prefab",
                $"{PrefabBuilder.PropsFolder}/Chest.prefab",
            };
            for (int i = 0; i < row1.Length; i++)
                Place(row1[i], new Vector3(-10f + i * 4f, 0f, 0f), 180f);

            // Fila 2: piezas de sala, con la puerta dentro del arco
            Place($"{PrefabBuilder.EnvironmentFolder}/Room_Wall.prefab", new Vector3(-6f, 0f, 9f), 0f);
            Place($"{PrefabBuilder.EnvironmentFolder}/Room_Corner.prefab", new Vector3(-3.6f, 0f, 9f), 0f);
            Place($"{PrefabBuilder.EnvironmentFolder}/Room_Arch.prefab", new Vector3(0f, 0f, 9f), 0f);
            Place($"{PrefabBuilder.EnvironmentFolder}/Door.prefab", new Vector3(0f, 0f, 9f), 0f);
            Place($"{PrefabBuilder.EnvironmentFolder}/Room_Arch.prefab", new Vector3(5f, 0f, 9f), 0f);

            var camera = Camera.main;
            if (camera != null)
            {
                camera.transform.position = new Vector3(0f, 7f, -13f);
                camera.transform.LookAt(new Vector3(0f, 1.5f, 3f));
            }

            PrefabBuilder.EnsureFolder("Assets/Scenes/Test");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[ModelShowcaseBuilder] Escena creada: {ScenePath}");
        }

        private static void Place(string prefabPath, Vector3 position, float yRotation)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[ModelShowcaseBuilder] Falta el prefab {prefabPath}");
                return;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yRotation, 0f));
        }
    }
}
