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
 * 1.3 carpets - they all require cloth?
 * 
 * In 1.4, building carpets gives you a color choice, so
 * it isn't needed there anymore. But Dubs Hygiene has a
 * pair of ceiling fans that are both steel and are both
 * in the same build slot (one small, one big). This mod
 * makes it so the icon is the fan, not steel. Yes, it's
 * a small thing, but I like it better this way!
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
            // This is throwing errors for 1l_apaJ on steam:
            /*
             * Root level exception in OnGUI(): System.NullReferenceException: Object reference not set to an instance of an object
               at LWM.MinorChanges.Patch_Designator_Dropdown.Prefix (Verse.ThingDef& __result, Verse.Designator des) [0x0002b] in <37a861eee9a4448fb7a61f19687aee6e>:0
               at (wrapper dynamic-method) RimWorld.Designator_Dropdown.RimWorld.Designator_Dropdown.GetDesignatorCost_Patch1(RimWorld.Designator_Dropdown,Verse.Designator)
             * ...I have no idea what could even be happening :(
             */
            // so....let's try re-writing it. Why not allow fallback to vanilla anyway, eh?
            /*
            if (des is Designator_Place designator_Place)
            {
                BuildableDef placingDef = designator_Place.PlacingDef;
                if (placingDef is ThingDef && !placingDef.MadeFromStuff)
                {
                    __result = (placingDef as ThingDef);
                    return false;
                }
                // vanilla logic
                if (placingDef.costList.Count > 0)
                {
                    __result = placingDef.costList.MaxBy((ThingDefCountClass c) => c.thingDef.BaseMarketValue * (float)c.count).thingDef;
                    return false;
                }
            }
            __result = null;
            return false;
            */
            try
            {
                if (des is Designator_Place designator_Place)
                {
                    BuildableDef placingDef = designator_Place.PlacingDef;
                    if (placingDef is ThingDef && placingDef?.MadeFromStuff == false)
                    {
                        __result = (placingDef as ThingDef);
                        return false;
                    }
                }
                return true; // why bother trying anything else anyway? This doesn't actually get called super often
            } catch (Exception e)
            {
                Log.Warning("LWM.MinorChanges: Designator_Dropdown for better icons failed for some weird reason: "+e);
                return true; // fallback
            }
        }
    }
}
