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

/****************************************************
 * Allow PartySpots to gather parties even if they  *
 * are under another object - I will allow Walkable *
 * instead of Standable...actually, I will allow    *
 * whatever.  At players own risk ;p                *
 ****************************************************/

namespace LWM.MinorChanges
{
    /* RCellFinder.TryFindPartySpot uses as a baseValidator a check
     * if (!cell.Standable(map)) {
     *   return false;
     * }
     * This is in a delegate function.
     * So. Fun.
     * Transpiler HO!
     * #DeepMagic
     */
    [HarmonyPatch]
    class Patch_RCellFinder_TFPartySpot {
        static MethodBase TargetMethod() {//The target method is found using the custom logic defined here
            // Our delegate function is <>m__0 which is inside an
            // anonymous class <TryFindPartySpot>c__AnonStoreyF
            var anonymousClass=typeof(RimWorld.RCellFinder).GetNestedTypes(Harmony.AccessTools.all)
                .FirstOrDefault(t => t.FullName.Contains("c__AnonStoreyF"));
            if (anonymousClass==null) {
                Log.Error("LWM.MC: Could not find RCellFinder's c__AnonStoreyF");
                return null;
            }
            var anonymousMethod=anonymousClass.GetMethods(AccessTools.all)
                .FirstOrDefault(t => t.Name.Contains("m__0"));
            if (anonymousMethod==null) {
                Log.Error("LWM.MC: Could not find RCellFinder's m__0");
            }
            // For the record, anonymousClass.GetMethod(anonymousMethod.Name) returns null
            // I have no idea why.
            return anonymousMethod;
        }
        // to remove the first Standable test in its entirety:
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
                                                       ILGenerator generator) {
            List<CodeInstruction> code=instructions.ToList();
            int i=0;
            // The "Standabl" test is the very first thing that happens, so we look for it:
            if (code[3].opcode==OpCodes.Call && code[3].operand==typeof(Verse.GenGrid).GetMethod("Standable")) {
                for (; i<code.Count; i++) {
                    if (code[i].opcode==OpCodes.Ret) {
                        i++;
                        break;
                    }
                }
            } else { // Hey, we can default to replacing with Walkable if anything weird happened!
                foreach (var c in OnlyStandableTranspiler(instructions, generator)) {
                    yield return c;
                }
                yield break;
            }
            for (; i<code.Count; i++) {
                yield return code[i];
            }
        }
        // To change Walkable instead of Standable
        static IEnumerable<CodeInstruction> OnlyStandableTranspiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator) {
            List<CodeInstruction> code=instructions.ToList();
            int i;
            for (i=0; i<code.Count; i++) {
                if (code[i].opcode==OpCodes.Call &&
                    code[i].operand==typeof(Verse.GenGrid).GetMethod("Standable")) {
                    //Log.Warning("Found Standable; replacing with Walkable");
                    yield return new CodeInstruction(OpCodes.Call, typeof(Verse.GenGrid).GetMethod("Walkable"));
                    i++;
                    break;
                }
                yield return code[i];
            }
            for (;i<code.Count;i++) { // finish up
                yield return code[i];
            }
        }
    }
}
