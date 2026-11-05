import sys, os
SRC, OUT = sys.argv[1], sys.argv[2]
code = open(os.path.join(os.path.dirname(__file__), 'maps.py'), encoding='utf-8').read().split('META = {}')[0]
sys.argv = ['x', SRC, OUT]
exec(code)
from PIL import Image
m = Map(27, 15, 21); m.room(0, 0, 27, 15)
m.portal(13.5, 3.0)
m.obj('candle', 11, 2); m.obj('candle', 16, 2)
for x in (4, 9, 18, 23): m.obj('storch', x, 0, dy=6)
for x in (7, 20): m.obj('flag', x, 0, dy=8)
m.obj('npc1', 3, 3); m.obj('crate', 2, 4); m.obj('crate', 3, 4); m.obj('crate', 4, 4)
m.obj('flask_r', 2, 4, dy=-10); m.obj('flask_b', 4, 4, dy=-10); m.obj('key', 3, 4, dy=-10)
m.obj('npc2', 23, 3); m.obj('chest', 24, 4); m.obj('brazier', 22, 4); m.obj('minichest', 25, 4)
for x in (12, 13, 14): m.obj('crate', x, 8)
m.obj('chalice', 12, 8, dy=-10); m.obj('candle', 14, 8, dy=-12); m.obj('coin', 13, 8, dy=-10)
m.obj('vase', 1, 1); m.obj('vase', 25, 1); m.obj('box', 1, 13); m.obj('minibox', 2, 13); m.obj('box', 25, 13)
m.obj('bones', 9, 12); m.obj('skullbone', 17, 12); m.obj('banner', 13, 13)
m.save('lobby_hall', 3)
SW = Image.open(os.path.join(SRC, 'Items/Weapon/Sword/File.png')).convert('RGBA')
def trim(im): return im.crop(im.getbbox())
P = {'iron': (0, 4), 'dark': (0, 3), 'ice': (1, 0), 'void': (3, 2), 'fire': (0, 0), 'thorn': (2, 0),
     'gold': (4, 1), 'lava': (4, 2), 'thunder': (0, 2), 'old': (5, 2), 'holy': (3, 4), 'venom': (2, 3)}
for k, (c, r) in P.items():
    s = trim(SW.crop((c * 32, r * 32, c * 32 + 32, r * 32 + 32)))
    s.resize((s.width * 3, s.height * 3), Image.NEAREST).save(os.path.join(OUT, f'sw_{k}.png'))
