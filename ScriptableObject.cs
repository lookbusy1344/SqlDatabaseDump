using Microsoft.SqlServer.Management.Smo;
using System.Collections.Specialized;

namespace SqlDatabaseDump;

/// <summary>
/// Types of db objects that can be scripted
/// </summary>
internal enum Scriptable
{
	// order is important here because each spawns a task and can run in parallel
	// start with the big 4: tables, views, stored procedures, user defined functions
	Tables,
	Views,
	StoredProcedures,
	UserDefinedFunctions,
	// then smaller items like schemas and roles
	Schemas,
	Roles,
	DatabaseTriggers,
	// then the last bits, frequently empty
	Sequences,
	UserDefinedDataTypes,
	UserDefinedTypes,
	Rules,
	Synonyms
}

/// <summary>
/// A wrapper class for database objects to allow polymorphic processing
/// </summary>
[System.Diagnostics.DebuggerDisplay("{FullName,nq}")]
internal sealed class ScriptableObject
{
	private IScriptable Scriptable { get; }

	private readonly ScriptingOptions Options;

	private string? Schema { get; }

	private string Ext { get; }

	private string? OverrideFilename { get; }

	public string Name { get; }

	/// <summary>
	/// Override the full name, or [schema.]name.ext
	/// </summary>
	public string FullName => OverrideFilename ?? $"{Schema}.{Name}.{Ext}".TrimStart('.');

	public override string ToString() => FullName;

	/// <summary>
	/// Constructor for general scriptable objects
	/// </summary>
	public ScriptableObject(IScriptable script, string? schema, string name, string extension, ScriptingOptions options)
	{
		Scriptable = script;
		Schema = schema;
		Name = name.Replace('\\', '-');
		Ext = extension;
		Options = options;
	}

	/// <summary>
	/// Constructor for database settings
	/// </summary>
	public ScriptableObject(Database db, string databaseName)
	{
		Scriptable = db;
		OverrideFilename = $"{databaseName}-Settings.TXT";
		Schema = null;
		Name = "database settings";
		Ext = string.Empty;
		Options = Shared.ScriptOptionsMinimal;
	}

	/// <summary>
	/// Script the object, and any nested sub-scripts
	/// </summary>
	public IEnumerable<string> Script()
	{
		var main = Scriptable.Script(Options);

		foreach (var s in main) {
			if (!string.IsNullOrWhiteSpace(s)) {
				yield return s;
				yield return "GO";
				yield return string.Empty;
			}
		}
	}
}
