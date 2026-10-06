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
- The Perilous Attack mod lives only in `Sekiro_Melee_Deadlock` (repo `jwd3t/Sekiro-melee`, zip named `Sekiro_Perilous_Attack_True.zip`). Versions are git tags + GitHub releases there: v1.0 (Tenor GIF, 25 frames at 200 px), v2.0 (green screen capture; the user found it worse in game than v1.0), v1.1 (v1.0 again with optical-flow midpoints: 49 frames at 288 px, 7x7 in a 2048 atlas via `vpcf_tool sheet-vtex`). Never overwrite an old version, add a new tag. The old loose copies were removed from this repo (still in its history).

## Commands

Packaging (pure-Python VPK v1 writer, no deps):
```bash
python tools/package_all_mods.py          # rebuilds Deathblow and Death_Victory
python mods/Sekiro_Melee_Deadlock/make_kanji_atlas.py   # Perilous Attack: 危 flipbook (v1.1: Tenor GIF frames, 49 frames, 2048 atlas)
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
dotnet run --project tools/vpcf_tool -- sheet-vtex <template.vtex_c> <image.rgba> <size> <cols> <rows> <cell> <margin> <frames> <out>   # animated flipbook, any grid (RED2 copied from template)
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

- **Deathblow** (`mods/Sekiro_Deathblow_Mod`): `melee_parry_debuff.vpcf` places it at the game's CP0 for the debuff (upper chest; Gemini removed vanilla's +70 head offset); `melee_parry_debuff_symbol.vpcf` draws it. Best reference: "Sekiro But Every Perilous Attack Has a Vine Boom.mp4" (1080p60), 1:10.7-1:11.0, Genichiro. Measured there per ring: the scene's green/blue under the dot drop to ~20% while red light is added on top (~150 at r=24 px, 0 at ~96 px), with a small warm orange-white core that belongs to the effect (the separate small white dot is the lock-on) and ~6% grain; constant size while active. So the dot is two renderers, like Deadlock's own marks (Valve uses ADD with overbright and MOD2X tints, never opaque sprites for glows): MOD2X `sekiro_deathblow_tint` (red kept, green/blue scaled) + ADD `sekiro_deathblow_dot` (the measured light), both from `tools/make_deathblow_texture.py`, applied with `vpcf_tool deathblow-anim <file> 18`. Deadlock adaptation after an in-game test (5/10: pinkish edges, dim, at the neck): tint uses 1-(1-occlusion)^2 so half-tinted edges over bright floors stay red, the light renders at overbright 2, and `vpcf_tool parent-offset <melee_parry_debuff.vpcf_c> -6` lowers the anchor from the `aim` attachment (the parent's CreateWithinBox position becomes child CP3 via SetChildControlPoints; on the hunched stunned dummy `aim` sits at the neck). Simulating the two layers over the measured background reproduces the footage within ~5% per ring. Lifecycle follows Valve's `area_leash_h`: no `C_OP_Decay`, lives until the stun's end cap, then `C_OP_LerpEndCapScalar` (alpha) + `C_OP_EndCapTimedDecay` fade it over 0.2 s; 0.12 s fade-in contracting from 1.3x; slow spin so the grain shimmers. Failed attempts, in order: opaque red disc fitted to a dark clip (pink cloud, hidden in the body), solid disc with synthetic grain (sticker), the isolated reference image `assets/source/deathblow_reference_isolated.png` (too orange), that image + pink glow + overbright (orange, halo too big). The dummy renders translucent after particles, so its head can still cover the dot; opaque heroes do not. Always ask the user for an in-game screenshot before releasing; previews outside the game have been misleading.
- **Perilous kanji**: see `mods/Sekiro_Melee_Deadlock/make_kanji_atlas.py`. Lesson from v2.0: a cleaner/higher-res source is not automatically better in game; the user preferred v1.0's look, so improvements keep the approved look and add frames/resolution.
