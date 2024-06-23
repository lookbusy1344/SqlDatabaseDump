using System.Collections.Concurrent;

namespace SqlDatabaseDump;

internal static class Shared
{
	/// <summary>
	/// Threadsafe counter for the number of items in the queue
	/// </summary>
	public static SafeCounter QueueCounter { get; } = new();

	/// <summary>
	/// Maximum number of items in the queue
	/// </summary>
	public static SafeCounter MaxCounter { get; } = new();

	/// <summary>
	/// Number of items written to disk
	/// </summary>
	public static SafeCounter WrittenCounter { get; } = new();

	/// <summary>
	/// Objects that caused errors
	/// </summary>
	public static ConcurrentBag<string> ErrorObjects { get; } = [];
}
