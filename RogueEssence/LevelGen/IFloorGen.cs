﻿using System;
using System.Runtime.Serialization;
using RogueElements;
using RogueEssence.Data;

namespace RogueEssence.LevelGen
{
    /// <summary>
    /// A floor generator that utilizes a grid plan.
    /// The grid plan is a grid of X by Y cells, which can be filled by a room generator.
    /// Additionally, horizontal and vertical hallways connect the cells to each other.
    /// Using this generator allows for gen steps that operate on grid plans.
    /// It also allows everything RoomFloorGen allows.
    /// </summary>
    [Serializable]
    public class GridFloorGen : FloorMapGen<MapGenContext>
    {
        //public Map ExtractMap(ulong seed)
        //{
        //    return ((MapGenContext)GenMap(seed)).Map;
        //}


        public override string ToString()
        {
            string startInfo = "[EMPTY]";
            foreach (GenStep<MapGenContext> step in GenSteps.EnumerateInOrder())
            {
                var startStep = step as InitGridPlanStep<MapGenContext>;
                if (startStep != null)
                {
                    startInfo = string.Format("Cells:{0}x{1} CellSize:{2}x{3}", startStep.CellX, startStep.CellY, startStep.CellWidth, startStep.CellHeight);
                    break;
                }
            }
            return String.Format("{0}: {1}", this.GetType().GetFormattedTypeName(), startInfo);
        }
    }

    /// <summary>
    /// A floor generator that utilizes a floor plan.
    /// The floor plan a list of rooms and hallways, which can be placed in any location.
    /// The room and hall positions are not confined to a grid.
    /// Using this generator allows for gen steps that operate on floor plans.
    /// It also allows everything StairsFloorGen allows.
    /// </summary>
    [Serializable]
    public class RoomFloorGen : FloorMapGen<ListMapGenContext>
    {
        //public Map ExtractMap(ulong seed)
        //{
        //    return ((MapGenContext)GenMap(seed)).Map;
        //}

        public override string ToString()
        {
            string startInfo = "[EMPTY]";
            foreach (GenStep<ListMapGenContext> step in GenSteps.EnumerateInOrder())
            {
                var startStep = step as InitFloorPlanStep<ListMapGenContext>;
                if (startStep != null)
                {
                    startInfo = string.Format("Size:{0}x{1}", startStep.Width, startStep.Height);
                    break;
                }
            }
            return String.Format("{0}: {1}", this.GetType().GetFormattedTypeName(), startInfo);
        }
    }

    /// <summary>
    /// A floor generator that utilizes stairs.
    /// Using this generator allows for gen steps that operate on stairs.
    /// It also allows basic tile and spawning gen steps.
    /// </summary>
    [Serializable]
    public class StairsFloorGen : FloorMapGen<StairsMapGenContext>
    {
        //public Map ExtractMap(ulong seed)
        //{
        //    return ((MapGenContext)GenMap(seed)).Map;
        //}

        public override string ToString()
        {
            string startInfo = "[EMPTY]";
            foreach (GenStep<StairsMapGenContext> step in GenSteps.EnumerateInOrder())
            {
                var startStep = step as InitTilesStep<StairsMapGenContext>;
                if (startStep != null)
                {
                    startInfo = string.Format("Size:{0}x{1}", startStep.Width, startStep.Height);
                    break;
                }
            }
            return String.Format("{0}: {1}", this.GetType().GetFormattedTypeName(), startInfo);
        }

    }

    /// <summary>
    /// A floor generator that is suited for loading maps created by the editor.
    /// Using this generator allows for gen steps that load maps.
    /// It also allows basic tile and spawning gen steps.
    /// </summary>
    [Serializable]
    public class LoadGen : FloorMapGen<MapLoadContext>
    {
        //public Map ExtractMap(ulong seed)
        //{
        //    return ((MapGenContext)GenMap(seed)).Map;
        //}

        public override string ToString()
        {
            string startInfo = "[EMPTY]";
            foreach (GenStep<MapLoadContext> step in GenSteps.EnumerateInOrder())
            {
                var startStep = step as MappedRoomStep<MapLoadContext>;
                if (startStep != null)
                {
                    startInfo = string.Format("Map:{0}", startStep.MapID);
                    break;
                }
            }
            return String.Format("{0}: {1}", this.GetType().GetFormattedTypeName(), startInfo);
        }

    }

    [Serializable]
    public abstract class FloorMapGen<T> : MapGen<T>, IFloorGen
        where T : BaseMapGenContext
    {
        [Dev.Multiline(0)]
        public string Comment;

        public FloorMapGen()
        {
            Comment = "";
        }

        public IGenContext GenMap(ZoneGenContext zoneContext)
        {
            //it will first create the instance of the context,
            //and set up a local List<GenPriority<IGenStep>> variable 
            //the zonecontext members include runtime zonepostprocs
            //that will appreciate a IGenContext + List<GenPriority<IGenStep>> being passed into them
            //then, gensteps will continue on as per usual

            T map = (T)Activator.CreateInstance(typeof(T));
            map.Map.ID = zoneContext.CurrentID;
            map.InitSeed(zoneContext.Seed);

            GenContextDebug.DebugInit(map);

            //postprocessing steps:
            StablePriorityQueue<Priority, IGenStep> queue = new StablePriorityQueue<Priority, IGenStep>();
            foreach (Priority priority in GenSteps.GetPriorities())
            {
                foreach (IGenStep genStep in GenSteps.GetItems(priority))
                    queue.Enqueue(priority, genStep);
            }

            foreach (ZoneStep zoneStep in zoneContext.ZoneSteps)
                zoneStep.Apply(zoneContext, map, queue);
            
            foreach (ZoneStep zoneStep in DataManager.Instance.UniversalEvent.ZoneSteps)
                zoneStep.Apply(zoneContext, map, queue);

            if (DiagManager.Instance.DevMode && DiagManager.Instance.ListenGen)
            {
                DiagManager.Instance.LogInfo("Generating map with these steps:");
                StablePriorityQueue<Priority, IGenStep> queue2 = new StablePriorityQueue<Priority, IGenStep>();
                while (queue.Count > 0)
                {
                    Priority pr = queue.FrontPriority();
                    IGenStep step = queue.Dequeue();
                    DiagManager.Instance.LogInfo(String.Format("\t{0}: {1}", pr.ToString(), step.ToString()));
                    queue2.Enqueue(pr, step);
                }
                queue = queue2;
            }

            ApplyGenSteps(map, queue);

            map.FinishGen();

            return map;
        }
    }


    /// <summary>
    /// Chooses one floorgen out of several possibilities.
    /// </summary>
    [Serializable]
    public class ChanceFloorGen : IFloorGen
    {
        public SpawnList<IFloorGen> Spawns;

        public ChanceFloorGen()
        {
            Spawns = new SpawnList<IFloorGen>();
        }

        public IGenContext GenMap(ZoneGenContext zoneContext)
        {
            //NOTE: initializing the seed like this means the genned map technically reuses the first roll used to pick its algorithm in the first place
            //problem?
            IRandom spawnRand = new ReRandom(zoneContext.Seed);
            IFloorGen gen = Spawns.Pick(spawnRand);

            IGenContext map = gen.GenMap(zoneContext);

            return map;
        }
    }

    public interface IFloorGen
    {
        //Map ExtractMap(ulong seed);
        IGenContext GenMap(ZoneGenContext zoneContext);
    }
}
