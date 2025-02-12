namespace SqlDatabaseDump;

using System.Collections.Concurrent;
using Microsoft.SqlServer.Management.Smo;

internal static class Shared
{
	public static ScriptingOptions ScriptOptionsFull { get; } = new() {
		DriAll = true,
		Indexes = true,
		ClusteredIndexes = true,
		ColumnStoreIndexes = true,
		NonClusteredIndexes = true,
		FullTextCatalogs = true,
		FullTextIndexes = true,
		FullTextStopLists = true,
		IncludeFullTextCatalogRootPath = true,
		LoginSid = true,
		OptimizerData = true,
		Permissions = true,
		PrimaryObject = true,
		ScriptDataCompression = true,
		ScriptOwner = true,
		ScriptSchema = true,
		SpatialIndexes = true,
		XmlIndexes = true,
		ScriptBatchTerminator = true,
		Triggers = true,
		//WithDependencies = true,
		//ExtendedProperties = true,
	};

	public static ScriptingOptions ScriptOptionsNormal { get; } = new() { DriAll = true, ScriptBatchTerminator = true, Triggers = true };

	public static ScriptingOptions ScriptOptionsMinimal { get; } = new() { DriAll = true, ScriptBatchTerminator = true, Triggers = true };

	public static void WithExtendedProperties()
	{
		ScriptOptionsFull.ExtendedProperties = true;
		ScriptOptionsNormal.ExtendedProperties = true;
		ScriptOptionsMinimal.ExtendedProperties = true;
	}

	public static void WithDependencies()
	{
		ScriptOptionsFull.WithDependencies = true;
		ScriptOptionsNormal.WithDependencies = true;

		// dependents may have errors, and we don't want to stop
		ScriptOptionsFull.ContinueScriptingOnError = true;
		ScriptOptionsNormal.ContinueScriptingOnError = true;
	}

	/// <summary>
	/// Thread safe counter for the number of items in the queue
	/// </summary>
	public static SafeCounter QueueCounter { get; } = new();

	/// <summary>
	/// Maximum number of items in the queue
	/// </summary>
	public static SafeCounter MaxCounter { get; } = new();

	/// <summary>
	/// Number of items written to disk
	/// </summary>
	public static SafeCounter WrittenCounter { get; } = new();

	/// <summary>
	/// Objects that caused errors
	/// </summary>
	public static ConcurrentBag<string> ErrorObjects { get; } = [];
}
