# Title/loading backdrop: dungeon entrance at night, built only from the project's tileset + props.
# Native 320x180 (16px tiles), exported x4 = 1280x720.
import sys, os, random
from PIL import Image
SRC, OUT = sys.argv[1], sys.argv[2]
code = open(os.path.join(os.path.dirname(__file__), 'maps.py'), encoding='utf-8').read().split('META = {}')[0]
sys.argv = ['x', SRC, OUT]
exec(code)

W = int(os.environ.get('BG_W', '320')); H = 180
CX = W // 2
SCALE = int(os.environ.get('BG_SCALE', '4'))
rng = random.Random(7)
img = Image.new('RGBA', (W, H), (27, 20, 32, 255))

# floor: whole frame, dungeon floor variants
for ty in range(0, H, 16):
    for tx in range(0, W, 16):
        f = FLOORS[0] if rng.random() < 0.5 else rng.choice(FLOORS)
        img.alpha_composite(tile(*f), (tx, ty))

# back wall band (rows 1-2 of tiles) with ceiling darkness above
WALL_Y = 28
for tx in range(0, W, 16):
    img.alpha_composite(tile(1 + (tx // 16) % 4, 0), (tx, WALL_Y))
    img.alpha_composite(tile(1 + (tx // 16) % 4, 5), (tx, WALL_Y + 16))
px = img.load()
for y in range(WALL_Y + 2):
    for x in range(W):
        px[x, y] = (18, 13, 22, 255)
for x in range(W):  # contact shadow under the wall
    for dy, a in ((0, 0.55), (1, 0.7), (2, 0.85)):
        r, g, b, _ = px[x, WALL_Y + 32 + dy]; px[x, WALL_Y + 32 + dy] = (int(r * a), int(g * a), int(b * a), 255)

# central gate (closed arched doors) set into the wall
img.alpha_composite(tile(6, 6), (W // 2 - 16, WALL_Y + 16))
img.alpha_composite(tile(7, 6), (W // 2, WALL_Y + 16))

def put(name, x, y):
    im = SP[name]; img.alpha_composite(im, (int(x - im.width / 2), int(y - im.height)))

TORCHES = [CX - 104, CX - 48, CX + 48, CX + 104]
for x in TORCHES: put('storch', x, WALL_Y + 20)
for x in (CX - 76, CX + 76): put('flag', x, WALL_Y + 26)
put('torch', CX - 126, 150); put('torch', CX + 126, 150)
put('bones', CX - 90, 164); put('skullbone', CX + 94, 160); put('rubble', CX - 140, 112); put('rubble2', CX + 142, 118)
put('candle', CX - 32, 70); put('candle', CX + 32, 70)
put('vase', CX + 140, 84); put('vase', CX - 138, 86)

# pixel lighting: 2-step dithered warm pools around light sources
LIGHTS = [(x, WALL_Y + 12, 34) for x in TORCHES] + [(CX - 126, 140, 28), (CX + 126, 140, 28), (CX, 62, 40)]
px = img.load()
for y in range(H):
    for x in range(W):
        r, g, b, a = px[x, y]
        best = 0
        if y < WALL_Y - 2:
            continue
        for lx, ly, rad in LIGHTS:
            d = ((x - lx) ** 2 + ((y - ly) * 1.3) ** 2) ** 0.5 / rad
            best = max(best, 2 if d < 0.55 else (1 if d < 1.0 and (x + y) % 2 == 0 else 0))
        if best:
            k = 0.10 * best
            px[x, y] = (min(255, int(r + (255 - r) * k * 0.9)), min(255, int(g + (200 - g) * k * 0.6)), min(255, int(b + (120 - b) * k * 0.3)), a)

# stepped vignette with ordered dither (keeps it pixel, no smooth gradient)
BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]
for y in range(H):
    for x in range(W):
        dx = (x - W / 2) / (W / 2); dy = (y - H * 0.45) / (H * 0.62)
        v = (dx * dx + dy * dy) ** 0.5
        t = max(0.0, min(1.0, (v - 0.55) / 0.55))
        level = int(t * 4 + BAYER[y % 4][x % 4] / 16)
        if level:
            r, g, b, a = px[x, y]; m = (1.0, 0.78, 0.6, 0.42, 0.3)[min(level, 4)]
            px[x, y] = (int(r * m + 27 * (1 - m) * 0.3), int(g * m + 20 * (1 - m) * 0.3), int(b * m + 32 * (1 - m) * 0.3), a)

# embers drifting up from braziers / torches
for (ex, ey) in [(CX - 126, 132), (CX + 126, 132)] + [(x, WALL_Y + 4) for x in TORCHES]:
    for i in range(5):
        x = ex + rng.randint(-6, 6); y = ey - rng.randint(4, 26)
        if 0 <= x < W and 0 <= y < H: px[x, y] = (255, 170 + rng.randint(0, 60), 60, 255)

img.resize((W * SCALE, H * SCALE), Image.NEAREST).save(os.path.join(OUT, os.environ.get('BG_NAME', 'bg_title') + '.png'))
print('ok')
