using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Columbarium
{
    [StaticConstructorOnStartup]
    internal static class MemorialUrnMarketValueRegistration
    {
        // Add to the resolved stat lists after XML patches, without creating another <parts> node.
        static MemorialUrnMarketValueRegistration()
        {
            AddPart("MarketValue");
            AddPart("MarketValueIgnoreHp");
        }

        private static void AddPart(string defName)
        {
            StatDef stat = DefDatabase<StatDef>.GetNamedSilentFail(defName);
            if (stat == null)
            {
                Log.Warning("[Columbarium] Could not find the " + defName + " stat.");
                return;
            }

            if (stat.parts == null) stat.parts = new List<StatPart>();
            foreach (StatPart part in stat.parts)
                if (part is StatPart_MemorialUrnMarketValue) return;

            stat.parts.Add(new StatPart_MemorialUrnMarketValue { parentStat = stat });
        }
    }
}
