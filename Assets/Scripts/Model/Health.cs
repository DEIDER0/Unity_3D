using System;

namespace Dungeon.Model
{
    /// <summary>
    /// MODELO. Vida reutilizable por el jugador, los enemigos y el jefe.
    /// Contiene las reglas básicas: la vida nunca baja de 0 ni supera el máximo,
    /// y una entidad muerta no recibe daño ni se cura.
    /// </summary>
    public sealed class Health
    {
        /// <summary>Se emite con (vidaActual, vidaMaxima) cada vez que cambia algún valor.</summary>
        public event Action<int, int> Changed;

        /// <summary>Se emite una sola vez, cuando la vida llega a 0.</summary>
        public event Action Died;

        public int Current { get; private set; }
        public int Max { get; private set; }
        public bool IsDead => Current <= 0;

        /// <summary>Porcentaje de vida entre 0 y 1 (útil para las barras de vida).</summary>
        public float Normalized => (float)Current / Max;

        public Health(int max)
        {
            if (max <= 0) throw new ArgumentOutOfRangeException(nameof(max), "La vida máxima debe ser mayor que 0.");
            Max = max;
            Current = max;
        }

        /// <summary>Resta vida. Devuelve el daño realmente aplicado.</summary>
        public int TakeDamage(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "El daño no puede ser negativo.");
            if (IsDead || amount == 0) return 0;

            int applied = Math.Min(amount, Current);
            Current -= applied;
            Changed?.Invoke(Current, Max);

            if (IsDead) Died?.Invoke();
            return applied;
        }

        /// <summary>Suma vida sin superar el máximo. Devuelve la curación realmente aplicada.</summary>
        public int Heal(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "La curación no puede ser negativa.");
            if (IsDead || amount == 0) return 0;

            int applied = Math.Min(amount, Max - Current);
            if (applied == 0) return 0;

            Current += applied;
            Changed?.Invoke(Current, Max);
            return applied;
        }

        /// <summary>Aumenta la vida máxima y cura esa misma cantidad (mejora de vida).</summary>
        public void IncreaseMax(int amount)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "El aumento debe ser mayor que 0.");
            if (IsDead) return;

            Max += amount;
            Current += amount;
            Changed?.Invoke(Current, Max);
        }
    }
}
