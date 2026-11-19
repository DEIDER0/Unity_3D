using System;

namespace Dungeon.Model
{
    /// <summary>
    /// MODELO. Jefe: un enemigo con más vida, un ataque especial (golpe de área) y una fase de furia.
    /// Cuando su vida baja del umbral (por defecto 50 %), entra en furia una sola vez
    /// y su daño normal y especial se multiplican.
    /// </summary>
    public sealed class BossState : EnemyState
    {
        /// <summary>Se emite una sola vez al entrar en la fase de furia.</summary>
        public event Action Enraged;

        public float EnrageThreshold { get; }
        public float EnragedDamageMultiplier { get; }
        public bool IsEnraged { get; private set; }

        /// <summary>Daño del ataque especial sin furia.</summary>
        public int BaseSpecialDamage { get; }

        public override int Damage => ApplyEnrage(BaseDamage);

        /// <summary>Daño del ataque especial (golpe de área), afectado por la furia.</summary>
        public int SpecialDamage => ApplyEnrage(BaseSpecialDamage);

        /// <param name="specialDamage">Daño del golpe de área. Si es 0 o menos, se usa el doble del daño normal.</param>
        public BossState(string name, int maxHealth, int damage,
                         float enrageThreshold = 0.5f, float enragedDamageMultiplier = 1.5f,
                         int specialDamage = 0)
            : base(name, maxHealth, damage)
        {
            if (enrageThreshold <= 0f || enrageThreshold >= 1f)
                throw new ArgumentOutOfRangeException(nameof(enrageThreshold), "El umbral debe estar entre 0 y 1.");
            if (enragedDamageMultiplier < 1f)
                throw new ArgumentOutOfRangeException(nameof(enragedDamageMultiplier), "El multiplicador debe ser al menos 1.");

            EnrageThreshold = enrageThreshold;
            EnragedDamageMultiplier = enragedDamageMultiplier;
            BaseSpecialDamage = specialDamage > 0 ? specialDamage : damage * 2;
        }

        private int ApplyEnrage(int value) =>
            IsEnraged ? (int)Math.Round(value * EnragedDamageMultiplier) : value;

        protected override void OnHealthChanged(int current, int max)
        {
            base.OnHealthChanged(current, max);

            if (!IsEnraged && current > 0 && (float)current / max <= EnrageThreshold)
            {
                IsEnraged = true;
                Enraged?.Invoke();
            }
        }
    }
}
