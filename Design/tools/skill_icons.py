"""Vẽ icon chiêu thức pixel-art 16x16 (tự viền tối), xuất PNG + ảnh preview trên nút skill."""
import math, sys, os
from PIL import Image, ImageDraw

OUT, PREV, GEN = sys.argv[1], sys.argv[2], sys.argv[3]
N = 16
INK = (27, 20, 32, 255)

def new():
    return Image.new('RGBA', (N, N), (0, 0, 0, 0))

def px(im, x, y, c):
    if 0 <= x < N and 0 <= y < N: im.putpixel((int(x), int(y)), c)

def line(im, a, b, c):
    ImageDraw.Draw(im).line([a, b], fill=c)

def outline(fg):
    """Viền 1px màu INK quanh mọi pixel đục (8 hướng ở góc trong để nét gọn)."""
    o = new()
    for y in range(N):
        for x in range(N):
            if fg.getpixel((x, y))[3] > 200: continue
            hit = False
            for dx, dy in ((1,0),(-1,0),(0,1),(0,-1)):
                xx, yy = x+dx, y+dy
                if 0 <= xx < N and 0 <= yy < N and fg.getpixel((xx, yy))[3] > 200: hit = True
            if hit: o.putpixel((x, y), INK)
    o.alpha_composite(fg)
    return o

