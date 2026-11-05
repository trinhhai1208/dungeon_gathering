import sys, os
from PIL import Image, ImageDraw
SRC = sys.argv[1]
OUT = sys.argv[2]
os.makedirs(OUT, exist_ok=True)

def load(p):
    return Image.open(os.path.join(SRC, p)).convert('RGBA')

def trim(im):
    bb = im.getbbox()
    return im.crop(bb) if bb else im

def save(name, im, scale=4, do_trim=True):
    if im.size == (16, 16) and not name.startswith(('ui_', 'it_', 'tr_', 'pr_')): im = clean(im)
    if do_trim:
        im = trim(im)
    im = im.resize((im.width * scale, im.height * scale), Image.NEAREST)
    im.save(os.path.join(OUT, name + '.png'))

def crop(p, x, y, w, h):
    return load(p).crop((x, y, x + w, y + h))

def red(im):
    import colorsys
    im = im.copy(); px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a == 0: continue
            h, s_, v = colorsys.rgb_to_hsv(r/255, g/255, b/255)
            if s_ > 0.35 and 0.45 < h < 0.75:
                nr, ng, nb = colorsys.hsv_to_rgb(0.985, s_, v)
                px[x, y] = (int(nr*255), int(ng*255), int(nb*255), a)
    return im

def clean(im):
    im = im.copy(); px = im.load(); w, h = im.size
    for (cx, cy) in [(0, 0), (w-3, 0), (0, h-3), (w-3, h-3)]:
        for y in range(cy, cy+3):
            for x in range(cx, cx+3):
                r, g, b, a = px[x, y]
                if a and (max(r, g, b) - min(r, g, b) > 60):
                    px[x, y] = (0, 0, 0, 0)
    return im

def gold(im):
    r, g, b, a = im.split()
    return Image.merge('RGBA', (b, g, r, a))

# characters
save('ch_knight_blue', crop('Characters/Knight/Blue Knight idle Sprite-sheet 16x16.png', 0, 0, 16, 16))
save('ch_knight_green', crop('Characters/Knight/Green Knight idle Sprite-sheet 16x16.png', 0, 0, 16, 16))
save('ch_paladin', crop('Characters/FantasyKnights/01Knight.png', 0, 8, 48, 48), 2)
save('ch_priest1', load('Characters/Priests/priest1/v1/priest1_v1_1.png'))
save('ch_priest2', load('Characters/Priests/priest2/v1/priest2_v1_1.png'))
save('ch_priest3', load('Characters/Priests/priest3/v1/priest3_v1_1.png'))
for c in range(7):
    for r in range(4):
        save(f'dc_{c}_{r}', crop('Characters/Dungeon_Character.png', c * 16, r * 16, 16, 16))

# enemies
M = 'Enemies/DungeonMonsters/'
save('en_skeleton1', load(M + 'skeleton1/v1/skeleton_v1_1.png'))
save('en_skeleton2', load(M + 'skeleton2/v1/skeleton2_v1_1.png') if os.path.exists(os.path.join(SRC, M + 'skeleton2/v1/skeleton2_v1_1.png')) else load(M + 'skeleton1/v2/skeleton_v2_1.png'))
save('en_skull', load(M + 'skull/v1/skull_v1_1.png'))
save('en_vampire', load(M + 'vampire/v1/vampire_v1_1.png'))
save('en_vampire_big', load(M + 'vampire/v1/vampire_v1_1.png'), 8)
save('en_zombie', crop('Enemies/Zombie/1Zombie-Idle.png', 0, 0, 32, 32))
save('en_zombie_face', crop('Enemies/Zombie/Zombies 1.1 portraits 16x16.png', 0, 0, 16, 16))

# items
I = 'Items/'
save('it_coin', load(I + 'Coins/coin_1.png'))
save('it_key', load(I + 'Keys/keys_1_1.png'))
save('it_key2', load(I + 'Keys/keys_2_1.png'))
for n in range(1, 5):
    save(f'it_flask{n}', load(I + f'Potions/flasks_{n}_1.png'))
