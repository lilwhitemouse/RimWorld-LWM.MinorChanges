using System;
using System.Xml;
using RimWorld;
using Verse;
using Verse.AI;
using HarmonyLib;

namespace LWM.MinorChanges
{
    //TODO: Find what lets someone's grey path get reset whenyou specify eating something so can 
    //  duplicate for our job
    /***********************************
     * Verb_CastAbility's ValidateTarget won't let anything ranged attempt to test for a valid target
     *   if it's outside of immediate line of sight or current range.  We patch range to zero for the 
     *   call (and then return it)
     * */
    [HarmonyPatch(typeof(Verb_CastAbility), "ValidateTarget")]
    static class Patch_Verb_CastAbility_ValidateTarget
    {
        public static bool Prepare()
        {
            // TODO: If we should want to add more affected Abilities, THIS is the place to set up
            //   ShouldPatchesAffectThisVerb
            return LoadedModManager.GetMod<MinorChangesMod>().GetSettings<Settings>().betterSolarPinholes;
        }

        public static bool ShouldPatchesAffectThisVerb(Verb_CastAbility v)
        {
            if (v.ability.def.defName == "SolarPinhole")
                return true;
            return false;
        }

        static bool inOurValidation = false;
        static bool Prefix(Verb_CastAbility __instance, ref bool __result, LocalTargetInfo target, bool showMessages = true)
        {
            if (inOurValidation ||
                !ShouldPatchesAffectThisVerb(__instance)
                || __instance.verbProps.range <= 0f) // just in case...
                return true;
            var originalRange = __instance.verbProps.range;
            __instance.verbProps.range = 0f;
            inOurValidation = true;
            __result = __instance.ValidateTarget(target, showMessages);
            inOurValidation = false;
            __instance.verbProps.range = originalRange;
            return false;
        }
    }
    /************
     * Current test for Verb_CastAbility's CanHitTarget is
     *     return this.verbProps.range <= 0f || base.CanHitTarget(targ);
     *   We are pretending range is 0f, so we'll just short circuit test immediately
     */
    [HarmonyPatch(typeof(Verb_CastAbility), "CanHitTarget")]
    static class Patch_Verb_CastAbility_CanHitTarget
    {
        public static bool Prepare()
        {
            return LoadedModManager.GetMod<MinorChangesMod>().GetSettings<Settings>().betterSolarPinholes;
        }

        static bool Prefix(Verb_CastAbility __instance, ref bool __result)
        {
            if (Patch_Verb_CastAbility_ValidateTarget.ShouldPatchesAffectThisVerb(__instance))
            {
                __result = true;
                return false;
            }
            return true;
        }
    }
}