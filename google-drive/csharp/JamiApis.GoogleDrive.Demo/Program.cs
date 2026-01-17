using JamiApis.GoogleDrive;

static void PrintUsage()
{
    Console.WriteLine("Google Drive C# Demo");
    Console.WriteLine("dotnet run upload <path> <mimeType>");
    Console.WriteLine("dotnet run list");
    Console.WriteLine("dotnet run download <id> <dest>");
    Console.WriteLine("dotnet run update <id> <path>");
    Console.WriteLine("dotnet run delete <id>");
    Console.WriteLine("dotnet run meta <id>");
}

if (args.Length < 1)
{
    PrintUsage();
    return;
}

var client = new GoogleDriveClient();
var command = args[0];

try
{
    switch (command)
    {
        case "upload":
            if (args.Length < 3)
            {
                throw new ArgumentException("upload requires <path> <mimeType>");
            }
            var uploaded = await client.UploadFileAsync(args[1], args[2]);
            Console.WriteLine($"Uploaded: {uploaded.Id} ({uploaded.Name})");
            break;
        case "list":
            var files = await client.ListFilesAsync();
            foreach (var file in files)
            {
                Console.WriteLine($"{file.Id} {file.Name} {file.MimeType}");
            }
            break;
        case "download":
            if (args.Length < 3)
            {
                throw new ArgumentException("download requires <id> <dest>");
            }
            await client.DownloadFileAsync(args[1], args[2]);
            Console.WriteLine($"Downloaded to {args[2]}");
            break;
        case "update":
            if (args.Length < 3)
            {
                throw new ArgumentException("update requires <id> <path>");
            }
            var updated = await client.UpdateFileAsync(args[1], args[2]);
            Console.WriteLine($"Updated: {updated.Id} ({updated.Name})");
            break;
        case "delete":
            if (args.Length < 2)
            {
                throw new ArgumentException("delete requires <id>");
            }
            await client.DeleteFileAsync(args[1]);
            Console.WriteLine($"Deleted: {args[1]}");
            break;
        case "meta":
            if (args.Length < 2)
            {
                throw new ArgumentException("meta requires <id>");
            }
            var meta = await client.GetFileMetaAsync(args[1]);
            Console.WriteLine($"{meta.Id} {meta.Name} {meta.MimeType}");
            break;
        default:
            PrintUsage();
            break;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
}
