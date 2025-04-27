namespace PicoArgs_dotnet;

/*  PICOARGS_DOTNET - a tiny command line argument parser for .NET
    https://github.com/lookbusy1344/PicoArgs-dotnet

    Version 3.2.3 - 27 Apr 2025

    Example usage:

	var pico = new PicoArgs(args);

	bool verbose = pico.Contains("-v", "--verbose");  // true if any of these switches are present
	string? patternOpt = pico.GetParamOpt("-t", "--pattern");  // optional parameter
	string pattern = pico.GetParamOpt("-t", "--pattern") ?? "*.txt";  // optional parameter with default
	string requirePath = pico.GetParam("-p", "--path");  // mandatory parameter, throws if not present
	IList<string> files = pico.GetMultipleParams("-f", "--file");  // get multiple parameters eg -f file1 -f file2
	string command = pico.GetCommand();  // first parameter, throws if not present
	string? commandOpt = pico.GetCommandOpt();  // first parameter, null if not present

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
	private readonly List<KeyValue> argList = [.. ProcessItems(args, recogniseEquals)];
	private bool finished;

	/// <summary>
	/// Get a boolean value from the command line, returns TRUE if found
	/// </summary>
	public bool Contains(params ReadOnlySpan<string> options)
	{
		ValidatePossibleParams(options);
		CheckFinished();

		// no args left
		if (argList.Count == 0) {
			return false;
		}

		// use HashSet for faster lookups, just one heap allocation
		var optionsSet = new HashSet<string>(options.Length);
		foreach (var o in options) {
			_ = optionsSet.Add(o);
		}

		for (var index = 0; index < argList.Count; ++index) {
			if (optionsSet.Contains(argList[index].Key)) {
				// if this argument has a value, throw
				if (argList[index].Value != null) {
					throw new PicoArgsException(80, $"Unexpected value for \"{string.Join(", ", options!)}\"");
				}

				// found switch so consume it and return
				argList.RemoveAt(index);
				return true;
			}
		}

		// not found
		return false;
	}

	/// <summary>
	/// Get multiple parameters from the command line, or empty list if not present
	/// eg -a value1 -a value2 will yield ["value1", "value2"]
	/// </summary>
	public IList<string> GetMultipleParams(params ReadOnlySpan<string> options)
	{
		ValidatePossibleParams(options);
		CheckFinished();

		var result = new List<string>(4);
		while (true) {
			var s = GetParamInternal(options);  // Internal call, because we have already validated the options
			if (s == null) {
				break;   // nothing else found, break out of loop
			}

			result.Add(s);
		}

		return result;
	}

	/// <summary>
	/// Get a string value from the command line, throws is not present
	/// eg -a "value" or --folder "value"
	/// </summary>
	public string GetParam(params ReadOnlySpan<string> options) => GetParamOpt(options) ?? throw new PicoArgsException(10, $"Expected value for \"{string.Join(", ", options!)}\"");

	/// <summary>
	/// Get a string value from the command line, or null if not present
	/// eg -a "value" or --folder "value"
	/// </summary>
	public string? GetParamOpt(params ReadOnlySpan<string> options)
	{
		ValidatePossibleParams(options);
		CheckFinished();

		return GetParamInternal(options);
	}

	/// <summary>
	/// Internal version of GetParamOpt, which does not check for valid options
	/// </summary>
	private string? GetParamInternal(ReadOnlySpan<string> options)
	{
		// does args contain any of the specified options? Can't use a lambda because of ref struct
		var index = -1;
		for (var i = 0; i < argList.Count; ++i) {
			if (options.Contains(argList[i].Key)) {
				// options contains this key, so we have a match. Record the index and break
				index = i;
				break;
			}
		}

		if (index == -1) {
			// not found
			return null;
		}

		// check if this key has an identified value
		var item = argList[index];
		if (item.Value != null) {
			argList.RemoveAt(index);
			return item.Value;
		}

		// otherwise, there is no identified value, so we need to look at the next parameter

		// is it the last parameter?
		if (index == argList.Count - 1) {
			throw new PicoArgsException(20, $"Expected value after \"{item.Key}\"");
		}

		// grab and check the next parameter
		var secondItem = argList[index + 1];
		if (secondItem.Value != null) {
			throw new PicoArgsException(30, $"Cannot identify value for param \"{item.Key}\", followed by \"{secondItem.Key}\" and \"{secondItem.Value}\"");
		}

		// consume the switch and the separate value
		argList.RemoveRange(index, 2);

		// return the value
		return secondItem.Key;
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
		if (argList.Count == 0) {
			return null;
		}

		// check for a switch, a single dash '-' or double-dash '--' is ok
		var cmd = argList[0].Key;
		if (cmd != "-" && cmd != "--" && cmd.StartsWith('-')) {
			throw new PicoArgsException(50, $"Expected command not \"{cmd}\"");
		}

		// consume the command, and return it
		argList.RemoveAt(0);
		return cmd;
	}

	/// <summary>
	/// Return any unused command line parameters
	/// </summary>
	public IReadOnlyList<KeyValue> UnconsumedArgs => argList;

	/// <summary>
	/// Return true if there are no unused command line parameters
	/// </summary>
	public bool IsEmpty => argList.Count == 0;

	/// <summary>
	/// Throw an exception if there are any unused command line parameters
	/// </summary>
	public void Finished()
	{
		if (argList.Count > 0) {
			throw new PicoArgsException(60, $"Unrecognised parameter(s): {string.Join(", ", argList)}");
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

	/// <summary>
	/// Check options are valid for Contains() or GetParam(), eg -a or --action, but not -aa (already expanded) or ---action or --a
	/// </summary>
	private static void ValidatePossibleParams(ReadOnlySpan<string> options)
	{
		if (options.IsEmpty) {
			throw new ArgumentException("Must specify at least one option", nameof(options));
		}

		foreach (var o in options) {
			if (o.Length == 1 || !o.StartsWith('-')) {
				throw new ArgumentException($"Options must start with a dash and be longer than 1 character: {o}", nameof(options));
			}
			if (o.Length > 2) {
				if (o[1] != '-') {
					// if it is longer than 2 characters, the second character must be a dash. eg -ab is invalid here (its already been expanded to -a -b)
					throw new ArgumentException($"Long options must start with 2 dashes: {o}", nameof(options));
				}
				if (o[2] == '-') {
					// if it is longer than 2 characters, the third character must not be a dash. eg ---a is not valid
					throw new ArgumentException($"Options should not start with 3 dashes: {o}", nameof(options));
				}
				if (o.Length == 3) {
					throw new ArgumentException($"Long options must be 2 characters or more: {o}", nameof(options));
				}
			}
		}
	}

	/// <summary>
	/// Helper function to process the command line arguments. Splits multiple switches into individual switches
	/// eg -abc becomes -a -b -c
	/// </summary>
	private static IEnumerable<KeyValue> ProcessItems(IEnumerable<string> args, bool recogniseEquals)
	{
		foreach (var arg in args) {
			ValidateInputParam(arg);

			if (arg == "-" || !arg.StartsWith('-')) {
				// not a switch, or just a single dash
				yield return KeyValue.Build(arg, false);
				continue;
			}

			var equalsPos = arg.IndexOf('=');
			var switchEnd = equalsPos > -1 ? equalsPos : arg.Length;

			if (switchEnd == 2 || arg[1] == '-') {
				// single switch or long switch, eg -a or --action
				yield return KeyValue.Build(arg, recogniseEquals);
			} else {
				// combined switches, eg -abc or -abc=code
				for (var i = 1; i < switchEnd; i++) {
					if (equalsPos > -1 && i == switchEnd - 1) {
						// last item in the combined switches, and there is a value eg -abc=code -> -c=code
						yield return KeyValue.Build($"-{arg[i..]}", recogniseEquals);
					} else {
						// normal switch eg -abc=code -> -a, -b
						yield return KeyValue.Build($"-{arg[i]}", false);
					}
				}
			}
		}
	}

	/// <summary>
	/// Validate this input param from command line. Invalid is ---something or --x. Valid options are -a or -ab or --action
	/// </summary>
	private static void ValidateInputParam(ReadOnlySpan<char> arg)
	{
		if (arg == "-") {
			// a single dash is not valid
			throw new PicoArgsException(90, "Parameter should not be a single dash");
		}
		if (arg.StartsWith("---")) {
			// eg ---something is not valid
			throw new PicoArgsException(90, $"Parameter should not start with 3 dashes: {arg}");
		}
		if (arg.Length == 3 && arg.StartsWith("--")) {
			// eg --a is not valid
			throw new PicoArgsException(90, $"Long options must be 2 characters or more: {arg}");
		}
	}
}

/// <summary>
/// Tiny command line argument parser. This version implements IDisposable, and will throw if there are any unused command line parameters
/// </summary>
public sealed class PicoArgsDisposable(IEnumerable<string> args, bool recogniseEquals = true) : PicoArgs(args, recogniseEquals), IDisposable
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
	/// <summary>
	/// Build a KeyValue from a string, optionally recognising an equals sign and quotes eg --key=value or --key="value"
	/// </summary>
	internal static KeyValue Build(string arg, bool recogniseEquals)
	{
		// if arg does not start with a dash, this cannot be a key+value eg --key=value vs key=value
		if (!recogniseEquals || !arg.StartsWith('-')) {
			return new KeyValue(arg, null);
		}

		// locate positions of quotes and equals
		var singleQuote = IndexOf(arg, '\'') ?? int.MaxValue;
		var doubleQuote = IndexOf(arg, '\"') ?? int.MaxValue;
		var eq = IndexOf(arg, '=');

		if (eq.HasValue && eq < singleQuote && eq < doubleQuote) {
			// if the equals is before the quotes, then split on the equals, using spans to avoid allocations before trimming
			var span = arg.AsSpan();
			var key = span[..eq.Value]; // everything before the equals
			var value = span[(eq.Value + 1)..]; // everything after the equals, might include quotes

			return new KeyValue(key.ToString(), TrimQuote(value).ToString());
		}

		return new KeyValue(arg, null);
	}

	/// <summary>
	/// Index of a char in a string, or null if not found
	/// </summary>
	private static int? IndexOf(string str, char chr) => str.IndexOf(chr) is int pos && pos >= 0 ? pos : null;

	/// <summary>
	/// If the span starts and ends with the same quote, remove them eg "hello world" -> hello world
	/// </summary>
	private static ReadOnlySpan<char> TrimQuote(ReadOnlySpan<char> str) =>
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
