﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using RogueEssence;
using RogueEssence.Dungeon;
using RogueEssence.Ground;
using RogueEssence.Data;
using Avalonia.Controls;
using System.IO;
using RogueEssence.Dev.Views;
using RogueEssence.Content;
using RogueElements;
using RogueEssence.Script;
using System.Linq;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using System.Reflection.Emit;

namespace RogueEssence.Dev.ViewModels
{
    public class MapEditViewModel : ViewModelBase
    {
        public MapEditViewModel()
        {
            Textures = new MapTabTexturesViewModel();
            Decorations = new MapTabDecorationsViewModel();
            Terrain = new MapTabTerrainViewModel();
            Tiles = new MapTabTilesViewModel();
            Items = new MapTabItemsViewModel();
            Entities = new MapTabEntitiesViewModel();
            Entrances = new MapTabEntrancesViewModel();
            Spawns = new MapTabSpawnsViewModel();
            Effects = new MapTabEffectsViewModel();
            Properties = new MapTabPropertiesViewModel();
            CurrentFile = "";
        }

        public MapTabTexturesViewModel Textures { get; set; }
        public MapTabDecorationsViewModel Decorations { get; set; }
        public MapTabTerrainViewModel Terrain { get; set; }
        public MapTabTilesViewModel Tiles { get; set; }
        public MapTabItemsViewModel Items { get; set; }
        public MapTabEntitiesViewModel Entities { get; set; }
        public MapTabEntrancesViewModel Entrances { get; set; }
        public MapTabSpawnsViewModel Spawns { get; set; }
        public MapTabEffectsViewModel Effects { get; set; }
        public MapTabPropertiesViewModel Properties { get; set; }

        private string currentFile;
        public string CurrentFile
        {
            get => currentFile;
            set => this.SetIfChanged(ref currentFile, value);
        }

        private int selectedTabIndex;
        public int SelectedTabIndex
        {
            get => selectedTabIndex;
            set
            {
                this.SetIfChanged(ref selectedTabIndex, value);
                TabChanged();
            }
        }


        public void mnuNew_Click()
        {
            CurrentFile = "";

            lock (GameBase.lockObj) //Schedule the map creation
                DoNew();
        }

        public async void mnuOpen_Click()
        {
            string mapDir = Path.GetFullPath(PathMod.ModPath(DataManager.MAP_PATH));
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Directory = mapDir;

            FileDialogFilter filter = new FileDialogFilter();
            filter.Name = "Map Files";
            filter.Extensions.Add(DataManager.MAP_EXT.Substring(1));
            openFileDialog.Filters.Add(filter);

            DevForm form = (DevForm)DiagManager.Instance.DevEditor;

            string[] results = await openFileDialog.ShowAsync(form.MapEditForm);

            if (results != null && results.Length > 0)
            {
                bool legalPath = false;
                foreach (string proposedPath in PathMod.FallbackPaths(DataManager.MAP_PATH))
                {
                    if (comparePaths(proposedPath, Path.GetDirectoryName(results[0])))
                        legalPath = true;
                }
                if (!legalPath)
                    await MessageBox.Show(form.MapEditForm, String.Format("Map can only be loaded from:\n{0}\nOr one of its parents.", PathMod.ModPath(DataManager.MAP_PATH)), "Error", MessageBox.MessageBoxButtons.Ok);
                else
                {
                    lock (GameBase.lockObj)
                        DoLoad(Path.GetFileNameWithoutExtension(results[0]));
                }
            }
        }


        public async void mnuExportAsGround_Click()
        {
            string mapDir = Path.GetFullPath(PathMod.ModPath(DataManager.GROUND_PATH));
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Directory = mapDir;

            FileDialogFilter filter = new FileDialogFilter();
            filter.Name = "Ground Files";
            filter.Extensions.Add(DataManager.GROUND_EXT.Substring(1));
            saveFileDialog.Filters.Add(filter);

            DevForm form = (DevForm)DiagManager.Instance.DevEditor;

            string result = await saveFileDialog.ShowAsync(form.MapEditForm);

            if (!String.IsNullOrEmpty(result))
            {
                string reqDir = PathMod.HardMod(DataManager.GROUND_PATH);
                if (!comparePaths(reqDir, Path.GetDirectoryName(result)))
                    await MessageBox.Show(form.MapEditForm, String.Format("Map can only be saved to:\n{0}", reqDir), "Error", MessageBox.MessageBoxButtons.Ok);
                else if (Path.GetFileName(result).Contains(" "))
                    await MessageBox.Show(form.MapEditForm, String.Format("Save file should not contain white space:\n{0}", Path.GetFileName(result)), "Error", MessageBox.MessageBoxButtons.Ok);
                else
                {
                    lock (GameBase.lockObj)
                    {
                        //Schedule saving the map
                        ExportToGround(ZoneManager.Instance.CurrentMap, result);
                    }
                    await MessageBox.Show(form.MapEditForm, String.Format("Textures have been saved to Ground!"), "Success", MessageBox.MessageBoxButtons.Ok);
                }
            }
        }

