namespace PicoArgs_dotnet;

/*  PICOARGS_DOTNET - a tiny command line argument parser for .NET
    https://github.com/lookbusy1344/PicoArgs-dotnet

    Version 3.1.1 - 14 Dec 2024

    Example usage:

	var pico = new PicoArgs(args);

	bool verbose = pico.Contains("-v", "--verbose");  // true if any of these switches are present
	string? patternOpt = pico.GetParamOpt("-t", "--pattern");  // optional parameter
	string pattern = pico.GetParamOpt("-t", "--pattern") ?? "*.txt";  // optional parameter with default
	string requirePath = pico.GetParam("-p", "--path");  // mandatory parameter, throws if not present
	IReadOnlyList<string> files = pico.GetMultipleParams("-f", "--file");  // get multiple parameters eg -f file1 -f file2
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
	private readonly List<KeyValue> args = ProcessItems(args, recogniseEquals).ToList();
	private bool finished;

	/// <summary>
	/// Get a boolean value from the command line, returns TRUE if found
	/// </summary>
	public bool Contains(params ReadOnlySpan<string> options)
	{
		ValidatePossibleParams(options);
		CheckFinished();

		// no args left
		if (args.Count == 0) {
			return false;
		}

		foreach (var o in options) {
			var index = args.FindIndex(a => a.Key == o);
			if (index >= 0) {
				// if this argument has a value, throw eg "--verbose=true" when we just expected "--verbose"
				if (args[index].Value != null) {
					throw new PicoArgsException(80, $"Unexpected value for \"{string.Join(", ", options!)}\"");
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
	/// Get multiple parameters from the command line, or empty list if not present
	/// eg -a value1 -a value2 will yield ["value1", "value2"]
	/// </summary>
	public IReadOnlyList<string> GetMultipleParams(params ReadOnlySpan<string> options)
	{
		ValidatePossibleParams(options);
		CheckFinished();

		var result = new List<string>(4);
		while (true) {
			var s = GetParamInternal(options);  // Internal call, because we have already validated the options
			if (s == null) {
				break;   // nothing else found, break out of loop
			} else {
				result.Add(s);
			}
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
		for (var i = 0; i < args.Count; ++i) {
			if (options.Contains(args[i].Key)) {
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
		var secondItem = args[index + 1];
		if (secondItem.Value != null) {
			throw new PicoArgsException(30, $"Cannot identify value for param \"{item.Key}\", followed by \"{secondItem.Key}\" and \"{secondItem.Value}\"");
		}

		// consume the switch and the separate value
		args.RemoveRange(index, 2);

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

			var countSwitches = CountCombinedSwitches(arg);

			switch (countSwitches) {
				case 0:
					// not a switch, just a value, eg "action". Never recognized an equals
					yield return KeyValue.Build(arg, false);
					break;

				case 1:
					// just a single item eg "-a" or "--key=value", but not "-abc"
					yield return KeyValue.Build(arg, recogniseEquals);
					break;

				default:
					// combined switches that need separating eg "-abc" or "-abc=code"
					var splitItems = arg.Contains('=') ?
						ProcessCombinedSwitchesWithEquals(arg, countSwitches, recogniseEquals) : ProcessCombinedSwitchesNoEquals(arg);

					foreach (var item in splitItems) { yield return item; }
					break;
			}
		}
	}

	/// <summary>
	/// Helper when there are multiple switches with equals eg "-abc=code" -> "-a", "-b", "-c=code"
	/// </summary>
	private static IEnumerable<KeyValue> ProcessCombinedSwitchesWithEquals(string arg, uint countSwitches, bool recogniseEquals)
	{
		// first process all but the last, eg -a -b but not -c=code
		for (var c = 1; c < countSwitches; ++c) {
			yield return KeyValue.Build($"-{arg[c]}", false);
		}

		// finally yield the final param with equals eg "-c=code" or "-c='code'"
		yield return KeyValue.Build($"-{arg[(int)countSwitches..]}", recogniseEquals);
	}

	/// <summary>
	/// Helper when there are multiple switches no equals eg "-abc" -> "-a", "-b", "-c"
	/// </summary>
	private static IEnumerable<KeyValue> ProcessCombinedSwitchesNoEquals(string arg)
	{
		// multiple switches, no equals eg "-abc"
		foreach (var c in arg[1..]) {
			yield return KeyValue.Build($"-{c}", false);
		}
	}

	/// <summary>
	/// Check if combined switches eg -abc. Returns the number of combined switches eg 3. This always respects '=' because its handled elsewhere
	/// Uses a span to avoid allocations
	/// </summary>
	private static uint CountCombinedSwitches(ReadOnlySpan<char> arg)
	{
		// ensure this is a switch
		if (!arg.StartsWith('-')) { return 0u; }

		// otherwise, we have a switch eg "-a", "-abc" or "-abc=code" or "--print"
		var equalsPos = arg.IndexOf('=');

		if (arg.Length > 2 && equalsPos > -1) {
			// only consider the part before the equals eg "-abc=value" -> "-abc"
			arg = arg[..equalsPos];
		}

		if (arg.Length > 2 && arg[1] != '-') {
			// if it starts with a dash, and is longer than 2 characters, and the second character is not a dash
			// we have length-1 items eg "-abc" has 3 switches
			return (uint)arg.Length - 1u;
		} else {
			// just a standard single-switch
			return 1u;
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

		if (eq.HasValue && eq < singleQuote && eq < doubleQuote) {
			// if the equals is before the quotes, then split on the equals, using spans to avoid allocations before trimming
			var span = arg.AsSpan();
			var key = span[..eq.Value]; // everything before the equals
			var value = span[(eq.Value + 1)..]; // everything after the equals, might include quotes

			return new KeyValue(key.ToString(), TrimQuote(value).ToString());
		} else {
			return new KeyValue(arg, null);
		}
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
