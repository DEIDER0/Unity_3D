using System;

namespace Dungeon.Model
{
    /// <summary>
    /// MODELO. Estado de un enemigo básico: vida y daño.
    /// BossState hereda de esta clase para reutilizar las mismas reglas.
    /// </summary>
    public class EnemyState
    {
        /// <summary>(vidaActual, vidaMaxima)</summary>
        public event Action<int, int> HealthChanged;
        public event Action Died;

        private readonly Health _health;
        private readonly int _baseDamage;

        public string Name { get; }
        public int CurrentHealth => _health.Current;
        public int MaxHealth => _health.Max;
        public float HealthNormalized => _health.Normalized;
        public bool IsDead => _health.IsDead;

        /// <summary>Daño que hace al jugador. El jefe lo modifica en su fase de furia.</summary>
        public virtual int Damage => _baseDamage;
        protected int BaseDamage => _baseDamage;

        public EnemyState(string name, int maxHealth, int damage)
        {
            if (damage <= 0) throw new ArgumentOutOfRangeException(nameof(damage), "El daño debe ser mayor que 0.");
            Name = string.IsNullOrWhiteSpace(name) ? "Enemy" : name;
            _health = new Health(maxHealth);
            _baseDamage = damage;

            _health.Changed += OnHealthChanged;
            _health.Died += () => Died?.Invoke();
        }

        /// <summary>Recibe daño. Devuelve el daño realmente aplicado.</summary>
        public int TakeDamage(int amount) => _health.TakeDamage(amount);

        /// <summary>Punto de extensión para las subclases (el jefe revisa aquí su fase).</summary>
        protected virtual void OnHealthChanged(int current, int max)
        {
            HealthChanged?.Invoke(current, max);
        }
    }
}