        public async Task<bool> mnuSave_Click()
        {
            if (CurrentFile == "")
                return await mnuSaveAs_Click(); //Since its the same thing, might as well re-use the function! It makes everyone's lives easier!
            else
            {
                string reqDir = PathMod.HardMod(DataManager.MAP_PATH);
                string result = Path.Join(reqDir, Path.GetFileName(CurrentFile));
                lock (GameBase.lockObj)
                {
                    string oldFilename = CurrentFile;
                    DoSave(ZoneManager.Instance.CurrentMap, result, oldFilename);
                }
                return true;
            }
        }
        public async Task<bool> mnuSaveAs_Click()
        {
            string mapDir = Path.GetFullPath(PathMod.ModPath(DataManager.MAP_PATH));
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Directory = mapDir;

            FileDialogFilter filter = new FileDialogFilter();
            filter.Name = "Map Files";
            filter.Extensions.Add(DataManager.MAP_EXT.Substring(1));
            saveFileDialog.Filters.Add(filter);

            DevForm form = (DevForm)DiagManager.Instance.DevEditor;

            string result = await saveFileDialog.ShowAsync(form.MapEditForm);

            if (!String.IsNullOrEmpty(result))
            {
                string reqDir = PathMod.HardMod(DataManager.MAP_PATH);
                if (!comparePaths(reqDir, Path.GetDirectoryName(result)))
                    await MessageBox.Show(form.MapEditForm, String.Format("Map can only be saved to:\n{0}", reqDir), "Error", MessageBox.MessageBoxButtons.Ok);
                else if (Path.GetFileName(result).Contains(" "))
                    await MessageBox.Show(form.MapEditForm, String.Format("Save file should not contain white space:\n{0}", Path.GetFileName(result)), "Error", MessageBox.MessageBoxButtons.Ok);
                else
                {
                    lock (GameBase.lockObj)
                    {
                        string oldFilename = CurrentFile;

                        //Schedule saving the map
                        DoSave(ZoneManager.Instance.CurrentMap, result, oldFilename);
                    }
                    return true;
                }
            }
            return false;
        }

        public async void mnuTest_Click()
        {
            bool saved = await mnuSave_Click();
            if (saved)
            {
                lock (GameBase.lockObj)
                {
                    DevForm form = (DevForm)DiagManager.Instance.DevEditor;
                    form.MapEditForm.SilentClose();
                    form.MapEditForm = null;
                    GameManager.Instance.SceneOutcome = GameManager.Instance.TestWarp(ZoneManager.Instance.CurrentMap.AssetName, false, MathUtils.Rand.NextUInt64());
                }
            }
        }

        public async void mnuImportFromPng_Click()
        {
            string mapDir = Path.GetFullPath(PathMod.ModPath(DataManager.MAP_PATH));
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Directory = mapDir;

            FileDialogFilter filter = new FileDialogFilter();
            filter.Name = "PNG Files";
            filter.Extensions.Add("png");
            openFileDialog.Filters.Add(filter);

            DevForm form = (DevForm)DiagManager.Instance.DevEditor;

            string[] results = await openFileDialog.ShowAsync(form.MapEditForm);

            if (results != null && results.Length > 0)
                DoImportPng(results[0]);
        }

        public void mnuClearLayer_Click()
        {
            lock (GameBase.lockObj)
                DoClearLayer();
        }


        public async void mnuImportFromTileset_Click()
        {
            DevForm form = (DevForm)DiagManager.Instance.DevEditor;

            if (Textures.TileBrowser.CurrentTileset == "")
                await MessageBox.Show(form.MapEditForm, String.Format("No tileset to import!"), "Error", MessageBox.MessageBoxButtons.Ok);
            else
            {
                lock (GameBase.lockObj)
                    DoImportTileset(Textures.TileBrowser.CurrentTileset);
            }
        }


