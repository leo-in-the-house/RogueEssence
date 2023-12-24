﻿using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence.Dev;

namespace RogueEssence.LevelGen
{
    /// <summary>
    /// A dungeon segment where each floor has its own map generator, with no gaps in between floors.
    /// Players attempting to enter a floor in this segment will be served by the generator corresponding to the floor number they requested.
    /// An error will occur if the requested floor is out of range.
    /// </summary>
    [Serializable]
    public class LayeredSegment : ZoneSegmentBase
    {
        //TODO: make this class generic:
        // LayeredSegment<T> where T : IFloorGen
        //Implementations in this project can use IProjectSegmentBase where the LayeredSegment uses IProjectFloorGen as a base
        //IProjectFloorGen will be implemented via a ProjectFloorGen that is like FloorGen but has the constraint of BaseMapGenContext
        [RankedList(0, true)]
        [Collection(0, true)]
        public List<IFloorGen> Floors;

        public override int FloorCount { get { return Floors.Count; } }
        public override IEnumerable<int> GetFloorIDs()
        {
            for (int ii = 0; ii < FloorCount; ii++)
                yield return ii;
        }


        //how do we get additional gen steps added to mapgens?
        //should we have the original mapgen create a runtime copy?
        //with this runtime copy being a list of IGenSteps?

        //problem: if we split gridfloorplan and listfloorplan into separate components
        //logic for adding special rooms will first need to check for grid floor plan
        //and then IF THAT FAILS, to check listfloorplan in order to function
        //in order to prevent adding two steps to the flow,
        //we must check if the first genstep will accept the mapcontext
        //this means that we must process the mapcontext of that floor AFTER IT IS CREATED
        //so for spreading classes, we need:
        //a class for the zone that can divvy up and choose which floors get the special step
        //a class for the mapgen, to check which type of step it needs to be given
        //and the actual gensteps to be placed in the genstep list

        //a prerequisite to this is that there MUST be a way for the gensteps,
        //at least in runtime, to include all forms of IGenStep, not only GenStep<T>

        //alternatively, zonepostprocs can hold state
        //when a new zone is introduced, a copy of the original zonepostprocs
        //are all added to the ZoneContext
        //when the time comes to create a new map,
        //first the gencontext is created for that floor
        //then it is passed into each of the zonecontext members
        //(the gensteps for the floor are currently in a separate list)
        //the zonecontext members will do their job, updating floor counts etc.
        //and they will alter the list of floorgen elements directly

        //how can we make MapGen not couple itself with outside sources,
        //yet allow itself to be modified by them?
        //1. create a runtime class that mapgen instantiates
        //with List<GenPriority<IGenStep>> defined instead of List<GenPriority<GenStep<T>>>
        //this class will actually generate its class
        //however, it needs to be able to give out its map context to be analyzed by contexts first
        //2. modify mapgen.  have it take in a ZoneGenContext
        //which will have its seed needed to generate
        //it will first create the instance of the context,
        //and set up a local List<GenPriority<IGenStep>> variable 
        //the zonecontext members include runtime zonepostprocs
        //that will appreciate a IGenContext + List<GenPriority<IGenStep>> being passed into them
        //then, gensteps will continue on as per usual
        //3. inherit from mapgen. do the above but make it overridden code
        //the base class will put the genstep execution code into its own function
        //a new method will be created that works like the above
        //this new method will need to go in a new IMapGen, however...

        //**is there a way to not alter the list of floorgen elements directly?
        //there doesn't have to.  there is precedent with battle flow priority

        //**in order to prevent searching for specific steps to put the new steps in front of or behind
        //you will need to establish a priority system
        //on the mapgen's list itself, as well as the list held in the zonecontext member
        //data devs makes up their own rules about the numberings

        public LayeredSegment() : base()
        {
            Floors = new List<IFloorGen>();
        }

        /// <summary>
        /// Get the map gen of the target floor ID.
        /// Throws an exception if it is a floor ID equal to or higher than FloorCount
        /// </summary>
        /// <param name="floor">Floor ID of the target floor</param>
        /// <returns></returns>
        public override IFloorGen GetMapGen(int floor)
        {
            if (floor >= 0 && floor < Floors.Count)
                return Floors[floor];
            else
                throw new Exception("Requested a map id out of range.");
        }
    }

    /// <summary>
    /// A dungeon segment where each floor has the same map generator.
    /// Players attempting to enter a floor in this segment will be served by the same generator no matter what.
    /// </summary>
    [Serializable]
    public class SingularSegment : ZoneSegmentBase
    {
        public IFloorGen BaseFloor;

        public int FloorSpan;
        public override int FloorCount { get { return FloorSpan; } }
        public override IEnumerable<int> GetFloorIDs()
        {
            for(int ii = 0; ii < FloorSpan; ii++)
                yield return ii;
        }


        public SingularSegment(int floors) : base()
        {
            FloorSpan = floors;
        }

