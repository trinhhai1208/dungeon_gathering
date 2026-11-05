import sys, os, json, random
from PIL import Image, ImageDraw
SRC = sys.argv[1]; OUT = sys.argv[2]
os.makedirs(OUT, exist_ok=True)
T = 16
TS = Image.open(os.path.join(SRC, 'Environment/Tilesets/Dungeon_Tileset.png')).convert('RGBA')

def tile(cx, cy):
    return TS.crop((cx * T, cy * T, cx * T + T, cy * T + T))

def spr(p, box=None):
    im = Image.open(os.path.join(SRC, p)).convert('RGBA')
    if box: im = im.crop(box)
    if im.size == (16, 16):
        px = im.load()
        for (cx, cy) in [(0, 0), (13, 0), (0, 13), (13, 13)]:
            for y in range(cy, cy + 3):
                for x in range(cx, cx + 3):
                    r, g, b, a = px[x, y]
                    if a and max(r, g, b) - min(r, g, b) > 60: px[x, y] = (0, 0, 0, 0)
    return im

FLOORS = [(6, 0), (7, 0), (8, 0), (9, 0), (6, 1), (7, 1), (8, 1), (9, 1), (6, 2), (7, 2), (8, 2), (9, 2)]
VOID = tile(8, 7)
SP = {
    'knight': spr('Characters/Knight/Blue Knight idle Sprite-sheet 16x16.png', (0, 0, 16, 16)),
    'ranger': spr('Characters/Dungeon_Character.png', (48, 0, 64, 16)),
    'wizard': spr('Characters/Priests/priest2/v1/priest2_v1_1.png'),
    'paladin': spr('Characters/FantasyKnights/01Knight.png', (0, 8, 48, 56)).crop((12, 14, 36, 44)),
    'npc1': spr('Characters/Dungeon_Character.png', (16, 0, 32, 16)),
    'npc2': spr('Characters/Dungeon_Character.png', (32, 48, 48, 64)),
    'skel': spr('Enemies/DungeonMonsters/skeleton1/v1/skeleton_v1_1.png'),
    'skel2': spr('Enemies/DungeonMonsters/skeleton2/v1/skeleton2_v1_1.png'),
    'skull': spr('Enemies/DungeonMonsters/skull/v1/skull_v1_1.png'),
    'vamp': spr('Enemies/DungeonMonsters/vampire/v1/vampire_v1_1.png'),
    'zombie': spr('Enemies/Zombie/1Zombie-Idle.png', (0, 0, 32, 32)),
    'spike_on': spr('Traps/SpikeTrap/peaks_4.png'), 'spike_off': spr('Traps/SpikeTrap/peaks_1.png'),
    'arrowtrap': spr('Traps/ArrowTrap/arrow_1.png'), 'button': spr('Traps/Button.png', (0, 0, 16, 16)),
    'torch': spr('Environment/Props/torch/torch_1.png'), 'storch': spr('Environment/Props/torch/side_torch_1.png'),
    'candle': spr('Environment/Props/torch/candlestick_1_1.png'), 'flag': spr('Environment/Props/flag/flag_1.png'),
    'box': spr('Environment/Props/box_1/box_1_1.png'), 'minibox': spr('Environment/Props/mini_box_1/mini_box_1_1.png'),
    'vase': spr('Environment/Props/Vase Shine Anim.png', (0, 0, 16, 16)),
    'chest': spr('Items/Chest/chest_1.png'), 'coin': spr('Items/Coins/coin_1.png'),
    'flask_r': spr('Items/Potions/flasks_4_1.png'), 'flask_b': spr('Items/Potions/flasks_3_1.png'),
    'key': spr('Items/Keys/keys_1_1.png'), 'minichest': spr('Items/MiniChest/mini_chest_1.png'),
}
for k, c in {'crate': (8, 3), 'door_l': (6, 3), 'door_r': (7, 3), 'gate_l': (6, 6), 'gate_r': (7, 6), 'web_l': (4, 6), 'web_r': (5, 6),
             'bones': (8, 6), 'skullbone': (7, 7), 'banner': (4, 7), 'rubble': (9, 4), 'rubble2': (9, 5), 'brazier': (2, 9),
             'stand': (4, 9), 'chalice': (5, 7), 'ladder': (9, 3)}.items():
    SP[k] = tile(*c)

