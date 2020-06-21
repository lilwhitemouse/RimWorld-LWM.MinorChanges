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
 * The sequence of events is that the player presses a hotkey *
 * and then it goes over to ThingSelectionUtility, either the *
 * SelectNextColonist() or SelectPreviousColonist(). We patch *
 * those two to add a check for whether colonist or animal is *
 * already selected and go from there.                        *
 * Of course, we have to transpile :p Actually, wait - do we? *
 *************************************************************/

namespace LWM.MinorChanges
{
    [HarmonyPatch(typeof(RimWorld.ThingSelectionUtility), "SelectNextColonist")]
    static class Patch_SelectNextColonist {
        static bool Prefix() {
            return Patch_SelectPreviousColonist.DetourToSelectXAnimal(true);
        }
    }
    [HarmonyPatch(typeof(RimWorld.ThingSelectionUtility), "SelectPreviousColonist")]
    public static class Patch_SelectPreviousColonist {
        static bool Prefix() {
            return DetourToSelectXAnimal(false);
        }
        public static bool DetourToSelectXAnimal(bool goToNext) {
            if (!LoadedModManager.GetMod<MinorChangesMod>()
                .GetSettings<Settings>().selectNextAnimal) return true; // vanilla
            Thing selThing = Find.Selector.SingleSelectedThing;
            if (selThing==null) return true;
            Pawn selPawn = selThing as Pawn;
            if (selPawn==null) return true;
            if (selPawn.Map != Find.CurrentMap) return true;
            //if (!p.RaceProps.Animal) return true; // also get wildppl:
            if (!selPawn.AnimalOrWildMan()) return true;
            List<Pawn> mapAnimals;
            if (selPawn.Faction == Faction.OfPlayer) {
                // tamed animal!!
                // This is trickier than I first thought for one reason:
                //   Sorting.
                // I want the animals to move in order as viewed in the Animals
                // main tab window (similar to pawns in the pawn bar). It's not
                // an easy sort to do by hand, and since I can grab it directly
                // from the main tab window...why not?
                // #SlightlyDeepMagic #Reflection

                // The MaintabWindow_... is *the* actual window; it sticks around and one can grab it:
                MainTabWindow_Animals mtw=(MainTabWindow_Animals)DefDatabase<MainButtonDef>.GetNamed("Animals").TabWindow;
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
                    table=(PawnTable)typeof(MainTabWindow_PawnTable).GetField("table",
                                                                              BindingFlags.Instance |
                                                                              BindingFlags.NonPublic |
                                                                              BindingFlags.GetField)
                        .GetValue(mtw as MainTabWindow_PawnTable);
                }
                mapAnimals = table.PawnsListForReading;
            } else {// one animal selected, but is not tame - Wildlife!
                // grabbed straight from MainTabWindow_Wildlife:
                MainTabWindow_Wildlife mtw=(MainTabWindow_Wildlife)DefDatabase<MainButtonDef>.GetNamed("Wildlife").TabWindow;
                var table=(PawnTable)typeof(MainTabWindow_PawnTable).GetField("table",
                                                                              BindingFlags.Instance |
                                                                              BindingFlags.NonPublic |
                                                                              BindingFlags.GetField)
                    .GetValue(mtw as MainTabWindow_PawnTable); // because table is a _PawnTable var
                if (table==null) {
                    // If the player has never opened the Wildlife window:
                    mtw.Notify_ResolutionChanged(); // force building table
                    table=(PawnTable)typeof(MainTabWindow_PawnTable).GetField("table",
                                                                              BindingFlags.Instance |
                                                                              BindingFlags.NonPublic |
                                                                              BindingFlags.GetField)
                        .GetValue(mtw as MainTabWindow_PawnTable);
                }
                mapAnimals = table.PawnsListForReading;
            }
            int index = mapAnimals.IndexOf(selPawn);
            if (index==-1) return true; // not found; who knows what went wrong
            if (goToNext) {
                index++; // go to next, eh?
                if (index >= mapAnimals.Count) index=0;
            } else {
                index--;
                if (index < 0) index=mapAnimals.Count-1;
            }
            CameraJumper.TryJumpAndSelect(mapAnimals[index]);
            return false;
        }
    }
}
