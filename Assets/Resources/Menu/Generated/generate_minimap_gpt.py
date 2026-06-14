import base64
import json
import os
import urllib.request
from collections import deque

from PIL import Image

API_KEY = "sk-inv-vge8miU-jgH5pdlCn99hGgLXC4k_sE0N"
OUT_DIR = os.path.dirname(os.path.abspath(__file__))

ASSETS = [
    (
        "minimap_paper.png",
        "Pixel art fantasy game UI panel, single torn aged parchment paper leaf with ragged uneven torn edges, "
        "large empty blank center for map UI content, warm beige tan medieval forest alchemist style, "
        "subtle paper fiber texture, no text, no icons, no map drawn inside, "
        "fully centered on a completely flat solid pure white background (#FFFFFF), plenty of white margin, isolated on white",
    ),
    (
        "minimap_map_surface.png",
        "Pixel art square fantasy forest map parchment texture, aged paper with faint green moss and dirt watercolor stains, "
        "soft hand-drawn cartography feel, empty center for gameplay markers, no text, no labels, no pins, no compass rose, "
        "fully centered on a completely flat solid pure white background (#FFFFFF), plenty of white margin, isolated on white",
    ),
    (
        "minimap_map_frame.png",
        "Pixel art hollow square wooden map frame border only, ornate carved dark wood corners and edges, "
        "completely empty transparent-looking center hole for map, forest RPG inventory UI frame, no text, "
        "fully centered on a completely flat solid pure white background (#FFFFFF), plenty of white margin, isolated on white",
    ),
]


def flood_trim(src_path, dst_path, thresh=240):
    im = Image.open(src_path).convert("RGBA")
    w, h = im.size
    px = im.load()
    visited = [[False] * w for _ in range(h)]
    q = deque()

    def is_white(x, y):
        r, g, b, a = px[x, y]
        return a > 0 and r >= thresh and g >= thresh and b >= thresh

    for x in range(w):
        for y in (0, h - 1):
            if not visited[y][x] and is_white(x, y):
                q.append((x, y))
                visited[y][x] = True
    for y in range(h):
        for x in (0, w - 1):
            if not visited[y][x] and is_white(x, y):
                q.append((x, y))
                visited[y][x] = True

    while q:
        x, y = q.popleft()
        px[x, y] = (0, 0, 0, 0)
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < w and 0 <= ny < h and not visited[ny][nx] and is_white(nx, ny):
                visited[ny][nx] = True
                q.append((nx, ny))

    bbox = im.getbbox()
    if bbox:
        im = im.crop(bbox)
    im.save(dst_path)
    print(f"processed {dst_path} -> {im.size[0]}x{im.size[1]}")


def generate_png(filename, prompt):
    dst = os.path.join(OUT_DIR, filename)
    raw = os.path.join(OUT_DIR, filename.replace(".png", "_raw.png"))
    if os.path.exists(dst) and os.path.getsize(dst) > 1000:
        print(f"skip existing {dst}")
        return

    body = json.dumps(
        {
            "model": "gpt-image-2",
            "prompt": prompt,
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

    print(f"generating {filename}...")
    with urllib.request.urlopen(req, timeout=180) as resp:
        payload = json.loads(resp.read().decode("utf-8"))

    b64 = payload["data"][0]["b64_json"]
    with open(raw, "wb") as f:
        f.write(base64.b64decode(b64))

    flood_trim(raw, dst)
    if os.path.exists(raw):
        os.remove(raw)


def generate_marker_icons():
  markers = {
      "minimap_icon_player.png": [(240, 220, 170, 255), (50, 36, 22, 255)],
      "minimap_icon_evacuation.png": [(70, 210, 90, 255), (20, 16, 10, 255)],
      "minimap_icon_portal.png": [(80, 150, 255, 255), (20, 16, 10, 255)],
      "minimap_icon_altar_fire.png": [(255, 120, 50, 255), (20, 16, 10, 255)],
      "minimap_icon_altar_water.png": [(80, 190, 255, 255), (20, 16, 10, 255)],
  }

  for name, (fill, border) in markers.items():
      path = os.path.join(OUT_DIR, name)
      img = Image.new("RGBA", (24, 24), (0, 0, 0, 0))
      px = img.load()
      for y in range(24):
          for x in range(24):
              dx = x - 11.5
              dy = y - 11.5
              if dx * dx + dy * dy <= 11 * 11:
                  px[x, y] = fill
              elif dx * dx + dy * dy <= 12.5 * 12.5:
                  px[x, y] = border
      img.save(path)
      print(f"saved marker {path}")


if __name__ == "__main__":
    for filename, prompt in ASSETS:
        generate_png(filename, prompt)
    generate_marker_icons()
