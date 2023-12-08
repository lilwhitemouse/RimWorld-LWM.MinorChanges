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
    [HarmonyPatch(typeof(JobDriver_FeedBaby), "IsValidBreastfeedChair")]
    static class Patch_JobDriver_FeedBaby_IsValidBreastfeedChair
    {
        static Type assignableFixture;
        static bool Prepare()
        {
            if (ModsConfig.IsActive("Dubwise.DubsBadHygiene") && Settings.IsOptionSet("doNotBreastfeedInBathrooms"))
            {
                assignableFixture = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic)
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.FullName.Equals("DubsBadHygiene.Building_AssignableFixture"));
                if (assignableFixture == null)
                {
                    Log.Error("Well, fuckadoodle, dubs bad hygiene isn't loaded yet and can't find assemblies - maybe try changing mod load order?");
                    return false;
                }
                return true;
            }
            return false;
        }

        static bool Postfix(bool __result, Thing t)
        {
            if (assignableFixture.IsAssignableFrom(t.GetType()))
            {
                // I.e., t is an assignableFixture
                return false; // No breastfeeding on the toilet
            }
            return __result;
        }
    }




}