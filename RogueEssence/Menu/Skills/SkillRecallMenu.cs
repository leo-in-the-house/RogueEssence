﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using RogueEssence.Content;
using RogueElements;
using RogueEssence.Dungeon;

namespace RogueEssence.Menu
{
    public class SkillRecallMenu : MultiPageMenu
    {
        private const int SLOTS_PER_PAGE = 5;

        string[] forgottenSkills;
        OnChooseSlot chooseSlotAction;
        Action refuseAction;
        Character player;

        SkillSummary summaryMenu;

        public SkillRecallMenu(Character player, string[] forgottenSkills, OnChooseSlot action, Action refuseAction) :
            this(MenuLabel.SKILL_RECALL_MENU, player, forgottenSkills, action, refuseAction) { }
        public SkillRecallMenu(string label, Character player, string[] forgottenSkills, OnChooseSlot action, Action refuseAction)
        {
            Label = label;
            int menuWidth = 152;
            this.player = player;
            this.forgottenSkills = forgottenSkills;
            this.chooseSlotAction = action;
            this.refuseAction = refuseAction;

            List<MenuChoice> flatChoices = new List<MenuChoice>();
            for (int ii = 0; ii < forgottenSkills.Length; ii++)
            {
                Data.SkillData skillEntry = Data.DataManager.Instance.GetSkill(forgottenSkills[ii]);
                string newSkillString = skillEntry.GetColoredName();
                int maxCharges = skillEntry.BaseCharges + player.ChargeBoost;
                string newSkillCharges = maxCharges + "/" + maxCharges;
                int index = ii;
                MenuText newMenuText = new MenuText(newSkillString, new Loc(2, 1));
                MenuText newMenuCharges = new MenuText(newSkillCharges, new Loc(menuWidth - 8 * 4, 1), DirH.Right);
                flatChoices.Add(new MenuElementChoice(() => { choose(index); }, true, newMenuText, newMenuCharges));
            }
            IChoosable[][] char_skills = SortIntoPages(flatChoices.ToArray(), SLOTS_PER_PAGE);

            summaryMenu = new SkillSummary(Rect.FromPoints(new Loc(16,
                GraphicsManager.ScreenHeight - 8 - GraphicsManager.MenuBG.TileHeight * 2 - LINE_HEIGHT * 2 - VERT_SPACE * 4),
                new Loc(GraphicsManager.ScreenWidth - 16, GraphicsManager.ScreenHeight - 8)));

            Initialize(new Loc(16, 16), menuWidth, Text.FormatKey("MENU_SKILL_RECALL"), char_skills, 0, 0, SLOTS_PER_PAGE);
        }

        protected override void MenuPressed()
        {

        }

        protected override void Canceled()
        {
            MenuManager.Instance.RemoveMenu();
            refuseAction();
        }

        private void choose(int choice)
        {
            MenuManager.Instance.RemoveMenu();

            chooseSlotAction(choice);
        }

        protected override void ChoiceChanged()
        {
            summaryMenu.SetSkill(forgottenSkills[CurrentChoiceTotal]);
            
            base.ChoiceChanged();
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
