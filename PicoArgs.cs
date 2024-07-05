namespace PicoArgs_dotnet;

/*  PICOARGS_DOTNET - a tiny command line argument parser for .NET
    https://github.com/lookbusy1344/PicoArgs-dotnet

    Version 1.4.0 - 05 Jul 2024

    Example usage:

	var pico = new PicoArgs(args);

	bool verbose = pico.Contains("-v", "--verbose");  // true if any of these switches are present
	string? pattern = pico.GetParamOpt("-t", "--pattern") ?? "*.txt";  // optional parameter
	string requiredpath = pico.GetParam("-p", "--path");  // mandatory parameter, throws if not present
	string[] files = pico.GetMultipleParams("-f", "--file");  // multiple parameters returned in string[]
	string command = pico.GetCommand();  // first parameter, throws if not present
	string? commandopt = pico.GetCommandOpt();  // first parameter, null if not present

	pico.Finished();  // We are done. Throw if there are any unused parameters


    INSPIRED BY PICO-ARGS FOR RUST: https://github.com/RazrFalcon/pico-args

*/

/// <summary>
/// Tiny command line argument parser
/// </summary>
/// <remarks>
/// Build a PicoArgs from the command line arguments
/// </remarks>
public class PicoArgs(IEnumerable<string> args, bool recogniseEquals = true)
{
	private readonly List<KeyValue> args = args.Select(a => KeyValue.Build(a, recogniseEquals)).ToList();
	private bool finished;

	/// <summary>
	/// Get a boolean value from the command line, returns TRUE if found
	/// </summary>
	public bool Contains(params string[] options)
	{
		CheckFinished();
		if (options == null || options.Length == 0) {
			throw new ArgumentException("Must specify at least one option", nameof(options));
		}

		// no args left
		if (args.Count == 0) {
			return false;
		}

		foreach (var o in options) {
			if (!o.StartsWith('-')) {
				throw new ArgumentException("Must start with -", nameof(options));
			}

			var index = args.FindIndex(a => a.Key == o);
			if (index >= 0) {
				// if this argument has a value, throw eg "--verbose=true" when we just expected "--verbose"
				if (args[index].Value != null) {
					throw new PicoArgsException(80, $"Unexpected value for \"{string.Join(", ", options)}\"");
				}

				// found switch so consume it and return
				args.RemoveAt(index);
				return true;
			}
		}

		// not found
		return false;
	}

	/// <summary>
	/// Get multiple parameters from the command line, or empty array if not present
	/// eg -a value1 -a value2 will return ["value1", "value2"]
	/// </summary>
	public string[] GetMultipleParams(params string[] options)
	{
		CheckFinished();
		var result = new List<string>();
		while (true) {
			var s = GetParamOpt(options);
			if (s == null) {
				break;   // nothing else found, break out of loop
			}

			result.Add(s);
		}

		return [.. result];
	}

	/// <summary>
	/// Get a string value from the command line, throws is not present
	/// eg -a "value" or --foldera "value"
	/// </summary>
	public string GetParam(params string[] options) => GetParamOpt(options) ?? throw new PicoArgsException(10, $"Expected value for \"{string.Join(", ", options)}\"");

	/// <summary>
	/// Get a string value from the command line, or null if not present
	/// eg -a "value" or --foldera "value"
	/// </summary>
	public string? GetParamOpt(params string[] options)
	{
		CheckFinished();
		if (options == null || options.Length == 0) {
			throw new ArgumentException("Must specify at least one option", nameof(options));
		}

		if (args.Count == 0) {
			return null;
		}

		// check all options are switches
		foreach (var o in options) {
			if (!o.StartsWith('-')) {
				throw new ArgumentException("Must start with -", nameof(options));
			}
		}

		// do we have this switch on command line?
		var index = args.FindIndex(a => options.Contains(a.Key));
		if (index == -1) {
			return null;
		}

		// check if this key has an identified value
		var item = args[index];
		if (item.Value != null) {
			args.RemoveAt(index);
			return item.Value;
		}

		// otherwise, there is no identified value, so we need to look at the next parameter

		// is it the last parameter?
		if (index == args.Count - 1) {
			throw new PicoArgsException(20, $"Expected value after \"{item.Key}\"");
		}

		// grab and check the next parameter
		var seconditem = args[index + 1];
		if (seconditem.Value != null) {
			throw new PicoArgsException(30, $"Cannot identify value for param \"{item.Key}\", followed by \"{seconditem.Key}\" and \"{seconditem.Value}\"");
		}

		// consume the switch and the separate value
		args.RemoveRange(index, 2);

		// return the value
		return seconditem.Key;
	}

