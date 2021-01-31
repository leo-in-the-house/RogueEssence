﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using RogueElements;
using RogueEssence.Dungeon;
using ReactiveUI;
using RogueEssence.Content;
using Avalonia.Controls;
using System.Collections.ObjectModel;
using RogueEssence.Data;

namespace RogueEssence.Dev.ViewModels
{
    public class TileBrush
    {
        private TileLayer layer;
        public Loc MultiSelect;

        private int autotile;
        private int bordertile;

        public TileBrush(TileLayer layer, Loc multiSelect)
        {
            autotile = -1;
            bordertile = -1;
            this.layer = layer;
            MultiSelect = multiSelect;
        }

        public TileBrush(int autotile, int bordertile)
        {
            this.autotile = autotile;
            this.bordertile = bordertile;
            this.layer = new TileLayer();
            this.MultiSelect = Loc.One;
        }

        public AutoTile GetSanitizedTile()
        {
            return GetSanitizedTile(Loc.Zero);
        }

        public AutoTile GetSanitizedTile(Loc offset)
        {
            if (autotile > -1)
            {
                AutoTile auto = new AutoTile(autotile, bordertile);
                AutoTileData autoTile = DataManager.Instance.GetAutoTile(autotile);
                auto.Layers.AddRange(autoTile.Tiles.Generic);
                return auto;
            }

            TileLayer newLayer = new TileLayer();
            bool hasFilled = false;
            foreach (TileFrame frame in layer.Frames)
            {
                TileFrame newFrame = new TileFrame(frame.TexLoc + offset, frame.Sheet);
                //check for emptiness
                long tilePos = GraphicsManager.TileIndex.GetPosition(newFrame.Sheet, newFrame.TexLoc);
                if (tilePos > 0)
                {
                    newLayer.Frames.Add(newFrame);
                    hasFilled = true;
                }
                else
                    newLayer.Frames.Add(TileFrame.Empty);
            }
            if (!hasFilled)
                return new AutoTile();

            newLayer.FrameLength = layer.FrameLength;
            return new AutoTile(newLayer);
        }
    }
    public class TileBrowserViewModel : ViewModelBase
    {
        public TileBrowserViewModel()
        {
            currentTileset = "";
            selectedTile = TileFrame.Empty;
            multiSelect = Loc.One;
            preview = TileFrame.Empty;
            frameLength = 60;
            Frames = new ObservableCollection<TileFrame>();
            Frames.Add(TileFrame.Empty);

            tileIndices = new List<string>();
            Tilesets = new SearchListBoxViewModel();
            Tilesets.DataName = "Tilesets:";
            Tilesets.SelectedIndexChanged += Tilesets_SelectedIndexChanged;

            UpdateTilesList();
        }

        List<string> tileIndices;
        public SearchListBoxViewModel Tilesets { get; set; }

        private bool animated;
        public bool Animated
        {
            get => animated;
            set
            {
                this.SetIfChanged(ref animated, value);
                if (!animated)
                {
                    FrameLength = 60;
                    while (Frames.Count > 1)
                        Frames.RemoveAt(Frames.Count-1);
                    SelectedTile = Frames[0];
                    Preview = Frames[0];
                }
            }
        }

        /// <summary>
        /// The current tile being previewed
        /// </summary>
        private TileFrame preview;
        public TileFrame Preview
        {
            get => preview;
            set => this.SetIfChanged(ref preview, value);
        }

        private int frameLength;
        public int FrameLength
        {
            get => frameLength;
            set => this.SetIfChanged(ref frameLength, value);
        }

        public ObservableCollection<TileFrame> Frames { get; }

        private int chosenFrame;
        public int ChosenFrame
        {
            get => chosenFrame;
            set => this.SetIfChanged(ref chosenFrame, value);
        }

        /// <summary>
        /// The chosen tile of the tileset.  Can trigger visibility changes. Only used for guidance.
        /// </summary>
        private TileFrame selectedTile;
        public TileFrame SelectedTile
        {
            get => selectedTile;
            set
            {
                if (this.SetIfChanged(ref selectedTile, value))
                    BorderPresent = BorderPresent;
            }
        }

        /// <summary>
        /// The chosen multiselect of the tileset.  Cannot trigger visibility changes.
        /// </summary>
        private Loc multiSelect;
        public Loc MultiSelect
        {
            get => multiSelect;
            set => this.SetIfChanged(ref multiSelect, value);
        }

        private string currentTileset;
        public string CurrentTileset
        {
            get { return currentTileset; }
            set
            {
                if (this.SetIfChanged(ref currentTileset, value))
                    BorderPresent = BorderPresent;
            }
        }

        public bool BorderPresent
        {
            get => (currentTileset != "") && (selectedTile.Sheet == currentTileset);
            set => this.RaisePropertyChanged();
        }

