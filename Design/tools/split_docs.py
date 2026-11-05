# One-off: split ARCHITECTURE.md into UI_SYSTEM.md + ENTITIES_PREFABS.md (moved text kept verbatim,
# headings demoted), leaving numbered pointer stubs in ARCHITECTURE.md so existing "§x" references still resolve.
import os, re
ROOT = os.path.join(os.path.dirname(__file__), '..', '..')
DOCS = os.path.join(ROOT, 'docs')
TPL = os.path.join(os.path.dirname(__file__), 'docs_tpl')
arch_path = os.path.join(DOCS, 'ARCHITECTURE.md')
A = open(arch_path, encoding='utf-8').read().replace('\r\n', '\n')

def cut(start, end=None):
    i = A.index(start)
    j = A.index(end, i) if end else len(A)
    return i, j, A[i:j]

def demote(block, n, tag):
    out, first = [], True
    for line in block.split('\n'):
        m = re.match(r'^(#{1,6}) (.*)$', line)
        if m:
            line = '#' * min(6, len(m.group(1)) + n) + ' ' + m.group(2)
            if first:
                line += f' *(nguyên văn từ ARCHITECTURE {tag})*'
                first = False
        out.append(line)
    return '\n'.join(out).rstrip() + '\n'

i11, j11, s11 = cut('### 1.1. Luồng màn hình (UI flow)', '---\n\n## 2.')
i4, j4, s4 = cut('## 4. ', '---\n\n## 5.')
i5, j5, s5 = cut('## 5. ', '---\n\n## 6.')
i6, j6, s6 = cut('## 6. ', '---\n\n## 7.')
i8, j8, s8 = cut('## 8. ', '---\n\n## 9.')
i13, j13, s13 = cut('## 13. ')
k = s5.index('### 5.4.'); l = s5.index('### 5.5.')
s54, s5_items = s5[k:l], s5[:k] + s5[l:]

# ---- new docs
ui = open(os.path.join(TPL, 'UI_SYSTEM.tpl.md'), encoding='utf-8').read()
a11 = demote(s11, 0, '§1.1').replace('### 1.1. Luồng màn hình (UI flow)', '## 10. Luồng màn hình & quy tắc UI đã chốt', 1)
ui = ui.replace('{{A54}}', demote(s54, 1, '§5.4')).replace('{{A13}}', demote(s13, 1, '§13')).replace('{{A11}}', '---\n\n' + a11)
open(os.path.join(DOCS, 'UI_SYSTEM.md'), 'w', encoding='utf-8').write(ui)

en = open(os.path.join(TPL, 'ENTITIES_PREFABS.tpl.md'), encoding='utf-8').read()
en = en.replace('xem §1.4 nguyên văn', 'xem mục 8 nguyên văn bên dưới')
en = (en.replace('{{A5_ITEMS}}', demote(s5_items, 1, '§5 (trừ 5.4 → UI_SYSTEM §6.1)'))
        .replace('{{A8}}', demote(s8, 1, '§8'))
        .replace('{{A4}}', demote(s4, 2, '§4'))
        .replace('{{A6}}', demote(s6, 2, '§6')))
open(os.path.join(DOCS, 'ENTITIES_PREFABS.md'), 'w', encoding='utf-8').write(en)

# ---- stubs in ARCHITECTURE (same numbers/titles)
def subheads(block):
    return [m.group(0).lstrip('# ').strip() for m in re.finditer(r'^#{3,4} .*$', block, re.M)]

stub11 = ('### 1.1. Luồng màn hình (UI flow)\n\n> **Đã chuyển** sang `docs/UI_SYSTEM.md` — §10 (nguyên văn), cùng scene↔UI (§1), `UIRoot` (§2), '
          'co giãn 1280×720 + pixel snap (§3), bảng neo từng màn (§4), luồng prefab UI (§5).\n\n')
def stub(block, title_line, target, extra=''):
    lines = '\n'.join(f'- {h}' for h in subheads(block))
    return f'{title_line}\n\n> **Đã chuyển** sang {target}.{extra}\n\n' + (f'Các mục con (giữ số để tra cứu):\n{lines}\n\n' if lines else '')
t4 = s4.split('\n', 1)[0]; t5 = s5.split('\n', 1)[0]; t6 = s6.split('\n', 1)[0]; t8 = s8.split('\n', 1)[0]; t13 = s13.split('\n', 1)[0]
parts = [
    (i11, j11, stub11),
    (i4, j4, stub(s4, t4, '`docs/ENTITIES_PREFABS.md` §2 (nguyên văn + prefab `Player` + luồng spawn)')),
    (i5, j5, stub(s5, t5, '`docs/ENTITIES_PREFABS.md` §1 (nguyên văn)', ' Riêng **5.4 Ô vật phẩm (`ItemTile`)** sang `docs/UI_SYSTEM.md` §6.1.')),
    (i6, j6, stub(s6, t6, '`docs/ENTITIES_PREFABS.md` §3 (nguyên văn + prefab quái/boss + luồng sinh → chết → pool)')),
    (i8, j8, stub(s8, t8, '`docs/ENTITIES_PREFABS.md` §1 (nguyên văn + prefab pickup/rương/loot)')),
    (i13, j13, stub(s13, t13, '`docs/UI_SYSTEM.md` §7 (nguyên văn)')),
]
out = A
for i, j, text in sorted(parts, key=lambda p: p[0], reverse=True):
    out = out[:i] + text + out[j:]
open(arch_path, 'w', encoding='utf-8').write(out)
print('ARCH', len(A.splitlines()), '->', len(out.splitlines()))
print('UI', len(ui.splitlines()), 'ENT', len(en.splitlines()))
