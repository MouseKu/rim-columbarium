"""Render per-flower previews using the compact 32-niche runtime geometry."""
from pathlib import Path
import re
import xml.etree.ElementTree as ET

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
NAMES = (
    ('Melee', 'Gladiolus'), ('Shooting', 'Gentian'),
    ('Construction', 'Forget-me-not'), ('Mining', 'Black hellebore'),
    ('Cooking', 'Alstroemeria'), ('Plants', 'Clover'),
    ('Animals', 'Sunflower'), ('Crafting', 'Cornflower'),
    ('Artistic', 'Dahlia'), ('Medicine', 'Lily'),
    ('Social', 'Rose'), ('Intellectual', 'Iris'),
)
PANEL = (700, 500)
TILE_SCALE = 140
OUT = ROOT / 'Preview' / 'CompactThirtyTwoFlowers'
OUT.mkdir(parents=True, exist_ok=True)

definition = next(d for d in ET.parse(ROOT / 'Defs/ThingDefs/Columbarium_32.xml').getroot()
                  if d.findtext('defName') == 'ColumbariumThirtyTwoCompact')
plane = tuple(map(float, definition.findtext('graphicData/drawSize').strip('()').split(',')))
layout_src = (ROOT / 'Source/Columbarium/ThirtyTwoLayout.cs').read_text(encoding='utf-8')
bounds_src = layout_src.split('Rect[] Bounds = {', 1)[1].split('};', 1)[0]
bounds = [tuple(map(float, v)) for v in re.findall(
    r'new Rect\(([\d.]+)f, ([\d.]+)f, ([\d.]+)f, ([\d.]+)f\)', bounds_src)]
flower_src = (ROOT / 'Source/Columbarium/CompactThirtyTwoFlowerLayout.cs').read_text(encoding='utf-8')
ratios = [float(v) for v in re.findall(r'([\d.]+)f', flower_src.split('Ratios = {', 1)[1].split('}', 1)[0])]
paddings = [float(v) for v in re.findall(r'([\d.]+)f', flower_src.split('RightPaddings = {', 1)[1].split('}', 1)[0])]
margin = float(re.search(r'MarginRatio = ([\d.]+)f', flower_src)[1])
spill = float(re.search(r'RightSpillRatio = ([\d.]+)f', flower_src)[1])
assert len(bounds) == 32 and len(ratios) == len(paddings) == 12

things = ROOT / 'Textures' / 'Things'
front = Image.open(things / 'Columbarium_Art32OutlineCompact_south.png').convert('RGBA')
opened = Image.open(things / 'Columbarium_ArtTallNicheOpen.png').convert('RGBA')
closed = Image.open(things / 'Columbarium_Compact_ArtTallNicheClosed.png').convert('RGBA')
flowers = [Image.open(things / 'Flowers' / f'{skill}.png').convert('RGBA') for skill, _ in NAMES]
font_path = Path('C:/Windows/Fonts/arial.ttf')
font = ImageFont.truetype(str(font_path), 16) if font_path.exists() else ImageFont.load_default()
art_size = (round(plane[0] * TILE_SCALE), round(plane[1] * TILE_SCALE))
north_offset = float(re.search(r'CompactNorthOffset = ([\d.]+)f', layout_src)[1])


