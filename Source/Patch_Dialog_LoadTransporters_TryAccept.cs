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
    /// In RimWorld v1.4, if you try to load a bunch of farm animals into the transport pod,
    /// you can end up with a situation where a pawn will add one animal but the pod's list
    /// will still include that animal to be loaded. This causes the pod loading to hang as
    /// the animal cannot be loaded....because it's already in the pod.
    /// 
    /// Alerted Devs:  https://discord.com/channels/684960023020961812/686141574500843521/1150863022340968548
    /// 
    /// At some point this will no longer be necessary and can be removed, but for now, this
    /// is a bug fix:
    /// </summary>
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
                for (int i = transporter.leftToLoad.Count - 1; i >= 0; i--)
                {
                    var t = transporter.leftToLoad[i];
                    if (t.CountToTransfer > 1 && t.AnyThing is Pawn)
                    {
                        Debug.Warn("Removing tranferables[" + i + "] with " + t.CountToTransfer + " items");
                        transporter.leftToLoad.RemoveAt(i);
                        foreach (var critter in t.things)
                        {
                            var justaTOW = new TransferableOneWay();
                            justaTOW.things.Add(critter);
                            justaTOW.AdjustTo(1);
                            Debug.Mess("Adding new item with " + justaTOW.AnyThing + " and a count of " + justaTOW.CountToTransfer);
                            tmp.Add(justaTOW);
                        }
                    }
                }
                if (tmp.Count > 0)
                {
                    Debug.Warn("Adding " + tmp.Count + " to the " + transporter.leftToLoad.Count + " still there.");
                    transporter.leftToLoad.AddRange(tmp);
                }
            } // end 1 transporter
        }
    }
    // Other attempts:
/*    //    [HarmonyPatch(typeof(RimWorld.Dialog_LoadTransporters), "AssignTransferablesToRandomTransporters")]
    static class Patch_AssignTransferablesToRandomTransporters
    {
        static void Prefix(List<TransferableOneWay> ___transferables)
        {
            Log.Warning("Transferables has " + ___transferables.Count);
            var tmp = new List<TransferableOneWay>();
            for (int i = (___transferables.Count - 1); i >= 0; i--)
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
            Log.Message("TransferableMatchingDesperate searching " +
                String.Join(", ", things) + " for " + thing + ": " + contains + "; result: " +
              (__result?.AnyThing != null ? String.Join(", ", __result.things) : "NULL:("));
        }
        static List<Thing> empty = new List<Thing>();
    }
    */
}
