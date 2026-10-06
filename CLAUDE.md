# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Client-side cosmetic mods for Valve's **Deadlock** (Source 2) that recreate *Sekiro: Shadows Die Twice* effects (deathblow dot, perilous-attack kanji, death/victory screens, parry sparks). There is no app to build: each mod is a set of compiled Source 2 resources packed into a `pak01_dir.vpk` (+ a `.zip` for Deadlock Mod Manager). The user communicates in Spanish; mod READMEs are in Spanish.

The full modding runbook (binary formats, engine quirks, reverse-engineered `particles.dll` behavior) lives in `C:\Users\juan\Documents\GitHub\Deadlock-Modding-Skill\SKILL.md`. Read the relevant section before touching `.vtex_c`, `.vpcf_c`, `.vsnd_c` or `.vsndevts_c` files.

## Layout and data flow

```
mods/<Mod>/extracted/<game path>   compiled resources, mirroring the in-game path they override
        └─► pack ─► mods/<Mod>/pak01_dir.vpk + <Mod>.zip
tools/      packaging + asset tooling (Python, C#)
assets/     source art/audio (source/), untouched game files for reference (vanilla/), compiled leftovers (compiled/)
dumps/      decompiled text dumps of game files (KV3, vcss, vsndevts) used as reference when editing
```

- A file overrides the game asset with the same internal path, e.g. `extracted/particles/abilities/melee/melee_parry_debuff_symbol.vpcf_c`. Everything under `extracted/` is packed verbatim; never put scratch files there.
- `mods/Sekiro_Melee_Deadlock/` and `mods/Sekiro_Parry_Sparks/` are **separate git repos** (their own GitHub remotes) and are git-ignored here. Commit inside them separately.
- The Perilous Attack mod lives only in `Sekiro_Melee_Deadlock` (repo `jwd3t/Sekiro-melee`, zip named `Sekiro_Perilous_Attack_True.zip`). Versions are git tags + GitHub releases there (v1.0 Tenor GIF animation, v2.0 green screen animation); never overwrite an old version, add a new tag. The old loose copies were removed from this repo (still in its history).

## Commands

Packaging (pure-Python VPK v1 writer, no deps):
```bash
python tools/package_all_mods.py          # rebuilds Deathblow and Death_Victory
python mods/Sekiro_Melee_Deadlock/make_kanji_atlas.py   # Perilous Attack: 危 flipbook from the green screen video
python mods/Sekiro_Melee_Deadlock/build_full_mod.py
python mods/Sekiro_Parry_Sparks/build_mod.py
python mods/Sekiro_Parry_Sparks/configure_parry.py   # GUI/CLI color+size configurator; can install into the Deadlock addons folder
```
To rebuild a single mod from `package_all_mods.py`, import it and call `pack_mod(name, mod_dir, title, description)`.

C# tooling (.NET 10 SDK; uses ValveResourceFormat 20.x, ValvePak, BCnEncoder):
```bash
dotnet run --project tools/vrf_dumper -- <pak01_dir.vpk>      # verify every entry in a VPK parses (textures/sounds/particles)
dotnet run --project tools/vpcf_tool -- dump  <file.vpcf_c|.vtex_c>          # print KV3 / texture info
dotnet run --project tools/vpcf_tool -- png   <file.vtex_c> <out.png>       # decode a texture to check it
dotnet run --project tools/vpcf_tool -- vtex  <template.vtex_c> <image.rgba> <size> <out.vtex_c>
dotnet run --project tools/vpcf_tool -- blend <file.vpcf_c> <BLEND_MODE> <overbright>
dotnet run --project tools/vpcf_tool -- timing <file.vpcf_c> <lifetime> <fadeInFrac> <fadeOutFrac>
dotnet run --project tools/vpcf_tool -- deathblow-anim <symbol.vpcf_c> <lifetime> <radius>
dotnet run --project tools/vpcf_tool -- scan "<Deadlock>/game/citadel/pak01_dir.vpk" <field>...   # how vanilla particles use a field
dotnet run --project tools/vpcf_tool -- extract "<Deadlock>/game/citadel/pak01_dir.vpk" <internal path> <out>
```
There are no tests; the verification loop is: rebuild → `vrf_dumper` on the VPK → decode textures to PNG and look at them → user tests in-game (Sandbox). Game files: `C:\Program Files (x86)\Steam\steamapps\common\Deadlock\game\citadel\pak01_dir.vpk`.

