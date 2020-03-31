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

/********************************************************
 * When a pawn's unloading a pack animal (or prisoner), *
 * they reserve the *pack-hauler*, because you can only *
 * make reservations on spawned things.                 *
 * So you can't reserve the thing in the pack.          *
 * Meh.                                                 *
 * However, in vanilla, that reservation stays in place *
 * until the entire job is done, so no other pawns will *
 * help with unloading.                                 *
 * Moreover, if you force another pawn to help with the *
 * unloading, it will cancel the reservations the first *
 * pawn has - which in this case, will cause the entire *
 * job to fail.  So the first pawn will drop stuff onto *
 * the ground and go do something else.                 *
 * Not ideal.                                           *
 *                                                      *
 * We patch one of RimWorld.JobDriver_UnloadInventory's *
 * toils to release this reservation after removing the *
 * item from the pack. We patch that toil which decides *
 * where to haul the item.                              *
 * Of course, it's an anonymous delegate.               *
 * #DeepMagic #ButOnlyALittleDeep                       *
 ********************************************************/

namespace LWM.MinorChanges
{
    [HarmonyPatch(typeof(RimWorld.JobDriver_UnloadInventory), "<MakeNewToils>b__7_0")]
    static class Patch_JobD_UnloadInv {
        static bool Prepare() {
            return LoadedModManager.GetMod<MinorChangesMod>().GetSettings<Settings>().allowMultiUnloading;
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
                                                       ILGenerator generator) {
            // <MakeNewToils>b__7_0 doesn't have an anonymous object assosciated with it, so
            // that makes things easy.  Arg0 is the JobDriver_UnloadInventory!
            // Our simple solution:  Before the rest of the toil does any work,
            // Release The Muffalos!
            // Or at least any we have reserved for this job:
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Call, typeof(Patch_JobD_UnloadInv)
                                             .GetMethod("ReleaseReservationsNow", BindingFlags.Static|BindingFlags.NonPublic));
            // Then do the rest of the toil stuff:
            foreach (var x in instructions) yield return x;
        }
        static void ReleaseReservationsNow(JobDriver driver) {
            // Note: I COULD write this all out as IL instructions.  I just choose not to :p
            //       I also COULD check the mod setting here instead of in the Prepare(),
            //            but I choose not to.
            driver.pawn.ClearReservationsForJob(driver.pawn.CurJob);
        }
    }
}
