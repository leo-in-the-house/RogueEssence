﻿using RogueElements;
using Microsoft.Xna.Framework.Input;
using System.IO;

namespace RogueEssence
{
    public class FrameInput
    {

        public enum InputType
        {
            Confirm,
            Cancel,
            Attack,
            Run,
            Skills,
            Turn,
            Diagonal,
            TeamMode,
            Minimap,
            Menu,
            MsgLog,
            SkillMenu,
            ItemMenu,
            TacticMenu,
            TeamMenu,
            LeaderSwap1,
            LeaderSwap2,
            LeaderSwap3,
            LeaderSwap4,
            LeaderSwapBack,
            LeaderSwapForth,
            Skill1,
            Skill2,
            Skill3,
            Skill4,
            SortItems,
            SelectItems,
            SkillPreview,
            Wait,
            LeftMouse,
            //meta input here
            RightMouse,
            MuteMusic,
            ShowDebug,
            Ctrl,
            Pause,
            AdvanceFrame,
            Screenshot,
            SpeedDown,
            SpeedUp,
            SeeAll,
            Restart,
            Test,
            Count
        }


        private bool[] inputStates;

        public bool this[InputType i]
        {
            get
            {
                return inputStates[(int)i];
            }
        }

        public int TotalInputs { get { return inputStates.Length; } }

        public Dir8 Direction { get; private set; }
        public KeyboardState BaseKeyState { get; private set; }
        public GamePadState BaseGamepadState { get; private set; }

        public Loc MouseLoc { get; private set; }
        public int MouseWheel { get; private set; }
        public bool Active { get; private set; }

        public bool HasGamePad => BaseGamepadState.IsConnected;

        public FrameInput()
        {
            inputStates = new bool[(int)InputType.Count];
            Direction = Dir8.None;
        }

        public FrameInput(GamePadState gamePad, KeyboardState keyboard, MouseState mouse, bool keyActive, bool mouseActive, bool screenActive, Loc screenOffset)
        {
            Active = screenActive;
            BaseGamepadState = gamePad;
            BaseKeyState = keyboard;

            Loc dirLoc = new Loc();
            inputStates = new bool[(int)InputType.Count];
            MouseLoc = new Loc(mouse.X, mouse.Y) - screenOffset;

            if (Active)
                ReadDevInput(keyboard, mouse, keyActive, mouseActive);

            keyActive &= screenActive;
            mouseActive &= screenActive;
            bool controllerActive = gamePad.IsConnected;
            controllerActive &= (Active || DiagManager.Instance.CurSettings.InactiveInput);

            if (controllerActive)
            {
                if (gamePad.ThumbSticks.Left.Length() > 0.25f)
                    dirLoc = DirExt.ApproximateDir8(new Loc((int)(gamePad.ThumbSticks.Left.X * 100), (int)(-gamePad.ThumbSticks.Left.Y * 100))).GetLoc();

                //if (gamePad.ThumbSticks.Right.Length() > 0.25f)
                //    dirLoc = DirExt.ApproximateDir8(new Loc((int)(gamePad.ThumbSticks.Right.X * 100), (int)(-gamePad.ThumbSticks.Right.Y * 100))).GetLoc();


                if (gamePad.IsButtonDown(Buttons.DPadDown))
                    dirLoc = dirLoc + Dir4.Down.GetLoc();
                if (gamePad.IsButtonDown(Buttons.DPadLeft))
                    dirLoc = dirLoc + Dir4.Left.GetLoc();
                if (gamePad.IsButtonDown(Buttons.DPadUp))
                    dirLoc = dirLoc + Dir4.Up.GetLoc();
                if (gamePad.IsButtonDown(Buttons.DPadRight))
                    dirLoc = dirLoc + Dir4.Right.GetLoc();

                //if (DiagManager.Instance.CurSettings.ControllerDisablesKeyboard)
                //    keyActive = false;
            }

            if (keyActive)
            {
                if (dirLoc == Loc.Zero)
                {
                    for (int ii = 0; ii < DiagManager.Instance.CurSettings.DirKeys.Length; ii++)
                    {
                        if (keyboard.IsKeyDown(DiagManager.Instance.CurSettings.DirKeys[ii]))
                            dirLoc = dirLoc + ((Dir4)ii).GetLoc();
                    }
                }

                if (dirLoc == Loc.Zero && DiagManager.Instance.CurSettings.NumPad)
                {
                    if (keyboard.IsKeyDown(Keys.NumPad2))
                        dirLoc = dirLoc + Dir8.Down.GetLoc();
                    if (keyboard.IsKeyDown(Keys.NumPad4))
                        dirLoc = dirLoc + Dir8.Left.GetLoc();
                    if (keyboard.IsKeyDown(Keys.NumPad8))
                        dirLoc = dirLoc + Dir8.Up.GetLoc();
                    if (keyboard.IsKeyDown(Keys.NumPad6))
                        dirLoc = dirLoc + Dir8.Right.GetLoc();

                    if (dirLoc == Loc.Zero)
                    {
                        if (keyboard.IsKeyDown(Keys.NumPad3) || keyboard.IsKeyDown(Keys.NumPad1))
                            dirLoc = dirLoc + Dir8.Down.GetLoc();
                        if (keyboard.IsKeyDown(Keys.NumPad1) || keyboard.IsKeyDown(Keys.NumPad7))
                            dirLoc = dirLoc + Dir8.Left.GetLoc();
                        if (keyboard.IsKeyDown(Keys.NumPad7) || keyboard.IsKeyDown(Keys.NumPad9))
                            dirLoc = dirLoc + Dir8.Up.GetLoc();
                        if (keyboard.IsKeyDown(Keys.NumPad9) || keyboard.IsKeyDown(Keys.NumPad3))
                            dirLoc = dirLoc + Dir8.Right.GetLoc();
                    }
                }
            }

            Direction = dirLoc.GetDir();

            if (controllerActive)
            {
                for (int ii = 0; ii < DiagManager.Instance.CurActionButtons.Length; ii++)
                    inputStates[ii] |= Settings.UsedByGamepad((InputType)ii) && gamePad.IsButtonDown(DiagManager.Instance.CurActionButtons[ii]);
            }

            if (keyActive)
            {
                for (int ii = 0; ii < DiagManager.Instance.CurSettings.ActionKeys.Length; ii++)
                    inputStates[ii] |= Settings.UsedByKeyboard((InputType)ii) && keyboard.IsKeyDown(DiagManager.Instance.CurSettings.ActionKeys[ii]);

                if (DiagManager.Instance.CurSettings.Enter)
                    inputStates[(int)InputType.Confirm] |= keyboard.IsKeyDown(Keys.Enter);

                if (DiagManager.Instance.CurSettings.NumPad)
                    inputStates[(int)InputType.Wait] = keyboard.IsKeyDown(Keys.NumPad5);
            }

            if (mouseActive)
            {
                inputStates[(int)InputType.LeftMouse] = (mouse.LeftButton == ButtonState.Pressed);
                inputStates[(int)InputType.RightMouse] = (mouse.RightButton == ButtonState.Pressed);
            }
        }

