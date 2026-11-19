using System;
using Dungeon.Model;
using NUnit.Framework;

namespace Dungeon.Tests
{
    public class HealthTests
    {
        [Test]
        public void NewHealth_StartsFull()
        {
            var health = new Health(50);
            Assert.AreEqual(50, health.Current);
            Assert.AreEqual(50, health.Max);
            Assert.IsFalse(health.IsDead);
        }

        [Test]
        public void Constructor_RejectsNonPositiveMax()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Health(0));
        }

        [Test]
        public void TakeDamage_NeverGoesBelowZero_AndDiesOnce()
        {
            var health = new Health(10);
            int deaths = 0;
            health.Died += () => deaths++;

            int applied = health.TakeDamage(25);
            health.TakeDamage(5);

            Assert.AreEqual(10, applied);
            Assert.AreEqual(0, health.Current);
            Assert.IsTrue(health.IsDead);
            Assert.AreEqual(1, deaths);
        }

        [Test]
        public void TakeDamage_RejectsNegativeAmount()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Health(10).TakeDamage(-1));
        }

        [Test]
        public void Heal_NeverExceedsMax()
        {
            var health = new Health(20);
            health.TakeDamage(5);

            int healed = health.Heal(100);

            Assert.AreEqual(5, healed);
            Assert.AreEqual(20, health.Current);
        }

        [Test]
        public void Heal_DoesNothingWhenDead()
        {
            var health = new Health(5);
            health.TakeDamage(5);
            Assert.AreEqual(0, health.Heal(5));
            Assert.AreEqual(0, health.Current);
        }

        [Test]
        public void Changed_ReportsCurrentAndMax()
        {
            var health = new Health(30);
            int lastCurrent = -1, lastMax = -1;
            health.Changed += (c, m) => { lastCurrent = c; lastMax = m; };

            health.TakeDamage(12);

            Assert.AreEqual(18, lastCurrent);
            Assert.AreEqual(30, lastMax);
        }

        [Test]
        public void IncreaseMax_RaisesMaxAndCurrent()
        {
            var health = new Health(30);
            health.TakeDamage(10);

            health.IncreaseMax(20);

            Assert.AreEqual(50, health.Max);
            Assert.AreEqual(40, health.Current);
        }
    }
}
