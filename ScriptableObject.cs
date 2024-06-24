using Microsoft.SqlServer.Management.Smo;

namespace SqlDatabaseDump;

/// <summary>
/// Types of db objects that can be scripted
/// </summary>
internal enum Scriptable
{
	Roles,
	Tables,
	Views,
	StoredProcedures,
	Schemas,
	Sequences,
	DatabaseTriggers,
	UserDefinedFunctions,
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
	public IScriptable Scriptable { get; }

	public string? Schema { get; }

	public string Name { get; }

	public string Ext { get; }

	public string? OverrideFilename { get; }

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
}
