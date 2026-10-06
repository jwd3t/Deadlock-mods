"""Builds the Sekiro deathblow dot texture from the isolated reference image.

Source: assets/source/deathblow_reference_isolated.png, the dot on pure black: an orange-red disc
(~200,40,25) with a fluffy irregular edge and a sparkly yellow-white core (~230,124,74), radius ~135 px.
On black, each pixel's brightness is how much the effect covers, so alpha comes from the brightest
channel and the color is un-premultiplied. The disc body becomes opaque, so it keeps its look over the
bright Deadlock scenes too; only the wispy edge fades.

Earlier versions synthesized a pure red disc from video measurements; in game they looked like a pink
cloud, so the reference image is used as-is instead.

Render with PARTICLE_OUTPUT_BLEND_MODE_ALPHA, overbright 1.0, self-illuminated, no depth test.
Output: mods/Sekiro_Deathblow_Mod/deathblow_dot.png and .rgba (input for `vpcf_tool vtex`).
"""
import os
import numpy as np
from PIL import Image

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
SOURCE = os.path.join(ROOT, 'assets', 'source', 'deathblow_reference_isolated.png')
OUT_DIR = os.path.join(ROOT, 'mods', 'Sekiro_Deathblow_Mod')
SIZE = 256
CENTER = (241, 199)   # disc center in the source image
HALF = 150            # crop half-size: disc radius ~135 plus a margin that stays black
OPAQUE_AT = 150.0     # brightest-channel value from which a pixel is fully opaque (disc body is 160-230)


def build():
    src = np.asarray(Image.open(SOURCE).convert('RGB')).astype(np.float64)
    cx, cy = CENTER
    crop = src[cy - HALF:cy + HALF, cx - HALF:cx + HALF]

    alpha = np.clip(crop.max(axis=2) / OPAQUE_AT, 0, 1)
    color = np.clip(crop / np.maximum(alpha, 1e-3)[..., None], 0, 255)
    rgba = np.dstack([color, alpha * 255])

    img = Image.fromarray(np.clip(rgba + 0.5, 0, 255).astype(np.uint8), 'RGBA').resize((SIZE, SIZE), Image.LANCZOS)
    img.save(os.path.join(OUT_DIR, 'deathblow_dot.png'))
    with open(os.path.join(OUT_DIR, 'deathblow_dot.rgba'), 'wb') as f:
        f.write(img.tobytes())


if __name__ == '__main__':
    build()
    print('deathblow_dot.png / .rgba written')
