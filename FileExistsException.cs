
namespace SqlDatabaseDump;

/// <summary>
/// Exception thrown when a file already exists
/// </summary>
public class FileExistsException : Exception
{
	public string? Filename { get; init; }

	public FileExistsException(string message, string filename) : base(message) => Filename = filename;

	public FileExistsException(string message) : base(message)
	{
	}

	public FileExistsException()
	{
	}

	public FileExistsException(string message, Exception innerException) : base(message, innerException)
	{
	}
}
