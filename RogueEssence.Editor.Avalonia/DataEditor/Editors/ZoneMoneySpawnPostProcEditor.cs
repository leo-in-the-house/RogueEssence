﻿using System;
using System.Collections.Generic;
using System.Text;
using RogueEssence.Content;
using RogueEssence.Dungeon;
using RogueEssence.Data;
using System.Drawing;
using RogueElements;
using Avalonia.Controls;
using RogueEssence.Dev.Views;
using System.Collections;
using Avalonia;
using System.Reactive.Subjects;
using RogueEssence.LevelGen;

namespace RogueEssence.Dev
{
    public class ZoneMoneySpawnPostProcEditor : Editor<ZoneMoneySpawnPostProc>
    {
        public override string GetString(ZoneMoneySpawnPostProc obj, Type type, object[] attributes)
        {
            string startString = getRangeString(obj.StartAmount);
            string addString = getRangeString(obj.AddAmount);
            //TODO: make this function pull directly from the RandRangeEditor's GetString to create this string.
            //will need to pass attributes of the member
            return string.Format("{0}: Base:{1} Add:{2}", type.Name, startString, addString);
        }

        private string getRangeString(RandRange obj)
        {
            int addMin = 0;
            int addMax = -1;

            if (obj.Min + addMin + 1 >= obj.Max + addMax)
                return obj.Min.ToString();
            else
                return string.Format("{0}-{1}", obj.Min + addMin, obj.Max + addMax);
        }
    }
}
