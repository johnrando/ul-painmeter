using System.Globalization;
using UnityEngine;

namespace PainMeter
{
	/// <summary>Colour blending and the <c>r,g,b</c> text form shared by the settings file and <c>pm colour</c>.</summary>
	internal static class BarColour
	{
		/// <summary>Straight blend from <paramref name="_low"/> to <paramref name="_high"/>, with the given alpha.</summary>
		internal static Color Lerp(Color32 _low, Color32 _high, float _t, float _alpha)
		{
			Color c = Color.Lerp(_low, _high, Mathf.Clamp01(_t));
			c.a = Mathf.Clamp01(_alpha);
			return c;
		}

		/// <summary>"r,g,b", each 0-255. Alpha is always full; opacity is a separate setting.</summary>
		internal static bool TryParse(string _value, out Color32 _colour)
		{
			_colour = default;
			if (_value == null)
			{
				return false;
			}
			string[] parts = _value.Split(',');
			if (parts.Length != 3)
			{
				return false;
			}
			byte[] channels = new byte[3];
			for (int i = 0; i < 3; i++)
			{
				if (!int.TryParse(parts[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int channel)
					|| channel < 0 || channel > 255)
				{
					return false;
				}
				channels[i] = (byte)channel;
			}
			_colour = new Color32(channels[0], channels[1], channels[2], 255);
			return true;
		}

		/// <summary>Written the way it is parsed, so a reported value can be typed back in.</summary>
		internal static string Format(Color32 _colour)
		{
			return _colour.r + "," + _colour.g + "," + _colour.b;
		}
	}
}
