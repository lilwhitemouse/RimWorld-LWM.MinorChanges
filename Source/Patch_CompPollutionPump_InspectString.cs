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
    [HarmonyPatch(typeof(Dialog_LoadTransporters), "TryAccept")]
    static class Patch_Dialog_LoadTransporters_TryAccept
    {
        static void Postfix(bool __result, List<CompTransporter> ___transporters)
        {
            if (__result == false) return;
            var tmp = new List<TransferableOneWay>();
            foreach (var transporter in ___transporters)
            {
                tmp.Clear();
                for(int i=transporter.leftToLoad.Count-1; i>=0; i--)
                {
                    var t = transporter.leftToLoad[i];
                    if (t.CountToTransfer > 1 && t.AnyThing is Pawn)
                    {
                        Log.Warning("Removing tranferables[" + i + "] with " + t.CountToTransfer + " items");
                        transporter.leftToLoad.RemoveAt(i);
                        foreach (var critter in t.things)
                        {
                            var justaTOW = new TransferableOneWay();
                            justaTOW.things.Add(critter);
                            justaTOW.AdjustTo(1);
                            Log.Message("Adding new item with " + justaTOW.AnyThing + " and a count of " + justaTOW.CountToTransfer);
                            tmp.Add(justaTOW);
                        }
                    }
                }
                if (tmp.Count > 0)
                {
                    Log.Warning("Adding " + tmp.Count + " to the " + transporter.leftToLoad.Count + " still there.");
                    transporter.leftToLoad.AddRange(tmp);
                }
            } // end 1 transporter
        }
    }
    //    [HarmonyPatch(typeof(RimWorld.Dialog_LoadTransporters), "AssignTransferablesToRandomTransporters")]
    static class Patch_AssignTransferablesToRandomTransporters
    {
        static void Prefix(List<TransferableOneWay> ___transferables)
        {
            Log.Warning("Transferables has " + ___transferables.Count);
            var tmp = new List<TransferableOneWay>();
            for (int i=(___transferables.Count-1); i>=0;  i--)
            {
                var t = ___transferables[i];
                if (t.CountToTransfer > 1 && t.AnyThing is Pawn)
                {
                    Log.Warning("Removing tranferables[" + i + "] with " + t.CountToTransfer + " items");
                    ___transferables.RemoveAt(i);
                    foreach (var critter in t.things)
                    {
                        var justaTOW = new TransferableOneWay();
                        justaTOW.things.Add(critter);
                        justaTOW.AdjustTo(1);
                        Log.Message("Adding new item with " + justaTOW.AnyThing + " and a count of " + justaTOW.CountToTransfer);
                        tmp.Add(justaTOW);
                    }
                }
            }
            if (tmp.Count > 0)
            {
                Log.Warning("Adding " + tmp.Count + " to the " + ___transferables.Count + " still there.");
                ___transferables.AddRange(tmp);
                Log.Message("transferables now has " + ___transferables.Count);
            }
        }
    }
    //    [HarmonyPatch(typeof(TransferableUtility), "TransferableMatchingDesperate")]
    static class TMP
    {
        static void Postfix(TransferableOneWay __result, Thing thing, List<TransferableOneWay> transferables)
        {
            var things = transferables.SelectMany(t => (t.HasAnyThing ? t.things : empty));
            var contains = things.Contains(thing);
            Log.Message("TransferableMatchingDesperate searching "+
                String.Join(", ", things)+ " for " + thing + ": " + contains+"; result: "+
              (__result?.AnyThing != null?String.Join(", ",__result.things):"NULL:("));
        }
        static List<Thing> empty = new List<Thing>();
    }
}