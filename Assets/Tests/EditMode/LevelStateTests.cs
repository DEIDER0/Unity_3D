using System;
using Dungeon.Model;
using NUnit.Framework;

namespace Dungeon.Tests
{
    public class LevelStateTests
    {
        [Test]
        public void StartsInEmptyRoom()
        {
            var level = new LevelState(1, new[] { 2, 0, 0 });
            Assert.AreEqual(0, level.CurrentRoomIndex);
            Assert.AreEqual(RoomType.Empty, level.CurrentRoomType);
            Assert.AreEqual(2, level.EnemiesRemaining);
            Assert.IsFalse(level.IsDoorUnlocked);
        }

        [Test]
        public void EmptyRoomWithoutEnemies_IsUnlockedImmediately()
        {
            var level = new LevelState(1);
            Assert.IsTrue(level.IsDoorUnlocked);
        }

        [Test]
        public void EmptyRoom_UnlocksWhenAllEnemiesDefeated()
        {
            var level = new LevelState(1, new[] { 2, 0, 0 });
            int unlockedRoom = -1;
            level.DoorUnlocked += room => unlockedRoom = room;

            level.NotifyEnemyDefeated();
            Assert.IsFalse(level.IsDoorUnlocked);
            level.NotifyEnemyDefeated();

            Assert.IsTrue(level.IsDoorUnlocked);
            Assert.AreEqual(0, unlockedRoom);
        }

        [Test]
        public void CannotAdvanceThroughLockedDoor()
        {
            var level = new LevelState(1, new[] { 1, 0, 0 });
            Assert.IsFalse(level.TryAdvance());
            Assert.AreEqual(0, level.CurrentRoomIndex);
        }

        [Test]
        public void ChestRoom_UnlocksWhenChestOpened()
        {
            var level = new LevelState(1);
            level.TryAdvance();
            Assert.AreEqual(RoomType.Chest, level.CurrentRoomType);
            Assert.IsFalse(level.IsDoorUnlocked);

            level.NotifyChestOpened();

            Assert.IsTrue(level.IsDoorUnlocked);
        }

        [Test]
        public void ChestOpened_IsIgnoredOutsideChestRoom()
        {
            var level = new LevelState(1, new[] { 1, 0, 0 });
            level.NotifyChestOpened();
            Assert.IsFalse(level.IsDoorUnlocked);
        }

        [Test]
        public void ChestRoomWithEnemies_NeedsBothConditions()
        {
            var level = new LevelState(2, new[] { 0, 1, 0 });
            level.TryAdvance();

            level.NotifyChestOpened();
            Assert.IsFalse(level.IsDoorUnlocked);
            level.NotifyEnemyDefeated();
            Assert.IsTrue(level.IsDoorUnlocked);
        }

        [Test]
        public void BossRoom_UnlocksWhenBossDefeated_AndCompletesLevel()
        {
            // RF3 a nivel de Modelo: al derrotar al jefe se abre la puerta al siguiente nivel.
            var level = new LevelState(1);
            level.TryAdvance();
            level.NotifyChestOpened();
            level.TryAdvance();
            Assert.AreEqual(RoomType.Boss, level.CurrentRoomType);
            Assert.IsFalse(level.IsDoorUnlocked);

            bool bossEvent = false;
            int completed = 0;
            level.BossDefeated += () => bossEvent = true;
            level.LevelCompleted += n => completed = n;

            level.NotifyBossDefeated();
            Assert.IsTrue(bossEvent);
            Assert.IsTrue(level.IsBossDefeated);
            Assert.IsTrue(level.IsDoorUnlocked);

            Assert.IsTrue(level.TryAdvance());
            Assert.IsTrue(level.IsCompleted);
            Assert.AreEqual(1, completed);
            Assert.IsFalse(level.TryAdvance());
        }

        [Test]
        public void RoomChanged_IsRaisedWhenAdvancing()
        {
            var level = new LevelState(1);
            int room = -1;
            level.RoomChanged += r => room = r;

            level.TryAdvance();

            Assert.AreEqual(1, room);
        }

        [Test]
        public void Constructor_RejectsWrongRoomCount()
        {
            Assert.Throws<ArgumentException>(() => new LevelState(1, new[] { 1, 2 }));
        }
    }
}
