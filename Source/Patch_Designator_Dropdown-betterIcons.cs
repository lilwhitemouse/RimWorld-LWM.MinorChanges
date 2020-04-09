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
 * Some items in the architect menu have more than one option,
 * such as dining chairs you can make out of either steel or
 * wood, or such as the carpets, that can come in different
 * colors.  In vanilla, they will both display a resource
 * that is needed to build them.
 *
 * However, that doesn't make sense for things like the
 * carpets - they all require cloth?
 *
 * The thing that contains the multiple options is the
 * RimWorld.Designator_Dropdown, and it decides what icon
 * to display by using GetDesignatorCost.
 *
 * We patch GetDesignatorCost to show the icon for the
 * item itself, if there's no actual options in what
 * material is used.
 ************************************************************/

namespace LWM.MinorChanges
{
    [HarmonyPatch(typeof(RimWorld.Designator_Dropdown), "GetDesignatorCost")]
    static class Patch_Designator_Dropdown {
        static bool Prepare() {
            return true;
//            return LoadedModManager.GetMod<MinorChangesMod>().GetSettings<Settings>().
        }
        static bool Prefix(ref ThingDef __result, Designator des) {
            Designator_Place designator_Place = des as Designator_Place;
			if (designator_Place != null)
			{
				BuildableDef placingDef = designator_Place.PlacingDef;
                if (placingDef is ThingDef && !placingDef.MadeFromStuff) {
                    __result=(placingDef as ThingDef);
                    return false;
                }
                // vanilla logic
                if (placingDef.costList.Count > 0)
				{
					__result=placingDef.costList.MaxBy((ThingDefCountClass c) => c.thingDef.BaseMarketValue * (float)c.count).thingDef;
                    return false;
				}
			}
			__result= null;
            return false;
        }
    }
}
