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

/*******************************************
 * When a new drug policy is created, make *
 * penoxycyline default to every 5 days.   *
 *******************************************/

namespace LWM.MinorChanges
{
    /************* DrugPolicy's InitializeIfNeeded *************/
    // This seems the obvious place to patch.
    // Add a check to add defaults to DrugPolicyEntries:
    //   DrugPolicyEntry drugPolicyEntry = new DrugPolicyEntry();
    //   drugPolicyEntry.drug = allDefsListForReading[i];
    //   drugPolicyEntry.allowedForAddiction = true;
    // * AddDefaults(drugPolicyEntry); //   Add this line into the code!
    //   this.entriesInt.Add(drugPolicyEntry);
    //
    // It's easy enough to insert a Transpiler command here, so we do it.
    [HarmonyPatch(typeof(RimWorld.DrugPolicy), "InitializeIfNeeded")]
    public static class Patch_DrugPolicy_IIN {
        public static bool Prepare() {
            return LoadedModManager.GetMod<MinorChangesMod>().GetSettings<Settings>().applyDrugDefaults;
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
                                                       ILGenerator generator) {
            List<CodeInstruction> code=instructions.ToList();
            for (int i=0; i<code.Count; i++) {
                yield return code[i];
                if (code[i].opcode==OpCodes.Stfld &&
                    (code[i].operand as FieldInfo).Name=="allowedForAddiction") {
                    yield return new CodeInstruction(OpCodes.Ldloc_2);
                    yield return new CodeInstruction(OpCodes.Call,
                                                     typeof(Patch_DrugPolicy_IIN)
                                                     .GetMethod("AddDefaults", BindingFlags.NonPublic
                                                         |BindingFlags.Static));
                }
            }
        }
        static void AddDefaults(DrugPolicyEntry dp) {
            // I hard code these values here.  I'm sure there's a slicker way to do this using XML etc.
            // But this is way easier and it does what *I* need, at any rate.
            if (dp.drug.defName=="Penoxycyline") {
                if (ModLister.HasActiveModWithName("Stronger Penoxycyline (1.4)")) {
                    dp.daysFrequency=30f; // one season apparently
                } else
                    dp.daysFrequency=5f; // vanilla
            }
        }
    }
}
