using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Columbarium
{
    [StaticConstructorOnStartup]
    internal static class FlowerCatalog
    {
        // The order is saved in each urn. Keep existing indices stable.
        private static readonly string[] Skills =
        {
            "Melee", "Shooting", "Construction", "Mining", "Cooking", "Plants",
            "Animals", "Crafting", "Artistic", "Medicine", "Social", "Intellectual"
        };

        private static readonly string[] Flowers =
        {
            "Melee", "Shooting", "Construction", "Mining", "Cooking", "Plants",
            "Animals", "Crafting", "Artistic", "Medicine", "Social", "Intellectual"
        };

        private static readonly Dictionary<string, int> SkillIndices = BuildSkillIndices();
        private static readonly Material[] CachedMaterials = new Material[Flowers.Length];
        public const int DefaultIndex = 9; // White lily for legacy records without a skill snapshot.

        private static Dictionary<string, int> BuildSkillIndices()
        {
            var indices = new Dictionary<string, int>(Skills.Length);
            for (int i = 0; i < Skills.Length; i++) indices.Add(Skills[i], i);
            return indices;
        }

        public static int ForPawn(Pawn pawn)
        {
            if (pawn?.skills?.skills == null) return DefaultIndex;
            int bestIndex = DefaultIndex;
            int bestLevel = -1;
            foreach (SkillRecord skill in pawn.skills.skills)
            {
                if (!SkillIndices.TryGetValue(skill.def.defName, out int index)) continue;
                int level = skill.Level;
                if (level > bestLevel || level == bestLevel && index < bestIndex)
                {
                    bestLevel = level;
                    bestIndex = index;
                }
            }
            return bestIndex;
        }

        public static Material MaterialAt(int index)
        {
            if (index < 0 || index >= Flowers.Length) index = DefaultIndex;
            Material material = CachedMaterials[index];
            if (material == null)
            {
                material = GraphicDatabase.Get<Graphic_Single>("Things/Flowers/" + Flowers[index],
                    ShaderDatabase.Cutout, Vector2.one, Color.white).MatSingle;
                CachedMaterials[index] = material;
            }
            return material;
        }

        public static string SkillName(int index)
        {
            if (index < 0 || index >= Skills.Length) index = DefaultIndex;
            return DefDatabase<SkillDef>.GetNamedSilentFail(Skills[index])?.LabelCap ?? Skills[index];
        }

        public static string FlowerName(int index)
        {
            if (index < 0 || index >= Flowers.Length) index = DefaultIndex;
            return ("Columbarium_Flower_" + Skills[index]).Translate();
        }

        public static void DrawAt(Vector3 urnCenter, float urnSize, int index)
        {
            float size = urnSize * .56f;
            float offset = (urnSize - size) * .5f - urnSize * .03f;
            Vector3 flowerCenter = urnCenter + new Vector3(offset, .02f, -offset);
            Graphics.DrawMesh(MeshPool.plane10,
                Matrix4x4.TRS(flowerCenter, Quaternion.identity, new Vector3(size, 1f, size)),
                MaterialAt(index), 0);
        }

        public static void DrawOnDoor(Vector3 doorCenter, Vector2 doorSize, int index, float size, float margin)
        {
            // Keep the flower's right and bottom edges the same distance from the door frame.
            float offsetX = (doorSize.x - size) * .5f - margin;
            float offsetZ = (doorSize.y - size) * .5f - margin;
            Vector3 flowerCenter = doorCenter + new Vector3(offsetX, .02f, -offsetZ);
            Graphics.DrawMesh(MeshPool.plane10,
                Matrix4x4.TRS(flowerCenter, Quaternion.identity, new Vector3(size, 1f, size)),
                MaterialAt(index), 0);
        }
    }
}
