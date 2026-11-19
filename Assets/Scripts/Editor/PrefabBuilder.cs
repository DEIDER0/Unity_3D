using Dungeon.Controller;
using Dungeon.Data;
using Dungeon.View;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Dungeon.EditorTools
{
    /// <summary>
    /// EDITOR. Crea los prefabs a partir de los FBX de Assets/Models, con sus colliders,
    /// controladores y vistas ya conectados. Menú: Dungeon > Build Model Prefabs
    /// (también lo ejecuta Dungeon > Build Everything).
    ///
    /// Estructura de un personaje:
    ///   Player (Rigidbody, CapsuleCollider, PlayerController, CharacterView)
    ///   └── Visual          (pivote a media altura: balanceo, voltereta, caída)
    ///       ├── Model       (el FBX desempaquetado)
    ///       └── WeaponPivot (pivote en la mano; el arma cuelga de aquí)
    ///
    /// OJO: volver a ejecutar el menú SOBRESCRIBE los prefabs. Los cambios permanentes van en este script
    /// (los números del juego van en los assets de Assets/Resources/Data, que no se sobrescriben).
    /// </summary>
    public static class PrefabBuilder
    {
        public const string ModelsFolder = "Assets/Models";
        public const string EnvironmentFolder = "Assets/Prefabs/Environment";
        public const string PropsFolder = "Assets/Prefabs/Props";
        public const string CharactersFolder = "Assets/Prefabs/Characters";
        public const string InputActionsPath = "Assets/Input/DungeonInput.inputactions";

        public static readonly string[] BossModels = { "Boss_Ogre", "Boss_Golem", "Boss_Demon" };

        [MenuItem("Dungeon/Build Model Prefabs", priority = 10)]
        public static void BuildAllMenu()
        {
            var data = DataAssetBuilder.EnsureAll();
            BuildAll(data.Player);
            AssetDatabase.Refresh();
            Debug.Log("[PrefabBuilder] Prefabs creados en Assets/Prefabs.");
        }

        /// <summary>Crea todos los prefabs. Devuelve los prefabs de los 3 jefes (en orden de nivel).</summary>
        public static GameObject[] BuildAll(PlayerConfig playerConfig)
        {
            BuildEnvironment();
            BuildChest();
            BuildPlayer(playerConfig);
            BuildEnemy();
            var bosses = new GameObject[BossModels.Length];
            for (int i = 0; i < BossModels.Length; i++) bosses[i] = BuildBoss(BossModels[i]);
            AssetDatabase.SaveAssets();
            return bosses;
        }

        // ------------------------------------------------------------------
        // Piezas de sala y puerta
        // ------------------------------------------------------------------
        private static void BuildEnvironment()
        {
            foreach (string name in new[] { "Room_Floor", "Room_Wall", "Room_Corner" })
            {
                GameObject root = CreateRootWithModel(name, name, unpack: false);
                if (root == null) continue;
                AddBoxColliderFromBounds(root);
                Save(root, EnvironmentFolder, name);
            }

            // Arco: dos colliders laterales y uno superior; el hueco (2 m) queda libre para pasar.
            GameObject arch = CreateRootWithModel("Room_Arch", "Room_Arch", unpack: false);
            if (arch != null)
            {
                AddBox(arch, new Vector3(-1.5f, 1.5f, 0f), new Vector3(1f, 3f, 0.5f));
                AddBox(arch, new Vector3(1.5f, 1.5f, 0f), new Vector3(1f, 3f, 0.5f));
                AddBox(arch, new Vector3(0f, 2.85f, 0f), new Vector3(2f, 0.3f, 0.5f));
                Save(arch, EnvironmentFolder, "Room_Arch");
            }

            GameObject door = CreateRootWithModel("Door", "Door", unpack: false);
            if (door != null)
            {
                AddBoxColliderFromBounds(door);
                var view = door.AddComponent<DoorView>();
                EditorUtil.Set(view, "doorLeaf", door.transform.Find("Model"));
                Save(door, EnvironmentFolder, "Door");
            }
        }

        // ------------------------------------------------------------------
        // Cofre
        // ------------------------------------------------------------------
        private static void BuildChest()
        {
            GameObject chest = CreateRootWithModel("Chest", "Chest", unpack: true);
            if (chest == null) return;
            AddBoxColliderFromBounds(chest);

            var trigger = chest.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 2.2f;
            trigger.center = new Vector3(0f, 0.5f, 0f);

            // Pivote de la tapa alineado con la raíz, en la bisagra (el origen de Chest_Lid).
            Transform lid = EditorUtil.FindDeep(chest.transform, "Chest_Lid");
            Transform lidPivot = null;
            if (lid != null)
            {
                lidPivot = new GameObject("LidPivot").transform;
                lidPivot.SetParent(chest.transform, false);
                lidPivot.position = lid.position;
                lid.SetParent(lidPivot, true);
            }
            else Debug.LogError("[PrefabBuilder] No se encontró Chest_Lid en el modelo del cofre.");

            // Aviso "[E] Abrir" sobre el cofre
            Canvas prompt = EditorUtil.CreateWorldCanvas("Prompt", chest.transform, new Vector3(0f, 1.6f, 0f), new Vector2(220f, 60f));
            EditorUtil.CreateImage("Background", prompt.transform, new Color(0f, 0f, 0f, 0.65f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220f, 60f));
            EditorUtil.CreateText("Text", prompt.transform, "[E] Abrir", 34, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220f, 60f));

            Light glow = EditorUtil.CreatePointLight("Glow", chest.transform, new Vector3(0f, 1f, 0f),
                new Color(1f, 0.8f, 0.3f), 4f, 5f);

            var view = chest.AddComponent<ChestView>();
            EditorUtil.Set(view, "lid", lidPivot);
            EditorUtil.Set(view, "prompt", prompt.gameObject);
            EditorUtil.Set(view, "glow", glow);

            var controller = chest.AddComponent<ChestController>();
            EditorUtil.Set(controller, "view", view);
            Save(chest, PropsFolder, "Chest");
        }

        // ------------------------------------------------------------------
        // Personajes
        // ------------------------------------------------------------------
        private static void BuildPlayer(PlayerConfig config)
        {
            GameObject root = BuildCharacterBase("Player", "Player_Sword", out CharacterView view, out float height);
            if (root == null) return;

            AddCapsule(root, 1.8f, 0.35f);
            AddRigidbody(root, 1f);
            EditorUtil.Set(view, "deathSound", (int)Sfx.PlayerDeath);
            EditorUtil.Set(view, "hitColor", new Color(1f, 0.15f, 0.15f));

            var controller = root.AddComponent<PlayerController>();
            EditorUtil.Set(controller, "config", config);
            EditorUtil.Set(controller, "inputActions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath));
            EditorUtil.Set(controller, "view", view);
            Save(root, CharactersFolder, "Player");
        }

        private static void BuildEnemy()
        {
            GameObject root = BuildCharacterBase("Enemy_Goblin", "Enemy_Weapon", out CharacterView view, out float height);
            if (root == null) return;

            AddCapsule(root, height, 0.35f);
            AddRigidbody(root, 1.5f);
            var bar = AddHealthBar(root, height + 0.35f, new Vector2(120f, 16f));

            var controller = root.AddComponent<EnemyController>();
            EditorUtil.Set(controller, "view", view);
            EditorUtil.Set(controller, "healthBar", bar);
            Save(root, CharactersFolder, "Enemy_Goblin");
        }

        private static GameObject BuildBoss(string modelName)
        {
            GameObject root = BuildCharacterBase(modelName, "Boss_Weapon", out CharacterView view, out float height);
            if (root == null) return null;

            Bounds b = GetLocalBounds(root);
            AddCapsule(root, height, Mathf.Clamp(Mathf.Min(b.size.x, b.size.z) * 0.4f, 0.5f, 1.2f));
            AddRigidbody(root, 40f);
            EditorUtil.Set(view, "bobHeight", 0.04f);
            EditorUtil.Set(view, "bobFrequency", 6f);

            // Círculo rojo que anuncia el golpe de área
            var telegraphGo = new GameObject("Telegraph");
            telegraphGo.transform.SetParent(root.transform, false);
            var indicator = EditorUtil.CreatePrimitive(PrimitiveType.Cylinder, "Indicator", telegraphGo.transform,
                new Vector3(0f, 0.03f, 0f), new Vector3(1f, 0.02f, 1f),
                EditorUtil.GetUnlitMaterial("M_Telegraph", new Color(0.9f, 0.1f, 0.05f)));
            indicator.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var telegraph = telegraphGo.AddComponent<TelegraphView>();
            EditorUtil.Set(telegraph, "indicator", indicator.transform);

            var controller = root.AddComponent<BossController>();
            EditorUtil.Set(controller, "view", view);
            EditorUtil.Set(controller, "telegraph", telegraph);
            return Save(root, CharactersFolder, modelName);
        }

        /// <summary>Raíz + Visual + Model desempaquetado + WeaponPivot, y el CharacterView conectado.</summary>
        private static GameObject BuildCharacterBase(string modelName, string weaponName, out CharacterView view, out float height)
        {
            view = null;
            height = 1f;
            GameObject root = CreateRootWithModel(modelName, modelName, unpack: true);
            if (root == null) return null;

            Transform model = root.transform.Find("Model");
            Bounds bounds = GetLocalBounds(root);
            height = bounds.size.y;

            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            visual.localPosition = new Vector3(0f, height * 0.5f, 0f);
            model.SetParent(visual, true); // conserva la posición en el mundo

            Transform weapon = EditorUtil.FindDeep(model, weaponName);
            Transform pivot = null;
            if (weapon != null)
            {
                pivot = new GameObject("WeaponPivot").transform;
                pivot.SetParent(visual, false);
                pivot.position = weapon.position;
                pivot.rotation = root.transform.rotation;
                weapon.SetParent(pivot, true);
            }
            else Debug.LogError($"[PrefabBuilder] No se encontró '{weaponName}' en {modelName}.");

            view = root.AddComponent<CharacterView>();
            EditorUtil.Set(view, "visualRoot", visual);
            EditorUtil.Set(view, "weaponPivot", pivot);
            return root;
        }

        private static EnemyHealthBarView AddHealthBar(GameObject root, float y, Vector2 size)
        {
            Canvas canvas = EditorUtil.CreateWorldCanvas("HealthBar", root.transform, new Vector3(0f, y, 0f), size);
            var barRoot = EditorUtil.CreateRect("Bar", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            RectTransform fill = EditorUtil.CreateBar("Background", barRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, size, new Color(0f, 0f, 0f, 0.7f), new Color(0.9f, 0.2f, 0.15f), out _);
            var view = canvas.gameObject.AddComponent<EnemyHealthBarView>();
            EditorUtil.Set(view, "barRoot", barRoot.gameObject);
            EditorUtil.Set(view, "fill", fill);
            return view;
        }

        // ------------------------------------------------------------------
        // Utilidades
        // ------------------------------------------------------------------
        public static GameObject CreateRootWithModel(string modelName, string rootName, bool unpack)
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
            if (unpack) PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = "Model";
            instance.transform.localPosition = Vector3.zero;
            return root;
        }

        public static Bounds GetLocalBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0) return new Bounds(Vector3.up * 0.5f, Vector3.one);

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

        private static void AddCapsule(GameObject root, float height, float radius)
        {
            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.direction = 1; // eje Y
            capsule.height = height;
            capsule.radius = Mathf.Min(radius, height * 0.5f);
            capsule.center = new Vector3(0f, height * 0.5f, 0f);
            capsule.sharedMaterial = GetNoFrictionMaterial();
        }

        private static void AddRigidbody(GameObject root, float mass)
        {
            var body = root.AddComponent<Rigidbody>();
            body.mass = mass;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

#if UNITY_6000_0_OR_NEWER
        private static PhysicsMaterial GetNoFrictionMaterial()
        {
            const string path = EditorUtil.MaterialsFolder + "/PM_NoFriction.asset";
            var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (mat != null) return mat;
            EditorUtil.EnsureFolder(EditorUtil.MaterialsFolder);
            mat = new PhysicsMaterial("PM_NoFriction")
            {
                dynamicFriction = 0f, staticFriction = 0f, bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum, bounceCombine = PhysicsMaterialCombine.Minimum
            };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
#else
        private static PhysicMaterial GetNoFrictionMaterial()
        {
            const string path = EditorUtil.MaterialsFolder + "/PM_NoFriction.asset";
            var mat = AssetDatabase.LoadAssetAtPath<PhysicMaterial>(path);
            if (mat != null) return mat;
            EditorUtil.EnsureFolder(EditorUtil.MaterialsFolder);
            mat = new PhysicMaterial("PM_NoFriction")
            {
                dynamicFriction = 0f, staticFriction = 0f, bounciness = 0f,
                frictionCombine = PhysicMaterialCombine.Minimum, bounceCombine = PhysicMaterialCombine.Minimum
            };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
#endif

        public static GameObject Save(GameObject root, string folder, string name)
        {
            EditorUtil.EnsureFolder(folder);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{folder}/{name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        public static void EnsureFolder(string folder) => EditorUtil.EnsureFolder(folder);
    }
}
