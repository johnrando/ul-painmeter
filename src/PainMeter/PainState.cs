using UnityEngine;

namespace PainMeter
{
	/// <summary>
	/// The two numbers the game keeps per entity, read once and turned into what the meter draws.
	/// Nothing here writes to the entity.
	///
	/// <c>painResistPercent</c> runs 0 to 3: each pain hit adds the entity class's
	/// <c>PainResistPerHit</c> (0.55 for a normal zombie, 0.7 feral, 0.9 radiated) and it decays
	/// 0.01 per tick in <c>EntityAlive.OnUpdateEntity</c> - 0.2 a second, on clients as well as
	/// the server. <c>EntityAlive.IsAttackValid</c> lets an entity at 1 or more attack straight
	/// through a hit; below 1 every pain hit sets <c>hasBeenAttackedTime</c> to 10 ticks, during
	/// which it cannot attack and moves at a tenth speed. Undead Legacy re-implements the damage
	/// response but keeps both rules as they are.
	/// </summary>
	internal readonly struct PainState
	{
		/// <summary>The point at which hits stop staggering the entity.</summary>
		internal const float Threshold = 1f;

		/// <summary>0.01 per tick at 20 ticks a second.</summary>
		internal const float DecayPerSecond = 0.2f;

		/// <summary>Cap in <c>EntityAlive.ProcessDamageResponseLocal</c>, so the longest wait is 10 s.</summary>
		internal const float MaxPain = 3f;

		internal const float MaxSecondsOver = (MaxPain - Threshold) / DecayPerSecond;

		internal readonly float Pain;

		/// <summary>What one pain hit adds for this entity's class; below zero means it never
		/// takes pain (<c>PainResistPerHit</c> of -1).</summary>
		internal readonly float PerHit;

		/// <summary>0 to 1: how full the meter draws.</summary>
		internal readonly float Fill;

		/// <summary>At or past the threshold: the entity attacks through hits.</summary>
		internal readonly bool OverThreshold;

		/// <summary>Seconds of decay until the meter is back below the threshold; 0 when under it.</summary>
		internal readonly float SecondsUntilBelow;

		/// <summary>Inside the 0.5 s window after a pain hit in which the entity cannot attack.</summary>
		internal readonly bool Staggered;

		/// <summary>Nothing to show: no pain and no lockout.</summary>
		internal readonly bool IsZero;

		private PainState(float _pain, int _attackedTicks, float _perHit)
		{
			Pain = _pain;
			PerHit = _perHit;
			Fill = Mathf.Clamp01(_pain / Threshold);
			OverThreshold = _pain >= Threshold;
			SecondsUntilBelow = OverThreshold ? (_pain - Threshold) / DecayPerSecond : 0f;
			Staggered = _attackedTicks > 0 && !OverThreshold;
			IsZero = _pain <= 0f && _attackedTicks <= 0;
		}

		internal static PainState Read(EntityAlive _entity)
		{
			float perHit = -1f;
			if (EntityClass.list.TryGetValue(_entity.entityClass, out EntityClass entityClass))
			{
				perHit = entityClass.PainResistPerHit;
			}
			return new PainState(_entity.painResistPercent, _entity.hasBeenAttackedTime, perHit);
		}

		/// <summary>Pain hits it takes to reach <paramref name="_level"/> from empty, or 0 when
		/// the class never takes pain.</summary>
		internal int HitsTo(float _level)
		{
			return PerHit > 0f ? Mathf.CeilToInt(_level / PerHit - 0.0001f) : 0;
		}

		/// <summary>The fill tint for this level, blended between the two configured colours.</summary>
		internal Color Colour(float _opacity)
		{
			return BarColour.Lerp(Settings.ColourLow, Settings.ColourHigh, Fill, _opacity);
		}
	}
}
