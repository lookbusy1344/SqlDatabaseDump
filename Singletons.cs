namespace SqlDatabaseDump;

internal static class Singletons
{
	private static readonly SafeCounter queueCounter = new();    // static so its shared across all instances. Counter is thread safe
	private static readonly SafeCounter maxCounter = new();
	private static readonly SafeCounter writtenCounter = new();

	public static SafeCounter QueueCounter => queueCounter;

	public static SafeCounter MaxCounter => maxCounter;

	public static SafeCounter WrittenCounter => writtenCounter;
}
