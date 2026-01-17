<?php

use JamiApis\GoogleDrive\GoogleDriveClient;
use PHPUnit\Framework\TestCase;

class GoogleDriveClientTest extends TestCase
{
    public function testConstructorRequiresCredentials(): void
    {
        $this->expectException(InvalidArgumentException::class);
        new GoogleDriveClient(null, null, false);
    }

    public function testListFilesReturnsArray(): void
    {
        $this->markTestSkipped('Provide credentials and network access to test Drive API.');
    }
}
