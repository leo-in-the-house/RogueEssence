using Avalonia.Controls;
using ReactiveUI;
using RogueEssence.Content;
using RogueEssence.Data;
using RogueEssence.Dev.Views;
using RogueEssence.Dungeon;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using RogueEssence.LevelGen;

namespace RogueEssence.Dev.ViewModels
{
    public class MapTabSpawnsViewModel : ViewModelBase
    {
        public MapTabSpawnsViewModel()
        {
            DevForm form = (DevForm)DiagManager.Instance.DevEditor;
            SpawnBoxWithAlly = new SpawnBoxWithAllyViewModel(form.MapEditForm, new StringConv(typeof(SpecificTeamSpawner), new object[0]));
            Items = new CollectionBoxViewModel(form.MapEditForm, new StringConv(typeof(InvItem), new object[0]));
        }

        //MaxFoes
        public int MaxFoes { get; set; }
        
        //RespawnTime
        public int RespawnTime { get; set; }
        
        //Spawns
        public SpawnBoxWithAllyViewModel SpawnBoxWithAlly { get; set; }
        public int ClumpFactor { get; set; }
        
        //MoneyAmount
        public int MoneyMin { get; set; }
        public int MoneyMax { get; set; }
        
        //ItemSpawns
        public CollectionBoxViewModel Items { get; set; }

        public void LoadMapSpawns()
        {


        }
    }
}
