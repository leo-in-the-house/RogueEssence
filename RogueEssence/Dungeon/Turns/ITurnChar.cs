﻿using System;

namespace RogueEssence.Dungeon
{
    public interface ITurnChar
    {
        bool Dead { get; set; }
        bool TurnUsed { get; set; }
        int TurnWait { get; set; }
        int MovementSpeed { get; set; }
    }
}
