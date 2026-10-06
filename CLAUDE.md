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
python tools/package_all_mods.py          # rebuilds Deathblow (Death_Victory has its own build.py)
python mods/Sekiro_Death_Victory_Mod/build.py   # rebuilds from the CURRENT game files; rerun after every Deadlock update
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
dotnet run --project tools/vpcf_tool -- deathblow-anim <symbol.vpcf_c> <radius>          # rewrites the deathblow symbol (renderers, ops, refs)
dotnet run --project tools/vpcf_tool -- parent-offset <melee_parry_debuff.vpcf_c> <z>   # moves the deathblow anchor from the aim attachment
dotnet run --project tools/vpcf_tool -- scan "<Deadlock>/game/citadel/pak01_dir.vpk" <field>...   # how vanilla particles use a field (SCAN_TOP=n rows; `_class` lists operator usage)
dotnet run --project tools/vpcf_tool -- extract "<Deadlock>/game/citadel/pak01_dir.vpk" <internal path> <out>
dotnet run --project tools/vpcf_tool -- grep "<Deadlock>/game/citadel/pak01_dir.vpk" <extension> <regex>   # search decompiled game files (vcss_c, vxml_c, vsndevts_c...)
dotnet run --project tools/vpcf_tool -- vcss-append <game.vcss_c> <extra.css> <out> [image refs...]   # game stylesheet + mod rules
dotnet run --project tools/vpcf_tool -- kv-patch <game .vsndevts_c> <patch.json> <out>                  # game sound events + mod edits
dotnet run --project tools/vpcf_tool -- vtex-raw <template.vtex_c> <image.rgba> <w> <h> <out>          # BGRA8888 (Panorama images)
dotnet run --project tools/vpcf_tool -- roundtrip <file> <out>                                          # check a type re-serializes faithfully
```
`vpcf_tool/Program.cs` is one `switch` with 15 commands (on `main`; the `deathblow-grain` branch differs); when replacing a case by slicing the file, check `grep -c 'case "'` afterwards (a slice once silently deleted `timing`/`scan`/`extract`). Before inventing particle fields, `scan` the game for how Valve uses them and `extract` + `dump` an example.

There are no tests; the verification loop is: rebuild → `vrf_dumper` on the VPK → decode textures to PNG and simulate the blend over the user's in-game screenshot → user tests in-game (Sandbox) and sends a screenshot. Previews outside the game have repeatedly been misleading; the user's in-game verdict is what counts, and nothing is released before it. Game files: `C:\Program Files (x86)\Steam\steamapps\common\Deadlock\game\citadel\pak01_dir.vpk`.

## Hard-won rules (from SKILL.md and past sessions)

- Never write into the Steam `addons` folder yourself; produce the VPK/ZIP in the repo and let the user install.
- **Textures for particles must be DXT5/BC3.** Uncompressed RGBA renders as the red/orange checkerboard "X". `vtex` in `vpcf_tool` clones the header from an existing 256×256 DXT5 no-mip `.vtex_c` (2068-byte header + 65536 bytes of blocks), so the template must match size/format.
- `Texture.Serialize()` is not implemented in VRF; animated flipbooks (`SHEET` extra data) are built at the binary level — see SKILL.md §2B. In `SHEET`, `TotalTime`/`DisplayTime` are tick units, not seconds (`TotalTime = N-1`, `DisplayTime = 1` except last frame `0`), or the flipbook plays in ~30 ms.
- Particle resource paths (`m_hTexture`, child refs) need `KVFlag.Resource`; plain strings give a null texture.
- `C_OP_DistanceCull` only works with `m_bCullInside = true` (engine early-exit bug); for max-distance cutoff use `C_OP_DistanceToTransform` with `PARTICLE_SET_SCALE_CURRENT_VALUE`, placed **last** in `m_Operators`. Float inputs must be authored as scalars, not `{m_nType, m_flLiteralValue}` tables.
- Billboards anchored to player entities render rotated 90° clockwise; pre-rotate images 90° CCW.
- Sounds: never hex-patch `.vsnd_c` (LZ4 control block breaks → silence); re-serialize via VRF. Update `vsnd_duration` in the `.vsndevts_c` or the clip gets cut at the vanilla length. Overriding `soundevents/player.vsndevts_c` clobbers other sound mods.
- Full-screen effects (death/victory kanji) belong in Panorama CSS (`hud.vcss_c`), not world particles.
- **Never ship a stale copy of a monolithic game file** (stylesheets, `.vsndevts_c`): build it from the current game file plus the mod's changes (`vcss-append`, `kv-patch`) and rebuild after updates. The v1 Death_Victory copies silently reverted Valve's `Stinger.RevealVote` change. Register every image a stylesheet uses in its external references, as Valve does.
- The main HUD layout does not decompile; panel ids and HUD classes live in `game/citadel/bin/win64/client.dll` strings (e.g. `gameplay_hud_dead`; root classes `dead`, `alive`, `deathReplayActive`, `rebirth`, `permadeath`, `GameState*`). Match end: `LocalPlayerTeamN` + `TeamNVictory`.
- Sound event meaning: `UI.PlayerDeath.Team/Opponent` are notifications for an ally/enemy dying (listed with `BossTier1.Death.Friendly/Enemy`), NOT your own death; your death is `Stinger.Death` (`music_stinger_player_death`). Death_Victory plays its clip from a new `Sekiro.Death` event via the death panel's CSS `sound:` and silences `Stinger.Death`.

## Releases

Both repos publish GitHub releases from tags via `.github/workflows/release.yml`: every push creates the releases missing for existing tags, attaching the mod zip as committed at that tag. Here tags are `deathblow-v<x>` / `death-victory-v<x>`; in `Sekiro_Melee_Deadlock` they are `v<x>`. Release notes go in `release-notes/<tag>.md` (Spanish). To release: rebuild the zip, commit, write the notes, tag, push with `--tags`. `gh` is not installed and the repos are private, so release state can only be checked on GitHub by the user.

Release state: `deathblow-v2.1` (approved), `deathblow-v2.1-granulado` (same dot plus the first animated-grain build, white core, tagged on branch `deathblow-grain`; the user preferred it over the warm-orange-core attempt, which was reverted), `death-victory-v2.0` (released at the user's request before an in-game test), Perilous `v1.1`. `deathblow-v2.0` was a rejected build. Variant tags like `deathblow-v2.1-granulado` work with the workflow (asset `Sekiro_Deathblow_Mod-granulado.zip`).

## Matching Sekiro effects

Without Sekiro's files, effects are matched against footage the user provides (kept in `Downloads`). The method that worked: decode with OpenCV (`cv2`; no ffmpeg on PATH), find the effect frames, subtract a background frame from just before it appears, and fit per-ring alpha/color by least squares; check by compositing the result over the real background next to the real frame. Green screen captures (perilous kanji) are keyed by color relationships, not by a plain green distance, because glows get mixed into the green.

- **Deathblow** (`mods/Sekiro_Deathblow_Mod`) — **approved in game by the user** ("ahora sí se ve muy bien") at commit `7399282`; treat that look as the baseline and change it only in small, testable steps.
  - Placement: the game creates `melee_parry_debuff.vpcf` on the enemy's `aim` attachment; its CreateWithinBox position becomes child CP3 via `C_OP_SetChildControlPoints`, and `melee_parry_debuff_symbol.vpcf` draws at CP3. Vanilla offsets +70 (stun stars over the head); now -6 (`parent-offset`) because on the hunched stunned dummy `aim` sits at the neck.
  - Reference: "Sekiro But Every Perilous Attack Has a Vine Boom.mp4" (1080p60), 1:10.7-1:11.0, Genichiro over a dark background ~(24,22,20). Per ring: the scene's green/blue under the dot drop to ~20% while red light is added on top (~150 at r=24 px, 0 at ~96 px), a small warm orange-white core that belongs to the effect (the separate small white dot is the lock-on), ~6% grain, constant size while active.
  - Build: two renderers, like Deadlock's own marks (Valve uses ADD with overbright 2-15 and MOD2X tints; opaque alpha sprites read as stickers): MOD2X `sekiro_deathblow_tint` (red kept, green/blue scaled by 1-occlusion, with occlusion pushed to 1-(1-occ)^2 so edges over bright floors stay red instead of pink) + ADD `sekiro_deathblow_dot` (measured light, overbright 2 because Deadlock's tonemapping dims it). Textures from `tools/make_deathblow_texture.py`, particle from `vpcf_tool deathblow-anim <file> 18`. Both layers: no depth test, no feathering, self-illuminated.
  - Lifecycle (Valve's `area_leash_h` pattern): no `C_OP_Decay`; lives until the stun's end cap, then `C_OP_LerpEndCapScalar` (alpha) + `C_OP_EndCapTimedDecay` fade it over 0.2 s. 0.12 s fade-in contracting from 1.3x; slow `C_OP_SpinUpdate` from a random angle so the grain shimmers.
  - Failed attempts (don't repeat): opaque red disc fitted to a dark clip (pink cloud, hidden inside the body, washed out by scene light); solid disc with synthetic grain (sticker); the isolated reference image `assets/source/deathblow_reference_isolated.png` (more orange than the game); that image + pink glow + overbright 3 (orange, halo too big); measured layers at overbright 1.05 without the offset (5/10: pinkish edges, dim, at the neck).
  - Known limit: the Sandbox dummy renders translucent after particles, so its head can cover the dot; opaque heroes do not.
  - Possible next steps the user has not chosen yet: the orange-white posture-break flash with sparks when the dot appears (footage frames 4236-4240, biggest visible gap), stronger animated grain (DXT5 softens it), a slight expansion while fading out, a warmer (less white) core.
- **Perilous kanji**: see `mods/Sekiro_Melee_Deadlock/make_kanji_atlas.py`. Lesson from v2.0: a cleaner/higher-res source is not automatically better in game; the user preferred v1.0's look, so improvements keep the approved look and add frames/resolution.
