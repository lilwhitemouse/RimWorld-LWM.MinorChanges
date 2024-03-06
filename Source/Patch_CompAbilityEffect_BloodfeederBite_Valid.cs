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

/***********************************************
 * Allow quest members or non-hostile visitors *
 *   to feed bloodfeeders IFF                  *
 *  -they have precept: bloodfeeders revered   *
 *  -they have backstory of willing thrall:    *
 *    More.Backstories by Ravinglegend ->      *
 *      VB_Thrall                              *
 ***********************************************/

namespace LWM.MinorChanges
{
    /************* RimWorld's CompAbilityEffect_BloodfeederBite's Valid() *************/
    [HarmonyPatch(typeof(RimWorld.CompAbilityEffect_BloodfeederBite), "Valid")]
    public static class Patch_BloodfeederBite_Valid {
        public static bool Prepare() {
            return Settings.IsOptionSet("bloodfeedOnPeopleWhoWant")
                && ModsConfig.BiotechActive // for bloodfeeders
                && (ModsConfig.IdeologyActive  // for Ideologies who like bloodfeeders
                    || ModsConfig.IsActive("More.Backstories") // for backstory VB_Thrall
                   );
        }
        //  ...I COULD do this as a Transpile....but there's almost no reason to.  I guess I'm so 
        //     used to worrying about performance, but this only gets run when the player clicks.

        // In if (pawn.IsQuestLodger() || pawn.Faction != this.parent.pawn.Faction)
        // Add a valid result if the pawn's faction reveres bloodfeeders
        //   (or if they're a (willing) Trall from More Backstories?)
        public static bool Prefix(LocalTargetInfo target, ref bool __result, CompAbilityEffect_BloodfeederBite __instance) {
            Pawn pawn = target.Pawn;
            // Note: this mirrors the vanilla tests. Vanilla would return 'false' here (i.e., not Valid,
            //   we will add our own tests and maybe return true!
            if (pawn != null && pawn.Faction != null && !pawn.IsSlaveOfColony && !pawn.IsPrisonerOfColony &&
                !pawn.Faction.HostileTo(__instance.parent.pawn.Faction) &&
                (pawn.IsQuestLodger() || pawn.Faction != __instance.parent.pawn.Faction)) {
                // Check to see if it's okay to feed on this QuestLodger or visiting pawn:
                if (
                    ModsConfig.BiotechActive &&
                    // Precept:
                    ((ModsConfig.IdeologyActive && 
                      pawn.ideo?.Ideo?.HasPrecept(DefDatabase<PreceptDef>.GetNamed("Bloodfeeders_Revered")) == true )
                     || // backstory:
                     (ModsConfig.IsActive("More.Backstories") && pawn.story?.Adulthood?.defName == "VB_Thrall")))
                {
                    __result = true;
                    return false;
                }
            }
            return true;
        }
    }
}
