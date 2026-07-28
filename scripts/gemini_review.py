#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import os
import random
import re
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from dataclasses import dataclass
from typing import Any


GITHUB_API_URL = "https://api.github.com"
GITHUB_API_VERSION = "2022-11-28"
GEMINI_API_URL = "https://generativelanguage.googleapis.com/v1beta/interactions"
DEFAULT_GEMINI_MODEL = "gemini-2.5-flash-lite"

REQUIRED_ENV_VARS = (
    "GEMINI_API_KEY",
    "GITHUB_TOKEN",
    "GITHUB_REPOSITORY",
    "PR_NUMBER",
    "PR_HEAD_SHA",
)

MAX_REVIEW_FILES = 40
MAX_PATCH_CHARS_PER_FILE = 12_000
MAX_PROMPT_CHARS = 120_000
MAX_ALLOWED_LINES = 800
MAX_COMMENTS = 10
MAX_COMMENT_BODY_CHARS = 2_500
REQUEST_TIMEOUT_SECONDS = 45
MAX_RETRIES = 3

REVIEW_MARKER_PREFIX = "<!-- gemini-code-review:"
REVIEW_MARKER_SUFFIX = " -->"

HUNK_HEADER_RE = re.compile(
    r"^@@ -(?P<old_start>\d+)(?:,(?P<old_len>\d+))? "
    r"\+(?P<new_start>\d+)(?:,(?P<new_len>\d+))? @@"
)

SKIP_DIRECTORY_NAMES = {
    ".git",
    ".vs",
    "bin",
    "obj",
    "dist",
    "build",
    "builds",
    "coverage",
    "node_modules",
    "packages",
    "library",
    "temp",
    "logs",
}

SKIP_EXACT_PATHS = {
    "Packages/packages-lock.json",
}

SKIP_FILENAMES = {
    "package-lock.json",
    "npm-shrinkwrap.json",
    "yarn.lock",
    "pnpm-lock.yaml",
    "composer.lock",
    "Gemfile.lock",
    "Cargo.lock",
    "Pipfile.lock",
    "poetry.lock",
}

BINARY_EXTENSIONS = {
    ".png",
    ".jpg",
    ".jpeg",
    ".gif",
    ".bmp",
    ".tga",
    ".tif",
    ".tiff",
    ".webp",
    ".ico",
    ".psd",
    ".mp3",
    ".wav",
    ".ogg",
    ".flac",
    ".m4a",
    ".mp4",
    ".mov",
    ".avi",
    ".mkv",
    ".webm",
    ".zip",
    ".7z",
    ".rar",
    ".gz",
    ".tar",
    ".dll",
    ".exe",
    ".pdb",
    ".so",
    ".dylib",
    ".nupkg",
}

GENERATED_PATTERNS = (
    re.compile(r"(^|/)[Gg]enerated/"),
    re.compile(r"(^|/)[Aa]uto[Gg]enerated/"),
    re.compile(r"\.generated\.", re.IGNORECASE),
    re.compile(r"\.g\.cs$", re.IGNORECASE),
    re.compile(r"\.min\.(js|css)$", re.IGNORECASE),
)

REVIEW_RESPONSE_SCHEMA: dict[str, Any] = {
    "type": "object",
    "properties": {
        "comments": {
            "type": "array",
            "maxItems": MAX_COMMENTS,
            "items": {
                "type": "object",
                "properties": {
                    "path": {"type": "string", "description": "Repository-relative file path."},
                    "line": {"type": "integer", "description": "New-file line number from the allowed list."},
                    "severity": {"type": "string", "enum": ["high", "medium", "low"]},
                    "title": {"type": "string", "description": "Non-empty issue title."},
                    "body": {"type": "string", "description": "Non-empty issue explanation and fix direction."},
                },
                "required": ["path", "line", "severity", "title", "body"],
            },
        }
    },
    "required": ["comments"],
}


@dataclass(frozen=True)
class EnvConfig:
    gemini_api_key: str
    github_token: str
    github_repository: str
    pr_number: int
    pr_head_sha: str
    gemini_model: str


