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
    /* Change Dialog_AssignBuildingOwner's DoWindowContents to add
     * a tooltip if the dialog's created from a CompAssignableToPawn_Throne
     * ...
     * Of course, we have to use a Transpiler.
     * ...
     * Ugh.    
     *     
     * There are two places in the code where a RectDivider is set. They look
     * kind of like this:
     *     RectDivider rect3 = rect2.NewRow (num, VerticalJustification.Top);
     *     if (num2 % 2 == 0) {
     *       Widgets.DrawLightHighlight (rect3);
     *     }
     *     rows++;
     * Right before that `if`, let's load that rectDivider and the assignable 
     * and the pawn in question onto the stack and call
     *     TooltipMeditationTypes(rectDivider, assignable, pawn);
     * What could go wrong?
     */
    [HarmonyPatch(typeof(Dialog_AssignBuildingOwner), "DoWindowContents")]
    static class Patch_Dialog_AssignBuildingOwner
    {
        static bool Prepare()
        {
            return Settings.IsOptionSet("showMeditationTypesWhenAssigningThrones");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructionsEnumerable)
        {
            var code = instructionsEnumerable.ToList();
            int i = 0;
            int shortCount = code.Count-10; // to make sure we don't overshoot past the end of the code!
            /*
             *....for the record, I hate doing it this way. It's messy and ugly and almost certainly
             *    WILL break next next time RW gets updated to a major version. Meh.
                    IL_02be: ldc.i4.0
                    IL_02bf: call instance valuetype Verse.RectDivider Verse.RectDivider::NewRow(float32, valuetype Verse.VerticalJustification)
                    IL_02c4: stloc.s 20    <----- Stop here, we just stored the first RectDivider!!
                                    Insert:
                                    ldloc.s 20  // the RD
                                    ldarg.0     // this
                                    ldfld class RimWorld.CompAssignableToPawn RimWorld.Dialog_AssignBuildingOwner::assignable
                                       // this.assignable
                                    ldloc.s 19  // the current pawn
                                    call TooltipMeditationTypes
                    IL_02c6: ldloc.s 18
                    IL_02c8: ldc.i4.2
                    IL_02c9: rem
                    IL_02ca: brtrue.s IL_02d8

                    IL_02cc: ldloc.s 20
                    IL_02ce: call valuetype [UnityEngine.CoreModule]UnityEngine.Rect Verse.RectDivider::op_Implicit(valuetype Verse.RectDivider)
                    IL_02d3: call void Verse.Widgets::DrawLightHighlight(valuetype [UnityEngine.CoreModule]UnityEngine.Rect)

                    IL_02d8: ldloc.s 18
                    IL_02da: ldc.i4.1
                    IL_02db: add          
             */
            for (; i < shortCount; i++)
            {
                yield return code[i];
                // Note the LocalBuilder cast to get the actual stored location index
                if (code[i].opcode == OpCodes.Stloc_S && ((LocalBuilder)code[i].operand).LocalIndex == 20)
                { // just stored
                    i++;  // Advance counter, as we have returned it and we break after this:
                    //Log.Error("Found breakpoint 1!");
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 20); // the RD
                    yield return new CodeInstruction(OpCodes.Ldarg_0);     //   this
                    yield return new CodeInstruction(OpCodes.Ldfld, typeof(RimWorld.Dialog_AssignBuildingOwner)
                        .GetField("assignable", BindingFlags.NonPublic | BindingFlags.Instance));   // this.assignable
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 19); //     pawn
                    yield return new CodeInstruction(OpCodes.Call, typeof(Patch_Dialog_AssignBuildingOwner)
                           .GetMethod("TooltipMeditationTypes", BindingFlags.Static | BindingFlags.NonPublic));
                    break;
                }
            }
            // Now, the second one - this one is less fun >_<
            /*
                    IL_0472: ldc.i4.0
                    IL_0473: call instance valuetype Verse.RectDivider Verse.RectDivider::NewRow(float32, valuetype Verse.VerticalJustification)
                    IL_0478: stloc.s 28     <----------- the RectDivider stored here!
                    ////Insert:
                                 ldloc.s 28  // the RD
                                 ldarg.0     //   this
                                 ldfld class &c &c ::assignable  // this.assignable
                                 ....getting the pawn is less fun:
                                 ldloc.s 25 // this is a '<>c__DisplayClass12_0'
                                 ldfld class Verse.Pawn RimWorld.Dialog_AssignBuildingOwner/'<>c__DisplayClass12_0'::pawn
                                 ...ugh
                    IL_047a: ldloc.s 18
                    IL_047c: ldc.i4.2
                    IL_047d: rem
                    IL_047e: brtrue.s IL_048c

                    IL_0480: ldloc.s 28
                    IL_0482: call valuetype [UnityEngine.CoreModule]UnityEngine.Rect Verse.RectDivider::op_Implicit(valuetype Verse.RectDivider)
                    IL_0487: call void Verse.Widgets::DrawLightHighlight(valuetype [UnityEngine.CoreModule]UnityEngine.Rect)

                    IL_048c: ldloc.s 18
                    IL_048e: ldc.i4.1
                    IL_048f: add
                    IL_0490: stloc.s 18
               */
            for (; i<shortCount; i++)
            {
                yield return code[i];
                if (code[i].opcode == OpCodes.Stloc_S && ((LocalBuilder)code[i].operand).LocalIndex == 28)
                { // Just stored the RectD
                    //Log.Warning("Found second breakpoint");
                    i++; // going to break after this again
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 28); // RD
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, typeof(RimWorld.Dialog_AssignBuildingOwner)
                        .GetField("assignable", BindingFlags.NonPublic | BindingFlags.Instance));   // this.assignable
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 25);
                    // ...just go looking for where the pawn is called next:
                    for (int j=1; j<shortCount; j++)
                    { // just return the next fld from that wacky hidden inner class thing
                        // Worst case scenario, something goes wrong and it can't be cast to Pawn
                        if (code[j].opcode == OpCodes.Ldloc_S && ((LocalBuilder)code[j].operand).LocalIndex == 25
                            && code[j+1].opcode == OpCodes.Ldfld)
                        {
                            yield return code[j + 1];
                            break;
                        }
                    }
                    // Finally, call the damn stuff:
                    yield return new CodeInstruction(OpCodes.Call, typeof(Patch_Dialog_AssignBuildingOwner)
                           .GetMethod("TooltipMeditationTypes", BindingFlags.Static | BindingFlags.NonPublic));
                    break;
                }
            }
            for (; i<code.Count; i++) // finish returning everything
            {
                yield return code[i];
            }
            yield break;
        }

        static void TooltipMeditationTypes(RectDivider rect, CompAssignableToPawn assignable, Pawn pawn)
        {
            if (assignable is CompAssignableToPawn_Throne && pawn != null
                && Mouse.IsOver(rect))
            {
                TooltipHandler.TipRegion(rect, "LWMMC_CanMeditateTo".Translate(pawn, MeditationUtility.FocusTypesAvailableForPawnString(pawn)));
            }
        }
    }




}
 