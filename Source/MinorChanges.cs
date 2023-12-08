using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using System.Diagnostics;

namespace LWM.MinorChanges
{
    [StaticConstructorOnStartup]
    public class MinorChangesModStartup {
        static MinorChangesModStartup() {
            Settings.SanityCheck();
            //harmony = new HarmonyLib.Harmony("net.littlewhitemouse.RimWorld.MinorChanges");
            MinorChangesMod.harmony.PatchAll();
        }
    }
    public class MinorChangesMod : Verse.Mod {
        public static HarmonyLib.Harmony harmony;

        public MinorChangesMod(ModContentPack content) : base(content) {
            harmony = new HarmonyLib.Harmony("net.littlewhitemouse.RimWorld.MinorChanges");
            var orig = Patch_ThingDefGenerator_Neurotrainer_Label.TargetMethod();
            if (orig != null)
            {
                harmony.Patch(orig, null, null,
                        // transpiler:
                        new HarmonyMethod(typeof(Patch_ThingDefGenerator_Neurotrainer_Label)
                                             .GetMethod("Transpiler", HarmonyLib.AccessTools.all)));
            }
        }
        public override string SettingsCategory() => "LWM_Minor_Changes".Translate();

        public override void DoSettingsWindowContents(Rect inRect) {
            GetSettings<Settings>().DoSettingsWindowContents(inRect);
        }
    }
    internal static class Debug
    {
        [Conditional("DEBUG")]
        internal static void Log(string s)
        {
            Verse.Log.Message(s);
        }

        [Conditional("DEBUG")]
        internal static void Warning(string s)
        {
            Verse.Log.Warning(s);
        }

        [Conditional("DEBUG")]
        internal static void Error(string s)
        {
            Verse.Log.Error(s);
        }
    }
}
