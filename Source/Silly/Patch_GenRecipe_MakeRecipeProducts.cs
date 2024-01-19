using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using System.Reflection.Emit; // for OpCodes in Harmony Transpiler
using HarmonyLib;

/*************************************************************

 ************************************************************/

namespace LWM.MinorChanges
{
    // Sometimes geese lay golden eggs (no rhyme or reason, just silly)
    [HarmonyPatch(typeof(Verse.GenRecipe), "MakeRecipeProducts")]
    static class Patch_GenRecipe_AddGoldEggs {
        static bool Prepare() {
            return Settings.IsOptionSet("beSilly");
        }
        static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, RecipeDef recipeDef, List<Thing> ingredients, Pawn worker) {
            if (recipeDef.defName=="ButcherCorpseFlesh" && ingredients.Count==1 &&
                ingredients[0].def.defName=="Corpse_Goose" &&
                (ingredients[0] as Corpse).InnerPawn.gender==Gender.Female &&
                ingredients[0].thingIDNumber%777==0) {// lucky number for some ppl?
                if (worker!=null && worker.Spawned) {
                    Messages.Message(TranslatorFormattedStringExtensions.Translate("LWMMCSillyGoose", worker),
                                     worker, MessageTypeDefOf.PositiveEvent, true);
                }
                ThoughtDef lucky=DefDatabase<ThoughtDef>.GetNamed("LWMMCSillyLucky");
                foreach (Pawn p in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners) {
                    if (p.needs.mood != null) {
                        if (p==worker)
                            worker.needs.mood.thoughts.memories.TryGainMemory(DefDatabase<ThoughtDef>
                                                                              .GetNamed("LWMMCSillyLuckyPersonal"),null);
                        else
                            p.needs.mood.thoughts.memories.TryGainMemory(lucky, null);
                    }
                }

                // put gold first because obviously worker is excited and runs off with egg:
                yield return ThingMaker.MakeThing(ThingDefOf.Gold);
            }
            foreach (Thing t in __result) yield return t;
        }

    }
}
