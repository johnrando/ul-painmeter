using System.Collections.Generic;

namespace PainMeter
{
	/// <summary>
	/// Live counters behind <c>pm info</c>: the startup log proves the patches were installed,
	/// these prove the frame hooks are being reached and that zombies in pain are being seen.
	/// No locking: all writes happen on the main thread.
	/// </summary>
	internal static class Counters
	{
		/// <summary>Frames the overhead hook ran with an overhead mode on.</summary>
		internal static int FramesRendered;

		/// <summary>Overhead bars drawn in the last frame.</summary>
		internal static int OverheadLive;

		internal static int WidgetsCreated;

		internal static int PeakLive;

		/// <summary>Distinct entities seen with a non-zero meter.</summary>
		internal static readonly HashSet<int> SeenInPain = new HashSet<int>();

		internal static float HighestPain;

		/// <summary>Entities seen crossing the threshold, counted once per crossing.</summary>
		internal static int CrossedThreshold;

		private static readonly HashSet<int> overThreshold = new HashSet<int>();

		internal static int BarRowUpdates;

		/// <summary>What the health-bar row is doing right now, for <c>pm info</c>.</summary>
		internal static string BarRowShowing = "hidden";

		/// <summary>Called for every entity a renderer reads, whether or not it ends up drawn.</summary>
		internal static void TrackSeen(int _entityId, in PainState _state)
		{
			if (_state.IsZero)
			{
				overThreshold.Remove(_entityId);
				return;
			}
			SeenInPain.Add(_entityId);
			if (_state.Pain > HighestPain)
			{
				HighestPain = _state.Pain;
			}
			if (_state.OverThreshold)
			{
				if (overThreshold.Add(_entityId))
				{
					CrossedThreshold++;
				}
			}
			else
			{
				overThreshold.Remove(_entityId);
			}
		}

		internal static void Reset()
		{
			FramesRendered = 0;
			OverheadLive = 0;
			WidgetsCreated = 0;
			PeakLive = 0;
			SeenInPain.Clear();
			HighestPain = 0f;
			CrossedThreshold = 0;
			overThreshold.Clear();
			BarRowUpdates = 0;
		}
	}
}
