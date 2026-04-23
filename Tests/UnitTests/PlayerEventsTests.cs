using NUnit.Framework;
using Shouldly;
using Project.Runtime.Services;

namespace Project.Tests.UnitTests
{
    [TestFixture, Category("Unit")]
    public class PlayerEventsTests : TestBase
    {
        // --- PlayerDiedEvent ---

        [Test]
        public void PlayerDiedEvent_StoresPlayerId()
        {
            var evt = new PlayerDiedEvent("hero_1");
            evt.PlayerId.ShouldBe("hero_1");
        }

        [Test]
        public void PlayerDiedEvent_EmptyPlayerId_IsAllowed()
        {
            var evt = new PlayerDiedEvent("");
            evt.PlayerId.ShouldBe("");
        }

        [Test]
        public void PlayerDiedEvent_ValueSemantics_CopiesCorrectly()
        {
            var original = new PlayerDiedEvent("p1");
            var copy = original;
            copy.PlayerId.ShouldBe(original.PlayerId);
        }

        [TestCase("player_1")]
        [TestCase("enemy_boss")]
        [TestCase("npc_guard_42")]
        public void PlayerDiedEvent_Parametrized(string id)
        {
            var evt = new PlayerDiedEvent(id);
            evt.PlayerId.ShouldBe(id);
        }

        // --- PlayerHealthChangedEvent ---

        [Test]
        public void PlayerHealthChangedEvent_StoresAllFields()
        {
            var evt = new PlayerHealthChangedEvent("p1", 75f, 100f);
            evt.PlayerId.ShouldBe("p1");
            evt.Current.ShouldBe(75f);
            evt.Max.ShouldBe(100f);
        }

        [Test]
        public void PlayerHealthChangedEvent_Fraction_CalculatedCorrectly()
        {
            var evt = new PlayerHealthChangedEvent("p1", 75f, 100f);
            evt.Fraction.ShouldBe(0.75f, tolerance: 0.001f);
        }

        [Test]
        public void PlayerHealthChangedEvent_Fraction_WhenMaxIsZero_IsZero()
        {
            var evt = new PlayerHealthChangedEvent("p1", 0f, 0f);
            evt.Fraction.ShouldBe(0f);
        }

        [Test]
        public void PlayerHealthChangedEvent_Fraction_FullHealth_IsOne()
        {
            var evt = new PlayerHealthChangedEvent("p1", 100f, 100f);
            evt.Fraction.ShouldBe(1f, tolerance: 0.001f);
        }

        [Test]
        public void PlayerHealthChangedEvent_Fraction_ZeroHealth_IsZero()
        {
            var evt = new PlayerHealthChangedEvent("p1", 0f, 100f);
            evt.Fraction.ShouldBe(0f, tolerance: 0.001f);
        }

        [TestCase(50f,  100f, 0.50f)]
        [TestCase(25f,  100f, 0.25f)]
        [TestCase(1f,   200f, 0.005f)]
        [TestCase(200f, 200f, 1.0f)]
        public void PlayerHealthChangedEvent_Fraction_Parametrized(
            float current, float max, float expected)
        {
            var evt = new PlayerHealthChangedEvent("p1", current, max);
            evt.Fraction.ShouldBe(expected, tolerance: 0.001f);
        }

        [Test]
        public void PlayerHealthChangedEvent_ValueSemantics_CopiesCorrectly()
        {
            var original = new PlayerHealthChangedEvent("p1", 50f, 100f);
            var copy = original;
            copy.Current.ShouldBe(original.Current);
            copy.Max.ShouldBe(original.Max);
            copy.Fraction.ShouldBe(original.Fraction, tolerance: 0.001f);
        }
    }
}
