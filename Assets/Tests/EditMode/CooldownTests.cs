using Dungeon.Model;
using NUnit.Framework;

namespace Dungeon.Tests
{
    public class CooldownTests
    {
        [Test]
        public void IsReadyAtStart_ThenWaitsDuration()
        {
            var cooldown = new Cooldown(0.5);

            Assert.IsTrue(cooldown.TryUse(10.0));
            Assert.IsFalse(cooldown.TryUse(10.2));
            Assert.IsTrue(cooldown.TryUse(10.5));
        }

        [Test]
        public void Reset_MakesItReadyAgain()
        {
            var cooldown = new Cooldown(5);
            cooldown.TryUse(0);
            cooldown.Reset();
            Assert.IsTrue(cooldown.IsReady(0.1));
        }
    }
}
