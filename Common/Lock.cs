using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Thue.Common;

public class Locking
{
	private static readonly ConcurrentDictionary<string, SemaphoreSlim> locks = new();
	public static int Release([CallerMemberName] string key = "")
	{
		try
		{
			return Get(key).Release();
		}
		catch { }
		return -1;
	}
	public static void Wait([CallerMemberName] string key = "") => Get(key).Wait();
	public static Task WaitAsync(CancellationToken cancellationToken, [CallerMemberName] string key = "") => Get(key).WaitAsync(cancellationToken);
	public static Task WaitAsync(string key, CancellationToken cancellationToken) => Get(key).WaitAsync(cancellationToken);
	static SemaphoreSlim Get(string key) => locks.GetOrAdd(key, k => new(1, 1));
}
