using NUnit.Framework;
using NSubstitute;
using Shouldly;
using Project.Runtime.Logic;
using Project.Tests.Common;

namespace Project.Tests.Examples
{
    /// <summary>
    /// Runnable NSubstitute reference examples.
    /// Demonstrates: mock, stub, spy, arg matchers, arg capture.
    /// Lightweight - no TestWorld, no EventBus. Each test is self-contained.
    ///
    /// Run: test examples
    /// </summary>
    [TestFixture, Category("Examples")]
    public class SubstituteExamples
    {
        [SetUp]    public void Setup()    => TestOutputHelper.LogStart(GetType());
        [TearDown] public void Teardown() => TestOutputHelper.LogEnd();

        // ---- MOCK: verify a method was called --------------------------------

        [Test]
        public void Mock_VerifiesExactCall()
        {
            var analytics = Substitute.For<IAnalyticsService>();
            analytics.TrackEvent("player_died");
            analytics.Received(1).TrackEvent("player_died");
            analytics.DidNotReceive().TrackEvent("level_up");
        }

        [Test]
        public void Mock_VerifiesCallCount()
        {
            var analytics = Substitute.For<IAnalyticsService>();
            analytics.TrackEvent("hit");
            analytics.TrackEvent("hit");
            analytics.TrackEvent("hit");
            analytics.Received(3).TrackEvent("hit");
        }

        // ---- STUB: configure return values -----------------------------------

        [Test]
        public void Stub_Returns_ConfiguredValue()
        {
            var persistence = Substitute.For<IPersistenceService>();
            persistence.Exists("death_player_1").Returns(true);
            persistence.Load<long>("death_player_1").Returns(1234567890L);
            persistence.Exists("death_player_1").ShouldBeTrue();
            persistence.Load<long>("death_player_1").ShouldBe(1234567890L);
        }

        [Test]
        public void Stub_Returns_Default_ForUnconfiguredCalls()
        {
            var persistence = Substitute.For<IPersistenceService>();
            // No stub configured - default for bool is false
            persistence.Exists("anything").ShouldBeFalse();
        }

        [Test]
        public void Stub_ThrowsOnCall()
        {
            var persistence = Substitute.For<IPersistenceService>();
            // Setup configures Save<object> - cast the argument to match
            persistence.When(p => p.Save(Arg.Any<string>(), Arg.Any<object>()))
                       .Do(_ => throw new System.IO.IOException("Disk full"));
            // Cast to object so T = object, matching the When/Do setup
            Should.Throw<System.IO.IOException>(() =>
                persistence.Save("key", (object)42));
        }

        // ---- ARG MATCHERS ---------------------------------------------------

        [Test]
        public void ArgMatcher_Any_MatchesAllValues()
        {
            var analytics = Substitute.For<IAnalyticsService>();
            analytics.TrackEvent("some_event");
            // Arg.Any<T> inside Received() - matches any string argument
            analytics.Received(1).TrackEvent(Arg.Any<string>());
        }

        [Test]
        public void ArgMatcher_Is_MatchesCondition()
        {
            var analytics = Substitute.For<IAnalyticsService>();
            analytics.TrackEvent("player_died");
            analytics.TrackEvent("player_hit");
            // Arg.Is with predicate - matches only "player_" prefixed events
            analytics.Received(2).TrackEvent(Arg.Is<string>(s => s.StartsWith("player")));
        }

        [Test]
        public void ArgCapture_CapturesArgumentValue()
        {
            var analytics = Substitute.For<IAnalyticsService>();
            string? captured = null;
            analytics.When(a => a.TrackEvent(Arg.Any<string>()))
                     .Do(ci => captured = ci.Arg<string>());
            analytics.TrackEvent("enemy_killed");
            captured.ShouldBe("enemy_killed");
        }

        // ---- SPY: selective behavior via When/Do ----------------------------

        [Test]
        public void Spy_WhenDo_SelectiveBehavior()
        {
            var spy = Substitute.For<IAnalyticsService>();
            // Configure a no-op only for "skip" - other calls are unaffected
            spy.When(a => a.TrackEvent("skip")).Do(_ => { });
            spy.TrackEvent("normal");
            spy.TrackEvent("skip");
            spy.Received(1).TrackEvent("normal");
            spy.Received(1).TrackEvent("skip");
        }
    }
}
