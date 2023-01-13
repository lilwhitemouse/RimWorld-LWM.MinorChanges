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

// A little Patch Operation that lets me patch things based on a mod setting
namespace LWM.MinorChanges
{
  public class PatchOpLWMMC : PatchOperationSequence
  {
    protected override bool ApplyWorker(System.Xml.XmlDocument xml) {
      if (optionKey==null || optionKey=="") {
        Log.Error("LWM.MinorChanges: no patch option key!\n"+xml);
        return false;
      }
      if (Settings.IsOptionSet(optionKey)) {
        return base.ApplyWorker(xml);
      }
      return true;
    }

    private string optionKey="";
  }
}
