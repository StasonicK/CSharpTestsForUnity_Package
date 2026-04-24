using Project.Runtime.Data;
using Project.Runtime.Services;

namespace Project.Runtime.Logic
{
    /// <summary>
    /// Orchestrates player combat: damage, healing, events, analytics, persistence.
    /// All dependencies injected — no static state, fully headless-testable.
    /// EventBus instances are injected; use EventBus<T>.Global for production.
    /// </summary>
    public class CombatSystem
    {
        private readonly HealthSystem                          _health;
        private readonly ITimeProvider                         _time;
        private readonly IAnalyticsService                     _analytics;
        private readonly IPersistenceService                   _persistence;
        private readonly IEventBus<PlayerDiedEvent>            _diedBus;
        private readonly IEventBus<PlayerHealthChangedEvent>   _healthChangedBus;
        private readonly string                                _playerId;

        public HealthSystem Health => _health;

        public CombatSystem(
            PlayerData                          data,
            ITimeProvider                       time,
            IAnalyticsService                   analytics,
            IPersistenceService                 persistence,
            IEventBus<PlayerDiedEvent>?         diedBus          = null,
            IEventBus<PlayerHealthChangedEvent>? healthChangedBus = null)
        {
            _playerId         = data.PlayerId;
            _time             = time;
            _analytics        = analytics;
            _persistence      = persistence;
            _diedBus          = diedBus          ?? EventBus<PlayerDiedEvent>.Global;
            _healthChangedBus = healthChangedBus ?? EventBus<PlayerHealthChangedEvent>.Global;
            _health           = new HealthSystem(data, time);

            _health.OnDeath         += HandleDeath;
            _health.OnHealthChanged += HandleHealthChanged;
        }

        public void TakeDamage(float amount) => _health.TakeDamage(amount);
        public void Heal(float amount)       => _health.Heal(amount);

        private void HandleHealthChanged(float current, float max)
        {
            // Publish event first, then use its Fraction (already guards max==0)
            var evt = new PlayerHealthChangedEvent(_playerId, current, max);
            _analytics.TrackEvent("health_changed", "fraction", evt.Fraction);
            _healthChangedBus.Publish(evt);
        }

        private void HandleDeath()
        {
            _analytics.TrackEvent("player_died");
            // Use ITimeProvider.Time — deterministic in tests via stub
            _persistence.Save($"death_{_playerId}", _time.Time);
            _diedBus.Publish(new PlayerDiedEvent(_playerId));
        }
    }
}
