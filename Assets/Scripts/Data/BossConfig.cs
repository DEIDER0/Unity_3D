using Dungeon.Model;
using UnityEngine;

namespace Dungeon.Data
{
    /// <summary>DATOS. Valores de un jefe (uno por nivel).</summary>
    [CreateAssetMenu(menuName = "Dungeon/Boss Config", fileName = "BossConfig")]
    public class BossConfig : ScriptableObject
    {
        public string displayName = "Jefe";

        [Header("Estadísticas")]
        [Min(1)] public int maxHealth = 220;
        [Min(1)] public int damage = 14;
        [Min(1)] public int specialDamage = 22;

        [Header("Furia")]
        [Range(0.05f, 0.95f)] public float enrageThreshold = 0.5f;
        [Min(1f)] public float enragedDamageMultiplier = 1.5f;
        [Tooltip("Multiplicador de velocidad y de ritmo de ataque en furia.")]
        [Min(1f)] public float enragedSpeedMultiplier = 1.35f;

        [Header("Ataque normal")]
        [Min(0.1f)] public float moveSpeed = 2.4f;
        [Min(0.5f)] public float attackRange = 2.6f;
        [Min(0.05f)] public float attackWindup = 0.7f;
        [Min(0.1f)] public float attackCooldown = 1.6f;

        [Header("Golpe de área (se anuncia con un círculo rojo en el piso)")]
        [Min(0.5f)] public float slamRadius = 4f;
        [Min(0.2f)] public float slamWindup = 1.2f;
        [Min(1f)] public float slamCooldown = 6f;

        public BossState CreateState() =>
            new BossState(displayName, maxHealth, damage, enrageThreshold, enragedDamageMultiplier, specialDamage);
    }
}
