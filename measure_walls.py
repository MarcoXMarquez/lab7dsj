from PIL import Image

img = Image.open('pdf_images/page_6_2_Image57.png.png')
# Crop the scene view area where the white arena is
# Image size is (1352, 788)
# Let's find white pixels (255, 255, 255)
w, h = img.size
white_pixels = []
for x in range(w):
    for y in range(h):
        r, g, b = img.getpixel((x, y))[:3]
        if r > 240 and g > 240 and b > 240 and x < 980 and y < 550 and y > 60:
            white_pixels.append((x, y))

min_x = min(p[0] for p in white_pixels)
max_x = max(p[0] for p in white_pixels)
min_y = min(p[1] for p in white_pixels)
max_y = max(p[1] for p in white_pixels)

print(f'Arena pixel bounds in screenshot: X [{min_x}, {max_x}] (W={max_x-min_x}), Y [{min_y}, {max_y}] (H={max_y-min_y})')

# In world units, the grid graph is 36 x 21
# Ratio W/H in pixels: (max_x - min_x) / (max_y - min_y) = 612 / 357 = 1.714
# 36 / 21 = 1.714! EXACT MATCH!
# Pixels per unit in this screenshot: (max_x - min_x) / 36 = 17.0 pixels / unit!

# Center of arena in screenshot:
cx = (min_x + max_x) / 2.0
cy = (min_y + max_y) / 2.0
ppu = (max_x - min_x) / 36.0

print(f'Center: ({cx}, {cy}), PPU: {ppu}')

# Now find all white connected components (walls) inside the arena!
# Inner walls are white pixels with margin from outer border
inner_walls = []
# Find upper horizontal wall:
# In screenshot, Y is around cy - 5 * ppu (in image Y is inverted, so cy - 5*ppu is above center)
# Let's sample along horizontal line Y = cy - 5*ppu:
for test_y_unit in [5, -5]:
    test_py = int(cy - test_y_unit * ppu)
    xs = [x for x in range(min_x + 30, max_x - 30) if img.getpixel((x, test_py))[:3] == (255, 255, 255)]
    if xs:
        print(f'Horizontal wall at Y={test_y_unit}: X from {(min(xs)-cx)/ppu:.2f} to {(max(xs)-cx)/ppu:.2f}, width={(max(xs)-min(xs))/ppu:.2f}')

for test_x_unit in [-10, 10]:
    test_px = int(cx + test_x_unit * ppu)
    ys = [y for y in range(min_y + 30, max_y - 30) if img.getpixel((test_px, y))[:3] == (255, 255, 255)]
    if ys:
        print(f'Vertical wall at X={test_x_unit}: Y from {(cy-max(ys))/ppu:.2f} to {(cy-min(ys))/ppu:.2f}, height={(max(ys)-min(ys))/ppu:.2f}')