def aura(color, r=7.0, a_in=110, a_out=40):
    im = new()
    c = (N-1)/2
    for y in range(N):
        for x in range(N):
            d = math.hypot(x-c, y-c)
            if d <= r:
                a = int(a_in + (a_out-a_in) * (d/r))
                # lượng tử hóa alpha theo bậc cho chất pixel
                a = (a // 24) * 24
                im.putpixel((x, y), color + (a,))
    return im

def compose(back, fg):
    out = new()
    if back: out.alpha_composite(back)
    out.alpha_composite(outline(fg))
    return out

# ---------------- 1. Song kiếm (dual-wield): 2 kiếm bắt chéo + tia lửa cuồng nộ
def dual_wield():
    fg = new()
    W, S = (244,247,255,255), (128,140,172,255)
    G, GD, B, BL = (255,210,90,255), (190,120,36,255), (120,70,42,255), (170,104,60,255)
    for mirror in (False, True):
        f = (lambda x: 15-x) if mirror else (lambda x: x)
        for t in range(2, 10):                      # lưỡi 2px: cạnh sáng + cạnh tối
            px(fg, f(t), t, W); px(fg, f(t+1), t, S)
        for gx, gy, c in ((11,9,G),(10,10,GD),(9,11,G)):    # chắn kiếm ngắn 3px, hai bên tách nhau 2px
            px(fg, f(gx), gy, c)
        px(fg, f(11), 11, BL); px(fg, f(12), 12, B)   # chuôi
        px(fg, f(13), 13, G)                          # núm
    return compose(aura((210,58,44), a_in=96, a_out=24), fg)

# ---------------- 2. Thánh quang (holy-light): mặt trời thánh + chữ thập
def holy_light():
    fg = new()
    Y, Gd, Wt, Gl = (255,214,96,255), (214,150,40,255), (255,250,224,255), (255,236,160,255)
    c = 7.5
    for ang in range(0, 360, 45):
        L = 7 if ang % 90 == 0 else 5
        a = math.radians(ang)
        for r in range(3, L+1):
            px(fg, round(c + math.cos(a)*r - 0.01), round(c + math.sin(a)*r - 0.01), Gd if r == L else Y)
    for y in range(N):
        for x in range(N):
            if math.hypot(x-c, y-c) <= 3.2: px(fg, x, y, Gl)
    for i in range(5, 11): px(fg, 7, i, Wt); px(fg, 8, i, Wt)          # chữ thập
    for i in range(6, 10): px(fg, i, 7, Wt)
    px(fg, 6, 7, Wt); px(fg, 9, 7, Wt)
    return compose(aura((255,210,110), a_in=120), fg)

# ---------------- 3. Lăn né (dodge-roll): vòng mũi tên xoay + vệt tốc độ
def dodge_roll():
    fg = new()
    G1, G2, G3 = (120,232,160,255), (44,160,100,255), (210,255,228,255)
    cx, cy, r = 7.5, 8.0, 4.6
    a0, a1 = 70, 330                                   # cung ngược chiều kim đồng hồ, chừa khe phía trên-phải
    for deg in range(a0, a1, 3):
        a = math.radians(deg)
        for rr, col in ((r+0.6, G1), (r-0.5, G2)):
            px(fg, round(cx + math.cos(a)*rr), round(cy - math.sin(a)*rr), col)
    # đầu mũi tên ở đầu cung (70°), chỉ theo chiều kim đồng hồ (tiếp tuyến)
    a = math.radians(a0)
    ex, ey = cx + math.cos(a)*r, cy - math.sin(a)*r
    tx, ty = math.sin(a), math.cos(a)                  # tiếp tuyến theo chiều kim đồng hồ (trục y hướng xuống)
    nx, ny = math.cos(a), -math.sin(a)
    tip = (ex + tx*3.2, ey + ty*3.2)
    ImageDraw.Draw(fg).polygon([tip, (ex + nx*2.6, ey + ny*2.6), (ex - nx*2.6, ey - ny*2.6)], fill=G1)
    px(fg, round(tip[0]-tx), round(tip[1]-ty), G3)
    for x0, y0, L in ((1,13,3),(2,15,2)):              # vệt tốc độ
        for i in range(L): px(fg, x0+i, y0, G3 if i == 0 else G2)
    return compose(aura((80,200,140), a_in=100), fg)

# ---------------- 4. Bão phép (arcane-storm): xoáy tím + sao
def arcane_storm():
    fg = new()
    P1, P2, P3, St = (196,120,255,255), (130,70,210,255), (244,200,255,255), (255,240,150,255)
    c = 7.5
    for i in range(0, 260):
        t = i / 260 * 2.6 * math.pi
        r = 0.8 + t * 0.78
        x, y = c + math.cos(t)*r, c + math.sin(t)*r
        col = P3 if r < 2.5 else (P1 if r < 5 else P2)
        px(fg, round(x), round(y), col)
        if r > 2.2: px(fg, round(x + math.cos(t)*0.9), round(y + math.sin(t)*0.9), P2)
    for sx, sy in ((2,2),(13,3),(12,13)):                                 # sao 4 cánh
        px(fg, sx, sy, St); px(fg, sx+1, sy, St); px(fg, sx-1, sy, St); px(fg, sx, sy+1, St); px(fg, sx, sy-1, St)
    return compose(aura((150,80,230), a_in=110), fg)

# ---------------- 5. Giơ khiên (raise-shield): khiên diều xanh viền bạc, núm vàng
def raise_shield():
    fg = new()
    Rim, RimD, Bl, BlD, BlL, G = (220,228,240,255), (140,150,176,255), (64,130,230,255), (40,82,170,255), (120,180,255,255), (255,206,84,255)
    rows = {2:(3,12),3:(2,13),4:(2,13),5:(2,13),6:(2,13),7:(2,13),8:(3,12),9:(3,12),10:(4,11),11:(5,10),12:(6,9),13:(7,8)}
    for y, (a, b) in rows.items():
        for x in range(a, b+1):
            edge = x in (a, b) or y in (2, 13) or (y == 3 and (x <= 3 or x >= 12))
            if edge: px(fg, x, y, Rim if x < 8 else RimD)
            else: px(fg, x, y, BlL if (x < 6 and y < 7) else (Bl if x < 9 else BlD))
    for y in range(4, 12): px(fg, 7, y, G); px(fg, 8, y, (214,150,40,255))   # đai dọc
    for x in range(4, 12): px(fg, x, 6, G)                                   # đai ngang
    px(fg, 7, 6, (255,244,190,255)); px(fg, 8, 6, G)
    for x, y in ((4,4),(5,4),(4,5)): px(fg, x, y, (210,236,255,255))          # ánh sáng
    return compose(aura((80,150,255), a_in=90), fg)

# ---------------- 6. Long hỏa (dragon-flame): ngọn lửa 3 lớp
def dragon_flame():
    fg = new()
    R, O, Y, W = (214,52,40,255), (255,128,40,255), (255,214,80,255), (255,248,210,255)
    def flame(cx, base, h, w, col):
        for y in range(base - h, base + 1):
            t = (base - y) / h                               # 0 ở đáy → 1 ở đỉnh
            half = w * math.sin(math.pi * min(1.0, 0.25 + t * 0.85)) * (1 - t * 0.55)
            sway = math.sin(t * 3.2) * 1.4 * t
            for x in range(N):
                if abs(x - (cx + sway)) <= half: px(fg, x, y, col)
    flame(7.5, 14, 13, 5.6, R)
    flame(7.8, 14, 10, 4.0, O)
    flame(8.0, 14, 6, 2.4, Y)
    for x, y in ((8,12),(7,12),(8,11)): px(fg, x, y, W)
    px(fg, 3, 4, O); px(fg, 12, 2, Y); px(fg, 13, 6, O)                       # tàn lửa
    return compose(aura((240,90,40), a_in=110), fg)

# ---------------- 7. Lôi phạt (thunder-judgement): tia sét vàng lõi trắng
def thunder():
    fg = new()
    Y, YD, W = (255,226,72,255), (230,160,30,255), (255,255,230,255)
    poly = [(9,0),(3,8),(7,8),(5,15),(13,6),(9,6),(11,0)]
    ImageDraw.Draw(fg).polygon(poly, fill=Y)
    for x, y in ((9,2),(8,3),(7,4),(6,5),(5,6),(7,9),(7,10),(6,11),(6,12)): px(fg, x, y, W)   # lõi
    for x, y in ((10,7),(9,8),(8,10),(11,6),(12,6)): px(fg, x, y, YD)                     # bóng
    for x, y in ((2,2),(13,11),(1,12)): px(fg, x, y, (150,230,255,255))                    # tia điện
    return compose(aura((90,190,255), a_in=110), fg)

# ---------------- 8. Rạch không gian (rift-cut): vệt chém lưỡi liềm + khe hư không
def rift_cut():
    fg = new()
    W, V1, V2, Dk = (246,236,255,255), (190,130,255,255), (120,64,214,255), (34,10,60,255)
    cx, cy, r = 1.5, 14.0, 11.0
    for y in range(N):
        for x in range(N):
            d = math.hypot(x-cx, y-cy); ang = math.degrees(math.atan2(cy-y, x-cx))
            if not (0 <= ang <= 90): continue
            t = ang / 90.0
            w = 0.8 + 3.4 * math.sin(math.pi * t)          # vệt dày giữa, nhọn 2 đầu
            off = abs(d - r)
            if off <= w / 2: px(fg, x, y, W if off < w*0.18 else (V1 if off < w*0.34 else V2))
    for i in range(-2, 3):                                  # khe hư không cắt ngang vệt chém
        px(fg, 8+i, 6+i, Dk)
    for x, y, c in ((12,11,V1),(13,9,V2),(5,3,V1),(3,5,V2),(11,13,V2)): px(fg, x, y, c)   # mảnh vụn hư không
    return compose(aura((140,70,230), a_in=110), fg)

# ---------------- 9. Băng vực (frost-field): bông tuyết 6 cánh
def frost_field():
    fg = new()
    C1, C2, W = (150,220,255,255), (80,160,230,255), (240,252,255,255)
    c = 7.5
    for k in range(6):
        a = math.radians(90 + k * 60)
        for r in range(0, 8):
            x, y = c + math.cos(a)*r, c - math.sin(a)*r
            px(fg, round(x), round(y), W if r < 3 else C1)
        for r, s in ((4.5, 2.0),):                                           # nhánh phụ
            bx, by = c + math.cos(a)*r, c - math.sin(a)*r
            for sgn in (-1, 1):
                b = a + sgn * math.radians(50)
                for q in (1, 2):
                    px(fg, round(bx + math.cos(b)*q*s/2), round(by - math.sin(b)*q*s/2), C2)
    for x in (7, 8):
        for y in (7, 8): px(fg, x, y, W)
    return compose(aura((110,200,255), a_in=100), fg)

ICONS = {
    'dual-wield': dual_wield, 'holy-light': holy_light, 'dodge-roll': dodge_roll, 'arcane-storm': arcane_storm,
    'raise-shield': raise_shield, 'dragon-flame': dragon_flame, 'thunder-judgement': thunder,
    'rift-cut': rift_cut, 'frost-field': frost_field,
}

os.makedirs(OUT, exist_ok=True)
imgs = {}
for k, fn in ICONS.items():
    im = fn(); im.save(os.path.join(OUT, f'skill_{k}.png')); imgs[k] = im

# preview: mỗi icon đặt trong nút skill (frame+disc 26px) như trên HUD, phóng x6
disc = Image.open(os.path.join(GEN, 'skill_btn_disc.png')).convert('RGBA')
frame = Image.open(os.path.join(GEN, 'skill_btn_frame.png')).convert('RGBA')
cols = 5; cell = 30
sheet = Image.new('RGBA', (cols*cell, ((len(imgs)+cols-1)//cols)*cell), (45,32,58,255))
for i, (k, im) in enumerate(imgs.items()):
    b = Image.new('RGBA', (26,26), (0,0,0,0)); b.alpha_composite(disc); b.alpha_composite(im, (5,5)); b.alpha_composite(frame)
    sheet.alpha_composite(b, ((i%cols)*cell+2, (i//cols)*cell+2))
sheet.resize((sheet.width*6, sheet.height*6), Image.NEAREST).save(PREV)
print('ok', list(imgs))
