# Google Drive PHP SDK

Service-account based Google Drive API v3 wrapper with a CLI demo.

## Install

```bash
composer install
```

## Configuration

Create `.env` based on `.env.example`:

```
GOOGLE_APPLICATION_CREDENTIALS=/absolute/path/service-account.json
GOOGLE_DRIVE_ROOT_FOLDER_ID=your_shared_folder_id
GOOGLE_DRIVE_SUPPORTS_ALL_DRIVES=true
```

## CLI Demo

```bash
php demo.php upload ./file.txt text/plain
php demo.php list
php demo.php download <fileId> ./downloaded.bin
php demo.php update <fileId> ./newfile.txt
php demo.php delete <fileId>
php demo.php meta <fileId>
```

## Library Usage

```php
use JamiApis\GoogleDrive\GoogleDriveClient;

$client = new GoogleDriveClient();
$uploaded = $client->uploadFile('./file.txt', 'text/plain');
$files = $client->listFiles();
```

## Testing

```bash
vendor/bin/phpunit
```