@dataclass(frozen=True)
class PullRequestFile:
    filename: str
    status: str
    patch: str
    additions: int
    deletions: int


@dataclass(frozen=True)
class AllowedLine:
    path: str
    line: int
    code: str
    context: str


@dataclass(frozen=True)
class ReviewComment:
    path: str
    line: int
    severity: str
    title: str
    body: str


class ApiError(RuntimeError):
    def __init__(self, service: str, status: int, message: str) -> None:
        super().__init__(f"{service} API error {status}: {message}")
        self.service = service
        self.status = status
        self.message = message


def log(message: str) -> None:
    print(message, flush=True)


def validate_env() -> EnvConfig:
    missing = [name for name in REQUIRED_ENV_VARS if not os.getenv(name)]
    if missing:
        raise SystemExit(f"Missing required environment variables: {', '.join(missing)}")

    try:
        pr_number = int(os.environ["PR_NUMBER"])
    except ValueError as exc:
        raise SystemExit("PR_NUMBER must be an integer.") from exc

    return EnvConfig(
        gemini_api_key=os.environ["GEMINI_API_KEY"],
        github_token=os.environ["GITHUB_TOKEN"],
        github_repository=os.environ["GITHUB_REPOSITORY"],
        pr_number=pr_number,
        pr_head_sha=os.environ["PR_HEAD_SHA"],
        gemini_model=os.getenv("GEMINI_MODEL", DEFAULT_GEMINI_MODEL),
    )


def request_json(
    url: str,
    *,
    method: str = "GET",
    headers: dict[str, str] | None = None,
    payload: dict[str, Any] | None = None,
    service: str,
    retry_statuses: set[int] | None = None,
) -> Any:
    retry_statuses = retry_statuses or {429, 500, 502, 503, 504}
    request_headers = dict(headers or {})
    encoded_payload = None

    if payload is not None:
        encoded_payload = json.dumps(payload).encode("utf-8")
        request_headers["Content-Type"] = "application/json"

    for attempt in range(MAX_RETRIES + 1):
        request = urllib.request.Request(url, data=encoded_payload, headers=request_headers, method=method)
        try:
            with urllib.request.urlopen(request, timeout=REQUEST_TIMEOUT_SECONDS) as response:
                body = response.read().decode("utf-8")
                return json.loads(body) if body else None
        except urllib.error.HTTPError as exc:
            status = exc.code
            message = read_error_message(exc)
            if status in retry_statuses and attempt < MAX_RETRIES:
                sleep_seconds = (2**attempt) + random.uniform(0.0, 0.5)
                log(f"{service} API returned {status}. Retrying in {sleep_seconds:.1f}s.")
                time.sleep(sleep_seconds)
                continue
            raise ApiError(service, status, message) from exc
        except urllib.error.URLError as exc:
            if attempt < MAX_RETRIES:
                sleep_seconds = (2**attempt) + random.uniform(0.0, 0.5)
                log(f"{service} API request failed temporarily. Retrying in {sleep_seconds:.1f}s.")
                time.sleep(sleep_seconds)
                continue
            raise RuntimeError(f"{service} API request failed: {exc.reason}") from exc

    raise RuntimeError(f"{service} API request failed after retries.")


def read_error_message(exc: urllib.error.HTTPError) -> str:
    try:
        body = exc.read().decode("utf-8")
    except Exception:
        return exc.reason or "request failed"

    try:
        parsed = json.loads(body)
    except json.JSONDecodeError:
        return body[:500] if body else (exc.reason or "request failed")

    if isinstance(parsed, dict):
        message = parsed.get("message")
        if isinstance(message, str):
            return message
        error = parsed.get("error")
        if isinstance(error, dict) and isinstance(error.get("message"), str):
            return error["message"]
    return "request failed"


def github_headers(token: str) -> dict[str, str]:
    return {
        "Accept": "application/vnd.github+json",
        "Authorization": f"Bearer {token}",
        "X-GitHub-Api-Version": GITHUB_API_VERSION,
        "User-Agent": "gemini-code-review-action",
    }


