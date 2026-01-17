import fs from 'node:fs';
import express from 'express';
import multer from 'multer';
import dotenv from 'dotenv';
import { GoogleDriveClient } from '../src/googleDriveClient.js';

dotenv.config();

const app = express();
const uploadDir = 'uploads';
const downloadDir = 'downloads';
fs.mkdirSync(uploadDir, { recursive: true });
fs.mkdirSync(downloadDir, { recursive: true });
const upload = multer({ dest: uploadDir });
const client = new GoogleDriveClient();

app.use(express.json());

app.post('/upload', upload.single('file'), async (req, res) => {
  try {
    if (!req.file) {
      return res.status(400).json({ error: 'Missing file.' });
    }

    const result = await client.uploadFile(req.file.path, req.file.mimetype);
    return res.json(result);
  } catch (error) {
    return res.status(500).json({ error: error.message });
  }
});

app.get('/files', async (req, res) => {
  try {
    const files = await client.listFiles();
    return res.json(files);
  } catch (error) {
    return res.status(500).json({ error: error.message });
  }
});

app.get('/files/:id', async (req, res) => {
  try {
    const meta = await client.getFileMeta(req.params.id);
    return res.json(meta);
  } catch (error) {
    return res.status(500).json({ error: error.message });
  }
});

app.get('/files/:id/download', async (req, res) => {
  try {
    const dest = `${downloadDir}/${req.params.id}`;
    await client.downloadFile(req.params.id, dest);
    return res.download(dest);
  } catch (error) {
    return res.status(500).json({ error: error.message });
  }
});

app.put('/files/:id', upload.single('file'), async (req, res) => {
  try {
    if (!req.file) {
      return res.status(400).json({ error: 'Missing file.' });
    }

    const updated = await client.updateFile(req.params.id, req.file.path);
    return res.json(updated);
  } catch (error) {
    return res.status(500).json({ error: error.message });
  }
});

app.delete('/files/:id', async (req, res) => {
  try {
    await client.deleteFile(req.params.id);
    return res.json({ status: 'deleted', id: req.params.id });
  } catch (error) {
    return res.status(500).json({ error: error.message });
  }
});

const port = process.env.PORT || 3000;
app.listen(port, () => {
  console.log(`Drive demo listening on port ${port}`);
});
