import io
import os
import time
from dataclasses import dataclass
from typing import Optional, List, Dict, Any, Callable

from google.oauth2 import service_account
from googleapiclient.discovery import build
from googleapiclient.errors import HttpError
from googleapiclient.http import MediaFileUpload, MediaIoBaseDownload

RETRYABLE_CODES = {429, 500, 502, 503, 504}


@dataclass
class DriveConfig:
    credentials_path: str
    root_folder_id: Optional[str] = None
    supports_all_drives: bool = False
    max_retries: int = 3
    retry_delay_ms: int = 500


class GoogleDriveClient:
    def __init__(
        self,
        credentials_path: Optional[str] = None,
        root_folder_id: Optional[str] = None,
        supports_all_drives: Optional[bool] = None,
        max_retries: int = 3,
        retry_delay_ms: int = 500,
    ) -> None:
        credentials_path = credentials_path or os.getenv("GOOGLE_APPLICATION_CREDENTIALS")
        if not credentials_path:
            raise ValueError("Missing GOOGLE_APPLICATION_CREDENTIALS path.")

        supports_env = os.getenv("GOOGLE_DRIVE_SUPPORTS_ALL_DRIVES", "false").lower() in {"1", "true"}
        self.config = DriveConfig(
            credentials_path=credentials_path,
            root_folder_id=root_folder_id or os.getenv("GOOGLE_DRIVE_ROOT_FOLDER_ID"),
            supports_all_drives=supports_all_drives if supports_all_drives is not None else supports_env,
            max_retries=max_retries,
            retry_delay_ms=retry_delay_ms,
        )

        credentials = service_account.Credentials.from_service_account_file(
            self.config.credentials_path,
            scopes=["https://www.googleapis.com/auth/drive"],
        )
        self.drive = build("drive", "v3", credentials=credentials)

    def upload_file(self, local_path: str, mime_type: str, parent_folder_id: Optional[str] = None) -> Dict[str, Any]:
        if not os.path.exists(local_path):
            raise ValueError(f"File not found: {local_path}")

        metadata = {
            "name": os.path.basename(local_path),
            "parents": self._build_parents(parent_folder_id),
        }
        media = MediaFileUpload(local_path, mimetype=mime_type, resumable=False)

        def action():
            request = self.drive.files().create(
                body=metadata,
                media_body=media,
                fields="id,name,parents,webViewLink",
                supportsAllDrives=self.config.supports_all_drives,
            )
            return request.execute()

        return self._with_retry(action, "upload_file")

    def list_files(self, query: Optional[str] = None, page_size: int = 20) -> List[Dict[str, Any]]:
        q_parts = []
        if self.config.root_folder_id:
            q_parts.append(f"'{self.config.root_folder_id}' in parents")
        if query:
            q_parts.append(query)
        q = " and ".join(q_parts) if q_parts else None

        def action():
            request = self.drive.files().list(
                q=q,
                pageSize=page_size,
                fields="files(id,name,mimeType,modifiedTime,parents)",
                supportsAllDrives=self.config.supports_all_drives,
                includeItemsFromAllDrives=self.config.supports_all_drives,
            )
            result = request.execute()
            return result.get("files", [])

        return self._with_retry(action, "list_files")

    def get_file_meta(self, file_id: str) -> Dict[str, Any]:
        def action():
            request = self.drive.files().get(
                fileId=file_id,
                fields="id,name,mimeType,size,modifiedTime,parents,webViewLink",
                supportsAllDrives=self.config.supports_all_drives,
            )
            return request.execute()

        return self._with_retry(action, "get_file_meta")

    def download_file(self, file_id: str, dest_path: str) -> None:
        def action():
            request = self.drive.files().get(fileId=file_id, alt="media", supportsAllDrives=self.config.supports_all_drives)
            with io.FileIO(dest_path, "wb") as fh:
                downloader = MediaIoBaseDownload(fh, request)
                done = False
                while not done:
                    _, done = downloader.next_chunk()

        self._with_retry(action, "download_file")

    def update_file(self, file_id: str, new_local_path: Optional[str] = None, new_name: Optional[str] = None) -> Dict[str, Any]:
        if new_local_path and not os.path.exists(new_local_path):
            raise ValueError(f"File not found: {new_local_path}")

        body = {}
        if new_name:
            body["name"] = new_name

        media = None
        if new_local_path:
            media = MediaFileUpload(new_local_path, resumable=False)

        def action():
            request = self.drive.files().update(
                fileId=file_id,
                body=body or None,
                media_body=media,
                fields="id,name,mimeType,modifiedTime",
                supportsAllDrives=self.config.supports_all_drives,
            )
            return request.execute()

        return self._with_retry(action, "update_file")

    def delete_file(self, file_id: str) -> None:
        def action():
            request = self.drive.files().delete(fileId=file_id, supportsAllDrives=self.config.supports_all_drives)
            request.execute()

        self._with_retry(action, "delete_file")

    def create_folder(self, name: str, parent_folder_id: Optional[str] = None) -> Dict[str, Any]:
        metadata = {
            "name": name,
            "mimeType": "application/vnd.google-apps.folder",
            "parents": self._build_parents(parent_folder_id),
        }

        def action():
            request = self.drive.files().create(
                body=metadata,
                fields="id,name,parents",
                supportsAllDrives=self.config.supports_all_drives,
            )
            return request.execute()

        return self._with_retry(action, "create_folder")

    def _build_parents(self, parent_folder_id: Optional[str]) -> Optional[List[str]]:
        parent = parent_folder_id or self.config.root_folder_id
        return [parent] if parent else None

    def _with_retry(self, fn: Callable[[], Any], context: str) -> Any:
        last_error = None
        for attempt in range(self.config.max_retries):
            try:
                return fn()
            except HttpError as error:
                last_error = error
                status = getattr(error, "status_code", None) or getattr(error, "resp", None).status
                if status not in RETRYABLE_CODES:
                    raise RuntimeError(f"Drive API error ({context}): {error}") from error
            except Exception as error:
                raise RuntimeError(f"Unexpected error ({context}): {error}") from error

            time.sleep(self.config.retry_delay_ms / 1000)

        raise RuntimeError(f"Drive API failed after {self.config.max_retries} attempts ({context}).") from last_error
