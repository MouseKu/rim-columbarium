using RimWorld;
using Verse;

namespace Columbarium
{
    public class StatPart_MemorialUrnMarketValue : StatPart
    {
        private const float UrnValueFactor = 0.5f;

        public override void TransformValue(StatRequest req, ref float value)
        {
            Thing thing = Unwrap(req.Thing);
            IMemorialRecord record = thing as IMemorialRecord;
            if (!TryGetBaseUrnValue(thing, record, out float baseUrnValue)) return;
            value = baseUrnValue * UrnValueFactor;
        }

        public override string ExplanationPart(StatRequest req)
        {
            Thing thing = Unwrap(req.Thing);
            IMemorialRecord record = thing as IMemorialRecord;
            if (!TryGetBaseUrnValue(thing, record, out float baseUrnValue)) return null;
            return "Empty urn market value at creation (50%): "
                + (baseUrnValue * UrnValueFactor).ToStringMoney("F2");
        }

        private static bool TryGetBaseUrnValue(Thing thing, IMemorialRecord record, out float baseUrnValue)
        {
            baseUrnValue = -1f;
            if (record == null || !(thing is Building_MemorialUrnDisplay))
                return false;

            if (record.BaseUrnMarketValueAtCreation >= 0f)
            {
                baseUrnValue = record.BaseUrnMarketValueAtCreation;
                return true;
            }

            return false;
        }

        private static Thing Unwrap(Thing thing)
        {
            return thing is MinifiedThing minified ? minified.InnerThing : thing;
        }
    }
}
