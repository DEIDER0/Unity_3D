using System.Collections.Generic;
using Dungeon.Model;
using NUnit.Framework;

namespace Dungeon.Tests
{
    public class PlayerStateTests
    {
        private PlayerState _player;

        [SetUp]
        public void SetUp() => _player = new PlayerState(maxHealth: 100, baseDamage: 10);

        [Test]
        public void TakeDamage_ReducesHealth_AndRaisesHealthChanged()
        {
            int reported = -1;
            _player.HealthChanged += (current, max) => reported = current;

            _player.TakeDamage(30);

            Assert.AreEqual(70, _player.CurrentHealth);
            Assert.AreEqual(70, reported);
        }

        [Test]
        public void TakeDamage_IsIgnoredWhileInvulnerable()
        {
            _player.SetInvulnerable(true);
            Assert.AreEqual(0, _player.TakeDamage(50));
            Assert.AreEqual(100, _player.CurrentHealth);

            _player.SetInvulnerable(false);
            _player.TakeDamage(50);
            Assert.AreEqual(50, _player.CurrentHealth);
        }

        [Test]
        public void Died_IsRaisedWhenHealthReachesZero()
        {
            bool died = false;
            _player.Died += () => died = true;

            _player.TakeDamage(100);

            Assert.IsTrue(died);
            Assert.IsTrue(_player.IsDead);
        }

        [Test]
        public void Heal_RestoresHealthUpToMax()
        {
            _player.TakeDamage(40);
            _player.Heal(25);
            Assert.AreEqual(85, _player.CurrentHealth);
            _player.Heal(999);
            Assert.AreEqual(100, _player.CurrentHealth);
        }

        [Test]
        public void AddItem_Weapon_IncreasesDamage()
        {
            int reportedDamage = -1;
            _player.DamageChanged += d => reportedDamage = d;

            _player.AddItem(new Item("Espada de hierro", ItemType.Weapon, 5));

            Assert.AreEqual(15, _player.Damage);
            Assert.AreEqual(15, reportedDamage);
        }

        [Test]
        public void AddItem_WorseWeapon_DoesNotReplaceBetterOne()
        {
            _player.AddItem(new Item("Hacha", ItemType.Weapon, 8));
            _player.AddItem(new Item("Daga", ItemType.Weapon, 3));

            Assert.AreEqual(18, _player.Damage);
            Assert.AreEqual("Hacha", _player.EquippedWeapon.Name);
        }

        [Test]
        public void AddItem_Upgrade_IncreasesMaxHealth()
        {
            _player.AddItem(new Item("Amuleto", ItemType.Upgrade, 25));
            Assert.AreEqual(125, _player.MaxHealth);
            Assert.AreEqual(125, _player.CurrentHealth);
        }

        [Test]
        public void AddItem_Heal_RestoresHealth()
        {
            _player.TakeDamage(60);
            _player.AddItem(new Item("Poción", ItemType.Heal, 40));
            Assert.AreEqual(80, _player.CurrentHealth);
        }

        [Test]
        public void AddItem_StoresItem_AndRaisesItemsChanged()
        {
            IReadOnlyList<Item> reported = null;
            _player.ItemsChanged += items => reported = items;
            var potion = new Item("Poción", ItemType.Heal, 10);

            _player.AddItem(potion);

            Assert.AreEqual(1, _player.Items.Count);
            Assert.AreSame(potion, _player.Items[0]);
            Assert.AreSame(_player.Items, reported);
        }
    }
}
