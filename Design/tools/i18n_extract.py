# Lists every user-facing string on the UI boards (markup text, alt/aria-label, JS string literals).
import re, glob, os, sys
os.chdir(os.path.join(os.path.dirname(__file__), '..', 'canvas', 'project'))
WORD = re.compile(r"[A-Za-zÀ-ỹĐđ]{2,}")
CODEY = re.compile(r"^(/_blob|#|\d+px|f-|b-|UI-)|\.dc\.html$|^[a-z]+([-_][a-z0-9]+)*$|^[A-Z]{1,3}$|^(WARRIOR|RANGER|MAGE|ASSASSIN)$")
out = []
for fn in sorted(glob.glob('UI-*.dc.html')):
    s = open(fn, encoding='utf-8').read()
    mk = s[s.index('</helmet>'):s.index('</x-dc>')]
    js = s[s.index('class Component'):]
    found = []
    for m in re.finditer(r">([^<>]*?)<", mk):
        t = m.group(1).strip()
        t2 = re.sub(r"\{\{[^}]*\}\}", "", t)
        if WORD.search(t2): found.append(('T', t))
    for m in re.finditer(r'(?:alt|aria-label|title)="([^"]+)"', mk):
        t = m.group(1)
        if WORD.search(re.sub(r"\{\{[^}]*\}\}", "", t)): found.append(('A', t))
    for m in re.finditer(r"'((?:[^'\\]|\\.)*)'", js):
        t = m.group(1)
        if WORD.search(t) and not CODEY.search(t): found.append(('J', t))
    seen = []
    for f in found:
        if f not in seen: seen.append(f)
    out.append(f'===== {fn} {len(seen)}')
    out += [f'{k} | {t}' for k, t in seen]
open(os.path.join('..', '..', 'tools', 'strings.txt'), 'w', encoding='utf-8').write('\n'.join(out))
print(len(out))
