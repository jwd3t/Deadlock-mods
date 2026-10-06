"""Generates the Sekiro deathblow dot texture from a profile measured on in-game footage.

Shape: "Sekiro Shadows Die Twice Mikiri Kick counter and Deathblow.mp4" (1920x1080, 29.97 fps),
frames 222-229, dot near (1135, 735). Opacity per 6 px ring = 1 - (green+blue inside) / (green+blue of
the scene ring around the dot): a ~80% plateau over the inner third, then a near-linear falloff to 0 at
~72 px. The rings under the lock-on dot (the white center, not part of this effect) are excluded and
the plateau is carried inward. An earlier fit on a dark-scene clip ("Enfeebled Deathblow Animation")
gave a cone with a long faint tail; that tail is kept only as the faint fringe past the main edge.

Color: pure red (255, 0, 0). The dot covers the scene (alpha blend), with fine grain inside.

Render with PARTICLE_OUTPUT_BLEND_MODE_ALPHA, overbright 1.0, self-illuminated, no depth test.
Output: mods/Sekiro_Deathblow_Mod/deathblow_dot.png and .rgba (input for `vpcf_tool vtex`).
"""
import os
import cv2
import numpy as np
from PIL import Image

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
OUT_DIR = os.path.join(ROOT, 'mods', 'Sekiro_Deathblow_Mod')
SIZE = 256
GRAIN = 0.15  # strength of the fine noise inside the dot

# (radius / main edge radius, alpha). The texture edge is at 1.15x the main edge.
PROFILE = [(0.0, 0.92), (0.25, 0.92), (0.33, 0.85), (0.42, 0.75), (0.5, 0.66), (0.58, 0.55), (0.67, 0.42),
           (0.75, 0.28), (0.83, 0.15), (0.92, 0.07), (1.0, 0.04), (1.15, 0.0)]


def build():
    radii = np.array([p[0] for p in PROFILE]) / PROFILE[-1][0]
    values = np.array([p[1] for p in PROFILE])
    yy, xx = np.mgrid[:SIZE, :SIZE].astype(np.float64)
    c = (SIZE - 1) / 2
    r = np.hypot(xx - c, yy - c) / (SIZE / 2)
    alpha = np.interp(r, radii, values, right=0)
    grain = cv2.GaussianBlur(np.random.default_rng(7).random((SIZE, SIZE)), (0, 0), 0.8)
    alpha *= 1 - GRAIN * (grain - grain.min()) / (grain.max() - grain.min())

    rgba = np.zeros((SIZE, SIZE, 4), dtype=np.uint8)
    rgba[..., 0] = 255
    rgba[..., 3] = np.clip(alpha * 255 + 0.5, 0, 255).astype(np.uint8)
    img = Image.fromarray(rgba, 'RGBA')
    img.save(os.path.join(OUT_DIR, 'deathblow_dot.png'))
    with open(os.path.join(OUT_DIR, 'deathblow_dot.rgba'), 'wb') as f:
        f.write(img.tobytes())


if __name__ == '__main__':
    build()
    print('deathblow_dot.png / .rgba written')
