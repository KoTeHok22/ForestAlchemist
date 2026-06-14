from PIL import Image
import math
import os

OUT = os.path.dirname(os.path.abspath(__file__))


def px(img, x, y, c):
    if 0 <= x < img.width and 0 <= y < img.height:
        img.putpixel((x, y), c)


def rect(img, x0, y0, w, h, c):
    for y in range(y0, y0 + h):
        for x in range(x0, x0 + w):
            px(img, x, y, c)


def draw_evacuation():
    img = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    rect(img, 10, 24, 12, 4, (90, 88, 82, 255))
    rect(img, 11, 20, 10, 5, (110, 108, 100, 255))
    for y in range(8, 20):
        px(img, 15, y, (55, 170, 75, 255))
        px(img, 16, y, (40, 140, 60, 255))
    rect(img, 13, 4, 6, 5, (120, 255, 140, 255))
    rect(img, 14, 2, 4, 3, (200, 255, 210, 255))
    px(img, 12, 6, (180, 255, 190, 180))
    px(img, 19, 6, (180, 255, 190, 180))
    px(img, 15, 0, (220, 255, 220, 160))
    return img


def draw_portal():
    img = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    for angle in range(0, 360, 3):
        rad = math.radians(angle)
        x = int(15.5 + math.cos(rad) * 11)
        y = int(15.5 + math.sin(rad) * 11)
        px(img, x, y, (70, 140, 255, 255))
    for i in range(120):
        t = i / 20.0
        ang = t * 2.4
        x = int(15.5 + math.cos(ang) * (2 + t * 0.35))
        y = int(15.5 + math.sin(ang) * (2 + t * 0.35))
        px(img, x, y, (160, 220, 255, 220))
    rect(img, 14, 14, 4, 4, (230, 245, 255, 255))
    return img


def draw_altar(fire=True):
    img = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    rect(img, 8, 20, 16, 6, (95, 90, 82, 255))
    rect(img, 10, 14, 12, 7, (115, 108, 98, 255))
    rect(img, 12, 10, 8, 5, (130, 122, 110, 255))
    if fire:
        cols = [(255, 90, 30, 255), (255, 150, 40, 255), (255, 220, 80, 255)]
        coords = [(14, 8), (15, 7), (16, 8), (15, 6), (14, 7), (16, 7), (15, 5)]
        for i, c in enumerate(coords):
            px(img, c[0], c[1], cols[i % 3])
        px(img, 15, 4, (255, 240, 120, 220))
    else:
        rect(img, 12, 11, 8, 3, (40, 110, 200, 255))
        px(img, 13, 10, (120, 200, 255, 255))
        px(img, 16, 9, (180, 230, 255, 220))
        px(img, 18, 10, (120, 200, 255, 200))
    return img


if __name__ == "__main__":
    draw_evacuation().save(os.path.join(OUT, "evacuation_beacon.png"))
    draw_portal().save(os.path.join(OUT, "portal_ring.png"))
    draw_altar(True).save(os.path.join(OUT, "altar_fire.png"))
    draw_altar(False).save(os.path.join(OUT, "altar_water.png"))
    print("saved objective sprites to", OUT)
