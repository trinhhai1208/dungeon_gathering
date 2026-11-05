# One-off patch: uniform tiles, rarity letters, item active skills (max 2), victory continue.
import json, re, os
os.chdir(os.path.join(os.path.dirname(__file__), '..', 'canvas', 'project'))
SW = json.load(open('../../cuts/blob-map.json'))['swordTiles']
LET = "const LET = { S: '/_blob/c04911b55cda3c35c672b6de5b2608de', A: '/_blob/5ab2dfa420835a27a80274756ebe07c6', B: '/_blob/520ce0ed0fca6b027f29471e6726bc28', C: '/_blob/6ce73af3c47c07ecb1e4177b5049d482', F: '/_blob/2a8c8dbb1a05c4b01ac9490c5e4f0ce1' };"
ARROW = '/_blob/22809a33cb74678617fb4dabe8444757'

def patch(fn, pairs):
    s = open(fn, encoding='utf-8').read()
    for a, b in pairs:
        assert s.count(a) == 1, (fn, a[:70], s.count(a))
        s = s.replace(a, b)
    open(fn, 'w', encoding='utf-8').write(s)

def swap_swords(fn, prefix="img: '/_blob/"):
    s = open(fn, encoding='utf-8').read()
    for a, b in SW.items():
        s = s.replace(prefix + a, prefix + b)
    open(fn, 'w', encoding='utf-8').write(s)

# ---- Lobby
fn = 'UI-Lobby.dc.html'
s = open(fn, encoding='utf-8').read()
line = re.search(r"    const SW = \{.*?\n", s).group(0)
nl = line
for a, b in SW.items():
    nl = nl.replace(a, b)
open(fn, 'w', encoding='utf-8').write(s.replace(line, nl))
patch(fn, [
 ("const NONE = { name: 'Trống', img: '' };", "const NONE = { name: 'Trống', img: '/_blob/f8a5b3547647c3396f6cea26aed916b1' };"),
 ('<div class="f-mid-s" style="display: flex; align-items: center; gap: 4px; padding: 0 4px; color: #1b1420">\n'
  '        <sc-for list="{{cur.eq}}" as="e" hint-placeholder-count="6"><img src="{{e}}" alt="" style="height: 40px"></sc-for>\n'
  '        <span style="flex: 1"></span>\n'
  '        <sc-for list="{{cur.wp}}" as="w" hint-placeholder-count="2"><span class="f-dark-xs" style="width: 44px; height: 44px; display: flex; align-items: center; justify-content: center"><sc-if value="{{w.img}}" hint-placeholder-val="{{ true }}"><img src="{{w.img}}" alt="{{w.name}}" style="height: 26px"></sc-if></span></sc-for>\n'
  '      </div>',
  '<div style="display: flex; align-items: flex-end; gap: 2px">\n'
  '        <sc-for list="{{cur.eq}}" as="e" hint-placeholder-count="6"><img src="{{e}}" alt="" style="width: 48px; display: block"></sc-for>\n'
  '        <span style="width: 10px"></span>\n'
  '        <sc-for list="{{cur.wp}}" as="w" hint-placeholder-count="2"><img src="{{w.img}}" alt="{{w.name}}" style="width: 48px; display: block"></sc-for>\n'
  '      </div>\n'
  '      <div style="font-size: 13px; color: #e6e9f2">Kích hoạt <b style="color: #9fd0ff">{{cur.actN}}/2</b> · {{cur.acts}}</div>'),
 ("eq: KEQ, wp: [SW.iron, SW.ice] },", "eq: KEQ, wp: [SW.iron, SW.ice], acts: ['Băng Trận', 'Bão Phép'] },"),
 ("wp: [SW.holy, NONE], price: 1500 }", "wp: [SW.holy, NONE], acts: ['Thánh Quang'], price: 1500 }"),
 ("cur: c ? { ...c, playable: !c.price,", "cur: c ? { ...c, playable: !c.price, actN: (c.acts || []).length, acts: (c.acts || []).join(' · ') || 'chưa có món nào',"),
])

# ---- HUD
patch('UI-HUD.dc.html', [
 ('<img src="{{w.img}}" alt="" style="height: 84px; display: block">', '<img src="{{w.img}}" alt="" style="width: 72px; display: block">'),
 ('    <img src="/_blob/4a34d3ca6f6a112a31cf508570c33cca" alt="Lướt"',
  '    <div style="position: absolute; right: 248px; bottom: 0; display: flex; flex-direction: column; gap: 6px; align-items: center">\n'
  '      <span style="font-size: 10px; font-weight: 700; letter-spacing: 1px; text-shadow: 0 1px 0 #1b1420">KÍCH HOẠT</span>\n'
  '      <sc-for list="{{acts}}" as="a" hint-placeholder-count="2">\n'
  '        <button type="button" aria-label="{{a.name}}" style="position: relative; display: block">\n'
  '          <img src="{{a.img}}" alt="" style="width: 60px; display: block; opacity: {{a.op}}">\n'
  '          <sc-if value="{{a.cd}}" hint-placeholder-val="{{ false }}"><span style="position: absolute; inset: 0; display: flex; align-items: center; justify-content: center; font-family: \'Pixelify Sans\', monospace; font-size: 24px; font-weight: 700; color: #ffffff; text-shadow: 0 2px 0 #1b1420">{{a.cd}}</span></sc-if>\n'
  '        </button>\n'
  '      </sc-for>\n'
  '    </div>\n'
  '    <img src="/_blob/4a34d3ca6f6a112a31cf508570c33cca" alt="Lướt"'),
 ("      w: weapons[this.state.w],",
  "      w: weapons[this.state.w],\n"
  "      acts: [\n"
  "        { name: 'Băng Trận (Kiếm Băng) · hồi 9s', img: '/_blob/b7309a422bec545deda9d4104492539e', cd: '9', op: 0.45 },\n"
  "        { name: 'Bão Phép (Sách Cổ Ngữ) · sẵn sàng', img: '/_blob/d74730072c3d826fbcbf102a822c8485', cd: '', op: 1 }\n"
  "      ],"),
])

