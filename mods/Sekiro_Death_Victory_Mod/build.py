"""Builds the Sekiro Death & Victory mod from the CURRENT game files.

The stylesheets and sound-event files this mod touches are monolithic: overriding them with a stale copy
silently reverts whatever Valve changed since (v1 did that to Stinger.RevealVote). So every build
extracts the game's current versions and applies only the mod's changes from src/:
  src/hud.css, src/abilities_frank.css, src/hud_match_end.css   appended to the game's stylesheets
  src/ui.json, src/music.json                                    edits to the game's sound events
Run again after a Deadlock update.

Requires the .NET SDK (tools/vpcf_tool) and the game installed at GAME_VPK.
"""
import os
import re
import subprocess
import sys

BASE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(BASE, '..', '..'))
GAME_VPK = r'C:\Program Files (x86)\Steam\steamapps\common\Deadlock\game\citadel\pak01_dir.vpk'
EXTRACTED = os.path.join(BASE, 'extracted')
BUILD = os.path.join(BASE, 'build')
SRC = os.path.join(BASE, 'src')

DEATH_TRUE = 'panorama/images/hud/sekiro_death_true.vtex'
DEATH_FAKE = 'panorama/images/hud/sekiro_death_fake.vtex'
VICTORY = 'panorama/images/hud/sekiro_victory.vtex'

STYLES = [  # (game file, css source, images the css uses)
    ('panorama/styles/hud.vcss_c', 'hud.css', [DEATH_TRUE, DEATH_FAKE]),
    ('panorama/styles/ability_hud_elements/abilities_frank.vcss_c', 'abilities_frank.css', [DEATH_FAKE]),
    ('panorama/styles/hud_match_end.vcss_c', 'hud_match_end.css', [VICTORY]),
]
SOUND_EVENTS = [
    ('soundevents/ui.vsndevts_c', 'ui.json'),
    ('soundevents/music.vsndevts_c', 'music.json'),
]


def tool(*args):
    cmd = ['dotnet', 'run', '--project', os.path.join(ROOT, 'tools', 'vpcf_tool'), '--no-build', '--', *args]
    result = subprocess.run(cmd, capture_output=True, text=True)
    if result.returncode != 0 or 'Unhandled exception' in result.stdout + result.stderr:
        sys.exit(f'vpcf_tool {args[0]} failed:\n{result.stdout}{result.stderr}')
    return result.stdout


def game_file(path):
    out = os.path.join(BUILD, 'game', path)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    tool('extract', GAME_VPK, path, out)
    return out


def target(path):
    out = os.path.join(EXTRACTED, path)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    return out


def main():
    subprocess.run(['dotnet', 'build', os.path.join(ROOT, 'tools', 'vpcf_tool'), '-nologo', '-v', 'q'], check=True,
                   capture_output=True)

    for path, css, images in STYLES:
        # The game's compiled stylesheets carry no comments; strip ours rather than rely on the parser.
        text = re.sub(r'/\*.*?\*/', '', open(os.path.join(SRC, css), encoding='utf-8').read(), flags=re.S)
        stripped = os.path.join(BUILD, css)
        os.makedirs(BUILD, exist_ok=True)
        open(stripped, 'w', encoding='utf-8').write(text)
        print(tool('vcss-append', game_file(path), stripped, target(path), *images).strip())
    for path, patch in SOUND_EVENTS:
        print(tool('kv-patch', game_file(path), os.path.join(SRC, patch), target(path)).strip())

    subprocess.run([sys.executable, os.path.join(BASE, 'make_victory_image.py')], check=True)
    print(tool('vtex-raw', target(DEATH_TRUE + '_c'), os.path.join(BUILD, 'sekiro_victory.rgba'), '1024', '576',
               target(VICTORY + '_c')).strip())

    sys.path.insert(0, os.path.join(ROOT, 'tools'))
    import package_all_mods
    package_all_mods.pack_mod('Sekiro_Death_Victory_Mod', BASE, 'Sekiro Death & Victory Mod', '''Features:
- Your own death: the red Sekiro kanji 死 (DEATH) on screen with the Sekiro death sound, once, in every mode
- Dying while holding the Rejuvenator's rebirth: the grey fake-death 死, as in Sekiro when you can resurrect
- Victor's Shocking Reanimation: the grey fake-death 死 over the revive bar
- Winning the match: 忍殺 (SHINOBI EXECUTION) on the match-end screen with the Sekiro victory fanfare
- Rebuilt from the current game files, so it never reverts other game changes
- 100% VAC-safe client-side cosmetic mod''')


if __name__ == '__main__':
    main()
