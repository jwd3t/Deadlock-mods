"""Renders the 忍殺 (Shinobi Execution) victory image in the style of the approved death image.

The death image (extracted/panorama/images/hud/sekiro_death_true.vtex_c) is a 1024x576 canvas with the
kanji ~170x148 px at x 429-598, y 181-328, color (183, 48, 44), a short glow of the same red (alpha ~56),
and "DEATH" in spaced serif capitals ~14 px tall just below. This keeps those proportions for two kanji.

Output: build/sekiro_victory.rgba (1024x576, for `vpcf_tool vtex-raw`) and build/sekiro_victory.png.
"""
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

BASE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.join(BASE, '..', '..')
KANJI_FONT = os.path.join(ROOT, 'assets', 'source', 'yujiboku.ttf')
TEXT_FONT = 'C:/Windows/Fonts/times.ttf'
W, H = 1024, 576
COLOR = (183, 48, 44)
KANJI_TOP, KANJI_HEIGHT = 181, 148
TEXT_TOP, TEXT_SIZE, TEXT_SPACING = 352, 18, 9


def spaced_text(draw, text, font, top, spacing):
    widths = [draw.textlength(ch, font=font) for ch in text]
    total = sum(widths) + spacing * (len(text) - 1)
    x = (W - total) / 2
    for ch, w in zip(text, widths):
        draw.text((x, top), ch, font=font, fill=255)
        x += w + spacing


def build():
    mask = Image.new('L', (W, H), 0)
    d = ImageDraw.Draw(mask)

    size = 200
    font = ImageFont.truetype(KANJI_FONT, size)
    l, t, r, b = d.textbbox((0, 0), '忍殺', font=font)
    size = int(size * KANJI_HEIGHT / (b - t))
    font = ImageFont.truetype(KANJI_FONT, size)
    l, t, r, b = d.textbbox((0, 0), '忍殺', font=font)
    d.text(((W - (r - l)) / 2 - l, KANJI_TOP - t), '忍殺', font=font, fill=255)

    spaced_text(d, 'SHINOBI EXECUTION', ImageFont.truetype(TEXT_FONT, TEXT_SIZE), TEXT_TOP, TEXT_SPACING)

    solid = np.asarray(mask).astype(np.float64) / 255
    glow = np.asarray(mask.filter(ImageFilter.GaussianBlur(6))).astype(np.float64) / 255 * 0.45
    alpha = np.maximum(solid, glow)
    rgba = np.dstack([np.broadcast_to(np.array(COLOR, float), (H, W, 3)), alpha * 255])
    img = Image.fromarray(np.clip(rgba + 0.5, 0, 255).astype(np.uint8), 'RGBA')

    out = os.path.join(BASE, 'build')
    os.makedirs(out, exist_ok=True)
    img.save(os.path.join(out, 'sekiro_victory.png'))
    with open(os.path.join(out, 'sekiro_victory.rgba'), 'wb') as f:
        f.write(img.tobytes())
    print('build/sekiro_victory.png written')


if __name__ == '__main__':
    build()
