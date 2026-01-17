# Google Drive API Setup (Service Account)

This guide enables server-to-server access to **your Drive** using a **Service Account** and the Drive API v3.

## 1) Create a Google Cloud Project

1. Go to the [Google Cloud Console](https://console.cloud.google.com/).
2. Click **Select a project** → **New Project**.
3. Name the project and click **Create**.

## 2) Enable Google Drive API

1. In the Cloud Console, go to **APIs & Services** → **Library**.
2. Search for **Google Drive API**.
3. Click **Enable**.

## 3) Create a Service Account

1. Go to **APIs & Services** → **Credentials**.
2. Click **Create Credentials** → **Service Account**.
3. Name the service account (e.g., `drive-svc`), then **Create and Continue**.
4. Skip granting roles (not required for Drive access) and click **Done**.

## 4) Create and Download a JSON Key

1. Click the service account you just created.
2. Go to **Keys** → **Add Key** → **Create new key**.
3. Select **JSON** and download the file.
4. Store the file securely and **do not commit it**.

Set the environment variable:

```
GOOGLE_APPLICATION_CREDENTIALS=/absolute/path/to/service-account.json
```

## 5) Share a Drive Folder with the Service Account (Mode A)

1. Create a folder in your Google Drive.
2. Right-click → **Share**.
3. Add the **service account email** (e.g., `drive-svc@project.iam.gserviceaccount.com`).
4. Give it **Editor** access.

> This is the **default mode**. All library operations are scoped to this shared folder.

## 6) Find the Folder ID

Open the folder in your browser. The URL looks like:

```
https://drive.google.com/drive/folders/<FOLDER_ID>
```

Set:

```
GOOGLE_DRIVE_ROOT_FOLDER_ID=<FOLDER_ID>
```

## 7) Required OAuth Scopes

Use Drive API v3 scope(s):

* `https://www.googleapis.com/auth/drive` (full access)

> The libraries in this repo use this scope to support file CRUD and Shared Drives.

## 8) Shared Drive (Team Drive) Notes

To support Shared Drives, enable:

* `supportsAllDrives=true`
* `includeItemsFromAllDrives=true`

Set via env var:

```
GOOGLE_DRIVE_SUPPORTS_ALL_DRIVES=true
```

## 9) Security Best Practices

* **Never commit** JSON keys.
* Store secrets in environment variables or secret managers.
* Rotate keys periodically.

## 10) Troubleshooting

| Error | Cause | Fix |
| --- | --- | --- |
| `403 insufficientPermissions` | Folder not shared with service account | Share the folder with the service account email. |
| `404 notFound` | File not in shared folder / wrong ID | Verify ID and parent folder. |
| `invalid_grant` | Key revoked/expired or wrong clock | Recreate key; verify server time. |
| `403 dailyLimitExceeded` | Quota limits | Check quotas in Cloud Console. |

## Optional: Domain-Wide Delegation (Workspace Only)

If you use Google Workspace, you can enable **domain-wide delegation** and impersonate a user. This is **not required** for the default shared-folder approach. If needed, add impersonation support by setting a subject (user email) in each SDK client.