        public void ReadDevInput(KeyboardState keyboard, MouseState mouse, bool keyActive, bool mouseActive)
        {
            if (keyActive)
            {
                inputStates[(int)InputType.ShowDebug] = keyboard.IsKeyDown(Keys.F1);
                inputStates[(int)InputType.Pause] |= keyboard.IsKeyDown(Keys.F2);
                inputStates[(int)InputType.AdvanceFrame] |= keyboard.IsKeyDown(Keys.F3);
                inputStates[(int)InputType.SpeedDown] |= keyboard.IsKeyDown(Keys.F5);
                inputStates[(int)InputType.SpeedUp] |= keyboard.IsKeyDown(Keys.F6);
                inputStates[(int)InputType.MuteMusic] = keyboard.IsKeyDown(Keys.F8);
            }

            if (DiagManager.Instance.DevMode)
            {
                if (mouseActive)
                    MouseWheel = mouse.ScrollWheelValue;

                if (keyActive)
                {
                    inputStates[(int)InputType.Ctrl] |= (keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl));

                    inputStates[(int)InputType.Test] |= keyboard.IsKeyDown(Keys.F4);
                    //inputStates[(int)InputType.] |= keyboard.IsKeyDown(Keys.F7);
                    //inputStates[(int)InputType.] |= keyboard.IsKeyDown(Keys.F8);
                    inputStates[(int)InputType.SeeAll] |= keyboard.IsKeyDown(Keys.F9);
                    inputStates[(int)InputType.Screenshot] |= keyboard.IsKeyDown(Keys.F11);
                    inputStates[(int)InputType.Restart] |= keyboard.IsKeyDown(Keys.F12);
                }
            }
        }


        public override bool Equals(object obj)
        {
            return (obj is FrameInput) && Equals((FrameInput)obj);
        }

        public bool Equals(FrameInput other)
        {
            if (Direction != other.Direction) return false;

            for (int ii = 0; ii < (int)InputType.Count; ii++)
            {
                if (inputStates[ii] != other.inputStates[ii]) return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return Direction.GetHashCode() ^ inputStates.GetHashCode();
        }

        public static bool operator ==(FrameInput input1, FrameInput input2)
        {
            return input1.Equals(input2);
        }

        public static bool operator !=(FrameInput input1, FrameInput input2)
        {
            return !(input1 == input2);
        }


        public static FrameInput Load(BinaryReader reader)
        {
            FrameInput input = new FrameInput();

            input.Direction = (Dir8)((int)reader.ReadByte());
            for (int ii = 0; ii < (int)FrameInput.InputType.Ctrl; ii++)
                input.inputStates[ii] = reader.ReadBoolean();
            //for (int ii = 0; ii < FrameInput.TOTAL_CHARS; ii++)
            //    input.CharInput[ii] = reader.ReadBoolean();
            return input;
        }
    }
}
