# Pixel flags for the language picker: 18x12 + 1px dark outline, exported x4.
import os, sys
from PIL import Image
OUT = sys.argv[1]
D = (26, 25, 50, 255)
def framed(inner):
    im = Image.new('RGBA', (inner.width + 2, inner.height + 2), D); im.paste(inner, (1, 1)); return im
# Vietnam: red field, 5-point yellow star
vn = Image.new('RGBA', (18, 12), (218, 37, 29, 255)); p = vn.load(); Y = (255, 205, 0, 255)
star = ["....#....", "....#....", "...###...", "#########", ".#######.", "..#####..", "..##.##..", ".##...##.", ".#.....#."]
for y, row in enumerate(star):
    for x, c in enumerate(row):
        if c == '#': p[4 + x, 1 + y] = Y
# USA: 6 red/white stripes (2px), blue canton with white dots
us = Image.new('RGBA', (18, 12)); p = us.load()
R, W, B = (191, 30, 46, 255), (240, 240, 240, 255), (40, 52, 120, 255)
for y in range(12):
    for x in range(18): p[x, y] = R if (y // 2) % 2 == 0 else W
for y in range(6):
    for x in range(8): p[x, y] = B
for y in range(1, 6, 2):
    for x in range(1, 8, 2): p[x, y] = W
for n, im in (('flag_vn', vn), ('flag_us', us)):
    f = framed(im); f.resize((f.width * 4, f.height * 4), Image.NEAREST).save(os.path.join(OUT, n + '.png'))