def split_repository(repository: str) -> tuple[str, str]:
    parts = repository.split("/", 1)
    if len(parts) != 2 or not parts[0] or not parts[1]:
        raise SystemExit("GITHUB_REPOSITORY must be in the form 'owner/repo'.")
    return parts[0], parts[1]


def github_get(config: EnvConfig, path: str, params: dict[str, Any] | None = None) -> Any:
    url = f"{GITHUB_API_URL}{path}"
    if params:
        url = f"{url}?{urllib.parse.urlencode(params)}"
    return request_json(url, headers=github_headers(config.github_token), service="GitHub")


def github_post(config: EnvConfig, path: str, payload: dict[str, Any]) -> Any:
    return request_json(
        f"{GITHUB_API_URL}{path}",
        method="POST",
        headers=github_headers(config.github_token),
        payload=payload,
        service="GitHub",
    )


def list_pr_files(config: EnvConfig) -> list[PullRequestFile]:
    owner, repo = split_repository(config.github_repository)
    files: list[PullRequestFile] = []
    page = 1

    while True:
        data = github_get(
            config,
            f"/repos/{owner}/{repo}/pulls/{config.pr_number}/files",
            {"per_page": 100, "page": page},
        )
        if not isinstance(data, list):
            raise RuntimeError("GitHub PR files response was not a list.")

        for item in data:
            if isinstance(item, dict):
                files.append(
                    PullRequestFile(
                        filename=str(item.get("filename", "")),
                        status=str(item.get("status", "")),
                        patch=str(item.get("patch") or ""),
                        additions=int(item.get("additions") or 0),
                        deletions=int(item.get("deletions") or 0),
                    )
                )

        if len(data) < 100:
            break
        page += 1

    log(f"Fetched {len(files)} changed file(s) from PR #{config.pr_number}.")
    return files


def has_existing_review_for_head(config: EnvConfig) -> bool:
    owner, repo = split_repository(config.github_repository)
    marker = f"{REVIEW_MARKER_PREFIX}{config.pr_head_sha}{REVIEW_MARKER_SUFFIX}"
    page = 1

    while True:
        data = github_get(
            config,
            f"/repos/{owner}/{repo}/pulls/{config.pr_number}/reviews",
            {"per_page": 100, "page": page},
        )
        if not isinstance(data, list):
            raise RuntimeError("GitHub PR reviews response was not a list.")

        for review in data:
            if not isinstance(review, dict):
                continue
            user = review.get("user")
            login = user.get("login") if isinstance(user, dict) else None
            body = str(review.get("body") or "")
            commit_id = str(review.get("commit_id") or "")
            if login == "github-actions[bot]" and commit_id == config.pr_head_sha and marker in body:
                log("Existing Gemini review for this head SHA found. Skipping.")
                return True

        if len(data) < 100:
            break
        page += 1

    return False


def normalize_path(path: str) -> str:
    return path.replace("\\", "/").lstrip("/")


def has_skipped_directory(path: str) -> bool:
    segments = path.split("/")[:-1]
    return any(segment.lower() in SKIP_DIRECTORY_NAMES for segment in segments)


def should_skip_file(file: PullRequestFile) -> str | None:
    path = normalize_path(file.filename)
    basename = path.rsplit("/", 1)[-1]
    _, ext = os.path.splitext(basename.lower())

    if file.status == "removed":
        return "deleted file"
    if not file.patch:
        return "missing patch or binary file"
    if path in SKIP_EXACT_PATHS or any(path.endswith(f"/{item}") for item in SKIP_EXACT_PATHS):
        return "excluded path"
    if path.endswith(".meta"):
        return "Unity .meta file"
    if basename in SKIP_FILENAMES or basename.endswith(".lock"):
        return "lock file"
    if has_skipped_directory(path):
        return "excluded directory or build output"
    if ext in BINARY_EXTENSIONS:
        return "binary or media extension"
    if any(pattern.search(path) for pattern in GENERATED_PATTERNS):
        return "generated file"
    if len(file.patch) > MAX_PATCH_CHARS_PER_FILE:
        return f"patch too large ({len(file.patch)} chars)"
    return None


