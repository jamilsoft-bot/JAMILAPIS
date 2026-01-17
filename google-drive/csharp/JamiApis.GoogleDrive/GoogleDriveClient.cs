using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.Upload;

namespace JamiApis.GoogleDrive;

public sealed class GoogleDriveClient
{
    private static readonly HashSet<int> RetryableCodes = new() { 429, 500, 502, 503, 504 };
    private readonly DriveService _service;
    public DriveConfig Config { get; }

    public GoogleDriveClient(
        string? credentialsPath = null,
        string? rootFolderId = null,
        bool? supportsAllDrives = null,
        int maxRetries = 3,
        int retryDelayMs = 500)
    {
        credentialsPath ??= Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        if (string.IsNullOrWhiteSpace(credentialsPath))
        {
            throw new ArgumentException("Missing GOOGLE_APPLICATION_CREDENTIALS path.");
        }

        var supportsEnv = Environment.GetEnvironmentVariable("GOOGLE_DRIVE_SUPPORTS_ALL_DRIVES");
        var supportsFlag = supportsAllDrives ?? (supportsEnv == "true" || supportsEnv == "1");

        Config = new DriveConfig(
            credentialsPath,
            rootFolderId ?? Environment.GetEnvironmentVariable("GOOGLE_DRIVE_ROOT_FOLDER_ID"),
            supportsFlag,
            maxRetries,
            retryDelayMs);

        var credential = GoogleCredential.FromFile(Config.CredentialsPath)
            .CreateScoped(DriveService.Scope.Drive);

        _service = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "JamiApis Google Drive C#"
        });
    }

    public async Task<File> UploadFileAsync(string localPath, string mimeType, string? parentFolderId = null)
    {
        if (!System.IO.File.Exists(localPath))
        {
            throw new ArgumentException($"File not found: {localPath}");
        }

        var fileMetadata = new File
        {
            Name = Path.GetFileName(localPath),
            Parents = BuildParents(parentFolderId)
        };

        return await WithRetryAsync(async () =>
        {
            await using var stream = new FileStream(localPath, FileMode.Open, FileAccess.Read);
            var request = _service.Files.Create(fileMetadata, stream, mimeType);
            request.SupportsAllDrives = Config.SupportsAllDrives;
            request.Fields = "id,name,parents,webViewLink";
            var result = await request.UploadAsync();
            if (result.Status != UploadStatus.Completed)
            {
                throw new IOException($"Upload failed: {result.Exception?.Message}");
            }

            return request.ResponseBody;
        }, "uploadFile");
    }

    public async Task<IList<File>> ListFilesAsync(string? query = null, int pageSize = 20)
    {
        var qParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(Config.RootFolderId))
        {
            qParts.Add($"'{Config.RootFolderId}' in parents");
        }
        if (!string.IsNullOrWhiteSpace(query))
        {
            qParts.Add(query);
        }

        return await WithRetryAsync(async () =>
        {
            var request = _service.Files.List();
            request.Q = qParts.Count > 0 ? string.Join(" and ", qParts) : null;
            request.PageSize = pageSize;
            request.Fields = "files(id,name,mimeType,modifiedTime,parents)";
            request.SupportsAllDrives = Config.SupportsAllDrives;
            request.IncludeItemsFromAllDrives = Config.SupportsAllDrives;
            var response = await request.ExecuteAsync();
            return response.Files ?? new List<File>();
        }, "listFiles");
    }

    public async Task<File> GetFileMetaAsync(string fileId)
    {
        return await WithRetryAsync(async () =>
        {
            var request = _service.Files.Get(fileId);
            request.Fields = "id,name,mimeType,size,modifiedTime,parents,webViewLink";
            request.SupportsAllDrives = Config.SupportsAllDrives;
            return await request.ExecuteAsync();
        }, "getFileMeta");
    }

    public async Task DownloadFileAsync(string fileId, string destPath)
    {
        await WithRetryAsync(async () =>
        {
            var request = _service.Files.Get(fileId);
            request.SupportsAllDrives = Config.SupportsAllDrives;
            await using var stream = new FileStream(destPath, FileMode.Create, FileAccess.Write);
            await request.DownloadAsync(stream);
            return true;
        }, "downloadFile");
    }

    public async Task<File> UpdateFileAsync(string fileId, string? newLocalPath = null, string? newName = null)
    {
        if (!string.IsNullOrWhiteSpace(newLocalPath) && !System.IO.File.Exists(newLocalPath))
        {
            throw new ArgumentException($"File not found: {newLocalPath}");
        }

        var metadata = new File();
        if (!string.IsNullOrWhiteSpace(newName))
        {
            metadata.Name = newName;
        }

        return await WithRetryAsync(async () =>
        {
            if (!string.IsNullOrWhiteSpace(newLocalPath))
            {
                await using var stream = new FileStream(newLocalPath, FileMode.Open, FileAccess.Read);
                var request = _service.Files.Update(metadata, fileId, stream, "application/octet-stream");
                request.SupportsAllDrives = Config.SupportsAllDrives;
                request.Fields = "id,name,mimeType,modifiedTime";
                var result = await request.UploadAsync();
                if (result.Status != UploadStatus.Completed)
                {
                    throw new IOException($"Update failed: {result.Exception?.Message}");
                }

                return request.ResponseBody;
            }

            var metadataRequest = _service.Files.Update(metadata, fileId);
            metadataRequest.SupportsAllDrives = Config.SupportsAllDrives;
            metadataRequest.Fields = "id,name,mimeType,modifiedTime";
            return await metadataRequest.ExecuteAsync();
        }, "updateFile");
    }

    public async Task DeleteFileAsync(string fileId)
    {
        await WithRetryAsync(async () =>
        {
            var request = _service.Files.Delete(fileId);
            request.SupportsAllDrives = Config.SupportsAllDrives;
            await request.ExecuteAsync();
            return true;
        }, "deleteFile");
    }

    public async Task<File> CreateFolderAsync(string name, string? parentFolderId = null)
    {
        var metadata = new File
        {
            Name = name,
            MimeType = "application/vnd.google-apps.folder",
            Parents = BuildParents(parentFolderId)
        };

        return await WithRetryAsync(async () =>
        {
            var request = _service.Files.Create(metadata);
            request.SupportsAllDrives = Config.SupportsAllDrives;
            request.Fields = "id,name,parents";
            return await request.ExecuteAsync();
        }, "createFolder");
    }

    private IList<string>? BuildParents(string? parentFolderId)
    {
        var parent = parentFolderId ?? Config.RootFolderId;
        return string.IsNullOrWhiteSpace(parent) ? null : new List<string> { parent };
    }

    private async Task<T> WithRetryAsync<T>(Func<Task<T>> fn, string context)
    {
        Exception? lastError = null;
        for (var attempt = 0; attempt < Config.MaxRetries; attempt++)
        {
            try
            {
                return await fn();
            }
            catch (Google.GoogleApiException apiException)
            {
                lastError = apiException;
                if (!RetryableCodes.Contains(apiException.HttpStatusCode.HasValue ? (int)apiException.HttpStatusCode.Value : 0))
                {
                    throw new InvalidOperationException($"Drive API error ({context}): {apiException.Message}", apiException);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error ({context}): {ex.Message}", ex);
            }

            await Task.Delay(Config.RetryDelayMs);
        }

        throw new InvalidOperationException($"Drive API failed after {Config.MaxRetries} attempts ({context}).", lastError);
    }
}
