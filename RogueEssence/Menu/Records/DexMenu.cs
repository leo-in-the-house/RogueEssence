﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RogueEssence.Content;
using RogueEssence.Dungeon;
using RogueElements;
using RogueEssence.Data;

namespace RogueEssence.Menu
{
    public class DexMenu : MultiPageMenu
    {
        private const int SLOTS_PER_PAGE = 14;

        SummaryMenu summaryMenu;
        SpeakerPortrait portrait;
        List<string> obtainableKeys;

        public DexMenu() : this(MenuLabel.DEX_MENU) { }
        public DexMenu(string label)
        {
            Label = label;
            int lastEntry = -1;
            int seen = 0;
            int befriended = 0;

            List<string> numericKeys = DataManager.Instance.DataIndices[DataManager.DataType.Monster].GetMappedKeys();

            for (int ii = 0; ii < numericKeys.Count; ii++)
            {
                if (numericKeys[ii] == null)
                    continue;
                if (numericKeys[ii] == DataManager.Instance.DefaultMonster)
                    continue;

                if (DataManager.Instance.Save.GetMonsterUnlock(numericKeys[ii]) > GameProgress.UnlockState.None)
                {
                    lastEntry = ii;
                    seen++;
                    if (DataManager.Instance.Save.GetMonsterUnlock(numericKeys[ii]) == GameProgress.UnlockState.Completed)
                        befriended++;
                }
            }

            obtainableKeys = new List<string>();
            List<MenuChoice> flatChoices = new List<MenuChoice>();
            for (int ii = 0; ii <= lastEntry; ii++)
            {
                if (numericKeys[ii] == null)
                    continue;
                if (numericKeys[ii] == DataManager.Instance.DefaultMonster)
                    continue;

                GameProgress.UnlockState unlock = DataManager.Instance.Save.GetMonsterUnlock(numericKeys[ii]);
                if (unlock > GameProgress.UnlockState.None)
                {
                    Color color = (unlock == GameProgress.UnlockState.Completed) ? Color.White : Color.Gray;

                    //name
                    MenuText dexNum = new MenuText(ii.ToString("D3"), new Loc(2, 1), color);
                    MenuText dexName = new MenuText(DataManager.Instance.DataIndices[DataManager.DataType.Monster].Get(numericKeys[ii]).Name.ToLocal(), new Loc(24, 1), color);
                    flatChoices.Add(new MenuElementChoice(() => { choose(ii); }, true, dexNum, dexName));
                    obtainableKeys.Add(numericKeys[ii]);
                }
                else if (DataManager.Instance.DataIndices[DataManager.DataType.Monster].Get(numericKeys[ii]).Released)
                {
                    //???
                    MenuText dexNum = new MenuText(ii.ToString("D3"), new Loc(2, 1), Color.Gray);
                    MenuText dexName = new MenuText("???", new Loc(24, 1), Color.Gray);
                    flatChoices.Add(new MenuElementChoice(() => { choose(ii); }, true, dexNum, dexName));
                    obtainableKeys.Add(numericKeys[ii]);
                }
                else
                {
                    // do not add to the final choice list
                }
            }
            IChoosable[][] choices = SortIntoPages(flatChoices.ToArray(), SLOTS_PER_PAGE);

            summaryMenu = new SummaryMenu(new Rect(new Loc(208, 16), new Loc(96, LINE_HEIGHT * 2 + GraphicsManager.MenuBG.TileHeight * 2)));
            MenuText seenText = new MenuText(Text.FormatKey("MENU_DEX_SEEN", seen), new Loc(GraphicsManager.MenuBG.TileWidth * 2, GraphicsManager.MenuBG.TileHeight));
            summaryMenu.Elements.Add(seenText);
            MenuText befriendedText = new MenuText(Text.FormatKey("MENU_DEX_CAUGHT", befriended), new Loc(GraphicsManager.MenuBG.TileWidth * 2, GraphicsManager.MenuBG.TileHeight + LINE_HEIGHT));
            summaryMenu.Elements.Add(befriendedText);

            portrait = new SpeakerPortrait(MonsterID.Invalid, new EmoteStyle(0), new Loc(232, 72), true);

            Initialize(new Loc(0, 0), 208, Text.FormatKey("MENU_DEX_TITLE"), choices, 0, 0, SLOTS_PER_PAGE);
        }


        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!Visible)
                return;
            base.Draw(spriteBatch);
            
            summaryMenu.Draw(spriteBatch);

            if (!String.IsNullOrEmpty(portrait.Speaker.Species))
                portrait.Draw(spriteBatch, new Loc());

        }

        protected override void ChoiceChanged()
        {
            if (DataManager.Instance.Save.GetMonsterUnlock(obtainableKeys[CurrentChoiceTotal]) > GameProgress.UnlockState.None)
                portrait.Speaker = new MonsterID(obtainableKeys[CurrentChoiceTotal], 0, DataManager.Instance.DefaultSkin, Gender.Unknown);
            else
                portrait.Speaker = MonsterID.Invalid;
            base.ChoiceChanged();
        }

        private void choose(int species)
        {
            
        }

    }
}
