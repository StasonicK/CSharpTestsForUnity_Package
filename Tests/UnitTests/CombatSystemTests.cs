using NUnit.Framework;
using NSubstitute;
using Shouldly;
using Project.Runtime.Data;
using Project.Runtime.Logic;
using Project.Runtime.Services;

namespace Project.Tests.UnitTests
{
    /// <summary>
    /// Unit tests for CombatSystem constructor wiring.
    /// Verifies that event subscriptions to HealthSystem are correctly established
    /// and that injected dependencies are invoked as expected on damage/death.
    /// </summary>
    [TestFixture, Category("Unit")]
    public class CombatSystemTests : TestBase
    {
        private ITimeProvider       _time             = null!;
        private IAnalyticsService   _analytics        = null!;
        private IPersistenceService _persistence      = null!;
        private IEventBus<PlayerDiedEvent>          _diedBus          = null!;
        private IEventBus<PlayerHealthChangedEvent> _healthChangedBus = null!;
        private CombatSystem _combat           = null!;

        [SetUp]
        public void SetupCombatSystem()
        {
            _time             = Substitute.For<ITimeProvider>();
            _analytics        = Substitute.For<IAnalyticsService>();
            _persistence      = Substitute.For<IPersistenceService>();
            _diedBus          = new EventBus<PlayerDiedEvent>();
            _healthChangedBus = new EventBus<PlayerHealthChangedEvent>();

            _time.DeltaTime.Returns(0.016f);
            _time.Time.Returns(0f);

            _combat = new CombatSystem(
                new PlayerData("p1", 100f),
                _time, _analytics, _persistence,
                _diedBus, _healthChangedBus);
        }

        // --- Constructor wires OnHealthChanged ---

        [Test]
        public void Constructor_WiresHealthChanged_AnalyticsCalledOnDamage()
        {
            _combat.TakeDamage(30f);
            _analytics.Received(1).TrackEvent("health_changed", "fraction",
                Arg.Is<object>(v => (float)v > 0f));
        }

        [Test]
        public void Constructor_WiresHealthChanged_BusPublishedOnDamage()
        {
            PlayerHealthChangedEvent? received = null;
            _healthChangedBus.Subscribe(e => received = e);

            _combat.TakeDamage(30f);

            received.ShouldNotBeNull();
            received!.Value.Fraction.ShouldBe(0.7f, tolerance: 0.001f);
        }

        // --- Constructor wires OnDeath ---

        [Test]
        public void Constructor_WiresDeath_AnalyticsCalledOnKill()
        {
            _combat.TakeDamage(100f);
            _analytics.Received(1).TrackEvent("player_died");
        }

        [Test]
        public void Constructor_WiresDeath_PersistsSaves_TimeOfDeath()
        {
            _time.Time.Returns(77.7f);
            _combat.TakeDamage(100f);
            _persistence.Received(1).Save(
                Arg.Is<string>(k => k.Contains("p1")),
                77.7f);
        }

        [Test]
        public void Constructor_WiresDeath_BusPublishedOnKill()
        {
            PlayerDiedEvent? received = null;
            _diedBus.Subscribe(e => received = e);

            _combat.TakeDamage(100f);

            received.ShouldNotBeNull();
            received!.Value.PlayerId.ShouldBe("p1");
        }

        // --- Default bus fallback (Global) ---

        [Test]
        public void Constructor_NullBusArgs_FallsBackToGlobal_DoesNotThrow()
        {
            var combat = new CombatSystem(
                new PlayerData("p2", 100f), _time, _analytics, _persistence);
            Should.NotThrow(() => combat.TakeDamage(50f));
        }

        // --- Delegate to HealthSystem ---

        [Test]
        public void TakeDamage_DelegatesToHealthSystem()
        {
            _combat.TakeDamage(40f);
            _combat.Health.Current.ShouldBe(60f, tolerance: 0.001f);
        }

        [Test]
        public void Heal_DelegatesToHealthSystem()
        {
            _combat.TakeDamage(60f);
            _combat.Heal(20f);
            _combat.Health.Current.ShouldBe(60f, tolerance: 0.001f);
        }
    }
}
