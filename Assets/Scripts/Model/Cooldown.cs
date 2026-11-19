using System;

namespace Dungeon.Model
{
    /// <summary>
    /// MODELO. Tiempo de espera entre acciones (ataque, esquive, golpe del enemigo).
    /// No lee el reloj de Unity: el controlador le pasa el tiempo actual,
    /// así se puede probar sin escena.
    /// </summary>
    public sealed class Cooldown
    {
        private double _readyAt = double.NegativeInfinity;

        public double Duration { get; }

        public Cooldown(double durationSeconds)
        {
            if (durationSeconds < 0) throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            Duration = durationSeconds;
        }

        public bool IsReady(double now) => now >= _readyAt;

        /// <summary>Si está listo, lo activa y devuelve true; si no, devuelve false.</summary>
        public bool TryUse(double now)
        {
            if (!IsReady(now)) return false;
            _readyAt = now + Duration;
            return true;
        }

        public void Reset() => _readyAt = double.NegativeInfinity;
    }
}
