# Google Drive C# SDK + Demo

Service-account based Google Drive API v3 wrapper with a console demo.

## Setup

Copy `.env.example` to `.env` and export variables (or load them in your shell):

```
GOOGLE_APPLICATION_CREDENTIALS=/absolute/path/service-account.json
GOOGLE_DRIVE_ROOT_FOLDER_ID=your_shared_folder_id
GOOGLE_DRIVE_SUPPORTS_ALL_DRIVES=true
```

## Build & Run

```bash
dotnet build JamiApis.GoogleDrive.sln

dotnet run --project JamiApis.GoogleDrive.Demo upload ./file.txt text/plain
dotnet run --project JamiApis.GoogleDrive.Demo list
dotnet run --project JamiApis.GoogleDrive.Demo download <id> ./downloaded.bin
dotnet run --project JamiApis.GoogleDrive.Demo update <id> ./newfile.txt
dotnet run --project JamiApis.GoogleDrive.Demo delete <id>
```

## Library Usage

```csharp
var client = new GoogleDriveClient();
var files = await client.ListFilesAsync();
```
