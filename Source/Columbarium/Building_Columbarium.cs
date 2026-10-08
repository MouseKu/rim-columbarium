using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Columbarium
{
    [StaticConstructorOnStartup]
    public partial class Building_Columbarium : Building, IThingHolder
    {
        private ThingOwner<Thing> urns;
        private int Capacity => IsThirtyTwoNicheArt ? 32 : 8;
        // Measured outer niche frames in Art/sample3.png (1774 x 887).
        // The preview reads these same rectangles to avoid a second layout.
        private static readonly Rect[] TallNicheBounds = {
            new Rect(102f, 324f, 347f, 208f),
            new Rect(503f, 324f, 357f, 208f),
            new Rect(915f, 324f, 359f, 208f),
            new Rect(1329f, 324f, 349f, 208f),
            new Rect(102f, 532f, 347f, 216f),
            new Rect(503f, 532f, 357f, 216f),
            new Rect(915f, 532f, 359f, 216f),
            new Rect(1329f, 532f, 349f, 216f)
        };
        private bool IsEightNicheArt => def.defName == "ColumbariumModularTall" ||
            def.defName == "ColumbariumModularCompactTall";
        private bool CompactEightNicheArt => def.defName == "ColumbariumModularCompactTall";
        private bool IsThirtyTwoNicheArt => def.defName == "ColumbariumThirtyTwoCompact" ||
            def.defName == "ColumbariumThirtyTwoLarge";
        private const float CompactNorthOffset = 0.07383191f;
        private const float LargeNorthOffset = 0.10452815f;
        private static readonly Dictionary<ThingDef, NicheGeometry> GeometryByDef =
            new Dictionary<ThingDef, NicheGeometry>();

        private sealed class NicheGeometry
        {
            public readonly Vector2 FrontSize;
            public readonly Vector3[] Offsets;
            public readonly Vector2[] Sizes;

            public NicheGeometry(Vector2 frontSize, int count)
            {
                FrontSize = frontSize;
                Offsets = new Vector3[count];
                Sizes = new Vector2[count];
            }
        }

        public Building_Columbarium()
        {
            urns = new ThingOwner<Thing>(this, oneStackOnly: false);
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            bool tallArt = IsEightNicheArt;
            // The front artwork extends north of the building footprint.
            if (tallArt && Rotation == Rot4.South)
                drawLoc.z += CompactEightNicheArt ? CompactNorthOffset : LargeNorthOffset;
            if (IsThirtyTwoNicheArt && Rotation == Rot4.South)
                drawLoc.z += def.defName == "ColumbariumThirtyTwoCompact" ?
                    ThirtyTwoLayout.CompactNorthOffset : ThirtyTwoLayout.LargeNorthOffset;
            base.DrawAt(drawLoc, flip);
            // The niches are on the front face, visible only from the south.
            if (Rotation != Rot4.South) return;
            if (IsThirtyTwoNicheArt)
            {
                DrawThirtyTwoNiches(drawLoc);
                return;
            }
            if (tallArt)
            {
                NicheGeometry geometry = GetNicheGeometry();
                Material openMaterial = GraphicDatabase.Get<Graphic_Single>(
                    "Things/Columbarium_ArtTallNicheOpen", ShaderDatabase.CutoutComplex,
                    Vector2.one, DrawColor).MatSingle;
                Material closedMaterial = GraphicDatabase.Get<Graphic_Single>(
                    CompactEightNicheArt ? "Things/Columbarium_Compact_ArtTallNicheClosed" :
                        "Things/Columbarium_ArtTallNicheClosed", ShaderDatabase.CutoutComplex,
                    Vector2.one, DrawColor).MatSingle;
                for (int i = 0; i < Capacity; i++)
                {
                    int urnIndex = UrnIndexForNiche(i);
                    Vector2 doorSize = geometry.Sizes[i];
                    Vector3 center = drawLoc + geometry.Offsets[i];
                    DrawLayer(urnIndex < urns.Count ? closedMaterial : openMaterial, doorSize, center);
                    if (urnIndex < urns.Count && !CompactEightNicheArt)
                    {
                        DrawTallNicheFlower(urnIndex, center, doorSize, compact: false);
                    }
                }
                if (CompactEightNicheArt)
                    for (int i = 0; i < Capacity; i++)
                    {
                        int urnIndex = UrnIndexForNiche(i);
                        if (urnIndex < urns.Count)
                            DrawTallNicheFlower(urnIndex, drawLoc + geometry.Offsets[i], geometry.Sizes[i], compact: true);
                    }
                return;
            }
        }

        private void DrawTallNicheFlower(int nicheIndex, Vector3 center, Vector2 doorSize, bool compact)
        {
            int flowerIndex = MemorialRecordOf(urns[nicheIndex]).FlowerIndex;
            float flowerSize = doorSize.y * (compact
                ? CompactNicheFlowerLayout.SizeRatio(flowerIndex)
                : NicheFlowerLayout.SizeRatio(flowerIndex));
            float margin = doorSize.y * (compact
                ? CompactNicheFlowerLayout.MarginRatio
                : NicheFlowerLayout.MarginRatio);
            Vector3 flowerCenter = center;
            flowerCenter.x += flowerSize * (compact
                ? CompactNicheFlowerLayout.RightPadding(flowerIndex)
                : NicheFlowerLayout.RightPadding(flowerIndex));
            if (compact)
                flowerCenter.x += doorSize.x * CompactNicheFlowerLayout.RightSpillRatio;
            else
                flowerCenter.x += doorSize.x * NicheFlowerLayout.RightSpillRatio;
            FlowerCatalog.DrawOnDoor(flowerCenter, doorSize, flowerIndex, flowerSize, margin);
        }

        private void DrawThirtyTwoNiches(Vector3 drawLoc)
        {
            bool compact = def.defName == "ColumbariumThirtyTwoCompact";
            NicheGeometry geometry = GetNicheGeometry();
            Material open = GraphicDatabase.Get<Graphic_Single>("Things/Columbarium_ArtTallNicheOpen",
                ShaderDatabase.CutoutComplex, Vector2.one, DrawColor).MatSingle;
            Material closed = GraphicDatabase.Get<Graphic_Single>(
                compact ? "Things/Columbarium_Compact_ArtTallNicheClosed" :
                    "Things/Columbarium_ArtTallNicheClosed",
                ShaderDatabase.CutoutComplex, Vector2.one, DrawColor).MatSingle;
            for (int i = 0; i < Capacity; i++)
            {
                int urnIndex = UrnIndexForNiche(i);
                Vector2 doorSize = geometry.Sizes[i];
                Vector3 center = drawLoc + geometry.Offsets[i];
                DrawLayer(urnIndex < urns.Count ? closed : open, doorSize, center);
                if (urnIndex >= urns.Count || compact) continue;
                int index = MemorialRecordOf(urns[urnIndex]).FlowerIndex;
                float flowerSize = doorSize.y * ThirtyTwoLayout.FlowerRatio(index);
                center.x += flowerSize * ThirtyTwoLayout.RightPadding(index);
                FlowerCatalog.DrawOnDoor(center, doorSize, index, flowerSize,
                    doorSize.y * ThirtyTwoLayout.FlowerMargin);
            }
            if (compact)
            {
                for (int i = 0; i < Capacity; i++)
                {
                    int urnIndex = UrnIndexForNiche(i);
                    if (urnIndex >= urns.Count) continue;
                    int index = MemorialRecordOf(urns[urnIndex]).FlowerIndex;
                    float flowerSize = geometry.Sizes[i].y * CompactThirtyTwoFlowerLayout.SizeRatio(index);
                    Vector3 center = drawLoc + geometry.Offsets[i];
                    center.x += flowerSize * CompactThirtyTwoFlowerLayout.RightPadding(index);
                    center.x += geometry.Sizes[i].x * CompactThirtyTwoFlowerLayout.RightSpillRatio;
                    FlowerCatalog.DrawOnDoor(center, geometry.Sizes[i], index, flowerSize,
                        geometry.Sizes[i].y * CompactThirtyTwoFlowerLayout.MarginRatio);
                }
            }
        }

        private NicheGeometry GetNicheGeometry()
        {
            Vector2 frontSize = def.graphicData.drawSize;
            if (GeometryByDef.TryGetValue(def, out NicheGeometry geometry) && geometry.FrontSize == frontSize)
                return geometry;

            bool thirtyTwo = IsThirtyTwoNicheArt;
            geometry = new NicheGeometry(frontSize, Capacity);
            for (int i = 0; i < Capacity; i++)
            {
                if (thirtyTwo)
                {
                    Rect bounds = ThirtyTwoLayout.Bounds[i];
                    geometry.Sizes[i] = new Vector2(
                        bounds.width / ThirtyTwoLayout.SourceWidth * frontSize.x,
                        bounds.height / ThirtyTwoLayout.SourceHeight * frontSize.y);
                    geometry.Offsets[i] = new Vector3(
                        (bounds.center.x / ThirtyTwoLayout.SourceWidth - .5f) * frontSize.x,
                        .035f,
                        (.5f - bounds.center.y / ThirtyTwoLayout.SourceHeight) * frontSize.y);
                }
                else
                {
                    // Match the visible edge of the inserts in the 1024 x 512 front artwork.
                    Rect bounds = TallNicheBounds[i];
                    float columnPixel = bounds.center.x * 1024f / 1774f;
                    float rowPixel = bounds.center.y * 512f / 887f;
                    float widthPixels = bounds.width * 1024f / 1774f;
                    float heightPixels = bounds.height * 512f / 887f;
                    geometry.Sizes[i] = new Vector2(widthPixels / 1024f * frontSize.x,
                        heightPixels / 512f * frontSize.y);
                    geometry.Offsets[i] = new Vector3(
                        (columnPixel / 1024f - .5f) * frontSize.x,
                        .035f,
                        (.5f - rowPixel / 512f) * frontSize.y);
                }
            }

            GeometryByDef[def] = geometry;
            return geometry;
        }

        private int UrnIndexForNiche(int nicheIndex)
        {
            // Translate a visible niche position to the corresponding index in the
            // insertion-ordered urn list: odd-numbered niches fill before even ones.
            return nicheIndex % 2 == 0 ? nicheIndex / 2 : Capacity / 2 + nicheIndex / 2;
        }

        private static void DrawLayer(Material material, Vector2 size, Vector3 pos)
        {
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(pos, Quaternion.identity, new Vector3(size.x, 1f, size.y)), material, 0);
        }

    }
}
