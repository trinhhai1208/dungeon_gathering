import sys, os
from PIL import Image
SRC = sys.argv[1]; OUT = sys.argv[2]
os.makedirs(OUT, exist_ok=True)
B = Image.open(os.path.join(SRC, 'UI/HUD/Basic Buttons 3 Fv.png')).convert('RGBA')
SW = Image.open(os.path.join(SRC, 'Items/Weapon/Sword/File.png')).convert('RGBA')
IND = os.path.join(SRC, 'UI/Indicators')

def save(name, im, scale):
    im.resize((im.width * scale, im.height * scale), Image.NEAREST).save(os.path.join(OUT, name + '.png'))

def trim(im):
    return im.crop(im.getbbox())

# 9-slice frames, exported x4 (slice 6px source -> 24px)
for n, box in {'fr_dark': (235, 62, 261, 86), 'fr_mid': (235, 11, 261, 35), 'fr_light': (283, 9, 309, 35), 'fr_btn': (283, 60, 309, 86),
               'fr_slot': (474, 222, 504, 252)}.items():
    im = B.crop(box)
    if n == 'fr_slot': im = trim(im)
    save(n, im, 4)

# connected components helper
def comps(region):
    x0, y0, x1, y1 = region
    sub = B.crop(region); px = sub.load(); w, h = sub.size
    seen = set(); out = []
    for y in range(h):
        for x in range(w):
            if px[x, y][3] and (x, y) not in seen:
                st = [(x, y)]; seen.add((x, y)); xs = []; ys = []
                while st:
                    a, b = st.pop(); xs.append(a); ys.append(b)
                    for c, d in ((a+1, b), (a-1, b), (a, b+1), (a, b-1)):
                        if 0 <= c < w and 0 <= d < h and (c, d) not in seen and px[c, d][3]:
                            seen.add((c, d)); st.append((c, d))
                out.append((min(xs)+x0, min(ys)+y0, max(xs)+x0+1, max(ys)+y0+1))
    return sorted(out, key=lambda r: (r[1] // 8, r[0]))

sq = [r for r in comps((44, 4, 136, 30)) if (r[2]-r[0]) > 6]
print('squares', sq)
for i, r in enumerate(sq): save(f'sq_{i}', B.crop(r), 4)
ck = [r for r in comps((44, 138, 100, 176)) if (r[2]-r[0]) > 6]
print('checks', ck)
for i, r in enumerate(ck): save(f'ck_{i}', B.crop(r), 4)

# rarity slots with letter but no icon (2nd column), sword pasted in
SLOTS = {'S': (424, 94, 34, 34), 'A': (424, 126, 34, 34), 'B': (424, 158, 34, 34), 'C': (424, 190, 34, 34), 'F': (424, 222, 34, 34)}
PICK = {'F': (0, 4), 'C': (0, 3), 'B': (1, 0), 'A': (3, 2), 'S': (0, 0)}
for k, (x, y, w, h) in SLOTS.items():
    slot = trim(B.crop((x, y, x + w, y + h)))
    slot = slot.resize((slot.width * 2, slot.height * 2), Image.NEAREST)
    c, r = PICK[k]
    sw = trim(SW.crop((c * 32, r * 32, c * 32 + 32, r * 32 + 32)))
    can = slot.copy()
    can.alpha_composite(sw, ((can.width - sw.width) // 2 - 2, (can.height - sw.height) // 2 - 2))
    save(f'wslot_{k}', can, 2)
    save(f'sword_{k}', sw, 3)
# all swords preview
for r in range(5):
    for c in range(6):
        pass

# indicators & d-pad
for n in ['square_up_down_1', 'square_left_1', 'square_right_1']:
    save('ind_' + n, Image.open(os.path.join(IND, n + '.png')).convert('RGBA'), 4)
for n, box in {'btn_up': (242, 176, 28, 30), 'btn_down': (338, 176, 28, 30)}.items():
    save(n, trim(B.crop((box[0], box[1], box[0] + box[2], box[1] + box[3]))), 3)
# armor heart: desaturated silver
H = Image.open(os.path.join(SRC, 'UI/HUD/Hearts Blue.png')).convert('RGBA')
for n, box in {'heart_armor': (32, 0, 48, 16), 'heart_armor_empty': (32, 64, 48, 80)}.items():
    im = H.crop(box); px = im.load()
    for yy in range(im.height):
        for xx in range(im.width):
            rr, gg, bb, a = px[xx, yy]
            if a:
                l = int(0.3 * rr + 0.55 * gg + 0.15 * bb)
                px[xx, yy] = (min(255, l + 25), min(255, l + 30), min(255, l + 45), a)
    save(n, trim(im), 4)
print('done', len(os.listdir(OUT)))
