using Dungeon.Model;
using NUnit.Framework;

namespace Dungeon.Tests
{
    public class ChestModelTests
    {
        [Test]
        public void TryOpen_GivesContentOnlyOnce()
        {
            var potion = new Item("Poción", ItemType.Heal, 30);
            var chest = new ChestModel(potion);
            int opened = 0;
            chest.Opened += _ => opened++;

            bool first = chest.TryOpen(out Item item);
            bool second = chest.TryOpen(out Item again);

            Assert.IsTrue(first);
            Assert.AreSame(potion, item);
            Assert.IsFalse(second);
            Assert.IsNull(again);
            Assert.IsTrue(chest.IsOpened);
            Assert.AreEqual(1, opened);
        }

        [Test]
        public void OpeningChest_AppliesRewardToPlayer()
        {
            // RF2 a nivel de Modelo: abrir el cofre entrega el ítem y el jugador cambia.
            var player = new PlayerState(100, 10);
            var chest = new ChestModel(new Item("Espada de hierro", ItemType.Weapon, 5));

            if (chest.TryOpen(out Item reward)) player.AddItem(reward);

            Assert.AreEqual(15, player.Damage);
            Assert.AreEqual(1, player.Items.Count);
        }
    }
}
