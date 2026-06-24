#!/usr/bin/env python3
"""Prepare generated DustBot art as tightly packed transparent Unity sprites."""

from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
ATLAS = ROOT / "tmp/imagegen/dustbot-object-atlas-alpha.png"
EMPTY_DOCK = ROOT / "tmp/imagegen/dustbot-empty-dock-alpha.png"
PLAYER = ROOT / "Assets/Resources/Sprites/dustbot-player.png"
OUTPUT = ROOT / "Assets/Resources/Sprites"

SPRITE_NAMES = (
    "sock",
    "wall",
    "cord",
    "wet-spot",
    "toy",
    "crumbs",
    "dock",
    "dust-bunny",
)


def trim_and_square(image: Image.Image, output_size: int, padding_ratio: float = 0.08) -> Image.Image:
    alpha = image.getchannel("A")
    bounds = alpha.point(lambda value: 255 if value > 8 else 0).getbbox()
    if bounds is None:
        raise ValueError("Generated sprite contains no visible pixels.")

    trimmed = image.crop(bounds)
    padding = max(8, round(max(trimmed.size) * padding_ratio))
    square_size = max(trimmed.size) + padding * 2
    square = Image.new("RGBA", (square_size, square_size), (0, 0, 0, 0))
    offset = ((square_size - trimmed.width) // 2, (square_size - trimmed.height) // 2)
    square.alpha_composite(trimmed, offset)
    return square.resize((output_size, output_size), Image.Resampling.LANCZOS)


def process_atlas() -> None:
    atlas = Image.open(ATLAS).convert("RGBA")
    cell_width = atlas.width / 4
    cell_height = atlas.height / 2
    for index, name in enumerate(SPRITE_NAMES):
        column = index % 4
        row = index // 4
        bounds = (
            round(column * cell_width),
            round(row * cell_height),
            round((column + 1) * cell_width),
            round((row + 1) * cell_height),
        )
        sprite = trim_and_square(atlas.crop(bounds), 384)
        sprite.save(OUTPUT / f"{name}.png", optimize=True)


def process_player() -> None:
    player = Image.open(PLAYER).convert("RGBA")
    prepared = trim_and_square(player, 512, 0.04)
    prepared.save(PLAYER, optimize=True)


def process_empty_dock() -> None:
    if not EMPTY_DOCK.exists():
        return

    dock = Image.open(EMPTY_DOCK).convert("RGBA")
    prepared = trim_and_square(dock, 384)
    prepared.save(OUTPUT / "dock.png", optimize=True)


if __name__ == "__main__":
    OUTPUT.mkdir(parents=True, exist_ok=True)
    process_atlas()
    process_empty_dock()
    process_player()
    print(f"Wrote {len(SPRITE_NAMES) + 1} sprites to {OUTPUT}")
