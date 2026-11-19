using System.Collections;
using System.Collections.Generic;
using Dungeon.Data;
using Dungeon.Model;
using Dungeon.View;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dungeon.Controller
{
    /// <summary>
    /// CONTROLADOR. Punto de entrada de cada nivel: crea los modelos (LevelState), conecta
    /// controladores y vistas, y gestiona victoria, derrota y la carga del siguiente nivel.
    /// La partida (GameSession, con el PlayerState) es estática para conservar vida e ítems
    /// entre escenas. No se guarda nada en disco.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        [SerializeField] private LevelConfig levelConfig;
        [SerializeField] private PlayerConfig playerConfig;
        [SerializeField] private PlayerController player;
        [SerializeField] private RoomController[] rooms = new RoomController[0];
        [SerializeField] private HUDView hud;
        [SerializeField] private BossHealthBarView bossBar;
        [SerializeField] private CameraFollowView cameraView;
        [SerializeField] private float sceneChangeDelay = 2.5f;

        /// <summary>Partida actual (sobrevive a los cambios de escena).</summary>
        public static GameSession Session { get; private set; }

        /// <summary>Resultado de la última partida, para la escena GameOver.</summary>
        public static bool LastGameWasVictory { get; private set; }
        public static int LastLevelReached { get; private set; } = 1;

        private LevelState _level;
        private bool _changingScene;

        public static void StartNewGame(PlayerConfig config, int startLevel = 1)
        {
            config = PlayerConfig.OrDefault(config, null);
            Session = new GameSession(config.CreateState(), SceneNames.LevelCount, startLevel);
        }

        private void Start()
        {
            // Si se abre un nivel directamente desde el Editor, se crea una partida que empieza en él.
            if (Session == null || Session.IsOver || Session.CurrentLevel != levelConfig.levelNumber)
                StartNewGame(playerConfig, levelConfig.levelNumber);

            _level = new LevelState(levelConfig.levelNumber, CountEnemiesPerRoom());

            player.Init(Session.Player);
            if (cameraView != null) cameraView.SetTarget(player.transform);
            if (hud != null) hud.Bind(Session.Player, _level, levelConfig.displayName);
            foreach (var room in rooms)
                if (room != null) room.Init(_level, levelConfig, player, bossBar, cameraView);

            _level.LevelCompleted += OnLevelCompleted;
            Session.LevelAdvanced += OnLevelAdvanced;
            Session.Victory += OnVictory;
            Session.Defeat += OnDefeat;

            if (hud != null) hud.ShowToast($"Nivel {levelConfig.levelNumber}: {levelConfig.displayName}");
        }

        private void OnDestroy()
        {
            // La sesión vive más que esta escena: hay que desuscribirse sí o sí.
            if (_level != null) _level.LevelCompleted -= OnLevelCompleted;
            if (Session != null)
            {
                Session.LevelAdvanced -= OnLevelAdvanced;
                Session.Victory -= OnVictory;
                Session.Defeat -= OnDefeat;
            }
        }

        private int[] CountEnemiesPerRoom()
        {
            var counts = new int[LevelState.RoomCount];
            foreach (var room in rooms)
                if (room != null && room.RoomIndex >= 0 && room.RoomIndex < counts.Length)
                    counts[room.RoomIndex] += room.EnemyCount;
            return counts;
        }

        private void OnLevelCompleted(int levelNumber) => Session.CompleteLevel(levelNumber);

        private void OnLevelAdvanced(int nextLevel)
        {
            player.SetInputEnabled(false);
            if (hud != null) hud.ShowToast("¡Nivel completado!");
            LoadSceneLater(SceneNames.Level(nextLevel));
        }

        private void OnVictory()
        {
            LastGameWasVictory = true;
            LastLevelReached = Session.CurrentLevel;
            player.SetInputEnabled(false);
            if (hud != null) hud.ShowToast("¡Escapaste de la mazmorra!");
            LoadSceneLater(SceneNames.GameOver);
        }

        private void OnDefeat()
        {
            LastGameWasVictory = false;
            LastLevelReached = Session.CurrentLevel;
            if (hud != null) hud.ShowToast("Has caído...");
            LoadSceneLater(SceneNames.GameOver);
        }

        private void LoadSceneLater(string sceneName)
        {
            if (_changingScene) return;
            _changingScene = true;
            StartCoroutine(LoadRoutine(sceneName));
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            yield return new WaitForSeconds(sceneChangeDelay);
            SceneManager.LoadScene(sceneName);
        }
    }
}
