"""KBS adapter for the original ai_auto.py; database changes go through the API."""
import json
import os
import time
from decimal import Decimal
from urllib.error import HTTPError, URLError
from urllib.parse import urlparse
from urllib.request import Request, urlopen


def decision(total_price, limit):
    return "CONFIRM" if Decimal(str(total_price)) < Decimal(str(limit)) else "MANUAL_REVIEW"


def post(base_url, key, path, payload=None):
    data = json.dumps(payload or {}).encode("utf-8")
    request = Request(base_url + path, data=data, method="POST", headers={
        "Content-Type": "application/json", "X-AI-Key": key,
    })
    with urlopen(request, timeout=15) as response:
        return json.loads(response.read(), parse_float=Decimal)


def run_once(base_url, key):
    batch = post(base_url, key, "/api/ai-worker/auto/poll")
    if not batch["isEnabled"]:
        return
    for order in batch["orders"]:
        try:
            result = post(base_url, key, f'/api/ai-worker/auto/orders/{order["id"]}/decision', {
                "decision": decision(order["totalPrice"], batch["approvalLimit"]),
            })
            print(f'Order #{order["id"]}: {result["result"]}', flush=True)
        except HTTPError as error:
            if error.code not in (404, 409):
                raise
            print(f'Order #{order["id"]}: changed concurrently; retry on next poll.', flush=True)


def main():
    base_url = os.environ.get("KBS_API_URL", "http://localhost:5231").rstrip("/")
    key = os.environ.get("KBS_AI_WORKER_KEY", "")
    parsed = urlparse(base_url)
    if parsed.scheme not in ("http", "https") or not parsed.hostname:
        raise SystemExit("KBS_API_URL must be an HTTP(S) URL.")
    if parsed.scheme != "https" and parsed.hostname not in ("localhost", "127.0.0.1", "::1"):
        raise SystemExit("Use HTTPS for a remote API.")
    if len(key) < 32:
        raise SystemExit("Set KBS_AI_WORKER_KEY to the server worker key (at least 32 characters).")
    while True:
        try:
            run_once(base_url, key)
        except (URLError, TimeoutError, OSError, ValueError, KeyError) as error:
            # Do not print request headers, credentials, or server response bodies.
            print(f'Worker connection/response error: {type(error).__name__}; retry in 5s.', flush=True)
        time.sleep(5)


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        pass
