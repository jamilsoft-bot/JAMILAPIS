# Google Drive Node.js SDK + Express Demo

Service-account based Google Drive API v3 wrapper with an Express demo.

## Install

```bash
npm install
```

## Configuration

Copy `.env.example` from `demo/` to `demo/.env`:

```
GOOGLE_APPLICATION_CREDENTIALS=/absolute/path/service-account.json
GOOGLE_DRIVE_ROOT_FOLDER_ID=your_shared_folder_id
GOOGLE_DRIVE_SUPPORTS_ALL_DRIVES=true
PORT=3000
```

## Express Demo

```bash
npm run start
```

### Example cURL

```bash
curl -F "file=@./file.txt" http://localhost:3000/upload
curl http://localhost:3000/files
curl http://localhost:3000/files/<fileId>
curl -O http://localhost:3000/files/<fileId>/download
curl -X PUT -F "file=@./newfile.txt" http://localhost:3000/files/<fileId>
curl -X DELETE http://localhost:3000/files/<fileId>
```

## Library Usage

```js
import { GoogleDriveClient } from './src/index.js';

const client = new GoogleDriveClient();
const files = await client.listFiles();
```

## Testing

```bash
npm test
```
