"""Builds the Sekiro deathblow dot texture from the isolated reference image.

Source: assets/source/deathblow_reference_isolated.png, the dot on pure black: an orange-red disc
(~200,40,25) with a fluffy irregular edge and a sparkly yellow-white core (~230,124,74), radius ~135 px.
On black, each pixel's brightness is how much the effect covers, so alpha comes from the brightest
channel and the color is un-premultiplied. The disc body becomes opaque, so it keeps its look over the
bright Deadlock scenes too; only the wispy edge fades.

Earlier versions synthesized a pure red disc from video measurements; in game they looked like a pink
cloud, so the reference image is used as-is instead.

Sekiro's dot is two things at once, which footage shows: under it the scene's green/blue drop (it
tints what is behind) while red rises above the scene (it emits light). An opaque alpha sprite only
covers, and in game it read as a flat sticker. So the dot is drawn in three layers, matching how
Deadlock's own marks are built (additive with overbright 2-15; MOD2X for tinting):
  deathblow_tint  MOD2X: red where the dot is, neutral grey outside, so the scene under it turns red
                  but keeps its own shading.
  deathblow_dot   ADD with overbright: the reference image as emitted light (blooms in game).
  deathblow_glow  ADD: soft halo past the dot's edge.
Output: mods/Sekiro_Deathblow_Mod/deathblow_{tint,dot,glow}.{png,rgba} (inputs for `vpcf_tool vtex`).
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
GLOW_COLOR = (255, 55, 25)
GLOW_SIGMA = 0.38     # in texture radii
TINT = (128, 25, 17)  # MOD2X: 128 = unchanged, so this keeps red and cuts green/blue to ~20%
TINT_EDGE = (0.55, 0.9)  # tint fades from full to neutral between these radii (texture radii)


def build():
    src = np.asarray(Image.open(SOURCE).convert('RGB')).astype(np.float64)
    cx, cy = CENTER
    crop = src[cy - HALF:cy + HALF, cx - HALF:cx + HALF]

    alpha = np.clip(crop.max(axis=2) / OPAQUE_AT, 0, 1)
    color = np.clip(crop / np.maximum(alpha, 1e-3)[..., None], 0, 255)
    rgba = np.dstack([color, alpha * 255])

    img = Image.fromarray(np.clip(rgba + 0.5, 0, 255).astype(np.uint8), 'RGBA').resize((SIZE, SIZE), Image.LANCZOS)
    save(img, 'deathblow_dot')

    yy, xx = np.mgrid[:SIZE, :SIZE].astype(np.float64)
    c = (SIZE - 1) / 2
    r = np.hypot(xx - c, yy - c) / (SIZE / 2)
    glow_alpha = np.exp(-0.5 * (r / GLOW_SIGMA) ** 2) * np.clip((1 - r) / 0.15, 0, 1)
    glow = np.dstack([np.broadcast_to(np.array(GLOW_COLOR, float), (SIZE, SIZE, 3)), glow_alpha * 255])
    save(Image.fromarray(np.clip(glow + 0.5, 0, 255).astype(np.uint8), 'RGBA'), 'deathblow_glow')

    t = np.clip((TINT_EDGE[1] - r) / (TINT_EDGE[1] - TINT_EDGE[0]), 0, 1)
    t = t * t * (3 - 2 * t)  # smoothstep
    tint_rgb = 128 + (np.array(TINT, float) - 128) * t[..., None]
    tint = np.dstack([tint_rgb, t * 255])
    save(Image.fromarray(np.clip(tint + 0.5, 0, 255).astype(np.uint8), 'RGBA'), 'deathblow_tint')


def save(img, name):
    img.save(os.path.join(OUT_DIR, name + '.png'))
    with open(os.path.join(OUT_DIR, name + '.rgba'), 'wb') as f:
        f.write(img.tobytes())


if __name__ == '__main__':
    build()
    print('deathblow_tint / deathblow_dot / deathblow_glow written')
