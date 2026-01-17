# Google Drive Python SDK

Service-account based Google Drive API v3 wrapper with a CLI demo.

## Install

```bash
python -m venv .venv
source .venv/bin/activate
pip install -e .
```

## Configuration

Copy `.env.example` to `.env`:

```
GOOGLE_APPLICATION_CREDENTIALS=/absolute/path/service-account.json
GOOGLE_DRIVE_ROOT_FOLDER_ID=your_shared_folder_id
GOOGLE_DRIVE_SUPPORTS_ALL_DRIVES=true
```

## CLI Demo

```bash
python demo.py upload ./file.txt text/plain
python demo.py list
python demo.py download <fileId> ./downloaded.bin
python demo.py update <fileId> ./newfile.txt
python demo.py delete <fileId>
python demo.py meta <fileId>
```

## Library Usage

```python
from jamiapis_gdrive import GoogleDriveClient

client = GoogleDriveClient()
files = client.list_files()
```

## Testing

```bash
pip install -e .[dev]
pytest
```
