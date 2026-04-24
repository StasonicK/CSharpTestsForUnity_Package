# CSharpTestToolForUnity

Headless C# test runner for Unity projects.
Runs pure C# unit and integration tests via `dotnet test` - no Unity Editor required, no Play Mode.

**Stack:** NUnit 4 - NSubstitute 5 - Shouldly 4 - .NET 9

---

## Folder Structure

```
CSharpTestToolForUnity/
  package.json                  - UPM package descriptor
  CSharpTestToolForUnity.csproj - test project
  CSharpTestToolForUnity.sln    - standalone solution for Rider / VS
  test.cmd                      - CMD runner (all logic here)
  test.ps1                      - PowerShell runner (optional fallback)
  test.runsettings              - NUnit/VSTest settings
  README.md

  Editor/                       - Unity Editor menu (CPM RP Tools > CSharp Test Tool)
    CSharpTestToolMenu.cs
    CSharpTestToolForUnity.Editor.asmdef

  Runtime/                      - example pure-C# scripts (no UnityEngine)
  Tests/
    Common/TestOutputHelper.cs
    Integration/TestWorld.cs, IntegrationTestBase.cs, ...
    UnitTests/TestBase.cs, HealthSystemTests.cs, ...
  Examples/
    SubstituteExamples.cs       - NSubstitute patterns: mock, stub, spy
```

---

## Setup

### 1. Install via UPM

Add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.ogames.csharptesttoolforunity": "https://github.com/yourname/CSharpTestToolForUnity.git#0.0.9"
  }
}
```

Or via Unity Editor: `Window > Package Manager > + > Add package from git URL`.

### 2. Open a terminal in the package folder

**Option A - Unity Editor menu (recommended):**
```
CPM RP Tools > CSharp Test Tool > Open Terminal Here
```
Opens CMD directly in the package folder. Type `test all` immediately.

**Option B - manually:**
```cmd
cd "C:\Unity projects\YourProject\Library\PackageCache\com.ogames.csharptesttoolforunity@<hash>"
test examples
```

### 3. Restore NuGet packages (first time only)

```cmd
dotnet restore
```

---

## Commands

| Command | Description |
|---|---|
| `test all` | Run your project tests (excludes built-in package tests) |
| `test examples` | Run all built-in package tests (unit + integration + NSubstitute) |
| `test <Folder>` | Run all tests in a folder |
| `test <Script.cs>` | Run tests in a single script (`.cs` required) |
| `test info` | Show help |

No setup or registration needed. CMD finds `test.cmd` in the current folder automatically.

---

## Coverage Flag

Add `coverage` anywhere in the command to collect code coverage,
generate an HTML report and open it in the browser.

```cmd
test examples coverage
test all coverage
test Tests\UnitTests\HealthSystemTests.cs coverage
test Tests\Integration coverage
test coverage examples
```

The `coverage` word is detected as a standalone token - safe inside path names.

Reports are saved to:
- `coverage-examples\html\index.html` - for package example tests
- `coverage\html\index.html` - for your project tests

Requires `reportgenerator` (installed automatically on first use):
```cmd
dotnet tool install -g dotnet-reportgenerator-globaltool
```

---

## Path Rules

Paths are relative to the tool folder. Both `\` and `/` accepted. Case-insensitive.

**Folder** - no `.cs` extension:
```cmd
test Tests
test Tests\Integration
test Tests\UnitTests
```

**Script** - `.cs` extension required:
```cmd
test Tests\UnitTests\HealthSystemTests.cs
test Tests\Integration\CombatIntegrationTests.cs
test Examples\SubstituteExamples.cs
```

Filename-only search (no path needed if unique):
```cmd
test HealthSystemTests.cs
```

---

## Test Output Format

```
  ---- HealthSystemTests ----
  >> TakeDamage_ReducesCurrentHealth
     PASSED

  ---- CombatIntegrationTests ----
  >> Death_TracksAnalyticsEvent
     PASSED
  >> Death_PersistsTimeOfDeath_FromTimeProvider
     FAILED
     Expected : 42.5
     Actual   : 0
