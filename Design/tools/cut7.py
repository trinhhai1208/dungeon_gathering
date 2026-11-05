# Uniform rarity tiles: coloured square (no letter) + centred icon; letters exported separately.
import sys, os
from PIL import Image
SRC, OUT = sys.argv[1], sys.argv[2]
B = Image.open(os.path.join(SRC, 'UI/HUD/Basic Buttons 3 Fv.png')).convert('RGBA')
IT = Image.open(os.path.join(SRC, 'Items/spritesheet_16x16.png')).convert('RGBA')
SW = Image.open(os.path.join(SRC, 'Items/Weapon/Sword/File.png')).convert('RGBA')
def trim(im): return im.crop(im.getbbox())
def up(im, k): return im.resize((im.width * k, im.height * k), Image.NEAREST)
def item(c, r): return trim(IT.crop((c * 16, r * 16, c * 16 + 16, r * 16 + 16)))
def sword(c, r): return trim(SW.crop((c * 32, r * 32, c * 32 + 32, r * 32 + 32)))
ROW = {'S': 94, 'A': 126, 'B': 158, 'C': 190, 'F': 222}
def square(k): return up(trim(B.crop((474, ROW[k], 504, ROW[k] + 30))), 2)  # 48x50
def tile(k, icon, dim=False):
    sq = square(k)
    if dim:
        px = sq.load()
        for y in range(sq.height):
            for x in range(sq.width):
                r, g, b, a = px[x, y]
                if a: px[x, y] = (int(r * .55), int(g * .55), int(b * .6), a)
    sq.alpha_composite(icon, ((sq.width - icon.width) // 2, (sq.height - 2 - icon.height) // 2))
    return up(sq, 2)
def ghost(im):
    im = im.copy(); px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            if px[x, y][3]: px[x, y] = (24, 26, 44, 150)
    return im
EQ = {'horn': ('C', (12, 11)), 'fire': ('A', (4, 0)), 'night': ('A', (11, 10)), 'wind': ('B', (1, 4)), 'book': ('B', (10, 2)),
      'shield': ('C', (5, 18)), 'silver': ('C', (1, 0)), 'hat': ('B', (2, 11)), 'mask': ('A', (8, 11)), 'gwhite': ('C', (12, 10)),
      'neck': ('B', (3, 13)), 'redb': ('F', (3, 4)), 'ring': ('F', (0, 16))}
for n, (k, cr) in EQ.items(): tile(k, up(item(*cr), 2)).save(os.path.join(OUT, f't_eq_{n}.png'))
SWD = {'fire': ('S', (0, 0)), 'thunder': ('S', (0, 2)), 'void': ('A', (3, 2)), 'gold': ('A', (4, 1)), 'lava': ('A', (4, 2)),
       'ice': ('B', (1, 0)), 'thorn': ('B', (2, 0)), 'holy': ('B', (3, 4)), 'dark': ('C', (0, 3)), 'venom': ('C', (2, 3)),
       'iron': ('F', (0, 4)), 'old': ('F', (5, 2))}
for n, (k, cr) in SWD.items(): tile(k, sword(*cr)).save(os.path.join(OUT, f't_sw_{n}.png'))
EMPTY = {'helmet': (12, 11), 'armor': (1, 0), 'gloves': (12, 10), 'boots': (0, 4), 'neck': (3, 13), 'acc': (0, 16)}
for n, cr in EMPTY.items(): tile('F', ghost(up(item(*cr), 2)), dim=True).save(os.path.join(OUT, f't_empty_{n}.png'))
tile('F', Image.new('RGBA', (1, 1)), dim=True).save(os.path.join(OUT, 't_empty_weapon.png'))
for i, k in enumerate('SABCF'):
    up(B.crop((496 + 16 * i, 54, 503 + 16 * i, 62)), 4).save(os.path.join(OUT, f'let_{k}.png'))
prev = Image.new('RGBA', (96 * 6 + 70, 110), (40, 34, 52, 255))
for i, n in enumerate(['t_eq_horn', 't_eq_fire', 't_sw_fire', 't_sw_iron', 't_empty_neck', 't_empty_weapon']):
    prev.alpha_composite(Image.open(os.path.join(OUT, n + '.png')), (8 + i * 106, 6))
    if i < 4: prev.alpha_composite(Image.open(os.path.join(OUT, 'let_' + 'CASF'[i] + '.png')), (2 + i * 106, 0))
prev.save(os.path.join(OUT, '..', '_prev7.png'))
print(len(os.listdir(OUT)))
