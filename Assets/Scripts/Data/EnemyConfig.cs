using Dungeon.Model;
using UnityEngine;

namespace Dungeon.Data
{
    /// <summary>DATOS. Valores de un tipo de enemigo básico (uno por nivel).</summary>
    [CreateAssetMenu(menuName = "Dungeon/Enemy Config", fileName = "EnemyConfig")]
    public class EnemyConfig : ScriptableObject
    {
        public string displayName = "Goblin";

        [Header("Estadísticas")]
        [Min(1)] public int maxHealth = 30;
        [Min(1)] public int damage = 8;

        [Header("IA")]
        [Min(0.1f)] public float moveSpeed = 3.2f;
        [Tooltip("Distancia a la que detecta al jugador y empieza a perseguirlo.")]
        [Min(1f)] public float detectionRange = 9f;
        [Min(0.5f)] public float attackRange = 1.5f;
        [Tooltip("Tiempo que levanta el arma antes de golpear (permite esquivar).")]
        [Min(0.05f)] public float attackWindup = 0.5f;
        [Min(0.1f)] public float attackCooldown = 1.3f;

        public EnemyState CreateState() => new EnemyState(displayName, maxHealth, damage);
    }
}
