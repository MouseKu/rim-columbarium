"""Preview the 32-niche horizontal and vertical art against its footprint."""
from pathlib import Path
import xml.etree.ElementTree as ET

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
THINGS = ROOT / "Textures/Things"
PREVIEW = ROOT / "Preview"
FOOTPRINTS = (("compact", (4, 2), 72), ("large", (8, 4), 38))
SCALE = 40


def add_grid(image, box, tiles):
    draw = ImageDraw.Draw(image)
    left, top, right, bottom = box
    draw.rectangle(box, fill="#455459", outline="#70dbe3", width=3)
    for x in range(1, tiles[0]):
        px = left + x * (right - left) // tiles[0]
        draw.line((px, top, px, bottom), fill="#687175", width=1)
    for y in range(1, tiles[1]):
        py = top + y * (bottom - top) // tiles[1]
        draw.line((left, py, right, py), fill="#687175", width=1)


def paste_center(canvas, art, center, xsize, zsize, north=0):
    image = art.resize((round(xsize), round(zsize)), Image.Resampling.LANCZOS)
    canvas.alpha_composite(image, (round(center[0] - image.width / 2),
                                   round(center[1] - image.height / 2 - north)))


def main():
    definitions = {d.findtext("defName"): d for d in ET.parse(ROOT / "Defs/ThingDefs/Columbarium_32.xml").getroot()}
    PREVIEW.mkdir(exist_ok=True)
    for name, footprint, scale in FOOTPRINTS:
        texture = 'Columbarium_Art32OutlineCompact' if name == 'compact' else 'Columbarium_Art32Outline'
        front = Image.open(THINGS / f'{texture}_south.png').convert('RGBA')
        side = Image.open(THINGS / f'{texture}_east.png').convert('RGBA')
        definition = definitions["ColumbariumThirtyTwoCompact" if name == "compact" else "ColumbariumThirtyTwoLarge"]
        draw_w, draw_h = map(float, definition.findtext("graphicData/drawSize").strip("()").split(","))
        offset_name = "CompactNorthOffset" if name == "compact" else "LargeNorthOffset"
        layout = (ROOT / "Source/Columbarium/ThirtyTwoLayout.cs").read_text(encoding="utf-8")
        import re
        north = float(re.search(offset_name + r" = ([\d.]+)f", layout)[1])
        canvas = Image.new("RGBA", (940, 600), "#34383a")
        draw = ImageDraw.Draw(canvas)
        font = ImageFont.load_default()
        draw.text((60, 25), f"32 niches / {name} / horizontal", font=font, fill="white")
        draw.text((540, 25), f"32 niches / {name} / vertical", font=font, fill="white")
        f_w, f_h = footprint[0] * scale, footprint[1] * scale
        horizontal_box = (65, 165, 65 + f_w, 165 + f_h)
        add_grid(canvas, horizontal_box, footprint)
        paste_center(canvas, front, ((horizontal_box[0] + horizontal_box[2]) / 2,
                                     (horizontal_box[1] + horizontal_box[3]) / 2),
                     draw_w * scale, draw_h * scale, north * scale)
        vertical_footprint = (footprint[1], footprint[0])
        v_w, v_h = vertical_footprint[0] * scale, vertical_footprint[1] * scale
        vertical_box = (540 + (330 - v_w) // 2, 165 + (330 - v_h) // 2,
                        540 + (330 + v_w) // 2, 165 + (330 + v_h) // 2)
        add_grid(canvas, vertical_box, vertical_footprint)
        # Graphic_Multi swaps the plane dimensions for east/west rotations.
        paste_center(canvas, side, ((vertical_box[0] + vertical_box[2]) / 2,
                                    (vertical_box[1] + vertical_box[3]) / 2),
                     draw_h * scale, draw_w * scale)
        draw = ImageDraw.Draw(canvas)
        draw.text((65, 470), "Cyan outline: occupied cells; grid scale " + str(scale) + " px/tile", font=font, fill="#d7faff")
        canvas.convert("RGB").save(PREVIEW / f"thirtytwo_orientations_{name}.png")


if __name__ == "__main__":
    main()
