"""Render both eight-niche footprints and export transparent sample artwork."""
from pathlib import Path
import argparse
import json
import xml.etree.ElementTree as ET
import re

from PIL import Image, ImageDraw, ImageFont
from niche_art import CLOSED_SOURCE


ROOT = Path(__file__).resolve().parents[1]
THINGS = ROOT / "Textures" / "Things"
PREVIEW = ROOT / "Preview"
SAMPLE = PREVIEW / "Sample"
PREVIEW.mkdir(exist_ok=True)
SAMPLE.mkdir(exist_ok=True)

SCALE = 256  # pixels per world tile
runtime = (ROOT / 'Source/Columbarium/Building_Columbarium.cs').read_text(encoding='utf-8')
layout = runtime.split('Rect[] TallNicheBounds = {', 1)[1].split('};', 1)[0]
NICHE_BOUNDS = [tuple(map(float, values)) for values in re.findall(
    r'new Rect\((\d+)f, (\d+)f, (\d+)f, (\d+)f\)', layout)]
assert len(NICHE_BOUNDS) == 8
flower_layout = (ROOT / 'Source/Columbarium/NicheFlowerLayout.cs').read_text(encoding='utf-8')
FLOWER_RATIOS = [float(v) for v in re.findall(r'([\d.]+)f',
    flower_layout.split('Ratios = {', 1)[1].split('}', 1)[0])]
FLOWER_MARGIN = float(re.search(r'MarginRatio = ([\d.]+)f', flower_layout)[1])
FLOWER_RIGHT_PADDING = [float(v) for v in re.findall(r'([\d.]+)f',
    flower_layout.split('RightPaddings = {', 1)[1].split('}', 1)[0])]
compact_flower_layout = (ROOT / 'Source/Columbarium/CompactNicheFlowerLayout.cs').read_text(encoding='utf-8')
COMPACT_FLOWER_RATIOS = [float(v) for v in re.findall(r'([\d.]+)f',
    compact_flower_layout.split('Ratios = {', 1)[1].split('}', 1)[0])]
COMPACT_FLOWER_MARGIN = float(re.search(r'MarginRatio = ([\d.]+)f', compact_flower_layout)[1])
COMPACT_FLOWER_RIGHT_PADDING = [float(v) for v in re.findall(r'([\d.]+)f',
    compact_flower_layout.split('RightPaddings = {', 1)[1].split('}', 1)[0])]
COMPACT_FLOWER_SPILL = float(re.search(r'RightSpillRatio = ([\d.]+)f', compact_flower_layout)[1])
FLOWER_NAMES = ("Melee", "Shooting", "Construction", "Mining",
                "Cooking", "Plants", "Animals", "Crafting")


def load(name, size):
    return Image.open(THINGS / f"{name}.png").convert("RGBA").resize(size, Image.Resampling.LANCZOS)


open_source = Image.open(THINGS / "Columbarium_ArtTallNicheOpen.png").convert("RGBA")
closed_source = Image.open(THINGS / "Columbarium_ArtTallNicheClosed.png").convert("RGBA")
compact_closed_source = Image.open(THINGS / "Columbarium_Compact_ArtTallNicheClosed.png").convert("RGBA")
flowers = {name: Image.open(THINGS / "Flowers" / f"{name}.png").convert("RGBA")
           for name in FLOWER_NAMES}