def select_review_files(files: list[PullRequestFile]) -> list[PullRequestFile]:
    selected: list[PullRequestFile] = []
    skipped = 0
    file_limit_applied = False

    for file in files:
        reason = should_skip_file(file)
        if reason:
            skipped += 1
            log(f"Skipping {file.filename}: {reason}.")
            continue
        if len(selected) >= MAX_REVIEW_FILES:
            skipped += 1
            file_limit_applied = True
            continue
        selected.append(file)

    if file_limit_applied:
        log(f"File limit applied: reviewing first {MAX_REVIEW_FILES} eligible file(s).")
    log(f"Selected {len(selected)} file(s) for review; skipped {skipped}.")
    return selected


def parse_patch(path: str, patch: str) -> list[AllowedLine]:
    allowed: list[AllowedLine] = []
    hunk_header = ""
    hunk_records: list[dict[str, Any]] = []
    old_line: int | None = None
    new_line: int | None = None

    def flush_hunk() -> None:
        if not hunk_records:
            return
        for index, record in enumerate(hunk_records):
            if record["prefix"] != "+":
                continue
            allowed.append(
                AllowedLine(
                    path=path,
                    line=int(record["new_line"]),
                    code=str(record["text"]),
                    context=format_context(hunk_header, hunk_records, index),
                )
            )

    for raw_line in patch.splitlines():
        header_match = HUNK_HEADER_RE.match(raw_line)
        if header_match:
            flush_hunk()
            hunk_header = raw_line
            hunk_records = []
            old_line = int(header_match.group("old_start"))
            new_line = int(header_match.group("new_start"))
            continue

        if old_line is None or new_line is None or raw_line.startswith("\\"):
            continue

        prefix = raw_line[:1]
        text = raw_line[1:] if raw_line else ""

        if prefix == "+" and not raw_line.startswith("+++"):
            hunk_records.append({"prefix": "+", "old_line": None, "new_line": new_line, "text": text})
            new_line += 1
        elif prefix == "-":
            if not raw_line.startswith("---"):
                hunk_records.append({"prefix": "-", "old_line": old_line, "new_line": None, "text": text})
                old_line += 1
        else:
            hunk_records.append(
                {
                    "prefix": " ",
                    "old_line": old_line,
                    "new_line": new_line,
                    "text": text if prefix == " " else raw_line,
                }
            )
            old_line += 1
            new_line += 1

    flush_hunk()
    return allowed


def format_context(hunk_header: str, hunk_records: list[dict[str, Any]], index: int) -> str:
    start = max(0, index - 3)
    end = min(len(hunk_records), index + 4)
    lines = [hunk_header]

    for record in hunk_records[start:end]:
        line_number = record["new_line"] if record["new_line"] is not None else record["old_line"]
        lines.append(f"{record['prefix']}{line_number}: {record['text']}")

    return "\n".join(lines)


def build_allowed_lines(files: list[PullRequestFile]) -> list[AllowedLine]:
    allowed: list[AllowedLine] = []
    prompt_chars = 0

    for file in files:
        path = normalize_path(file.filename)
        parsed = parse_patch(path, file.patch)
        if not parsed:
            log(f"No added commentable lines found in {path}.")
            continue

        for item in parsed:
            item_size = len(item.path) + len(item.code) + len(item.context) + 32
            if len(allowed) >= MAX_ALLOWED_LINES:
                log(f"Allowed-line limit applied at {MAX_ALLOWED_LINES} line(s).")
                return allowed
            if prompt_chars + item_size > MAX_PROMPT_CHARS:
                log(f"Prompt diff size limit applied at {MAX_PROMPT_CHARS} chars.")
                return allowed
            allowed.append(item)
            prompt_chars += item_size

    log(f"Built {len(allowed)} allowed inline comment position(s).")
    return allowed


