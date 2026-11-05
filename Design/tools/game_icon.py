import sys, os
from PIL import Image
# Usage: python Design/tools/game_icon.py Assets/Project/Art/Sprites <out_dir>
ART = sys.argv[1]; OUT = sys.argv[2]
def hx(s): s=s.lstrip('#'); return tuple(int(s[i:i+2],16) for i in (0,2,4))+(255,)
OUTL=hx('1a1932')

# --- helmet from portrait (flood-remove bg B/G) ---
ks=Image.open(os.path.join(ART,'Characters/Knight/Blue Knight set Sprite-sheet.png')).convert('RGBA')
reg=ks.crop((4,100,28,124)).copy()
bg={hx('1a1932')[:3],hx('0069aa')[:3]}
W,H=reg.size; px=reg.load()
stack=[(x,y) for x in range(W) for y in (0,H-1)]+[(x,y) for y in range(H) for x in (0,W-1)]
seen=set()
while stack:
    x,y=stack.pop()
    if (x,y) in seen or not(0<=x<W and 0<=y<H): continue
    seen.add((x,y))
    if px[x,y][:3] in bg and px[x,y][3]>0:
        px[x,y]=(0,0,0,0); stack+= [(x+1,y),(x-1,y),(x,y+1),(x,y-1)]
helm=reg.crop(reg.getbbox())
def outline(im,col):
    w,h=im.size; o=Image.new('RGBA',(w+2,h+2),(0,0,0,0)); o.paste(im,(1,1),im); p=o.load(); src=o.copy().load()
    for y in range(h+2):
        for x in range(w+2):
            if src[x,y][3]==0 and any(0<=x+dx<w+2 and 0<=y+dy<h+2 and src[x+dx,y+dy][3]>0 for dx,dy in((1,0),(-1,0),(0,1),(0,-1))):
                p[x,y]=col
    return o
helm=outline(helm,OUTL)
helm.save(os.path.join(OUT,'part_helm.png'))
print('helm',helm.size)

sw=Image.open(os.path.join(ART,'Items/Weapon/Sword/File.png')).convert('RGBA')
def sword(c,r): return sw.crop((c*32,r*32,c*32+32,r*32+32))

def rounded_mask(n,cut):
    m=Image.new('L',(n,n),255); p=m.load()
    for y in range(n):
        for x in range(n):
            dx=min(x,n-1-x); dy=min(y,n-1-y)
            if dx+dy<cut: p[x,y]=0
    return m

def compose(swA,swB,glow=True,name='A',N=48):
    im=Image.new('RGBA',(N,N),(0,0,0,0))
    base=Image.new('RGBA',(N,N),hx('1b1420'))
    p=base.load()
    # stepped radial glow behind helmet
    cx,cy=(N-1)/2,N*0.46
    ring=[(8,hx('f2b33d')),(11,hx('c9772e')),(14.5,hx('6b3f1e')),(18.5,hx('3a2c3f')),(23,hx('2a1f2e'))]
    for y in range(N):
        for x in range(N):
            d=((x-cx)**2+(y-cy)**2)**.5
            for r,c in ring:
                if d<r: p[x,y]=c; break
    # brick mortar hint in the dark corners
    for y in range(N):
        for x in range(N):
            d=((x-cx)**2+(y-cy)**2)**.5
            if d>=23:
                row=y//4; off=(row%2)*4
                if y%4==3 or (x+off)%8==7: p[x,y]=hx('2a1f2e')
    im.paste(base,(0,0),rounded_mask(N,4))
    a=swA; b=swB.transpose(Image.FLIP_LEFT_RIGHT)
    im.alpha_composite(a,(3,3)); im.alpha_composite(b,(N-35,3))
    hx0=(N-helm.width)//2; hy0=N-helm.height-5
    im.alpha_composite(helm,(hx0,hy0))
    # frame: 1px dark outline on rounded edge
    m=rounded_mask(N,4).load(); q=im.load()
    for y in range(N):
        for x in range(N):
            if m[x,y] and any(not(0<=x+dx<N and 0<=y+dy<N) or not m[x+dx,y+dy] for dx,dy in((1,0),(-1,0),(0,1),(0,-1))):
                q[x,y]=hx('0e071b')
    return im

variants={'A':compose(sword(0,4),sword(0,4)),'B':compose(sword(0,3),sword(0,3)),'C':compose(sword(4,1),sword(4,1))}
for k,v in variants.items():
    v.save(os.path.join(OUT,f'icon_{k}_48.png'))
    v.resize((240,240),Image.NEAREST).save(os.path.join(OUT,f'icon_{k}_256.png'))
sheet=Image.new('RGBA',(3*272,272),hx('efe6dc'))
for i,k in enumerate('ABC'): sheet.alpha_composite(variants[k].resize((240,240),0),(i*272+16,16))
sheet.save(os.path.join(OUT,'preview.png'))

# ---- small sizes: no swords, tighter glow ----
def compose_small(N=32):
    im=Image.new('RGBA',(N,N),(0,0,0,0)); base=Image.new('RGBA',(N,N),hx('1b1420')); p=base.load()
    cx,cy=(N-1)/2,N*0.45
    ring=[(6,hx('f2b33d')),(9,hx('c9772e')),(12,hx('6b3f1e')),(15.5,hx('3a2c3f'))]
    for y in range(N):
        for x in range(N):
            d=((x-cx)**2+(y-cy)**2)**.5
            for r,c in ring:
                if d<r: p[x,y]=c; break
    im.paste(base,(0,0),rounded_mask(N,3))
    im.alpha_composite(helm,((N-helm.width)//2,N-helm.height-2))
    m=rounded_mask(N,3).load(); q=im.load()
    for y in range(N):
        for x in range(N):
            if m[x,y] and any(not(0<=x+dx<N and 0<=y+dy<N) or not m[x+dx,y+dy] for dx,dy in((1,0),(-1,0),(0,1),(0,-1))):
                q[x,y]=hx('0e071b')
    return im
s32=compose_small(32); s32.save(os.path.join(OUT,'icon_small_32.png'))
s16=s32.resize((16,16),Image.NEAREST); s16.save(os.path.join(OUT,'icon_small_16.png'))
pv=Image.new('RGBA',(400,200),hx('efe6dc'))
pv.alpha_composite(s32.resize((160,160),0),(16,20)); pv.alpha_composite(s16.resize((160,160),0),(208,20))
pv.save(os.path.join(OUT,'preview_small.png'))
