using Dungeon.Model;
using NUnit.Framework;

namespace Dungeon.Tests
{
    public class EnemyStateTests
    {
        [Test]
        public void TakeDamage_ReducesHealth_AndRaisesHealthChanged()
        {
            var enemy = new EnemyState("Esqueleto", 30, 5);
            int reported = -1;
            enemy.HealthChanged += (current, max) => reported = current;

            enemy.TakeDamage(12);

            Assert.AreEqual(18, enemy.CurrentHealth);
            Assert.AreEqual(18, reported);
        }

        [Test]
        public void Died_IsRaisedOnlyOnce()
        {
            var enemy = new EnemyState("Esqueleto", 20, 5);
            int deaths = 0;
            enemy.Died += () => deaths++;

            enemy.TakeDamage(20);
            enemy.TakeDamage(20);

            Assert.IsTrue(enemy.IsDead);
            Assert.AreEqual(1, deaths);
        }

        [Test]
        public void Damage_IsTheConfiguredValue()
        {
            Assert.AreEqual(7, new EnemyState("Esqueleto", 20, 7).Damage);
        }

        [Test]
        public void PlayerHitsEnemy_WithItsCurrentDamage()
        {
            // Flujo del juego a nivel de Modelo: el daño del jugador (con arma) baja la vida del enemigo.
            var player = new PlayerState(100, 10);
            player.AddItem(new Item("Espada", ItemType.Weapon, 5));
            var enemy = new EnemyState("Esqueleto", 30, 5);

            enemy.TakeDamage(player.Damage);
            enemy.TakeDamage(player.Damage);

            Assert.IsTrue(enemy.IsDead);
        }
    }
}
