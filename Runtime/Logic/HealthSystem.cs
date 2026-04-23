using System;
using Project.Runtime.Data;


namespace Project.Runtime.Logic
{
	/// <summary>
	/// Manages player health: damage, healing, regeneration, death.
	/// Pure C# — no UnityEngine dependency. Fully headless-testable.
	/// Depends on ITimeProvider for regen ticks.
	/// </summary>
	public class HealthSystem
	{
		public event Action<float, float>? OnHealthChanged; // (current, max)
		public event Action? OnDeath;

		private readonly PlayerData _data;
		private readonly ITimeProvider _time;

		private float _current;
		private bool _isDead;

		public float Current => _current;
		public float Max => _data.MaxHealth;
		public bool IsAlive => !_isDead;
		public float Fraction => _data.MaxHealth > 0f ? _current / _data.MaxHealth : 0f;

		public HealthSystem(PlayerData data, ITimeProvider time)
		{
			_data = data;
			_time = time;
			_current = data.MaxHealth;
		}

		/// <summary>Call once per frame (or fixed update) to process regeneration.</summary>
		public void Tick()
		{
			if (_isDead || _data.RegenPerSecond <= 0f)
				return;

			Heal(_data.RegenPerSecond * _time.DeltaTime);
		}

		public void TakeDamage(float amount)
		{
			if (_isDead || amount <= 0f)
				return;

			_current = Math.Max(0f, _current - amount);
			OnHealthChanged?.Invoke(_current, Max);

			if (_current <= 0f)
			{
				_isDead = true;
				OnDeath?.Invoke();
			}
		}

		public void Heal(float amount)
		{
			if (_isDead || amount <= 0f)
				return;

			_current = Math.Min(Max, _current + amount);
			OnHealthChanged?.Invoke(_current, Max);
		}

		public void Revive(float healthFraction = 1f)
		{
			_isDead = false;
			_current = Max * Math.Clamp(healthFraction, 0.01f, 1f);
			OnHealthChanged?.Invoke(_current, Max);
		}
	}
}