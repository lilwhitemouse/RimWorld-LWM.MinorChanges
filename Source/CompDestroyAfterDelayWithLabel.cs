using System;
using RimWorld;
using Verse;

namespace LWM.MinorChanges
{
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