def build_gemini_input(allowed_lines: list[AllowedLine]) -> str:
    payload = {
        "project": {
            "name": "ChaosChess.AI",
            "type": "Pure C# AI decision library for Chaos Chess",
            "runtime": "src library targets netstandard2.1; tests target net8.0 xUnit",
            "architecture": [
                "No UnityEngine, DOTween, FairyStockfishBridge, file IO, network IO, or adapter concerns inside the library.",
                "AI logic should remain pure, synchronous, and deterministic.",
                "External chess engine access must stay behind IChessEngine.",
                "Randomness must stay behind IRandom.",
                "The library decides moves/cards but does not mutate or apply Unity game state.",
            ],
            "review_focus": [
                "FEN parse/serialize correctness and malformed input handling",
                "0-based square coordinates: a1=(0,0), h8=(7,7)",
                "BoardState/GameState immutability and state consistency",
                "GameStateEvaluator score sign, clamp, weights, mate handling, and tile-effect calculations",
                "PieceKind/FenCode preservation for Chaos Chess custom pieces",
                "Important missing domain regression tests",
            ],
        },
        "allowed_inline_comment_positions": [
            {
                "path": item.path,
                "line": item.line,
                "code": item.code,
                "diff_context": item.context,
            }
            for item in allowed_lines
        ],
    }
    return json.dumps(payload, ensure_ascii=False, separators=(",", ":"))


def gemini_system_instruction() -> str:
    return """
당신은 ChaosChess.AI 순수 C# 라이브러리의 Pull Request를 검토하는 자동 코드 리뷰어입니다.

반드시 allowed_inline_comment_positions 배열에 있는 path와 line 중에서만 댓글 위치를 선택하세요.
전체 파일 줄 번호를 추측하거나, 목록에 없는 파일/줄을 반환하지 마세요.
변경된 코드로 인해 새로 발생한 문제만 리뷰하고 기존 코드의 관련 없는 문제는 리뷰하지 마세요.

우선순위:
- 실제 버그나 장애 가능성이 높은 문제
- C# null 처리 문제와 예외 처리 누락
- 잘못된 상태 변경, 도메인 모델 불변성 훼손, 보드/기물/카드 상태 불일치
- FEN 파싱/직렬화, 좌표 변환, 경계 조건 오류
- 평가 점수 부호, clamp, mate 처리, 타일 효과 계산 오류
- 결정론을 깨는 난수/시간/IO/비동기 처리 도입
- IChessEngine, IRandom, Unity 어댑터 경계 위반
- 테스트로 확인해야 하는 중요한 회귀 가능성

리뷰하지 않을 것:
- 코드 스타일, 취향, 사소한 이름 변경
- 단순 요약, 칭찬, 일반적인 개선 제안
- 확실하지 않은 추측
- 같은 원인의 중복 댓글
- low 심각도의 근거가 약하거나 사소한 내용

댓글은 한국어로 작성하세요.
body에는 반드시 "문제 설명:"과 "수정 방향:"을 포함하고, 발생 가능한 상황과 수정 방향을 간결하게 쓰세요.
문제가 없으면 {"comments": []}를 반환하세요.
""".strip()


def call_gemini(config: EnvConfig, allowed_lines: list[AllowedLine]) -> dict[str, Any]:
    payload = {
        "model": config.gemini_model,
        "system_instruction": gemini_system_instruction(),
        "input": build_gemini_input(allowed_lines),
        "generation_config": {"thinking_level": "low"},
        "response_format": {
            "type": "text",
            "mime_type": "application/json",
            "schema": REVIEW_RESPONSE_SCHEMA,
        },
    }
    response = request_json(
        GEMINI_API_URL,
        method="POST",
        headers={"x-goog-api-key": config.gemini_api_key},
        payload=payload,
        service="Gemini",
    )
    text = extract_gemini_text(response)
    try:
        parsed = json.loads(text)
    except json.JSONDecodeError as exc:
        raise RuntimeError("Gemini response was not valid JSON.") from exc
    if not isinstance(parsed, dict):
        raise RuntimeError("Gemini JSON response must be an object.")
    return parsed


def extract_gemini_text(response: Any) -> str:
    if not isinstance(response, dict):
        raise RuntimeError("Gemini response was not a JSON object.")

    for key in ("output_text", "outputText"):
        value = response.get(key)
        if isinstance(value, str) and value.strip():
            return value.strip()

    texts = collect_text_values(response.get("steps", []))
    if texts:
        return "".join(texts).strip()

    candidates = response.get("candidates")
    if isinstance(candidates, list):
        texts = collect_text_values(candidates)
        if texts:
            return "".join(texts).strip()

    raise RuntimeError("Could not find text output in Gemini response.")


