# Columbarium for RimWorld 1.6

Columbarium adds memorial urns and buildings that can store them. Cremate a colonist into an urn, install the urn on its own, or keep it in a columbarium.

Standard columbaria hold one urn per tile, while compact columbaria hold four. You can remember your colonists even when space is tight.

![Half-filled columbaria](.img/half.png)

The flower on an urn reflects the deceased colonist's strongest skill. For example, a colonist whose strongest skill was Melee receives a gladiolus. Each urn records the colonist's name and dates, and you can engrave an epitaph.

## Install

Install [Harmony](https://github.com/pardeike/HarmonyRimWorld) first. Copy this folder into RimWorld's `Mods` directory, then enable **Harmony** and **Columbarium** in that order. The mod includes a prebuilt `Assemblies/Columbarium.dll`, so players do not need to run the build script.

## How to use

![Making and filling an urn](.img/step-to-step.jpg)

1. At a sculptor's table, add **Make empty urn**. It uses 50 units of the chosen material, 5 wood, and 5 dye. The urn body takes the material's color.
2. At an electric crematorium, add **Cremate colonist into urn**. It uses an empty urn and a colonist's corpse. You can also cremate at a campfire, but that recipe needs an additional 150 wood and takes about 12 hours at normal labor speed.
3. The recipe produces an installable memorial urn. Select it to read its memorial record or engrave an epitaph. Once engraved, the epitaph cannot be changed.

![Storing an urn](.img/step-to-step2.jpg)

4. Build a columbarium from the **Misc** menu. Uninstall the memorial urn, then use **Store urn** to have a colonist carry it into the columbarium. Use **Memorial records** to view stored records and **Remove urn** to take one out.

![Engraving an epitaph](.img/step-to-step3.jpg)

## Columbarium variants

![Columbarium variants](.img/variation.jpg)

Columbaria hold either 8 or 32 urns. Standard and large variants store one urn per occupied tile; compact variants store four urns per tile.

| Capacity | Footprint | Variant | Beauty |
| --- | --- | --- | --- |
| 8 urns | 2 × 1 | Compact | 0 |
| 8 urns | 4 × 2 | Standard | 4 |
| 32 urns | 4 × 2 | Compact | 0 |
| 32 urns | 8 × 4 | Large | 4 |

The compact variants' details can be hard to see without a zoom mod. Apart from their size and a small Beauty bonus for the larger variants, their functions are nearly identical.

Niches fill in number order with odd numbers first, then even numbers. For an 8-niche building, the order is **1 → 3 → 5 → 7 → 2 → 4 → 6 → 8**. Each occupied niche shows its urn's memorial flower.

An installed memorial urn adds half the Tomb room score of a vanilla sarcophagus (25 points). Columbarium buildings contribute their existing Tomb room scores.

## Notes

AI tools were used during development. The mod has passed short-term testing. Long-term save testing and compatibility testing are still in progress; it has also been tested alongside more than 200 mods. Please report any bugs you find.

In terms of balance, a columbarium costs twice as much as a vanilla sarcophagus, but it can hold far more people, making it more efficient. Then again, should remembrance really be measured by efficiency?

## Build and develop

The C# project targets RimWorld 1.6 and .NET Framework 4.7.2. Run this command from the repository root, replacing the path with your RimWorld installation:

```powershell
.\scripts\build.ps1 -RimWorldPath 'D:\SteamLibrary\steamapps\common\RimWorld'
```

The build creates a ready-to-install package in `dist/Columbarium/`. You can also set the `RimWorldPath` environment variable or omit the argument if the game is in a location checked by the script. See [Source/README.md](Source/README.md) for the C# code map and [scripts/README.md](Scripts/README.md) for the artwork, flower-fitting, and preview tools.

Columbarium adds its score to the Tomb room role with a Harmony postfix. It uses the Tomb worker registered after Def loading, so it does not replace another mod's `Tomb.workerClass`.

For a quick developer-mode check, open **Debug actions → Spawn thing** and spawn `ColumbariumUrnDisplay`. It copies a memorial record from the first corpse on the map, or creates a test record if there is no corpse.

## Translations

Player-facing text is in `Languages/English` and `Languages/Korean`. To add a language, copy the English folder and translate the values while keeping keys and Def names intact. `Keyed/Columbarium.xml` contains strings used by code; `DefInjected` contains names and descriptions for things, recipes, and jobs.
