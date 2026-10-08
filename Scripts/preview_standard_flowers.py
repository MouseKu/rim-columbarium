"""Render per-flower previews for the standard eight-niche columbarium."""
from pathlib import Path
import re
import xml.etree.ElementTree as ET

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
THINGS = ROOT / 'Textures' / 'Things'
OUT_DIR = ROOT / 'Preview' / 'StandardFlowers'
OUT_DIR.mkdir(parents=True, exist_ok=True)
NAMES = (
    ('Melee', 'Gladiolus'), ('Shooting', 'Gentian'),
    ('Construction', 'Forget-me-not'), ('Mining', 'Black hellebore'),
    ('Cooking', 'Alstroemeria'), ('Plants', 'Clover'),
    ('Animals', 'Sunflower'), ('Crafting', 'Cornflower'),
    ('Artistic', 'Dahlia'), ('Medicine', 'Lily'),
    ('Social', 'Rose'), ('Intellectual', 'Iris'),
)
PANEL = (500, 400)
TILE_SCALE = 96

definition = next(d for d in ET.parse(ROOT / 'Defs/ThingDefs/Columbarium_Things.xml').getroot()
                  if d.findtext('defName') == 'ColumbariumModularTall')
plane = tuple(map(float, definition.findtext('graphicData/drawSize').strip('()').split(',')))
runtime = (ROOT / 'Source/Columbarium/Building_Columbarium.cs').read_text(encoding='utf-8')
layout_src = runtime.split('Rect[] TallNicheBounds = {', 1)[1].split('};', 1)[0]
bounds = [tuple(map(float, values)) for values in re.findall(
    r'new Rect\(([\d.]+)f, ([\d.]+)f, ([\d.]+)f, ([\d.]+)f\)', layout_src)]
layout = (ROOT / 'Source/Columbarium/NicheFlowerLayout.cs').read_text(encoding='utf-8')
ratios = [float(v) for v in re.findall(r'([\d.]+)f', layout.split('Ratios = {', 1)[1].split('}', 1)[0])]
paddings = [float(v) for v in re.findall(r'([\d.]+)f', layout.split('RightPaddings = {', 1)[1].split('}', 1)[0])]
margin = float(re.search(r'MarginRatio = ([\d.]+)f', layout)[1])
spill = float(re.search(r'RightSpillRatio = ([\d.]+)f', layout)[1])
assert len(bounds) == 8 and len(ratios) == len(paddings) == 12

front = Image.open(THINGS / 'Columbarium_ArtTall_south.png').convert('RGBA')
opened = Image.open(THINGS / 'Columbarium_ArtTallNicheOpen.png').convert('RGBA')
closed = Image.open(THINGS / 'Columbarium_ArtTallNicheClosed.png').convert('RGBA')
flowers = [Image.open(THINGS / 'Flowers' / f'{skill}.png').convert('RGBA') for skill, _ in NAMES]
font_path = Path('C:/Windows/Fonts/arial.ttf')
font = ImageFont.truetype(str(font_path), 15) if font_path.exists() else ImageFont.load_default()
art_size = (round(plane[0] * TILE_SCALE), round(plane[1] * TILE_SCALE))
large_offset = float(re.search(r'LargeNorthOffset = ([\d.]+)f', runtime)[1])


def render(index):
    panel = Image.new('RGBA', PANEL, '#34383a')
    center = (PANEL[0] // 2, PANEL[1] // 2 - 8)
    footprint = (center[0] - 2*TILE_SCALE, center[1] - TILE_SCALE,
                 center[0] + 2*TILE_SCALE, center[1] + TILE_SCALE)
    draw = ImageDraw.Draw(panel)
    draw.rectangle(footprint, fill='#455459')

    art = Image.new('RGBA', art_size, (0, 0, 0, 0))
    art.alpha_composite(front.resize(art_size, Image.Resampling.LANCZOS))
    geometry = []
    for slot, (x, y, w, h) in enumerate(bounds):
        left, top = round(x/1774*art_size[0]), round(y/887*art_size[1])
        right, bottom = round((x+w)/1774*art_size[0]), round((y+h)/887*art_size[1])
        art.alpha_composite((closed if slot == 0 else opened).resize(
            (right-left, bottom-top), Image.Resampling.LANCZOS), (left, top))
        geometry.append((left, top, right, bottom))

    left, top, right, bottom = geometry[0]
    width, height = right-left, bottom-top
    size = round(height * ratios[index])
    x = right-size-round(height*margin)+round(size*paddings[index])+round(width*spill)
    y = bottom-size-round(height*margin)
    art.alpha_composite(flowers[index].resize((size,size), Image.Resampling.LANCZOS), (x,y))
    art_pos = (round(center[0]-art_size[0]/2),
               round(center[1]-art_size[1]/2-large_offset*TILE_SCALE))
    panel.alpha_composite(art, art_pos)

    draw = ImageDraw.Draw(panel)
    l,t,r,b = footprint
    for px in range(l,r,12):
        draw.line((px,t,min(px+7,r),t), fill='#70dbe3', width=2)
        draw.line((px,b,min(px+7,r),b), fill='#70dbe3', width=2)
    for py in range(t,b,12):
        draw.line((l,py,l,min(py+7,b)), fill='#70dbe3', width=2)
        draw.line((r,py,r,min(py+7,b)), fill='#70dbe3', width=2)
    skill, name = NAMES[index]
    draw.text((12,10), f'{skill} / {name}', fill='white', font=font)
    draw.text((12,PANEL[1]-22), f'Flower size {ratios[index]:.3f} | right overlap {spill:.0%}',
              fill='#d7faff', font=font)
    return panel.convert('RGB')


panels = []
for i, (skill, _) in enumerate(NAMES):
    image = render(i)
    image.save(OUT_DIR / f'{i+1:02d}_{skill}.png')
    panels.append(image)
sheet = Image.new('RGB', (3*PANEL[0], 4*PANEL[1]), '#202020')
for i, image in enumerate(panels):
    sheet.paste(image, ((i%3)*PANEL[0], (i//3)*PANEL[1]))
path = ROOT / 'Preview' / 'standard_flowers_all.png'
sheet.save(path)
print(path)
print(OUT_DIR)
