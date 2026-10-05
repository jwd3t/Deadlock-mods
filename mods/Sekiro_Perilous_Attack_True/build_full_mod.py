import os
import sys
import struct
import zlib
import zipfile

def build_vpk(file_dict, output_path):
    """
    Empaqueta un diccionario de archivos {ruta_interna: bytes} en un archivo VPK (v1).
    Totalmente autonomo sin librerias externas.
    """
    tree = {}
    for path, data in file_dict.items():
        dirname, basename = os.path.split(path)
        name, ext = os.path.splitext(basename)
        ext = ext.lstrip('.')
        if ext not in tree:
            tree[ext] = {}
        if dirname not in tree[ext]:
            tree[ext][dirname] = {}
        tree[ext][dirname][name] = data

    data_blobs = []
    current_offset = 0
    dir_entries = []
    for ext, dirs in tree.items():
        for dirname, files in dirs.items():
            for name, data in files.items():
                crc = zlib.crc32(data) & 0xffffffff
                length = len(data)
                offset = current_offset
                current_offset += length
                data_blobs.append(data)
                dir_entries.append((ext, dirname, name, crc, offset, length))

    tree_bytes = bytearray()
    for ext, dirs in tree.items():
        tree_bytes.extend(ext.encode('utf-8') + b'\x00')
        for dirname, files in dirs.items():
            dir_str = dirname.replace('\\', '/') if dirname else ' '
            tree_bytes.extend(dir_str.encode('utf-8') + b'\x00')
            for name, data in files.items():
                tree_bytes.extend(name.encode('utf-8') + b'\x00')
                entry_offset = None
                for e in dir_entries:
                    if e[0] == ext and e[1] == dirname and e[2] == name:
                        entry_offset = e[4]
                        break
                crc = zlib.crc32(data) & 0xffffffff
                length = len(data)
                entry_struct = struct.pack('<IHHIIH', crc, 0, 0x7fff, entry_offset, length, 0xffff)
                tree_bytes.extend(entry_struct)
            tree_bytes.append(0)
        tree_bytes.append(0)
    tree_bytes.append(0)

    tree_size = len(tree_bytes)
    header = struct.pack('<III', 0x55aa1234, 1, tree_size)

    with open(output_path, 'wb') as f:
        f.write(header)
        f.write(tree_bytes)
        for blob in data_blobs:
            f.write(blob)


def main():
    base_dir = os.path.dirname(os.path.abspath(__file__))
    extracted_dir = os.path.join(base_dir, 'extracted')
    output_vpk = os.path.join(base_dir, 'pak01_dir.vpk')
    output_zip = os.path.join(base_dir, 'Sekiro_Perilous_Attack_True.zip')

    print(f"Empaquetando Sekiro Perilous Attack True Mod...")
    # 1. Recopilar archivos
    files_to_pack = {}
    for root, dirs, files in os.walk(extracted_dir):
        for f in files:
            full_path = os.path.join(root, f)
            rel_path = os.path.relpath(full_path, extracted_dir).replace('\\', '/')
            with open(full_path, 'rb') as fp:
                files_to_pack[rel_path] = fp.read()
            print(f"  + {rel_path} ({len(files_to_pack[rel_path])} bytes)")

    # 2. Generar pak01_dir.vpk
    build_vpk(files_to_pack, output_vpk)
    print(f"Generado VPK: {output_vpk} ({os.path.getsize(output_vpk)} bytes)")

    # 3. Generar zip para distribucion
    with zipfile.ZipFile(output_zip, 'w', zipfile.ZIP_DEFLATED) as z:
        z.write(output_vpk, 'pak01_dir.vpk')
        readme_content = """Sekiro Perilous Attack True ("危") Mod for Deadlock
===========================================================
Features (True Version - Fiel a Sekiro):
- Warning ONLY appears above your head when an enemy charges heavy melee nearby (128 units proximity)
- Silent when you attack (self-immunity)
- Calibrated height (+75.0) resting right above the hero's head
- Half size (radius 21.0) for authentic, clean, non-obtrusive telegraph
- Authentic Sekiro Perilous Attack sound with boosted volume (+8 dB in-engine gain)
- Full sound duration fix (no cut-off)

Installation:
Drop pak01_dir.vpk into your Deadlock addons folder:
<Steam>/steamapps/common/Deadlock/game/citadel/addons/
Or load it with Deadlock Mod Manager.
"""
        z.writestr('README.txt', readme_content)

    print(f"Generado ZIP: {output_zip} ({os.path.getsize(output_zip)} bytes)")
    print("¡Listo! Todo empaquetado correctamente.")


if __name__ == '__main__':
    main()
