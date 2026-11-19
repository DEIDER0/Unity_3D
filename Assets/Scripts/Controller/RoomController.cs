using System.Collections.Generic;
using Dungeon.Data;
using Dungeon.Model;
using Dungeon.View;
using UnityEngine;

namespace Dungeon.Controller
{
    /// <summary>
    /// CONTROLADOR. Coordina una sala: crea los modelos de sus enemigos, jefe y cofre,
    /// avisa a LevelState cuando mueren o se abre el cofre, activa la sala cuando el jugador entra
    /// y abre la puerta cuando el Modelo la desbloquea (evento DoorUnlocked).
    /// </summary>
    public class RoomController : MonoBehaviour
    {
        [Tooltip("0 = sala vacía, 1 = sala del cofre, 2 = sala del jefe.")]
        [SerializeField] private int roomIndex;
        [SerializeField] private DoorView exitDoor;
        [SerializeField] private Collider exitDoorCollider;
        [SerializeField] private RoomExitTrigger exitTrigger;
        [SerializeField] private EnemyController[] enemies = new EnemyController[0];
        [SerializeField] private BossController boss;
        [SerializeField] private ChestController chest;

        public int RoomIndex => roomIndex;
        public int EnemyCount
        {
            get
            {
                int count = 0;
                foreach (var e in enemies) if (e != null) count++;
                return count;
            }
        }

        private readonly List<EnemyState> _enemyStates = new List<EnemyState>();
        private LevelState _level;
        private PlayerController _player;
        private BossHealthBarView _bossBar;
        private BossState _bossState;
        private ChestModel _chestModel;
        private bool _activated;
        private int _pendingDeaths; // enemigos que murieron antes de que el jugador entrara a la sala

        public void Init(LevelState level, LevelConfig config, PlayerController player,
                         BossHealthBarView bossBar, CameraFollowView cameraView)
        {
            _level = level;
            _player = player;
            _bossBar = bossBar;

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                EnemyState state = config.enemy.CreateState();
                enemy.Init(state, config.enemy);
                state.Died += OnEnemyDied;
                _enemyStates.Add(state);
            }

            if (boss != null)
            {
                _bossState = config.boss.CreateState();
                boss.Init(_bossState, config.boss, cameraView);
                _bossState.Died += OnBossDied;
                if (_bossBar != null) _bossBar.Bind(_bossState);
            }

            if (chest != null && config.chestItem != null)
            {
                _chestModel = new ChestModel(config.chestItem.CreateItem());
                chest.Init(_chestModel);
                _chestModel.Opened += OnChestOpened;
            }

            if (exitTrigger != null) exitTrigger.PlayerEntered += OnExitEntered;
            _level.RoomChanged += OnRoomChanged;
            _level.DoorUnlocked += OnDoorUnlocked;

            // El Modelo pudo emitir eventos antes de que nos suscribiéramos (p. ej. sala sin enemigos).
            if (_level.CurrentRoomIndex == roomIndex)
            {
                ActivateRoom();
                if (_level.IsDoorUnlocked) OpenDoor();
            }
        }

        private void OnDestroy()
        {
            foreach (var state in _enemyStates) state.Died -= OnEnemyDied;
            if (_bossState != null) _bossState.Died -= OnBossDied;
            if (_chestModel != null) _chestModel.Opened -= OnChestOpened;
            if (exitTrigger != null) exitTrigger.PlayerEntered -= OnExitEntered;
            if (_level != null)
            {
                _level.RoomChanged -= OnRoomChanged;
                _level.DoorUnlocked -= OnDoorUnlocked;
            }
        }

        private void OnEnemyDied()
        {
            // LevelState solo cuenta los enemigos de la sala actual; si murió antes, se avisa al entrar.
            if (_level.CurrentRoomIndex == roomIndex) _level.NotifyEnemyDefeated();
            else _pendingDeaths++;
        }

        private void OnChestOpened(Item item) => _level.NotifyChestOpened();

        private void OnBossDied() => _level.NotifyBossDefeated(); // RF3: el Modelo desbloquea la puerta

        private void OnRoomChanged(int index)
        {
            if (index == roomIndex) ActivateRoom();
        }

        private void OnDoorUnlocked(int index)
        {
            if (index == roomIndex) OpenDoor();
        }

        private void OnExitEntered()
        {
            if (_level.CurrentRoomIndex == roomIndex) _level.TryAdvance();
        }

        private void ActivateRoom()
        {
            if (_activated) return;
            _activated = true;
            for (; _pendingDeaths > 0; _pendingDeaths--) _level.NotifyEnemyDefeated();
            foreach (var enemy in enemies)
                if (enemy != null) enemy.Activate(_player);
            if (boss != null)
            {
                boss.Activate(_player);
                if (_bossBar != null) _bossBar.Show();
            }
        }

        private void OpenDoor()
        {
            if (exitDoorCollider != null) exitDoorCollider.enabled = false;
            if (exitDoor != null) exitDoor.Open();
        }
    }
}
