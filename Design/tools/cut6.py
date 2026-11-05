import sys, os, math
from PIL import Image
SRC, OUT = sys.argv[1], sys.argv[2]
def up(im, k): return im.resize((im.width * k, im.height * k), Image.NEAREST)
sel = Image.open(os.path.join(SRC, 'UI/Indicators/square_right_4.png')).convert('RGBA')
up(sel, 4).save(os.path.join(OUT, 'sel_frame.png'))
D, F0, F1, F2, F3, HL = (26,25,50,255), (66,76,110,255), (84,96,128,255), (113,127,156,255), (139,155,180,255), (192,203,220,255)
def disc(n, r_out, layers):
    im = Image.new('RGBA', (n, n), (0,0,0,0)); px = im.load(); c = (n - 1) / 2
    for y in range(n):
        for x in range(n):
            d = math.hypot(x - c, y - c)
            for r, col in layers:
                if d <= r: px[x, y] = col
    return im
# base: dark outline, mid ring, inner well
base = disc(40, 19.5, [(19.6, D), (18.4, F2), (17.2, F1), (13.5, D), (12.5, F0)])
px = base.load()
for y in range(40):  # top highlight on outer ring
    for x in range(40):
        d = math.hypot(x - 19.5, y - 19.5)
        if 17.2 < d <= 18.4 and y < 14: px[x, y] = F3
for (dx, dy) in [(0,-1),(0,1),(-1,0),(1,0)]:  # direction pips on ring
    cx, cy = 19.5 + dx * 15.5, 19.5 + dy * 15.5
    for i in range(-1, 2):
        for j in range(0, 2):
            x = int(cx + (i if dy else -dx * j)); y = int(cy + (i if dx else -dy * j))
            px[x, y] = HL
knob = disc(20, 9.5, [(9.6, D), (8.5, F2), (7.3, F3)])
kp = knob.load()
for y in range(20):
    for x in range(20):
        d = math.hypot(x - 9.5, y - 9.5)
        if d <= 7.3 and (x - 7) ** 2 + (y - 6) ** 2 < 9: kp[x, y] = HL
        if 7.3 < d <= 8.5 and y > 12: kp[x, y] = F1
up(base, 4).save(os.path.join(OUT, 'joy_base.png')); up(knob, 4).save(os.path.join(OUT, 'joy_knob.png'))
prev = Image.new('RGBA', (80, 40), (60,50,70,255)); prev.alpha_composite(base, (0, 0)); prev.alpha_composite(knob, (10 + 3, 10 - 4)); prev.alpha_composite(sel, (50, 12))
up(prev, 6).save(os.path.join(OUT, '_prev.png'))
