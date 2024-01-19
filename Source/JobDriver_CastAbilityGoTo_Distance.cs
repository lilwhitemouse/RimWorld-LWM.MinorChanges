using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using System.Reflection;

namespace LWM.MinorChanges
{
    /**********************************
     * A JobDriver that lets one cast an ability on a location (not on a pawn or object)
     *   ^at a distance^! Mainly intended for Solar Pinhole, so the pawns could start a
     *   job where they walk to a good casting place and then cast and then are done w/
     *   their job, all without human(player) interaction!
     * 
     *  Note: Because the system is SO full of the expectation(limitation) that one is
     *        casting on a Thing, I deem it best to create a fake Thing located at our
     *        target casting position and use the vanilla GotoCastPosition to take the
     *        pawn where they can cast. The fake thing only needs to be in "existance"
     *        for the brief moment while the pathing calculation is done in the toil's
     *        initAction, and then it's forgotten about again, so there is no drain on
     *        the system. I hope.
     * 
     *  Note 2: There are still several places that need to be patched before RimWorld
     *          will allow us to actually *target* the desired location.
     * 
     *  Note 3: And of course the job needs to be adjusted in the AbilityDef. If there
     *          are 38974 things this would be useful for, I could update the database
     *          directly on system load, but for now it's much easier to patch all the
     *          wanted defs via XML PatchOperations
     * 
     *  Note 4: For the record, the vanilla behavior is dumb sometimes, but whatevs...
     */
    [StaticConstructorOnStartup]
    public class JobDriver_CastAbilityGoTo_Distance : JobDriver_CastAbilityGoTo
    {
        public static JobDef jobDef;
        static JobDriver_CastAbilityGoTo_Distance()
        {
            // Controlled by key "easierCasting" via existance of the necessary JobDef added by xml
            jobDef = DefDatabase<JobDef>.GetNamed("LWM_MC_CastAbilityGoTo_Distance", false);
            if (jobDef == null) {
                LWM.MinorChanges.Debug.Err("LWM, your CastAbility JobDefs are off! Are you all right?");
                return;
            }
            LWM.MinorChanges.Debug.Warn("Adding custom JobDefs to all ranged castabilities...");
            // So SolarPinhole and many many other casting abilities have a jobDef of null...
            //   This isn't a mistake; the job gets chosen elsewhere. If the ability has
            //   targetWorldCell, then Command_Ability calls the ability's QueueCastingJob, 
            //   which only seems to be used if it's a world targeting ability??
            //   Otherwise, the Targeter makes the job using JobDefOf.UseVerbOnThing, which
            //   uses JobDriver_CastVerbOnce.
            //   We don't want to f--- with world targeting abilities like Farskip (because...
            //   it doesn't work, and having your cornered pawns unable to farskip home can be
            //   a bit...aaaack!), so we remove those from our patching:
            List<string> defNames = new List<string>();
            foreach (var d in DefDatabase<AbilityDef>.AllDefsListForReading
                        .Where(d=>d.jobDef == null && d.targetWorldCell == false)
                        .Where(d=>d.verbProperties.range > 0f).Where(d=>d.hostile==false))
            {
                Debug.Mess("  --Patching " + d.defName);
                defNames.Add(d.defName);
                d.jobDef = jobDef;
                d.displayGizmoWhileUndrafted = true;
                d.disableGizmoWhileUndrafted = false;
            }
            Log.Message("LWM.MinorChanges: Better Ability Casting option has triggered for: " + String.Join(", ", defNames));
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil goToLocation = GoTowardsPositionUntilCanCast(TargetIndex.A, PathEndMode.Touch, this.job.ability);
            Toil castTheSpell = CastWithLog(TargetIndex.A, TargetIndex.A, false);
            Toil startWholeThing = InitiateCasting(TargetIndex.A, castTheSpell, this.job.ability);
            yield return startWholeThing;
            yield return goToLocation;
            yield return castTheSpell;
            yield break;
            //yield return Toils_Combat.CastVerb(TargetIndex.A, TargetIndex.A, false);
        }
        Toil InitiateCasting(TargetIndex ind, Toil toilIfReadyToCast, Ability ability)
        {
            Toil toil = Toils_Jump.JumpIf(toilIfReadyToCast,
                () => ability.verb.CanHitTargetFrom(this.pawn.Position, this.pawn.CurJob.GetTarget(ind)));
            toil.initAction = delegate ()
                {
                    this.pawn.pather.StopDead();
                    if (this.pawn.CurJob.GetTarget(ind).HasThing) this.FailOnDespawnedNullOrForbidden(ind);
                }
                + toil.initAction;
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }
        Toil GoTowardsPositionUntilCanCast(TargetIndex ind, PathEndMode peMode, Ability ability)
        {
            Toil toil = Toils_Goto.GotoCell(ind, peMode);
            IntVec3 previousPosition = IntVec3.Invalid;
            toil.initAction = delegate {
                Debug.Warn("CastAbilityGoTo_Distance: " + toil.actor + " is starting from " + toil.actor.Position);
                previousPosition = toil.actor.Position;
                if (this.pawn.CurJob.GetTarget(ind).HasThing) this.FailOnDespawnedNullOrForbidden(ind);
                //Log.Error("InitAction setting previousPosition to " + previousPosition);
            }+toil.initAction;
            toil.tickAction += delegate {
                if (toil.actor.Position != previousPosition)
                {
                    Debug.Mess("CastAbilityGoTo_Distance: " + toil.actor + " has moved to " + toil.actor.Position);
                    previousPosition = toil.actor.Position;
                    //Log.Message("Have moved to " + previousPosition);
                    // This won't work: if (ability.verb.CanHitTarget(toil.actor.CurJob.GetTarget(ind)))
                    //   because we've already patched CanHitTarget.  :facepalm:
                    // But this should be sufficient:
                    if ((((Verb)ability.verb) as Verb).CanHitTargetFrom(toil.actor.Position, toil.actor.CurJob.GetTarget(ind)))
                    {
                        Debug.Warn("CastAbilityGoTo_Distance: " + toil.actor + " can cast " + ability.def.defName);
                        if (toil.actor.Position.Standable(toil.actor.Map))
                        {
                            Debug.Mess("Finishing pathing to stand on the cell");
                            toil.actor.pather.StartPath(toil.actor.Position, PathEndMode.OnCell);
                        }
                        else
                        {
                            Debug.Mess("Starting casting!");
                            toil.actor.jobs.curDriver.ReadyForNextToil();
                        }
                    }
                }
            };
            return toil;
        }
        // This exists just so I can get debugging info if needed
        Toil CastWithLog(TargetIndex ind1, TargetIndex ind2, bool someBool)
        {
            var t = Toils_Combat.CastVerb(ind1, ind2, someBool);
            t.initAction = (Action)delegate {
                Debug.Warn("CastAbilityGotoDistance: " + t.actor + " is starting to cast "+
                              ((t.actor.CurJob.verbToUse is Verb_CastPsycast vcp) ? vcp.ability.def.defName : "something!"));            
            }+t.initAction;
            return t;
        }
//Worse approaches:
#if false
        Toil XGoTowardsPositionUntilCanCast(TargetIndex ind, PathEndMode peMode, Ability ability)
        {
            Toil toil = ToilMaker.MakeToil("GoTowardsPositionUntilCanCast");
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;
                Log.Message("Casting pawn " + actor + " is starting to path to " + actor.jobs.curJob.GetTarget(ind));
                actor.pather.StartPath(actor.jobs.curJob.GetTarget(ind), peMode);

            };
            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            return toil;
        }
#endif
#if false // Dear gods of the ether, vanilla pathfinding to cast is actually really dumb. REALLY REALLY dumb.
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            Toil toil = Toils_Combat.GotoCastPosition(TargetIndex.A);
            // Tack on our own initAction before GotoCastPosition to make GotoCastPosition work!
            //   (It needs an object for the initAction targetting - so...we make one!)
            var origAction = toil.initAction;
            toil.initAction = delegate ()
            {
                // Make up a new fake object.  Since we're trying to cacst a pinhole, let's use the target of what we 
                //   want to cast!  I know there are other modded pinholes out there (arctic?) so let's be general:
                var pretendDef = toil.actor.CurJob.ability?.def?.comps?.OfType<CompProperties_AbilitySpawn>()
                       .FirstOrDefault()?.thingDef;
                if (pretendDef == null || pretendDef.category == ThingCategory.Item) // ...lets not use actual items, just in case
                {
                    pretendDef = DefDatabase<ThingDef>.GetNamed("SolarPinhole", false);
                    if (pretendDef == null)
                    {
                        // Uh...first etherial?
                        pretendDef = DefDatabase<ThingDef>.AllDefs.FirstOrFallback(td => td.category == ThingCategory.Ethereal, null);
                        if (pretendDef == null) DefDatabase<ThingDef>.GetRandom();
                        if (pretendDef == null) // def database completely empty? o.?
                        {
                            toil.actor.jobs.EndCurrentJob(JobCondition.Errored);
                            return;
                        }
                    }
                }
                Thing fakeThing = new Thing
                {
                    // Fake things need a def or else checking to see if you can hit them throws a hissy fit
                    //   (because it can't find the size)
                    def = pretendDef
                };
                // Set distance directly, so anyone patching Position doesn't get a weird fake object to deal with
                typeof(Thing).GetField("positionInt", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(fakeThing, toil.actor.jobs.curJob.GetTarget(TargetIndex.A).Cell);
                // Needs a map, which makes it pretend to be spawned
                typeof(Thing).GetField("mapIndexOrState", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(fakeThing,
                    typeof(Thing).GetField("mapIndexOrState", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(toil.actor));
                // Now, make this made up fake object our target!
                toil.actor.CurJob.SetTarget(TargetIndex.A, fakeThing);
                origAction(); // From GotoCastPosion
            };
            yield return toil; // ...don't forget to return it :o
            yield return Toils_Combat.CastVerb(TargetIndex.A, TargetIndex.B, false);
            yield break;
        }
#endif
        // Original thought to make this work:
        //   Turns out there are too many tricky irritating places the engine needs a real Thing to make it
        //   easy to work around. I think the temporary fake Thing is the way to go
#if false
        private static Toil GotoUntilCanUseAbility(TargetIndex ind, PathEndMode peMode, Ability ability)
        {
            Toil toil = ToilMaker.MakeToil("GotoCastOnGroundPosition");
            toil.initAction = delegate ()
            {
                float maxRangeFactor = 1f;
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;
                Thing thing;
                thing = actor.Map.thingGrid.ThingAt<Thing>(curJob.GetTarget(ind).Cell);
                if (thing == null)
                {
                    Log.Message("Making up thing");
                    thing = new Thing();
                    typeof(Thing).GetField("mapIndexOrState", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(thing,
                        typeof(Thing).GetField("mapIndexOrState", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(actor));
                    thing.def = DefDatabase<ThingDef>.GetNamed("SolarPinhole");
                    typeof(Thing).GetField("positionInt", BindingFlags.NonPublic | BindingFlags.Instance)
                        .SetValue(thing, curJob.GetTarget(ind).Cell);
                }
                else
                {
                    Log.Message("Using thing " + thing);
                }


                CastPositionRequest newReq = default(CastPositionRequest);
                newReq.caster = toil.actor;
//                newReq.targetLocation = curJob.GetTarget(ind).Cell; //dammit, needs actual Thing
                newReq.target = thing;
                newReq.verb = curJob.verbToUse;
                newReq.maxRangeFromTarget = Math.Max(curJob.verbToUse.verbProps.range * maxRangeFactor, 1.42f);
                newReq.wantCoverFromTarget = false;

                IntVec3 intVec;
                if (!CastPositionFinder.TryFindCastPosition(newReq, out intVec))
                {
                    Log.Error("Couldn't reach " + newReq.target.Position);
                    //Log.Error("Couldn't reach " + newReq.targetLocation);

                    toil.actor.jobs.EndCurrentJob(JobCondition.Incompletable, true, true);
                    return;
                }
                Log.Warning("Starting path to " + intVec);
                toil.actor.pather.StartPath(intVec, PathEndMode.OnCell);
            };
            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            return toil;
// Another approach was to just walk torwards the casting point and check every new cell to see if we'd reached a place
//   we could cast yet.  This probably would have worked fine, but the "fake Thing" behavior is closer to vanilla behavior
            toil.initAction = delegate ()
            {
                Pawn actor = toil.actor;
                Log.Message("Casting pawn " + actor + " is starting to path to " + actor.jobs.curJob.GetTarget(ind));
                actor.pather.StartPath(actor.jobs.curJob.GetTarget(ind), peMode);
            };
            //toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            IntVec3 currentLocation = toil.actor.Position;
            toil.tickAction = delegate {
                if (toil.actor.Position != currentLocation)
                {
                    currentLocation = toil.actor.Position;
                    if (ability.CanApplyOn(toil.actor.CurJob.GetTarget(ind)))
                    {
                        Log.Error("AND CAN SEE TARGET!");
                        toil.actor.jobs.curDriver.ReadyForNextToil();
                    }
                }
            };
            //toil.FailOnDespawnedOrNull(ind); //going to a place, durak
            //Log.Message("Have new toil! Starting with location "+currentLocation);
            return toil;
        }
#endif
    }
}
