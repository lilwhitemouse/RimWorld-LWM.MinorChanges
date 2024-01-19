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
    /// </summary>
//    [HarmonyPatch()]
    static class Patch_ThingDefGenerator_Neurotrainer_Label
    {
        /* We can't do settings here, because settings haven't been initialized.  Drat.
         * So.....we just run our transpile, and check the setting when the transpiled
         * funciton is called - which is fine, it doesn't happen often or anything :-)
        static bool Prepare()
        {
            return ModsConfig.RoyaltyActive && Settings.IsOptionSet("labelPsycastLevels");
        }
        */
        // This is used directly in MinorChanges.cs too, because why not
        public static MethodBase TargetMethod() //The target method is found using the custom logic defined here
        {
            // Copied from Deep Storage, so there might be newer, easier ways of doing this:
            // So IEnumerables suck.
            // There is the hidden IL class called MoveNext that's a part of <ImpliedThingDef>d__3, and we want
            //   to patch that. So.
            var method = typeof(RimWorld.ThingDefGenerator_Neurotrainer).GetNestedType("<ImpliedThingDefs>d__3", AccessTools.all)
                    .GetMethod("MoveNext", AccessTools.all);
            if (method == null) Log.Error("LWM.MinorChanges: Transpiler could not find \"<ImpliedThingDefs>d__3\" :( ");
            return method;
        }

        /// <summary>
        /// </summary>
        /// <returns>The transpile.</returns>
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructionsEnumerable)
        {
            var code = instructionsEnumerable.ToList();
            // Find the IL code references we want:


            for (int i = 0; i < code.Count; i++)
            {
                yield return code[i];
                if (code[i].opcode == OpCodes.Ldstr && (string)code[i].operand == "PsycastNeurotrainerLabel")
                {
                    //Log.Message("LWM.MinorChanges: Found PsycastNeurotrainerLabel");
                    i++;
                    yield return code[i]; // should be ldloc.2 or something - loading the def
                    if (code[i+1].opcode == OpCodes.Ldfld && code[i+1].OperandIs(typeof(Verse.Def).GetField("label")))
                    {
                        //Log.Message("....and found the label!");
                        yield return code[i]; // put the def on the stack again!
                        i++;
                        yield return code[i]; // the ldfld .label
                        yield return new CodeInstruction(OpCodes.Call, typeof(Patch_ThingDefGenerator_Neurotrainer_Label)
                                 .GetMethod("AdjustLabel", BindingFlags.NonPublic | BindingFlags.Static));
                        for (int j=i+1; j<code.Count; j++)
                        {
                            yield return code[j];
                        }
                        yield break;
                    }
                }
            }

            Log.Error("LWM.MinorChanges: transpile for Psytrainer label failed :(");
            yield break;
        } // end transpile

        static string AdjustLabel(AbilityDef def, string oldLabel)
        {
            //Log.Error("AdjustLabel for " + def.defName + ": " + oldLabel + " -> " +
            // "LWMMC_Parens".Translate(oldLabel,
            //                "TextMote_SkillUp".Translate(def.level)));
            if ( // ModsConfig.RoyaltyActive &&  // has to be active for psycasts :p
                Settings.IsOptionSet("labelPsycastLevels"))
            {
                return "LWMMC_Parens".Translate(oldLabel,
                                "TextMote_SkillUp".Translate(def.level) // Level 1, etc
                                );
            }
            return oldLabel;
        }

    }
}
