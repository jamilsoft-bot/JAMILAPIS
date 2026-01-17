<?php

require_once __DIR__ . '/vendor/autoload.php';

use JamiApis\GoogleDrive\GoogleDriveClient;

function loadEnv(string $path): void
{
    if (!file_exists($path)) {
        return;
    }

    $lines = file($path, FILE_IGNORE_NEW_LINES | FILE_SKIP_EMPTY_LINES);
    foreach ($lines as $line) {
        if (str_starts_with(trim($line), '#')) {
            continue;
        }
        [$key, $value] = array_map('trim', explode('=', $line, 2));
        if (!getenv($key)) {
            putenv("{$key}={$value}");
        }
    }
}

loadEnv(__DIR__ . '/.env');

$usage = <<<TXT
Google Drive PHP Demo

Commands:
  php demo.php upload <path> <mimeType>
  php demo.php list
  php demo.php download <fileId> <dest>
  php demo.php update <fileId> <path>
  php demo.php delete <fileId>
  php demo.php meta <fileId>
TXT;

if ($argc < 2) {
    echo $usage;
    exit(1);
}

$command = $argv[1];
$client = new GoogleDriveClient();

try {
    switch ($command) {
        case 'upload':
            if ($argc < 4) {
                throw new InvalidArgumentException('upload requires <path> <mimeType>');
            }
            $result = $client->uploadFile($argv[2], $argv[3]);
            echo "Uploaded: {$result->id} ({$result->name})\n";
            break;
        case 'list':
            $files = $client->listFiles();
            foreach ($files as $file) {
                echo "{$file->getId()} {$file->getName()} {$file->getMimeType()}\n";
            }
            break;
        case 'download':
            if ($argc < 4) {
                throw new InvalidArgumentException('download requires <fileId> <dest>');
            }
            $client->downloadFile($argv[2], $argv[3]);
            echo "Downloaded to {$argv[3]}\n";
            break;
        case 'update':
            if ($argc < 4) {
                throw new InvalidArgumentException('update requires <fileId> <path>');
            }
            $result = $client->updateFile($argv[2], $argv[3]);
            echo "Updated: {$result->id} ({$result->name})\n";
            break;
        case 'delete':
            if ($argc < 3) {
                throw new InvalidArgumentException('delete requires <fileId>');
            }
            $client->deleteFile($argv[2]);
            echo "Deleted: {$argv[2]}\n";
            break;
        case 'meta':
            if ($argc < 3) {
                throw new InvalidArgumentException('meta requires <fileId>');
            }
            $meta = $client->getFileMeta($argv[2]);
            echo json_encode($meta, JSON_PRETTY_PRINT) . "\n";
            break;
        default:
            echo $usage;
            exit(1);
    }
} catch (Throwable $e) {
    fwrite(STDERR, "Error: {$e->getMessage()}\n");
    exit(1);
}
