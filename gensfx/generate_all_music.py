#!/usr/bin/env python3
"""Batch-generate Forest Alchemist music tracks from AUDIO.md prompts."""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
import time
from concurrent.futures import ThreadPoolExecutor, as_completed
from dataclasses import dataclass
from pathlib import Path

import requests

ROOT = Path(__file__).resolve().parent
PROJECT = ROOT.parent
AUDIO_MD = PROJECT / "AUDIO.md"
MUSIC_ROOT = PROJECT / "Assets" / "Audio" / "Music"

PREFIX = "instrumental only, no vocals, seamless loop, fantasy forest alchemist game, "

HF_URL = "https://api-inference.huggingface.co/models/facebook/musicgen-small"
HF_TOKEN_ENV = ("HF_TOKEN", "HUGGINGFACE_TOKEN")


@dataclass(frozen=True)
class MusicJob:
    filename: str
    prompt: str
    loop: bool = True


def _load_hf_token() -> str | None:
    for name in HF_TOKEN_ENV:
        value = os.environ.get(name)
        if value:
            return value.strip()
    return None


def parse_music_jobs(path: Path) -> list[MusicJob]:
    text = path.read_text(encoding="utf-8")
    jobs: list[MusicJob] = []
    current_name: str | None = None
    current_loop = True

    for line in text.splitlines():
        heading = re.match(r"^##\s+1\.\d+\.\s+.+\s+—\s+`([^`]+)`", line)
        if heading:
            current_name = heading.group(1) + ".mp3"
            lowered = line.lower()
            current_loop = "stinger" not in lowered and "death" not in lowered and "one-shot" not in lowered
            continue
        if current_name and line.startswith(">"):
            prompt = line.lstrip("> ").strip()
            if prompt:
                jobs.append(MusicJob(current_name, prompt, loop=current_loop))
                current_name = None
    return jobs


JOBS: list[MusicJob] = parse_music_jobs(AUDIO_MD) if AUDIO_MD.exists() else []


def _duration_for_job(job: MusicJob) -> float:
    if "stinger" in job.filename or "death" in job.filename or "threat" in job.filename:
        return 12.0
    if "layer" in job.filename or "pause" in job.filename or "loading" in job.filename:
        return 30.0
    if "low_health" in job.filename:
        return 18.0
    if "combat" in job.filename:
        return 45.0
    if "boss" in job.filename:
        return 60.0
    return 30.0


def generate_hf(prompt: str, *, duration_sec: float, hf_token: str | None, timeout_sec: float) -> bytes:
    headers: dict[str, str] = {"Content-Type": "application/json"}
    if hf_token:
        headers["Authorization"] = f"Bearer {hf_token}"

    payload = {
        "inputs": PREFIX + prompt,
        "parameters": {"max_new_tokens": int(duration_sec * 50)},
    }
    resp = requests.post(HF_URL, headers=headers, json=payload, timeout=timeout_sec)
    if resp.status_code == 503:
        deadline = time.time() + timeout_sec
        while time.time() < deadline:
            time.sleep(8)
            resp = requests.post(HF_URL, headers=headers, json=payload, timeout=timeout_sec)
            if resp.status_code == 200:
                break
    if resp.status_code != 200:
        raise RuntimeError(f"HF HTTP {resp.status_code}: {resp.text[:400]}")
    if resp.headers.get("content-type", "").startswith("application/json"):
        data = resp.json()
        if isinstance(data, dict) and data.get("error"):
            raise RuntimeError(str(data["error"]))
        raise RuntimeError(f"HF returned JSON instead of audio: {str(data)[:300]}")
    return resp.content


def run_job(job: MusicJob, *, hf_token: str | None, skip_existing: bool, timeout_sec: float) -> tuple[MusicJob, str | None]:
    dest = MUSIC_ROOT / job.filename
    if skip_existing and dest.exists() and dest.stat().st_size > 10_000:
        return job, "skipped"
    try:
        MUSIC_ROOT.mkdir(parents=True, exist_ok=True)
        audio = generate_hf(
            job.prompt,
            duration_sec=_duration_for_job(job),
            hf_token=hf_token,
            timeout_sec=timeout_sec,
        )
        dest.write_bytes(audio)
        return job, None
    except Exception as exc:
        return job, str(exc)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Batch-generate Forest Alchemist music")
    parser.add_argument("-j", "--jobs", type=int, default=3, help="Parallel workers (default 3)")
    parser.add_argument("--skip-existing", action="store_true")
    parser.add_argument("--only", nargs="*", help="Generate only matching filename substrings")
    parser.add_argument("--timeout", type=float, default=600.0, help="Per-track timeout seconds")
    args = parser.parse_args(argv)

    hf_token = _load_hf_token()

    jobs = JOBS
    if args.only:
        needles = [n.lower() for n in args.only]
        jobs = [j for j in jobs if any(n in j.filename.lower() for n in needles)]
    if not jobs:
        print("Error: no music jobs found in AUDIO.md", file=sys.stderr)
        return 1

    print(f"Generating {len(jobs)} music tracks with {args.jobs} workers -> {MUSIC_ROOT}")
    t0 = time.time()
    ok = skip = fail = 0
    errors: list[str] = []

    with ThreadPoolExecutor(max_workers=args.jobs) as pool:
        futures = {
            pool.submit(
                run_job,
                job,
                hf_token=hf_token,
                skip_existing=args.skip_existing,
                timeout_sec=args.timeout,
            ): job
            for job in jobs
        }
        for fut in as_completed(futures):
            job, err = fut.result()
            if err == "skipped":
                skip += 1
                print(f"SKIP {job.filename}")
            elif err:
                fail += 1
                errors.append(f"{job.filename}: {err}")
                print(f"FAIL {job.filename}: {err}", file=sys.stderr)
            else:
                ok += 1
                size_kb = (MUSIC_ROOT / job.filename).stat().st_size // 1024
                print(f"OK   {job.filename} ({size_kb} KB)")

    elapsed = time.time() - t0
    print(f"\nDone in {elapsed:.1f}s — ok={ok} skip={skip} fail={fail}")
    if errors:
        print("\nFailures:", file=sys.stderr)
        for e in errors:
            print(f"  {e}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
