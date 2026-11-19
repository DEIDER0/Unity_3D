using System;
using System.Collections.Generic;

namespace Dungeon.Model
{
    /// <summary>
    /// MODELO. Estado y reglas del jugador: vida, daño, arma equipada, ítems recogidos
    /// e invulnerabilidad durante el esquive. Se comunica hacia afuera solo con eventos.
    /// </summary>
    public sealed class PlayerState
    {
        /// <summary>(vidaActual, vidaMaxima)</summary>
        public event Action<int, int> HealthChanged;
        /// <summary>Daño total nuevo (base + arma).</summary>
        public event Action<int> DamageChanged;
        /// <summary>Lista completa de ítems recogidos tras añadir uno.</summary>
        public event Action<IReadOnlyList<Item>> ItemsChanged;
        public event Action Died;

        private readonly Health _health;
        private readonly List<Item> _items = new List<Item>();

        public int BaseDamage { get; }
        public Item EquippedWeapon { get; private set; }
        public bool IsInvulnerable { get; private set; }

        public int CurrentHealth => _health.Current;
        public int MaxHealth => _health.Max;
        public float HealthNormalized => _health.Normalized;
        public bool IsDead => _health.IsDead;
        public int Damage => BaseDamage + (EquippedWeapon?.Value ?? 0);
        public IReadOnlyList<Item> Items => _items;

        public PlayerState(int maxHealth, int baseDamage)
        {
            if (baseDamage <= 0) throw new ArgumentOutOfRangeException(nameof(baseDamage), "El daño base debe ser mayor que 0.");
            _health = new Health(maxHealth);
            BaseDamage = baseDamage;

            // Reenviamos los eventos de la vida con los nombres del jugador.
            _health.Changed += (current, max) => HealthChanged?.Invoke(current, max);
            _health.Died += () => Died?.Invoke();
        }

        /// <summary>Recibe daño, salvo que esté esquivando (invulnerable). Devuelve el daño aplicado.</summary>
        public int TakeDamage(int amount)
        {
            if (IsInvulnerable) return 0;
            return _health.TakeDamage(amount);
        }

        public int Heal(int amount) => _health.Heal(amount);

        /// <summary>Lo llama el controlador al empezar y terminar el esquive.</summary>
        public void SetInvulnerable(bool value) => IsInvulnerable = value;

        /// <summary>
        /// Recoge un ítem y aplica su efecto:
        /// - Arma: se equipa solo si es mejor que la actual.
        /// - Mejora: aumenta la vida máxima.
        /// - Curación: recupera vida al instante.
        /// </summary>
        public void AddItem(Item item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (IsDead) return;

            switch (item.Type)
            {
                case ItemType.Weapon:
                    if (EquippedWeapon == null || item.Value > EquippedWeapon.Value)
                    {
                        EquippedWeapon = item;
                        DamageChanged?.Invoke(Damage);
                    }
                    break;
                case ItemType.Upgrade:
                    _health.IncreaseMax(item.Value);
                    break;
                case ItemType.Heal:
                    _health.Heal(item.Value);
                    break;
            }

            _items.Add(item);
            ItemsChanged?.Invoke(_items);
        }
    }
}
