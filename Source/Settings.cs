﻿using System;
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
        // To add a setting, need 4 things:
        //   save it here
        //   add the variable at the bottom
        //   put another line in DoSettignsWindows
        //   add another language key
        public override void ExposeData() {
            Scribe_Values.Look(ref smelterIsHot, "smelterIsHot", true);
            Scribe_Values.Look(ref bigComputersAreHot, "bigComputersAreHot", true);

            Scribe_Values.Look(ref applyDrugDefaults, "applyDrugDefaults", true);
            Scribe_Values.Look(ref allowMultiUnloading, "allowMultiUnloading", true);

            Scribe_Values.Look(ref betterSpots, "betterSpots", true);
            Scribe_Values.Look(ref selectNextAnimal, "selectNextAnimal", true);

            Scribe_Values.Look(ref geoPlantWalkable,"geoPlantWalkable", false);

            Scribe_Values.Look(ref beSilly, "beSilly", false);
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
            Widgets.Label(r, "LWMMCsettingsWarning".Translate());
            curY+=LabelHeight+3f;

            MakeBoolButton(ref curY, rectThatHasEverything.width,
                           "LWMMCbetterSpots", ref betterSpots);
            MakeBoolButton(ref curY, rectThatHasEverything.width,
                           "LWMMCselectNextAnimal", ref selectNextAnimal);
            MakeBoolButton(ref curY, rectThatHasEverything.width,
                           "LWMMCsmelterIsHot", ref smelterIsHot);
            MakeBoolButton(ref curY, rectThatHasEverything.width,
                           "LWMMCbigComputersAreHot", ref bigComputersAreHot);
            MakeBoolButton(ref curY, rectThatHasEverything.width,
                           "LWMMCapplyDrugDefaults", ref applyDrugDefaults);
            MakeBoolButton(ref curY, rectThatHasEverything.width,
                           "LWMMCallowMultiUnloading", ref allowMultiUnloading);
            MakeBoolButton(ref curY, rectThatHasEverything.width,
                           "LWMMCgeoPlantWalkable", ref geoPlantWalkable);

            Widgets.DrawLineHorizontal(10, curY+7, rectThatHasEverything.width-10);
            curY+=15;

            MakeBoolButton(ref curY, rectThatHasEverything.width,
                           "LWMMCbeSilly", ref beSilly);

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

        // Make the button/handle the setting change:
        void MakeBoolButton(ref float curY, float width,
                           string labelKey, // also has Desc key
                            ref bool setting) {
            Rect r=new Rect(0,curY,width, LabelHeight);
            Widgets.CheckboxLabeled(r, labelKey.Translate(), ref setting);
            TooltipHandler.TipRegion(r, (labelKey+"Desc").Translate());
            if (Mouse.IsOver(r)) Widgets.DrawHighlight(r);
            curY+=LabelHeight+1f;
        }


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
            //return (bool)v.GetValue(null); // use this line instead of the one below if you use static settings
            //    e.g., static bool smeltherIsHot=true; //etc
            return (bool)v.GetValue(LoadedModManager.GetMod<MinorChangesMod>().GetSettings<Settings>());
        }
        // Actual variables:
        //   public ones are ones C# needs to access
        //   private ones are ones only used for xml Patching.
        public bool selectNextAnimal=true;
        bool smelterIsHot=true;
        bool bigComputersAreHot=true;

        public bool applyDrugDefaults=true;
        public bool allowMultiUnloading=true;

        bool betterSpots=true;
        bool geoPlantWalkable=false;

        public bool beSilly=false; // well, slightly silly anyway
    }
}