def collect_text_values(value: Any) -> list[str]:
    texts: list[str] = []
    if isinstance(value, dict):
        text = value.get("text")
        if isinstance(text, str):
            texts.append(text)
        for child in value.values():
            texts.extend(collect_text_values(child))
    elif isinstance(value, list):
        for child in value:
            texts.extend(collect_text_values(child))
    return texts


def validate_comments(response: dict[str, Any], allowed_lines: list[AllowedLine]) -> list[ReviewComment]:
    raw_comments = response.get("comments")
    if not isinstance(raw_comments, list):
        log("Gemini response does not contain a comments array. Ignoring response.")
        return []

    allowed_map = {(item.path, item.line): item for item in allowed_lines}
    validated: list[ReviewComment] = []
    seen: set[tuple[str, int]] = set()

    for raw in raw_comments[:MAX_COMMENTS]:
        if not isinstance(raw, dict):
            log("Ignoring malformed Gemini comment: not an object.")
            continue

        path = normalize_path(str(raw.get("path", "")))
        line_raw = raw.get("line")
        severity = str(raw.get("severity", "")).lower()
        title = str(raw.get("title", "")).strip()
        body = str(raw.get("body", "")).strip()

        if not isinstance(line_raw, int):
            log(f"Ignoring Gemini comment for {path}: line is not an integer.")
            continue
        line = line_raw

        if (path, line) not in allowed_map:
            log(f"Ignoring Gemini comment for invalid location: {path}:{line}.")
            continue
        if (path, line) in seen:
            log(f"Ignoring duplicate Gemini comment location: {path}:{line}.")
            continue
        if severity not in {"high", "medium", "low"}:
            log(f"Ignoring Gemini comment at {path}:{line}: invalid severity.")
            continue
        if not title or not body:
            log(f"Ignoring Gemini comment at {path}:{line}: empty title or body.")
            continue
        if severity == "low" and is_weak_low_comment(title, body):
            log(f"Ignoring weak low-severity Gemini comment at {path}:{line}.")
            continue

        seen.add((path, line))
        validated.append(
            ReviewComment(
                path=path,
                line=line,
                severity=severity,
                title=trim_text(title, 140),
                body=trim_text(body, MAX_COMMENT_BODY_CHARS),
            )
        )

    if len(raw_comments) > MAX_COMMENTS:
        log(f"Gemini returned more than {MAX_COMMENTS} comment(s); extra comments were ignored.")
    log(f"Validated {len(validated)} inline review comment(s).")
    return validated


def is_weak_low_comment(title: str, body: str) -> bool:
    combined = f"{title}\n{body}".lower()
    weak_terms = (
        "style",
        "format",
        "formatting",
        "naming",
        "readability",
        "typo",
        "스타일",
        "포맷",
        "공백",
        "네이밍",
        "이름",
        "오타",
        "가독성",
        "취향",
    )
    return len(body) < 40 or any(term in combined for term in weak_terms)


def trim_text(value: str, limit: int) -> str:
    if len(value) <= limit:
        return value
    return value[: limit - 20].rstrip() + "\n\n...(truncated)"


def format_review_comment(comment: ReviewComment) -> str:
    description, fix = split_body_sections(comment.body)
    severity = {"high": "High", "medium": "Medium", "low": "Low"}[comment.severity]
    return f"**[{severity}] {comment.title}**\n\n{description}\n\n**수정 방향:** {fix}"


def split_body_sections(body: str) -> tuple[str, str]:
    normalized = body.strip()
    fix_patterns = (
        r"\*\*수정 방향:\*\*",
        r"수정 방향:",
        r"해결 방향:",
        r"권장 수정:",
    )

    for pattern in fix_patterns:
        match = re.search(pattern, normalized, flags=re.IGNORECASE)
        if not match:
            continue
        description = normalized[: match.start()].strip()
        fix = normalized[match.end() :].strip()
        description = re.sub(r"^\s*(문제 설명:|\*\*문제 설명:\*\*)\s*", "", description).strip()
        if description and fix:
            return description, fix

    normalized = re.sub(r"^\s*(문제 설명:|\*\*문제 설명:\*\*)\s*", "", normalized).strip()
    return normalized, "문제 설명에 포함된 발생 조건을 기준으로 상태, 경계 조건, 예외 처리 또는 테스트를 보완하세요."


