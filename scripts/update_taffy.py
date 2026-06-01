#!/usr/bin/env -S uv run --script
#
# /// script
# requires-python = ">=3.11"
# dependencies = [
#   "requests",
# ]
# ///

"""
Downloads the latest TaffySharp artifacts from GitHub CI and installs them
into the pawn-editor lib directory.
"""

import io
import os
import shutil
import sys
import zipfile
from argparse import ArgumentParser
from pathlib import Path

import requests

from scripts._common import (
    GITHUB_API_BASE,
    REPO_OWNER,
    TIMEOUT,
    make_headers,
    resolve_token,
)

REPO_NAME = "taffy"
WORKFLOW_FILE = "csharp-bindings.yml"

LIB_DIR = Path(__file__).parent.parent / "lib"

NATIVE_LIBS = {
    "TaffySharp-win-x64": ("target/release/ctaffy.dll", "win-x64/ctaffy.dll"),
    "TaffySharp-linux-x64": ("target/release/libctaffy.so", "linux-x64/libctaffy.so"),
    "TaffySharp-osx-arm64": (
        "target/release/libctaffy.dylib",
        "osx-arm64/libctaffy.dylib",
    ),
}
MANAGED_DLL_ARTIFACT = list(NATIVE_LIBS.keys())[
    0
]  # Where to extract the managed DLL from the artifact
MANAGED_DLL_SRC = "bindings/csharp/include/bin/Release/net4.7.2/Taffy.dll"
MANAGED_DLL_DST = "Taffy.dll"


def get_latest_run_id(headers: dict) -> int:
    url = f"{GITHUB_API_BASE}/repos/{REPO_OWNER}/{REPO_NAME}/actions/workflows/{WORKFLOW_FILE}/runs"
    response = requests.get(
        url,
        headers=headers,
        params={"status": "success", "per_page": 1},
        timeout=TIMEOUT,
    )
    response.raise_for_status()
    runs = response.json()["workflow_runs"]
    if not runs:
        sys.exit("No successful workflow runs found.")
    return runs[0]["id"]


def get_artifact_id(headers: dict, run_id: int, name: str) -> int:
    url = f"{GITHUB_API_BASE}/repos/{REPO_OWNER}/{REPO_NAME}/actions/runs/{run_id}/artifacts"
    response = requests.get(
        url, headers=headers, params={"per_page": 100}, timeout=TIMEOUT
    )
    response.raise_for_status()
    for artifact in response.json()["artifacts"]:
        if artifact["name"] == name:
            return artifact["id"]
    sys.exit(f"Artifact '{name}' not found in run {run_id}.")


def download_artifact(headers: dict, artifact_id: int) -> zipfile.ZipFile:
    url = f"{GITHUB_API_BASE}/repos/{REPO_OWNER}/{REPO_NAME}/actions/artifacts/{artifact_id}/zip"
    # Don't follow the redirect automatically: the signed download URL (Azure/S3) rejects requests that still carry the Authorization header.
    response = requests.get(
        url, headers=headers, allow_redirects=False, timeout=TIMEOUT
    )
    response.raise_for_status()
    download_url = response.headers["Location"]
    response = requests.get(download_url, timeout=TIMEOUT)
    response.raise_for_status()
    return zipfile.ZipFile(io.BytesIO(response.content))


def extract_file(zf: zipfile.ZipFile, src: str, dst: Path) -> None:
    if src not in zf.namelist():
        print(f"Archive contents: {zf.namelist()}", file=sys.stderr)
        sys.exit(f"Could not find '{src}' in archive.")
    with zf.open(src) as src_file:
        with open(dst, "wb") as dst_file:
            shutil.copyfileobj(src_file, dst_file)


def main() -> None:
    parser = ArgumentParser(description=__doc__)
    parser.add_argument(
        "--github-token",
        default=os.getenv("GITHUB_TOKEN"),
        help="GitHub personal access token (or set GITHUB_TOKEN)",
    )
    args = parser.parse_args()
    headers = make_headers(resolve_token(args.github_token))

    print(f"Fetching latest successful run of {WORKFLOW_FILE}...")
    run_id = get_latest_run_id(headers)
    print(f"Using run {run_id}")

    artifact_zips: dict[str, zipfile.ZipFile] = {}
    for name in NATIVE_LIBS:
        print(f"Downloading {name}...")
        artifact_id = get_artifact_id(headers, run_id, name)
        artifact_zips[name] = download_artifact(headers, artifact_id)

    managed_dst = LIB_DIR / MANAGED_DLL_DST
    extract_file(artifact_zips[MANAGED_DLL_ARTIFACT], MANAGED_DLL_SRC, managed_dst)
    print(f"  -> {managed_dst.relative_to(LIB_DIR.parent)}")

    for artifact_name, (src_rel, dst_rel) in NATIVE_LIBS.items():
        dst = LIB_DIR / dst_rel
        extract_file(artifact_zips[artifact_name], src_rel, dst)
        print(f"  -> {dst.relative_to(LIB_DIR.parent)}")

    print("Done.")


if __name__ == "__main__":
    main()
