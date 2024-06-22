using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
internal readonly struct DbObjectWrapper
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
	public DbObjectWrapper(IScriptable script, string? schema, string name, string extension)
	{
		Scriptable = script;
		Schema = schema;
		Name = name.Replace('\\', '-');
		Ext = extension;
	}

	/// <summary>
	/// Constructor for database settings
	/// </summary>
	public DbObjectWrapper(Database db, string databaseName)
	{
		Scriptable = db;
		OverrideFilename = $"{databaseName}-settings.TXT";
		Schema = null;
		Name = "database settings";
		Ext = string.Empty;
	}
}

/// <summary>
/// A polymorphic list of database objects
/// </summary>
[System.Diagnostics.DebuggerDisplay("{Items}")]
internal sealed class DbObjectList(SafeCounter queueCounter, SafeCounter maxCounter, CancellationTokenSource cancellationToken)
{
	private readonly List<DbObjectWrapper> items = [];

	public IReadOnlyList<DbObjectWrapper> Items => items;

	/// <summary>
	/// Add a scriptable object to the list
	/// </summary>
	private void Add(IScriptable script, string? schema, string name, string extension) => items.Add(new DbObjectWrapper(script, schema, name, extension));

	private void UpdateCounters()
	{
		_ = queueCounter.Increment();    // current items in queue, this goes up and down
		_ = maxCounter.Increment();     // max items in queue, this only goes up
	}

	public void AddDatabase(Database db, string databaseName)
	{
		UpdateCounters();
		items.Add(new DbObjectWrapper(db, databaseName));
	}

	public void AddTables(TableCollection tableCollection)
	{
		foreach (Table t in tableCollection) {
			cancellationToken.Token.ThrowIfCancellationRequested();

			if (!t.IsSystemObject) {
				UpdateCounters();
				ThreadsafeWrite.Write($"Enumerating table {t.Name}");
				Add(t, t.Schema, t.Name, "TAB");
			}
		}
	}

	public void AddViews(ViewCollection viewCollection)
	{
		foreach (View v in viewCollection) {
			cancellationToken.Token.ThrowIfCancellationRequested();

			if (!v.IsSystemObject) {
				UpdateCounters();
				ThreadsafeWrite.Write($"Enumerating view {v.Name}");
				Add(v, v.Schema, v.Name, "VIW");
			}
		}
	}

	public void AddStoredProcedures(StoredProcedureCollection storedProcedures)
	{
		foreach (StoredProcedure p in storedProcedures) {
			cancellationToken.Token.ThrowIfCancellationRequested();

			if (!p.IsSystemObject) {
				UpdateCounters();
				ThreadsafeWrite.Write($"Enumerating sproc {p.Name}");
				Add(p, p.Schema, p.Name, "PRC");
			}
		}
	}

	public void AddUserDefinedFunctions(UserDefinedFunctionCollection udfs)
	{
		foreach (UserDefinedFunction u in udfs) {
			cancellationToken.Token.ThrowIfCancellationRequested();

			if (!u.IsSystemObject) {
				UpdateCounters();
				ThreadsafeWrite.Write($"Enumerating UDF {u.Name}");
				Add(u, u.Schema, u.Name, "UDF");
			}
		}
	}

	public void AddRoles(DatabaseRoleCollection roles)
	{
		foreach (DatabaseRole r in roles) {
			cancellationToken.Token.ThrowIfCancellationRequested();

			if (!r.IsFixedRole) {
				UpdateCounters();
				ThreadsafeWrite.Write($"Enumerating role {r.Name}");
				Add(r, null, r.Name, "ROLE");
			}
		}
	}

	public void AddRules(RuleCollection rules)
	{
		foreach (Rule r in rules) {
			cancellationToken.Token.ThrowIfCancellationRequested();

			UpdateCounters();
			ThreadsafeWrite.Write($"Enumerating rule {r.Name}");
			Add(r, r.Schema, r.Name, "RULE");
		}
	}

	public void AddDatabaseTriggers(DatabaseDdlTriggerCollection triggers)
	{
		foreach (DatabaseDdlTrigger t in triggers) {
			cancellationToken.Token.ThrowIfCancellationRequested();

			if (!t.IsSystemObject) {
				UpdateCounters();
				ThreadsafeWrite.Write($"Enumerating trigger {t.Name}");
				Add(t, null, t.Name, "TRIG");
			}
		}
	}

	public void AddSchemas(SchemaCollection schemas)
	{
		foreach (Schema s in schemas) {
			cancellationToken.Token.ThrowIfCancellationRequested();

			if (!s.IsSystemObject) {
				UpdateCounters();
				ThreadsafeWrite.Write($"Enumerating schema {s.Name}");
				Add(s, null, s.Name, "SCH");
			}
		}
	}

	public void AddUserDefinedDataTypes(UserDefinedDataTypeCollection types)
	{
		// UserDefinedDataTypes are SQL types, not .NET types (that are called UserDefinedTypes)
		foreach (UserDefinedDataType t in types) {
			cancellationToken.Token.ThrowIfCancellationRequested();

			UpdateCounters();
			ThreadsafeWrite.Write($"Enumerating uddt {t.Name}");
			Add(t, t.Schema, t.Name, "UDDT");
		}
	}

	public void AddUserDefinedTypes(UserDefinedTypeCollection types)
	{
		// UserDefinedTypes are .NET types, not SQL types (that are called UserDefinedDataTypes)
		foreach (UserDefinedType t in types) {
			cancellationToken.Token.ThrowIfCancellationRequested();

			UpdateCounters();
			ThreadsafeWrite.Write($"Enumerating udt {t.Name}");
			Add(t, t.Schema, t.Name, "TYPE");
		}
	}

	public void AddSequences(SequenceCollection sequences)
	{
		foreach (Sequence s in sequences) {
			cancellationToken.Token.ThrowIfCancellationRequested();

			UpdateCounters();
			ThreadsafeWrite.Write($"Enumerating sequence {s.Name}");
			Add(s, s.Schema, s.Name, "SEQ");
		}
	}

	public void AddSynonyms(SynonymCollection syns)
	{
		foreach (Synonym s in syns) {
			cancellationToken.Token.ThrowIfCancellationRequested();

			UpdateCounters();
			ThreadsafeWrite.Write($"Enumerating synonym {s.Name}");
			Add(s, s.Schema, s.Name, "SYNO");
		}
	}
}
