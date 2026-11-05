import sys, os
from PIL import Image
SRC = sys.argv[1]; OUT = sys.argv[2]
os.makedirs(OUT, exist_ok=True)
def L(p): return Image.open(os.path.join(SRC, p)).convert('RGBA')
B = L('UI/HUD/Basic Buttons 3 Fv.png'); IT = L('Items/spritesheet_16x16.png')

def clean(im):
    im = im.copy(); px = im.load(); w, h = im.size
    for (cx, cy) in [(0, 0), (w-3, 0), (0, h-3), (w-3, h-3)]:
        for y in range(cy, cy+3):
            for x in range(cx, cx+3):
                r, g, b, a = px[x, y]
                if a and max(r, g, b) - min(r, g, b) > 60: px[x, y] = (0, 0, 0, 0)
    return im

def anim(name, frames, scale, ms):
    # common bbox
    bb = None
    for f in frames:
        b = f.getbbox()
        if b: bb = b if bb is None else (min(bb[0], b[0]), min(bb[1], b[1]), max(bb[2], b[2]), max(bb[3], b[3]))
    fr = [f.crop(bb).resize(((bb[2]-bb[0]) * scale, (bb[3]-bb[1]) * scale), Image.NEAREST) for f in frames]
    fr[0].save(os.path.join(OUT, name + '.webp'), save_all=True, append_images=fr[1:], duration=ms, loop=0, lossless=True)
    print(name, fr[0].size)

def bob(img):
    a = Image.new('RGBA', (img.width, img.height + 1)); a.alpha_composite(img, (0, 1))
    b = Image.new('RGBA', (img.width, img.height + 1)); b.alpha_composite(img, (0, 0))
    return [a, a, b, b]

K = L('Characters/Knight/Blue Knight idle Sprite-sheet 16x16.png')
anim('idle_knight', [K.crop((i * 16, 0, i * 16 + 16, 16)) for i in range(8)], 6, 120)
anim('idle_wizard', [clean(L(f'Characters/Priests/priest2/v1/priest2_v1_{i}.png')) for i in range(1, 5)], 6, 180)
DC = L('Characters/Dungeon_Character.png')
anim('idle_ranger', bob(clean(DC.crop((48, 0, 64, 16)))), 6, 220)
anim('idle_rogue', bob(clean(DC.crop((32, 16, 48, 32)))), 6, 220)
P = L('Characters/FantasyKnights/01Knight.png')
anim('idle_paladin', [P.crop((i * 48, 8, i * 48 + 48, 56)) for i in range(8)], 3, 120)

def save(n, im, s): im.resize((im.width * s, im.height * s), Image.NEAREST).save(os.path.join(OUT, n + '.png'))
save('ic_swap', B.crop((513, 227, 519, 233)), 8)
def trim(im): return im.crop(im.getbbox())
def item(c, r): return IT.crop((c * 16, r * 16, c * 16 + 16, r * 16 + 16))
for n, (c, r) in {'tab_helmet': (12, 11), 'tab_armor': (1, 0), 'tab_gloves': (12, 10), 'tab_boots': (0, 4), 'tab_neck': (3, 13), 'tab_acc': (0, 16)}.items():
    save(n, trim(item(c, r)), 3)
# necklace empty slot silhouette + white gloves eslot
slot = trim(B.crop((474, 222, 504, 252))); slot = slot.resize((slot.width * 2, slot.height * 2), Image.NEAREST)
ic = trim(item(3, 13)); ic = ic.resize((ic.width * 2, ic.height * 2), Image.NEAREST); px = ic.load()
for y in range(ic.height):
    for x in range(ic.width):
        if px[x, y][3]: px[x, y] = (40, 46, 64, 110)
can = slot.copy(); can.alpha_composite(ic, ((can.width - ic.width) // 2 - 2, (can.height - ic.height) // 2 - 2)); save('slot_neck', can, 2)
x, y = 424, 190; s2 = trim(B.crop((x, y, x + 34, y + 34))); s2 = s2.resize((s2.width * 2, s2.height * 2), Image.NEAREST)
ic = trim(item(12, 10)); ic = ic.resize((ic.width * 2, ic.height * 2), Image.NEAREST)
c2 = s2.copy(); c2.alpha_composite(ic, ((c2.width - ic.width) // 2 - 2, (c2.height - ic.height) // 2 - 2)); save('eq_gloves_white', c2, 2)
print('ok')
