using NUnit.Framework;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Shouldly;
using Project.Runtime.Logic;
using Project.Runtime.Services;

namespace Project.Tests.Integration
{
    [TestFixture, Category("Integration")]
    public class CombatIntegrationTests : IntegrationTestBase
    {
        [Test]
        public void Death_PublishesPlayerDiedEvent()
        {
            PlayerDiedEvent? received = null;
            World.DiedBus.Subscribe(e => received = e);
            World.Combat.TakeDamage(100f);
            received.ShouldNotBeNull();
            received!.Value.PlayerId.ShouldBe("test_player");
        }

        [Test]
        public void Death_TracksAnalyticsEvent()
        {
            World.Combat.TakeDamage(100f);
            World.Analytics.Received(1).TrackEvent("player_died");
        }

        [Test]
        public void Death_PersistsTimeOfDeath_FromTimeProvider()
        {
            World.Time.Time.Returns(42.5f);
            World.Combat.TakeDamage(100f);
            World.Persistence.Received(1).Save(
                Arg.Is<string>(k => k.Contains("test_player")),
                42.5f);
        }

        [Test]
        public void Death_DoesNotPersist_WhenPlayerSurvives()
        {
            World.Combat.TakeDamage(50f);
            World.Persistence.DidNotReceive().Save(Arg.Any<string>(), Arg.Any<float>());
        }

        [Test]
        public void TakeDamage_PublishesHealthChangedEvent_WithCorrectFraction()
        {
            PlayerHealthChangedEvent? received = null;
            World.HealthChangedBus.Subscribe(e => received = e);
            World.Combat.TakeDamage(25f);
            received.ShouldNotBeNull();
            received!.Value.Fraction.ShouldBe(0.75f, tolerance: 0.001f);
        }

        [Test]
        public void TakeDamage_TracksHealthChangedAnalytics()
        {
            World.Combat.TakeDamage(25f);
            World.Analytics.Received(1).TrackEvent(
                "health_changed", "fraction",
                Arg.Is<object>(v => (float)v > 0f));
        }

        [Test]
        public void DamageHealDamage_HealthTracksCorrectly()
        {
            World.Combat.TakeDamage(40f);
            World.Combat.Heal(20f);
            World.Combat.TakeDamage(30f);
            World.Combat.Health.Current.ShouldBe(50f, tolerance: 0.001f);
            World.Combat.Health.IsAlive.ShouldBeTrue();
        }

        [Test]
        public void MultipleHits_OnlyOneDeathEvent_Published()
        {
            int deathCount = 0;
            World.DiedBus.Subscribe(_ => deathCount++);
            World.Combat.TakeDamage(60f);
            World.Combat.TakeDamage(60f);
            deathCount.ShouldBe(1);
        }

        [Test]
        public void Regen_OverTicks_RestoresHealth()
        {
            var world = new TestWorld(regenPerSec: 10f);
            world.Combat.TakeDamage(30f);
            world.Tick(3, dt: 1f);
            world.Combat.Health.Current.ShouldBe(100f, tolerance: 0.001f);
            world.Dispose();
        }

        [Test]
        public void Regen_UsesCurrentDeltaTime_FromTimeProvider()
        {
            var world = new TestWorld(regenPerSec: 20f);
            world.Combat.TakeDamage(60f);
            world.Time.DeltaTime.Returns(0.5f);
            world.Tick();
            world.Combat.Health.Current.ShouldBe(50f, tolerance: 0.001f);
            world.Dispose();
        }

        [Test]
        public void Analytics_CapturesHealthFraction_OnDamage()
        {
            float? capturedFraction = null;
            World.Analytics
                 .When(a => a.TrackEvent("health_changed", "fraction", Arg.Any<object>()))
                 .Do(ci => capturedFraction = (float)ci[2]);
            World.Combat.TakeDamage(50f);
            capturedFraction.ShouldNotBeNull();
            capturedFraction!.Value.ShouldBe(0.5f, tolerance: 0.001f);
        }

        [Test]
        public void Persistence_Load_ReturnsStubbed_DeathTimestamp()
        {
            World.Persistence.Load<float>("death_test_player").Returns(999f);
            World.Persistence.Exists("death_test_player").Returns(true);
            World.Persistence.Exists("death_test_player").ShouldBeTrue();
            World.Persistence.Load<float>("death_test_player").ShouldBe(999f);
        }
    }
}
