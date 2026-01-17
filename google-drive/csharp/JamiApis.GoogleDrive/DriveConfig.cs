namespace JamiApis.GoogleDrive;

public sealed class DriveConfig
{
    public string CredentialsPath { get; }
    public string? RootFolderId { get; }
    public bool SupportsAllDrives { get; }
    public int MaxRetries { get; }
    public int RetryDelayMs { get; }

    public DriveConfig(
        string credentialsPath,
        string? rootFolderId,
        bool supportsAllDrives,
        int maxRetries,
        int retryDelayMs)
    {
        CredentialsPath = credentialsPath;
        RootFolderId = rootFolderId;
        SupportsAllDrives = supportsAllDrives;
        MaxRetries = maxRetries;
        RetryDelayMs = retryDelayMs;
    }
}
