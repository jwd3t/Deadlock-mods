"""Generates the Sekiro deathblow dot texture from a profile fitted to in-game footage.

Source: "Enfeebled Deathblow Animation - Sekiro.mp4" (1280x720, 29.97 fps), frames 114-120, dot near
(657, 283); frames 108-109 (no dot) are the background. The dot is pure red (255, 0, 0) and hides
the scene behind it, so it is modeled as out = bg * (1 - a) + red * a. For every 2 px ring, `a` is the
least-squares fit over all three channels (lock-on dot pixels excluded), then made non-increasing
and smoothed. The white center seen in captures is Sekiro's lock-on dot, not part of this effect.

Render with PARTICLE_OUTPUT_BLEND_MODE_ALPHA, overbright 1.0.
Output: mods/Sekiro_Deathblow_Mod/deathblow_dot.png and .rgba (input for `vpcf_tool vtex`).
"""
import os
import numpy as np
from PIL import Image

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
OUT_DIR = os.path.join(ROOT, 'mods', 'Sekiro_Deathblow_Mod')
SIZE = 256

# Fitted alpha every 2 px from the center (720p video pixels); the texture edge maps to the last entry.
ALPHA = [0.763, 0.752, 0.735, 0.715, 0.694, 0.674, 0.658, 0.643, 0.627, 0.607, 0.581, 0.55, 0.518, 0.484,
         0.451, 0.417, 0.382, 0.348, 0.314, 0.283, 0.253, 0.224, 0.197, 0.172, 0.148, 0.128, 0.11, 0.096,
         0.084, 0.073, 0.064, 0.057, 0.052, 0.049, 0.047, 0.045, 0.043, 0.041, 0.038, 0.035, 0.032, 0.0]


def build():
    radii = np.linspace(0, 1, len(ALPHA))
    yy, xx = np.mgrid[:SIZE, :SIZE].astype(np.float64)
    c = (SIZE - 1) / 2
    r = np.hypot(xx - c, yy - c) / (SIZE / 2)
    alpha = np.interp(r, radii, ALPHA, right=0)

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
