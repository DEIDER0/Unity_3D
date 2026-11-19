using System;
using Dungeon.Data;
using Dungeon.Model;
using UnityEditor;
using UnityEngine;

namespace Dungeon.EditorTools
{
    /// <summary>
    /// EDITOR. Crea los ScriptableObjects con los datos del juego (Assets/Data).
    /// Solo los crea si NO existen: si el equipo cambia valores en el Inspector, se respetan.
    /// Los niveles 2 y 3 reutilizan todo el código; solo cambian estos datos.
    /// </summary>
    public static class DataAssetBuilder
    {
        public const string DataFolder = "Assets/Data";

        public sealed class GameData
        {
            public PlayerConfig Player;
            public LevelConfig[] Levels;
        }

        public static GameData EnsureAll()
        {
            EditorUtil.EnsureFolder(DataFolder);
            var data = new GameData
            {
                Player = LoadOrCreate<PlayerConfig>($"{DataFolder}/PlayerConfig.asset", p => { }),
                Levels = new[]
                {
                    // ---------------- NIVEL 1 ----------------
                    EnsureLevel(1, "Las Catacumbas",
                        enemy: e => { e.displayName = "Goblin"; e.maxHealth = 30; e.damage = 8; e.moveSpeed = 3.2f; e.attackWindup = 0.55f; e.attackCooldown = 1.4f; },
                        enemiesPerRoom: new[] { 2, 0, 0 },
                        item: i => { i.itemName = "Espada de acero"; i.type = ItemType.Weapon; i.value = 6; },
                        boss: b => { b.displayName = "Grok, el Ogro"; b.maxHealth = 220; b.damage = 14; b.specialDamage = 22; b.moveSpeed = 2.4f; b.attackRange = 2.6f; b.slamRadius = 4f; b.slamCooldown = 6.5f; },
                        torch: new Color(1f, 0.62f, 0.3f), ambient: new Color(0.17f, 0.15f, 0.2f), fog: new Color(0.05f, 0.04f, 0.06f)),

                    // ---------------- NIVEL 2 ----------------
                    EnsureLevel(2, "Las Cavernas de Cristal",
                        enemy: e => { e.displayName = "Goblin veterano"; e.maxHealth = 45; e.damage = 11; e.moveSpeed = 3.6f; e.attackWindup = 0.5f; e.attackCooldown = 1.25f; },
                        enemiesPerRoom: new[] { 3, 1, 0 },
                        item: i => { i.itemName = "Amuleto de vida"; i.type = ItemType.Upgrade; i.value = 40; },
                        boss: b => { b.displayName = "Gólem de Cristal"; b.maxHealth = 320; b.damage = 18; b.specialDamage = 28; b.moveSpeed = 2.2f; b.attackRange = 2.9f; b.slamRadius = 4.5f; b.slamCooldown = 6f; },
                        torch: new Color(0.45f, 0.8f, 1f), ambient: new Color(0.12f, 0.16f, 0.22f), fog: new Color(0.03f, 0.05f, 0.08f)),

                    // ---------------- NIVEL 3 ----------------
                    EnsureLevel(3, "El Abismo Ardiente",
                        enemy: e => { e.displayName = "Goblin de élite"; e.maxHealth = 60; e.damage = 14; e.moveSpeed = 4f; e.attackWindup = 0.45f; e.attackCooldown = 1.1f; },
                        enemiesPerRoom: new[] { 3, 2, 1 },
                        item: i => { i.itemName = "Elixir del héroe"; i.type = ItemType.Heal; i.value = 100; },
                        boss: b => { b.displayName = "Malakor, el Demonio"; b.maxHealth = 380; b.damage = 22; b.specialDamage = 32; b.moveSpeed = 2.8f; b.attackRange = 3f; b.slamRadius = 5f; b.slamCooldown = 5.5f; b.enragedSpeedMultiplier = 1.45f; },
                        torch: new Color(1f, 0.35f, 0.2f), ambient: new Color(0.2f, 0.1f, 0.1f), fog: new Color(0.08f, 0.02f, 0.02f)),
                },
            };
            AssetDatabase.SaveAssets();
            return data;
        }

        private static LevelConfig EnsureLevel(int number, string title, Action<EnemyConfig> enemy, int[] enemiesPerRoom,
                                               Action<ItemData> item, Action<BossConfig> boss,
                                               Color torch, Color ambient, Color fog)
        {
            string folder = $"{DataFolder}/Level{number}";
            EditorUtil.EnsureFolder(folder);

            var enemyConfig = LoadOrCreate($"{folder}/Enemy_L{number}.asset", enemy);
            var itemData = LoadOrCreate($"{folder}/ChestItem_L{number}.asset", item);
            var bossConfig = LoadOrCreate($"{folder}/Boss_L{number}.asset", boss);

            return LoadOrCreate<LevelConfig>($"{folder}/Level{number}.asset", level =>
            {
                level.levelNumber = number;
                level.displayName = title;
                level.enemy = enemyConfig;
                level.enemiesPerRoom = enemiesPerRoom;
                level.chestItem = itemData;
                level.boss = bossConfig;
                level.torchColor = torch;
                level.ambientColor = ambient;
                level.fogColor = fog;
            });
        }

        private static T LoadOrCreate<T>(string path, Action<T> initialize) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            initialize(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        /// <summary>Asigna a cada nivel el prefab de su jefe (si todavía no tiene).</summary>
        public static void AssignBossPrefabs(GameData data, GameObject[] bossPrefabs)
        {
            for (int i = 0; i < data.Levels.Length && i < bossPrefabs.Length; i++)
            {
                if (data.Levels[i].bossPrefab != null) continue;
                data.Levels[i].bossPrefab = bossPrefabs[i];
                EditorUtility.SetDirty(data.Levels[i]);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
