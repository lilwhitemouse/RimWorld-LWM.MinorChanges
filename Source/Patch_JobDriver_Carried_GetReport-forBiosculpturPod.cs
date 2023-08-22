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

namespace LWM.MinorChanges
{
    /************* Verse.AI's JobDriver_Carried's GetReport(Pawn, Thing) *************/
    [HarmonyPatch(typeof(Verse.AI.JobDriver_Carried), "GetReport", new Type[] {typeof(Pawn), typeof(Thing)})]
    public static class Patch_JobDriver_Carried_GetReport_forBiosculpturPod {
        public static bool Prepare() { // TODO: Go ahead and make this setting dependent
            return true //LoadedModManager.GetMod<MinorChangesMod>().GetSettings<Settings>()....??
              && (ModsConfig.IdeologyActive);
        }
        public static void Postfix(ref string __result, Pawn pawn, Thing spawnedParentOrMe) {
            if (spawnedParentOrMe.TryGetComp<CompBiosculpterPod>() is CompBiosculpterPod compPod
                && compPod.State == BiosculpterPodState.Occupied)
            {
                /*  You know what?  Let's not try doing this ourselves:
                 "LWMMC_TimeLeftLabel".Translate(label, numTicks.ToStringTicksToPeriod(true, // allow seconds?
                          true, // f-short form?
                          true,  // canUseDecimals?
                          true,  // allowYears?
                          true  // f-canUseDecimalsShortForm?
                          ));
                          */
                var s = compPod.CompInspectStringExtra();
                if (!s.NullOrEmpty())
                {
                    __result += ": " + s; // this fits "1.2 days left" in *my* screen, anyway
                }
            }
        }
    }
}
