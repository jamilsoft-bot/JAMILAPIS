import assert from 'node:assert/strict';
import { test } from 'node:test';
import { GoogleDriveClient } from '../src/googleDriveClient.js';

test('constructor requires credentials', () => {
  assert.throws(() => new GoogleDriveClient({ credentialsPath: '' }), /Missing GOOGLE_APPLICATION_CREDENTIALS/);
});

test('mocking example placeholder', () => {
  // Example: mock GoogleDriveClient.drive with a fake implementation.
  // const client = new GoogleDriveClient({ credentialsPath: '/tmp/key.json' });
  // client.drive = { files: { list: async () => ({ data: { files: [] } }) } };
  assert.ok(true);
});
