# Turns the canvas boards into key-based i18n (same keys as the Unity String Table DG_Multilanguage).
# - markup text / alt / aria-label  -> {{tx.<key_ident>}}
# - JS string literals              -> S.<key_ident>
# - root font follows the language: EN = Pixelify Sans, VI = Tiny5 (pixel font with Vietnamese glyphs)
# - language lives in localStorage 'dg-lang' (shared by all boards), fallback to the `lang` tweak.
import os, re, sys, json, html
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..', 'localization'))
from strings_src import ROWS
os.chdir(os.path.join(HERE, '..', 'canvas', 'project'))

ident = lambda k: k.replace('.', '_')
EN = {r[0]: r[1] for r in ROWS}
VI = {r[0]: r[2] for r in ROWS}
SRC = {}
for r in ROWS:
    SRC.setdefault(r[3] if len(r) > 3 else r[2], r[0])
SRC_SORTED = sorted(SRC, key=len, reverse=True)

# Fragments around template holes, applied before the generic pass.
FRAG = {
 'UI-Equipment.dc.html': [('{{bagCount}} món · nhặt trong dungeon, chỉ thay ở sảnh', '{{bagCount}} {{tx.equip_bag_note}}')],
 'UI-Weapon.dc.html': [('Kiếm · Cận chiến · Rarity {{cur.rar}}', '{{tx.weapon_type}} {{cur.rar}}'),
                       ('{{bagCount}} vũ khí trong kho', '{{bagCount}} {{tx.weapon_armory}}')],
}

FONT_LINK_OLD = 'family=Be+Vietnam+Pro:wght@400;500;700&amp;family=Pixelify+Sans:wght@500;700'
FONT_LINK_NEW = 'family=Pixelify+Sans:wght@400;500;700&amp;family=Tiny5'

METHODS = '''
  componentDidMount() {
    this._dgLang = () => this.forceUpdate();
    window.addEventListener('storage', this._dgLang);
    window.addEventListener('dg-lang', this._dgLang);
  }
  componentWillUnmount() {
    window.removeEventListener('storage', this._dgLang);
    window.removeEventListener('dg-lang', this._dgLang);
  }
  lang() {
    let v = null;
    try { v = window.localStorage.getItem('dg-lang'); } catch (e) {}
    return v === 'vi' || v === 'en' ? v : (this.props.lang === 'vi' ? 'vi' : 'en');
  }
  setLang(v) {
    try { window.localStorage.setItem('dg-lang', v); } catch (e) {}
    try { window.dispatchEvent(new Event('dg-lang')); } catch (e) {}
    this.forceUpdate();
  }
  font() { return this.lang() === 'vi' ? "'Tiny5', monospace" : "'Pixelify Sans', monospace"; }
  strs() {
    const i = this.lang() === 'vi' ? 1 : 0, o = {};
    Object.keys(STR).forEach(k => { o[k] = STR[k][i]; });
    return o;
  }
'''

def apply(fn):
    s = open(fn, encoding='utf-8').read()
    if 'strs()' in s:
        print('skip (already done)', fn); return
    for a, b in FRAG.get(fn, []):
        assert s.count(a) == 1, (fn, a); s = s.replace(a, b)
    head, rest = s.split('</helmet>', 1)
    mk, tail = rest.split('</x-dc>', 1)
    used = set()

    def key_for(text):
        t = html.unescape(text)
        return SRC.get(t)

    # text nodes
    def tnode(m):
        raw = m.group(1); t = raw.strip()
        k = key_for(t)
        if not k: return m.group(0)
        used.add(k)
        return '>' + raw.replace(t, '{{tx.%s}}' % ident(k)) + '<'
    mk = re.sub(r'>([^<>]*?)<', tnode, mk)

    # attribute values: whole value, else known names inside (longest first)
    def attr(m):
        name, val = m.group(1), m.group(2)
        k = key_for(val)
        if k:
            used.add(k); return '%s="{{tx.%s}}"' % (name, ident(k))
        out = val
        for src in SRC_SORTED:
            if len(src) >= 3 and src in out and not src.isascii():
                out = out.replace(src, '{{tx.%s}}' % ident(SRC[src])); used.add(SRC[src])
        return '%s="%s"' % (name, out)
    mk = re.sub(r'\b(alt|aria-label)="([^"]*)"', attr, mk)

    # JS literals
    def lit(m):
        k = SRC.get(m.group(1))
        if not k: return m.group(0)
        used.add(k); return 'S.' + ident(k)
    tail = re.sub(r"'((?:[^'\\\n]|\\.)*)'", lambda m: lit(m) if not m.group(0).startswith("'/_blob") else m.group(0), tail)
    # undo replacements inside data-props JSON (never has VI strings, but stay safe) – nothing to do
    s = head + '</helmet>' + mk + '</x-dc>' + tail

    # fonts + lang
    s = s.replace(FONT_LINK_OLD, FONT_LINK_NEW).replace('<html lang="vi">', '<html lang="en">')
    s = s.replace("font-family: 'Be Vietnam Pro', system-ui, sans-serif", 'font-family: {{font}}')
    s = s.replace("font-family: 'Pixelify Sans', monospace; ", '').replace("; font-family: 'Pixelify Sans', monospace", '')
    s = s.replace("font-weight: 700; ", '').replace("; font-weight: 700", '')
    # tweak + string table
    s = s.replace("data-props='{", "data-props='{\"lang\":{\"editor\":\"enum\",\"options\":[\"en\",\"vi\"],\"default\":\"en\"},", 1)
    table = {ident(k): [EN[k], VI[k]] for k in sorted(used)}
    table_js = 'const STR = ' + json.dumps(table, ensure_ascii=False, indent=0).replace('\n', ' ') + ';\n'
    s = s.replace('class Component extends DCLogic {', table_js + 'class Component extends DCLogic {' + METHODS, 1)
    if 'renderVals() { return {}; }' in s:
        s = s.replace('renderVals() { return {}; }', 'renderVals() {\n    const S = this.strs();\n    return { tx: S, font: this.font() };\n  }')
    else:
        s = s.replace('  renderVals() {\n', '  renderVals() {\n    const S = this.strs();\n', 1)
        i = s.rindex('\n    return {')
        s = s[:i] + '\n    return { tx: S, font: this.font(),' + s[i + len('\n    return {'):]
    open(fn, 'w', encoding='utf-8').write(s)
    left = [t for t in re.findall(r"[^\x00-\x7f]{1,}[^<>'\"{}]*", s.split('</x-dc>')[0].split('</helmet>')[1]) if re.search(r'[À-ỹĐđ]', t)]
    print(fn, len(used), 'keys; untranslated markup bits:', left[:8])

for fn in sorted(f for f in os.listdir('.') if f.startswith('UI-') and f.endswith('.dc.html')):
    apply(fn)