	/// <summary>
	/// Return and consume the first command line parameter. Throws if not present
	/// </summary>
	public string GetCommand() => GetCommandOpt() ?? throw new PicoArgsException(40, "Expected command");

	/// <summary>
	/// Return and consume the first command line parameter. Returns null if not present
	/// </summary>
	public string? GetCommandOpt()
	{
		CheckFinished();
		if (args.Count == 0) {
			return null;
		}

		// check for a switch
		var cmd = args[0].Key;
		if (cmd.StartsWith('-')) {
			throw new PicoArgsException(50, $"Expected command not \"{cmd}\"");
		}

		// consume the command, and return it
		args.RemoveAt(0);
		return cmd;
	}

	/// <summary>
	/// Return any unused command line parameters
	/// </summary>
	public IReadOnlyList<KeyValue> UnconsumedArgs => args;

	/// <summary>
	/// Return true if there are no unused command line parameters
	/// </summary>
	public bool IsEmpty => args.Count == 0;

	/// <summary>
	/// Throw an exception if there are any unused command line parameters
	/// </summary>
	public void Finished()
	{
		if (args.Count > 0) {
			throw new PicoArgsException(60, $"Unrecognised parameter(s): {string.Join(", ", args)}");
		}

		finished = true;
	}

	/// <summary>
	/// Ensure that Finished() has not been called
	/// </summary>
	private void CheckFinished()
	{
		if (finished) {
			throw new PicoArgsException(70, "Cannot use PicoArgs after calling Finished()");
		}
	}
}

/// <summary>
/// Tiny command line argument parser. This version implements IDisposable, and will throw if there are any unused command line parameters
/// </summary>
public sealed class PicoArgsDisposable(IEnumerable<string> args) : PicoArgs(args), IDisposable
{
	/// <summary>
	/// If true, supress the check for unused command line parameters
	/// </summary>
	public bool SuppressCheck { get; set; }

	/// <summary>
	/// Throw an exception if there are any unused command line parameters
	/// </summary>
	public void Dispose()
	{
		if (!SuppressCheck) {
			Finished();
		}
	}
}

/// <summary>
/// a key and optional identified value eg --key=value becomes "--key" and "value"
/// </summary>
public readonly record struct KeyValue(string Key, string? Value)
{
	public static KeyValue Build(string arg, bool recogniseEquals)
	{
		ArgumentNullException.ThrowIfNull(arg);

		// if arg does not start with a dash, this cannot be a key+value eg --key=value vs key=value
		if (!recogniseEquals || !arg.StartsWith('-')) {
			return new KeyValue(arg, null);
		}

		// locate positions of quotes and equals
		var singleQuote = IndexOf(arg, '\'') ?? int.MaxValue;
		var doubleQuote = IndexOf(arg, '\"') ?? int.MaxValue;
		var eq = IndexOf(arg, '=');

		if (eq < singleQuote && eq < doubleQuote) {
			// if the equals is before the quotes, then split on the equals
			var parts = arg.Split('=', 2);
			return new KeyValue(parts[0], TrimQuote(parts[1]));
		} else {
			return new KeyValue(arg, null);
		}
	}

	/// <summary>
	/// Index of a char in a string, or null if not found
	/// </summary>
	private static int? IndexOf(string str, char chr) => str.IndexOf(chr) is int pos && pos >= 0 ? pos : null;

	/// <summary>
	/// If the string starts and ends with the same quote, remove them eg "hello world" -> hello world
	/// </summary>
	private static string TrimQuote(string str) =>
		(str.Length > 1 && (str[0] is '\'' or '\"') && str[^1] == str[0]) ? str[1..^1] : str;

	public override string ToString() => Value == null ? Key : $"{Key}={Value}";
}

/// <summary>
/// Exception thrown when there is a problem with the command line arguments
/// </summary>
public class PicoArgsException : Exception
{
	public int Code { get; init; }

	public PicoArgsException(int code, string message) : base(message) => this.Code = code;

	public PicoArgsException() { }

	public PicoArgsException(string message) : base(message) { }

	public PicoArgsException(string message, Exception innerException) : base(message, innerException) { }
}
