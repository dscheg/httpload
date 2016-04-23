using System;
using System.Globalization;

namespace httpload.utils
{
	internal static class NumbersHelper
	{
		public static string ToStdString(this double value, int digits = 1, string suffix = null)
		{
			return value.ToString("0." + new string('0', digits), new NumberFormatInfo {NumberDecimalSeparator = "."}) + (suffix == null ? null : ' ' + suffix);
		}

		public static string ToPercentString(this double value)
		{
			return (Math.Truncate(value * 10000.0) / 10000.0).ToString("P", CultureInfo.InvariantCulture);
		}

		public static string ToPercentString(this int value)
		{
			return (value / 100.0).ToString("P0", CultureInfo.InvariantCulture);
		}

		public static string ToSmartString(this double value, int digits = 1, string suffix = null)
		{
			if(value > 1000000000.0)
				return (value / 1000000000.0).ToStdString(digits, 'G' + suffix);
			if(value > 1000000.0)
				return (value / 1000000.0).ToStdString(digits, 'M' + suffix);
			if(value > 1000.0)
				return (value / 1000.0).ToStdString(digits, 'k' + suffix);
			return value.ToStdString(0, suffix);
		}

		public static string ToSmartString(this long value, int digits = 1, string suffix = null)
		{
			return ((double)value).ToSmartString(digits, suffix);
		}
	}
}