using NUnit.Framework;
using NSubstitute;
using Shouldly;
using Project.Runtime.Data;
using Project.Runtime.Logic;

namespace Project.Tests.UnitTests
{
    [TestFixture, Category("Unit")]
    public class HealthSystemTests : TestBase
    {
        private ITimeProvider _time   = null!;
        private HealthSystem  _health = null!;

        [SetUp]
        public void SetupHealthSystem()
        {            
            _time   = Substitute.For<ITimeProvider>();
            _time.DeltaTime.Returns(0.1f);
            _health = new HealthSystem(new PlayerData("p1", 100f, regenPerSecond: 10f), _time);
        }

        // --- Initial state ---

        [Test]
        public void Initial_Current_EqualsMaxHealth()
            => _health.Current.ShouldBe(100f);

        [Test]
        public void Initial_Fraction_IsOne()
            => _health.Fraction.ShouldBe(1f, tolerance: 0.001f);

        [Test]
        public void Initial_IsAlive_IsTrue()
            => _health.IsAlive.ShouldBeTrue();

        [Test]
        public void Initial_Max_EqualsPlayerDataMaxHealth()
            => _health.Max.ShouldBe(100f);

        // --- TakeDamage ---

        [Test]
        public void TakeDamage_ReducesCurrentHealth()
        {
            _health.TakeDamage(30f);
            _health.Current.ShouldBe(70f, tolerance: 0.001f);
        }

        [Test]
        public void TakeDamage_Zero_IsIgnored()
        {
            _health.TakeDamage(0f);
            _health.Current.ShouldBe(100f);
        }

        [Test]
        public void TakeDamage_Negative_IsIgnored()
        {
            _health.TakeDamage(-10f);
            _health.Current.ShouldBe(100f);
        }

        [Test]
        public void TakeDamage_ExactMax_SetsCurrentToZero()
        {
            _health.TakeDamage(100f);
            _health.Current.ShouldBe(0f);
        }

        [Test]
        public void TakeDamage_Overkill_ClampsToZero()
        {
            _health.TakeDamage(999f);
            _health.Current.ShouldBe(0f);
        }

        [Test]
        public void TakeDamage_ToZero_SetsIsDeadTrue()
        {
            _health.TakeDamage(100f);
            _health.IsAlive.ShouldBeFalse();
        }

        [Test]
        public void TakeDamage_ToZero_FiresOnDeath()
        {
            bool fired = false;
            _health.OnDeath += () => fired = true;
            _health.TakeDamage(100f);
            fired.ShouldBeTrue();
        }

        [Test]
        public void TakeDamage_FiresOnHealthChanged_WithCorrectValues()
        {
            float reportedCurrent = -1f, reportedMax = -1f;
            _health.OnHealthChanged += (c, m) => { reportedCurrent = c; reportedMax = m; };
            _health.TakeDamage(40f);
            reportedCurrent.ShouldBe(60f, tolerance: 0.001f);
            reportedMax.ShouldBe(100f, tolerance: 0.001f);
        }

        [Test]
        public void TakeDamage_WhenDead_IsIgnored()
        {
            _health.TakeDamage(100f);
            _health.TakeDamage(50f);
            _health.Current.ShouldBe(0f);
        }

        [Test]
        public void TakeDamage_WhenDead_DoesNotFireOnDeathAgain()
        {
            int count = 0;
            _health.OnDeath += () => count++;
            _health.TakeDamage(100f);
            _health.TakeDamage(50f);
            count.ShouldBe(1);
        }

        [Test]
        public void TakeDamage_UpdatesFraction()
        {
            _health.TakeDamage(25f);
            _health.Fraction.ShouldBe(0.75f, tolerance: 0.001f);
        }

        // --- Heal ---

        [Test]
        public void Heal_IncreasesCurrentHealth()
        {
            _health.TakeDamage(50f);
            _health.Heal(20f);
            _health.Current.ShouldBe(70f, tolerance: 0.001f);
        }

        [Test]
        public void Heal_ClampsToMax()
        {
            _health.Heal(999f);
            _health.Current.ShouldBe(100f);
        }

        [Test]
        public void Heal_Zero_IsIgnored()
        {
            _health.TakeDamage(20f);
            _health.Heal(0f);
            _health.Current.ShouldBe(80f, tolerance: 0.001f);
        }

