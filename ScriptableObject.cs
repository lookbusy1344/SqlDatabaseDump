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
	private IScriptable Scriptable { get; }

	//private readonly IReadOnlyList<IScriptable>? Subscripts;

	private readonly ScriptingOptions Options;

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

	public override string ToString() => FullName;

	/// <summary>
	/// Constructor for general scriptable objects
	/// </summary>
	public ScriptableObject(IScriptable script, string? schema, string name, string extension, bool tableOptions)
	{
		Scriptable = script;
		Schema = schema;
		Name = name.Replace('\\', '-');
		Ext = extension;
		Options = tableOptions ? Shared.ScriptOptionsFull : Shared.ScriptOptionsNormal;
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
		Options = Shared.ScriptOptionsNormal;
	}

	/// <summary>
	/// Script the object, and any nested sub-scripts
	/// </summary>
	public IReadOnlyList<string> Script()
	{
		// main script
		var main = Scriptable.Script(Options);

		//var additional = Subscripts == null ? 0 : Subscripts.Count * 4;
		var result = new List<string>(main.Count);

		foreach (var s in main) {
			if (!string.IsNullOrWhiteSpace(s)) {
				result.Add(s);
				result.Add("GO");
				result.Add(string.Empty);
			}
		}

		// sub-scripts
		//if (Subscripts?.Count > 0) {
		//	result.Add("-- ============================ Additional sub-objects ============================");
		//	result.Add(string.Empty);

		//	foreach (var sub in Subscripts) {
		//		var subcol = sub.Script(ScriptOptionsNormal);
		//		foreach (var s in subcol) {
		//			if (!string.IsNullOrWhiteSpace(s)) {
		//				result.Add(s);
		//				result.Add("GO");
		//				result.Add(string.Empty);
		//			}
		//		}
		//	}
		//}

		return result;
	}
}
