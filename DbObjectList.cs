using Microsoft.SqlServer.Management.Smo;

namespace SqlDatabaseDump;

/// <summary>
/// A polymorphic list of database objects
/// </summary>
[System.Diagnostics.DebuggerDisplay("{Items}")]
internal sealed class DbObjectList(CancellationTokenSource cancellationToken)
{
	private readonly List<ScriptableObject> items = [];

	public IReadOnlyList<ScriptableObject> Items => items;

	/// <summary>
	/// Add a scriptable object to the list
	/// </summary>
	private void Add(IScriptable script, string? schema, string name, string extension) => items.Add(new ScriptableObject(script, schema, name, extension));

	private static void UpdateCounters()
	{
		_ = Shared.QueueCounter.Increment();    // current items in queue, this goes up and down
		_ = Shared.MaxCounter.Increment();     // max items in queue, this only goes up
	}

	public void AddDatabase(Database db, string databaseName)
	{
		UpdateCounters();
		items.Add(new ScriptableObject(db, databaseName));
	}

	public void AddTables(TableCollection tableCollection)
	{
		foreach (Table tab in tableCollection) {
			cancellationToken.Token.ThrowIfCancellationRequested();

			if (!tab.IsSystemObject) {
				UpdateCounters();
				ThreadsafeWrite.Write($"Enumerating table {tab.Name}");
				Add(tab, tab.Schema, tab.Name, "TAB");

				// script triggers
				foreach (Trigger trig in tab.Triggers) {
					cancellationToken.Token.ThrowIfCancellationRequested();

					UpdateCounters();
					ThreadsafeWrite.Write($"Enumerating trigger {trig.Name}");

					// name format: table-trigger eg dbo.MyTable-MyTrigger.TRIG
					Add(trig, tab.Schema, $"{tab.Name}-{trig.Name}", "TRIG");
				}
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
