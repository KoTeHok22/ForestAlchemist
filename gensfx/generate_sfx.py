#!/usr/bin/env python3
"""
ElevenLabs SFX via https://elevenlabs.io/sound-effects landing API.

Uses Proxy6 (proxy6.net) proxies: auto-buy on start, rotate on quota limit.
Fresh browser fingerprint on every request.

Spec: gensfx/captured/web_api_spec.json
"""

from __future__ import annotations

import argparse
import base64
import json
import random
import re
import shutil
import subprocess
import sys
import time
from dataclasses import dataclass
from pathlib import Path
from typing import Any

import requests

from browser_fingerprint import build_headers, fingerprint_summary
from free_proxy_manager import REQUEST_TIMEOUT, FreeProxyManager, get_free_proxy_manager
from proxy_manager import ProxyManager, get_proxy_manager

ROOT = Path(__file__).resolve().parent

ELEVENLABS_URL = "https://api.elevenlabs.io/sound-generation"
DEFAULT_OUTPUT_FORMAT = "opus_48000_128"
SUPPORTED_EXTENSIONS = {".opus", ".ogg", ".wav"}
MAX_PROXY_ROTATIONS = 8
MAX_FREE_PROXY_ROTATIONS = 50

ProxyBackend = ProxyManager | FreeProxyManager | None


class QuotaExceededError(RuntimeError):
    pass


@dataclass
class GenerationResult:
    prompt: str
    variant_index: int
    source_id: str
    duration_seconds: float | None
    path: Path


@dataclass
class AudioVariant:
    audio_bytes: bytes
    source_id: str
    duration_seconds: float | None = None


def _slug(text: str, max_len: int = 48) -> str:
    slug = re.sub(r"[^a-z0-9]+", "_", text.lower()).strip("_")
    return slug[:max_len] or "sfx"


def _find_ffmpeg() -> str | None:
    found = shutil.which("ffmpeg")
    if found:
        return found
    try:
        import imageio_ffmpeg

        return imageio_ffmpeg.get_ffmpeg_exe()
    except Exception:
        return None


def _ffmpeg_to_wav(src: Path, wav_path: Path) -> tuple[bool, str]:
    ffmpeg = _find_ffmpeg()
    if not ffmpeg:
        return False, "ffmpeg not found (install ffmpeg or: pip install imageio-ffmpeg)"
    proc = subprocess.run(
        [ffmpeg, "-y", "-i", str(src), "-ar", "44100", "-ac", "2", str(wav_path)],
        capture_output=True,
        text=True,
    )
    if proc.returncode == 0 and wav_path.exists():
        return True, ""
    detail = (proc.stderr or proc.stdout or "").strip()
    return False, detail or f"ffmpeg exited with code {proc.returncode}"


def _variant_path(base: Path, index: int, *, multiple: bool) -> Path:
    if not multiple:
        return base
    return base.with_name(f"{base.stem}_{index:02d}{base.suffix}")


def _write_audio(audio_bytes: bytes, dest: Path) -> None:
    ext = dest.suffix.lower()
    if ext not in SUPPORTED_EXTENSIONS:
        supported = ", ".join(sorted(SUPPORTED_EXTENSIONS))
        raise ValueError(f"Unsupported format {ext!r}; use: {supported}")

    dest.parent.mkdir(parents=True, exist_ok=True)

    if ext in (".opus", ".ogg"):
        dest.write_bytes(audio_bytes)
        return

    tmp = dest.with_suffix(".opus")
    try:
        tmp.write_bytes(audio_bytes)
        ok, err = _ffmpeg_to_wav(tmp, dest)
        if not ok:
            raise RuntimeError(f"WAV conversion failed: {err}")
    finally:
        if tmp.exists():
            tmp.unlink()


def _parse_elevenlabs_error(response: requests.Response) -> None:
    detail = response.text[:500]
    try:
        err = response.json().get("detail") or {}
        if isinstance(err, dict):
            code = err.get("code", "")
            if code in ("quota_exceeded", "sign_in_required") or response.status_code == 401:
                raise QuotaExceededError(err.get("message") or code)
            if err.get("message"):
                detail = err["message"]
    except (ValueError, AttributeError):
        pass
    if response.status_code in (401, 429):
        raise QuotaExceededError(detail)
    raise RuntimeError(f"sound-generation HTTP {response.status_code}: {detail}")


