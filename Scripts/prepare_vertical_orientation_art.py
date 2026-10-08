"""Make vertical top-and-side stone views using the current facade material."""
from pathlib import Path

from PIL import Image, ImageDraw, ImageEnhance, ImageFont


ROOT = Path(__file__).resolve().parents[1]
THINGS = ROOT / "Textures" / "Things"
PREVIEW = ROOT / "Preview"


def color_mask(image):
    """Match the mod's material tint mask for neutral stone pixels."""
    rgba = image.convert("RGBA")
    pixels = rgba.load()
    mask = Image.new("RGBA", rgba.size, (0, 0, 0, 0))
    output = mask.load()
    for y in range(rgba.height):
        for x in range(rgba.width):
            red, green, blue, alpha = pixels[x, y]
            brightness = (red + green + blue) / 3
            neutral = max(0.0, min(1.0, (24 - (max(red, green, blue) - min(red, green, blue))) / 16))
            tint = round(max(0.0, min(1.0, (brightness - 105) / 90)) * neutral * 255)
            output[x, y] = (tint, 0, 0, alpha)
    return mask


def slab(front, crop, width_fraction, height_fraction, compact=False):
    canvas = Image.new("RGBA", (512, 1024))
    width, height = round(512 * width_fraction), round(1024 * height_fraction)
    left, top = (512 - width) // 2, (1024 - height) // 2
    right, bottom = left + width - 1, top + height - 1
    scale = 2 if compact else 1
    bevel, corner = 9, 9
    roof_bottom = top + round(height * .70)
    outline = [(left + corner, top), (right - corner, top), (right, top + corner),
               (right, bottom - corner), (right - corner, bottom),
               (left + corner, bottom), (left, bottom - corner), (left, top + corner)]
    draw = ImageDraw.Draw(canvas)
    draw.polygon(outline, fill="#222221")
    source_stone = front.crop(crop).transpose(Image.Transpose.ROTATE_90)
    # The roof is the broad upper plane seen from RimWorld's overhead camera.
    roof = source_stone.resize((width - bevel * 2, roof_bottom - top - bevel),
                               Image.Resampling.LANCZOS)
    canvas.alpha_composite(roof, (left + bevel, top + bevel))
    # The darker bottom section is the visible side wall.
    wall = source_stone.resize((width - bevel * 2, bottom - roof_bottom - bevel),
                               Image.Resampling.LANCZOS)
    wall = ImageEnhance.Brightness(wall).enhance(.68)
    canvas.alpha_composite(wall, (left + bevel, roof_bottom))
    draw = ImageDraw.Draw(canvas)
    # A narrow rim defines the roof's thickness and projects over the wall.
    draw.line((left + 9, top + 9, right - 9, top + 9), fill="#c7c5be", width=2 * scale)
    draw.line((left + 9, top + 9, left + 9, roof_bottom - 12),
              fill="#a8a7a1", width=scale)
    draw.line((right - 9, top + 9, right - 9, roof_bottom - 12),
              fill="#777670", width=scale)
    draw.rectangle((left + 4, roof_bottom - 11, right - 4, roof_bottom + 5),
                   fill="#6e6d68")
    draw.line((left + 6, roof_bottom - 11, right - 6, roof_bottom - 11),
              fill="#bdbbb5", width=2 * scale)
    draw.line((left + 4, roof_bottom + 5, right - 4, roof_bottom + 5),
              fill="#292927", width=4 * scale)

    # Pilasters and two restrained joints give the lower wall stone structure.
    wall_top = roof_bottom + 12
    wall_bottom = bottom - 34
    panel = (left + 29, wall_top + 8, right - 29, wall_bottom - 8)
    draw.rectangle((left + 10, wall_top, left + 24, wall_bottom), fill="#85847f")
    draw.rectangle((right - 24, wall_top, right - 10, wall_bottom), fill="#666560")
    draw.line((left + 10, wall_top, left + 10, wall_bottom),
              fill="#aaa9a2", width=scale)
    draw.line((right - 10, wall_top, right - 10, wall_bottom),
              fill="#393936", width=scale)
    draw.rectangle(panel, outline="#33322f", width=2 * scale)
    draw.line((panel[0] + 4, panel[1] + 4, panel[2] - 4, panel[1] + 4),
              fill="#aaa9a2", width=scale)
    for fraction in (1 / 3, 2 / 3):
        y = round(panel[1] + (panel[3] - panel[1]) * fraction)
        draw.line((panel[0] + 5, y, panel[2] - 5, y), fill="#55544f", width=scale)
        draw.line((panel[0] + 5, y + scale, panel[2] - 5, y + scale),
                  fill="#8d8c86", width=scale)
    draw.rectangle((left + 7, wall_bottom + 2, right - 7, bottom - 9),
                   fill="#777671")
    draw.line((left + 7, wall_bottom + 2, right - 7, wall_bottom + 2),
              fill="#33322f", width=3 * scale)
    draw.line((left + 9, wall_bottom + 7, right - 9, wall_bottom + 7),
              fill="#a6a49e", width=scale)
    draw.line(outline + [outline[0]], fill="#171716", width=4, joint="curve")
    draw.line([(left + bevel, bottom - bevel), (right - bevel, bottom - bevel)],
              fill="#4c4b48", width=2 * scale)
    return canvas


def save_preview(stem, name, front, vertical):
    background = Image.new("RGBA", (900, 950), "#34383a")
    draw = ImageDraw.Draw(background)
    font = ImageFont.load_default()
    draw.text((30, 20), f"{name}: current horizontal art", font=font, fill="#f3f1e8")
    horizontal = front.copy()
    horizontal.thumbnail((840, 380), Image.Resampling.LANCZOS)
    background.alpha_composite(horizontal, ((900 - horizontal.width) // 2, 55))
    draw.text((30, 470), f"{name}: vertical placement / top and shaded side", font=font, fill="#f3f1e8")
    # Art comparison, not an in-game screenshot. Both footprints have 1:2 sides.
    vertical = vertical.crop(vertical.getbbox()).resize((215, 430), Image.Resampling.LANCZOS)
    background.alpha_composite(vertical, ((900 - vertical.width) // 2, 505))
    background.convert("RGB").save(PREVIEW / f"{stem}_orientation.png")


def main():
    PREVIEW.mkdir(exist_ok=True)
    for stem, crop, width, height in (
        ("Columbarium_ArtTall", (48, 90, 975, 164), .65, .96),
        ("Columbarium_Art32Outline", (70, 165, 1695, 263), .66, .98),
    ):
        for variant in ("", "Compact"):
            prefix = f"{stem}{variant}"
            front = Image.open(THINGS / f"{prefix}_south.png").convert("RGBA")
            image = slab(front, crop, width, height, compact=bool(variant))
            # Both vertical directions expose the same top, without front niches.
            for direction in ("east", "west"):
                image.save(THINGS / f"{prefix}_{direction}.png")
                color_mask(image).save(THINGS / f"{prefix}_{direction}m.png")
            name = ("8 niches" if "ArtTall" in stem else "32 niches") + (" compact" if variant else " large")
            save_preview(prefix.replace("Columbarium_", ""), name, front, image)


if __name__ == "__main__":
    main()