def render(flower_index):
    panel = Image.new('RGBA', PANEL, '#34383a')
    center = (PANEL[0] // 2, PANEL[1] // 2 - 12)
    footprint = (center[0] - 2 * TILE_SCALE, center[1] - TILE_SCALE,
                 center[0] + 2 * TILE_SCALE, center[1] + TILE_SCALE)
    draw = ImageDraw.Draw(panel)
    draw.rectangle(footprint, fill='#455459')
    art = Image.new('RGBA', art_size, (0, 0, 0, 0))
    art.alpha_composite(front.resize(art_size, Image.Resampling.LANCZOS))
    geometry = []
    for i, (x, y, w, h) in enumerate(bounds):
        left, top = round(x / 1774 * art_size[0]), round(y / 887 * art_size[1])
        right, bottom = round((x+w) / 1774 * art_size[0]), round((y+h) / 887 * art_size[1])
        art.alpha_composite((closed if i == 0 else opened).resize(
            (right-left, bottom-top), Image.Resampling.LANCZOS), (left, top))
        geometry.append((left, top, right, bottom))
    left, top, right, bottom = geometry[0]
    width, height = right-left, bottom-top
    size = round(height * ratios[flower_index])
    x = right - size - round(height * margin) + round(size * paddings[flower_index]) + round(width * spill)
    y = bottom - size - round(height * margin)
    art.alpha_composite(flowers[flower_index].resize((size, size), Image.Resampling.LANCZOS), (x, y))
    art_pos = (round(center[0] - art_size[0]/2),
               round(center[1] - art_size[1]/2 - north_offset*TILE_SCALE))
    panel.alpha_composite(art, art_pos)
    draw = ImageDraw.Draw(panel)
    l, t, r, b = footprint
    for px in range(l, r, 12):
        draw.line((px,t,min(px+7,r),t), fill='#70dbe3', width=2)
        draw.line((px,b,min(px+7,r),b), fill='#70dbe3', width=2)
    for py in range(t, b, 12):
        draw.line((l,py,l,min(py+7,b)), fill='#70dbe3', width=2)
        draw.line((r,py,r,min(py+7,b)), fill='#70dbe3', width=2)
    skill, name = NAMES[flower_index]
    draw.text((12, 10), f'{skill} / {name}', fill='white', font=font)
    draw.text((12, PANEL[1]-25), f'Flower size: {ratios[flower_index]:.3f} x niche height',
              fill='#d7faff', font=font)
    return panel.convert('RGB')


panels = []
for i, (skill, _) in enumerate(NAMES):
    image = render(i)
    image.save(OUT / f'{i+1:02d}_{skill}.png')
    panels.append(image)
sheet = Image.new('RGB', (3*PANEL[0], 4*PANEL[1]), '#202020')
for i, image in enumerate(panels):
    sheet.paste(image, ((i % 3)*PANEL[0], (i//3)*PANEL[1]))
sheet.save(ROOT / 'Preview' / 'compact_thirtytwo_flowers_all.png')
print(ROOT / 'Preview' / 'compact_thirtytwo_flowers_all.png')
print(OUT)


def render_compact_state(occupied, state):
    scale = 256
    canvas_size = (1536, 1024)
    center = (canvas_size[0] // 2, canvas_size[1] // 2)
    footprint = (center[0] - 2 * scale, center[1] - scale,
                 center[0] + 2 * scale, center[1] + scale)
    size = (round(plane[0] * scale), round(plane[1] * scale))
    art = front.resize(size, Image.Resampling.LANCZOS)
    geometry = []
    for i, (x, y, w, h) in enumerate(bounds):
        left, top = round(x / 1774 * size[0]), round(y / 887 * size[1])
        right, bottom = round((x + w) / 1774 * size[0]), round((y + h) / 887 * size[1])
        door = closed if i < occupied else opened
        art.alpha_composite(door.resize((right - left, bottom - top), Image.Resampling.LANCZOS),
                            (left, top))
        geometry.append((left, top, right, bottom))
    for i in range(occupied):
        left, top, right, bottom = geometry[i]
        width, height = right - left, bottom - top
        flower_index = i % len(flowers)
        flower_size = round(height * ratios[flower_index])
        flower_x = right - flower_size - round(height * margin) + round(flower_size * paddings[flower_index]) + round(width * spill)
        flower_y = bottom - flower_size - round(height * margin)
        flower = flowers[flower_index].resize((flower_size, flower_size), Image.Resampling.LANCZOS)
        art.alpha_composite(flower, (flower_x, flower_y))

    preview = Image.new('RGBA', canvas_size, '#34383a')
    draw = ImageDraw.Draw(preview)
    draw.rectangle(footprint, fill='#455459')
    for x in range(0, canvas_size[0], scale):
        draw.line((x, 0, x, canvas_size[1]), fill='#687175', width=2)
    for y in range(0, canvas_size[1], scale):
        draw.line((0, y, canvas_size[0], y), fill='#687175', width=2)
    position = (round(center[0] - size[0] / 2),
                round(center[1] - size[1] / 2 - north_offset * scale))
    preview.alpha_composite(art, position)
    draw = ImageDraw.Draw(preview)
    left, top, right, bottom = footprint
    for x in range(left, right, 20):
        for y in (top, bottom):
            draw.line((x, y, min(x + 11, right), y), fill='#70dbe3', width=4)
    for y in range(top, bottom, 20):
        for x in (left, right):
            draw.line((x, y, x, min(y + 11, bottom)), fill='#70dbe3', width=4)
    draw.text((40, canvas_size[1] - 65),
              f'32 niches | footprint 4 wide x 2 high | {occupied}/32 occupied', fill='white')
    path = ROOT / 'Preview' / f'thirtytwo_compact_{state}.png'
    preview.convert('RGB').save(path)
    print(path)


render_compact_state(24, 'mixed')
render_compact_state(32, 'full')
