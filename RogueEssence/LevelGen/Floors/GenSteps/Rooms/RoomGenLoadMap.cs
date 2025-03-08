﻿using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence.Dungeon;
using RogueEssence.Dev;
using RogueEssence.LevelGen;
using RogueEssence.Data;
using RogueEssence.Content;
using RogueEssence.Ground;

namespace RogueEssence.LevelGen
{
    /// <summary>
    /// Generates a room by loading a map as the room.
    /// Includes tiles, items, enemies, and mapstarts.
    /// Borders are specified by a tile.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class RoomGenLoadMap<T> : RoomGenLoadMapBase<T> where T : BaseMapGenContext
    {
        /// <summary>
        /// The terrain that counts as room.  Halls will only attach to room tiles, or tiles specified with Borders.
        /// </summary>
        public ITile RoomTerrain { get; set; }

        public RoomGenLoadMap()
        {
            MapID = "";
        }


        protected RoomGenLoadMap(RoomGenLoadMap<T> other) : base(other)
        {
            this.RoomTerrain = other.RoomTerrain;
        }

        public override RoomGen<T> Copy() { return new RoomGenLoadMap<T>(this); }



        protected override void PrepareFulfillableBorders(IRandom rand)
        {
            // NOTE: Because the context is not passed in when preparing borders,
            // the tile ID representing an opening must be specified on this class instead.
            if (this.Draw.Width != this.roomMap.Width || this.Draw.Height != this.roomMap.Height)
            {
                foreach (Dir4 dir in DirExt.VALID_DIR4)
                {
                    for (int jj = 0; jj < this.FulfillableBorder[dir].Length; jj++)
                        this.FulfillableBorder[dir][jj] = true;
                }
            }
            else
            {
                for (int ii = 0; ii < this.Draw.Width; ii++)
                {
                    this.FulfillableBorder[Dir4.Up][ii] = this.roomMap.Tiles[ii][0].TileEquivalent(this.RoomTerrain);
                    this.FulfillableBorder[Dir4.Down][ii] = this.roomMap.Tiles[ii][this.Draw.Height - 1].TileEquivalent(this.RoomTerrain);
                }

                for (int ii = 0; ii < this.Draw.Height; ii++)
                {
                    this.FulfillableBorder[Dir4.Left][ii] = this.roomMap.Tiles[0][ii].TileEquivalent(this.RoomTerrain);
                    this.FulfillableBorder[Dir4.Right][ii] = this.roomMap.Tiles[this.Draw.Width - 1][ii].TileEquivalent(this.RoomTerrain);
                }
            }
        }
    }


    /// <summary>
    /// Generates a room by loading a map as the room.
    /// Includes tiles, items, enemies, and mapstarts.
    /// Borders are specified by the user.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class RoomGenLoadMapBordered<T> : RoomGenLoadMapBase<T> where T : BaseMapGenContext
    {
        /// <summary>
        /// Determines which tiles of the border are open for halls.
        /// </summary>
        public Dictionary<Dir4, bool[]> Borders { get; set; }

        /// <summary>
        /// Determines if connecting hallways should continue digging inward after they hit the room bounds, until a walkable tile is found.
        /// </summary>
        //public bool FulfillAll { get; set; }


        public RoomGenLoadMapBordered()
        {
            MapID = "";
            this.Borders = new Dictionary<Dir4, bool[]>();
        }


        protected RoomGenLoadMapBordered(RoomGenLoadMapBordered<T> other) : base(other)
        {
            //this.FulfillAll = other.FulfillAll;

            this.Borders = new Dictionary<Dir4, bool[]>();
            foreach (Dir4 dir in DirExt.VALID_DIR4)
            {
                this.Borders[dir] = new bool[other.Borders[dir].Length];
                for (int jj = 0; jj < other.Borders[dir].Length; jj++)
                    this.Borders[dir][jj] = other.Borders[dir][jj];
            }

        }

        public override RoomGen<T> Copy() { return new RoomGenLoadMapBordered<T>(this); }


        protected override void PrepareFulfillableBorders(IRandom rand)
        {
            // NOTE: Because the context is not passed in when preparing borders,
            // the tile ID representing an opening must be specified on this class instead.
            if (this.Draw.Width != this.roomMap.Width || this.Draw.Height != this.roomMap.Height)
            {
                foreach (Dir4 dir in DirExt.VALID_DIR4)
                {
                    for (int jj = 0; jj < this.FulfillableBorder[dir].Length; jj++)
                        this.FulfillableBorder[dir][jj] = true;
                }
            }
            else
            {
                for (int ii = 0; ii < this.Draw.Width; ii++)
                {
                    this.FulfillableBorder[Dir4.Up][ii] = this.Borders[Dir4.Up][ii];
                    this.FulfillableBorder[Dir4.Down][ii] = this.Borders[Dir4.Down][ii];
                }

                for (int ii = 0; ii < this.Draw.Height; ii++)
                {
                    this.FulfillableBorder[Dir4.Left][ii] = this.Borders[Dir4.Left][ii];
                    this.FulfillableBorder[Dir4.Right][ii] = this.Borders[Dir4.Right][ii];
                }
            }
        }
    }


    [Serializable]
    public abstract class RoomGenLoadMapBase<T> : RoomGen<T> where T : BaseMapGenContext
    {
        /// <summary>
        /// Map file to load.
        /// </summary>
        [Dev.DataFolder(0, "Map/")]
        public string MapID;

        /// <summary>
        /// Prevents later steps from changing the tiles or items specified by this room.
        /// </summary>
        public PostProcType PreventChanges { get; set; }

