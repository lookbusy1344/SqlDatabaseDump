﻿using Microsoft.SqlServer.Management.Smo;

namespace SqlDatabaseDump;

internal sealed class DumpDb(Config config, Scriptable scriptType, CancellationTokenSource cancellationToken)
{
	// https://learn.microsoft.com/en-us/dotnet/api/microsoft.sqlserver.management.smo.scriptingoptions.driall?view=sql-smo-160&devlangs=csharp&f1url=%3FappId%3DDev17IDEF1%26l%3DEN-US%26k%3Dk(Microsoft.SqlServer.Management.Smo.ScriptingOptions.DriAll)%3Bk(DevLang-csharp)%26rd%3Dtrue

	private Database? myDB;

	public void Run()
	{
		var theServer = new Server(config.InstanceName);
		myDB = theServer.Databases[config.DatabaseName];
		theServer.SetDefaultInitFields(true);

		var list = new DbObjectList(cancellationToken);

		switch (scriptType) {
			case Scriptable.Tables:
				list.AddDatabase(myDB, config.DatabaseName);
				list.AddTables(myDB.Tables, config.SeparateTriggers);
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

		ThreadsafeWrite.Write($"-- Queue contains {Shared.QueueCounter.Value} item(s) out of {Shared.MaxCounter.Value} --");

		foreach (var o in list.Items) {
			try {
				WriteObject(o);
			}
			catch (FailedOperationException ex) {
				// eg if the object is encrypted, log and continue with the other objects
				ThreadsafeWrite.Write($"Failed to script {o.FullName}: {ex.Message}");

				Shared.ErrorObjects.Add(o.FullName);

				if (!config.SkipErrors) {
					WritePlaceMarker(o, "-- Failed to script object");
				}
			}
		}
	}

	private void WriteObject(ScriptableObject wrappedObject)
	{
		cancellationToken.Token.ThrowIfCancellationRequested();

		ThreadsafeWrite.Write($"Scripting {wrappedObject.Name} ({Shared.QueueCounter.Value} of {Shared.MaxCounter.Value} remaining)");

		var filename = $"{config.OutputDirectory}{wrappedObject.FullName}";

		try {
			if (!config.ReplaceExistingFiles && File.Exists(filename)) {
				// signal to any other tasks to cancel, and throw
				cancellationToken.Cancel();
				throw new FileExistsException($"File already exists: {filename}", filename);
			}

			var sc = wrappedObject.Script(); // this will throw if access is denied

			if (sc.Count == 0) {
				return; // nothing to write
			}

			using var wr = new StreamWriter(filename);

			foreach (var s in sc) {
				wr.WriteLine(s);
			}

			wr.Close();
			_ = Shared.WrittenCounter.Increment();  // file has been written
		}
		finally {
			// one less in queue
			_ = Shared.QueueCounter.Decrement();
		}
	}

	private void WritePlaceMarker(ScriptableObject wrappedObject, string content)
	{
		var filename = $"{config.OutputDirectory}{wrappedObject.FullName}";
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
