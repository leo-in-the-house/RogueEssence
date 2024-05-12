﻿using System;
using System.Collections.Generic;
using RogueElements;

namespace RogueEssence.LevelGen
{
    /// <summary>
    /// Generates specific rooms randomly across the whole dungeon segment.  This is done by replacing an existing room on the floor.
    /// </summary>
    [Serializable]
    public class SpreadRoomZoneStep : SpreadZoneStep
    {
        /// <summary>
        /// The rooms to distribute.
        /// </summary>
        public SpawnList<RoomGenOption> Spawns;

        /// <summary>
        /// At what point in the map gen process to add the room in, if the floor gen uses a grid plan.
        /// </summary>
        public Priority PriorityGrid;

        /// <summary>
        /// At what point in the map gen process to add the room in, if the floor gen uses a floor plan.
        /// </summary>
        public Priority PriorityList;

        public SpreadRoomZoneStep()
        {
            Spawns = new SpawnList<RoomGenOption>();
        }

        public SpreadRoomZoneStep(Priority priorityGrid, Priority priorityList, SpreadPlanBase plan) : base(plan)
        {
            Spawns = new SpawnList<RoomGenOption>();
            PriorityGrid = priorityGrid;
            PriorityList = priorityList;
            SpreadPlan = plan;
        }

        protected SpreadRoomZoneStep(SpreadRoomZoneStep other, ulong seed) : base(other, seed)
        {
            Spawns = (SpawnList<RoomGenOption>)other.Spawns.CopyState();
            PriorityGrid = other.PriorityGrid;
            PriorityList = other.PriorityList;
        }
        public override ZoneStep Instantiate(ulong seed) { return new SpreadRoomZoneStep(this, seed); }

        protected override bool ApplyToFloor(ZoneGenContext zoneContext, IGenContext context, StablePriorityQueue<Priority, IGenStep> queue, int dropIdx)
        {
            //find the first postproc that is a GridRoom postproc and add this to its special rooms
            //NOTE: if a room-based generator is not found as the generation step, it will just skip this floor but treat it as though it was placed.

            //TODO: allow arbitrary components to be added
            RoomGenOption genDuo = Spawns.Pick(context.Rand);
            SetGridSpecialRoomStep<MapGenContext> specialStep = new SetGridSpecialRoomStep<MapGenContext>();
            SetSpecialRoomStep<ListMapGenContext> listSpecialStep = new SetSpecialRoomStep<ListMapGenContext>();

            specialStep.Filters = genDuo.Filters;
            if (specialStep.CanApply(context))
            {
                specialStep.Rooms = new PresetPicker<RoomGen<MapGenContext>>(genDuo.GridOption);
                specialStep.Filters = genDuo.Filters;
                specialStep.RoomComponents.Set(new ImmutableRoom());
                queue.Enqueue(PriorityGrid, specialStep);
            }
            else if (listSpecialStep.CanApply(context))
            {
                listSpecialStep.Rooms = new PresetPicker<RoomGen<ListMapGenContext>>(genDuo.ListOption);
                listSpecialStep.Filters = genDuo.Filters;
                listSpecialStep.RoomComponents.Set(new ImmutableRoom());
                PresetPicker<PermissiveRoomGen<ListMapGenContext>> picker = new PresetPicker<PermissiveRoomGen<ListMapGenContext>>();
                picker.ToSpawn = new RoomGenAngledHall<ListMapGenContext>(0);
                listSpecialStep.Halls = picker;
                queue.Enqueue(PriorityList, listSpecialStep);
            }
            return true;
        }

        public override string ToString()
        {
            int count = 0;
            RoomGenOption singleGen = null;
            if (Spawns != null)
            {
                foreach (RoomGenOption gen in Spawns.EnumerateOutcomes())
                {
                    count++;
                    singleGen = gen;
                }
            }
            if (count == 1)
                return string.Format("{0}: {1}", this.GetType().GetFormattedTypeName(), singleGen.ToString());
            return string.Format("{0}[{1}]", this.GetType().GetFormattedTypeName(), count);
        }
    }

    [Serializable]
    public class RoomGenOption
    {
        /// <summary>
        /// The room generator used if the floor gen uses a grid plan.
        /// </summary>
        public RoomGen<MapGenContext> GridOption;

        /// <summary>
        /// The room generator used if the floor gen uses a floor plan.
        /// </summary>
        public RoomGen<ListMapGenContext> ListOption;

        /// <summary>
        /// Determines which rooms are eligible to be replaced.
        /// </summary>
        public List<BaseRoomFilter> Filters;

        public RoomGenOption(RoomGen<MapGenContext> gridOption, RoomGen<ListMapGenContext> listOption, List<BaseRoomFilter> filters)
        {
            GridOption = gridOption;
            ListOption = listOption;
            Filters = filters;
        }

        public override string ToString()
        {
            if (GridOption != null)
                return GridOption.ToString();
            if (ListOption != null)
                return ListOption.ToString();

            return string.Format("{0}: [EMPTY]", this.GetType().GetFormattedTypeName());
        }
    }
}
