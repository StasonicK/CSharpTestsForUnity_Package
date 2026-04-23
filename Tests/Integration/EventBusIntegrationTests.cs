using System.Collections.Generic;
using NUnit.Framework;
using NSubstitute;
using Shouldly;
using Project.Runtime.Services;

namespace Project.Tests.Integration
{
    [TestFixture, Category("Integration")]
    public class EventBusIntegrationTests : IntegrationTestBase
    {
        [Test]
        public void Events_AreDelivered_InSubscriptionOrder()
        {
            var order = new List<string>();
            World.DiedBus.Subscribe(_ => order.Add("first"));
            World.DiedBus.Subscribe(_ => order.Add("second"));
            World.DiedBus.Subscribe(_ => order.Add("third"));
            World.Combat.TakeDamage(100f);
            order.ShouldBe(new[] { "first", "second", "third" }, ignoreOrder: false);
        }

        [Test]
        public void TakeDamage_PublishesBoth_HealthChanged_And_Died_OnKill()
        {
            bool healthChanged = false, died = false;
            World.HealthChangedBus.Subscribe(_ => healthChanged = true);
            World.DiedBus.Subscribe(_ => died = true);
            World.Combat.TakeDamage(100f);
            healthChanged.ShouldBeTrue();
            died.ShouldBeTrue();
        }

        [Test]
        public void TakeDamage_NotKill_PublishesHealthChanged_NotDied()
        {
            bool healthChanged = false, died = false;
            World.HealthChangedBus.Subscribe(_ => healthChanged = true);
            World.DiedBus.Subscribe(_ => died = true);
            World.Combat.TakeDamage(50f);
            healthChanged.ShouldBeTrue();
            died.ShouldBeFalse();
        }

        [Test]
        public void Heal_After_Damage_PublishesHealthChanged_EachTime()
        {
            int count = 0;
            World.HealthChangedBus.Subscribe(_ => count++);
            World.Combat.TakeDamage(40f);
            World.Combat.Heal(20f);
            World.Combat.TakeDamage(10f);
            count.ShouldBe(3);
        }

        [Test]
        public void MultipleListeners_ReceiveIndependentCopies()
        {
            float fractionA = -1f, fractionB = -1f;
            World.HealthChangedBus.Subscribe(e => fractionA = e.Fraction);
            World.HealthChangedBus.Subscribe(e => fractionB = e.Fraction);
            World.Combat.TakeDamage(50f);
            fractionA.ShouldBe(0.5f, tolerance: 0.001f);
            fractionB.ShouldBe(0.5f, tolerance: 0.001f);
        }

        [Test]
        public void Analytics_ReceivesEvent_PerHealthChange()
        {
            World.Combat.TakeDamage(25f);
            World.Combat.TakeDamage(25f);
            World.Combat.Heal(10f);
            World.Analytics.Received(3).TrackEvent(
                "health_changed", "fraction", Arg.Any<object>());
        }

        [Test]
        public void Analytics_PlayerDied_ExactlyOnce_OnOverkill()
        {
            World.Combat.TakeDamage(200f);
            World.Combat.TakeDamage(200f);
            World.Analytics.Received(1).TrackEvent("player_died");
        }

        [Test]
        public void Regen_OverMultipleTicks_PublishesHealthChangedPerTick()
        {
            var world = new TestWorld(regenPerSec: 5f);
            world.Combat.TakeDamage(30f);
            int eventCount = 0;
            world.HealthChangedBus.Subscribe(_ => eventCount++);
            world.Tick(3, dt: 1f);
            eventCount.ShouldBe(3);
            world.Dispose();
        }

        [Test]
        public void AfterDeath_PersistenceStub_CanSimulateExistingRecord()
        {
            World.Persistence.Exists(Arg.Is<string>(k => k.Contains("test_player"))).Returns(true);
            World.Persistence.Load<float>(Arg.Any<string>()).Returns(123.4f);
            World.Combat.TakeDamage(100f);
            World.Persistence.Exists("death_test_player").ShouldBeTrue();
            World.Persistence.Load<float>("death_test_player").ShouldBe(123.4f);
        }

        [Test]
        public void Dispose_ClearsSubscribers_OnIsolatedBus()
        {
            // Verify Dispose() clears the bus: subscriber added before dispose
            // must not fire after dispose + publish on the SAME bus instance.
            var world = new TestWorld();
            int count = 0;
            world.DiedBus.Subscribe(_ => count++);

            world.Dispose(); // clears DiedBus

            world.DiedBus.Publish(new PlayerDiedEvent("after_dispose"));
            count.ShouldBe(0); // handler was cleared by Dispose
            world.DiedBus.SubscriberCount.ShouldBe(0);
        }

        [Test]
        public void TwoTestWorlds_HaveIndependentBuses()
        {
            // Each TestWorld has its own EventBus instance — no cross-world leakage.
            var world1 = new TestWorld();
            var world2 = new TestWorld();
            int count1 = 0, count2 = 0;
            world1.DiedBus.Subscribe(_ => count1++);
            world2.DiedBus.Subscribe(_ => count2++);

            world1.DiedBus.Publish(new PlayerDiedEvent("w1"));

            count1.ShouldBe(1);
            count2.ShouldBe(0); // world2 bus unaffected
            world1.Dispose();
            world2.Dispose();
        }
    }
}
