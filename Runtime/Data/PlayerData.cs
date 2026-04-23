namespace Project.Runtime.Data
{
	/// <summary>
	/// Immutable snapshot of player stats. Pure C# — no UnityEngine dependency.
	/// Passed by value; modify via HealthSystem, not directly.
	/// </summary>
	public readonly struct PlayerData
	{
		public readonly string PlayerId;
		public readonly float MaxHealth;
		public readonly float RegenPerSecond;

		public PlayerData(string playerId, float maxHealth, float regenPerSecond = 0f)
		{
			PlayerId = playerId;
			MaxHealth = maxHealth;
			RegenPerSecond = regenPerSecond;
		}

		public static PlayerData Default => new PlayerData("player_1", maxHealth: 100f, regenPerSecond: 5f);
	}
}