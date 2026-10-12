namespace SqlDatabaseDump;

internal sealed record class Config(string InstanceName, string DatabaseName, string OutputDirectory, int MaxParallel,
	bool SingleThread, bool ReplaceExistingFiles, bool SkipErrors, bool ExtendedProperties, bool WithDependencies,
	SqlLogin? Login, bool TrustServerCertificate);

/// <summary>
/// SQL Server authentication credentials. Null login in Config means Windows authentication
/// </summary>
internal sealed record class SqlLogin(string User, string Password)
{
	public override string ToString() => User;
}
