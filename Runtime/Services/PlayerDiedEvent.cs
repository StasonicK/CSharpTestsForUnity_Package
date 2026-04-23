namespace Project.Runtime.Services
{
	/// <summary>
	/// Domain event fired when a player dies.
	/// Pure C# struct, no UnityEngine dependency.
	/// </summary>
	public readonly struct PlayerDiedEvent
	{
		public readonly string PlayerId;

		public PlayerDiedEvent(string playerId) => PlayerId = playerId;
	}
}