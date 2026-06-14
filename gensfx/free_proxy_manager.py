"""Rotate through free public HTTP/SOCKS5 proxies for ElevenLabs requests."""

from __future__ import annotations

import random
import threading
from concurrent.futures import ThreadPoolExecutor, as_completed
from dataclasses import dataclass

import requests

from browser_fingerprint import build_headers

ELEVENLABS_URL = "https://api.elevenlabs.io/sound-generation"
FETCH_TIMEOUT = 20.0
DISCOVER_TIMEOUT = 18.0
DISCOVER_BATCH = 80
DISCOVER_WORKERS = 25
REQUEST_TIMEOUT = 25.0

FREE_LIST_URLS = [
    "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=socks5&timeout=10000&country=all",
    "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=http&timeout=10000&country=all&ssl=all&anonymity=all",
    "https://raw.githubusercontent.com/TheSpeedX/PROXY-List/master/socks5.txt",
    "https://raw.githubusercontent.com/TheSpeedX/PROXY-List/master/http.txt",
    "https://raw.githubusercontent.com/monosans/proxy-list/main/proxies/socks5.txt",
    "https://raw.githubusercontent.com/monosans/proxy-list/main/proxies/http.txt",
]

_lock = threading.Lock()
_manager: FreeProxyManager | None = None


@dataclass(frozen=True)
class FreeProxy:
    host: str
    port: int
    kind: str  # "socks5" | "http"

    def to_requests_proxies(self, *, scheme: str | None = None) -> dict[str, str]:
        if scheme is None:
            scheme = "socks5h" if self.kind == "socks5" else "http"
        url = f"{scheme}://{self.host}:{self.port}"
        return {"http": url, "https": url}

    def schemes_to_try(self) -> list[str]:
        if self.kind == "socks5":
            return ["socks5h", "http"]
        return ["http", "socks5h"]

    def masked(self) -> str:
        return f"{self.host}:{self.port} ({self.kind})"


def _parse_proxy_line(line: str, default_kind: str) -> FreeProxy | None:
    line = line.strip()
    if not line or line.startswith("#"):
        return None
    if "://" in line:
        line = line.split("://", 1)[1]
    if "@" in line:
        line = line.split("@", 1)[1]
    if ":" not in line:
        return None
    host, port_s = line.rsplit(":", 1)
    host = host.strip()
    try:
        port = int(port_s.strip())
    except ValueError:
        return None
    if not host or port < 1 or port > 65535:
        return None
    return FreeProxy(host=host, port=port, kind=default_kind)


def _fetch_list(url: str) -> list[FreeProxy]:
    kind = "socks5" if "socks5" in url.lower() else "http"
    try:
        response = requests.get(url, timeout=FETCH_TIMEOUT)
        response.raise_for_status()
    except requests.RequestException:
        return []
    out: list[FreeProxy] = []
    for line in response.text.splitlines():
        proxy = _parse_proxy_line(line, kind)
        if proxy:
            out.append(proxy)
    return out


def fetch_free_proxies() -> list[FreeProxy]:
    seen: set[tuple[str, int, str]] = set()
    merged: list[FreeProxy] = []
    for url in FREE_LIST_URLS:
        for proxy in _fetch_list(url):
            key = (proxy.host, proxy.port, proxy.kind)
            if key in seen:
                continue
            seen.add(key)
            merged.append(proxy)
    random.shuffle(merged)
    return merged


def _test_proxy(proxy: FreeProxy) -> tuple[FreeProxy, str] | None:
    body = {"text": "ui click", "loop": False, "output_format": "opus_48000_128"}
    headers = build_headers("elevenlabs")
    for scheme in proxy.schemes_to_try():
        proxies = proxy.to_requests_proxies(scheme=scheme)
        try:
            response = requests.post(
                ELEVENLABS_URL,
                headers=headers,
                json=body,
                proxies=proxies,
                timeout=DISCOVER_TIMEOUT,
            )
            if response.status_code == 200:
                try:
                    payload = response.json()
                    if payload.get("sound_generations_with_waveforms"):
                        return proxy, scheme
                except ValueError:
                    continue
            if response.status_code in (401, 429):
                detail = response.text
                if "quota_exceeded" in detail:
                    continue
        except requests.RequestException:
            continue
    return None


