import fs from 'node:fs';
import path from 'node:path';
import { google } from 'googleapis';

const RETRYABLE_CODES = new Set([429, 500, 502, 503, 504]);

export class GoogleDriveClient {
  constructor({
    credentialsPath = process.env.GOOGLE_APPLICATION_CREDENTIALS,
    rootFolderId = process.env.GOOGLE_DRIVE_ROOT_FOLDER_ID,
    supportsAllDrives = ['true', '1'].includes(process.env.GOOGLE_DRIVE_SUPPORTS_ALL_DRIVES),
    maxRetries = 3,
    retryDelayMs = 500
  } = {}) {
    if (!credentialsPath) {
      throw new Error('Missing GOOGLE_APPLICATION_CREDENTIALS path.');
    }

    this.rootFolderId = rootFolderId || null;
    this.supportsAllDrives = Boolean(supportsAllDrives);
    this.maxRetries = maxRetries;
    this.retryDelayMs = retryDelayMs;

    const auth = new google.auth.GoogleAuth({
      keyFile: credentialsPath,
      scopes: ['https://www.googleapis.com/auth/drive']
    });

    this.drive = google.drive({ version: 'v3', auth });
  }

  async uploadFile(localPath, mimeType, parentFolderId = null) {
    if (!fs.existsSync(localPath)) {
      throw new Error(`File not found: ${localPath}`);
    }

    const parents = this.buildParents(parentFolderId);
    const requestBody = { name: path.basename(localPath), parents };

    return this.withRetry(async () => {
      const response = await this.drive.files.create({
        requestBody,
        media: { mimeType, body: fs.createReadStream(localPath) },
        fields: 'id,name,parents,webViewLink',
        supportsAllDrives: this.supportsAllDrives
      });

      return response.data;
    }, 'uploadFile');
  }

  async listFiles(query = null, pageSize = 20) {
    const parts = [];
    if (this.rootFolderId) {
      parts.push(`'${this.rootFolderId}' in parents`);
    }
    if (query) {
      parts.push(query);
    }
    const q = parts.length ? parts.join(' and ') : undefined;

    return this.withRetry(async () => {
      const response = await this.drive.files.list({
        q,
        pageSize,
        fields: 'files(id,name,mimeType,modifiedTime,parents)',
        supportsAllDrives: this.supportsAllDrives,
        includeItemsFromAllDrives: this.supportsAllDrives
      });

      return response.data.files || [];
    }, 'listFiles');
  }

  async getFileMeta(fileId) {
    return this.withRetry(async () => {
      const response = await this.drive.files.get({
        fileId,
        fields: 'id,name,mimeType,size,modifiedTime,parents,webViewLink',
        supportsAllDrives: this.supportsAllDrives
      });

      return response.data;
    }, 'getFileMeta');
  }

  async downloadFile(fileId, destPath) {
    return this.withRetry(async () => {
      const response = await this.drive.files.get(
        { fileId, alt: 'media', supportsAllDrives: this.supportsAllDrives },
        { responseType: 'stream' }
      );

      await new Promise((resolve, reject) => {
        const writeStream = fs.createWriteStream(destPath);
        response.data.pipe(writeStream);
        response.data.on('error', reject);
        writeStream.on('finish', resolve);
      });
    }, 'downloadFile');
  }

  async updateFile(fileId, newLocalPath = null, newName = null) {
    if (newLocalPath && !fs.existsSync(newLocalPath)) {
      throw new Error(`File not found: ${newLocalPath}`);
    }

    return this.withRetry(async () => {
      const requestBody = {};
      if (newName) {
        requestBody.name = newName;
      }

      const params = {
        fileId,
        requestBody,
        fields: 'id,name,mimeType,modifiedTime',
        supportsAllDrives: this.supportsAllDrives
      };

      if (newLocalPath) {
        params.media = {
          mimeType: 'application/octet-stream',
          body: fs.createReadStream(newLocalPath)
        };
      }

      const response = await this.drive.files.update(params);
      return response.data;
    }, 'updateFile');
  }

  async deleteFile(fileId) {
    return this.withRetry(async () => {
      await this.drive.files.delete({ fileId, supportsAllDrives: this.supportsAllDrives });
    }, 'deleteFile');
  }

  async createFolder(name, parentFolderId = null) {
    const parents = this.buildParents(parentFolderId);

    return this.withRetry(async () => {
      const response = await this.drive.files.create({
        requestBody: {
          name,
          parents,
          mimeType: 'application/vnd.google-apps.folder'
        },
        fields: 'id,name,parents',
        supportsAllDrives: this.supportsAllDrives
      });

      return response.data;
    }, 'createFolder');
  }

  buildParents(parentFolderId) {
    const parent = parentFolderId || this.rootFolderId;
    return parent ? [parent] : undefined;
  }

  async withRetry(fn, context) {
    let attempt = 0;
    let lastError;

    while (attempt < this.maxRetries) {
      try {
        return await fn();
      } catch (error) {
        lastError = error;
        const status = error?.code || error?.response?.status;
        if (!RETRYABLE_CODES.has(status)) {
          throw new Error(`Drive API error (${context}): ${error.message}`);
        }
      }

      attempt += 1;
      await new Promise((resolve) => setTimeout(resolve, this.retryDelayMs));
    }

    throw new Error(`Drive API failed after ${this.maxRetries} attempts (${context}). ${lastError?.message || ''}`);
  }
}
