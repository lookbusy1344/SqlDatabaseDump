using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlDatabaseDump;

internal static class ThreadsafeWrite
{
	private static readonly object lockObject = new();

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
