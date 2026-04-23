using NUnit.Framework;
using Shouldly;
using Project.Runtime.Data;

namespace Project.Tests.UnitTests
{
    [TestFixture, Category("Unit")]
    public class PlayerDataTests : TestBase
    {
        // --- Constructor ---

        [Test]
        public void Constructor_SetsAllFields()
        {
            var d = new PlayerData("p1", 150f, 10f);
            d.PlayerId.ShouldBe("p1");
            d.MaxHealth.ShouldBe(150f);
            d.RegenPerSecond.ShouldBe(10f);
        }

        [Test]
        public void Constructor_DefaultRegen_IsZero()
        {
            var d = new PlayerData("p1", 100f);
            d.RegenPerSecond.ShouldBe(0f);
        }

        [Test]
        public void Constructor_EmptyPlayerId_IsAllowed()
        {
            var d = new PlayerData("", 100f);
            d.PlayerId.ShouldBe("");
        }

        [Test]
        public void Constructor_ZeroMaxHealth_IsAllowed()
        {
            var d = new PlayerData("p1", 0f);
            d.MaxHealth.ShouldBe(0f);
        }

        // --- Default ---

        [Test]
        public void Default_PlayerId_IsPlayer1()
            => PlayerData.Default.PlayerId.ShouldBe("player_1");

        [Test]
        public void Default_MaxHealth_Is100()
            => PlayerData.Default.MaxHealth.ShouldBe(100f);

        [Test]
        public void Default_RegenPerSecond_Is5()
            => PlayerData.Default.RegenPerSecond.ShouldBe(5f);

        // --- Value semantics ---

        [Test]
        public void Struct_ValueSemantics_CopyIsIndependent()
        {
            var original = new PlayerData("p1", 100f, 5f);
            var copy = original;
            // Structs are values — copy should have same fields
            copy.PlayerId.ShouldBe(original.PlayerId);
            copy.MaxHealth.ShouldBe(original.MaxHealth);
            copy.RegenPerSecond.ShouldBe(original.RegenPerSecond);
        }

        [TestCase("hero",   200f, 20f)]
        [TestCase("enemy",  50f,  0f)]
        [TestCase("boss",   999f, 1.5f)]
        public void Constructor_Parametrized(string id, float hp, float regen)
        {
            var d = new PlayerData(id, hp, regen);
            d.PlayerId.ShouldBe(id);
            d.MaxHealth.ShouldBe(hp);
            d.RegenPerSecond.ShouldBe(regen);
        }
    }
}
