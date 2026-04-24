using NUnit.Framework;
using Project.Tests.Common;

namespace Project.Tests.UnitTests
{
    public abstract class TestBase
    {
        [SetUp]    public void OnStart() => TestOutputHelper.LogStart(GetType());
        [TearDown] public void OnEnd()   => TestOutputHelper.LogEnd();
    }
}
