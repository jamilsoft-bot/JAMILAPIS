<?php

namespace JamiApis\GoogleDrive;

use Google_Client;
use Google_Service_Drive;
use Google_Service_Drive_DriveFile;
use Google_Service_Exception;

class GoogleDriveClient
{
    private Google_Service_Drive $drive;
    private ?string $rootFolderId;
    private bool $supportsAllDrives;
    private int $maxRetries;
    private int $retryDelayMs;

    public function __construct(
        ?string $credentialsPath = null,
        ?string $rootFolderId = null,
        ?bool $supportsAllDrives = null,
        int $maxRetries = 3,
        int $retryDelayMs = 500
    ) {
        $credentialsPath = $credentialsPath ?: getenv('GOOGLE_APPLICATION_CREDENTIALS') ?: null;
        if (!$credentialsPath) {
            throw new \InvalidArgumentException('Missing GOOGLE_APPLICATION_CREDENTIALS path.');
        }

        $this->rootFolderId = $rootFolderId ?: getenv('GOOGLE_DRIVE_ROOT_FOLDER_ID') ?: null;
        $supportsEnv = getenv('GOOGLE_DRIVE_SUPPORTS_ALL_DRIVES');
        $this->supportsAllDrives = $supportsAllDrives ?? ($supportsEnv === 'true' || $supportsEnv === '1');
        $this->maxRetries = $maxRetries;
        $this->retryDelayMs = $retryDelayMs;

        $client = new Google_Client();
        $client->setApplicationName('JamiApis Google Drive PHP');
        $client->setAuthConfig($credentialsPath);
        $client->setScopes(['https://www.googleapis.com/auth/drive']);

        $this->drive = new Google_Service_Drive($client);
    }

    public function uploadFile(string $localPath, string $mimeType, ?string $parentFolderId = null): array
    {
        if (!file_exists($localPath)) {
            throw new \InvalidArgumentException("File not found: {$localPath}");
        }

        $metadata = new Google_Service_Drive_DriveFile([
            'name' => basename($localPath),
            'parents' => $this->buildParents($parentFolderId),
        ]);

        return $this->withRetry(function () use ($metadata, $localPath, $mimeType) {
            $result = $this->drive->files->create($metadata, [
                'data' => file_get_contents($localPath),
                'mimeType' => $mimeType,
                'uploadType' => 'multipart',
                'supportsAllDrives' => $this->supportsAllDrives,
                'fields' => 'id,name,parents,webViewLink',
            ]);

            return $result->toSimpleObject();
        }, 'uploadFile');
    }

    public function listFiles(?string $query = null, int $pageSize = 20): array
    {
        $qParts = [];
        if ($this->rootFolderId) {
            $qParts[] = sprintf("'%s' in parents", $this->rootFolderId);
        }
        if ($query) {
            $qParts[] = $query;
        }
        $q = $qParts ? implode(' and ', $qParts) : null;

        return $this->withRetry(function () use ($q, $pageSize) {
            $response = $this->drive->files->listFiles([
                'q' => $q,
                'pageSize' => $pageSize,
                'fields' => 'files(id,name,mimeType,modifiedTime,parents)',
                'supportsAllDrives' => $this->supportsAllDrives,
                'includeItemsFromAllDrives' => $this->supportsAllDrives,
            ]);

            return $response->getFiles();
        }, 'listFiles');
    }

    public function getFileMeta(string $fileId): array
    {
        return $this->withRetry(function () use ($fileId) {
            $file = $this->drive->files->get($fileId, [
                'fields' => 'id,name,mimeType,size,modifiedTime,parents,webViewLink',
                'supportsAllDrives' => $this->supportsAllDrives,
            ]);

            return $file->toSimpleObject();
        }, 'getFileMeta');
    }

    public function downloadFile(string $fileId, string $destPath): void
    {
        $this->withRetry(function () use ($fileId, $destPath) {
            $response = $this->drive->files->get($fileId, [
                'alt' => 'media',
                'supportsAllDrives' => $this->supportsAllDrives,
            ]);

            file_put_contents($destPath, $response->getBody()->getContents());
        }, 'downloadFile');
    }

    public function updateFile(string $fileId, ?string $newLocalPath = null, ?string $newName = null): array
    {
        if ($newLocalPath && !file_exists($newLocalPath)) {
            throw new \InvalidArgumentException("File not found: {$newLocalPath}");
        }

        $metadata = new Google_Service_Drive_DriveFile();
        if ($newName) {
            $metadata->setName($newName);
        }

        return $this->withRetry(function () use ($fileId, $metadata, $newLocalPath) {
            $params = [
                'supportsAllDrives' => $this->supportsAllDrives,
                'fields' => 'id,name,mimeType,modifiedTime',
            ];

            if ($newLocalPath) {
                $params['data'] = file_get_contents($newLocalPath);
                $params['uploadType'] = 'multipart';
                $params['mimeType'] = mime_content_type($newLocalPath) ?: 'application/octet-stream';
            }

            $file = $this->drive->files->update($fileId, $metadata, $params);

            return $file->toSimpleObject();
        }, 'updateFile');
    }

    public function deleteFile(string $fileId): void
    {
        $this->withRetry(function () use ($fileId) {
            $this->drive->files->delete($fileId, [
                'supportsAllDrives' => $this->supportsAllDrives,
            ]);
        }, 'deleteFile');
    }

    public function createFolder(string $name, ?string $parentFolderId = null): array
    {
        $metadata = new Google_Service_Drive_DriveFile([
            'name' => $name,
            'mimeType' => 'application/vnd.google-apps.folder',
            'parents' => $this->buildParents($parentFolderId),
        ]);

        return $this->withRetry(function () use ($metadata) {
            $folder = $this->drive->files->create($metadata, [
                'supportsAllDrives' => $this->supportsAllDrives,
                'fields' => 'id,name,parents',
            ]);

            return $folder->toSimpleObject();
        }, 'createFolder');
    }

    private function buildParents(?string $parentFolderId): ?array
    {
        $parent = $parentFolderId ?: $this->rootFolderId;
        return $parent ? [$parent] : null;
    }

    private function withRetry(callable $fn, string $context)
    {
        $attempt = 0;
        $lastException = null;

        while ($attempt < $this->maxRetries) {
            try {
                return $fn();
            } catch (Google_Service_Exception $e) {
                $lastException = $e;
                $code = $e->getCode();
                if (!$this->isRetryable($code)) {
                    throw new \RuntimeException("Drive API error ({$context}): {$e->getMessage()}", $code, $e);
                }
            } catch (\Exception $e) {
                $lastException = $e;
                throw new \RuntimeException("Unexpected error ({$context}): {$e->getMessage()}", $e->getCode(), $e);
            }

            $attempt++;
            usleep($this->retryDelayMs * 1000);
        }

        throw new \RuntimeException("Drive API failed after {$this->maxRetries} attempts ({$context}).", 0, $lastException);
    }

    private function isRetryable(int $statusCode): bool
    {
        return in_array($statusCode, [429, 500, 502, 503, 504], true);
    }
}
