import os
import subprocess
import sys
from argparse import ArgumentParser
from typing import Optional

import requests

REPO_OWNER = "ISOR3X"
GITHUB_API_BASE = "https://api.github.com"
TIMEOUT = 30


def resolve_token(cli_token: str | None = None) -> str:
    if cli_token:
        return cli_token
    env = os.getenv("GITHUB_TOKEN")
    if env:
        return env
    try:
        result = subprocess.run(
            ["gh", "auth", "token"], capture_output=True, text=True, check=True
        )
        return result.stdout.strip()
    except (subprocess.CalledProcessError, FileNotFoundError):
        sys.exit(
            "GitHub token is required. Pass --github-token, set GITHUB_TOKEN, "
            "or log in with `gh auth login`."
        )


def make_headers(token: str) -> dict:
    return {
        "Authorization": f"Bearer {token}",
        "Accept": "application/vnd.github+json",
        "X-GitHub-Api-Version": "2022-11-28",
    }
