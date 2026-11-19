using System.Collections.Generic;
using Dungeon.Controller;
using Dungeon.Data;
using Dungeon.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dungeon.EditorTools
{
    /// <summary>
    /// EDITOR. Arma automáticamente las escenas MenuPrincipal, Nivel1, Nivel2, Nivel3 y GameOver
    /// y las agrega a Build Settings. Los tres niveles se construyen con el MISMO código:
    /// solo cambian los datos de su LevelConfig (enemigos, jefe, cofre, colores).
    ///
    /// Distribución de un nivel (vista desde arriba, el jugador avanza hacia +Z):
    ///   [Sala 1: entrada 12x12] → puerta → [Sala 2: cofre 12x12] → puerta → [Sala 3: jefe 20x16] → puerta → salida
    /// </summary>
    public static class SceneBuilder
    {
        public const string ScenesFolder = "Assets/Scenes";
        private const float Tile = 4f;

        // Tamaño de cada sala en baldosas de 4 m (ancho impar para que la puerta quede centrada).
        private static readonly int[] RoomWidths = { 3, 3, 5 };
        private static readonly int[] RoomDepths = { 3, 3, 4 };

        private static string EnvPath(string name) => $"{PrefabBuilder.EnvironmentFolder}/{name}.prefab";

        public static string ScenePath(string sceneName) => $"{ScenesFolder}/{sceneName}.unity";

        // ==================================================================
        // Nivel
        // ==================================================================
        public static void BuildLevel(LevelConfig config, PlayerConfig playerConfig)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupLighting(config);

            var environment = new GameObject("Environment").transform;
            var lights = new GameObject("Lights").transform;

            // Posición Z donde empieza cada sala
            var roomStartZ = new float[3];
            float z = 0f;
            for (int r = 0; r < 3; r++)
            {
                roomStartZ[r] = z;
                z += RoomDepths[r] * Tile;
            }
            float levelEndZ = z;

            var corners = new HashSet<Vector2Int>();
            for (int r = 0; r < 3; r++) BuildRoomGeometry(r, roomStartZ[r], environment, corners);
            BuildExitAlcove(levelEndZ, environment, corners, config);
            for (int r = 0; r < 3; r++) BuildRoomLights(r, roomStartZ[r], lights, config.torchColor);

            // Jugador y cámara
            var player = Instantiate($"{PrefabBuilder.CharactersFolder}/Player.prefab", new Vector3(0f, 0.05f, 3f), 0f, null);
            var playerController = player.GetComponent<PlayerController>();
            var cameraView = CreateGameCamera(player.transform, config.fogColor);

            // Salas
            var rooms = new RoomController[3];
            for (int r = 0; r < 3; r++)
                rooms[r] = BuildRoomLogic(r, roomStartZ[r], config);

            // HUD
            CreateHud(out HUDView hud, out BossHealthBarView bossBar);

            // GameController
            var gameGo = new GameObject("GameController");
            var game = gameGo.AddComponent<GameController>();
            EditorUtil.Set(game, "levelConfig", config);
            EditorUtil.Set(game, "playerConfig", playerConfig);
            EditorUtil.Set(game, "player", playerController);
            EditorUtil.SetArray(game, "rooms", rooms);
            EditorUtil.Set(game, "hud", hud);
            EditorUtil.Set(game, "bossBar", bossBar);
            EditorUtil.Set(game, "cameraView", cameraView);

            EditorUtil.EnsureFolder(ScenesFolder);
            EditorSceneManager.SaveScene(scene, ScenePath(SceneNames.Level(config.levelNumber)));
        }

        private static void SetupLighting(LevelConfig config)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = config.ambientColor;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = config.fogColor;
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.skybox = null;

            var sun = new GameObject("Directional Light");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.45f;
            light.color = Color.Lerp(Color.white, config.torchColor, 0.25f);
            light.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
        }

        private static CameraFollowView CreateGameCamera(Transform target, Color background)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            camera.fieldOfView = 45f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 120f;
            go.AddComponent<AudioListener>();
            var follow = go.AddComponent<CameraFollowView>();
            EditorUtil.Set(follow, "target", target);
            go.transform.position = target.position + Quaternion.Euler(68f, 0f, 0f) * new Vector3(0f, 0f, -16f);
            go.transform.rotation = Quaternion.Euler(68f, 0f, 0f);
            return follow;
        }

        // ------------------------------------------------------------------
        // Geometría: piso, paredes, arcos y columnas
        // ------------------------------------------------------------------
        private static void BuildRoomGeometry(int r, float startZ, Transform parent, HashSet<Vector2Int> corners)
        {
            int w = RoomWidths[r], d = RoomDepths[r];
            float halfWidth = w * Tile * 0.5f;
            float endZ = startZ + d * Tile;
            var room = new GameObject($"Room{r + 1}_Geometry").transform;
            room.SetParent(parent, false);

            for (int i = 0; i < w; i++)
                for (int j = 0; j < d; j++)
                    Instantiate(EnvPath("Room_Floor"), new Vector3(TileX(i, w), 0f, startZ + Tile * (j + 0.5f)), 0f, room);

            // Paredes laterales (oeste y este)
            for (int j = 0; j < d; j++)
            {
                float zc = startZ + Tile * (j + 0.5f);
                Instantiate(EnvPath("Room_Wall"), new Vector3(-halfWidth, 0f, zc), 90f, room);
                Instantiate(EnvPath("Room_Wall"), new Vector3(halfWidth, 0f, zc), 90f, room);
            }

            // Pared sur: la primera sala tiene pared lisa; las demás, el arco por el que se entra.
            // (Las salas siguientes son iguales o más anchas, así que esta pared cubre el límite compartido.)
            for (int i = 0; i < w; i++)
            {
                bool center = i == w / 2;
                string piece = r > 0 && center ? "Room_Arch" : "Room_Wall";
                Instantiate(EnvPath(piece), new Vector3(TileX(i, w), 0f, startZ), 0f, room);
            }

            // Pared norte solo en la última sala (con el arco de salida)
            if (r == 2)
                for (int i = 0; i < w; i++)
                    Instantiate(EnvPath(i == w / 2 ? "Room_Arch" : "Room_Wall"), new Vector3(TileX(i, w), 0f, endZ), 0f, room);

            AddCorner(new Vector3(-halfWidth, 0f, startZ), room, corners);
            AddCorner(new Vector3(halfWidth, 0f, startZ), room, corners);
            AddCorner(new Vector3(-halfWidth, 0f, endZ), room, corners);
            AddCorner(new Vector3(halfWidth, 0f, endZ), room, corners);
            // Columnas a los lados de cada arco
            if (r > 0)
            {
                AddCorner(new Vector3(-Tile * 0.5f, 0f, startZ), room, corners);
                AddCorner(new Vector3(Tile * 0.5f, 0f, startZ), room, corners);
            }
        }

        /// <summary>Pequeño pasillo después de la puerta del jefe, con un portal de salida.</summary>
        private static void BuildExitAlcove(float startZ, Transform parent, HashSet<Vector2Int> corners, LevelConfig config)
        {
            var alcove = new GameObject("Exit_Geometry").transform;
            alcove.SetParent(parent, false);
            float zc = startZ + Tile * 0.5f;
            Instantiate(EnvPath("Room_Floor"), new Vector3(0f, 0f, zc), 0f, alcove);
            Instantiate(EnvPath("Room_Wall"), new Vector3(-Tile * 0.5f, 0f, zc), 90f, alcove);
            Instantiate(EnvPath("Room_Wall"), new Vector3(Tile * 0.5f, 0f, zc), 90f, alcove);
            Instantiate(EnvPath("Room_Wall"), new Vector3(0f, 0f, startZ + Tile), 0f, alcove);
            AddCorner(new Vector3(-Tile * 0.5f, 0f, startZ), alcove, corners);
            AddCorner(new Vector3(Tile * 0.5f, 0f, startZ), alcove, corners);
            AddCorner(new Vector3(-Tile * 0.5f, 0f, startZ + Tile), alcove, corners);
            AddCorner(new Vector3(Tile * 0.5f, 0f, startZ + Tile), alcove, corners);

            Color portalColor = Color.Lerp(config.torchColor, Color.white, 0.4f);
            EditorUtil.CreatePrimitive(PrimitiveType.Cylinder, "Portal", alcove, new Vector3(0f, 0.03f, zc + 0.5f),
                new Vector3(2.2f, 0.02f, 2.2f), EditorUtil.GetUnlitMaterial($"M_Portal_L{config.levelNumber}", portalColor));
            EditorUtil.CreatePointLight("PortalLight", alcove, new Vector3(0f, 1.5f, zc + 0.5f), portalColor, 3f, 7f);
        }

        private static void BuildRoomLights(int r, float startZ, Transform parent, Color color)
        {
            int w = RoomWidths[r], d = RoomDepths[r];
            float x = w * Tile * 0.5f - 0.45f;
            float endZ = startZ + d * Tile;
            var torchMat = EditorUtil.GetUnlitMaterial("M_TorchFlame", new Color(1f, 0.75f, 0.35f));

            var positions = new List<Vector3> { new Vector3(-x, 2.2f, (startZ + endZ) * 0.5f), new Vector3(x, 2.2f, (startZ + endZ) * 0.5f) };
            if (r == 2)
            {
                positions.Add(new Vector3(-x, 2.2f, startZ + 3f));
                positions.Add(new Vector3(x, 2.2f, startZ + 3f));
                positions.Add(new Vector3(-x, 2.2f, endZ - 3f));
                positions.Add(new Vector3(x, 2.2f, endZ - 3f));
            }

            int n = 0;
            foreach (var p in positions)
            {
                var torch = new GameObject($"Torch_R{r + 1}_{n++}").transform;
                torch.SetParent(parent, false);
                torch.position = p;
                EditorUtil.CreatePrimitive(PrimitiveType.Cylinder, "Sconce", torch, new Vector3(0f, -0.25f, 0f),
                    new Vector3(0.12f, 0.2f, 0.12f), EditorUtil.GetUnlitMaterial("M_Iron", new Color(0.12f, 0.12f, 0.13f)));
                EditorUtil.CreatePrimitive(PrimitiveType.Sphere, "Flame", torch, Vector3.zero,
                    new Vector3(0.18f, 0.28f, 0.18f), torchMat);
                EditorUtil.CreatePointLight("Light", torch, new Vector3(Mathf.Sign(-p.x) * 0.5f, 0.2f, 0f), color, 2.6f, 11f);
            }
        }

        private static float TileX(int i, int width) => (i - (width - 1) * 0.5f) * Tile;

        private static void AddCorner(Vector3 position, Transform parent, HashSet<Vector2Int> placed)
        {
            var key = new Vector2Int(Mathf.RoundToInt(position.x * 10f), Mathf.RoundToInt(position.z * 10f));
            if (!placed.Add(key)) return;
            Instantiate(EnvPath("Room_Corner"), position, 0f, parent);
        }

        // ------------------------------------------------------------------
        // Lógica de cada sala: enemigos, cofre, jefe, puerta y zona de salida
        // ------------------------------------------------------------------
        private static RoomController BuildRoomLogic(int r, float startZ, LevelConfig config)
        {
            int w = RoomWidths[r], d = RoomDepths[r];
            float endZ = startZ + d * Tile;
            var roomGo = new GameObject($"Room{r + 1}_{(r == 0 ? "Entrada" : r == 1 ? "Cofre" : "Jefe")}");
            var room = roomGo.AddComponent<RoomController>();
            EditorUtil.Set(room, "roomIndex", r);

            // Enemigos básicos, repartidos al fondo de la sala, mirando hacia la entrada
            int count = config.enemiesPerRoom != null && r < config.enemiesPerRoom.Length ? config.enemiesPerRoom[r] : 0;
            var enemies = new List<Object>();
            float spread = w * Tile * 0.6f;
            for (int k = 0; k < count; k++)
            {
                float x = count == 1 ? 0f : Mathf.Lerp(-spread * 0.5f, spread * 0.5f, k / (float)(count - 1));
                float zPos = startZ + d * Tile * (r == 2 ? 0.45f : 0.72f) + (k % 2) * 1.2f;
                if (r == 1 && Mathf.Abs(x) < 1.5f) x += 3f; // no encima del cofre
                var enemy = Instantiate($"{PrefabBuilder.CharactersFolder}/Enemy_Goblin.prefab", new Vector3(x, 0.05f, zPos), 180f, roomGo.transform);
                enemies.Add(enemy.GetComponent<EnemyController>());
            }
            EditorUtil.SetArray(room, "enemies", enemies.ToArray());

            if (r == 1)
            {
                var chest = Instantiate($"{PrefabBuilder.PropsFolder}/Chest.prefab", new Vector3(0f, 0f, startZ + d * Tile * 0.55f), 180f, roomGo.transform);
                EditorUtil.Set(room, "chest", chest.GetComponent<ChestController>());
            }

            if (r == 2)
            {
                GameObject bossPrefab = config.bossPrefab;
                if (bossPrefab == null) Debug.LogError($"[SceneBuilder] El nivel {config.levelNumber} no tiene bossPrefab asignado.");
                else
                {
                    var bossGo = (GameObject)PrefabUtility.InstantiatePrefab(bossPrefab);
                    bossGo.transform.SetParent(roomGo.transform, false);
                    bossGo.transform.SetPositionAndRotation(new Vector3(0f, 0.05f, startZ + d * Tile * 0.7f), Quaternion.Euler(0f, 180f, 0f));
                    EditorUtil.Set(room, "boss", bossGo.GetComponent<BossController>());
                }
            }

            // Puerta de salida (en el arco norte de la sala)
            var door = Instantiate(EnvPath("Door"), new Vector3(0f, 0f, endZ), 0f, roomGo.transform);
            EditorUtil.Set(room, "exitDoor", door.GetComponent<DoorView>());
            EditorUtil.Set(room, "exitDoorCollider", door.GetComponent<Collider>());

            // Zona de salida: justo después de la puerta (en la sala siguiente o en el pasillo final)
            var triggerGo = new GameObject("ExitTrigger");
            triggerGo.transform.SetParent(roomGo.transform, false);
            float triggerWidth = r < 2 ? RoomWidths[r + 1] * Tile - 1f : Tile - 1f;
            triggerGo.transform.position = new Vector3(0f, 1.5f, endZ + (r < 2 ? 1.6f : 2.2f));
            var box = triggerGo.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(triggerWidth, 3f, 1.2f);
            EditorUtil.Set(room, "exitTrigger", triggerGo.AddComponent<RoomExitTrigger>());

            return room;
        }

        // ------------------------------------------------------------------
        // HUD
        // ------------------------------------------------------------------
        private static void CreateHud(out HUDView hud, out BossHealthBarView bossBar)
        {
            Canvas canvas = EditorUtil.CreateScreenCanvas("HUD");
            Transform root = canvas.transform;
            var topLeft = new Vector2(0f, 1f);
            var white = Color.white;

            EditorUtil.CreateText("HealthLabel", root, "VIDA", 22, TextAnchor.LowerLeft, new Color(1f, 0.85f, 0.6f),
                topLeft, topLeft, new Vector2(32f, -24f), new Vector2(200f, 28f), style: FontStyle.Bold);
            RectTransform healthFill = EditorUtil.CreateBar("HealthBar", root, topLeft, topLeft, new Vector2(30f, -56f),
                new Vector2(420f, 34f), new Color(0f, 0f, 0f, 0.7f), new Color(0.25f, 0.8f, 0.3f), out Image healthImage);
            Text healthText = EditorUtil.CreateText("HealthText", healthFill.parent, "100 / 100", 22, TextAnchor.MiddleCenter, white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 34f), style: FontStyle.Bold);
            Text damageText = EditorUtil.CreateText("DamageText", root, "Daño: 10", 24, TextAnchor.UpperLeft, new Color(1f, 0.8f, 0.4f),
                topLeft, topLeft, new Vector2(32f, -100f), new Vector2(420f, 32f), style: FontStyle.Bold);
            Text itemsText = EditorUtil.CreateText("ItemsText", root, "Ítems: —", 22, TextAnchor.UpperLeft, white,
                topLeft, topLeft, new Vector2(32f, -138f), new Vector2(420f, 220f));

            var topRight = new Vector2(1f, 1f);
            Text roomText = EditorUtil.CreateText("RoomText", root, "Nivel 1", 26, TextAnchor.UpperRight, white,
                topRight, topRight, new Vector2(-32f, -28f), new Vector2(620f, 80f), style: FontStyle.Bold);

            var center = new Vector2(0.5f, 0.5f);
            Text toastText = EditorUtil.CreateText("ToastText", root, "", 42, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.35f),
                center, center, new Vector2(0f, 230f), new Vector2(1400f, 130f), style: FontStyle.Bold);

            var bottom = new Vector2(0.5f, 0f);
            EditorUtil.CreateText("ControlsText", root,
                "WASD / Flechas: mover   ·   Mouse: apuntar   ·   Clic / Espacio: atacar   ·   Shift: esquivar   ·   E: abrir cofre",
                22, TextAnchor.MiddleCenter, new Color(0.85f, 0.85f, 0.85f, 0.85f), bottom, bottom, new Vector2(0f, 26f), new Vector2(1700f, 40f));

            // Barra del jefe (oculta hasta entrar a su sala)
            var top = new Vector2(0.5f, 1f);
            var bossPanel = EditorUtil.CreateRect("BossPanel", root, top, top, new Vector2(0f, -110f), new Vector2(820f, 80f));
            Text bossName = EditorUtil.CreateText("BossName", bossPanel, "Jefe", 28, TextAnchor.LowerCenter, new Color(1f, 0.75f, 0.6f),
                top, top, Vector2.zero, new Vector2(820f, 36f), style: FontStyle.Bold);
            RectTransform bossFill = EditorUtil.CreateBar("BossBar", bossPanel, top, top, new Vector2(0f, -40f),
                new Vector2(800f, 30f), new Color(0f, 0f, 0f, 0.75f), new Color(0.8f, 0.15f, 0.15f), out Image bossImage);

            hud = canvas.gameObject.AddComponent<HUDView>();
            EditorUtil.Set(hud, "healthFill", healthFill);
            EditorUtil.Set(hud, "healthFillImage", healthImage);
            EditorUtil.Set(hud, "healthText", healthText);
            EditorUtil.Set(hud, "damageText", damageText);
            EditorUtil.Set(hud, "itemsText", itemsText);
            EditorUtil.Set(hud, "roomText", roomText);
            EditorUtil.Set(hud, "toastText", toastText);

            bossBar = canvas.gameObject.AddComponent<BossHealthBarView>();
            EditorUtil.Set(bossBar, "panel", bossPanel.gameObject);
            EditorUtil.Set(bossBar, "nameText", bossName);
            EditorUtil.Set(bossBar, "fill", bossFill);
            EditorUtil.Set(bossBar, "fillImage", bossImage);
        }

        // ==================================================================
        // Menú principal y GameOver
        // ==================================================================
        public static void BuildMainMenu(PlayerConfig playerConfig)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildMenuBackdrop(new Color(0.04f, 0.03f, 0.05f));

            Canvas canvas = EditorUtil.CreateScreenCanvas("MenuCanvas");
            Transform root = canvas.transform;
            var center = new Vector2(0.5f, 0.5f);
            var dim = EditorUtil.CreateImage("Dim", root, new Color(0f, 0f, 0f, 0.35f), center, center, Vector2.zero, Vector2.zero);
            EditorUtil.Stretch(dim.rectTransform);

            var title = EditorUtil.CreateText("Title", root, "DUNGEON DEPTHS", 110, TextAnchor.MiddleCenter, new Color(1f, 0.8f, 0.3f),
                center, center, new Vector2(0f, 300f), new Vector2(1600f, 160f), style: FontStyle.Bold);
            title.gameObject.AddComponent<Outline>().effectColor = new Color(0.3f, 0.1f, 0f);
            EditorUtil.CreateText("Subtitle", root, "Tres niveles. Tres jefes. ¿Podrás escapar de la mazmorra?", 34, TextAnchor.MiddleCenter,
                new Color(0.95f, 0.9f, 0.85f), center, center, new Vector2(0f, 190f), new Vector2(1500f, 60f));

            Button play = EditorUtil.CreateButton("PlayButton", root, "JUGAR", center, new Vector2(0f, 30f), new Vector2(380f, 90f));
            Button quit = EditorUtil.CreateButton("QuitButton", root, "SALIR", center, new Vector2(0f, -90f), new Vector2(380f, 90f));

            EditorUtil.CreateText("Controls", root,
                "CONTROLES\nWASD / Flechas: mover   ·   Mouse: apuntar   ·   Clic izquierdo / Espacio: atacar\nShift: esquivar (eres invulnerable un instante)   ·   E: abrir cofres",
                26, TextAnchor.MiddleCenter, new Color(0.85f, 0.85f, 0.85f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 60f), new Vector2(1600f, 130f));

            var view = canvas.gameObject.AddComponent<MainMenuView>();
            EditorUtil.Set(view, "playButton", play);
            EditorUtil.Set(view, "quitButton", quit);

            var controller = new GameObject("MenuController").AddComponent<MenuController>();
            EditorUtil.Set(controller, "mainMenu", view);
            EditorUtil.Set(controller, "playerConfig", playerConfig);

            CreateEventSystem(play.gameObject);
            EditorUtil.EnsureFolder(ScenesFolder);
            EditorSceneManager.SaveScene(scene, ScenePath(SceneNames.MainMenu));
        }

        public static void BuildGameOver(PlayerConfig playerConfig)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.03f, 0.04f);
            camGo.AddComponent<AudioListener>();

            Canvas canvas = EditorUtil.CreateScreenCanvas("GameOverCanvas");
            Transform root = canvas.transform;
            var center = new Vector2(0.5f, 0.5f);
            Text title = EditorUtil.CreateText("Title", root, "DERROTA", 120, TextAnchor.MiddleCenter, Color.white,
                center, center, new Vector2(0f, 220f), new Vector2(1400f, 170f), style: FontStyle.Bold);
            title.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            Text message = EditorUtil.CreateText("Message", root, "", 36, TextAnchor.MiddleCenter, new Color(0.95f, 0.9f, 0.85f),
                center, center, new Vector2(0f, 90f), new Vector2(1400f, 80f));
            Button retry = EditorUtil.CreateButton("RetryButton", root, "JUGAR DE NUEVO", center, new Vector2(0f, -60f), new Vector2(460f, 90f));
            Button menu = EditorUtil.CreateButton("MenuButton", root, "MENÚ PRINCIPAL", center, new Vector2(0f, -180f), new Vector2(460f, 90f));

            var view = canvas.gameObject.AddComponent<GameOverView>();
            EditorUtil.Set(view, "titleText", title);
            EditorUtil.Set(view, "messageText", message);
            EditorUtil.Set(view, "retryButton", retry);
            EditorUtil.Set(view, "menuButton", menu);

            var controller = new GameObject("MenuController").AddComponent<MenuController>();
            EditorUtil.Set(controller, "gameOver", view);
            EditorUtil.Set(controller, "playerConfig", playerConfig);

            CreateEventSystem(retry.gameObject);
            EditorUtil.EnsureFolder(ScenesFolder);
            EditorSceneManager.SaveScene(scene, ScenePath(SceneNames.GameOver));
        }

        /// <summary>Fondo 3D del menú: el caballero frente a los tres jefes, iluminados por antorchas.</summary>
        private static void BuildMenuBackdrop(Color background)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.2f, 0.17f, 0.2f);
            RenderSettings.fog = false;

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.fieldOfView = 40f;
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 3.2f, -9f);
            camGo.transform.rotation = Quaternion.Euler(12f, 0f, 0f);

            var set = new GameObject("Backdrop").transform;
            for (int i = -2; i <= 2; i++)
                for (int j = 0; j < 3; j++)
                    Instantiate(EnvPath("Room_Floor"), new Vector3(i * Tile, 0f, j * Tile - 2f), 0f, set);
            for (int i = -2; i <= 2; i++)
                Instantiate(EnvPath(i == 0 ? "Room_Arch" : "Room_Wall"), new Vector3(i * Tile, 0f, 10f), 0f, set);

            PlaceModel("Boss_Ogre", new Vector3(-5f, 0f, 6f), 160f, set);
            PlaceModel("Boss_Demon", new Vector3(0f, 0f, 7f), 180f, set);
            PlaceModel("Boss_Golem", new Vector3(5f, 0f, 6f), 200f, set);
            PlaceModel("Player", new Vector3(0f, 0f, 0.5f), 200f, set);
            PlaceModel("Chest", new Vector3(-2.5f, 0f, 1.5f), 150f, set);

            EditorUtil.CreatePointLight("TorchLeft", set, new Vector3(-4f, 2.5f, 3f), new Color(1f, 0.6f, 0.3f), 3f, 12f);
            EditorUtil.CreatePointLight("TorchRight", set, new Vector3(4f, 2.5f, 3f), new Color(1f, 0.6f, 0.3f), 3f, 12f);
            EditorUtil.CreatePointLight("BossGlow", set, new Vector3(0f, 4f, 5f), new Color(1f, 0.3f, 0.2f), 2f, 10f);
        }

        private static void PlaceModel(string modelName, Vector3 position, float yRotation, Transform parent)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabBuilder.ModelsFolder}/{modelName}.fbx");
            if (model == null) return;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yRotation, 0f));
        }

        private static void CreateEventSystem(GameObject firstSelected)
        {
            var go = new GameObject("EventSystem");
            var eventSystem = go.AddComponent<EventSystem>();
            eventSystem.firstSelectedGameObject = firstSelected;
            // Sin asignar acciones a mano: al activarse, el módulo usa las acciones de UI por defecto del Input System.
            go.AddComponent<InputSystemUIInputModule>();
        }

        // ==================================================================
        // Utilidades
        // ==================================================================
        private static GameObject Instantiate(string prefabPath, Vector3 position, float yRotation, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[SceneBuilder] Falta el prefab {prefabPath}. Ejecuta Dungeon > Build Everything.");
                return new GameObject("MISSING_" + System.IO.Path.GetFileNameWithoutExtension(prefabPath));
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yRotation, 0f));
            return go;
        }

        public static void RegisterBuildScenes()
        {
            var names = new List<string> { SceneNames.MainMenu };
            for (int i = 1; i <= SceneNames.LevelCount; i++) names.Add(SceneNames.Level(i));
            names.Add(SceneNames.GameOver);

            var scenes = new List<EditorBuildSettingsScene>();
            foreach (var name in names) scenes.Add(new EditorBuildSettingsScene(ScenePath(name), true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
