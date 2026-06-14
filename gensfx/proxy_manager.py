"""Auto-buy and rotate Proxy6 proxies for ElevenLabs requests."""

from __future__ import annotations

import json
import os
import threading
import time
from dataclasses import dataclass
from pathlib import Path

from proxy6_client import Proxy6Client, Proxy6Error, Proxy6Proxy

ROOT = Path(__file__).resolve().parent
LOCAL_CONFIG = ROOT / "proxy6.local.json"
EXAMPLE_CONFIG = ROOT / "proxy6.local.json.example"
STATE_FILE = ROOT / ".proxy_state.json"

# version 3 = IPv4 Shared (дешевле обычных IPv4, version 4)
DEFAULT_CONFIG = {
    "country": "us",
    "version": 3,
    "period": 7,
    "descr": "gensfx",
}


@dataclass
class ProxyConfig:
    api_key: str
    country: str = "us"
    version: int = 3
    period: int = 7
    descr: str = "gensfx"


_lock = threading.Lock()
_manager: ProxyManager | None = None


class ProxyManager:
    def __init__(self, config: ProxyConfig) -> None:
        self.config = config
        self.client = Proxy6Client(config.api_key)
        self._current: Proxy6Proxy | None = None
        self._load_state()

    def _load_state(self) -> None:
        if not STATE_FILE.exists():
            return
        try:
            data = json.loads(STATE_FILE.read_text(encoding="utf-8"))
            if data.get("id"):
                saved_version = str(data.get("version", ""))
                if saved_version and saved_version != str(self.config.version):
                    self._current = None
                    return
                self._current = Proxy6Proxy(
                    id=data["id"],
                    host=data["host"],
                    port=data["port"],
                    user=data["user"],
                    password=data["password"],
                    proxy_type=data.get("proxy_type", "http"),
                    country=data.get("country", ""),
                    active=True,
                    version=saved_version,
                )
        except (json.JSONDecodeError, KeyError, TypeError):
            self._current = None

    def _save_state(self, proxy: Proxy6Proxy) -> None:
        STATE_FILE.write_text(
            json.dumps(
                {
                    "id": proxy.id,
                    "host": proxy.host,
                    "port": proxy.port,
                    "user": proxy.user,
                    "password": proxy.password,
                    "proxy_type": proxy.proxy_type,
                    "country": proxy.country,
                    "version": proxy.version or str(self.config.version),
                },
                indent=2,
            ),
            encoding="utf-8",
        )

    def _matches_config(self, proxy: Proxy6Proxy) -> bool:
        if proxy.version and proxy.version != str(self.config.version):
            return False
        if proxy.country and proxy.country != self.config.country:
            return False
        return True

    def _sync_active(self) -> Proxy6Proxy | None:
        active = self.client.list_proxies(descr=self.config.descr, state="active")
        matching = [p for p in active if self._matches_config(p)]
        if not matching:
            return None
        if self._current:
            for item in matching:
                if item.id == self._current.id:
                    return item
        return matching[0]

    def _buy_new(self) -> Proxy6Proxy:
        old_id = self._current.id if self._current else None
        bought = self.client.buy(
            count=1,
            period=self.config.period,
            country=self.config.country,
            version=self.config.version,
            descr=self.config.descr,
        )
        if not bought:
            raise Proxy6Error("proxy6 buy returned empty list")
        proxy = bought[0]
        time.sleep(2)
        self._current = proxy
        self._save_state(proxy)
        if old_id and old_id != proxy.id:
            try:
                self.client.delete(old_id)
            except Proxy6Error:
                pass
        return proxy

    def ensure_proxy(self) -> Proxy6Proxy:
        synced = self._sync_active()
        if synced:
            self._current = synced
            self._save_state(synced)
            return synced
        return self._buy_new()

    def rotate(self, reason: str = "") -> Proxy6Proxy:
        if reason:
            print(f"proxy: rotating ({reason})", flush=True)
        return self._buy_new()

    def proxies_dict(self) -> dict[str, str]:
        proxy = self.ensure_proxy()
        return proxy.to_requests_proxies()

    def schemes_for_current(self) -> list[str]:
        proxy = self.ensure_proxy()
        return proxy.schemes_to_try()

    def current_label(self) -> str:
        if not self._current:
            return "none"
        return self._current.masked()


def load_config() -> ProxyConfig:
    api_key = os.environ.get("PROXY6_API_KEY", "").strip()
    data: dict = dict(DEFAULT_CONFIG)

    if LOCAL_CONFIG.exists():
        file_data = json.loads(LOCAL_CONFIG.read_text(encoding="utf-8"))
        data.update({k: v for k, v in file_data.items() if k != "api_key" or not api_key})
        if not api_key:
            api_key = str(file_data.get("api_key", "")).strip()

    if not api_key:
        raise Proxy6Error(
            f"Set PROXY6_API_KEY env or create {LOCAL_CONFIG.name} "
            f"(see {EXAMPLE_CONFIG.name})"
        )

    return ProxyConfig(
        api_key=api_key,
        country=str(data.get("country", DEFAULT_CONFIG["country"])),
        version=int(data.get("version", DEFAULT_CONFIG["version"])),
        period=int(data.get("period", DEFAULT_CONFIG["period"])),
        descr=str(data.get("descr", DEFAULT_CONFIG["descr"]))[:50],
    )


def get_proxy_manager(*, enabled: bool = True) -> ProxyManager | None:
    global _manager
    if not enabled:
        return None
    with _lock:
        if _manager is None:
            _manager = ProxyManager(load_config())
        return _manager


def reset_proxy_manager() -> None:
    global _manager
    with _lock:
        _manager = None
