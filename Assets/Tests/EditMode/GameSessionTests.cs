using Dungeon.Model;
using NUnit.Framework;

namespace Dungeon.Tests
{
    public class GameSessionTests
    {
        [Test]
        public void CompletingLevel_AdvancesToNext()
        {
            var session = new GameSession(new PlayerState(100, 10));
            int next = 0;
            session.LevelAdvanced += n => next = n;

            session.CompleteLevel(1);

            Assert.AreEqual(2, session.CurrentLevel);
            Assert.AreEqual(2, next);
            Assert.IsFalse(session.IsOver);
        }

        [Test]
        public void CompletingLastLevel_IsVictory()
        {
            var session = new GameSession(new PlayerState(100, 10));
            bool victory = false;
            session.Victory += () => victory = true;

            session.CompleteLevel(1);
            session.CompleteLevel(2);
            session.CompleteLevel(3);

            Assert.IsTrue(victory);
            Assert.IsTrue(session.IsOver);
            Assert.IsTrue(session.IsVictory);
        }

        [Test]
        public void PlayerDeath_IsDefeat()
        {
            var player = new PlayerState(50, 10);
            var session = new GameSession(player);
            bool defeat = false;
            session.Defeat += () => defeat = true;

            player.TakeDamage(50);

            Assert.IsTrue(defeat);
            Assert.IsTrue(session.IsOver);
            Assert.IsFalse(session.IsVictory);
        }

        [Test]
        public void PlayerKeepsItemsBetweenLevels()
        {
            var player = new PlayerState(100, 10);
            var session = new GameSession(player);
            player.AddItem(new Item("Espada", ItemType.Weapon, 5));

            session.CompleteLevel(1);

            Assert.AreEqual(15, session.Player.Damage);
        }

        [Test]
        public void CompletingWrongLevel_IsIgnored()
        {
            var session = new GameSession(new PlayerState(100, 10));
            session.CompleteLevel(2);
            Assert.AreEqual(1, session.CurrentLevel);
        }

        [Test]
        public void CanStartOnAnotherLevel_ForTesting()
        {
            var session = new GameSession(new PlayerState(100, 10), startLevel: 3);
            bool victory = false;
            session.Victory += () => victory = true;

            session.CompleteLevel(3);

            Assert.IsTrue(victory);
        }

        [Test]
        public void CreateCurrentLevel_UsesCurrentLevelNumber()
        {
            var session = new GameSession(new PlayerState(100, 10));
            session.CompleteLevel(1);
            Assert.AreEqual(2, session.CreateCurrentLevel(new[] { 1, 0, 0 }).LevelNumber);
        }
    }
}
