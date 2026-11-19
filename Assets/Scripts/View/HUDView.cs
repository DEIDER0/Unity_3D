using System.Collections.Generic;
using System.Text;
using Dungeon.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Dungeon.View
{
    /// <summary>
    /// VISTA. HUD del nivel: barra de vida, daño, ítems, sala actual, mensajes y ayuda de controles.
    /// Se suscribe a los eventos de PlayerState y LevelState (y se desuscribe en OnDisable).
    /// </summary>
    public class HUDView : MonoBehaviour
    {
        private static readonly string[] RoomNames = { "Sala 1 · Entrada", "Sala 2 · Tesoro", "Sala 3 · Guarida del jefe" };

        [SerializeField] private RectTransform healthFill;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Text healthText;
        [SerializeField] private Text damageText;
        [SerializeField] private Text itemsText;
        [SerializeField] private Text roomText;
        [SerializeField] private Text toastText;
        [SerializeField] private Color healthyColor = new Color(0.25f, 0.8f, 0.3f);
        [SerializeField] private Color lowHealthColor = new Color(0.9f, 0.2f, 0.15f);
        [SerializeField] private float toastDuration = 2.5f;

        private PlayerState _player;
        private LevelState _level;
        private string _levelTitle;
        private bool _subscribed;
        private float _toastTimer;

        public void Bind(PlayerState player, LevelState level, string levelTitle)
        {
            Unsubscribe();
            _player = player;
            _level = level;
            _levelTitle = levelTitle;
            if (isActiveAndEnabled) Subscribe();
            RefreshAll();
        }

        /// <summary>Muestra un mensaje temporal en el centro de la pantalla.</summary>
        public void ShowToast(string message)
        {
            if (toastText == null) return;
            // Si ya hay un mensaje visible, se agrega debajo (máximo 2 líneas) para no perderlo.
            bool stack = _toastTimer > 0.5f && !string.IsNullOrEmpty(toastText.text) && toastText.text != message;
            if (stack)
            {
                string[] lines = toastText.text.Split('\n');
                toastText.text = lines[lines.Length - 1] + "\n" + message;
            }
            else
            {
                toastText.text = message;
            }
            _toastTimer = toastDuration;
            SetToastAlpha(1f);
        }

        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();

        private void Start()
        {
            if (_toastTimer <= 0f) SetToastAlpha(0f);
        }

        private void Update()
        {
            if (_toastTimer <= 0f) return;
            _toastTimer -= Time.deltaTime;
            SetToastAlpha(Mathf.Clamp01(_toastTimer / 0.5f));
        }

        private void Subscribe()
        {
            if (_subscribed || _player == null || _level == null) return;
            _player.HealthChanged += OnHealthChanged;
            _player.DamageChanged += OnDamageChanged;
            _player.ItemsChanged += OnItemsChanged;
            _level.RoomChanged += OnRoomChanged;
            _level.DoorUnlocked += OnDoorUnlocked;
            _level.BossDefeated += OnBossDefeated;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _player.HealthChanged -= OnHealthChanged;
            _player.DamageChanged -= OnDamageChanged;
            _player.ItemsChanged -= OnItemsChanged;
            _level.RoomChanged -= OnRoomChanged;
            _level.DoorUnlocked -= OnDoorUnlocked;
            _level.BossDefeated -= OnBossDefeated;
            _subscribed = false;
        }

        private void RefreshAll()
        {
            if (_player == null || _level == null) return;
            OnHealthChanged(_player.CurrentHealth, _player.MaxHealth);
            OnDamageChanged(_player.Damage);
            WriteItems(_player.Items);
            OnRoomChanged(_level.CurrentRoomIndex);
        }

        private void OnHealthChanged(int current, int max)
        {
            float normalized = (float)current / max;
            UIBar.SetFill(healthFill, normalized);
            if (healthFillImage != null) healthFillImage.color = normalized <= 0.3f ? lowHealthColor : healthyColor;
            if (healthText != null) healthText.text = $"{current} / {max}";
        }

        private void OnDamageChanged(int damage)
        {
            if (damageText != null) damageText.text = $"Daño: {damage}";
        }

        private void OnItemsChanged(IReadOnlyList<Item> items)
        {
            WriteItems(items);
            if (items.Count > 0) ShowToast(DescribePickup(items[items.Count - 1]));
        }

        private void WriteItems(IReadOnlyList<Item> items)
        {
            if (itemsText == null) return;
            if (items.Count == 0) { itemsText.text = "Ítems: —"; return; }
            var sb = new StringBuilder("Ítems:");
            foreach (var item in items) sb.Append("\n• ").Append(item.Name);
            itemsText.text = sb.ToString();
        }

        private void OnRoomChanged(int roomIndex)
        {
            if (roomText != null)
                roomText.text = $"Nivel {_level.LevelNumber}: {_levelTitle}\n{RoomNames[Mathf.Clamp(roomIndex, 0, RoomNames.Length - 1)]}";
            if (roomIndex == (int)RoomType.Boss) ShowToast("¡Cuidado! El jefe despertó");
            else if (roomIndex == (int)RoomType.Chest) ShowToast("Busca el cofre y pulsa E para abrirlo");
        }

        private void OnDoorUnlocked(int roomIndex)
        {
            if (roomIndex == (int)RoomType.Boss) return; // el mensaje del jefe lo da OnBossDefeated
            ShowToast("¡La puerta se abrió!");
        }

        private void OnBossDefeated() => ShowToast("¡Jefe derrotado! Cruza la puerta");

        private static string DescribePickup(Item item)
        {
            switch (item.Type)
            {
                case ItemType.Weapon: return $"¡Obtuviste {item.Name}! Daño +{item.Value}";
                case ItemType.Upgrade: return $"¡Obtuviste {item.Name}! Vida máxima +{item.Value}";
                default: return $"¡{item.Name}! Recuperas {item.Value} de vida";
            }
        }

        private void SetToastAlpha(float alpha)
        {
            if (toastText == null) return;
            var c = toastText.color;
            c.a = alpha;
            toastText.color = c;
        }
    }
}
