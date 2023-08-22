using System;
using System.Xml;
using RimWorld;
using Verse;
using Verse.AI;
using HarmonyLib;

namespace LWM.MinorChanges
{
    /******************NOTE: Both ValidateTarget and CanHitTarget are patched **********/
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
            return Settings.IsOptionSet("easierCasting");
        }

        public static bool ShouldPatchesAffectThisVerb(Verb_CastAbility v)
        {
            // We check to see if it's OUR JobDriver:
            // NOTE: the static constructor for our jobdriver contains the code that applies 
            //   the jobdriver to any abilities that should be applied (e.g., Solar Pinhole, Pain Block, &c)
            return v.ability.def.jobDef == JobDriver_CastAbilityGoTo_Distance.jobDef;
            /*
            if (v.ability.def.jobDef == JobDriver_CastAbilityGoTo_Distance.jobDef)
            {
                return true;
            }
            return false;
            */           
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
     *   and avoid any questions of trying to hit a target through a wall & far away
     */
    [HarmonyPatch(typeof(Verb_CastAbility), "CanHitTarget")]
    static class Patch_Verb_CastAbility_CanHitTarget
    {
        public static bool Prepare()
        {
            return Settings.IsOptionSet("easierCasting");
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