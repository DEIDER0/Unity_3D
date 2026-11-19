using System;
using Dungeon.Model;
using NUnit.Framework;

namespace Dungeon.Tests
{
    public class BossStateTests
    {
        [Test]
        public void Enrages_OnceWhenHealthDropsToThreshold()
        {
            var boss = new BossState("Rey Esqueleto", 100, 10, enrageThreshold: 0.5f, enragedDamageMultiplier: 1.5f);
            int enrages = 0;
            boss.Enraged += () => enrages++;

            boss.TakeDamage(40);
            Assert.IsFalse(boss.IsEnraged);
            Assert.AreEqual(10, boss.Damage);

            boss.TakeDamage(10);
            boss.TakeDamage(10);

            Assert.IsTrue(boss.IsEnraged);
            Assert.AreEqual(15, boss.Damage);
            Assert.AreEqual(1, enrages);
        }

        [Test]
        public void DoesNotEnrage_WhenKilledInOneHit()
        {
            var boss = new BossState("Rey Esqueleto", 100, 10);
            bool enraged = false, died = false;
            boss.Enraged += () => enraged = true;
            boss.Died += () => died = true;

            boss.TakeDamage(500);

            Assert.IsTrue(died);
            Assert.IsFalse(enraged);
        }

        [Test]
        public void StillRaisesHealthChanged()
        {
            var boss = new BossState("Rey Esqueleto", 100, 10);
            int reported = -1;
            boss.HealthChanged += (current, max) => reported = current;

            boss.TakeDamage(25);

            Assert.AreEqual(75, reported);
        }

        [Test]
        public void SpecialDamage_UsesConfiguredValue_AndScalesWithEnrage()
        {
            var boss = new BossState("Rey Esqueleto", 100, 10, 0.5f, 1.5f, specialDamage: 20);
            Assert.AreEqual(20, boss.SpecialDamage);

            boss.TakeDamage(60);

            Assert.AreEqual(30, boss.SpecialDamage);
        }

        [Test]
        public void SpecialDamage_DefaultsToTwiceNormalDamage()
        {
            Assert.AreEqual(24, new BossState("X", 100, 12).SpecialDamage);
        }

        [Test]
        public void Constructor_RejectsInvalidThreshold()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BossState("X", 100, 10, enrageThreshold: 1.2f));
        }
    }
}
