import argparse
import os
from jamiapis_gdrive import GoogleDriveClient


def load_env(path: str) -> None:
    if not os.path.exists(path):
        return
    with open(path, "r", encoding="utf-8") as handle:
        for line in handle:
            line = line.strip()
            if not line or line.startswith("#") or "=" not in line:
                continue
            key, value = line.split("=", 1)
            os.environ.setdefault(key, value)


def main() -> None:
    load_env(os.path.join(os.path.dirname(__file__), ".env"))

    parser = argparse.ArgumentParser(description="Google Drive Python Demo")
    subparsers = parser.add_subparsers(dest="command")

    upload = subparsers.add_parser("upload")
    upload.add_argument("path")
    upload.add_argument("mime_type")

    subparsers.add_parser("list")

    download = subparsers.add_parser("download")
    download.add_argument("file_id")
    download.add_argument("dest")

    update = subparsers.add_parser("update")
    update.add_argument("file_id")
    update.add_argument("path")

    delete = subparsers.add_parser("delete")
    delete.add_argument("file_id")

    meta = subparsers.add_parser("meta")
    meta.add_argument("file_id")

    args = parser.parse_args()
    if not args.command:
        parser.print_help()
        return

    client = GoogleDriveClient()

    if args.command == "upload":
        result = client.upload_file(args.path, args.mime_type)
        print(f"Uploaded: {result['id']} ({result['name']})")
    elif args.command == "list":
        files = client.list_files()
        for file in files:
            print(f"{file['id']} {file['name']} {file.get('mimeType')}")
    elif args.command == "download":
        client.download_file(args.file_id, args.dest)
        print(f"Downloaded to {args.dest}")
    elif args.command == "update":
        result = client.update_file(args.file_id, args.path)
        print(f"Updated: {result['id']} ({result['name']})")
    elif args.command == "delete":
        client.delete_file(args.file_id)
        print(f"Deleted: {args.file_id}")
    elif args.command == "meta":
        result = client.get_file_meta(args.file_id)
        print(result)


if __name__ == "__main__":
    main()