def discover_working_proxies(candidates: list[FreeProxy], *, limit: int = 8) -> list[tuple[FreeProxy, str]]:
    if not candidates:
        return []
    batch = candidates[:DISCOVER_BATCH]
    found: list[tuple[FreeProxy, str]] = []
    with ThreadPoolExecutor(max_workers=DISCOVER_WORKERS) as pool:
        futures = {pool.submit(_test_proxy, proxy): proxy for proxy in batch}
        for fut in as_completed(futures):
            hit = fut.result()
            if hit:
                found.append(hit)
                if len(found) >= limit:
                    for pending in futures:
                        pending.cancel()
                    break
    return found


class FreeProxyManager:
    def __init__(self) -> None:
        self._pool: list[FreeProxy] = []
        self._verified: list[tuple[FreeProxy, str]] = []
        self._bad: set[tuple[str, int, str]] = set()
        self._current: FreeProxy | None = None
        self._current_scheme: str | None = None
        self._refill()

    def _refill(self) -> None:
        fresh = fetch_free_proxies()
        self._pool = [p for p in fresh if (p.host, p.port, p.kind) not in self._bad]
        if self._pool:
            print(f"free-proxy: loaded {len(self._pool)} candidates", flush=True)
        hits = discover_working_proxies(self._pool)
        tested = {(p.host, p.port, p.kind) for p in self._pool[:DISCOVER_BATCH]}
        self._pool = [p for p in self._pool if (p.host, p.port, p.kind) not in tested]
        for item in hits:
            key = (item[0].host, item[0].port, item[0].kind)
            if key not in self._bad:
                self._verified.append(item)
        if hits:
            print(f"free-proxy: discovered {len(hits)} working proxies", flush=True)

    def _activate(self, proxy: FreeProxy, scheme: str) -> FreeProxy:
        self._current = proxy
        self._current_scheme = scheme
        print(f"free-proxy: using {proxy.masked()} via {scheme}", flush=True)
        return proxy

    def ensure_proxy(self) -> FreeProxy:
        if (
            self._current
            and self._current_scheme
            and (self._current.host, self._current.port, self._current.kind) not in self._bad
        ):
            return self._current
        if self._verified:
            proxy, scheme = self._verified.pop(0)
            return self._activate(proxy, scheme)
        self._refill()
        if self._verified:
            proxy, scheme = self._verified.pop(0)
            return self._activate(proxy, scheme)
        raise RuntimeError("free-proxy: no working proxies found in public lists")

    def rotate(self, reason: str = "") -> FreeProxy:
        if reason:
            print(f"free-proxy: rotating ({reason})", flush=True)
        if self._current:
            self._bad.add((self._current.host, self._current.port, self._current.kind))
            self._current = None
            self._current_scheme = None
        return self.ensure_proxy()

    def schemes_for_current(self) -> list[str]:
        proxy = self.ensure_proxy()
        if self._current_scheme:
            rest = [s for s in proxy.schemes_to_try() if s != self._current_scheme]
            return [self._current_scheme, *rest]
        return proxy.schemes_to_try()

    def current_label(self) -> str:
        if not self._current:
            return "none"
        return self._current.masked()

    def proxies_dict(self) -> dict[str, str]:
        return self.ensure_proxy().to_requests_proxies()


def get_free_proxy_manager() -> FreeProxyManager:
    global _manager
    with _lock:
        if _manager is None:
            _manager = FreeProxyManager()
        return _manager


def reset_free_proxy_manager() -> None:
    global _manager
    with _lock:
        _manager = None
