using System.IO;
using System.Threading.Tasks;

namespace httpload.utils
{
	internal static class StreamHelper
	{
		public static async Task<long> ReadToNull(this Stream src)
		{
			int read;
			long totalRead = 0;
			while((read = await src.ReadAsync(ToNullReadBuffer, 0, ToNullReadBuffer.Length).ConfigureAwait(false)) > 0)
				totalRead += read;
			return totalRead;
		}

		private static readonly byte[] ToNullReadBuffer = new byte[65536];
	}
}