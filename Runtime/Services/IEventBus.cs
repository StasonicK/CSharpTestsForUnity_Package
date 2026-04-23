using System;

namespace Project.Runtime.Services
{
    /// <summary>
    /// Instance-based event bus interface.
    /// Inject in production code and tests for full isolation.
    /// </summary>
    public interface IEventBus<TEvent>
    {
        void Subscribe(Action<TEvent> handler);
        void Unsubscribe(Action<TEvent> handler);
        void Publish(TEvent evt);
        void Clear();
        int SubscriberCount { get; }
    }
}
