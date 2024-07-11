using Microsoft.SqlServer.Management.Smo;
using System.Collections.Concurrent;

namespace SqlDatabaseDump;

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