class Map:
    def __init__(s, w, h, seed=1):
        s.w, s.h = w, h
        s.base = Image.new('RGBA', (w * T, h * T), VOID.getpixel((8, 8)))
        s.top = Image.new('RGBA', (w * T, h * T), (0, 0, 0, 0))
        s.rng = random.Random(seed)
        s.labels = []
    def put(s, t, x, y, layer='base'):
        L = s.base if layer == 'base' else s.top
        L.alpha_composite(t, (int(x * T), int(y * T)))
    def obj(s, name, x, y, dx=0, dy=0):
        im = SP[name]
        px = int(x * T + (T - im.width) / 2 + dx); py = int(y * T + T - im.height + dy)
        s.top.alpha_composite(im, (px, py))
    def floor(s, x, y):
        f = FLOORS[0] if s.rng.random() < 0.55 else s.rng.choice(FLOORS)
        s.put(tile(*f), x, y)
    def room(s, ox, oy, w, h, doors=(), closed=()):
        for y in range(oy, oy + h):
            for x in range(ox, ox + w):
                top, bot, lef, rig = y == oy, y == oy + h - 1, x == ox, x == ox + w - 1
                if top and lef: t = (0, 0)
                elif top and rig: t = (5, 0)
                elif bot and lef: t = (0, 4)
                elif bot and rig: t = (5, 4)
                elif top: t = (1 + (x - ox) % 4, 0)
                elif bot: t = (1 + (x - ox) % 4, 4)
                elif lef: t = (0, 1 + (y - oy) % 3)
                elif rig: t = (5, 1 + (y - oy) % 3)
                else:
                    s.floor(x, y); continue
                s.put(tile(*t), x, y)
        cx, cy = ox + w // 2, oy + h // 2
        for d in doors:
            if d == 'N':
                for x in (cx - 1, cx): s.floor(x, oy)
                if 'N' in closed: s.put(SP['door_l'], cx - 1, oy); s.put(SP['door_r'], cx, oy)
            if d == 'S':
                for x in (cx - 1, cx): s.floor(x, oy + h - 1)
            if d == 'W':
                for y in (cy - 1, cy): s.floor(ox, y)
            if d == 'E':
                for y in (cy - 1, cy): s.floor(ox + w - 1, y)
    def hcorr(s, x0, x1, cy):
        for x in range(x0, x1 + 1):
            s.put(tile(1 + x % 4, 0), x, cy - 2)
            s.floor(x, cy - 1); s.floor(x, cy)
            s.put(tile(1 + x % 4, 4), x, cy + 1)
    def vcorr(s, cx, y0, y1):
        for y in range(y0, y1 + 1):
            s.put(tile(0, 1 + y % 3), cx - 2, y)
            s.floor(cx - 1, y); s.floor(cx, y)
            s.put(tile(5, 1 + y % 3), cx + 1, y)
    def block(s, x, y, w):
        for i in range(w):
            t = (0, 5) if i == 0 else (5, 5) if i == w - 1 else (1 + i % 4, 5)
            s.put(tile(*t), x + i, y)
    def ring(s, x, y, color, rx=7, ry=4):
        d = ImageDraw.Draw(s.top)
        cx, cy = x * T + 8, y * T + 11
        d.ellipse((cx - rx, cy - ry, cx + rx, cy + ry), outline=color)
        d.ellipse((cx - rx + 2, cy - ry + 1, cx + rx - 2, cy + ry - 1), outline=color[:3] + (110,))
    def portal(s, x, y):
        d = ImageDraw.Draw(s.top)
        cx, cy = x * T, y * T
        for i, c in enumerate([(30, 60, 140, 255), (40, 120, 220, 255), (90, 190, 255, 255), (190, 240, 255, 255)]):
            d.ellipse((cx - 22 + i * 4, cy - 12 + i * 3, cx + 22 - i * 4, cy + 12 - i * 3), outline=c, width=2)
        d.ellipse((cx - 6, cy - 3, cx + 6, cy + 3), fill=(210, 245, 255, 255))
    def label(s, text, x, y, kind='info'):
        s.labels.append({'t': text, 'x': x, 'y': y, 'k': kind})
    def save(s, name, scale):
        im = s.base.copy(); im.alpha_composite(s.top)
        im = im.resize((im.width * scale, im.height * scale), Image.NEAREST)
        im.save(os.path.join(OUT, name + '.png'))
        meta = {'w': im.width, 'h': im.height, 'tile': T * scale, 'labels': s.labels}
        return meta

