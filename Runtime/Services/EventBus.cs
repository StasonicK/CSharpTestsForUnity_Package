using System;
using System.Collections.Generic;

namespace Project.Runtime.Services
{
    /// <summary>
    /// Instance-based event bus implementing IEventBus.
    /// Inject per-system for full test isolation.
    ///
    /// Production Views use the static Global instance:
    ///   EventBus<PlayerDiedEvent>.Global.Subscribe(...);
    /// Tests inject a fresh EventBus<T>() per TestWorld — no cross-test leakage.
    /// </summary>
    public sealed class EventBus<TEvent> : IEventBus<TEvent>
    {
        private readonly List<Action<TEvent>> _handlers = new();

        // ── Static Global for production Views (backward compat) ──────────
        // Never use in tests — always inject an instance instead.
        public static readonly EventBus<TEvent> Global = new();

        // ── Instance API ─────────────────────────────────────────────────
        public void Subscribe(Action<TEvent> handler)
        {
            if (!_handlers.Contains(handler))
                _handlers.Add(handler);
        }

        public void Unsubscribe(Action<TEvent> handler)
            => _handlers.Remove(handler);

        public void Publish(TEvent evt)
        {
            // Snapshot prevents issues when handlers unsubscribe during dispatch
            var snapshot = _handlers.ToArray();
            foreach (var handler in snapshot)
                handler(evt);
        }

        public void Clear() => _handlers.Clear();

        public int SubscriberCount => _handlers.Count;
    }
}