def _decode_elevenlabs(payload: dict[str, Any]) -> list[AudioVariant]:
    items = payload.get("sound_generations_with_waveforms") or []
    out: list[AudioVariant] = []
    for item in items:
        meta = item.get("sound_generation_history_item") or {}
        b64 = item.get("waveform_base_64")
        if not b64:
            continue
        audio = base64.b64decode(b64)
        if audio[:4] != b"OggS":
            raise ValueError(f"Expected Ogg/Opus, got magic {audio[:4]!r}")
        out.append(
            AudioVariant(
                audio_bytes=audio,
                source_id=meta.get("sound_generation_history_item_id", "gen"),
                duration_seconds=meta.get("audio_duration_seconds"),
            )
        )
    return out


def _post_elevenlabs(
    prompt: str,
    *,
    loop: bool,
    manager,
    timeout_sec: float,
    verbose: bool,
) -> list[AudioVariant]:
    if verbose and manager:
        print(f"proxy: {manager.current_label()}", file=sys.stderr)

    headers = build_headers("elevenlabs")
    if verbose:
        print(f"fingerprint: {fingerprint_summary(headers)}", file=sys.stderr)

    body = {"text": prompt, "loop": loop, "output_format": DEFAULT_OUTPUT_FORMAT}
    schemes = manager.schemes_for_current() if manager else [None]
    last_exc: Exception | None = None

    for scheme in schemes:
        if manager:
            proxies = manager.ensure_proxy().to_requests_proxies(scheme=scheme)
        else:
            proxies = None
        try:
            response = requests.post(
                ELEVENLABS_URL,
                headers=headers,
                json=body,
                proxies=proxies,
                timeout=timeout_sec,
            )
            if response.status_code != 200:
                _parse_elevenlabs_error(response)
            variants = _decode_elevenlabs(response.json())
            if not variants:
                raise RuntimeError("No sound_generations_with_waveforms in response")
            return variants
        except QuotaExceededError:
            raise
        except requests.RequestException as exc:
            last_exc = exc
            if verbose and scheme:
                print(f"proxy scheme {scheme} failed: {exc}", file=sys.stderr)
            continue

    if last_exc:
        raise last_exc
    raise RuntimeError("ElevenLabs request failed")


def _resolve_manager(*, use_proxy: bool, free_proxy: bool) -> ProxyBackend:
    if not use_proxy:
        return None
    if free_proxy:
        return get_free_proxy_manager()
    return get_proxy_manager(enabled=True)


def generate_sfx(
    prompt: str,
    *,
    loop: bool = False,
    use_proxy: bool = True,
    free_proxy: bool = False,
    timeout_sec: float = 120.0,
    jitter_sec: float = 0.0,
    verbose: bool = False,
) -> list[AudioVariant]:
    if jitter_sec > 0:
        time.sleep(random.uniform(0, jitter_sec))

    manager = _resolve_manager(use_proxy=use_proxy, free_proxy=free_proxy)
    max_rotations = MAX_FREE_PROXY_ROTATIONS if free_proxy else MAX_PROXY_ROTATIONS
    if free_proxy and timeout_sec == 120.0:
        timeout_sec = REQUEST_TIMEOUT
    last_error: Exception | None = None

    for attempt in range(1, max_rotations + 1):
        try:
            return _post_elevenlabs(
                prompt,
                loop=loop,
                manager=manager,
                timeout_sec=timeout_sec,
                verbose=verbose,
            )
        except QuotaExceededError as exc:
            last_error = exc
            if not manager:
                raise
            manager.rotate(f"quota exceeded (attempt {attempt})")
        except (requests.RequestException, RuntimeError, ValueError) as exc:
            last_error = exc
            if not manager:
                raise RuntimeError(f"Request failed: {exc}") from exc
            reason = str(exc).encode("ascii", "replace").decode("ascii")
            manager.rotate(f"proxy error: {reason[:120]}")

    raise RuntimeError(f"Failed after {max_rotations} proxy rotations: {last_error}")


def save_variants(
    variants: list[AudioVariant],
    *,
    prompt: str,
    output_file: Path | None,
    output_dir: Path,
    name_prefix: str,
    save_all: bool,
    convert_wav: bool,
) -> list[GenerationResult]:
    selected = variants if save_all else [variants[0]]
    multiple = len(selected) > 1
    results: list[GenerationResult] = []

    for idx, variant in enumerate(selected, start=1):
        if output_file is not None:
            dest = _variant_path(output_file, idx, multiple=multiple)
            _write_audio(variant.audio_bytes, dest)
        else:
            ext = ".wav" if convert_wav else ".opus"
            dest = output_dir / f"{name_prefix}_{idx:02d}{ext}"
            dest.parent.mkdir(parents=True, exist_ok=True)
            _write_audio(variant.audio_bytes, dest)

        results.append(
            GenerationResult(
                prompt=prompt,
                variant_index=idx,
                source_id=variant.source_id,
                duration_seconds=variant.duration_seconds,
                path=dest,
            )
        )
    return results