META = {}
RED = (230, 60, 60, 255); GOLD = (250, 200, 80, 255)

# ---------- Detailed room templates ----------
# Start room
m = Map(13, 11, 2); m.room(0, 0, 13, 11, doors='E')
m.obj('flag', 3, 0, dy=8); m.obj('flag', 9, 0, dy=8); m.obj('storch', 6, 0, dy=6)
m.obj('knight', 6, 5); m.obj('vase', 1, 1); m.obj('vase', 11, 1); m.obj('box', 1, 9); m.obj('minibox', 2, 9)
m.obj('bones', 10, 8)
m.label('Player spawn', 6, 5, 'player'); m.label('Door E → hành lang', 12, 5, 'door')
META['room_start'] = m.save('room_start', 2)

# Combat room
m = Map(19, 15, 3); m.room(0, 0, 19, 15, doors='NWES', closed='N')
m.block(4, 4, 4); m.block(11, 4, 4); m.block(4, 10, 4); m.block(11, 10, 4)
for (x, y) in [(8, 7), (9, 7), (10, 7)]: m.obj('spike_on' if x == 9 else 'spike_off', x, y)
for (x, y) in [(3, 2), (15, 2), (3, 12), (15, 12), (9, 3)]: m.ring(x, y, RED)
m.obj('skel', 15, 2); m.obj('skel2', 3, 12); m.obj('skull', 9, 3, dy=-4); m.obj('zombie', 15, 12)
m.obj('knight', 2, 7); m.obj('crate', 1, 1); m.obj('rubble', 17, 8); m.obj('web_l', 1, 13)
m.obj('storch', 5, 0, dy=6); m.obj('storch', 13, 0, dy=6)
m.label('Cửa khóa khi vào phòng', 9, 0, 'door'); m.label('SpawnWarning 1.0s', 3, 2, 'enemy')
m.label('Spike TimedTrap', 9, 7, 'trap'); m.label('Tường nội bộ → A* đi vòng', 5, 4, 'info'); m.label('Skull bay (isFlying) bỏ qua tường', 9, 3, 'enemy')
META['room_combat'] = m.save('room_combat', 2)

# Treasure room
m = Map(11, 11, 4); m.room(0, 0, 11, 11, doors='S')
m.obj('chest', 5, 5); m.obj('candle', 3, 4); m.obj('candle', 7, 4); m.obj('candle', 3, 6); m.obj('candle', 7, 6)
m.obj('banner', 5, 0, dy=0); m.obj('coin', 2, 8); m.obj('coin', 8, 8); m.obj('chalice', 1, 1); m.obj('vase', 9, 1)
m.label('Chest: vũ khí / vàng / bình máu', 5, 5, 'loot')
META['room_treasure'] = m.save('room_treasure', 2)

# Shop room
m = Map(15, 11, 5); m.room(0, 0, 15, 11, doors='N')
for x in (3, 7, 11):
    m.obj('crate', x, 5)
