using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using System.Diagnostics;

namespace LWM.MinorChanges
{
    public class Settings : ModSettings
    {
        public override void ExposeData() {
            Scribe_Values.Look(ref smelterIsHot, "smelterIsHot", true);
            Scribe_Values.Look(ref bigComputersAreHot, "bigComputersAreHot", true);

            Scribe_Values.Look(ref applyDrugDefaults, "applyDrugDefaults", true);

            Scribe_Values.Look(ref betterSpots, "betterSpots", true);
        }

        public void DoSettingsWindowContents(Rect inRect) {
            Rect rectWeCanSee=inRect.ContractedBy(10f);
            rectWeCanSee.height-=100f; // "close" button
            bool scrollBarVisible = totalContentHeight > rectWeCanSee.height;
            Rect rectThatHasEverything=new Rect(0f,0f,rectWeCanSee.width-
                                                (scrollBarVisible ? ScrollBarWidthMargin : 0),totalContentHeight);
            Widgets.BeginScrollView(rectWeCanSee, ref scrollPosition, rectThatHasEverything);
            float curY=0f;
            Rect r=new Rect(0,curY,rectThatHasEverything.width, LabelHeight);
            Widgets.Label(r, "IMPORTANT REMINDER: you MUST restart the game for changes to take effect");

            r=new Rect(0,curY,rectThatHasEverything.width, LabelHeight);
            Widgets.CheckboxLabeled(r, "Smelter gives off heat", ref smelterIsHot); // TODO: translate
            TooltipHandler.TipRegion(r, "It takes a LOT of heat to smelt steel, and insulation on the Rim is lacking...");
            curY+=LabelHeight+1f;

            r=new Rect(0,curY,rectThatHasEverything.width, LabelHeight);
            Widgets.CheckboxLabeled(r, "Computers get hot too", ref bigComputersAreHot); // TODO: translate
            TooltipHandler.TipRegion(r, "Server rooms need giant air conditioning systems.  Are computers on the Rim somehow super efficient?  Ha.  High Tech Research Benches produce heat.");
            curY+=LabelHeight+1f;

            r=new Rect(0,curY,rectThatHasEverything.width, LabelHeight);
            Widgets.CheckboxLabeled(r, "Penoxycyline defaults to 'take every 5 days'", ref applyDrugDefaults); // TODO: translate
            TooltipHandler.TipRegion(r, "If there are other drugs that should have this sort of default, let me know?");
            curY+=LabelHeight+1f;

            r=new Rect(0,curY,rectThatHasEverything.width, LabelHeight);
            Widgets.CheckboxLabeled(r, "Better 'Spots'", ref betterSpots); // TODO: translate
            TooltipHandler.TipRegion(r, "Lets you place spots pretty much anywhere.\nWant to get married in a cornfield?  Sure.  In the river?  Sure.  In the mud?  Sure.  In the lava?  Sure....okay, if you pawns cannot get there, then no.  This includes inside walls.\nSpots affected:\n  Marriage Spot\n  Party Spot\n  Caravan Packing Spot\n  Trading Spot (any mod that uses the defName TradingSpot)");
            curY+=LabelHeight+1f;

            Widgets.EndScrollView();
            totalContentHeight=curY+50f;
        }
        private static Vector2 scrollPosition=new Vector2(0f,0f);
        private static float totalContentHeight=1000f;
        private const float TopAreaHeight = 40f;
        private const float TopButtonHeight = 35f;
        private const float TopButtonWidth = 150f;
        private const float ScrollBarWidthMargin = 18f;
        private const float LabelHeight=22f;

        // Grab a given setting given its string name:
        //   (only allow boolean results, eh?)
        public static bool IsOptionSet(string name) {
            //var v = typeof(LWM.MinorChanges.Settings).GetField(name);
            var v = typeof(LWM.MinorChanges.Settings).GetField(name, System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.GetField|System.Reflection.BindingFlags.Instance);
            if (v==null) {
                Log.Error("LWM.MinorChanges: option \""+name+"\" is not a valid Settings variable. Failing.");
                return false;
            }
            if (v.FieldType != typeof(bool)) {
                Log.Error("LWM.MinorChanges: option \""+name+"\" is not a valid Settings boolean. Failing.");
                return false;
            }
            //return (bool)v.GetValue(null); // only use static, so null
            return (bool)v.GetValue(LoadedModManager.GetMod<MinorChangesMod>().GetSettings<Settings>());
        }

        bool smelterIsHot=true;
        bool bigComputersAreHot=true;

        public bool applyDrugDefaults=true;

        bool betterSpots=true;
    }
}
