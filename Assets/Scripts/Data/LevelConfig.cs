using UnityEngine;

namespace Dungeon.Data
{
    /// <summary>
    /// DATOS. Todo lo que cambia entre los niveles 1, 2 y 3. El código y las escenas
    /// son los mismos; solo cambian estos datos.
    /// </summary>
    [CreateAssetMenu(menuName = "Dungeon/Level Config", fileName = "LevelConfig")]
    public class LevelConfig : ScriptableObject
    {
        [Min(1)] public int levelNumber = 1;
        public string displayName = "Las Catacumbas";

        [Header("Enemigos")]
        public EnemyConfig enemy;
        [Tooltip("Enemigos básicos por sala: [sala vacía, sala del cofre, sala del jefe].")]
        public int[] enemiesPerRoom = { 2, 0, 0 };

        [Header("Recompensa del cofre")]
        public ItemData chestItem;

        [Header("Jefe")]
        public BossConfig boss;
        [Tooltip("Prefab del jefe (modelo distinto por nivel).")]
        public GameObject bossPrefab;

        [Header("Apariencia")]
        public Color torchColor = new Color(1f, 0.62f, 0.3f);
        public Color ambientColor = new Color(0.16f, 0.15f, 0.2f);
        public Color fogColor = new Color(0.05f, 0.04f, 0.06f);

        /// <summary>Carga Assets/Resources/Data/LevelN/LevelN.asset (respaldo si falta la referencia).</summary>
        public static LevelConfig LoadFromResources(int levelNumber) =>
            Resources.Load<LevelConfig>($"Data/Level{levelNumber}/Level{levelNumber}");
    }
}
