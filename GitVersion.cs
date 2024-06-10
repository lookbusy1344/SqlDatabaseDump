using System.Reflection;

namespace GitVersion;

public class VersionInfo
{
	/// <summary>
	/// Version string eg "1.0.0.0"
	/// </summary>
	public string? Version { get; init; }

	/// <summary>
	/// Git hash eg "a1b2c3d4e5f6"
	/// </summary>
	public string? GitHash { get; private set; }

	/// <summary>
	/// Git modified flag
	/// </summary>
	public bool GitModified { get; private set; }

	/// <summary>
	/// An empty version info
	/// </summary>
	public static readonly VersionInfo Empty = new() { Version = string.Empty, GitHash = string.Empty };

	public string GetHash(int? len = null)
	{
		if (string.IsNullOrEmpty(Version) && string.IsNullOrEmpty(GitHash)) {
			return "(unknown)";
		}

		GitHash ??= string.Empty;

		if (len == null) {
			return GitHash;
		} else {
			return $"{GitHash[..len.Value]}{(GitModified ? "+" : string.Empty)}";
		}
	}

	public string GetVersionHash(int? len = null)
	{
		if (string.IsNullOrEmpty(Version) && string.IsNullOrEmpty(GitHash)) {
			return "(unknown)";
		}

		GitHash ??= string.Empty;

		if (len == null) {
			return $"v{Version} - {GitHash}";
		} else {
			return $"v{Version} - {GitHash[..len.Value]}{(GitModified ? "+" : string.Empty)}";
		}
	}

	public static VersionInfo Get()
	{
		var assembly = Assembly.GetExecutingAssembly();
		if (assembly == null) {
			return VersionInfo.Empty;
		}

		var verinfo = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
		if (verinfo == null) {
			return VersionInfo.Empty;
		}

		var items = verinfo.InformationalVersion.Split('+', 2);
		var version = items[0];
		var hash = items.Length > 1 ? items[1] : string.Empty;
		var modified = hash[^1] == '+';

		return new VersionInfo { Version = version, GitHash = hash, GitModified = modified };
	}
}