def make_art(size, filled, flower_reference_height=None):
    compact_layout = size[0] < 800
    base = load("Columbarium_ArtTallCompact_south" if compact_layout else "Columbarium_ArtTall_south", size)
    art = Image.new("RGBA", size, (0, 0, 0, 0))
    art.alpha_composite(base)
    compact_overlays = []
    for index in range(8):
        x, y, width, height = NICHE_BOUNDS[index]
        left, top = round(x / 1774 * size[0]), round(y / 887 * size[1])
        right, bottom = round((x + width) / 1774 * size[0]), round((y + height) / 887 * size[1])
        source = (compact_closed_source if compact_layout else closed_source) if index in filled else open_source
        door = source.resize((right - left, bottom - top), Image.Resampling.LANCZOS)
        art.alpha_composite(door, (left, top))
        if index in filled:
            flower_height = door.height if flower_reference_height is None else (
                round((y + height) / 887 * flower_reference_height) - round(y / 887 * flower_reference_height))
            ratios = COMPACT_FLOWER_RATIOS if compact_layout else FLOWER_RATIOS
            paddings = COMPACT_FLOWER_RIGHT_PADDING if compact_layout else FLOWER_RIGHT_PADDING
            margin_ratio = COMPACT_FLOWER_MARGIN if compact_layout else FLOWER_MARGIN
            flower_size = round(flower_height * ratios[index])
            flower = flowers[FLOWER_NAMES[index]].resize((flower_size, flower_size), Image.Resampling.LANCZOS)
            margin = round(flower_height * margin_ratio)
            spill = round((right - left) * COMPACT_FLOWER_SPILL) if compact_layout else 0
            position = (right - flower_size - margin + round(flower_size * paddings[index]) + spill,
                        bottom - flower_size - margin)
            if compact_layout:
                compact_overlays.append((flower, position))
            else:
                art.alpha_composite(flower, position)
    for flower, position in compact_overlays:
        art.alpha_composite(flower, position)
    return art


def add_footprint(image, footprint):
    draw = ImageDraw.Draw(image)
    draw.rectangle(footprint, fill="#455459")
    for coordinate in range(0, image.width + 1, SCALE):
        draw.line((coordinate, 0, coordinate, image.height), fill="#687175", width=2)
    for coordinate in range(0, image.height + 1, SCALE):
        draw.line((0, coordinate, image.width, coordinate), fill="#687175", width=2)


def dashed_box(draw, box):
    left, top, right, bottom = box
    for x in range(left, right, 20):
        draw.line((x, top, min(x + 11, right), top), fill="#70dbe3", width=4)
        draw.line((x, bottom, min(x + 11, right), bottom), fill="#70dbe3", width=4)
    for y in range(top, bottom, 20):
        draw.line((left, y, left, min(y + 11, bottom)), fill="#70dbe3", width=4)
        draw.line((right, y, right, min(y + 11, bottom)), fill="#70dbe3", width=4)


