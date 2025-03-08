﻿using System.Collections.Generic;
using System.IO;
using RogueElements;
using RogueEssence.Data;
using RogueEssence.Dungeon;
using RogueEssence.Script;
using SDL2;
using System;
using RogueEssence.Content;


namespace RogueEssence.Menu
{
    public class ReplayChosenMenu : SingleStripMenu
    {

        private string recordDir;

        public ReplayChosenMenu(string dir) : this(MenuLabel.REPLAY_CHOSEN_MENU, dir) { }
        public ReplayChosenMenu(string label, string dir)
        {
            Label = label;
            this.recordDir = dir;

            List<MenuTextChoice> choices = new List<MenuTextChoice>();
            
            choices.Add(new MenuTextChoice(Text.FormatKey("MENU_INFO"), SummaryAction));
            choices.Add(new MenuTextChoice(Text.FormatKey("MENU_REPLAY_REPLAY"), ReplayAction));
            if (DiagManager.Instance.DevMode)
                choices.Add(new MenuTextChoice(Text.FormatKey("MENU_REPLAY_VERIFY"), VerifyAction));
            choices.Add(new MenuTextChoice(Text.FormatKey("MENU_REPLAY_SEED"), SeedAction));

            if (DataManager.Instance.GetRecordHeader(recordDir).IsFavorite)
            {
                choices.Add(new MenuTextChoice(Text.FormatKey("MENU_FAVORITE_OFF"), UnFavoriteAction));
            }
            else
            {
                choices.Add(new MenuTextChoice(Text.FormatKey("MENU_FAVORITE"), FavoriteAction));
            }

            choices.Add(new MenuTextChoice(Text.FormatKey("MENU_DELETE"), DeleteAction));
            choices.Add(new MenuTextChoice(Text.FormatKey("MENU_EXIT"), ExitAction));

            int choiceLength = CalculateChoiceLength(choices, 72);
            Initialize(new Loc(Math.Min(224, GraphicsManager.ScreenWidth - choiceLength), 0), choiceLength, choices.ToArray(), 0);
        }


        private void cannotRead()
        {
            MenuManager.Instance.AddMenu(MenuManager.Instance.CreateDialogue(Text.FormatKey("DLG_ERR_READ_FILE"),
                Text.FormatKey("DLG_ERR_READ_FILE_FALLBACK", recordDir)), false);
        }

        private void SummaryAction()
        {
            GameProgress ending = DataManager.Instance.GetRecord(recordDir);
            if (ending == null)
                cannotRead();
            else
                MenuManager.Instance.AddMenu(new FinalResultsMenu(ending), false);
        }

        private void ReplayAction()
        {
            ReplayData replay = DataManager.Instance.LoadReplay(recordDir, false);
            if (replay == null)
                cannotRead();
            else
            {
                List<ModDiff> modDiffs = replay.States[0].Save.GetModDiffs();
                if (modDiffs.Count > 0)
                    DiagManager.Instance.LogInfo("Loading with version diffs:");

                if (modDiffs.Count > 0)
                {
                    DialogueChoice[] choices = new DialogueChoice[2];
                    choices[0] = new DialogueChoice(Text.FormatKey("DLG_CHOICE_YES"), () => { attemptLoadReplay(replay); });
                    choices[1] = new DialogueChoice(Text.FormatKey("DLG_CHOICE_NO"), () => { });
                    MenuManager.Instance.AddMenu(new ModDiffDialog(Text.FormatKey("DLG_ASK_VERSION_DIFF"), Text.FormatKey("MENU_MODS_DIFF"), modDiffs, false, choices, 0, 1), false);
                }
                else
                    attemptLoadReplay(replay);
            }
        }

        private void attemptLoadReplay(ReplayData replay)
        {
            MenuManager.Instance.RemoveMenu();
            TitleScene.TitleMenuSaveState = MenuManager.Instance.SaveMenuState();

            MenuManager.Instance.ClearMenus();
            GameManager.Instance.SceneOutcome = Replay(replay, false, false);
        }

        private void VerifyAction() {

            ReplayData replay = DataManager.Instance.LoadReplay(recordDir, false);
            if (replay == null)
                cannotRead();
            else
            {
                MenuManager.Instance.RemoveMenu();
                TitleScene.TitleMenuSaveState = MenuManager.Instance.SaveMenuState();

                MenuManager.Instance.ClearMenus();
                GameManager.Instance.SceneOutcome = Replay(replay, true, false);
            }
        }

        private void SeedAction()
        {
            GameProgress ending = DataManager.Instance.GetRecord(recordDir);
            if (ending == null)
                cannotRead();
            else
            {
                SDL.SDL_SetClipboardText(ending.Rand.FirstSeed.ToString("X"));
                GameManager.Instance.SE("Menu/Sort");
            }
        }

        // FavoriteAction and UnFavoriteAction could be moved into one function
        private void FavoriteAction()
        {
            DataManager.Instance.ReplaySetFavorite(recordDir, true);
            MenuManager.Instance.RemoveMenu();
        }

        private void UnFavoriteAction()
        {
            DataManager.Instance.ReplaySetFavorite(recordDir, false);
            MenuManager.Instance.RemoveMenu();
        }

        private void DeleteAction()
        {
            if (File.Exists(recordDir))
                File.Delete(recordDir);

            MenuManager.Instance.RemoveMenu();

            if (DataManager.Instance.FoundRecords(PathMod.ModSavePath(DataManager.REPLAY_PATH), DataManager.REPLAY_EXTENSION))
                MenuManager.Instance.ReplaceMenu(new ReplaysMenu());
            else
            {
                MenuManager.Instance.RemoveMenu();
                MenuManager.Instance.ReplaceMenu(new TopMenu());
            }
        }

        private void ExitAction()
        {
            MenuManager.Instance.RemoveMenu();
        }

        public static IEnumerator<YieldInstruction> Replay(ReplayData replay, bool verifying, bool silent)
        {
            GameManager.Instance.BGM("", true);
            yield return CoroutineManager.Instance.StartCoroutine(GameManager.Instance.FadeOut(false));

            DataManager.Instance.MsgLog.Clear();

            if (replay.States.Count > 0)
            {
                GameState state = replay.ReadState();
                if (state.Save.NextDest.IsValid())
                {
                    DataManager.Instance.SetProgress(state.Save);
                    LuaEngine.Instance.LoadSavedData(DataManager.Instance.Save); //notify script engine
                    ZoneManager.LoadFromState(state.Zone);
                    LuaEngine.Instance.UpdateZoneInstance();

                    if (verifying)
                    {
                        DataManager.Instance.Loading = DataManager.LoadMode.Verifying;
                        if (silent)
                            replay.SilentVerify = true;
                    }
                    DataManager.Instance.CurrentReplay = replay;
                    yield return CoroutineManager.Instance.StartCoroutine(GameManager.Instance.MoveToZone(DataManager.Instance.Save.NextDest, true, false));
                    yield break;
                }
            }

            if (verifying && !silent)
                yield return CoroutineManager.Instance.StartCoroutine(MenuManager.Instance.SetDialogue(Text.FormatKey("DLG_NO_ADVENTURE")));
            GameManager.Instance.SceneOutcome = GameManager.Instance.ReturnToReplayMenu();
        }
    }
}
