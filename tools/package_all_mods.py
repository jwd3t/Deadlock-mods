import os
import sys
import struct
import zlib
import zipfile

def build_vpk(file_dict: dict, output_path: str):
    """Packages {internal_rel_path: file_bytes} into a valid Source 2 VPK v1."""
    tree = {}
    for path, data in file_dict.items():
        dirname, basename = os.path.split(path)
        name, ext = os.path.splitext(basename)
        ext = ext.lstrip('.')
        tree.setdefault(ext, {}).setdefault(dirname, {})[name] = data

    data_blobs, dir_entries, current_offset = [], [], 0
    for ext, dirs in tree.items():
        for dirname, files in dirs.items():
            for name, data in files.items():
                crc = zlib.crc32(data) & 0xffffffff
                length = len(data)
                dir_entries.append((ext, dirname, name, crc, current_offset, length))
                data_blobs.append(data)
                current_offset += length

    tree_bytes = bytearray()
    for ext, dirs in tree.items():
        tree_bytes.extend(ext.encode('utf-8') + b'\x00')
        for dirname, files in dirs.items():
            dir_str = dirname.replace('\\', '/') if dirname else ' '
            tree_bytes.extend(dir_str.encode('utf-8') + b'\x00')
            for name in files:
                for e in dir_entries:
                    if e[0] == ext and e[1] == dirname and e[2] == name:
                        tree_bytes.extend(name.encode('utf-8') + b'\x00')
                        tree_bytes.extend(struct.pack('<IHHIIH', e[3], 0, 0x7fff, e[4], e[5], 0xffff))
                        break
            tree_bytes.append(0)
        tree_bytes.append(0)
    tree_bytes.append(0)

    header = struct.pack('<III', 0x55aa1234, 1, len(tree_bytes))
    with open(output_path, 'wb') as f:
        f.write(header)
        f.write(tree_bytes)
        for blob in data_blobs:
            f.write(blob)
    print(f"Created VPK: {output_path} ({os.path.getsize(output_path)} bytes)")


def pack_mod(mod_name: str, mod_dir: str, title: str, description: str):
    print(f"\nPackaging {mod_name}...")
    extracted_dir = os.path.join(mod_dir, "extracted")
    output_vpk = os.path.join(mod_dir, "pak01_dir.vpk")
    output_zip = os.path.join(mod_dir, f"{mod_name}.zip")

    files = {}
    for root, dirs, filenames in os.walk(extracted_dir):
        for f in filenames:
            full = os.path.join(root, f)
            rel = os.path.relpath(full, extracted_dir).replace('\\', '/')
            with open(full, 'rb') as fp:
                files[rel] = fp.read()
            print(f"  + {rel} ({len(files[rel])} bytes)")

    build_vpk(files, output_vpk)

    # Build ZIP for Deadlock Mod Manager
    with zipfile.ZipFile(output_zip, 'w', zipfile.ZIP_DEFLATED) as z:
        z.write(output_vpk, 'pak01_dir.vpk')
        readme = f"""{title} for Deadlock
{"=" * len(title)}
{description}

Installation:
1. Deadlock Mod Manager (Recommended):
   Drag and drop {mod_name}.zip directly into Deadlock Mod Manager.
2. Manual Installation:
   Copy pak01_dir.vpk into your Deadlock addons folder:
   <Steam>/steamapps/common/Deadlock/game/citadel/addons/
"""
        z.writestr('README.txt', readme)

    print(f"Created ZIP: {output_zip} ({os.path.getsize(output_zip)} bytes)")


if __name__ == '__main__':
    repo = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'mods')

    # Mod 1
    pack_mod(
        mod_name="Sekiro_Deathblow_Mod",
        mod_dir=os.path.join(repo, "Sekiro_Deathblow_Mod"),
        title="Sekiro Deathblow Red Dot Mod",
        description="""Features:
- Crimson glowing Deathblow target dot projected onto parried / stunned enemies
- Authentic heavy Sekiro posture break and deathblow gong sound effect
- Clean 100% VAC-safe client-side cosmetic mod"""
    )

    # Mod 2
    pack_mod(
        mod_name="Sekiro_Death_Victory_Mod",
        mod_dir=os.path.join(repo, "Sekiro_Death_Victory_Mod"),
        title="Sekiro Death, Victory & Resurrection Mod",
        description="""Features:
- True Death Screen: Calligraphic crimson kanji '死' (Death) centered on screen with authentic Sekiro death stinger audio
- Victory: Calligraphic kanji '忍殺' (Shinobi Execution) with triumphant Sekiro gong and victory audio upon winning the match
- Fake Death for Victor (Frank): Taking lethal damage with revival ultimate active triggers the misty '回生' (Resurrection) state before shocking reanimation
- Fake Death for Rejuvenator: Carrying the Mid-Boss Rejuvenator buff triggers the Sekiro '回生' resurrection effect upon defeat
- 100% VAC-safe client-side cosmetic mod"""
    )

    # Mod 3
    pack_mod(
        mod_name="Sekiro_Perilous_Attack_True",
        mod_dir=os.path.join(repo, "Sekiro_Perilous_Attack_True"),
        title="Sekiro Perilous Attack True Warning Mod",
        description="""Features (True Version - Authentic to Sekiro):
- Animated Danger Kanji '危' flipbook with flowing white energy streaks across brush strokes and expanding shockwave aura pulse
- Warning ONLY appears above your head when an enemy charges heavy melee nearby (128 units / 13m proximity)
- Silent when you attack (100% self-immunity)
- Calibrated height (+75.0) resting directly above the hero's head
- Authentic half-size radius (21.0) for a clean, non-obtrusive telegraph
- Authentic Sekiro Perilous Attack sound with boosted in-engine gain (+8 dB) and full duration fix
- 100% VAC-safe client-side cosmetic mod"""
    )
