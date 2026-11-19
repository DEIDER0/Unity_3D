using System;

namespace Dungeon.Model
{
    /// <summary>
    /// MODELO. Jefe: un enemigo con más vida y una fase de furia.
    /// Cuando su vida baja del umbral (por defecto 50 %), entra en furia una sola vez
    /// y su daño se multiplica.
    /// </summary>
    public sealed class BossState : EnemyState
    {
        /// <summary>Se emite una sola vez al entrar en la fase de furia.</summary>
        public event Action Enraged;

        public float EnrageThreshold { get; }
        public float EnragedDamageMultiplier { get; }
        public bool IsEnraged { get; private set; }

        public override int Damage =>
            IsEnraged ? (int)Math.Round(BaseDamage * EnragedDamageMultiplier) : BaseDamage;

        public BossState(string name, int maxHealth, int damage,
                         float enrageThreshold = 0.5f, float enragedDamageMultiplier = 1.5f)
            : base(name, maxHealth, damage)
        {
            if (enrageThreshold <= 0f || enrageThreshold >= 1f)
                throw new ArgumentOutOfRangeException(nameof(enrageThreshold), "El umbral debe estar entre 0 y 1.");
            if (enragedDamageMultiplier < 1f)
                throw new ArgumentOutOfRangeException(nameof(enragedDamageMultiplier), "El multiplicador debe ser al menos 1.");

            EnrageThreshold = enrageThreshold;
            EnragedDamageMultiplier = enragedDamageMultiplier;
        }

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
