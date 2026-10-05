import os
import numpy as np
from PIL import Image

ASSETS = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'assets', 'source')

selected_frame_indices = [0] + list(range(2, 26))
assert len(selected_frame_indices) == 25

def process_frame(img):
    f_rgb = np.array(img).astype(np.float32) / 255.0
    h, w, _ = f_rgb.shape
    
    # Background color estimate from corners
    bg_color = (
        f_rgb[:15, :15].mean(axis=(0, 1)) +
        f_rgb[:15, -15:].mean(axis=(0, 1)) +
        f_rgb[-15:, :15].mean(axis=(0, 1)) +
        f_rgb[-15:, -15:].mean(axis=(0, 1))
    ) / 4.0
    
    # Difference from background
    diff = np.maximum(0.0, f_rgb - bg_color)
    alpha = np.max(f_rgb, axis=2) - np.max(bg_color)
    alpha = np.clip(alpha / (1.0 - np.max(bg_color)), 0.0, 1.0)
    alpha = np.where(alpha < 0.05, 0.0, (alpha - 0.05) / 0.95)
    alpha = np.clip(alpha * 1.3, 0.0, 1.0)
    
    # Outer edge feathering to guarantee perimeter alpha == 0
    dist_x = np.minimum(np.arange(w), w - 1 - np.arange(w))
    dist_y = np.minimum(np.arange(h), h - 1 - np.arange(h))
    edge_dist = np.minimum(dist_x[None, :], dist_y[:, None])
    edge_fade = np.clip(edge_dist / 15.0, 0.0, 1.0)
    alpha = alpha * edge_fade
    
    rgb = np.clip(f_rgb * 255.0, 0, 255).astype(np.uint8)
    a = (alpha * 255.0).astype(np.uint8)
    
    # Zero RGB where alpha is 0
    rgb[a == 0] = 0
    
    out_rgba = np.zeros((h, w, 4), dtype=np.uint8)
    out_rgba[:, :, :3] = rgb
    out_rgba[:, :, 3] = a
    return Image.fromarray(out_rgba, mode='RGBA')

atlas = Image.new('RGBA', (1024, 1024), (0, 0, 0, 0))
atlas_upright = Image.new('RGBA', (1024, 1024), (0, 0, 0, 0))

for idx, f_num in enumerate(selected_frame_indices):
    col = idx % 5
    row = idx // 5
    x = 12 + col * 200
    y = 12 + row * 200
    
    src = Image.open(f'{ASSETS}/tenor_frames/frame_{f_num:03d}.png')
    proc = process_frame(src)
    
    # Upright resized for visual verification
    proc_small_upright = proc.resize((200, 200), Image.Resampling.LANCZOS)
    atlas_upright.paste(proc_small_upright, (x, y), proc_small_upright)
    
    # Rotated 90 deg counter-clockwise for Source 2
    proc_rot = proc.transpose(Image.Transpose.ROTATE_90)
    proc_small_rot = proc_rot.resize((200, 200), Image.Resampling.LANCZOS)
    atlas.paste(proc_small_rot, (x, y), proc_small_rot)

atlas.save(f'{ASSETS}/atlas_rotated_1024.png')
atlas_upright.save(f'{ASSETS}/atlas_upright_1024.png')
with open(f'{ASSETS}/raw_atlas_1024.bin', 'wb') as f:
    f.write(atlas.tobytes())

print(f"Done! Generated raw_atlas_1024.bin ({os.path.getsize(f'{ASSETS}/raw_atlas_1024.bin')} bytes)")
