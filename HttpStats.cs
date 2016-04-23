using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using httpload.utils;

namespace httpload
{
	internal static class HttpStats
	{
		public static void Init(int count)
		{
			QuantileSample = new long[count > 100000 ? 10000 : count];
			QuantileMod = count / QuantileSample.Length;
			Start = DateTime.UtcNow;
		}

		public static long Update(HttpResult result)
		{
			var ticks = result.ElapsedTicks;

			Interlocked.Add(ref TotalBytes, result.BytesReceived);
			var index = Interlocked.Increment(ref TotalCount) - 1;

			if(index % QuantileMod == 0)
				QuantileSample[index / QuantileMod % QuantileSample.Length] = ticks;

			long min;
			while((min = Interlocked.Read(ref MinTime)) > ticks && Interlocked.CompareExchange(ref MinTime, ticks, min) != min);

			long max;
			while((max = Interlocked.Read(ref MaxTime)) < ticks && Interlocked.CompareExchange(ref MaxTime, ticks, max) != max);

			HttpCodes.AddOrUpdate(result.StatusCode, 1, (code, count) => count + 1);

			var idx = Array.BinarySearch(Marks, ticks);
			if(idx < 0) idx = ~idx;

			Interlocked.Add(ref TotalTimes[idx], ticks);
			Interlocked.Increment(ref TotalCounts[idx]);

			return index;
		}

		public static void Write(TextWriter writer)
		{
			TotalTime = TotalTimes.Sum();

			var elapsed = DateTime.UtcNow - Start;
			var avg = TotalTime / TotalCount;

			Array.Sort(QuantileSample);

			writer.WriteLine("Requests/sec:    {0}", (TotalCount / (elapsed.TotalSeconds + 0.000001)).ToStdString());
			writer.WriteLine("Min time:        {0} ms", MinTime.TicksToMs());
			writer.WriteLine("Max time:        {0} ms", MaxTime.TicksToMs());
			writer.WriteLine("Avg time:        {0} ms", avg.TicksToMs());
			writer.WriteLine("Median time:     {0} ms", QuantileSample[QuantileSample.Length >> 1].TicksToMs());
			writer.WriteLine("Std deviation:   {0}", Math.Sqrt((double)QuantileSample.Sum(time => (time - avg) * (time - avg)) / QuantileSample.Length).TicksToMs().ToStdString(suffix: "ms"));
			writer.WriteLine();
			writer.WriteLine("Time taken:      {0}", elapsed.TotalSeconds.ToStdString(suffix: "sec"));
			writer.WriteLine("Data received:   {0}", TotalBytes.ToSmartString(suffix: "B"));
			writer.WriteLine("Transfer rate:   {0}", (TotalBytes / (elapsed.TotalSeconds + 0.000001)).ToSmartString(suffix: "B/sec"));
			writer.WriteLine();

			writer.WriteLine("=== http status codes ===");
			HttpCodes.OrderBy(pair => pair.Key).ForEach(pair => writer.WriteLine("{0,10}\t{1,-10}\t{2}", pair.Value, pair.Key == 0 ? "Unknown error" : ((int)pair.Key).ToString(), ((double)pair.Value / TotalCount).ToPercentString()));
			writer.WriteLine();

			writer.WriteLine("=== quantiles ===");
			for(int i = 0; i < Quantiles.Length; i++)
			{
				var quantile = Quantiles[i];
				writer.WriteLine("{0,10}\t{1} ms", quantile.ToPercentString(), QuantileSample[Math.Max(1, quantile * QuantileSample.Length / 100) - 1].TicksToMs());
			}
			writer.WriteLine();

			long sum = 0;
			writer.WriteLine("=== times ===");
			for(int i = 0; i < Marks.Length; i++)
			{
				sum += TotalCounts[i];
				writer.WriteLine("{0,10}\t{1,-10}\t{2}", TotalCounts[i], $"<{(Marks[i] == long.MaxValue ? "inf" : Marks[i].TicksToMs().ToString())} ms", ((double)sum / TotalCount).ToPercentString());
			}
			writer.WriteLine("----------");
			writer.WriteLine("{0,10}\t{1,-10}\t{2}", TotalCount, $"<{MaxTime.TicksToMs()} ms", 1.0.ToPercentString());
		}

		public static void Zero()
		{
			TotalCount = 0;
			TotalTime = 0;
			TotalBytes = 0;

			for(int i = 0; i < Marks.Length; i++)
			{
				TotalCounts[i] = 0;
				TotalTimes[i] = 0;
				MinTime = long.MaxValue;
				MaxTime = 0;
			}

			for(int i = 0; i < QuantileSample.Length; i++)
				QuantileSample[i] = 0;

			HttpCodes = new ConcurrentDictionary<HttpStatusCode, long>();

			Start = DateTime.UtcNow;
		}

		private static DateTime Start;
		private static long TotalCount;
		private static long TotalBytes;
		private static long TotalTime;
		private static long MinTime;
		private static long MaxTime;

		private static readonly long[] Marks =
		{
			1L.MsToTicks(),
			5L.MsToTicks(),
			10L.MsToTicks(),
			20L.MsToTicks(),
			30L.MsToTicks(),
			50L.MsToTicks(),
			100L.MsToTicks(),
			200L.MsToTicks(),
			300L.MsToTicks(),
			500L.MsToTicks(),
			1000L.MsToTicks(),
			2000L.MsToTicks(),
			3000L.MsToTicks(),
			5000L.MsToTicks(),
			10000L.MsToTicks(),
			30000L.MsToTicks(),
			60000L.MsToTicks(),
			long.MaxValue
		};

		private static readonly long[] TotalCounts = new long[Marks.Length];
		private static readonly long[] TotalTimes = new long[Marks.Length];

		private static readonly int[] Quantiles =
		{
			50,
			75,
			90,
			95,
			98,
			99,
			100
		};

		private static long[] QuantileSample;
		private static int QuantileMod;

		private static ConcurrentDictionary<HttpStatusCode, long> HttpCodes = new ConcurrentDictionary<HttpStatusCode, long>();
	}
}