namespace SqlDatabaseDump;

using System;

internal static class ThreadsafeWrite
{
	private static readonly Lock lockObject = new();

	/// <summary>
	/// Thread safe Console.WriteLine
	/// </summary>
	public static void Write(string s)
	{
		lock (lockObject) {
			Console.WriteLine(s);
		}
	}

	/// <summary>
	/// Thread safe Console.WriteLine for ReadOnlySpan
	/// </summary>
	public static void Write(ReadOnlySpan<char> span)
	{
		lock (lockObject) {
			Console.Out.WriteLine(span);
		}
	}
}
