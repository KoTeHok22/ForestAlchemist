from PIL import Image, ImageDraw
import os
import random

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "minimap_paper.png")
random.seed(42)

W, H = 512, 512
img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
draw = ImageDraw.Draw(img)

base = (228, 206, 164, 255)
edge = (176, 142, 96, 255)
stain = (198, 170, 126, 255)

draw.rectangle((24, 20, W - 24, H - 20), fill=base)

for _ in range(1200):
    x = random.randint(20, W - 20)
    y = random.randint(16, H - 16)
    c = (
        base[0] + random.randint(-18, 18),
        base[1] + random.randint(-18, 18),
        base[2] + random.randint(-18, 18),
        255,
    )
    draw.point((x, y), fill=c)

for _ in range(18):
    x = random.randint(40, W - 40)
    y = random.randint(36, H - 36)
    r = random.randint(8, 28)
    draw.ellipse((x - r, y - r, x + r, y + r), fill=stain)

# torn edges
for x in range(W):
    if random.random() < 0.08:
        h = random.randint(4, 18)
        draw.rectangle((x, 0, x, h), fill=(0, 0, 0, 0))
        draw.rectangle((x, H - h, x, H), fill=(0, 0, 0, 0))

for y in range(H):
    if random.random() < 0.08:
        w = random.randint(4, 18)
        draw.rectangle((0, y, w, y), fill=(0, 0, 0, 0))
        draw.rectangle((W - w, y, W, y), fill=(0, 0, 0, 0))

draw.rectangle((24, 20, W - 24, H - 20), outline=edge, width=3)
img.save(OUT)
print("saved", OUT)
