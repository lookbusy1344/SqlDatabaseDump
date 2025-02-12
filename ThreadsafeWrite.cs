using System;
using System.Threading.Tasks;

namespace SqlDatabaseDump;

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
}
