import base64
import json
import os
import urllib.request

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "grass.png")
API_KEY = "sk-inv-vge8miU-jgH5pdlCn99hGgLXC4k_sE0N"

PROMPT = (
    "Seamless tileable top-down pixel art forest grass ground texture for 2D RPG, "
    "fills the entire image edge to edge with no frame and no border, "
    "rich natural green meadow with subtle color variation, tiny wildflowers and soft dirt patches, "
    "medieval fantasy forest alchemist game style, crisp pixels, "
    "must tile perfectly on all four sides when repeated, no objects no trees no characters, "
    "fully opaque texture covering 100 percent of canvas"
)

body = json.dumps(
    {
        "model": "gpt-image-2",
        "prompt": PROMPT,
        "size": "1024x1024",
        "response_format": "b64_json",
    }
).encode("utf-8")

req = urllib.request.Request(
    "https://codex.sale/v1/images/generations",
    data=body,
    headers={
        "Authorization": f"Bearer {API_KEY}",
        "Content-Type": "application/json",
    },
    method="POST",
)

print("generating grass...")
with urllib.request.urlopen(req, timeout=180) as resp:
    payload = json.loads(resp.read().decode("utf-8"))

b64 = payload["data"][0]["b64_json"]
with open(OUT, "wb") as f:
    f.write(base64.b64decode(b64))

print("saved", OUT, os.path.getsize(OUT), "bytes")
