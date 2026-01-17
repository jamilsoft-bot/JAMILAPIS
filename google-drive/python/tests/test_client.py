import os
import pytest
from jamiapis_gdrive import GoogleDriveClient


def test_constructor_requires_credentials():
    os.environ.pop("GOOGLE_APPLICATION_CREDENTIALS", None)
    with pytest.raises(ValueError):
        GoogleDriveClient()


def test_mocking_placeholder():
    # Example: monkeypatch client.drive with a fake in-memory stub.
    assert True
