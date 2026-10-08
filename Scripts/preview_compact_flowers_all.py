"""Render one preview per flower using the compact eight-niche layout."""
from pathlib import Path
import re
import xml.etree.ElementTree as ET

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
THINGS = ROOT / 'Textures' / 'Things'
OUT_DIR = ROOT / 'Preview' / 'CompactFlowers'
OUT_DIR.mkdir(parents=True, exist_ok=True)

NAMES = (
    ('Melee', 'Gladiolus'), ('Shooting', 'Gentian'),
    ('Construction', 'Forget-me-not'), ('Mining', 'Black hellebore'),
    ('Cooking', 'Alstroemeria'), ('Plants', 'Clover'),
    ('Animals', 'Sunflower'), ('Crafting', 'Cornflower'),
    ('Artistic', 'Dahlia'), ('Medicine', 'Lily'),
    ('Social', 'Rose'), ('Intellectual', 'Iris'),
)
TILE_SCALE = 192
PANEL_SIZE = (440, 340)

definition = next(d for d in ET.parse(ROOT / 'Defs/ThingDefs/Columbarium_Things.xml').getroot()
                  if d.findtext('defName') == 'ColumbariumModularCompactTall')
plane = tuple(map(float, definition.findtext('graphicData/drawSize').strip('()').split(',')))
runtime = (ROOT / 'Source/Columbarium/Building_Columbarium.cs').read_text(encoding='utf-8')
bounds_text = runtime.split('Rect[] TallNicheBounds = {', 1)[1].split('};', 1)[0]
bounds = [tuple(map(float, v)) for v in re.findall(
    r'new Rect\((\d+)f, (\d+)f, (\d+)f, (\d+)f\)', bounds_text)]
layout = (ROOT / 'Source/Columbarium/CompactNicheFlowerLayout.cs').read_text(encoding='utf-8')
ratios = [float(v) for v in re.findall(r'([\d.]+)f', layout.split('Ratios = {', 1)[1].split('}', 1)[0])]
paddings = [float(v) for v in re.findall(r'([\d.]+)f', layout.split('RightPaddings = {', 1)[1].split('}', 1)[0])]
margin_ratio = float(re.search(r'MarginRatio = ([\d.]+)f', layout)[1])
spill_ratio = float(re.search(r'RightSpillRatio = ([\d.]+)f', layout)[1])
assert len(bounds) == len(ratios) == len(paddings) == 8 or len(ratios) == len(paddings) == 12

front = Image.open(THINGS / 'Columbarium_ArtTallCompact_south.png').convert('RGBA')
opened = Image.open(THINGS / 'Columbarium_ArtTallNicheOpen.png').convert('RGBA')
closed = Image.open(THINGS / 'Columbarium_Compact_ArtTallNicheClosed.png').convert('RGBA')
flowers = [Image.open(THINGS / 'Flowers' / f'{skill}.png').convert('RGBA') for skill, _ in NAMES]
font_path = Path('C:/Windows/Fonts/arial.ttf')
font = ImageFont.truetype(str(font_path), 15) if font_path.exists() else ImageFont.load_default()
canvas_size = (round(plane[0] * TILE_SCALE), round(plane[1] * TILE_SCALE))
north_offset = float(re.search(r'CompactNorthOffset = ([\d.]+)f', runtime)[1])


def draw_preview(index):
    panel = Image.new('RGBA', PANEL_SIZE, '#34383a')
    center = (PANEL_SIZE[0] // 2, PANEL_SIZE[1] // 2 - 12)
    footprint = (center[0] - TILE_SCALE, center[1] - TILE_SCALE // 2,
                 center[0] + TILE_SCALE, center[1] + TILE_SCALE // 2)
    draw = ImageDraw.Draw(panel)
    draw.rectangle(footprint, fill='#455459')
    for x in range(0, PANEL_SIZE[0] + 1, TILE_SCALE):
        draw.line((x, 0, x, PANEL_SIZE[1]), fill='#687175', width=1)
    for y in range(0, PANEL_SIZE[1] + 1, TILE_SCALE):
        draw.line((0, y, PANEL_SIZE[0], y), fill='#687175', width=1)

    art_size = canvas_size
    art_pos = (round(center[0] - art_size[0] / 2),
               round(center[1] - art_size[1] / 2 - north_offset * TILE_SCALE))
    art = Image.new('RGBA', art_size, (0, 0, 0, 0))
    art.alpha_composite(front.resize(art_size, Image.Resampling.LANCZOS))
    door_geometry = []
    for slot, (x, y, width, height) in enumerate(bounds):
        left, top = round(x / 1774 * art_size[0]), round(y / 887 * art_size[1])
        right = round((x + width) / 1774 * art_size[0])
        bottom = round((y + height) / 887 * art_size[1])
        source = closed if slot == 0 else opened
        art.alpha_composite(source.resize((right - left, bottom - top), Image.Resampling.LANCZOS),
                            (left, top))
        door_geometry.append((left, top, right, bottom))

    left, top, right, bottom = door_geometry[0]
    width, height = right - left, bottom - top
    size = round(height * ratios[index])
    margin = round(height * margin_ratio)
    x = right - size - margin + round(size * paddings[index]) + round(width * spill_ratio)
    y = bottom - size - margin
    flower = flowers[index].resize((size, size), Image.Resampling.LANCZOS)
    art.alpha_composite(flower, (x, y))
    panel.alpha_composite(art, art_pos)

    draw = ImageDraw.Draw(panel)
    # Dashes show the actual two-by-one footprint.
    l, t, r, b = footprint
    for px in range(l, r, 12):
        draw.line((px, t, min(px + 7, r), t), fill='#70dbe3', width=2)
        draw.line((px, b, min(px + 7, r), b), fill='#70dbe3', width=2)
    for py in range(t, b, 12):
        draw.line((l, py, l, min(py + 7, b)), fill='#70dbe3', width=2)
        draw.line((r, py, r, min(py + 7, b)), fill='#70dbe3', width=2)
    skill, flower_name = NAMES[index]
    draw.text((12, 10), f'{skill} / {flower_name}', fill='white', font=font)
    draw.text((12, PANEL_SIZE[1] - 22), f'Flower size: {ratios[index]:.3f} x niche height',
              fill='#d7faff', font=font)
    return panel.convert('RGB')


panels = []
for index, (skill, _) in enumerate(NAMES):
    panel = draw_preview(index)
    panel.save(OUT_DIR / f'{index + 1:02d}_{skill}.png')
    panels.append(panel)

columns, rows = 3, 4
sheet = Image.new('RGB', (columns * PANEL_SIZE[0], rows * PANEL_SIZE[1]), '#202020')
for index, panel in enumerate(panels):
    sheet.paste(panel, ((index % columns) * PANEL_SIZE[0], (index // columns) * PANEL_SIZE[1]))
sheet.save(ROOT / 'Preview' / 'compact_flowers_all.png')
print(ROOT / 'Preview' / 'compact_flowers_all.png')
print(OUT_DIR)