        /// <summary>
        /// Get the map gen of the target floor ID.
        /// Throws an exception if it is a floor ID equal to or higher than FloorCount
        /// </summary>
        /// <param name="floor">Floor ID of the target floor</param>
        /// <returns></returns>
        public override IFloorGen GetMapGen(int floor)
        {
            //There is only one floor
            if (FloorSpan < 0 || floor < FloorSpan)
                return BaseFloor;
            else
                throw new Exception("Requested a map id out of range.");
        }
    }


    /// <summary>
    /// A dungeon segment where multiple floors can be mapped to the same map generator.
    /// Players attempting to enter a floor in this segment will be served by the generator corresponding to the floor range that covers the number they requested.
    /// An error will occur if the requested floor is out of range.
    /// </summary>
    [Serializable]
    public class RangeDictSegment : ZoneSegmentBase
    {
        [RangeBorder(0, true, true)]
        [Collection(0, true)]
        public RangeDict<IFloorGen> Floors;
        public override int FloorCount
        {
            get
            {
                int total = 0;
                foreach (IntRange range in Floors.EnumerateRanges())
                    total += range.Length;
                return total;
            }
        }

        public override IEnumerable<int> GetFloorIDs()
        {
            foreach (IntRange range in Floors.EnumerateRanges())
            {
                for(int ii = range.Min; ii < range.Max; ii++)
                    yield return ii;
            }
        }

        public RangeDictSegment() : base()
        {
            Floors = new RangeDict<IFloorGen>();
        }


        /// <summary>
        /// Get the map gen of the target floor ID.
        /// Throws an exception if it is a floor ID equal to or higher than FloorCount
        /// </summary>
        /// <param name="floor">Floor ID of the target floor</param>
        /// <returns></returns>
        public override IFloorGen GetMapGen(int floor)
        {
            if (Floors.ContainsItem(floor))
                return Floors[floor];
            else
                throw new Exception("Requested a map id out of range.");
        }
    }

    /// <summary>
    /// A dungeon segment where each floor has its own map generator, with gaps between floors allowed.
    /// Players attempting to enter a floor in this segment will be served by the generator corresponding to the floor number they requested.
    /// An error will occur if the requested floor does not have a generator.
    /// </summary>
    [Serializable]
    public class DictionarySegment : ZoneSegmentBase
    {
        [Collection(0, true)]
        public Dictionary<int, IFloorGen> Floors;
        public override int FloorCount { get { return Floors.Count; } }
        public override IEnumerable<int> GetFloorIDs()
        {
            foreach (int key in Floors.Keys)
                yield return key;
        }


        public DictionarySegment() : base()
        {
            Floors = new Dictionary<int, IFloorGen>();
        }

        /// <summary>
        /// Get the map gen of the target floor ID.
        /// Throws an exception if it is a floor ID equal to or higher than FloorCount
        /// </summary>
        /// <param name="floor">Floor ID of the target floor</param>
        /// <returns></returns>
        public override IFloorGen GetMapGen(int floor)
        {
            if (Floors.ContainsKey(floor))
                return Floors[floor];
            else
                throw new Exception("Requested a map id out of range.");
        }
    }


    [Serializable]
    public abstract class ZoneSegmentBase
    {
        public abstract int FloorCount { get; }
        public abstract IEnumerable<int> GetFloorIDs();

        /// <summary>
        /// Map Generation steps that apply to multiple floors at a time.
        /// </summary>
        [Collection(0, true)]
        public List<ZoneStep> ZoneSteps;
        /// <summary>
        /// Determines if the segment counts to the dungeon's total floor count.
        /// </summary>
        public bool IsRelevant;

        [Dev.Multiline(0)]
        public string Comment;

        public ZoneSegmentBase()
        {
            ZoneSteps = new List<ZoneStep>();
            Comment = "";
        }

        /// <summary>
        /// Get the map gen of the target floor ID.
        /// Throws an exception if it is a floor ID equal to or higher than FloorCount
        /// </summary>
        /// <param name="floorId">Floor ID of the target floor</param>
        /// <returns></returns>
        public abstract IFloorGen GetMapGen(int floorId);

        public IGenContext GetMap(ZoneGenContext zoneContext)
        {
            return GetMapGen(zoneContext.CurrentID).GenMap(zoneContext);
        }

        public override string ToString()
        {
            foreach (ZoneStep step in ZoneSteps)
            {
                var startStep = step as FloorNameIDZoneStep;
                if (startStep != null)
                    return LocalText.FormatLocalText(startStep.Name, FloorCount.ToString()).ToLocal().Replace('\n', ' ');
            }
            return String.Format("[{0}] {1}F", this.GetType().GetFormattedTypeName(), FloorCount);
        }
    }

    [Serializable]
    public class ZoneGenContext
    {
        public ulong Seed;
        public string CurrentZone;
        public int CurrentSegment;
        public int CurrentID;
        public List<ZoneStep> ZoneSteps;

        public ZoneGenContext()
        {
            ZoneSteps = new List<ZoneStep>();
        }
    }
}
