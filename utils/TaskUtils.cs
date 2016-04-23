using System;
using System.Threading;
using System.Threading.Tasks;

namespace httpload.utils
{
	internal static class TaskUtils
	{
		public static Task<T> WithTimeout<T>(this Task<T> task, int timeout, T defaultValue = default(T))
		{
			if(task.IsCompleted || timeout == Timeout.Infinite)
				return task;

			var source = new TaskCompletionSource<T>();

			if(timeout == 0)
			{
				source.SetException(new TimeoutException());
				return source.Task;
			}

			var timer = new Timer(state => source.TrySetResult(defaultValue), null, timeout, Timeout.Infinite);

			task.ContinueWith(t =>
			{
				timer.Dispose();
				switch(task.Status)
				{
					case TaskStatus.Faulted:
						source.TrySetException(task.Exception);
						break;
					case TaskStatus.Canceled:
						source.TrySetCanceled();
						break;
					case TaskStatus.RanToCompletion:
						source.TrySetResult(task.Result);
						break;
				}
			}, TaskContinuationOptions.ExecuteSynchronously);

			return source.Task;
		}
	}
}