# ---- Pause (in-game: colour tile only, no letters)
fn = 'UI-Pause.dc.html'
s = open(fn, encoding='utf-8').read()
s = s.replace('style="height: 56px">', 'style="width: 56px">').replace('style="height: 44px">', 'style="width: 48px">')
s = s.replace('2 vũ khí · trang bị đang mặc · túi: 3 món mới (thay ở sảnh)', '2 vũ khí · trang bị đang mặc · kích hoạt 2/2 · túi: 3 món mới (thay ở sảnh)')
open(fn, 'w', encoding='utf-8').write(s)

# ---- RunEnd
fn = 'UI-RunEnd.dc.html'
s = open(fn, encoding='utf-8').read()
open(fn, 'w', encoding='utf-8').write(s.replace('style="height: 68px">', 'style="width: 72px">'))

# ---- Victory
swap_swords('UI-Victory.dc.html')
patch('UI-Victory.dc.html', [
 ('            <div class="f-slot-s" style="position: relative; width: 76px; height: 76px; display: flex; align-items: center; justify-content: center">\n'
  '              <img src="{{l.img}}" alt="{{l.name}}" style="max-height: 52px; max-width: 52px">\n'
  '              <span style="position: absolute; right: -6px; bottom: -8px; font-family: \'Pixelify Sans\', monospace; font-size: 15px; font-weight: 700; color: {{l.rc}}; text-shadow: 0 2px 0 #1b1420">{{l.rar}}</span>\n'
  '            </div>',
  '            <div style="position: relative">\n'
  '              <img src="{{l.img}}" alt="{{l.name}}" style="width: 72px; display: block">\n'
  '              <img src="{{l.let}}" alt="" style="position: absolute; left: -6px; top: -7px; width: 21px; height: 24px">\n'
  '            </div>'),
 ("    const RC = { S: '#ffd27a', A: '#ff8a84', B: '#9fd0ff', C: '#b8e6a0', F: '#e6e9f2' };", "    " + LET),
 ("loot.map(l => ({ ...l, rc: RC[l.rar] }))", "loot.map(l => ({ ...l, let: LET[l.rar] }))"),
 ('DUNGEON HOÀN THÀNH · TẦNG 3', 'ĐÃ HẠ BOSS · TẦNG 3'),
 ('<a href="UI-Lobby.dc.html" class="f-light"', '<a href="UI-Lobby.dc.html" class="f-btn"'),
 ('VỀ SẢNH · THAY ĐỒ</a>', 'VỀ SẢNH · NHẬN +346</a>'),
 ('<a href="UI-HUD.dc.html" class="f-btn" style="display: flex; align-items: center; gap: 10px; height: 76px; padding: 0 12px; color: #1b1420; text-decoration: none; font-family: \'Pixelify Sans\', monospace; font-size: 24px; font-weight: 700">CHƠI LẠI</a>',
  '<a href="UI-HUD.dc.html" class="f-light" style="display: flex; align-items: center; gap: 10px; height: 76px; padding: 0 12px; color: #1b1420; text-decoration: none; font-family: \'Pixelify Sans\', monospace; font-size: 24px; font-weight: 700">TIẾP TỤC · TẦNG 4<img src="' + ARROW + '" alt="" style="height: 30px"></a>'),
 ('Coin mang về sảnh</div>', 'Coin mang về nếu về sảnh</div>'),
 ('<span class="f-light-s" style="padding: 0 8px; font-size: 14px; font-weight: 700; color: #1b1420">KỶ LỤC 14:32</span>',
  '<span class="f-light-s" style="padding: 0 8px; font-size: 14px; font-weight: 700; color: #1b1420">KỶ LỤC 14:32</span>\n'
  '        <span class="f-dark-s" style="padding: 0 8px; font-size: 13px">Tiếp tục: giữ đồ &amp; coin của run, quái mạnh hơn</span>'),
 ("{ k: 'Lần đầu clear dungeon', v: '+100'", "{ k: 'Lần đầu hạ Boss tầng 3', v: '+100'"),
])

# ---- Shop
swap_swords('UI-Shop.dc.html')
fn = 'UI-Shop.dc.html'
s = open(fn, encoding='utf-8').read()
s = s.replace('<img src="{{it.img}}" alt="" style="height: 96px; margin-top: 18px">', '<img src="{{it.img}}" alt="" style="width: 96px; margin-top: 18px">')
s = s.replace('<img src="{{cur.img}}" alt="" style="height: 96px">', '<img src="{{cur.img}}" alt="" style="width: 96px">')
open(fn, 'w', encoding='utf-8').write(s)
print('ok')
