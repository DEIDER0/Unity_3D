using UnityEngine;

namespace Dungeon.Controller
{
    /// <summary>
    /// CONTROLADOR (contrato). Algo que puede recibir golpes: jugador, enemigos y jefe.
    /// Así el atacante no necesita conocer la clase concreta del objetivo (evita dependencias circulares).
    /// </summary>
    public interface IDamageable
    {
        bool IsAlive { get; }
        Vector3 Position { get; }

        /// <summary>Aplica daño (lo decide el Modelo). Devuelve el daño realmente aplicado.</summary>
        int ReceiveDamage(int amount, Vector3 sourcePosition);
    }
}
