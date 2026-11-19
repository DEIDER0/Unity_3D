using Dungeon.Model;
using UnityEngine;

namespace Dungeon.Data
{
    /// <summary>
    /// DATOS. Valores del jugador editables desde el Inspector. No contiene reglas:
    /// solo crea el PlayerState del Modelo con estos números.
    /// </summary>
    [CreateAssetMenu(menuName = "Dungeon/Player Config", fileName = "PlayerConfig")]
    public class PlayerConfig : ScriptableObject
    {
        [Header("Estadísticas")]
        [Min(1)] public int maxHealth = 100;
        [Min(1)] public int baseDamage = 10;

        [Header("Movimiento")]
        [Min(0.1f)] public float moveSpeed = 6f;
        [Min(1f)] public float turnSpeed = 900f;

        [Header("Ataque")]
        [Min(0.05f)] public float attackCooldown = 0.4f;
        [Tooltip("Tiempo entre pulsar atacar y que el golpe conecte (sincronizado con la animación).")]
        [Min(0f)] public float attackHitDelay = 0.12f;
        [Tooltip("Distancia al frente del centro del golpe.")]
        [Min(0.1f)] public float attackRange = 1.3f;
        [Min(0.1f)] public float attackRadius = 1.2f;

        [Header("Esquive")]
        [Min(0.1f)] public float dodgeSpeed = 14f;
        [Tooltip("Duración del esquive; el jugador es invulnerable durante este tiempo.")]
        [Min(0.05f)] public float dodgeDuration = 0.3f;
        [Min(0.1f)] public float dodgeCooldown = 0.8f;

        public PlayerState CreateState() => new PlayerState(maxHealth, baseDamage);

        /// <summary>
        /// Devuelve 'config' o, si falta la referencia (no se asignó en el Inspector),
        /// una configuración con los valores por defecto, y lo avisa en la consola.
        /// </summary>
        public static PlayerConfig OrDefault(PlayerConfig config, Object context)
        {
            if (config != null) return config;
            Debug.LogWarning("[Dungeon] Falta asignar PlayerConfig; se usan los valores por defecto. " +
                             "Asigna Assets/Data/PlayerConfig.asset en el Inspector o ejecuta Dungeon > Build Everything.", context);
            if (_fallback == null) _fallback = CreateInstance<PlayerConfig>();
            return _fallback;
        }

        private static PlayerConfig _fallback;
    }
}
