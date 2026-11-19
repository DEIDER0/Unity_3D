using System;

namespace Dungeon.Model
{
    /// <summary>
    /// MODELO. Partida en curso: nivel actual, el jugador (que conserva vida e ítems entre niveles),
    /// y las reglas de victoria y derrota.
    ///   - Victoria: completar el último nivel (vencer al jefe del nivel 3 y cruzar su puerta).
    ///   - Derrota: la vida del jugador llega a 0.
    /// No guarda nada en disco (el guardado de partida está fuera de alcance).
    /// </summary>
    public sealed class GameSession
    {
        public const int DefaultTotalLevels = 3;

        /// <summary>Número del nuevo nivel al que se debe pasar.</summary>
        public event Action<int> LevelAdvanced;
        public event Action Victory;
        public event Action Defeat;

        public PlayerState Player { get; }
        public int TotalLevels { get; }
        public int CurrentLevel { get; private set; } = 1;
        public bool IsOver { get; private set; }
        public bool IsVictory { get; private set; }

        /// <param name="startLevel">Nivel inicial (normalmente 1; otro valor sirve para probar un nivel suelto).</param>
        public GameSession(PlayerState player, int totalLevels = DefaultTotalLevels, int startLevel = 1)
        {
            Player = player ?? throw new ArgumentNullException(nameof(player));
            if (totalLevels <= 0) throw new ArgumentOutOfRangeException(nameof(totalLevels));
            if (startLevel < 1 || startLevel > totalLevels) throw new ArgumentOutOfRangeException(nameof(startLevel));
            TotalLevels = totalLevels;
            CurrentLevel = startLevel;
            Player.Died += OnPlayerDied;
        }

        /// <summary>Crea el estado del nivel actual a partir de sus enemigos por sala.</summary>
        public LevelState CreateCurrentLevel(int[] enemiesPerRoom) => new LevelState(CurrentLevel, enemiesPerRoom);

        /// <summary>Lo llama el controlador cuando LevelState emite LevelCompleted.</summary>
        public void CompleteLevel(int levelNumber)
        {
            if (IsOver || levelNumber != CurrentLevel) return;

            if (CurrentLevel >= TotalLevels)
            {
                IsOver = true;
                IsVictory = true;
                Victory?.Invoke();
                return;
            }

            CurrentLevel++;
            LevelAdvanced?.Invoke(CurrentLevel);
        }

        private void OnPlayerDied()
        {
            if (IsOver) return;
            IsOver = true;
            IsVictory = false;
            Defeat?.Invoke();
        }
    }
}