        public async void mnuReSize_Click()
        {
            MapResizeWindow window = new MapResizeWindow();
            MapResizeViewModel viewModel = new MapResizeViewModel(ZoneManager.Instance.CurrentMap.Width, ZoneManager.Instance.CurrentMap.Height);
            window.DataContext = viewModel;

            DevForm form = (DevForm)DiagManager.Instance.DevEditor;

            bool result = await window.ShowDialog<bool>(form.MapEditForm);

            lock (GameBase.lockObj)
            {
                if (result)
                {
                    //TODO: support undo for this
                    DiagManager.Instance.DevEditor.MapEditor.Edits.Clear();

                    DiagManager.Instance.LoadMsg = "Resizing Map...";
                    DevForm.EnterLoadPhase(GameBase.LoadPhase.Content);

                    ZoneManager.Instance.CurrentMap.ResizeJustified(viewModel.MapWidth, viewModel.MapHeight, viewModel.ResizeDir);

                    DevForm.EnterLoadPhase(GameBase.LoadPhase.Ready);
                }
            }
        }

        public void mnuUndo_Click()
        {
            if (DiagManager.Instance.DevEditor.MapEditor.Edits.CanUndo)
            {
                DiagManager.Instance.DevEditor.MapEditor.Edits.Undo();
                ProcessUndo();
            }
        }

        public void mnuRedo_Click()
        {
            if (DiagManager.Instance.DevEditor.MapEditor.Edits.CanRedo)
                DiagManager.Instance.DevEditor.MapEditor.Edits.Redo();
        }

        private void DoNew()
        {
            DiagManager.Instance.DevEditor.MapEditor.Edits.Clear();

            //take all the necessary steps before and after moving to the map
            DiagManager.Instance.LoadMsg = "Loading Map...";
            DevForm.EnterLoadPhase(GameBase.LoadPhase.Content);
            GameManager.Instance.ForceReady();

            ZoneManager.Instance.CurrentZone.DevNewMap();

            loadEditorSettings();
            DevForm.EnterLoadPhase(GameBase.LoadPhase.Ready);
        }
        private void DoLoad(string mapName)
        {
            DiagManager.Instance.DevEditor.MapEditor.Edits.Clear();

            //take all the necessary steps before and after moving to the map
            DiagManager.Instance.LoadMsg = "Loading Map...";
            DevForm.EnterLoadPhase(GameBase.LoadPhase.Content);
            GameManager.Instance.ForceReady();

            ZoneManager.Instance.CurrentZone.DevLoadMap(mapName);

            CurrentFile = PathMod.ModPath(Path.Combine(DataManager.MAP_PATH, mapName + DataManager.MAP_EXT));
            loadEditorSettings();
            DevForm.EnterLoadPhase(GameBase.LoadPhase.Ready);

        }

        public void LoadFromCurrentMap()
        {
            if (ZoneManager.Instance.CurrentMap.AssetName != "")
                CurrentFile = PathMod.ModPath(Path.Combine(DataManager.MAP_PATH, ZoneManager.Instance.CurrentMap.AssetName + DataManager.MAP_EXT));
            else
                CurrentFile = "";

            loadEditorSettings();
        }

        private void loadEditorSettings()
        {
            Textures.Layers.LoadLayers();
            Textures.TileBrowser.TileSize = GraphicsManager.TileSize;
            Textures.AutotileBrowser.TileSize = GraphicsManager.TileSize;
            Terrain.TileBrowser.TileSize = GraphicsManager.TileSize;
            Terrain.AutotileBrowser.TileSize = GraphicsManager.TileSize;

            Decorations.SelectEntity(null);
            Decorations.Layers.LoadLayers();

            Terrain.SetupLayerVisibility();
            Entrances.SetupLayerVisibility();

            Entities.SelectEntity(null);
            //Entities.Teams.LoadTeams();
            Spawns.LoadMapSpawns();
            Effects.LoadMapEffects();
            Properties.LoadMapProperties();
        }

        private void DoImportPng(string filePath)
        {
            DevForm.ExecuteOrPend(() => { tryImportPng(filePath); });

            string sheetName = Path.GetFileNameWithoutExtension(filePath);
            lock (GameBase.lockObj)
            {
                Textures.TileBrowser.UpdateTilesList();
                Terrain.TileBrowser.UpdateTilesList();
            }
            Textures.TileBrowser.SelectTileset(sheetName);
            Terrain.TileBrowser.SelectTileset(sheetName);
        }

