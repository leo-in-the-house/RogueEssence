﻿using System.Collections.Generic;
using RogueElements;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RogueEssence.Content;
using RogueEssence.Dungeon;
using RogueEssence.Data;
using RogueEssence.Ground;
using System;

namespace RogueEssence.Menu
{
    public class TeachMenu : TitledStripMenu
    {
        SummaryMenu summaryMenu;
        MenuText SummaryTitle;
        MenuText[] Skills;
        MenuText[] SkillCharges;

        private int slot;
        private bool held;
        private int commandIdx;

        public TeachMenu(int slot, bool held, int commandIdx) : this(MenuLabel.TEACH_MENU, slot, held, commandIdx) { }
        public TeachMenu(string label, int slot, bool held, int commandIdx)
        {
            Label = label;
            this.slot = slot;
            this.held = held;
            this.commandIdx = commandIdx;
            int width = 144;

            List<MenuTextChoice> team = new List<MenuTextChoice>();
            foreach (Character character in DataManager.Instance.Save.ActiveTeam.Players)
            {
                bool canLearn = CanLearnSkill(character, DataManager.Instance.Save.ActiveTeam.Leader, slot, held, commandIdx) && !character.Dead;
                int teamIndex = team.Count;
                team.Add(new MenuTextChoice(character.GetDisplayName(true), () => { choose(teamIndex); }, canLearn, canLearn ? Color.White : Color.Red));
            }

            Loc summaryStart = new Loc(16, 16 + team.Count * VERT_SPACE + GraphicsManager.MenuBG.TileHeight * 2 + ContentOffset);
            summaryMenu = new SummaryMenu(new Rect(summaryStart, new Loc(144, CharData.MAX_SKILL_SLOTS * VERT_SPACE + GraphicsManager.MenuBG.TileHeight * 2 + ContentOffset)));

            SummaryTitle = new MenuText("", new Loc(GraphicsManager.MenuBG.TileWidth + 8, GraphicsManager.MenuBG.TileHeight));
            summaryMenu.Elements.Add(SummaryTitle);
            summaryMenu.Elements.Add(new MenuDivider(new Loc(GraphicsManager.MenuBG.TileWidth, GraphicsManager.MenuBG.TileHeight + LINE_HEIGHT), width - GraphicsManager.MenuBG.TileWidth * 2));
            Skills = new MenuText[CharData.MAX_SKILL_SLOTS];
            SkillCharges = new MenuText[CharData.MAX_SKILL_SLOTS];
            for (int ii = 0; ii < Skills.Length; ii++)
            {
                Skills[ii] = new MenuText("", new Loc(GraphicsManager.MenuBG.TileWidth + 8, GraphicsManager.MenuBG.TileHeight + ContentOffset + VERT_SPACE * ii));
                summaryMenu.Elements.Add(Skills[ii]);
                SkillCharges[ii] = new MenuText("", new Loc(summaryMenu.Bounds.Width - GraphicsManager.MenuBG.TileWidth, GraphicsManager.MenuBG.TileHeight + ContentOffset + VERT_SPACE * ii), DirH.Right);
                summaryMenu.Elements.Add(SkillCharges[ii]);
            }

            Initialize(new Loc(16, 16), width, Text.FormatKey("MENU_TEACH_TITLE"), team.ToArray(), 0);
        }

        public static bool CanLearnSkill(Character character, Character user, int slot, bool held, int commandIdx = -1)
        {
            BaseMonsterForm entry = DataManager.Instance.GetMonster(character.BaseForm.Species).Forms[character.BaseForm.Form];
            string itemNum = "";
            if (slot == BattleContext.FLOOR_ITEM_SLOT)
            {
                //item on the ground
                int mapSlot = ZoneManager.Instance.CurrentMap.GetItem(user.CharLoc);
                MapItem mapItem = ZoneManager.Instance.CurrentMap.Items[mapSlot];
                itemNum = mapItem.Value;
            }
            else
            {
                if (held)
                    itemNum = DataManager.Instance.Save.ActiveTeam.Players[slot].EquippedItem.ID;
                else
                    itemNum = DataManager.Instance.Save.ActiveTeam.GetInv(slot).ID;
            }
                
            ItemData itemData = DataManager.Instance.GetItem(itemNum);
            string moveNum;
            
            if (GameManager.Instance.CurrentScene == GroundScene.Instance && commandIdx > -1)
            {
                LearnItemEvent learnEvent = (LearnItemEvent)itemData.GroundUseActions[commandIdx];
                moveNum = learnEvent.Skill;
            }
            else
            {
                ItemIDState effect = itemData.ItemStates.GetWithDefault<ItemIDState>();
                moveNum = effect.ID;
            }
            
            //check for already knowing the skill
            for(int ii = 0; ii < character.BaseSkills.Count; ii++)
            {
                if (character.BaseSkills[ii].SkillNum == moveNum)
                    return false;
            }

            if (!DataManager.Instance.DataIndices[DataManager.DataType.Skill].Get(moveNum).Released)
                return false;

            return entry.CanLearnSkill(moveNum);
        }

        protected override void ChoiceChanged()
        {
            Character character = DataManager.Instance.Save.ActiveTeam.Players[CurrentChoice];
            SummaryTitle.SetText(Text.FormatKey("MENU_SKILLS_TITLE", character.GetDisplayName(true)));
            for (int ii = 0; ii < Skills.Length; ii++)
            {
                if (!String.IsNullOrEmpty(character.BaseSkills[ii].SkillNum))
                {
                    SkillData data = DataManager.Instance.GetSkill(character.BaseSkills[ii].SkillNum);
                    Skills[ii].SetText(data.GetColoredName());
                    SkillCharges[ii].SetText(character.BaseSkills[ii].Charges + "/" + (data.BaseCharges + character.ChargeBoost));
                }
                else
                {
                    Skills[ii].SetText("");
                    SkillCharges[ii].SetText("");
                }
            }
            base.ChoiceChanged();
        }

        private int getItemUseSlot()
        {
            if (held)
                return BattleContext.EQUIP_ITEM_SLOT;
            else
                return slot;
        }
        
        private void choose(int choice)
        {
            MenuManager.Instance.ClearMenus();
            //give the item at the inv slot to the given team slot

            if (GameManager.Instance.CurrentScene == GroundScene.Instance)
                MenuManager.Instance.EndAction = GroundScene.Instance.ProcessInput(new GameAction(GameAction.ActionType.UseItem, Dir8.None, getItemUseSlot(), choice, commandIdx));
            else if (GameManager.Instance.CurrentScene == DungeonScene.Instance)
                MenuManager.Instance.EndAction = DungeonScene.Instance.ProcessPlayerInput(new GameAction(GameAction.ActionType.UseItem, Dir8.None, getItemUseSlot(), choice));
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!Visible)
                return;
            base.Draw(spriteBatch);

            //draw other windows
            summaryMenu.Draw(spriteBatch);
        }
    }
}
