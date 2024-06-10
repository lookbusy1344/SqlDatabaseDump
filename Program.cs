using PicoArgs_dotnet;
using System;

namespace SqlDatabaseDump;

// --instance (localdb)\MSSQLLocalDB --db DogTest --dir c:\dev\1

internal static class Program
{
	private const int DefaultMaxParallel = 8;

	private static void Main(string[] args)
	{
		AppDomain.CurrentDomain.UnhandledException += App_UnhandledException;

		var ver = GitVersion.VersionInfo.Get();
		Console.WriteLine($"SqlDatabaseDump.exe {ver.GetVersionHash(12)}");

		var pico = new PicoArgs(args);

		if (pico.Contains("-h", "--help", "-?")) {
			Console.WriteLine(CommandLineMessage);
			Environment.Exit(0);
		}

		var instance = pico.GetParamOpt("-i", "--instance") ?? Environment.GetEnvironmentVariable("DB_INSTANCE");
		var database = pico.GetParamOpt("-d", "--database") ?? Environment.GetEnvironmentVariable("DB_DATABASE");
		var dir = pico.GetParamOpt("-o", "--dir") ?? Environment.GetEnvironmentVariable("DB_DIR");
		var maxparallel = ParseOrDefault(pico.GetParamOpt("-p", "--parallel"), DefaultMaxParallel);
		var singlethread = pico.Contains("-s", "--singlethread");
		var replace = pico.Contains("-r", "--replace");

		pico.Finished();

		if (string.IsNullOrWhiteSpace(instance) || string.IsNullOrWhiteSpace(database) || string.IsNullOrWhiteSpace(dir) || maxparallel < 1 || maxparallel > 16) {
			Console.WriteLine(CommandLineMessage);
			Environment.Exit(1);
		}

		Console.WriteLine($"Dumping '{database}' from '{instance}' into '{dir}'");
		if (singlethread) {
			Console.WriteLine("Single thread processing");
		}
		if (replace) {
			Console.WriteLine("Replacing existing files");
		}

		dir = DumpDb.EnsurePathExists(dir);

		var stopwatch = System.Diagnostics.Stopwatch.StartNew();

		using var cancellationToken = new CancellationTokenSource();
		var types = Enum.GetValues(typeof(Scriptable)).Cast<Scriptable>().ToArray();

		if (singlethread) {
			// run in sequence
			foreach (var type in types) {
				Console.WriteLine($"Starting {type}...");
				var dumper = new DumpDb(instance, database, dir, replace, type, cancellationToken);
				dumper.Run();
			}
		} else {
			// run in parallel
			try {
				_ = Parallel.ForEach(types, new ParallelOptions { CancellationToken = cancellationToken.Token, MaxDegreeOfParallelism = maxparallel }, type => {
					ThreadsafeWrite.Write($"Starting {type}...");

					var dumper = new DumpDb(instance, database, dir, replace, type, cancellationToken);
					dumper.Run();

					ThreadsafeWrite.Write($"Finished {type}.");
				});
			}
			catch (AggregateException ae) {
				// handle exceptions from Parallel.ForEach, but ignore OperationCanceledException, they are just a side-effect
				foreach (var e in ae.InnerExceptions) {
					if (e != ae.InnerException && e is not OperationCanceledException) {
						// display significant inner exceptions
						Console.WriteLine($"INNER ERROR: {e.Message}");
					}
				}

				// rethrow either the first inner exception (true error) or the AggregateException as a fallback
				if (ae.InnerException != null) {
					throw ae.InnerException;
				} else {
					throw;
				}
			}
		}

		stopwatch.Stop();
		var seconds = Convert.ToDouble(stopwatch.ElapsedMilliseconds) / 1000.0;
		Console.WriteLine($"Execution Time: {seconds:f1} secs");
	}

	/// <summary>
	/// Global exception handler (for unhandled exceptions)
	/// </summary>
	private static void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		Console.WriteLine();
		if (e.ExceptionObject is Exception ex) {
			Console.WriteLine($"ERROR: {ex.Message}");
		} else {
			Console.WriteLine($"ERROR value: {0}", e.ExceptionObject?.ToString() ?? "?");
		}

		Console.WriteLine();
		Console.WriteLine(CommandLineMessage);
		Environment.Exit(1);
	}

	private static int ParseOrDefault(string? value, int defaultValue) => int.TryParse(value, out var result) ? result : defaultValue;

	private const string CommandLineMessage = """
		Usage: SqlDatabaseDump.exe --instance <instance> --database <db> --dir <dir>

		Required:
		  -i, --instance <instance>  SQL Server instance to connect to  (or DB_INSTANCE environment variable)
		  -d, --database <db>        Database to process                (or DB_DATABASE environment variable)
		  -o, --dir <dir>            Output directory                   (or DB_DIR environment variable)

		Options:
		  -r, --replace              Replace existing files (default is to fail if file exists)
		  -s, --singlethread         Single thread processing
		  -p, --parallel <n>         Maximum parallel tasks 1..16 (default is 8)
		  -h, --help, -?             Help information
		""";
}
