using NSubstitute;
using Project.Runtime.Data;
using Project.Runtime.Logic;
using Project.Runtime.Services;

namespace Project.Tests.Integration
{
    /// <summary>
    /// Composition root for integration tests.
    ///
    /// Real:  HealthSystem, CombatSystem, EventBus instances
    /// Stubs: ITimeProvider  — World.Time.DeltaTime.Returns(x), World.Time.Time.Returns(x)
    /// Mocks: IAnalyticsService    — World.Analytics.Received(1).TrackEvent(...)
    ///        IPersistenceService  — World.Persistence.Load<T>(...).Returns(v)
    ///
    /// Each TestWorld owns isolated EventBus instances — no static state, no TearDown
    /// cleanup required. Dispose() clears them to release handler references.
    /// </summary>
    public sealed class TestWorld
    {
        // --- Stubs ---
        public ITimeProvider        Time        { get; }
        public IAnalyticsService    Analytics   { get; }
        public IPersistenceService  Persistence { get; }

        // --- Isolated EventBus instances (no static leakage between tests) ---
        public IEventBus<PlayerDiedEvent>          DiedBus          { get; }
        public IEventBus<PlayerHealthChangedEvent> HealthChangedBus { get; }

        // --- Real systems ---
        public CombatSystem Combat { get; }

        public TestWorld(
            string playerId    = "test_player",
            float  maxHealth   = 100f,
            float  regenPerSec = 0f,
            float  deltaTime   = 0.016f)
        {
            Time        = Substitute.For<ITimeProvider>();
            Analytics   = Substitute.For<IAnalyticsService>();
            Persistence = Substitute.For<IPersistenceService>();

            Time.DeltaTime.Returns(deltaTime);

            DiedBus          = new EventBus<PlayerDiedEvent>();
            HealthChangedBus = new EventBus<PlayerHealthChangedEvent>();

            var data = new PlayerData(playerId, maxHealth, regenPerSec);
            Combat = new CombatSystem(data, Time, Analytics, Persistence,
                diedBus: DiedBus, healthChangedBus: HealthChangedBus);
        }

        public void Tick()                        => Combat.Health.Tick();
        public void Tick(int steps, float dt)
        {
            Time.DeltaTime.Returns(dt);
            for (int i = 0; i < steps; i++) Combat.Health.Tick();
        }

        /// <summary>Clears isolated bus instances. No global EventBus cleanup needed.</summary>
        public void Dispose()
        {
            DiedBus.Clear();
            HealthChangedBus.Clear();
        }
    }
}
