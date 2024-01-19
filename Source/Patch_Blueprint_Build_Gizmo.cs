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
    /// <summary>
    /// Allow changing Ideology styles of blueprints
    /// In Blueprint_Build, it determins whether to show the gizmo to change styles based on:
    /// if (ModsConfig.IdeologyActive && Find.IdeoManager.classicMode && (thingDef = (this.def.entityDefToBuil...
    /// We want to simply remove Find.IdeoManager.classicMode
    /// </summary>
    [HarmonyPatch()]
    static class Patch_Blueprint_Build_Gizmo
    {
        static bool Prepare()
        {
            return ModsConfig.IdeologyActive && Settings.IsOptionSet("allowChangingStyles");
        }
        static MethodBase TargetMethod() //The target method is found using the custom logic defined here
        {
            // Copied from Deep Storage, so there might be newer, easier ways of doing this:
            // So IEnumerables suck.
            // There is a hidden IL class inside Blueprint_Build that GetGizmos uses for the IEnumerable
            //   In the IL it's listed as <GetGizmos>d__16, and the method we want to patch is from that
            //   class.  It's called MoveNext.  If we are lucky, we can do it ALL in one go.
            var method = typeof(RimWorld.Blueprint_Build).GetNestedType("<GetGizmos>d__16", AccessTools.all)
                    .GetMethod("MoveNext", AccessTools.all);
            if (method == null) Log.Error("LWM.MinorChanges: Transpiler could not find \"<GetGizmos>d__16\" :( ");
            return method;
            /* Another way to go about it, if we ever need it:
             *   (above HAS failed before (perhaps in earlier versions of Harmony?)
            var predicateClass = typeof(RimWorld.Building_Storage).GetNestedTypes(HarmonyLib.AccessTools.all)
               .FirstOrDefault(t => t.FullName.Contains("<GetGizmos>d__43"));
            var m = predicateClass.GetMethods(AccessTools.all)
                                 .FirstOrDefault(t => t.Name.Contains("MoveNext"));
              */

        }
        /// <summary>
        /// Remove && Find.IdeoManager.classicMode
        /// Directly remove these IL code:
        ///    IL_011c: call class RimWorld.IdeoManager Verse.Find::get_IdeoManager()
        ///    IL_0121: ldfld bool RimWorld.IdeoManager::classicMode
        ///    IL_0126: brfalse IL_0249
        /// </summary>
        /// <returns>The transpile.</returns>
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructionsEnumerable)
        {
            var code = instructionsEnumerable.ToList();
            // Find the IL code references we want:
            var get_IdeoManager = typeof(Verse.Find).GetMethod("get_IdeoManager");
            var classicMode = typeof(RimWorld.IdeoManager).GetField("classicMode");
            if (get_IdeoManager == null || classicMode == null)
            {
                Log.Error("LWM.MinorChanges: could not find get_IdeoManager or classicMode :(");
                foreach (var c in instructionsEnumerable) yield return c;
                yield break;
            }
            for(int i=0; i<code.Count; i++)
            {
                if (code[i].opcode == OpCodes.Call && code[i].OperandIs(get_IdeoManager)
                    && code[i+1].opcode == OpCodes.Ldfld && code[i+1].OperandIs(classicMode)
                    && code[i+2].opcode == OpCodes.Brfalse)
                {
                    // just skip right on over those three
                    for (int j=i+3; j<code.Count;j++)
                        yield return code[j];
                    yield break;
                }
                yield return code[i];
            }
            Log.Error("LWM.MinorChanges: transpile for Blueprint_Build failed :(");
            yield break;
        } // end transpile
    }
}