m.obj('flask_r', 3, 5, dy=-10); m.obj('flask_b', 7, 5, dy=-10); m.obj('key', 11, 5, dy=-10)
m.obj('npc1', 7, 3); m.obj('storch', 3, 0, dy=6); m.obj('storch', 11, 0, dy=6); m.obj('box', 1, 9); m.obj('box', 13, 9); m.obj('vase', 13, 1)
m.label('ShopStand × 3', 7, 5, 'loot'); m.label('NPC thương nhân', 7, 3, 'npc')
META['room_shop'] = m.save('room_shop', 2)

# ---------- Boss arena ----------
m = Map(25, 19, 9); m.room(0, 0, 25, 19, doors='WN', closed='N')
for (x, y) in [(5, 5), (19, 5), (5, 13), (19, 13)]: m.block(x - 1, y, 3)
for x in range(9, 16):
    m.obj('spike_off' if x % 2 else 'spike_on', x, 15)
m.obj('arrowtrap', 1, 9); m.obj('arrowtrap', 23, 9)
vb = SP['vamp'].resize((48, 48), Image.NEAREST)
m.top.alpha_composite(vb, (12 * T - 16, 8 * T - 32))
m.ring(12, 9, RED, 22, 9)
m.obj('knight', 12, 15, dy=-18)
m.obj('skull', 8, 8); m.obj('skull', 16, 8)
for x in (3, 21): m.obj('storch', x, 0, dy=6)
m.obj('flag', 8, 0, dy=8); m.obj('flag', 16, 0, dy=8); m.obj('skullbone', 2, 16); m.obj('bones', 22, 2); m.obj('web_r', 23, 1)
m.portal(12 * T // T + 0.5, 4) if False else None
m.label('Vampire Lord', 12, 8, 'boss'); m.label('Phase 2 (60%): triệu hồi Skull', 16, 8, 'enemy')
m.label('Spike row – Phase 3 (30%)', 12, 15, 'trap'); m.label('ArrowTrap 2 bên', 1, 9, 'trap'); m.label('Portal xuất hiện sau khi hạ Boss', 12, 4, 'door')
META['room_boss'] = m.save('room_boss', 3)

# ---------- Lobby ----------
m = Map(29, 17, 11); m.room(0, 0, 29, 17)
m.block(1, 8, 4); m.block(24, 8, 4)
m.portal(14.5 * T / T, 4.2)
m.obj('candle', 12, 3); m.obj('candle', 16, 3)
peds = [('knight', 8, 11), ('ranger', 11, 11), ('wizard', 17, 11), ('paladin', 20, 11)]
for n, x, y in peds:
    m.obj('button', x, y + 1, dy=0)
    m.obj(n, x, y)
m.obj('npc1', 4, 4); m.obj('crate', 3, 5); m.obj('crate', 5, 5); m.obj('flask_r', 3, 5, dy=-10); m.obj('flask_b', 5, 5, dy=-10)
m.obj('npc2', 24, 4); m.obj('chest', 25, 5); m.obj('brazier', 23, 5)
for x in (4, 10, 18, 24): m.obj('storch', x, 0, dy=6)
for x in (7, 21): m.obj('flag', x, 0, dy=8)
m.obj('vase', 1, 1); m.obj('vase', 27, 1); m.obj('box', 1, 15); m.obj('box', 27, 15); m.obj('minibox', 2, 15)
m.obj('knight', 14, 13)
m.label('Portal → Play (ghi DungeonConfig)', 14.5, 4, 'door')
m.label('Chọn nhân vật (4 bệ)', 14, 11, 'player'); m.label('NPC Shop vĩnh viễn (gems)', 4, 4, 'npc'); m.label('NPC Nâng cấp chỉ số', 24, 4, 'npc')
META['lobby'] = m.save('lobby', 2)

# ---------- Floor overview ----------
SPC = 22
cells = {(0, 1): 'start', (1, 1): 'combat', (2, 1): 'combat', (2, 0): 'treasure', (2, 2): 'combat', (3, 2): 'combat', (4, 2): 'boss', (1, 2): 'shop'}
size = {'start': (11, 9), 'combat': (15, 13), 'treasure': (11, 9), 'shop': (13, 9), 'boss': (21, 17)}
edges = [((0, 1), (1, 1)), ((1, 1), (2, 1)), ((2, 1), (2, 0)), ((2, 1), (2, 2)), ((2, 2), (3, 2)), ((3, 2), (4, 2)), ((1, 1), (1, 2))]
m = Map(5 * SPC, 3 * SPC + 2, 21)
def rect(c):
    w, h = size[cells[c]]; cx, cy = c[0] * SPC + SPC // 2, c[1] * SPC + SPC // 2 + 1
    return cx - w // 2, cy - h // 2, w, h, cx, cy
doors = {c: '' for c in cells}
for a, b in edges:
    if a[0] == b[0]:
        top, bot = (a, b) if a[1] < b[1] else (b, a); doors[top] += 'S'; doors[bot] += 'N'
    else:
        l, r = (a, b) if a[0] < b[0] else (b, a); doors[l] += 'E'; doors[r] += 'W'
for a, b in edges:
    ax, ay, aw, ah, acx, acy = rect(a); bx, by, bw, bh, bcx, bcy = rect(b)
    if a[1] == b[1]:
        l, r = (rect(a), rect(b)) if a[0] < b[0] else (rect(b), rect(a))
        m.hcorr(l[0] + l[2], r[0] - 1, l[5])
    else:
        t, u = (rect(a), rect(b)) if a[1] < b[1] else (rect(b), rect(a))
        m.vcorr(t[4], t[1] + t[3], u[1] - 1)
for c, kind in cells.items():
    ox, oy, w, h, cx, cy = rect(c)
    m.room(ox, oy, w, h, doors=doors[c])
    if kind == 'start': m.obj('knight', cx, cy); m.obj('flag', cx - 2, oy, dy=8); m.obj('flag', cx + 2, oy, dy=8)
    if kind == 'combat':
        m.block(ox + 3, oy + 3, 3); m.block(ox + w - 6, oy + h - 4, 3)
        en = m.rng.sample(['skel', 'skel2', 'skull', 'zombie', 'vamp'], 3)
        for (dx, dy), e in zip([(-4, -3), (4, -2), (0, 3)], en): m.obj(e, cx + dx, cy + dy)
        m.obj('spike_on', cx, cy); m.obj('spike_off', cx + 1, cy)
    if kind == 'treasure': m.obj('chest', cx, cy); m.obj('candle', cx - 2, cy); m.obj('candle', cx + 2, cy)
    if kind == 'shop':
        for dx in (-3, 0, 3): m.obj('crate', cx + dx, cy + 1)
        m.obj('flask_r', cx - 3, cy + 1, dy=-10); m.obj('flask_b', cx, cy + 1, dy=-10); m.obj('key', cx + 3, cy + 1, dy=-10); m.obj('npc1', cx, cy - 2)
    if kind == 'boss':
        m.top.alpha_composite(vb, (cx * T - 16, cy * T - 40)); m.ring(cx, cy - 1, RED, 22, 9)
        for (dx, dy) in [(-6, -4), (6, -4), (-6, 4), (6, 4)]: m.block(cx + dx - 1, cy + dy, 3)
    for x in (ox + 2, ox + w - 3): m.obj('storch', x, oy, dy=6)
    m.label(kind, cx, oy, kind)
META['floor1'] = m.save('floor1', 1)
META['floor_cells'] = {f'{c[0]},{c[1]}': k for c, k in cells.items()}
json.dump(META, open(os.path.join(OUT, 'maps_meta.json'), 'w'), ensure_ascii=False, indent=1)
print({k: (v['w'], v['h']) for k, v in META.items() if isinstance(v, dict) and 'w' in v})
