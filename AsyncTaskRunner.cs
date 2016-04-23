using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace httpload
{
	internal static class AsyncTaskRunner
	{
		public static void Run<T>(IEnumerable<T> enumerable, Func<T, Task> job, int concurrency)
		{
			using(var semaphore = new SemaphoreSlim(concurrency, concurrency))
			{
				foreach(var item in enumerable)
				{
					semaphore.Wait();
					var current = item;
					Task.Run(async () =>
					{
						await job(current);
						semaphore.Release();
					});
				}
				for(int i = 0; i < concurrency; i++)
					semaphore.Wait();
			}
		}
	}
}