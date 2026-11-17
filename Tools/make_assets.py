# Regenerates the pictures in App/Assets (logo, banner, icons, drop zone, app icon).
# Optional: the finished pictures are already in the project. Needs Pillow (pip install pillow).
import math, random
from PIL import Image, ImageDraw, ImageFilter, ImageFont

random.seed(11)
A = "App/Assets/"   # run this script from the project root
CY, VI, PK, BG = (0, 220, 255), (150, 90, 255), (255, 90, 190), (14, 17, 32)

def grad(w, h, c1, c2, diag=True):
    img = Image.new("RGB", (w, h)); px = img.load()
    for y in range(h):
        for x in range(w):
            t = (x / w * 0.7 + y / h * 0.3) if diag else x / w
            px[x, y] = tuple(int(c1[i] + (c2[i] - c1[i]) * t) for i in range(3))
    return img

def font(size, bold=True):
    for p in ["/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf" if bold else "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"]:
        try: return ImageFont.truetype(p, size)
        except Exception: pass
    return ImageFont.load_default()

def glow(img, layer_fn, radius=8):
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0)); layer_fn(ImageDraw.Draw(layer))
    img.alpha_composite(layer.filter(ImageFilter.GaussianBlur(radius))); img.alpha_composite(layer)

def tree(d, cx, cy, spread, depth, col, lw):
    if depth == 0: return
    for sgn in (-1, 1):
        nx, ny = cx + sgn * spread, cy + spread * 1.1
        d.line([cx, cy, nx, ny], fill=col, width=lw); tree(d, nx, ny, spread * 0.52, depth - 1, col, lw)
    r = max(3, lw * 2); d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=col)

# ---- logo (rounded tile with a glowing Huffman tree)
S = 512
logo = Image.new("RGBA", (S, S), (0, 0, 0, 0))
tile = grad(S, S, VI, CY).convert("RGBA")
mask = Image.new("L", (S, S), 0); ImageDraw.Draw(mask).rounded_rectangle([8, 8, S - 8, S - 8], 110, fill=255)
logo.paste(tile, (0, 0), mask)
dark = Image.new("RGBA", (S, S), (10, 12, 28, 60)); dark.putalpha(mask.point(lambda v: 60 if v else 0)); logo.alpha_composite(dark)
glow(logo, lambda d: tree(d, S // 2, 120, 130, 3, (255, 255, 255, 255), 9), 10)
logo.save(A + "logo.png")

# ---- banner (wide animated-looking backdrop; the app also animates on top of it)
W, H = 1400, 170
ban = grad(W, H, (20, 24, 60), (60, 22, 110)).convert("RGBA")
d = ImageDraw.Draw(ban, "RGBA")
for _ in range(90):                                   # bit streams
    x, y = random.randint(0, W), random.randint(0, H)
    d.text((x, y), random.choice("01"), font=font(random.randint(10, 22), False),
           fill=(0, 220, 255, random.randint(25, 90)))
glow(ban, lambda g: tree(g, W - 230, 14, 95, 4, (0, 220, 255, 170), 3), 5)
glow(ban, lambda g: tree(g, W - 520, 30, 70, 3, (255, 90, 190, 120), 2), 4)
ban.convert("RGB").save(A + "banner.jpg", quality=92)

# ---- icons 64x64
def icon(name, fn, c1, c2):
    s = 128; im = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    m = Image.new("L", (s, s), 0); ImageDraw.Draw(m).rounded_rectangle([4, 4, s - 4, s - 4], 30, fill=255)
    im.paste(grad(s, s, c1, c2).convert("RGBA"), (0, 0), m)
    d = ImageDraw.Draw(im); fn(d, s); im = im.resize((64, 64), Image.LANCZOS); im.save(A + name)

W_ = (255, 255, 255, 255)
def open_i(d, s):  d.polygon([(30, 44), (56, 44), (62, 54), (98, 54), (98, 94), (30, 94)], outline=W_, width=7)
def comp_i(d, s):
    for a, b in [((26, 26), (54, 54)), ((102, 26), (74, 54)), ((26, 102), (54, 74)), ((102, 102), (74, 74))]:
        d.line([a, b], fill=W_, width=9)
    d.rectangle([54, 54, 74, 74], fill=W_)
def decomp_i(d, s):
    for a, b in [((54, 54), (26, 26)), ((74, 54), (102, 26)), ((54, 74), (26, 102)), ((74, 74), (102, 102))]:
        d.line([a, b], fill=W_, width=9)
    d.ellipse([54, 54, 74, 74], fill=W_)
def save_i(d, s):
    d.line([64, 26, 64, 82], fill=W_, width=10); d.polygon([(42, 66), (86, 66), (64, 92)], fill=W_)
    d.line([30, 104, 98, 104], fill=W_, width=9)
def sample_i(d, s):
    d.rounded_rectangle([34, 24, 94, 104], 8, outline=W_, width=7)
    for y in (46, 62, 78): d.line([46, y, 82, y], fill=W_, width=6)
icon("icon_open.png", open_i, (0, 150, 255), (0, 220, 200))
icon("icon_compress.png", comp_i, (150, 90, 255), (255, 90, 190))
icon("icon_decompress.png", decomp_i, (255, 140, 60), (255, 70, 110))
icon("icon_save.png", save_i, (40, 200, 120), (0, 220, 255))
icon("icon_sample.png", sample_i, (110, 110, 160), (60, 70, 130))

# ---- drop-zone illustration (cloud + arrow)
dz = Image.new("RGBA", (220, 160), (0, 0, 0, 0))
glow(dz, lambda g: (g.ellipse([40, 50, 120, 120], fill=(0, 220, 255, 90)), g.ellipse([80, 30, 170, 120], fill=(150, 90, 255, 90)),
                    g.rectangle([60, 85, 150, 120], fill=(100, 150, 255, 90)),
                    g.line([110, 135, 110, 70], fill=(255, 255, 255, 230), width=9),
                    g.polygon([(85, 90), (135, 90), (110, 58)], fill=(255, 255, 255, 230))), 6)
dz.save(A + "dropzone.png")

# ---- sample files
txt = ("Data compression reduces the number of bits needed to store information. "
       "Huffman coding gives frequent symbols short codes and rare symbols long codes. ") * 1
words = "the of and to in is that for it as was with be by on not he this are or his from at which but have an had they you were their one all we can her has there been if more when will would who so no".split()
random.seed(3)
body = []
for _ in range(900):
    body.append(" ".join(random.choice(words) for _ in range(random.randint(8, 16))).capitalize() + ".")
open("Samples/sample_text.txt", "w").write(txt + "\n\n" + "\n".join(body))
open("Samples/sample_dna.txt", "w").write("".join(random.choices("ACGT", weights=[30, 20, 20, 30], k=60000)))

img = Image.new("RGB", (320, 200), (255, 255, 255)); dd = ImageDraw.Draw(img)   # flat colours compress well
for i in range(8):
    dd.rectangle([i * 40, 0, i * 40 + 40, 100], fill=[(255,255,255),(255,255,0),(0,255,255),(0,255,0),(255,0,255),(255,0,0),(0,0,255),(0,0,0)][i])
for y in range(100, 200):
    dd.line([0, y, 320, y], fill=(int(255 * (y - 100) / 100), 40, 160))
img.save("Samples/sample_image.bmp")
print("ok")