variants = (
    ("compact", (2, 1), (2.6667, 1.3333), (1024, 768), "columbarium_arttall_empty.png", "columbarium_arttall_mixed.png"),
    ("large", (4, 2), (4.6667, 2.3333), (1536, 1024), "columbarium_arttall_large_empty.png", "columbarium_arttall_large_mixed.png"),
)
parser = argparse.ArgumentParser()
parser.add_argument('--height-test', action='store_true', help='Preview a taller front without changing game definitions.')
args = parser.parse_args()
height_report = {}
for name, footprint_tiles, plane_tiles, canvas_size, empty_name, mixed_name in variants:
    flower_reference_height = round(plane_tiles[1] * SCALE)
    north_offset = 0
    if not args.height_test:
        def_name = 'ColumbariumModularCompactTall' if name == 'compact' else 'ColumbariumModularTall'
        definition = next(d for d in ET.parse(ROOT / 'Defs/ThingDefs/Columbarium_Things.xml').getroot()
                          if d.findtext('defName') == def_name)
        plane_tiles = tuple(map(float, definition.findtext('graphicData/drawSize').strip('()').split(',')))
        prefix = 'Compact' if name == 'compact' else 'Large'
        north_offset = float(re.search(prefix + r'NorthOffset = ([\d.]+)f', runtime)[1])
        flower_reference_height = round(plane_tiles[1] * SCALE)
    size = (round(plane_tiles[0] * SCALE), round(plane_tiles[1] * SCALE))
    center = (canvas_size[0] // 2, canvas_size[1] // 2)
    base_position = (round(center[0] - size[0] / 2), round(center[1] - size[1] / 2 - north_offset * SCALE))
    footprint = (center[0] - round(footprint_tiles[0] * SCALE / 2),
                 center[1] - round(footprint_tiles[1] * SCALE / 2),
                 center[0] + round(footprint_tiles[0] * SCALE / 2),
                 center[1] + round(footprint_tiles[1] * SCALE / 2))

    if args.height_test:
        # The visible silhouette, rather than its transparent texture canvas,
        # extends 0.42 tiles north and ends 0.02 tiles inside the south edge.
        alpha_bounds = Image.open(THINGS / 'Columbarium_ArtTall_south.png').getchannel('A').point(
            lambda a: 255 if a >= 128 else 0).getbbox()
        visible_height = footprint_tiles[1] + .4
        tall_height = round(visible_height * SCALE * 512 / (alpha_bounds[3] - alpha_bounds[1]))
        tall_size = (size[0], tall_height)
        tall_position = (base_position[0], round(footprint[3] - .02 * SCALE - alpha_bounds[3] / 512 * tall_height))
        for state, filled in (('empty', set()), ('mixed', {0, 1, 2, 4, 5})):
            candidate = Image.new('RGBA', canvas_size, '#34383a')
            add_footprint(candidate, footprint)
            candidate.alpha_composite(make_art(tall_size, filled, flower_reference_height=size[1]), tall_position)
            dashed_box(ImageDraw.Draw(candidate), footprint)
            ImageDraw.Draw(candidate).text((40, canvas_size[1] - 55),
                f'Height test: {visible_height:.2f} tiles visible; north overlap 0.42; south margin 0.02', fill='white')
            path = PREVIEW / f'sample3_height_test_{name}_{state}.png'
            candidate.convert('RGB').save(path)
            print(path)
            if state == 'empty':
                before = Image.open(PREVIEW / empty_name).convert('RGB')
                comparison = Image.new('RGB', (canvas_size[0] * 2, canvas_size[1] + 35), '#34383a')
                comparison.paste(before, (0, 35))
                comparison.paste(candidate.convert('RGB'), (canvas_size[0], 35))
                labels = ImageDraw.Draw(comparison)
                labels.text((40, 10), 'CURRENT', fill='white')
                labels.text((canvas_size[0] + 40, 10), 'HEIGHT TEST', fill='white')
                comparison.save(PREVIEW / f'sample3_height_comparison_{name}.png')
        top = tall_position[1] + alpha_bounds[1] / 512 * tall_height
        bottom = tall_position[1] + alpha_bounds[3] / 512 * tall_height
        overlap = (footprint[1] - top) / SCALE
        assert 1 / 3 <= overlap <= .5 and bottom <= footprint[3]
        height_report[name] = dict(visible_height_tiles=(bottom - top) / SCALE,
            north_overlap_tiles=overlap, south_margin_tiles=(footprint[3] - bottom) / SCALE,
            draw_size_tiles=[tall_size[0] / SCALE, tall_size[1] / SCALE],
            north_draw_offset_tiles=(center[1] - tall_position[1] - tall_size[1] / 2) / SCALE)
        continue

    sample = make_art(size, {0, 1, 2, 4, 5}, flower_reference_height)
    sample.save(SAMPLE / f"eight_{name}.png")

    preview = Image.new("RGBA", canvas_size, "#34383a")
    add_footprint(preview, footprint)
    preview.alpha_composite(sample, base_position)
    dashed_box(ImageDraw.Draw(preview), footprint)
    draw = ImageDraw.Draw(preview)
    font = ImageFont.load_default()
    draw.text((40, canvas_size[1] - 80), f"{name.title()}: occupied {footprint_tiles[0]} x {footprint_tiles[1]} tiles", fill="#d7faff", font=font)
    draw.text((40, canvas_size[1] - 58), f"Artwork plane: {plane_tiles[0]:.4f} x {plane_tiles[1]:.4f} tiles", fill="#f3f1e8", font=font)
    out = PREVIEW / (mixed_name if name == "large" else "columbarium_arttall_mixed.png")
    preview.convert("RGB").save(out)
    preview.convert("RGB").save(PREVIEW / f"sample3_aligned_{name}.png")
    preview.convert("RGB").save(PREVIEW / f"sample3_flowers_{name}.png")
    preview.convert("RGB").save(PREVIEW / f"sample3_{Path(CLOSED_SOURCE).stem}_{name}.png")
    print(out)
    empty = Image.new("RGBA", canvas_size, "#34383a")
    add_footprint(empty, footprint)
    empty.alpha_composite(make_art(size, set()), base_position)
    dashed_box(ImageDraw.Draw(empty), footprint)
    empty_path = PREVIEW / (empty_name if name == "large" else "columbarium_arttall_empty.png")
    empty.convert("RGB").save(empty_path)
    print(empty_path)

if args.height_test:
    (PREVIEW / 'sample3_height_test.json').write_text(json.dumps(height_report, indent=2), encoding='utf-8')
    print(json.dumps(height_report, indent=2))