        private void tryImportPng(string filePath)
        {
            lock (GameBase.lockObj)
            {
                string sheetName = Path.GetFileNameWithoutExtension(filePath);
                string outputFile = PathMod.HardMod(String.Format(GraphicsManager.TILE_PATTERN, sheetName));

                //load into tilesets
                using (BaseSheet tileset = BaseSheet.Import(filePath))
                {
                    List<BaseSheet[]> tileList = new List<BaseSheet[]>();
                    tileList.Add(new BaseSheet[] { tileset });
                    ImportHelper.SaveTileSheet(tileList, outputFile, GraphicsManager.TileSize);
                }

                GraphicsManager.RebuildIndices(GraphicsManager.AssetType.Tile);
                GraphicsManager.ClearCaches(GraphicsManager.AssetType.Tile);
                DevDataManager.ClearCaches();
            }
        }

        private void DoClearLayer()
        {
            //TODO: support undo for this
            DiagManager.Instance.DevEditor.MapEditor.Edits.Clear();

            DiagManager.Instance.LoadMsg = "Loading Map...";
            DevForm.EnterLoadPhase(GameBase.LoadPhase.Content);

            //set tilesets
            for (int yy = 0; yy < ZoneManager.Instance.CurrentMap.Height; yy++)
            {
                for (int xx = 0; xx < ZoneManager.Instance.CurrentMap.Width; xx++)
                    ZoneManager.Instance.CurrentMap.Layers[Textures.Layers.ChosenLayer].Tiles[xx][yy] = new AutoTile();
            }

            DevForm.EnterLoadPhase(GameBase.LoadPhase.Ready);
        }

        private void DoImportTileset(string sheetName)
        {
            //TODO: support undo for this
            DiagManager.Instance.DevEditor.MapEditor.Edits.Clear();

            Loc newSize = GraphicsManager.TileIndex.GetTileDims(sheetName);

            DiagManager.Instance.LoadMsg = "Loading Map...";
            DevForm.EnterLoadPhase(GameBase.LoadPhase.Content);

            ZoneManager.Instance.CurrentMap.ResizeJustified(newSize.X, newSize.Y, Dir8.UpLeft);

            //set tilesets
            for (int yy = 0; yy < newSize.Y; yy++)
            {
                for (int xx = 0; xx < newSize.X; xx++)
                    ZoneManager.Instance.CurrentMap.Layers[Textures.Layers.ChosenLayer].Tiles[xx][yy] = new AutoTile(new TileLayer(new Loc(xx, yy), sheetName));
            }

            DevForm.EnterLoadPhase(GameBase.LoadPhase.Ready);
        }

        private void DoSave(Map curmap, string filepath, string oldfname)
        {
            curmap.AssetName = Path.GetFileNameWithoutExtension(filepath); //Set the assetname to the file name!
            DataManager.SaveObject(curmap, filepath);

            CurrentFile = filepath;
        }

        private void ExportToGround(Map curmap, string filepath)
        {
            GroundMap curgrnd = new GroundMap();
            curgrnd.CreateNew(curmap.Width, curmap.Height, GraphicsManager.DungeonTexSize);

            curgrnd.Layers.Clear();

            foreach (MapLayer layer in curmap.Layers)
            {
                if (layer.Layer == DrawLayer.Under)
                {
                    //Add layers marked "under", using DrawLayer.Bottom
                    MapLayer newLayer = new MapLayer(layer.Name);
                    newLayer.Layer = DrawLayer.Bottom;
                    newLayer.Visible = layer.Visible;
                    newLayer.Tiles = layer.Tiles;
                    curgrnd.Layers.Add(newLayer);
                }
            }

            //Add terrain
            {
                MapLayer newLayer = new MapLayer("Terrain");
                newLayer.CreateNew(curmap.Width, curmap.Height);
                newLayer.Layer = DrawLayer.Bottom;
                newLayer.Visible = true;
                for (int xx = 0; xx < curmap.Width; xx++)
                {
                    for (int yy = 0; yy < curmap.Height; yy++)
                        newLayer.Tiles[xx][yy] = curmap.Tiles[xx][yy].Data.TileTex;
                }
                curgrnd.Layers.Add(newLayer);
            }

            //Add layers marked Bottom, using DrawLayer.Bottom
            foreach (MapLayer layer in curmap.Layers)
            {
                if (layer.Layer != DrawLayer.Under && layer.Layer != DrawLayer.Top)
                {
                    //Add layers marked "under", using DrawLayer.Bottom
                    MapLayer newLayer = new MapLayer(layer.Name);
                    newLayer.Layer = DrawLayer.Bottom;
                    newLayer.Visible = layer.Visible;
                    newLayer.Tiles = layer.Tiles;
                    curgrnd.Layers.Add(newLayer);
                }
            }

            //add layers marked top, using DrawLayer.Top
            foreach (MapLayer layer in curmap.Layers)
            {
                if (layer.Layer == DrawLayer.Top)
                {
                    //Add layers marked "Top", using DrawLayer.Top
                    MapLayer newLayer = new MapLayer(layer.Name);
                    newLayer.Layer = DrawLayer.Top;
                    newLayer.Visible = layer.Visible;
                    newLayer.Tiles = layer.Tiles;
                    curgrnd.Layers.Add(newLayer);
                }
            }

            //Add decorations
            curgrnd.Decorations.Clear();
            foreach (AnimLayer layer in curmap.Decorations)
            {
                AnimLayer newLayer = new AnimLayer(layer.Name);
                newLayer.Layer = layer.Layer;
                newLayer.Visible = layer.Visible;
                newLayer.Anims = layer.Anims;
                curgrnd.Decorations.Add(newLayer);
            }

            curgrnd.AssetName = Path.GetFileNameWithoutExtension(filepath); //Set the assetname to the file name!
            DataManager.SaveObject(curgrnd, filepath);

            //Actually create the script folder, and default script file.
            GroundEditViewModel.CreateOrCopyScriptData("", filepath);

            CurrentFile = filepath;
        }


