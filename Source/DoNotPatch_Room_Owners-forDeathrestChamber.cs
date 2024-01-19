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
#if false // skip this for now, not really working :/

/*************************************************************************
 * Currently, Sanguophages in the Empire will get a -6 No Personal Bedroom
 * debuff if they have a (personal) Deathrest Chamber. I have reported the
 * oversight to Devs... but there are several parts to the issue and maybe
 * it's not considered an issue anyway. Maybe in 'nilla it doesn't come up
 * enough to make a difference or maybe who knows.
 * But for me, it's a problem because the vampires keep trying to sleep in
 * their deathrest rooms after waking up and getting the -6 penalty, which
 * ...well, let's just say no one wants an angry vampire.
 * Part of the probelem is in Verse.Room's Owner's getter, which only says
 * someone is an owner of a room if that room has a RoofRoleDefOf Bedroom,
 * Barracks, etc.  But not DeathrestChamber....
 * .....
 * maybe because don't wan to stack fancy deathrest chamber with fancy bedroom?
 * maybe because .....???
 * BUT ALSO: can't have biotech buildings in empire rooms.
 * BUT ALSO: how do we reconcile with wanting to share a bedroom? How do
 * we ..... argh.
 * We can fix room owner:
 * Of course, we have to Transpile.
 * Of course.
 * Of course, it's an IEnumerable getter.  Fun.
 ************************************************************************/
namespace LWM.MinorChanges
{
    [HarmonyPatch()]
    class Patch_BugFix_Room_Owners_forDeathrestChamber
    {
        static RoomRoleDef deathrestChamber;
        static bool Prepare()
        {
            return ModsConfig.BiotechActive && Settings.IsOptionSet("fixDeathrestChambersAreBedrooms");
        }
        static MethodBase TargetMethod()
        {
            return typeof(Verse.Room).GetNestedType("<get_Owners>d__59", AccessTools.all).GetMethod("MoveNext", AccessTools.all);
        }
        // to remove the first Standable test in its entirety:
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
                                                       ILGenerator generator)
        {
            List<CodeInstruction> code = instructions.ToList();
            deathrestChamber = DefDatabase<RoomRoleDef>.GetNamed("DeathrestChamber");
            var get_Role = typeof(Verse.Room).GetMethod("get_Role", AccessTools.all);
            // First, check to see if it's been fixed?
            for (int i=0; i<code.Count; i++)
            {
                if (code[i].opcode==OpCodes.Call && code[i].OperandIs(get_Role) && code[i+1].opcode == OpCodes.Ldsfld &&
                    code[i+1].operand.ToString() == "Verse.RoomRoleDef DeathrestChamber")
                {
                    Settings.ForceSetting("fixDeathrestChambersAreBedrooms", false, "Deathrest Chambers as Bedrooms Fix not needed: This is OKAY!");
                    for (int j = 0; j < code.Count; j++) yield return code[j];
                    yield break;
                }
            }

            bool found = false;
            for (int i = 0; i < code.Count; i++)
            {
                //Log.Message("------>" + code[i]);
                yield return code[i];
                if (!found &&
                    code[i].opcode == OpCodes.Call && code[i].OperandIs(get_Role) &&
                    code[i + 1].opcode == OpCodes.Ldsfld && code[i + 1].operand.ToString() == "Verse.RoomRoleDef PrisonCell")
                {
                    // Let's insert deathrest chamber before prison cell
                    //Log.Message("++++++> OUR CALL");
                    yield return new CodeInstruction(OpCodes.Ldsfld, typeof(Patch_BugFix_Room_Owners_forDeathrestChamber)
                                      .GetField("deathrestChamber", BindingFlags.Static | BindingFlags.NonPublic));
                    //Log.Message("++++++>" + code[i + 2]);
                    yield return code[i + 2]; // the test for equality
                    found = true;
                    i--; // go back to call get_Role again
                    i--; // go back to loading the room variable again
                    // Now, when we go forward, we'll be at i-1, which will load the room, then we'll call get_Role again,
                    //   then the PrisonCell will get called, then the comparison will happen again, and all will be good.
                }
            }
            if (!found) { Log.Warning("LWM.MinorChanges: could not patch Room's get_Owner"); }
        }
    }
}
#endif