def submit_review(config: EnvConfig, comments: list[ReviewComment]) -> None:
    if not comments:
        log("No valid review comments. Skipping GitHub review creation.")
        return

    owner, repo = split_repository(config.github_repository)
    payload = {
        "commit_id": config.pr_head_sha,
        "body": f"{REVIEW_MARKER_PREFIX}{config.pr_head_sha}{REVIEW_MARKER_SUFFIX}",
        "event": "COMMENT",
        "comments": [
            {
                "path": comment.path,
                "line": comment.line,
                "side": "RIGHT",
                "body": format_review_comment(comment),
            }
            for comment in comments
        ],
    }

    try:
        github_post(config, f"/repos/{owner}/{repo}/pulls/{config.pr_number}/reviews", payload)
    except ApiError as exc:
        if exc.service == "GitHub" and exc.status == 422:
            locations = ", ".join(f"{comment.path}:{comment.line}" for comment in comments)
            log(f"GitHub rejected one or more inline comment locations: {locations}")
        raise

    log(f"Submitted one GitHub pull request review with {len(comments)} inline comment(s).")


def run_review() -> None:
    config = validate_env()
    log(f"Starting Gemini code review with model {config.gemini_model}.")

    if has_existing_review_for_head(config):
        return

    pr_files = list_pr_files(config)
    review_files = select_review_files(pr_files)
    allowed_lines = build_allowed_lines(review_files)
    if not allowed_lines:
        log("No allowed inline comment positions found. Nothing to review.")
        return

    gemini_response = call_gemini(config, allowed_lines)
    comments = validate_comments(gemini_response, allowed_lines)
    submit_review(config, comments)


def run_self_test() -> None:
    sample_patch = """@@ -10,7 +10,9 @@ public sealed class FenParser
     public BoardState Parse(string fen)
     {
-        return ParseInternal(fen);
+        if (fen.Length == 0)
+            return BoardState.Empty;
+        return ParseInternal(fen);
     }
"""
    allowed = parse_patch("src/ChaosChess.AI/Fen/FenParser.cs", sample_patch)
    assert [line.line for line in allowed] == [12, 13, 14]
    assert allowed[0].code == "        if (fen.Length == 0)"

    response = {
        "comments": [
            {
                "path": "src/ChaosChess.AI/Fen/FenParser.cs",
                "line": 12,
                "severity": "high",
                "title": "null FEN 입력에서 예외 발생",
                "body": "문제 설명: fen이 null이면 Length 접근에서 NullReferenceException이 발생해 호출자가 malformed FEN 오류를 일관되게 처리할 수 없습니다.\n수정 방향: Length 접근 전에 null을 검사하고 기존 파서의 오류 처리 방식과 같은 예외를 반환하세요.",
            },
            {
                "path": "src/ChaosChess.AI/Fen/FenParser.cs",
                "line": 10,
                "severity": "medium",
                "title": "invalid",
                "body": "문제 설명: invalid\n수정 방향: invalid",
            },
        ]
    }
    comments = validate_comments(response, allowed)
    assert len(comments) == 1
    assert comments[0].line == 12
    formatted = format_review_comment(comments[0])
    assert "**[High] null FEN 입력에서 예외 발생**" in formatted
    assert "**수정 방향:** Length 접근 전에 null을 검사" in formatted
    log("Self-test passed.")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Review GitHub PR diffs with Gemini.")
    parser.add_argument("--self-test", action="store_true", help="Run local parser and validation tests.")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    try:
        if args.self_test:
            run_self_test()
        else:
            run_review()
        return 0
    except SystemExit:
        raise
    except Exception as exc:
        print(f"Gemini code review failed: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
