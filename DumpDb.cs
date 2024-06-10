﻿using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlDatabaseDump;

internal sealed class DumpDb(Config config, Scriptable scriptType, CancellationTokenSource cancellationToken)
{
	// https://learn.microsoft.com/en-us/dotnet/api/microsoft.sqlserver.management.smo.scriptingoptions.driall?view=sql-smo-160&devlangs=csharp&f1url=%3FappId%3DDev17IDEF1%26l%3DEN-US%26k%3Dk(Microsoft.SqlServer.Management.Smo.ScriptingOptions.DriAll)%3Bk(DevLang-csharp)%26rd%3Dtrue

	private readonly ScriptingOptions op = new() { DriAll = true };
	private Database? myDB;

	public void Run()
	{
		var theServer = new Server(config.Instance);
		myDB = theServer.Databases[config.Database];
		theServer.SetDefaultInitFields(true);

		var list = new DbObjectList(cancellationToken);

		switch (scriptType) {
			case Scriptable.Tables:
				list.AddDatabase(myDB, config.Database);
				list.AddTables(myDB.Tables);
				break;
			case Scriptable.Views:
				list.AddViews(myDB.Views);
				break;
			case Scriptable.StoredProcedures:
				list.AddStoredProcedures(myDB.StoredProcedures);
				break;
			case Scriptable.UserDefinedFunctions:
				list.AddUserDefinedFunctions(myDB.UserDefinedFunctions);
				break;
			case Scriptable.Roles:
				list.AddRoles(myDB.Roles);
				break;
			case Scriptable.Rules:
				list.AddRules(myDB.Rules);
				break;
			case Scriptable.DatabaseTriggers:
				list.AddDatabaseTriggers(myDB.Triggers);
				break;
			case Scriptable.Schemas:
				list.AddSchemas(myDB.Schemas);
				break;
			case Scriptable.UserDefinedDataTypes:
				list.AddUserDefinedDataTypes(myDB.UserDefinedDataTypes);
				break;
			case Scriptable.UserDefinedTypes:
				list.AddUserDefinedTypes(myDB.UserDefinedTypes);
				break;
			case Scriptable.Sequences:
				list.AddSequences(myDB.Sequences);
				break;
			case Scriptable.Synonyms:
				list.AddSynonyms(myDB.Synonyms);
				break;
			default:
				throw new InvalidOperationException($"Invalid type: {scriptType}");
		}

		foreach (var o in list.Items) {
			try {
				WriteObject(o);
			}
			catch (FailedOperationException ex) {
				// eg if the object is encrypted, continue with the other objects
				ThreadsafeWrite.Write($"Failed to script {o.Name}: {ex.Message}");
				WritePlaceMarker(o, "-- Failed to script object");
			}
		}
	}

	private void WriteObject(DbObjectWrapper wrappedObject)
	{
		cancellationToken.Token.ThrowIfCancellationRequested();

		ThreadsafeWrite.Write($"Scripting {wrappedObject.Name}");

		var filename = $"{config.Dir}{wrappedObject.FullName}";

		if (!config.Replace && File.Exists(filename)) {
			// signal to any other tasks to cancel, and throw
			cancellationToken.Cancel();
			throw new Exception($"File already exists: {filename}");
		}

		var sc = wrappedObject.Scriptable.Script(op); // this will throw if access is denied
		if (sc.Count == 0) {
			return;
		}

		using var wr = new StreamWriter(filename);

		foreach (var s in sc) {
			wr.WriteLine(s);
			wr.WriteLine("GO");
			wr.WriteLine();
		}
	}

	private void WritePlaceMarker(DbObjectWrapper wrappedObject, string content)
	{
		var filename = $"{config.Dir}{wrappedObject.FullName}";
		File.WriteAllText(filename, $"{content} {DateTime.Now}");
	}

	/// <summary>
	/// Ensure the path of the given filename exists, and return it with a trailing backslash
	/// </summary>
	public static string EnsurePathExists(string path)
	{
		if (!path.EndsWith('\\')) {
			path += "\\";
		}

		var directoryPath = Path.GetDirectoryName(path)
			?? throw new InvalidOperationException($"Could not determine directory path for '{path}'");

		_ = Directory.CreateDirectory(directoryPath);
		return directoryPath + "\\";
	}
}