        [Test]
        public void Heal_Negative_IsIgnored()
        {
            _health.TakeDamage(20f);
            _health.Heal(-10f);
            _health.Current.ShouldBe(80f, tolerance: 0.001f);
        }

        [Test]
        public void Heal_WhenDead_IsIgnored()
        {
            _health.TakeDamage(100f);
            _health.Heal(50f);
            _health.Current.ShouldBe(0f);
        }

        [Test]
        public void Heal_FiresOnHealthChanged()
        {
            bool fired = false;
            _health.TakeDamage(30f);
            _health.OnHealthChanged += (c, m) => fired = true;
            _health.Heal(10f);
            fired.ShouldBeTrue();
        }

        // --- Tick / Regen ---

        [Test]
        public void Tick_RegeneratesHealth_UsingDeltaTime()
        {
            _health.TakeDamage(50f);     // 50
            _time.DeltaTime.Returns(1f);
            _health.Tick();              // +10/s * 1s = 60
            _health.Current.ShouldBe(60f, tolerance: 0.001f);
        }

        [Test]
        public void Tick_UsesDeltaTime_FromTimeProvider()
        {
            _health.TakeDamage(50f);
            _time.DeltaTime.Returns(0.5f);
            _health.Tick();              // +10 * 0.5 = 5 -> 55
            _health.Current.ShouldBe(55f, tolerance: 0.001f);
        }

        [Test]
        public void Tick_ReadsDeltaTime_OnEveryCall()
        {
            _health.TakeDamage(50f);
            _time.DeltaTime.Returns(1f);
            _health.Tick();              // 60
            _time.DeltaTime.Returns(2f);
            _health.Tick();              // +10*2 = 80
            _health.Current.ShouldBe(80f, tolerance: 0.001f);
        }

        [Test]
        public void Tick_ClampsToMax()
        {
            _time.DeltaTime.Returns(1000f);
            _health.Tick();
            _health.Current.ShouldBe(100f);
        }

        [Test]
        public void Tick_WhenDead_NoRegen()
        {
            _health.TakeDamage(100f);
            _time.DeltaTime.Returns(10f);
            _health.Tick();
            _health.Current.ShouldBe(0f);
        }

        [Test]
        public void Tick_WhenRegenIsZero_NoRegen()
        {
            var h = new HealthSystem(new PlayerData("p2", 100f, regenPerSecond: 0f), _time);
            h.TakeDamage(30f);
            _time.DeltaTime.Returns(100f);
            h.Tick();
            h.Current.ShouldBe(70f, tolerance: 0.001f);
        }

        [Test]
        public void Tick_CallsDeltaTime_OnTimeProvider()
        {
            _health.TakeDamage(10f);
            _health.Tick();
            _ = _time.Received().DeltaTime;
        }

        // --- Revive ---

        [Test]
        public void Revive_SetsIsAliveTrue()
        {
            _health.TakeDamage(100f);
            _health.Revive();
            _health.IsAlive.ShouldBeTrue();
        }

        [Test]
        public void Revive_Default_RestoresFullHealth()
        {
            _health.TakeDamage(100f);
            _health.Revive();
            _health.Current.ShouldBe(100f, tolerance: 0.001f);
        }

        [Test]
        public void Revive_WithFraction_RestoresPartialHealth()
        {
            _health.TakeDamage(100f);
            _health.Revive(0.5f);
            _health.Current.ShouldBe(50f, tolerance: 0.001f);
        }

        [Test]
        public void Revive_FractionClampedAboveZero()
        {
            _health.TakeDamage(100f);
            _health.Revive(0f);
            _health.Current.ShouldBeGreaterThan(0f);
        }

        [Test]
        public void Revive_FractionClampedBelowOne()
        {
            _health.TakeDamage(100f);
            _health.Revive(2f);
            _health.Current.ShouldBe(100f, tolerance: 0.001f);
        }

        [Test]
        public void Revive_FiresOnHealthChanged()
        {
            bool fired = false;
            _health.TakeDamage(100f);
            _health.OnHealthChanged += (c, m) => fired = true;
            _health.Revive();
            fired.ShouldBeTrue();
        }

        [Test]
        public void Revive_AllowsTakeDamage_AfterDeath()
        {
            _health.TakeDamage(100f);
            _health.Revive();
            _health.TakeDamage(30f);
            _health.Current.ShouldBe(70f, tolerance: 0.001f);
        }
    }
}
