using System;
using RimWorld;
using Verse;

namespace LWM.MinorChanges
{
  /* An extension that provides a bit more information for the label than just "def.label" - 
   * sometimes it's very useful to know how long before the Destroy kicks in!
   * For example: Solar Pinhole
   * (patched in via XML: SolarPinholesAndThingsThatBurnOutSaySo)  
   */
  public class CompDestroyAfterDelayWithLabel : CompDestroyAfterDelay
  {
    public override string TransformLabel(string label)
    {
      int numTicks = this.spawnTick + this.Props.delayTicks - Find.TickManager.TicksGame;
      if (numTicks < 0) numTicks = 0;
      return "LWMMC_TimeLeftLabel".Translate(label, numTicks.ToStringTicksToPeriod(true, // allow seconds?
                true, // f-short form?
                true,  // canUseDecimals?
                true,  // allowYears?
                true  // f-canUseDecimalsShortForm?
                ));
    }
  }
}