def run_probe(trace_out: Path, args: argparse.Namespace) -> None:
    prompt = "Short UI click, wooden button, fantasy game, 0.1 sec"
    variants = generate_sfx(
        prompt,
        loop=resolve_loop(args.loop),
        use_proxy=not args.no_proxy,
        free_proxy=args.free_proxy,
        verbose=args.verbose,
    )
    results = save_variants(
        variants,
        prompt=prompt,
        output_file=None,
        output_dir=ROOT / "output" / "probe",
        name_prefix="probe",
        save_all=True,
        convert_wav=False,
    )
    mgr = _resolve_manager(use_proxy=not args.no_proxy, free_proxy=args.free_proxy)
    trace = {
        "endpoint": ELEVENLABS_URL,
        "proxy": mgr.current_label() if mgr else None,
        "variant_count": len(variants),
        "saved_files": [str(r.path) for r in results],
    }
    trace_out.parent.mkdir(parents=True, exist_ok=True)
    trace_out.write_text(json.dumps(trace, indent=2, ensure_ascii=False), encoding="utf-8")
    print(f"OK — {len(results)} variants -> {trace_out}")


def run_single(prompt: str, args: argparse.Namespace) -> None:
    if args.file and args.wav and args.file.suffix.lower() != ".wav":
        print("Warning: --wav ignored when --file extension is not .wav", file=sys.stderr)

    variants = generate_sfx(
        prompt,
        loop=resolve_loop(args.loop),
        use_proxy=not args.no_proxy,
        free_proxy=args.free_proxy,
        jitter_sec=args.jitter,
        verbose=args.verbose,
    )
    results = save_variants(
        variants,
        prompt=prompt,
        output_file=args.file,
        output_dir=args.output,
        name_prefix=args.name or _slug(prompt),
        save_all=args.all_variants,
        convert_wav=args.wav and args.file is None,
    )
    for r in results:
        dur = f"{r.duration_seconds}s" if r.duration_seconds else "?"
        print(f"Saved {r.path} ({dur})")


def resolve_loop(cli_loop: bool | None) -> bool:
    return False if cli_loop is None else cli_loop


def _add_shared_flags(parser: argparse.ArgumentParser) -> None:
    parser.add_argument("--wav", action="store_true", help="Convert to 44.1kHz stereo WAV")
    parser.add_argument("--all-variants", action="store_true", help="Save all 4 generations")
    loop_group = parser.add_mutually_exclusive_group()
    loop_group.add_argument("--loop", dest="loop", action="store_true", help="Seamless loop")
    loop_group.add_argument("--no-loop", dest="loop", action="store_false", help="One-shot (default)")
    proxy_group = parser.add_mutually_exclusive_group()
    proxy_group.add_argument("--no-proxy", action="store_true", help="Direct connection (no proxy)")
    proxy_group.add_argument("--free-proxy", action="store_true", help="Free public proxy lists (no Proxy6)")
    parser.add_argument("--jitter", type=float, default=0.0, metavar="SEC")
    parser.add_argument("-v", "--verbose", action="store_true")
    parser.set_defaults(loop=None)


def build_parser() -> argparse.ArgumentParser:
    shared = argparse.ArgumentParser(add_help=False)
    _add_shared_flags(shared)

    p = argparse.ArgumentParser(
        description="ElevenLabs SFX + Proxy6 auto-rotation (gensfx)"
    )
    sub = p.add_subparsers(dest="cmd", required=True)

    sub.add_parser("probe", parents=[shared]).add_argument(
        "--trace-out", type=Path, default=ROOT / "captured" / "http_trace.json"
    )

    one = sub.add_parser("generate", parents=[shared])
    one.add_argument("prompt", help="Sound effect description (English)")
    one.add_argument("-f", "--file", type=Path, default=None)
    one.add_argument("-o", "--output", type=Path, default=ROOT / "output")
    one.add_argument("-n", "--name", default=None)

    return p


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)

    if args.cmd == "probe":
        try:
            run_probe(args.trace_out, args)
        except Exception as exc:
            print(f"Probe failed: {exc}", file=sys.stderr)
            return 1
        return 0

    if args.cmd == "generate":
        if args.file and args.file.suffix.lower() not in SUPPORTED_EXTENSIONS:
            print(
                f"Error: --file must end with {', '.join(sorted(SUPPORTED_EXTENSIONS))}",
                file=sys.stderr,
            )
            return 1
        try:
            run_single(args.prompt, args)
        except Exception as exc:
            print(f"Error: {exc}", file=sys.stderr)
            return 1
        return 0

    return 1


if __name__ == "__main__":
    raise SystemExit(main())
