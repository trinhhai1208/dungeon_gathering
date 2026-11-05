import sys, os, colorsys
from PIL import Image
SRC = sys.argv[1]; OUT = sys.argv[2]
os.makedirs(OUT, exist_ok=True)
B = Image.open(os.path.join(SRC, 'UI/HUD/Basic Buttons 3 Fv.png')).convert('RGBA')
IT = Image.open(os.path.join(SRC, 'Items/spritesheet_16x16.png')).convert('RGBA')
V0 = Image.open(os.path.join(SRC, 'VFX/00.png')).convert('RGBA')
V1 = Image.open(os.path.join(SRC, 'VFX/01.png')).convert('RGBA')
DC = Image.open(os.path.join(SRC, 'Characters/Dungeon_Character.png')).convert('RGBA')

def trim(im): return im.crop(im.getbbox())
def save(n, im, s): im.resize((im.width * s, im.height * s), Image.NEAREST).save(os.path.join(OUT, n + '.png'))
def item(c, r): return IT.crop((c * 16, r * 16, c * 16 + 16, r * 16 + 16))
def hue(im, h_to, sat_min=0.3):
    im = im.copy(); px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if not a: continue
            h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
            if s > sat_min:
                nr, ng, nb = colorsys.hsv_to_rgb(h_to, s, v); px[x, y] = (int(nr * 255), int(ng * 255), int(nb * 255), a)
    return im

# empty colored slots (no letter, 3rd column) for 9-slice bars + frames
EMPTY = {'S': (474, 94), 'A': (474, 126), 'B': (474, 158), 'C': (474, 190), 'F': (474, 222)}
def empty(k): x, y = EMPTY[k]; return trim(B.crop((x, y, x + 30, y + 30)))
save('bar_red', hue(empty('S'), 0.99), 4)
save('bar_blue', empty('B'), 4)
save('bar_grey', empty('F'), 4)
save('bar_green', empty('C'), 4)
save('bar_gold', hue(empty('S'), 0.12), 4)

# rarity slot with letter + item
LET = {'S': (424, 94), 'A': (424, 126), 'B': (424, 158), 'C': (424, 190), 'F': (424, 222)}
def eslot(name, k, icon, isz=2):
    x, y = LET[k]; slot = trim(B.crop((x, y, x + 34, y + 34)))
    slot = slot.resize((slot.width * 2, slot.height * 2), Image.NEAREST)
    ic = trim(icon); ic = ic.resize((ic.width * isz, ic.height * isz), Image.NEAREST)
    can = slot.copy(); can.alpha_composite(ic, ((can.width - ic.width) // 2 - 2, (can.height - ic.height) // 2 - 2))
    save(name, can, 2)

EQ = {
    'eq_armor_fire': ('A', (4, 0)), 'eq_armor_white': ('C', (1, 0)), 'eq_book_rune': ('B', (10, 2)),
    'eq_gloves_night': ('A', (11, 10)), 'eq_boots_wind': ('B', (1, 4)), 'eq_helm_horn': ('C', (12, 11)),
    'eq_ring': ('F', (0, 16)), 'eq_necklace': ('B', (3, 13)), 'eq_shield': ('C', (5, 18)), 'eq_hat_blue': ('B', (2, 11)),
    'eq_boots_red': ('F', (3, 4)), 'eq_mask_blue': ('A', (8, 11)),
    'wp_bow': ('C', (5, 4)), 'wp_staff': ('B', (2, 20)), 'wp_dagger': ('F', (0, 21)),
}
for n, (k, (c, r)) in EQ.items():
    eslot(n, k, item(c, r)); save('ic_' + n[3:], trim(item(c, r)), 4)
for n, (c, r) in {'ic_potion_red': (12, 14), 'ic_potion_blue': (13, 14), 'ic_gem': (0, 8)}.items():
    save(n, trim(item(c, r)), 4)

# role badges (raw icons)
for n, (c, r) in {'role_warrior': (5, 18), 'role_ranger': (5, 4), 'role_mage': (10, 2), 'role_assassin': (0, 21)}.items():
    save(n, trim(item(c, r)), 3)

# empty equipment slot frame with faint silhouette
for n, (c, r) in {'slot_helmet': (12, 11), 'slot_armor': (1, 0), 'slot_gloves': (12, 10), 'slot_boots': (0, 4), 'slot_acc': (0, 16)}.items():
    slot = trim(B.crop((474, 222, 504, 252))); slot = slot.resize((slot.width * 2, slot.height * 2), Image.NEAREST)
    ic = trim(item(c, r)); ic = ic.resize((ic.width * 2, ic.height * 2), Image.NEAREST)
    px = ic.load()
    for y in range(ic.height):
        for x in range(ic.width):
            if px[x, y][3]: px[x, y] = (40, 46, 64, 110)
    can = slot.copy(); can.alpha_composite(ic, ((can.width - ic.width) // 2 - 2, (can.height - ic.height) // 2 - 2)); save(n, can, 2)

# rogue character
im = DC.crop((32, 16, 48, 32)); px = im.load()
for (cx, cy) in [(0, 0), (13, 0), (0, 13), (13, 13)]:
    for y in range(cy, cy + 3):
        for x in range(cx, cx + 3):
            r, g, b, a = px[x, y]
            if a and max(r, g, b) - min(r, g, b) > 60: px[x, y] = (0, 0, 0, 0)
save('ch_rogue', trim(im), 4)

# VFX frames 64x64
def vf(sheet, c, r): return sheet.crop((c * 64, r * 64, c * 64 + 64, r * 64 + 64))
save('vfx_slash', vf(V1, 1, 3), 2)
save('vfx_firering', vf(V1, 2, 5), 2)
save('vfx_arcane', vf(V1, 4, 6), 2)
save('vfx_wind', vf(V0, 6, 2), 2)
print('ok', len(os.listdir(OUT)))
