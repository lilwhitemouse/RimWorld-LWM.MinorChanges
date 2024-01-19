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

/****************************************************
 * Allow PartySpots to gather parties even if they  *
 * are under another object - I will allow Walkable *
 * instead of Standable...actually, I will allow    *
 * whatever.  At players own risk ;p                *
 ****************************************************/
//NOTE: This may not work with Royalty - the investiture ceremonies
//      make the Empire person walk over to a ...Party Spot?
//      Certainly, more research is required before letting this one go! TODO
//      NOTE: TODO: XML changse will be required
namespace LWM.MinorChanges
{
    /* RCellFinder.TryFindGatheringSpot checks for valid gathering spots
     *   using RimWorld.GatheringsUtility's ValidateGatheringSpot(...)
     * That contains the test we are interested in changing:    
     * if (!cell.Standable(map)) {
     *   return false;
     * } // remove this entirely?
     *     
     * (In 1.0, this was in a delegate function; this will be easier in 1.1)
     */
    [HarmonyPatch(typeof(RimWorld.GatheringsUtility), "ValidateGatheringSpot")]
    class Patch_ValidateGatheringSpot {
        static bool Prepare()
        {
            return Settings.IsOptionSet("betterSpots");
        }
        // to remove the first Standable test in its entirety:
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
                                                       ILGenerator generator) {
            List<CodeInstruction> code=instructions.ToList();
            // The "Standable" test Standable(cell, map) will look like
            //   ldarg.0 // cell
            //   ldloc.0 // map
            bool found = false;
            for (int i=0;i<code.Count; i++) {
                if (code[i].opcode==OpCodes.Ldarg_0 && code[i+1].opcode==OpCodes.Ldloc_0 &&
                    code[i+2].opcode==OpCodes.Call && 
                    (MethodInfo)code[i+2].operand== typeof(Verse.GenGrid).GetMethod("Standable")
                    && (code[i+3].opcode==OpCodes.Brtrue ||
                        code[i+3].opcode==OpCodes.Brtrue_S)) {
                    // Skip the entire code block until past the return false:
                    int j = i;
                    for (;j<code.Count;j++) {
                        if (code[j].opcode == OpCodes.Ret) {
                            j++; // move past OpCodes.Ret
                            break;
                        }
                    }
                    if (j < code.Count - 1) {
                        found = true;
                        i = j; // set i to next code after that block
                    }
                }
                yield return code[i];
            }
            if (!found) { Log.Warning("LWM.MinorChanges: could not patch ValidateGatheringSpot"); }
        }
#if false
        // To change Walkable instead of Standable
        static IEnumerable<CodeInstruction> OnlyStandableTranspiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator) {
            List<CodeInstruction> code=instructions.ToList();
            int i;
            for (i=0; i<code.Count; i++) {
                if (code[i].opcode==OpCodes.Call &&
                    (MethodInfo)code[i].operand==typeof(Verse.GenGrid).GetMethod("Standable")) {
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
#endif
    }
}
