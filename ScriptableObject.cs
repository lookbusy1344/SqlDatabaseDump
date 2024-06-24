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
[System.Diagnostics.DebuggerDisplay("{FullName}")]
internal sealed class ScriptableObject
{
	private static readonly ScriptingOptions ScriptOpts = new() { DriAll = true };

	private IScriptable Scriptable { get; }

	private string? Schema { get; }

	private string Ext { get; }

	private string? OverrideFilename { get; }

	public string Name { get; }

	public string FullName
	{
		get
		{
			if (OverrideFilename != null) {
				return OverrideFilename;                // explicit overriden filename
			} else {
				if (Schema != null) {
					return $"{Schema}.{Name}.{Ext}";    // filename including schema
				} else {
					return $"{Name}.{Ext}";             // filename without a schema
				}
			}
		}
	}

	/// <summary>
	/// Constructor for general scriptable objects
	/// </summary>
	public ScriptableObject(IScriptable script, string? schema, string name, string extension)
	{
		Scriptable = script;
		Schema = schema;
		Name = name.Replace('\\', '-');
		Ext = extension;
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
	}

	/// <summary>
	/// Script the object
	/// </summary>
	public StringCollection Script() => Scriptable.Script(ScriptOpts);
}
