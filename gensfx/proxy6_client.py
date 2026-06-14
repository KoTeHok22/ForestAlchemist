"""Minimal client for https://proxy6.net API (px6.link)."""

from __future__ import annotations

import time
from dataclasses import dataclass
from typing import Any
from urllib.parse import urlencode

import requests

API_BASE = "https://px6.link/api"
MIN_REQUEST_INTERVAL = 0.34  # max 3 req/s


@dataclass(frozen=True)
class Proxy6Proxy:
    id: str
    host: str
    port: str
    user: str
    password: str
    proxy_type: str
    country: str
    active: bool
    version: str = ""

    def to_requests_proxies(self, *, scheme: str | None = None) -> dict[str, str]:
        if scheme is None:
            # Shared (auto) на Proxy6 надёжнее через SOCKS5
            scheme = "socks5h" if self.proxy_type in ("socks", "auto") else "http"
        auth = f"{self.user}:{self.password}@{self.host}:{self.port}"
        url = f"{scheme}://{auth}"
        return {"http": url, "https": url}

    def schemes_to_try(self) -> list[str]:
        if self.proxy_type in ("socks", "auto"):
            return ["socks5h", "http"]
        return ["http", "socks5h"]

    def masked(self) -> str:
        return f"{self.host}:{self.port} ({self.country})"


class Proxy6Error(RuntimeError):
    pass


class Proxy6Client:
    def __init__(self, api_key: str) -> None:
        self._api_key = api_key
        self._last_call = 0.0

    def _call(self, method: str, **params: Any) -> dict[str, Any]:
        elapsed = time.monotonic() - self._last_call
        if elapsed < MIN_REQUEST_INTERVAL:
            time.sleep(MIN_REQUEST_INTERVAL - elapsed)
        query = urlencode({k: v for k, v in params.items() if v is not None})
        url = f"{API_BASE}/{self._api_key}/{method}/"
        if query:
            url = f"{url}?{query}"
        response = requests.get(url, timeout=30)
        self._last_call = time.monotonic()
        try:
            payload = response.json()
        except ValueError as exc:
            raise Proxy6Error(f"Invalid JSON from proxy6: {response.text[:200]}") from exc
        if payload.get("status") != "yes":
            raise Proxy6Error(
                f"proxy6 {method} failed: [{payload.get('error_id')}] {payload.get('error')}"
            )
        return payload

    def get_balance(self) -> tuple[float, str]:
        data = self._call("")
        return float(data.get("balance", 0)), str(data.get("currency", "RUB"))

    def get_price(self, *, count: int, period: int, version: int) -> float:
        data = self._call("getprice", count=count, period=period, version=version)
        return float(data["price"])

    def list_proxies(self, *, descr: str, state: str = "active") -> list[Proxy6Proxy]:
        data = self._call("getproxy", descr=descr, state=state)
        items = data.get("list") or {}
        out: list[Proxy6Proxy] = []
        for raw in items.values():
            out.append(_parse_proxy(raw))
        return out

    def buy(
        self,
        *,
        count: int,
        period: int,
        country: str,
        version: int,
        descr: str,
    ) -> list[Proxy6Proxy]:
        data = self._call(
            "buy",
            count=count,
            period=period,
            country=country,
            version=version,
            descr=descr,
        )
        items = data.get("list") or {}
        return [_parse_proxy(raw) for raw in items.values()]

    def delete(self, proxy_id: str) -> None:
        self._call("delete", ids=proxy_id)


def _parse_proxy(raw: dict[str, Any]) -> Proxy6Proxy:
    return Proxy6Proxy(
        id=str(raw.get("id", "")),
        host=str(raw.get("host", "")),
        port=str(raw.get("port", "")),
        user=str(raw.get("user", "")),
        password=str(raw.get("pass", "")),
        proxy_type=str(raw.get("type", "http")),
        country=str(raw.get("country", "")),
        active=str(raw.get("active", "0")) == "1",
        version=str(raw.get("version", "")),
    )