        private static bool comparePaths(string path1, string path2)
        {
            return String.Compare(Path.GetFullPath(path1).TrimEnd('\\').TrimEnd('/'),
                Path.GetFullPath(path2).TrimEnd('\\').TrimEnd('/'),
                StringComparison.InvariantCultureIgnoreCase) == 0;
        }


        public void TabChanged()
        {
            switch (selectedTabIndex)
            {
                case 0://Textures
                    DungeonEditScene.Instance.EditMode = DungeonEditScene.EditorMode.Texture;
                    break;
                case 1://Decorations
                    DungeonEditScene.Instance.EditMode = DungeonEditScene.EditorMode.Decoration;
                    Decorations.TabbedIn();
                    break;
                case 2://Terrain
                    DungeonEditScene.Instance.EditMode = DungeonEditScene.EditorMode.Terrain;
                    break;
                case 3://Tiles
                    DungeonEditScene.Instance.EditMode = DungeonEditScene.EditorMode.Tile;
                    break;
                case 4://Items
                    DungeonEditScene.Instance.EditMode = DungeonEditScene.EditorMode.Item;
                    Items.TabbedIn();
                    break;
                case 5://Entities
                    DungeonEditScene.Instance.EditMode = DungeonEditScene.EditorMode.Entity;
                    Entities.TabbedIn();
                    break;
                case 6://Entrances
                    DungeonEditScene.Instance.EditMode = DungeonEditScene.EditorMode.Entrance;
                    break;
                default:
                    DungeonEditScene.Instance.EditMode = DungeonEditScene.EditorMode.Other;
                    break;
            }
            if (selectedTabIndex != 1)
                Decorations.TabbedOut();
            if (selectedTabIndex != 4)
                Items.TabbedOut();
            if (selectedTabIndex != 5)
                Entities.TabbedOut();
        }

        public void ProcessInput(InputManager input)
        {
            lock (GameBase.lockObj)
            {
                if (input.BaseKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl))
                {
                    if (input.BaseKeyPressed(Microsoft.Xna.Framework.Input.Keys.Z) && input.BaseKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift)
                        || input.BaseKeyPressed(Microsoft.Xna.Framework.Input.Keys.Y))
                    {
                        mnuRedo_Click();
                        return;
                    }
                    else if (input.BaseKeyPressed(Microsoft.Xna.Framework.Input.Keys.Z))
                    {
                        mnuUndo_Click();
                        return;
                    }
                }
                switch (selectedTabIndex)
                {
                    case 0://Textures
                        Textures.ProcessInput(input);
                        break;
                    case 1://Decorations
                        Decorations.ProcessInput(input);
                        break;
                    case 2://Terrain
                        Terrain.ProcessInput(input);
                        break;
                    case 3://Tiles
                        Tiles.ProcessInput(input);
                        break;
                    case 4://Items
                        Items.ProcessInput(input);
                        break;
                    case 5://Entities
                        Entities.ProcessInput(input);
                        break;
                    case 6://Entrances
                        Entrances.ProcessInput(input);
                        break;
                }
            }
        }

        public void ProcessUndo()
        {
            lock (GameBase.lockObj)
            {
                switch (selectedTabIndex)
                {
                    case 3://Items
                        Items.ProcessUndo();
                        break;
                    case 4://Entities
                        Entities.ProcessUndo();
                        break;
                    case 5://Entrances
                        Entrances.ProcessUndo();
                        break;
                }
            }
        }
    }
}
