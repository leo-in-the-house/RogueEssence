﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RogueEssence.Content;
using RogueElements;
using RogueEssence.Dungeon;
using RogueEssence.Data;
using RogueEssence.Ground;

namespace RogueEssence.Menu
{
    public class AppraiseMenu : MultiPageMenu
    {
        public const int APPRAISE_MENU_WIDTH = 176;
        public delegate void OnChooseSlots(List<InvSlot> slots);
        private const int SLOTS_PER_PAGE = 8;

        ItemSummary summaryMenu;
        MoneySummary moneySummary;
        private OnChooseSlots action;


        public AppraiseMenu(int defaultChoice, OnChooseSlots chooseSlots) : this(MenuLabel.APPRAISE_MENU, defaultChoice, chooseSlots) { }
        public AppraiseMenu(string label, int defaultChoice, OnChooseSlots chooseSlots)
        {
            Label = label;
            action = chooseSlots;

            List<MenuChoice> flatChoices = new List<MenuChoice>();
            for (int ii = 0; ii < DataManager.Instance.Save.ActiveTeam.Players.Count; ii++)
            {
                Character activeChar = DataManager.Instance.Save.ActiveTeam.Players[ii];
                int index = ii;
                if (!String.IsNullOrEmpty(activeChar.EquippedItem.ID))
                {
                    bool enabled = DataManager.Instance.GetItem(activeChar.EquippedItem.ID).UsageType == ItemData.UseType.Box;
                    flatChoices.Add(new MenuTextChoice((index + 1).ToString() + ": " + activeChar.EquippedItem.GetDisplayName(), () => { choose(new InvSlot(true, index)); }, enabled, enabled ? Color.White : Color.Red));
                }
            }
            for (int ii = 0; ii < DataManager.Instance.Save.ActiveTeam.GetInvCount(); ii++)
            {
                int index = ii;

                bool enabled = DataManager.Instance.GetItem(DataManager.Instance.Save.ActiveTeam.GetInv(index).ID).UsageType == ItemData.UseType.Box;
                flatChoices.Add(new MenuTextChoice(DataManager.Instance.Save.ActiveTeam.GetInv(index).GetDisplayName(), () => { choose(new InvSlot(false, index)); }, enabled, enabled ? Color.White : Color.Red));
            }
            defaultChoice = Math.Min(defaultChoice, flatChoices.Count - 1);
            int startChoice = defaultChoice % SLOTS_PER_PAGE;
            int startPage = defaultChoice / SLOTS_PER_PAGE;
            IChoosable[][] inv = SortIntoPages(flatChoices.ToArray(), SLOTS_PER_PAGE);


            summaryMenu = new ItemSummary(Rect.FromPoints(new Loc(16, GraphicsManager.ScreenHeight - 8 - 4 * VERT_SPACE - GraphicsManager.MenuBG.TileHeight * 2),
                new Loc(GraphicsManager.ScreenWidth - 16, GraphicsManager.ScreenHeight - 8)));

            moneySummary = new MoneySummary(Rect.FromPoints(new Loc(16 + ItemMenu.ITEM_MENU_WIDTH, summaryMenu.Bounds.Top - LINE_HEIGHT * 2 - GraphicsManager.MenuBG.TileHeight * 2),
                new Loc(GraphicsManager.ScreenWidth - 16, summaryMenu.Bounds.Top)));

            Initialize(new Loc(16, 16), APPRAISE_MENU_WIDTH, Text.FormatKey("MENU_ITEM_TITLE"), inv, startChoice, startPage, SLOTS_PER_PAGE, false, flatChoices.Count);

        }

        private void choose(InvSlot slot)
        {
            int startIndex = CurrentChoiceTotal;

            MenuManager.Instance.RemoveMenu();
            action(new List<InvSlot>() { slot });
        }


