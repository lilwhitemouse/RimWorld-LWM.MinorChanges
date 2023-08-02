using System;
using RimWorld;
using Verse;

namespace LWM.MinorChanges
{
  public class CompProperties_DestroyAfterDelay_WithLabel : CompProperties_DestroyAfterDelay
  {
    public CompProperties_DestroyAfterDelay_WithLabel()
    {
      this.compClass = typeof(CompDestroyAfterDelayWithLabel);
    }
  }
}