## Hard-won rules (from SKILL.md and past sessions)

- Never write into the Steam `addons` folder yourself; produce the VPK/ZIP in the repo and let the user install.
- **Textures for particles must be DXT5/BC3.** Uncompressed RGBA renders as the red/orange checkerboard "X". `vtex` in `vpcf_tool` clones the header from an existing 256×256 DXT5 no-mip `.vtex_c` (2068-byte header + 65536 bytes of blocks), so the template must match size/format.
- `Texture.Serialize()` is not implemented in VRF; animated flipbooks (`SHEET` extra data) are built at the binary level — see SKILL.md §2B. In `SHEET`, `TotalTime`/`DisplayTime` are tick units, not seconds (`TotalTime = N-1`, `DisplayTime = 1` except last frame `0`), or the flipbook plays in ~30 ms.
- Particle resource paths (`m_hTexture`, child refs) need `KVFlag.Resource`; plain strings give a null texture.
- `C_OP_DistanceCull` only works with `m_bCullInside = true` (engine early-exit bug); for max-distance cutoff use `C_OP_DistanceToTransform` with `PARTICLE_SET_SCALE_CURRENT_VALUE`, placed **last** in `m_Operators`. Float inputs must be authored as scalars, not `{m_nType, m_flLiteralValue}` tables.
- Billboards anchored to player entities render rotated 90° clockwise; pre-rotate images 90° CCW.
- Sounds: never hex-patch `.vsnd_c` (LZ4 control block breaks → silence); re-serialize via VRF. Update `vsnd_duration` in the `.vsndevts_c` or the clip gets cut at the vanilla length. Overriding `soundevents/player.vsndevts_c` clobbers other sound mods.
- Full-screen effects (death/victory kanji) belong in Panorama CSS (`hud.vcss_c`), not world particles.

## Releases

Both repos publish GitHub releases from tags via `.github/workflows/release.yml`: every push creates the releases missing for existing tags, attaching the mod zip as committed at that tag. Here tags are `deathblow-v<x>` / `death-victory-v<x>`; in `Sekiro_Melee_Deadlock` they are `v<x>`. Release notes go in `release-notes/<tag>.md` (Spanish). To release: rebuild the zip, commit, write the notes, tag, push with `--tags`. `gh` is not installed and the repos are private, so release state can only be checked on GitHub by the user.

## Matching Sekiro effects

Without Sekiro's files, effects are matched against footage the user provides (kept in `Downloads`). The method that worked: decode with OpenCV (`cv2`; no ffmpeg on PATH), find the effect frames, subtract a background frame from just before it appears, and fit per-ring alpha/color by least squares; check by compositing the result over the real background next to the real frame. Green screen captures (perilous kanji) are keyed by color relationships, not by a plain green distance, because glows get mixed into the green.

- **Deathblow** (`mods/Sekiro_Deathblow_Mod`): `melee_parry_debuff.vpcf` anchors to the `aim` attachment (inside the chest); `melee_parry_debuff_symbol.vpcf` draws the dot. Measured: a solid pure red (255,0,0) disc that covers the scene (~0.9 alpha plateau over the inner third, near-linear falloff), fine grain, ~0.12 s fade-in settling from 1.25x, ~0.18 s fade-out expanding to 1.3x. The white center in Sekiro captures is the lock-on dot. In-game test of an earlier build showed the dot hidden inside the body and washed out pink: the renderer now has `m_bDisableZBuffering = true`, `m_nFeatheringMode = "PARTICLE_DEPTH_FEATHERING_OFF"`, `m_flSelfIllumAmount = 1.0` (all found in vanilla particles with `vpcf_tool scan`). Texture: `tools/make_deathblow_texture.py`; particle: `vpcf_tool deathblow-anim <file> 2.5 25`. A dark-scene clip alone is misleading for shape/opacity; check against a bright-scene clip too.
- **Perilous kanji**: see `mods/Sekiro_Melee_Deadlock/make_kanji_atlas.py`.