```

---

## Unity Editor Menu

```
CPM RP Tools
  └── CSharp Test Tool
        ├── Open Terminal Here    opens CMD in the package folder
        ├── Run Examples          opens CMD and runs 'test examples'
        └── Show Help             opens CMD and runs 'test info'
```

The menu finds the package path automatically - works whether installed from
`Packages/` folder or `Library/PackageCache/` (git/registry install with `@hash` suffix).

---

## Test Types

### Unit tests - `Tests/UnitTests/`

Pure C# logic. No Unity engine. Sub-millisecond per test.

```csharp
[TestFixture, Category("Unit")]
public class HealthSystemTests : TestBase
{
    private ITimeProvider _time   = null!;
    private HealthSystem  _health = null!;

    [SetUp]
    public void Setup()
    {
        _time   = Substitute.For<ITimeProvider>();
        _time.DeltaTime.Returns(0.016f);
        _health = new HealthSystem(new PlayerData("p1", 100f), _time);
    }

    [Test]
    public void TakeDamage_ReducesHealth()
    {
        _health.TakeDamage(30f);
        _health.Current.ShouldBe(70f, tolerance: 0.001f);
    }
}
```

### Integration tests - `Tests/Integration/`

Multiple systems wired together. Real logic + NSubstitute mocks.

```csharp
[TestFixture, Category("Integration")]
public class CombatIntegrationTests : IntegrationTestBase
{
    [Test]
    public void Death_TracksAnalyticsEvent()
    {
        World.Combat.TakeDamage(100f);
        World.Analytics.Received(1).TrackEvent("player_died");
    }
}
```

`IntegrationTestBase` creates a `TestWorld` per test:

| Property | Type | Role |
|---|---|---|
| `Time` | `ITimeProvider` | Stub |
| `Analytics` | `IAnalyticsService` | Mock - verify with `.Received()` |
| `Persistence` | `IPersistenceService` | Mock / stub |
| `DiedBus` | `IEventBus<PlayerDiedEvent>` | Isolated instance |
| `HealthChangedBus` | `IEventBus<PlayerHealthChangedEvent>` | Isolated instance |
| `Combat` | `CombatSystem` | Real implementation |

---

## NSubstitute Patterns

```csharp
// Mock
analytics.Received(1).TrackEvent("player_died");
analytics.DidNotReceive().TrackEvent("level_up");

// Stub
persistence.Exists("key").Returns(true);
persistence.Load<float>("key").Returns(99f);

// Throw
persistence.When(p => p.Save(Arg.Any<string>(), Arg.Any<object>()))
           .Do(_ => throw new IOException("Disk full"));

// Arg matchers
analytics.Received(1).TrackEvent(Arg.Any<string>());
analytics.Received(1).TrackEvent(Arg.Is<string>(s => s.StartsWith("player")));

// Capture
string? captured = null;
analytics.When(a => a.TrackEvent(Arg.Any<string>()))
         .Do(ci => captured = ci.Arg<string>());
```

---

## Shouldly Assertions

```csharp
value.ShouldBe(expected);
value.ShouldBe(0.75f, tolerance: 0.001f);
value.ShouldNotBeNull();
value.ShouldBeTrue();
value.ShouldBeGreaterThan(0f);
collection.ShouldBe(new[] { "a", "b" }, ignoreOrder: false);
Should.Throw<IOException>(() => risky());
Should.NotThrow(() => safe());
```

---

## Adding Your Project Scripts

Open `CSharpTestToolForUnity.csproj` and uncomment:

```xml
<ItemGroup>
  <Compile Include="..\Assets\Scripts\Runtime\**\*.cs" LinkBase="ProjectRuntime" />
  <Compile Remove="..\Assets\Scripts\Runtime\Views\**\*.cs" />
</ItemGroup>
```

Adjust the path to your project's pure C# scripts.
Use a different namespace (e.g. `MyGame.Tests`) so `test all` runs your tests
without touching the built-in package tests.

---

## Rider / Visual Studio

The package includes `Editor/CSharpTestToolSolutionHook.cs` which injects
`CSharpTestToolForUnity.csproj` into the Unity solution on every recompile.
Open the Unity solution in Rider - `CSharpTestToolForUnity` appears as a project.

To open the tool in isolation:
```
File > Open > CSharpTestToolForUnity.sln
```
