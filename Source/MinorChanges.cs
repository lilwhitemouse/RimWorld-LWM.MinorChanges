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
            Log.Message("LWM.MinorChanges Version 1.4.0.4: initiating first transpile. Enjoy!");
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
        // Nifty! Won't even be compiled into assembly if not DEBUG
        [Conditional("DEBUG")]
        internal static void Mess(string s)
        {
            Verse.Log.Message("MinorChanges: " + s);
        }

        [Conditional("DEBUG")]
        internal static void Warn(string s)
        {
            Verse.Log.Warning("MinorChanges: " + s);
        }

        [Conditional("DEBUG")]
        internal static void Err(string s)
        {
            Verse.Log.Error("MinorChanges: " + s);
        }
    }
}
