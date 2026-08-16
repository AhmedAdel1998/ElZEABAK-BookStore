namespace BookStore.Shared.Constants;

/// <summary>
/// Resolves writable application data paths.
/// </summary>
public static class ApplicationPaths
{
    private const string DataDirectoryEnvironmentVariable = "BOOKSTORE_DATA_DIR";

    /// <summary>Gets the writable application data root.</summary>
    public static string GetDataRoot()
    {
        var configured = Environment.GetEnvironmentVariable(DataDirectoryEnvironmentVariable);
        return string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ApplicationConstants.ApplicationName)
            : Environment.ExpandEnvironmentVariables(configured);
    }

    /// <summary>Resolves a path under the writable data root when it is relative.</summary>
    public static string ResolveDataPath(string path)
    {
        var expanded = Environment.ExpandEnvironmentVariables(path);
        return Path.GetFullPath(Path.IsPathRooted(expanded) ? expanded : Path.Combine(GetDataRoot(), expanded));
    }
}
