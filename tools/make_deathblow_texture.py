"""Builds the Sekiro deathblow dot textures from measurements on 1080p60 gameplay.

Source: "Sekiro But Every Perilous Attack Has a Vine Boom.mp4", 1:10.7-1:11.0 (frames 4244-4260),
Genichiro with the dot at (939, 516) over a dark background of ~(24, 22, 20). Median color per 6 px
ring (lock-on dot pixels excluded) shows two things at once:
  - the scene's green/blue under the dot drop to ~20% (it tints what is behind red), fading out by ~96 px;
  - red light is added on top (~150 at r=24 falling to 0 at ~96), with a small warm core
    (orange-white, r < ~18) that is part of the effect, not the lock-on dot;
  - grain: ~6% noise in the red.
So the dot is two layers, the way Deadlock builds its own marks:
  deathblow_tint  MOD2X: red channel unchanged, green/blue scaled by (1 - occlusion).
  deathblow_dot   ADD: the measured emitted light, with grain.
The texture edge maps to 96 px of the footage. Earlier attempts (opaque sprite, an orange isolated
reference image, an extra pink glow) read as a sticker or came out orange in game.

Output: mods/Sekiro_Deathblow_Mod/deathblow_{tint,dot}.{png,rgba} (inputs for `vpcf_tool vtex`).
"""
import os
import numpy as np
from PIL import Image

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
OUT_DIR = os.path.join(ROOT, 'mods', 'Sekiro_Deathblow_Mod')
SIZE = 256
EDGE_PX = 96.0
GRAIN = 0.07
SEED = 7

# Footage radius (px) -> fraction of the scene's green/blue hidden by the dot (1 - G_out / G_bg).
OCCLUSION = [(0, 0.8), (30, 0.8), (36, 0.74), (42, 0.68), (48, 0.6), (54, 0.5), (60, 0.41), (66, 0.29),
             (72, 0.19), (78, 0.11), (84, 0.06), (90, 0.02), (96, 0.0)]
# Footage radius (px) -> light added on top of the tinted scene (R, G, B).
EMISSION = [(0, (212, 157, 120)), (6, (215, 120, 85)), (12, (203, 66, 53)), (18, (170, 16, 18)),
            (24, (150, 0, 0)), (30, (141, 0, 0)), (36, (128, 0, 0)), (42, (111, 0, 0)), (48, (93, 0, 0)),
            (54, (74, 0, 0)), (60, (55, 0, 0)), (66, (40, 0, 0)), (72, (26, 0, 0)), (78, (15, 0, 0)),
            (84, (7, 0, 0)), (90, (3, 0, 0)), (96, (0, 0, 0))]


def radius_map():
    yy, xx = np.mgrid[:SIZE, :SIZE].astype(np.float64)
    c = (SIZE - 1) / 2
    return np.hypot(xx - c, yy - c) / (SIZE / 2) * EDGE_PX


def build():
    r = radius_map()

    occ = np.interp(r, [p[0] for p in OCCLUSION], [p[1] for p in OCCLUSION], right=0)
    keep = 1 - occ
    tint = np.dstack([np.full_like(r, 128.0), 128 * keep, 128 * keep, np.full_like(r, 255.0)])
    save(tint, 'deathblow_tint')

    xs = [p[0] for p in EMISSION]
    light = np.dstack([np.interp(r, xs, [p[1][i] for p in EMISSION], right=0) for i in range(3)])
    grain = np.random.default_rng(SEED).normal(0, 1, (SIZE, SIZE))
    light *= np.clip(1 + GRAIN * grain, 0, None)[..., None]
    save(np.dstack([light, np.full_like(r, 255.0)]), 'deathblow_dot')


def save(rgba, name):
    img = Image.fromarray(np.clip(rgba + 0.5, 0, 255).astype(np.uint8), 'RGBA')
    img.save(os.path.join(OUT_DIR, name + '.png'))
    with open(os.path.join(OUT_DIR, name + '.rgba'), 'wb') as f:
        f.write(img.tobytes())


if __name__ == '__main__':
    build()
    print('deathblow_tint / deathblow_dot written')
