"""Rotate browser-like HTTP headers per request (IP unchanged)."""

from __future__ import annotations

import random
from dataclasses import dataclass

PROVIDERS = {
    "elevenlabs": {
        "origin": "https://elevenlabs.io",
        "referers": (
            "https://elevenlabs.io/",
            "https://elevenlabs.io/sound-effects",
            "https://elevenlabs.io/app/sound-effects/generate",
        ),
    },
    "poppop": {
        "origin": "https://poppop.ai",
        "referers": (
            "https://poppop.ai/",
            "https://poppop.ai/ai-sound-effect-generator",
        ),
    },
}

_ACCEPT_LANGUAGES = (
    "en-US,en;q=0.9",
    "en-GB,en;q=0.9",
    "en-US,en;q=0.9,ru;q=0.8",
    "en-US,en;q=0.8,ru;q=0.6",
    "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7",
)

_ACCEPT_ENCODINGS = (
    "gzip, deflate, br, zstd",
    "gzip, deflate, br",
)

_PRIORITIES = ("u=1, i", "u=1")


@dataclass(frozen=True)
class BrowserProfile:
    user_agent: str
    sec_ch_ua: str | None
    sec_ch_ua_mobile: str
    sec_ch_ua_platform: str


_PROFILES: tuple[BrowserProfile, ...] = (
    BrowserProfile(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
        "(KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
        '"Google Chrome";v="131", "Chromium";v="131", "Not_A Brand";v="24"',
        "?0",
        '"Windows"',
    ),
    BrowserProfile(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
        "(KHTML, like Gecko) Chrome/133.0.0.0 Safari/537.36",
        '"Not(A:Brand";v="99", "Google Chrome";v="133", "Chromium";v="133"',
        "?0",
        '"Windows"',
    ),
    BrowserProfile(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
        "(KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36 Edg/134.0.0.0",
        '"Chromium";v="134", "Microsoft Edge";v="134", "Not.A/Brand";v="24"',
        "?0",
        '"Windows"',
    ),
    BrowserProfile(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:135.0) Gecko/20100101 Firefox/135.0",
        None,
        "?0",
        '"Windows"',
    ),
)


def random_profile() -> BrowserProfile:
    return random.choice(_PROFILES)


def build_headers(provider: str = "poppop", profile: BrowserProfile | None = None) -> dict[str, str]:
    cfg = PROVIDERS[provider]
    p = profile or random_profile()
    headers: dict[str, str] = {
        "Content-Type": "application/json",
        "Origin": cfg["origin"],
        "Referer": random.choice(cfg["referers"]),
        "User-Agent": p.user_agent,
        "Accept": "*/*",
        "Accept-Language": random.choice(_ACCEPT_LANGUAGES),
        "Accept-Encoding": random.choice(_ACCEPT_ENCODINGS),
        "Sec-Fetch-Dest": "empty",
        "Sec-Fetch-Mode": "cors",
        "Sec-Fetch-Site": "same-site" if provider == "elevenlabs" else "cross-site",
        "Priority": random.choice(_PRIORITIES),
    }
    if p.sec_ch_ua:
        headers["sec-ch-ua"] = p.sec_ch_ua
        headers["sec-ch-ua-mobile"] = p.sec_ch_ua_mobile
        headers["sec-ch-ua-platform"] = p.sec_ch_ua_platform
    return headers


def fingerprint_summary(headers: dict[str, str]) -> str:
    ua = headers.get("User-Agent", "")
    if "Firefox" in ua:
        browser = "Firefox"
    elif "Edg/" in ua:
        browser = "Edge"
    else:
        browser = "Chrome"
    return f"{browser} | {headers.get('Accept-Language', '')} | {headers.get('Referer', '')}"
