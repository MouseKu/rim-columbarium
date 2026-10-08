# Script guide

Run commands from the repository root. Python image scripts require Pillow; flower-fitting scripts also require NumPy. Building the mod requires the .NET SDK and a RimWorld 1.6 installation for assembly references. NuGet restores the compile-only `Lib.Harmony.Ref` package; the installed [Harmony mod](https://github.com/pardeike/HarmonyRimWorld) supplies Harmony at game runtime.

## Build the mod

`build.ps1` compiles the C# project and copies the mod files into `dist/Columbarium/`. Pass the RimWorld installation path if the script cannot find it automatically:

```powershell
.\scripts\build.ps1 -RimWorldPath 'D:\SteamLibrary\steamapps\common\RimWorld'
```

The compiled DLL is `dist/Columbarium/Assemblies/Columbarium.dll`.

## Recalculate flower placement

These scripts read the current flower textures, ThingDefs, and C# layout. Each one **rewrites a C# layout file** and saves a coverage report in `Preview/`. Rebuild the mod after running one.

| Script | Use it when | Files written |
| --- | --- | --- |
| `fit_niche_flowers.py` | Changing flowers on the standard 8-niche building. Add `--keep-sizes` to keep the current flower sizes and adjust placement only. | `Source/Columbarium/NicheFlowerLayout.cs`, `Preview/flower_coverage.json` |
| `fit_compact_niche_flowers.py` | Changing flowers on the compact 8-niche building. | `Source/Columbarium/CompactNicheFlowerLayout.cs`, `Preview/compact_flower_coverage.json` |
| `fit_compact_thirtytwo_flowers.py` | Changing flowers on the compact 32-niche building. It imports shared fitting values from `fit_compact_niche_flowers.py`. | `Source/Columbarium/CompactThirtyTwoFlowerLayout.cs`, `Preview/compact_thirtytwo_flower_coverage.json` |

For example:

```powershell
python scripts/fit_niche_flowers.py --keep-sizes
```

`niche_art.py` contains frame and plaque measurements shared by the fitting and 8-niche preview scripts. It is an imported module, not a command to run.

## Generate textures or previews

| Script | What it does | Files written |
| --- | --- | --- |
| `prepare_vertical_orientation_art.py` | Rebuilds east and west textures and masks for the 8- and 32-niche buildings from their current south-facing fronts. **This changes game textures.** | `Textures/Things/*_east*.png`, `Textures/Things/*_west*.png`, `Preview/*_orientation.png` |
| `preview_arttall_runtime.py` | Renders both 8-niche footprints using current runtime geometry. `--height-test` adds a taller-front comparison without changing game definitions. | Preview images under `Preview/`; transparent samples under `Preview/Sample/` |
| `preview_standard_flowers.py` | Shows each flower on the standard 8-niche building. | `Preview/StandardFlowers/`, `Preview/standard_flowers_all.png` |
| `preview_compact_flowers_all.py` | Shows each flower on the compact 8-niche building. | `Preview/CompactFlowers/`, `Preview/compact_flowers_all.png` |
| `preview_compact_thirtytwo_flowers.py` | Shows each flower on the compact 32-niche building, along with empty, partly filled, and full views. | `Preview/CompactThirtyTwoFlowers/`, `Preview/compact_thirtytwo_flowers_all.png`, `Preview/thirtytwo_compact_*.png` |
| `preview_thirtytwo_orientations.py` | Compares the 32-niche front and side artwork with the building footprints. | `Preview/thirtytwo_orientations_*.png` |

Preview scripts read the current files in `Textures/Things/`, `Defs/`, and `Source/`. They do not modify the game DLL.
