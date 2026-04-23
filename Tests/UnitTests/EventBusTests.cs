using System.Collections.Generic;
using NUnit.Framework;
using Shouldly;
using Project.Runtime.Services;

namespace Project.Tests.UnitTests
{
    /// <summary>
    /// Unit tests for EventBus<T> instance.
    /// Each test creates its own EventBus instance — no static state, no TearDown cleanup.
    /// </summary>
    [TestFixture, Category("Unit")]
    public class EventBusTests : TestBase
    {
        // Helper: fresh isolated bus per call
        private static EventBus<PlayerDiedEvent>          NewDiedBus()    => new();
        private static EventBus<PlayerHealthChangedEvent> NewChangedBus() => new();

        // --- Subscribe + Publish ---

        [Test]
        public void Subscribe_Publish_HandlerReceivesEvent()
        {
            var bus = NewDiedBus();
            PlayerDiedEvent? received = null;
            bus.Subscribe(e => received = e);
            bus.Publish(new PlayerDiedEvent("p1"));
            received.ShouldNotBeNull();
            received!.Value.PlayerId.ShouldBe("p1");
        }

        [Test]
        public void Publish_NoSubscribers_DoesNotThrow()
        {
            var bus = NewDiedBus();
            Should.NotThrow(() => bus.Publish(new PlayerDiedEvent("p1")));
        }

        [Test]
        public void Publish_MultipleSubscribers_AllReceive()
        {
            var bus = NewDiedBus();
            var results = new List<string>();
            bus.Subscribe(e => results.Add("A:" + e.PlayerId));
            bus.Subscribe(e => results.Add("B:" + e.PlayerId));
            bus.Publish(new PlayerDiedEvent("p1"));
            results.ShouldBe(new[] { "A:p1", "B:p1" }, ignoreOrder: false);
        }

        [Test]
        public void Publish_DeliversCorrectEventData()
        {
            var bus = NewChangedBus();
            PlayerHealthChangedEvent? received = null;
            bus.Subscribe(e => received = e);
            bus.Publish(new PlayerHealthChangedEvent("p2", 75f, 100f));
            received!.Value.Current.ShouldBe(75f);
            received!.Value.Max.ShouldBe(100f);
        }

        // --- Unsubscribe ---

        [Test]
        public void Unsubscribe_HandlerNoLongerReceives()
        {
            var bus = NewDiedBus();
            int count = 0;
            void UnsubHandler(PlayerDiedEvent _) => count++;
            bus.Subscribe(UnsubHandler);
            bus.Unsubscribe(UnsubHandler);
            bus.Publish(new PlayerDiedEvent("p1"));
            count.ShouldBe(0);
        }

        [Test]
        public void Unsubscribe_OnlyRemovesTargetHandler()
        {
            var bus = NewDiedBus();
            int countA = 0, countB = 0;
            void HandlerA(PlayerDiedEvent _) => countA++;
            void HandlerB(PlayerDiedEvent _) => countB++;
            bus.Subscribe(HandlerA);
            bus.Subscribe(HandlerB);
            bus.Unsubscribe(HandlerA);
            bus.Publish(new PlayerDiedEvent("p1"));
            countA.ShouldBe(0);
            countB.ShouldBe(1);
        }

        [Test]
        public void Unsubscribe_NonRegistered_DoesNotThrow()
        {
            var bus = NewDiedBus();
            Should.NotThrow(() => bus.Unsubscribe(_ => { }));
        }

        // --- Duplicate subscribe ---

        [Test]
        public void Subscribe_SameHandlerTwice_CalledOnce()
        {
            var bus = NewDiedBus();
            int count = 0;
            void DupeHandler(PlayerDiedEvent _) => count++;
            bus.Subscribe(DupeHandler);
            bus.Subscribe(DupeHandler);
            bus.Publish(new PlayerDiedEvent("p1"));
            count.ShouldBe(1);
        }

        // --- SubscriberCount ---

        [Test]
        public void SubscriberCount_StartsAtZero()
            => NewDiedBus().SubscriberCount.ShouldBe(0);

        [Test]
        public void SubscriberCount_IncrDecr_Correctly()
        {
            var bus = NewDiedBus();
            void SubA(PlayerDiedEvent _) { }
            void SubB(PlayerDiedEvent _) { }
            bus.Subscribe(SubA); bus.Subscribe(SubB);
            bus.SubscriberCount.ShouldBe(2);
            bus.Unsubscribe(SubA);
            bus.SubscriberCount.ShouldBe(1);
        }

        // --- Clear ---

        [Test]
        public void Clear_RemovesAllSubscribers()
        {
            var bus = NewDiedBus();
            int count = 0;
            bus.Subscribe(_ => count++);
            bus.Clear();
            bus.Publish(new PlayerDiedEvent("p1"));
            count.ShouldBe(0);
        }

        [Test]
        public void Clear_SetsSubscriberCountToZero()
        {
            var bus = NewDiedBus();
            bus.Subscribe(_ => { });
            bus.Clear();
            bus.SubscriberCount.ShouldBe(0);
        }

        // --- Dispatch safety ---

        [Test]
        public void Publish_HandlerUnsubscribesDuringDispatch_NoException()
        {
            var bus = NewDiedBus();
            void SelfRemove(PlayerDiedEvent _) => bus.Unsubscribe(SelfRemove);
            bus.Subscribe(SelfRemove);
            Should.NotThrow(() => bus.Publish(new PlayerDiedEvent("p1")));
        }

        // --- Instance isolation ---

        [Test]
        public void TwoInstances_AreFullyIndependent()
        {
            var bus1 = NewDiedBus();
            var bus2 = NewDiedBus();
            int count1 = 0, count2 = 0;
            bus1.Subscribe(_ => count1++);
            bus2.Subscribe(_ => count2++);
            bus1.Publish(new PlayerDiedEvent("p1"));
            count1.ShouldBe(1);
            count2.ShouldBe(0);
        }

        [Test]
        public void DifferentEventTypes_AreIndependent()
        {
            var diedBus    = NewDiedBus();
            var changedBus = NewChangedBus();
            int diedCount = 0, changedCount = 0;
            diedBus.Subscribe(_ => diedCount++);
            changedBus.Subscribe(_ => changedCount++);
            diedBus.Publish(new PlayerDiedEvent("p1"));
            diedCount.ShouldBe(1);
            changedCount.ShouldBe(0);
        }
    }
}
