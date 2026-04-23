namespace Project.Runtime.Logic
{
	/// <summary>
	/// Abstracts Unity's Time class for headless testability.
	/// Inject UnityTimeProvider at runtime, FakeTimeProvider in tests.
	/// </summary>
	public interface ITimeProvider
	{
		float DeltaTime { get; }
		float Time { get; }
		float FixedDeltaTime { get; }
	}
}