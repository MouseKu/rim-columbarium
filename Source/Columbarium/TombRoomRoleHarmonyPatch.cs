using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Columbarium
{
    [StaticConstructorOnStartup]
    internal static class TombRoomRoleHarmonyPatch
    {
        // Vanilla RoomRoleWorker_Tomb gives each sarcophagus 50 points.
        private const float HalfSarcophagusScore = 25f;
        private static readonly RoomRoleWorker TombWorker;

        static TombRoomRoleHarmonyPatch()
        {
            RoomRoleDef tomb = DefDatabase<RoomRoleDef>.GetNamedSilentFail("Tomb");
            if (tomb == null) return;

            TombWorker = tomb.Worker;
            if (TombWorker == null)
            {
                Log.Warning("[Columbarium] The Tomb room role has no worker to patch.");
                return;
            }
            Type workerType = TombWorker.GetType();
            MethodInfo getScore = workerType.GetMethod("GetScore", new[] { typeof(Room) });
            MethodInfo getScoreDelta = workerType.GetMethod("GetScoreDeltaIfBuildingPlaced",
                new[] { typeof(Room), typeof(ThingDef) });
            if (getScore == null || getScoreDelta == null)
            {
                Log.Warning("[Columbarium] Could not patch the Tomb room role worker.");
                return;
            }

            try
            {
                var harmony = new Harmony("mint.columbarium.tomb");
                harmony.Patch(getScore, postfix: new HarmonyMethod(
                    typeof(TombRoomRoleHarmonyPatch).GetMethod(nameof(GetScorePostfix),
                        BindingFlags.Static | BindingFlags.NonPublic)));
                harmony.Patch(getScoreDelta, postfix: new HarmonyMethod(
                    typeof(TombRoomRoleHarmonyPatch).GetMethod(nameof(GetScoreDeltaPostfix),
                        BindingFlags.Static | BindingFlags.NonPublic)));
            }
            catch (Exception exception)
            {
                Log.Error("[Columbarium] Could not patch the Tomb room role: " + exception);
            }
        }

        private static float ScoreForColumbarium(ThingDef def)
        {
            switch (def.defName)
            {
                case "ColumbariumUrnDisplay":
                    return HalfSarcophagusScore;
                case "ColumbariumModularCompactTall":
                case "ColumbariumModularTall":
                    return 8f * HalfSarcophagusScore;
                case "ColumbariumThirtyTwoCompact":
                case "ColumbariumThirtyTwoLarge":
                    return 32f * HalfSarcophagusScore;
                default:
                    return 0f;
            }
        }

        private static void GetScorePostfix(RoomRoleWorker __instance, Room room, ref float __result)
        {
            if (!ReferenceEquals(__instance, TombWorker) || room == null) return;
            foreach (Thing thing in room.ContainedAndAdjacentThings)
                __result += ScoreForColumbarium(thing.def);
        }

        private static void GetScoreDeltaPostfix(RoomRoleWorker __instance, ThingDef buildingDef,
            ref float __result)
        {
            if (ReferenceEquals(__instance, TombWorker) && buildingDef != null)
                __result += ScoreForColumbarium(buildingDef);
        }
    }
}
