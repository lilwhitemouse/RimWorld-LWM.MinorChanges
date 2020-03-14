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

/******************************************************
 * If the user has a blueprint or frame selected, and *
 * some pawn finishs builidng that blueprint or frame,*
 * select the newly finished building.                *
 ******************************************************/

namespace LWM.MinorChanges
{
    /********************* Blueprint -> new Thing **************************/
    // This handles Blueprints becoming things - this could include a dining chair
    //   being moved to a new location (replacing the blueprint with the chair) or
    //   a pawn bringing wood to build a new dining chair to the blueprint, replacing
    //   it with a frame.
    //
    // We patch Blueprint's TryReplaceWithSolidThing, which does what it says on the box.
    // We use Prefix() to check if the Blueprint is the only thing selected,
    //   and then use Postfix() to select the created Thing if it was.
    [HarmonyPatch(typeof(RimWorld.Blueprint), "TryReplaceWithSolidThing")]
    public static class Patch_Blueprint {
        public static bool SelectNewThing=false;
        [HarmonyPriority(HarmonyLib.Priority.First)]
        public static void Prefix(Blueprint __instance) {
            SelectNewThing=false;
            if (! __instance.Spawned) return;
            //if (__instance.Map==null) return; // got to be, it's spawned?
            if (Find.Selector.SingleSelectedThing != __instance) return;
            SelectNewThing=true;
        }
        public static void Postfix(Thing createdThing, bool __result) {
            if (!SelectNewThing) return;
            SelectNewThing=false;
            if (!__result) return;
            if (createdThing==null) return;
            Find.Selector.Select(createdThing);
        }
    }

    /**************************  Frame -> new Thing *****************************/
    // When the Frame is finished being built, CompleteConstruction() is called.
    //   We use Transpiler to replace creating the new item
    //     GenSpawn.Spawn(thing, base.Position, map, base.Rotation, WipeMode.FullRefund, false);
    //   with
    //     Thing t=GenSpawn.Spawn(thing, base.Position, map, base.Rotation, WipeMode.FullRefund, false);
    //     HandleSelector(t);
    //   which will select the new item if only the frame was selected.
    //
    // In the MSIL world, GenSpawn.Spawn puts a Thing on the stack.
    // The next instruction is Pop, to remove the unused Thing.
    // We replace the Pop with a call to HandleSelector(), which uses the Thing, and all is good.
    //
    // We have to do it via Transpiler because the Thing created doesn't show up anywhere we can grab via Postfix.
    [HarmonyPatch(typeof(RimWorld.Frame), "CompleteConstruction")]
    class Patch_Frame_CompleteConstruction {
        [HarmonyPriority(HarmonyLib.Priority.First)]
        public static void Prefix(Frame __instance) {
            Patch_Blueprint.SelectNewThing=false;
            if (! __instance.Spawned) return;
            //if (__instance.Map==null) return; // got to be, it's spawned?
            if (Find.Selector.SingleSelectedThing != __instance) return;
            Patch_Blueprint.SelectNewThing=true;
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
                                                       ILGenerator generator) {
            List<CodeInstruction> code=instructions.ToList();
            for (int i=0; i<code.Count; i++) {
                yield return code[i];
                if (code[i].opcode==OpCodes.Call &&
                    (code[i].operand as MethodInfo).Name=="Spawn" &&
                    code[i+1].opcode==OpCodes.Pop) {
                    i++; // skip original "pop" command
                    // Thing created by Spawn(...) is now on the stack, call HandleSelector
                    yield return new CodeInstruction(OpCodes.Call,
                                                     typeof(Patch_Frame_CompleteConstruction)
                                                     .GetMethod("HandleSelector", BindingFlags.Public |
                                                                BindingFlags.Static));
                }
            }
        }
        public static void HandleSelector(Thing t) {
            if (!Patch_Blueprint.SelectNewThing) return;
            Patch_Blueprint.SelectNewThing=false;
            if (t==null) return;
            Find.Selector.Select(t);
        }
        // Handle Selector above may not be called: player may have selected frame
        //   for concrete, which is not selectable when finished.
        // So make sure SelectNewThing is false:
        public static void Postfix() {
            Patch_Blueprint.SelectNewThing = false;
        }
    }
}