        [NonSerialized]
        protected Map roomMap;

        public RoomGenLoadMapBase()
        {
            MapID = "";
        }


        protected RoomGenLoadMapBase(RoomGenLoadMapBase<T> other)
        {
            MapID = other.MapID;
            this.PreventChanges = other.PreventChanges;
        }

        public override Loc ProposeSize(IRandom rand)
        {
            roomMap = DataManager.Instance.GetMap(MapID);
            return new Loc(this.roomMap.Width, this.roomMap.Height);
        }

        protected void DrawTiles(T map)
        {
            //add needed layers
            Dictionary<int, int> layerMap = new Dictionary<int, int>();
            Dictionary<Content.DrawLayer, int> drawOrderDict = new Dictionary<Content.DrawLayer, int>();
            for (int ii = 0; ii < this.roomMap.Layers.Count; ii++)
            {
                if (!this.roomMap.Layers[ii].Visible)
                    continue;

                // find the next layer that has the same draw layer as this one
                int layerStart;
                if (!drawOrderDict.TryGetValue(this.roomMap.Layers[ii].Layer, out layerStart))
                    layerStart = 0;
                for (; layerStart < map.Map.Layers.Count; layerStart++)
                {
                    if (map.Map.Layers[layerStart].Layer == this.roomMap.Layers[ii].Layer)
                    {
                        //TODO: also check that the region is not drawn on already
                        break;
                    }
                }
                //add it if it doesn't exist
                if (layerStart == map.Map.Layers.Count)
                {
                    map.Map.AddLayer(this.roomMap.Layers[ii].Name);
                    map.Map.Layers[map.Map.Layers.Count - 1].Layer = this.roomMap.Layers[ii].Layer;
                }
                //set the new layer start variable for which to continue checking from
                layerMap[ii] = layerStart;
                drawOrderDict[this.roomMap.Layers[ii].Layer] = layerStart + 1;
            }

            //draw the tiles
            for (int xx = 0; xx < this.Draw.Width; xx++)
            {
                for (int yy = 0; yy < this.Draw.Height; yy++)
                {
                    map.SetTile(new Loc(this.Draw.X + xx, this.Draw.Y + yy), this.roomMap.Tiles[xx][yy]);
                    for (int ii = 0; ii < this.roomMap.Layers.Count; ii++)
                    {
                        int layerTo;
                        if (layerMap.TryGetValue(ii, out layerTo))
                        {
                            Loc wrapLoc = this.Draw.Start + new Loc(xx, yy);
                            if (map.Map.GetLocInMapBounds(ref wrapLoc))
                                map.Map.Layers[layerTo].Tiles[wrapLoc.X][wrapLoc.Y] = this.roomMap.Layers[ii].Tiles[xx][yy];
                            else
                                throw new IndexOutOfRangeException("Attempted to draw custom room graphics out of range!");
                        }
                    }
                }
            }
        }

        protected void DrawDecorations(T map)
        {
            //place decorations
            foreach (AnimLayer layer in this.roomMap.Decorations)
            {
                if (!layer.Visible)
                    continue;

                foreach (GroundAnim anim in layer.Anims)
                    anim.MapLoc = anim.MapLoc + this.Draw.Start * GraphicsManager.TileSize;
                map.Map.Decorations.Add(layer);
            }
        }

        protected void DrawItems(T map)
        {
            //place items
            foreach (MapItem item in this.roomMap.Items)
            {
                Loc wrapLoc = item.TileLoc + this.Draw.Start;
                if (map.Map.GetLocInMapBounds(ref wrapLoc))
                    item.TileLoc = wrapLoc;
                else
                    throw new IndexOutOfRangeException("Attempted to draw custom room item out of range!");
                map.Items.Add(item);
            }
        }

        protected void DrawMobs(T map)
        {
            //place mobs
            foreach (Team team in this.roomMap.MapTeams)
            {
                foreach (Character member in team.EnumerateChars())
                {
                    Loc wrapLoc = member.CharLoc + this.Draw.Start;
                    if (map.Map.GetLocInMapBounds(ref wrapLoc))
                        member.CharLoc = wrapLoc;
                    else
                        throw new IndexOutOfRangeException("Attempted to draw custom room enemy out of range!");
                }
                map.MapTeams.Add(team);
            }
        }

        protected void DrawEntrances(T map)
        {
            //place map entrances
            foreach (LocRay8 entrance in this.roomMap.EntryPoints)
            {
                Loc wrapLoc = entrance.Loc + this.Draw.Start;
                if (map.Map.GetLocInMapBounds(ref wrapLoc))
                    map.Map.EntryPoints.Add(new LocRay8(wrapLoc, entrance.Dir));
                else
                    throw new IndexOutOfRangeException("Attempted to draw custom room entrance out of range!");
            }
        }

        public override void DrawOnMap(T map)
        {
            if (this.Draw.Width != this.roomMap.Width || this.Draw.Height != this.roomMap.Height)
            {
                this.DrawMapDefault(map);
                return;
            }

            //no copying is needed here since the map is disposed of after use

            DrawTiles(map);

            DrawDecorations(map);

            DrawItems(map);

            DrawMobs(map);

            DrawEntrances(map);

            //this.FulfillRoomBorders(map, this.FulfillAll);
            this.SetRoomBorders(map);

            for (int xx = 0; xx < Draw.Width; xx++)
            {
                for (int yy = 0; yy < Draw.Height; yy++)
                    map.GetPostProc(new Loc(Draw.X + xx, Draw.Y + yy)).AddMask(new PostProcTile(PreventChanges));
            }
        }
    }
}
