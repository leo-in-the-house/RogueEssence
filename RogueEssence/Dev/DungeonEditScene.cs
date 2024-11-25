﻿using System;
using System.Collections.Generic;
using RogueEssence.Content;
using RogueElements;
using RogueEssence.Data;
using RogueEssence.Dungeon;
using RogueEssence.Ground;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace RogueEssence.Dev
{
    //The game engine for Ground Mode, in which the player has free movement
    public partial class DungeonEditScene : BaseDungeonScene
    {
        public enum EditorMode
        {
            None = -1,
            Texture,
            Decoration,
            Terrain,
            Tile,
            Item,
            Entity,
            Entrance,
            Other
        }

        private static DungeonEditScene instance;
        public static void InitInstance()
        {
            if (instance != null)
                GraphicsManager.ZoomChanged -= instance.ZoomChanged;
            instance = new DungeonEditScene();
            GraphicsManager.ZoomChanged += instance.ZoomChanged;
        }
        public static DungeonEditScene Instance { get { return instance; } }

        static Keys[] DirKeys = new Keys[4] { Keys.S, Keys.A, Keys.W, Keys.D };

        public Loc FocusedLoc;
        public Loc DiffLoc;

        public EditorMode EditMode;

        public CanvasStroke<AutoTile> AutoTileInProgress;
        public CanvasStroke<TerrainTile> TerrainInProgress;
        public CanvasStroke<EffectTile> TileInProgress;
        public MapItem ItemInProgress;
        public Character CharacterInProgress;
        public GroundAnim DecorationInProgress;
        public bool PendingStroke;

        public bool ShowTerrain;
        public bool ShowEntrances;
        public bool ShowObjectBoxes;

        public GroundAnim SelectedDecoration;

        public override void UpdateMeta()
        {
            base.UpdateMeta();

            InputManager input = GameManager.Instance.MetaInputManager;
            var mapEditor = DiagManager.Instance.DevEditor.MapEditor;

            if (mapEditor != null && mapEditor.Active)
                mapEditor.ProcessInput(input);
        }

        public override IEnumerator<YieldInstruction> ProcessInput()
        {
            GameManager.Instance.FrameProcessed = false;

            if (PendingDevEvent != null)
            {
                yield return CoroutineManager.Instance.StartCoroutine(PendingDevEvent);
                PendingDevEvent = null;
            }
            else
                yield return CoroutineManager.Instance.StartCoroutine(ProcessInput(GameManager.Instance.InputManager));

            if (!GameManager.Instance.FrameProcessed)
                yield return new WaitForFrames(1);
        }

        IEnumerator<YieldInstruction> ProcessInput(InputManager input)
        {
            Loc dirLoc = Loc.Zero;

            for (int ii = 0; ii < DirKeys.Length; ii++)
            {
                if (input.BaseKeyDown(DirKeys[ii]))
                    dirLoc = dirLoc + ((Dir4)ii).GetLoc();
            }

            bool slow = input.BaseKeyDown(Keys.LeftShift);
            int speed = 8;
            if (slow)
                speed = 1;
            else
            {
                switch (GraphicsManager.Zoom)
                {
                    case GraphicsManager.GameZoom.x8Near:
                        speed = 1;
                        break;
                    case GraphicsManager.GameZoom.x4Near:
                        speed = 2;
                        break;
                    case GraphicsManager.GameZoom.x2Near:
                        speed = 4;
                        break;
                }
            }

            DiffLoc = dirLoc * speed;

            yield break;
        }


        public override void Update(FrameTick elapsedTime)
        {
            if (ZoneManager.Instance.CurrentMap != null)
            {
                foreach (Character character in ZoneManager.Instance.CurrentMap.IterateCharacters())
                    character.UpdateFrame();

                FocusedLoc += DiffLoc;
                DiffLoc = new Loc();

                float scale = GraphicsManager.Zoom.GetScale();

                if (ZoneManager.Instance.CurrentMap.EdgeView == Map.ScrollEdge.Clamp)
                    FocusedLoc = new Loc(Math.Max((int)(GraphicsManager.ScreenWidth / scale / 2), Math.Min(FocusedLoc.X,
                        ZoneManager.Instance.CurrentMap.Width * GraphicsManager.TileSize - (int)(GraphicsManager.ScreenWidth / scale / 2))),
                        Math.Max((int)(GraphicsManager.ScreenHeight / scale / 2), Math.Min(FocusedLoc.Y,
                        ZoneManager.Instance.CurrentMap.Height * GraphicsManager.TileSize - (int)(GraphicsManager.ScreenHeight / scale / 2))));
                else
                    FocusedLoc = new Loc(Math.Max(0, Math.Min(FocusedLoc.X, ZoneManager.Instance.CurrentMap.Width * GraphicsManager.TileSize)),
                        Math.Max(0, Math.Min(FocusedLoc.Y, ZoneManager.Instance.CurrentMap.Height * GraphicsManager.TileSize)));

                base.UpdateCamMod(elapsedTime, ref FocusedLoc);

                UpdateCam(FocusedLoc);

                base.Update(elapsedTime);
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            //
            //When in editor mode, we want to display an overlay over some entities
            //

            if (DiagManager.Instance.DevEditor.MapEditor != null && DiagManager.Instance.DevEditor.MapEditor.Active && ZoneManager.Instance.CurrentMap != null)
                DrawGame(spriteBatch);
        }
        protected override void PostDraw(SpriteBatch spriteBatch)
        {
            Matrix matrix = Matrix.CreateScale(new Vector3(drawScale, drawScale, 1));
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, matrix);

            if (AutoTileInProgress != null)
            {
                for (int jj = viewTileRect.Y; jj < viewTileRect.End.Y; jj++)
                {
                    for (int ii = viewTileRect.X; ii < viewTileRect.End.X; ii++)
                    {
                        Loc testLoc = new Loc(ii, jj);
                        if (Collision.InBounds(ZoneManager.Instance.CurrentMap.Width, ZoneManager.Instance.CurrentMap.Height, testLoc) &&
                            AutoTileInProgress.IncludesLoc(testLoc))
                        {
                            AutoTile brush = AutoTileInProgress.GetBrush(testLoc);
                            if (brush.IsEmpty())
                                GraphicsManager.Pixel.Draw(spriteBatch, new Rectangle(ii * GraphicsManager.TileSize - ViewRect.X, jj * GraphicsManager.TileSize - ViewRect.Y, GraphicsManager.TileSize, GraphicsManager.TileSize), null, Color.Black);
                            else
                                brush.Draw(spriteBatch, new Loc(ii * GraphicsManager.TileSize, jj * GraphicsManager.TileSize) - ViewRect.Start);
                        }
                    }
                }
            }

            //draw the blocks

            for (int jj = viewTileRect.Y; jj < viewTileRect.End.Y; jj++)
            {
                for (int ii = viewTileRect.X; ii < viewTileRect.End.X; ii++)
                {
                    Loc testLoc = new Loc(ii, jj);
                    if (Collision.InBounds(ZoneManager.Instance.CurrentMap.Width, ZoneManager.Instance.CurrentMap.Height, testLoc))
                    {
                        TerrainTile tile = ZoneManager.Instance.CurrentMap.Tiles[ii][jj].Data;
                        if (TerrainInProgress != null && TerrainInProgress.IncludesLoc(testLoc))
                        {
                            tile = TerrainInProgress.GetBrush(testLoc);
                            if (tile.TileTex.IsEmpty())
                                GraphicsManager.Pixel.Draw(spriteBatch, new Rectangle(ii * GraphicsManager.TileSize - ViewRect.X, jj * GraphicsManager.TileSize - ViewRect.Y, GraphicsManager.TileSize, GraphicsManager.TileSize), null, Color.Black);
                            else
                                tile.TileTex.Draw(spriteBatch, new Loc(ii * GraphicsManager.TileSize, jj * GraphicsManager.TileSize) - ViewRect.Start);
                        }

                        if (ShowTerrain)
                        {
                            TerrainData data = (TerrainData)tile.GetData();
                            Color color = Color.Transparent;
                            switch (data.BlockType)
                            {
                                case TerrainData.Mobility.Block:
                                    color = Color.Red;
                                    break;
                                case TerrainData.Mobility.Water:
                                    color = Color.Blue;
                                    break;
                                case TerrainData.Mobility.Lava:
                                    color = Color.Orange;
                                    break;
                                case TerrainData.Mobility.Abyss:
                                    color = Color.Black;
                                    break;
                                case TerrainData.Mobility.Impassable:
                                    color = Color.White;
                                    break;

                            }
                            if (color != Color.Transparent)
                                GraphicsManager.Pixel.Draw(spriteBatch, new Rectangle(ii * GraphicsManager.TileSize - ViewRect.X, jj * GraphicsManager.TileSize - ViewRect.Y, GraphicsManager.TileSize, GraphicsManager.TileSize), null, color * 0.5f);
                        }
                    }
                }
            }
            if (ShowEntrances)
            {
                foreach (LocRay8 entrance in ZoneManager.Instance.CurrentMap.EntryPoints)
                {
                    Color showColor = Color.OrangeRed;
                    GraphicsManager.Pixel.Draw(spriteBatch, new Rectangle(entrance.Loc.X * GraphicsManager.TileSize - ViewRect.X, entrance.Loc.Y * GraphicsManager.TileSize - ViewRect.Y, GraphicsManager.TileSize, GraphicsManager.TileSize), null, showColor * 0.75f);
                }
            }
            if (ShowObjectBoxes)
            {
                //Draw Entity bounds
                GroundDebug dbg = new GroundDebug(spriteBatch, Color.BlueViolet);
                foreach (GroundAnim entity in ZoneManager.Instance.CurrentMap.IterateDecorations())
                {
                    Rect bounds = entity.GetBounds();
                    if (SelectedDecoration == entity)
                    {
                        //Invert the color of selected entities
                        dbg.LineThickness = 1.0f;
                        dbg.DrawColor = Color.BlueViolet;
                        dbg.DrawFilledBox(new Rect(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1), 92);
                    }
                    else
                    {
                        //Draw boxes around other entities with graphics using low opacity
                        dbg.DrawColor = new Color(Color.BlueViolet.R, Color.BlueViolet.G, Color.BlueViolet.B, 92);
                        dbg.LineThickness = 1.0f;
                        dbg.DrawBox(new Rect(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1));
                    }
                }
            }
            if (DecorationInProgress != null)
            {
                DecorationInProgress.DrawPreview(spriteBatch, ViewRect.Start, PendingStroke ? 1f : 0.75f);
                if (ShowObjectBoxes)
                {
                    Rect bounds = DecorationInProgress.GetBounds();
                    GroundDebug dbg = new GroundDebug(spriteBatch, Color.White);
                    dbg.DrawColor = new Color(255, 255, 255, 92);
                    dbg.LineThickness = 1.0f;
                    dbg.DrawBox(new Rect(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1));
                }
            }

            for (int jj = viewTileRect.Y; jj < viewTileRect.End.Y; jj++)
            {
                for (int ii = viewTileRect.X; ii < viewTileRect.End.X; ii++)
                {
                    Loc testLoc = new Loc(ii, jj);
                    if (Collision.InBounds(ZoneManager.Instance.CurrentMap.Width, ZoneManager.Instance.CurrentMap.Height, testLoc))
                    {
                        EffectTile existingEffect = ZoneManager.Instance.CurrentMap.Tiles[ii][jj].Effect;
                        //draw normally invisible tiles
                        if (!String.IsNullOrEmpty(existingEffect.ID))
                        {
                            TileData entry = DataManager.Instance.GetTile(existingEffect.ID);
                            if (entry.Anim.AnimIndex == "")
                                GraphicsManager.Pixel.Draw(spriteBatch, new Rectangle(ii * GraphicsManager.TileSize - ViewRect.X, jj * GraphicsManager.TileSize - ViewRect.Y, GraphicsManager.TileSize, GraphicsManager.TileSize), null, Color.White * 0.5f);
                        }
                        if (TileInProgress != null && TileInProgress.IncludesLoc(testLoc))
                        {
                            EffectTile tile = TileInProgress.GetBrush(testLoc);
                            if (String.IsNullOrEmpty(tile.ID))
                                GraphicsManager.Pixel.Draw(spriteBatch, new Rectangle(ii * GraphicsManager.TileSize - ViewRect.X, jj * GraphicsManager.TileSize - ViewRect.Y, GraphicsManager.TileSize, GraphicsManager.TileSize), null, Color.Black);
                            else
                            {
                                TileData entry = DataManager.Instance.GetTile(tile.ID);
                                if (entry.Anim.AnimIndex != "")
                                {
                                    DirSheet sheet = GraphicsManager.GetObject(entry.Anim.AnimIndex);
                                    Loc drawLoc = new Loc(ii * GraphicsManager.TileSize, jj * GraphicsManager.TileSize) - ViewRect.Start + new Loc(GraphicsManager.TileSize / 2) - new Loc(sheet.Width, sheet.Height) / 2;
                                    drawLoc += entry.Offset;
                                    sheet.DrawDir(spriteBatch, drawLoc.ToVector2(), entry.Anim.GetCurrentFrame(GraphicsManager.TotalFrameTick, sheet.TotalFrames),
                                        entry.Anim.GetDrawDir(Dir8.None), Color.White);
                                }
                                else
                                    GraphicsManager.Pixel.Draw(spriteBatch, new Rectangle(ii * GraphicsManager.TileSize - ViewRect.X, jj * GraphicsManager.TileSize - ViewRect.Y, GraphicsManager.TileSize, GraphicsManager.TileSize), null, Color.White);
                            }
                        }
                    }
                }
            }

            if (ItemInProgress != null)
                ItemInProgress.DrawPreview(spriteBatch, ViewRect.Start, 0.75f);
            if (CharacterInProgress != null)
                CharacterInProgress.DrawPreview(spriteBatch, ViewRect.Start, 0.75f);

            spriteBatch.End();
        }

        public override void DrawDev(SpriteBatch spriteBatch)
        {
            BaseSheet blank = GraphicsManager.Pixel;
            int tileSize = GraphicsManager.TileSize;
            for (int jj = viewTileRect.Y; jj < viewTileRect.End.Y; jj++)
            {
                for (int ii = viewTileRect.X; ii < viewTileRect.End.X; ii++)
                {
                    if (Collision.InBounds(ZoneManager.Instance.CurrentMap.Width, ZoneManager.Instance.CurrentMap.Height, new Loc(ii, jj)))
                    {
                        blank.Draw(spriteBatch, new Rectangle((int)((ii * tileSize - ViewRect.X) * WindowScale * scale), (int)((jj * tileSize - ViewRect.Y) * WindowScale * scale), (int)(tileSize * WindowScale * scale), 1), null, Color.White * 0.5f);
                        blank.Draw(spriteBatch, new Rectangle((int)((ii * tileSize - ViewRect.X) * WindowScale * scale), (int)((jj * tileSize - ViewRect.Y) * WindowScale * scale), 1, (int)(tileSize * WindowScale * scale)), null, Color.White * 0.5f);
                    }
                    else if (ii == ZoneManager.Instance.CurrentMap.Width && Collision.InBounds(ZoneManager.Instance.CurrentMap.Height, jj))
                        blank.Draw(spriteBatch, new Rectangle((int)((ii * tileSize - ViewRect.X) * WindowScale * scale), (int)((jj * tileSize - ViewRect.Y) * WindowScale * scale), 1, (int)(tileSize * WindowScale * scale)), null, Color.White * 0.5f);
                    else if (jj == ZoneManager.Instance.CurrentMap.Height && Collision.InBounds(ZoneManager.Instance.CurrentMap.Width, ii))
                        blank.Draw(spriteBatch, new Rectangle((int)((ii * tileSize - ViewRect.X) * WindowScale * scale), (int)((jj * tileSize - ViewRect.Y) * WindowScale * scale), (int)(tileSize * WindowScale * scale), 1), null, Color.White * 0.5f);
                }
            }

            base.DrawDev(spriteBatch);
        }

        public override void DrawDebug(SpriteBatch spriteBatch)
        {
            base.DrawDebug(spriteBatch);

            if (ZoneManager.Instance.CurrentMap != null)
            {
                if (EditMode == EditorMode.Decoration)
                {
                    if (SelectedDecoration != null)
                        GraphicsManager.SysFont.DrawText(spriteBatch, GraphicsManager.WindowWidth - 2, 82, String.Format("Obj X:{0:D3} Y:{1:D3}", SelectedDecoration.MapLoc.X, SelectedDecoration.MapLoc.Y), null, DirV.Up, DirH.Right, Color.White);
                }
            }
        }

        public void EnterMapEdit(int entryPoint)
        {
            if (ZoneManager.Instance.CurrentMap.EntryPoints.Count > 0)
            {
                LocRay8 entry = ZoneManager.Instance.CurrentMap.EntryPoints[entryPoint];
                FocusedLoc = entry.Loc;
            }

            DiagManager.Instance.DevEditor.OpenMap();
        }

        public override void Exit()
        {
            DiagManager.Instance.DevEditor.CloseMap();
        }
    }
}
