﻿using System;

namespace RogueEssence.Content
{
    [Serializable]
    public class BattleFX
    {
        /// <summary>
        /// After playing this VFX, will wait this many milliseconds before moving to the next one.
        /// </summary>
        public int Delay;

        /// <summary>
        /// Do not modify delay due to batle speed
        /// </summary>
        public bool AbsoluteDelay;

        /// <summary>
        /// The sound effect of the VFX
        /// </summary>
        [Dev.Sound(0)]
        public string Sound;

        /// <summary>
        /// The Particle FX
        /// </summary>
        [Dev.SubGroup]
        public FiniteEmitter Emitter;

        /// <summary>
        /// Screen shake and other effects.
        /// </summary>
        [Dev.SubGroup]
        public ScreenMover ScreenMovement;

        public BattleFX()
        {
            Emitter = new EmptyFiniteEmitter();
            ScreenMovement = new ScreenMover();
            Sound = "";
        }
        public BattleFX(FiniteEmitter emitter, string sound, int delay, bool absolute = false)
        {
            Emitter = emitter;
            Sound = sound;
            Delay = delay;
            AbsoluteDelay = absolute;
            ScreenMovement = new ScreenMover();
        }

        public BattleFX(BattleFX other)
        {
            Delay = other.Delay;
            AbsoluteDelay = other.AbsoluteDelay;
            Emitter = (FiniteEmitter)other.Emitter.Clone();
            ScreenMovement = new ScreenMover(other.ScreenMovement);
            Sound = other.Sound;
        }


        public override string ToString()
        {
            string result = Emitter.ToString();
            if (Sound != "")
                result += ", SE:" + Sound;
            if (Delay > 0)
                result += " +" + Delay;
            return result;
        }
    }
}
