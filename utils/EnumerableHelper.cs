using System;
using System.Collections.Generic;

namespace httpload.utils
{
	internal static class EnumerableHelper
	{
		public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
		{
			foreach(var item in enumerable)
				action(item);
		}

		public static IEnumerable<T> TakeLoopback<T>(this ICollection<T> collection, int take, int skip = 0)
		{
			if(collection == null || collection.Count == 0)
				throw new ArgumentException("Collection contains no elements", nameof(collection));
			skip = skip % collection.Count;
			while(take > 0)
			{
				foreach(var item in collection)
				{
					if(skip > 0)
					{
						skip--;
						continue;
					}
					yield return item;
					if(--take == 0) break;
				}
			}
		}
	}
}