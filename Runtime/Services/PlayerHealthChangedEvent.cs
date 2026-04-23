namespace Project.Runtime.Services
{
	/// <summary>
	/// Domain event fired when player health changes.
	/// Pure C# struct, no UnityEngine dependency.
	/// </summary>
	public readonly struct PlayerHealthChangedEvent
	{
		public readonly string PlayerId;
		public readonly float Current;
		public readonly float Max;
		public readonly float Fraction;

		public PlayerHealthChangedEvent(string playerId, float current, float max)
		{
			PlayerId = playerId;
			Current = current;
			Max = max;
			Fraction = max > 0f ? current / max : 0f;
		}
	}
}