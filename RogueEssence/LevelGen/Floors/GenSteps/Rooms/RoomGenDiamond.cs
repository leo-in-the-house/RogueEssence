﻿using System;
using RogueElements;

namespace RogueEssence.LevelGen
{
    /// <summary>
    /// Generates a diamond-shaped room.  Square dimensions result in a perfect diamond, while rectangular dimensions result in edged capsules.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class RoomGenDiamond<T> : RoomGen<T>, ISizedRoomGen
        where T : ITiledGenContext
    {
        public RoomGenDiamond()
        {
        }

        public RoomGenDiamond(RandRange width, RandRange height)
        {
            this.Width = width;
            this.Height = height;
        }

        protected RoomGenDiamond(RoomGenDiamond<T> other)
        {
            this.Width = other.Width;
            this.Height = other.Height;
        }

        /// <summary>
        /// Width of the room.
        /// </summary>
        public RandRange Width { get; set; }

        /// <summary>
        /// Height of the room.
        /// </summary>
        public RandRange Height { get; set; }

        public override RoomGen<T> Copy() => new RoomGenDiamond<T>(this);

        public override Loc ProposeSize(IRandom rand)
        {
            return new Loc(this.Width.Pick(rand), this.Height.Pick(rand));
        }

        public override void DrawOnMap(T map)
        {
            int diameter = Math.Min(this.Draw.Width, this.Draw.Height);

            for (int ii = 0; ii < this.Draw.Width; ii++)
            {
                for (int jj = 0; jj < this.Draw.Height; jj++)
                {
                    if (IsTileWithinDiamond(ii, jj, diameter, this.Draw.Size))
                        map.SetTile(new Loc(this.Draw.X + ii, this.Draw.Y + jj), map.RoomTerrain.Copy());
                }
            }

            // hall restrictions
            this.SetRoomBorders(map);
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}x{2}", this.GetType().GetFormattedTypeName(), this.Width.ToString(), this.Height.ToString());
        }

        protected override void PrepareFulfillableBorders(IRandom rand)
        {
            int diameter = Math.Min(this.Draw.Width, this.Draw.Height);
            for (int jj = 0; jj < this.Draw.Width; jj++)
            {
                if (IsTileWithinDiamond(jj, 0, diameter, this.Draw.Size))
                {
                    this.FulfillableBorder[Dir4.Up][jj] = true;
                    this.FulfillableBorder[Dir4.Down][jj] = true;
                }
            }

            for (int jj = 0; jj < this.Draw.Height; jj++)
            {
                if (IsTileWithinDiamond(0, jj, diameter, this.Draw.Size))
                {
                    this.FulfillableBorder[Dir4.Left][jj] = true;
                    this.FulfillableBorder[Dir4.Right][jj] = true;
                }
            }
        }

        private static bool IsTileWithinDiamond(int baseX, int baseY, int diameter, Loc size)
        {
            Loc sizeX2 = size * 2;
            int x = (baseX * 2) + 1;
            int y = (baseY * 2) + 1;

            if (x < diameter)
            {
                int xdiff = diameter - x;
                if (y < diameter)
                {
                    int ydiff = diameter - y;
                    if (xdiff + ydiff <= diameter)
                        return true;
                }
                else if (y > sizeX2.Y - diameter)
                {
                    int ydiff = y - (sizeX2.Y - diameter);
                    if (xdiff + ydiff <= diameter)
                        return true;
                }
                else
                {
                    return true;
                }
            }
            else if (x > sizeX2.X - diameter)
            {
                int xdiff = x - (sizeX2.X - diameter);
                if (y < diameter)
                {
                    int ydiff = diameter - y;
                    if (xdiff + ydiff <= diameter)
                        return true;
                }
                else if (y > sizeX2.Y - diameter)
                {
                    int ydiff = y - (sizeX2.Y - diameter);
                    if (xdiff + ydiff <= diameter)
                        return true;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }

            return false;
        }
    }
}
