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
    /********************************************
     * Pawns randomly get married.  Often, they
     * will pick a really stupid time (everyone
     * might have caravaned to go fight someone
     * or maybe some guests have plague, &c) We
     * add a dialog window to ask the player if
     * the pawns should have their wedding NOW.
     *
     * We could hijack
     *   VoluntarilyJoinableLordsStarter
     *     .TryStartMarriageCeremony()
     * BUT that is more complicated and risker,
     * if another modder changes wedding logic.
     *
     * Instead, we modify
     *   Pawn_RelationsTracker
     *     .Tick_CheckStartMarriageCeremony()
     * and insert our dialog after the tests for
     * whether the pawns are ready for their big
     * day have all passed.
     *
     * We use a Transpiler to replace this:
     *   this.pawn.Map.lordsStarter.TryStartMarriageCeremony(this.pawn, this.directRelations[i].otherPawn);
     * with our call
     *   CheckIfShouldStartWedding(pawn1, pawn2);
     *
     * Also, a "WeddingCeremony" is totaly a "Marriage"
     */
    [HarmonyPatch(typeof(RimWorld.Pawn_RelationsTracker), "Tick_CheckStartMarriageCeremony")]
    static class Patch_Pawn_RelationsTracker {
        //Hmm... should I add a setting for this?  Prolly not, as it doesn't change game play.
        public const int NumberWeddingPrompts = 7;
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
                                                       ILGenerator generator) {
            List<CodeInstruction> code=instructions.ToList();
            var pawnDotGetMap=HarmonyLib.AccessTools.Method(typeof(Thing), "get_Map");
            //if (pawnDotGetMap==null) Log.Error("Could not load pawn.Map");
            var mapDotLordsStarter=HarmonyLib.AccessTools.Field(typeof(Map), "lordsStarter");
            //if (mapDotLordsStarter==null) Log.Error("Could not load Map.lordsStarter?");
            int i=0;
            for (; i<code.Count; i++) {
                if (code[i].opcode==OpCodes.Callvirt &&
                    (MethodInfo)code[i].operand==pawnDotGetMap &&
                    code[i+1].opcode==OpCodes.Ldfld &&
                    (FieldInfo)code[i+1].operand==mapDotLordsStarter &&
                    i+10 < code.Count && // safety check here:
                    code[i+9].opcode==OpCodes.Callvirt &&
                    ((MethodInfo)code[i+9].operand).Name=="TryStartMarriageCeremony"
                    /* todo? Could put test here to see if it has two parameters */ ) {
                    //this.pawn.Map.lordsStarter.TryStartMarriageCeremony(this.pawn, this.directRelations[i].otherPawn);
                    //         ^                                                    ^
                    //         we are here (pawn1 is on the stack)                  |
                    //              ^ i+1 is here                                   |
                    // We want pawn1 on the stack, so that's good.                  |
                    // We also want pawn2 on the stack   ----------------------------
                    // so skip ahead a little:
                    i++; // code[i] is now .lordsStarter
                    i++; // code[i] is now ldarg0 (this)
                    i++; // code[i] is now .pawn
                    i++; // code[i] is now starting this.directRelations[i].otherPawn (we want this)
                    // return IL code until reach code for TryStartMarriageCeremony -
                    for (; i<code.Count; i++) {
                        if (code[i].opcode==OpCodes.Callvirt &&
                            ((MethodInfo)code[i].operand).Name=="TryStartMarriageCeremony") {
                            //Log.Message("Found TryStartMarriageCeremony; inserting own code");
                            break;
                        }
                        yield return code[i];
                    }
                    i++; // code[i] now points to line AFTER TryStartMarriageCeremony: pop
                    i++; // we don't need pop, because we call a void, not a bool
                    yield return new CodeInstruction(OpCodes.Call,
                                HarmonyLib.AccessTools.Method(typeof(Patch_Pawn_RelationsTracker), "CheckIfShouldStartWedding"));
                    break;
                } //end if
                yield return code[i];
            }//end loop
            for (; i<code.Count; i++) {
                yield return code[i];
            }
            yield break;
        }
        // Open the dialog box that will ask the player if the wedding should start
        static void CheckIfShouldStartWedding(Pawn p1, Pawn p2) {
            // The dialog window is modeled after RW.IncidentWorker_CaravanDemand
            //   (caravan ambushed, demand XYZ, give and leave, refuse and fight, etc)
            string yesPrompt = "LWMMCweddingYes" + Rand.RangeInclusive(1, NumberWeddingPrompts).ToString();
            // Ugh. New translation methods. Ugh. I stole this from MentalState_SocialFighting
            // Insha'allah it'll work. I don't have a good way to test it right now ><
            DiaNode diaNode = new DiaNode("LWMMCweddingPrompt".Translate(p1.NameFullColored, p2.NameFullColored, 
                                                                         p1.Named("PAWN1"), p2.Named("PAWN2")));
            DiaOption optionYes = new DiaOption(yesPrompt.Translate());
            optionYes.action=delegate() {
                p1.Map.lordsStarter.TryStartMarriageCeremony(p1, p2);
            };
            optionYes.resolveTree=true;
            DiaOption optionNo = new DiaOption("LWMMCweddingNo".Translate());
            optionNo.resolveTree=true;
            diaNode.options.Add(optionYes);
            diaNode.options.Add(optionNo);
            Find.WindowStack.Add(new Dialog_NodeTree(diaNode, false, false, null /*title*/));
        }
    }
    /************* DEBUG TEST FOR MARRIAGE TEXTS *****************/
#if false
    // Throws up marriage prompts for the first two pawns on the map on game load.
    //   Selecting the "yes" prompt tries the next.
    //   Selecting the "no" stops
    public class Tmp : GameComponent
    {
        int count = 1;
        public Tmp(Game game)
        {
            count = 1;
        }
        public override void LoadedGame()
        {
            if (Find.CurrentMap != null)
            {
                Pawn p1 = Find.CurrentMap.mapPawns.FreeColonists[0];
                Pawn p2 = Find.CurrentMap.mapPawns.FreeColonists[1];

                string yesPrompt = "LWMMCweddingYes" + count.ToString();  //Rand.RangeInclusive(1, 7).ToString();
                // Ugh. New translation methods. Ugh. I stole this from MentalState_SocialFighting
                // Insha'allah it'll work. I don't have a good way to test it right now ><
                DiaNode diaNode = new DiaNode("LWMMCweddingPrompt".Translate(p1.NameFullColored, p2.NameFullColored, p1.Named("PAWN1"), p2.Named("PAWN2")));
                DiaOption optionYes = new DiaOption(yesPrompt.Translate());
                optionYes.action = delegate () {
                    count++;
                    if (count > Patch_Pawn_RelationsTracker.NumberWeddingPrompts) count = 1;
                    LoadedGame();
                };
                optionYes.resolveTree = true;
                DiaOption optionNo = new DiaOption("LWMMCweddingNo".Translate());
                optionNo.resolveTree = true;
                diaNode.options.Add(optionYes);
                diaNode.options.Add(optionNo);
                Find.WindowStack.Add(new Dialog_NodeTree(diaNode, false, false, null /*title*/));
            }
        }
    }
#endif
}
