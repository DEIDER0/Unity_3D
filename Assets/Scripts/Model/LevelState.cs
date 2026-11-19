using System;

namespace Dungeon.Model
{
    /// <summary>
    /// MODELO. Estado de un nivel: número de nivel, sala actual y si el jefe fue derrotado.
    /// Aquí viven las reglas de avance de sala. La puerta de una sala se desbloquea cuando:
    ///   - quedan 0 enemigos básicos vivos en la sala, Y
    ///   - en la sala del cofre, el cofre fue abierto; en la sala del jefe, el jefe fue derrotado.
    /// </summary>
    public sealed class LevelState
    {
        public const int RoomCount = 3;

        /// <summary>Índice (0..2) de la nueva sala actual.</summary>
        public event Action<int> RoomChanged;
        /// <summary>Índice de la sala cuya puerta se acaba de desbloquear.</summary>
        public event Action<int> DoorUnlocked;
        public event Action BossDefeated;
        /// <summary>Número del nivel completado (el jugador cruzó la puerta de la sala del jefe).</summary>
        public event Action<int> LevelCompleted;

        private readonly int[] _enemiesPerRoom;
        private int _enemiesRemaining;
        private bool _chestOpened;

        public int LevelNumber { get; }
        public int CurrentRoomIndex { get; private set; }
        public RoomType CurrentRoomType => (RoomType)CurrentRoomIndex;
        public bool IsBossDefeated { get; private set; }
        public bool IsDoorUnlocked { get; private set; }
        public bool IsCompleted { get; private set; }
        public int EnemiesRemaining => _enemiesRemaining;

        /// <param name="levelNumber">Número de nivel (1, 2 o 3).</param>
        /// <param name="enemiesPerRoom">Enemigos básicos por sala (3 valores). Null = ninguno.</param>
        public LevelState(int levelNumber, int[] enemiesPerRoom = null)
        {
            if (levelNumber <= 0) throw new ArgumentOutOfRangeException(nameof(levelNumber));
            enemiesPerRoom ??= new int[RoomCount];
            if (enemiesPerRoom.Length != RoomCount)
                throw new ArgumentException($"Se necesitan exactamente {RoomCount} valores.", nameof(enemiesPerRoom));
            foreach (int count in enemiesPerRoom)
                if (count < 0) throw new ArgumentOutOfRangeException(nameof(enemiesPerRoom), "No puede haber enemigos negativos.");

            LevelNumber = levelNumber;
            _enemiesPerRoom = (int[])enemiesPerRoom.Clone();
            EnterRoom(0);
        }

        public static RoomType GetRoomType(int roomIndex) => (RoomType)roomIndex;

        public int GetEnemyCount(int roomIndex) => _enemiesPerRoom[roomIndex];

        /// <summary>Un enemigo básico de la sala actual murió.</summary>
        public void NotifyEnemyDefeated()
        {
            if (_enemiesRemaining == 0) return;
            _enemiesRemaining--;
            EvaluateDoor();
        }

        /// <summary>Se abrió el cofre (solo cuenta en la sala del cofre).</summary>
        public void NotifyChestOpened()
        {
            if (CurrentRoomType != RoomType.Chest) return;
            _chestOpened = true;
            EvaluateDoor();
        }

        /// <summary>Murió el jefe (solo cuenta en la sala del jefe).</summary>
        public void NotifyBossDefeated()
        {
            if (CurrentRoomType != RoomType.Boss || IsBossDefeated) return;
            IsBossDefeated = true;
            BossDefeated?.Invoke();
            EvaluateDoor();
        }

        /// <summary>
        /// El jugador cruza la puerta de la sala actual. Solo funciona si está desbloqueada.
        /// En la última sala, completa el nivel. Devuelve true si se avanzó.
        /// </summary>
        public bool TryAdvance()
        {
            if (!IsDoorUnlocked || IsCompleted) return false;

            if (CurrentRoomIndex == RoomCount - 1)
            {
                IsCompleted = true;
                LevelCompleted?.Invoke(LevelNumber);
                return true;
            }

            EnterRoom(CurrentRoomIndex + 1);
            return true;
        }

        private void EnterRoom(int index)
        {
            CurrentRoomIndex = index;
            _enemiesRemaining = _enemiesPerRoom[index];
            _chestOpened = false;
            IsDoorUnlocked = false;
            RoomChanged?.Invoke(index);
            EvaluateDoor();
        }

        private void EvaluateDoor()
        {
            if (IsDoorUnlocked) return;

            bool roomGoalMet;
            switch (CurrentRoomType)
            {
                case RoomType.Chest: roomGoalMet = _chestOpened; break;
                case RoomType.Boss: roomGoalMet = IsBossDefeated; break;
                default: roomGoalMet = true; break;
            }

            if (_enemiesRemaining == 0 && roomGoalMet)
            {
                IsDoorUnlocked = true;
                DoorUnlocked?.Invoke(CurrentRoomIndex);
            }
        }
    }
}
