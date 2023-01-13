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

/**************************************************************
 * When you select a colonist, you can press < or > to select *
 * the previous or the next colonist. This is a super awesome *
 * feature. But it only works with a colonist. Why not extend *
 * it so it works to move to the previous and next animals in *
 * either the domestic "Animals" tab or in the "Wildlife" tab *
 * as well? Let's do that!                                    *
 * And as pointed out by ZzZombo, why not do prisoners, too?  *
 * Heck, we can do foreign factinos by faction as well!       *
 * The sequence of events is that the player presses a hotkey *
 * and then it goes over to ThingSelectionUtility, either the *
 * SelectNextColonist() or SelectPreviousColonist(). We patch *
 * those two to add a check for whether colonist or animal is *
 * already selected and go from there.                        *
 * Of course, we have to transpile :p Actually, wait - do we? *
 * No!  We use a Prefix call, which lets us revert to vanilla *
 * behavior any time we want.                                 *
 *************************************************************/

namespace LWM.MinorChanges
{
    [HarmonyPatch(typeof(RimWorld.ThingSelectionUtility), "SelectNextColonist")]
    static class Patch_SelectNextColonist {
        static bool Prefix() {
            return Patch_SelectPreviousColonist.DetourToSelectXPawn(true);
        }
    }
    [HarmonyPatch(typeof(RimWorld.ThingSelectionUtility), "SelectPreviousColonist")]
    public static class Patch_SelectPreviousColonist {
        static bool Prefix() {
            return DetourToSelectXPawn(false);
        }
        public static bool DetourToSelectXPawn(bool goToNext) {
            if (!LoadedModManager.GetMod<MinorChangesMod>()
                .GetSettings<Settings>().selectNextAnimal) return true; // vanilla
            Thing selThing = Find.Selector.SingleSelectedThing;
            if (selThing==null) return true;
            Pawn selPawn = selThing as Pawn;
            if (selPawn==null) return true;
            if (selPawn.Map != Find.CurrentMap) {
                Debug.Log("Selection: pawn "+selPawn+" is not in the same map");
                return true;
            }
            List<Pawn> listOfSimilarPawns;
            // Get "civilized" humans:
            //if (!p.RaceProps.Animal) return true; // also get wildppl:
            if (!selPawn.AnimalOrWildMan()) {
                Debug.Warning("SelectXPawn: "+selPawn+" is humanlike");
                // human-ish
                if (selPawn.Faction==Faction.OfPlayer) {
                    Debug.Log("  but is player's faction.");
                    return true;
                }
                if (selPawn.IsPrisoner) {
                    Debug.Log("  and is a Prisoner");
                    // there is no in-game list of prisoners, so we make one
                    // and sort it how we please and use it:
                    // Note: don't use selPawn.IsPrisonerOfColony because may be 
                    //     a prisoner in a rescue quest
                    listOfSimilarPawns = Find.CurrentMap.mapPawns.AllPawnsSpawned
                               .Where(x => x.HostFaction == selPawn.HostFaction).ToList();
                    // If this is not correct, we'll still default to vanilla later, so all good.
                } else { //non player faction, non prisoner.
                    Debug.Log("  and is a member of faction "+selPawn.Faction);
                    // cycle through all pawns of this faction:
                    listOfSimilarPawns=Find.CurrentMap.mapPawns.FreeHumanlikesSpawnedOfFaction(selPawn.Faction);
                }
            } else { //animal (or wildperson, which is counted with the animals)
                if (selPawn.Faction == Faction.OfPlayer) {
                    Debug.Warning("SelectXPawn: "+selPawn+" is tamed animal!");
                    // tamed animal!!
                    // This is trickier than I first thought for one reason:
                    //   Sorting.
                    // I want the animals to move in order as viewed in the Animals
                    // main tab window (similar to pawns in the pawn bar). It's not
                    // an easy sort to do by hand, and since I can grab it directly
                    // from the main tab window...why not?
                    // #SlightlyDeepMagic #Reflection

                    // The MaintabWindow_... is *the* actual window; it sticks around and one can grab it:
                    //   use "as" to make sure it CAN be cast to MTW_A:
                    MainTabWindow_Animals mtw=(MainTabWindow_Animals)
                        (DefDatabase<MainButtonDef>.GetNamed("Animals").TabWindow as MainTabWindow_Animals);
                    if (mtw == null) { Log.Message("LWM:Minor changes: could not get MainTabWindow_Animals, as it's a "+
                                                   DefDatabase<MainButtonDef>.GetNamed("Animals").GetType().ToString());
                        return true; } // fail gracefully.
                    // Now want mtw.table...which is private. So we use reflection to get it:
                    // The MainTabWindow_Animals(Wildlife, etc) is a MainTabWindow_PawnTable
                    // Getting the PawnTable takes a little work:
                    var table=(PawnTable)typeof(MainTabWindow_PawnTable).GetField("table",
                                                                                  BindingFlags.Instance |
                                                                                  BindingFlags.NonPublic |
                                                                                  BindingFlags.GetField)
                        .GetValue(mtw as MainTabWindow_PawnTable); // because table is a ..._PawnTable var
                    if (table==null) {
                        // If the player has never opened the Animals window, there's no table!
                        // But we can force building the table:
                        mtw.Notify_ResolutionChanged();
                        // try again
                        table=(PawnTable)typeof(MainTabWindow_PawnTable).GetField("table",
                                                                                  BindingFlags.Instance |
                                                                                  BindingFlags.NonPublic |
                                                                                  BindingFlags.GetField)
                            .GetValue(mtw as MainTabWindow_PawnTable);
                        if (table == null)
                        {
                            Log.Warning("LWM.MinorChanges: Could not generate Animals MainTabWindow's .table");
                            return true;  // fail gracefully
                        }
                    }
                    listOfSimilarPawns = table.PawnsListForReading;
                } else {// one animal selected, but is not tame - Wildlife!
                    Debug.Warning("SelectXPawn: "+selPawn+" is wild animal!");
                    // grabbed straight from MainTabWindow_Wildlife:
                    MainTabWindow_Wildlife mtw=(MainTabWindow_Wildlife)
                        (DefDatabase<MainButtonDef>.GetNamed("Wildlife").TabWindow as MainTabWindow_Wildlife);
                    if (mtw == null) { Log.Message("LWM:Minor changes: could not get MainTabWindow_Wildlife");
                        return true; } // fail gracefully.
                    var table=(PawnTable)typeof(MainTabWindow_PawnTable).GetField("table",
                                                                                  BindingFlags.Instance |
                                                                                  BindingFlags.NonPublic |
                                                                                  BindingFlags.GetField)
                        .GetValue(mtw as MainTabWindow_PawnTable); // because table is a _PawnTable var
                    if (table==null) {
                        // If the player has never opened the Wildlife window:
                        mtw.Notify_ResolutionChanged(); // force building table
                        // try again
                        table=(PawnTable)typeof(MainTabWindow_PawnTable).GetField("table",
                                                                                  BindingFlags.Instance |
                                                                                  BindingFlags.NonPublic |
                                                                                  BindingFlags.GetField)
                            .GetValue(mtw as MainTabWindow_PawnTable);
                        if (table == null)
                        {
                            Log.Warning("LWM.MinorChanges: Could not generate Wildlife MainTabWindow's .table");
                            return true; // fail gracefully
                        }
                    }
                    listOfSimilarPawns = table.PawnsListForReading;
                }
            } // end else //animal
            int index = listOfSimilarPawns.IndexOf(selPawn);
            if (index==-1) return true; // not found; who knows what went wrong
            if (goToNext) {
                index++; // go to next, eh?
                if (index >= listOfSimilarPawns.Count) index=0;
            } else {
                index--;
                if (index < 0) index=listOfSimilarPawns.Count-1;
            }
            CameraJumper.TryJumpAndSelect(listOfSimilarPawns[index]);
            return false;
        }
    }
}
