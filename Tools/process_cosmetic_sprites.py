#!/usr/bin/env python3
"""Split the approved cosmetic atlases into consistently framed Unity sprites."""

from pathlib import Path
from collections import deque

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "tmp/imagegen"
OUTPUT = ROOT / "Assets/Resources/Sprites/Cosmetics"

ATLASES = (
    (
        "cosmetic-bots-atlas-alpha.png",
        4,
        3,
        (
            "bot_gold",
            "bot_midnight",
            "bot_bubblegum",
            "bot_honey",
            "bot_lavender",
            "bot_rusty",
            "bot_space",
            "bot_cat",
            "bot_frog",
            "bot_retro",
            "bot_arcade",
        ),
        0.035,
    ),
    (
        "cosmetic-cats-atlas-alpha.png",
        4,
        2,
        (
            "cat_tuxedo",
            "cat_sleepy",
            "cat_fancy",
            "cat_space",
            "cat_ghost",
            "cat_robot",
            "cat_tiger",
        ),
        0.045,
    ),
    (
        "cosmetic-crumbs-atlas-alpha.png",
        3,
        2,
        (
            "crumb_cookie",
            "crumb_cereal",
            "crumb_dust",
            "crumb_leaf",
            "crumb_candy",
            "crumb_popcorn",
        ),
        0.06,
    ),
    (
        "cosmetic-paths-atlas-alpha.png",
        4,
        2,
        (
            "path_blue",
            "path_sunset",
            "path_coral",
            "path_neon",
            "path_gold",
            "path_bubble",
            "path_space",
            "path_rainbow",
        ),
        0.07,
    ),
    (
        "cosmetic-docks-atlas-alpha.png",
        3,
        2,
        (
            "dock_gold",
            "dock_neon",
            "dock_wood",
            "dock_space",
            "dock_catbed",
        ),
        0.04,
    ),
)


def trim_and_square(image: Image.Image, output_size: int, padding_ratio: float) -> Image.Image:
    alpha = image.getchannel("A")
    bounds = alpha.point(lambda value: 255 if value > 12 else 0).getbbox()
    if bounds is None:
        raise ValueError("Cosmetic cell contains no visible pixels")

    trimmed = image.crop(bounds)
    padding = max(6, round(max(trimmed.size) * padding_ratio))
    square_size = max(trimmed.size) + padding * 2
    square = Image.new("RGBA", (square_size, square_size), (0, 0, 0, 0))
    offset = ((square_size - trimmed.width) // 2, (square_size - trimmed.height) // 2)
    square.alpha_composite(trimmed, offset)
    return square.resize((output_size, output_size), Image.Resampling.LANCZOS)


def remove_outside_primary_bounds(image: Image.Image, margin: int = 8) -> Image.Image:
    """Discard atlas bleed while retaining disconnected details inside the main silhouette."""
    alpha = image.getchannel("A")
    width, height = image.size
    visible = bytearray(1 if value > 12 else 0 for value in alpha.getdata())
    visited = bytearray(width * height)
    largest: tuple[int, int, int, int, int] | None = None

    for start, is_visible in enumerate(visible):
        if not is_visible or visited[start]:
            continue
        visited[start] = 1
        queue = deque((start,))
        count = 0
        min_x = max_x = start % width
        min_y = max_y = start // width
        while queue:
            index = queue.popleft()
            x, y = index % width, index // width
            count += 1
            min_x, max_x = min(min_x, x), max(max_x, x)
            min_y, max_y = min(min_y, y), max(max_y, y)
            for neighbor in (index - 1, index + 1, index - width, index + width):
                if neighbor < 0 or neighbor >= width * height or visited[neighbor] or not visible[neighbor]:
                    continue
                nx, ny = neighbor % width, neighbor // width
                if abs(nx - x) + abs(ny - y) != 1:
                    continue
                visited[neighbor] = 1
                queue.append(neighbor)
        component = (count, min_x, min_y, max_x + 1, max_y + 1)
        if largest is None or component[0] > largest[0]:
            largest = component

    if largest is None:
        return image
    _, left, top, right, bottom = largest
    left, top = max(0, left - margin), max(0, top - margin)
    right, bottom = min(width, right + margin), min(height, bottom + margin)
    cleaned_alpha = alpha.copy()
    pixels = cleaned_alpha.load()
    for y in range(height):
        for x in range(width):
            if x < left or x >= right or y < top or y >= bottom:
                pixels[x, y] = 0
    cleaned = image.copy()
    cleaned.putalpha(cleaned_alpha)
    return cleaned


def split_atlas(
    filename: str,
    columns: int,
    rows: int,
    names: tuple[str, ...],
    padding_ratio: float,
) -> None:
    atlas = Image.open(SOURCE / filename).convert("RGBA")
    cell_width = atlas.width / columns
    cell_height = atlas.height / rows

    for index, name in enumerate(names):
        column = index % columns
        row = index // columns
        inset = max(2, round(min(cell_width, cell_height) * 0.012))
        bounds = (
            round(column * cell_width) + inset,
            round(row * cell_height) + inset,
            round((column + 1) * cell_width) - inset,
            round((row + 1) * cell_height) - inset,
        )
        cell = atlas.crop(bounds)
        if name.startswith("dock_"):
            cell = remove_outside_primary_bounds(cell)
        sprite = trim_and_square(cell, 384, padding_ratio)
        sprite.save(OUTPUT / f"{name}.png", optimize=True)


def main() -> None:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    for atlas in ATLASES:
        split_atlas(*atlas)
    print(f"Wrote {sum(len(atlas[3]) for atlas in ATLASES)} cosmetic sprites to {OUTPUT}")


if __name__ == "__main__":
    main()