        protected override void ChoseMultiIndex(List<int> slots)
        {
            int startIndex = CurrentChoiceTotal;

            List<int> heldIDs = new List<int>();
            for (int ii = 0; ii < DataManager.Instance.Save.ActiveTeam.Players.Count; ii++)
            {
                Character activeChar = DataManager.Instance.Save.ActiveTeam.Players[ii];
                if (!String.IsNullOrEmpty(activeChar.EquippedItem.ID))
                    heldIDs.Add(ii);
            }

            List<InvSlot> invSlots = new List<InvSlot>();
            for (int ii = 0; ii < slots.Count; ii++)
            {
                if (slots[ii] < heldIDs.Count)
                    invSlots.Add(new InvSlot(true, heldIDs[slots[ii]]));
                else
                    invSlots.Add(new InvSlot(false, slots[ii] - heldIDs.Count));
            }

            MenuManager.Instance.RemoveMenu();
            action(invSlots);
        }

        private int getMaxInvPages()
        {
            if (DataManager.Instance.Save.ActiveTeam.GetInvCount() == 0)
                return 0;
            return MathUtils.DivUp(DataManager.Instance.Save.ActiveTeam.GetInvCount(), SLOTS_PER_PAGE);
        }

        protected override void ChoiceChanged()
        {
            InvItem item = getChosenItemID(CurrentChoiceTotal);
            summaryMenu.SetItem(item);
            base.ChoiceChanged();
        }

        private InvItem getChosenItemID(int menuIndex)
        {
            if (menuIndex == -1)
                return null;

            int countedEquip = 0;
            for (int ii = 0; ii < DataManager.Instance.Save.ActiveTeam.Players.Count; ii++)
            {
                Character activeChar = DataManager.Instance.Save.ActiveTeam.Players[ii];
                if (!String.IsNullOrEmpty(activeChar.EquippedItem.ID))
                {
                    if (countedEquip == menuIndex)
                        return activeChar.EquippedItem;
                    countedEquip++;
                }
            }
            menuIndex -= countedEquip;
            return DataManager.Instance.Save.ActiveTeam.GetInv(menuIndex);
        }

        protected override void UpdateKeys(InputManager input)
        {
            if (input.JustPressed(FrameInput.InputType.SortItems))
            {
                GameManager.Instance.SE("Menu/Sort");
                MenuManager.Instance.NextAction = SortCommand();
            }
            else
                base.UpdateKeys(input);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!Visible)
                return;
            base.Draw(spriteBatch);

            //draw other windows
            summaryMenu.Draw(spriteBatch);
            moneySummary.Draw(spriteBatch);
        }

        public IEnumerator<YieldInstruction> SortCommand()
        {
            //generate list of selected items
            List<int> equip_selected = new List<int>();
            List<int> selected = new List<int>();
            int eqIndex = 0;
            for (int ii = 0; ii < DataManager.Instance.Save.ActiveTeam.Players.Count; ii++)
            {
                Character activeChar = DataManager.Instance.Save.ActiveTeam.Players[ii];
                if (!String.IsNullOrEmpty(activeChar.EquippedItem.ID))
                {
                    if (GetTotalChoiceAtIndex(eqIndex).Selected)
                        equip_selected.Add(eqIndex);
                    eqIndex++;
                }
            }
            for (int ii = 0; ii < DataManager.Instance.Save.ActiveTeam.GetInvCount(); ii++)
            {
                if (GetTotalChoiceAtIndex(eqIndex + ii).Selected)
                    selected.Add(ii);
            }

            // get mapping
            Dictionary<int, int> mapping = DataManager.Instance.Save.ActiveTeam.GetSortMapping(true);

            // reorder the inventory
            yield return CoroutineManager.Instance.StartCoroutine(GroundScene.Instance.ProcessInput(new GameAction(GameAction.ActionType.SortItems, Dir8.None)));
            // create the new menu
            AppraiseMenu menu = new AppraiseMenu(CurrentChoiceTotal, action);

            // reselect equip slots
            for (int ii = 0; ii < equip_selected.Count; ii++)
            {
                int slot = equip_selected[ii];
                ((MenuChoice)menu.GetTotalChoiceAtIndex(slot)).SilentSelect(true);
            }
            // reselect the rest of the inventory
            for (int ii = selected.Count - 1; ii >= 0; ii--)
            {
                int oldPos = selected[ii];
                int newPos = mapping[oldPos];
                int index = newPos + eqIndex;
                ((MenuChoice)menu.GetTotalChoiceAtIndex(index)).SilentSelect(true);
            }

            //replace the menu
            MenuManager.Instance.ReplaceMenu(menu);
        }
    }
}
