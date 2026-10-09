"""Render transparent, half-occupied south-facing columbaria for previews."""
from pathlib import Path
import re
import xml.etree.ElementTree as ET

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
THINGS = ROOT / "Textures" / "Things"
PREVIEW = ROOT / "Preview"
SKILLS = (
    "Melee", "Shooting", "Construction", "Mining", "Cooking", "Plants",
    "Animals", "Crafting", "Artistic", "Medicine", "Social", "Intellectual",
)
FLOWER_CHOICES = (0, 2, 4, 6, 8, 10, 1, 3, 5, 7, 9, 11)
SCALE = 320
RESAMPLE = Image.Resampling.LANCZOS


def floats_between(source, start):
    return [float(value) for value in re.findall(
        r"([\d.]+)f", source.split(start, 1)[1].split("}", 1)[0]
    )]


def bounds_from(source, start):
    return [tuple(map(float, values)) for values in re.findall(
        r"new Rect\(([\d.]+)f, ([\d.]+)f, ([\d.]+)f, ([\d.]+)f\)",
        source.split(start, 1)[1].split("};", 1)[0],
    )]


def definition(path, name):
    return next(item for item in ET.parse(ROOT / path).getroot()
                if item.findtext("defName") == name)


def render(count, compact):
    if count == 8:
        name = "ColumbariumModularCompactTall" if compact else "ColumbariumModularTall"
        item = definition("Defs/ThingDefs/Columbarium_Things.xml", name)
        runtime = (ROOT / "Source/Columbarium/Building_Columbarium.cs").read_text(encoding="utf-8")
        bounds = bounds_from(runtime, "Rect[] TallNicheBounds = {")
        layout_name = "CompactNicheFlowerLayout" if compact else "NicheFlowerLayout"
        front_name = "Columbarium_ArtTallCompact" if compact else "Columbarium_ArtTall"
    else:
        name = "ColumbariumThirtyTwoCompact" if compact else "ColumbariumThirtyTwoLarge"
        item = definition("Defs/ThingDefs/Columbarium_32.xml", name)
        runtime = (ROOT / "Source/Columbarium/ThirtyTwoLayout.cs").read_text(encoding="utf-8")
        bounds = bounds_from(runtime, "Rect[] Bounds = {")
        layout_name = "CompactThirtyTwoFlowerLayout" if compact else "ThirtyTwoLayout"
        front_name = "Columbarium_Art32OutlineCompact" if compact else "Columbarium_Art32Outline"

    layout = (ROOT / "Source/Columbarium" / f"{layout_name}.cs").read_text(encoding="utf-8")
    if layout_name == "ThirtyTwoLayout":
        ratios = floats_between(layout, "FlowerRatios = {")
        paddings = floats_between(layout, "RightPaddings = {")
        margin = float(re.search(r"FlowerMargin = ([\d.]+)f", layout)[1])
        spill = 0.0
    else:
        ratios = floats_between(layout, "Ratios = {")
        paddings = floats_between(layout, "RightPaddings = {")
        margin = float(re.search(r"MarginRatio = ([\d.]+)f", layout)[1])
        spill = float(re.search(r"RightSpillRatio = ([\d.]+)f", layout)[1])

    assert len(bounds) == count and len(ratios) == len(paddings) == len(SKILLS)
    plane = tuple(map(float, item.findtext("graphicData/drawSize").strip("()").split(",")))
    size = (round(plane[0] * SCALE), round(plane[1] * SCALE))
    art = Image.open(THINGS / f"{front_name}_south.png").convert("RGBA").resize(size, RESAMPLE)
    opened = Image.open(THINGS / "Columbarium_ArtTallNicheOpen.png").convert("RGBA")
    closed_name = "Columbarium_Compact_ArtTallNicheClosed" if compact else "Columbarium_ArtTallNicheClosed"
    closed = Image.open(THINGS / f"{closed_name}.png").convert("RGBA")
    flowers = [Image.open(THINGS / "Flowers" / f"{skill}.png").convert("RGBA") for skill in SKILLS]
    deferred_flowers = []

    for niche, (x, y, width, height) in enumerate(bounds):
        # Runtime fills odd-numbered visible niches first, via UrnIndexForNiche.
        urn_index = niche // 2 if niche % 2 == 0 else count // 2 + niche // 2
        occupied = urn_index < count // 2
        left, top = round(x / 1774 * size[0]), round(y / 887 * size[1])
        right, bottom = round((x + width) / 1774 * size[0]), round((y + height) / 887 * size[1])
        door_width, door_height = right - left, bottom - top
        art.alpha_composite((closed if occupied else opened).resize((door_width, door_height), RESAMPLE),
                            (left, top))
        if not occupied:
            continue

        flower_index = FLOWER_CHOICES[urn_index % len(FLOWER_CHOICES)]
        flower_size = round(door_height * ratios[flower_index])
        flower = flowers[flower_index].resize((flower_size, flower_size), RESAMPLE)
        position = (
            right - flower_size - round(door_height * margin)
            + round(flower_size * paddings[flower_index]) + round(door_width * spill),
            bottom - flower_size - round(door_height * margin),
        )
        if compact:
            deferred_flowers.append((flower, position))
        else:
            art.alpha_composite(flower, position)

    for flower, position in deferred_flowers:
        art.alpha_composite(flower, position)

    path = PREVIEW / f"half_full_{count}_{'compact' if compact else 'large'}.png"
    art.save(path)
    print(path)


if __name__ == "__main__":
    PREVIEW.mkdir(exist_ok=True)
    for count in (8, 32):
        for compact in (True, False):
            render(count, compact)
