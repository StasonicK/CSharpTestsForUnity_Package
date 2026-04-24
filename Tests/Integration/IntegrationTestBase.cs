using NUnit.Framework;
using Project.Tests.Common;

namespace Project.Tests.Integration
{
    public abstract class IntegrationTestBase
    {
        protected TestWorld World { get; private set; } = null!;

        [SetUp]
        public virtual void SetUp()
        {
            World = BuildWorld();
            TestOutputHelper.LogStart(GetType());
        }

        [TearDown]
        public virtual void TearDown()
        {
            try    { TestOutputHelper.LogEnd(); }
            catch  { }
            finally { try { World?.Dispose(); } catch { } }
        }

        protected virtual TestWorld BuildWorld() => new TestWorld();
    }
}
