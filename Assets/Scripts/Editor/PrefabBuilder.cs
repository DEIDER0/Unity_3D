using UnityEditor;
using UnityEngine;

namespace Dungeon.EditorTools
{
    /// <summary>
    /// EDITOR. Crea los prefabs a partir de los FBX de Assets/Models.
    /// Menú: Dungeon > Build Model Prefabs.
    ///
    /// Cada prefab tiene una raíz vacía con los componentes (colliders y, en fases siguientes,
    /// controladores y vistas) y un hijo "Model" con el FBX. Así se puede cambiar el modelo
    /// sin perder los componentes.
    /// OJO: volver a ejecutar el menú SOBRESCRIBE los prefabs. Si quieren cambiar algo de forma
    /// permanente, cámbienlo en este script.
    /// </summary>
    public static class PrefabBuilder
    {
        public const string ModelsFolder = "Assets/Models";
        public const string EnvironmentFolder = "Assets/Prefabs/Environment";
        public const string PropsFolder = "Assets/Prefabs/Props";
        public const string CharactersFolder = "Assets/Prefabs/Characters";

        public static readonly string[] CharacterModels =
            { "Player", "Enemy_Goblin", "Boss_Ogre", "Boss_Golem", "Boss_Demon" };

        [MenuItem("Dungeon/Build Model Prefabs", priority = 0)]
        public static void BuildAll()
        {
            BuildEnvironment();
            BuildProps();
            BuildCharacters();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PrefabBuilder] Prefabs creados en Assets/Prefabs.");
        }

        // ------------------------------------------------------------------
        // Piezas de sala
        // ------------------------------------------------------------------
        private static void BuildEnvironment()
        {
            foreach (string name in new[] { "Room_Floor", "Room_Wall", "Room_Corner" })
            {
                GameObject root = CreateRootWithModel(name, name);
                if (root == null) continue;
                AddBoxColliderFromBounds(root);
                Save(root, EnvironmentFolder, name);
            }

            // Arco: dos colliders laterales y uno superior; el hueco queda libre para pasar.
            GameObject arch = CreateRootWithModel("Room_Arch", "Room_Arch");
            if (arch != null)
            {
                AddBox(arch, new Vector3(-1.5f, 1.5f, 0f), new Vector3(1f, 3f, 0.5f));
                AddBox(arch, new Vector3(1.5f, 1.5f, 0f), new Vector3(1f, 3f, 0.5f));
                AddBox(arch, new Vector3(0f, 2.85f, 0f), new Vector3(2f, 0.3f, 0.5f));
                Save(arch, EnvironmentFolder, "Room_Arch");
            }

            GameObject door = CreateRootWithModel("Door", "Door");
            if (door != null)
            {
                AddBoxColliderFromBounds(door);
                Save(door, EnvironmentFolder, "Door");
            }
        }

        // ------------------------------------------------------------------
        // Objetos
        // ------------------------------------------------------------------
        private static void BuildProps()
        {
            GameObject chest = CreateRootWithModel("Chest", "Chest");
            if (chest == null) return;
            AddBoxColliderFromBounds(chest);
            Save(chest, PropsFolder, "Chest");
        }

        // ------------------------------------------------------------------
        // Personajes (por ahora solo modelo y collider; los componentes llegan en las fases c, d y g)
        // ------------------------------------------------------------------
        private static void BuildCharacters()
        {
            foreach (string name in CharacterModels)
            {
                GameObject root = CreateRootWithModel(name, name);
                if (root == null) continue;
                AddCapsuleFromBounds(root);
                Save(root, CharactersFolder, name);
            }
        }

        // ------------------------------------------------------------------
        // Utilidades
        // ------------------------------------------------------------------
        public static GameObject CreateRootWithModel(string modelName, string rootName)
        {
            string path = $"{ModelsFolder}/{modelName}.fbx";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null)
            {
                Debug.LogError($"[PrefabBuilder] No se encontró {path}. Genera los modelos con BlenderScripts/generar_todo.py.");
                return null;
            }

            var root = new GameObject(rootName);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            instance.name = "Model";
            instance.transform.localPosition = Vector3.zero;
            return root;
        }

        public static Bounds GetLocalBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one);

            // La raíz está en el origen y sin rotación, así que los bounds del mundo son locales.
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        public static void AddBoxColliderFromBounds(GameObject root)
        {
            Bounds b = GetLocalBounds(root);
            AddBox(root, b.center, b.size);
        }

        public static BoxCollider AddBox(GameObject root, Vector3 center, Vector3 size)
        {
            var box = root.AddComponent<BoxCollider>();
            box.center = center;
            box.size = size;
            return box;
        }

        public static CapsuleCollider AddCapsuleFromBounds(GameObject root)
        {
            Bounds b = GetLocalBounds(root);
            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.direction = 1; // eje Y
            capsule.height = b.size.y;
            // El radio usa la profundidad del cuerpo (no los brazos ni el arma), con un mínimo razonable.
            capsule.radius = Mathf.Clamp(Mathf.Min(b.size.x, b.size.z) * 0.5f, 0.2f, b.size.y * 0.5f);
            capsule.center = new Vector3(0f, b.size.y * 0.5f, 0f);
            return capsule;
        }

        public static void Save(GameObject root, string folder, string name)
        {
            EnsureFolder(folder);
            PrefabUtility.SaveAsPrefabAsset(root, $"{folder}/{name}.prefab");
            Object.DestroyImmediate(root);
        }

        public static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }
    }
}
