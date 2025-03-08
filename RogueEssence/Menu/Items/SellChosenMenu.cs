﻿using System.Collections.Generic;
using RogueElements;
using RogueEssence.Dungeon;
using RogueEssence.Data;
using System;
using RogueEssence.Content;

namespace RogueEssence.Menu
{
    public class SellChosenMenu : SingleStripMenu
    {

        private int origIndex;
        private List<InvSlot> selections;
        private SellMenu.OnChooseSlots action;

        public SellChosenMenu(List<InvSlot> selections, int origIndex, SellMenu.OnChooseSlots chooseSlots) :
            this(MenuLabel.SELL_CHOSEN_MENU, selections, origIndex, chooseSlots) { }
        public SellChosenMenu(string label, List<InvSlot> selections, int origIndex, SellMenu.OnChooseSlots chooseSlots)
        {
            Label = label;
            this.origIndex = origIndex;
            this.selections = selections;
            action = chooseSlots;

            List<MenuTextChoice> choices = new List<MenuTextChoice>();
            choices.Add(new MenuTextChoice(Text.FormatKey("MENU_SHOP_SELL"), SellAction));

            if (selections.Count == 1)
            {
                InvItem invItem = null;
                if (selections[0].IsEquipped)
                    invItem = DataManager.Instance.Save.ActiveTeam.Players[selections[0].Slot].EquippedItem;
                else
                    invItem = DataManager.Instance.Save.ActiveTeam.GetInv(selections[0].Slot);
                ItemData entry = DataManager.Instance.GetItem(invItem.ID);

                if (entry.UsageType == ItemData.UseType.Learn)
                    choices.Add(new MenuTextChoice(Text.FormatKey("MENU_INFO"), InfoAction));
            }

            choices.Add(new MenuTextChoice(Text.FormatKey("MENU_EXIT"), ExitAction));

            int choice_width = CalculateChoiceLength(choices, 72);
            Initialize(new Loc(Math.Min(ItemMenu.ITEM_MENU_WIDTH + 16, GraphicsManager.ScreenWidth - choice_width), 16), choice_width, choices.ToArray(), 0);
        }

        private void SellAction()
        {
            //remove both this and parent menu
            MenuManager.Instance.RemoveMenu();
            MenuManager.Instance.RemoveMenu();

            action(selections);
        }

        private void InfoAction()
        {
            if (selections[0].IsEquipped)
                MenuManager.Instance.AddMenu(new TeachInfoMenu(DataManager.Instance.Save.ActiveTeam.Players[selections[0].Slot].EquippedItem.ID), false);
            else
                MenuManager.Instance.AddMenu(new TeachInfoMenu(DataManager.Instance.Save.ActiveTeam.GetInv(selections[0].Slot).ID), false);
        }

        private void ExitAction()
        {
            MenuManager.Instance.RemoveMenu();
        }
        
    }
}
