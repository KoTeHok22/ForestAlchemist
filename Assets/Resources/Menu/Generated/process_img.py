import sys
from PIL import Image
from collections import deque

def process(src, dst, thresh=240):
    im = Image.open(src).convert("RGBA")
    w,h = im.size
    px = im.load()
    visited = [[False]*w for _ in range(h)]
    q = deque()
    def is_white(x,y):
        r,g,b,a = px[x,y]
        return a>0 and r>=thresh and g>=thresh and b>=thresh
    # seed from all border pixels
    for x in range(w):
        for y in (0,h-1):
            if not visited[y][x] and is_white(x,y):
                q.append((x,y)); visited[y][x]=True
    for y in range(h):
        for x in (0,w-1):
            if not visited[y][x] and is_white(x,y):
                q.append((x,y)); visited[y][x]=True
    while q:
        x,y = q.popleft()
        px[x,y] = (0,0,0,0)
        for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)):
            nx,ny = x+dx,y+dy
            if 0<=nx<w and 0<=ny<h and not visited[ny][nx] and is_white(nx,ny):
                visited[ny][nx]=True; q.append((nx,ny))
    # auto-trim transparent margins (alpha>16)
    bbox = im.getbbox()  # bbox of non-zero alpha after flood
    # getbbox uses alpha too for RGBA
    if bbox:
        im = im.crop(bbox)
    im.save(dst)
    a0 = sum(1 for p in im.getdata() if p[3]==0)
    total = im.size[0]*im.size[1]
    print(f"{dst} -> {im.size[0]}x{im.size[1]} alpha0%={100*a0//total}")

process(sys.argv[1], sys.argv[2])