        private int tileSize;
        public int TileSize
        {
            get => tileSize;
            set
            {
                this.RaiseAndSetIfChanged(ref tileSize, value);
                UpdateTilesList();
            }
        }






        /// <summary>
        /// The full draw data to be used as a brush. Computed.
        /// </summary>
        public TileBrush GetBrush()
        {
            TileLayer layer = new TileLayer(frameLength);
            foreach (TileFrame frame in Frames)
                layer.Frames.Add(frame);
            return new TileBrush(layer, multiSelect);
        }

        public void SetBrush(TileLayer layer)
        {
            MultiSelect = Loc.One;
            FrameLength = layer.FrameLength;
            Frames.Clear();
            foreach (TileFrame frame in layer.Frames)
                Frames.Add(frame);

            //if we eyedropped an empty, we need to add one empty here
            if (Frames.Count == 0)
                Frames.Add(TileFrame.Empty);

            SelectedTile = Frames[0];

            Animated = !(Frames.Count == 1 && FrameLength == 60);
            if (!Animated)
                Preview = SelectedTile;

            //set current tileset if applicable
            if (tileIndices.FindIndex(str => (str == SelectedTile.Sheet)) > -1)
                SelectTileset(SelectedTile.Sheet);
        }


        public void UpdateFrame()
        {
            if (!animated)
                return;

            int currentFrame = (int)(GraphicsManager.TotalFrameTick / (ulong)FrameTick.FrameToTick(FrameLength) % (ulong)Frames.Count);
            Preview = Frames[currentFrame];
        }


        public void SelectTileset(string sheetName)
        {
            Tilesets.SearchText = "";
            Tilesets.SelectedSearchIndex = tileIndices.FindIndex(str => (str == sheetName));
        }

        public void SelectTile(Loc loc, bool shiftMode, bool ctrlMode)
        {
            //normal mode:
            //no mods: choose new start
            //shift: multiselect

            //animation mode:
            //no mods: change the current frame
            //shift: multiselect
            //ctrl: add a new frame and cancel multiselect
            //ctrl+shift: add a new frame and preserve multiselect

            if (ctrlMode && Animated)
            {
                SelectedTile = new TileFrame(loc, CurrentTileset);
                if (!shiftMode)
                    MultiSelect = Loc.One;

                //add the (one) frame
                Frames.Add(SelectedTile);
                ChosenFrame = Frames.Count - 1;
            }
            else
            {
                if (shiftMode)
                {
                    //Update selected tile / multiselect
                    if (SelectedTile.Sheet != CurrentTileset)
                        SelectedTile = new TileFrame(loc, CurrentTileset);
                    else
                    {
                        Loc startTile = new Loc(Math.Min(SelectedTile.TexLoc.X, loc.X), Math.Min(SelectedTile.TexLoc.Y, loc.Y));
                        Loc endTile = new Loc(Math.Max(SelectedTile.TexLoc.X, loc.X), Math.Max(SelectedTile.TexLoc.Y, loc.Y));
                        SelectedTile = new TileFrame(startTile, CurrentTileset);
                        MultiSelect = endTile - startTile + Loc.One;
                    }

                    //update preview
                    if (!Animated)
                        Preview = SelectedTile;
                    //update the (one) frame
                    Frames[ChosenFrame] = SelectedTile;
                }
                else
                {
                    SelectedTile = new TileFrame(loc, CurrentTileset);
                    MultiSelect = Loc.One;

                    if (!Animated)
                        Preview = new TileFrame(loc, CurrentTileset);

                    //update the (one) frame
                    Frames[ChosenFrame] = SelectedTile;
                }
            }
        }

        public void UpdateTilesList()
        {
            if (Design.IsDesignMode)
                return;
            tileIndices.Clear();
            Tilesets.Clear();

            foreach (string name in GraphicsManager.TileIndex.Nodes.Keys)
            {
                if (GraphicsManager.TileIndex.GetTileSize(name) == TileSize)
                {
                    tileIndices.Add(name);
                    Tilesets.AddItem(name);
                }
            }

            //reset tile choices
            SelectedTile = TileFrame.Empty;
            MultiSelect = Loc.One;
            Preview = TileFrame.Empty;
            FrameLength = 60;
            Frames.Clear();
            Frames.Add(TileFrame.Empty);
        }


        private void Tilesets_SelectedIndexChanged()
        {
            if (Tilesets.InternalIndex > -1)
                CurrentTileset = tileIndices[Tilesets.InternalIndex];
            else
                CurrentTileset = "";
        }


        public void btnAddFrame_Click()
        {
            Frames.Add(Frames[Frames.Count - 1]);
            ChosenFrame = Frames.Count - 1;
        }


        public void btnDeleteFrame_Click()
        {
            if (Frames.Count > 1)
                Frames.RemoveAt(ChosenFrame);
        }
    }
}
