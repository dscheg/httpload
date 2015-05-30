using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using httpload.utils;
using NDesk.Options;

namespace httpload
{
	internal static class Program
	{
		private static void Main(string[] args)
		{
			try
			{
				var showHelpAndExit = false;

				Method = HttpMethod.Get;
				Headers = new NameValueCollection();

				int? warmCount = 0;
				int concurrency = 1;
				double rps = double.PositiveInfinity;
				string qparams = null, input = null, urlFormat = null;
				bool expect100Continue = false, useNagle = false;
				Timeout = 30000;

				var options = new OptionSet
				{
					{"n|count=", "Number of requests to perform", (int v) => Count = v},
					{"c|concurrency=", "Number of concurrent requests (default 1)", (int v) => concurrency = v > 0 ? v : concurrency},
					{"m|method=", "HTTP {verb} (default GET)", v => Method = new HttpMethod(v)},
					{
						"H|header=", "Additional header ({name:value})", v =>
						{
							var parts = v.Split(new[] {':'}, 2);
							if(string.IsNullOrWhiteSpace(parts[0])) throw new OptionException("Header name must not be empty", "header");
							Headers.Add(parts[0].Trim(), parts.Length > 1 ? parts[1] : string.Empty);
						}
					},
					{"q|qparams=", "Input {file} with query string params", v => qparams = v},
					{"i|input=", "Input {file} with data to POST or PUT", v => input = v},
					{"w|warmup:", "Warm-up requests (default 0)", (int? v) => warmCount = v == null || v >= 0 ? v : warmCount},
					{"rps=", "Requests per second limit (default +inf)", (double v) => rps = v > 0 ? v : rps},
					{"timeout=", "Requests timeout (default 30000 msec)", (int v) => Timeout = v > 0 ? v : Timeout},
					{"no-keep-alive", "Turn off keep alives", v => NoKeepAlive = v != null},
					{"100-continue", "Enable 100-Continue behavior for POST/PUT", v => expect100Continue = v != null},
					{"nagle", "Turn on Nagle algorithm", v => useNagle = v != null},
					{"debug", "Show debug info about requests", v => Debug = v != null},
					{"h|help", "Show this message", v => showHelpAndExit = v != null}
				};

				List<string> rest = null;
				try
				{
					rest = options.Parse(args);
				}
				catch(OptionException e)
				{
					Console.Error.WriteLine("Option '{0}' value is invalid: {1}", e.OptionName, e.Message);
					Console.Error.WriteLine();
					showHelpAndExit = true;
				}

				if(rest != null && rest.Count == 1)
					urlFormat = rest[0];

				if(showHelpAndExit || string.IsNullOrEmpty(urlFormat) || urlFormat == "-?" || urlFormat == "/?" || Count <= 0)
				{
					Console.WriteLine("Usage: httpload [OPTIONS] URL");
					Console.WriteLine("Options:");
					options.WriteOptionDescriptions(Console.Out);
					Console.WriteLine();
					Console.WriteLine("Examples:");
					Console.WriteLine("  httpload http://example.com");
					Console.WriteLine("  httpload -n100 -c8 --timeout=1000 http://example.com");
					Console.WriteLine("  httpload -n100 -mHEAD -q\"params.txt\" http://example.com");
					Console.WriteLine("  httpload -n100 -mPOST -i\"data.txt\" --100-continue http://example.com");
					Console.WriteLine("  httpload -n100 -H\"Authorization:Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ==\" -H\"Cookie:key=value\" http://example.com");
					Console.WriteLine("  httpload -n100 --rps=0.5 --debug http://example.com");
					Environment.Exit(0);
				}

				TimeToSleep = double.IsPositiveInfinity(rps) ? 0 : (long)(concurrency * 1000.0 / rps);
				if(warmCount == null) warmCount = Math.Min(Count, concurrency * 10);

				Console.WriteLine("URL format:      {0}", urlFormat);
				Console.WriteLine("Requests count:  {0}", Count);
				if(!string.IsNullOrEmpty(qparams))
					Console.WriteLine("Params file:     {0}", qparams);
				if(!string.IsNullOrEmpty(input))
					Console.WriteLine("Input data file: {0}", input);
				Console.WriteLine("Concurrency:     {0}", concurrency);
				Console.WriteLine("RPS Limit:       {0}", rps.ToString(CultureInfo.InvariantCulture));
				Console.WriteLine("KeepAlive:       {0}", !NoKeepAlive);
				Console.WriteLine("Use Nagle:       {0}", useNagle);
				Console.WriteLine();

				if(!string.IsNullOrEmpty(input))
					Data = File.ReadAllBytes(input);

				var urls = string.IsNullOrEmpty(qparams)
					? new[] {new Uri(urlFormat)}
					: File
						.ReadLines(qparams, Encoding.Default)
						.Where(line => !string.IsNullOrEmpty(line))
						.Select(line => new Uri(string.Format(urlFormat, line.Split('t').Select(HttpUtility.UrlEncode).ToArray())))
						.ToArray();

				if(urls.Length == 0)
				{
					Console.Error.WriteLine("QParams file is empty");
					return;
				}

				WebRequest.DefaultWebProxy = null;
				ServicePointManager.EnableDnsRoundRobin = true;
				ServicePointManager.DefaultConnectionLimit = int.MaxValue;
				ServicePointManager.UseNagleAlgorithm = useNagle;
				ServicePointManager.Expect100Continue = expect100Continue;
				ServicePointManager.CheckCertificateRevocationList = false;
				ServicePointManager.ServerCertificateValidationCallback = null;

				Jitter.LoadAssembliesAndJitMethods();
				Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
				GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

				AsyncHttpClient.TestCreateRequest(urls[0], Method, Headers, !NoKeepAlive);

				HttpStats.Init(Count);

				if(warmCount > 0)
				{
					Console.Error.WriteLine("Warming up ({0} requests)...", warmCount);
					AsyncTaskRunner.Run(urls.TakeLoopback(warmCount.Value), Run, concurrency);
					HttpStats.Zero();
				}

				LogCount = Count >= 100 ? Count / 10 : 0;

				Console.Error.WriteLine("Starting load test (be patient)...");
				AsyncTaskRunner.Run(urls.TakeLoopback(Count, warmCount.Value), Run, concurrency);
				Console.Error.WriteLine("Done");
				Console.Error.WriteLine();

				HttpStats.Write(Console.Out);
			}
			catch(Exception e)
			{
				Console.Error.WriteLine(e);
			}
		}

		private static async Task Run(Uri url)
		{
			var start = DateTime.UtcNow;
			var result = await AsyncHttpClient.DoRequestAsync(url, Method, Headers, Data, Timeout, !NoKeepAlive);
			if(Debug) Console.WriteLine("{0}\t{1,10}\t{2,-10}\t{3}", start.ToLocalTime().ToString("HH:mm:ss.fff"), (int)result.StatusCode, (result.ElapsedTicks.TicksToMs()) + " ms", url);
			var count = HttpStats.Update(result) + 1;
			if(LogCount > 0 && count % LogCount == 0)
				Console.Error.WriteLine("Completed {0}", count);
			if(TimeToSleep > 0 && count < Count)
			{
				var sleep = TimeToSleep - result.ElapsedTicks.TicksToMs();
				if(sleep > 0) await Task.Delay((int)sleep);
			}
		}

		private static int Count = 1;
		private static int LogCount;
		private static bool Debug;
		private static bool NoKeepAlive;
		private static long TimeToSleep;
		private static HttpMethod Method;
		private static NameValueCollection Headers;
		private static byte[] Data;
		private static int Timeout;
	}
}