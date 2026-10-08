using UnityEngine;
using Verse;

namespace Columbarium
{
    public class RecipeWorker_FillUrn : RecipeWorker
    {
        private Building_MemorialUrnDisplay pendingDisplay;
        private Urn pendingEmptyUrn;
        private Corpse pendingCorpse;

        public void RegisterProduct(Building_MemorialUrnDisplay display)
        {
            pendingDisplay = display;
            ApplyIngredientsToProduct();
        }

        public override void ConsumeIngredient(Thing ingredient, RecipeDef recipe, Map map)
        {
            if (ingredient is Urn emptyUrn) pendingEmptyUrn = emptyUrn;
            else if (ingredient is Corpse corpse && corpse.InnerPawn != null) pendingCorpse = corpse;
            ApplyIngredientsToProduct();
            base.ConsumeIngredient(ingredient, recipe, map);
        }

        private void ApplyIngredientsToProduct()
        {
            // RimWorld may notify the product before or after it consumes the ingredients.
            // Apply each value as soon as both the product and its source are available.
            if (pendingDisplay == null) return;
            if (pendingEmptyUrn != null)
            {
                Color urnColor = pendingEmptyUrn.Stuff != null && pendingEmptyUrn.Stuff.stuffProps != null
                    ? pendingEmptyUrn.Stuff.stuffProps.color
                    : pendingEmptyUrn.DrawColor;
                pendingDisplay.SetUrnColor(urnColor);
                pendingDisplay.baseUrnMarketValueAtCreation = pendingEmptyUrn.MarketValue;
                pendingEmptyUrn = null;
            }
            if (pendingCorpse != null)
            {
                pendingDisplay.Record(pendingCorpse);
                pendingCorpse = null;
            }
        }

        public override void Notify_IterationCompleted(Pawn billDoer, System.Collections.Generic.List<Thing> ingredients)
        {
            pendingDisplay = null;
            pendingEmptyUrn = null;
            pendingCorpse = null;
            base.Notify_IterationCompleted(billDoer, ingredients);
        }
    }
}
