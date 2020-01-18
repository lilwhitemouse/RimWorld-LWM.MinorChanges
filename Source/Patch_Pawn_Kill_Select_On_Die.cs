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
using Harmony;

/*********************************************************
 * If the user is trying to kill someone (enemy, animal, *
 * etc), and has the pawn selected, when the pawn dies,  *
 * we want the corpse to be selected.                    *
 * Patch Pawn's Kill() to make this happen.              *
 * See Patch_Blueprint_Select_On_Build.cs                *
 *********************************************************/

namespace LWM.MinorChanges
{
    /********************* Pawn -> Corpse **************************/
    // See Patch_Blueprint_Select_On_Build.cs
    [HarmonyPatch(typeof(Verse.Pawn), "Kill")]
    public static class Patch_Pawn_Kill {
        // We use Prefix() to check if the Pawn is the only thing selected
        [HarmonyPriority(Harmony.Priority.First)]
        public static void Prefix(Pawn __instance) {
            Patch_Blueprint.SelectNewThing=false;
            if (! __instance.Spawned) return;
            if (Find.Selector.SingleSelectedThing != __instance) return;
            Patch_Blueprint.SelectNewThing=true;
        }
        // When the pawn dies, the code checks several times whether a corpse has been
        // generated.
        //   We use Transpiler to insert code that will select the corpse if needed.
        // if (corpse != null) {
        //   HandleSelector(corpse); //<-----Add this line
        //   // vanilla stuff
        //   // probably icky
        //   // and gross....
        //
        // We have to do it via Transpiler because the corpse created doesn't show up anywhere we can grab via Postfix.
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
                                                       ILGenerator generator) {
            List<CodeInstruction> code=instructions.ToList();
            bool insertedCodeYet=false;
            for (int i=0; i<code.Count; i++) {
                yield return code[i];
                if (code[i].opcode==OpCodes.Ldloc_S &&     // ldloc.s 29 is the corpse
                    ((LocalBuilder)code[i].operand).LocalIndex==29 &&
                    code[i+1].opcode==OpCodes.Brfalse &&   // the if (corpse==null) jump past the code block
                    !insertedCodeYet) {
                    i++;  // advance to the branch
                    yield return code[i];
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 29);
                    yield return new CodeInstruction(OpCodes.Call,
                                                     typeof(Patch_Frame_CompleteConstruction) // might as well reuse code
                                                     .GetMethod("HandleSelector", BindingFlags.Public |
                                                         BindingFlags.Static));
                }
            }
        }
/*        static void HandleSelector(Corpse body) {
            if (!Patch_Blueprint.SelectNewThing) return;
            Patch_Blueprint.SelectNewThing=false;
            if (body==null) return;
            Find.Selector.Select(body);
        }*/
    }
}
