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
			var need = 0;
			var done = 0;
			var threads = 0;
			var lockObj = new object();
			foreach(var item in enumerable)
			{
				var current = item;
				lock(lockObj)
				{
					need++;
					while(threads >= concurrency)
						Monitor.Wait(lockObj);
					threads++;
				}
				Task.Factory.StartNew(async () =>
				{
					await job(current);
					lock(lockObj)
					{
						done++;
						threads--;
						Monitor.Pulse(lockObj);
					}
				}, TaskCreationOptions.PreferFairness);
			}
			lock(lockObj)
			{
				while(need > done)
					Monitor.Wait(lockObj);
			}
		}
	}
}