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
    /// <summary>
    /// 
    /// 
    /// </summary>
    [HarmonyPatch(typeof(RimWorld.CompPollutionPump), "CompInspectStringExtra")]
    static class Patch_CompPollutionPump_InspectString
    {
        const int CountCutoffForMany = 60; // a season's work for a pollution pump
        static bool Prepare()
        {
            return Settings.IsOptionSet("pollutionPumpsShowPollutionLeft");
        }
        static string Postfix(string __result, CompPollutionPump __instance, int ___ticksUntilPump)
        {
            int cutoffAmountPollution = (3600000 // 3,600,000 = 1 year
                                        / __instance.Props.intervalTicks)
                                        + 1;
            if (!__instance.parent.Spawned) return __result;
            int numCellsPollution = 0;
            int num = GenRadial.NumCellsInRadius(__instance.Props.radius);
            Map map = __instance.parent.Map;
            for (int i = 0; i < num; i++)
            {
                IntVec3 intVec = __instance.parent.Position + GenRadial.RadialPattern[i];
                if (intVec.InBounds(map) && intVec.CanUnpollute(map))
                {
                    numCellsPollution++;
                    if (numCellsPollution > cutoffAmountPollution) break;
                    //if (numCellsPollution > CountCutoffForMany) break;
                }
            }
            if (numCellsPollution > 0)
            {
                //if (numCellsPollution > CountCutoffForMany) return __result + "\n" +
                if (numCellsPollution > cutoffAmountPollution) return __result + "\n"+
                        "LWMMC_PollutionLeftOverYear".Translate(numCellsPollution);
                int ticksUntilAllCleanedUp = ___ticksUntilPump + (numCellsPollution - 1) * __instance.Props.intervalTicks;
                return __result + "\n" + "LWMMC_PollutionLeftTime".Translate(numCellsPollution, 
                    ticksUntilAllCleanedUp.ToStringTicksToPeriod(
                        true, // allow secs
                        false, // f-short form
                        true, // can use decimals
                        true, // allowYears (altho prolly not useful)
                        true // f-can use Decimals in short form
                        )
                    );
            }
            return __result;
        }
    }
}