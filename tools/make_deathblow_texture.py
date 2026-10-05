"""Generates the Sekiro deathblow dot texture procedurally.

The radial profile below was measured from an in-game Sekiro capture
(assets/source/sekiro_deathblow_cropped.png, dot centered at 102,102) with the
background color subtracted, so the texture holds pure emitted light. It is meant
to be rendered with PARTICLE_OUTPUT_BLEND_MODE_ADD.

Outputs (in mods/Sekiro_Deathblow_Mod/):
  deathblow_dot.png          straight RGBA, used to build the .vtex_c
  deathblow_dot_preview.png  the dot added over the reference background, for comparison
"""
import os
import numpy as np
from PIL import Image

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
OUT_DIR = os.path.join(ROOT, 'mods', 'Sekiro_Deathblow_Mod')
SIZE = 256

# Measured radius in reference pixels -> observed median RGB
PROFILE = [
    # The white/yellow core in the capture (r < 10) is Sekiro's lock-on dot, not part of the
    # deathblow effect, so the red halo is extended inward instead.
    (0,  (255,  36,  22)),
    (10, (240,  34,  20)),
    (12, (212,  32,  18)),
    (16, (192,  30,  18)),
    (20, (164,  28,  19)),
    (24, (135,  27,  20)),
    (28, (108,  31,  26)),
    (32, ( 89,  36,  34)),
    (36, ( 75,  44,  44)),
    (40, ( 70,  54,  59)),
]
BACKGROUND = np.array([70, 50, 55], dtype=np.float64)
REF_RADIUS = 40.0  # reference radius that maps to the texture edge


def emission_profile():
    radii = np.array([p[0] for p in PROFILE], dtype=np.float64) / REF_RADIUS
    rgb = np.array([p[1] for p in PROFILE], dtype=np.float64)
    # The center is clipped at 255 in the capture, so keep it as-is; beyond it, remove the scene behind the glow.
    core = radii <= 10 / REF_RADIUS
    emitted = np.where(core[:, None], rgb, np.clip(rgb - BACKGROUND, 0, 255))
    emitted[-1] = 0  # fully dark at the edge
    return radii, emitted


def build():
    radii, emitted = emission_profile()
    yy, xx = np.mgrid[:SIZE, :SIZE].astype(np.float64)
    c = (SIZE - 1) / 2
    r = np.hypot(xx - c, yy - c) / (SIZE / 2)
    rgb = np.stack([np.interp(r, radii, emitted[:, i], right=0) for i in range(3)], axis=-1)

    # Straight alpha so color * alpha == emitted light, works for both ADD and ALPHA blending.
    alpha = rgb.max(axis=-1, keepdims=True) / 255.0
    straight = np.where(alpha > 0, rgb / np.maximum(alpha, 1e-6), 0)
    rgba = np.concatenate([straight, alpha * 255.0], axis=-1)
    Image.fromarray(np.clip(rgba + 0.5, 0, 255).astype(np.uint8), 'RGBA').save(
        os.path.join(OUT_DIR, 'deathblow_dot.png'))

    # Preview: add the dot over the reference capture, scaled to the same size as the real one.
    ref = Image.open(os.path.join(ROOT, 'assets', 'source', 'sekiro_deathblow_cropped.png')).convert('RGB')
    ref_arr = np.asarray(ref).astype(np.float64)
    dot = Image.fromarray(np.clip(rgb, 0, 255).astype(np.uint8)).resize((int(REF_RADIUS * 2),) * 2, Image.LANCZOS)
    side = np.asarray(ref).copy().astype(np.float64)
    side[:] = BACKGROUND  # flat background version
    x0 = 102 - int(REF_RADIUS)
    side[x0:x0 + dot.height, x0:x0 + dot.width] += np.asarray(dot)
    preview = np.concatenate([ref_arr, np.clip(side, 0, 255)], axis=1).astype(np.uint8)
    Image.fromarray(preview).resize((800, 400), Image.LANCZOS).save(
        os.path.join(OUT_DIR, 'deathblow_dot_preview.png'))


if __name__ == '__main__':
    build()
    print('deathblow_dot.png written')
