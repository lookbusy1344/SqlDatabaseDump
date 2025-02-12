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

		var config = BuildConfig(args);

		Console.WriteLine($"Dumping '{config.DatabaseName}' from '{config.InstanceName}' into '{config.OutputDirectory}'");
		if (config.SingleThread) {
			Console.WriteLine("Single thread processing");
		}
		if (config.ReplaceExistingFiles) {
			Console.WriteLine("Replacing existing files");
		}
		if (config.ExtendedProperties) {
			Console.WriteLine("Including extended properties");
			Shared.WithExtendedProperties();
		}
		if (config.WithDependencies) {
			Console.WriteLine("Including dependencies");
			Shared.WithDependencies();
		}

		var stopwatch = System.Diagnostics.Stopwatch.StartNew();

		using var cancellationToken = new CancellationTokenSource();
		var types = Enum.GetValues<Scriptable>();

		if (config.SingleThread || config.MaxParallel == 1) {
			// run in sequence
			SequentialProcess(types, config, cancellationToken);
		} else {
			// run in parallel
			ParallelProcess(types, config, cancellationToken);
		}

		if (!config.SkipErrors) {
			WriteAnyErrors(config);
		}

		stopwatch.Stop();
		var seconds = Convert.ToDouble(stopwatch.ElapsedMilliseconds) / 1000.0;

		Console.WriteLine($"Items found: {Shared.MaxCounter.Value}, files written: {Shared.WrittenCounter.Value}, errors: {Shared.ErrorObjects.Count}, remaining: {Shared.QueueCounter.Value}");
		Console.WriteLine($"Execution Time: {seconds:f1} secs");
	}

	private static void SequentialProcess(IEnumerable<Scriptable> types, Config config, CancellationTokenSource cancellationToken)
	{
		foreach (var type in types) {
			Console.WriteLine($"Starting {type}...");
			var dumper = new DumpDb(config, type, cancellationToken);
			dumper.Run();
		}
	}

	private static void ParallelProcess(IEnumerable<Scriptable> types, Config config, CancellationTokenSource cancellationToken)
	{
		try {
			_ = Parallel.ForEach(types, new ParallelOptions { CancellationToken = cancellationToken.Token, MaxDegreeOfParallelism = config.MaxParallel }, type => {
				ThreadsafeWrite.Write($"Starting {type}...");

				var dumper = new DumpDb(config, type, cancellationToken);
				dumper.Run();

				ThreadsafeWrite.Write($"Finished {type}.");
			});
		}
		catch (AggregateException ae) {
			// handle exceptions from Parallel.ForEach, but ignore OperationCancelledException, they are just a side-effect
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

	private static Config BuildConfig(string[] args)
	{
		var pico = new PicoArgs(args);

		// handle help
		if (pico.Contains("-h", "--help", "-?")) {
			Console.WriteLine(CommandLineMessage);
			Environment.Exit(0);
		}

		// parse command line parameters
		var instance = pico.GetParamOpt("-i", "--instance") ?? Environment.GetEnvironmentVariable("DB_INSTANCE");
		var database = pico.GetParamOpt("-d", "--database") ?? Environment.GetEnvironmentVariable("DB_DATABASE");
		var dir = pico.GetParamOpt("-o", "--dir") ?? Environment.GetEnvironmentVariable("DB_DIR");
		var maxParallel = ParseOrDefault(pico.GetParamOpt("-p", "--parallel"), DefaultMaxParallel);

		var allExtras = pico.Contains("-a", "--all");
		var extendedProperties = pico.Contains("-e", "--extended-properties");
		var withDependencies = pico.Contains("-w", "--with-dependencies");

		var singleThread = pico.Contains("-s", "--single-thread");
		var replace = pico.Contains("-r", "--replace");
		var skipErrors = pico.Contains("-k", "--skip-errors");

		pico.Finished();

		// ensure required parameters are present
		if (string.IsNullOrWhiteSpace(instance) || string.IsNullOrWhiteSpace(database) || string.IsNullOrWhiteSpace(dir) || maxParallel < 1 || maxParallel > 16) {
			Console.WriteLine(CommandLineMessage);
			Environment.Exit(1);
		}

		dir = DumpDb.EnsurePathExists(dir);

		return new Config(instance, database, dir, maxParallel,
			singleThread, replace, skipErrors, extendedProperties || allExtras, withDependencies || allExtras);
	}

	private static void WriteAnyErrors(Config config)
	{
		if (Shared.ErrorObjects.IsEmpty) {
			return;
		}

		// sort the concurrent bag
		IReadOnlyList<string> items = [.. Shared.ErrorObjects.AsEnumerable().Order()];

		var filename = $"{config.OutputDirectory}{config.DatabaseName}-Errors.TXT";
		using var wr = new StreamWriter(filename);

		wr.WriteLine($"Objects with errors in {config.DatabaseName} {DateTime.Now}:");
		wr.WriteLine();

		foreach (var s in items) {
			wr.WriteLine(s);
		}

		wr.Close();
	}

	private static int ParseOrDefault(string? value, int defaultValue) => int.TryParse(value, out var result) ? result : defaultValue;

	private const string CommandLineMessage = """
		Usage: SqlDatabaseDump.exe --instance <instance> --database <db> --dir <dir>

		Required:
		  -i, --instance <instance>  SQL Server instance to connect to  (or DB_INSTANCE environment variable)
		  -d, --database <db>        Database to process                (or DB_DATABASE environment variable)
		  -o, --dir <dir>            Output directory                   (or DB_DIR environment variable)

		Options:
		  -e, --extended-properties  Include extended properties
		  -w, --with-dependencies    Include dependencies
		  -a, --all                  Include all extras (extended properties and dependencies)

		  -r, --replace              Replace existing files (default is to fail if file exists)
		  -s, --single-thread        Single thread processing
		  -p, --parallel <n>         Maximum parallel tasks 1..16 (default is 8)
		  -k, --skip-errors          Skip errors without writing to file
		  -h, --help, -?             Help information
		""";
}
