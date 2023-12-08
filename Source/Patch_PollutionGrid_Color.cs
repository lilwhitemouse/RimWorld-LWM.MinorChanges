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
    /// A Harmony Transpiler to make the Pollution Overlay use a custom color instead of overwhelming red
    /// </summary>
    [HarmonyPatch(typeof(PollutionGrid), "CellBoolDrawerGetExtraColorInt")]
    static class Patch_PollutionGrid_Color
    {
        static bool Prepare()
        {
            return Settings.IsOptionSet("differentPollutionColor");
        }
        // Color set via settings
        public static Color ourColor = Color.red; // default

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructionsEnumerable)
        {
            foreach (var code in instructionsEnumerable)
            {
                if (code.opcode == OpCodes.Call && code.OperandIs(typeof(Color).GetMethod("get_red", AccessTools.all)))
                {
                    // if the code puts Color.red on the stack
                    var myC = new CodeInstruction(OpCodes.Ldsfld, typeof(Patch_PollutionGrid_Color).GetField("ourColor", AccessTools.all))
                    {
                        labels = code.labels
                    };
                    yield return myC;
                }
                else
                {
                    yield return code;
                }
            }
        }

        /* This was MUCh slower, at least with the Log call. I decided it was easier to just eliminate it entirely and transpile
         * It would run into pauses when it suddenly got called 350 times at once....       
        static UnityEngine.Color XXXPostfix(UnityEngine.Color __result)
        {
            if (__result == Color.red)
            {
                //Log.Message("Returning " + ourColor);
                //Returning RGBA(0.720, 0.551, 0.186, 0.386)
                return ourColor;
            }
            return __result;
        }
        */
    }
}
