# Columbarium for RimWorld 1.6

Columbarium lets colonists cremate the dead, keep their ashes in memorial urns, and store those urns in a columbarium. The urn and its niche display a flower chosen from the deceased colonist's strongest skill.

## Install

Install [Harmony](https://github.com/pardeike/HarmonyRimWorld) first. Copy this folder into RimWorld's `Mods` directory, then enable **Harmony** and **Columbarium** in that order. The mod includes a prebuilt `Assemblies/Columbarium.dll`, so players do not need to run the build script.

## Play

1. At a sculptor's table, add **Make empty urn**. It uses 50 units of the chosen material, 5 wood, and 5 dye. The urn body takes the material's color.
2. At an electric crematorium, add **Cremate colonist into urn**. The recipe uses an empty urn and a colonist corpse. The colonist's strongest skill selects one of 12 memorial flowers.
3. The cremation recipe produces an installable memorial urn directly. Select it to read its memorial record or edit its note; uninstall it to move or store it.
4. Build a columbarium from the **Misc** menu. Use **Store urn** to order a colonist to carry an uninstalled memorial urn into it. Use **Memorial records** to view stored records and **Remove urn** to take one out.

| Capacity | Footprint | Variant |
| --- | --- | --- |
| 8 urns | 2 × 1 | Compact |
| 8 urns | 4 × 2 | Standard |
| 32 urns | 4 × 2 | Compact |
| 32 urns | 8 × 4 | Large |

Niches fill in number order with odd numbers first, then even numbers. For an 8-niche building, the order is **1 → 3 → 5 → 7 → 2 → 4 → 6 → 8**. Each occupied niche shows its urn's memorial flower.

An installed memorial urn adds half the Tomb room score of a vanilla sarcophagus (25 points). Columbarium buildings contribute their existing Tomb room scores.

## Build and develop

The C# project targets RimWorld 1.6 and .NET Framework 4.7.2. Run this command from the repository root, replacing the path with your RimWorld installation:

```powershell
.\scripts\build.ps1 -RimWorldPath 'D:\SteamLibrary\steamapps\common\RimWorld'
```

The build creates a ready-to-install package in `dist/Columbarium/`. You can also set the `RimWorldPath` environment variable or omit the argument if the game is in a location checked by the script. See [Source/README.md](Source/README.md) for the C# code map and [scripts/README.md](scripts/README.md) for the artwork, flower-fitting, and preview tools.

Columbarium adds its score to the Tomb room role with a Harmony postfix. It uses the Tomb worker registered after Def loading, so it does not replace another mod's `Tomb.workerClass`.

For a quick developer-mode check, open **Debug actions → Spawn thing** and spawn `ColumbariumUrnDisplay`. It copies a memorial record from the first corpse on the map, or creates a test record if there is no corpse.

## Translations

Player-facing text is in `Languages/English` and `Languages/Korean`. To add a language, copy the English folder and translate the values while keeping keys and Def names intact. `Keyed/Columbarium.xml` contains strings used by code; `DefInjected` contains names and descriptions for things, recipes, and jobs.
