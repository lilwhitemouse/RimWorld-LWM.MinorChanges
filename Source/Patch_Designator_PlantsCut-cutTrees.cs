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

/*************************************************************
 * Trees, unlike all other plants, do not have the "Cut Plants"
 * option when they are harvestable.  Instead, they ONLY have
 * the "Chop Wood" option.
 *
 * This is clearly very irritating, because one cannot go thru
 * a patch of trees, clicking on each, and then hitting 'y' to
 * remove them.  You have to check: is it mature?  Then chop
 * the bloody wood.
 *
 * F that.
 *
 * Patch RimWorld.Designator_PlantsCut's AcceptsThing()
 * to always return true.
 ************************************************************/

namespace LWM.MinorChanges
{
    [HarmonyPatch(typeof(RimWorld.Designator_PlantsCut), "AffectsThing")]
    static class Patch_Designator_PlantsCut_AffectsThing {
        static bool Prepare() {
            return true;
//            return LoadedModManager.GetMod<MinorChangesMod>().GetSettings<Settings>().
        }
        static bool Prefix(ref bool __result) {
            __result=true;
            return false;
        }
    }
}
