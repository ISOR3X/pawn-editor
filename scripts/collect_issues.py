#!/usr/bin/env -S uv run --script
#
# /// script
# requires-python = ">=3.11"
# dependencies = [
#   "requests",
# ]
# ///

"""
Update a GitHub issue with a list of all open issues for a given label.
Inspired by https://github.com/zed-industries/zed/blob/main/script/update_top_ranking_issues/main.py.
"""

import os
import subprocess
import sys
from argparse import ArgumentParser
from typing import Optional

import requests

REPO_OWNER = "ISOR3X"
REPO_NAME = "pawn-editor"
GITHUB_API_BASE = "https://api.github.com"


def resolve_token(cli_token: Optional[str]) -> str:
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


def fetch_issues(headers: dict, label: str) -> list[dict]:
    """Return all open issues with the given label, sorted by issue number."""
    issues: list[dict] = []
    page = 1
    while True:
        response = requests.get(
            f"{GITHUB_API_BASE}/repos/{REPO_OWNER}/{REPO_NAME}/issues",
            headers=headers,
            params={
                "labels": label,
                "state": "open",
                "per_page": 100,
                "page": page,
            },
        )
        response.raise_for_status()
        batch = response.json()
        if not batch:
            break
        # /issues also returns pull requests — filter those out
        issues.extend(item for item in batch if "pull_request" not in item)
        page += 1
    issues.sort(key=lambda i: i["number"])
    return issues


def build_body(issues: list[dict], label: str) -> str:
    lines = [f"This issue lists all open issues labelled `{label}`.", ""]
    if issues:
        for issue in issues:
            lines.append(f"- {issue['html_url']}")
    else:
        lines.append("_No open issues with this label at this time._")
    return "\n".join(lines)


def update_issue(headers: dict, issue_number: int, body: str) -> None:
    url = f"{GITHUB_API_BASE}/repos/{REPO_OWNER}/{REPO_NAME}/issues/{issue_number}"
    response = requests.patch(url, headers=headers, json={"body": body})
    response.raise_for_status()
    print(f"Updated issue #{issue_number}: {response.json()['html_url']}")


def main() -> None:
    parser = ArgumentParser(description=__doc__)
    parser.add_argument("--github-token", help="GitHub personal access token")
    parser.add_argument(
        "--label",
        required=True,
        help="Label to filter issues by (e.g. stable, rewrite)",
    )
    parser.add_argument(
        "--issue-number",
        type=int,
        help="Issue number to update with the generated body (omit to print instead)",
    )
    args = parser.parse_args()

    token = resolve_token(args.github_token)
    headers = {
        "Authorization": f"Bearer {token}",
        "Accept": "application/vnd.github+json",
        "X-GitHub-Api-Version": "2022-11-28",
    }

    issues = fetch_issues(headers, args.label)
    print(f"Found {len(issues)} open issue(s) labelled '{args.label}'.")

    body = build_body(issues, args.label)

    if args.issue_number:
        update_issue(headers, args.issue_number, body)
    else:
        print("\n--- Generated body ---\n")
        print(body)


if __name__ == "__main__":
    main()
