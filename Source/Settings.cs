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
        /* This is set up with a bunch of "keys" that are basically boolean variables.
         * Dealing with actual variables was a pain - I had to do every step for every
         * new setting I added. Boring! Now adding a new setting involves three things
         *   1. Put the key name in the list (e.g., "newSettingName")
         *   2. Create language keys for the setting: (<LWMMCnewSettingName/> and <LWMMCnewSettingNameDesc/>)
         *   3. Use the key
         *        in the code via `if (Settings.IsOptionSet("newSettingName"))...`
         *        in the XML patching via   <Operation Class="LWM.MinorChanges.PatchOpLWMMC"><!--like Sequence-->
                                               <optionKey>newSettingName</optionKey>
         */

        readonly static string[] ListOfOptionalSettings = // Default to false
        {
            "easierCasting",
            "bloodfeedOnPeopleWhoWant",
            "differentPollutionColor",
            "smelterIsHot",
            "bigComputersAreHot",
            "easierConvert",
            "labelPsycastLevels",
            "geoPlantWalkable",
            "cheaperFlagstone",
            "beSilly"
        };
        readonly static string[] ListOfProbablyYes =      // Default to true
        {
            "allowMultiUnloading",
            "selectNextAnimal",
            "betterPinholes",
            "applyDrugDefaults",
            "fastAnimalSleepingSpots",
            "cutTreesLikePlants",
            "showHowLongBiosculpting",
            "doNotBreastfeedInBathrooms",
            "showMeditationTypesWhenAssigningThrones",
            "allowChangingStyles",
            "pollutionPumpsShowPollutionLeft"
        };
        readonly static string[] ListOfNotQuiteWorkingRight = // Default to False and also get warning in settings
        {
            "betterSpots",
            //"fixDeathrestChambersAreBedrooms", // don't even try yet
        };
        Dictionary<string, bool> OptionalSettings = ListOfOptionalSettings.ToDictionary(k=>k, k => false);
        Dictionary<string, bool> ProbablyYes = ListOfProbablyYes.ToDictionary(k => k, k => true);
        Dictionary<string, bool> NotQuiteWorkingRight = ListOfNotQuiteWorkingRight.ToDictionary(k => k, v => false);
        /* <rant> Seriously, C#, what the heck?? I can't explictly write a dictionary with elements like this?
           Dictionary<string, bool> OptionalSettings = new Dictionary<string, bool>()
           {
               "easierCasting" => false, // WTF? We can't do this??
               { "easierCasting", false }, // We have to do THIS??
           };
           Do better, C#. Do better.
           </rant>*/
        // Old way of doing this:
        // To add a setting, need 4 things:
        //   save it here
        //   add the variable at the bottom
        //   put another line in DoSettignsWindows
        //   add another language key
        public override void ExposeData() {
            ExposeDictionary(OptionalSettings, false);
            ExposeDictionary(ProbablyYes, true);
            ExposeDictionary(NotQuiteWorkingRight, false);

            // "differentPollutionColor" :
            if ((OptionalSettings.TryGetValue("differentPollutionColor", out bool x) && x) ||
                (ProbablyYes.TryGetValue("differentPollutionColor", out x) && x))
            {
                ExposeColor("pollution", ref Patch_PollutionGrid_Color.ourColor, Color.red);
            }
        }
        void ExposeDictionary(Dictionary<string, bool> dict, bool defaultVal)
        {
            foreach (var key in dict.Keys.ToList())
            {
                bool tmpVal = dict[key];
                Scribe_Values.Look(ref tmpVal, key, defaultVal);
                dict[key] = tmpVal;
            }
        }
        void ExposeColor(string whichColor, ref Color color, Color defaultColor)
        {
            string saveString = ColorToSave(color);
            Scribe_Values.Look(ref saveString, whichColor + "Color", ColorToSave(defaultColor));
            color = SaveToColor(saveString);
        }
        string ColorToSave(Color color)
        {
            string v = Math.Round(color.r * 100F).ToString() + ":" + Math.Round(color.g * 100).ToString()
                      + ":" + Math.Round(color.b * 100).ToString() + ":" + Math.Round(color.a * 100).ToString();
            //Log.Message("LWM.MinorChanges: Changing color " + color + " into [" + v + "]");
            return v;
        }
        Color SaveToColor(string s)
        {
            string[] bits = s.Split(':');
            //Log.Message("LWM.MinorChanges: Splitting [" + s + "] into " + new Color(((float)int.Parse(bits[0])) / 100F, ((float)int.Parse(bits[1])) / 100F, ((float)int.Parse(bits[2])) / 100F, ((float)int.Parse(bits[3])) / 100F));
            return new Color(((float)int.Parse(bits[0])) / 100F, ((float)int.Parse(bits[1])) / 100F,
                              ((float)int.Parse(bits[2])) / 100F, ((float)int.Parse(bits[3])) / 100F);
        }

        public void DoSettingsWindowContents(Rect inRect) {
            Rect rectWeCanSee=inRect.ContractedBy(10f);
            rectWeCanSee.height-=100f; // "close" button
            bool scrollBarVisible = totalContentHeight > rectWeCanSee.height;
            Rect rectThatHasEverything=new Rect(0f,0f,rectWeCanSee.width-
                                                (scrollBarVisible ? ScrollBarWidthMargin : 0),totalContentHeight);
            Widgets.BeginScrollView(rectWeCanSee, ref scrollPosition, rectThatHasEverything);
            float curY=0f;
            Rect tmpRect=new Rect(0,curY,rectThatHasEverything.width, LabelHeight);
            Widgets.Label(tmpRect, "LWMMCsettingsWarning".Translate());
            curY+=LabelHeight+3f;
            Widgets.DrawLineHorizontal(10, curY + 7, rectThatHasEverything.width - 10);
            curY += 15;

            MakeDictBoolButtons(ref curY, rectThatHasEverything.width, OptionalSettings);

            Widgets.DrawLineHorizontal(10, curY + 7, rectThatHasEverything.width - 10);
            curY += 15;

            Widgets.Label(new Rect(0, curY, rectThatHasEverything.width, LabelHeight), 
                                   "LWMMCsettingsThatDefaultToYes".Translate());
            curY += LabelHeight + 3f;
            MakeDictBoolButtons(ref curY, rectThatHasEverything.width, ProbablyYes);


            Widgets.DrawLineHorizontal(10, curY + 7, rectThatHasEverything.width - 10);
            curY += 15;
            if (NotQuiteWorkingRight.Count > 0)
            {
                Widgets.Label(new Rect(0, curY, rectThatHasEverything.width, LabelHeight),
                     "LWMMCsettingsThatMayNotWork".Translate());
                curY += LabelHeight + 3f;
                MakeDictBoolButtons(ref curY, rectThatHasEverything.width, NotQuiteWorkingRight);
            }

            Widgets.DrawLineHorizontal(10, curY + 7, rectThatHasEverything.width - 10);
            curY += 15;

            ////////////////////////////// Color /////////////////////////
            if (true || IsOptionSet("pollutionPumpsShowPollutionLeft"))
            {
                Widgets.Label(new Rect(0, curY, rectThatHasEverything.width, LabelHeight), "LWMMCwhatColorPollution".Translate());
                curY += LabelHeight;

                DrawColorOptions(ref curY, rectThatHasEverything.width, ref Patch_PollutionGrid_Color.ourColor,
                    delegate()
                    {
                        foreach (var map in Find.Maps)
                        {
                            if (map != null)
                            {
                                typeof(PollutionGrid)
                                  .GetField("drawerInt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                  .SetValue(map.pollutionGrid, null);
                            }
                        }
                    });

                /*
                string buffer;
                float r = Patch_PollutionGrid_Color.ourColor.r;
                float g = Patch_PollutionGrid_Color.ourColor.g;
                float b = Patch_PollutionGrid_Color.ourColor.b;
                float a = Patch_PollutionGrid_Color.ourColor.a;
                Log.Message("R is " + r);
                buffer = (r*100).ToString();
                Widgets.TextFieldPercent(new Rect(50f, curY, 250f, LabelHeight), ref r, ref textBuffer, 0f, 1f);
                curY += LabelHeight;
                buffer = g.ToString();
                Widgets.TextFieldPercent(new Rect(50f, curY, 250f, LabelHeight), ref g, ref textBuffer, 0, 1);
                curY += LabelHeight;
                textBuffer = ((int)(b*100)).ToString();
                Widgets.TextFieldPercent(new Rect(50f, curY, 250f, LabelHeight), ref b, ref textBuffer, 0, 1);
                curY += LabelHeight;
                buffer = a.ToString();
                Widgets.TextFieldPercent(new Rect(50f, curY, 250f, LabelHeight), ref a, ref textBuffer, 0, 1);
                curY += LabelHeight;
                Patch_PollutionGrid_Color.ourColor = new Color(r, g, b, a);
                Log.Warning("Color is now " + Patch_PollutionGrid_Color.ourColor);
                var map = Find.CurrentMap;
                if (map != null)
                {
                    typeof(PollutionGrid)
                      .GetField("drawerInt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                      .SetValue(map.pollutionGrid, null);
                }
                */

                Widgets.DrawLineHorizontal(10, curY + 7, rectThatHasEverything.width - 10);
                curY += 15;
            }

            // For RealRuins
            MakeButton(ref curY, rectThatHasEverything.width,
                "LWMMCNukeFactionData", () =>
                {
                    var map = Find.CurrentMap;
                    if (map != null)
                    {
                        foreach (var c in map.AllCells)
                        {
                            foreach (var t in c.GetThingList(map))
                            {
                                if (t.def?.IsDoor == true) // .IsEdiface() is also a possible choice, but I think this better
                                {
                                    if (t.def.CanHaveFaction && (t.thingIDNumber % 8 != 1)) t.SetFaction(null);
                                }
                            }
                        }
                    }
                });
#if false // Fixed by mlie. Well, this isn't needed anymore, anyway
            // For Nature's Pretty Sweet
            MakeButton(ref curY, rectThatHasEverything.width,
                "LWMMCNukeLavaFieldsSteamVents", () =>
                {
                    var map = Find.CurrentMap;
                    if (map != null)
                    {
                        foreach (var c in map.AllCells)
                        {
                            foreach (var t in c.GetThingList(map))
                            {
                                if (t.def.defName == "TKKN_SteamVent")
                                {
                                    t.Destroy();
                                    break;
                                }
                            }
                        }
                    }
                });
#endif
            Widgets.EndScrollView();

            totalContentHeight=curY+50f;
        }

        static void DrawColorOptions(ref float curY, float width, ref Color color, Action onChange)
        {
            DrawColorFragment(ref curY, width, ref color.r, "Red", onChange);
            DrawColorFragment(ref curY, width, ref color.g, "Green", onChange);
            DrawColorFragment(ref curY, width, ref color.b, "Blue", onChange);
            DrawColorFragment(ref curY, width, ref color.a, "LWMMCalpha", onChange);
        }
        static void DrawColorFragment(ref float curY, float width, ref float colorFragment, string label, Action onChange)
        {
            float sliderValue = colorFragment;

            // NOTE: This will break for RimWorld v1.5 - remove the "_NewTemp" and it should work fine!
            //       ....I know of no way to make this future safe >_<
            sliderValue = Widgets.HorizontalSlider_NewTemp(new Rect(50f, curY, width - 100f, LabelHeight), colorFragment, 0f, 1f, false, null, label.Translate(), null, 0.01F);

            //Widgets.HorizontalSlider(new Rect(50f, curY, width - 100f, LabelHeight), ref sliderValue, new FloatRange(0, 1), label);
            curY += LabelHeight;
            if (sliderValue != colorFragment)
            {
                //Log.Message("Converting " + label + " from [" + colorFragment + "] to [" + sliderValue + "]");
                colorFragment = sliderValue;
                onChange();
            }
            return;
        }
        private static Vector2 scrollPosition=new Vector2(0f,0f);
        private static float totalContentHeight=1000f;
        private const float TopAreaHeight = 40f;
        private const float TopButtonHeight = 35f;
        private const float TopButtonWidth = 150f;
        private const float ScrollBarWidthMargin = 18f;
        private const float LabelHeight=22f;

        void MakeDictBoolButtons(ref float curY, float width, Dictionary<string, bool> dict)
        {
            foreach (var k in dict.Keys.ToList()) {
                bool tmpVal = dict[k];
                MakeBoolButton(ref curY, width, "LWMMC" + k, ref tmpVal);
                if (tmpVal != dict[k]) dict[k] = tmpVal;
            }
        }

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

        void MakeButton(ref float curY, float width, string labelKey, Action action)
        {
            Rect r = new Rect(10, curY, width - 20, LabelHeight);
            if (Widgets.ButtonText(r, labelKey.Translate(), true, false))
            {
                action();
            }
            TooltipHandler.TipRegion(r, (labelKey + "Desc").Translate());
            if (Mouse.IsOver(r)) Widgets.DrawHighlight(r);
            curY += LabelHeight + 1f;
        }


        // Grab a given setting given its string name:
        //   (only allow boolean results, eh?)
        public static bool IsOptionSet(string name) {
            var s = LoadedModManager.GetMod<MinorChangesMod>().GetSettings<Settings>();
            bool x;
            if (s.OptionalSettings.TryGetValue(name, out x)) return x;
            if (s.ProbablyYes.TryGetValue(name, out x)) return x;
            if (s.NotQuiteWorkingRight.TryGetValue(name, out x)) return x;
            /* Fancy shenanigans to get setting value from bool value....
             * but honestly, it's probably better to do a Dict that has all the values stored           
            //var v = typeof(LWM.MinorChanges.Settings).GetField(name);
            var v = typeof(LWM.MinorChanges.Settings).GetField(name, System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public | // Heh, can't forget this, right?
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
            return (bool)v.GetValue(s);
            */
            // Note to self:
            Log.Error("LWM.MinorChanges: LWM, you lazy dog, put in an actual setting for " + name);
            return true;
        }
        public static void ForceSetting(string key, bool val, string message = null)
        {
            foreach (var dict in Dicts())
            {
                if (dict.ContainsKey(key))
                {
                    dict[key] = val;
                    if (message == null) Log.Message("LWM.MinorChanges: Setting directly changed for some reason. This is probably okay. " + key + " set to " + val);
                    else Log.Message("LWM.MinorChanges: Settings changed: " + message);
                    // Now, save the changed setting:
                    LoadedModManager.GetMod<MinorChangesMod>().GetSettings<Settings>().Write();
                }
            }
            Log.Warning("Tried to change setting " + key + " to " + val + " but cannot find setting?!");
            return;
        }
        static IEnumerable<Dictionary<string, bool>> Dicts()
        {
            var s = LoadedModManager.GetMod<MinorChangesMod>().GetSettings<Settings>();
            yield return s.OptionalSettings;
            yield return s.ProbablyYes;
            yield return s.NotQuiteWorkingRight;
        }

        [Conditional("DEBUG")]
        public static void SanityCheck()
        {
            if (LanguageDatabase.activeLanguage.DisplayName.ToLower() == "english")
            {
                foreach (var s in Dicts().SelectMany(d => d.Keys).Select(s=>"LWMMC"+s))
                {
                    if (!s.CanTranslate()) Log.Error("Cannot find translation string for " + s + ": "+s.Translate());
                    if (!(s + "Desc").CanTranslate()) Log.Error("Cannot find translation string for " + s+"Desc");
                }
            }
        }
        //public bool beSilly=false; // well, slightly silly anyway

        private static string textBuffer;
    }

}
