﻿using System.Collections.Generic;
using RogueElements;
using Microsoft.Xna.Framework;
using RogueEssence.Content;
using RogueEssence.Data;

namespace RogueEssence.Menu
{
    public class ScoreMenu : SideScrollMenu
    {
        public MenuText Title;
        public MenuDivider Div;
        public MenuText[] Scores;
        private Dictionary<string, List<RecordHeaderData>> scoreDict;
        private string chosenZone;
        private string highlightedPath;

        public ScoreMenu(Dictionary<string, List<RecordHeaderData>> scoreDict, string chosenZone, string highlightedPath) :
            this(MenuLabel.SCORE_MENU, scoreDict, chosenZone, highlightedPath) { }
        public ScoreMenu(string label, Dictionary<string, List<RecordHeaderData>> scoreDict, string chosenZone, string highlightedPath)
        {
            Label = label;
            this.scoreDict = scoreDict;
            this.chosenZone = chosenZone;
            this.highlightedPath = highlightedPath;
            List<RecordHeaderData> scores = scoreDict[chosenZone];

            Bounds = Rect.FromPoints(new Loc(GraphicsManager.ScreenWidth / 2 - 128, 16), new Loc(GraphicsManager.ScreenWidth / 2 + 128, 224));

            string zoneName = DataManager.Instance.DataIndices[DataManager.DataType.Zone].Get(chosenZone).GetColoredName();
            Title = new MenuText(Text.FormatKey("MENU_SCORES_TITLE") + ": " + zoneName, new Loc(GraphicsManager.MenuBG.TileWidth + 8, GraphicsManager.MenuBG.TileHeight));
            Div = new MenuDivider(new Loc(GraphicsManager.MenuBG.TileWidth, GraphicsManager.MenuBG.TileHeight + LINE_HEIGHT), Bounds.Width - GraphicsManager.MenuBG.TileWidth * 2);

            Scores = new MenuText[scores.Count * 3];
            for (int ii = 0; ii < scores.Count; ii++)
            {
                Color color = (scores[ii].Path == highlightedPath) ? Color.Yellow : Color.White;
                Scores[ii * 3] = new MenuText((ii + 1) + ". ",
                new Loc(GraphicsManager.MenuBG.TileWidth * 2 + 12, GraphicsManager.MenuBG.TileHeight + VERT_SPACE * ii + TitledStripMenu.TITLE_OFFSET), DirV.Up, DirH.Right, color);
                Scores[ii * 3 + 1] = new MenuText(scores[ii].Name,
                new Loc(GraphicsManager.MenuBG.TileWidth * 2 + 12, GraphicsManager.MenuBG.TileHeight + VERT_SPACE * ii + TitledStripMenu.TITLE_OFFSET), color);
                Scores[ii * 3 + 2] = new MenuText(scores[ii].Score.ToString(),
                new Loc(Bounds.Width - GraphicsManager.MenuBG.TileWidth * 2, GraphicsManager.MenuBG.TileHeight + VERT_SPACE * ii + TitledStripMenu.TITLE_OFFSET), DirV.Up, DirH.Right, color);
            }

            base.Initialize();
        }

        protected override IEnumerable<IMenuElement> GetDrawElements()
        {
            yield return Title;
            yield return Div;
            foreach (MenuText score in Scores)
                yield return score;
        }

        public override void Update(InputManager input)
        {
            Visible = true;
            if (input.JustPressed(FrameInput.InputType.Menu) || input.JustPressed(FrameInput.InputType.Confirm)
                || input.JustPressed(FrameInput.InputType.Cancel))
            {
                GameManager.Instance.SE("Menu/Confirm");
                MenuManager.Instance.RemoveMenu();
            }
            else if (IsInputting(input, Dir8.Left))
            {
                GameManager.Instance.SE("Menu/Skip");
                string newZone = chosenZone;
                int curIndex = 0;
                List<string> asset_names = new List<string>();
                foreach (string key in DataManager.Instance.DataIndices[DataManager.DataType.Zone].GetOrderedKeys(true))
                {
                    if (newZone == key)
                        curIndex = asset_names.Count;
                    asset_names.Add(key);
                }

                do
                {
                    curIndex = (curIndex + asset_names.Count-1) % asset_names.Count;
                    newZone = asset_names[curIndex];
                }
                while (!scoreDict.ContainsKey(newZone));
                MenuManager.Instance.ReplaceMenu(new ScoreMenu(scoreDict, newZone, highlightedPath));
            }
            else if (IsInputting(input, Dir8.Right))
            {
                GameManager.Instance.SE("Menu/Skip");
                string newZone = chosenZone;
                int curIndex = 0;
                List<string> asset_names = new List<string>();
                foreach (string key in DataManager.Instance.DataIndices[DataManager.DataType.Zone].GetOrderedKeys(true))
                {
                    if (newZone == key)
                        curIndex = asset_names.Count;
                    asset_names.Add(key);
                }

                do
                {
                    curIndex = (curIndex + 1) % asset_names.Count;
                    newZone = asset_names[curIndex];
                }
                while (!scoreDict.ContainsKey(newZone));
                MenuManager.Instance.ReplaceMenu(new ScoreMenu(scoreDict, newZone, highlightedPath));
            }
        }
    }
}
