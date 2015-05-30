using System;

namespace httpload.utils
{
	internal static class TimeHelper
	{
		public static long TicksToMs(this long value)
		{
			return value / TimeSpan.TicksPerMillisecond;
		}

		public static double TicksToMs(this double value)
		{
			return value / TimeSpan.TicksPerMillisecond;
		}

		public static long MsToTicks(this long value)
		{
			return value * TimeSpan.TicksPerMillisecond;
		}
	}
}