save('it_chest', load(I + 'Chest/chest_1.png'))
save('it_chest_open', load(I + 'Chest/chest_open_4.png'))
save('it_minichest', load(I + 'MiniChest/mini_chest_1.png'))
save('it_pot_red', crop(I + 'Potions/Potions.png', 0, 0, 16, 16))
save('it_pot_blue', crop(I + 'Potions/Potions.png', 16, 0, 16, 16))
save('it_pot_grey', crop(I + 'Potions/Potions.png', 32, 0, 16, 16))
save('it_bluecoin', crop(I + 'Coins/BlueCoin Sheet.png', 0, 0, 16, 32))

# traps & props
save('tr_spike_off', load('Traps/SpikeTrap/peaks_1.png'))
save('tr_spike_on', load('Traps/SpikeTrap/peaks_4.png'))
save('tr_flame', load('Traps/Flamethrower/flamethrower_1_3.png'))
save('tr_flame2', load('Traps/Flamethrower/flamethrower_2_3.png'))
save('tr_arrow', load('Traps/ArrowTrap/arrow_1.png'))
save('tr_button', crop('Traps/Button.png', 0, 0, 16, 16))
P = 'Environment/Props/'
save('pr_torch', load(P + 'torch/torch_1.png'))
save('pr_side_torch', load(P + 'torch/side_torch_1.png'))
save('pr_candle', load(P + 'torch/candlestick_1_1.png'))
save('pr_flag', load(P + 'flag/flag_1.png'))
save('pr_box', load(P + 'box_1/box_1_1.png'))
save('pr_minibox', load(P + 'mini_box_1/mini_box_1_1.png'))
save('pr_vase', crop(P + 'Vase Shine Anim.png', 0, 0, 16, 16))
save('ui_arrow', load('UI/Indicators/arrow_1.png'))

# UI sheet crops (generous boxes, trimmed)
B = 'UI/HUD/Basic Buttons 3 Fv.png'
ui = {
    'ui_btn_play': (238, 94, 50, 30), 'ui_btn_playtext': (238, 126, 62, 30),
    'ui_gear_btn': (140, 10, 24, 24), 'ui_btn_sword': (178, 176, 28, 30), 'ui_btn_boot': (210, 176, 28, 30),
    'ui_btn_up': (242, 176, 28, 30), 'ui_btn_right': (274, 176, 28, 30), 'ui_btn_left': (306, 176, 28, 30), 'ui_btn_down': (338, 176, 28, 30),
    'ui_btn_pause': (130, 216, 24, 28), 'ui_btn_sound': (14, 182, 26, 28), 'ui_btn_music': (14, 214, 26, 28),
    'ui_slot_s': (376, 94, 34, 34), 'ui_slot_a': (376, 126, 34, 34), 'ui_slot_b': (376, 158, 34, 34), 'ui_slot_c': (376, 190, 34, 34), 'ui_slot_f': (376, 222, 34, 34),
    'ui_slot_empty': (474, 222, 30, 30), 'ui_slot_empty_s': (474, 94, 30, 30), 'ui_slot_empty_a': (474, 126, 30, 30), 'ui_slot_empty_b': (474, 158, 30, 30), 'ui_slot_empty_c': (474, 190, 30, 30),
    'ui_panel': (234, 8, 30, 30), 'ui_panel_big': (378, 8, 44, 28),
    'ui_ic_shield': (176, 46, 18, 20), 'ui_ic_gear': (112, 46, 18, 20), 'ui_ic_home': (128, 46, 18, 20), 'ui_ic_exit': (192, 46, 18, 20), 'ui_ic_x': (208, 46, 18, 20), 'ui_ic_pause': (144, 46, 18, 20),
}
for k, (x, y, w, h) in ui.items():
    save(k, crop(B, x, y, w, h), 3)
H = 'UI/HUD/Hearts Blue.png'
save('ui_heart_full', red(crop(H, 32, 0, 16, 16)))
save('ui_heart_half', red(crop(H, 32, 32, 16, 16)))
save('ui_heart_empty', crop(H, 32, 64, 16, 16))
save('ui_mana', crop(H, 32, 0, 16, 16))
save('ui_heart_big', red(crop(H, 160, 0, 16, 16)))
save('ui_cursor', crop('UI/HUD/Mouse cursors.png', 0, 0, 16, 16))
print('ok', len(os.listdir(OUT)))
